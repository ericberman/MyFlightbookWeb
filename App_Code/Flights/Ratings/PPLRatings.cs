using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
{
    /// <summary>
    /// Private pilot milestones
    /// </summary>
    [Serializable]
    public class PrivatePilotMilestones : MilestoneGroup
    {
        public PrivatePilotMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupPrivatePilot;
            List<MilestoneProgress> lst = new List<MilestoneProgress>() {
                    new PPL61109A(),
                    new PPL61109B(),
                    new PPL61109C(),
                    new PPL61109D(),
                    new PPL61109E(),
                    new PPL61109F(),
                    new PPL61109G(),
                    new PPL61109H1(),
                    new PPL61109H2(),
                    new Part141AirplaneSingleEngine(),
                    new Part141AirplaneMultiEngine(),
                    new Part141Helicopter(),
                    new Part141Gyroplane(),
                    new EASAPPLAirplane(),
                    new EASAPPLHelicopter(),
                    new EASAPPLNightAirplane(),
                    };

            lst.AddRange(CAPrivatePilot.AvailableRatings);
            lst.AddRange(SAPPLBase.AvailableRatings);
            lst.AddRange(CASRLicenseBase.AvailableRatings);
            Milestones = lst.ToArray();
        }
    }

    #region 61.109 - Private Pilot
    /// <summary>
    /// 61.109AB - Requirements for Private Pilot, ASEL and AMEL
    /// </summary>
    [Serializable]
    public abstract class PPL61109Base : MilestoneProgress
    {
        protected MilestoneItem miMinTime { get; set; }
        protected MilestoneItem miMinTraining { get; set; }
        protected MilestoneItem miMinSolo { get; set; }
        protected MilestoneItem miMinDualXC { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinXCNight { get; set; }
        protected MilestoneItem miMinNightTO { get; set; }
        protected MilestoneItem miMinNightFSLandings { get; set; }
        protected MilestoneItem miMinSimIMC { get; set; }
        protected MilestoneItem miMinTestPrep { get; set; }
        protected MilestoneItem miMinXCSolo { get; set; }
        protected MilestoneItem miMinXCDistance { get; set; }
        protected MilestoneItem miMinXCLandings { get; set; }
        protected MilestoneItem miMinSoloInType { get; set; }
        protected MilestoneItem miMinSoloTakeoffsTowered { get; set; }
        protected MilestoneItem miMinSoloLandingsTowered { get; set; }

        protected const decimal SimLimit = 2.5M;

        #region local variables
        private int _MinXCSoloDistance = 150;
        private int _MinXCSoloSegment = 50;
        private int _MinXCSoloFSLandings = 3;
        private int _MinXCSoloTime = 5;
        private int _MinNightXCDistance = 100;
        private int _MinNightTakeoffs = 10;
        private int _MinNightLandings = 10;
        private int _MinTraining = 20;
        private int _MinSoloLandingsTowered = 3;
        private int _MinSoloTakeoffsTowered = 3;

        protected int MinXCSoloDistance
        {
            get { return _MinXCSoloDistance; }
            set { _MinXCSoloDistance = value; }
        }

        protected int MinXCSoloSegment
        {
            get { return _MinXCSoloSegment; }
            set { _MinXCSoloSegment = value; }
        }

        protected int MinXCSoloFSLandings
        {
            get { return _MinXCSoloFSLandings; }
            set { _MinXCSoloFSLandings = value; }
        }

        protected int MinXCSoloTime
        {
            get { return _MinXCSoloTime; }
            set { _MinXCSoloTime = value; }
        }

        protected int MinNightXCDistance
        {
            get { return _MinNightXCDistance; }
            set { _MinNightXCDistance = value; }
        }

        protected int MinNightTakeoffs
        {
            get { return _MinNightTakeoffs; }
            set { _MinNightTakeoffs = value; }
        }

        protected int MinNightLandings
        {
            get { return _MinNightLandings; }
            set { _MinNightLandings = value; }
        }

        protected int MinTraining
        {
            get { return _MinTraining; }
            set { _MinTraining = value; }
        }

        protected int MinSoloLandingsTowered
        {
            get { return _MinSoloLandingsTowered; }
            set { _MinSoloLandingsTowered = value; }
        }

        protected int MinSoloTakeoffsTowered
        {
            get { return _MinSoloTakeoffsTowered; }
            set { _MinSoloTakeoffsTowered = value; }
        }
        #endregion

        protected RatingType RatingFromCatClass(CategoryClass.CatClassID ccid)
        {
            switch (ccid)
            {
                case CategoryClass.CatClassID.AMES:
                case CategoryClass.CatClassID.AMEL:
                    return RatingType.PPLAirplaneMulti;
                case CategoryClass.CatClassID.ASES:
                case CategoryClass.CatClassID.ASEL:
                    return RatingType.PPLAirplaneSingle;
                case CategoryClass.CatClassID.Helicopter:
                    return RatingType.PPLHelicopter;
                case CategoryClass.CatClassID.GasBalloon:
                case CategoryClass.CatClassID.HotAirBalloon:
                    return RatingType.PPLBalloon;
                case CategoryClass.CatClassID.Glider:
                    return RatingType.PPLGlider;
                case CategoryClass.CatClassID.Gyroplane:
                    return RatingType.PPLGyroplane;
                case CategoryClass.CatClassID.PoweredLift:
                    return RatingType.PPLPoweredLift;
                case CategoryClass.CatClassID.Airship:
                    return RatingType.PPLAirship;
                case CategoryClass.CatClassID.PoweredParachuteLand:
                case CategoryClass.CatClassID.PoweredParachuteSea:
                    return RatingType.PPLPoweredParachute;
                case CategoryClass.CatClassID.WeightShiftControlLand:
                case CategoryClass.CatClassID.WeightShiftControlSea:
                    return RatingType.PPLWeightShift;
                default:
                    return RatingType.Unknown;
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() { 

                // 61.109(ab) - Base
                miMinTime,
                miMinTraining,
                miMinSolo,

                // 61.109(ab)(1) - Dual XC
                miMinDualXC,

                // 61.109(ab)(2) - Night
                miMinNightTime,
                miMinXCNight,
                miMinNightTO,
                miMinNightFSLandings,

                // 61.109(ab)(3) - IMC
                miMinSimIMC,

                // 61.109(ab)(4) - 3 hours of training in prior 2 calendar months
                miMinTestPrep,

                // 61.109(ab)(5) - solo and XC requirements
                miMinSoloInType,
                miMinXCSolo,
                miMinXCDistance,
                miMinSoloTakeoffsTowered,
                miMinSoloLandingsTowered
                };
            }
        }

        protected void Init(string szBaseFAR, string szTitle, RatingType ratingSought, string szAircraftRestriction, string szXCAircraftRestriction)
        {
            BaseFAR = szBaseFAR;
            RatingSought = ratingSought;
            Title = szTitle;

            // 61.109(ab) - Base
            miMinTime = new MilestoneItem(Resources.MilestoneProgress.MinTime, ResolvedFAR(String.Empty), String.Empty, MilestoneItem.MilestoneType.Time, 40.0M);
            miMinTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTraining, MinTraining), ResolvedFAR(String.Empty), String.Empty, MilestoneItem.MilestoneType.Time, MinTraining);
            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.MinSolo, ResolvedFAR(String.Empty), String.Empty, MilestoneItem.MilestoneType.Time, 10.0M);

            // 61.109(ab)(1) - Dual XC
            miMinDualXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinDualXC, szAircraftRestriction), ResolvedFAR("(1)"), String.Empty, MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.109(ab)(2) - Night
            miMinNightTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightTime, szAircraftRestriction), ResolvedFAR("(2)"), String.Empty, MilestoneItem.MilestoneType.Time, 3.0M);
            miMinXCNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCNight, szAircraftRestriction, MinNightXCDistance), ResolvedFAR("(2)(i)"), String.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinNightTO = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightTakeoffs, MinNightTakeoffs, szAircraftRestriction), ResolvedFAR("(2)(ii)"), Resources.MilestoneProgress.NoteNightRequirements, MilestoneItem.MilestoneType.Count, MinNightTakeoffs);
            miMinNightFSLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightFSLandings, MinNightLandings, szAircraftRestriction), ResolvedFAR("(2)(ii)"), Resources.MilestoneProgress.NoteNightRequirements, MilestoneItem.MilestoneType.Count, MinNightLandings);

            // 61.109(ab)(3) - IMC
            miMinSimIMC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinSimulatedIMC, szAircraftRestriction), ResolvedFAR("(3)"), Resources.MilestoneProgress.NoteIMCRequirements, MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.109(ab)(4) - 3 hours of training in prior 2 calendar months
            miMinTestPrep = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTestPrep, szAircraftRestriction), ResolvedFAR("(4)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.109(ab)(5) - XC requirements
            miMinSoloInType = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinSoloInType, szXCAircraftRestriction), ResolvedFAR("(5)"), Resources.MilestoneProgress.NoteSoloTime, MilestoneItem.MilestoneType.Time, 10.0M);
            miMinXCSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCSolo, szXCAircraftRestriction, MinXCSoloTime), ResolvedFAR("(5)(i)"), Resources.MilestoneProgress.NoteSoloTime, MilestoneItem.MilestoneType.Time, MinXCSoloTime);
            miMinXCDistance = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCDistance, szXCAircraftRestriction, MinXCSoloDistance, MinXCSoloSegment), ResolvedFAR("(5)(ii)"), Resources.MilestoneProgress.NoteXCLandings, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinSoloTakeoffsTowered = new MilestoneItem(Resources.MilestoneProgress.MinTakeoffsToweredAirport, ResolvedFAR("(5)(iii)"), string.Empty, MilestoneItem.MilestoneType.Count, MinSoloTakeoffsTowered);
            miMinSoloLandingsTowered = new MilestoneItem(Resources.MilestoneProgress.MinLandingsFSToweredAirport, ResolvedFAR("(5)(iii)"), Resources.MilestoneProgress.NoteToweredAirport, MilestoneItem.MilestoneType.Count, MinSoloLandingsTowered);
        }

        protected void Init(string szBaseFAR, string szTitle, RatingType ratingSought, string szAirplaneRestriction)
        {
            Init(szBaseFAR, szTitle, ratingSought, szAirplaneRestriction, szAirplaneRestriction);
        }

        protected PPL61109Base()
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            // Only real aircraft and FTDs need apply...
            if (!cfr.fIsRealAircraft && !cfr.fIsFTD)
                return;

            decimal soloTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsSolo) { soloTime += pf.DecValue; } });

            bool fIsCorrectCatClass = RatingFromCatClass(cfr.idCatClassOverride) == this.RatingSought;

            // 61.109(a), (b), (c) - these can have FS/FTD time applied, up to SimLimit.
            miMinTime.AddTrainingEvent(cfr.fIsFTD ? Math.Max(cfr.Total, cfr.GroundSim) : cfr.Total, SimLimit, cfr.fIsFTD);

            // per http://www.faa.gov/about/office_org/headquarters_offices/agc/practice_areas/regulations/interpretations/data/interps/2018/domingo-afx-1%20-%20(2018)%20legal%20interpretation.pdf, training must be in category and match on single vs. multi engine
            if (fIsCorrectCatClass)
                miMinTraining.AddTrainingEvent(cfr.fIsFTD ? Math.Min(cfr.GroundSim, cfr.Dual) : cfr.Dual, SimLimit, cfr.fIsFTD);

            // 61.109(ab)(3) - IMC.  This can also substitute up to simlimit
            if (fIsCorrectCatClass)
                miMinSimIMC.AddTrainingEvent(Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim), SimLimit, cfr.fIsFTD);

            // All milestones below here require a real aircraft (since they say "In an airplane" or "in a single-engine airplane")
            if (!cfr.fIsRealAircraft)
                return;

            miMinSolo.AddEvent(soloTime);

            AirportList al = null;

            if (fIsCorrectCatClass)
            {
                // Get the matching airport list
                al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                // 61.109(abc)(1)
                miMinDualXC.AddEvent(Math.Min(cfr.Dual, cfr.XC));

                // 61.109(abc)(2) - Night
                if (cfr.fNight && cfr.Dual > 0)
                {
                    miMinNightTime.AddEvent(cfr.Night);

                    if (!miMinXCNight.IsSatisfied && al.DistanceForRoute() >= MinNightXCDistance)
                        miMinXCNight.MatchFlightEvent(cfr);

                    cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsNightTakeOff) { miMinNightTO.AddEvent(pf.IntValue); } });
                    miMinNightFSLandings.AddEvent(cfr.cFullStopNightLandings);
                }

                // 61.109(ab)(4) or (c)(3) - 3 hours of training in prior 2 calendar months
                if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miMinTestPrep.AddEvent(cfr.Dual);
            }

            // 61.109(a)(5), (c)(4) - Solo XC requirements MUST BE in a single-engine aircraft
            // 61.109(b)(5) - Solo XC requirements must simply be in an airplane
            // 61.109(e)(5) - Solo time may be in powered lift OR airplane
            if (soloTime > 0 &&
                (this.RatingSought == RatingFromCatClass(cfr.idCatClassOverride) ||
                ((this.RatingSought == RatingType.PPLAirplaneMulti || this.RatingSought == RatingType.PPLPoweredLift) && CategoryClass.IsAirplane(cfr.idCatClassOverride))))
            {
                miMinSoloInType.AddEvent(soloTime);

                miMinXCSolo.AddEvent(Math.Min(cfr.XC, soloTime));

                if (al == null)
                    al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (!miMinXCDistance.IsSatisfied && al.DistanceForRoute() >= MinXCSoloDistance && al.MaxSegmentForRoute() >= MinXCSoloSegment && (cfr.cFullStopLandings + cfr.cFullStopNightLandings) >= MinXCSoloFSLandings)
                    miMinXCDistance.MatchFlightEvent(cfr);

                int cToweredTakeoffs = 0;
                int cToweredLandings = 0;
                cfr.FlightProps.ForEachEvent(pf =>
                {
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropLandingTowered || pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropLandingToweredNight)
                        cToweredLandings += pf.IntValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffTowered || pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffToweredNight)
                        cToweredTakeoffs += pf.IntValue;
                });

                // Towered landings must be to a full stop for airplane multi/single, helicopter, gyro, and powered lift
                if (this.RatingSought == RatingType.PPLAirplaneMulti || this.RatingSought == RatingType.PPLAirplaneSingle || this.RatingSought == RatingType.PPLHelicopter || this.RatingSought == RatingType.PPLGyroplane || this.RatingSought == RatingType.PPLPoweredLift)
                    cToweredLandings = Math.Min(cfr.cFullStopNightLandings + cfr.cFullStopLandings, cToweredLandings);

                miMinSoloTakeoffsTowered.AddEvent(cToweredTakeoffs);
                miMinSoloLandingsTowered.AddEvent(cToweredLandings);
            }
        }
    }

    #region Concrete PPL ratings
    /// <summary>
    /// 61.109(a) - Ratings for Airplane Single Engine
    /// </summary>
    [Serializable]
    public class PPL61109A : PPL61109Base
    {
        public PPL61109A() { Init("61.109(a)", Resources.MilestoneProgress.Title61109A, RatingType.PPLAirplaneSingle, Resources.MilestoneProgress.ratingAirplaneSingle); }
    }

    /// <summary>
    /// 61.109(b) - Ratings for Airplane Multi Engine
    /// </summary>
    [Serializable]
    public class PPL61109B : PPL61109Base
    {
        public PPL61109B() { Init("61.109(b)", Resources.MilestoneProgress.Title61109B, RatingType.PPLAirplaneMulti, Resources.MilestoneProgress.ratingAirplaneMulti, Resources.MilestoneProgress.ratingAirplane); }
    }

    /// <summary>
    /// 61.109(c) - Ratings for Helicopter 
    /// </summary>
    [Serializable]
    public class PPL61109C : PPL61109Base
    {
        public PPL61109C()
        {
            string szHelicopterRestriction = Resources.MilestoneProgress.ratingHelicopter;
            BaseFAR = "61.109(c)";
            Title = Resources.MilestoneProgress.Title61109C;
            RatingSought = RatingType.PPLHelicopter;

            MinXCSoloDistance = 100;
            MinXCSoloSegment = 25;
            MinNightXCDistance = 50;
            MinXCSoloTime = 3;
            MinTraining = 20;

            // 61.109(c) - Base
            miMinTime = new MilestoneItem(Resources.MilestoneProgress.MinTime, ResolvedFAR(""), String.Empty, MilestoneItem.MilestoneType.Time, 40.0M);
            miMinTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTraining, MinTraining), ResolvedFAR(""), String.Empty, MilestoneItem.MilestoneType.Time, MinTraining);
            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.MinSolo, ResolvedFAR(""), String.Empty, MilestoneItem.MilestoneType.Time, 10.0M);

            // 61.109(c)(1) - Dual XC
            miMinDualXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinDualXC, szHelicopterRestriction), ResolvedFAR("(1)"), String.Empty, MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.109(c)(2) - Night
            miMinNightTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightTime, szHelicopterRestriction), ResolvedFAR("(2)"), String.Empty, MilestoneItem.MilestoneType.Time, 3.0M);
            miMinXCNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCNight, szHelicopterRestriction, MinNightXCDistance), ResolvedFAR("(2)(i)"), String.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miMinNightTO = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightTakeoffs, MinNightTakeoffs, szHelicopterRestriction), ResolvedFAR("(2)(ii)"), Resources.MilestoneProgress.NoteNightRequirements, MilestoneItem.MilestoneType.Count, MinNightLandings);
            miMinNightFSLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinNightFSLandings, MinNightLandings, szHelicopterRestriction), ResolvedFAR("(2)(ii)"), Resources.MilestoneProgress.NoteNightRequirements, MilestoneItem.MilestoneType.Count, MinNightTakeoffs);

            // 61.109(c)(3) - 3 hours of training in prior 2 calendar months
            miMinTestPrep = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinTestPrep, szHelicopterRestriction), ResolvedFAR("(3)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.109(c)(4) - Solo and XC requirements
            miMinSoloInType = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinSoloInType, szHelicopterRestriction), ResolvedFAR("(4)"), Resources.MilestoneProgress.NoteSoloTime, MilestoneItem.MilestoneType.Time, 10.0M);
            miMinXCSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCSolo, szHelicopterRestriction, MinXCSoloTime), ResolvedFAR("(4)(i)"), Resources.MilestoneProgress.NoteSoloTime, MilestoneItem.MilestoneType.Time, MinXCSoloTime);
            miMinXCDistance = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCDistance, szHelicopterRestriction, MinXCSoloDistance, MinXCSoloSegment), ResolvedFAR("(4)(ii), 61.109(a)(4)(iii)"), Resources.MilestoneProgress.NoteXCLandings, MilestoneItem.MilestoneType.AchieveOnce, 1);

            miMinSoloTakeoffsTowered = new MilestoneItem(Resources.MilestoneProgress.MinTakeoffsToweredAirport, ResolvedFAR("(4)(iii)"), string.Empty, MilestoneItem.MilestoneType.Count, MinSoloTakeoffsTowered);
            miMinSoloLandingsTowered = new MilestoneItem(Resources.MilestoneProgress.MinLandingsFSToweredAirport, ResolvedFAR("(4)(iii)"), Resources.MilestoneProgress.NoteToweredAirport, MilestoneItem.MilestoneType.Count, MinSoloLandingsTowered);

            // Initialize unused milestone items.
            miMinSimIMC = new MilestoneItem(string.Empty, string.Empty, string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 0);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() { 

                // 61.109(c) - Base
                miMinTime,
                miMinTraining,
                miMinSolo,

                // 61.109(c)(1) - Dual XC
                miMinDualXC,

                // 61.109(c)(2) - Night
                miMinNightTime,
                miMinXCNight,
                miMinNightTO,
                miMinNightFSLandings,

                // 61.109(c)(3) - 3 hours of training in prior 2 calendar months
                miMinTestPrep,

                // 61.109(c)(4) - Solo & XC requirements
                miMinSoloInType,
                miMinXCSolo,
                miMinXCDistance,
                miMinSoloTakeoffsTowered,
                miMinSoloLandingsTowered
                };
            }
        }
    }

    /// <summary>
    /// 61.109(d) - Gyroplane
    /// </summary>
    [Serializable]
    public class PPL61109D : PPL61109Base
    {
        public PPL61109D()
        {
            MinNightXCDistance = 50;
            MinXCSoloDistance = 100;
            MinXCSoloSegment = 25;
            MinXCSoloTime = 3;
            Init("61.109(d)", Resources.MilestoneProgress.Title61109D, RatingType.PPLGyroplane, Resources.MilestoneProgress.ratingGyroplane);
            miMinXCSolo.Title = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinXCSolo, Resources.MilestoneProgress.ratingGyroplane, MinXCSoloTime);
            miMinSoloTakeoffsTowered.FARRef = miMinSoloLandingsTowered.FARRef = ResolvedFAR("(4)(iii)");
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> lst = new Collection<MilestoneItem>() { 

                // 61.109(d) - Base
                miMinTime,
                miMinTraining,
                miMinSolo,

                // 61.109(d)(1) - Dual XC
                miMinDualXC,

                // 61.109(d)(2) - Night
                miMinNightTime,
                miMinXCNight,
                miMinNightTO,
                miMinNightFSLandings,

                // 61.109(d)(3) - 3 hours of training in prior 2 calendar months
                miMinTestPrep,

                // 61.109(d)(4) - Solo & XC requirements
                miMinSoloInType,
                miMinXCSolo,
                miMinXCDistance,
                miMinSoloTakeoffsTowered,
                miMinSoloLandingsTowered,
                };
                return lst;
            }
        }
    }

    /// <summary>
    /// 61.109(e) - powered-lift
    /// </summary>
    [Serializable]
    public class PPL61109E : PPL61109Base
    {
        public PPL61109E()
        {
            MinNightXCDistance = 100;
            MinXCSoloDistance = 150;
            MinXCSoloSegment = 50;
            Init("61.109(e)", Resources.MilestoneProgress.Title61109E, RatingType.PPLPoweredLift, Resources.MilestoneProgress.ratingPoweredLift);
        }
    }

    /// <summary>
    /// 61.109(f) - Glider
    /// </summary>
    [Serializable]
    public class PPL61109F : PPL61109Base
    {
        private readonly MilestoneItem miHoursInHeavierThanAir, miHoursInGlider1, miHoursInGlider2, miMinFlights1i, miMinSoloFlights, miMinSoloLandings;

        public PPL61109F()
        {
            MinNightXCDistance = 100;
            MinXCSoloDistance = 150;
            MinXCSoloSegment = 50;
            BaseFAR = "61.109(f)";
            Title = Resources.MilestoneProgress.Title61109F;
            RatingSought = RatingType.PPLGlider;

            // Track hours in heavier-than-air to determine which milestones contribute.
            miHoursInHeavierThanAir = new MilestoneItem(String.Empty, String.Empty, String.Empty, MilestoneItem.MilestoneType.Time, 40.0M);

            // 61.109(f)(1) - less than 40 hours in heavier-than-air
            miHoursInGlider1 = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.GliderMinFlightTime, 10), ResolvedFAR("(1)"), String.Empty, MilestoneItem.MilestoneType.Time, 10);
            miMinFlights1i = new MilestoneItem(Resources.MilestoneProgress.GliderMinFlights, ResolvedFAR("(1)(i)"), String.Empty, MilestoneItem.MilestoneType.Count, 20);
            miMinTestPrep = new MilestoneItem(Resources.MilestoneProgress.GliderMinTestPrep, ResolvedFAR("(1)(i)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Count, 3);
            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.GliderMinSoloTime, ResolvedFAR("(1)(ii)"), Branding.ReBrand(Resources.MilestoneProgress.NoteGliderSoloLandings), MilestoneItem.MilestoneType.Time, 2);
            miMinSoloLandings = new MilestoneItem(Resources.MilestoneProgress.GliderSoloLandings, ResolvedFAR("(1)(ii)"), Branding.ReBrand(Resources.MilestoneProgress.NoteGliderSoloLandings), MilestoneItem.MilestoneType.Count, 10);

            // 61.109(f)(2) - more than 40-hours in heavier-than-air
            miHoursInGlider2 = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.GliderMinFlightTime, 3), ResolvedFAR("(2)"), String.Empty, MilestoneItem.MilestoneType.Time, 3);
            miMinSoloFlights = new MilestoneItem(Resources.MilestoneProgress.GliderSoloFlights, ResolvedFAR("(2)(i)"), Branding.ReBrand(Resources.MilestoneProgress.NoteGliderSoloLandings), MilestoneItem.MilestoneType.Count, 10);
            // miMinTestPrep - can be re-used here
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!cfr.fIsFTD && !cfr.fIsRealAircraft)
                return;

            if (CategoryClass.IsHeavierThanAir(cfr.idCatClassOverride))
                miHoursInHeavierThanAir.AddTrainingEvent(cfr.fIsFTD ? Math.Max(cfr.Total, cfr.GroundSim) : cfr.Total, SimLimit, cfr.fIsFTD);

            // Everything below this point requires a real glider.
            if (!cfr.fIsRealAircraft)
                return;

            if (RatingFromCatClass(cfr.idCatClassOverride) == RatingType.PPLGlider)
            {
                int cFlights = Math.Max(1, cfr.cLandingsThisFlight);
                miHoursInGlider1.AddEvent(cfr.Total);
                miHoursInGlider2.AddEvent(cfr.Total);
                if (cfr.Total > 0)
                    miMinFlights1i.AddEvent(cFlights);

                if (cfr.Dual > 0 && DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miMinTestPrep.AddEvent(cFlights);

                decimal soloTime = 0.0M;
                cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsSolo) { soloTime += pf.DecValue; } });

                if (soloTime > 0)
                {
                    miMinSolo.AddEvent(soloTime);
                    miMinSoloLandings.AddEvent(cFlights);
                    miMinSoloFlights.AddEvent(cFlights);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> l = new Collection<MilestoneItem>();

                if (!miHoursInHeavierThanAir.IsSatisfied)
                {
                    l.Add(miHoursInGlider1);
                    l.Add(miMinFlights1i);
                    l.Add(miMinTestPrep);
                    l.Add(miMinSolo);
                    l.Add(miMinSoloLandings);
                }
                else
                {
                    miMinTestPrep.FARRef = ResolvedFAR("(2)(ii)");

                    l.Add(miHoursInGlider2);
                    l.Add(miMinSoloFlights);
                    l.Add(miMinTestPrep);
                }

                return l;
            }
        }

    }

    /// <summary>
    /// 61.109(g) - Airship
    /// </summary>
    [Serializable]
    public class PPL61109G : PPL61109Base
    {
        private readonly MilestoneItem miMinNightXC, miMinPIC;

        public PPL61109G()
        {
            MinNightTakeoffs = MinNightLandings = 5;
            MinTraining = 25;

            Init("61.109(g)", Resources.MilestoneProgress.Title61109G, RatingType.PPLAirship, Resources.MilestoneProgress.ratingAirship);

            miMinNightXC = new MilestoneItem(Resources.MilestoneProgress.AirshipXCNightFlight, ResolvedFAR("(1)(ii)(A)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
            miMinPIC = new MilestoneItem(Resources.MilestoneProgress.AirshipDPIC, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.Time, 5.0M);

            // Fix up the FAR Refs on the remainder.
            miMinTraining.FARRef = ResolvedFAR("(1)");
            miMinDualXC.FARRef = ResolvedFAR("(1)(i)");
            miMinNightTime.FARRef = ResolvedFAR("(1)(ii)");
            miMinNightTO.FARRef = miMinNightFSLandings.FARRef = ResolvedFAR("(1)(ii)(B)");
            miMinSimIMC.FARRef = ResolvedFAR("(2)");
            miMinTestPrep.FARRef = ResolvedFAR("(3)");
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] {miMinTraining, miMinDualXC, miMinNightTime,
            miMinNightXC, miMinNightTO, miMinNightFSLandings, miMinSimIMC,
            miMinTestPrep,
            miMinPIC});
            }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            base.ExamineFlight(cfr);

            if (RatingFromCatClass(cfr.idCatClassOverride) == this.RatingSought && cfr.fIsRealAircraft)
            {
                // Get the matching airport list
                if (cfr.fNight)
                {
                    AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                    if (!miMinXCNight.IsSatisfied && al.DistanceForRoute() >= 25.0)
                        miMinNightXC.MatchFlightEvent(cfr);

                    cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC) { miMinPIC.AddEvent(Math.Max(cfr.PIC, pf.DecValue)); } });
                }
            }
        }
    }

    [Serializable]
    abstract public class PPL61109H : PPL61109Base
    {
        protected MilestoneItem miMinFlights { get; set; }
        protected MilestoneItem miAscent { get; set; }
        protected int AscentAltitude { get; set; }

        protected PPL61109H(string szRatingSuffix)
        {
            MinTraining = 10;

            Init("61.109(h)", Resources.MilestoneProgress.Title61109H + szRatingSuffix, RatingType.PPLBalloon, Resources.MilestoneProgress.ratingBalloon);
            miMinTraining.FARRef = ResolvedFAR(string.Empty);
            miMinFlights = new MilestoneItem(Resources.MilestoneProgress.BalloonMinFlights, ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Count, 6.0M);
            // miAscent needs to be initialized by a subclass
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            base.ExamineFlight(cfr);
            if (cfr.fIsRealAircraft && RatingFromCatClass(cfr.idCatClassOverride) == this.RatingSought)
                miMinFlights.AddEvent(1.0M);
        }
    }

    /// <summary>
    /// 61.109(h)(1) - Gas Balloon
    /// </summary>
    [Serializable]
    public class PPL61109H1 : PPL61109H
    {
        protected MilestoneItem miMinFlightsGas { get; set; }
        protected MilestoneItem miTestPrep { get; set; }
        protected MilestoneItem miDPIC { get; set; }

        public PPL61109H1()
            : base(Resources.MilestoneProgress.Title61109H1Suffix)
        {
            AscentAltitude = 3000;
            miMinFlightsGas = new MilestoneItem(Resources.MilestoneProgress.BalloonGasMinFlights, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Count, 2.0M);
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.BalloonGasMinFlightTestPrep, ResolvedFAR("(1)(i)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Count, 1.0M);
            miDPIC = new MilestoneItem(Resources.MilestoneProgress.BalloonGasMinFlightsDPIC, ResolvedFAR("(1)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 1.0M);
            miAscent = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.BalloonAscent, AscentAltitude), ResolvedFAR("(1)(iii)"), Resources.MilestoneProgress.NoteBalloonAscent, MilestoneItem.MilestoneType.Count, 1.0M);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            base.ExamineFlight(cfr);

            // Must be in a gas balloon AND over 2 hours each
            if (cfr.fIsRealAircraft && cfr.idCatClassOverride == CategoryClass.CatClassID.GasBalloon && cfr.Total >= 2.0M)
            {
                if (cfr.Dual >= 2.0M && cfr.Total >= 2.0M)
                {
                    miMinFlightsGas.AddEvent(1);
                    if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                        miTestPrep.AddEvent(1);
                }
                if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropDutiesOfPIC))
                    miDPIC.AddEvent(1);
                CustomFlightProperty pe1 = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropMaximumAltitude);
                if (pe1 != null && pe1.IntValue >= AscentAltitude)
                    miAscent.AddEvent(1);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miMinTraining, miMinFlights, miMinFlightsGas, miTestPrep, miDPIC, miAscent });
            }
        }
    }

    /// <summary>
    /// 61.109(h)(2) - Hot Air Balloon
    /// </summary>
    [Serializable]
    public class PPL61109H2 : PPL61109H
    {
        protected MilestoneItem miTestPrep { get; set; }
        protected MilestoneItem miSolo { get; set; }

        public PPL61109H2()
            : base(Resources.MilestoneProgress.Title61109H2Suffix)
        {
            AscentAltitude = 2000;
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.BalloonHotAirMinFlightTestPrep, ResolvedFAR("(2)(i)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Count, 2.0M);
            miSolo = new MilestoneItem(Resources.MilestoneProgress.BalloonHotAirSolo, ResolvedFAR("(2)(ii)"), string.Empty, MilestoneItem.MilestoneType.Count, 1.0M);
            miAscent = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.BalloonAscent, AscentAltitude), ResolvedFAR("(2)(iii)"), Resources.MilestoneProgress.NoteBalloonAscent, MilestoneItem.MilestoneType.Count, 1.0M);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            base.ExamineFlight(cfr);

            // MUST be in a hot-air balloon
            if (cfr.fIsRealAircraft && cfr.idCatClassOverride == CategoryClass.CatClassID.HotAirBalloon)
            {
                if (cfr.Dual >= 1.0M && cfr.Total >= 1.0M && DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miTestPrep.AddEvent(1);

                CustomFlightProperty pe = cfr.FlightProps.FindEvent(p => p.PropertyType.IsSolo);
                if (pe != null)
                    miSolo.AddEvent(1);
                pe = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropMaximumAltitude);
                if (pe != null && pe.IntValue >= AscentAltitude)
                    miAscent.AddEvent(1);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miMinTraining, miMinFlights, miTestPrep, miSolo, miAscent });
            }
        }
    }

    #region Part 141
    /// <summary>
    /// Part 141 Appendix B - http://www.law.cornell.edu/cfr/text/14/part-141/appendix-B
    /// </summary>
    [Serializable]
    public abstract class Part141Base : MilestoneProgress
    {
        protected MilestoneItem miTotalTime { get; set; }
        protected MilestoneItem miDualTime { get; set; }
        protected MilestoneItem miXCTraining { get; set; }
        protected MilestoneItem miNightFlight { get; set; }
        protected MilestoneItem miNightXC { get; set; }
        protected MilestoneItem miNightTakeoffs { get; set; }
        protected MilestoneItem miNightLandings { get; set; }
        protected MilestoneItem miInstrumentManeuvers { get; set; }
        protected MilestoneItem miTestPrep { get; set; }
        protected MilestoneItem miSoloTime { get; set; }
        protected MilestoneItem miSoloXC { get; set; }
        protected MilestoneItem miSoloTakeoffs { get; set; }
        protected MilestoneItem miSoloLandings { get; set; }

        #region Additional parameters and thresholds
        /// <summary>
        /// Multi-engine uses DPIC instead of solo
        /// </summary>
        private bool _AllowDPICInsteadOfSolo = false;

        protected bool AllowDPICInsteadOfSolo
        {
            get { return _AllowDPICInsteadOfSolo; }
            set { _AllowDPICInsteadOfSolo = value; }
        }

        /// <summary>
        /// Minimum distance for a cross-country flight
        /// </summary>
        private double _XCDistance = 100.0;

        protected double XCDistance
        {
            get { return _XCDistance; }
            set { _XCDistance = value; }
        }

        /// <summary>
        /// Minimum distance for the solo XC flight.
        /// </summary>
        private double _XCSoloDistance = 100.0;

        protected double XCSoloDistance
        {
            get { return _XCSoloDistance; }
            set { _XCSoloDistance = value; }
        }

        /// <summary>
        /// Minimum straight-line distance for the solo XC flight
        /// </summary>
        private double _XCSoloStraightLineDistance = 50.0;

        protected double XCSoloStraightLineDistance
        {
            get { return _XCSoloStraightLineDistance; }
            set { _XCSoloStraightLineDistance = value; }
        }

        /// <summary>
        /// Minimum amount of total training time
        /// </summary>
        private decimal _minTrainingTime = 35.0M;

        protected decimal minTrainingTime
        {
            get { return _minTrainingTime; }
            set { _minTrainingTime = value; }
        }
        #endregion

        protected Part141Base(RatingType rt, string szTitle, string szAircraftCategory, string szTrainingBase, string szSoloBase, double xcDistance = 100.0)
        {
            BaseFAR = "14 CFR Part 141 Appendix B ";
            RatingSought = rt;
            Title = szTitle;
            XCDistance = xcDistance;

            miTotalTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141MinTime, minTrainingTime), ResolvedFAR("(4)(a)"), string.Empty, MilestoneItem.MilestoneType.Time, minTrainingTime);
            miDualTime = new MilestoneItem(Resources.MilestoneProgress.Part141MinInstruction, ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Time, 20.0M);
            miXCTraining = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141XC, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(i)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Time, 3.0M);
            miNightFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141NightTraining, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(ii)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Time, 3.0M);
            miNightXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141NightXC, szAircraftCategory, XCDistance), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(ii)(A)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
            miNightTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141NightTakeoffs, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(ii)(B)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Count, 10.0M);
            miNightLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141NightLandings, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(ii)(B)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Count, 10.0M);

            miInstrumentManeuvers = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141InstrumentTraining, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(iii)", szTrainingBase)), string.Empty, MilestoneItem.MilestoneType.Time, 3.0M);
            miTestPrep = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141TestPrep, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(4)(b){0}(iv)", szTrainingBase)), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M);

            miSoloTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141SoloFlight, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(5){0}", szSoloBase)), string.Empty, MilestoneItem.MilestoneType.Time, 5.0M);
            miSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141SoloXC, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(5){0}(1)", szSoloBase)), Resources.MilestoneProgress.NoteXCLandings, MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
            miSoloTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141SoloTakeoffs, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(5){0}(2)", szSoloBase)), Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, 3.0M);
            miSoloLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part141SoloLandings, szAircraftCategory), ResolvedFAR(String.Format(CultureInfo.CurrentCulture, "(5){0}(2)", szSoloBase)), Branding.ReBrand(Resources.MilestoneProgress.Part141ControltowerWarning), MilestoneItem.MilestoneType.Count, 3.0M);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            // Only real aircraft and FTDs need apply...
            if (!cfr.fIsRealAircraft && !cfr.fIsFTD)
                return;

            bool fCatClassMatches = CatClassMatchesRatingSought(cfr.idCatClassOverride);

            decimal soloTime = 0.0M;
            decimal instructorOnBoardTime = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;
            decimal cToweredTakeoffs = 0.0M;
            int nightTakeoffs = 0;
            if (fCatClassMatches)
            {
                cfr.FlightProps.ForEachEvent(pf =>
                {
                    if (pf.PropertyType.IsSolo)
                        soloTime += pf.DecValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                        fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                        dutiesOfPICTime += pf.DecValue;
                    if (pf.PropertyType.IsNightTakeOff)
                        nightTakeoffs += pf.IntValue;
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffTowered || pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffToweredNight)
                        cToweredTakeoffs += pf.IntValue;
                });
            }

            if (fInstructorOnBoard && AllowDPICInsteadOfSolo)
                instructorOnBoardTime = Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            miTotalTime.AddEvent(cfr.Total);
            miDualTime.AddEvent(cfr.Dual);

            if (fCatClassMatches)
            {
                double xcDistance = 0.0;
                double xcLongestLeg = 0.0;

                if (cfr.XC > 0)
                {
                    AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                    xcDistance = al.DistanceForRoute();
                    xcLongestLeg = al.MaxSegmentForRoute();
                }

                // i)(1)
                miXCTraining.AddEvent(cfr.XC);

                // i)(2)
                if (cfr.Night > 0)
                {
                    miNightFlight.AddEvent(cfr.Night);

                    // i)(2)(i)
                    if (cfr.XC > 0 && xcDistance >= XCDistance)
                    {
                        miNightXC.AddEvent(1.0M);
                        miNightXC.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                        miNightXC.MatchingEventID = cfr.flightID;
                    }

                    // i)(2)(ii)
                    miNightTakeoffs.AddEvent(nightTakeoffs);
                    miNightLandings.AddEvent(cfr.cFullStopNightLandings);
                }

                // i)(3)
                miInstrumentManeuvers.AddEvent(cfr.IMCSim + cfr.IMC);

                // i)(4)
                if (DateTime.Now.AddDays(-60).CompareTo(cfr.dtFlight) <= 0)
                    miTestPrep.AddEvent(cfr.Dual);

                if (soloTime + instructorOnBoardTime > 0)
                {
                    // i)(5)
                    miSoloTime.AddEvent(soloTime + instructorOnBoardTime);
                    // i)(5)1)
                    if (xcDistance >= XCSoloDistance && xcLongestLeg >= XCSoloStraightLineDistance && cfr.cLandingsThisFlight >= 3)
                    {
                        miSoloXC.AddEvent(1.0M);
                        miSoloXC.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                        miSoloXC.MatchingEventID = cfr.flightID;
                    }
                    // i)(5)2)
                    miSoloTakeoffs.AddEvent(cToweredTakeoffs);
                    miSoloLandings.AddEvent(cfr.cFullStopLandings + cfr.cFullStopNightLandings);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miTotalTime,
                miDualTime,
                miXCTraining,
                miNightFlight,
                miNightXC,
                miNightTakeoffs,
                miNightLandings,
                miInstrumentManeuvers,
                miTestPrep,
                miSoloTime,
                miSoloXC,
                miSoloTakeoffs,
                miSoloLandings,
                };
            }
        }
    }

    [Serializable]
    public class Part141AirplaneSingleEngine : Part141Base
    {
        public Part141AirplaneSingleEngine()
            : base(RatingType.PPLPart141SingleEngine, Resources.MilestoneProgress.Title141PPLSEL, Resources.MilestoneProgress.ratingAirplaneSingle, "(1)", "(a)")
        {
        }
    }

    [Serializable]
    public class Part141AirplaneMultiEngine : Part141Base
    {
        public Part141AirplaneMultiEngine()
            : base(RatingType.PPLPart141MultiEngine, Resources.MilestoneProgress.Title141PPLMEL, Resources.MilestoneProgress.ratingAirplaneMulti, "(2)", "(b)")
        {
            AllowDPICInsteadOfSolo = true;
            this.GeneralDisclaimer = Resources.MilestoneProgress.Part141MultiEngineSoloDisclaimer;
        }
    }

    [Serializable]
    public class Part141Helicopter : Part141Base
    {
        public Part141Helicopter()
            : base(RatingType.PPLPart141Helicopter, Resources.MilestoneProgress.Title141PPLHelicopter, Resources.MilestoneProgress.ratingHelicopter, "(3)", "(c)", 50.0)
        {
            XCSoloStraightLineDistance = 25.0;
            miTestPrep.FARRef = ResolvedFAR("(4)(b)(3)(iii)");
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> lst = base.Milestones;
                lst.Remove(miInstrumentManeuvers);
                return lst;
            }
        }
    }

    [Serializable]
    public class Part141Gyroplane : Part141Base
    {
        public Part141Gyroplane()
            : base(RatingType.PPLPart141Gyroplane, Resources.MilestoneProgress.Title141PPLGyroplane, Resources.MilestoneProgress.ratingGyroplane, "(4)", "(c)", 50.0)
        {
            XCDistance = 50.0;
            XCSoloStraightLineDistance = 25.0;
            miTestPrep.FARRef = ResolvedFAR("(4)(b)4)(iii)");
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> lst = base.Milestones;
                lst.Remove(miInstrumentManeuvers);
                return lst;
            }
        }
    }
    #endregion
    #endregion
    #endregion

    #region Canadian Licenses - pretty much subset of JAAPrivate Pilot.  See https://www.tc.gc.ca/eng/civilaviation/regserv/cars/part4-standards-421-1086.htm#421_26

    [Serializable]
    public abstract class CAPrivatePilot : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miDual { get; set; }
        protected MilestoneItem miInstrumentDual { get; set; }
        protected MilestoneItem miXC { get; set; }
        protected MilestoneItem miSolo { get; set; }
        protected MilestoneItem miSoloXC { get; set; }
        protected MilestoneItem miSoloLongXC { get; set; }

        private const decimal CATotalTime = 45;
        private const decimal CASimSub = 5;
        private const decimal CAGroundInstr = 3;
        private const decimal CADual = 17;
        private const decimal CAMinXC = 3;
        private const decimal CAIFRDual = 5;
        private const decimal CASolo = 12;
        private const decimal CASoloXC = 5;

        protected const decimal CALongXCDistanceAirplane = 150;
        protected const decimal CALongXCDistanceHelicopter = 100;

        protected CAPrivatePilot(string szBaseFAR, string szTitle, RatingType rt, decimal XCDistance)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = rt;

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLMinTime, CATotalTime), ResolvedFAR("(a)"), Branding.ReBrand(Resources.MilestoneProgress.JARPPLMinTimeNote), MilestoneItem.MilestoneType.Time, CATotalTime);
            miDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinDual, CADual), ResolvedFAR("(b)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, CADual);
            miXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLMinXC, CAMinXC), ResolvedFAR("(b)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, CAMinXC);
            miInstrumentDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinDualInstrument, CAIFRDual), ResolvedFAR("(b)(i)"), Resources.MilestoneProgress.CAPPLGroundSimNote, MilestoneItem.MilestoneType.Time, CAIFRDual);

            miSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLSolo, CASolo), ResolvedFAR("(b)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, CASolo);
            miSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinSoloXC, CASoloXC), ResolvedFAR("(b)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, CASoloXC);
            miSoloLongXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinSoloLongXC, XCDistance), ResolvedFAR("(b)(ii)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, XCDistance);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (!fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            miTotal.AddTrainingEvent(Math.Min(cfr.Dual, cfr.fIsRealAircraft ? cfr.Total : cfr.GroundSim), CASimSub, fIsSim);

            miInstrumentDual.AddTrainingEvent(Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim), CAGroundInstr, fIsSim);

            // Everything below here must be done in a real aircraft
            if (!cfr.fIsRealAircraft)
                return;

            miDual.AddEvent(cfr.Dual);
            miXC.AddEvent(Math.Min(cfr.Dual, cfr.XC));

            // Get solo time
            decimal soloTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf =>
            {
                if (pf.PropertyType.IsSolo)
                    soloTime += pf.DecValue;
            });

            miSolo.AddEvent(soloTime);
            miSoloXC.AddEvent(Math.Min(soloTime, cfr.XC));

            if (!miSoloLongXC.IsSatisfied && soloTime > 0)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (al.DistanceForRoute() >= (double)miSoloLongXC.Threshold && al.GetAirportList().Length >= 3 && (cfr.cFullStopLandings + cfr.cFullStopNightLandings) >= 2)
                    miSoloLongXC.MatchFlightEvent(cfr);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miTotal,
                    miDual,
                    miXC,
                    miInstrumentDual,
                    miSolo,
                    miSoloXC,
                    miSoloLongXC
                };
            }
        }

        public static IEnumerable<MilestoneProgress> AvailableRatings
        {
            get
            {
                return new MilestoneProgress[]
                {
                    new CAPPLAirplaneLand(),
                    new CAPPLAirplaneSea(),
                    new CAPPLHelicopter(),
                    new CAPPLNightAirplane(),
                    new CAPPLNightHelicopter()
                };
            }
        }
    }

    [Serializable]
    public abstract class CANightRating : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miNightTime { get; set; }
        protected MilestoneItem miNightDual { get; set; }
        protected MilestoneItem miNightXC { get; set; }
        protected MilestoneItem miNightSolo { get; set; }
        protected MilestoneItem miNightSoloTakeoffs { get; set; }
        protected MilestoneItem miNightSoloLandings { get; set; }
        protected MilestoneItem miInstrument { get; set; }

        private const decimal CATotalTime = 20;
        private const decimal CANightTime = 10;
        private const decimal CANightDual = 5;
        private const decimal CANightSolo = 5;
        private const decimal CANightXCDual = 2;
        private const decimal CASimulatorTimeAllowed = 5;
        private const int CANightSoloTakeoffs = 10;
        private const int CANightSoloLandings = 10;
        private const decimal CANightInstrument = 10;

        protected CANightRating(string szBaseFAR, string szTitle, RatingType rt, string szCategory)
        {
            BaseFAR = szBaseFAR;
            Title = szTitle;
            RatingSought = rt;

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CANightTotalTime, CATotalTime, szCategory), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, CATotalTime);
            miNightTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNight, CANightTime), ResolvedFAR("(a)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, CANightTime);
            miNightDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightDual, CANightDual), ResolvedFAR("(a)(i)(A)"), string.Empty, MilestoneItem.MilestoneType.Time, CANightDual);
            miNightXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightDualXC, CANightXCDual), ResolvedFAR("(a)(i)(A)"), string.Empty, MilestoneItem.MilestoneType.Time, CANightXCDual);

            miNightSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CANightSolo, CANightSolo), ResolvedFAR("(a)(i)(B)"), string.Empty, MilestoneItem.MilestoneType.Time, CANightSolo);

            miNightSoloTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightSoloTakeoffs, CANightSoloTakeoffs), ResolvedFAR("(a)(i)(B)"), string.Empty, MilestoneItem.MilestoneType.Count, CANightSoloTakeoffs);
            miNightSoloLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightSoloLandings, CANightSoloLandings), ResolvedFAR("(a)(i)(B)"), string.Empty, MilestoneItem.MilestoneType.Count, CANightSoloLandings);

            miInstrument = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CANightInstrument, CANightInstrument), ResolvedFAR("(a)(ii)"), Resources.MilestoneProgress.CANightInstrumentNote, MilestoneItem.MilestoneType.Time, CANightInstrument);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miTotal, miNightTime, miNightDual, miNightXC, miNightSolo, miNightSoloTakeoffs, miNightSoloLandings, miInstrument
                };
            }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (!fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            // Up to 5 hours of instrument can be in a sim
            miInstrument.AddTrainingEvent(Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim), CASimulatorTimeAllowed, fIsSim);

            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(cfr.Total);

            if (cfr.Night <= 0)
                return;

            miNightTime.AddEvent(cfr.Night);
            miNightDual.AddEvent(Math.Min(cfr.Dual, cfr.Night));
            miNightXC.AddEvent(Math.Min(cfr.Dual, Math.Min(cfr.Night, cfr.XC)));

            decimal soloTime = Math.Min(cfr.Night, cfr.FlightProps.TotalTimeForPredicate(p => p.PropertyType.IsSolo));
            if (soloTime > 0)
            {
                miNightSolo.AddEvent(soloTime);
                int nightTakeoffs = 0;
                cfr.FlightProps.ForEachEvent((cfp) => { if (cfp.PropertyType.IsNightTakeOff) nightTakeoffs += cfp.IntValue; });
                miNightSoloTakeoffs.AddEvent(nightTakeoffs);
                miNightSoloLandings.AddEvent(cfr.cFullStopNightLandings);
            }
        }
    }

    #region Concrete Canadian Classes
    [Serializable]
    public abstract class CAPPLAirplane : CAPrivatePilot
    {
        private const decimal LandingsInClass = 5;

        // these vary between sea and land
        protected MilestoneItem miTimeInClass { get; set; }
        protected MilestoneItem miDualInClass { get; set; }
        protected MilestoneItem miSoloLandings { get; set; }

        protected CAPPLAirplane(string szClassBase, string szClass, string szTitle, RatingType rt, decimal minTimeInClass, decimal minDualInClass) : base("421.26(4)", szTitle, rt, CALongXCDistanceAirplane)
        {
            miTimeInClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLTimeInClass, minTimeInClass, szClass), szClassBase + "(i)", string.Empty, MilestoneItem.MilestoneType.Time, minTimeInClass);
            miDualInClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLDualInClass, minDualInClass, szClass), szClassBase + "(i)(A)", string.Empty, MilestoneItem.MilestoneType.Time, minDualInClass);
            miSoloLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAPPLSoloLandingsInClass, szClass), szClassBase + "(i)(B)", Branding.ReBrand(Resources.MilestoneProgress.CAPPLSoloNote), MilestoneItem.MilestoneType.Count, LandingsInClass);
        }

        protected abstract bool IsMatchingClass(ExaminerFlightRow cfr);

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            base.ExamineFlight(cfr);

            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!IsMatchingClass(cfr))
                return;

            miTimeInClass.AddEvent(cfr.Total);
            miDualInClass.AddEvent(cfr.Dual);
            decimal soloTime = cfr.FlightProps.TotalTimeForPredicate(p => p.PropertyType.IsSolo);
            if (soloTime > 0)
                miSoloLandings.AddEvent(cfr.cLandingsThisFlight);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> c = base.Milestones;
                c.Add(miTimeInClass);
                c.Add(miDualInClass);
                c.Add(miSoloLandings);
                return c;
            }
        }
    }

    [Serializable]
    public class CAPPLAirplaneLand : CAPPLAirplane
    {
        public CAPPLAirplaneLand() : base("421.38(2)(a)", Resources.MilestoneProgress.CAPPLClassLandPlane, Resources.MilestoneProgress.TitleCAPPLAirplaneLand, RatingType.CAPPLAirplaneLand, 3, 2) { }

        protected override bool IsMatchingClass(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return CategoryClass.IsAirplane(cfr.idCatClassOverride) && !CategoryClass.IsSeaClass(cfr.idCatClassOverride);
        }
    }

    [Serializable]
    public class CAPPLAirplaneSea : CAPPLAirplane
    {
        public CAPPLAirplaneSea() : base("421.38(1)(a)", Resources.MilestoneProgress.CAPPLClassSeaPlane, Resources.MilestoneProgress.TitleCAPPLAirplaneSea, RatingType.CAPPLAirplaneSea, 7, 5) { }

        protected override bool IsMatchingClass(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return CategoryClass.IsAirplane(cfr.idCatClassOverride) && CategoryClass.IsSeaClass(cfr.idCatClassOverride);
        }
    }

    [Serializable]
    public class CAPPLHelicopter : CAPrivatePilot
    {
        public CAPPLHelicopter() : base("421.27(4)", Resources.MilestoneProgress.TitleCAPPLHelicopter, RatingType.CAPPLHelicopter, CALongXCDistanceHelicopter)
        {

        }
    }

    [Serializable]
    public class CAPPLNightAirplane : CANightRating
    {
        public CAPPLNightAirplane() : base("421.42(1)", Resources.MilestoneProgress.TitleCANightAirplane, RatingType.CANightAirplane, Resources.MilestoneProgress.EASAClassAirplane) { }
    }

    [Serializable]
    public class CAPPLNightHelicopter : CANightRating
    {
        public CAPPLNightHelicopter() : base("421.42(2)", Resources.MilestoneProgress.TitleCANightHelicopter, RatingType.CANightHelicopter, Resources.MilestoneProgress.EASAClassHelicopter) { }
    }
    #endregion
    #endregion

    #region SA Licenses - see http://caa.mylexisnexis.co.za/# and click through to 61.03.1, 61.04.1
    [Serializable]
    public abstract class SAPPLBase : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miDual { get; set; }
        protected MilestoneItem miSolo { get; set; }
        protected MilestoneItem miSoloXC { get; set; }
        protected MilestoneItem miSoloLongXC { get; set; }

        protected decimal TotalTime { get; set; }
        protected decimal TotalDual { get; set; }
        protected decimal TotalSolo { get; set; }
        protected decimal TotalSoloXC { get; set; }
        protected decimal SoloLongXCDistance { get; set; }
        protected double SoloLongXCDistanceFromStart { get; set; }

        protected decimal MaxFTD { get; set; }

        protected void Init()
        {
            string szCategory = RatingSought == RatingType.SAPPLAirplane ? Resources.MilestoneProgress.ratingAirplane : Resources.MilestoneProgress.ratingHelicopter;

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SATotalTime, TotalTime, szCategory), ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, TotalTime);
            miDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SADual, TotalDual, szCategory), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, TotalDual);

            miSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SASolo, TotalSolo), ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Time, TotalSolo);
            miSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SASoloXC, TotalSoloXC), ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Time, TotalSoloXC);
            miSoloLongXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SASoloLongXC, SoloLongXCDistance), ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
        }

        protected SAPPLBase(string szBaseFAR, string szTitle, RatingType rt)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = rt;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (!fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            miDual.AddTrainingEvent(cfr.fIsRealAircraft ? cfr.Total : Math.Min(cfr.Dual, cfr.GroundSim), MaxFTD, fIsSim);

            // Everything below here must be done in a real aircraft
            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(cfr.Total);

            // Get solo time
            decimal soloTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf =>
            {
                if (pf.PropertyType.IsSolo)
                    soloTime += pf.DecValue;
            });

            miSolo.AddEvent(soloTime);
            miSoloXC.AddEvent(Math.Min(soloTime, cfr.XC));

            if (!miSoloLongXC.IsSatisfied && soloTime > 0)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (al.MaxDistanceFromStartingAirport() > SoloLongXCDistanceFromStart && al.DistanceForRoute() >= (double)SoloLongXCDistance && al.GetAirportList().Length >= 3 && (cfr.cFullStopLandings + cfr.cFullStopNightLandings) >= 2)
                    miSoloLongXC.MatchFlightEvent(cfr);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miTotal,
                    miDual,
                    miSolo,
                    miSoloXC,
                    miSoloLongXC
                };
            }
        }

        public static IEnumerable<MilestoneProgress> AvailableRatings
        {
            get
            {
                return new MilestoneProgress[]
              {
                new SAPPLAirplane(),
                new SAPPLHelicopter(),
                new SANightAirplane(),
                new SANightHelicopter()
              };
            }
        }
    }

    [Serializable]
    public abstract class SANightRating : MilestoneProgress
    {
        protected MilestoneItem miInstrumentDual;
        protected MilestoneItem miNightLandings;
        protected MilestoneItem miNightTakeoffs;
        protected MilestoneItem miNightDualXC;

        private const decimal MaxFTD = 5.0M;
        private readonly decimal nightXCDistance = 0;

        protected SANightRating(string szTitle, RatingType rt, decimal minNightXCDistance)
        {
            RatingSought = rt;
            Title = szTitle;
            BaseFAR = "61.10.1(2)";
            nightXCDistance = minNightXCDistance;

            miInstrumentDual = new MilestoneItem(Resources.MilestoneProgress.SANightInstrument, ResolvedFAR("(b)"), Resources.MilestoneProgress.SANightInstrumentNote, MilestoneItem.MilestoneType.Time, 10);
            miNightLandings = new MilestoneItem(Resources.MilestoneProgress.SANightLandings, ResolvedFAR("(c)/(d)"), string.Empty, MilestoneItem.MilestoneType.Count, 5);
            miNightTakeoffs = new MilestoneItem(Resources.MilestoneProgress.SANightTakeoffs, ResolvedFAR("(c)/(d)"), string.Empty, MilestoneItem.MilestoneType.Count, 5);
            miNightDualXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SADualXC, minNightXCDistance), ResolvedFAR("(e)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miInstrumentDual, miNightTakeoffs, miNightLandings, miNightDualXC }; }
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (cfr.Dual <= 0 || !fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            miInstrumentDual.AddTrainingEvent(Math.Min(cfr.IMC + cfr.IMCSim, cfr.Dual), MaxFTD, fIsSim);

            if (cfr.Night <= 0)
                return;

            int cNightTakeoffs = 0;
            cfr.FlightProps.ForEachEvent(cfp => { if (cfp.PropertyType.IsNightTakeOff) cNightTakeoffs += cfp.IntValue; });

            miNightTakeoffs.AddEvent(cNightTakeoffs);
            miNightLandings.AddEvent(cfr.cFullStopNightLandings);

            if (fIsMatch)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (al.DistanceForRoute() >= (double)nightXCDistance && al.GetAirportList().Length >= 3 && cfr.cFullStopNightLandings >= 2)
                    miNightDualXC.MatchFlightEvent(cfr);
            }
        }
    }

    #region concrete SA ratings
    [Serializable]
    public class SAPPLAirplane : SAPPLBase
    {
        public SAPPLAirplane() : base("61.03.1(2)", Resources.MilestoneProgress.TitleSAPPLAirplane, RatingType.SAPPLAirplane)
        {
            TotalTime = 45;
            TotalDual = 25;
            TotalSolo = 15;
            TotalSoloXC = 5;
            SoloLongXCDistance = 150;
            SoloLongXCDistanceFromStart = 50;
            MaxFTD = 0;
            Init();
        }
    }

    [Serializable]
    public class SAPPLHelicopter : SAPPLBase
    {
        public SAPPLHelicopter() : base("61.04.1(2)", Resources.MilestoneProgress.TitleSAPPLHelicopter, RatingType.SAPPLHelicopter)
        {
            TotalTime = 50;
            TotalDual = 25;
            TotalSolo = 15;
            TotalSoloXC = 5;
            SoloLongXCDistance = 100;
            SoloLongXCDistanceFromStart = 0;
            MaxFTD = 5;
            Init();
        }
    }

    [Serializable]
    public class SANightAirplane : SANightRating
    {
        public SANightAirplane() : base(Resources.MilestoneProgress.TitleSANightAeroplane, RatingType.SAPPLNightAirplane, 150) { }
    }

    [Serializable]
    public class SANightHelicopter : SANightRating
    {
        public SANightHelicopter() : base(Resources.MilestoneProgress.TitleSANightHelicopter, RatingType.SAPPLNightHelicopter, 75) { }
    }
    #endregion
    #endregion
}