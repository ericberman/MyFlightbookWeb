using MyFlightbook.Currency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    /// <summary>
    /// ATP milestones
    /// </summary>
    [Serializable]
    public class CFIMilestones : MilestoneGroup
    {
        public CFIMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupCFI;
        }

        public override Collection<MilestoneProgress> Milestones
        {
            get
            {
                return new Collection<MilestoneProgress> {
                new CFIASEL(),
                new CFIASES(),
                new CFIAMEL(),
                new CFIAMES(),
                new CFIGlider(),
                new CFIHelicopter(),
                new CFISportASEL()
                };
            }
        }
    }

    #region 61.183 - CFI
    [Serializable]
    public abstract class CFIBase : MilestoneProgress
    {
        const decimal CFIMinTime = 15.0M;

        protected MilestoneItem miPIC { get; set; }

        protected CategoryClass.CatClassID requiredCatClassID { get; set; }

        protected CFIBase(string title, CategoryClass.CatClassID ccid, RatingType rt)
        {
            Title = title;
            requiredCatClassID = ccid;
            BaseFAR = "61.183";
            RatingSought = rt;
            FARLink = "https://www.law.cornell.edu/cfr/text/14/61.183";

            miPIC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CFITimeInCategoryClass, CFIMinTime, CategoryClass.CategoryClassFromID(ccid).CatClass), ResolvedFAR("(j)"), string.Empty, MilestoneItem.MilestoneType.Time, CFIMinTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.idCatClassOverride == requiredCatClassID && cfr.fIsRealAircraft)
                miPIC.AddEvent(cfr.PIC);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() { miPIC };
            }
        }
    }

    #region concrete CFI classes
    [Serializable]
    public class CFIASEL : CFIBase
    {
        public CFIASEL() : base(Resources.MilestoneProgress.Title61183ASEL, CategoryClass.CatClassID.ASEL, RatingType.CFIASEL) { }
    }

    [Serializable]
    public class CFIASES : CFIBase
    {
        public CFIASES() : base(Resources.MilestoneProgress.Title61183ASES, CategoryClass.CatClassID.ASES, RatingType.CFIASES) { }
    }

    [Serializable]
    public class CFIAMEL : CFIBase
    {
        public CFIAMEL() : base(Resources.MilestoneProgress.Title61183AMEL, CategoryClass.CatClassID.AMEL, RatingType.CFIAMEL) { }
    }

    [Serializable]
    public class CFIAMES : CFIBase
    {
        public CFIAMES() : base(Resources.MilestoneProgress.Title61183AMES, CategoryClass.CatClassID.AMES, RatingType.CFIAMES) { }
    }

    [Serializable]
    public class CFIGlider : CFIBase
    {
        public CFIGlider() : base(Resources.MilestoneProgress.Title61183Glider, CategoryClass.CatClassID.Glider, RatingType.CFIGlider) { }
    }

    [Serializable]
    public class CFIHelicopter : CFIBase
    {
        public CFIHelicopter() : base(Resources.MilestoneProgress.Title61183Helicopter, CategoryClass.CatClassID.Helicopter, RatingType.CFIHelicopter) { }
    }
    #endregion
    #endregion

    #region 61.411 - CFI Sport
    [Serializable]
    public class CFISportASEL : MilestoneProgress
    {
        protected MilestoneItem miTotal { get; set; }
        protected MilestoneItem miPICPowered { get; set; }
        protected MilestoneItem miTotalSEL { get; set; }
        protected MilestoneItem miXC { get; set; }
        protected MilestoneItem miXCSEL { get; set; }

        public CFISportASEL()
        {
            Title = Resources.MilestoneProgress.Title61411CFISportSEL;
            BaseFAR = "61.411";
            FARLink = "https://www.law.cornell.edu/cfr/text/14/61.411";

            miTotal = new MilestoneItem(Resources.MilestoneProgress.CFISASELTotalTime, ResolvedFAR("(a)(1)"), string.Empty, MilestoneItem.MilestoneType.Time, 150);
            miPICPowered = new MilestoneItem(Resources.MilestoneProgress.CFISASELPICPowered, ResolvedFAR("(a)(1)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 100);
            miTotalSEL = new MilestoneItem(Resources.MilestoneProgress.CFISASELTotalSEL, ResolvedFAR("(a)(1)(ii)"), Branding.ReBrand(Resources.MilestoneProgress.NoteLSATime), MilestoneItem.MilestoneType.Time, 50);
            miXC = new MilestoneItem(Resources.MilestoneProgress.CFISASELXC, ResolvedFAR("(a)(1)(iii)"), string.Empty, MilestoneItem.MilestoneType.Time, 25);
            miXCSEL = new MilestoneItem(Resources.MilestoneProgress.CFISASELXCSEL, ResolvedFAR("(a)(1)(iv)"), string.Empty, MilestoneItem.MilestoneType.Time, 10);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsRealAircraft)
                return;

            miTotal.AddEvent(cfr.Total);
            if (CategoryClass.IsPowered(cfr.idCatClassOverride))
                miPICPowered.AddEvent(cfr.PIC);
            if (cfr.idCatClassOverride == CategoryClass.CatClassID.ASEL)
            {
                miTotalSEL.AddEvent(cfr.Total);
                miXCSEL.AddEvent(cfr.XC);
            }
            miXC.AddEvent(cfr.XC);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() { miTotal, miPICPowered, miTotalSEL, miXC, miXCSEL };
            }
        }
    }
    #endregion
}