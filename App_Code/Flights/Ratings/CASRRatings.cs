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
    #region Australian CASR Licensing - see https://www.comlaw.gov.au/Details/F2015C00762/Html/Volume_2#_Toc429575457). In this document go to volume 2, subsection 61 is everything on licences etc. The first part is mainly definitions . Subsections 61.g-z are where you can get all the info for requirements and restrictions for all the ratings/licences for Australia.
    [Serializable]
    public abstract class CASRLicenseBase : MilestoneProgress
    {
        protected MilestoneItem miAeronauticalExperience { get; set; }
        protected MilestoneItem miPilotTime { get; set; }
        protected MilestoneItem miTimeInCategory { get; set; }
        protected MilestoneItem miSoloTimeInCategory { get; set; }
        protected MilestoneItem miSoloXCTimeInCategory { get; set; }
        protected MilestoneItem miSoloLongCrossCountry { get; set; }
        protected MilestoneItem miDualInstrumentTime { get; set; }
        protected MilestoneItem miDualInstrumentTimeInCategory { get; set; }

        protected int reqAeronauticalExperience { get; set; }
        protected int reqPilotTime { get; set; }
        protected int reqTimeInCategory { get; set; }
        protected int reqSoloTimeInCategory { get; set; }
        protected int reqSoloXCTimeInCategory { get; set; }
        protected int reqXCDistance { get; set; }
        protected int reqDualInstrumentTime { get; set; }
        protected int reqDualInstrumentTimeInCategory { get; set; }
        protected bool fXCLandingsMustBeFullStop { get; set; }
        protected bool fLongCrossCountryMustBeSolo { get; set; }

        /// <summary>
        /// The required category for flight operations (e.g., "In an airplane")
        /// </summary>
        protected string CategoryRestriction { get; set; }

        protected CategoryClass.CatClassID requiredCategory { get; set; }

        protected Collection<MilestoneItem> m_milestones { get; set; }

        protected void Init()
        {
            InitRequiredParams();
            SetUpMilestones();
        }

        protected CASRLicenseBase(string szBaseFAR, string szTitle, RatingType rt, string szCategoryName)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            RatingSought = rt;
            CategoryRestriction = szCategoryName;

            m_milestones = new Collection<MilestoneItem>();
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return m_milestones;
            }
        }

        /// <summary>
        /// Sets up the parameters for the milestones (e.g., # of hours, etc.)
        /// </summary>
        protected abstract void InitRequiredParams();

        /// <summary>
        /// Sets up the milestones for this progress
        /// </summary>
        protected abstract void SetUpMilestones();

        /// <summary>
        /// Determines if the flight matches the required category for the milestone
        /// </summary>
        /// <param name="ccid">the categoryclass ID</param>
        /// <returns>True if it's a match</returns>
        protected abstract bool IsMatchingCategory(CategoryClass.CatClassID ccid);

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            decimal ifrTraining = Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim);

            // Aeronautical experience - can be in any aircraft or certified sim
            if (cfr.fIsCertifiedIFR)    // includes real aircraft
                miAeronauticalExperience.AddEvent(Math.Max(cfr.GroundSim, cfr.Total));

            // total pilot time and IFR time can both be in any real aircraft
            if (miPilotTime != null && cfr.fIsRealAircraft)
                miPilotTime.AddEvent(cfr.Total);
            miDualInstrumentTime.AddEvent((cfr.fIsRealAircraft || cfr.fIsCertifiedIFR) ? ifrTraining : 0);

            // everything else must be in a matching category AND must be in a real aircraft
            if (IsMatchingCategory(cfr.idCatClassOverride) && cfr.fIsRealAircraft)
            {
                decimal soloTime = 0.0M;
                cfr.FlightProps.ForEachEvent(pf =>
                {
                    if (pf.PropertyType.IsSolo)
                        soloTime += pf.DecValue;
                });

                miTimeInCategory.AddEvent(cfr.Total);
                miSoloTimeInCategory.AddEvent(soloTime);
                miSoloXCTimeInCategory.AddEvent(Math.Min(soloTime, cfr.XC));
                miDualInstrumentTimeInCategory.AddEvent(ifrTraining);

                bool fAllowLongXC = (soloTime > 0 || (!fLongCrossCountryMustBeSolo && cfr.PIC > 0));    // solo is always OK for cross country, otherwise need PIC.
                if (fAllowLongXC && !miSoloLongCrossCountry.IsSatisfied)
                {
                    AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                    int cRequiredLandings = fXCLandingsMustBeFullStop ? cfr.cFullStopLandings + cfr.cFullStopNightLandings : cfr.cLandingsThisFlight;

                    if (al.DistanceForRoute() >= reqXCDistance && al.GetAirportList().Length >= 3 && cRequiredLandings >= 2)
                        miSoloLongCrossCountry.MatchFlightEvent(cfr);
                }
            }
        }

        public static IEnumerable<MilestoneProgress> AvailableRatings
        {
            get
            {
                return new MilestoneProgress[]
                {
                    new CASRPrivatePilotAirplaneApprovedTraining(),
                    new CASRPrivatePilotAirplaneNoApprovedTraining(),
                    new CASRPrivatePilotHelicopterApprovedTraining(),
                    new CASRPrivatePilotHelicopterNoApprovedTraining()
                };
            }
        }
    }

    #region Concrete CASR Classes
    #region PPL
    [Serializable]
    public abstract class CASRPrivatePilot : CASRLicenseBase
    {
        protected override void InitRequiredParams()
        {
            reqAeronauticalExperience = 35;
            reqPilotTime = 30;
            reqTimeInCategory = 20;
            reqSoloTimeInCategory = 10;
            reqSoloXCTimeInCategory = 5;
            reqXCDistance = 150;
            reqDualInstrumentTime = 2;
            reqDualInstrumentTimeInCategory = 1;
            fXCLandingsMustBeFullStop = true;
            fLongCrossCountryMustBeSolo = true;
        }

        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid)
        {
            return CategoryClass.IsAirplane(ccid);
        }

        protected override void SetUpMilestones()
        {
            miAeronauticalExperience = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRAeronauticalExperience, reqAeronauticalExperience), ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, reqAeronauticalExperience);
            miPilotTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinTime, reqPilotTime), ResolvedFAR("(1)(a)"), string.Empty, MilestoneItem.MilestoneType.Time, reqPilotTime);
            miTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinTimeInCategory, reqTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(b)"), string.Empty, MilestoneItem.MilestoneType.Time, reqTimeInCategory);
            miSoloTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinSoloTimeInCategory, reqSoloTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(c)"), string.Empty, MilestoneItem.MilestoneType.Time, reqSoloTimeInCategory);
            miSoloXCTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinSoloXCTimeInCategory, reqSoloXCTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(d)"), string.Empty, MilestoneItem.MilestoneType.Time, reqSoloXCTimeInCategory);
            miSoloLongCrossCountry = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLSoloLongCrossCountry, CategoryRestriction, reqXCDistance, fXCLandingsMustBeFullStop ? Resources.MilestoneProgress.CASRFullStopLandings : Resources.MilestoneProgress.CASRLandings), ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miDualInstrumentTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLDualIFR, reqDualInstrumentTime), ResolvedFAR("(1)(e)"), string.Empty, MilestoneItem.MilestoneType.Time, reqDualInstrumentTime);
            miDualInstrumentTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLDualIFRInCategory, CategoryRestriction), ResolvedFAR("(1)(f)"), string.Empty, MilestoneItem.MilestoneType.Time, reqDualInstrumentTimeInCategory);

            m_milestones = new Collection<MilestoneItem>() { miAeronauticalExperience, miPilotTime, miTimeInCategory, miSoloTimeInCategory, miSoloXCTimeInCategory, miSoloLongCrossCountry, miDualInstrumentTime, miDualInstrumentTimeInCategory };
        }

        protected CASRPrivatePilot(string szBaseFar, string szTitle, RatingType rt, string szCategory) : base(szBaseFar, szTitle, rt, szCategory)
        {
            Init();
        }
    }

    [Serializable]
    public class CASRPrivatePilotAirplaneApprovedTraining : CASRPrivatePilot
    {
        public CASRPrivatePilotAirplaneApprovedTraining() : base("61.H.2 - 61.525", Resources.MilestoneProgress.TitleCASRPPLAirplaneTraining, RatingType.CASRAirplaneWithCourse, Resources.MilestoneProgress.CASRCategoryAirplane) { }
    }

    [Serializable]
    public class CASRPrivatePilotAirplaneNoApprovedTraining : CASRPrivatePilot
    {
        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqPilotTime = 35;
            reqAeronauticalExperience = 40;
        }

        public CASRPrivatePilotAirplaneNoApprovedTraining()
            : base("61.H.3 - 61.545", Resources.MilestoneProgress.TitleCASRPPLAirplaneNoTraining, RatingType.CASRAirplaneWithoutCourse, Resources.MilestoneProgress.CASRCategoryAirplane) { }
    }

    [Serializable]
    public class CASRPrivatePilotHelicopterNoApprovedTraining : CASRPrivatePilot
    {
        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqPilotTime = 35;
            reqAeronauticalExperience = 40;
            reqTimeInCategory = 30;
            reqXCDistance = 100;
            fXCLandingsMustBeFullStop = false;
        }

        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid)
        {
            return ccid == CategoryClass.CatClassID.Helicopter;
        }

        public CASRPrivatePilotHelicopterNoApprovedTraining()
            : base("61.H.3 - 61.550", Resources.MilestoneProgress.TitleCASRPPLHeliNoTraining, RatingType.CASRHelicopterWithoutCourse, Resources.MilestoneProgress.CASRCategoryHelicopter) { }
    }

    [Serializable]
    public class CASRPrivatePilotHelicopterApprovedTraining : CASRLicenseBase
    {
        protected override void InitRequiredParams()
        {
            reqAeronauticalExperience = 35;
            reqPilotTime = 30;
            reqTimeInCategory = 30;
            reqSoloTimeInCategory = 10;
            reqSoloXCTimeInCategory = 5;
            reqXCDistance = 100;
            reqDualInstrumentTime = 2;
            reqDualInstrumentTimeInCategory = 1;
            fXCLandingsMustBeFullStop = false;
            fLongCrossCountryMustBeSolo = true;
        }

        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid)
        {
            return ccid == CategoryClass.CatClassID.Helicopter;
        }

        protected override void SetUpMilestones()
        {
            miAeronauticalExperience = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRAeronauticalExperience, reqAeronauticalExperience), ResolvedFAR("(1)"), string.Empty, MilestoneItem.MilestoneType.Time, reqAeronauticalExperience);
            miTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinTimeInCategory, reqTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(a)"), string.Empty, MilestoneItem.MilestoneType.Time, reqTimeInCategory);
            miSoloTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinSoloTimeInCategory, reqSoloTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(b)"), string.Empty, MilestoneItem.MilestoneType.Time, reqSoloTimeInCategory);
            miSoloXCTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLMinSoloXCTimeInCategory, reqSoloXCTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(c)"), string.Empty, MilestoneItem.MilestoneType.Time, reqSoloXCTimeInCategory);
            miSoloLongCrossCountry = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLSoloLongCrossCountry, CategoryRestriction, reqXCDistance, fXCLandingsMustBeFullStop ? Resources.MilestoneProgress.CASRFullStopLandings : Resources.MilestoneProgress.CASRLandings), ResolvedFAR("(3)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miDualInstrumentTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLDualIFR, reqDualInstrumentTime), ResolvedFAR("(1)(d)"), string.Empty, MilestoneItem.MilestoneType.Time, reqDualInstrumentTime);
            miDualInstrumentTimeInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRPPLDualIFRInCategory, CategoryRestriction), ResolvedFAR("(1)(e)"), string.Empty, MilestoneItem.MilestoneType.Time, reqDualInstrumentTimeInCategory);

            m_milestones = new Collection<MilestoneItem>() { miAeronauticalExperience, miTimeInCategory, miSoloTimeInCategory, miSoloXCTimeInCategory, miSoloLongCrossCountry, miDualInstrumentTime, miDualInstrumentTimeInCategory };
        }

        public CASRPrivatePilotHelicopterApprovedTraining() : base("61.H.2 - 61.530", Resources.MilestoneProgress.TitleCASRPPLHeliTraining, RatingType.CASRHelicopterWithCourse, Resources.MilestoneProgress.CASRCategoryHelicopter)
        {
            Init();
        }
    }
    #endregion // PPL

    #region Commercial
    [Serializable]
    public abstract class CASRCommercialBase : CASRPrivatePilot
    {
        protected MilestoneItem miPICTime { get; set; }
        protected MilestoneItem miPICXCTime { get; set; }
        protected MilestoneItem miInstrumentTime { get; set; }
        protected MilestoneItem miInstrumentInCategory { get; set; }

        protected int reqPICTime { get; set; }
        protected int reqPICXCTime { get; set; }
        protected int reqIFRTime { get; set; }
        protected int reqIFRTimeInCategory { get; set; }

        protected CASRCommercialBase(string szBaseFAR, string szTitle, RatingType rt, string szCategoryName) : base(szBaseFAR, szTitle, rt, szCategoryName) { }

        protected override void SetUpMilestones()
        {
            base.SetUpMilestones();
            miPICTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRCommPIC, reqPICTime, CategoryRestriction), ResolvedFAR("(1)(b)"), string.Empty, MilestoneItem.MilestoneType.Time, reqPICTime);
            miPICXCTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRCommPICXC, reqPICXCTime, CategoryRestriction), ResolvedFAR("(1)(c)"), string.Empty, MilestoneItem.MilestoneType.Time, reqPICXCTime);
            miInstrumentTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRCommIFR, reqIFRTime, CategoryRestriction), ResolvedFAR("(1)(d)"), string.Empty, MilestoneItem.MilestoneType.Time, reqIFRTime);
            miInstrumentInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRCommIFRInCategory, reqIFRTimeInCategory, CategoryRestriction), ResolvedFAR("(1)(e)"), string.Empty, MilestoneItem.MilestoneType.Time, reqIFRTimeInCategory);
            miSoloLongCrossCountry.FARRef = ResolvedFAR("(3)");
            miSoloLongCrossCountry.Title = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CASRCommLongCrossCountry, CategoryRestriction, reqXCDistance, fXCLandingsMustBeFullStop ? Resources.MilestoneProgress.CASRFullStopLandings : Resources.MilestoneProgress.CASRLandings);

            m_milestones = new Collection<MilestoneItem>() { miAeronauticalExperience, miTimeInCategory, miPICTime, miPICXCTime, miSoloLongCrossCountry, miInstrumentTime, miInstrumentInCategory };
        }

        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            fLongCrossCountryMustBeSolo = false;
            reqIFRTime = 10;
            reqIFRTimeInCategory = 5;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            base.ExamineFlight(cfr);

            decimal ifrTime = cfr.IMCSim + cfr.IMC;

            if (cfr.fIsRealAircraft || cfr.fIsCertifiedIFR)
                miInstrumentTime.AddEvent(ifrTime);

            if (cfr.fIsRealAircraft && IsMatchingCategory(cfr.idCatClassOverride))
            {
                miPICTime.AddEvent(cfr.PIC);
                miPICXCTime.AddEvent(Math.Min(cfr.PIC, cfr.XC));
                miInstrumentInCategory.AddEvent(ifrTime);
            }
        }
    }

    [Serializable]
    public class CASRCommAirplaneApprovedTraining : CASRCommercialBase
    {
        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid) { return CategoryClass.IsAirplane(ccid); }

        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqAeronauticalExperience = 150;
            reqTimeInCategory = 140;
            reqPICTime = 70;
            reqPICXCTime = 20;
            reqXCDistance = 300;
            fXCLandingsMustBeFullStop = true;
        }

        public CASRCommAirplaneApprovedTraining() : base("61.I.2 - 61.590", Resources.MilestoneProgress.TitleCASRCommAirplaneTraining, RatingType.CASRCommAirplaneWithCourse, Resources.MilestoneProgress.CASRCategoryAirplane) { }
    }

    [Serializable]
    public class CASRCommAirplaneNoApprovedTraining : CASRCommercialBase
    {
        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid) { return CategoryClass.IsAirplane(ccid); }

        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqAeronauticalExperience = 200;
            reqPilotTime = 190;
            reqPICTime = 100;
            reqPICXCTime = 20;
            reqXCDistance = 300;
            fXCLandingsMustBeFullStop = true;
        }

        protected override void SetUpMilestones()
        {
            base.SetUpMilestones();

            m_milestones = new Collection<MilestoneItem>() { miAeronauticalExperience, miPilotTime, miPICTime, miPICXCTime, miSoloLongCrossCountry, miInstrumentTime, miInstrumentInCategory };
        }

        public CASRCommAirplaneNoApprovedTraining() : base("61.I.3 - 61.610", Resources.MilestoneProgress.TitleCASRCommAirplaneNoTraining, RatingType.CASRCommAirplaneWithoutCourse, Resources.MilestoneProgress.CASRCategoryAirplane) { }
    }

    [Serializable]
    public class CASRCommHelicopterApprovedTraining : CASRCommercialBase
    {
        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid) { return ccid == CategoryClass.CatClassID.Helicopter; }

        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqAeronauticalExperience = 100;
            reqTimeInCategory = 90;
            reqPICTime = 35;
            reqPICXCTime = 10;
            reqXCDistance = 150;
            fXCLandingsMustBeFullStop = false;
        }

        public CASRCommHelicopterApprovedTraining() : base("61.I.2 - 61.595", Resources.MilestoneProgress.TitleCASRCommHeliTraining, RatingType.CASRCommHelicopterWithCourse, Resources.MilestoneProgress.CASRCategoryHelicopter) { }
    }

    [Serializable]
    public class CASRCommHelicopterNoApprovedTraining : CASRCommercialBase
    {
        protected override bool IsMatchingCategory(CategoryClass.CatClassID ccid) { return ccid == CategoryClass.CatClassID.Helicopter; }

        protected override void InitRequiredParams()
        {
            base.InitRequiredParams();
            reqAeronauticalExperience = 150;
            reqPilotTime = 140;
            reqTimeInCategory = 70;
            reqPICTime = 35;
            reqPICXCTime = 10;
            reqXCDistance = 150;
            fXCLandingsMustBeFullStop = false;
        }

        protected override void SetUpMilestones()
        {
            base.SetUpMilestones();

            // fix up the farrefs
            miPilotTime.FARRef = ResolvedFAR("(1)(a)");
            miTimeInCategory.FARRef = ResolvedFAR("(1)(b)");
            miPICTime.FARRef = ResolvedFAR("(1)(c)");
            miPICXCTime.FARRef = ResolvedFAR("(1)(d)");
            miInstrumentTime.FARRef = ResolvedFAR("(1)(e)");
            miInstrumentInCategory.FARRef = ResolvedFAR("(1)(f)");

            m_milestones = new Collection<MilestoneItem>() { miAeronauticalExperience, miPilotTime, miTimeInCategory, miPICTime, miPICXCTime, miSoloLongCrossCountry, miInstrumentTime, miInstrumentInCategory };
        }

        public CASRCommHelicopterNoApprovedTraining() : base("61.I.3 - 61.615", Resources.MilestoneProgress.TitleCASRCommHeliNoTraining, RatingType.CASRCommHelicopterWithoutCourse, Resources.MilestoneProgress.CASRCategoryHelicopter) { }
    }
    #endregion
    #endregion // Concrete CASR classes
    #endregion // Australian CASR
}