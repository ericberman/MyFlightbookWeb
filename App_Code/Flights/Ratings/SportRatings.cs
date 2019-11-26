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
    /// Sports pilot milestones
    /// </summary>
    [Serializable]
    public class SportsMilestones : MilestoneGroup
    {
        public SportsMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupSportPilot;
            Milestones = new MilestoneProgress[] {
                new SportPilotAirplane(),
                new SportPilotGlider(),
                new SportPilotGyroplane() };
        }
    }

    #region 61.313 - Sport Pilot
    [Serializable]
    public abstract class SportPilotBase : MilestoneProgress
    {
        protected decimal minTime { get; set; }
        protected decimal minInstruction { get; set; }
        protected decimal minSolo { get; set; }

        protected MilestoneItem miMinTime { get; set; }
        protected MilestoneItem miMinInstruction { get; set; }
        protected MilestoneItem miMinSolo { get; set; }
        protected MilestoneItem miTestPrep { get; set; }

        protected CategoryClass.CatClassID CatClassID { get; set; }
        protected string CategoryName { get; set; }

        protected SportPilotBase() : base() { }
    }

    #region Concrete Sport Pilot Classes
    /// <summary>
    /// Airplane and rotorcraft share a lot of features, so put them in a common base class
    /// </summary>
    [Serializable]
    public abstract class SportPilotAirplaneGyroplane : SportPilotBase
    {
        protected int MinXCDistance { get; set; }
        protected MilestoneItem miMinCrossCountry { get; set; }
        protected MilestoneItem miMinLandings { get; set; }
        protected MilestoneItem miSoloXCFlight { get; set; }

        protected void Init()
        {
            string szFar = ResolvedFAR("(1)");
            miMinTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportMinTime, minTime, CategoryName), szFar, string.Empty, MilestoneItem.MilestoneType.Time, minTime);
            miMinInstruction = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportMinInstruction, minInstruction, CategoryName), szFar, string.Empty, MilestoneItem.MilestoneType.Time, minInstruction);
            miMinSolo = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportMinSolo, minSolo, CategoryName), szFar, string.Empty, MilestoneItem.MilestoneType.Time, minSolo);

            miMinCrossCountry = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportMinXC, CategoryName), ResolvedFAR("(1)(i)"), string.Empty, MilestoneItem.MilestoneType.Time, 2.0M);
            miMinLandings = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportMinLandings, CategoryName), ResolvedFAR("(1)(ii)"), Resources.MilestoneProgress.SportPilotLandingNote, MilestoneItem.MilestoneType.Count, 10);
            miSoloXCFlight = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.SportSoloXCFlight, CategoryName, MinXCDistance), ResolvedFAR("(1)(iii)"), string.Empty, MilestoneItem.MilestoneType.AchieveOnce, 1);
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.SportTestPrepTime, ResolvedFAR("(1)(iv)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 2.0M);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // No training devices for sport pilots
            if (!cfr.fIsRealAircraft)
                return;

            // Minimum time can be in anything
            miMinTime.AddEvent(cfr.Total);

            // Everything else must be in matching category/class
            // allow perfect match or ASEL->ASES match
            if (CatClassID != cfr.idCatClassOverride && !(CatClassID == CategoryClass.CatClassID.ASEL && cfr.idCatClassOverride == CategoryClass.CatClassID.ASES))
                return;

            miMinInstruction.AddEvent(cfr.Dual);
            decimal soloTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsSolo) { soloTime += pf.DecValue; } });
            miMinSolo.AddEvent(soloTime);

            int cFSLandings = cfr.cFullStopLandings + cfr.cFullStopNightLandings;
            miMinCrossCountry.AddEvent(Math.Min(cfr.XC, cfr.Dual));
            miMinLandings.AddEvent(cFSLandings);
            if (soloTime > 0 && cFSLandings > 1)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);

                if (al.DistanceForRoute() > MinXCDistance && al.MaxSegmentForRoute() > 25)
                {
                    miSoloXCFlight.AddEvent(1);
                    miSoloXCFlight.MatchingEventID = cfr.flightID;
                    miSoloXCFlight.MatchingEventText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.MatchingXCFlightTemplate, cfr.dtFlight.ToShortDateString(), cfr.Route);
                }
            }

            if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                miTestPrep.AddEvent(cfr.Dual);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miMinTime, miMinInstruction, miMinSolo, miMinCrossCountry, miMinLandings, miSoloXCFlight, miTestPrep }; }
        }
    }

    [Serializable]
    public class SportPilotAirplane : SportPilotAirplaneGyroplane
    {
        public SportPilotAirplane() : base()
        {
            // Basic MilestoneProgress stuff
            Title = Resources.MilestoneProgress.TitleSportSingleEngine;
            CatClassID = CategoryClass.CatClassID.ASEL;
            BaseFAR = "61.313(a)";
            RatingSought = RatingType.SportSingleEngine;

            // Parameters for SportBasePilot and SportPilotAirplaneRotorcraft
            minTime = 20.0M;
            minInstruction = 15.0M;
            minSolo = 5.0M;
            MinXCDistance = 75;
            CategoryName = Resources.MilestoneProgress.SportAirplaneCategory;
            Init();
        }
    }

    [Serializable]
    public class SportPilotGlider : SportPilotBase
    {
        protected MilestoneItem miMinTrainingFlights { get; set; }
        protected MilestoneItem miMinSoloFlights { get; set; }
        protected decimal TotalHeavierThanAir { get; set; }
        protected bool QualifiesByHeavierThanAir { get; set; }

        public SportPilotGlider()
            : base()
        {
            // Basic MilestoneProgress stuff
            Title = Resources.MilestoneProgress.TitleSportGlider;
            CatClassID = CategoryClass.CatClassID.Glider;
            BaseFAR = "61.313(b, c)";
            RatingSought = RatingType.SportGlider;

            // Parameters for SportBasePilot
            CategoryName = Resources.MilestoneProgress.SportGliderCategory;

            // Unique to this class
            TotalHeavierThanAir = 0.0M;
            QualifiesByHeavierThanAir = false;
            string szFar = ResolvedFAR("(1)");
            miMinTime = new MilestoneItem(Resources.MilestoneProgress.SportMinTimeGlider, szFar, string.Empty, MilestoneItem.MilestoneType.Time, 10);
            miMinTrainingFlights = new MilestoneItem(Resources.MilestoneProgress.SportMinFlightsGlider, szFar, string.Empty, MilestoneItem.MilestoneType.Count, 10);
            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.SportMinSoloGlider, szFar, string.Empty, MilestoneItem.MilestoneType.Time, 2);

            miMinSoloFlights = new MilestoneItem(Resources.MilestoneProgress.SportMinSoloLandings, ResolvedFAR("(1)(i)"), Resources.MilestoneProgress.SportMinLandingsNote, MilestoneItem.MilestoneType.Count, 5);
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.SportMinTraining, ResolvedFAR("(1)(ii)"), Resources.MilestoneProgress.NoteTestPrep, MilestoneItem.MilestoneType.Count, 3);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            // No training devices for sport pilots
            if (!cfr.fIsRealAircraft)
                return;

            // Keep track of heavier-than-air time, reduce minimums if achieved
            if (CategoryClass.IsHeavierThanAir(cfr.idCatClassOverride))
            {
                TotalHeavierThanAir += cfr.Total;
                if (!QualifiesByHeavierThanAir && TotalHeavierThanAir >= 20)
                {
                    QualifiesByHeavierThanAir = true;
                    miMinTime.Threshold = 3;
                    miMinTrainingFlights.Threshold = 5;
                    miMinSolo.Threshold = 1;
                    miMinSolo.Threshold = 3;
                }
            }

            // now reject anything not in a glider
            if (CatClassID != cfr.idCatClassOverride)
                return;

            miMinTime.AddEvent(cfr.Total);
            if (cfr.Dual > 0)
            {
                miMinTrainingFlights.AddEvent(Math.Max(1, cfr.cLandingsThisFlight));
                if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miTestPrep.AddEvent(cfr.Dual);
            }

            decimal soloTime = 0.0M;
            cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsSolo) { soloTime += pf.DecValue; } });
            if (soloTime > 0)
            {
                miMinSolo.AddEvent(soloTime);
                miMinSoloFlights.AddEvent(cfr.cLandingsThisFlight); // assuming no touch-and-go in a glider!
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miMinTime, miMinTrainingFlights, miMinSolo, miMinSoloFlights, miTestPrep }; }
        }
    }

    [Serializable]
    public class SportPilotGyroplane : SportPilotAirplaneGyroplane
    {
        // Basic MilestoneProgress stuff
        public SportPilotGyroplane()
            : base()
        {
            Title = Resources.MilestoneProgress.TitleSportGyroplane;
            CatClassID = CategoryClass.CatClassID.Gyroplane;
            BaseFAR = "61.313(c)";
            RatingSought = RatingType.SportRotorcraft;

            // Parameters for SportBasePilot and SportPilotAirplaneRotorcraft
            minTime = 20.0M;
            minInstruction = 15.0M;
            minSolo = 5.0M;
            MinXCDistance = 50;
            CategoryName = Resources.MilestoneProgress.SportGyroplaneCategory;
            Init();

            // and now what's unique to this class
        }
    }
    #endregion
    #endregion
}