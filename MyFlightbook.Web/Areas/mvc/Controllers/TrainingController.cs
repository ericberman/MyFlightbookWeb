using MyFlightbook.Achievements;
using MyFlightbook.RatingsProgress;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class TrainingController : AdminControllerBase
    {
        #region Webservices
        #endregion

        #region Partial views/child views
        #region Achievements, badges, and calendar
        [ChildActionOnly]
        public ActionResult RenderBadgeSets(IEnumerable<BadgeSet> badgeSets, bool fReadOnly)
        {
            ViewBag.badgeSets = badgeSets;
            ViewBag.fReadOnly = fReadOnly;
            return PartialView("_badgeSet");
        }

        [ChildActionOnly]
        public ActionResult RenderRecentAchievements(RecentAchievements ra, bool IsReadOnly = false)
        {
            ViewBag.isReadOnly = IsReadOnly;
            ViewBag.ra = ra;
            return PartialView("_recentAchievements");
        }

        [ChildActionOnly]
        public ActionResult FlyingCalendar(RecentAchievements ra)
        {
            ViewBag.recentAchievements = ra;
            return PartialView("_flyingCalendar");
        }
        #endregion
        #endregion

        #region Visible endpoints
        #region Achievements
        private ViewResult AchievementsForRange(FlightQuery.DateRanges range = FlightQuery.DateRanges.AllTime, DateTime? dtStart = null, DateTime? dtEnd = null, bool includeCalendar = false)
        {
            RecentAchievements ra = RecentAchievements.AchievementsForDateRange(User.Identity.Name, range, dtStart, dtEnd);
            ViewBag.ra = ra;
            ViewBag.range = range.ToString();
            ViewBag.includeCalendar = includeCalendar;
            ViewBag.badgeSets = BadgeSet.BadgeSetsFromBadges(new Achievement(User.Identity.Name).BadgesForUser());
            return View("achievements");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Achievements(FlightQuery.DateRanges recentAchievementsDateRange, DateTime? dtStart, DateTime? dtEnd, bool includeCalendar, bool fRecompute)
        {
            if (fRecompute)
                MyFlightbook.Profile.GetUser(User.Identity.Name).SetAchievementStatus(Achievement.ComputeStatus.NeedsComputing);
            return AchievementsForRange(recentAchievementsDateRange, dtStart, dtEnd, includeCalendar);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Achievements()
        {
            return AchievementsForRange();
        }
        #endregion

        // GET: mvc/Training
        public ActionResult Index()
        {
            return View();
        }
        #endregion
    }
}