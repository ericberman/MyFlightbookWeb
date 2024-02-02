using System;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminUserController : Controller
    {
        [HttpPost]
        [Authorize]
        public ActionResult findUsers(string szSearch)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            ViewBag.FoundUsers = ProfileAdmin.FindUsers(szSearch);
            return PartialView("_foundUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Impersonate(string szUserPKID)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException("Attempt to access an admin page by an unauthorized user: " + User.Identity.Name);

            string szUser = ProfileAdmin.UserFromPKID(szUserPKID).UserName;

            if (String.Compare(szUser, User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                throw new InvalidOperationException("Can't emulate yourself, silly!");

            ProfileRoles.ImpersonateUser(User.Identity.Name, szUser);
            return Redirect("~/Member/LogbookNew.aspx");
        }

        // GET: mvc/AdminUser
        [Authorize]
        public ActionResult Index()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException("Attempt to access an admin page by an unauthorized user: " +  User.Identity.Name);

            ViewBag.DupeUsers = ProfileAdmin.ADMINDuplicateUsers();
            ViewBag.LockedUsers = ProfileAdmin.ADMINLockedUsers();
            return View("adminUser");
        }
    }
}