using MyFlightbook.FlightCurrency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MilestoneProgress
{

    /// <summary>
    /// Recreational Pilot milestones
    /// </summary>
    [Serializable]
    public class DPEMilestones : MilestoneGroup
    {
        public DPEMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupDPE;
            Milestones = new MilestoneProgress[] {
                new DPEASELPPL(),
                new DPEAMELPPL(),
                new DPEASESPPL(),
                new DPEAMESPPL(),
                new DPEHelicopterPPL(),
                new DPEGyrpolanePPL(),
                new DPECommASEL(),
                new DPECommASES(),
                new DPECommAMEL(),
                new DPECommAMES(),
                new DPECommHelicopter(),
                new DPECommGlider()
            };
        }
    }

    /// <summary>
    /// Progress towards becoming a DPE.  
    /// See www.faa.gov/other_visit/aviation_industry/designees_delegations/resources/forms/media/8710-9.pdf or
    /// http://www.faa.gov/documentLibrary/media/Order/Order_8900_2A_CHG_1-3.pdf
    /// </summary>
    internal abstract class DPEBase : MilestoneProgress
    {
        internal class DPEThresholds
        {
            public int PIC { get; set; }
            public int PICPastYear { get; set; }
            public int PICCategory { get; set; }
            public int PICClass { get; set; }
            public int PICNight { get; set; }
            public int PICFlightsInClassPriorYear { get; set; }
            public int PICComplexTime { get; set; }
            public int PICInstrumentTime { get; set; }
            public int CFICategory { get; set; }
            public int CFIClass { get; set; }
            public int CFIITime { get; set; }
            public int CFIITimeInCategory { get; set; }
        }

        protected CategoryClass catClass { get; set; }

        protected MilestoneItem miPIC { get; set; }
        protected MilestoneItem miPICPastYear { get; set; }
        protected MilestoneItem miPICCategory { get; set; }
        protected MilestoneItem miPICClass { get; set; }
        protected MilestoneItem miPICNight { get; set; }
        protected MilestoneItem miCFICategory { get; set; }
        protected MilestoneItem miCFIClass { get; set; }
        protected MilestoneItem miPICFlightsInPriorYear { get; set; }
        protected MilestoneItem miPICComplexTime { get; set; }
        protected MilestoneItem miPICInstrumentTime { get; set; }
        protected MilestoneItem miCFIITime { get; set; }
        protected MilestoneItem miCFIITimeInCategory { get; set; }

        public DPEBase(DPEThresholds dpet, CategoryClass.CatClassID ccid, string szFarRef)
        {
            if (dpet == null)
                throw new ArgumentNullException("dpet");
            BaseFAR = "Order 8900.2A";
            string szResolved = ResolvedFAR(szFarRef);
            catClass = CategoryClass.CategoryClassFromID(ccid);
            miPIC = new MilestoneItem(Resources.MilestoneProgress.DPEPICTime, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PIC);
            miPICPastYear = new MilestoneItem(Resources.MilestoneProgress.DPEPICTimePastYear, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICPastYear);
            miPICCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICCategory, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICCategory);
            miPICClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICClass, catClass.CatClass), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICClass);
            miPICNight = new MilestoneItem(Resources.MilestoneProgress.DPEPICNight, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICNight);
            miCFICategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPECFICategory, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFICategory);
            miCFIClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPECFIClass, catClass.CatClass), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIClass);
            miPICFlightsInPriorYear = new MilestoneItem(Resources.MilestoneProgress.DPEFlightsPastYear, szResolved, string.Empty, MilestoneItem.MilestoneType.Count, dpet.PICFlightsInClassPriorYear);
            miPICComplexTime = new MilestoneItem(Resources.MilestoneProgress.DPEComplex, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICComplexTime);
            miPICInstrumentTime = new MilestoneItem(Resources.MilestoneProgress.DPEInstrument, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICInstrumentTime);
            miCFIITime = new MilestoneItem(Resources.MilestoneProgress.DPEInstrumentInstruction, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIITime);
            miCFIITimeInCategory = new MilestoneItem(Resources.MilestoneProgress.DPEInstrumentInstructionInCategory, szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIITimeInCategory);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (!cfr.fIsRealAircraft)
                return;

            miPIC.AddEvent(cfr.PIC);
            miPICNight.AddEvent(Math.Min(cfr.PIC, cfr.Night));

            miPICInstrumentTime.AddEvent(Math.Min(cfr.PIC, cfr.IMCSim + cfr.IMC));

            if (cfr.fIsComplex)
                miPICComplexTime.AddEvent(cfr.PIC);

            decimal cfiiTime = cfr.TimeForProperty(CustomPropertyType.KnownProperties.IDPropCFIITime);
            decimal instInstruction = cfr.TimeForProperty(CustomPropertyType.KnownProperties.IDPropInstrumentInstructionTime);
            decimal instrumentInstruction = Math.Max(cfiiTime, instInstruction);
            miCFIITime.AddEvent(instrumentInstruction);

            if (cfr.szCategory.CompareOrdinalIgnoreCase(catClass.Category) == 0)
            {
                if (cfr.dtFlight.CompareTo(DateTime.Now.AddYears(-1)) >= 0)
                {
                    miPICPastYear.AddEvent(cfr.PIC);
                    miPICFlightsInPriorYear.AddEvent(1);
                }

                miPICCategory.AddEvent(cfr.PIC);
                miCFICategory.AddEvent(cfr.CFI);

                if (cfr.idCatClassOverride == catClass.IdCatClass)
                {
                    miPICClass.AddEvent(cfr.PIC);
                    miCFIClass.AddEvent(cfr.CFI);
                    miCFIITimeInCategory.AddEvent(instrumentInstruction);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // Every DPE class is a bit different here, so ensure that it is implemented in a subclass.
                throw new NotImplementedException();
            }
        }
    }

    #region Concrete DPE progress classes
    #region DPE PPL classes
    #region Airplane PPL
    internal abstract class DPEAirplaneBasePPL : DPEBase
    {
        public DPEAirplaneBasePPL(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 2000, PICCategory = 1000, PICClass = 300, PICNight = 100, PICPastYear = 300, CFICategory = 500, CFIClass = 100 }, ccid, "Figure 7-2A")
        {
            Title = szTitle;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miPIC, miPICCategory, miPICClass, miPICNight, miPICPastYear, miCFICategory, miCFIClass
                };
            }
        }
    }

    internal class DPEASELPPL : DPEAirplaneBasePPL
    {
        public DPEASELPPL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPEPPLASEL) { }
    }

    internal class DPEAMELPPL : DPEAirplaneBasePPL
    {
        public DPEAMELPPL() : base(CategoryClass.CatClassID.AMEL, Resources.MilestoneProgress.TitleDPEPPLAMEL)
        {
            GeneralDisclaimer = Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }
    }

    internal class DPEASESPPL : DPEAirplaneBasePPL
    {
        public DPEASESPPL() : base(CategoryClass.CatClassID.ASES, Resources.MilestoneProgress.TitleDPEPPLASES) { }
    }

    internal class DPEAMESPPL : DPEAirplaneBasePPL
    {
        public DPEAMESPPL() : base(CategoryClass.CatClassID.AMES, Resources.MilestoneProgress.TitleDPEPPLAMES)
        { 
            GeneralDisclaimer = Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }
    }
    #endregion

    #region Rotorcraft PPL
    internal abstract class DPERotorcraftBasePPL : DPEBase
    {
        public DPERotorcraftBasePPL(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 1000, PICCategory = 500, PICClass = (ccid == CategoryClass.CatClassID.Gyroplane ? 150 : 250), PICPastYear = 100, CFIClass = 200 }, ccid, "Figure 7-2A")
        {
            GeneralDisclaimer = Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
            Title = szTitle;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miPIC, miPICCategory, miPICClass, miPICPastYear, miCFIClass
                };
            }
        }
    }

    internal class DPEHelicopterPPL : DPERotorcraftBasePPL
    {
        public DPEHelicopterPPL() : base(CategoryClass.CatClassID.Helicopter, Resources.MilestoneProgress.TitleDPEPPLHelicopter) { }
    }

    internal class DPEGyrpolanePPL : DPERotorcraftBasePPL
    {
        public DPEGyrpolanePPL() : base(CategoryClass.CatClassID.Gyroplane, Resources.MilestoneProgress.TitleDPEPPLGyroplane) { }
    }
    #endregion

    internal class DPEGliderPPL : DPEBase
    {

        public DPEGliderPPL() : base(new DPEThresholds() { PIC = 500, PICClass = 200, PICPastYear = 10, PICFlightsInClassPriorYear = 10, CFIClass = 100 }, CategoryClass.CatClassID.Glider, "Figure 7-2A")
        {
            Title = Resources.MilestoneProgress.TitleDPEPPLGlider;
        }
         
        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miPIC, miPICClass, miPICPastYear, miPICFlightsInPriorYear, miCFIClass
                };
            }
        }
    }
    #endregion

    #region DPE Commercial classes
    #region Airplane Comm
    internal abstract class DPEAirplaneCommBase : DPEBase
    {
        public DPEAirplaneCommBase(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 2000, PICCategory = 1000, PICClass = 500, PICNight = 100, PICComplexTime = 200, PICInstrumentTime = 100, PICPastYear = 300, CFICategory =500, CFIClass= 100, CFIITime = 250, CFIITimeInCategory = 200 }, ccid, "Figure 7-3A")
        {
            Title = szTitle;
            GeneralDisclaimer = Resources.MilestoneProgress.DPECommInstrumentDisclaimer;
            if (ccid == CategoryClass.CatClassID.AMEL || ccid == CategoryClass.CatClassID.AMES)
                GeneralDisclaimer += Resources.LocalizedText.LocalizedSpace + Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICCategory, miPICClass, miPICNight, miPICComplexTime, miPICInstrumentTime, miPICFlightsInPriorYear, miCFICategory, miCFIClass, miCFIITime, miCFIITimeInCategory }; }
        }
    }

    internal class DPECommASEL : DPEAirplaneCommBase
    {
        internal DPECommASEL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPECommASEL) { }
    }

    internal class DPECommAMEL : DPEAirplaneCommBase
    {
        internal DPECommAMEL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPECommAMEL) { }
    }

    internal class DPECommASES : DPEAirplaneCommBase
    {
        internal DPECommASES() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPECommASES) { }
    }

    internal class DPECommAMES : DPEAirplaneCommBase
    {
        internal DPECommAMES() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPECommAMES) { }
    }
    #endregion

    internal class DPECommHelicopter : DPEBase
    {
        internal DPECommHelicopter() : base(new DPEThresholds() { PIC = 2000, PICClass = 500, PICInstrumentTime = 100, PICPastYear = 100, CFIClass = 250, CFIITimeInCategory = 50 }, CategoryClass.CatClassID.Helicopter, "Figure 7-3A")
        {
            Title = Resources.MilestoneProgress.TitleDPECommHelicopter;
            GeneralDisclaimer = Resources.MilestoneProgress.DPECommInstrumentDisclaimer + Resources.LocalizedText.LocalizedSpace + Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
            miCFIITimeInCategory.Note = Branding.ReBrand(Resources.MilestoneProgress.DPEInstrumentInstructionDisclaimer);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICClass, miPICInstrumentTime, miPICPastYear, miCFIClass, miCFIITimeInCategory }; }
        }
    }

    internal class DPECommGlider : DPEBase
    {
        internal DPECommGlider() : base(new DPEThresholds() { PIC = 500, PICClass = 250, PICPastYear = 20, PICFlightsInClassPriorYear = 50, CFIClass = 100 }, CategoryClass.CatClassID.Glider, "Figure 7-2B")
        {
            Title = Resources.MilestoneProgress.TitleDPECommGlider;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICClass, miPICPastYear, miPICFlightsInPriorYear, miCFIClass }; }
        }
    }

    #endregion
    #endregion
}