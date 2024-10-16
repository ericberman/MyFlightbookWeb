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
    /// IFR milestones
    /// </summary>
    [Serializable]
    public class InstrumentMilestones : MilestoneGroup
    {
        public InstrumentMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupInstrument;
        }

        public override Collection<MilestoneProgress> Milestones
        {
            get
            {
                return new Collection<MilestoneProgress> {
                    new IFR6165D(),
                    new IFR6165E(),
                    new IFR6165F(),
                    new IFR6165D142(),
                    new IFR6165E142(),
                    new IFR6165F142(),
                    new IFR141CAirplane(),
                    new IFR141CHelicopter(),
                    new IFRCanadaAirplane(),
                    new IFRCanadaHelicopter(),
                    new IFRFCL610Airplane(),
                    new IFRUKIRRestricted()
                };
            }
        }
    }

    #region 61.65 - Instrument Ratings
    /// <summary>
    /// 61.65 - Instrument rating
    /// </summary>
    [Serializable]
    public abstract class IFR6165Base : MilestoneProgress
    {
        protected MilestoneItemXC miMinXCTime { get; set; }
        protected MilestoneItemXC miMinTimeInCategory { get; set; }
        protected MilestoneItem miMinIMCTime { get; set; }
        protected MilestoneItemDecayable miMinIMCTestPrep { get; set; }
        protected MilestoneItem miIMCXC { get; set; }

        protected MilestoneItem miAATDIMCTime { get; set; }

        protected MilestoneItem miBATDIMCTime { get; set; }

        protected MilestoneItem miFTDFFSTime { get; set; }

        protected MilestoneItem miInstrumentTraining { get; set; }

        // Per 61.65(h), can count 20 or 30 (part 142) hours of FTD time
        // We use 20 hours unless we know it's part 142
        // Per 61.65(i), we can count 10 of BATD time or 20 hours of AATD time
        // We don't generally distinguish BATD from ATD, so as a hack We look for "AATD" in the flight remarks OR in the model name; 
        // if it's a match, we add to the AATD time, otherwise, we add to BATD time.
        // These limits seem additive, though (can do both FTD and ATD), but the ATD limit appears to be mutually exclusive (10 hours of BATD XOR 20 hours of AATD)
        // BUT...per 61.65(j), ATD+FTD time can - at MOST - equal 20 (30, for part 142) hours.  So 10+10 or 5+15 is OK, but 5+20 is not.
        protected int MaxFTDTime { get; set; } = 20;

        protected const int MaxSimTime = 20;

        protected int MaxBATDTime { get; set; } = 10;

        protected int MaxAATDTime { get; set; } = 20;

        static readonly LazyRegex regAATD = new LazyRegex("\\bAATD\\b", true);

        protected int MinXCDistance { get; set; } = 250;

        /// <summary>
        /// Initializes an instrument rating object
        /// </summary>
        /// <param name="szTitle">Title</param>
        /// <param name="szBaseFAR">Base FAR</param>
        /// <param name="ratingSought">Specific rating being sought</param>
        protected virtual void Init(string szTitle, string szBaseFAR, RatingType ratingSought, string farLink)
        {
            Title = szTitle;
            BaseFAR = szBaseFAR;
            FARLink = farLink;
            RatingSought = ratingSought;

            string szAircraftCategory;

            switch (RatingSought)
            {
                case RatingType.InstrumentAirplane:
                case RatingType.InstrumentAirplaneCA:
                    szAircraftCategory = Resources.MilestoneProgress.ratingAirplane;
                    break;
                case RatingType.InstrumentHelicopter:
                case RatingType.InstrumentHelicopterCA:
                    szAircraftCategory = Resources.MilestoneProgress.ratingHelicopter;
                    break;
                case RatingType.InstrumentPoweredLift:
                    szAircraftCategory = Resources.MilestoneProgress.ratingPoweredLift;
                    break;
                default:
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Unallowed rating for 61.65: {0}", RatingSought.ToString()));
            }

            // 61.65(def)(1)
            miMinXCTime = new MilestoneItemXC(Resources.MilestoneProgress.MinInstrumentPICXC, ResolvedFAR("(1)"), Resources.MilestoneProgress.NoteXCTime, MilestoneItem.MilestoneType.Time, 50.0M);
            miMinTimeInCategory = new MilestoneItemXC(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinInstrumentPICInCategory, szAircraftCategory), ResolvedFAR("(1)"), String.Empty, MilestoneItem.MilestoneType.Time, 10.0M);

            // 61.65(def)(2) - total instrument time
            miMinIMCTime = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTime, ResolvedFAR("(2)"), Branding.ReBrand(Resources.MilestoneProgress.NoteInstrumentTime), MilestoneItem.MilestoneType.Time, 40.0M);

            // 61.65(h)-(j) allow for sim time to contribute; we'll tally these individually and then apply them when we're done
            miAATDIMCTime = new MilestoneItem(string.Empty, string.Empty, string.Empty, MilestoneItem.MilestoneType.Time, 0);
            miBATDIMCTime = new MilestoneItem(string.Empty, string.Empty, string.Empty, MilestoneItem.MilestoneType.Time, 0);
            miFTDFFSTime = new MilestoneItem(string.Empty, string.Empty, string.Empty, MilestoneItem.MilestoneType.Time, 0);

            // 61.65(def)(2) - Instrument training
            miInstrumentTraining = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTraining, ResolvedFAR("(2)"), Branding.ReBrand(Resources.MilestoneProgress.MinInstrumentTrainingNote), MilestoneItem.MilestoneType.Time, 15.0M);

            // 61.65(def)(2)(i) - recent test prep
            miMinIMCTestPrep = new MilestoneItemDecayable(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinInstrumentTestPrep, szAircraftCategory), ResolvedFAR("(2)(i)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M, 2);

            // 61.65(def)(2)(ii) - cross-country
            miIMCXC = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinInstrumentXC, szAircraftCategory, MinXCDistance), ResolvedFAR("(2)(ii)"), Branding.ReBrand(Resources.MilestoneProgress.NoteInstrumentXC), MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
        }

        protected IFR6165Base() { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            decimal IMCTime = cfr.IMC + cfr.IMCSim;

            if (!cfr.fIsRealAircraft)
            {
                if (cfr.fIsFTD)
                    miFTDFFSTime.AddTrainingEvent(IMCTime, MaxFTDTime, true);
                else if (cfr.fIsATD)
                {
                    bool fIsAATD = regAATD.IsMatch(cfr.Comments + " " + cfr.szModel);

                    if (fIsAATD)
                        miAATDIMCTime.AddTrainingEvent(IMCTime, MaxAATDTime, true);
                    else
                        miBATDIMCTime.AddTrainingEvent(IMCTime, MaxBATDTime, true);
                }
                return;
            }

            // Everything past this point only applies to real aircraft

            AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route);
            double distanceFromStart = al.MaxDistanceFromStartingAirport();
            decimal xc = distanceFromStart > MinXCDistanceForRating() ? cfr.XC : 0;
            bool IsInMatchingCategory = CatClassMatchesRatingSought(cfr.idCatClassOverride);
            decimal XCPICTime = Math.Min(cfr.PIC, xc);
            decimal IMCXCTime = Math.Min(IMCTime, xc);

            // 61.65(def)(1) - Look for cross-country time as PIC
            miMinXCTime.AddEvent(XCPICTime);
            if (IsInMatchingCategory)
                miMinTimeInCategory.AddEvent(XCPICTime);

            // Determine ignored flights
            if (XCPICTime < Math.Min(cfr.PIC, cfr.XC))
            {
                miMinXCTime.AddIgnoredFlight(cfr);
                miMinTimeInCategory.AddIgnoredFlight(cfr);
            }

            // 61.65(def)(2) - IMC time (total)
            miMinIMCTime.AddEvent(IMCTime);

            decimal instTrainingTime = Math.Min(cfr.Dual, IMCTime);
            miInstrumentTraining.AddEvent(instTrainingTime);

            // 61.65(def)(2)(i) - recent test prep
            if (IsInMatchingCategory)
            {
                miMinIMCTestPrep.AddDecayableEvent(cfr.dtFlight, instTrainingTime, DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0);

                if (cfr.cApproaches >= 3 && IMCXCTime > 0.0M && instTrainingTime > 0)
                {
                    if (al.DistanceForRoute() >= MinXCDistance)
                    {
                        miIMCXC.AddEvent(1.0M);
                        miIMCXC.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                        miIMCXC.MatchingEventID = cfr.flightID;
                    }
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get 
            {
                // Implement 61.65(h)-(j): allow up to 20 hours of FTD/FFS time and up to (10 hours of BATD time OR 20 hours of AATD time), with a max of 20 hours overall (30 if FTD under part 142.
                // I.e., miMinIMCTime below currently has ONLY real aircraft.  AATD/BATD/FTD time is already limited in each of those milestones.
                // So we are going to add MAX(MIN(MaxSimTime, FTD time + MAX(BATD time, AATD time), MIN(MaxFTDTime, FTD Time)).  
                // The first term provides the overall 20 hour limit, while the 2nd term - if MaxFTD Time is higher than MaxSimTime - allows for the part 142 exception


                decimal simTimeToCredit = Math.Max(
                    Math.Min(MaxSimTime, miFTDFFSTime.Progress + miAATDIMCTime.Progress + miBATDIMCTime.Progress),           // limits atd time + ftd time to 20hrs
                    Math.Min(MaxFTDTime, miFTDFFSTime.Progress));                                                          // if maxFTD time is > 20, allows that FTD time to be applied.

                // Don't pollute the IMC time (keep this getter idempotent), so add the sim time to actual time in a "netIMC" object
                MilestoneItem miNetIMC = new MilestoneItem(miMinIMCTime);
                miNetIMC.AddEvent(simTimeToCredit);
                return new Collection<MilestoneItem>() { miMinXCTime, miMinTimeInCategory, miNetIMC, miInstrumentTraining, miMinIMCTestPrep, miIMCXC }; 
            }
        }
    }

    #region Concrete IFR Classes
    /// <summary>
    /// 61.65(d) - Instrument Airplane
    /// </summary>
    [Serializable]
    public class IFR6165D : IFR6165Base
    {
        public IFR6165D() { Init(Resources.MilestoneProgress.Title6165D, "61.65(d)", RatingType.InstrumentAirplane, "https://www.law.cornell.edu/cfr/text/14/61.65"); }
    }

    /// <summary>
    /// 61.65(e) - Instrument Helicopter
    /// </summary>
    [Serializable]
    public class IFR6165E : IFR6165Base
    {
        public IFR6165E() { MinXCDistance = 100; Init(Resources.MilestoneProgress.Title6165E, "61.65(e)", RatingType.InstrumentHelicopter, "https://www.law.cornell.edu/cfr/text/14/61.65"); }
    }

    /// <summary>
    /// 61.65(f) - Instrument Powered Lift
    /// </summary>
    [Serializable]
    public class IFR6165F : IFR6165Base
    {
        public IFR6165F() { Init(Resources.MilestoneProgress.Title6165F, "61.65(f)", RatingType.InstrumentPoweredLift, "https://www.law.cornell.edu/cfr/text/14/61.65"); }
    }

    [Serializable]
    public class IFR6165D142 : IFR6165D
    {
        public IFR6165D142()
        {
            MaxFTDTime = 30;
            Init(Resources.MilestoneProgress.Title6165DPart142, "61.65(d)", RatingType.InstrumentAirplane, "https://www.law.cornell.edu/cfr/text/14/61.65");
        }
    }

    [Serializable]
    public class IFR6165E142 : IFR6165E
    {
        public IFR6165E142()
        {
            MaxFTDTime = 30;
            Init(Resources.MilestoneProgress.Title6165EPart142, "61.65(e)", RatingType.InstrumentAirplane, "https://www.law.cornell.edu/cfr/text/14/61.65");
        }
    }

    [Serializable]
    public class IFR6165F142 : IFR6165F
    {
        public IFR6165F142()
        {
            MaxFTDTime = 30;
            Init(Resources.MilestoneProgress.Title6165FPart142, "61.65(f)", RatingType.InstrumentAirplane, "https://www.law.cornell.edu/cfr/text/14/61.65");
        }
    }

    [Serializable]
    public abstract class IFRCanadaBase : IFR6165Base
    {
        protected MilestoneItem miXCPIC { get; set; }
        protected MilestoneItem miXCDualXC { get; set; }

        protected MilestoneItem miIFRTrainingInCategory { get; set; }

        protected override void Init(string szTitle, string szBaseFAR, RatingType ratingSought, string farLink)
        {
            MaxBATDTime = MaxAATDTime = 20;    // can do 20 hours of FTD OR ATD time.
            MinXCDistance = 100;

            base.Init(szTitle, szBaseFAR, ratingSought, farLink);

            miXCPIC = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentPICXC, ResolvedFAR("i"), Resources.MilestoneProgress.NoteCanadaIRXC, MilestoneItem.MilestoneType.Time, 50.0M);
            miXCDualXC = new MilestoneItem(String.Format(CultureInfo.InvariantCulture, Resources.MilestoneProgress.MinInstrumentXCCanada, MinXCDistance), ResolvedFAR("(ii)(D)"), Resources.MilestoneProgress.MinInstrumentXCCanadaNote, MilestoneItem.MilestoneType.AchieveOnce, 1.0M);
            miIFRTrainingInCategory = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MinInstrumentTrainingInCategoryCanada, ratingSought == RatingType.InstrumentAirplaneCA ? Resources.MilestoneProgress.CAATPAeroplanes : Resources.MilestoneProgress.CAATPHelicopters), ResolvedFAR("(ii)(B)"), string.Empty, MilestoneItem.MilestoneType.Time, 5.0M);

            // fix up FAR references for inherited milestones
            miMinTimeInCategory.FARRef = ResolvedFAR("(i)");
            miMinIMCTime.FARRef = ResolvedFAR("(ii)");
            miInstrumentTraining.FARRef = ResolvedFAR("(ii)(C)");
            miInstrumentTraining.Title = Resources.MilestoneProgress.MinInstrumentTrainingNonUS;

            this.GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.InstrumentCanadaGeneralDisclaimer);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            base.ExamineFlight(cfr);

            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsRealAircraft)
                return;

            switch (cfr.idCatClassOverride)
            {
                case CategoryClass.CatClassID.Helicopter:
                case CategoryClass.CatClassID.ASEL:
                case CategoryClass.CatClassID.ASES:
                case CategoryClass.CatClassID.AMEL:
                case CategoryClass.CatClassID.AMES:
                    miXCPIC.AddEvent(Math.Min(cfr.PIC, cfr.XC));
                    break;
                default:
                    break;
            }

            if (cfr.IMC + cfr.IMCSim > 0 && cfr.Dual > 0 && CatClassMatchesRatingSought(cfr.idCatClassOverride))
                miIFRTrainingInCategory.AddEvent(Math.Min(cfr.Dual, cfr.IMC + cfr.IMCSim));

            if (cfr.cApproaches >= 2 && cfr.Dual > 0 && cfr.IMC + cfr.IMCSim > 0)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route);
                if (al.DistanceForRoute() >= MinXCDistance)
                {
                    miXCDualXC.AddEvent(1.0M);
                    miXCDualXC.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                    miXCDualXC.MatchingEventID = cfr.flightID;
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get 
            {
                // miMinIMCTime below currently has ONLY real aircraft.  AATD/BATD/FTD time is already limited in each of those milestones.
                // So we are going to add MIN(Allowed Sim Time, FTD time + BATD time, AATD time).

                decimal allowedSimTime = Math.Min(MaxSimTime, miFTDFFSTime.Progress + miBATDIMCTime.Progress + miAATDIMCTime.Progress);
                // Don't pollute the IMC time (keep this getter idempotent), so add the sim time to actual time in a "netIMC" object
                MilestoneItem miNetIMC = new MilestoneItem(miMinIMCTime);
                miNetIMC.AddEvent(allowedSimTime);
                return new Collection<MilestoneItem>() { miXCPIC, miMinTimeInCategory, miNetIMC, miIFRTrainingInCategory, miInstrumentTraining, miXCDualXC }; 
            }
        }
    }

    /// <summary>
    /// 421.46(2)(b) - Instrument airplane (Canada)
    /// Essentially the same as 61.65, but a few minor differences
    /// </summary>
    [Serializable]
    public class IFRCanadaAirplane : IFRCanadaBase
    {
        public IFRCanadaAirplane() : base()
        {
            Init(Resources.MilestoneProgress.Title42146Airplane, "421.46(2)(b)", RatingType.InstrumentAirplaneCA, "https://tc.canada.ca/en/corporate-services/acts-regulations/list-regulations/canadian-aviation-regulations-sor-96-433/standards/standard-421-flight-crew-permits-licences-ratings-canadian-aviation-regulations-cars#421_46");
        }
    }

    /// <summary>
    /// 421.46(2)(b) - Instrument airplane (Canada)
    /// Essentially the same as 61.65, but a few minor differences
    /// </summary>
    [Serializable]
    public class IFRCanadaHelicopter : IFRCanadaBase
    {
        public IFRCanadaHelicopter() : base()
        {
            Init(Resources.MilestoneProgress.Title42146Helicopter, "421.46(2)(b)", RatingType.InstrumentHelicopterCA, "https://tc.canada.ca/en/corporate-services/acts-regulations/list-regulations/canadian-aviation-regulations-sor-96-433/standards/standard-421-flight-crew-permits-licences-ratings-canadian-aviation-regulations-cars#421_46");
        }
    }

    [Serializable]
    public abstract class IFR141Base : IFR6165Base
    {
        private readonly MilestoneItem miIMCAircraftTime, miIMCFSTime, miIMCFTDTime, miIMCATDTime;

        const decimal totalIMCTime = 35.0M;
        const decimal maxIMCFSTime = totalIMCTime * 0.5M;
        const decimal maxIMCFTDTime = totalIMCTime * 0.4M;
        const decimal maxIMCATDTime = totalIMCTime * 0.25M;
        const decimal maxIMCTrainingDeviceTime = totalIMCTime * 0.5M;

        protected IFR141Base(string szTitle, string szBaseFAR, RatingType ratingSought, int minXCDistance = 250)
        {
            MinXCDistance = minXCDistance;
            Init(szTitle, szBaseFAR, ratingSought, "https://www.law.cornell.edu/cfr/text/14/appendix-C_to_part_141");

            miIMCAircraftTime = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTime141, ResolvedFAR("(a)(1)"), Resources.MilestoneProgress.Note141InstrumentReducedHours, MilestoneItem.MilestoneType.Time, totalIMCTime);
            miIMCFSTime = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTime141, ResolvedFAR("(b)(1)"), Resources.MilestoneProgress.Note141InstrumentReducedHours, MilestoneItem.MilestoneType.Time, maxIMCFSTime);
            miIMCFTDTime = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTime141, ResolvedFAR("(b)(2)"), Resources.MilestoneProgress.Note141InstrumentReducedHours, MilestoneItem.MilestoneType.Time, maxIMCFTDTime);
            miIMCATDTime = new MilestoneItem(Resources.MilestoneProgress.MinInstrumentTime141, ResolvedFAR("(b)(3)"), Resources.MilestoneProgress.Note141InstrumentReducedHours, MilestoneItem.MilestoneType.Time, maxIMCATDTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            base.ExamineFlight(cfr);    // really only picks up the cross-country flight

            if (cfr.fIsCertifiedIFR && CatClassMatchesRatingSought(cfr.idCatClassOverride))
            {
                decimal IMCTime = cfr.IMC + cfr.IMCSim;
                if (cfr.fIsRealAircraft)
                    miIMCAircraftTime.AddEvent(IMCTime);
                else if (cfr.fIsCertifiedIFR)
                {
                    if (cfr.fIsCertifiedLanding)
                        miIMCFSTime.AddEvent(IMCTime);
                    else if (cfr.fIsFTD)
                        miIMCFTDTime.AddEvent(IMCTime);
                    else
                        miIMCATDTime.AddEvent(IMCTime);
                }
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                // Adjust for maximum training time.
                decimal allowedFSTime = Math.Min(miIMCFSTime.Threshold, miIMCFSTime.Progress);
                decimal allowedFTDTime = Math.Min(miIMCFTDTime.Threshold, miIMCFTDTime.Progress);
                decimal allowedATDTime = Math.Min(miIMCATDTime.Threshold, miIMCATDTime.Progress);
                decimal allowedSimTime = Math.Min(maxIMCTrainingDeviceTime, allowedFSTime + allowedFTDTime + allowedATDTime);
                miIMCAircraftTime.AddEvent(allowedSimTime);

                return new Collection<MilestoneItem>() { miIMCAircraftTime, miIMCXC };
            }
        }
    }

    [Serializable]
    public class IFR141CAirplane : IFR141Base
    {
        public IFR141CAirplane() : base(Resources.MilestoneProgress.Title141IFRAirplane, "Part 141, Appendix (C)(4)", RatingType.InstrumentAirplane)
        {
            miIMCXC.FARRef = ResolvedFAR("(c)(1)");
        }
    }

    [Serializable]
    public class IFR141CHelicopter : IFR141Base
    {
        public IFR141CHelicopter() : base(Resources.MilestoneProgress.Title141IFRHelicopter, "Part 141, Appendix (C)(4)", RatingType.InstrumentHelicopter, 100)
        {
            miIMCXC.FARRef = ResolvedFAR("(c)(2)");
        }
    }
    #endregion
    #endregion

    #region EASA/UK IR ratings
    /// <summary>
    /// Implements FCL.610 - EASA's IR rating for airplanes.  See https://www.easa.europa.eu/sites/default/files/dfu/Part-FCL.pdf
    /// </summary>
    public class IFRFCL610Airplane : MilestoneProgress
    {
        protected MilestoneItem miMinXCPICTime { get; set; }
        protected MilestoneItem miMinXCPICTimeInCategory { get; set; }

        public override Collection<MilestoneItem> Milestones => new Collection<MilestoneItem>() { miMinXCPICTime, miMinXCPICTimeInCategory };

        public IFRFCL610Airplane()
        {
            Title = Resources.MilestoneProgress.TitleEASAFCLIRAirplane;
            BaseFAR = "FCL.610 IR(b)";
            RatingSought = RatingType.InstrumentEASAIRAirplane;
            FARLink = MilestoneProgress.EASA_PART_FCL_LINK;

            miMinXCPICTime = new MilestoneItem(Resources.MilestoneProgress.MinEASAIRXCPICTime, ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, 50);
            miMinXCPICTimeInCategory = new MilestoneItem(Resources.MilestoneProgress.MinEASAIRXCPICTimeAirplanes, ResolvedFAR(string.Empty), string.Empty, MilestoneItem.MilestoneType.Time, 20);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsRealAircraft)
                return;

            bool fIsAirplane = CategoryClass.IsAirplane(cfr.idCatClassOverride);
            if (fIsAirplane || cfr.idCatClassOverride == CategoryClass.CatClassID.Helicopter || cfr.idCatClassOverride == CategoryClass.CatClassID.Airship)
            {
                decimal PICXC = Math.Min(cfr.PIC, cfr.XC);
                miMinXCPICTime.AddEvent(PICXC);
                if (fIsAirplane)
                    miMinXCPICTimeInCategory.AddEvent(PICXC);
            }
        }
    }

    /// <summary>
    /// Implements UK IR(R) (restricted) rating - see https://publicapps.caa.co.uk/docs/33/CAP804April2015REFONLY.pdf
    /// </summary>
    public class IFRUKIRRestricted : MilestoneProgress
    {
        protected MilestoneItem miTotalTime { get; set; }
        protected MilestoneItem miPICTime { get; set; }
        protected MilestoneItem miPICXCTime { get; set; }
        protected MilestoneItem miTraining { get; set; }
        protected MilestoneItem miInstTraining { get; set; }

        public IFRUKIRRestricted()
        {
            Title = Resources.MilestoneProgress.TitleUKIRRestricted;
            BaseFAR = "CAP 804 Part I E 3.1";
            FARLink = "https://publicapps.caa.co.uk/docs/33/CAP804April2015REFONLY.pdf";
            RatingSought = RatingType.InstrumentUKIRRestricted;

            miTotalTime = new MilestoneItem(Resources.MilestoneProgress.MinUKTotalTimeTraining, ResolvedFAR("(a)(i)"), Branding.ReBrand(Resources.MilestoneProgress.MinUKTotalTimeTrainingNote), MilestoneItem.MilestoneType.Time, 25);
            miPICTime = new MilestoneItem(Resources.MilestoneProgress.MinUKPICTime, ResolvedFAR("(a)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, 10);
            miPICXCTime = new MilestoneItem(Resources.MilestoneProgress.MinUKPICXCTime, ResolvedFAR("(a)(ii)"), string.Empty, MilestoneItem.MilestoneType.Time, 10);
            miTraining = new MilestoneItem(Resources.MilestoneProgress.MinIFRTrainingUK, ResolvedFAR("(b)"), Branding.ReBrand(Resources.MilestoneProgress.NoteMinIFRTrainingUK), MilestoneItem.MilestoneType.Time, 15);
            miInstTraining = new MilestoneItem(Resources.MilestoneProgress.MinIFRTrainingByRef, ResolvedFAR("(b)"), string.Empty, MilestoneItem.MilestoneType.Time, 10);
        }

        public override Collection<MilestoneItem> Milestones => new Collection<MilestoneItem>() { miTotalTime, miPICTime, miPICXCTime, miTraining, miInstTraining };

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!CatClassMatchesRatingSought(cfr.idCatClassOverride))
                return;

            miTotalTime.AddEvent(cfr.Total);
            miPICTime.AddEvent(cfr.PIC);
            miPICXCTime.AddEvent(Math.Min(cfr.PIC, cfr.XC));

            decimal instTime = cfr.IMC + cfr.IMCSim;
            decimal instInstruction = Math.Min(cfr.Dual, instTime);

            if (instTime == 0 || !cfr.fIsCertifiedIFR)
                return;

            miTraining.AddTrainingEvent(cfr.Dual, 5, !cfr.fIsRealAircraft);

            if (cfr.fIsRealAircraft)
                miInstTraining.AddEvent(instInstruction);
            if (!cfr.fIsRealAircraft)
                return;
        }
    }
    #endregion
}