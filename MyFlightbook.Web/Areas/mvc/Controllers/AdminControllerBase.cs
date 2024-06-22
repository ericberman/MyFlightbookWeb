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