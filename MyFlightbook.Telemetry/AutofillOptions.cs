using MyFlightbook.Geography.SolarTools;
using Newtonsoft.Json;
using System;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    /// <summary>
    /// Container for options that can affect how autofill operates
    /// </summary>
    public class AutoFillOptions
    {
        public enum AutoFillTotalOption { None, FlightTime, EngineTime, HobbsTime, BlockTime };
        public enum AutoFillHobbsOption { None, FlightTime, EngineTime, TotalTime };
        public enum NightCritera { EndOfCivilTwilight, Sunset, SunsetPlus15, SunsetPlus30, SunsetPlus60 };
        public enum NightLandingCriteria { SunsetPlus60, Night };

        public enum TimeConversionCriteria { None, Local, Preferred }

        public const int DefaultCrossCountryDistance = 50;
        public const int DefaultTakeoffSpeedKts = 70;
        public const int DefaultLandingSpeedKts = 55;
        public const double FullStopSpeed = 5.0;

        private readonly static int[] _rgSpeeds = { 20, 40, 55, 70, 85, 100 };
        private const int DefaultSpeedIndex = 3;
        private const int SpeedBreakPoint = 50;
        private const int LandingSpeedDifferentialLow = 10;
        private const int LandingSpeedDifferentialHigh = 15;

        #region Constructors
        public AutoFillOptions()
        {
        }

        /// <summary>
        /// returns a new autofill options from another one.
        /// </summary>
        /// <param name="afoSrc"></param>
        public AutoFillOptions(AutoFillOptions afoSrc) : this()
        {
            if (afoSrc != null)
                util.CopyObject(afoSrc, this);
        }

        private const string szKeyPersistedPrefAutoFill = "DefaultAutofillOptions";

        public static AutoFillOptions DefaultOptionsForUser(IUserPreference userPreference)
        {
            return userPreference?.GetPreferenceForKey<AutoFillOptions>(szKeyPersistedPrefAutoFill) ?? new AutoFillOptions();
        }

        public void SaveForUser(IUserPreference userPreference)
        {
            userPreference?.SetPreferenceForKey(szKeyPersistedPrefAutoFill, this);
        }
        #endregion

        #region Properties
        /// <summary>
        /// timezone offset from UTC, in minutes
        /// </summary>
        public int TimeZoneOffset { get; set; } = 0;

        /// <summary>
        /// AutoTotal options
        /// </summary>
        public AutoFillTotalOption AutoFillTotal { get; set; } = AutoFillTotalOption.HobbsTime;

        /// <summary>
        /// AutoHobbs options
        /// </summary>
        public AutoFillHobbsOption AutoFillHobbs { get; set; } = AutoFillHobbsOption.EngineTime;

        /// <summary>
        /// By what criteria do we begin accumulating night flight?
        /// </summary>
        public NightCritera Night { get; set; } = NightCritera.EndOfCivilTwilight;

        /// <summary>
        /// By what criteria do we log a night landing?
        /// </summary>
        public NightLandingCriteria NightLanding { get; set; } = NightLandingCriteria.SunsetPlus60;

        /// <summary>
        /// Return the time offset (in minutes) relative to sunset to use for IsWithinNightOffset
        /// </summary>
        public int NightFlightSunsetOffset
        {
            get
            {
                switch (Night)
                {
                    case NightCritera.EndOfCivilTwilight:
                    case NightCritera.Sunset:
                    default:
                        return 0;
                    case NightCritera.SunsetPlus60:
                        return 60;
                    case NightCritera.SunsetPlus15:
                        return 15;
                    case NightCritera.SunsetPlus30:
                        return 30;
                }
            }
        }

        /// <summary>
        /// Threshold for cross-country flight
        /// </summary>
        public double CrossCountryThreshold { get; set; } = DefaultCrossCountryDistance;

        /// <summary>
        /// Speed above which the aircraft is assumed to be flying
        /// </summary>
        public double TakeOffSpeed { get; set; } = DefaultTakeoffSpeedKts;

        /// <summary>
        /// Speed below which the aircraft is assumed to be taxiing or stopped
        /// </summary>
        public double LandingSpeed { get; set; } = DefaultLandingSpeedKts;

        /// <summary>
        /// Include heliports in autodetection?
        /// </summary>
        public bool IncludeHeliports { get; set; } = false;

        /// <summary>
        /// True to plow ahead and continue even if errors are encountered.
        /// </summary>
        public bool IgnoreErrors { get; set; }

        /// <summary>
        /// Indicates if a path should be synthesized if not present (needed to estimate night time, for example)
        /// True by default.
        /// </summary>
        public bool AutoSynthesizePath { get; set; } = true;

        /// <summary>
        /// Indicates whether auto-fill times should be rounded to the nearest 10th of an hour
        /// </summary>
        public bool RoundToTenth { get; set; } = false;

        /// <summary>
        /// Indicates whether to try to convert times to UTC time based on lat/long
        /// </summary>
        [Obsolete("Use TimeConversion Instead")]
        public bool TryLocal { get; set; } = false;

        /// <summary>
        /// How should times be interpreted?  UTC?  Local time?  Or in the user's preferred time zone (using PreferredTimeZone).
        /// </summary>
        [JsonIgnore]
        public TimeConversionCriteria TimeConversion { get; set; } = TimeConversionCriteria.None;

        [JsonIgnore]
        public TimeZoneInfo PreferredTimeZone { get; set; }
        #endregion

        #region testing for night, night landings
        /// <summary>
        /// Determines if the specified date counts as night for logging night flight
        /// </summary>
        /// <param name="sst">The sunrisesunsettimes that contains the date and relevant location</param>
        /// <returns>True if the time sample should be considered night per user options</returns>
        public bool IsNightForFlight(SunriseSunsetTimes sst)
        {
            if (sst == null)
                throw new ArgumentNullException(nameof(sst));

            // short circuit
            if (!sst.IsNight)
                return false;

            switch (Night)
            {
                case AutoFillOptions.NightCritera.EndOfCivilTwilight:
                    return sst.IsFAACivilNight;
                case AutoFillOptions.NightCritera.Sunset:
                    return true;    // Actually sst.IsNight, but we've determined from the above short circuit that it must be true.
                case AutoFillOptions.NightCritera.SunsetPlus15:
                case AutoFillOptions.NightCritera.SunsetPlus30:
                case AutoFillOptions.NightCritera.SunsetPlus60:
                    return sst.IsWithinNightOffset;
            }

            return false;   // should never hit this.
        }
        #endregion

        /// <summary>
        /// Get the default take-off speed
        /// </summary>
        public static int DefaultTakeoffSpeed
        {
            get { return _rgSpeeds[DefaultSpeedIndex]; }
        }

        /// <summary>
        /// Array of default speed values.
        /// </summary>
        public static System.Collections.ObjectModel.ReadOnlyCollection<int> DefaultSpeeds
        {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<int>(_rgSpeeds); }
        }

        /// <summary>
        /// What is the best landing speed for the given take-off speed?
        /// </summary>
        /// <param name="TOSpeed"></param>
        /// <returns></returns>
        public static int BestLandingSpeedForTakeoffSpeed(int TOSpeed)
        {
            if (TOSpeed >= SpeedBreakPoint)
                return TOSpeed - LandingSpeedDifferentialHigh;
            else
                return Math.Max(TOSpeed - LandingSpeedDifferentialLow, _rgSpeeds[0] - LandingSpeedDifferentialLow);
        }
    }
}
