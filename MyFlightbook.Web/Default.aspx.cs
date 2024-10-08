using MyFlightbook;
using MyFlightbook.FlightStatistics;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Home : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabHome;
        Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitleWithDescription, Branding.CurrentBrand.AppName);

        string s = util.GetStringParam(Request, "m");
        if (s.Length > 0)
            util.SetMobile(string.Compare(s, "no", StringComparison.OrdinalIgnoreCase) != 0);

        FlightStats fs = FlightStats.GetFlightStats();

        if (!IsPostBack)
        {
            rptFeatures.DataSource = AppAreaDescriptor.DefaultDescriptors;
            rptFeatures.DataBind();

            if (User.Identity.IsAuthenticated)
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageWelcomeBack, HttpUtility.HtmlEncode(Profile.GetUser(User.Identity.Name).PreferredGreeting));
                pnlWelcome.Visible = false;
            }
            else
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
                pnlWelcome.Visible = true;
            }

            locRecentStats.Text = Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsHeader, fs.MaxDays));

            rptStats.DataSource = fs.Stats;
            rptStats.DataBind();
        }

        // redirect to a mobile view if this is from a mobile device UNLESS cookies suggest to do otherwise.
        if (Request.IsMobileSession())
        {
            if ((Request.Cookies[MFBConstants.keyClassic] == null || String.Compare(Request.Cookies[MFBConstants.keyClassic].Value, "yes", StringComparison.OrdinalIgnoreCase) != 0))
                Response.Redirect("DefaultMini.aspx");
        }

        imageSlider.Images = fs.RecentImages();
    }
}
