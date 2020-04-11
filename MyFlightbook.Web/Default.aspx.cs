using MyFlightbook;
using MyFlightbook.FlightStatistics;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Home : System.Web.UI.Page
{
    protected class AppAreaDescriptor {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public tabID TabID { get; set; }

        public AppAreaDescriptor(string title, string link, string description, tabID id)
        {
            Title = title;
            Link = link;
            Description = description;
            TabID = id;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabHome;
        Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitleWithDescription, Branding.CurrentBrand.AppName);

        string s = util.GetStringParam(Request, "m");
        if (s.Length > 0)
        {
            this.Master.SetMobile((string.Compare(s, "no", StringComparison.OrdinalIgnoreCase) != 0));
        }

        FlightStats fs = FlightStats.GetFlightStats();

        if (!IsPostBack)
        {
            List<AppAreaDescriptor> lst = new List<AppAreaDescriptor>()
            {
                new AppAreaDescriptor(Resources.Tabs.TabLogbook, "~/Member/LogbookNew.aspx", Branding.ReBrand(Resources.Profile.appDescriptionLogbook), tabID.tabLogbook),
                new AppAreaDescriptor(Resources.Tabs.TabAircraft, "~/Member/Aircraft.aspx", Branding.ReBrand(Resources.Profile.appDescriptionAircraft), tabID.tabAircraft),
                new AppAreaDescriptor(Resources.Tabs.TabAirports, "~/Public/MapRoute2.aspx", Branding.ReBrand(Resources.Profile.appDescriptionAirports), tabID.tabMaps),
                new AppAreaDescriptor(Resources.Tabs.TabInstruction, "~/Member/Training.aspx", Branding.ReBrand(Resources.Profile.appDescriptionTraining), tabID.tabTraining),
                new AppAreaDescriptor(Resources.Tabs.TabProfile, "~/Member/EditProfile.aspx", Branding.ReBrand(Resources.Profile.appDescriptionProfile), tabID.tabProfile)
            };
            rptFeatures.DataSource = lst;
            rptFeatures.DataBind();
            locRecentFlightsHeader.Text = Branding.ReBrand(Resources.LocalizedText.DefaultPageRecentFlightsHeader);


            if (User.Identity.IsAuthenticated)
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageWelcomeBack, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFirstName);
                pnlWelcome.Visible = false;
            }
            else
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
                pnlWelcome.Visible = true;
            }

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

        // redirect to a mobile view if this is from a mobile device UNLESS cookies suggest to do otherwise.
        if (this.Master.IsMobileSession())
        {
            if ((Request.Cookies[MFBConstants.keyClassic] == null || String.Compare(Request.Cookies[MFBConstants.keyClassic].Value, "yes", StringComparison.OrdinalIgnoreCase) != 0))
                Response.Redirect("DefaultMini.aspx");
        }

        PopulateRecentImages(fs.RecentPublicFlights);
    }

    protected void PopulateRecentImages(IEnumerable<LogbookEntry> flights)
    {
        List<LogbookEntry> lstRecent = new List<LogbookEntry>(flights);
        List<MFBImageInfo> lstRecentImages = new List<MFBImageInfo>();
        HashSet<int> recentAircraft = new HashSet<int>();

        foreach (LogbookEntry le in lstRecent)
        {
            if (le.FlightImages == null || le.FlightImages.Count == 0)
                le.PopulateImages();
            if (le.FlightImages.Count > 0)
                lstRecentImages.Add(le.FlightImages[0]);
            else
                recentAircraft.Add(le.AircraftID);

            if (lstRecentImages.Count > 10)
                break;
        }

        foreach (int aircraftID in recentAircraft)
        {
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Aircraft, aircraftID.ToString(CultureInfo.InvariantCulture));
            il.Refresh();
            if (il.ImageArray.Count > 0)
                lstRecentImages.Add(il.ImageArray[0]);
            if (lstRecentImages.Count > 10)
                break;
        }

        imageSlider.Images = lstRecentImages;
    }
}
