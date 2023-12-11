using MyFlightbook.Airports;
using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2013-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
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
        /// Additional details about how this particular progress item was calculated
        /// </summary>
        public virtual string Details { get { return string.Empty; } }

        /// <summary>
        /// Quick determination if there are any relevant details
        /// </summary>
        public virtual bool HasDetails { get { return false; } }

        /// <summary>
        /// The progress made so far
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public decimal Progress { get; set; }

        /// <summary>
        /// Progress achieved in a display format
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
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
                        return String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ProgressTemplate, ((int) Progress).PrettyString(), Percentage);
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
        [Newtonsoft.Json.JsonIgnore]
        public decimal Percentage
        {
            get { return Threshold == 0.0M ? 0.0M : (Progress / Threshold) * 100.0M; }
        }

        /// <summary>
        /// Percentage complete in a display format
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayPercent
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0:#,##0}%", Percentage); }
        }

        [Newtonsoft.Json.JsonIgnore]
        public virtual string ExpirationNote { get { return string.Empty; } }

        /// <summary>
        /// The type of milestone this represents
        /// </summary>
        [Newtonsoft.Json.JsonIgnore] 
        public MilestoneType Type { get; set; }

        /// <summary>
        /// For "AchieveOnce" milestones, contains a description of the match
        /// </summary>
        [Newtonsoft.Json.JsonIgnore] 
        public string MatchingEventText { get; set; }

        /// <summary>
        /// For "AchieveOnce" milestones, links to the matching flight.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int MatchingEventID { get; set; }

        /// <summary>
        /// Display title
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayTitle
        {
            get { return String.IsNullOrEmpty(FARRef) ? Title : String.Format(CultureInfo.CurrentCulture, "{0} - {1} {2}", FARRef, Title, MatchingEventText).Trim(); }
        }

        /// <summary>
        /// Has this been satisfied?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsSatisfied
        {
            get { return (this.Type == MilestoneType.AchieveOnce) ? Progress > 0 : Progress >= Threshold; }
        }

        /// <summary>
        /// If time in a training device may be substituted up to a point, this defines how much has been contributed so far.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore] 
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

        /// <summary>
        /// New milestone item based on an existing one
        /// </summary>
        public MilestoneItem(MilestoneItem mi)
        {
            InitObject();
            if (mi == null)
                return;
            Type = mi.Type;
            Title = mi.Title;
            Note = mi.Note;
            Threshold = mi.Threshold;
            FARRef = mi.FARRef;
            Progress= mi.Progress;
            MatchingEventText = mi.MatchingEventText;
            MatchingEventID= mi.MatchingEventID;
            TrainingDeviceContribution= mi.TrainingDeviceContribution;
        }
        #endregion

        public virtual void AddEvent(decimal x)
        {
            Progress += x;
        }

        /// <summary>
        /// Adds progress towards a rating in a training device up to a specified limit
        /// </summary>
        /// <param name="x">The progress to add</param>
        /// <param name="limit">The "do not exceed" amount that is allowed</param>
        /// <param name="fIsTrainingDevice">True if this is in a training device rather than a real aircraft</param>
        public virtual void AddTrainingEvent(decimal x, decimal limit, bool fIsTrainingDevice)
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

        public virtual void MatchFlightEvent(ExaminerFlightRow cfr)
        {
            if (Type == MilestoneType.AchieveOnce && !IsSatisfied)
            {
                if (cfr == null)
                    throw new ArgumentNullException(nameof(cfr));
                AddEvent(1);
                MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                MatchingEventID = cfr.flightID;
            }
        }
    }

    /// <summary>
    /// MilestoneItem that includes decayable status.  Typically this is test prep before practical test.
    /// </summary>
    [Serializable]
    public class MilestoneItemDecayable : MilestoneItem
    {
        protected FlightCurrency DecayingCurrency { get; set; } = new FlightCurrency(3, 2, true, string.Empty);

        public MilestoneItemDecayable() : base() { }

        public MilestoneItemDecayable(string title, string farref, string note, MilestoneType type, decimal threshold, int timespan, bool fCalendar = true) : base(title, farref, note, type, threshold)
        {
            DecayingCurrency = new FlightCurrency(threshold, timespan, fCalendar, string.Empty);
        }

        /// <summary>
        /// Adds a decayable event as of the specified date
        /// </summary>
        /// <param name="dt">The date of the event</param>
        /// <param name="x">Quantity of the event</param>
        /// <param name="fCountTowardsRating">True if it counts towards the rating (e.g., an old training event counts towards decayable currency but NOT towards ability to take a practical test)</param>
        public void AddDecayableEvent(DateTime dt, decimal x, bool fCountTowardsRating)
        {
            if (fCountTowardsRating)
                base.AddEvent(x);
            if (x > 0)
                DecayingCurrency.AddRecentFlightEvents(dt, x);
        }

        public override void AddEvent(decimal x)
        {
            throw new InvalidOperationException("Don't call AddEvent on a decayable; use AddDecayableEvent instead");
        }

        public override void AddTrainingEvent(decimal x, decimal limit, bool fIsTrainingDevice)
        {
            throw new InvalidOperationException("Don't call AddTrainingEvent on a decayable; use AddDecayableEvent instead");
        }

        public override void MatchFlightEvent(ExaminerFlightRow cfr)
        {
            throw new InvalidOperationException("Don't call MatchFlightEvent on a decayable; use AddDecayableEvent instead");
        }

        public override string ExpirationNote
        {
            get
            {
                return DecayingCurrency == null || !DecayingCurrency.HasBeenCurrent ? string.Empty : string.Format(CultureInfo.CurrentCulture,
                    DecayingCurrency.CurrentState == CurrencyState.NotCurrent ? Resources.Currency.FormatExpired : Resources.Currency.FormatCurrent, DecayingCurrency.ExpirationDate.ToShortDateString());
            }
        }
    }

    /// <summary>
    /// Class that encapsulates cross-country training requirements for a rating.
    /// Since XC training is only counted if it meets distance thresholds, this records the flights that
    /// were excluded
    /// </summary>
    [Serializable]
    public class MilestoneItemXC : MilestoneItem
    {
        #region properties
        private readonly List<ExaminerFlightRow> lstRows = new List<ExaminerFlightRow>();

        public override string Details
        {
            get
            {
                if (!lstRows.Any())
                    return string.Empty;

                StringBuilder sb = new StringBuilder(Resources.MilestoneProgress.DetailFlightsIgnored);
                sb.AppendLine();
                foreach (ExaminerFlightRow cfr in lstRows)
                    sb.AppendLine(String.Format(CultureInfo.CurrentCulture, "[{0:d}]({1}) - [*{2}*]({3}{4})",
                        cfr.dtFlight,
                        String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}", cfr.flightID).ToAbsoluteURL(HttpContext.Current.Request),
                        cfr.Route,
                        "~/mvc/Airport/MapRoute?Airports=".ToAbsoluteURL(HttpContext.Current.Request),
                        HttpUtility.UrlEncode(cfr.Route)));
                return sb.ToString();
            }
        }

        public override bool HasDetails { get { return lstRows.Any(); } }

        #endregion

        public void AddIgnoredFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            lstRows.Add(cfr);
        }

        public MilestoneItemXC() : base() { }

        public MilestoneItemXC(string title, string farref, string note, MilestoneType type, decimal threshold) : base(title, farref, note, type, threshold) { }
    }

    #region Groups of milestones
    /// <summary>
    /// A class which returns a grouping of concrete MilestoneProgress classes which logically belong together.  Doing this reduces class coupling.
    /// </summary>
    [Serializable]
    public abstract class MilestoneGroup
    {
        public abstract Collection<MilestoneProgress> Milestones { get; }
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
        InstrumentEASAIRAirplane,
        InstrumentUKIRRestricted,
        InstrumentAirplaneCA,
        InstrumentHelicopterCA,

        // Recreational Pilot - 61.99
        RecreationalPilot,

        // Sport pilot - 61.313
        SportSingleEngine,
        SportGlider,
        SportRotorcraft,
        SportWeightShift,

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
        CommercialCanadaAeroplane,

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

        // CFI - 61.183
        CFIASEL,
        CFIASES,
        CFIAMEL,
        CFIAMES,
        CFIGlider,
        CFIHelicopter,

        // Placeholder for unknown
        Unknown
    };

    /// <summary>
    /// Base class for milestone progress.  Uses the currency model.
    /// </summary>
    [Serializable]
    public abstract class MilestoneProgress : IFlightExaminer
    {
        public const string EASA_PART_FCL_LINK = "https://www.easa.europa.eu/sites/default/files/dfu/Part-FCL.pdf";
        public const string FAA_COMM_61129_LINK = "https://www.law.cornell.edu/cfr/text/14/61.129";

        #region properties
        /// <summary>
        /// The user for whom this is being computed
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
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
        [Newtonsoft.Json.JsonIgnore]
        protected AirportList AirportListOfRoutes { get; set; }

        /// <summary>
        /// Cached list of milestones - internal only.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        private Collection<MilestoneItem> CachedMilestones { get; set; }

        /// <summary>
        /// Has this been computed?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool HasData { get { return CachedMilestones != null && CachedMilestones.Count > 0; } }

        /// <summary>
        /// Retrieves the milestones; forces a refresh if needed, thereafter are cached
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
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

        /// <summary>
        /// A URL for a link to view the regulation.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string FARLink { get; set; }

        protected RatingType RatingSought { get; set; }

        protected string ResolvedFAR(string sz)
        {
            return String.IsNullOrEmpty(FARLink) ?
                String.Format(CultureInfo.CurrentCulture, "{0}{1}", BaseFAR, sz) :
                String.Format(CultureInfo.CurrentCulture, "[{0}{1}]({2})", BaseFAR, sz, FARLink);
        }

        /// <summary>
        /// Determines if the category/class matches the rating being sought
        /// </summary>
        /// <param name="ccid">The category/class</param>
        /// <returns>True if it applies to the rating</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
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
                case RatingType.InstrumentHelicopterCA:
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
                case RatingType.InstrumentAirplaneCA:
                case RatingType.InstrumentEASAIRAirplane:
                case RatingType.InstrumentUKIRRestricted:
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

        /// <summary>
        /// Try to capture the distance requirements for 61.1 based on rating being sought.  Generally 50nm for most ratings, but 25 for rotorcraft and 15 for powered parachutes
        /// Returns -1 outside of FAA jurisdictions (e.g., EASA) or for ratings where no landing is required (e.g., ATP).  Thus the ">" check (vs. >=) will work for any such flight.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        protected int MinXCDistanceForRating()
        {
            switch (RatingSought)
            {
                // (i) - basic XC, no distance threshold.
                // (vi) - ATP is 50nm, but no landing required, so use 0
                default:
                    return -1;

                // (ii) - PPL, Commercial, or instrument: 50nm, except rotorcraft (v - 25nm) and powered parachutes (iv - 15nm)
                case RatingType.PPLAirplaneSingle:
                case RatingType.PPLAirplaneMulti:
                case RatingType.PPLPoweredLift:
                case RatingType.PPLGlider:
                case RatingType.PPLAirship:
                case RatingType.PPLBalloon:
                case RatingType.PPLWeightShift:
                case RatingType.PPLPart141SingleEngine:
                case RatingType.PPLPart141MultiEngine:
                case RatingType.PPLPart141Glider:
                case RatingType.InstrumentAirplane:
                case RatingType.InstrumentPoweredLift:
                case RatingType.CommercialASEL:
                case RatingType.CommercialASES:
                case RatingType.CommercialAMEL:
                case RatingType.CommercialAMES:
                case RatingType.CommercialBalloonHot:
                case RatingType.CommercialBalloonGas:
                case RatingType.Commercial141AirplaneSingleEngineLand:
                case RatingType.Commercial141AirplaneSingleEngineSea:
                case RatingType.Commercial141AirplaneMultiEngineLand:
                case RatingType.Commercial141AirplaneMultiEngineSea:
                case RatingType.CommercialGlider:
                case RatingType.RecreationalPilot:
                    return 50;

                case RatingType.PPLPart141Helicopter:
                case RatingType.PPLPart141Gyroplane:
                case RatingType.PPLHelicopter:
                case RatingType.PPLGyroplane:
                case RatingType.InstrumentHelicopter:
                case RatingType.CommercialHelicopter:
                case RatingType.CommercialGyroplane:
                case RatingType.Commercial141Helicopter:
                    return 25;

                // (iii) and (iv) - sport pilot: 25nm unless powered parachute
                case RatingType.SportSingleEngine:
                case RatingType.SportGlider:
                case RatingType.SportRotorcraft:
                case RatingType.SportWeightShift:
                    return 25;

                // (iv) - PPL powered parachute 
                case RatingType.PPLPoweredParachute:
                    return 15;
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
        public static IEnumerable<MilestoneGroup> AvailableProgressItems(string szUser)
        {
            List<MilestoneGroup> lst = new List<MilestoneGroup>() {
                new PrivatePilotMilestones(),   // PPL Ratings
                new InstrumentMilestones(),     // IFR Ratings.
                new RecreationalMilestones(),   // Recreational Pilot ratings
                new SportsMilestones(),         // Sports ratings
                new LAPLMilestones(),           // EASA LAPL ratings
                new CommercialMilestones(),     // Commercial Pilot Ratings
                new ATPMilestones(),            // ATP Ratings
                new Part135Milestones(),        // Part 135 Ratings
                new DPEMilestones(),            // DPEMilestones
                new CFIMilestones(),            // CFI Milestones
                new CustomRatingsGroup(szUser)  // Custom milestones
            };
            return lst;
        }

        /// <summary>
        /// Computes the progress against this milestone
        /// </summary>
        /// <returns>A list of MilestoneItem objects</returns>
        virtual public Collection<MilestoneItem> Refresh()
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
                    sbRoutes.AppendFormat(CultureInfo.InvariantCulture, "{0} ", cfr.Route);
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
 