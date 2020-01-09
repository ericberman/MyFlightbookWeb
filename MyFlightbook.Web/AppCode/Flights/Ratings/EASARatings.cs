using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System;
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
    /// LAPL milestones
    /// </summary>
    [Serializable]
    public class LAPLMilestones : MilestoneGroup
    {
        public LAPLMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupLAPL;
            Milestones = new MilestoneProgress[] {
                new EASALAPLAirplane(),
                new EASALAPLHelicopter(),
                new EASALAPLSailplane() };
        }
    }

    #region EASA (JAA/JAR) Licensing - see http://www.jaa.nl/publications/section1.html for documents, specifically JAR-FCL 1 (aeroplane) and JAR-FCL 2 (Helicopter); now see https://www.easa.europa.eu/system/files/dfu/Part-FCL.pdf; now see http://skyrise.aero/wp-content/uploads/2016/07/Licensing-requirements-quick-reference-rev5-01-07-2015_publication_V04.pdf
    [Serializable]
    public abstract class EASAPrivatePilot : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miDual { get; set; }
        protected MilestoneItem miInstrumentDual { get; set; }
        protected MilestoneItem miSolo { get; set; }
        protected MilestoneItem miSoloXC { get; set; }
        protected MilestoneItem miSoloLongXC { get; set; }

        private const decimal JAATotalTime = 45;
        private const decimal JAASimSub = 5;
        private const decimal JAADual = 25;
        private const decimal JAAIFRDual = 5;
        private const decimal JAASolo = 10;
        private const decimal JAASoloXC = 5;
        protected const decimal JAALongXCDistanceAirplane = 150;
        protected const decimal JAALongXCDistanceHelicopter = 100;

        protected EASAPrivatePilot(string szBaseFAR, string szTitle, RatingType rt, string szExperienceSection, string szTrainingSection, decimal XCDistance)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = rt;

            string szExperience = ResolvedFAR(szExperienceSection);
            string szTraining = ResolvedFAR(szTrainingSection);

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinTime, JAATotalTime), szExperience, Branding.ReBrand(Resources.MilestoneProgress.JARPPLMinTimeNote), MilestoneItem.MilestoneType.Time, JAATotalTime);
            miDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinDual, JAADual), szTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAADual);
            miInstrumentDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinDualInstrument, JAAIFRDual), szTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAAIFRDual);
            miSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinSolo, JAASolo), szTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAASolo);
            miSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinSoloXC, JAASoloXC), szTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAASoloXC);
            miSoloLongXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinSoloLongXC, XCDistance), szTraining, string.Empty, MilestoneItem.MilestoneType.AchieveOnce, XCDistance);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (!fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            miTotal.AddTrainingEvent(fIsSim ? cfr.GroundSim : cfr.Total, JAASimSub, fIsSim);

            // Everything below here must be done in a real aircraft
            if (!cfr.fIsRealAircraft)
                return;

            miDual.AddEvent(cfr.Dual);
            miInstrumentDual.AddEvent(Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim));

            // Get solo time
            decimal soloTime = 0.0M;
            decimal instructorOnBoardTime = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf =>
            {
                if (pf.PropertyType.IsSolo)
                    soloTime += pf.DecValue;
                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                    fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                    dutiesOfPICTime += pf.DecValue;
            });

            if (fInstructorOnBoard)
                instructorOnBoardTime = Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            decimal supervisedSolo = soloTime + Math.Min(instructorOnBoardTime, dutiesOfPICTime);
            miSolo.AddEvent(supervisedSolo);
            miSoloXC.AddEvent(Math.Min(supervisedSolo, cfr.XC));

            AirportList al = null;

            if (!miSoloLongXC.IsSatisfied && supervisedSolo > 0)
            {
                al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (al.DistanceForRoute() >= (double)miSoloLongXC.Threshold && al.GetAirportList().Length >= 3 && (cfr.cFullStopLandings + cfr.cFullStopNightLandings) >= 2)
                    miSoloLongXC.MatchFlightEvent(cfr);
            }
        }
    }

    [Serializable]
    public class EASAPPLNightBase : MilestoneProgress
    {
        protected MilestoneItem miNightTime { get; set; }
        protected MilestoneItem miNightDual { get; set; }
        protected MilestoneItem miNightXC { get; set; }
        protected MilestoneItem miNightLongXC { get; set; }
        protected MilestoneItem miNightSoloTakeoffs { get; set; }
        protected MilestoneItem miNightSoloLandings { get; set; }

        protected const decimal JAALongNightXCDistanceAirplane = 27;
        private const decimal JAANightTime = 5;
        private const decimal JAANightDual = 3;
        private const decimal JAANightXC = 1;
        private const decimal JAANightSoloTakeoffs = 5;
        private const decimal JAANightSoloLandings = 5;

        public EASAPPLNightBase(string szTitle, RatingType rt) : base()
        {
            Title = szTitle;
            RatingSought = rt;
            string szNightTraining = ResolvedFAR("FCL.810(a)");

            miNightTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNight, JAANightTime), szNightTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAANightTime);
            miNightDual = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightDual, JAANightDual), szNightTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAANightDual);
            miNightXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightDualXC, JAANightXC), szNightTraining, string.Empty, MilestoneItem.MilestoneType.Time, JAANightXC);
            miNightLongXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLMinDualNightLongXC, JAALongNightXCDistanceAirplane), szNightTraining, string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
            miNightSoloTakeoffs = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightSoloTakeoffs, JAANightSoloTakeoffs), szNightTraining, string.Empty, MilestoneItem.MilestoneType.Count, JAANightSoloTakeoffs);
            miNightSoloLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.JARPPLNightSoloLandings, JAANightSoloLandings), szNightTraining, string.Empty, MilestoneItem.MilestoneType.Count, JAANightSoloLandings);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsMatch = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            bool fIsSim = cfr.fIsCertifiedIFR && !cfr.fIsRealAircraft;

            if (!fIsMatch || !cfr.fIsCertifiedIFR)
                return;

            // Night - optional
            if (cfr.Night > 0)
            {
                miNightTime.AddEvent(cfr.Night);
                decimal nightDual = Math.Min(cfr.Night, cfr.Dual);
                miNightDual.AddEvent(nightDual);
                miNightXC.AddEvent(Math.Min(nightDual, cfr.XC));

                decimal soloTime = cfr.FlightProps.TotalTimeForPredicate(cfp => cfp.PropertyType.IsSolo);
                decimal nightTakeoffs = cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropertyType.IsNightTakeOff);

                if (soloTime > 0)
                {
                    miNightSoloTakeoffs.AddEvent(nightTakeoffs);
                    miNightSoloLandings.AddEvent(cfr.cFullStopNightLandings);
                }

                if (nightDual > 0)
                {
                    AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                    if (al == null)
                        al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                    if (al.DistanceForRoute() > (double)JAALongNightXCDistanceAirplane)
                        miNightLongXC.MatchFlightEvent(cfr);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miNightTime,
                miNightDual,
                miNightXC,
                miNightLongXC,
                miNightSoloTakeoffs,
                miNightSoloLandings
                };
            }
        }
    }
    #region Concrete EASA Night Ratings
    [Serializable]
    public class EASAPPLNightAirplane : EASAPPLNightBase
    {
        public EASAPPLNightAirplane() : base(Resources.MilestoneProgress.TitleEASAPPLNightAirplane, RatingType.PPLEASANightAirplane) { }
    }
    #endregion

    #region Concrete EASA Classes
    [Serializable]
    public class EASAPPLAirplane : EASAPrivatePilot
    {
        public EASAPPLAirplane()
            : base("EASA PPL(A) - ", Resources.MilestoneProgress.TitleEASAAirplanePPL, RatingType.PPLEASAAirplane, "FCL.210.A(a)", "FCL.210.A(a)", JAALongXCDistanceAirplane)
        {
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> l = new Collection<MilestoneItem>() {
                miTotal,
                miDual,
                miSolo,
                miSoloXC,
                miSoloLongXC
                };
                return l;
            }
        }
    }

    [Serializable]
    public class EASAPPLHelicopter : EASAPrivatePilot
    {
        public EASAPPLHelicopter()
            : base("EASA PPL(H) - ", Resources.MilestoneProgress.TitleEASAHelicopterPPL, RatingType.PPLEASAHelicopter, "FCL.210.H(a)", "FCL.210.H(a)", JAALongXCDistanceHelicopter)
        {
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.JARPPLHelicopterDisclaimer);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miTotal, miDual, miSolo, miSoloXC, miSoloLongXC }; }
        }
    }
    #endregion
    #endregion

    #region EASA LAPL Ratings
    // Summary of all EASA ratings are at https://www.eurocockpit.be/sites/default/files/licensing_requirements_-_quick_reference-rev2-may-2015_publication.pdf.
    // Note that this overlaps with the JAA licensing above too.
    // https://www.easa.europa.eu/system/files/dfu/Part-FCL.pdf has more details.
    // https://www.caa.co.uk/General-aviation/Pilot-licences/EASA-requirements/Light-aircraft-pilot-licence-%28LAPL%29/ is another good reference.

    /// <summary>
    /// See https://www.easa.europa.eu/system/files/dfu/Part-FCL.pdf
    /// </summary>
    [Serializable]
    public abstract class EASALAPL : MilestoneProgress
    {
        protected string AircraftClass { get; set; }
        protected CategoryClass.CatClassID CatClass { get; set; }
        protected string DualClassRestriction { get; set; }
        protected decimal MinTime { get; set; }
        protected decimal MinDualInClass { get; set; }
        protected decimal MinSolo { get; set; }
        protected decimal MinSoloXC { get; set; }
        protected int MinSoloDistance { get; set; }

        protected MilestoneItem miMinTime { get; set; }
        protected MilestoneItem miMinDualInClass { get; set; }
        protected MilestoneItem miMinSolo { get; set; }
        protected MilestoneItem miMinSoloXC { get; set; }
        protected MilestoneItem miMinSoloXCMinDist { get; set; }

        protected void Init()
        {
            miMinTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.EASALAPLAirplaneMinDual, MinTime, DualClassRestriction), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, MinTime);
            miMinDualInClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.EASALAPLAirplaneMinDualInClass, MinDualInClass, AircraftClass), ResolvedFAR("(a)(1)"), string.Empty, MilestoneItem.MilestoneType.Time, MinDualInClass);
            miMinSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.EASALAPLAirplaneMinSolo, MinSolo), ResolvedFAR("(a)(2)"), Branding.ReBrand(Resources.MilestoneProgress.EASASupervisedSoloNote), MilestoneItem.MilestoneType.Time, MinSolo);
            miMinSoloXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.EASALAPLAirplaneMinSoloXC, MinSoloXC), ResolvedFAR("(a)(2)"), Branding.ReBrand(Resources.MilestoneProgress.EASASupervisedSoloNote), MilestoneItem.MilestoneType.Time, MinSoloXC);
            miMinSoloXCMinDist = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.EASALAPLAirplaneMinSoloXCMinDist, MinSoloDistance), ResolvedFAR("(a)(2)"), Branding.ReBrand(Resources.MilestoneProgress.EASASupervisedSoloNote), MilestoneItem.MilestoneType.AchieveOnce, 1);
        }

        protected EASALAPL(RatingType rt, string szBaseFar, string szTitle) : base()
        {
            RatingSought = rt;
            BaseFAR = szBaseFar;
            Title = szTitle;
        }

        public abstract bool MatchesSoloOrTotalDual(ExaminerFlightRow cfr);
        public abstract bool MatchesClassDual(ExaminerFlightRow cfr);

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // no provision for training devices.
            if (!cfr.fIsRealAircraft)
                return;

            if (MatchesClassDual(cfr))
                miMinDualInClass.AddEvent(cfr.Dual);

            if (MatchesSoloOrTotalDual(cfr))
            {
                miMinTime.AddEvent(cfr.Total);

                decimal soloTime = 0.0M;
                bool fInstructorOnBoard = false;
                decimal dutiesOfPICTime = 0.0M;
                cfr.FlightProps.ForEachEvent(pf => {
                    if (pf.PropertyType.IsSolo)
                        soloTime += pf.DecValue;

                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                        fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                    if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                        dutiesOfPICTime += pf.DecValue;
                });

                if (fInstructorOnBoard)
                    soloTime += Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

                if (soloTime > 0.0M)
                {
                    miMinSolo.AddEvent(soloTime);
                    miMinSoloXC.AddEvent(Math.Min(soloTime, cfr.XC));

                    if (cfr.cFullStopLandings >= 1 && !miMinSoloXCMinDist.IsSatisfied)
                    {
                        AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                        if (al.GetAirportList().Length > 1 && al.DistanceForRoute() >= MinSoloDistance)
                            miMinSoloXCMinDist.MatchFlightEvent(cfr);
                    }
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miMinTime, miMinDualInClass, miMinSolo, miMinSoloXC, miMinSoloXCMinDist });
            }
        }
    }

    #region Concrete LAPL ratings
    [Serializable]
    public class EASALAPLAirplane : EASALAPL
    {
        public EASALAPLAirplane() : base(RatingType.EASALAPLAirplane, "FCL.110.A", Resources.MilestoneProgress.TitleEASALAPLAirplane)
        {
            AircraftClass = Resources.MilestoneProgress.EASAClassAirplane;
            CatClass = CategoryClass.CatClassID.ASEL;
            DualClassRestriction = Resources.MilestoneProgress.EASALAPLAirplaneOrTMG;
            MinTime = 30.0M;
            MinDualInClass = 15.0M;
            MinSolo = 6.0M;
            MinSoloXC = 3.0M;
            MinSoloDistance = 80;

            Init();

            miMinTime.Note = Branding.ReBrand(Resources.MilestoneProgress.EASAClassAirplaneNote);
        }

        public override bool MatchesClassDual(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return cfr.idCatClassOverride == CategoryClass.CatClassID.ASEL;
        }

        public override bool MatchesSoloOrTotalDual(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            bool fAirplane = cfr.idCatClassOverride == CategoryClass.CatClassID.ASEL;
            bool fTMG = cfr.idCatClassOverride == CategoryClass.CatClassID.Glider && cfr.fMotorGlider;
            return fAirplane || fTMG;
        }
    }

    [Serializable]
    public class EASALAPLHelicopter : EASALAPL
    {
        public EASALAPLHelicopter() : base(RatingType.EASALAPLHelicopter, "FCL.110.H", Resources.MilestoneProgress.TitleEASALAPLHelicopter)
        {
            AircraftClass = Resources.MilestoneProgress.EASAClassHelicopter;
            CatClass = CategoryClass.CatClassID.Helicopter;
            DualClassRestriction = Resources.MilestoneProgress.EASAClassHelicopter;
            MinTime = 40.0M;
            MinDualInClass = 20.0M;
            MinSolo = 10.0M;
            MinSoloXC = 5.0M;
            MinSoloDistance = 80;

            Init();
            miMinTime.Note = Branding.ReBrand(Resources.MilestoneProgress.EASAClassHelicopterNote);
        }

        public override bool MatchesClassDual(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter;
        }

        public override bool MatchesSoloOrTotalDual(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            return cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter;
        }
    }

    [Serializable]
    public class EASALAPLSailplane : MilestoneProgress
    {
        private MilestoneItem miTotal { get; set; }
        private MilestoneItem miDual { get; set; }
        private MilestoneItem miSolo { get; set; }
        private MilestoneItem miLandings { get; set; }
        private MilestoneItem miCrossCountry { get; set; }
        const decimal maxTMGTime = 7.0M;
        const double minSoloXC = 27.0;
        const double minDualXC = 55.0;

        public EASALAPLSailplane() : base()
        {
            BaseFAR = "FCL.110.S";
            Title = Resources.MilestoneProgress.EASALAPLSailplaneTitle;
            RatingSought = RatingType.EASALAPLSailplane;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.EASALAPLSailplaneTMGNote);

            miTotal = new MilestoneItem(Resources.MilestoneProgress.EASALAPLSailplaneTotal, ResolvedFAR(string.Empty), Resources.MilestoneProgress.EASALAPLSailplaneNoteTMG, MilestoneItem.MilestoneType.Time, 15.0M);
            miDual = new MilestoneItem(Resources.MilestoneProgress.EASALAPLSailplaneDual, ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, 10.0M);
            miSolo = new MilestoneItem(Resources.MilestoneProgress.EASALAPLSailplaneSolo, ResolvedFAR("(2)"), string.Empty, MilestoneItem.MilestoneType.Time, 2.0M);
            miLandings = new MilestoneItem(Resources.MilestoneProgress.EASALAPLSailplaneLaunchesAndLandings, ResolvedFAR("(3)"), Resources.MilestoneProgress.EASALAPLSailplaneLaunchesNotes, MilestoneItem.MilestoneType.Count, 45);
            miCrossCountry = new MilestoneItem(Resources.MilestoneProgress.EASALAPLSailplaneXC, ResolvedFAR("(4)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // no provision for training devices.
            if (!cfr.fIsRealAircraft)
                return;

            // Must be a glider; motorgliders ("powered sailplanes") are subsets of gliders
            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Glider)
                return;

            // Treat TMG time as if it were a training device.
            miTotal.AddTrainingEvent(cfr.Total, maxTMGTime, cfr.fMotorGlider);

            miDual.AddEvent(cfr.Dual);

            decimal soloTime = 0.0M;
            bool fInstructorOnBoard = false;
            decimal dutiesOfPICTime = 0.0M;

            cfr.FlightProps.ForEachEvent(pf =>
            {
                if (pf.PropertyType.IsSolo)
                    soloTime += pf.DecValue;

                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInstructorOnBoard && !pf.IsDefaultValue)
                    fInstructorOnBoard = true;    // instructor-on-board time only counts if you are acting as PIC
                if (pf.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropDutiesOfPIC && !pf.IsDefaultValue)
                    dutiesOfPICTime += pf.DecValue;
            });

            if (fInstructorOnBoard)
                soloTime += Math.Max(Math.Min(dutiesOfPICTime, cfr.Total - cfr.Dual), 0);    // dual received does NOT count as duties of PIC time here

            miSolo.AddEvent(soloTime);
            miLandings.AddEvent(cfr.cLandingsThisFlight);

            if (!miCrossCountry.IsSatisfied)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route);
                double distance = al.GetAirportList().Length > 1 ? al.DistanceForRoute() : 0.0;
                if ((soloTime > 0.0M && distance > minSoloXC) || (cfr.Dual > 0 && distance > minDualXC))
                    miCrossCountry.MatchFlightEvent(cfr);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>(new MilestoneItem[] { miTotal, miDual, miSolo, miLandings, miCrossCountry });
            }
        }
    }
    #endregion
    #endregion
}