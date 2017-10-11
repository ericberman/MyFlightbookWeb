using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2007-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class makes : System.Web.UI.Page
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

        UserAircraft.AircraftRestriction r = IsAdminMode ? UserAircraft.AircraftRestriction.AllMakeModel : UserAircraft.AircraftRestriction.UserAircraft;
        SourceAircraft = ua.GetAircraftForUser(r, idModel);
        Refresh();
    }

    protected void Refresh()
    {
        rptAircraftGroups.DataSource = AircraftGroup.AssignToGroups(SourceAircraft, IsAdminMode ? AircraftGroup.GroupMode.All : GroupingMode);
        rptAircraftGroups.DataBind();
    }

    public void AddPictures(Object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Aircraft ac = (Aircraft)e.Row.DataItem;

            // Refresh the images
            if (!IsAdminMode)
                ((Controls_mfbHoverImageList)e.Row.FindControl("mfbHoverThumb")).Refresh();

            // Show aircraft capabilities too.
            Controls_popmenu popup = (Controls_popmenu)e.Row.FindControl("popmenu1");
            ((RadioButtonList)popup.FindControl("rblRole")).SelectedValue = ac.RoleForPilot.ToString();
            ((CheckBox)popup.FindControl("ckShowInFavorites")).Checked = !ac.HideFromSelection;

            ((Label)popup.FindControl("lblOptionHeader")).Text = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.optionHeader, ac.DisplayTailnumber);

            HyperLink lnkRegistration = (HyperLink)e.Row.FindControl("lnkRegistration");
            if (IsAdminMode)
            {
                string szURL = ac.LinkForTailnumberRegistry();
                lnkRegistration.Visible = szURL.Length > 0;
                lnkRegistration.NavigateUrl = szURL;
            }
        }
    }
    protected void btnAddNew_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Member/EditAircraft.aspx?id=-1");
    }

    protected void gvAircraft_RowCommand(Object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        int idAircraft = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            Aircraft ac = new Aircraft(idAircraft);

            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            try
            {
                SourceAircraft = ua.FDeleteAircraftforUser(ac.AircraftID);
                Refresh();
            }
            catch (MyFlightbookException ex)
            {
                GridView gvSource = (GridView)sender;
                IList<Aircraft> src = (IList<Aircraft>)gvSource.DataSource;
                for (int iRow = 0; iRow < src.Count; iRow++)
                {
                    if (src[iRow].AircraftID == ac.AircraftID)
                    {
                        GridViewRow gvr = gvSource.Rows[iRow];
                        Label l = (Label)gvr.FindControl("lblAircraftErr");
                        l.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MyAircraftDeleteError, ac.TailNumber, ex.Message);
                        l.Visible = true;
                        break;
                    }
                }
            }

        }
    }

    protected Aircraft RowFromControl(Control c)
    {
        while (c != null && c.NamingContainer != null && !typeof(GridViewRow).IsAssignableFrom(c.NamingContainer.GetType()))
            c = c.NamingContainer;
        GridViewRow grow = (GridViewRow)c.NamingContainer;
        GridView gv = (GridView)grow.NamingContainer;
        if (grow.RowIndex >= 0 && grow.RowIndex < gv.Rows.Count)
            return ((IList<Aircraft>)gv.DataSource)[grow.RowIndex];
        return null;
    }

    protected void rblRole_SelectedIndexChanged(object sender, EventArgs e)
    {
        RadioButtonList rbl = (RadioButtonList)sender;
        Aircraft ac = RowFromControl(rbl);
        ac.RoleForPilot = (Aircraft.PilotRole) Enum.Parse(typeof(Aircraft.PilotRole), rbl.SelectedValue);
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
    }

    protected void ckShowInFavorites_CheckedChanged(object sender, EventArgs e)
    {
        CheckBox ck = (CheckBox)sender;
        Aircraft ac = RowFromControl(ck);
        ac.HideFromSelection = !ck.Checked;
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        Refresh();
    }

    protected void cmbAircraftGrouping_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void rptAircraftGroups_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        GridView gv = (GridView) e.Item.FindControl("gvAircraft");
        gv.DataSource = ((AircraftGroup)e.Item.DataItem).MatchingAircraft;
        gv.DataBind();
        gv.Columns[0].Visible = !IsAdminMode;
    }

    protected void lnkDownloadCSV_Click(object sender, EventArgs e)
    {
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
