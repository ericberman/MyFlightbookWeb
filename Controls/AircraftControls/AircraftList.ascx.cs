using MyFlightbook;
using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_AircraftControls_AircraftList : System.Web.UI.UserControl
{
    #region properties
    public bool IsAdminMode { get; set; }

    public IEnumerable<Aircraft> AircraftSource
    {
        get { return (IEnumerable<Aircraft>) gvAircraft.DataSource; }
        set
        {
            gvAircraft.DataSource = value;
            gvAircraft.DataBind();
            gvAircraft.Columns[0].Visible = !IsAdminMode;
        }
    }

    public event EventHandler<CommandEventArgs> AircraftDeleted = null;

    public event EventHandler<EventArgs> FavoriteChanged = null;

    public bool EnableAircraftViewState
    {
        get { return gvAircraft.EnableViewState; }
        set { gvAircraft.EnableViewState = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

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

            if (!IsAdminMode)
            {
                List<LinkedString> lst = new List<LinkedString>();

                if (ac.Stats != null)
                    lst.Add(ac.Stats.UserStatsDisplay);
                MakeModel mm = MakeModel.GetModel(ac.ModelID);
                if (mm != null)
                {
                    if (!String.IsNullOrEmpty(mm.FamilyName))
                        lst.Add(new LinkedString(ModelQuery.ICAOPrefix + mm.FamilyName));

                    foreach (string sz in mm.AttributeList(ac.AvionicsTechnologyUpgrade, ac.GlassUpgradeDate))
                        lst.Add(new LinkedString(sz));
                }

                Repeater rpt = (Repeater)e.Row.FindControl("rptAttributes");
                rpt.DataSource = lst;
                rpt.DataBind();
            }

            if (IsAdminMode)
            {
                HyperLink lnkRegistration = (HyperLink)e.Row.FindControl("lnkRegistration");
                string szURL = ac.LinkForTailnumberRegistry();
                lnkRegistration.Visible = szURL.Length > 0;
                lnkRegistration.NavigateUrl = szURL;
            }
        }
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
                ua.FDeleteAircraftforUser(ac.AircraftID);
                if (AircraftDeleted != null)
                    AircraftDeleted(sender, e);
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
        ac.RoleForPilot = (Aircraft.PilotRole)Enum.Parse(typeof(Aircraft.PilotRole), rbl.SelectedValue);
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
        if (FavoriteChanged != null)
            FavoriteChanged(sender, e);
    }
}