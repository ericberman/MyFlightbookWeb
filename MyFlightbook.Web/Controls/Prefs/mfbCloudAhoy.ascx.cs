using MyFlightbook.OAuth.CloudAhoy;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbCloudAhoy : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Profile pf = Profile.GetUser(Page.User.Identity.Name);
                mvCloudAhoy.SetActiveView(pf.CloudAhoyToken == null ? vwAuthCloudAhoy : vwDeAuthCloudAhoy);
                lnkAuthCloudAhoy.Text = Branding.ReBrand(Resources.Profile.AuthorizeCloudAhoy);
                lnkDeAuthCloudAhoy.Text = Branding.ReBrand(Resources.Profile.DeAuthCloudAhoy);
                locCloudAhoyIsAuthed.Text = Branding.ReBrand(Resources.Profile.CloudAhoyIsAuthed);
            }
        }

        protected void lnkAuthCloudAhoy_Click(object sender, EventArgs e)
        {
            new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).Authorize(new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/CloudAhoyRedir.aspx"))));
        }

        protected void lnkDeAuthCloudAhoy_Click(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            pf.CloudAhoyToken = null;
            pf.FCommit();
            mvCloudAhoy.SetActiveView(vwAuthCloudAhoy);
        }
    }
}