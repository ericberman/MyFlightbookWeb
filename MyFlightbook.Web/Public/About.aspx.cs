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
        private const int maxAirports = 20;
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

                locRecentStats.Text = Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentStatsHeader, fs.MaxDays));

                rptStats.DataSource = fs.Stats;
                rptStats.DataBind();

                if (fs.HasSlowInformation)
                {
                    List<AirportStats> lstTopAirports = new List<AirportStats>(fs.AirportsVisited);
                    if (lstTopAirports.Count > maxAirports)
                        lstTopAirports.RemoveRange(maxAirports, lstTopAirports.Count - maxAirports);
                    rptTopAirports.DataSource = lstTopAirports;
                    rptTopAirports.DataBind();

                    rptTopModels.DataSource = fs.ModelsUsed;
                    rptTopModels.DataBind();

                    pnlLazyStats.Visible = true;
                }
            }
        }
    }
}