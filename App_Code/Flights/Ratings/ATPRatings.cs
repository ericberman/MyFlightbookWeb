using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
                new ATPPoweredLift() };
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

            // Above are the only things to which we will alow training devices.
            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(Math.Max(CanCreditSICAndFlightEngineer ? cfr.SIC : 0, cfr.Total));
            miMinXCTime.AddEvent(cfr.XC);
            miMinNightTime.AddEvent(cfr.Night);

            if (CanCreditSICAndFlightEngineer)
            {
                decimal flightEngineerTime = 0.0M;
                cfr.ForEachEvent((pe) => { if (pe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropFlightEngineerTime) flightEngineerTime += pe.DecValue; });

                if (flightEngineerTime > 0)
                    miTotal.AddTrainingEvent(flightEngineerTime / 3.0M, ATPMaxFlightEngineer, false);
            }

            // Remainder must be done in a real aircraft and it must be in an airplane.  
            // Not clear if the night takeoffs/landings need to be in a real aircraft / airplane, but I'll require it to be safe.
            if (fIsAirplane)
            {
                miMinPIC.AddEvent(cfr.PIC);
                miMinPICXC.AddEvent(Math.Min(cfr.PIC, cfr.XC));
                miMinPICNight.AddEvent(Math.Min(cfr.PIC, cfr.Night));
                cfr.ForEachEvent((pe) => { if (pe.PropertyType.IsNightTakeOff) miNightTO.AddEvent(pe.IntValue); });
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

                Collection<MilestoneItem> l = new Collection<MilestoneItem>();
                l.Add(miTotal);
                l.Add(miMinXCTime);
                l.Add(miMinNightTime);
                l.Add(miMinTimeInClass);
                l.Add(miMinInstrumentTime);
                l.Add(miMinPIC);
                l.Add(miMinPICXC);
                l.Add(miMinPICNight);
                return l;
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

        protected ATPRestrictedBase(string title, CategoryClass.CatClassID ccid, string szBaseFAR, decimal minTime, decimal minXCTime) : base(title, ccid, minTime, minXCTime)
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
                Collection<MilestoneItem> l = new Collection<MilestoneItem>();
                l.Add(miTotal);
                l.Add(miMinXCTime);
                l.Add(miMinNightTime);
                l.Add(miMinNightHelicopter);
                l.Add(miMinHeli);
                l.Add(miMinHeliPIC);
                l.Add(miMinInstrumentTime);
                l.Add(miMinHeliIMC);
                l.Add(miMinHeliPICIMC);
                return l;
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
                Collection<MilestoneItem> l = new Collection<MilestoneItem>();
                l.Add(miTotal);
                l.Add(miMinXCTime);
                l.Add(miMinNightTime);
                l.Add(miMinPLPIC);
                l.Add(miMinPLPICXC);
                l.Add(miMinPLPICNight);
                l.Add(miMinInstrument);
                return l;
            }
        }
    }
    #endregion
    #endregion
}