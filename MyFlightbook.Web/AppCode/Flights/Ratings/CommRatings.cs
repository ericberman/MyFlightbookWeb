﻿using MyFlightbook.Airports;
using MyFlightbook.Currency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
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
        }

        public override Collection<MilestoneProgress> Milestones
        {
            get { return new Collection<MilestoneProgress> {
                new Comm61129ASEL(),
                new Comm61129ASES(),
                new Comm61129AMEL(),
                new Comm61129AMES(),
                new Comm61129Helicopter(),
                new Comm61129Gyroplane(),
                new Comm61129Glider(),
                new Comm61129HotAirBalloon(),
                new Comm61129GasBalloon(),
                new Comm141SingleEngineAirplaneLand(),
                new Comm141SingleEngineAirplaneSea(),
                new Comm141MultiEngineAirplaneLand(),
                new Comm141MultiEngineAirplaneSea(),
                new Comm141Helicopter(),
                new CASRCommAirplaneApprovedTraining(),
                new CASRCommAirplaneNoApprovedTraining(),
                new CASRCommHelicopterApprovedTraining(),
                new CASRCommHelicopterNoApprovedTraining(),
                new CommCanadaAirplane()
                };
            }
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
        protected static bool IsComplexOrTurbine(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            return (cfr.fIsComplex || cfr.turbineLevel == MakeModel.TurbineLevel.Jet || cfr.turbineLevel == MakeModel.TurbineLevel.UnspecifiedTurbine || cfr.turbineLevel == MakeModel.TurbineLevel.TurboProp);
        }
    }

    [Serializable]
    public abstract class Comm61129Base : CommBase
    {
        #region Thresholds and parameters
        private int _minTotalTime = 250;
        private const int _minPIC = 100;
        private int _minPICInCategory = 50;
        private const int _minPICXC = 50;
        private int _minPICXCInCategory = 10;
        private int _minSimIMC = 10;
        private decimal _minSimIMCInCategory = 5;
        private const int _minPoweredTime = 100;
        private int _minPoweredInCategory = 50;
        private const int _minSoloPIC = 10;
        private int _minDistanceXCTraining = 100;
        private const int _minSubstDualForPIC = 10;

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

        protected int MinPoweredInCategory
        {
            get { return _minPoweredInCategory; }
            set { _minPoweredInCategory = value; }
        }

        protected int MinPICXCInCategory
        {
            get { return _minPICXCInCategory; }
            set { _minPICXCInCategory = value; }
        }

        protected decimal MinSimIMCInCategory
        {
            get { return _minSimIMCInCategory; }
            set { _minSimIMCInCategory = value; }
        }

        /// <summary>
        /// By default, no sim is allowed towards total time, but can be for gyroplanes
        /// </summary>
        protected int AllowedOverallSimTime { get; set; }

        protected bool IFRTrainingCanBeInSim { get; set; }
        #endregion
        #endregion

        protected MilestoneItem miTotalTime { get; set; }
        protected MilestoneItem miMinPowered { get; set; }
        protected MilestoneItem miMinPoweredInCategory { get; set; }
        protected MilestoneItem miPICMin { get; set; }
        protected MilestoneItem miPICMinCategory { get; set; }
        protected MilestoneItemXC miPICMinXC { get; set; }
        protected MilestoneItemXC miPICMinXCCategory { get; set; }
        protected MilestoneItem miMinTraining { get; set; }
        protected MilestoneItem miMintrainingSimIMC { get; set; }
        protected MilestoneItem miMinTrainingSimIMCCategory { get; set; }
        protected MilestoneItem miMinTrainingComplex { get; set; }
        protected MilestoneItem miMinXCCategory { get; set; }
        protected MilestoneItem miMinXCCategoryNight { get; set; }
        protected MilestoneItemDecayable miMinTestPrep { get; set; }
        protected MilestoneItem miMinSoloCategory { get; set; }
        protected MilestoneItem miMinSoloXC { get; set; }
        protected MilestoneItem miMinSoloNight { get; set; }
        protected MilestoneItem miMinSoloNightTO { get; set; }
        protected MilestoneItem miMinSoloNightLandings { get; set; }
        protected MilestoneItem miMinSoloSubCategory { get; set; }
        protected MilestoneItem miMinSoloSubXC { get; set; }
        protected MilestoneItem miMinSoloSubNight { get; set; }
        protected MilestoneItem miMinSoloSubNightTO { get; set; }
        protected MilestoneItem miMinSoloSubNightLandings { get; set; }
        protected MilestoneItem miDualAsPIC { get; set; }
        protected MilestoneItem miDualAsPICXC { get; set; }
        protected bool AllowTAAForComplex { get; set; }

        protected Comm61129Base() {
            FARLink = FAA_COMM_61129_LINK;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.CommGeneralDisclaimer);
            AllowTAAForComplex = DateTime.Now.CompareTo(ExaminerFlightRow.Aug2018Cutover) > 0;
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
            miPICMinXC = new MilestoneItemXC(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPICXC, _minPICXC), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPICXC);
            miPICMinXCCategory = new MilestoneItemXC(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinPICXCInCategory, _minPICXCInCategory, szCategory), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, _minPICXCInCategory);

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
            miMinTestPrep = new MilestoneItemDecayable(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTestPrep, szCatClass), ResolvedFAR("(3)(v)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M, 2);

            miMinSoloCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloCategory, _minSoloPIC, szCatClass), ResolvedFAR("(4)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, _minSoloPIC);
            miMinSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloXCCategory, szCatClass), ResolvedFAR("(4)(i)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinSoloNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightFlight, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, 5);
            miMinSoloNightTO = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightTakeoffs, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);
            miMinSoloNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightLandings, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);

            // Solo substitutions (i.e., DPIC with Instructor on Board)
            miMinSoloSubCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloCategory, _minSoloPIC, szCatClass), ResolvedFAR("(4)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, _minSoloPIC);
            miMinSoloSubXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommPICSoloXCCategory, szCatClass), ResolvedFAR("(4)(i)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinSoloSubNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightFlight, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Time, 5);
            miMinSoloSubNightTO = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightTakeoffs, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);
            miMinSoloSubNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommMinNightLandings, szCatClass), ResolvedFAR("(4)(ii)"), Resources.MilestoneProgress.CommSoloNote, MilestoneItem.MilestoneType.Count, 10);
        }

        private void ExamineSimFlight(ExaminerFlightRow cfr, bool fCatClassMatches)
        {
            if (cfr.fIsCertifiedIFR)
            {
                if (cfr.fIsFTD || cfr.fIsFullMotion)
                    miMinPoweredInCategory.AddTrainingEvent(cfr.Total, AllowedOverallSimTime, true);
                // Helicopters and gyroplanes allow IFR training in a sim.
                if (IFRTrainingCanBeInSim && fCatClassMatches)
                    miMintrainingSimIMC.AddEvent(Math.Min(cfr.Dual, cfr.IMCSim));
            }
        }

        private bool IsComplexForRating(ExaminerFlightRow cfr, CategoryClass cc)
        {
            switch (RatingSought)
            {
                default:
                    return false;
                case RatingType.CommercialASEL:
                    // can be complex OR turbine, can be AMEL or ASEL
                    return (cc.IdCatClass == CategoryClass.CatClassID.ASEL || cc.IdCatClass == CategoryClass.CatClassID.AMEL) && ((AllowTAAForComplex && cfr.fIsTAA) || IsComplexOrTurbine(cfr));
                case RatingType.CommercialAMEL:
                    // can be complex OR turbine, MUST be multi-engine airplane.  TAA doesn't matter
                    return IsComplexOrTurbine(cfr) && (cc.IdCatClass == CategoryClass.CatClassID.AMEL || cc.IdCatClass == CategoryClass.CatClassID.AMES);
                case RatingType.CommercialASES:
                    return (cc.IdCatClass == CategoryClass.CatClassID.ASES || cc.IdCatClass == CategoryClass.CatClassID.AMES) && ((AllowTAAForComplex && cfr.fIsTAA) || IsComplexOrTurbine(cfr));
                case RatingType.CommercialAMES:
                    // Must be complex - turbine is not sufficient, MUST match catclass.
                    return cfr.fIsComplex && cc.IdCatClass == CategoryClass.CatClassID.AMES;
            }
        }

        private void CheckXCTraining(ExaminerFlightRow cfr, double distFromStart)
        {
            if (cfr.Dual > 0)
            {
                // (3)(iii)
                if ((cfr.XC - cfr.Night) >= 2.0M && distFromStart > _minDistanceXCTraining)
                    miMinXCCategory.MatchFlightEvent(cfr);

                // (3)(iv)
                if (Math.Min(cfr.XC, cfr.Night) >= 2.0M && distFromStart > _minDistanceXCTraining)
                    miMinXCCategoryNight.MatchFlightEvent(cfr);

                // (3)(v)
                miMinTestPrep.AddDecayableEvent(cfr.dtFlight, cfr.Dual, DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0);
            }
        }

        private bool MeetsLongXCCriteria(ExaminerFlightRow cfr, AirportList al, double distFromStart)
        {
            if (cfr.XC > 0 && cfr.cLandingsThisFlight >= 3 && al.GetNormalizedAirports().Length >= 3)
            {
                switch (RatingSought)
                {
                    case RatingType.CommercialGyroplane:
                        return distFromStart >= 50.0;
                    case RatingType.CommercialHelicopter:
                        return al.MaxSegmentForRoute() > 50.0 && distFromStart >= 50.0;
                    case RatingType.CommercialAMEL:
                    case RatingType.CommercialAMES:
                    case RatingType.CommercialASEL:
                    case RatingType.CommercialASES:
                        return al.DistanceForRoute() > 300 && (al.GetNormalizedAirports()[0].IsHawaiian ? al.MaxSegmentForRoute() >= 150 : distFromStart >= 250);
                    default:
                        return false;
                }
            }
            return false;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            CategoryClass cc = CategoryClass.CategoryClassFromID(cfr.idCatClassOverride);
            bool fCatClassMatches = CatClassMatchesRatingSought(cc.IdCatClass);

            if (!cfr.fIsRealAircraft)
            {
                ExamineSimFlight(cfr, fCatClassMatches);
                return;
            }

            // 61.129(a/b)
            miTotalTime.AddEvent(cfr.Total);

            // 61.129(a/b)(1)
            if (CategoryClass.IsPowered(cc.IdCatClass))
                miMinPowered.AddEvent(cfr.Total);

            bool fIsInCategory = fCatClassMatches || (IsAirplaneRating && CategoryClass.IsAirplane(cc.IdCatClass));
            if (fIsInCategory)
                miMinPoweredInCategory.AddEvent(cfr.Total);

            // 61.129(a/b)(2)
            decimal soloTime = 0.0M;
            decimal substituteSolo = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;
            int cNightLandingsAtToweredAirport = 0;
            int cNightTakefoffsAtToweredAirport = 0;
            int cNightTouchAndGo = 0;
            int nightTakeoffs = 0;
            if (fCatClassMatches)
            {
                soloTime = cfr.FlightProps.TotalTimeForPredicate(fp => fp.PropertyType.IsSolo);
                fInstructorOnBoard = cfr.FlightProps[CustomPropertyType.KnownProperties.IDPropInstructorOnBoard] != null; // instructor-on-board time only counts if you are acting as PIC
                dutiesOfPICTime = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropDutiesOfPIC);
                nightTakeoffs = cfr.FlightProps.TotalCountForPredicate(fp => fp.PropertyType.IsNightTakeOff);
                cNightLandingsAtToweredAirport = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropLandingToweredNight);
                cNightTakefoffsAtToweredAirport = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropTakeoffToweredNight);
                cNightTouchAndGo = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo);
            }

            if (fInstructorOnBoard)
                substituteSolo = Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
            double distFromStart = al.MaxDistanceFromStartingAirport();
            decimal xc = distFromStart > MinXCDistanceForRating() ? cfr.XC : 0;
            if (xc < cfr.XC)
            {
                miPICMinXC.AddIgnoredFlight(cfr);
                miPICMinXCCategory.AddIgnoredFlight(cfr);
            }

            decimal PIC = Math.Max(cfr.PIC, soloTime);
            decimal PICXC = Math.Min(PIC, xc);

            decimal PICSubstRemaining = Math.Max(miDualAsPIC.Threshold - miDualAsPIC.Progress, 0);
            decimal PICSubstXCRemaining = Math.Max(miDualAsPICXC.Threshold - miDualAsPICXC.Progress, 0);
            decimal PICSubst = Math.Min(PICSubstRemaining, substituteSolo);   // add in any duties-of-PIC time with instructor on-board 
            decimal PICSubstXC = Math.Min(PICSubstXCRemaining, Math.Min(substituteSolo, xc));

            // Reduce actual PIC time by substitute PIC time, to avoid double counting
            PIC = Math.Max(0, PIC - PICSubst);
            PICXC = Math.Max(0, PICXC - PICSubstXC);

            miDualAsPIC.AddEvent(PICSubst);
            miDualAsPICXC.AddEvent(PICSubstXC);

            miPICMin.AddEvent(PIC);
            miPICMin.AddEvent(PICSubst);
            miPICMinXC.AddEvent(PICXC);
            miPICMinXC.AddEvent(PICSubstXC);

            if (fIsInCategory)
            {
                miPICMinCategory.AddEvent(PIC);
                miPICMinCategory.AddEvent(PICSubst);
                miPICMinXCCategory.AddEvent(PICXC);
                miPICMinXCCategory.AddEvent(PICSubstXC);
            }

            // 61.129(a/b)(3)
            miMinTraining.AddEvent(cfr.Dual);

            // (3)(i)
            miMintrainingSimIMC.AddEvent(Math.Min(cfr.Dual, cfr.IMCSim));

            // (3)(ii) - complex training: complex or turbine
            bool fIsComplex = IsComplexForRating(cfr, cc);

            // Comp
            if (fIsComplex)
                miMinTrainingComplex.AddEvent(cfr.Dual);

            if (fCatClassMatches)
            {
                miMinTrainingSimIMCCategory.AddEvent(Math.Min(cfr.Dual, cfr.IMCSim));

                CheckXCTraining(cfr, distFromStart);

                // (4)
                // Solo time for section 4 is defined as EITHER solo time OR duties of PIC time with an authorized instructor on board.  We computed the latter above
                // We'll compute twice: once for actual solo, once for substitute solo (Duties of PIC with an authorized instructor on board); we can only use one OR the other, not mix-and-match
                // See https://www.faa.gov/about/office_org/headquarters_offices/agc/practice_areas/regulations/interpretations/data/interps/2016/grannis%20-%20%282016%29%20legal%20interpretation.pdf

                if (soloTime > 0 || substituteSolo > 0)
                {
                    miMinSoloCategory.AddEvent(soloTime);
                    miMinSoloSubCategory.AddEvent(substituteSolo);

                    // (4)(i) - Long solo cross-country
                    if (MeetsLongXCCriteria(cfr, al, distFromStart))
                    {
                        if (soloTime > 0)
                            miMinSoloXC.MatchFlightEvent(cfr);
                        if (substituteSolo > 0)
                            miMinSoloSubXC.MatchFlightEvent(cfr);
                    }

                    // (4)(ii)
                    if ((cfr.Night - cfr.IMC) > 0.0M)
                    {
                        if (soloTime > 0)
                        {
                            miMinSoloNight.AddEvent(Math.Min(soloTime, cfr.Night - cfr.IMC));
                            miMinSoloNightTO.AddEvent(Math.Max(nightTakeoffs, cNightTakefoffsAtToweredAirport));
                            miMinSoloNightLandings.AddEvent(Math.Max(cfr.cFullStopNightLandings + cNightTouchAndGo, cNightLandingsAtToweredAirport));
                        }
                        if (substituteSolo > 0)
                        {
                            miMinSoloSubNight.AddEvent(Math.Min(substituteSolo, cfr.Night - cfr.IMC));
                            miMinSoloSubNightTO.AddEvent(Math.Max(nightTakeoffs, cNightTakefoffsAtToweredAirport));
                            miMinSoloSubNightLandings.AddEvent(Math.Max(cfr.cFullStopNightLandings + cNightTouchAndGo, cNightLandingsAtToweredAirport));
                        }
                    }
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> l = new Collection<MilestoneItem>() {
                miTotalTime,
                miMinPowered,
                miMinPoweredInCategory,
                miPICMin,
                miPICMinCategory,
                miPICMinXC,
                miPICMinXCCategory,
                miMinTraining,
                miMintrainingSimIMC,
                miMinTrainingSimIMCCategory,
                miMinTrainingComplex,
                miMinXCCategory,
                miMinXCCategoryNight,
                miMinTestPrep
                };

                bool fSoloMet = miMinSoloCategory.IsSatisfied && miMinSoloXC.IsSatisfied && miMinSoloNight.IsSatisfied && miMinSoloNightTO.IsSatisfied && miMinSoloNightLandings.IsSatisfied;
                bool fSubSoloMet = miMinSoloSubCategory.IsSatisfied && miMinSoloSubXC.IsSatisfied && miMinSoloSubNight.IsSatisfied && miMinSoloSubNightTO.IsSatisfied && miMinSoloSubNightLandings.IsSatisfied;

                if (fSoloMet || (!fSubSoloMet && miMinSoloCategory.Progress >= miMinSoloSubCategory.Progress))
                {
                    l.Add(miMinSoloCategory);
                    l.Add(miMinSoloXC);
                    l.Add(miMinSoloNight);
                    l.Add(miMinSoloNightTO);
                    l.Add(miMinSoloNightLandings);
                }
                else
                {
                    l.Add(miMinSoloSubCategory);
                    l.Add(miMinSoloSubXC);
                    l.Add(miMinSoloSubNight);
                    l.Add(miMinSoloSubNightTO);
                    l.Add(miMinSoloSubNightLandings);
                }
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
            MinSimIMCInCategory = 5;
            MinSimIMC = 5;
            IFRTrainingCanBeInSim = true;
            MinDistanceXCTraining = 50;
            Init("61.129(c)", Resources.MilestoneProgress.Title61129C, RatingType.CommercialHelicopter, Resources.MilestoneProgress.ratingHelicopter, Resources.MilestoneProgress.ratingHelicopter);

            // fix up some FAR references:
            miMinXCCategory.FARRef = ResolvedFAR("(3)(ii)");
            miMinXCCategoryNight.FARRef = ResolvedFAR("(3)(iii)");
            miMinTestPrep.FARRef = ResolvedFAR("(3)(iv)");
            miMinSoloXC.Title = miMinSoloSubXC.Title = Resources.MilestoneProgress.CommSoloXCHelicopter;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // everything for airplanes applies to helicopters EXCEPT complex and the broad cross-country time requirement
                Collection<MilestoneItem> l = base.Milestones;
                l.Remove(miMinTrainingComplex);
                l.Remove(miPICMinXC);
                l.Remove(miMintrainingSimIMC);
                return l;
            }
        }
    }

    /// <summary>
    /// Commercial Gyroplane
    /// </summary>
    [Serializable]
    public class Comm61129Gyroplane : Comm61129Base
    {
        private readonly MilestoneItem miNightTrainingTime, miNightTrainingLandings, miNightTrainingTakeoffs;

        public Comm61129Gyroplane()
        {
            MinTotalTime = 150;
            MinPoweredInCategory = 25;
            MinPICInCategory = 10;
            MinPICXCInCategory = 3;
            MinSimIMCInCategory = 2.5M;
            IFRTrainingCanBeInSim = true;
            AllowedOverallSimTime = 5;
            MinDistanceXCTraining = 50;
            Init("61.129(d)", Resources.MilestoneProgress.Title61129DGyroplane, RatingType.CommercialGyroplane, Resources.MilestoneProgress.ratingGyroplane, Resources.MilestoneProgress.ratingGyroplane);

            // fix up some FAR references:
            miMinXCCategory.FARRef = ResolvedFAR("(3)(ii)");
            miMinXCCategoryNight.FARRef = ResolvedFAR("(3)(iii)");
            miMinTestPrep.FARRef = ResolvedFAR("(3)(iv)");
            miMinSoloXC.Title = miMinSoloSubXC.Title = Resources.MilestoneProgress.CommSoloXCGyroplane;

            miNightTrainingTime = new MilestoneItem(Resources.MilestoneProgress.CommGyroplaneNightTraining, ResolvedFAR("(3)(iii)"), string.Empty, MilestoneItem.MilestoneType.Time, 2.0M);
            miNightTrainingTakeoffs = new MilestoneItem(Resources.MilestoneProgress.CommGyroplaneNightTakeoffs, ResolvedFAR("(3)(iii)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
            miNightTrainingLandings = new MilestoneItem(Resources.MilestoneProgress.CommGyroplaneNightLandings, ResolvedFAR("(3)(iii)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            base.ExamineFlight(cfr);

            if (!cfr.fIsRealAircraft || cfr.Dual == 0.0M || !CatClassMatchesRatingSought(CategoryClass.CategoryClassFromID(cfr.idCatClassOverride).IdCatClass))
                return;

            miNightTrainingTime.AddEvent(Math.Min(cfr.Dual, cfr.Night));
            miNightTrainingTakeoffs.AddEvent(cfr.FlightProps.TotalCountForPredicate(p => p.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTakeoff));
            miNightTrainingLandings.AddEvent(cfr.cFullStopNightLandings);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // everything for airplanes applies to helicopters EXCEPT complex and the broad cross-country time requirement
                Collection<MilestoneItem> l = base.Milestones;
                l.Remove(miMinTrainingComplex);
                l.Remove(miPICMinXC);
                l.Remove(miMintrainingSimIMC);
                l.Remove(miMinXCCategoryNight);

                // Insert the night training milestones after the cross-country milestone
                int i = l.IndexOf(miMinXCCategory);
                l.Insert(++i, miNightTrainingTime);
                l.Insert(++i, miNightTrainingTakeoffs);
                l.Insert(++i, miNightTrainingLandings);
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
        private MilestoneItemDecayable miTestPrep { get; set; }
        private MilestoneItem miSoloFlightTime { get; set; }
        private MilestoneItem miSoloFlights1ii { get; set; }
        private MilestoneItem miHeavierThanAir { get; set; }
        private MilestoneItem miGliderFlightsPIC2i { get; set; }
        private MilestoneItem miSoloFlights2ii { get; set; }

        public Comm61129Glider() : base()
        {
            Title = Resources.MilestoneProgress.Title61129F;
            FARLink = FAA_COMM_61129_LINK;
            BaseFAR = "61.129(f)";
            RatingSought = RatingType.CommercialGlider;

            miGliderTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderGliderTime, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, 25);
            miGliderFlightsPIC1i = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderFlightsPIC, 100), ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Count, 100);
            miFlightTrainingTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderTrainingTime, ResolvedFAR("(1)(i) or (2)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 3);
            miTrainingFlights = new MilestoneItem(Resources.MilestoneProgress.CommGliderTrainingFlights, ResolvedFAR("(1)(i) or (2)(i)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
            miTestPrep = new MilestoneItemDecayable(Resources.MilestoneProgress.CommGliderTestPrep, ResolvedFAR("(1)(i) or (2)(i)"), Branding.ReBrand(Resources.MilestoneProgress.Comm141TestPrepNote), MilestoneItem.MilestoneType.Count, 3, 2);
            miSoloFlightTime = new MilestoneItem(Resources.MilestoneProgress.CommGliderSoloTime, ResolvedFAR("(1)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, 2);
            miSoloFlights1ii = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderSoloFlights, 10), ResolvedFAR("(1)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);

            miHeavierThanAir = new MilestoneItem(Resources.MilestoneProgress.CommGliderHeavierThanAir, ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, 200);
            miGliderFlightsPIC2i = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderFlightsPIC, 20), ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Count, 20);
            miSoloFlights2ii = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommGliderSoloFlights, 5), ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 5);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // simulators need not apply!
            if (!cfr.fIsRealAircraft)
                return;

            if (CategoryClass.IsHeavierThanAir(cfr.idCatClassOverride))
                miHeavierThanAir.AddEvent(cfr.Total);

            // Done with everything that is not a glider
            if (!CatClassMatchesRatingSought(cfr.idCatClassOverride))
                return;

            // In a glider, you can't do a touch-and-go (right?) so landings = flights.
            int cLandings = Math.Max(1, cfr.cLandingsThisFlight);

            miGliderTime.AddEvent(cfr.Total);
            if (cfr.PIC > 0)
            {
                miGliderFlightsPIC1i.AddEvent(cLandings);
                miGliderFlightsPIC2i.AddEvent(cLandings);
            }

            if (cfr.Dual > 0)
            {
                miFlightTrainingTime.AddEvent(cfr.Dual);
                miTrainingFlights.AddEvent(cLandings);
                miTestPrep.AddDecayableEvent(cfr.dtFlight, cLandings, DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0);
            }

            decimal soloTime = cfr.FlightProps.TotalTimeForPredicate(cfp => cfp.PropertyType.IsSolo);
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
        private const int _minHoursTotalInBalloons = 20;
        private const int _minFlightsInBalloons = 10;
        private const int _minFlightsAsPIC = 2;
        private const int _minFlightTrainingTime = 10;
        private const int _minFlightTrainingFlights = 10;
        private const int _minFlightDPIC = 2;
        private const int _minFlightHASolo = 2;
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
        protected MilestoneItemDecayable miTestPrep { get; set; }
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
            FARLink = FAA_COMM_61129_LINK;

            miBalloonTime = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinTime, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, _minHoursTotalInBalloons);
            miBalloonFlights = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinFlights, ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightsInBalloons);
            miBalloonPIC = new MilestoneItem(Resources.MilestoneProgress.CommBalloonMinPIC, ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightsAsPIC);
            miBalloonTrainingFlights = new MilestoneItem(Resources.MilestoneProgress.CommBalloonTrainingFlights, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightTrainingFlights);
            miBalloonTrainingTime = new MilestoneItem(Resources.MilestoneProgress.CommBalloonTrainingTime, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.Time, _minFlightTrainingTime);
            miTestPrep = new MilestoneItemDecayable(_szTestPrep, ResolvedFAR(_szFARBase + "(A)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Count, 2, 2);
            miGasDPIC = new MilestoneItem(Resources.MilestoneProgress.CommBalloonDPIC, ResolvedFAR(_szFARBase + "(B)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightDPIC);
            miHASolo = new MilestoneItem(Resources.MilestoneProgress.CommBalloonSolo, ResolvedFAR(_szFARBase + "(B)"), string.Empty, MilestoneItem.MilestoneType.Count, _minFlightHASolo);
            miControlledAscent = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CommBalloonAscent, MinAscentHeight), ResolvedFAR(_szFARBase + "(C)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
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
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (!CategoryClass.IsBalloon(cfr.idCatClassOverride))
                return;

            miBalloonTime.AddEvent(cfr.Total);
            miBalloonFlights.AddEvent(1);
            if (cfr.PIC > 0)
                miBalloonPIC.AddEvent(1);
            if (cfr.Dual > 0)
            {
                miBalloonTrainingFlights.AddEvent(1);
                miBalloonTrainingTime.AddEvent(cfr.Dual);
            }

            decimal dpic = 0.0M;
            decimal solo = 0.0M;
            decimal maxAscent = 0.0M;

            // see if DPIC
            cfr.FlightProps.ForEachEvent((pe) =>
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
                miTestPrep.AddDecayableEvent(cfr.dtFlight, 1, DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0 && cfr.Dual > 0 && cfr.Total > MinTrainingFlightLength);

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
        private const int _minInstrumentTrainingInCatClass = 5;
        private const int _minComplexOrTurbineTraining = 10;
        private const decimal _minTimeXCDayFlight = 2;
        private double _minDistXCDayFlight = 100;
        private const decimal _minTimeXCNightFlight = 2;
        private double _minDistXCNightFlight = 100;
        private const decimal _minTestPrep = 3;
        private const double _minDaysTestPrep = 60;

        // Solo thresholds
        private const int _minSoloInCatClass = 10;
        private int _minSoloXCDistance = 250;
        private int _minSoloXCDistanceHawaii = 150;
        private const int _minSoloNightHours = 5;
        private const int _minSoloNightTakeoffs = 10;
        private const int _minSoloNightLandings = 10;

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
        protected MilestoneItemDecayable miTestPrep { get; set; }

        protected MilestoneItem miSoloTime { get; set; }
        protected MilestoneItem miSoloXCFlight { get; set; }
        protected MilestoneItem miSoloNight { get; set; }
        protected MilestoneItem miSoloNightTakeoffs { get; set; }
        protected MilestoneItem miSoloNightLandings { get; set; }

        protected bool AllowTAAForComplex { get; set; }

        protected Comm141Base()
        {
            FARLink = "https://www.law.cornell.edu/cfr/text/14/appendix-D_to_part_141";
            AllowTAAForComplex = DateTime.Now.CompareTo(ExaminerFlightRow.Aug2018Cutover) > 0;
        }

        /// <summary>
        /// Determines if the flight aircraft is appropriate for the milestone
        /// </summary>
        /// <param name="ccid">The categoryclassID for the flight in question</param>
        /// <returns>true if it matches</returns>
        protected bool IsAppropriateCategoryClassForRating(CategoryClass.CatClassID ccid)
        {
            switch (RatingSought)
            {
                case RatingType.Commercial141AirplaneSingleEngineLand:
                case RatingType.Commercial141AirplaneSingleEngineSea:
                    return (ccid == CategoryClass.CatClassID.ASEL || ccid == CategoryClass.CatClassID.ASES);
                case RatingType.Commercial141AirplaneMultiEngineLand:
                case RatingType.Commercial141AirplaneMultiEngineSea:
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
            FARLink = "https://www.law.cornell.edu/cfr/text/14/appendix-D_to_part_141";
            RatingSought = ratingSought;
            string szBaseSoloFAR = "Part 141 Appendix D, 5" + szSoloFar;

            BaseFAR = string.Empty;
            miOverallTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141MinTimeOverall, OverallTime),
                ResolvedFAR("Part 141 Appendix D, 4(a)" + szOverallSubsection), Resources.MilestoneProgress.Comm141OverallTimeNote, MilestoneItem.MilestoneType.Time, OverallTime);
            BaseFAR = szBaseFAR;
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
            miTestPrep = new MilestoneItemDecayable(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141TestPrep, _minTestPrep, szCatClass, _minDaysTestPrep),
                ResolvedFAR("(v)"), Branding.ReBrand(Resources.MilestoneProgress.Comm141TestPrepNote), MilestoneItem.MilestoneType.Time, _minTestPrep, (int) _minDaysTestPrep, false);


            BaseFAR = szBaseSoloFAR;
            miSoloTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloTime, _minSoloInCatClass, szCatClass),
                ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, _minSoloInCatClass);
            miSoloXCFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloXCFlight, _minSoloXCDistance),
                ResolvedFAR("(1) or (2)"), Resources.MilestoneProgress.Comm141SoloXCHawaiiNote, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miSoloNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNight, _minSoloNightHours),
                ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.Time, _minSoloNightHours);
            miSoloNightTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNightTakeoffs, _minSoloNightTakeoffs),
                ResolvedFAR("(3)"), Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, _minSoloNightTakeoffs);
            miSoloNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Comm141SoloNightLandings, _minSoloNightLandings),
                ResolvedFAR("(3)"), Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, _minSoloNightLandings);
            
            BaseFAR = szBaseFAR;
        }

        private bool IsComplexForRating(ExaminerFlightRow cfr)
        {
            switch (RatingSought)
            {
                case RatingType.Commercial141AirplaneSingleEngineLand:
                    return cfr.fIsRealAircraft && (cfr.idCatClassOverride == CategoryClass.CatClassID.ASEL || cfr.idCatClassOverride == CategoryClass.CatClassID.AMEL) && ((AllowTAAForComplex && cfr.fIsTAA) || IsComplexOrTurbine(cfr));
                case RatingType.Commercial141AirplaneSingleEngineSea:
                    return cfr.fIsRealAircraft && (cfr.idCatClassOverride == CategoryClass.CatClassID.ASES || cfr.idCatClassOverride == CategoryClass.CatClassID.AMES) && ((AllowTAAForComplex && cfr.fIsTAA) || IsComplexOrTurbine(cfr));
                case RatingType.Commercial141AirplaneMultiEngineLand:
                case RatingType.Commercial141AirplaneMultiEngineSea:
                    return cfr.fIsRealAircraft && (cfr.idCatClassOverride == CategoryClass.CatClassID.AMEL || cfr.idCatClassOverride == CategoryClass.CatClassID.AMES) && IsComplexOrTurbine(cfr);
            }
            return false;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.fIsRealAircraft)
            {
                miOverallTraining.AddEvent(cfr.Total);
                miTotalTraining.AddEvent(cfr.Dual);
            }

            // Helicopter can be actual or simulated IMC, airplane needs to be simulated.  Bizarre
            decimal IMCTraining = Math.Min(cfr.Dual, RatingSought == RatingType.Commercial141Helicopter ? cfr.IMC + cfr.IMCSim : cfr.IMCSim);
            if (cfr.fIsCertifiedIFR)
                miInstrumentTraining.AddEvent(IMCTraining);

            if (IsComplexForRating(cfr))
                miComplexTurbineTraining.AddEvent(cfr.Dual);

            // Ignore anything that remains is not in the appropriate cat/class or not in a real aircraft
            if (!cfr.fIsRealAircraft || !IsAppropriateCategoryClassForRating(cfr.idCatClassOverride))
                return;

            miInstrumentTrainingInCatClass.AddEvent(IMCTraining);

            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
            double distFromStart = al.MaxDistanceFromStartingAirport();
            decimal xc = distFromStart > MinXCDistanceForRating() ? cfr.XC : 0;

            decimal DualXC = Math.Min(xc, cfr.Dual);
            if ((DualXC - cfr.Night) >= _minTimeXCDayFlight && distFromStart >= _minDistXCDayFlight)
                miDayXCFlight.MatchFlightEvent(cfr);
            if (Math.Min(DualXC, cfr.Night) >= _minTimeXCNightFlight && distFromStart > _minDistXCNightFlight)
                miNightXCFlight.MatchFlightEvent(cfr);

            miTestPrep.AddDecayableEvent(cfr.dtFlight, cfr.Dual, cfr.dtFlight.AddDays(_minDaysTestPrep).CompareTo(DateTime.Now) >= 0);

            // Derive properties for solo time
            decimal soloTime = cfr.FlightProps.TotalTimeForPredicate(pf => pf.PropertyType.IsSolo);
            bool fInstructorOnBoard = cfr.FlightProps[CustomPropertyType.KnownProperties.IDPropInstructorOnBoard] != null;
            decimal dutiesOfPICTime = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropDutiesOfPIC);
            decimal instructorOnBoardTime = fInstructorOnBoard ? Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0) : 0.0M;    // dual received does NOT count as duties of PIC time here
            int nightTakeoffs = cfr.FlightProps.TotalCountForPredicate(pf => pf.PropertyType.IsNightTakeOff);

            decimal effectiveSoloTime = soloTime + instructorOnBoardTime;

            if (effectiveSoloTime > 0)
            {
                miSoloTime.AddEvent(effectiveSoloTime);

                MatchCrossCountry(cfr, al, distFromStart);

                MatchNightVFR(cfr, nightTakeoffs);
            }
        }

        private void MatchCrossCountry(ExaminerFlightRow cfr, AirportList al, double distFromStart)
        {
            if (cfr.XC > 0 && cfr.cLandingsThisFlight >= 3)
            {
                double distLongestSegment = al.MaxSegmentForRoute();

                // for airplanes, solo XC must meet the distance threshold on any segment.  For helicopter, must be from start.
                double dist = RatingSought == RatingType.Commercial141Helicopter ? distFromStart : distLongestSegment;

                airport[] rgap = al.GetNormalizedAirports();
                if (rgap.Length >= 3 && dist > (rgap[0].IsHawaiian ? _minSoloXCDistanceHawaii : _minSoloXCDistance))
                    miSoloXCFlight.MatchFlightEvent(cfr);
            }
        }

        private void MatchNightVFR(ExaminerFlightRow cfr, int nightTakeoffs)
        {
            if (cfr.Night > 0 && cfr.IMC == 0)      // exclude flights with IMC because it is supposed to be VFR conditions.
            {
                miSoloNight.AddEvent(cfr.Night);
                // Part 141 - interestingly - does NOT 
                miSoloNightLandings.AddEvent(cfr.cFullStopNightLandings + cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo));
                miSoloNightTakeoffs.AddEvent(nightTakeoffs);
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
    public class Comm141SingleEngineAirplaneLand : Comm141Base
    {
        public Comm141SingleEngineAirplaneLand()
        {
            Init("Part 141 Appendix D, 4(b)(1)", Resources.MilestoneProgress.Title141CommercialSingleEngineLand, RatingType.Commercial141AirplaneSingleEngineLand, Resources.MilestoneProgress.ratingAirplaneSingle, "(a)", 120, "(1)");
        }
    }

    [Serializable]
    public class Comm141SingleEngineAirplaneSea : Comm141Base
    {
        public Comm141SingleEngineAirplaneSea()
        {
            Init("Part 141 Appendix D, 4(b)(1)", Resources.MilestoneProgress.Title141CommercialSingleEngineSea, RatingType.Commercial141AirplaneSingleEngineSea, Resources.MilestoneProgress.ratingAirplaneSingle, "(a)", 120, "(1)");
        }
    }

    [Serializable]
    public class Comm141MultiEngineAirplaneLand : Comm141Base
    {
        public Comm141MultiEngineAirplaneLand()
        {
            Init("Part 141 Appendix D, 4(b)(2)", Resources.MilestoneProgress.Title141CommercialMultiEngineLand, RatingType.Commercial141AirplaneMultiEngineLand, Resources.MilestoneProgress.ratingAirplaneMulti, "(b)", 120, "(1)");
        }
    }

    [Serializable]
    public class Comm141MultiEngineAirplaneSea : Comm141Base
    {
        public Comm141MultiEngineAirplaneSea()
        {
            Init("Part 141 Appendix D, 4(b)(2)", Resources.MilestoneProgress.Title141CommercialMultiEngineSea, RatingType.Commercial141AirplaneMultiEngineSea, Resources.MilestoneProgress.ratingAirplaneMulti, "(b)", 120, "(1)");
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
            BaseFAR = "Part 141 Appendix D, 5(c)";
            miSoloXCFlight.FARRef = ResolvedFAR("(1)");
            miSoloXCFlight.Note = string.Empty;
            miSoloNight.FARRef = miSoloNightLandings.FARRef = miSoloNightTakeoffs.FARRef = ResolvedFAR("(2)");
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

    #region Canada Commercial
    [Serializable]
    public class CommCanadaAirplane : MilestoneProgress
    {
        bool HasSeenPPL { get; set; }

        protected const int MaxSim = 10;

        #region Milestoneitems
        protected MilestoneItem miMinHours { get; set; }
        protected MilestoneItem miMinPIC { get; set; }
        protected MilestoneItem miMinXCPIC { get; set; }
        protected MilestoneItem miMinDual { get; set; }
        protected MilestoneItem miMinNight { get; set; }
        protected MilestoneItem miMinNightXC { get; set; }
        protected MilestoneItem miMinXC { get; set; }
        protected MilestoneItem miMinInstrument { get; set; }
        protected MilestoneItem miMinSolo { get; set; }
        protected MilestoneItem miMinSoloXC { get; set; }
        protected MilestoneItem miMinSoloNight { get; set; }
        protected MilestoneItem miMinSoloNightTakeoffs { get; set; }
        protected MilestoneItem miMinSoloNightLandings { get; set; }
        #endregion

        public CommCanadaAirplane() : base()
        {
            BaseFAR = "421.30(4)(a)";
            FARLink = "https://tc.canada.ca/en/corporate-services/acts-regulations/list-regulations/canadian-aviation-regulations-sor-96-433/standards/standard-421-flight-crew-permits-licences-ratings-canadian-aviation-regulations-cars#421_30";
            RatingSought = RatingType.CommercialCanadaAeroplane;
            Title = Resources.MilestoneProgress.Title42130Aeroplane;

            HasSeenPPL = false;

            miMinHours = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinHours, ResolvedFAR("(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 200);
            miMinPIC = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinPIC, ResolvedFAR("(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 100);
            miMinXCPIC = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinPICXC, ResolvedFAR("(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 20);

            miMinDual = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinDual, ResolvedFAR("(ii)(A)"), string.Empty, MilestoneItem.MilestoneType.Time, 35);
            miMinNight = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinNightDual, ResolvedFAR("(ii)(A)(I)"), string.Empty, MilestoneItem.MilestoneType.Time, 5);
            miMinNightXC = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinNightDualXC, ResolvedFAR("(ii)(A)(I)"), string.Empty, MilestoneItem.MilestoneType.Time, 2);
            miMinXC = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinXCDual, ResolvedFAR("(ii)(A)(II)"), string.Empty, MilestoneItem.MilestoneType.Time, 5);
            miMinInstrument = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinIFRDual, ResolvedFAR("(ii)(A)(III)"), Resources.MilestoneProgress.CommCanadaMinIFRNote, MilestoneItem.MilestoneType.Time, 20);

            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.CommCanadaMinSolo, ResolvedFAR("(ii)(B)"), Resources.MilestoneProgress.CommCanadaSoloNote, MilestoneItem.MilestoneType.Time, 30);
            miMinSoloXC = new MilestoneItem(Resources.MilestoneProgress.CommCanadaSoloXC, ResolvedFAR("(ii)(B)(I)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinSoloNight = new MilestoneItem(Resources.MilestoneProgress.CommCanadaSoloNight, ResolvedFAR("(ii)(B)(II)"), string.Empty, MilestoneItem.MilestoneType.Time, 5);
            miMinSoloNightTakeoffs = new MilestoneItem(Resources.MilestoneProgress.CommCanadaSoloNightTakeoffs, ResolvedFAR("(ii)(B)(II)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
            miMinSoloNightLandings = new MilestoneItem(Resources.MilestoneProgress.CommCanadaSoloNightLandings, ResolvedFAR("(ii)(B)(II)"), string.Empty, MilestoneItem.MilestoneType.Count, 10);
        }

        public override Collection<MilestoneItem> Milestones => new Collection<MilestoneItem>() { miMinHours, miMinPIC, miMinXCPIC, miMinDual, miMinNight, miMinNightXC, miMinXC, miMinInstrument, miMinSolo, miMinSoloXC, miMinSoloNight, miMinSoloNightTakeoffs, miMinSoloNightLandings};

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!CategoryClass.IsAirplane(cfr.idCatClassOverride))
                return;

            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropCheckridePPL))
                HasSeenPPL = true;

            // Issue #1309 - assume all sim time is training time in Canada.
            if (!HasSeenPPL && cfr.fIsCertifiedIFR)
                miMinInstrument.AddTrainingEvent(!cfr.fIsRealAircraft ? cfr.IMCSim : Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim), MaxSim, !cfr.fIsRealAircraft);

            if (!cfr.fIsRealAircraft)
                return;

            // From here on, we can assume a real aircraft and an airplane
            miMinHours.AddEvent(cfr.Total);
            miMinPIC.AddEvent(cfr.PIC);
            miMinXCPIC.AddEvent(Math.Min(cfr.PIC, cfr.XC));

            // All of the remaining items MUST be post-PPL.  We are looking at flights in reverse-chronological order, so once we see the PPL in an airplane, we stop
            if (HasSeenPPL)
                return;

            miMinDual.AddEvent(cfr.Dual);
            miMinNight.AddEvent(Math.Min(cfr.Dual, cfr.Night));
            miMinNightXC.AddEvent(Math.Min(cfr.Dual, Math.Min(cfr.Night, cfr.XC)));
            miMinXC.AddEvent(Math.Min(cfr.Dual, cfr.XC));

            // Check for solo time; all remaining checks only apply to solo
            decimal solo = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropSolo);
            if (solo == 0)
                return;

            miMinSolo.AddEvent(solo);
            miMinSoloNight.AddEvent(Math.Min(solo, cfr.Night));
            miMinSoloNightTakeoffs.AddEvent(cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTakeoff));
            miMinSoloNightLandings.AddEvent(cfr.cFullStopNightLandings + cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo));

            if (cfr.XC > 0 && cfr.cLandingsThisFlight >= 3)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                if (al.MaxDistanceFromStartingAirport() > 300)
                    miMinSoloXC.MatchFlightEvent(cfr);
            }
        }
    }
    #endregion

    #endregion
}