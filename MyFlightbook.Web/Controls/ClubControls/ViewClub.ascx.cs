using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Airports;

/******************************************************
 * 
 * Copyright (c) 2014-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_ClubControls_ViewClub : System.Web.UI.UserControl
{
    const string szVSKeyActiveClub = "ActiveClubVSKey";

    public Club ActiveClub {
        get { return (Club)ViewState[szVSKeyActiveClub]; }
        set
        {
            ViewState[szVSKeyActiveClub] = value; 
            Refresh();
        }
    }

    public bool LinkToDetails { get; set; }

    public event System.EventHandler<ClubChangedEventsArgs> ClubChanged = null;
    public event System.EventHandler<ClubChangedEventsArgs> ClubChangeCanceled = null;
    public event System.EventHandler<ClubChangedEventsArgs> ClubDeleted = null;

    public bool ShowCancel { get; set; }
    public bool ShowDelete { get; set; }

    public FormViewMode DefaultMode
    {
        get { return fvClub.DefaultMode; }
        set { fvClub.DefaultMode = value; }
    }

    public void Update()
    {
        fvClub.UpdateItem(true);
    }

    public void Insert()
    {
        fvClub.InsertItem(true);
    }

    public void Refresh()
    {
        Club c = ActiveClub;
        if (c == null)
            return;
        // Hack - page_load isn't getting called on mfbhtmlEdit, need to fix up the HTML here.
        string szDesc = c.Description;
        if (fvClub.CurrentMode == FormViewMode.Edit)
            c.Description = Controls_mfbHtmlEdit.UnFixHtml(c.Description);

        fvClub.DataSource = new List<Club> { c };
        fvClub.DataBind();

        // Restore the description
        c.Description = szDesc;

        // Show the link or don't
        if (fvClub.CurrentMode == FormViewMode.ReadOnly)
        {
            MultiView mv = (MultiView)fvClub.FindControl("mvClubHeader");
            if (mv != null)
                mv.ActiveViewIndex = (LinkToDetails ? 0 : 1);
        }
        else
        {
            Button b = (Button)fvClub.FindControl("btnCancel");
            b.Visible = ShowCancel;
            b = (Button)fvClub.FindControl("btnDelete");
            b.Visible = ShowDelete;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void fvClub_ItemUpdating(object sender, FormViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Page.Validate("valEditClub");
        if (Page.IsValid)
        {
            Club c = ActiveClub ?? new Club();
            c.City = (string)e.NewValues["City"];
            c.ContactPhone = (string)e.NewValues["ContactPhone"];
            c.Country = (string)e.NewValues["Country"];
            c.Description = Controls_mfbHtmlEdit.FixHtml((string)e.NewValues["Description"]);
            c.HomeAirportCode = (string)e.NewValues["HomeAirportCode"];
            if (!String.IsNullOrEmpty(c.HomeAirportCode))
            {
                AirportList al = new AirportList(c.HomeAirportCode);
                List<airport> lst = new List<airport>(al.GetAirportList());
                airport ap = lst.FirstOrDefault(a => a.IsPort);
                c.HomeAirportCode = ap == null ? c.HomeAirportCode : ap.Code;
            }
            c.Name = (string)e.NewValues["Name"];
            c.StateProvince = (string)e.NewValues["StateProvince"];
            c.URL = (string)e.NewValues["URL"];
            c.ID = Convert.ToInt32(e.NewValues["ID"], CultureInfo.InvariantCulture);
            c.RestrictEditingToOwnersAndAdmins = Convert.ToBoolean(e.NewValues["RestrictEditingToOwnersAndAdmins"], CultureInfo.InvariantCulture);
            c.IsPrivate = Convert.ToBoolean(e.NewValues["IsPrivate"], CultureInfo.InvariantCulture);
            c.PrependsScheduleWithOwnerName = Convert.ToBoolean(e.NewValues["PrependsScheduleWithOwnerName"], CultureInfo.InvariantCulture);
            c.DeleteNotifications = (Club.DeleteNoficiationPolicy) Enum.Parse(typeof(Club.DeleteNoficiationPolicy), (string) e.NewValues["DeleteNotifications"]);
            c.DoubleBookRoleRestriction = (Club.DoubleBookPolicy)Enum.Parse(typeof(Club.DoubleBookPolicy), (string)e.NewValues["DoubleBookRoleRestriction"]);
            c.AddModifyNotifications = (Club.AddModifyNotificationPolicy)Enum.Parse(typeof(Club.AddModifyNotificationPolicy), (string)e.NewValues["AddModifyNotifications"]);
            c.TimeZone = TimeZoneInfo.FindSystemTimeZoneById((string)e.NewValues["TimeZone.Id"]);
            if (c.IsNew)
                c.Creator = Page.User.Identity.Name;
            if (c.FCommit())
            {
                if (ClubChanged != null)
                    ClubChanged(this, new ClubChangedEventsArgs(ActiveClub));
                this.ActiveClub = c;
            }
            else
                lblErr.Text = c.LastError;
        }
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        fvClub.ChangeMode(FormViewMode.ReadOnly);
        Refresh();
        if (ClubChangeCanceled != null)
            ClubChangeCanceled(sender, new ClubChangedEventsArgs(ActiveClub));
    }

    protected void btnDelete_Click(object sender, EventArgs e)
    {
        fvClub.ChangeMode(FormViewMode.ReadOnly);
        Refresh();
        if (ClubDeleted != null)
            ClubDeleted(sender, new ClubChangedEventsArgs(ActiveClub));
    }

    protected void fvClub_ModeChanging(object sender, FormViewModeEventArgs e)
    {
        
    }
    protected void fvClub_ModeChanged(object sender, EventArgs e)
    {

    }

    protected void fvClub_DataBound(object sender, EventArgs e)
    {
    }
}