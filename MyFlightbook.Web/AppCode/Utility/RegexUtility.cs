using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Container for commonly used (and re-used!) regexes.
    /// These are (a) static for performance, and (b) lazy-loaded for startup performance
    /// We should add to this over time...
    /// </summary>
    public static class RegexUtility
    {
        private static Regex mEmail = null;
        /// <summary>
        /// Regex for an email address
        /// </summary>
        public static Regex Email { get { return mEmail ?? (mEmail = new Regex("^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mUnsafeChars = null;
        /// <summary>
        /// Technically, there are LOTS more safe characters, but this keeps things simple by letting you eliminate anything that is not alphanumeric or a hyphen.
        /// </summary>
        public static Regex UnSafeFileChars { get { return mUnsafeChars ?? (mUnsafeChars = new Regex("[^0-9a-zA-Z-]", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mAlphaNumeric = null;
        /// <summary>
        /// Strict ASCII alpha numeric match
        /// </summary>
        public static Regex AlphaNumeric { get { return mAlphaNumeric ?? (mAlphaNumeric = new Regex("[a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mNonAlphanumeric = null;
        /// <summary>
        /// Strict non-ASCII alpha numeric match
        /// </summary>
        public static Regex NonAlphaNumeric { get { return mNonAlphanumeric ?? (mNonAlphanumeric = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mICAO = null;
        /// <summary>
        /// ICAO codes are 1-4 alphanumeric characters
        /// </summary>
        public static Regex ICAO { get { return mICAO ?? (mICAO = new Regex("^[a-zA-Z0-9]{0,4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        // Adapted from http://linuxpanda.wordpress.com/2013/07/24/ultimate-best-regex-pattern-to-get-grab-parse-youtube-video-id-from-any-youtube-link-url/
        // Note: these two youtube URLs don't work:
        // "http://www.youtube.com/watch?v=yVpbFMhOAwE&feature=player_embedded", ** doesn't work
        // "http://www.youtube.com/watch?v=6zUVS4kJtrA&feature=c4-overview-vl&list=PLbzoR-pLrL6qucl8-lOnzvhFc2UM1tcZA" ** doesn't work,
        private const string szRegExpMatchYouTube = "^(?:http|https)?(?:://)?(?:www\\.)?(?:youtu\\.be/|youtube\\.com(?:/embed/|/v/|/watch?v=|/ytscreeningroom?v=|/feeds/api/videos/|/user\\S*[^\\w\\-\\s]|\\S*[^\\w\\-\\s]))([\\w\\-]{11})[a-z0-9;:@?&%=+/\\$_.-]*";
        private static Regex mYouTube = null;
        /// <summary>
        /// Identify a link to a video on Youtube
        /// </summary>
        public static Regex YouTubeReference { get { return mYouTube ?? (mYouTube = new Regex(szRegExpMatchYouTube, RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        // Adapted from http://stackoverflow.com/questions/10488943/easy-way-to-get-vimeo-id-from-a-vimeo-url
        private const string szRegExpMatchVimeo = "^(?:http|https)(?:://)?(?:www\\.|player\\.)?vimeo.com/(.*)";
        private static Regex mVimeo = null;
        /// <summary>
        /// Identify a link to a video on Vimeo
        /// </summary>
        public static Regex VimeoReference { get { return mVimeo ?? (mVimeo = new Regex(szRegExpMatchVimeo, RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        private static Regex mLocalFlight = null;
        /// <summary>
        /// Determines if this flight looks like a local flight.  A-B is not local, but A is local, as is A-A.  ("-" can be any non-alpha)
        /// Matches:
        ///  - ABC
        ///  - ABCD
        ///  - ABCDE
        ///  - ABC-ABC
        ///  - ABCD-ABCD
        ///  - ABCDE-ABCDE
        /// </summary>
        public static Regex LocalFlight { get { return mLocalFlight ?? (mLocalFlight = new Regex("^([0-9a-zA-Z]{3,5})([^0-9a-zA-Z]+\\1)?$")); } }

        private static Regex mApproach = null;
        /// <summary>
        /// Approach description - of the form 3-ILS-YRWY16L@KABC
        /// </summary>
        public static Regex ApproachDescription { get { return mApproach ?? (mApproach = new Regex("\\b(?<count>\\d{1,2})[-.:/ ]?(?<desc>[-a-zA-Z/]{3,}?(?:-[abcxyzABCXYZ])?)[-.:/ ]?(?:RWY)?(?<rwy>[0-3]?\\d[LRC]?)[-.:/ @](?<airport>[a-zA-Z0-9]{3,4})\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }
    }
}
