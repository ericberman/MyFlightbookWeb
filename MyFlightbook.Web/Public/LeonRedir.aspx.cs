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
            Title = Resources.LogbookEntry.LeonImportHeader;

            btnDeAuth.Text = Branding.ReBrand(Resources.LogbookEntry.LeonDeauthorize);
            lblNoAuthToken.Text = Branding.ReBrand(Resources.LogbookEntry.LeonUnauthorized);
            lnkAuthorize.Text = Branding.ReBrand(Resources.LogbookEntry.LeonAuthorize);
            lblAuthedHeader.Text = Branding.ReBrand(Resources.LogbookEntry.LeonAuthorizedHeader);

            if (User.Identity.IsAuthenticated)
            {
                Profile pf = Profile.GetUser(User.Identity.Name);
                AuthState = pf.GetPreferenceForKey<AuthorizationState>(LeonClient.TokenPrefKey);
                string szSubDomain = pf.GetPreferenceForKey<string>(LeonClient.SubDomainPrefKey);

                if (!String.IsNullOrEmpty(Request["code"]) && !String.IsNullOrWhiteSpace(szSubDomain))
                {
                    txtSubDomain.Text = szSubDomain;
                    AuthState = new LeonClient(szSubDomain, LeonClient.UseSandbox(Request.Url.Host)).ConvertToken(Request);
                    pf.SetPreferenceForKey(LeonClient.TokenPrefKey, AuthState, AuthState == null);
                    Response.Redirect(Request.Url.AbsolutePath);
                }
                else 
                    txtSubDomain.Text = HttpUtility.HtmlEncode(util.GetStringParam(Request, "subdomain"));

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
            if (Page.IsValid)
            {
                Profile pf = Profile.GetUser(User.Identity.Name);
                pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, txtSubDomain.Text, String.IsNullOrEmpty(txtSubDomain.Text));
                new LeonClient(txtSubDomain.Text, LeonClient.UseSandbox(Request.Url.Host)).Authorize(new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/LeonRedir.aspx"))));
            }
        }

        protected void btnDeAuth_Click(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(LeonClient.TokenPrefKey, null, true);
            pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, null, true);
            AuthState = null;
            Response.Redirect(Request.Path);
        }
    }
}