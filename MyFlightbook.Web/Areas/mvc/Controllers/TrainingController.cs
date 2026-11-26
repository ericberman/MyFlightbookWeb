using MyFlightbook.Achievements;
using MyFlightbook.Currency.WINGS;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.RatingsProgress;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static MyFlightbook.Instruction.Endorsement;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class TrainingController : AdminControllerBase
    {
        #region Webservices
        #region Ratings Progress
        /// <summary>
        /// Returns potential training completions for the provided prefix text.  E.g., if it is "[61.1" then it might produce all of the 61.1xxx training items.
        /// </summary>
        /// <param name="prefixText">The prefix</param>
        /// <param name="count">Number of items to return</param>
        /// <returns>An array of training items (strings) in JSON format</returns>
        [Authorize]
        [HttpPost]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult SuggestTraining(string prefixText, int count)
        {
            return SafeOp(() =>
            {
                return Json(TrainingItems.SuggestTraining(prefixText, count));
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
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
        [ValidateHeaderAntiForgeryToken]
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
                    Request["miFARRef"], Request["miNote"], Convert.ToDecimal(Request["miThreshold"], CultureInfo.InvariantCulture),
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

        #region Instructor management
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteInstructor()
        {
            return SafeOp(() =>
            {
                CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
                sm.RemoveInstructor(Request["username"]);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInstructorPermissions()
        {
            return SafeOp(() =>
            {
                CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
                string szInstructor = Request["username"];
                bool canView = Convert.ToBoolean(Request["canView"], CultureInfo.InvariantCulture);
                bool canAdd = Convert.ToBoolean(Request["canAdd"], CultureInfo.InvariantCulture);
                InstructorStudent i = sm.Instructors.FirstOrDefault(inst => inst.UserName.CompareCurrentCulture(szInstructor) == 0);
                i.CanViewLogbook = canView;
                i.CanAddLogbook = canAdd;
                sm.SetCFIPermissions(i);

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult InviteInstructor()
        {
            return SafeOp(() =>
            {
                string instructorEmail = Request["instructorEmail"];
                if (!RegexUtility.Email.IsMatch(instructorEmail))
                    throw new InvalidOperationException(Resources.LocalizedText.ValidationEmailFormat);

                CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
                CFIStudentMapRequest smr = sm.GetRequest(CFIStudentMapRequest.RoleType.RoleCFI, instructorEmail);
                smr.Send();

                return Content(String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, HttpUtility.HtmlEncode(instructorEmail)));
            });
        }
        #endregion

        #region Student Management
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult InviteStudent()
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                string szNewCert = Request["pilotCertificate"];
                string szExpiration = Request["pilotCertificateExpiration"];
                string szStudentEmail = Request["studentEmail"];
                if (!RegexUtility.Email.IsMatch(szStudentEmail))
                    throw new InvalidOperationException(Resources.LocalizedText.ValidationEmailFormat);
                if (String.IsNullOrEmpty(pf.Certificate) && !String.IsNullOrEmpty(szNewCert))
                {
                    pf.Certificate = szNewCert.LimitTo(30);
                    pf.CertificateExpiration = String.IsNullOrEmpty(szExpiration) ? DateTime.MinValue : DateTime.Parse(szExpiration, CultureInfo.InvariantCulture);
                    pf.FCommit();
                }
                CFIStudentMapRequest smr = new CFIStudentMap(User.Identity.Name).GetRequest(CFIStudentMapRequest.RoleType.RoleStudent, szStudentEmail);
                smr.Send();
                return Content(String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, HttpUtility.HtmlEncode(szStudentEmail)));
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudent()
        {
            return SafeOp(() =>
            {
                new CFIStudentMap(User.Identity.Name).RemoveStudent(Request["studentName"]);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePendingSignatureRequestForStudent()
        {
            return SafeOp(() =>
            {
                // Validate
                LogbookEntry le = new LogbookEntry(Convert.ToInt32(Request["idFlight"], CultureInfo.InvariantCulture), Request["studentName"]);
                if (le.FlightID <= 0) // flight doesn't exist or isn't owned by that student.
                    throw new UnauthorizedAccessException(le.ErrorString);
                le.ClearPendingSignature();
                return new EmptyResult();
            });
        }
        #endregion

        #region WINGS
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult WINGSAutoCompletion(string prefixText, int count)
        {
            return SafeOp(() =>
            {
                List<string> lst = new List<string>();
                if ((prefixText ?? string.Empty).Length > 3)
                {
                    IEnumerable<WINGSActivity> rgWings = WINGSActivity.ActivitiesForPrefix(prefixText.Substring(1));
                    foreach (WINGSActivity w in rgWings)
                    {
                        lst.Add($"{w.ActivityNumber} - {w.ActivityName}");
                        if (lst.Count >= count)
                            break;
                    }
                }
                return Json(lst);
            });
        }
        #endregion
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

        #region Scribble signature
        [ChildActionOnly]
        public ActionResult RenderScribble(string saveFunc = null, string cancelFunc = null, string colorRef = "#0000ff", string watermarkRef = null)
        {
            ViewBag.watermarkRef = watermarkRef ?? string.Empty;
            ViewBag.colorRef = colorRef;
            ViewBag.saveFunc = saveFunc;
            ViewBag.cancelFunc = cancelFunc;
            return PartialView("_scribbleSignature");
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

        #region Ratings Progress
        private const string szCookieLastGroup = "cookMilestoneLastGroup";
        private const string szCookieLastMilestone = "cookMilestoneLastMilestone";
        private const string szCommaCookieSub = "#COMMA#";

        [HttpGet]
        [Authorize]
        public ActionResult RatingsProgress(string user = null)
        {
            // If a user is passed in, use them IFF we are an instructor with appropriate privileges.
            // Admins should just emulate the user to view progress; this is a (minor) change from the old .aspx implementation
            string szUser = (!String.IsNullOrEmpty(user) && (CFIStudentMap.GetInstructorStudent((new CFIStudentMap(User.Identity.Name)).Students, user)?.CanViewLogbook ?? false)) ? user : User.Identity.Name;

            HttpCookie cookieLastGroup = Request.Cookies[szCookieLastGroup];
            HttpCookie cookieLastMilestone = Request.Cookies[szCookieLastMilestone];
            ViewBag.selectedGroup = (cookieLastGroup?.Value ?? string.Empty).Replace(szCommaCookieSub, ",");
            ViewBag.selectedRating = (cookieLastMilestone?.Value ?? string.Empty).Replace(szCommaCookieSub, ",");
            ViewBag.cannedQueries = CannedQuery.QueriesForUser(szUser);
            ViewBag.milestoneFields = CustomRatingProgress.MilestoneFieldsForUser(user);
            ViewBag.targetUser = szUser;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.PageTitle, HttpUtility.HtmlEncode(MyFlightbook.Profile.GetUser(szUser).UserFullName));
            ViewBag.milestones = MilestoneProgress.AvailablePrgressItemsDictionary(szUser);
            ViewBag.customRatings = CustomRatingProgress.CustomRatingsForUser(szUser);
            return View("ratingsProgress");
        }
        #endregion

        #region Instructor/Student management
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SaveDefaultScribble()
        {
            string base64Data = Request["hdnSigData"];
            byte[] rgbSig = String.IsNullOrEmpty(base64Data) ? Array.Empty<byte>() : ScribbleImage.FromDataLinkURL(base64Data);
            CFIStudentMap.SetDefaultScribbleForInstructor(MyFlightbook.Profile.GetUser(User.Identity.Name), rgbSig);
            return Redirect("./Students");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Students()
        {
            CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
            Dictionary<string, IEnumerable<LogbookEntry>> d = new Dictionary<string, IEnumerable<LogbookEntry>>();
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            foreach (InstructorStudent student in sm.Students)
                d[student.UserName] = LogbookEntryBase.PendingSignaturesForStudent(pf, student);
            ViewBag.pendingFlightMap = d;
            ViewBag.instructorMap = sm;
            List<Endorsement> lst = new List<Endorsement>(EndorsementsForUser(null, User.Identity.Name));
            lst.RemoveAll(e => e.StudentType == StudentTypes.Member);
            ViewBag.externalEndorsements = lst;

            ImageList il = new ImageList(MFBImageInfoBase.ImageClass.OfflineEndorsement, User.Identity.Name);
            il.Refresh(true);
            il = ImageEndorsementManager.UnabsorbedImages(il);
            ViewBag.offlineImageList = il;
            ViewBag.offlineScanStatus = ImageEndorsementManager.ScanStatusForImages(il);
            return View("students");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Instructors()
        {
            ViewBag.instructorMap = new CFIStudentMap(User.Identity.Name);
            return View("instructors");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult RequestWINGSCredit(string cfiUser, int[] wingsFlightID)
        {
            return SafeOp(() =>
            {
                if (string.IsNullOrEmpty(cfiUser))
                    throw new InvalidOperationException(Resources.SignOff.WINGSRequestErrNoInstructor);
                if (wingsFlightID == null || wingsFlightID.Length == 0)
                    throw new InvalidOperationException(Resources.SignOff.WINGSErrNoActivity);
                InstructorStudent.RequestWINGSCredit(User.Identity.Name, cfiUser, wingsFlightID);
                return Content(Resources.SignOff.WINGSRequestSent);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddWINGSActivity(DateTime WINGSActivityDate, string WINGSActivity)
        {
            LogbookEntry.FromWINGSActivity(User.Identity.Name, WINGSActivityDate, WINGSActivity, Resources.SignOff.WINGSActivityComment).FCommit();
            return Redirect("~/mvc/Training/Instructors?pane=WINGS");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmRelationship()
        {
            if (Request["btnCancel"] != null)
                return Redirect("~/mvc/pub");
            else
            {
                try
                {
                    return Redirect(CFIStudentMapRequest.ExecuteRequestForUser(Request["req"], User.Identity.Name));
                }
                catch (Exception ex) when (ex.IsNonFatal())
                {
                    ViewBag.error = ex.Message;
                    return View("addRelationship");
                }
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult AddRelationship(string req)
        {
            ViewBag.req = req;
            try
            {
                ViewBag.requestPrompt = CFIStudentMapRequest.ProcessRequestParameter(req, User.Identity.Name);
            } 
            catch (Exception ex) when (ex.IsNonFatal())
            {
                ViewBag.error = ex.Message;
            }
            return View("addRelationship");
        }
        #endregion

        #region Request Signatures
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult RequestSigs(string[] idFlight, string instructor, string instrEmail)
        {
            if ((idFlight?.Length ?? 0) > 0)
                InstructorStudent.RequestSigs(String.Join(",", idFlight).ToInts(), User.Identity.Name, instructor, instrEmail);
            return Redirect("~/mvc/Training/RequestSigs");
        }

        [HttpGet]
        [Authorize]
        public ActionResult RequestSigs(string ids = null)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.ids = ids;
            if (ViewBag.reviewPending = ids == null)
                ViewBag.flightsPendingSignature = LogbookEntryBase.PendingSignaturesForStudent(null, pf);
            else
            {
                List<LogbookEntryDisplay> lst = LogbookEntryDisplay.GetEnumeratedFlightsForUser(User.Identity.Name, ids.ToInts());
                lst.RemoveAll(le => !le.CanRequestSig);
                ViewBag.flights = lst;
                ViewBag.instructors = new CFIStudentMap(User.Identity.Name);
            }

            return View("requestSigs");
        }
        #endregion

        #region Endorsements (pages; web services and partial views are in EndorsementsController)
        [Authorize]
        [HttpGet]
        public ActionResult AddEndorsement()
        {
            ViewBag.instructorMap = new CFIStudentMap(User.Identity.Name);
            ViewBag.endorsements = EndorsementsForUser(User.Identity.Name, string.Empty);
            return View("addEndorsement");
        }

        [Authorize]
        [HttpGet]
        public ActionResult EndorseStudent(string student, int @extern = 0, int tid = 0)
        {
            ViewBag.studentType = @extern != 0 ? StudentTypes.External : StudentTypes.Member;
            ViewBag.templateID = tid;
            ViewBag.endorsements = EndorsementsForUser(student, User.Identity.Name, fIncludeDeleted: @extern == 0); // issue # 1196 - don't include deleted external endorsements.
            ViewBag.nonOwnedEndorsements = string.IsNullOrEmpty(student) ? Array.Empty<Endorsement>() : RemoveEndorsementsByInstructor(EndorsementsForUser(student, null), User.Identity.Name);
            InstructorStudent instrStudent = (CFIStudentMap.GetInstructorStudent(new CFIStudentMap(User.Identity.Name).Students, student)) ?? (@extern == 0 && !String.IsNullOrEmpty(student) ? throw new UnauthorizedAccessException() : (InstructorStudent) null);
            ViewBag.targetUser = (instrStudent == null) ? string.Empty : student;
            ViewBag.canViewStudent = instrStudent?.CanViewLogbook ?? false;
            ViewBag.canEditStudent = instrStudent?.CanAddLogbook ?? false;

            return View("endorseStudent");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Endorsements(int print = 0)
        {
            bool fPrintView = print != 0;
            ViewBag.printView = fPrintView;

            ImageList il = new ImageList(MFBImageInfoBase.ImageClass.Endorsement, User.Identity.Name);
            il.Refresh(true);
            ImageList ilUnabsorbed = ImageEndorsementManager.UnabsorbedImages(il);
            ViewBag.unabsorbedImageList = ilUnabsorbed;
            ViewBag.scanStatus = ImageEndorsementManager.ScanStatusForImages(ilUnabsorbed);

            ViewBag.student = User.Identity.Name;
            ViewBag.instructor = string.Empty;

            ViewBag.endorsements = EndorsementsForUser(User.Identity.Name, string.Empty, true);
            return View("endorsements");
        }
        #endregion

        #region Reports (8710 and rollups)
        private async Task<ViewResult> PopulateReports(string id = null, string fqJSON = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            FlightQuery fq = String.IsNullOrEmpty(fqJSON) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromJSON(fqJSON);

            if (fq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            object o = Session[MFBConstants.keyMathRoundingUnits];
            ViewBag.reports = await Currency.TrainingReportsForUser.ReportsForUser(fq, o == null ? MyFlightbook.Profile.GetUser(fq.UserName).MathRoundingUnit : (int)o);
            ViewBag.query = fq;
            ViewBag.defaultPane = id;
            return View("reports");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Reports(string id = null, string fqJSON = null, bool fPropDeleteClicked = false, string propToDelete = null)
        {
            return await PopulateReports(id, fqJSON, fPropDeleteClicked, propToDelete);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Reports(string id = null)
        {
            return await PopulateReports(id);
        }
        #endregion

        [HttpGet]
        [Authorize]
        public ActionResult Index()
        {
            return Redirect("./Instructors");
        }
        #endregion
    }
}