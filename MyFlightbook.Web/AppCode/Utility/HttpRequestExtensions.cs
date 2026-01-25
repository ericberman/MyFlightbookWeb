using MyFlightbook.Image;
using System;
using System.IO;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Helper methods for working with HttpRequest objects
    /// </summary>
    public static class HttpRequestExtensions
    {
        #region HttpRequest Extensions
        /// <summary>
        /// Returns the string as a fully resolved absolute url, including scheme and host
        /// </summary>
        /// <param name="s">The relative URL</param>
        /// <param name="Request">The request</param>
        /// <returns>A fully resolved absolute URL</returns>
        public static Uri ToAbsoluteURL(this string s, HttpRequestBase Request)
        {
            if (Request == null)
                throw new ArgumentNullException(nameof(Request));
            return s.ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host, Request.Url.Port);
        }

        /// <summary>
        /// Determines if this is a known mobile device.  Tablets are NOT considered mobile; use IsMobileDeviceOrTablet
        /// </summary>
        /// <param name="r">The HTTPRequest object</param>
        /// <returns>True if it's known</returns>
        public static bool IsMobileDevice(this HttpRequestBase r)
        {
            if (r == null || r.UserAgent == null)
                return false;

            string s = r.UserAgent.ToUpperInvariant();

            return !s.Contains("IPAD") && // iPad is NOT mobile, as far as I'm concerned.
            (r.Browser.IsMobileDevice || s.Contains("IPHONE") ||
              s.Contains("PPC") ||
              s.Contains("WINDOWS CE") ||
              s.Contains("BLACKBERRY") ||
              s.Contains("OPERA MINI") ||
              s.Contains("MOBILE") ||
              s.Contains("PARLM") ||
              s.Contains("PORTABLE"));
        }

        /// <summary>
        /// IsMobileDevice OR iPad OR Android
        /// </summary>
        /// <param name="r">The HTTPRequest object</param>
        /// <returns>True if it's a mobile device or a tablet</returns>
        public static bool IsMobileDeviceOrTablet(this HttpRequestBase r)
        {
            if (r == null || String.IsNullOrEmpty(r.UserAgent))
                return false;

            return IsMobileDevice(r) || RegexUtility.IPadOrAndroid.IsMatch(r.UserAgent);
        }

        public static IPostedImageFile ImageFile(this HttpRequestBase req, int i)
        {
            return new PostedImageFile(req?.Files[i]);
        }

        public static IPostedImageFile ImageFile(this HttpRequestBase req, string key)
        {
            return new PostedImageFile(req?.Files[key]);
        }
        #endregion

        #region Passing posted files to image handling
        public class PostedImageFile : IPostedImageFile
        {
            private readonly HttpPostedFileBase _pf;

            public PostedImageFile(HttpPostedFileBase pf)
            {
                _pf = pf ?? throw new ArgumentNullException(nameof(pf));
            }

            public string FileName => _pf.FileName;
            public string ContentType => _pf.ContentType;
            public int ContentLength => _pf.ContentLength;
            public Stream InputStream => _pf.InputStream;
        }
        #endregion
    }

}