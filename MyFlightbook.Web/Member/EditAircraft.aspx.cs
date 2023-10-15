using MyFlightbook.Web.Ajax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    public partial class EditAircraft : Page
    {
        protected bool AdminMode
        {
            get { return !String.IsNullOrEmpty(hdnAdminMode.Value); }
            set { hdnAdminMode.Value = value ? "1" : string.Empty; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.tabAircraft;
            this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleAircraft, Branding.CurrentBrand.AppName);

            if (!IsPostBack)
            {
                int id = util.GetIntParam(Request, "id", Aircraft.idAircraftUnknown);
                bool fAdminMode = AdminMode = id > 0 && (util.GetIntParam(Request, "a", 0) != 0) && Profile.GetUser(Page.User.Identity.Name).CanSupport;
                bool fCanMigrate = !String.IsNullOrEmpty(util.GetStringParam(Request, "genCandidate"));

                MfbEditAircraft1.AircraftID = id = AircraftTombstone.MapAircraftID(id);

                MfbEditAircraft1.AdminMode = lblAdminMode.Visible = pnlAdminUserFlights.Visible = fAdminMode;
                if (fAdminMode)
                {
                    Page.ClientScript.RegisterClientScriptInclude("adminajax", ResolveClientUrl(AdminWebServices.AjaxScriptLink));
                    sqlDSFlightsPerUser.SelectParameters.Add(new Parameter("idaircraft", TypeCode.Int32, id.ToString(CultureInfo.InvariantCulture)));
                    gvFlightsPerUser.DataSource = sqlDSFlightsPerUser;
                    try
                    {
                        gvFlightsPerUser.DataBind();
                    }
                    catch (MySql.Data.MySqlClient.MySqlException) { }

                    List<Aircraft> lst = Aircraft.AircraftMatchingTail(new Aircraft(id).TailNumber);
                    if (lst.Count > 1)
                    {
                        btnMakeDefault.Visible = true;
                        btnMakeDefault.Enabled = (lst[0].AircraftID != id); // only enable it if we aren't the default.
                    }

                    btnMigrateGeneric.Visible = fCanMigrate && !MfbEditAircraft1.Aircraft.IsAnonymous && MfbEditAircraft1.Aircraft.InstanceType == AircraftInstanceTypes.RealAircraft;
                    btnMigrateSim.Visible = btnMigrateGeneric.Visible && AircraftUtility.CouldBeSim(MfbEditAircraft1.Aircraft);

                    btnMakeDefault.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:makeDefault(this, {0}); return false;", id);
                    btnMigrateGeneric.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:migrateGeneric(this, {0}, x => location.reload()); return false;", id);
                    btnMigrateSim.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:migrateSim(this, {0}, x => location.reload()); return false;", id);
                }

                if (MfbEditAircraft1.AircraftID == Aircraft.idAircraftUnknown)
                    lblAddEdit1.Text = Resources.Aircraft.AircraftEditAdd;
                else
                {
                    lblAddEdit1.Text = Resources.Aircraft.AircraftEditEdit;
                    lblTail.Text = MfbEditAircraft1.Aircraft.DisplayTailnumberWithModel;
                }

                // Remember the return URL, but only if it is relative (for security)
                string szReturnURL = util.GetStringParam(Request, "Ret");
                if (Uri.IsWellFormedUriString(szReturnURL, UriKind.Relative))
                    hdnReturnURL.Value = szReturnURL;
            }
        }

        protected void AircraftUpdated(object sender, EventArgs e)
        {
            AircraftUtility.LastTail = MfbEditAircraft1.AircraftID;
            Response.Redirect(!String.IsNullOrEmpty(hdnReturnURL.Value) ? hdnReturnURL.Value : "Aircraft.aspx");
        }

        protected void valModelSelected_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args != null && MfbEditAircraft1.SelectedModelID == MakeModel.UnknownModel)
                args.IsValid = false;
        }

        #region Admin functions
        protected void btnAdminCloneThis_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                List<string> lstUsers = new List<string>();
                foreach (GridViewRow gvr in gvFlightsPerUser.Rows)
                {
                    CheckBox ck = (CheckBox)gvr.FindControl("ckMigrateUser");
                    HiddenField h = (HiddenField)gvr.FindControl("hdnUsername");
                    if (ck.Checked && !String.IsNullOrEmpty(h.Value))
                        lstUsers.Add(h.Value);
                }
                MfbEditAircraft1.Aircraft.Clone(MfbEditAircraft1.SelectedModelID, lstUsers);
                Response.Redirect(Request.Url.PathAndQuery);
            }
        }
        #endregion

        protected void sqlDSFlightsPerUser_Selecting(object sender, SqlDataSourceSelectingEventArgs e)
        {
            if (e != null)
                e.Command.CommandTimeout = 60; // set a long timeout
        }
    }
}