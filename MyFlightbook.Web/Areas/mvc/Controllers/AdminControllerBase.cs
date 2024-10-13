using Google.Authenticator;
using System;
using System.IO;
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
        #region authentication/authorization
        /// <summary>
        /// Determines if the user has the requested role (for admin operations).
        /// </summary>
        /// <param name="roleMask"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected void CheckAuth(uint roleMask)
        {
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);

            if (!User.Identity.IsAuthenticated || (((uint)MyFlightbook.Profile.GetUser(User.Identity.Name).Role & roleMask) == 0))
                throw new UnauthorizedAccessException("Attempt to access an admin page by an unauthorized user: " + User.Identity.Name);
        }

        protected bool IsLocalCall()
        {
            string szIPThis = Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
            return Request.UserHostAddress.CompareCurrentCultureIgnoreCase(szIPThis) == 0;
        }
        #endregion

        #region Non-standard page renderings
        public static string RenderViewToString(ControllerContext context, string viewName, object model)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var viewEngineResult = ViewEngines.Engines.FindView(context, viewName, null);
            if (viewEngineResult.View == null)
                throw new FileNotFoundException($"View '{viewName}' was not found.");

            var view = viewEngineResult.View;
            context.Controller.ViewData.Model = model;

            using (var stringWriter = new StringWriter())
            {
                var viewContext = new ViewContext(context, view, context.Controller.ViewData, context.Controller.TempData, stringWriter);
                view.Render(viewContext, stringWriter);
                return stringWriter.GetStringBuilder().ToString();
            }
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
                if (ShuntState.IsShunted)
                    throw new MyFlightbookException(ShuntState.ShuntMessage);
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
                if (ShuntState.IsShunted)
                    throw new MyFlightbookException(ShuntState.ShuntMessage);
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
                if (ShuntState.IsShunted)
                    throw new MyFlightbookException(ShuntState.ShuntMessage);
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

        #region Utilities for redirecting based on passed query parameters
        /// <summary>
        /// Determines if a specified url fragment passed as a parameter is "safe" to return to: specifically, it's not empty, and it's relative (so it stays on this site).
        /// If safe, redirects to it; otherwise, redirects to the fallback
        /// </summary>
        /// <param name="retHref">The "Return" parameter</param>
        /// <param name="hrefFallback">The fallback redirect</param>
        /// <returns></returns>
        protected ActionResult SafeRedirect(string retHref, string hrefFallback)
        {
            return Redirect(SafeRedirectParam(retHref, hrefFallback));
        }

        /// <summary>
        /// Returns a safe-to-use relative URI string from possibly user-tainted data (e.g.., a query string)
        /// </summary>
        /// <param name="href">The passed parameter</param>
        /// <param name="hrefFallback">An optional fallback to use, if the parameter is unsafe</param>
        /// <returns>The passed href if it is strictly relative, otherwise the hrefFallBack, if provided, else an empty string</returns>
        protected static string SafeRedirectParam(string href, string hrefFallback = null)
        {
            return !String.IsNullOrWhiteSpace(href) && Uri.IsWellFormedUriString(href, UriKind.Relative) ? href : hrefFallback ?? string.Empty;
        }
        #endregion

        #region Utilities for guarding with 2 factor auth

        /// <summary>
        /// Checks to see if the provided code passes 2fa or is unnecessary.  Throws an exception if it's not a valid code
        /// </summary>
        /// <param name="pf"></param>
        /// <param name="code"></param>
        protected static bool Check2FA(Profile pf, string code)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            if (pf.PreferenceExists(MFBConstants.keyTFASettings))
            {
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                if (!tfa.ValidateTwoFactorPIN(pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string, code, new TimeSpan(0, 2, 0)))
                {
                    // TFA is required but a correct value was not passed
                    System.Threading.Thread.Sleep(1000); // pause for a second to thwart dictionary attacks.
                    return false;
                }
            }
            // 2fa not set up or was correctly provided.
            return true;
        }

        protected static void SetUp2FA(Profile pf, string seed, string verification)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            if (pf.PreferenceExists(MFBConstants.keyTFASettings))
                throw new InvalidOperationException("2 factor authentication already set up for user " + pf.UserName);

            if (String.IsNullOrWhiteSpace(verification))
                throw new InvalidOperationException(Resources.Profile.TFAErrCodeFormat);
            if (String.IsNullOrWhiteSpace(seed))
                throw new InvalidOperationException("No seed provided - how?");

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            if (!tfa.ValidateTwoFactorPIN(seed, verification, new TimeSpan(0, 2, 0)))
                throw new InvalidOperationException(Resources.Profile.TFACodeFailed);

            pf.SetPreferenceForKey(MFBConstants.keyTFASettings, seed);
        }
        #endregion
    }
}