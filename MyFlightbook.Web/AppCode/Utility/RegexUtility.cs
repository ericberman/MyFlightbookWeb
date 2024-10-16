using System;
using System.Collections.Generic;
using System.Globalization;
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
        #region General Purpose
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

        private static Regex mWhiteSpace = null;

        /// <summary>
        /// Whitespace (useful for splitting on whitespace)
        /// </summary>
        public static Regex WhiteSpace { get { return mWhiteSpace ?? (mWhiteSpace = new Regex("\\s", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mWords = null;

        /// <summary>
        /// Words - non-word characters (useful for splitting routes into airports)
        /// </summary>
        public static Regex Words { get { return mWords ?? (mWords = new Regex("\\W", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mHexColor = null;

        public static Regex HexColor { get { return mHexColor ?? (mHexColor = new Regex("^#[a-zA-Z0-9]{6}$", RegexOptions.IgnoreCase)); } }
        #endregion

        #region FlightQuery
        private static Regex mQuotedExpressions = null;
        private static Regex mMergeOR = null;
        private static Regex mTrailingDate = null;
        private static Regex mSpecificField = null;
        public const string szPrefixLimitComments = "CMT";
        public const string szPrefixLimitRoute = "RTE";

        /// <summary>
        /// Returns quoted phrases, with an optional negation.  Negation group is "negated", the phrase is "phrase"
        /// </summary>
        public static Regex QueryQuotedExpressions { get { return mQuotedExpressions ?? (mQuotedExpressions = new Regex("(?<negated>-?)\"(?<phrase>[^\"]*)\"", RegexOptions.Compiled)); } }
        public static Regex QueryMergeOR { get { return mMergeOR ?? (mMergeOR = new Regex("\\sOR\\s", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        /// <summary>
        /// Matches TrailingxxxD|CM|M|W.  "quantity" contains the digits, "rangetype" contains the unit
        /// </summary>
        public static Regex QueryTrailingDate { get { return mTrailingDate ?? (mTrailingDate = new Regex("\\bTrailing:(?<quantity>\\d{1,3}?)(?<rangetype>D|CM|M|W)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        /// <summary>
        /// Matches a query on a specific field (CMT or RTE). "field" contains the desired field, "value" its value.
        /// </summary>
        public static Regex QuerySpecificField { get { return mSpecificField ?? (mSpecificField = new Regex(String.Format(CultureInfo.InvariantCulture, "\\b(?<field>{0}|{1})=(?<value>\\S*)", szPrefixLimitComments, szPrefixLimitRoute), RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        #endregion

        #region Images
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

        private static Regex mMFBIIBackwardsCompatHack = null;
        public static Regex MFBIIBackwardsCompatHack { get { return mMFBIIBackwardsCompatHack ?? (mMFBIIBackwardsCompatHack = new Regex("(.*/)([^/]+)/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }
        #endregion

        #region Flights
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
        public static Regex LocalFlight { get { return mLocalFlight ?? (mLocalFlight = new Regex("^([0-9a-zA-Z]{3,5})([^0-9a-zA-Z]+\\1)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mApproach = null;
        /// <summary>
        /// Approach description - of the form 3-ILS-YRWY16L@KABC
        /// </summary>
        public static Regex ApproachDescription { get { return mApproach ?? (mApproach = new Regex("\\b(?<count>\\d{1,2})[-.:/ ]?(?<desc>[-a-zA-Z/]{3,}?(?:-[abcxyzABCXYZ])?)[-.:/ ]?(?:RWY)?(?<rwy>[0-3]?\\d[LRC]?)[-.:/ @](?<airport>[a-zA-Z0-9]{3,4})\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mApproachForeFlight = null;

        /// <summary>
        /// Approach description, in the form used by foreflight (e.g., 1;LOC;17;KUAO;Circle)
        /// </summary>
        public static Regex ApproachDescriptionForeflight { get { return mApproachForeFlight ?? (mApproachForeFlight = new Regex("\\b(?<count>\\d{1,2});(?<desc>[-a-zA-Z() /]{3,}?)(?: *RWY[^;]*?)?;(?<rwy>[0-3]?\\d[LRC]?);(?<airport>[a-zA-Z0-9]{3,4});(?<remark>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)); } }

        private static Regex mApproachCountForeFlight = null;

        /// <summary>
        /// Returns an approach count as used in Foreflight, which is just 1- or 2- digits
        /// </summary>
        public static Regex ApproachCountForeFlight { get { return mApproachCountForeFlight ?? (mApproachCountForeFlight = new Regex("\\b(?<count>\\d{1,2});", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)); } }


        private static Regex mFlightHash = null;
        /// <summary>
        /// Read a logbookentry (flight) from its flight hash
        /// </summary>
        public static Regex FlightHash { get { return mFlightHash ?? (mFlightHash = new Regex("^ID(?<ID>\\d+)DT(?<Date>[0-9-]+)AC(?<Aircraft>\\d+)A(?<Approaches>\\d+)H(?<Hold>[01])L(?<Landings>\\d+)NL(?<NightLandings>\\d+)XC(?<XC>[0-9.]+)N(?<Night>[0-9.]+)SI(?<SimInst>[0-9.]+)IM(?<IMC>[0-9.]+)GS(?<GroundSim>[0-9.]+)DU(?<Dual>[0-9.]+)CF(?<CFI>[0-9.]+)SI(?<SIC>[0-9.]+)PI(?<PIC>[0-9.]+)TT(?<Total>[0-9.]+)PR(?<props>.*)CC(?<CatClassOver>\\d+)CM(?<Comments>.*)$", RegexOptions.Compiled | RegexOptions.Singleline)); } }

        private static Regex mFlightHashProps = null;
        /// <summary>
        /// When reading a flight from its hash, the props also need to be decrypted
        /// </summary>
        public static Regex FlightHashProps { get { return mFlightHashProps ?? (mFlightHashProps = new Regex("(?<PropID>\\d+)V(?<Value>.+)", RegexOptions.Compiled)); } }

        private static Regex mPPH = null;
        /// <summary>
        /// #PPH:12.34# indicates a price-per-hour of 12.34 (units undefined)
        /// </summary>
        public static Regex PPH { get { return mPPH ?? (mPPH = new Regex("#PPH:(?<rate>\\d+(?:[.,]\\d+)?)#", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)); } }
        #endregion

        #region Misc
        static private Regex mIpadAndroid = null;
        /// <summary>
        /// Determines if 
        /// </summary>
        public static Regex IPadOrAndroid
        {
            get { return mIpadAndroid ?? (mIpadAndroid = new Regex("(IPAD|ANDROID)", RegexOptions.Compiled | RegexOptions.IgnoreCase)); }
        }

        private static Regex mHexRGB = null;
        /// <summary>
        /// Matches a 6-digit hex number (i.e., RGB)
        /// </summary>
        public static Regex HexRGB { get { return mHexRGB ?? (mHexRGB = new Regex("^[0-9a-fA-F]{6}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mEndorsementTemplateField = null;
        public static Regex EndorsementTemplateField { get { return mEndorsementTemplateField ?? (mEndorsementTemplateField = new Regex("\\{[^}]*\\}", RegexOptions.Compiled)); } }
        #endregion

        #region Aircraft, models, and manufacturers
        private static Regex mICAO = null;
        /// <summary>
        /// ICAO codes are 1-4 alphanumeric characters
        /// </summary>
        public static Regex ICAO { get { return mICAO ?? (mICAO = new Regex("^[a-zA-Z0-9]{0,4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }
        private static Regex mVORCheck = null;

        /// <summary>
        /// If someone puts "VORCHK" (whole word) into the comments for a flight, we find it and record that a VOR check was done in the maintenance for the aircraft
        /// </summary>
        public static Regex VORCheck { get { return mVORCheck ?? (mVORCheck = new Regex("\\bVORCHK[^a-zA-Z0-9]*(\\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mModelFragementBoundary = null;
        /// <summary>
        /// Used for splitting model names for model searching - splits at non-alpha but preserves colons (so that "ICAO:xxxx" works)
        /// </summary>
        public static Regex ModelFragmentBoundary { get { return mModelFragementBoundary ?? (mModelFragementBoundary = new Regex("[^a-zA-Z0-9:]", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mPseudoManufacturer = null;
        public static Regex FakeManufacturer { get { return mPseudoManufacturer ?? (mPseudoManufacturer = new Regex("GROUND|VARIOUS|UNKNOWN|MISC|MISCELLANEOUS|OTHER|NONE", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mAnonymous = null;

        public static Regex AnonymousTail { get { return mAnonymous ?? (mAnonymous = new Regex("^#\\d{6}$")); } }

        private static Regex rNormal = null;
        public static Regex NormalizedTailChars { get { return rNormal ?? (rNormal = new Regex("[^a-zA-Z0-9#]", RegexOptions.Compiled)); } }
        #endregion

        #region latitudes/longitude
        private static Regex mDMSBasic = null;
        /// <summary>
        /// Matches a degree-minute-second latitude/longitude string
        /// </summary>
        public static Regex DMSLatLong { get { return mDMSBasic ?? (mDMSBasic = new Regex("([^a-zA-Z]+[NS]) *([^a-zA-Z]+[EW])", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        private static Regex mCompassDirections = null;

        /// <summary>
        /// Matches potential compass directions (N, E, W, and S)
        /// </summary>
        public static Regex CompassDirections { get { return mCompassDirections ?? (mCompassDirections = new Regex("[NEWS]", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        private static Regex mDMSNumeric = null;
        private static Regex mDMSDecimal = null;
        private static Regex mDMSDotted = null;
        private static Regex mDMSDegrees = null;
        private static Regex mDMSDegreesSlashed = null;

        /// <summary>
        /// Matches a degree-minute-second latitude/longitude string using apostrophes in the format of "22 03' 26.123"S
        /// </summary>
        public static Regex DMSNumeric { get { return mDMSNumeric ?? (mDMSNumeric = new Regex("(\\d{1,3})\\D+([0-5]?\\d)\\D+(\\d+\\.?\\d*)\\D*([NEWS])", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        /// <summary>
        /// Matches a decimal degree-minute-second latitude/longitude string, e.g., "22.5483 S 27.863E"
        /// </summary>
        public static Regex DMSDecimal { get { return mDMSDecimal ?? (mDMSDecimal = new Regex("(\\d{0,3}([,.]\\d+)?)\\D*([NEWS])", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        /// <summary>
        /// Matches a decimal string preceded by compass direction, e.g., "W122.23.15"
        /// </summary>
        public static Regex DMSDotted { get { return mDMSDotted ?? (mDMSDotted = new Regex("([NEWSnews])[ .]?(\\d{0,3})[ .]?(\\d{0,2})[ .]?(\\d{0,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }

        /// <summary>
        /// Matches a degree-minute-second string that uses the degree sign, e.g., 48°01.3358"
        /// </summary>
        public static Regex DMSDegrees { get { return mDMSDegrees ?? (mDMSDegrees = new Regex("-?(\\d+)°(\\d+([.,]\\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }


        /// <summary>
        /// Matches a degree-minute-second string that uses the degree sign and NS or EW and a slash, e.g., 47.39°N/121.24°W
        /// </summary>
        public static Regex DMSDegreesSlashed { get { return mDMSDegreesSlashed ?? (mDMSDegreesSlashed = new Regex("(\\d{0,2}[.,]\\d*)\\D{0,2}°?([NS])/(\\d{0,3}[.,]\\d*)\\D{0,2}°?([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }
        #endregion

        #region Dates/Times
        private static Regex mRegexDay = null;
        private static Regex mRegexMonth = null;
        private static Regex mRegexYear = null;
        private static Regex mRegexDateSep = null;
        private static Regex mRegexYMD = null;
        private static Regex mRegexHHMM = null;

        private static readonly Dictionary<string, string> dictRegexForFormat = new Dictionary<string, string>();

        private static Regex regexDay
        {
            get
            {
                if (mRegexDay == null)
                    mRegexDay = new Regex("[dD]+", RegexOptions.Compiled);
                return mRegexDay;
            }
        }

        private static Regex regexMonth
        {
            get
            {
                if (mRegexMonth == null)
                    mRegexMonth = new Regex("[mM]+", RegexOptions.Compiled);
                return mRegexMonth;
            }
        }

        private static Regex regexYear
        {
            get
            {
                if (mRegexYear == null)
                    mRegexYear = new Regex("[yY]+", RegexOptions.Compiled);

                return mRegexYear;
            }
        }

        public static Regex regexHHMM
        {
            get
            {
                if (mRegexHHMM == null)
                    mRegexHHMM = new Regex("^(?<hour>\\d{2}):?(?<minute>\\d{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return mRegexHHMM;
            }
        }


        /// <summary>
        /// dots and slashes need to be escaped in a date, so find them to replace with escaped values.
        /// </summary>
        private static Regex regexDateSep
        {
            get
            {
                if (mRegexDateSep == null)
                    mRegexDateSep = new Regex("([./])", RegexOptions.Compiled);

                return mRegexDateSep;
            }
        }

        private const string regexPatternYMD = "\\d{2,4}-[01]?\\d-[0123]?\\d";

        /// <summary>
        /// Returns a regex pattern that can be put into an HTML Input field to validate a short date
        /// The result is cached (on the short date pattern) for fast retrieval
        /// ALWAYS allows for yyyy-mm-dd in addition to a localized date (m/d/y, d/m/y, etc.)
        /// </summary>
        public static string RegexPatternForShortDate
        {
            get
            {
                string szDateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                if (dictRegexForFormat.TryGetValue(szDateFormat, out string regex))
                    return regex;

                string locRegex = regexDateSep.Replace(regexYear.Replace(regexMonth.Replace(regexDay.Replace(szDateFormat, "[0-3]?[0-9]"), "[0-1]?[0-9]"), "\\d{2}(?:\\d{2})?"), "\\$1");
                string result = String.Format(CultureInfo.InvariantCulture, "({0}|{1})", regexPatternYMD, locRegex);
                dictRegexForFormat[szDateFormat] = result;
                return result;
            }
        }

        /// <summary>
        /// Returns the current short date pattern as a format specifier usable by things like jqueryui datepicker (i.e., see https://api.jqueryui.com/datepicker/#utility-formatDate
        /// Converts days to "d" (no leading 0), months to m (no leading 0) and years to "yy" (4 digit year).
        /// </summary>
        public static string ShortDatePatternToHtmlFormatString
        {
            get
            {
                return regexDay.Replace(regexMonth.Replace(regexYear.Replace(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, "yy"), "m"), "d");
            }
        }

        /// <summary>
        /// Returns a regex pattern that can be put into an HTML input field to validate a short date + hours and minutes time.
        /// The result is cached (on the short date pattern) for fast retrieval
        /// This allows for an OPTIONAL date component.
        /// This also ALWAYS allows for yyyy-mm-dd
        /// </summary>
        public static string RegexPatternForDateTimeOptionalDate
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "^({0} )?[0-2]?\\d:[0-5]\\d$", RegexPatternForShortDate);
            }
        }

        /// <summary>
        /// Returns a regex pattern that can be put into an HTML input field to validate a short date + hours and minutes time.
        /// The result is cached (on the short date pattern) for fast retrieval
        /// This allows for an OPTIONAL date component.
        /// This also ALWAYS allows for yyyy-mm-dd
        /// </summary>
        public static string RegexPatternForDateTimeRequiredDate
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "^{0} [0-2]?\\d:[0-5]\\d$", RegexPatternForShortDate);
            }
        }

        /// <summary>
        /// Returns a regex that detects a YMD pattern at the start of the string.
        /// </summary>
        public static Regex RegexYMDDate
        {
            get
            {
                if (mRegexYMD == null)
                    mRegexYMD = new Regex("^(?:19|20)\\d{2}[. _-]?[01]?\\d[. _-]?[012]?\\d", RegexOptions.Compiled);
                return mRegexYMD;
            }
        }
        #endregion

        #region Import
        private static Regex mNakedTime = null;
        /// <summary>
        /// Matches a naked time - e.g., 11:32, 23:27, or :51.
        /// </summary>
        public static Regex NakedTime { get { return mNakedTime ?? (mNakedTime = new Regex("^\\s*([012]?\\d)?:\\d{2}\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)); } }
        #endregion

        #region Admin Regexes
        static private Regex mPseudoSim = null;
        public static Regex ADMINPseudoSim { get { return mPseudoSim ?? (mPseudoSim = new Regex("N[a-zA-Z-]+([0-9].*)", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        static private Regex mZeroOneOI = null;

        public static Regex ADMINZeroOrIConfusion { get { return mZeroOneOI ?? (mZeroOneOI = new Regex("^N.*[oOiI].*", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        static private Regex mPseudoCertifiedSim = null;

        public static Regex ADMINPseudoCertifiedSim { get { return mPseudoCertifiedSim ?? (mPseudoCertifiedSim = new Regex("FS|SIM|FTD|REDB|FRAS|ELIT|CAE|ALSIM|FLIG|SAFE|PREC|TRUF|FMX|MENT|FAA", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        static private Regex mPseudoFFS = null;
        public static Regex AdminPseudoFFS { get { return mPseudoFFS ?? (mPseudoFFS = new Regex("(D-?SIM)|FFS", RegexOptions.Compiled | RegexOptions.IgnoreCase)); } }

        private static Regex mAdminSignatureSanity = null;
        /// <summary>
        /// Internal regex for an old signature bug
        /// </summary>
        public static Regex AdminSignatureSanity { get { return mAdminSignatureSanity ?? (mAdminSignatureSanity = new Regex("^(.*)(XC[0-9., ]+N[0-9., ]+SI[0-9., ]+IM[0-9., ]+GS[0-9., ]+DU[0-9., ]+CF[0-9., ]+SI[0-9., ]+PI[0-9., ]+TT[0-9., ]+)(.*)$", RegexOptions.Compiled)); } }
        #endregion
    }

    /// <summary>
    /// Provides a lazy-loaded compiled regex for regexes that are less general use thatn RegexUtility
    /// Can be used like a regex in that it provides IsMatch, Match, Matches, and Replace functionality
    /// ALWAYS compiled.
    /// </summary>
    public class LazyRegex
    {
        #region private vars
        private Regex re = null;
        private readonly string expr = null;
        private readonly RegexOptions flags = RegexOptions.Compiled;
        #endregion

        #region Constructors
        public LazyRegex(string Expression, RegexOptions Options)
        {
            expr = Expression;
            flags = flags | Options;
        }

        public LazyRegex(string Expression, bool ignoreCase = false, bool multiLine = false) : 
            this(Expression, 
                (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | (multiLine ? RegexOptions.Multiline : RegexOptions.None))
        {
        }
        #endregion

        /// <summary>
        /// Returns the described regular expression.  Only ever creates a new Regex once
        /// </summary>
        public Regex Expr { get { return re ?? (re = new Regex(expr, flags)); } }

        #region Regex shortcuts
        /// <summary>
        /// Shortcut for testing if the string is a match for the regex
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public bool IsMatch(string s)
        {
            return Expr.IsMatch(s);
        }

        /// <summary>
        /// Returns the match collection for a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public MatchCollection Matches(string s)
        {
            return Expr.Matches(s);
        }

        public Match Match(string s)
        {
            return Expr.Match(s);
        }

        public string Replace(string s, string replacement)
        {
            return Expr.Replace(s, replacement);
        }
        #endregion

        public override string ToString()
        {
            return expr;
        }
    }
}
