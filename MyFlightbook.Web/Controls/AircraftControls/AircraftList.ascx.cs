using MyFlightbook;
using MyFlightbook.Templates;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
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

    public event EventHandler<EventArgs> AircraftPrefChanged = null;

    public bool EnableAircraftViewState
    {
        get { return gvAircraft.EnableViewState; }
        set { gvAircraft.EnableViewState = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e) {  }

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
            switch (ac.RoleForPilot)
            {
                case Aircraft.PilotRole.None:
                    ((RadioButton)popup.FindControl("rbRoleNone")).Checked = true;
                    break;
                case Aircraft.PilotRole.CFI:
                    ((RadioButton)popup.FindControl("rbRoleCFI")).Checked = true;
                    break;
                case Aircraft.PilotRole.SIC:
                    ((RadioButton)popup.FindControl("rbRoleSIC")).Checked = true;
                    break;
                case Aircraft.PilotRole.PIC:
                    ((RadioButton)popup.FindControl("rbRolePIC")).Checked = true;
                    break;
            }
            ((CheckBox)popup.FindControl("ckShowInFavorites")).Checked = !ac.HideFromSelection;
            ((CheckBox)popup.FindControl("ckAddNameAsPIC")).Checked = ac.CopyPICNameWithCrossfill;

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

            Controls_mfbSelectTemplates selectTemplates = (Controls_mfbSelectTemplates)popup.FindControl("mfbSelectTemplates");
            foreach (int i in ac.DefaultTemplates)
                selectTemplates.AddTemplate(i);

            selectTemplates.Refresh();

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

    protected void SelectRole(Control sender, Aircraft.PilotRole role)
    {
        Aircraft ac = RowFromControl(sender);
        ac.RoleForPilot = role;
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        if (AircraftPrefChanged != null)
            AircraftPrefChanged(this, new EventArgs());
    }

    protected void rbRoleCFI_CheckedChanged(object sender, EventArgs e)
    {
        SelectRole((Control) sender, Aircraft.PilotRole.CFI);
    }

    protected void rbRolePIC_CheckedChanged(object sender, EventArgs e)
    {
        SelectRole((Control)sender, Aircraft.PilotRole.PIC);
    }

    protected void rbRoleSIC_CheckedChanged(object sender, EventArgs e)
    {
        SelectRole((Control)sender, Aircraft.PilotRole.SIC);
    }

    protected void rbRoleNone_CheckedChanged(object sender, EventArgs e)
    {
        SelectRole((Control)sender, Aircraft.PilotRole.None);
    }

    protected void ckShowInFavorites_CheckedChanged(object sender, EventArgs e)
    {
        CheckBox ck = (CheckBox)sender;
        Aircraft ac = RowFromControl(ck);
        ac.HideFromSelection = !ck.Checked;
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        if (FavoriteChanged != null)
            FavoriteChanged(this, e);
    }

    protected void ckAddNameAsPIC_CheckedChanged(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");
        CheckBox ck = (CheckBox)sender;
        Aircraft ac = RowFromControl(ck);
        ac.CopyPICNameWithCrossfill = ck.Checked;
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        if (AircraftPrefChanged != null)
            AircraftPrefChanged(this, e);
    }

    protected void mfbSelectTemplates_TemplatesReady(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");

        Controls_mfbSelectTemplates selectTemplates = sender as Controls_mfbSelectTemplates;
        // Hide the pop menu if only automatic templates are available
        if (selectTemplates.GroupedTemplates.Count() == 0)
            selectTemplates.NamingContainer.FindControl("pnlTemplates").Visible = false;
    }

    protected void mfbSelectTemplates_TemplateSelected(object sender, PropertyTemplateEventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");

        Aircraft ac = RowFromControl(sender as Control);
        ac.DefaultTemplates.Add(e.TemplateID);
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        if (AircraftPrefChanged != null)
            AircraftPrefChanged(this, e);
    }

    protected void mfbSelectTemplates_TemplateUnselected(object sender, PropertyTemplateEventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");

        Aircraft ac = RowFromControl(sender as Control);
        ac.DefaultTemplates.Remove(e.TemplateID);
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.FAddAircraftForUser(ac);
        if (AircraftPrefChanged != null)
            AircraftPrefChanged(this, e);
    }
}