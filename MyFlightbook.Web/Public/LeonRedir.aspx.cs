using DotNetOpenAuth.OAuth2;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.Leon
{
    public partial class LeonRedir : Page
    {
        #region Properties
        protected IAuthorizationState AuthState { get; set; }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            Profile pf = null;
            Title = Resources.LogbookEntry.LeonImportHeader;

            btnDeAuth.Text = Branding.ReBrand(Resources.LogbookEntry.LeonDeauthorize);
            lblNoAuthToken.Text = Branding.ReBrand(Resources.LogbookEntry.LeonUnauthorized);
            lnkAuthorize.Text = Branding.ReBrand(Resources.LogbookEntry.LeonAuthorize);
            lblAuthedHeader.Text = Branding.ReBrand(Resources.LogbookEntry.LeonAuthorizedHeader);

            if (User.Identity.IsAuthenticated)
            {
                pf = Profile.GetUser(User.Identity.Name);
                AuthState = pf.GetPreferenceForKey<AuthorizationState>(LeonClient.TokenPrefKey);

                if (!String.IsNullOrEmpty(Request["code"]))
                {
                    AuthState = new LeonClient(LeonClient.UseSandbox(Request.Url.Host)).ConvertToken(Request);
                    pf.SetPreferenceForKey(LeonClient.TokenPrefKey, AuthState, AuthState == null);
                    Response.Redirect(Request.Url.AbsolutePath);
                }

                if (AuthState == null)
                    mvLeonState.SetActiveView(vwNoAuthToken);
                else
                {
                    mvLeonState.SetActiveView(vwAuthorized);
                    btnDeAuth.Text = Branding.ReBrand(Resources.LogbookEntry.LeonDeauthorize);
                }
            }
            else
                mvLeonState.SetActiveView(vwUnauthenticated);
        }

        protected void lnkAuthorize_Click(object sender, EventArgs e)
        {
            new LeonClient(LeonClient.UseSandbox(Request.Url.Host)).Authorize(new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/LeonRedir.aspx"))));
        }

        protected void btnDeAuth_Click(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(LeonClient.TokenPrefKey, null, true);
            AuthState = null;
            Response.Redirect(Request.Path);
        }
    }
}