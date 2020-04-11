using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_EditAircraft : System.Web.UI.Page
{
    #region webservices
    [System.Web.Services.WebMethod]
    [System.Web.Script.Services.ScriptMethod]
    public static string[] SuggestFullModels(string prefixText, int count)
    {
        if (String.IsNullOrEmpty(prefixText))
            return Array.Empty<string>();

        ModelQuery modelQuery = new ModelQuery() { FullText = prefixText.Replace("-", "*"), PreferModelNameMatch=true, Skip = 0, Limit = count };
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
            btnMigrateSim.Visible = btnMigrateGeneric.Visible && CouldBeSim(MfbEditAircraft1.Aircraft);

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
        AircraftUtility.SaveLastTail(MfbEditAircraft1.AircraftID);
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

    protected static AircraftInstanceTypes PseudoSimTypeFromTail(string szTail)
    {
        if (szTail == null)
            throw new ArgumentNullException(nameof(szTail));
        szTail = szTail.ToUpper(CultureInfo.CurrentCulture).Replace("-", string.Empty);
        if (szTail.Contains("ATD"))
            return AircraftInstanceTypes.CertifiedATD;
        else if (Regex.IsMatch(szTail, "(D-?SIM)|FFS"))
            return AircraftInstanceTypes.CertifiedIFRAndLandingsSimulator;
        else if (Regex.IsMatch(szTail, "FS|SIM|FTD|REDB|FRAS|ELIT|CAE|ALSIM|FLIG|SAFE|PREC|TRUF"))
            return AircraftInstanceTypes.CertifiedIFRSimulator;
        else
            return AircraftInstanceTypes.RealAircraft;
    }

    protected static bool CouldBeSim(Aircraft ac)
    {
        if (ac == null)
            throw new ArgumentNullException(nameof(ac));
        return ac.InstanceType == AircraftInstanceTypes.RealAircraft && PseudoSimTypeFromTail(ac.TailNumber) != AircraftInstanceTypes.RealAircraft;
    }

    protected void btnMigrateSim_Click(object sender, EventArgs e)
    {
        Aircraft acOriginal = new Aircraft(MfbEditAircraft1.AircraftID);
        if (!CouldBeSim(acOriginal))
        {
            lblErr.Text = Resources.Aircraft.AdminNotASim;
            return;
        }

        // detect likely sim type
        AircraftInstanceTypes ait = PseudoSimTypeFromTail(acOriginal.TailNumber);

        // see if the specified sim exists
        string szSimTail = Aircraft.SuggestSims(acOriginal.ModelID, ait)[0].TailNumber;
        Aircraft acNew = new Aircraft(szSimTail);

        if (acNew.IsNew)
        {
            acNew.TailNumber = szSimTail;
            acNew.ModelID = acOriginal.ModelID;
            acNew.InstanceType = ait;
            acNew.Commit();
        }

        // set the original's instance type so that merge works.
        acOriginal.InstanceType = ait;
        AircraftUtility.AdminMergeDupeAircraft(acNew, acOriginal);
        Response.Redirect(Request.Url.PathAndQuery.Replace(String.Format(CultureInfo.InvariantCulture, "id={0}", acOriginal.AircraftID), String.Format(CultureInfo.InvariantCulture, "id={0}", acNew.AircraftID)));
    }
    #endregion
}
