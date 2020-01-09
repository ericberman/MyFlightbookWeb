using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Aircraft : System.Web.UI.Page
{
    private int idModel = -1;

    protected bool IsAdminMode { get; set; }

    protected AircraftGroup.GroupMode GroupingMode
    {
        get
        {
            return (AircraftGroup.GroupMode)Enum.Parse(typeof(AircraftGroup.GroupMode), cmbAircraftGrouping.SelectedValue);
        }
    }

    protected string FormatOptionalDate(DateTime dt)
    {
        return dt.HasValue() ? dt.ToShortDateString() : string.Empty;
    }

    protected IEnumerable<Aircraft> SourceAircraft { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.actMyAircraft;
        this.Title = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftTitle, Branding.CurrentBrand.AppName);

        idModel = util.GetIntParam(Request, "m", -1);
        IsAdminMode = (idModel > 0) && (util.GetIntParam(Request, "a", 0) != 0) && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData;

        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        if (!IsPostBack)
        {
            bool fClearCache = (util.GetIntParam(Request, "flush", 0) != 0);
            if (fClearCache)
                ua.InvalidateCache();
        }
        lblAdminMode.Visible = IsAdminMode;
        pnlDownload.Visible = !IsAdminMode;

        RefreshAircraftList();

        Refresh();
    }

    protected void RefreshAircraftList()
    {
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        UserAircraft.AircraftRestriction r = IsAdminMode ? UserAircraft.AircraftRestriction.AllMakeModel : UserAircraft.AircraftRestriction.UserAircraft;
        SourceAircraft = ua.GetAircraftForUser(r, idModel);
    }

    /// <summary>
    /// Populates stats, if needed.
    /// </summary>
    protected void PopulateStats()
    {
        if (String.IsNullOrEmpty(hdnStatsFetched.Value))
        {
            hdnStatsFetched.Value = "yes";
            RefreshAircraftList();
            AircraftStats.PopulateStatsForAircraft(SourceAircraft, Page.User.Identity.Name);
        }
    }

    protected void Refresh(bool fForceStats = false)
    {
        if (fForceStats)
            hdnStatsFetched.Value = string.Empty;

        if (GroupingMode == AircraftGroup.GroupMode.Recency)
            PopulateStats();

        rptAircraftGroups.DataSource = AircraftGroup.AssignToGroups(SourceAircraft, IsAdminMode ? AircraftGroup.GroupMode.All : GroupingMode);
        rptAircraftGroups.DataBind();
    }

    protected void btnAddNew_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Member/EditAircraft.aspx?id=-1");
    }

    protected void cmbAircraftGrouping_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void rptAircraftGroups_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Controls_AircraftControls_AircraftList aircraftList = (Controls_AircraftControls_AircraftList)e.Item.FindControl("AircraftList");
        aircraftList.IsAdminMode = IsAdminMode;
        aircraftList.AircraftSource = ((AircraftGroup)e.Item.DataItem).MatchingAircraft;
    }

    protected void AircraftList_AircraftDeleted(object sender, CommandEventArgs e)
    {
        RefreshAircraftList();
        Refresh(true);
    }

    protected void AircraftList_FavoriteChanged(object sender, EventArgs e)
    {
        Refresh(true);
    }

    protected void AircraftList_AircraftPrefChanged(object sender, EventArgs e)
    {
        Refresh(true);
    }

    protected string ValueString(object o, decimal offSet = 0.0M)
    {
        if (o is DateTime)
        {
            DateTime dt = (DateTime)o;
            if (dt != null && dt.HasValue())
                return dt.ToShortDateString();
        }
        else if (o is decimal)
        {
            decimal d = (decimal)o;
            if (d > 0)
                return (d + offSet).ToString("#,##0.0#", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    protected void lnkDownloadCSV_Click(object sender, EventArgs e)
    {
        PopulateStats();
        gvAircraftToDownload.DataSource = SourceAircraft;
        gvAircraftToDownload.DataBind();
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "Aircraft-{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write('\uFEFF');   // UTF-8 BOM.
        Response.Write(gvAircraftToDownload.CSVFromData());
        Response.End();
    }
}
