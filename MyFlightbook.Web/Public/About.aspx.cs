using MyFlightbook.FlightStatistics;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.PublicPages
{
    public partial class About : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lnkFeatures.Text = Branding.ReBrand(Resources.LocalizedText.AboutViewFeatures);
                lnkFacebook.Visible = !String.IsNullOrEmpty(lnkFacebook.NavigateUrl = Branding.CurrentBrand.FacebookFeed);
                lnkTwitter.Visible = !String.IsNullOrEmpty(lnkTwitter.NavigateUrl = Branding.CurrentBrand.TwitterFeed);
                lblFollowFacebook.Text = Branding.ReBrand(Resources.LocalizedText.FollowOnFacebook);
                lblFollowTwitter.Text = Branding.ReBrand(Resources.LocalizedText.FollowOnTwitter);

                FlightStats fs = FlightStats.GetFlightStats();
                lblRecentFlightsStats.Text = fs.ToString();

                locRecentStats.Text = Branding.ReBrand(Resources.LocalizedText.DefaultPageRecentStats);
                List<string> lstStats = new List<string>()
                {
                String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsFlights, fs.NumFlightsTotal),
                String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsAircraft, fs.NumAircraft),
                String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPagerecentStatsModels, fs.NumModels)
                };

                rptStats.DataSource = lstStats;
                rptStats.DataBind();
            }
        }
    }
}