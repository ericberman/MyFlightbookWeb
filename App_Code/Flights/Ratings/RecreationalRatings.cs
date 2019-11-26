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
    /// Recreational Pilot milestones
    /// </summary>
    [Serializable]
    public class RecreationalMilestones : MilestoneGroup
    {
        public RecreationalMilestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroupRecreational;
            Milestones = new MilestoneProgress[] {
                    new RP6199ASEL(),
                    new RP6199ASES(),
                    new RP6199AMEL(),
                    new RP6199AMES(),
                    new RP6199Glider(),
                    new RP6199Helicopter(),
                    new RP6199Gyroplane(),
                    new RP6199GasBalloon(),
                    new RP6199HotAirBalloon()};
        }
    }

    #region 61.99 - Recreational Pilot
    [Serializable]
    public abstract class RP6199Base : MilestoneProgress
    {
        protected MilestoneItem miMinTime { get; set; }
        protected MilestoneItem miMinInstruction { get; set; }
        protected MilestoneItem miXCFlight { get; set; }
        protected MilestoneItem miTestPrep { get; set; }
        protected MilestoneItem miMinSolo { get; set; }

        protected CategoryClass.CatClassID CatClassID { get; set; }

        protected void Init(CategoryClass.CatClassID ccid)
        {
            CategoryClass cc = CategoryClass.CategoryClassFromID(CatClassID = ccid);
            Title = String.Format(CultureInfo.CurrentCulture, "{0} - {1}", Resources.MilestoneProgress.Title6199, cc.CatClass);
            BaseFAR = "61.99";
            RatingSought = RatingType.RecreationalPilot;

            // 61.99 overall
            miMinTime = new MilestoneItem(Resources.MilestoneProgress.RecreationalMinTime, BaseFAR, string.Empty, MilestoneItem.MilestoneType.Time, 30.0M);

            // 61.99(a) - 15 hours of dual
            miMinInstruction = new MilestoneItem(Resources.MilestoneProgress.RecreationalMinTraining, ResolvedFAR("(a)"), String.Empty, MilestoneItem.MilestoneType.Time, 15.0M);

            // 61.99(a)(1)(i) - 2 hours of flight training in 25nm flights with at least 4 landings
            miXCFlight = new MilestoneItem(Resources.MilestoneProgress.RecreationalMinXC, ResolvedFAR("(a)(1)(i)"), Resources.MilestoneProgress.RecreationalMinXCNote, MilestoneItem.MilestoneType.Time, 2.0M);

            // 61.99(a)(1)(ii) - 3 hours of flight training within preceding 2 months.
            miTestPrep = new MilestoneItem(Resources.MilestoneProgress.RecreationTestPrep, ResolvedFAR("(a)(1)(ii)"), Branding.ReBrand(Resources.MilestoneProgress.NoteTestPrep), MilestoneItem.MilestoneType.Time, 3.0M);

            // 61.99(a)(2) - 3 hours of solo time
            miMinSolo = new MilestoneItem(Resources.MilestoneProgress.RecreationalMinSolo, ResolvedFAR("(a)(2)"), Resources.MilestoneProgress.NoteSoloTime, MilestoneItem.MilestoneType.Time, 3.0M);
        }

        protected RP6199Base()
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!cfr.fIsRealAircraft)
                return;

            // 61.99
            miMinTime.AddEvent(cfr.Total);

            // 61.99(a)
            miMinInstruction.AddEvent(cfr.Dual);

            // 61.99(a)(1)
            if (cfr.cLandingsThisFlight >= 4 && cfr.Dual > 0)
            {
                AirportList al = AirportListOfRoutes.CloneSubset(cfr.Route, true);
                if (al.MaxDistanceForRoute() >= 25.0)
                    miXCFlight.AddEvent(cfr.Dual);
            }

            if (cfr.idCatClassOverride == CatClassID)
            {
                // 61.99(a)(2)
                if (DateTime.Now.AddCalendarMonths(-2).CompareTo(cfr.dtFlight) <= 0)
                    miTestPrep.AddEvent(cfr.Dual);

                // 61.99(b)
                decimal soloTime = 0.0M;
                cfr.FlightProps.ForEachEvent(pf => { if (pf.PropertyType.IsSolo) { soloTime += pf.DecValue; } });
                miMinSolo.AddEvent(soloTime);
            }
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                return new Collection<MilestoneItem>() {
                miMinTime,
                miMinInstruction,
                miXCFlight,
                miTestPrep,
                miMinSolo,
                };
            }
        }
    }

    #region Concreate recreational pilot ratings
    [Serializable]
    public class RP6199ASEL : RP6199Base
    {
        public RP6199ASEL()
        {
            Init(CategoryClass.CatClassID.ASEL);
        }
    }

    [Serializable]
    public class RP6199AMEL : RP6199Base
    {
        public RP6199AMEL()
        {
            Init(CategoryClass.CatClassID.AMEL);
        }
    }

    [Serializable]
    public class RP6199ASES : RP6199Base
    {
        public RP6199ASES()
        {
            Init(CategoryClass.CatClassID.ASES);
        }
    }

    [Serializable]
    public class RP6199AMES : RP6199Base
    {
        public RP6199AMES()
        {
            Init(CategoryClass.CatClassID.AMES);
        }
    }

    [Serializable]
    public class RP6199Helicopter : RP6199Base
    {
        public RP6199Helicopter()
        {
            Init(CategoryClass.CatClassID.Helicopter);
        }
    }

    [Serializable]
    public class RP6199Glider : RP6199Base
    {
        public RP6199Glider()
        {
            Init(CategoryClass.CatClassID.Glider);
        }
    }

    [Serializable]
    public class RP6199Gyroplane : RP6199Base
    {
        public RP6199Gyroplane()
        {
            Init(CategoryClass.CatClassID.Gyroplane);
        }
    }

    [Serializable]
    public class RP6199HotAirBalloon : RP6199Base
    {
        public RP6199HotAirBalloon()
        {
            Init(CategoryClass.CatClassID.HotAirBalloon);
        }
    }

    [Serializable]
    public class RP6199GasBalloon : RP6199Base
    {
        public RP6199GasBalloon()
        {
            Init(CategoryClass.CatClassID.GasBalloon);
        }
    }
    #endregion
    #endregion
}