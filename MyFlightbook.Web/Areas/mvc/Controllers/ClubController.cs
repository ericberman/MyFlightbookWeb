using MyFlightbook.Airports;
using MyFlightbook.Clubs;
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
        #region WebServices
        [HttpPost]
        [Authorize]
        public ActionResult SendMsgToClubUser(string szTarget, string szSubject, string szText)
        {
            return SafeOp(() =>
            {
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
                    Club c2 = Club.ClubWithID(club.ID);
                    if (c2 == null)
                        throw new InvalidOperationException(Resources.Club.errNoSuchClub);
                    else if (!c2.HasAdmin(User.Identity.Name))
                        throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);
                    club.Status = c2.Status;    // ensure we aren't changing status
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
                Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);
                
                if (club.GetMember(User.Identity.Name).RoleInClub != ClubMember.ClubMemberRole.Owner)
                    throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);

                return club.FDelete() ? VirtualPathUtility.ToAbsolute("~/mvc/club/") : throw new InvalidOperationException(club.LastError);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ScheduleSummary(int idClub, string resourceName, bool fAllUsers)
        {
            return SafeOp(() =>
            {
                // Verify that the viewing user is authorized.
                Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);
                if (!club.HasMember(User.Identity.Name))
                    throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);

                ViewBag.idClub = idClub;
                ViewBag.resourceName = resourceName;
                ViewBag.userName = fAllUsers ? null : User.Identity.Name;
                ViewBag.events = club.GetUpcomingEvents(10, resourceName, ViewBag.userName);
                return PartialView("_schedSummary");
            });
        }
        #endregion

        #region Partials/child actions
        [ChildActionOnly]
        public ActionResult ClubView(Club club, bool fLinkToDetails = true)
        {
            ViewBag.club = club;
            ViewBag.linkToDetails = fLinkToDetails;
            return PartialView("_clubDesc");
        }

        [ChildActionOnly]
        public ActionResult EditClub(Club club)
        {
            ViewBag.club = club;
            return PartialView("_editClubDetails");
        }

        [ChildActionOnly]
        public ActionResult ResourceSchedule(Club club, ClubAircraft ac, string resourceHeader, int resourceID, bool showNavContainer = false, ScheduleDisplayMode mode = ScheduleDisplayMode.Day, string navInitFunc = "")
        {
            ViewBag.club = club;
            ViewBag.aircraft = ac;
            ViewBag.resourceHeader = resourceHeader;
            ViewBag.resourceID = resourceID;
            ViewBag.showNavContainer = showNavContainer;
            ViewBag.scheduleMode = mode;
            ViewBag.navInitFunc = navInitFunc;
            return PartialView("_clubAircraftSchedule");
        }

        [ChildActionOnly]
        public ActionResult EditAppt(string defaultTitle = "")
        {
            ViewBag.defaulttitle = defaultTitle;
            return PartialView("_editAppt");
        }
        #endregion

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult LeaveClub(int idClub)
        {
            Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);
            if (!club.HasMember(User.Identity.Name))
                throw new UnauthorizedAccessException(Resources.Club.errNotAuthorized);

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
            Club club = Club.ClubWithID(idClub) ?? throw new InvalidOperationException(Resources.Club.errNoSuchClub);
            if (!club.HasMember(User.Identity.Name))
                throw new UnauthorizedAccessException(Resources.Club.errNotAuthorizedToManage);

            // if from is after to, then just swap them.
            if (dateDownloadTo.CompareTo(dateDownloadFrom) < 0)
                (dateDownloadFrom, dateDownloadTo) = (dateDownloadTo, dateDownloadFrom);

            IEnumerable<ScheduledEvent> rgevents = ScheduledEvent.AppointmentsInTimeRange(dateDownloadFrom.Date, dateDownloadTo.Date, club.ID, club.TimeZone);
            club.MapAircraftAndUsers(rgevents);  // fix up aircraft, usernames
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.Club.DownloadClubScheduleFileName, club.Name.Replace(" ", "-"));

            return File(ScheduledEvent.DownloadScheduleTable(rgevents), "text/csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", string.Empty) + ".csv");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Details(int id = 0, int a = 0)
        {
            if (id == 0)
                return Redirect(string.Empty);
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