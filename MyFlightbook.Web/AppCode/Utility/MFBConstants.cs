using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class MFBConstants
    {
        public const string keyIsImpersonating = "IsImpersonating";
        public const string keyOriginalID = "OriginalID";
        public const string keyNewUser = "IsNewUser";
        public const string keyCookiePrivacyOld = "cookiesAccepted";
        public const string keyCookiePrivacy = "cookiesAcceptedNEW";
        public const string keyTFASettings = "prefTFASettings"; // any 2-factor authentication settings.
        public static string keyDecimalSettings { get { return MFBExtensions.keyDecimalSettings; } }  // adaptive, single, or double digit precision    Note: moved to MFBExtensions for shared access.
        public static string keyMathRoundingUnits { get {return MFBExtensions.keyMathRoundingUnits;} }   // whether to use decimal math (36-second precision) or minute math (60-second precision) when adding
        public const string keyPrefLastUsedLocale = "prefLastUsedLocale";   // most recently used locale, if not en-us
        public const string keyPrefHobbsDefault = "prefUseHobbs";
        public const string keyPrefTachDefault = "prefUseTach";
        public const string keyPrefBlockDefault = "prefUseBlock";
        public const string keyPrefEngineDefault = "prefUseEngine";
        public const string keyPrefFlightsDefault = "prefUseFlight";
        public const string USCulture = "en-us";
        public const string keyMedicalNotes = "prefMedicalNotes";   // any notes on your medical
        public const string keyCoreFieldsPermutation = "prefCoreFields";    // permutation of the core fields
        public const string keyWindowAircraftMaintenance = "prefMaintenanceWindow"; // default window for showing/hiding aircraft maintenance
        public const string keyTrackOriginal = "prefTrackOriginal";  // true if the user tracks the original version of a flight.
        public const string keyRouteColor = "prefRouteColor";   // key for the color when showing routes on a map
        public const string keyPathColor = "prefPathColor";     // key for the color when showing a path on a map
        public const string keyIsNightSession = "IsNightSession";   // key if we have a night session.
        public const int DefaultMaintenanceWindow = 90;

        // Logbook preferences
        public const string keyPrefFlatHierarchy = "UsesFlatCloudStorageFileHierarchy";    // indicates that cloud storage should be done in a flat hierarchy rather than by month.
        public const string keyPrefCompact = "mfbLogbookDisplayCompact";
        public const string keyPrefInlineImages = "mfbLogbookDisplayImages";
        public const string keyPrefFlightsPerPage = "mfbLogbookDisplayFlightsPerPage";
        public const int DefaultFlightsPerPage = 25;

        // Signing preferences
        public const string keyPrefCopyFlightToCFI = "copySignedFlights";

        public const string keySessLastNewFlight = "sessNewFlightID";

        public const int StyleSheetVer = 73;

        public static string BaseStylesheet
        {
            get { return String.Format(CultureInfo.InvariantCulture, "~/Public/stylesheet.css?v={0}", StyleSheetVer); }
        }

        public static string BaseCssVars(bool isNight)
        {
            return String.Format(CultureInfo.InvariantCulture, isNight ? "~/public/css/night.css?v=fdn{0}" : "~/public/css/day.css?v=fdn{0}", StyleSheetVer).ToAbsolute();
        }

        public static string AdminAjaxScriptLink
        {
            get { return "~/Public/Scripts/adminajax.js?v=6".ToAbsolute(); }
        }
    }
}