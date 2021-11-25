using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    public partial class EditAircraft : Page
    {
        #region webservices
        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static string[] SuggestFullModels(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();

            ModelQuery modelQuery = new ModelQuery() { FullText = prefixText.Replace("-", "*"), PreferModelNameMatch = true, Skip = 0, Limit = count };
            List<string> lst = new List<string>();
            foreach (MakeModel mm in MakeModel.MatchingMakes(modelQuery))
                lst.Add(AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithDash, mm.ManufacturerDisplay, mm.ModelDisplayName), mm.MakeModelID.ToString(CultureInfo.InvariantCulture)));

            return lst.ToArray();
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static string[] SuggestAircraft(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            IEnumerable<Aircraft> lstAircraft = Aircraft.AircraftWithPrefix(prefixText, count);
            List<string> lst = new List<string>();
            foreach (Aircraft ac in lstAircraft)
                lst.Add(AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(String.Format(CultureInfo.CurrentCulture, "{0} - {1}", ac.TailNumber, ac.ModelDescription), ac.AircraftID.ToString(CultureInfo.InvariantCulture)));
            return lst.ToArray();
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static string GetHighWaterMarks(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new MyFlightbookException("Unauthenticated call to GetHighWaterMarks");

            if (idAircraft <= 0)
                return String.Empty;

            decimal hwHobbs = AircraftUtility.HighWaterMarkHobbsForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name);
            decimal hwTach = AircraftUtility.HighWaterMarkTachForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name);

            if (hwTach == 0)
                return hwHobbs == 0 ? String.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkHobbsOnly, hwHobbs);
            else if (hwHobbs == 0)
                return String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachOnly, hwTach);
            else
                return String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachAndHobbs, hwTach, hwHobbs);
        }
        #endregion

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
                bool fAdminMode = AdminMode = id > 0 && (util.GetIntParam(Request, "a", 0) != 0) && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport;
                bool fCanMigrate = !String.IsNullOrEmpty(util.GetStringParam(Request, "genCandidate"));

                MfbEditAircraft1.AircraftID = id = AircraftTombstone.MapAircraftID(id);

                MfbEditAircraft1.AdminMode = lblAdminMode.Visible = pnlAdminUserFlights.Visible = fAdminMode;
                if (fAdminMode)
                {
                    sqlDSFlightsPerUser.SelectParameters.Add(new Parameter("idaircraft", TypeCode.Int32, id.ToString(CultureInfo.InvariantCulture)));
                    gvFlightsPerUser.DataSource = sqlDSFlightsPerUser;
                    gvFlightsPerUser.DataBind();

                    List<Aircraft> lst = Aircraft.AircraftMatchingTail(new Aircraft(id).TailNumber);
                    if (lst.Count > 1)
                    {
                        btnMakeDefault.Visible = true;
                        btnMakeDefault.Enabled = (lst[0].AircraftID != id); // only enable it if we aren't the default.
                    }
                }

                btnMigrateGeneric.Visible = fAdminMode && fCanMigrate && !MfbEditAircraft1.Aircraft.IsAnonymous && MfbEditAircraft1.Aircraft.InstanceType == AircraftInstanceTypes.RealAircraft;
                btnMigrateSim.Visible = btnMigrateGeneric.Visible && AircraftUtility.CouldBeSim(MfbEditAircraft1.Aircraft);

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

        protected void btnAdminMakeDefault_Click(object sender, EventArgs e)
        {
            Aircraft ac = new Aircraft(MfbEditAircraft1.AircraftID);
            ac.MakeDefault();
            btnMakeDefault.Enabled = false;
        }

        protected void btnMigrateGeneric_Click(object sender, EventArgs e)
        {
            Aircraft acOriginal = new Aircraft(MfbEditAircraft1.AircraftID);

            // See if there is a generic for the model
            string szTailNumGeneric = Aircraft.AnonymousTailnumberForModel(acOriginal.ModelID);
            Aircraft acGeneric = new Aircraft(szTailNumGeneric);
            if (acGeneric.IsNew)
            {
                acGeneric.TailNumber = szTailNumGeneric;
                acGeneric.ModelID = acOriginal.ModelID;
                acGeneric.InstanceType = AircraftInstanceTypes.RealAircraft;
                acGeneric.Commit();
            }

            AircraftUtility.AdminMergeDupeAircraft(acGeneric, acOriginal);
            Response.Redirect(Request.Url.PathAndQuery.Replace(String.Format(CultureInfo.InvariantCulture, "id={0}", acOriginal.AircraftID), String.Format(CultureInfo.InvariantCulture, "id={0}", acGeneric.AircraftID)));
        }

        protected void btnMigrateSim_Click(object sender, EventArgs e)
        {
            Aircraft acOriginal = new Aircraft(MfbEditAircraft1.AircraftID);
            int idNew = AircraftUtility.MapToSim(acOriginal);
            if (idNew == Aircraft.idAircraftUnknown)
                lblErr.Text = Resources.Aircraft.AdminNotASim;
            else
                Response.Redirect(Request.Url.PathAndQuery.Replace(String.Format(CultureInfo.InvariantCulture, "id={0}", acOriginal.AircraftID), String.Format(CultureInfo.InvariantCulture, "id={0}", idNew)));
        }
        #endregion
    }
}