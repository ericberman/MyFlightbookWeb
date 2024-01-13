using MyFlightbook.Achievements;
using MyFlightbook.Histogram;
using MyFlightbook.Instruction;
using MyFlightbook.RatingsProgress;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class TrainingController : AdminControllerBase
    {
        #region Webservices
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ProgressAgainstRating()
        {
            return SafeOp(() =>
            {
                string targetUser = Request["targetUser"];
                if (String.IsNullOrEmpty(nameof(targetUser)))
                    throw new ArgumentNullException(nameof(targetUser));
                if (targetUser.CompareCurrentCulture(User.Identity.Name) != 0 &&
                    !(CFIStudentMap.GetInstructorStudent((new CFIStudentMap(User.Identity.Name)).Students, targetUser)?.CanViewLogbook ?? false))
                    throw new UnauthorizedAccessException();
                string szGroup = Request["groupName"];
                string szRating = Request["ratingName"];
                return RenderProgressForRating(szGroup, szRating, targetUser);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddCustomProgress()
        {
            return SafeOp(() =>
            {
                ViewBag.customRatings = CustomRatingProgress.UpdateRatingForUser(User.Identity.Name, Request["ratingName"], string.Empty, Request["ratingDisclaimer"]);
                return PartialView("_customRatingList");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCustomProgress()
        {
            return SafeOp(() =>
            {
                ViewBag.customRatings = CustomRatingProgress.UpdateRatingForUser(User.Identity.Name, Request["ratingName"], string.Empty, Request["newDisclaimer"], Request["newName"]);
                return PartialView("_customRatingList");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCustomProgress()
        {
            return SafeOp(() =>
            {
                ViewBag.customRatings = CustomRatingProgress.DeleteRatingForUser(Request["ratingName"], User.Identity.Name);
                return PartialView("_customRatingList");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult MilestonesForCustomRating(string szTitle)
        {
            return SafeOp(() =>
            {
                ViewBag.crp = CustomRatingProgress.CustomRatingWithName(szTitle, User.Identity.Name);
                return PartialView("_milestonesForCustomRating");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddMilestoneForRating()
        {
            return SafeOp(() =>
            {
                ViewBag.crp = CustomRatingProgress.AddMilestoneForRating(User.Identity.Name, Request["ratingName"], Request["miTitle"],
                    Request["miFARRef"], Request["miNote"], Convert.ToInt32(Request["miThreshold"], CultureInfo.InvariantCulture),
                    Request["queryName"], Request["miField"], Request["miFieldFriendly"]);
                return PartialView("_milestonesForCustomRating");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMilestoneForRating()
        {
            return SafeOp(() =>
            {
                ViewBag.crp = CustomRatingProgress.DeleteMilestoneForRating(User.Identity.Name, Request["ratingName"], Convert.ToInt32(Request["milestoneIndex"], CultureInfo.InvariantCulture));
                return PartialView("_milestonesForCustomRating");
            });
        }
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
        #endregion

        #region Ratings Progress
        #endregion
        [ChildActionOnly]
        public ActionResult RenderProgressForRating(string szGroup, string szRating, string targetUser)
        {
            if (szGroup == null)
                throw new ArgumentNullException(nameof(szGroup));
            if (szRating == null)
                throw new ArgumentNullException(nameof(szRating));
            if (targetUser == null)
                throw new ArgumentNullException(nameof(targetUser));
            MilestoneProgress mp = MilestoneProgress.RatingProgressForGroup(szGroup, szRating, targetUser);
            if (mp == null)
                return new EmptyResult();
            Response.Cookies[szCookieLastGroup].Value = szGroup.Replace(",", szCommaCookieSub);
            Response.Cookies[szCookieLastMilestone].Value = szRating.Replace(",", szCommaCookieSub);
            ViewBag.mp = mp;
            ViewBag.headerText = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.RatingProgressPrintHeaderTemplate, mp.Title, MyFlightbook.Profile.GetUser(targetUser).UserFullName, DateTime.Now.Date.ToShortDateString());
            return PartialView("_ratingsProgressList");
        }
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

        #region Ratings Progress
        private const string szCookieLastGroup = "cookMilestoneLastGroup";
        private const string szCookieLastMilestone = "cookMilestoneLastMilestone";
        private const string szCommaCookieSub = "#COMMA#";

        [HttpGet]
        [Authorize]
        public ActionResult RatingsProgress(string user = null, string a = null)
        {
            // If a user is passed in, use them IFF we are an instructor with appropriate privileges.
            // Admins should just emulate the user to view progress; this is a (minor) change from the old .aspx implementation
            string szUser = (!String.IsNullOrEmpty(user) && (CFIStudentMap.GetInstructorStudent((new CFIStudentMap(User.Identity.Name)).Students, user)?.CanViewLogbook ?? false)) ? user : User.Identity.Name;

            // build a map of all histogrammable values for naming
            List<HistogramableValue> lstMilestoneFields = new List<HistogramableValue>(LogbookEntryDisplay.HistogramableValues);
            foreach (CustomPropertyType cpt in CustomPropertyType.GetCustomPropertyTypes(szUser))
            {
                HistogramableValue hv = LogbookEntryDisplay.HistogramableValueForPropertyType(cpt);
                if (hv != null)
                    lstMilestoneFields.Add(hv);
            }

            HttpCookie cookieLastGroup = Request.Cookies[szCookieLastGroup];
            HttpCookie cookieLastMilestone = Request.Cookies[szCookieLastMilestone];
            ViewBag.selectedGroup = (cookieLastGroup?.Value ?? string.Empty).Replace(szCommaCookieSub, ",");
            ViewBag.selectedRating = (cookieLastMilestone?.Value ?? string.Empty).Replace(szCommaCookieSub, ",");
            ViewBag.cannedQueries = CannedQuery.QueriesForUser(szUser);
            ViewBag.milestoneFields = lstMilestoneFields;
            ViewBag.targetUser = szUser;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.PageTitle, HttpUtility.HtmlEncode(MyFlightbook.Profile.GetUser(szUser).UserFullName));
            ViewBag.milestones = MilestoneProgress.AvailablePrgressItemsDictionary(szUser);
            ViewBag.customRatings = CustomRatingProgress.CustomRatingsForUser(szUser);
            return View("ratingsProgress");
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