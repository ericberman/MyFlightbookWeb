using MyFlightbook.Utility.Email;
using System;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Mvc;

/******************************************************
    * 
    * Copyright (c) 2015-2025 MyFlightbook LLC
    * Contact myflightbook-at-gmail.com for more information
    *
   *******************************************************/

namespace MyFlightbook.Web
{
    public class MFBGlobal : HttpApplication
    {
       protected void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();

            // CoordinateSharp can be very slow - pegging CPU - due to EagerLoading, which matters for celestial computations that we generally don't care about, so just set the default to NOT do eager load.
            CoordinateSharp.GlobalSettings.Default_EagerLoad = new CoordinateSharp.EagerLoad(false);

            // Dependency Injection

            // Do a quick MySQL request to prime the connection cache
            DBHelper dBHelper = new DBHelper("SELECT Version();");
            bool _ = dBHelper.ReadRow((comm) => { }, (dr) => { });

            ShuntState.Init(ConfigurationManager.AppSettings[ShuntState.keyShuntState].CompareOrdinalIgnoreCase("Shunted") == 0, ConfigurationManager.AppSettings[ShuntState.keyShuntMsg]);
            util.InitEmail(new SmtpSupport());
            util.InitRequestContext(new MVCRequestContext());
        }

        protected void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            // Issue #476: Switch from www.myflightbook.com to myflightbook.com with 301 error; this helps google analytics
            Uri uri = HttpContext.Current?.Request?.Url;
            if (uri is null)
                return;

            if (uri.Host.CompareCurrentCultureIgnoreCase("www.myflightbook.com") == 0 && HttpContext.Current.Request.HttpMethod.CompareCurrentCultureIgnoreCase("GET") == 0)
            {
                Response.Status = "301 Moved Permanently";
                Response.AddHeader("Location", uri.ToString().Replace("www.myflightbook.com", "myflightbook.com"));
                Response.End();
            }

            if (Request?.UserLanguages != null && Request.UserLanguages.Length > 0)
                util.SetCulture(Request.UserLanguages[0]);
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            // Issue #1069 - remember the most recently used locale so that we can use that for things like emails.
            if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Profile pf = Profile.GetUser(HttpContext.Current.User.Identity.Name);
                string szCulture = System.Globalization.CultureInfo.CurrentCulture.Name ?? string.Empty;
                if (szCulture.CompareCurrentCultureIgnoreCase(MFBConstants.USCulture) == 0)
                {
                    // US culture - delete alternative if present
                    if (pf.PreferenceExists(MFBConstants.keyPrefLastUsedLocale))
                        pf.SetPreferenceForKey(MFBConstants.keyPrefLastUsedLocale, string.Empty, true);
                }
                else
                {
                    // non-us culture - save the new one if not already saved
                    if (!pf.PreferenceExists(MFBConstants.keyPrefLastUsedLocale) || szCulture.CompareCurrentCultureIgnoreCase(pf.GetPreferenceForKey(MFBConstants.keyPrefLastUsedLocale) as string) != 0)
                        pf.SetPreferenceForKey(MFBConstants.keyPrefLastUsedLocale, szCulture);
                }
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            StringBuilder ErrorMessage = new StringBuilder();
            Exception myError = Server.GetLastError();

            if (myError is HttpRequestValidationException)
            {
                Context.ClearError();
                Response.Redirect("~/mvc/pub/DangerousRequest");
                Response.End();
                return;
            }

            if (Context.Request.IsLocal)
                return;

            if (myError is UnauthorizedAccessException || (myError.InnerException != null && myError.InnerException is UnauthorizedAccessException))
            {
                Context.ClearError();
                Response.Redirect("~/HTTP403.htm");
                Response.End();
                return;
            }

            if ((myError is HttpException err && err.GetHttpCode() == 404))
                return;

            if (myError is HttpAntiForgeryException)
            {
                if (!(Context?.User?.Identity?.IsAuthenticated ?? true))
                    return;
                Context.ClearError();
                Response.Redirect("~/mvc/pub/SessionExpired");
                Response.End();
                return;
            }

            if (Context != null)
            {
                ErrorMessage.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Request = {0}://{1}{2}\r\nUser = {3}\r\nLanguage={4}\r\nBy: {5} {6} {7}\r\n\r\n\r\n", Context.Request.IsSecureConnection ? "https" : "http",
                    Context.Request.Url.Host, Context.Request.Url.PathAndQuery, Context.User.Identity.Name,
                    Context.Request.UserLanguages == null ? "(none)" : String.Join(", ", Context.Request.UserLanguages),
                Context.Request.UserAgent, Context.Request.UserHostAddress, Context.Request.UserHostName);
                if (Context.Request != null && Context.Request.UrlReferrer != null)
                    ErrorMessage.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Referrer requested: {0}\r\n", Context.Request.UrlReferrer.ToString());

                string szKey = Context.User.Identity.Name + "-LastPage";
                if (Session[szKey] != null)
                    ErrorMessage.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Last page requested by user = {0}\r\n", Session[szKey].ToString()));
                Session[szKey] = null;  // clear it out
            }

            util.NotifyAdminException(ErrorMessage.ToString(), myError);
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Make sure that the decimal setting is set correctly
            // A bit of a hack, but it's the only way to ensure that this is passed everywhere it is needed appropriately.
            if (!String.IsNullOrEmpty(Context?.User.Identity?.Name))
            {
                Profile pf = Profile.GetUser(Context.User.Identity.Name);
                if (pf.PreferenceExists(MFBConstants.keyDecimalSettings))
                    Session[MFBConstants.keyDecimalSettings] = pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings);
                Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
            }
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.
        }
    }
}