using MyFlightbook.Instruction;
using MyFlightbook.Web.Sharing;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminControllerBase : Controller
    {
        #region Check if you are authorized for a given operation
        /// <summary>
        /// Determines if the user has the requested role (for admin operations).
        /// </summary>
        /// <param name="roleMask"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected void CheckAuth(uint roleMask)
        {
            if (!User.Identity.IsAuthenticated || (((uint)MyFlightbook.Profile.GetUser(User.Identity.Name).Role & roleMask) == 0))
                throw new UnauthorizedAccessException("Attempt to access an admin page by an unauthorized user: " + User.Identity.Name);
        }

        /// <summary>
        /// Determines if the viewing user has access to view flight data for the specified target user
        /// </summary>
        /// <param name="targetUser">User whose data is being accessed</param>
        /// <param name="viewingUser">Viewing user (should be User.identity.name, but null is a shortcut for that)</param>
        /// <param name="sk">An optional sharekey that may provide access</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected void CheckCanViewFlights(string targetUser, string viewingUser, ShareKey sk = null)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));

            // three allowed conditions:
            // a) Viewing user (Authenticated or not!) has a valid sharekey for the user and can view THAT USER's flights.  This is the only unauthenticated access allowed
            if ((sk?.CanViewFlights ?? false) && (sk?.Username ?? string.Empty).CompareOrdinal(targetUser) == 0)
                return;

            if (User.Identity.IsAuthenticated)
            {
                // Viewing user should ALWAYS be the authenticated user; null just means "use the logged user
                viewingUser = viewingUser ?? User.Identity.Name;

                if (viewingUser.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("Supplied viewing user is different from the authenticated user - that should never happen!");

                // b) Authenticated, viewing user is target user
                if (targetUser.CompareOrdinal(viewingUser) == 0)
                    return;

                // c) Authenticated, viewing user is an instructor of target user and user has given permission to view logbook
                if (CFIStudentMap.GetInstructorStudent(new CFIStudentMap(viewingUser).Students, targetUser)?.CanViewLogbook ?? false)
                    return;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException("Not authorized to view this user's logbook data");
        }
        #endregion

        #region SafeOp - return an actionresult with a naked error (no html error) as oppropriate
        /// <summary>
        /// Perform an action that returns an ActionResult, returning a (naked) error message if needed.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected ActionResult SafeOp(uint roleMask, Func<ActionResult> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                CheckAuth(roleMask);

                return func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return Content(ex.Message);
            }
        }

        protected async Task<ActionResult> SafeOp(uint roleMask, Func<Task<ActionResult>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                CheckAuth(roleMask);

                return await func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return Content(ex.Message);
            }
        }

        /// <summary>
        /// Perform an action that returns an ActionResult, returning a (naked) error message
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        protected string SafeOp(uint roleMask, Func<string> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                CheckAuth(roleMask);

                return func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return ex.Message;
            }
        }

        /// <summary>
        /// Performs an operation returning any exception as text (not limited to admin)
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected string SafeOp(Func<string> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                return func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return ex.Message;
            }
        }

        /// <summary>
        /// Performs an operation returning any exception as text (not limited to admin)
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected ActionResult SafeOp(Func<ActionResult> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                return func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return Content(ex.Message);
            }
        }


        /// <summary>
        /// Performs an async operation returning any exception as ActionResult (not limited to admin)
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected async Task<ActionResult> SafeOp(Func<Task<ActionResult>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            try
            {
                return await func();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return Content(ex.Message);
            }
        }
        #endregion
    }
}