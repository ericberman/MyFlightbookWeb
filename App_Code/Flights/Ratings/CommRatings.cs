using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;

/******************************************************
 * 
 * Copyright (c) 2013-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
{
    /// <summary>
    /// Commercial rating milestones
    /// </summary>
    [Serializable]
    public class CommercialMilestones : MilestoneGroup
    {
        public CommercialMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupCommercial;
            Milestones = new MilestoneProgress[] {
                new Comm61129ASEL(),
                new Comm61129ASES(),
                new Comm61129AMEL(),
                new Comm61129AMES(),
                new Comm61129Helicopter(),
                new Comm61129Glider(),
                new Comm61129HotAirBalloon(),
                new Comm61129GasBalloon(),
                new Comm141SingleEngineAirplane(),
                new Comm141MultiEngineAirplane(),
                new Comm141Helicopter(),
                new CASRCommAirplaneApprovedTraining(),
                new CASRCommAirplaneNoApprovedTraining(),
                new CASRCommHelicopterApprovedTraining(),
                new CASRCommHelicopterNoApprovedTraining()
                };
        }
    }

    #region 61.129 - Commercial Pilot
    [Serializable]
    public abstract class CommBase : MilestoneProgress
    {
        /// <summary>
        /// Complex training generally requires complex OR turbine.
        /// </summary>
        /// <param name="cfr"></param>
        /// <returns></returns>
        protected bool IsComplexOrTurbine(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return (cfr.fIsComplex || cfr.turbineLevel == MakeModel.TurbineLevel.Jet || cfr.turbineLevel == MakeModel.TurbineLevel.UnspecifiedTurbine || cfr.turbineLevel == MakeModel.TurbineLevel.TurboProp);
        }
    }

    [Serializable]
    public abstract class Comm61129Base : CommBase
    {
        #region Thresholds and parameters
        private int _minTotalTime = 250;
        private int _minPIC = 100;
        private int _minPICInCategory = 50;
        private int _minPICXC = 50;
        private int _minPICXCInCategory = 10;
        private int _minSimIMC = 10;
        private int _minSimIMCInCategory = 5;
        private int _minPoweredTime = 100;
        private int _minPoweredInCategory = 50;
        private int _minSoloPIC = 10;
        private int _minDistanceXCTraining = 100;
        private int _minSubstDualForPIC = 10;

        #region public accessors for thresholds and parameters
        protected int MinTotalTime
        {
            get { return _minTotalTime; }
            set { _minTotalTime = value; }
        }

        protected int MinPICInCategory
        {
            get { return _minPICInCategory; }
            set { _minPICInCategory = value; }
        }

        protected int MinSimIMC
        {
            get { return _minSimIMC; }
            set { _minSimIMC = value; }
        }

        protected int MinDistanceXCTraining
        {
            get { return _minDistanceXCTraining; }
            set { _minDistanceXCTraining = value; }
        }
        #endregion
        #endregion

        protected MilestoneItem miTotalTime { get; set; }
        protected MilestoneItem miMinPowered { get; set; }
        protected MilestoneItem miMinPoweredInCategory { get; set; }
        protected MilestoneItem miPICMin { get; set; }
        protected MilestoneItem miPICMinCategory { get; set; }
        protected MilestoneItem miPICMinXC { get; set; }
        protected MilestoneItem miPICMinXCCategory { get; set; }
        protected MilestoneItem miMinTraining { get; set; }
        protected MilestoneItem miMintrainingSimIMC { get; set; }
        protected MilestoneItem miMinTrainingSimIMCCategory { get; set; }
        protected MilestoneItem miMinTrainingComplex { get; set; }
        protected MilestoneItem miMinXCCategory { get; set; }
        protected MilestoneItem miMinXCCategoryNight { get; set; }
        protected MilestoneItem miMinTestPrep { get; set; }
        protected MilestoneItem miMinSoloOrPICCategory { get; set; }
        protected MilestoneItem miMinSoloORPICXC { get; set; }
        protected MilestoneItem miMinNight { get; set; }
        protected MilestoneItem miMinNightTO { get; set; }
        protected MilestoneItem miMinNightLandings { get; set; }
        protected MilestoneItem miDualAsPIC { get; set; }
        protected MilestoneItem miDualAsPICXC { get; set; }

        protected Comm61129Base() {
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.CommGeneralDisclaimer);
        }

        /// <summary>
        /// Is this for a commercial airplane rating?
        /// </summary>
        protected bool IsAirplaneRating
        {
            get { return RatingSought == RatingType.CommercialAMEL || RatingSought == RatingType.CommercialAMES || RatingSought == RatingType.CommercialASEL || RatingSought == RatingType.CommercialASES; }
        }

        protected void Init(string szBaseFAR, string szTitle, RatingType ratingSought, string szCatClass, string szCategory)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = ratingSought;

            string szFTDNote = Branding.ReBrand(Resources.MilestoneProgress.Note61129TrainingDevice);
            // 61.129 (a/b)
            miTotalTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinTime, _minTotalTime), ResolvedFAR(string.Empty), szFTDNote, MilestoneItem.MilestoneType.Time, _minTotalTime);

            // 61.129 (a/b)(1)
            miMinPowered = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPoweredTime, _minPoweredTime), ResolvedFAR("(1)"), szFTDNote, MilestoneItem.MilestoneType.Time, _minPoweredTime);
            miMinPoweredInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPoweredInCategory, _minPoweredInCategory, szCategory), ResolvedFAR("(1)"), szFTDNote, MilestoneItem.MilestoneType.Time, _minPoweredInCategory);

            // 61.129 (a/b)(2)
            miPICMin = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPIC, _minPIC), ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPIC);
            miPICMinCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPICInCategory, _minPICInCategory, szCategory), ResolvedFAR("(2)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPICInCategory);
            miPICMinXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPICXC, _minPICXC), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPICXC);
            miPICMinXCCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPICXCInCategory, _minPICXCInCategory, szCategory), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPICXCInCategory);

            miDualAsPIC = new MilestoneItem("local - dual as PIC", ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, _minSubstDualForPIC);
            miDualAsPICXC = new MilestoneItem("local - dual as PICXC", ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, _minSubstDualForPIC);

            // 61.129 (a/b)(3)
            miMinTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinTraining, 20), ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.Time, 20.0M);
            miMintrainingSimIMC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinSimIMC, _minSimIMC), ResolvedFAR("(3)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, _minSimIMC);
            miMinTrainingSimIMCCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinSimIMCInCategory, _minSimIMCInCategory, szCatClass), ResolvedFAR("(3)(i)"), Resources.MilestoneProgress.Note611293i, MilestoneItem.MilestoneType.Time, _minSimIMCInCategory);

            string szComplex;
            switch (RatingSought)
            {
                default:
                    szComplex = string.Empty;
                    break;
                case RatingType.CommercialASEL:
                    szComplex = Resources.MilestoneProgress.CommMinComplexASEL;
                    break;
                case RatingType.CommercialASES:
                    szComplex = Resources.MilestoneProgress.CommMinComplexASES;
                    break;
                case RatingType.CommercialAMEL:
                    szComplex = Resources.MilestoneProgress.CommMinComplexAMEL;
                    break;
                case RatingType.CommercialAMES:
                    szComplex = Resources.MilestoneProgress.CommMinComplexAMES;
                    break;
            }
            miMinTrainingComplex = new MilestoneItem(szComplex, ResolvedFAR("(3)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, 10);
            miMinXCCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommXCCategory, szCatClass, _minDistanceXCTraining), ResolvedFAR("(3)(iii)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinXCCategoryNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommXCCategoryNight, szCatClass, _minDistanceXCTraining), ResolvedFAR("(3)(iv)"), Resources.MilestoneProgress.NoteNightXC, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinTestPrep = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTestPrep, szCatClass), ResolvedFAR("(3)(v)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M);

            miMinSoloOrPICCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloCategory, _minSoloPIC, szCatClass), ResolvedFAR("(4)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, _minSoloPIC);
            miMinSoloORPICXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloXCCategory, szCatClass), ResolvedFAR("(4)(i)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightFlight, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, 5);
            miMinNightTO = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightTakeoffs, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);
            miMinNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightLandings, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (!cfr.fIsRealAircraft)
                return;

            // 61.129(a/b)
            miTotalTime.AddEvent(cfr.Total);

            // 61.129(a/b)(1)
            CategoryClass cc = CategoryClass.CategoryClassFromID(cfr.idCatClassOverride);
            if (CategoryClass.IsPowered(cc.IdCatClass))
                miMinPowered.AddEvent(cfr.Total);

            bool fCatClassMatches = CatClassMatchesRatingSought(cc.IdCatClass);
            bool fIsInCategory = fCatClassMatches || (IsAirplaneRating && CategoryClass.IsAirplane(cc.IdCatClass));
            if (fIsInCategory)
                miMinPoweredInCategory.AddEvent(cfr.Total);

            // 61.129(a/b)(2)
            decimal soloTime = 0.0M;
            decimal instructorOnBoardTime = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;
            int cNightLandingsAtToweredAirport = 0;
            int cNightTakefoffsAtToweredAirport = 0;
            int nightTakeoffs = 0;
            if (fCatClassMatches)
            {
                cfr.ForEachEvent(pf =>
                {
                    if (pf.PropertyType.IsSolo)
                        soloTime += pf.DecValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                        fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                        dutiesOfPICTime += pf.DecValue;
                    if (pf.PropertyType.IsNightTakeOff)
                        nightTakeoffs += pf.IntValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropLandingToweredNight)
                        cNightLandingsAtToweredAirport += pf.IntValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffToweredNight)
                        cNightTakefoffsAtToweredAirport += pf.IntValue;
                });
            }

            if (fInstructorOnBoard)
                instructorOnBoardTime = Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            decimal PIC = Math.Max(cfr.PIC, soloTime);
            decimal PICXC = Math.Min(PIC, cfr.XC);

            // We can add up to 10 hours each of dual and dual XC towards this time, but only if not also logged as PIC. (Avoid double counting)
            // See http://www.faa.gov/about/office_org/headquarters_offices/agc/pol_adjudication/agc200/interpretations/data/interps/2014/kuhn%20-%20%282014%29%20legal%20interpretation.pdf
            // for why this is not the same as dual.
            decimal PICSubstRemaining = Math.Max(miDualAsPIC.Threshold - miDualAsPIC.Progress, 0);
            decimal PICSubstXCRemaining = Math.Max(miDualAsPICXC.Threshold - miDualAsPICXC.Progress, 0);
            decimal PICSubst = Math.Min(PICSubstRemaining, instructorOnBoardTime);   // add in any duties-of-PIC time with instructor on-board 
            decimal PICSubstXC = Math.Min(PICSubstXCRemaining, Math.Min(instructorOnBoardTime, cfr.XC));
            miDualAsPIC.AddEvent(PICSubst);
            miDualAsPICXC.AddEvent(PICSubstXC);

            miPICMin.AddEvent(PIC);
            miPICMin.AddEvent(PICSubst);

            if (fIsInCategory)
            {
                miPICMinCategory.AddEvent(PIC);
                miPICMinCategory.AddEvent(PICSubst);
            }

            miPICMinXC.AddEvent(PICXC);
            miPICMinXC.AddEvent(PICSubstXC);
            if (fIsInCategory)
            {
                miPICMinXCCategory.AddEvent(PICXC);
                miPICMinXCCategory.AddEvent(PICSubstXC);
            }

            // 61.129(a/b)(3)
            miMinTraining.AddEvent(cfr.Dual);

            // (3)(i)
            miMintrainingSimIMC.AddEvent(Math.Min(cfr.Dual, cfr.IMCSim));

            // (3)(ii) - complex training: complex or turbine
            bool fIsComplex = false;

            switch (RatingSought)
            {
                case RatingType.CommercialASEL:
                    // can be complex OR turbine, can be AMEL or ASEL
                    fIsComplex = IsComplexOrTurbine(cfr) && (cc.IdCatClass == CategoryClass.CatClassID.ASEL || cc.IdCatClass == CategoryClass.CatClassID.AMEL);
                    break;
                case RatingType.CommercialAMEL:
                    // can be complex OR turbine, MUST be AMEL
                    fIsComplex = IsComplexOrTurbine(cfr) && cc.IdCatClass == CategoryClass.CatClassID.AMEL;
                    break;
                case RatingType.CommercialASES:
                    fIsComplex = cfr.fIsComplex && cc.IdCatClass == CategoryClass.CatClassID.ASES;
                    break;
                case RatingType.CommercialAMES:
                    // Must be complex - turbine is not sufficient, MUST match catclass.
                    fIsComplex = cfr.fIsComplex && cc.IdCatClass == CategoryClass.CatClassID.AMES;
                    break;
            }
            // Comp
            if (fIsComplex)
                miMinTrainingComplex.AddEvent(cfr.Dual);

            if (fCatClassMatches)
            {
                miMinTrainingSimIMCCategory.AddEvent(Math.Min(cfr.Dual, cfr.IMCSim));

                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route);
                double distFromStart = al.MaxDistanceFromStartingAirport();
                if (cfr.Dual > 0)
                {
                    // (3)(iii)
                    if ((cfr.XC - cfr.Night) >= 2.0M && distFromStart > _minDistanceXCTraining)
                        miMinXCCategory.MatchFlightEvent(cfr);

                    // (3)(iv)
                    if (Math.Min(cfr.XC, cfr.Night) >= 2.0M && distFromStart > _minDistanceXCTraining)
                        miMinXCCategoryNight.MatchFlightEvent(cfr);

                    // (3)(v)
                    if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                        miMinTestPrep.AddEvent(cfr.Dual);
                }

                // (4)
                // Solo time for section 4 is defined as EITHER solo time OR duties of PIC time with an authorized instructor on board.  We computed the latter above
                soloTime = Math.Max(soloTime, instructorOnBoardTime);

                miMinSoloOrPICCategory.AddEvent(soloTime);

                // (4)(i) - Long solo cross-country
                if (soloTime > 0 && cfr.XC > 0 && cfr.cLandingsThisFlight >= 3 && al.GetNormalizedAirports().Count() >= 3)
                {
                    bool fMeetsReqs = false;
                    switch (RatingSought)
                    {
                        case RatingType.CommercialHelicopter:
                            fMeetsReqs = al.MaxSegmentForRoute() > 50.0 && distFromStart >= 50.0;
                            break;
                        case RatingType.CommercialAMEL:
                        case RatingType.CommercialAMES:
                        case RatingType.CommercialASEL:
                        case RatingType.CommercialASES:
                            fMeetsReqs = al.DistanceForRoute() > 300 && (al.GetNormalizedAirports()[0].IsHawaiian ? al.MaxSegmentForRoute() >= 150 : distFromStart >= 250);
                            break;
                        default:
                            break;
                    }
                    if (fMeetsReqs)
                        miMinSoloORPICXC.MatchFlightEvent(cfr);
                }

                // (4)(ii)
                if (soloTime > 0 && (cfr.Night - cfr.IMC) > 0.0M)
                {
                    miMinNight.AddEvent(Math.Min(soloTime, cfr.Night - cfr.IMC));
                    miMinNightTO.AddEvent(Math.Max(nightTakeoffs, cNightTakefoffsAtToweredAirport));
                    miMinNightLandings.AddEvent(Math.Max(cfr.cFullStopNightLandings, cNightLandingsAtToweredAirport));
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> l = new Collection<MilestoneItem>();
                l.Add(miTotalTime);
                l.Add(miMinPowered);
                l.Add(miMinPoweredInCategory);
                l.Add(miPICMin);
                l.Add(miPICMinCategory);
                l.Add(miPICMinXC);
                l.Add(miPICMinXCCategory);
                l.Add(miMinTraining);
                l.Add(miMintrainingSimIMC);
                l.Add(miMinTrainingSimIMCCategory);
                l.Add(miMinTrainingComplex);
                l.Add(miMinXCCategory);
                l.Add(miMinXCCategoryNight);
                l.Add(miMinTestPrep);
                l.Add(miMinSoloOrPICCategory);
                l.Add(miMinSoloORPICXC);
                l.Add(miMinNight);
                l.Add(miMinNightTO);
                l.Add(miMinNightLandings);
                return l;
            }
        }
    }

    #region Concrete Commercial classes
    /// <summary>
    /// Commercial ASEL
    /// </summary>
    [Serializable]
    public class Comm61129ASEL : Comm61129Base
    {
        public Comm61129ASEL()
        {
            Init("61.129(a)", Resources.MilestoneProgress.Title61129A, RatingType.CommercialASEL, Resources.MilestoneProgress.ratingAirplaneSingle, Resources.MilestoneProgress.ratingAirplane);
        }
    }

    /// <summary>
    /// Commercial AMEL
    /// </summary>
    [Serializable]
    public class Comm61129AMEL : Comm61129Base
    {
        public Comm61129AMEL()
        {
            Init("61.129(b)", Resources.MilestoneProgress.Title61129B, RatingType.CommercialAMEL, Resources.MilestoneProgress.ratingAirplaneMulti, Resources.MilestoneProgress.ratingAirplane);
        }
    }

    /// <summary>
    /// Commercial ASES
    /// </summary>
    [Serializable]
    public class Comm61129ASES : Comm61129Base
    {
        public Comm61129ASES()
        {
            Init("61.129(b)", Resources.MilestoneProgress.Title61129ASEA, RatingType.CommercialASES, Resources.MilestoneProgress.ratingAirplaneSingle, Resources.MilestoneProgress.ratingAirplane);
        }
    }

    /// <summary>
    /// Commercial AMES
    /// </summary>
    [Serializable]
    public class Comm61129AMES : Comm61129Base
    {
        public Comm61129AMES()
        {
            Init("61.129(b)", Resources.MilestoneProgress.Title61129BSEA, RatingType.CommercialAMES, Resources.MilestoneProgress.ratingAirplaneMulti, Resources.MilestoneProgress.ratingAirplane);
        }
    }

    /// <summary>
    /// Commercial Helicopter
    /// </summary>
    [Serializable]
    public class Comm61129Helicopter : Comm61129Base
    {
        public Comm61129Helicopter()
        {
            MinTotalTime = 150;
            MinPICInCategory = 35;
            MinSimIMC = 5;
            MinDistanceXCTraining = 50;
            Init("61.129(c)", Resources.MilestoneProgress.Title61129C, RatingType.CommercialHelicopter, Resources.MilestoneProgress.ratingHelicopter, Resources.MilestoneProgress.ratingHelicopter);

            // fix up some FAR references:
            miMinXCCategory.FARRef = ResolvedFAR("(3)(ii)");
            miMinXCCategoryNight.FARRef = ResolvedFAR("(3)(iii)");
            miMinTestPrep.FARRef = ResolvedFAR("(3)(iv)");
            miMinSoloORPICXC.Title = Resources.MilestoneProgress.CommSoloXCHelicopter;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // everything for airplanes applies to helicopters EXCEPT complex and the broad cross-country time requirement
                Collection<MilestoneItem> l = base.Milestones;
                l.Remove(miMinTrainingComplex);
                l.Remove(miPICMinXC);
                return l;
            }
        }
    }

    /// <summary>
    /// Commercial Glider
    /// </summary>
    [Serializable]
    public class Comm61129Glider : MilestoneProgress
    {
        private MilestoneItem miGliderTime { get; set; }
        private MilestoneItem miGliderFlightsPIC1i { get; set; }
        private MilestoneItem miFlightTrainingTime { get; set; }
        private MilestoneItem miTrainingFlights { get; set; }
        private MilestoneItem miTestPrep { get; set; }
        private MilestoneItem miSoloFlightTime { get; set; }
        private MilestoneItem miSoloFlights1ii { get; set; }
        private MilestoneItem miHeavierThanAir { get; set; }
        private MilestoneItem miGliderFlightsPIC2i { get; set; }
        private MilestoneItem miSoloFlights2ii { get; set; }

        public Comm61129Glider() : base()
        {
            Title = Resources.MilestoneProgress.Title61129F;
            BaseFAR = "61.129(f)";
            RatingSought = RatingType.CommercialGlider;

            miGliderTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderGliderTime, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, 25);
            miGliderFlightsPIC1i = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderFlightsPIC, 100), ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Count, 100);
            miFlightTrainingTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderTrainingTime, ResolvedFAR("(1)(i) or (2)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 3);
            miTrainingFlights = new MilestoneItem(Resources.MilestoneProgress.CommGliderTrainingFlights, ResolvedFAR("(1)(i) or (2)(i)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.CommGliderTestPrep, ResolvedFAR("(1)(i) or (2)(i)"), Branding.ReBrand(Resources.MilestoneProgress.Comm141TestPrepNote), MilestoneItem.MilestoneType.Count, 3);
            miSoloFlightTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderSoloTime, ResolvedFAR("(1)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, 2);
            miSoloFlights1ii = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderSoloFlights, 10), ResolvedFAR("(1)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);

            miHeavierThanAir = new MilestoneItem(Resources.MilestoneProgress.CommGliderHeavierThanAir, ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, 200);
            miGliderFlightsPIC2i = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderFlightsPIC, 20), ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Count, 20);
            miSoloFlights2ii = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderSoloFlights, 5), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 5);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // simulators need not apply!
            if (!cfr.fIsRealAircraft)
                return;

            if (CategoryClass.IsHeavierThanAir(cfr.idCatClassOverride))
                miHeavierThanAir.AddEvent(cfr.Total);

            // Done with everything that is not a glider
            if (!CatClassMatchesRatingSought(cfr.idCatClassOverride))
                return;

            miGliderTime.AddEvent(cfr.Total);
            if (cfr.PIC > 0)
            {
                miGliderFlightsPIC1i.AddEvent(1);
                miGliderFlightsPIC2i.AddEvent(1);
            }

            int cLandings = Math.Max(1, cfr.cLandingsThisFlight);

            if (cfr.Dual > 0)
            {
                miFlightTrainingTime.AddEvent(cfr.Dual);
                miTrainingFlights.AddEvent(cLandings);
                if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miTestPrep.AddEvent(cLandings);
            }

            decimal soloTime = cfr.TotalTimeForPredicate(cfp => cfp.PropertyType.IsSolo);
            if (soloTime > 0)
            {
                miSoloFlightTime.AddEvent(soloTime);
                miSoloFlights1ii.AddEvent(cLandings);
                miSoloFlights2ii.AddEvent(cLandings);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> result = (miHeavierThanAir.IsSatisfied) ?
                    new Collection<MilestoneItem>() { miHeavierThanAir, miGliderFlightsPIC2i, miFlightTrainingTime, miTrainingFlights, miTestPrep, miSoloFlights2ii } :
                    new Collection<MilestoneItem>() { miGliderTime, miGliderFlightsPIC1i, miFlightTrainingTime, miTrainingFlights, miTestPrep, miSoloFlightTime, miSoloFlights1ii };

                if (miFlightTrainingTime.IsSatisfied && !miTrainingFlights.IsSatisfied)
                    result.Remove(miTrainingFlights);
                else if (miTrainingFlights.IsSatisfied && !miFlightTrainingTime.IsSatisfied)
                    result.Remove(miFlightTrainingTime);

                return result;
            }
        }

    }

    /// <summary>
    /// Commercial Balloon
    /// </summary>
    [Serializable]
    public abstract class Comm61129Balloon : MilestoneProgress
    {
        #region parameters and thresholds
        private int _minHoursTotalInBalloons = 20;
        private int _minFlightsInBalloons = 10;
        private int _minFlightsAsPIC = 2;
        private int _minFlightTrainingTime = 10;
        private int _minFlightTrainingFlights = 10;
        private int _minFlightDPIC = 2;
        private int _minFlightHASolo = 2;
        private decimal _minTrainingFlightLength = 2;
        private decimal _minAscentHeight = 3000;
        private string _szFARBase = string.Empty;
        private string _szTestPrep = string.Empty;

        #region public accessors
        protected decimal MinTrainingFlightLength
        {
            get { return _minTrainingFlightLength; }
            set { _minTrainingFlightLength = value; }
        }

        protected decimal MinAscentHeight
        {
            get { return _minAscentHeight; }
            set { _minAscentHeight = value; }
        }

        protected string FARBase
        {
            get { return _szFARBase; }
            set { _szFARBase = value; }
        }

        protected string TestPrep
        {
            get { return _szTestPrep; }
            set { _szTestPrep = value; }
        }
        #endregion
        #endregion

        protected MilestoneItem miBalloonTime { get; set; }
        protected MilestoneItem miBalloonFlights { get; set; }
        protected MilestoneItem miBalloonPIC { get; set; }
        protected MilestoneItem miBalloonTrainingFlights { get; set; }
        protected MilestoneItem miBalloonTrainingTime { get; set; }
        protected MilestoneItem miTestPrep { get; set; }
        protected MilestoneItem miGasDPIC { get; set; }
        protected MilestoneItem miHASolo { get; set; }
        protected MilestoneItem miControlledAscent { get; set; }

        protected Comm61129Balloon()
            : base()
        {
        }

        protected void Init(string szBaseFAR, string szTitle, RatingType ratingSought)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = ratingSought;

            miBalloonTime = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinTime, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, _minHoursTotalInBalloons);
            miBalloonFlights = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinFlights, ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightsInBalloons);
            miBalloonPIC = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinPIC, ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.Time, _minFlightsAsPIC);
            miBalloonTrainingFlights = new MilestoneItem(Resources.MilestoneProgress.CommBalloonTrainingFlights, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightTrainingFlights);
            miBalloonTrainingTime = new MilestoneItem(Resources.MilestoneProgress.CommBalloonTrainingTime, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.Time, _minFlightTrainingTime);
            miTestPrep = new MilestoneItem(_szTestPrep, String.Format(CultureInfo.CurrentCulture, "{0}(A)", ResolvedFAR(_szFARBase)), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Count, _minFlightTrainingFlights);
            miGasDPIC = new MilestoneItem(Resources.MilestoneProgress.CommBalloonDPIC, String.Format(CultureInfo.CurrentCulture, "{0}(B)", ResolvedFAR(_szFARBase)), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightDPIC);
            miHASolo = new MilestoneItem(Resources.MilestoneProgress.CommBalloonSolo, String.Format(CultureInfo.CurrentCulture, "{0}(B)", ResolvedFAR(_szFARBase)), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightHASolo);
            miControlledAscent = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommBalloonAscent, MinAscentHeight), String.Format(CultureInfo.CurrentCulture, "{0}(C)", ResolvedFAR(_szFARBase)), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() { miBalloonTime, miBalloonFlights, miBalloonPIC, miBalloonTrainingFlights, miBalloonTrainingTime, miTestPrep, miGasDPIC, miHASolo, miControlledAscent };
            }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (!CategoryClass.IsBalloon(cfr.idCatClassOverride))
                return;

            miBalloonTime.AddEvent(cfr.Total);
            miBalloonFlights.AddEvent(1);
            miBalloonPIC.AddEvent(cfr.PIC);
            if (cfr.Dual > 0)
            {
                miBalloonTrainingFlights.AddEvent(1);
                miBalloonTrainingTime.AddEvent(cfr.Dual);
            }

            decimal dpic = 0.0M;
            decimal solo = 0.0M;
            decimal maxAscent = 0.0M;

            // see if DPIC
            cfr.ForEachEvent((pe) =>
            {
                if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC)
                    dpic += pe.DecValue;
                if (pe.PropertyType.IsSolo)
                    solo += pe.DecValue;
                if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMaximumAltitude)
                    maxAscent = pe.IntValue;
            });

            if (CatClassMatchesRatingSought(cfr.idCatClassOverride))
            {
                if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0 && cfr.Dual > 0 && cfr.Total > MinTrainingFlightLength)
                    miTestPrep.AddEvent(1);

                if (dpic + cfr.PIC > 0)
                    miGasDPIC.AddEvent(1);
                if (solo > 0)
                    miHASolo.AddEvent(1);
            }

            if (maxAscent > MinAscentHeight)
                miControlledAscent.MatchFlightEvent(cfr);
        }
    }

    /// <summary>
    /// Commercial Gas Balloon
    /// </summary>
    [Serializable]
    public class Comm61129GasBalloon : Comm61129Balloon
    {
        public Comm61129GasBalloon()
            : base()
        {
            FARBase = "(i)";
            MinAscentHeight = 5000;
            MinTrainingFlightLength = 2;
            TestPrep = Resources.MilestoneProgress.CommBalloonGasTestPrep;
            Init("61.129(h)", Resources.MilestoneProgress.Title61129HGasBalloon, RatingType.CommercialBalloonGas);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> lst = base.Milestones;
                lst.Remove(miHASolo);
                return lst;
            }
        }
    }

    /// <summary>
    /// Commercial Hot Air Balloon
    /// </summary>
    [Serializable]
    public class Comm61129HotAirBalloon : Comm61129Balloon
    {
        public Comm61129HotAirBalloon()
            : base()
        {
            FARBase = "(ii)";
            MinAscentHeight = 3000;
            MinTrainingFlightLength = 1;
            TestPrep = Resources.MilestoneProgress.CommBalloonHotAirTestPrep;
            Init("61.129(h)", Resources.MilestoneProgress.Title61129HHotAirBalloon, RatingType.CommercialBalloonHot);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> lst = base.Milestones;
                lst.Remove(miGasDPIC);
                return lst;
            }
        }
    }
    #endregion

    #region Part 141 commercial
    /// <summary>
    /// Base class for part 141 commercial.  See https://www.law.cornell.edu/cfr/text/14/part-141/appendix-D for rules.
    /// </summary>
    [Serializable]
    public abstract class Comm141Base : CommBase
    {
        #region Thresholds and parameters
        private int _minTotalTraining = 55;
        private int _minInstrumentTraining = 10;
        private int _minInstrumentTrainingInCatClass = 5;
        private int _minComplexOrTurbineTraining = 10;
        private decimal _minTimeXCDayFlight = 2;
        private double _minDistXCDayFlight = 100;
        private decimal _minTimeXCNightFlight = 2;
        private double _minDistXCNightFlight = 100;
        private decimal _minTestPrep = 3;
        private double _minDaysTestPrep = 60;

        // Solo thresholds
        private int _minSoloInCatClass = 10;
        private int _minSoloXCDistance = 250;
        private int _minSoloXCDistanceHawaii = 150;
        private int _minSoloNightHours = 5;
        private int _minSoloNightTakeoffs = 10;
        private int _minSoloNightLandings = 10;

        public int MinTotalTraining
        {
            get { return _minTotalTraining; }
            set { _minTotalTraining = value; }
        }

        public double MinDistXCNightFlight
        {
            get { return _minDistXCNightFlight; }
            set { _minDistXCNightFlight = value; }
        }

        public double MinDistXCDayFlight
        {
            get { return _minDistXCDayFlight; }
            set { _minDistXCDayFlight = value; }
        }

        public int MinInstrumentTraining
        {
            get { return _minInstrumentTraining; }
            set { _minInstrumentTraining = value; }
        }

        public int MinSoloXCDistance
        {
            get { return _minSoloXCDistance; }
            set { _minSoloXCDistance = value; }
        }
        public int MinSoloXCDistanceHawaii
        {
            get { return _minSoloXCDistanceHawaii; }
            set { _minSoloXCDistanceHawaii = value; }
        }
        #endregion

        protected MilestoneItem miOverallTraining { get; set; }
        protected MilestoneItem miTotalTraining { get; set; }
        protected MilestoneItem miInstrumentTraining { get; set; }
        protected MilestoneItem miInstrumentTrainingInCatClass { get; set; }
        protected MilestoneItem miComplexTurbineTraining { get; set; }
        protected MilestoneItem miDayXCFlight { get; set; }
        protected MilestoneItem miNightXCFlight { get; set; }
        protected MilestoneItem miTestPrep { get; set; }

        protected MilestoneItem miSoloTime { get; set; }
        protected MilestoneItem miSoloXCFlight { get; set; }
        protected MilestoneItem miSoloNight { get; set; }
        protected MilestoneItem miSoloNightTakeoffs { get; set; }
        protected MilestoneItem miSoloNightLandings { get; set; }

        protected Comm141Base() { }

        /// <summary>
        /// Determines if the flight aircraft is appropriate for the milestone
        /// </summary>
        /// <param name="ccid">The categoryclassID for the flight in question</param>
        /// <returns>true if it matches</returns>
        protected bool IsAppropriateCategoryClassForRating(CategoryClass.CatClassID ccid)
        {
            switch (RatingSought)
            {
                case RatingType.Commercial141AirplaneSingleEngine:
                    return (ccid == CategoryClass.CatClassID.ASEL || ccid == CategoryClass.CatClassID.ASES);
                case RatingType.Commercial141AirplaneMultiEngine:
                    return (ccid == CategoryClass.CatClassID.AMEL || ccid == CategoryClass.CatClassID.AMES);
                case RatingType.Commercial141Helicopter:
                    return ccid == CategoryClass.CatClassID.Helicopter;
                default:
                    return false;
            }
        }

        protected void Init(string szBaseFAR, string szTitle, RatingType ratingSought, string szCatClass, string szSoloFar, int OverallTime, string szOverallSubsection)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = ratingSought;
            string szBaseSoloFAR = "Part 141 Appendix D, 5" + szSoloFar;

            miOverallTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinTimeOverall, OverallTime),
                "Part 141 Appendix D, 4(a)" + szOverallSubsection, Resources.MilestoneProgress.Comm141OverallTimeNote, MilestoneItem.MilestoneType.Time, OverallTime);
            miTotalTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinTime, _minTotalTraining),
                ResolvedFAR(string.Empty), Resources.MilestoneProgress.Comm141TrainingNote, MilestoneItem.MilestoneType.Time, _minTotalTraining);
            miInstrumentTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinInstrumentTraining, _minInstrumentTraining),
                ResolvedFAR("(i)"), Resources.MilestoneProgress.Comm141InstrumentTrainingNote, MilestoneItem.MilestoneType.Time, _minInstrumentTraining);
            miInstrumentTrainingInCatClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinInstrumentTrainingInCatClass, _minInstrumentTrainingInCatClass, szCatClass),
                ResolvedFAR("(i)"), Resources.MilestoneProgress.Comm141InstrumentTrainingNote, MilestoneItem.MilestoneType.Time, _minInstrumentTrainingInCatClass);
            miComplexTurbineTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinComplexOrTurbine, _minComplexOrTurbineTraining, szCatClass),
                ResolvedFAR("(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, _minComplexOrTurbineTraining);
            miDayXCFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141DayXCFlight, _minTimeXCDayFlight, _minDistXCDayFlight),
                ResolvedFAR("(iii)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miNightXCFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141NightXCFlight, _minTimeXCNightFlight, _minDistXCNightFlight), ResolvedFAR("(iv)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miTestPrep = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141TestPrep, _minTestPrep, szCatClass, _minDaysTestPrep),
                ResolvedFAR("(v)"), Branding.ReBrand(Resources.MilestoneProgress.Comm141TestPrepNote), MilestoneItem.MilestoneType.Time, _minTestPrep);

            miSoloTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloTime, _minSoloInCatClass, szCatClass),
                szBaseSoloFAR, string.Empty, MilestoneItem.MilestoneType.Time, _minSoloInCatClass);
            miSoloXCFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloXCFlight, _minSoloXCDistance),
                szBaseSoloFAR + "(1) or (2)", Resources.MilestoneProgress.Comm141SoloXCHawaiiNote, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miSoloNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNight, _minSoloNightHours),
                szBaseSoloFAR + "(3)", string.Empty, MilestoneItem.MilestoneType.Time, _minSoloNightHours);
            miSoloNightTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNightTakeoffs, _minSoloNightTakeoffs),
                szBaseSoloFAR + "(3)", Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, _minSoloNightTakeoffs);
            miSoloNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNightLandings, _minSoloNightLandings),
                szBaseSoloFAR + "(3)", Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, _minSoloNightLandings);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr.fIsRealAircraft)
            {
                miOverallTraining.AddEvent(cfr.Total);
                miTotalTraining.AddEvent(cfr.Dual);
            }

            // Helicopter can be actual or simulated IMC, airplane needs to be simulated.  Bizarre
            decimal IMCTraining = Math.Min(cfr.Dual, RatingSought == RatingType.Commercial141Helicopter ? cfr.IMC + cfr.IMCSim : cfr.IMCSim);
            if (cfr.fIsCertifiedIFR)
                miInstrumentTraining.AddEvent(IMCTraining);

            // Ignore anything that remains is not in the appropriate cat/class or not in a real aircraft
            if (!cfr.fIsRealAircraft || !IsAppropriateCategoryClassForRating(cfr.idCatClassOverride))
                return;

            miInstrumentTrainingInCatClass.AddEvent(IMCTraining);
            if (cfr.fIsComplex || cfr.turbineLevel == MakeModel.TurbineLevel.Jet || cfr.turbineLevel == MakeModel.TurbineLevel.TurboProp || cfr.turbineLevel == MakeModel.TurbineLevel.UnspecifiedTurbine)
                miComplexTurbineTraining.AddEvent(cfr.Dual);

            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route);
            double distFromStart = al.MaxDistanceFromStartingAirport();
            decimal DualXC = Math.Min(cfr.XC, cfr.Dual);
            if ((DualXC - cfr.Night) >= _minTimeXCDayFlight && distFromStart >= _minDistXCDayFlight)
                miDayXCFlight.MatchFlightEvent(cfr);
            if (Math.Min(DualXC, cfr.Night) >= _minTimeXCNightFlight && distFromStart > _minDistXCNightFlight)
                miNightXCFlight.MatchFlightEvent(cfr);

            if (cfr.dtFlight.AddDays(_minDaysTestPrep).CompareTo(DateTime.Now) >= 0)
                miTestPrep.AddEvent(cfr.Dual);

            // Derive properties for solo time
            decimal soloTime = 0.0M;
            decimal instructorOnBoardTime = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;
            int nightTakeoffs = 0;
            cfr.ForEachEvent(pf =>
            {
                if (pf.PropertyType.IsSolo)
                    soloTime += pf.DecValue;
                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                    fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                    dutiesOfPICTime += pf.DecValue;
                if (pf.PropertyType.IsNightTakeOff)
                    nightTakeoffs += pf.IntValue;
            });

            if (fInstructorOnBoard)
                instructorOnBoardTime = Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            decimal effectiveSoloTime = soloTime + instructorOnBoardTime;

            if (effectiveSoloTime > 0)
            {
                miSoloTime.AddEvent(effectiveSoloTime);

                if (cfr.XC > 0 && cfr.cLandingsThisFlight >= 3)
                {
                    double distLongestSegment = al.MaxSegmentForRoute();

                    // for airplanes, solo XC must meet the distance threshold on any segment.  For helicopter, must be from start.
                    double dist = RatingSought == RatingType.Commercial141Helicopter ? distFromStart : distLongestSegment;

                    airport[] rgap = al.GetNormalizedAirports();
                    if (rgap.Length >= 3 && dist > (rgap[0].IsHawaiian ? _minSoloXCDistanceHawaii : _minSoloXCDistance))
                        miSoloXCFlight.MatchFlightEvent(cfr);
                }

                if (cfr.Night > 0 && cfr.IMC == 0)      // exclude flights with IMC because it is supposed to be VFR conditions.
                {
                    miSoloNight.AddEvent(cfr.Night);
                    miSoloNightLandings.AddEvent(cfr.cFullStopNightLandings);
                    miSoloNightTakeoffs.AddEvent(nightTakeoffs);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miOverallTraining,
                miTotalTraining,
                miInstrumentTraining,
                miInstrumentTrainingInCatClass,
                miComplexTurbineTraining,
                miDayXCFlight,
                miNightXCFlight,
                miTestPrep,
                miSoloTime,
                miSoloXCFlight,
                miSoloNight,
                miSoloNightTakeoffs,
                miSoloNightLandings
            };
            }
        }
    }

    #region Concrete 141 commercial classes
    [Serializable]
    public class Comm141SingleEngineAirplane : Comm141Base
    {
        public Comm141SingleEngineAirplane()
        {
            Init("Part 141 Appendix D, 4(b)(1)", Resources.MilestoneProgress.Title141CommercialSingleEngine, RatingType.Commercial141AirplaneSingleEngine, Resources.MilestoneProgress.ratingAirplaneSingle, "(a)", 120, "(1)");
        }
    }

    [Serializable]
    public class Comm141MultiEngineAirplane : Comm141Base
    {
        public Comm141MultiEngineAirplane()
        {
            Init("Part 141 Appendix D, 4(b)(2)", Resources.MilestoneProgress.Title141CommercialMultiEngine, RatingType.Commercial141AirplaneMultiEngine, Resources.MilestoneProgress.ratingAirplaneMulti, "(b)", 120, "(1)");
        }
    }

    [Serializable]
    public class Comm141Helicopter : Comm141Base
    {
        public Comm141Helicopter()
        {
            MinTotalTraining = 30;
            MinInstrumentTraining = 5;
            MinDistXCDayFlight = MinDistXCNightFlight = 50;
            MinSoloXCDistance = MinSoloXCDistanceHawaii = 50;


            Init("Part 141 Appendix D, 4(b)(3)", Resources.MilestoneProgress.Title141CommercialHelicopter, RatingType.Commercial141Helicopter, Resources.MilestoneProgress.ratingHelicopter, "(c)", 115, "(3)");
            miDayXCFlight.FARRef = ResolvedFAR("(ii)");
            miNightXCFlight.FARRef = ResolvedFAR("(iii)");
            miTestPrep.FARRef = ResolvedFAR("(iv)");
            miSoloXCFlight.FARRef = "Part 141 Appendix D, 5(c)(1)";
            miSoloXCFlight.Note = string.Empty;
            miSoloNight.FARRef = miSoloNightLandings.FARRef = miSoloNightTakeoffs.FARRef = "Part 141 Appendix D, 5(c)(2)";
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> coll = base.Milestones;
                coll.Remove(miComplexTurbineTraining);
                coll.Remove(miInstrumentTrainingInCatClass);
                return coll;
            }
        }

    }
    #endregion
    #endregion
    #endregion
}