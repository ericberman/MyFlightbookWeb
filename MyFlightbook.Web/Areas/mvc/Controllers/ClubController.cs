using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Instruction;
using MyFlightbook.Mapping;
using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class ClubController : AdminControllerBase
    {
        const string szCookieLastStart = "clubFlyingLastStart";
        const string szCookieLastEnd = "clubFlyingLastEnd";

        #region Check Authorization
        private Club ValidateClubAdmin(int idClub)
        {
            Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);

            if (!club.HasAdmin(User.Identity.Name))
                throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);
            return club;
        }

        private Club ValidateClubMember(int idClub)
        {
            Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);

            if (!club.HasMember(User.Identity.Name))
                throw new UnauthorizedAccessException(Resources.Club.errNotAMember);
            return club;
        }
        #endregion

        #region WebServices
        #region Manage (admin) functions
        [HttpPost]
        [Authorize]
        public ActionResult DeleteMember(int idClub, string userName)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                ClubMember cm = club.GetMember(userName) ?? throw new InvalidOperationException(Resources.Club.errNoSuchUser);
                if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)
                    throw new InvalidOperationException(Resources.Club.errCannotDeleteOwner);
                if (!cm.FDeleteClubMembership())
                    throw new InvalidOperationException(cm.LastError);
                club.InvalidateMembers();

                ViewBag.club = club;
                return PartialView("_memberList");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult UpdateMember(int idClub, string userName, ClubMember.ClubMemberRole requestedRole, bool isMaintenanceOfficer, bool isTreasurer, bool isInsuranceOfficer, string officesHeld, bool isInactive)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                ClubMember cm = club.GetMember(userName) ?? throw new InvalidOperationException(Resources.Club.errNoSuchUser);

                if (requestedRole == ClubMember.ClubMemberRole.Owner) // that's fine, but we need to un-make any other creators/owners
                {
                    ClubMember cmOldOwner = club.Members.FirstOrDefault(pf => pf.RoleInClub == ClubMember.ClubMemberRole.Owner);
                    if (cmOldOwner != null) //should never happen!
                    {
                        cmOldOwner.RoleInClub = ClubMember.ClubMemberRole.Admin;
                        if (!cmOldOwner.FCommitClubMembership())
                            throw new MyFlightbookException(cmOldOwner.LastError);
                    }
                }
                else if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)    // if we're not requesting creator role, but this person currently is creator, then we are demoting - that's a no-no
                    throw new MyFlightbookException(Resources.Club.errCantDemoteOwner);

                bool oldInactiveState = cm.IsInactive;

                cm.RoleInClub = requestedRole;
                cm.IsMaintanenceOfficer = isMaintenanceOfficer;
                cm.IsTreasurer = isTreasurer;
                cm.IsInsuranceOfficer = isInsuranceOfficer;
                cm.ClubOffice = officesHeld ?? string.Empty;
                cm.IsInactive = isInactive;
                if (!cm.FCommitClubMembership())
                    throw new MyFlightbookException(cm.LastError);

                // Issue #1143: notify target user of being deactivated/activated:
                if (oldInactiveState != isInactive)
                    util.NotifyUser(Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.Club.statusChangeSubject, club.Name)), Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.Club.statsuChangeNotificationBody, club.Name, oldInactiveState ? Resources.Club.RoleInactive : Resources.Club.roleActive, isInactive ? Resources.Club.RoleInactive : Resources.Club.roleActive)), new System.Net.Mail.MailAddress(cm.Email, cm.UserFullName), false, false);

                club.InvalidateMembers();

                ViewBag.club = club;
                return PartialView("_memberList");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult UpdateAircraft(int idClub, int idAircraft, string description, decimal highWater)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                ClubAircraft ca = club.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == idAircraft) ?? throw new InvalidOperationException(Resources.Club.errNoSuchAircraft);
                ca.ClubDescription = description ?? string.Empty;
                ca.HighWater = highWater;

                if (!ca.FSaveToClub())
                    throw new InvalidOperationException(ca.LastError);
                ViewBag.club = club;
                ClubAircraft.RefreshClubAircraftTimes(club.ID, club.MemberAircraft);    // make sure high-water marks are set
                return PartialView("_clubAircraft");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteAircraft(int idClub, int idAircraft)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                ClubAircraft ca = club.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == idAircraft) ?? throw new InvalidOperationException(Resources.Club.errNoSuchAircraft);

                if (!ca.FDeleteFromClub())
                    throw new InvalidOperationException(ca.LastError);

                club.InvalidateMemberAircraft();

                ViewBag.club = club;
                return PartialView("_clubAircraft");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddAircraft(int idClub, int idAircraft, string description)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                // Ensure it's a valid aircraft in the user's account
                if (new UserAircraft(User.Identity.Name).GetUserAircraftByID(idAircraft) == null)
                    throw new InvalidOperationException(Resources.Club.errNoSuchAircraft);

                ClubAircraft ca = new ClubAircraft()
                {
                    AircraftID = idAircraft,
                    ClubDescription = description ?? string.Empty,
                    ClubID = idClub
                };

                if (!ca.FSaveToClub())
                    throw new InvalidOperationException(ca.LastError);
                
                club.InvalidateMemberAircraft(); // force a reload

                ViewBag.club = club;
                ClubAircraft.RefreshClubAircraftTimes(club.ID, club.MemberAircraft);    // make sure high-water marks are set
                return PartialView("_clubAircraft");
            });
        }

        [HttpPost]
        [Authorize]
        public string InviteToClub(int idClub, string szEmail)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);
                if (!RegexUtility.Email.IsMatch(szEmail))
                    throw new ArgumentException(Resources.LocalizedText.ValidationEmailFormat);

                if (club.Status == Club.ClubStatus.Inactive)
                    throw new InvalidOperationException(Branding.ReBrand(Resources.Club.errClubInactive));
                if (club.Status == Club.ClubStatus.Expired)
                    throw new InvalidOperationException(Branding.ReBrand(Resources.Club.errClubPromoExpired));

                new CFIStudentMapRequest(User.Identity.Name, szEmail, CFIStudentMapRequest.RoleType.RoleInviteJoinClub, club).Send();
                    return String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, HttpUtility.HtmlEncode(szEmail));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult FlyingReport(int idClub, bool fAsFile, DateTime dateStart, DateTime dateEnd, string reportMember, int reportAircraft, string fileFormat)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);
                ClubFlyingReport cfr = new ClubFlyingReport(idClub, dateStart, dateEnd, reportMember, reportAircraft);
                if (fAsFile)
                {
                    string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.Club.ClubReportFlying, club.Name.Replace(" ", "-"), DateTime.Now.YMDString());
                return fileFormat.CompareCurrentCultureIgnoreCase("kml") == 0 ?
                    File(cfr.RefreshKML(), "application/vnd.google-earth.kml+xml", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty) + ".kml") :
                    File(cfr.RefreshCSV(), "text/csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty) + ".csv");
                }
                else
                {
                    Response.Cookies[szCookieLastStart].Value = dateStart.Date.YMDString();
                    Response.Cookies[szCookieLastEnd].Value = dateEnd.Date.YMDString();
                    Response.Cookies[szCookieLastStart].Expires = Response.Cookies[szCookieLastEnd].Expires = DateTime.Now.AddYears(5);

                    ViewBag.items = cfr.Items;
                    return PartialView("_clubFlyingReport");
                }
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult MaintenanceReport(int idClub, bool fAsFile)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);
                ClubMaintenanceReport cmr = new ClubMaintenanceReport(idClub);
                if (fAsFile)
                {
                    string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.Club.ClubReportMaintenance, club.Name.Replace(" ", "-"), DateTime.Now.YMDString());
                    return File(cmr.RefreshCSV(), "text/csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty) + ".csv");
                }
                else
                {
                    ViewBag.items = cmr.Items;
                    return PartialView("_clubMaintenanceReport");
                }
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult InsuranceReport(int idClub, bool fAsFile, int monthsInterval)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);
                ClubInsuranceReport cir = new ClubInsuranceReport(idClub, monthsInterval);

                if (fAsFile)
                {
                    string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.Club.ClubReportInsurance, club.Name.Replace(" ", "-"), DateTime.Now.YMDString());
                    return File(cir.RefreshCSV(), "text/csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty) + ".csv");
                } else
                {
                    ViewBag.items = cir.Items;
                    return PartialView("_clubInsuranceReport");
                }
            });
        }

        [HttpPost]
        [Authorize]
        public string SaveClub(Club club)
        {
            return SafeOp(() => {
                if (club == null)
                    throw new ArgumentNullException(nameof(club));
                if (club.IsNew)
                {
                    club.Creator = User.Identity.Name;
                    club.Status = Club.StatusForUser(User.Identity.Name);
                }
                else
                {
                    Club c2 = ValidateClubAdmin(club.ID);
                    club.Status = c2.Status;    // ensure we aren't changing status of the club
                    club.Creator = c2.Creator;  // ensure that we have an owner and aren't changing it here.
                }

                if (!club.FCommit())
                    throw new InvalidOperationException(club.LastError);

                Club.ClearCachedClub(club.ID);   // cache actually doesn't have things like the airport code, so force a reload when we redirect.
                return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/mvc/club/details/{0}", club.ID));

            });
        }

        [HttpPost]
        [Authorize]
        public string DeleteClub(int idClub)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubAdmin(idClub);

                if (club.GetMember(User.Identity.Name).RoleInClub != ClubMember.ClubMemberRole.Owner)
                    throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);

                return club.FDelete() ? VirtualPathUtility.ToAbsolute("~/mvc/club/") : throw new InvalidOperationException(club.LastError);
            });
        }
        #endregion

        [HttpPost]
        [Authorize]
        public ActionResult SendMsgToClubUser(int idClub, string szTarget, string szSubject, string szText)
        {
            return SafeOp(() =>
            {
                ValidateClubMember(idClub);
                Club.ContactMember(User.Identity.Name, szTarget, szSubject, szText);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ContactClub(int idClub, string szMessage, bool fRequestMembership)
        {
            return SafeOp(() =>
            {
                Club.ContactClubAdmins(User.Identity.Name, idClub, szMessage, fRequestMembership);
                return new EmptyResult();
            });
        }

        [HttpPost]
        public ActionResult PopulateClub(int idClub)
        {
            ViewBag.club = Club.ClubWithID(idClub);
            ViewBag.linkToDetails = true;
            return PartialView("_clubDesc");
        }

        [HttpPost]
        [Authorize]
        public ActionResult ScheduleSummary(int idClub, string resourceName, bool fAllUsers)
        {
            return SafeOp(() =>
            {
                return SchedSummaryInternal(idClub, resourceName, fAllUsers);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SchedulesForAircraft(int idAircraft)
        {
            return SafeOp(() =>
            {
                List<Club> lstClubs = new List<Club>(Club.ClubsForAircraft(idAircraft, User.Identity.Name));
                if (lstClubs.Count == 0)
                    return new EmptyResult();

                ViewBag.rgClubs = lstClubs;
                ViewBag.idAircraft = idAircraft;
                return PartialView("_editAircraftViewSchedule");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AvailabilityMap(DateTime dtStart, int clubID, int limitAircraft = Aircraft.idAircraftUnknown, int cDays = 1)
        {
            return SafeOp(() =>
            {
                Club club = ValidateClubMember(clubID);
                int minutes = cDays == 1 ? 15 : cDays == 2 ? 60 : 180;

                // Sort the aircraft by tail
                List<Aircraft> lstAc = new List<Aircraft>(club.MemberAircraft);
                lstAc.Sort((ac1, ac2) => { return String.Compare(ac1.DisplayTailnumber, ac2.DisplayTailnumber, true, CultureInfo.CurrentCulture); });
                ViewBag.lstAc = lstAc;

                // update the cookie with the preferred range
                ViewBag.dtStart = dtStart;
                ViewBag.days = cDays;
                ViewBag.minutes = minutes;
                ViewBag.map = ScheduledEvent.ComputeAvailabilityMap(dtStart, club, limitAircraft = Aircraft.idAircraftUnknown, cDays, minutes);

                return PartialView("_availMap");
            });
        }
        #endregion

        #region Partials/child actions
        [ChildActionOnly]
        public PartialViewResult SchedSummaryInternal(int idClub, string resourceName, bool fAllUsers)
        {
            // Verify that the viewing user is authorized.
            Club club = ValidateClubMember(idClub);

            ViewBag.idClub = idClub;
            ViewBag.resourceName = resourceName;
            ViewBag.userName = fAllUsers ? null : User.Identity.Name;
            ViewBag.events = club.GetUpcomingEvents(10, resourceName, ViewBag.userName);
            return PartialView("_schedSummary");
        }

        [ChildActionOnly]
        public ActionResult ClubView(Club club, bool fLinkToDetails = true)
        {
            ViewBag.club = club;
            ViewBag.linkToDetails = fLinkToDetails;
            return PartialView("_clubDesc");
        }

        [ChildActionOnly]
        public ActionResult EditClub(Club club, string onSave, string onDelete)
        {
            ViewBag.club = club;
            ViewBag.onSaveFunc = onSave;
            ViewBag.onDeleteFunc = onDelete;
            return PartialView("_editClubDetails");
        }

        [ChildActionOnly]
        public ActionResult ResourceSchedule(Club club, ClubAircraft ac, string resourceHeader, int resourceID, ScheduleDisplayMode mode = ScheduleDisplayMode.Day, string navInitFunc = "", bool fIncludeDetails = false)
        {
            ViewBag.club = club;
            ViewBag.aircraft = ac;
            ViewBag.resourceHeader = resourceHeader;
            ViewBag.resourceID = resourceID;
            ViewBag.scheduleMode = mode;
            ViewBag.navInitFunc = navInitFunc;
            ViewBag.includeDetails = fIncludeDetails;
            return PartialView("_clubAircraftSchedule");
        }

        [ChildActionOnly]
        public ActionResult EditAppt(string defaultTitle = "")
        {
            ViewBag.defaulttitle = defaultTitle;
            return PartialView("_editAppt");
        }

        [ChildActionOnly]
        public ActionResult MemberList(Club club)
        {
            ViewBag.club = club;
            return PartialView("_memberList");
        }

        [ChildActionOnly]
        public ActionResult AircraftList(Club club)
        {
            ViewBag.club = club ?? throw new ArgumentNullException(nameof(club));
            ClubAircraft.RefreshClubAircraftTimes(club.ID, club.MemberAircraft);    // make sure high-water marks are set
            return PartialView("_clubAircraft");
        }
        #endregion

        [HttpGet]
        [Authorize]
        public ActionResult AppointmentDownload(int idClub, string sid, string fmt = "")
        {
            Club club = ValidateClubMember(idClub);

            if (fmt.CompareCurrentCultureIgnoreCase("Y") == 0)
                return Redirect(ScheduledEvent.WriteYahoo(club, sid, User.Identity.Name, Request.Url.Host).ToString());
            else if (fmt.CompareCurrentCultureIgnoreCase("G") == 0)
                return Redirect(ScheduledEvent.WriteGoogle(club, sid, User.Identity.Name, Request.Url.Host).ToString());
            else
                return File(ScheduledEvent.WriteICal(club, sid, User.Identity.Name, out string szFileName), "text/calendar", szFileName);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult LeaveClub(int idClub)
        {
            Club club = ValidateClubMember(idClub);

            ClubMember cm = club.Members.FirstOrDefault(pf => String.Compare(pf.UserName, User.Identity.Name, StringComparison.Ordinal) == 0);
            if (cm.RoleInClub == ClubMember.ClubMemberRole.Member)
            {
                cm.FDeleteClubMembership();
                return Redirect("~/mvc/Club");
            } else
                throw new UnauthorizedAccessException(Resources.Club.errNotAuthorized);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadSchedule(int idClub, DateTime dateDownloadFrom, DateTime dateDownloadTo)
        {
            Club club = ValidateClubMember(idClub);

            // if from is after to, then just swap them.
            if (dateDownloadTo.CompareTo(dateDownloadFrom) < 0)
                (dateDownloadFrom, dateDownloadTo) = (dateDownloadTo, dateDownloadFrom);

            IEnumerable<ScheduledEvent> rgevents = ScheduledEvent.AppointmentsInTimeRange(dateDownloadFrom.Date, dateDownloadTo.Date, club.ID, club.TimeZone);
            club.MapAircraftAndUsers(rgevents);  // fix up aircraft, usernames
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.Club.DownloadClubScheduleFileName, club.Name.Replace(" ", "-"));

            return File(ScheduledEvent.DownloadScheduleTable(rgevents), "text/csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty) + ".csv");
        }

        [HttpGet]
        [Authorize]
        public ActionResult ACSchedule(int id = -1)
        {
            if (id != Aircraft.idAircraftUnknown && User.Identity.IsAuthenticated && !String.IsNullOrEmpty(User.Identity.Name))
            {
                IEnumerable<Club> lstClubsForUserInAircraft = Club.ClubsForAircraft(id, User.Identity.Name);
                IEnumerable<Club> lstClubsForAircraft = (lstClubsForUserInAircraft.Any() ? Array.Empty<Club>() : Club.ClubsForAircraft(id));
                ViewBag.rgClubs = lstClubsForUserInAircraft;
                ViewBag.rgAvailableClubs = lstClubsForAircraft;
                ViewBag.ac = new Aircraft(id);
                return View("clubACSchedule");
            }
            else
                throw new UnauthorizedAccessException("Invalid user or aircraft");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Details(int id = 0, int a = 0)
        {
            if (id == 0)
                return Redirect("~/mvc/club");
            Club club = Club.ClubWithID(id);
            bool fIsAdmin = a != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            ClubMember cm = club.GetMember(User.Identity.Name);
            if (fIsAdmin && cm == null)
                cm = new ClubMember(club.ID, User.Identity.Name, ClubMember.ClubMemberRole.Admin);

            ViewBag.club = club;
            ViewBag.fIsAdmin = fIsAdmin;
            ViewBag.cm = cm;

            return View("clubDetails");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Manage(int id = 0)
        {
            if (id == 0)
                return Redirect("~/mvc/club");
            Club club = ValidateClubAdmin(id);

            UserAircraft ua = new UserAircraft(User.Identity.Name);
            List<Aircraft> lst = new List<Aircraft>(ua.GetAircraftForUser());
            lst.RemoveAll(ac => ac.IsAnonymous || club.MemberAircraft.FirstOrDefault(ca => ca.AircraftID == ac.AircraftID) != null); // remove all anonymous aircraft, or aircraft that are already in the list

            ViewBag.club = club;
            club.InvalidateMemberAircraft();    // force things like high water marks
            ViewBag.cm = club.GetMember(User.Identity.Name);
            ViewBag.candidateAircraft = lst;

            ViewBag.defaultStartReport = Request.Cookies[szCookieLastStart] != null && DateTime.TryParse(Request.Cookies[szCookieLastStart].Value, out DateTime dtStart) ? dtStart: club.CreationDate;
            ViewBag.defaultEndReport = (Request.Cookies[szCookieLastEnd] != null && DateTime.TryParse(Request.Cookies[szCookieLastEnd].Value, out DateTime dtEnd)) ? dtEnd : DateTime.Now;
            return View("clubManage");
        }

        private IEnumerable<Club> FillClubViewBag()
        {
            Club.ClubStatus status = Club.StatusForUser(User.Identity.IsAuthenticated ? User.Identity.Name : null);
            IEnumerable<Club> clubsForUser = (status == Club.ClubStatus.Inactive) ? Array.Empty<Club>() : Club.AllClubsForUser(User.Identity.Name);

            ViewBag.ownedClubs = clubsForUser;
            ViewBag.clubStatus = status;
            ViewBag.clubTrialStatus = Branding.ReBrand(User.Identity.IsAuthenticated ? (status == Club.ClubStatus.Promotional ? Resources.Club.ClubCreateTrial : Resources.Club.ClubCreateNoTrial) : Resources.Club.MustBeMember);

            return clubsForUser;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string szAirport = "")
        {
            FillClubViewBag();

            szAirport = (szAirport ?? string.Empty).Trim();

            GoogleMap googleMap = new GoogleMap("divClubAirports", GMap_Mode.Dynamic)
            {
                ClubClickHandler = "displayClubDetails"
            };

            bool fAdmin = User.Identity.IsAuthenticated && util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            IEnumerable<Club> rgClubs = Array.Empty<Club>();
            if (String.IsNullOrEmpty(szAirport))
                rgClubs = Club.AllClubs(fAdmin);
            else
            {
                AirportList al = new AirportList(szAirport);
                List<airport> lst = new List<airport>(al.GetAirportList());
                lst.RemoveAll(ap => !ap.IsPort);

                if (lst.Count == 0)
                    ViewBag.errorText = Resources.Club.errHomeAirportNotFound;
                else
                {
                    rgClubs = Club.ClubsNearAirport(lst[0].Code, fAdmin);
                    googleMap.SetAirportList(al);
                }
            }

            googleMap.Clubs = rgClubs;
            ViewBag.Map = googleMap;
            ViewBag.searchedAirport = szAirport.ToUpper(CultureInfo.CurrentCulture);

            return View("clubs");
        }

        // GET: mvc/Club
        [HttpGet]
        public ActionResult Index(int noredir = 0)
        {
            // No specific club requested - show page where
            //  a) if you are in a club and have exactly one club, you get redirected to that (unless "noredir" is specified), or
            //  b) you can create or find one.
            IEnumerable<Club> clubsForUser = FillClubViewBag();

            // Exactly one club: redirect unless "noredir=1" is specified.
            return (clubsForUser.Count() == 1 && noredir == 0) ? (ActionResult)
                Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/Club/Details/{0}", clubsForUser.First().ID)) : View("clubs");
        }
    }
}