using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Serves images, thumbnails, and digitized signatures.
    /// 
    /// Authorization happens when the page/service that emits the URL is rendered; these endpoints only verify the URL's signature (see ResourceUrlSigner),
    /// so they need neither a session nor the database.  Session state is disabled so that concurrent image requests from a page don't serialize
    /// on the session lock.  Routes in mvcAreaRegistration keep the historical /mvc/Image/... URLs pointing here.
    /// </summary>
    [SessionState(SessionStateBehavior.Disabled)]
    public class ImageServeController : Controller
    {
        private static readonly char[] rgUnsafePathChars = new char[] { '/', '\\', ':', '?', '*', '"', '<', '>', '|' };

        /// <summary>
        /// True if the string is safe to use as a single path component (a key or a filename) - i.e., can't be used to walk out of the image folder.
        /// </summary>
        private static bool IsSafePathComponent(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && s.IndexOfAny(rgUnsafePathChars) < 0 && !s.Contains("..");
        }

        /// <summary>
        /// Serves a local image file, provided that it is actually within the image folder for its class.
        /// </summary>
        private ActionResult ServeLocalImageFile(MFBImageInfoBase.ImageClass ic, string szVirtualPath)
        {
            string szBase = MFBImageInfoBase.BasePathFromClass(ic);
            if (String.IsNullOrEmpty(szBase) || String.IsNullOrEmpty(szVirtualPath))
                return HttpNotFound();

            try
            {
                string szRoot = System.IO.Path.GetFullPath(szBase.MapAbsoluteFilePath()).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar;
                string szFull = System.IO.Path.GetFullPath(szVirtualPath.MapAbsoluteFilePath());
                if (!szFull.StartsWith(szRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(szFull))
                    return HttpNotFound();
                return File(szFull, MimeMapping.GetMimeMapping(szFull));
            }
            catch (Exception ex) when (ex is HttpException || ex is ArgumentException || ex is NotSupportedException || ex is System.IO.PathTooLongException)
            {
                return HttpNotFound();
            }
        }

        /// <summary>
        /// Serves a thumbnail.  Aircraft thumbnails are public (and normally served statically anyhow); all others require a signed URL (see ResourceUrlSigner),
        /// which the page that rendered it issued after authorizing the viewer.
        /// </summary>
        public ActionResult Thumb(string r, string k, string t, string exp = null, string sig = null)
        {
            if (!Enum.TryParse(r, out MFBImageInfoBase.ImageClass ic) || !IsSafePathComponent(k) || !IsSafePathComponent(t) || !t.EndsWith(FileExtensions.JPG, StringComparison.OrdinalIgnoreCase))
                return HttpNotFound();

            if (ic != MFBImageInfoBase.ImageClass.Aircraft && !CheckSignedRequest(ResourceUrlSigner.ImagePayload(ic, k, t), exp, sig))
                return HttpNotFound();

            return ServeLocalImageFile(ic, String.Format(CultureInfo.InvariantCulture, "{0}{1}/{2}", MFBImageInfoBase.BasePathFromClass(ic), k, t));
        }

        /// <summary>
        /// Serves a full-sized image/PDF/video.  Aircraft images are public; all others require a signed URL (see ResourceUrlSigner).
        /// Local files are served directly; S3 files are redirected to.
        /// </summary>
        public ActionResult ViewPic(string r, string k, string t, string exp = null, string sig = null)
        {
            // issue #1389 - seeing a bunch of requests that include an encoded ampersand.  If we see that, redirect to notfound
            if (t?.Contains('\\') ?? true)
                return HttpNotFound();

            if (Enum.TryParse(r, out MFBImageInfoBase.ImageClass ic) && !String.IsNullOrEmpty(k) && !String.IsNullOrEmpty(t) && !t.Contains("?"))  // Googlebot seems to be adding "?resize=300,300
            {
                if (!IsSafePathComponent(k) || !IsSafePathComponent(t))
                    return HttpNotFound();

                bool fPublic = ic == MFBImageInfoBase.ImageClass.Aircraft;
                if (!fPublic && !CheckSignedRequest(ResourceUrlSigner.ImagePayload(ic, k, t), exp, sig))
                    return HttpNotFound();

                MFBImageInfo mfbii = new MFBImageInfo(ic, k, t);
                if (mfbii.IsLocal)
                    return fPublic ? Redirect(mfbii.PathFullImage) : ServeLocalImageFile(ic, mfbii.PathFullImage);
                return Redirect(mfbii.PathFullImageS3);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Checks a signed resource request; returns true if the request should be served.
        /// The page that rendered the URL is responsible for authorization; here we only verify the signature, so no session is needed
        /// (share links, wkhtmltopdf, exports).  Until RequireSignedResourceUrls is on, unsigned/invalid/expired requests are still served
        /// but logged, so that we can find code paths that still emit unsigned URLs.
        /// </summary>
        private bool CheckSignedRequest(string payload, string exp, string sig)
        {
            ResourceUrlSigner.ValidationResult result = ResourceUrlSigner.Validate(payload, exp, sig, out long secondsRemaining);
            if (result == ResourceUrlSigner.ValidationResult.Valid)
            {
                // Signed URLs are stable for a while (expiration is quantized), so let the browser cache - but never beyond the expiration.
                Response.Cache.SetCacheability(HttpCacheability.Private);
                Response.Cache.SetMaxAge(TimeSpan.FromSeconds(Math.Min(secondsRemaining, (long)TimeSpan.FromDays(1).TotalSeconds)));
                return true;
            }

            // Nothing to log (or enforce) until a signing key is configured
            if (!ResourceUrlSigner.IsConfigured)
                return true;

            MyFlightbook.SharedUtility.EventRecorder.EventRecorder.LogCall("Signed resource check: {result} for {url}, referrer {referrer}", result.ToString(), Request.Url?.PathAndQuery ?? string.Empty, Request.UrlReferrer?.ToString() ?? "(none)");
            return !ResourceUrlSigner.EnforceSignatures;
        }

        public ActionResult FlightSig(int id, string exp = null, string sig = null)
        {
            if (id == LogbookEntryCore.idFlightNew)
                return HttpNotFound();

            if (!CheckSignedRequest(ResourceUrlSigner.FlightSignaturePayload(id), exp, sig))
                return HttpNotFound();

            LogbookEntry le = new LogbookEntry { FlightID = id };
            le.LoadDigitalSig();

            byte[] rgSig = le.GetDigitizedSignature();

            return (rgSig != null && rgSig.Length > 0) ? (ActionResult)File(rgSig, "image/png") : HttpNotFound();
        }
    }
}
