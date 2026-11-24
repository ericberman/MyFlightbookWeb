using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Signs and validates URLs for protected resources (images, digitized signatures, etc.)
    /// 
    /// The model: the page that RENDERS a URL decides whether the viewer may see the resource (CheckCanViewFlights, PubController,
    /// ImageAuthorization.ValidateViewAuth, etc.), and emits a URL carrying an expiration and an HMAC over (payload, expiration).
    /// The endpoint that SERVES the resource checks only the signature - no session, no database - so it works for unauthenticated
    /// share-link viewers, for wkhtmltopdf (which sends no cookies), and for long-lived links in exports or the mobile apps.
    /// 
    /// Expirations are quantized so that a given resource produces the same URL for a while, which keeps browser caching effective.
    /// 
    /// Configuration (localconfig table):
    ///  - ResourceUrlSigningKey: base64-encoded secret, at least 32 bytes.  If empty, URLs are emitted unsigned, so this is safe to deploy before configuring.
    ///  - ResourceUrlSigningKeyPrevious: optional (may be empty); the prior key, still accepted during a key rotation.
    ///  - RequireSignedResourceUrls: "yes" to reject unsigned/invalid/expired requests.  Otherwise they are served (callers should log them).
    /// All three keys must be present in localconfig (empty values are fine).
    /// </summary>
    public static class ResourceUrlSigner
    {
        public const string ParamExpiration = "exp";
        public const string ParamSignature = "sig";

        private const string szConfigKeyCurrent = "ResourceUrlSigningKey";
        private const string szConfigKeyPrevious = "ResourceUrlSigningKeyPrevious";
        private const string szConfigEnforce = "RequireSignedResourceUrls";

        private const int signatureBytes = 16;  // 128 bits of HMAC-SHA256 is plenty, and keeps URLs short

        /// <summary>
        /// Default lifetime for URLs rendered into web pages.
        /// </summary>
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(1);

        /// <summary>
        /// Lifetime for URLs that get stored somewhere: export files, mobile app caches.
        /// </summary>
        public static readonly TimeSpan LongLifetime = TimeSpan.FromDays(400);

        public enum ValidationResult { Valid, Unsigned, Expired, Invalid }

        #region Lifetime selection
        private static readonly AsyncLocal<TimeSpan?> ambientLifetime = new AsyncLocal<TimeSpan?>();

        private sealed class LifetimeScope : IDisposable
        {
            private readonly TimeSpan? previous;
            public LifetimeScope(TimeSpan? prev) { previous = prev; }
            public void Dispose() { ambientLifetime.Value = previous; }
        }

        /// <summary>
        /// Overrides the lifetime for URLs signed within the scope (e.g., an export file generated during an interactive request).
        /// Usage: using (ResourceUrlSigner.UseLifetime(ResourceUrlSigner.LongLifetime)) { ... }
        /// </summary>
        public static IDisposable UseLifetime(TimeSpan lifetime)
        {
            TimeSpan? prev = ambientLifetime.Value;
            ambientLifetime.Value = lifetime;
            return new LifetimeScope(prev);
        }

        // Requests whose responses are consumed by apps (which cache URLs) rather than rendered into a page.  Note that the asmx serializer reads
        // URL properties AFTER the web method returns, but still within the request, so keying off of the request works where a scope wouldn't.
        private static readonly string[] apiPaths = new string[] { "/WebService.asmx", "/oAuth/OAuthResource" };

        /// <summary>
        /// The lifetime to use for a URL signed right now:
        ///  - an explicit UseLifetime scope, if any; otherwise
        ///  - LongLifetime if there is no HTTP request (background jobs, nightly emails/backups) or this is a web service/API request; otherwise
        ///  - DefaultLifetime (a web page).
        /// </summary>
        public static TimeSpan CurrentLifetime
        {
            get
            {
                if (ambientLifetime.Value.HasValue)
                    return ambientLifetime.Value.Value;

                Uri uri = util.RequestContext?.CurrentRequestUrl;
                if (uri == null)
                    return LongLifetime;

                foreach (string szPath in apiPaths)
                    if (uri.AbsolutePath.IndexOf(szPath, StringComparison.OrdinalIgnoreCase) >= 0)
                        return LongLifetime;

                return DefaultLifetime;
            }
        }
        #endregion

        #region Configuration
        // These keys must exist in localconfig (possibly with empty values); LocalConfig caches them.
        private static byte[] KeyFromSetting(string szKey)
        {
            string szSetting = LocalConfig.SettingForKey(szKey);
            if (String.IsNullOrWhiteSpace(szSetting))
                return null;
            try
            {
                return Convert.FromBase64String(szSetting.Trim());
            }
            catch (FormatException)
            {
                return Encoding.UTF8.GetBytes(szSetting.Trim());
            }
        }

        private static byte[] CurrentKey { get { return KeyFromSetting(szConfigKeyCurrent); } }

        private static byte[] PreviousKey { get { return KeyFromSetting(szConfigKeyPrevious); } }

        /// <summary>
        /// True if a signing key is configured (i.e., URLs will be signed)
        /// </summary>
        public static bool IsConfigured
        {
            get { return !String.IsNullOrWhiteSpace(LocalConfig.SettingForKey(szConfigKeyCurrent)); }
        }

        /// <summary>
        /// True if requests that are not validly signed should be rejected
        /// </summary>
        public static bool EnforceSignatures
        {
            get { return IsConfigured && String.Compare(LocalConfig.SettingForKey(szConfigEnforce), "yes", StringComparison.OrdinalIgnoreCase) == 0; }
        }
        #endregion

        #region Signing
        private static long NowUnix { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

        /// <summary>
        /// Computes a quantized expiration (unix seconds) that is at least lifetime from now.  Quantization is to the lesser of the lifetime or one day,
        /// so a given resource produces the same URL for that long.
        /// </summary>
        public static long ExpirationFor(TimeSpan lifetime)
        {
            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifetime));
            long quantum = (long)Math.Min(lifetime.TotalSeconds, TimeSpan.FromDays(1).TotalSeconds);
            long target = NowUnix + (long)lifetime.TotalSeconds;
            return ((target / quantum) + 1) * quantum;
        }

        private static string ComputeSignature(byte[] key, string payload, long exp)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(String.Format(CultureInfo.InvariantCulture, "{0}\n{1}", payload, exp)));
                return Convert.ToBase64String(hash, 0, signatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        /// <summary>
        /// Returns the query string fragment ("exp=...&amp;sig=...") that signs the specified payload, or an empty string if signing isn't configured.
        /// </summary>
        /// <param name="payload">A canonical string identifying the resource; the validator must construct exactly the same string.</param>
        /// <param name="lifetime">How long the URL should remain valid (at minimum)</param>
        public static string SignedQuery(string payload, TimeSpan lifetime)
        {
            if (String.IsNullOrEmpty(payload))
                throw new ArgumentNullException(nameof(payload));
            byte[] key = CurrentKey;
            if (key == null)
                return string.Empty;
            long exp = ExpirationFor(lifetime);
            return String.Format(CultureInfo.InvariantCulture, "{0}={1}&{2}={3}", ParamExpiration, exp, ParamSignature, ComputeSignature(key, payload, exp));
        }

        /// <summary>
        /// Appends a signature for the payload to the URL.  Returns the URL unchanged if signing isn't configured.
        /// </summary>
        public static string AppendSignature(string url, string payload, TimeSpan? lifetime = null)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            string q = SignedQuery(payload, lifetime ?? CurrentLifetime);
            return String.IsNullOrEmpty(q) ? url : url + (url.Contains("?") ? "&" : "?") + q;
        }

        /// <summary>
        /// Validates a signed request.
        /// </summary>
        /// <param name="payload">The canonical payload, constructed by the endpoint from the request (NOT taken from the request as-is)</param>
        /// <param name="exp">The exp parameter from the request</param>
        /// <param name="sig">The sig parameter from the request</param>
        /// <param name="secondsRemaining">For a valid signature, the number of seconds until it expires (useful for cache headers); otherwise 0</param>
        public static ValidationResult Validate(string payload, string exp, string sig, out long secondsRemaining)
        {
            secondsRemaining = 0;
            if (String.IsNullOrEmpty(exp) && String.IsNullOrEmpty(sig))
                return ValidationResult.Unsigned;

            byte[] key = CurrentKey;
            if (key == null || String.IsNullOrEmpty(payload) || String.IsNullOrEmpty(sig) || !long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lExp))
                return ValidationResult.Invalid;

            byte[] keyPrevious;
            if (!FixedTimeEquals(sig, ComputeSignature(key, payload, lExp)) && ((keyPrevious = PreviousKey) == null || !FixedTimeEquals(sig, ComputeSignature(keyPrevious, payload, lExp))))
                return ValidationResult.Invalid;

            long remaining = lExp - NowUnix;
            if (remaining <= 0)
                return ValidationResult.Expired;

            secondsRemaining = remaining;
            return ValidationResult.Valid;
        }
        #endregion

        #region Specific resources
        /// <summary>
        /// Payload for an image (thumbnail or full).  Anyone who may see the thumbnail may see the full image, so they share a payload.
        /// </summary>
        public static string ImagePayload(MFBImageInfoBase.ImageClass ic, string key, string thumbnailFile)
        {
            return String.Format(CultureInfo.InvariantCulture, "Img|{0}|{1}|{2}", ic.ToString(), key, thumbnailFile);
        }

        /// <summary>
        /// Payload for a flight's digitized CFI signature
        /// </summary>
        public static string FlightSignaturePayload(int idFlight)
        {
            return String.Format(CultureInfo.InvariantCulture, "FlightSig|{0}", idFlight);
        }

        /// <summary>
        /// Signed, absolute URL for a flight's digitized CFI signature.  Caller is responsible for having determined that the viewer may see the flight.
        /// </summary>
        public static string FlightSignatureUrl(int idFlight, TimeSpan? lifetime = null)
        {
            return AppendSignature(String.Format(CultureInfo.InvariantCulture, "~/mvc/Image/FlightSig/{0}", idFlight).ToAbsolute(), FlightSignaturePayload(idFlight), lifetime);
        }
        #endregion
    }
}
