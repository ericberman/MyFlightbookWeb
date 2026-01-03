using Google.Authenticator;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
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

        #region Impersonation
        /// <summary>
        /// Returns the username of the user who might be doing any impersonation.
        /// </summary>
        public string OriginalAdmin
        {
            get { return Request.Cookies[MFBConstants.keyOriginalID]?.Value ?? string.Empty; }
        }

        /// <summary>
        /// Returns true if the current user is being impersonated
        /// </summary>
        /// <param name="szCurrentUser">The username of the current user</param>
        /// <returns>True if the current user is being emulated by an admin</returns>
        public bool IsImpersonating(string szCurrentUser)
        {
            bool fIsImpersonatingCookie = false;
            HttpCookie cookie = Request.Cookies[MFBConstants.keyIsImpersonating];
            if (cookie != null)
            {
                if (!bool.TryParse(cookie.Value, out fIsImpersonatingCookie))
                    Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(-1);
            }

            // to be impersonating, must be both an admin and have the impersonating cookie set and be impersonating someone other than yourself.
            string szOriginalAdmin = OriginalAdmin;
            return fIsImpersonatingCookie && String.Compare(szOriginalAdmin, szCurrentUser, StringComparison.Ordinal) != 0 && MyFlightbook.Profile.GetUser(szOriginalAdmin).CanSupport;
        }

        /// <summary>
        /// If currently impersonating, stops the impersonation
        /// </summary>
        public void StopImpersonating()
        {
            if (Request.Cookies[MFBConstants.keyIsImpersonating] != null)
                Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(-1);
            if (Response.Cookies[MFBConstants.keyOriginalID] != null)
            {
                string szUser = Request.Cookies[MFBConstants.keyOriginalID].Value;
                MembershipUser mu = (szUser == null) ? null : Membership.GetUser(szUser);
                if (!String.IsNullOrEmpty(mu?.UserName))
                {
                    FormsAuthentication.SetAuthCookie(Request.Cookies[MFBConstants.keyOriginalID].Value, true);
                    Profile pf = MyFlightbook.Profile.GetUser(mu.UserName);
                    Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                        ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                        : (object)null;
                    Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
                }
                else
                    FormsAuthentication.SignOut();
                Response.Cookies[MFBConstants.keyOriginalID].Expires = DateTime.Now.AddDays(-1);
            }
        }

        /// <summary>
        /// Starts impersonating the specified person
        /// </summary>
        /// <param name="szAdminName">The admin name to impersonate</param>
        /// <param name="szTargetName">The impersonation target</param>
        public void ImpersonateUser(string szAdminName, string szTargetName)
        {
            Response.Cookies[MFBConstants.keyOriginalID].Value = szAdminName;
            Response.Cookies[MFBConstants.keyOriginalID].Expires = DateTime.Now.AddDays(30);
            FormsAuthentication.SetAuthCookie(szTargetName, true);
            Response.Cookies[MFBConstants.keyIsImpersonating].Value = true.ToString(CultureInfo.InvariantCulture);
            Response.Cookies[MFBConstants.keyIsImpersonating].Expires = DateTime.Now.AddDays(30);

            Profile pf = MyFlightbook.Profile.GetUser(szTargetName);
            Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                : (object)null;
            Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
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

        #region Utilities for parameters
        /// <summary>
        /// HttpRequestBase variant of GetStringParam
        /// </summary>
        /// <param name="req"></param>
        /// <param name="szKey"></param>
        /// <returns></returns>
        protected string GetStringParam(string szKey)
        {
            if (String.IsNullOrEmpty(szKey))
                throw new ArgumentNullException(nameof(szKey));
            return Request[szKey] ?? string.Empty;
        }

        /// <summary>
        /// HttpRequestBase variant of GetIntParam
        /// </summary>
        /// <param name="req"></param>
        /// <param name="szKey"></param>
        /// <returns></returns>
        protected int GetIntParam(string szKey, int defaultValue)
        {
            if (String.IsNullOrEmpty(szKey))
                throw new ArgumentNullException(nameof(szKey));

            return (Request[szKey] ?? string.Empty).SafeParseInt(defaultValue);
        }

        #endregion

        #region Utilities for mobile/full page
        protected const string keyLite = "Lite";   // Show a lightweight version of the site?
        protected const string keyClassic = "Classic"; // persistant cookie - show a full version of the site even on mobile devices?

        /// <summary>
        /// Switches to/from mobile state (overriding default detection) by setting the appropriate session variables.
        /// </summary>
        /// <param name="fMobile">True for the mobile state</param>
        public void SetMobile(bool fMobile)
        {
            if (fMobile)
            {
                Response.Cookies[keyClassic].Value = null; // let autodetect do its thing next time...
                Request.Cookies[keyClassic].Value = null;
                Session[keyLite] = bool.TrueString; // ...but keep it lite for the session
            }
            else
            {
                Response.Cookies[keyClassic].Value = "yes"; // override autodetect
                Request.Cookies[keyClassic].Value = "yes";
                Session[keyLite] = null; // and hence there should be no need for a session variable.
            }
        }

        /// <summary>
        /// Determines if this is a mobile session.  Pays attention to cookies and session state
        /// </summary>
        /// <returns>True if we should be treating this as a mobile session</returns>
        public bool IsMobileSession()
        {
            return (Request.IsMobileDevice() && ((Request.Cookies[keyClassic]?.Value ?? string.Empty) != "yes")) || ((string) (Session[keyLite] ?? string.Empty)) == bool.TrueString;
        }

        #endregion
    }
}