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
    /// ATP milestones
    /// </summary>
    [Serializable]
    public class ATPMilestones : MilestoneGroup
    {
        public ATPMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupATP;
            Milestones = new MilestoneProgress[] {
                new ATPAirplaneASEL(),
                new ATPAirplaneASES(),
                new ATPAirplaneAMEL(),
                new ATPAirplaneAMES(),
                new ATPRestrictedAirplaneAMELMilitary(),
                new ATPRestrictedAirplaneAMELBachelor(),
                new ATPRestrictedAirplaneAMELAssociate(),
                new ATPRestrictedAirplaneAMELPartial(),
                new ATPRestrictedAirplaneASEL160F(),
                new ATPRestrictedAirplaneASES160F(),
                new ATPRestrictedAirplaneAMEL160F(),
                new ATPRestrictedAirplaneAMES160F(),
                new ATPHelicopter(),
                new ATPPoweredLift(),
                new ATPCanadaAirplane(),
                new ATPCanadaHelicopter()
            };
        }
    }

    #region 61.159, 160, 161, 163 - ATP
    [Serializable]
    public abstract class ATPAirplane : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinInstrumentTime { get; set; }
        protected MilestoneItem miMinTimeInClass { get; set; }
        protected MilestoneItem miMinPIC { get; set; }
        protected MilestoneItem miMinPICXC { get; set; }
        protected MilestoneItem miMinPICNight { get; set; }
        protected MilestoneItem miNightTO { get; set; }
        protected MilestoneItem miNightLanding { get; set; }
        protected CategoryClass.CatClassID requiredCatClassID { get; set; }
        private bool CanCreditSICAndFlightEngineer { get; set; }

        protected decimal ATPTotalTime { get; set; }
        protected decimal ATPMinXC { get; set; }
        const decimal ATPMinNight = 100;
        const decimal ATPMinIFR = 75;
        const decimal ATPMinTimeInClass = 50;
        const decimal ATPMinTimeInClassFullSimulator = 25;
        const decimal ATPMinTotalFullSimulator = 100;
        const decimal ATPMinPIC = 250;
        const decimal ATPMinPICXC = 100;
        const decimal ATPMinPICNight = 25;
        const decimal ATPMinNightTakeoffs = 20;
        const decimal ATPMinNightLandings = 20;
        const decimal ATPMaxIFRSimulator = 25;
        const decimal ATPMaxFlightEngineer = 500;

        protected ATPAirplane(string title, CategoryClass.CatClassID ccid, decimal minTime = 1500, decimal minXCTime = 500, bool fCanCreditSICAndFlightEngineer = false)
        {
            Title = title;
            requiredCatClassID = ccid;
            BaseFAR = "61.159";
            RatingSought = RatingType.ATPAirplane;
            ATPTotalTime = minTime;
            ATPMinXC = minXCTime;
            CanCreditSICAndFlightEngineer = fCanCreditSICAndFlightEngineer;
            CategoryClass cc = CategoryClass.CategoryClassFromID(ccid);

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ATPMinTime, ATPTotalTime), ResolvedFAR("(a)"), fCanCreditSICAndFlightEngineer ? Branding.ReBrand(Resources.MilestoneProgress.ATPTotalTimeSubstitutionNote) : string.Empty, MilestoneItem.MilestoneType.Time, ATPTotalTime);
            miMinXCTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinXCTime, ResolvedFAR("(a)(1)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinXC);
            miMinNightTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinNightTime, ResolvedFAR("(a)(2)"), Resources.MilestoneProgress.ATPAirplaneNightLandingsNote, MilestoneItem.MilestoneType.Time, ATPMinNight);
            miMinTimeInClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ATPTimeInClass, ATPMinTimeInClass, cc.Class), ResolvedFAR("(a)(3)"), Branding.ReBrand(Resources.MilestoneProgress.ATPAirplaneClassSimulatorNote), MilestoneItem.MilestoneType.Time, ATPMinTimeInClass);
            miMinInstrumentTime = new MilestoneItem(Resources.MilestoneProgress.ATPInstrumentTime, ResolvedFAR("(a)(4)"), Resources.MilestoneProgress.ATPAirplaneSimulatorNote, MilestoneItem.MilestoneType.Time, ATPMinIFR);
            miMinPIC = new MilestoneItem(Resources.MilestoneProgress.ATPPICTime, ResolvedFAR("(a)(5)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPIC);
            miMinPICXC = new MilestoneItem(Resources.MilestoneProgress.ATPXCPICTime, ResolvedFAR("(a)(5)(i)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPICXC);
            miMinPICNight = new MilestoneItem(Resources.MilestoneProgress.ATPNightPICTime, ResolvedFAR("(a)(5)(ii)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPICNight);
            miNightTO = new MilestoneItem(string.Empty, ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Count, ATPMinNightTakeoffs);
            miNightLanding = new MilestoneItem(string.Empty, ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Count, ATPMinNightLandings);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            bool fIsAirplane = CategoryClass.IsAirplane(cfr.idCatClassOverride);
            bool fIsIFRSim = (fIsAirplane && cfr.fIsFTD);

            if (fIsAirplane || cfr.fIsRealAircraft)
                miMinInstrumentTime.AddTrainingEvent(cfr.IMC + cfr.IMCSim, ATPMaxIFRSimulator, fIsIFRSim);

            if (fIsAirplane && cfr.idCatClassOverride == requiredCatClassID)
            {
                bool fIsFullSim = !cfr.fIsRealAircraft && cfr.fIsCertifiedLanding;
                if (fIsAirplane && (cfr.fIsRealAircraft || fIsFullSim))
                    miMinTimeInClass.AddTrainingEvent(cfr.Total, ATPMinTimeInClassFullSimulator, fIsFullSim);
            }

            if (!cfr.fIsRealAircraft && fIsIFRSim)
                miTotal.AddTrainingEvent(Math.Max(cfr.Total, cfr.GroundSim), ATPMinTotalFullSimulator, true);

            // Above are the only things to which we will alow training devices.
            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(Math.Max(CanCreditSICAndFlightEngineer ? cfr.SIC : 0, cfr.Total));
            miMinXCTime.AddEvent(cfr.XC);
            miMinNightTime.AddEvent(cfr.Night);

            if (CanCreditSICAndFlightEngineer)
            {
                decimal flightEngineerTime = 0.0M;
                cfr.FlightProps.ForEachEvent((pe) => { if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropFlightEngineerTime) flightEngineerTime += pe.DecValue; });

                if (flightEngineerTime > 0)
                    miTotal.AddTrainingEvent(flightEngineerTime / 3.0M, ATPMaxFlightEngineer, true);
            }

            // Remainder must be done in a real aircraft and it must be in an airplane.  
            // Not clear if the night takeoffs/landings need to be in a real aircraft / airplane, but I'll require it to be safe.
            if (fIsAirplane)
            {
                miMinPIC.AddEvent(cfr.PIC);
                miMinPICXC.AddEvent(Math.Min(cfr.PIC, cfr.XC));
                miMinPICNight.AddEvent(Math.Min(cfr.PIC, cfr.Night));
                cfr.FlightProps.ForEachEvent((pe) => { if (pe.PropertyType.IsNightTakeOff) miNightTO.AddEvent(pe.IntValue); });
                miNightLanding.AddEvent(cfr.cFullStopNightLandings);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // For each night takeoff/landing AFTER 20 have been satisfied, can substitue for one hour of night flying UP TO 25 hours.
                if (miNightTO.IsSatisfied && miNightLanding.IsSatisfied)
                    miMinNightTime.AddEvent(Math.Min(Math.Max(
                                                        Math.Min(miNightTO.Progress, miNightLanding.Progress) - Math.Max(ATPMinNightLandings, ATPMinNightLandings),
                                                        0),
                                                     25));

                return new Collection<MilestoneItem>() {
                miTotal,
                miMinXCTime,
                miMinNightTime,
                miMinTimeInClass,
                miMinInstrumentTime,
                miMinPIC,
                miMinPICXC,
                miMinPICNight
                };
            }
        }
    }

    #region concrete ATP Airplane classes
    [Serializable]
    public class ATPAirplaneASEL : ATPAirplane
    {
        public ATPAirplaneASEL() : base(Resources.MilestoneProgress.Title61159ASEL, CategoryClass.CatClassID.ASEL, fCanCreditSICAndFlightEngineer: true) { }
    }

    [Serializable]
    public class ATPAirplaneASES : ATPAirplane
    {
        public ATPAirplaneASES() : base(Resources.MilestoneProgress.Title61159ASES, CategoryClass.CatClassID.ASES, fCanCreditSICAndFlightEngineer: true) { }
    }

    [Serializable]
    public class ATPAirplaneAMEL : ATPAirplane
    {
        public ATPAirplaneAMEL() : base(Resources.MilestoneProgress.Title61159AMEL, CategoryClass.CatClassID.AMEL, fCanCreditSICAndFlightEngineer: true) { }
    }

    [Serializable]
    public class ATPAirplaneAMES : ATPAirplane
    {
        public ATPAirplaneAMES() : base(Resources.MilestoneProgress.Title61159AMES, CategoryClass.CatClassID.AMES, fCanCreditSICAndFlightEngineer: true) { }
    }

    [Serializable]
    public abstract class ATPRestrictedBase : ATPAirplane
    {
        protected const decimal minXCReduced = 200;

        protected ATPRestrictedBase(string title, CategoryClass.CatClassID ccid, string szBaseFAR, decimal minTime, decimal minXCTime) : base(title, ccid, minTime, minXCTime, fCanCreditSICAndFlightEngineer: true)
        {
            miTotal.FARRef = szBaseFAR;
            miMinXCTime.Note = Resources.MilestoneProgress.ATPMinXCTimeNoteRestricted;
            RatingSought = RatingType.ATPAirplaneRestricted;
        }

        protected void updateXCTime(string szFARRef)
        {
            miMinXCTime.Threshold = minXCReduced;
            miMinXCTime.Title = Resources.MilestoneProgress.ATPMinXCTimeRestricted;
            miMinXCTime.FARRef = szFARRef;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                if (miTotal.Progress > 1500 && miMinXCTime.Threshold > minXCReduced)
                    updateXCTime("61.160(f)");

                return base.Milestones;
            }
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMELMilitary : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMELMilitary()
            : base(Resources.MilestoneProgress.Title61160FormerMilitary, CategoryClass.CatClassID.AMEL, "61.160(a)", 750, minXCReduced)
        {
            miTotal.Note = Branding.ReBrand(Resources.MilestoneProgress.ATPRestrictedMilitaryNote);
            updateXCTime("61.160(e)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMELBachelor : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMELBachelor()
            : base(Resources.MilestoneProgress.Title61160BachelorsDegree, CategoryClass.CatClassID.AMEL, "61.160(b)", 1000, minXCReduced)
        {
            miTotal.Note = Branding.ReBrand(Resources.MilestoneProgress.ATPRestrictedEducationNote);
            updateXCTime("61.160(e)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMELAssociate : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMELAssociate()
            : base(Resources.MilestoneProgress.Title61160AssociatesDegree, CategoryClass.CatClassID.AMEL, "61.160(c)", 1250, minXCReduced)
        {
            miTotal.Note = Branding.ReBrand(Resources.MilestoneProgress.ATPRestrictedEducationNote);
            updateXCTime("61.160(e)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMELPartial : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMELPartial()
            : base(Resources.MilestoneProgress.Title61160PartialDegree, CategoryClass.CatClassID.AMEL, "61.160(d)", 1250, 500)
        {
            miTotal.Note = Branding.ReBrand(Resources.MilestoneProgress.ATPRestrictedEducationNote);
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneASEL160F : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneASEL160F()
            : base(Resources.MilestoneProgress.Title61160FASEL, CategoryClass.CatClassID.ASEL, "61.160(f)", 1500, minXCReduced)
        {
            updateXCTime("61.160(f)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneASES160F : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneASES160F()
            : base(Resources.MilestoneProgress.Title61160FASES, CategoryClass.CatClassID.ASES, "61.160(f)", 1500, minXCReduced)
        {
            updateXCTime("61.160(f)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMEL160F : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMEL160F()
            : base(Resources.MilestoneProgress.Title61160FAMEL, CategoryClass.CatClassID.AMEL, "61.160(f)", 1500, minXCReduced)
        {
            updateXCTime("61.160(f)");
        }
    }

    [Serializable]
    public class ATPRestrictedAirplaneAMES160F : ATPRestrictedBase
    {
        public ATPRestrictedAirplaneAMES160F()
            : base(Resources.MilestoneProgress.Title61160FAMES, CategoryClass.CatClassID.AMES, "61.160(f)", 1500, minXCReduced)
        {
            updateXCTime("61.160(f)");
        }
    }

    [Serializable]
    public class ATPHelicopter : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinNightHelicopter { get; set; }
        protected MilestoneItem miMinHeli { get; set; }
        protected MilestoneItem miMinHeliPIC { get; set; }
        protected MilestoneItem miMinInstrumentTime { get; set; }
        protected MilestoneItem miMinHeliIMC { get; set; }
        protected MilestoneItem miMinHeliPICIMC { get; set; }
        const decimal ATPTotalTime = 1200;
        const decimal ATPMinXC = 500;
        const decimal ATPMinNight = 100;
        const decimal ATPMinNightHelicopters = 15;
        const decimal ATPMinHelicopterTime = 200;
        const decimal ATPMinHelicopterPIC = 75;
        const decimal ATPMinIFR = 75;
        const decimal ATPMinIFRHeli = 50;
        const decimal ATPMinIFRHeliPIC = 25;
        const decimal ATPMaxSimulator = 25;

        public ATPHelicopter()
        {
            Title = Resources.MilestoneProgress.Title61161;
            BaseFAR = "61.161";
            RatingSought = RatingType.ATPHelicopter;

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ATPMinTime, ATPTotalTime), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPTotalTime);
            miMinXCTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinXCTime, ResolvedFAR("(a)(1)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinXC);
            miMinNightTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinNightTime, ResolvedFAR("(a)(2)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinNight);
            miMinNightHelicopter = new MilestoneItem(Resources.MilestoneProgress.ATPNightHelicopterTime, ResolvedFAR("(a)(2)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinNightHelicopters);

            miMinHeli = new MilestoneItem(Resources.MilestoneProgress.ATPMinHeliTime, ResolvedFAR("(a)(3)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinHelicopterTime);
            miMinHeliPIC = new MilestoneItem(Resources.MilestoneProgress.ATPMinHeliPICTime, ResolvedFAR("(a)(3)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinHelicopterPIC);

            miMinInstrumentTime = new MilestoneItem(Resources.MilestoneProgress.ATPInstrumentTime, ResolvedFAR("(a)(4)"), Resources.MilestoneProgress.ATPAirplaneSimulatorNote, MilestoneItem.MilestoneType.Time, ATPMinIFR);
            miMinHeliIMC = new MilestoneItem(Resources.MilestoneProgress.ATPHelicopterInstrumentTime, ResolvedFAR("(a)(4)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinIFRHeli);
            miMinHeliPICIMC = new MilestoneItem(Resources.MilestoneProgress.ATPHelicopterPICInstrumentTime, ResolvedFAR("(a)(4)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinIFRHeliPIC);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            bool fIsHelicopter = (cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter);
            bool fIsRotorcraft = (fIsHelicopter || cfr.idCatClassOverride == CategoryClass.CatClassID.Gyroplane);
            bool fIsIFRSim = (fIsRotorcraft && cfr.fIsFTD);
            bool fIsTotalSim = (fIsRotorcraft && cfr.fIsFTD && cfr.fIsCertifiedLanding);

            if (fIsRotorcraft || cfr.fIsRealAircraft)
            {
                miTotal.AddTrainingEvent(cfr.Total, ATPMaxSimulator, fIsTotalSim);
                miMinInstrumentTime.AddTrainingEvent(cfr.IMC + cfr.IMCSim, ATPMaxSimulator, fIsIFRSim);
            }

            // Everything else MUST be done in a real aircraft
            if (!cfr.fIsRealAircraft)
                return;

            miMinXCTime.AddEvent(cfr.XC);
            miMinNightTime.AddEvent(cfr.Night);


            // And the remainder must not only be in a real aircraft, it must be in a helicopter
            if (cfr.idCatClassOverride != CategoryClass.CatClassID.Helicopter)
                return;

            miMinNightHelicopter.AddEvent(cfr.Night);
            miMinHeli.AddEvent(cfr.Total);
            miMinHeliPIC.AddEvent(cfr.PIC);
            miMinHeliIMC.AddEvent(cfr.IMC + cfr.IMCSim);
            miMinHeliPICIMC.AddEvent(Math.Min(cfr.PIC, cfr.IMC + cfr.IMCSim));
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miTotal,
                miMinXCTime,
                miMinNightTime,
                miMinNightHelicopter,
                miMinHeli,
                miMinHeliPIC,
                miMinInstrumentTime,
                miMinHeliIMC,
                miMinHeliPICIMC
                };
            }
        }
    }

    [Serializable]
    public class ATPPoweredLift : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinPLPIC { get; set; }
        protected MilestoneItem miMinPLPICXC { get; set; }
        protected MilestoneItem miMinPLPICNight { get; set; }
        protected MilestoneItem miMinInstrument { get; set; }
        const decimal ATPTotalTime = 1500;
        const decimal ATPMinXC = 500;
        const decimal ATPMinNight = 100;
        const decimal ATPMinPLPIC = 250;
        const decimal ATPMinPLPICXC = 100;
        const decimal ATPMinPLPICNight = 25;
        const decimal ATPMinPLInstrument = 75;
        const decimal ATPMaxIFRSim = 25;

        public ATPPoweredLift()
        {
            Title = Resources.MilestoneProgress.Title61163;
            BaseFAR = "61.163";
            RatingSought = RatingType.ATPPoweredLift;

            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ATPMinTime, ATPTotalTime), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPTotalTime);
            miMinXCTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinXCTime, ResolvedFAR("(a)(1)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinXC);
            miMinNightTime = new MilestoneItem(Resources.MilestoneProgress.ATPMinNightTime, ResolvedFAR("(a)(2)"), string.Empty, MilestoneItem.MilestoneType.Time, ATPMinNight);
            miMinPLPIC = new MilestoneItem(Resources.MilestoneProgress.ATPMinPLPIC, ResolvedFAR("(a)(3)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPLPIC);
            miMinPLPICXC = new MilestoneItem(Resources.MilestoneProgress.ATPMinPLPICXC, ResolvedFAR("(a)(3)(i)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPLPICXC);
            miMinPLPICNight = new MilestoneItem(Resources.MilestoneProgress.ATPMinPLPICNight, ResolvedFAR("(a)(3)(ii)"), Resources.MilestoneProgress.ATPPICTimeNote, MilestoneItem.MilestoneType.Time, ATPMinPLPICNight);
            miMinInstrument = new MilestoneItem(Resources.MilestoneProgress.ATPInstrumentTime, ResolvedFAR("(a)(4)"), Resources.MilestoneProgress.ATPAirplaneSimulatorNote, MilestoneItem.MilestoneType.Time, ATPMinPLInstrument);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            bool fIsPoweredLift = (cfr.idCatClassOverride == CategoryClass.CatClassID.PoweredLift);
            bool fIsIFRSim = (fIsPoweredLift && cfr.fIsFTD);

            if (fIsPoweredLift || cfr.fIsRealAircraft)
                miMinInstrument.AddTrainingEvent(cfr.IMC + cfr.IMCSim, ATPMaxIFRSim, fIsIFRSim);


            // Everything else MUST be done in a real aircraft
            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(cfr.Total);
            miMinXCTime.AddEvent(cfr.XC);
            miMinNightTime.AddEvent(cfr.Night);


            // And the remainder must not only be in a real aircraft, it must be in a powered lift
            if (cfr.idCatClassOverride != CategoryClass.CatClassID.PoweredLift)
                return;

            miMinPLPIC.AddEvent(cfr.PIC);
            miMinPLPICXC.AddEvent(Math.Min(cfr.PIC, cfr.XC));
            miMinPLPICNight.AddEvent(Math.Min(cfr.PIC, cfr.Night));
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miTotal,
                miMinXCTime,
                miMinNightTime,
                miMinPLPIC,
                miMinPLPICXC,
                miMinPLPICNight,
                miMinInstrument
                };
            }
        }
    }
    #endregion
    #endregion

    #region Canada
    /// <summary>
    /// ATP Airplane - http://www.tc.gc.ca/eng/civilaviation/regserv/cars/part4-standards-421-1086.htm?WT.mc_id=vl7fd#421_34
    /// </summary>
    [Serializable]
    public abstract class ATPCanadaBase : MilestoneProgress
    {
        protected int MinHours;
        protected int MinInCategory;
        protected int MinPICInCategory;
        protected int MaxPICUSInCategory;
        protected int MinXC;
        protected int MinPICXC;
        protected int MinPICXCNight;
        protected int MinNight;
        protected int MinNightInCategory;
        protected int MinXCPICAdditional;
        protected int MinXCSICPICAdditional;
        protected int MinInstrument;
        protected int MaxSimulator;
        protected int MaxInstrumentAlternativeCategory;
        protected string Category;
        protected string AltCategory;

        #region MilestoneItems
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miCategory { get; set; }
        protected MilestoneItem miXC { get; set; }
        protected MilestoneItem miPIC { get; set; }
        protected MilestoneItem miPICXC { get; set; }
        protected MilestoneItem miPICXCNight { get; set; }
        protected MilestoneItem miNight { get; set; }
        protected MilestoneItem miNightInCategory { get; set; }
        protected MilestoneItem miXCPICAdditional { get; set; }
        protected MilestoneItem miXCSICPICAdditional { get; set; }
        protected MilestoneItem miInstrument { get; set; }

        protected MilestoneItem miInstrumentAltCategory { get; set; }
        #endregion

        protected void InitMilestones(string catDisplay, string altCatDisplay)
        {
            miTotal = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPTotalTime, MinHours), ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, MinHours);
            miCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPTotalTimeInCategory, MinInCategory, catDisplay), ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, MinInCategory);
            miPIC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPPIC, MinPICInCategory, catDisplay), ResolvedFAR("(a)"), String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPPICUSNote, MaxPICUSInCategory), MilestoneItem.MilestoneType.Time, MinPICInCategory);
            miPICXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPPICXC, MinPICXC, catDisplay), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, MinPICXC);
            miPICXCNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPPICXCNight, MinPICXCNight, catDisplay), ResolvedFAR("(a)"), string.Empty, MilestoneItem.MilestoneType.Time, MinPICXCNight);
            miXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPXC, MinXC, catDisplay), ResolvedFAR("(c)"), string.Empty, MilestoneItem.MilestoneType.Time, MinXC);
            miNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPNight, MinNight), ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Time, MinNight);
            miNightInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPNightCategory, MinNightInCategory, catDisplay), ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Time, MinNightInCategory);
            miXCPICAdditional = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPExtraXC, MinXCPICAdditional), ResolvedFAR("(c)"), Resources.MilestoneProgress.CAATPExtraXCNote, MilestoneItem.MilestoneType.Time, MinXCPICAdditional);
            miXCSICPICAdditional = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPExtraXC, MinXCPICAdditional), ResolvedFAR("(c)"), Resources.MilestoneProgress.CAATPExtraXCNote, MilestoneItem.MilestoneType.Time, MinXCSICPICAdditional);
            miInstrument = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPInstrument, MinInstrument), ResolvedFAR("(d)"), String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CAATPInstrumentNote, MaxInstrumentAlternativeCategory, altCatDisplay), MilestoneItem.MilestoneType.Time, MinInstrument);
            miInstrumentAltCategory = new MilestoneItem(string.Empty, string.Empty, string.Empty, MilestoneItem.MilestoneType.Time, 0.0M);
        }

        protected ATPCanadaBase(string title, string category, string altcategory, string basefar, RatingType ratingtype)
        {
            Title = title;
            Category = category;
            AltCategory = altcategory;
            BaseFAR = basefar;
            RatingSought = ratingtype;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!cfr.fIsRealAircraft && cfr.fIsCertifiedIFR)
                miInstrument.AddTrainingEvent(cfr.IMCSim, MaxSimulator, !cfr.fIsRealAircraft);

            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(cfr.Total);

            decimal CoPilot = cfr.SIC + cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropMilitaryCoPilottime) + cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropCoPilotTime);

            miNight.AddEvent(Math.Min(CoPilot + cfr.PIC, cfr.Night));

            // Can add PIC or PICUS, but only up to MinPICUSInCategory for the latter.
            // So if a flight has, say, an hour of PIC and an hour of PICUS, it's possible that they're double-logged.  Looking at the database, that seems common.
            // E.g., a 1.5 hour flight has 1.5 hours of PIC and 1.5 hours of PICUS.
            // To be conservative, we'll subtract off any PICUS from PIC; the remainder is PIC time we can use unlimited.
            // Thus we can compute total PIC time for a flight as PIC+PICUS - which will possibly undercount but never overcount.
            decimal PICUS = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropPICUS);
            decimal netPIC = Math.Max(cfr.PIC - PICUS, 0.0M);

            miXCPICAdditional.AddEvent(Math.Min(cfr.PIC, cfr.XC));
            miXCSICPICAdditional.AddEvent(Math.Min(cfr.Total, Math.Min(cfr.PIC + cfr.SIC + CoPilot, cfr.XC)));

            if (cfr.szCategory.CompareCurrentCultureIgnoreCase(Category) == 0)
            {
                miCategory.AddEvent(cfr.Total);
                miXC.AddEvent(cfr.XC);

                miPIC.AddEvent(netPIC);
                miPICXC.AddEvent(Math.Min(netPIC, cfr.XC));
                miPICXCNight.AddEvent(Math.Min(netPIC, Math.Min(cfr.XC, cfr.Night)));

                miPIC.AddTrainingEvent(PICUS, MaxPICUSInCategory, true);
                miPICXC.AddTrainingEvent(Math.Min(PICUS, cfr.XC), MaxPICUSInCategory, true);
                miPICXCNight.AddTrainingEvent(Math.Min(PICUS, Math.Min(cfr.XC, cfr.Night)), MaxPICUSInCategory, true);

                miNightInCategory.AddEvent(Math.Min(CoPilot + cfr.PIC, cfr.Night));

                miInstrument.AddEvent(cfr.IMC + cfr.IMCSim);
            }
            else if (cfr.szCategory.CompareCurrentCultureIgnoreCase(AltCategory) == 0)
                miInstrumentAltCategory.AddTrainingEvent(cfr.IMC + cfr.IMCSim, MaxInstrumentAlternativeCategory, true);
        }
    }

    #region concrete ATP Canada classes
    [Serializable]
    /// <summary>
    /// ATP Airplane - http://www.tc.gc.ca/eng/civilaviation/regserv/cars/part4-standards-421-1086.htm?WT.mc_id=vl7fd#421_34(4)
    /// </summary>
    public class ATPCanadaAirplane : ATPCanadaBase
    {
        public ATPCanadaAirplane() : base(Resources.MilestoneProgress.TitleCAATPAirplane,
            CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASEL).Category,
            CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Helicopter).Category,            
            "421.34(4)",
            RatingType.ATPAirplane)
        {
            MinHours = 1500;
            MinInCategory = 900;
            MinXC = 0;
            MinPICInCategory = 250;
            MaxPICUSInCategory = 100;
            MinPICXC = 100;
            MinPICXCNight = 25;
            MinNight = 100;
            MinNightInCategory = 30;
            MinXCPICAdditional = 100;
            MinXCSICPICAdditional = 200;
            MinInstrument = 75;
            MaxSimulator = 25;
            MaxInstrumentAlternativeCategory = 35;
            InitMilestones(Resources.MilestoneProgress.CAATPAeroplanes, Resources.MilestoneProgress.CAATPHelicopters);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // "Additional" PICXC appears to need to be above and beyond the hours in-category, so subtract off those
                // we'll then return that one (using straight PIC-XC), if it's met OR if the alternative isn't met; otherwise, we'll return the alternative.
                miXCPICAdditional.Progress = Math.Max(miXCPICAdditional.Progress - miPICXC.Threshold, 0);
                MilestoneItem miAdditionalPICXC = miXCPICAdditional.IsSatisfied || !miXCSICPICAdditional.IsSatisfied ? miXCPICAdditional : miXCSICPICAdditional;

                // We can also add instrument time in alternative category to the training time.  (Couldn't do this with training event because we're already using the limit for sims)
                miInstrument.AddEvent(Math.Min(miInstrumentAltCategory.Progress, MaxInstrumentAlternativeCategory));

                return new Collection<MilestoneItem>() {
                    miTotal, miCategory, miPIC, miPICXC, miPICXCNight, miNight, miNightInCategory, miAdditionalPICXC, miInstrument };
            }
        }
    }

    [Serializable]
    /// <summary>
    /// ATP Helicopter - http://www.tc.gc.ca/eng/civilaviation/regserv/cars/part4-standards-421-1086.htm?WT.mc_id=vl7fd#421_35(4)
    /// </summary>
    public class ATPCanadaHelicopter : ATPCanadaBase
    {
        public ATPCanadaHelicopter() : base(Resources.MilestoneProgress.TitleCAATPHelicopter, 
            CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Helicopter).Category, 
            CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASEL).Category,
            "421.35(4)", 
            RatingType.ATPHelicopter)
        {
            MinHours = 1000;
            MinInCategory = 600;
            MinXC = 200;
            MinPICInCategory = 250;
            MaxPICUSInCategory = 150;
            MinPICXC = 100;
            MinPICXCNight = 0;
            MinNight = 50;
            MinNightInCategory = 15;
            MinXCPICAdditional = 100;
            MinXCSICPICAdditional = 200;
            MinInstrument = 30;
            MaxSimulator = 0;
            MaxInstrumentAlternativeCategory = 15;

            InitMilestones(Resources.MilestoneProgress.CAATPHelicopters, Resources.MilestoneProgress.CAATPAeroplanes);

            miPICXC.FARRef = ResolvedFAR("(c)");
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // We can also add instrument time in alternative category to the training time.  (Couldn't do this with training event because we're already using the limit for sims)
                miInstrument.AddEvent(Math.Min(miInstrumentAltCategory.Progress, MaxInstrumentAlternativeCategory));

                return new Collection<MilestoneItem>() {
                    miTotal, miCategory, miPIC,  miNight, miNightInCategory, miXC, miPICXC, miInstrument };
            }
        }
    }
    #endregion
    #endregion
}