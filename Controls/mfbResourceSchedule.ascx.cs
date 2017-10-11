using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Clubs;
using MyFlightbook.Schedule;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbResourceSchedule : System.Web.UI.UserControl
{
    /// <summary>
    /// The resource whose calendar is being managed.
    /// </summary>
    public string ResourceID
    {
        get { return hdnResourceName.Value; }
        set { hdnResourceName.Value = value; }
    }

    /// <summary>
    /// The name of a global javascript function to initialize the nav.
    /// </summary>
    public string NavInitClientFunction { get; set; }

    /// <summary>
    /// Set to true to hide the navigation panel
    /// </summary>
    public Boolean HideNavContainer 
    {
        get { return !pnlNavContainer.Visible; }
        set { pnlNavContainer.Visible = !value; }
    }

    [TemplateContainer(typeof(ScheduledResourceTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate ResourceTemplate { get; set; }

    [TemplateContainer(typeof(ScheduledResourceSubNavTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate SubNavTemplate { get; set; }

    public int ClubID
    {
        get { return String.IsNullOrEmpty(hdnClubID.Value) ? 0 : Convert.ToInt32(hdnClubID.Value, System.Globalization.CultureInfo.InvariantCulture); }
        set { hdnClubID.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    }

    public bool ShowResourceDetails
    {
        get { return divResourceDetails.Visible; }
        set { divResourceDetails.Visible = value; }
    }

    public ScheduleDisplayMode Mode { get; set; }

    public string NowUTCInClubTZ
    {
        get {
            DateTime d = DateTime.UtcNow;
            Club c = Club.ClubWithID(ClubID);
            if (c != null)
                d = TimeZoneInfo.ConvertTimeFromUtc(d, c.TimeZone);
            return d.ToString("yyyy-MM-ddThh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Human readable header for the resource.
    /// </summary>
    public string ResourceHeader
    {
        get { return lblResourceHeader.Text; }
        set { lblResourceHeader.Text = value; }
    }

    public PlaceHolder ResourceContainer
    {
        get { return plcResource; }
    }

    public PlaceHolder SubNavContainer
    {
        get { return plcSubNav; }
    }

    protected override void OnInit(EventArgs e)
    {
        if (ResourceTemplate != null)
        {
            ScheduledResourceTemplate srt = new ScheduledResourceTemplate();
            plcResource.Controls.Add(srt);
            ResourceTemplate.InstantiateIn(srt);
        }

        if (SubNavTemplate != null)
        {
            ScheduledResourceSubNavTemplate snt = new ScheduledResourceSubNavTemplate();
            plcSubNav.Controls.Add(snt);
            SubNavTemplate.InstantiateIn(snt);
        }
        base.OnInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}

public class ScheduledResourceTemplate : Control, INamingContainer
{
    public ScheduledResourceTemplate()
    {
    }
}

public class ScheduledResourceSubNavTemplate : Control, INamingContainer
{
    public ScheduledResourceSubNavTemplate()
    {
    }
}


