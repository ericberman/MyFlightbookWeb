using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminUserController : AdminControllerBase
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
        [Authorize]
        public ActionResult SetRoleForUser(string szRole, string szTargetUser, string szPass)
        {
            return SafeOp(() =>
            {
                if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanDoAllAdmin)
                    throw new UnauthorizedAccessException();

                ProfileRoles.UserRoles role = Enum.TryParse<ProfileRoles.UserRoles>(szRole, out ProfileRoles.UserRoles userRoles) ? userRoles : throw new InvalidOperationException($"Role '{szRole}' is not valid");
                ProfileAdmin.SetRoleForUser(role, szTargetUser, User.Identity.Name, szPass);
                return new EmptyResult();
            });
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

            ImpersonateUser(User.Identity.Name, szUser);
            return Redirect("~/mvc/flights");
        }

        // GET: mvc/AdminUser
        [Authorize]
        public async Task<ActionResult> Index()
        {
            // Validate authorization.  Since the admin tab comes here and reporters and accountants have the admin tab but can't manage users,
            // redirect to those as appropriate.
            Profile pfUser = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (!pfUser.CanSupport)
            {
                if (pfUser.CanReport)
                    return Redirect("~/mvc/Admin/Stats");
                else if (pfUser.CanManageMoney)
                    return Redirect("~/mvc/AdminPayment");

                throw new UnauthorizedAccessException($"Attempt to access an admin page by an unauthorized user: {User.Identity.Name}");
            }

            // Was getting some issues with assigning ViewBag values directly within async tasks, so use local 
            // variables and then assign to ViewBag synchronously.
            IEnumerable<string> rgDupeUsers = null;
            IEnumerable<ProfileAdmin> rgLockedUsers = null;
            IEnumerable<Profile> rgAdminUsers = null;

            await Task.WhenAll(
                    Task.Run(() => { rgDupeUsers = ProfileAdmin.ADMINDuplicateUsers(); }),
                    Task.Run(() => { rgLockedUsers = ProfileAdmin.ADMINLockedUsers(); }),
                    Task.Run(() => { rgAdminUsers = ProfileAdmin.ADMINAdminUsers(); })
                );

            ViewBag.DupeUsers = rgDupeUsers;
            ViewBag.LockedUsers = rgLockedUsers;
            ViewBag.adminUsers = rgAdminUsers;

            return View("adminUser");
        }
    }
}