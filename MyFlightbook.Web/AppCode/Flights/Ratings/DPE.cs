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
                new DPEGliderPPL(),
                new DPECommASEL(),
                new DPECommAMEL(),
                new DPECommASES(),
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
    [Serializable]
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
            string szCatClass = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, catClass.Category, catClass.Class);
            miPIC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTime, dpet.PIC), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PIC);
            miPICPastYear = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTimePastYear, dpet.PICPastYear, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICPastYear);
            miPICCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICCategory, dpet.PICCategory, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICCategory);
            miPICClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICClass, dpet.PICClass, szCatClass), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICClass);
            miPICNight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICNight, dpet.PICNight), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICNight);
            miCFICategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPECFICategory, dpet.CFICategory, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFICategory);
            miCFIClass = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPECFIClass, dpet.CFIClass, szCatClass), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIClass);
            miPICFlightsInPriorYear = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEFlightsPastYear, dpet.PICFlightsInClassPriorYear, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Count, dpet.PICFlightsInClassPriorYear);
            miPICComplexTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEComplex, dpet.PICComplexTime), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICComplexTime);
            miPICInstrumentTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEInstrument, dpet.PICInstrumentTime), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICInstrumentTime);
            miCFIITime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEInstrumentInstruction, dpet.CFIITime), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIITime);
            miCFIITimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEInstrumentInstructionInCategory, dpet.CFIITimeInCategory, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.CFIITimeInCategory);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!cfr.fIsRealAircraft)
                return;

            miPIC.AddEvent(cfr.PIC);
            miPICNight.AddEvent(Math.Min(cfr.PIC, cfr.Night));

            miPICInstrumentTime.AddEvent(Math.Min(cfr.PIC, cfr.IMCSim + cfr.IMC));

            if (cfr.fIsComplex)
                miPICComplexTime.AddEvent(cfr.PIC);

            decimal cfiiTime = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropCFIITime);
            decimal instInstruction = cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropInstrumentInstructionTime);
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

                miCFIITimeInCategory.AddEvent(instrumentInstruction);

                if (cfr.idCatClassOverride == catClass.IdCatClass)
                {
                    miPICClass.AddEvent(cfr.PIC);
                    miCFIClass.AddEvent(cfr.CFI);
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
    [Serializable]
    internal abstract class DPEAirplaneBasePPL : DPEBase
    {
        public DPEAirplaneBasePPL(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 2000, PICCategory = 1000, PICClass = 300, PICNight = 100, PICPastYear = 300, CFICategory = 500, CFIClass = 100 }, ccid, " Figure 7-2A")
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

    [Serializable]
    internal class DPEASELPPL : DPEAirplaneBasePPL
    {
        public DPEASELPPL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPEPPLASEL) { }
    }

    [Serializable]
    internal class DPEAMELPPL : DPEAirplaneBasePPL
    {
        public DPEAMELPPL() : base(CategoryClass.CatClassID.AMEL, Resources.MilestoneProgress.TitleDPEPPLAMEL)
        {
            GeneralDisclaimer = Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }
    }

    [Serializable]
    internal class DPEASESPPL : DPEAirplaneBasePPL
    {
        public DPEASESPPL() : base(CategoryClass.CatClassID.ASES, Resources.MilestoneProgress.TitleDPEPPLASES) { }
    }

    [Serializable]
    internal class DPEAMESPPL : DPEAirplaneBasePPL
    {
        public DPEAMESPPL() : base(CategoryClass.CatClassID.AMES, Resources.MilestoneProgress.TitleDPEPPLAMES)
        { 
            GeneralDisclaimer = Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }
    }
    #endregion

    #region Rotorcraft PPL
    [Serializable]
    internal abstract class DPERotorcraftBasePPL : DPEBase
    {
        public DPERotorcraftBasePPL(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 1000, PICCategory = 500, PICClass = (ccid == CategoryClass.CatClassID.Gyroplane ? 150 : 250), PICPastYear = 100, CFIClass = 200 }, ccid, " Figure 7-2A")
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

    [Serializable]
    internal class DPEHelicopterPPL : DPERotorcraftBasePPL
    {
        public DPEHelicopterPPL() : base(CategoryClass.CatClassID.Helicopter, Resources.MilestoneProgress.TitleDPEPPLHelicopter) { }
    }

    [Serializable]
    internal class DPEGyrpolanePPL : DPERotorcraftBasePPL
    {
        public DPEGyrpolanePPL() : base(CategoryClass.CatClassID.Gyroplane, Resources.MilestoneProgress.TitleDPEPPLGyroplane) { }
    }
    #endregion

    [Serializable]
    internal class DPEGliderPPL : DPEBase
    {

        public DPEGliderPPL() : base(new DPEThresholds() { PIC = 500, PICClass = 200, PICPastYear = 10, PICFlightsInClassPriorYear = 10, CFIClass = 100 }, CategoryClass.CatClassID.Glider, " Figure 7-2A")
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
    [Serializable]
    internal abstract class DPEAirplaneCommBase : DPEBase
    {
        public DPEAirplaneCommBase(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { PIC = 2000, PICCategory = 1000, PICClass = 500, PICNight = 100, PICComplexTime = 200, PICInstrumentTime = 100, PICPastYear = 300, CFICategory =500, CFIClass= 100, CFIITime = 250, CFIITimeInCategory = 200 }, ccid, " Figure 7-3A")
        {
            Title = szTitle;
            GeneralDisclaimer = Resources.MilestoneProgress.DPECommInstrumentDisclaimer;
            if (ccid == CategoryClass.CatClassID.AMEL || ccid == CategoryClass.CatClassID.AMES)
                GeneralDisclaimer += Resources.LocalizedText.LocalizedSpace + Resources.MilestoneProgress.DPEMultiHelicopterDisclaimer;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICCategory, miPICClass, miPICNight, miPICComplexTime, miPICInstrumentTime, miPICPastYear, miCFICategory, miCFIClass, miCFIITime, miCFIITimeInCategory }; }
        }
    }

    [Serializable]
    internal class DPECommASEL : DPEAirplaneCommBase
    {
        internal DPECommASEL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPECommASEL) { }
    }

    [Serializable]
    internal class DPECommAMEL : DPEAirplaneCommBase
    {
        internal DPECommAMEL() : base(CategoryClass.CatClassID.AMEL, Resources.MilestoneProgress.TitleDPECommAMEL) { }
    }

    [Serializable]
    internal class DPECommASES : DPEAirplaneCommBase
    {
        internal DPECommASES() : base(CategoryClass.CatClassID.ASES, Resources.MilestoneProgress.TitleDPECommASES) { }
    }

    [Serializable]
    internal class DPECommAMES : DPEAirplaneCommBase
    {
        internal DPECommAMES() : base(CategoryClass.CatClassID.AMES, Resources.MilestoneProgress.TitleDPECommAMES) { }
    }
    #endregion

    [Serializable]
    internal class DPECommHelicopter : DPEBase
    {
        internal DPECommHelicopter() : base(new DPEThresholds() { PIC = 2000, PICClass = 500, PICInstrumentTime = 100, PICPastYear = 100, CFIClass = 250, CFIITimeInCategory = 50 }, CategoryClass.CatClassID.Helicopter, " Figure 7-3A")
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

    [Serializable]
    internal class DPECommGlider : DPEBase
    {
        internal DPECommGlider() : base(new DPEThresholds() { PIC = 500, PICClass = 250, PICPastYear = 20, PICFlightsInClassPriorYear = 50, CFIClass = 100 }, CategoryClass.CatClassID.Glider, " Figure 7-2B")
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