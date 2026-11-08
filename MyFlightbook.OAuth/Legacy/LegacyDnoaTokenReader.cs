using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * Read-only decoder for tokens minted by DotNetOpenAuth 4.3.x, so that refresh tokens
 * (and, during the cutover window, access tokens) issued before the migration keep working
 * without DotNetOpenAuth.
 *
 * The wire formats below were derived from the DotNetOpenAuth 4.3 source (MS-PL), specifically
 * DataBagFormatterBase.Serialize, MessagingUtilities.Encrypt / EncryptWithRandomSymmetricKey,
 * UriStyleMessageFormatter, DataBag, AuthorizationDataBag, AccessToken, and RefreshToken.
 *
 * This class only ever READS legacy tokens; nothing new is ever minted in this format.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Legacy
{
    /// <summary>
    /// The decoded contents of a legacy DotNetOpenAuth token
    /// </summary>
    public sealed class LegacyTokenContents
    {
        /// <summary>The client ID the token was issued to (for PKCE tokens this is the proxy client, not the real client)</summary>
        public string ClientIdentifier { get; internal set; }

        /// <summary>The MyFlightbook username</summary>
        public string User { get; internal set; }

        /// <summary>The granted scopes</summary>
        public HashSet<string> Scope { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>When the token was minted (UTC, 1-second precision)</summary>
        public DateTime UtcIssued { get; internal set; }

        /// <summary>Access tokens only: the lifetime of the token, if any</summary>
        public TimeSpan? Lifetime { get; internal set; }

        /// <summary>The .NET type name that DNOA recorded for the bag (e.g., "DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken")</summary>
        public string BagType { get; internal set; }

        /// <summary>Access tokens only: has the token expired?</summary>
        public bool IsExpired(DateTime utcNow)
        {
            return Lifetime.HasValue && UtcIssued.Add(Lifetime.Value) < utcNow;
        }
    }

    public static class LegacyDnoaTokenReader
    {
        /// <summary>
        /// The cryptokeys.bucket value under which DNOA stored the symmetric keys for refresh tokens
        /// </summary>
        public const string RefreshTokenKeyBucket = "https://localhost/dnoa/oauth_refresh_token";

        private const string RefreshTokenBagType = "DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken";
        private const string AccessTokenBagType = "DotNetOpenAuth.OAuth2.AccessToken";

        // Max sizes that DNOA itself enforced when reading
        private const int MaxSignatureLength = 1024;
        private const int MaxDataLength = 8 * 1024;
        private const int MaxEncryptedPrequelLength = 4096;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Quick structural test: legacy refresh tokens are "{4-char handle}!{base64url}".  Doesn't validate anything; use it only to route.
        /// </summary>
        public static bool LooksLikeLegacyRefreshToken(string token)
        {
            if (String.IsNullOrEmpty(token))
                return false;
            int i = token.IndexOf('!');
            return i > 0 && i < token.Length - 1;
        }

        #region Refresh tokens
        /// <summary>
        /// Decodes and validates a legacy refresh token.
        /// </summary>
        /// <param name="token">The refresh token as presented by the client</param>
        /// <param name="keyLookup">Returns the raw symmetric key bytes for (bucket, handle) from the cryptokeys table, or null if not found.
        /// NOTE: DNOA never checked cryptokeys.ExpiresUtc when DECODING (only when choosing a key to ENCODE with), so neither should this.</param>
        /// <param name="contents">The decoded token, if successful</param>
        /// <returns>True if the token is authentic and well-formed.  This does NOT check whether the underlying authorization is still valid - the caller must do that.</returns>
        public static bool TryReadRefreshToken(string token, Func<string, string, byte[]> keyLookup, out LegacyTokenContents contents)
        {
            contents = null;
            if (keyLookup == null)
                throw new ArgumentNullException(nameof(keyLookup));
            if (!LooksLikeLegacyRefreshToken(token))
                return false;

            try
            {
                int iBang = token.IndexOf('!');
                string handle = token.Substring(0, iBang);
                string payload = token.Substring(iBang + 1);

                byte[] key = keyLookup(RefreshTokenKeyBucket, handle);
                if (key == null || key.Length == 0)
                    return false;

                if (!TryReadSignedEnvelope(payload.FromBase64URL(), out byte[] signature, out byte[] cipherText))
                    return false;

                // Signature is HMAC-SHA256 over the (encrypted) data, using the same symmetric key
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    if (!ConstantTimeEquals(signature, hmac.ComputeHash(cipherText)))
                        return false;
                }

                byte[] plainText = DecryptSymmetric(cipherText, key);
                contents = ParseBag(plainText);
                if (contents.BagType != RefreshTokenBagType)
                {
                    contents = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException || ex is EndOfStreamException || ex is ArgumentException || ex is OverflowException || ex is InvalidDataException)
            {
                contents = null;
                return false;
            }
        }

        /// <summary>
        /// Version 1 of DNOA's symmetric encryption: [byte version=1][16-byte IV][AES-256-CBC/PKCS7 ciphertext]
        /// (DNOA used SymmetricAlgorithm.Create(), which on .NET Framework is Rijndael with a 128-bit block, i.e., AES.)
        /// </summary>
        private static byte[] DecryptSymmetric(byte[] buffer, byte[] key)
        {
            if (buffer.Length < 1 + 16 || buffer[0] != 1)
                throw new InvalidDataException("Unsupported legacy encryption version");

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                byte[] iv = new byte[16];
                Array.Copy(buffer, 1, iv, 0, 16);
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    return decryptor.TransformFinalBlock(buffer, 17, buffer.Length - 17);
            }
        }
        #endregion

        #region Access tokens
        /// <summary>
        /// Decodes and validates a legacy access token.  Only needed for the cutover window (legacy access tokens were issued with a 14-day lifetime).
        /// </summary>
        /// <param name="token">The access token, as presented by the client</param>
        /// <param name="signingPublicKey">The authorization server's RSA public key (MyFlightbookContainer)</param>
        /// <param name="encryptionPrivateKey">The resource server's RSA private key (also MyFlightbookContainer)</param>
        /// <param name="contents">The decoded token</param>
        /// <returns>True if authentic and well-formed.  Caller must still check expiration (contents.IsExpired) and user.</returns>
        public static bool TryReadAccessToken(string token, RSA signingPublicKey, RSA encryptionPrivateKey, out LegacyTokenContents contents)
        {
            contents = null;
            if (signingPublicKey == null)
                throw new ArgumentNullException(nameof(signingPublicKey));
            if (encryptionPrivateKey == null)
                throw new ArgumentNullException(nameof(encryptionPrivateKey));
            if (String.IsNullOrEmpty(token) || token.IndexOf('!') >= 0)
                return false;

            try
            {
                if (!TryReadSignedEnvelope(token.FromBase64URL(), out byte[] signature, out byte[] data))
                    return false;

                // RSA PKCS#1 v1.5 signature with SHA-1 over the (encrypted) data
                if (!signingPublicKey.VerifyData(data, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                    return false;

                byte[] plainText;
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    // [int32 len][RSA-PKCS1v1.5(symmetric key || IV)][AES-CBC ciphertext]
                    byte[] encryptedPrequel = ReadBuffer(br, MaxEncryptedPrequelLength);
                    byte[] prequel = encryptionPrivateKey.Decrypt(encryptedPrequel, RSAEncryptionPadding.Pkcs1);
                    if (prequel.Length != 32 + 16)
                        return false;

                    using (Aes aes = Aes.Create())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        byte[] k = new byte[32], iv = new byte[16];
                        Array.Copy(prequel, 0, k, 0, 32);
                        Array.Copy(prequel, 32, iv, 0, 16);
                        aes.Key = k;
                        aes.IV = iv;
                        int offset = (int)ms.Position;
                        using (ICryptoTransform decryptor = aes.CreateDecryptor())
                            plainText = decryptor.TransformFinalBlock(data, offset, data.Length - offset);
                    }
                }

                contents = ParseBag(plainText);
                if (contents.BagType != AccessTokenBagType)
                {
                    contents = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException || ex is EndOfStreamException || ex is ArgumentException || ex is OverflowException || ex is InvalidDataException)
            {
                contents = null;
                return false;
            }
        }
        #endregion

        #region Shared helpers
        /// <summary>
        /// DNOA's outer envelope for signed bags: [int32 len][signature][int32 len][data], little-endian lengths (BinaryWriter)
        /// </summary>
        private static bool TryReadSignedEnvelope(byte[] envelope, out byte[] signature, out byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(envelope))
            using (BinaryReader br = new BinaryReader(ms))
            {
                signature = ReadBuffer(br, MaxSignatureLength);
                data = ReadBuffer(br, MaxDataLength);
                return ms.Position == ms.Length;
            }
        }

        private static byte[] ReadBuffer(BinaryReader br, int maxLength)
        {
            int length = br.ReadInt32();
            if (length < 0 || length > maxLength)
                throw new InvalidDataException("Buffer length out of range");
            byte[] result = br.ReadBytes(length);
            if (result.Length != length)
                throw new EndOfStreamException();
            return result;
        }

        /// <summary>
        /// The plaintext is a UTF-8 query string, e.g. "ClientIdentifier=abc&amp;User=joe&amp;Scope=currency%20totals&amp;ts=1700000000&amp;t=DotNetOpenAuth...."
        /// </summary>
        private static LegacyTokenContents ParseBag(byte[] plainText)
        {
            Dictionary<string, string> fields = ParseQueryString(Encoding.UTF8.GetString(plainText));
            LegacyTokenContents result = new LegacyTokenContents()
            {
                ClientIdentifier = Field(fields, "ClientIdentifier"),
                User = Field(fields, "User"),
                BagType = Field(fields, "t"),
                UtcIssued = Epoch.AddSeconds(long.Parse(Field(fields, "ts") ?? throw new InvalidDataException("missing ts"), NumberStyles.Integer, CultureInfo.InvariantCulture))
            };

            string scopes = Field(fields, "Scope");
            if (!String.IsNullOrEmpty(scopes))
                result.Scope.UnionWith(scopes.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            string lifetime = Field(fields, "Lifetime");
            if (!String.IsNullOrEmpty(lifetime))
                result.Lifetime = TimeSpan.FromSeconds(int.Parse(lifetime, NumberStyles.Integer, CultureInfo.InvariantCulture));

            if (String.IsNullOrEmpty(result.ClientIdentifier))
                throw new InvalidDataException("missing client identifier");

            return result;
        }

        /// <summary>
        /// Minimal application/x-www-form-urlencoded parser (no System.Web in netstandard).  DNOA wrote each field once, RFC 3986-escaped.
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string pair in query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = pair.IndexOf('=');
                string key = WebUtility.UrlDecode(i < 0 ? pair : pair.Substring(0, i));
                string value = i < 0 ? string.Empty : WebUtility.UrlDecode(pair.Substring(i + 1));
                if (result.ContainsKey(key))
                    throw new InvalidDataException("Duplicate field in legacy token");
                result[key] = value;
            }
            return result;
        }

        private static string Field(Dictionary<string, string> fields, string name)
        {
            return fields.TryGetValue(name, out string value) ? value : null;
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
        #endregion
    }
}
