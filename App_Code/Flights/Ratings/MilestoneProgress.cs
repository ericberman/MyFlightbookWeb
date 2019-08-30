using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
{
    [Serializable]
    public class MilestoneItem
    {
        public enum MilestoneType { AchieveOnce, Count, Time };

        #region properties
        /// <summary>
        /// The title for this item (e.g., "Hours of instruction")
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The relevant FAR reference
        /// </summary>
        public string FARRef { get; set; }

        /// <summary>
        /// A notice/warning/caveat (e.g., "Unknown if these airports have towers")
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// The progress made so far
        /// </summary>
        public decimal Progress { get; set; }

        /// <summary>
        /// Progress achieved in a display format
        /// </summary>
        public string ProgressDisplay
        {
            get
            {
                switch (Type)
                {
                    case MilestoneType.AchieveOnce:
                    default:
                        return Progress.ToString(CultureInfo.CurrentCulture);
                    case MilestoneType.Count:
                        return String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ProgressTemplate, Progress.ToString("#,##0", CultureInfo.CurrentCulture), Percentage);
                    case MilestoneType.Time:
                        return String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ProgressTemplate, Progress.ToString("#,##0.0", CultureInfo.CurrentCulture), Percentage);
                }
            }
        }

        /// <summary>
        /// The amount of progress necessary to meet the requirement
        /// </summary>
        public decimal Threshold { get; set; }

        /// <summary>
        /// Percentage achieved
        /// </summary>
        public decimal Percentage
        {
            get { return Threshold == 0.0M ? 0.0M : (Progress / Threshold) * 100.0M; }
        }

        /// <summary>
        /// Percentage complete in a display format
        /// </summary>
        public string DisplayPercent
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0:#,##0}%", Percentage); }
        }

        /// <summary>
        /// The type of milestone this represents
        /// </summary>
        public MilestoneType Type { get; set; }

        /// <summary>
        /// For "AchieveOnce" milestones, contains a description of the match
        /// </summary>
        public string MatchingEventText { get; set; }

        /// <summary>
        /// For "AchieveOnce" milestones, links to the matching flight.
        /// </summary>
        public int MatchingEventID { get; set; }

        /// <summary>
        /// Display title
        /// </summary>
        public string DisplayTitle
        {
            get { return String.IsNullOrEmpty(FARRef) ? Title : String.Format(CultureInfo.CurrentCulture, "{0} - {1} {2}", FARRef, Title, MatchingEventText).Trim(); }
        }

        /// <summary>
        /// Has this been satisfied?
        /// </summary>
        public bool IsSatisfied
        {
            get { return (this.Type == MilestoneType.AchieveOnce) ? Progress > 0 : Progress >= Threshold; }
        }

        /// <summary>
        /// If time in a training device may be substituted up to a point, this defines how much has been contributed so far.
        /// </summary>
        public decimal TrainingDeviceContribution { get; set; }
        #endregion

        #region Object creation/initialization
        private void InitObject()
        {
            Title = Note = FARRef = String.Empty;
            Progress = Threshold = TrainingDeviceContribution = 0.0M;
            Type = MilestoneType.AchieveOnce;
        }

        public MilestoneItem()
        {
            InitObject();
        }

        /// <summary>
        /// New Milestone item with initializing values
        /// </summary>
        /// <param name="title">The title for the item</param>
        /// <param name="farref">The relevant FAR</param>
        /// <param name="note">Any qualifying notes</param>
        /// <param name="type">Type of milestoneitem</param>
        /// <param name="threshold">Threshold for the item</param>
        public MilestoneItem(string title, string farref, string note, MilestoneType type, decimal threshold)
        {
            InitObject();
            Type = type;
            Title = title;
            Note = note;
            Threshold = threshold;
            FARRef = farref;
        }
        #endregion

        /// <summary>
        /// Adds progress towards a rating.
        /// </summary>
        /// <param name="x"></param>
        public void AddEvent(decimal x)
        {
            Progress += x;
        }

        /// <summary>
        /// Adds progress towards a rating in a training device up to a specified limit
        /// </summary>
        /// <param name="x">The progress to add</param>
        /// <param name="limit">The "do not exceed" amount that is allowed</param>
        /// <param name="fIsTrainingDevice">True if this is in a training device rather than a real aircraft</param>
        public void AddTrainingEvent(decimal x, decimal limit, bool fIsTrainingDevice)
        {
            if (fIsTrainingDevice && TrainingDeviceContribution + x > limit)
                x = Math.Max(limit - TrainingDeviceContribution, 0.0M);
            AddEvent(x);
            if (fIsTrainingDevice)
                TrainingDeviceContribution += x;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "({0}) {1} ({2} {3} {4}", FARRef, Title, Progress, Threshold, Type.ToString());
        }

        public void MatchFlightEvent(ExaminerFlightRow cfr)
        {
            if (Type == MilestoneType.AchieveOnce && !IsSatisfied)
            {
                if (cfr == null)
                    throw new ArgumentNullException("cfr");
                AddEvent(1);
                MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                MatchingEventID = cfr.flightID;
            }
        }
    }

    #region Groups of milestones
    /// <summary>
    /// A class which returns a grouping of concrete MilestoneProgress classes which logically belong together.  Doing this reduces class coupling.
    /// </summary>
    [Serializable]
    public abstract class MilestoneGroup
    {
        public MilestoneProgress[] Milestones { get; set; }
        public string GroupName { get; set; }
    }
    #endregion

    public enum RatingType
    {
        // PPL Ratings - 61.109
        PPLAirplaneSingle,
        PPLAirplaneMulti,
        PPLHelicopter,
        PPLGyroplane,
        PPLPoweredLift,
        PPLGlider,
        PPLAirship,
        PPLBalloon,
        PPLPoweredParachute,
        PPLWeightShift,
        PPLPart141SingleEngine,
        PPLPart141MultiEngine,
        PPLPart141Helicopter,
        PPLPart141Gyroplane,
        PPLPart141Glider,
        PPLEASAAirplane,
        PPLEASAHelicopter,
        PPLEASANightAirplane,
        PPLEASANightHelicopter,
        CAPPLAirplaneLand,
        CAPPLAirplaneSea,
        CANightAirplane,
        CAPPLHelicopter,
        CANightHelicopter,
        CASRAirplaneWithCourse,
        CASRAirplaneWithoutCourse,
        CASRHelicopterWithCourse,
        CASRHelicopterWithoutCourse,
        SAPPLAirplane,
        SAPPLHelicopter,
        SAPPLNightAirplane,
        SAPPLNightHelicopter,

        // EASA LAPL
        EASALAPLAirplane,
        EASALAPLHelicopter,
        EASALAPLSailplane,

        // Instrument Ratings - 61.65
        InstrumentAirplane,
        InstrumentHelicopter,
        InstrumentPoweredLift,

        // Recreational Pilot - 61.99
        RecreationalPilot,

        // Sport pilot - 61.313
        SportSingleEngine,
        SportGlider,
        SportRotorcraft,

        // Commercial - 61.129
        CommercialASEL,
        CommercialASES,
        CommercialAMEL,
        CommercialAMES,
        CommercialHelicopter,
        CommercialGyroplane,
        CommercialBalloonHot,
        CommercialBalloonGas,
        Commercial141AirplaneSingleEngineLand,
        Commercial141AirplaneSingleEngineSea,
        Commercial141AirplaneMultiEngineLand,
        Commercial141AirplaneMultiEngineSea,
        Commercial141Helicopter,
        CommercialGlider,
        CASRCommAirplaneWithCourse,
        CASRCommAirplaneWithoutCourse,
        CASRCommHelicopterWithCourse,
        CASRCommHelicopterWithoutCourse,

        // ATP - 61.159, 61.161, 61.163
        ATPAirplane,
        ATPAirplaneRestricted,
        ATPHelicopter,
        ATPPoweredLift,

        // Part 135
        Part135PIC,
        Part135PICIFR,

        // DPE Milestones
        DPEASELPPL,
        DPEAMELPPL,
        DPEASESPPL,
        DPEAMESPPL,
        DPEGyroplanePPL,
        DPEHelicopterPPL,
        DPEGliderPPL,
        DPEASELCommercial,
        DPEAMELCommercial,
        DPEASESCommercial,
        DPEAMESCommercial,
        DPEHelicopterCommercial,
        DPEGliderCommercial,

        // Placeholder for unknown
        Unknown
    };

    /// <summary>
    /// Base class for milestone progress.  Uses the currency model.
    /// </summary>
    [Serializable]
    public abstract class MilestoneProgress : IFlightExaminer
    {
        #region properties
        /// <summary>
        /// The user for whom this is being computed
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The title for this milestone - e.g., "61.109 - ASEL Private Pilot"
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Any general-purpose disclaimer
        /// </summary>
        public string GeneralDisclaimer { get; set; }

        /// <summary>
        /// Master airportlist for all of your routes
        /// </summary>
        protected AirportList AirportListOfRoutes { get; set; }

        /// <summary>
        /// Cached list of milestones - internal only.
        /// </summary>
        private Collection<MilestoneItem> CachedMilestones { get; set; }

        /// <summary>
        /// Has this been computed?
        /// </summary>
        public bool HasData { get { return CachedMilestones != null && CachedMilestones.Count > 0; } }

        /// <summary>
        /// Retrieves the milestones; forces a refresh if needed, thereafter are cached
        /// </summary>
        public Collection<MilestoneItem> ComputedMilestones
        {
            get
            {
                if (!HasData)
                    CachedMilestones = Refresh();
                return new Collection<MilestoneItem>(CachedMilestones);
            }
        }
        #endregion

        #region Utility Functions
        /// <summary>
        /// The base FAR (61.109(a) or 61.109(b))
        /// </summary>
        protected string BaseFAR { get; set; }

        protected RatingType RatingSought { get; set; }

        protected string ResolvedFAR(string sz)
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}{1}", BaseFAR, sz);
        }

        /// <summary>
        /// Determines if the category/class matches the rating being sought
        /// </summary>
        /// <param name="ccid">The category/class</param>
        /// <returns>True if it applies to the rating</returns>
        protected bool CatClassMatchesRatingSought(CategoryClass.CatClassID ccid)
        {
            switch (RatingSought)
            {
                case RatingType.PPLAirplaneMulti:
                case RatingType.CommercialAMES:
                case RatingType.CommercialAMEL:
                case RatingType.PPLPart141MultiEngine:
                    return (ccid == CategoryClass.CatClassID.AMEL || ccid == CategoryClass.CatClassID.AMES);
                case RatingType.PPLAirplaneSingle:
                case RatingType.CommercialASEL:
                case RatingType.CommercialASES:
                case RatingType.PPLPart141SingleEngine:
                    return (ccid == CategoryClass.CatClassID.ASEL || ccid == CategoryClass.CatClassID.ASES);
                case RatingType.PPLHelicopter:
                case RatingType.InstrumentHelicopter:
                case RatingType.CommercialHelicopter:
                case RatingType.PPLPart141Helicopter:
                case RatingType.SAPPLHelicopter:
                case RatingType.SAPPLNightHelicopter:
                case RatingType.CAPPLHelicopter:
                case RatingType.CANightHelicopter:
                case RatingType.PPLEASAHelicopter:
                    return ccid == CategoryClass.CatClassID.Helicopter;
                case RatingType.PPLEASANightAirplane:
                    return CategoryClass.IsAirplane(ccid);
                case RatingType.PPLEASANightHelicopter:
                    return (ccid == CategoryClass.CatClassID.Helicopter || ccid == CategoryClass.CatClassID.Gyroplane);
                case RatingType.InstrumentAirplane:
                case RatingType.SAPPLAirplane:
                case RatingType.SAPPLNightAirplane:
                case RatingType.CAPPLAirplaneLand:
                case RatingType.CAPPLAirplaneSea:
                case RatingType.CANightAirplane:
                case RatingType.PPLEASAAirplane:
                    return CategoryClass.IsAirplane(ccid);
                case RatingType.InstrumentPoweredLift:
                    return ccid == CategoryClass.CatClassID.PoweredLift;
                case RatingType.PPLPart141Gyroplane:
                case RatingType.PPLGyroplane:
                case RatingType.CommercialGyroplane:
                    return ccid == CategoryClass.CatClassID.Gyroplane;
                case RatingType.PPLPoweredLift:
                    return ccid == CategoryClass.CatClassID.PoweredLift;
                case RatingType.PPLAirship:
                    return ccid == CategoryClass.CatClassID.Airship;
                case RatingType.PPLGlider:
                case RatingType.PPLPart141Glider:
                case RatingType.CommercialGlider:
                    return ccid == CategoryClass.CatClassID.Glider;
                case RatingType.PPLBalloon:
                    return CategoryClass.IsBalloon(ccid);
                case RatingType.CommercialBalloonGas:
                    return ccid == CategoryClass.CatClassID.GasBalloon;
                case RatingType.CommercialBalloonHot:
                    return ccid == CategoryClass.CatClassID.HotAirBalloon;
                case RatingType.PPLPoweredParachute:
                    return ccid == CategoryClass.CatClassID.PoweredParachuteLand || ccid == CategoryClass.CatClassID.PoweredParachuteSea;
                case RatingType.PPLWeightShift:
                    return ccid == CategoryClass.CatClassID.WeightShiftControlLand || ccid == CategoryClass.CatClassID.WeightShiftControlSea;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion

        #region Object creation and Initialization
        protected MilestoneProgress()
        {
            Username = Title = GeneralDisclaimer = String.Empty;
        }

        protected MilestoneProgress(string szUser) : this()
        {
            Username = szUser;
        }
        #endregion

        /// <summary>
        /// Returns a set of milestone groups that represent ratings progress that can be tracked.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MilestoneGroup> AvailableProgressItems()
        {
            return new MilestoneGroup[] {
                new PrivatePilotMilestones(),   // PPL Ratings
                new InstrumentMilestones(),     // IFR Ratings.
                new RecreationalMilestones(),   // Recreational Pilot ratings
                new SportsMilestones(),         // Sports ratings
                new LAPLMilestones(),           // EASA LAPL ratings
                new CommercialMilestones(),     // Commercial Pilot Ratings
                new ATPMilestones(),            // ATP Ratings
                new Part135Milestones(),         // Part 135 Ratings
                new DPEMilestones()
            };
        }

        /// <summary>
        /// Computes the progress against this milestone
        /// </summary>
        /// <returns>A list of MilestoneItem objects</returns>
        public Collection<MilestoneItem> Refresh()
        {
            if (String.IsNullOrEmpty(Username))
                throw new MyFlightbookException("Cannot compute milestones on an empty user!");

            List<ExaminerFlightRow> lstRows = new List<ExaminerFlightRow>();
            StringBuilder sbRoutes = new StringBuilder();

            DBHelper dbh = new DBHelper(CurrencyExaminer.CurrencyQuery(CurrencyExaminer.CurrencyQueryDirection.Descending));
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", Username);
                    comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                },
                (dr) =>
                {
                    ExaminerFlightRow cfr = new ExaminerFlightRow(dr);
                    sbRoutes.AppendFormat("{0} ", cfr.Route);
                    lstRows.Add(cfr);   // we'll examine it below, after we've populated the routes
                });

            // Set up the airport list once for DB efficiency
            AirportListOfRoutes = new AirportList(sbRoutes.ToString());

            lstRows.ForEach(cfr => { ExamineFlight(cfr); });

            return Milestones;
        }

        abstract public void ExamineFlight(ExaminerFlightRow cfr);

        abstract public Collection<MilestoneItem> Milestones { get; }
    }
}
 