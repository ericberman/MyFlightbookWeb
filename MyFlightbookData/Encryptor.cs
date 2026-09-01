using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Encryptors
{
    #region concrete encryption classes
    public class UserAccessEncryptor : MFBEncryptor
    {
        public UserAccessEncryptor() : base(LocalConfig.SettingForKey("UserAccessEncryptorKey")) { }
    }

    /// <summary>
    /// Encryptor to be used on a per-user basis (uses the username as the key) - not super secure, but enough to make
    /// tampering hard. ONLY USE THIS ON DATA THAT IS NOT EXPOSED (i.e., flight signatures).
    ///
    /// IMPORTANT: This stays on the legacy deterministic algorithm (same plaintext -> same ciphertext, always).
    /// LogbookEntry.IsValidSignature()/IsValidSigningDetails() compare ciphertext directly rather than decrypting
    /// first, so this class must never emit the randomized "v2:" format - that would invalidate every existing
    /// flight signature the moment it shipped.
    /// </summary>
    public class UserEncryptor : MFBEncryptor
    {
        public UserEncryptor(string szUser) : base(szUser) { }

        protected override bool IsDeterministic => true;
    }

    public class SharedDataEncryptor : MFBEncryptor
    {
        public SharedDataEncryptor() : base(LocalConfig.SettingForKey("SharedDataEncryptorKey")) { }
    }

    public class WebServiceEncryptor : MFBEncryptor
    {
        public WebServiceEncryptor() : base(LocalConfig.SettingForKey("WebAccessEncryptorKey")) { }
    }

    public class PeerRequestEncryptor : MFBEncryptor
    {
        public PeerRequestEncryptor() : base(LocalConfig.SettingForKey("PeerRequestEncryptorKey")) { }
    }

    public class AdminAuthEncryptor : MFBEncryptor
    {
        public AdminAuthEncryptor() : base(LocalConfig.SettingForKey("AdminAuthAccessKey")) { }
    }
    #endregion

    /// <summary>
    /// Provides simple symmetrical encryption services for a variety of applications.
    /// </summary>
    public abstract class MFBEncryptor
    {
        private const int KeySize = 32;           // AES-256
        private const int IVSize = 16;
        private const int Pbkdf2Iterations = 100_000;
        private const string V2Prefix = "v2:";
        private static readonly TimeSpan KeyCacheLifetime = TimeSpan.FromMinutes(30);

        private readonly string m_szPass;

        protected MFBEncryptor(string szPass)
        {
            m_szPass = szPass ?? string.Empty;
        }

        /// <summary>
        /// True for encryptors whose ciphertext must be directly, byte-for-byte comparable for identical
        /// plaintext. Those stay on the legacy fixed-salt/fixed-IV algorithm. Default is false (random IV).
        /// </summary>
        protected virtual bool IsDeterministic => false;

        #region key-derivation caching (shared across instances - every call site does "new XEncryptor()")
        private sealed class CacheEntry
        {
            public byte[] Bytes;
            public DateTime ExpiresUtc;
        }

        private static readonly ConcurrentDictionary<string, CacheEntry> s_cache = new ConcurrentDictionary<string, CacheEntry>();

        private byte[] GetOrAddCached(string tag, Func<byte[]> compute)
        {
            string cacheKey = GetType().FullName + "|" + tag + "|" + m_szPass;
            if (s_cache.TryGetValue(cacheKey, out CacheEntry entry) && entry.ExpiresUtc > DateTime.UtcNow)
                return entry.Bytes;

            byte[] bytes = compute();
            s_cache[cacheKey] = new CacheEntry { Bytes = bytes, ExpiresUtc = DateTime.UtcNow.Add(KeyCacheLifetime) };
            return bytes;
        }
        #endregion

        #region v2: random IV per call, cached key (everything except IsDeterministic encryptors)
        private static byte[] FixedSaltFor(Type t)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(t.FullName));
        }

        private byte[] DerivedKeyV2()
        {
            return GetOrAddCached("v2", () =>
            {
                using (var kdf = new Rfc2898DeriveBytes(m_szPass, FixedSaltFor(GetType()), Pbkdf2Iterations))
                    return kdf.GetBytes(KeySize);
            });
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] buffer = new byte[count];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(buffer);
            return buffer;
        }

        private static byte[] AesEncryptBytes(string plaintext, Aes aes)
        {
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding.Unicode))
                {
                    swEncrypt.Write(plaintext);
                }
                // csEncrypt's Dispose() flushes the final padded block into msEncrypt before we read it here.
                return msEncrypt.ToArray();
            }
        }

        private static string AesDecryptBytes(byte[] cipherBytes, Aes aes)
        {
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.Unicode))
            {
                return srDecrypt.ReadToEnd();
            }
        }

        private string EncryptV2(string plaintext)
        {
            byte[] iv = RandomBytes(IVSize);
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = DerivedKeyV2();
                aes.IV = iv;
                byte[] cipherBytes = AesEncryptBytes(plaintext, aes);
                return V2Prefix + Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(cipherBytes);
            }
        }

        private string DecryptV2(string cipherText)
        {
            string[] parts = cipherText.Substring(V2Prefix.Length).Split(':');
            if (parts.Length != 2)
                return string.Empty;

            try
            {
                byte[] iv = Convert.FromBase64String(parts[0]);
                byte[] cipherBytes = Convert.FromBase64String(parts[1]);

                using (Aes aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = DerivedKeyV2();
                    aes.IV = iv;
                    return AesDecryptBytes(cipherBytes, aes);
                }
            }
            catch (FormatException) { return string.Empty; }
            catch (CryptographicException) { return string.Empty; }
        }
        #endregion

        #region legacy: original deterministic algorithm, untouched output, just with cached key/IV material
        private byte[] LegacyKeyAndIV()
        {
            return GetOrAddCached("legacy", () =>
            {
                byte[] salt = Encoding.ASCII.GetBytes(m_szPass.Length.ToString(CultureInfo.InvariantCulture));
                using (PasswordDeriveBytes pdb = new PasswordDeriveBytes(m_szPass, salt))
                {
                    byte[] key = pdb.GetBytes(KeySize);   // same two calls, same order, as the original code
                    byte[] iv = pdb.GetBytes(IVSize);
                    byte[] combined = new byte[KeySize + IVSize];
                    Buffer.BlockCopy(key, 0, combined, 0, KeySize);
                    Buffer.BlockCopy(iv, 0, combined, KeySize, IVSize);
                    return combined;
                }
            });
        }

        private string EncryptLegacy(string textToBeEncrypted)
        {
            using (RijndaelManaged rijndaelCipher = new RijndaelManaged())
            {
                byte[] plainText = Encoding.Unicode.GetBytes(textToBeEncrypted);
                byte[] keyAndIv = LegacyKeyAndIV();
                byte[] key = new byte[KeySize];
                byte[] iv = new byte[IVSize];
                Buffer.BlockCopy(keyAndIv, 0, key, 0, KeySize);
                Buffer.BlockCopy(keyAndIv, KeySize, iv, 0, IVSize);

                using (ICryptoTransform encryptor = rijndaelCipher.CreateEncryptor(key, iv))
                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainText, 0, plainText.Length);
                    cryptoStream.FlushFinalBlock();
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        private string DecryptLegacy(string textToBeDecrypted)
        {
            using (RijndaelManaged rijndaelCipher = new RijndaelManaged())
            {
                try
                {
                    byte[] encryptedData = Convert.FromBase64String(textToBeDecrypted);
                    byte[] keyAndIv = LegacyKeyAndIV();
                    byte[] key = new byte[KeySize];
                    byte[] iv = new byte[IVSize];
                    Buffer.BlockCopy(keyAndIv, 0, key, 0, KeySize);
                    Buffer.BlockCopy(keyAndIv, KeySize, iv, 0, IVSize);

                    using (ICryptoTransform decryptor = rijndaelCipher.CreateDecryptor(key, iv))
                    using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (StreamReader reader = new StreamReader(cryptoStream, Encoding.Unicode))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (FormatException) { return string.Empty; }
                catch (CryptographicException) { return string.Empty; }
            }
        }
        #endregion

        public string Encrypt(string plaintext)
        {
            return IsDeterministic ? EncryptLegacy(plaintext) : EncryptV2(plaintext);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            return cipherText.StartsWith(V2Prefix, StringComparison.Ordinal)
                ? DecryptV2(cipherText)
                : DecryptLegacy(cipherText);
        }
    }
}