using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2013-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
{
    /// <summary>
    /// PArt 135 Milestones
    /// </summary>
    [Serializable]
    public class Part135Milestones : MilestoneGroup
    {
        public Part135Milestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroup135;
            Milestones = new MilestoneProgress[]
            {
                new Part135243b(),
                new Part135243c()
            };
        }

        /// <summary>
        /// Determines the amount of part 135 cross-country time in this flight.
        /// Part 135 time is cross country if it goes from airport A to airport B
        /// We mimic the FlightQuery definition of non-local here, which looks at the length of the route field and verifies that it matches neither
        /// ABCD-ABCD or ABC-ABC.  (If it's simply ABC or simply ABCD, then it fails the length test)
        /// If it is non-local, then we use MAX(total time, cross-country).
        /// </summary>
        /// <param name="cfr">The flight</param>
        /// <returns>The amount of cross-country time to contribute</returns>
        public static decimal Part135CrossCountry(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                return 0.0M;
            if (cfr.Route == null || cfr.Route.Length <= airport.maxCodeLength)
                return cfr.XC;

            // Mimic
            string szNormalRoute = Regex.Replace(cfr.Route, "[^a-zA-Z0-9]", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex r4x4 = new Regex(String.Format(CultureInfo.InvariantCulture, "^{0}[^a-zA-Z0-9]*{0}$", szNormalRoute.Substring(0, 4)), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex r3x3 = new Regex(String.Format(CultureInfo.InvariantCulture, "^{0}[^a-zA-Z0-9]*{0}$", szNormalRoute.Substring(0, 3)), RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return (r4x4.IsMatch(cfr.Route) || r3x3.IsMatch(cfr.Route)) ? cfr.XC : Math.Max(cfr.XC, cfr.Total);
        }
    }

    #region Part 135 Ratings
    /// <summary>
    /// Part 135.243b - PIC (VFR)
    /// </summary>
    [Serializable]
    public class Part135243b : MilestoneProgress
    {
        protected MilestoneItem miMinTimeAsPilot { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinXCNightTime { get; set; }

        protected const decimal minTime = 500.0M;
        protected const decimal minXCTime = 100.0M;
        protected const decimal minXCNightTime = 25.0M;

        public Part135243b() : base()
        {
            RatingSought = RatingType.Part135PIC;
            BaseFAR = "135.243(b)(2)";
            string szFAR = ResolvedFAR(string.Empty);
            Title = Resources.MilestoneProgress.Title135243PIC;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.Part135PICDisclaimer);
            miMinTimeAsPilot = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICMinTime, minTime), szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minTime);
            miMinXCTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICXCMinTime, minXCTime), szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCTime);
            miMinXCNightTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICNightXCMinTime, szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCNightTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!cfr.fIsRealAircraft)
                return;

            decimal part135XC = Part135Milestones.Part135CrossCountry(cfr);

            miMinTimeAsPilot.AddEvent(cfr.Total);
            miMinXCTime.AddEvent(part135XC);
            miMinXCNightTime.AddEvent(Math.Min(part135XC, cfr.Night));
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miMinTimeAsPilot, miMinXCTime, miMinXCNightTime });
            }
        }
    }

    /// <summary>
    /// Part 135.243(c) - PIC (IFR)
    /// </summary>
    [Serializable]
    public class Part135243c : MilestoneProgress
    {
        protected MilestoneItem miMinTimeAsPilot { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinIFRTime { get; set; }
        protected MilestoneItem miMinIFRAircraftTime { get; set; }

        protected const decimal minTime = 1200.0M;
        protected const decimal minXCTime = 500.0M;
        protected const decimal minNightTime = 100.0M;
        protected const decimal minIFRTime = 75.0M;
        protected const decimal minIFRAircraftTime = 50.0M;

        public Part135243c() : base()
        {
            RatingSought = RatingType.Part135PICIFR;
            BaseFAR = "135.243(c)(2)";
            string szFAR = ResolvedFAR(string.Empty);
            Title = Resources.MilestoneProgress.Title135243PICIFR;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.Part135PICDisclaimer);
            miMinTimeAsPilot = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICMinTime, minTime), szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minTime);
            miMinXCTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICXCMinTime, minXCTime), szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCTime);
            miMinNightTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRNightTime, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minNightTime);
            miMinIFRTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRTime, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minIFRTime);
            miMinIFRAircraftTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRTimeInFlight, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minIFRAircraftTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            decimal IMCTime = cfr.IMC + cfr.IMCSim;
            if (cfr.fIsCertifiedIFR)
                miMinIFRTime.AddEvent(IMCTime);

            if (!cfr.fIsRealAircraft)
                return;

            decimal part135XC = Part135Milestones.Part135CrossCountry(cfr);

            miMinTimeAsPilot.AddEvent(cfr.Total);
            miMinXCTime.AddEvent(part135XC);
            miMinNightTime.AddEvent(cfr.Night);
            miMinIFRAircraftTime.AddEvent(IMCTime);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miMinTimeAsPilot, miMinXCTime, miMinNightTime, miMinIFRTime, miMinIFRAircraftTime });
            }
        }
    }
    #endregion
}