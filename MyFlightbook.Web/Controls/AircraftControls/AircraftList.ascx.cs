using MyFlightbook.Templates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftControls
{
    public partial class AircraftList : UserControl
    {
        #region properties
        public bool IsAdminMode { get; set; }

        public IEnumerable<Aircraft> AircraftSource
        {
            get { return (IEnumerable<Aircraft>)gvAircraft.DataSource; }
            set
            {
                gvAircraft.DataSource = value;
                gvAircraft.DataBind();
                gvAircraft.Columns[0].Visible = !IsAdminMode;
            }
        }

        public event EventHandler<CommandEventArgs> AircraftDeleted;
        public event EventHandler<AircraftEventArgs> MigrateAircraft;

        public bool EnableAircraftViewState
        {
            get { return gvAircraft.EnableViewState; }
            set { gvAircraft.EnableViewState = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.ClientScript.RegisterClientScriptInclude(GetType(), "AircraftContext", ResolveClientUrl("~/public/Scripts/aircraftcontext.js?v=2"));
        }

        public void gvAircraft_OnRowDataBound(Object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
            {
                Aircraft ac = (Aircraft)e.Row.DataItem;

                // Refresh the images
                if (!IsAdminMode)
                    ((Controls_mfbHoverImageList)e.Row.FindControl("mfbHoverThumb")).Refresh();

                // Show aircraft capabilities too.
                Control popup = e.Row.FindControl("popmenu1");
                RadioButton rbRoleNone = (RadioButton)popup.FindControl("rbRoleNone");
                RadioButton rbRoleCFI = (RadioButton)popup.FindControl("rbRoleCFI");
                RadioButton rbRoleSIC = (RadioButton)popup.FindControl("rbRoleSIC");
                RadioButton rbRolePIC = (RadioButton)popup.FindControl("rbRolePIC");

                switch (ac.RoleForPilot)
                {
                    case Aircraft.PilotRole.None:
                        rbRoleNone.Checked = true;
                        break;
                    case Aircraft.PilotRole.CFI:
                        rbRoleCFI.Checked = true;
                        break;
                    case Aircraft.PilotRole.SIC:
                        rbRoleSIC.Checked = true;
                        break;
                    case Aircraft.PilotRole.PIC:
                        rbRolePIC.Checked = true;
                        break;
                }

                CheckBox ckIsFavorite = (CheckBox)popup.FindControl("ckShowInFavorites");
                ckIsFavorite.Checked = !ac.HideFromSelection;
                ckIsFavorite.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:toggleFavorite({0},this.checked,'{1}');", ac.AircraftID, ResolveClientUrl("~/Member/Ajax.asmx/SetActive"));
                CheckBox ckAddName = (CheckBox)popup.FindControl("ckAddNameAsPIC");
                ckAddName.Checked = ac.CopyPICNameWithCrossfill;
                ckAddName.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById(\"{0}\").click();", rbRolePIC.ClientID);  // clicking the "Add Name" checkbox should effectively click the PIC checkbox.

                rbRoleNone.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:setRole({0},\"{1}\",false,document.getElementById(\"{2}\"),\"{3}\");", ac.AircraftID, Aircraft.PilotRole.None.ToString(), ckAddName.ClientID, ResolveClientUrl("~/Member/Ajax.asmx/SetRole"));
                rbRoleCFI.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:setRole({0},\"{1}\",false,document.getElementById(\"{2}\"),\"{3}\");", ac.AircraftID, Aircraft.PilotRole.CFI.ToString(), ckAddName.ClientID, ResolveClientUrl("~/Member/Ajax.asmx/SetRole"));
                rbRoleSIC.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:setRole({0},\"{1}\",false,document.getElementById(\"{2}\"),\"{3}\");", ac.AircraftID, Aircraft.PilotRole.SIC.ToString(), ckAddName.ClientID, ResolveClientUrl("~/Member/Ajax.asmx/SetRole"));
                rbRolePIC.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:setRole({0},\"{1}\",document.getElementById(\"{2}\").checked, document.getElementById(\"{3}\"),\"{4}\");", ac.AircraftID, Aircraft.PilotRole.PIC.ToString(), ckAddName.ClientID, ckAddName.ClientID, ResolveClientUrl("~/Member/Ajax.asmx/SetRole"));

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

                mfbSelectTemplates selectTemplates = (mfbSelectTemplates)popup.FindControl("mfbSelectTemplates");
                foreach (int i in ac.DefaultTemplates)
                    selectTemplates.AddTemplate(i);

                // set up for web service rather than postback
                selectTemplates.ToggleClientScript = String.Format(CultureInfo.InvariantCulture, "toggleTemplate({0}, {{0}}, document.getElementById('{{1}}').checked, '{1}')", ac.AircraftID, VirtualPathUtility.ToAbsolute("~/Member/Ajax.asmx/AddRemoveTemplate"));
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
                throw new ArgumentNullException(nameof(e));

            int idAircraft = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Aircraft ac = new Aircraft(idAircraft);

                UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
                try
                {
                    ua.FDeleteAircraftforUser(ac.AircraftID);
                    AircraftDeleted?.Invoke(sender, e);
                }
                catch (MyFlightbookException ex)
                {
                    if (sender is GridView gvSource)
                    {
                        IList<Aircraft> src = (IList<Aircraft>)gvSource.DataSource;
                        for (int iRow = 0; iRow < src.Count; iRow++)
                        {
                            if (src[iRow].AircraftID == ac.AircraftID)
                            {
                                GridViewRow gvr = gvSource.Rows[iRow];
                                Label l = (Label)gvr.FindControl("lblAircraftErr");
                                l.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MyAircraftDeleteError, ac.TailNumber, ex.Message));
                                l.Visible = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        protected static Aircraft AircraftFromControl(Control c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c));
            while (c != null && c.NamingContainer != null && !typeof(GridViewRow).IsAssignableFrom(c.NamingContainer.GetType()))
                c = c.NamingContainer;
            GridViewRow grow = (GridViewRow)c.NamingContainer;
            GridView gv = (GridView)grow.NamingContainer;
        if (grow.RowIndex >= 0 && grow.RowIndex < gv.Rows.Count)
            return ((IList<Aircraft>)gv.DataSource)[grow.RowIndex];
        return null;
        }

        protected void mfbSelectTemplates_TemplatesReady(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            mfbSelectTemplates selectTemplates = sender as mfbSelectTemplates;
            if (!selectTemplates.GroupedTemplates.Any())
                selectTemplates.NamingContainer.FindControl("pnlTemplates").Visible = false;
        }

        protected void lnkMigrate_Click(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            Aircraft ac = AircraftFromControl((Control)sender);
            MigrateAircraft?.Invoke(this, new AircraftEventArgs(ac));
        }
    }
}