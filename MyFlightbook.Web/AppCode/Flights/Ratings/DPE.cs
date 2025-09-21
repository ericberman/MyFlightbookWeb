using MyFlightbook.Currency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
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
        }

        public override Collection<MilestoneProgress> Milestones
        {
            get {
                return new Collection<MilestoneProgress> {
                new DPEASELPPL(),
                new DPEAMELPPL(),
                new DPEASESPPL(),
                new DPEAMESPPL(),
                new DPEHelicopter800095D(),
                new DPEGyroplane800095D(),
                new DPEPPLGlider(),
                new DPECommASEL(),
                new DPECommAMEL(),
                new DPECommASES(),
                new DPECommAMES(),
                new DPECommHelicopter(),
                };
            }
        }
    }

    /// <summary>
    /// Progress towards becoming a DPE.  
    /// See FAA order 800095C or 8900.2C for more information.
    /// </summary>
    [Serializable]
    internal abstract class DPEBase : MilestoneProgress
    {
        protected const string baseFAR800095D = "Order 8000.95D";
        protected const string href800095D = "https://www.faa.gov/regulations_policies/orders_notices/index.cfm/go/document.information/documentID/1043481";

        internal class DPEThresholds
        {
            public int PIC { get; set; }
            public int PICPastYear { get; set; }
            public int PICCategoryLastYear { get; set; }
            public int PICClassLastYear { get; set; }
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

        #region Actual milestones
        protected MilestoneItem miPIC { get; set; }
        protected MilestoneItem miPICPastYear { get; set; }
        protected MilestoneItem miPICCategoryPastYear { get; set; }
        protected MilestoneItem miPICClassPastYear { get; set; }
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
        #endregion

        public DPEBase(DPEThresholds dpet, CategoryClass.CatClassID ccid, string szFarRef, string baseFAR, string farLink)
        {
            if (dpet == null)
                throw new ArgumentNullException(nameof(dpet));
            BaseFAR = baseFAR;
            FARLink = farLink;
            string szResolved = ResolvedFAR(szFarRef);
            catClass = CategoryClass.CategoryClassFromID(ccid);
            string szCatClass = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, catClass.Category, catClass.Class);
            miPIC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTime, dpet.PIC), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PIC);
            miPICPastYear = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTimeLastYear, dpet.PICPastYear), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICPastYear);
            miPICCategoryPastYear= new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTimePastYear, dpet.PICCategoryLastYear, catClass.Category), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICCategoryLastYear);
            miPICClassPastYear = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.DPEPICTimePastYear, dpet.PICClassLastYear, catClass.CatClass), szResolved, string.Empty, MilestoneItem.MilestoneType.Time, dpet.PICClassLastYear);
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
                throw new ArgumentNullException(nameof(cfr));

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

            bool fIsCategorymatch = cfr.szCategory.CompareOrdinalIgnoreCase(catClass.Category) == 0;
            bool fIsCatClassMatch = cfr.idCatClassOverride == catClass.IdCatClass;

            if (cfr.dtFlight.CompareTo(DateTime.Now.AddYears(-1)) >= 0)
            {
                miPICPastYear.AddEvent(cfr.PIC);
                if (fIsCategorymatch)
                    miPICCategoryPastYear.AddEvent(cfr.PIC);
                if (fIsCatClassMatch)
                {
                    miPICFlightsInPriorYear.AddEvent(1);
                    miPICClassPastYear.AddEvent(cfr.PIC);
                }
            }

            if (fIsCategorymatch)
            {
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
        public DPEAirplaneBasePPL(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() { 
            PIC = 2000, 
            PICCategory = 1500, 
            PICClass = 500, 
            PICNight = 100,
            PICComplexTime = 200,
            PICCategoryLastYear = 100,
            CFICategory = 500, 
            CFIClass = 100 }, ccid, " Table 3-5", baseFAR800095D, href800095D)
        {
            Title = szTitle;
            GeneralDisclaimer = Resources.MilestoneProgress.DPEDisclaimerPreparing;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>()
                {
                    miPIC, miPICCategory, miPICClass, miPICNight, miPICComplexTime, miPICCategoryPastYear, miCFICategory, miCFIClass
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
        public DPEAMELPPL() : base(CategoryClass.CatClassID.AMEL, Resources.MilestoneProgress.TitleDPEPPLAMEL){ }
    }

    [Serializable]
    internal class DPEASESPPL : DPEAirplaneBasePPL
    {
        public DPEASESPPL() : base(CategoryClass.CatClassID.ASES, Resources.MilestoneProgress.TitleDPEPPLASES) { }
    }

    [Serializable]
    internal class DPEAMESPPL : DPEAirplaneBasePPL
    {
        public DPEAMESPPL() : base(CategoryClass.CatClassID.AMES, Resources.MilestoneProgress.TitleDPEPPLAMES) { }
    }
    #endregion

    #region Rotorcraft PPL
    [Serializable]
    internal abstract class DPERotorcraft800095DBase : DPEBase
    {
        public DPERotorcraft800095DBase(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds()
        {
            PIC = 2000,
            PICCategory = 500,
            PICClass = (ccid == CategoryClass.CatClassID.Gyroplane ? 150 : 250),
            PICCategoryLastYear = 100,
            CFIClass = ccid == CategoryClass.CatClassID.Gyroplane ? 200 : 250
        }, ccid, " Table 3-5", baseFAR800095D, href800095D)
        {
            GeneralDisclaimer = Resources.MilestoneProgress.DPERotorcraft800095DDisclaimer;
            Title = szTitle;
       }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICCategory, miPICClass, miPICCategoryPastYear, miCFIClass }; }
        }
    }

    [Serializable]
    internal class DPEHelicopter800095D : DPERotorcraft800095DBase
    {
        public DPEHelicopter800095D() : base(CategoryClass.CatClassID.Helicopter, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DHelicopter) { }
    }

    [Serializable]
    internal class DPEGyroplane800095D : DPERotorcraft800095DBase
    {
        public DPEGyroplane800095D() : base(CategoryClass.CatClassID.Gyroplane, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DGyroplane) { }
    }
    #endregion

    #region Glider PPL
    [Serializable]
    internal class DPEPPLGlider : DPEBase
    {
        internal DPEPPLGlider() : base(new DPEThresholds() {
            PIC = 500, 
            PICClass = 250, 
            PICClassLastYear = 20, 
            PICFlightsInClassPriorYear = 50, 
            CFIClass = 100 }, CategoryClass.CatClassID.Glider, " Table 3-5", baseFAR800095D, href800095D)
        {
            Title = Resources.MilestoneProgress.TitleDPEPPLGlider;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICClass, miPICClassPastYear, miPICFlightsInPriorYear, miCFIClass }; }
        }
    }
    #endregion
    #endregion

    #region DPE Commercial+Instrument classes
    #region Airplane Comm
    [Serializable]
    internal abstract class DPEAirplaneCommBase : DPEBase
    {
        public DPEAirplaneCommBase(CategoryClass.CatClassID ccid, string szTitle) : base(new DPEThresholds() 
        { 
            PIC = 2000, 
            PICCategory = 1500, 
            PICClass = 500, 
            PICNight = 100, 
            PICComplexTime = 200,
            PICInstrumentTime = 100,
            PICCategoryLastYear = 100,
            CFICategory = 500, 
            CFIClass= 100, 
            CFIITime = 250, 
            CFIITimeInCategory = 200 }, ccid, " Table 3-6", baseFAR800095D, href800095D)
        {
            Title = szTitle;
            GeneralDisclaimer = Resources.MilestoneProgress.DPECommInstrumentDisclaimer;
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICCategory, miPICClass, miPICNight, miPICComplexTime, miPICInstrumentTime, miPICCategoryPastYear, miCFICategory, miCFIClass, miCFIITime, miCFIITimeInCategory }; }
        }
    }

    [Serializable]
    internal class DPECommASEL : DPEAirplaneCommBase
    {
        internal DPECommASEL() : base(CategoryClass.CatClassID.ASEL, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DASEL) { }
    }

    [Serializable]
    internal class DPECommAMEL : DPEAirplaneCommBase
    {
        internal DPECommAMEL() : base(CategoryClass.CatClassID.AMEL, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DAMEL) { }
    }

    [Serializable]
    internal class DPECommASES : DPEAirplaneCommBase
    {
        internal DPECommASES() : base(CategoryClass.CatClassID.ASES, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DASES) { }
    }

    [Serializable]
    internal class DPECommAMES : DPEAirplaneCommBase
    {
        internal DPECommAMES() : base(CategoryClass.CatClassID.AMES, Resources.MilestoneProgress.TitleDPEPPLCOMM800095DAMES) { }
    }
    #endregion

    [Serializable]
    internal class DPECommHelicopter : DPEBase
    {
        internal DPECommHelicopter() : base(new DPEThresholds() { PIC = 2000, PICClass = 500, PICInstrumentTime = 100, PICPastYear = 100, CFIClass = 250, CFIITimeInCategory = 50 }, 
            CategoryClass.CatClassID.Helicopter, " Table 3-6", baseFAR800095D, href800095D)
        {
            Title = Resources.MilestoneProgress.TitleDPEHelicopter;
            GeneralDisclaimer = Resources.MilestoneProgress.DPECommInstrumentDisclaimer;
            miCFIITimeInCategory.Note = Branding.ReBrand(Resources.MilestoneProgress.DPEInstrumentInstructionDisclaimer);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miPIC, miPICClass, miPICInstrumentTime, miPICPastYear, miCFIClass, miCFIITimeInCategory }; }
        }
    }



    #endregion
    #endregion
}