using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class EditMake : System.Web.UI.Page
{
    protected override void OnError(EventArgs e)
    {
        Exception ex = Server.GetLastError();

        if (ex.GetType() == typeof(HttpRequestValidationException))
        {
            Context.ClearError();
            Response.Redirect("~/SecurityError.aspx");
            Response.End();
        }
        else
            base.OnError(e);
    }

    protected FlightQuery QueryForAircraft()
    {
        FlightQuery fq = new FlightQuery(Page.User.Identity.Name);
        fq.AircraftList = new Aircraft[] { MfbEditAircraft1.Aircraft };
        return fq;
    }

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

            MfbEditAircraft1.AircraftID = AircraftTombstone.MapAircraftID(id);
            btnMigrateGeneric.Visible = fAdminMode && fCanMigrate && !MfbEditAircraft1.Aircraft.IsAnonymous && MfbEditAircraft1.Aircraft.InstanceType == AircraftInstanceTypes.RealAircraft;

            if (MfbEditAircraft1.AircraftID == Aircraft.idAircraftUnknown)
            {
                lblAddEdit1.Text = Resources.Aircraft.AircraftEditAdd;
                reusetext.Visible = true;
                pnlTotals.Visible = mfbLogbook1.Visible = false;
            }
            else
            {
                bool fIsKnownAircraft = new UserAircraft(Page.User.Identity.Name).CheckAircraftForUser(MfbEditAircraft1.Aircraft);
                lblAddEdit1.Text = Resources.Aircraft.AircraftEditEdit;
                mfbATDFTD1.Visible = false;
                lblTail2.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftTotalsHeader, MfbEditAircraft1.Aircraft.TailNumber);
                lblTail.Text = MfbEditAircraft1.Aircraft.TailNumber;

                if (!fAdminMode)
                    mfbTotalSummary1.CustomRestriction = QueryForAircraft();
                pnlTotals.Visible = !fAdminMode && fIsKnownAircraft;
            }

            // Remember the return URL, but only if it is relative (for security)
            string szReturnURL = util.GetStringParam(Request, "Ret");
            if (Uri.IsWellFormedUriString(szReturnURL, UriKind.Relative))
                hdnReturnURL.Value = szReturnURL;
        }
    }

    protected void AircraftUpdated(object sender, EventArgs e)
    {
        Aircraft.SaveLastTail(MfbEditAircraft1.AircraftID);
        Response.Redirect(!String.IsNullOrEmpty(hdnReturnURL.Value) ? hdnReturnURL.Value : "Aircraft.aspx");
    }

    protected void lnkShowFlights_Click(object sender, EventArgs e)
    {
        if (!AdminMode)
        {
            mfbLogbook1.Restriction = QueryForAircraft();
            mfbLogbook1.Visible = true;
            mfbLogbook1.RefreshData();
            lnkShowFlights.Visible = false;
        }
    }

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
            MfbEditAircraft1.Aircraft.Clone(MfbEditAircraft1.SelectedModelID, new System.Collections.ObjectModel.ReadOnlyCollection<string>(lstUsers));
            Response.Redirect(Request.Url.PathAndQuery);
        }
    }

    protected void btnAdminMakeDefault_Click(object sender, EventArgs e)
    {
        Aircraft ac = new Aircraft(MfbEditAircraft1.AircraftID);
        ac.MakeDefault();
        btnMakeDefault.Enabled = false;
    }

    protected void valModelSelected_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args != null && MfbEditAircraft1.SelectedModelID == MakeModel.UnknownModel)
            args.IsValid = false;
    }
    protected void btnMigrateGeneric_Click(object sender, EventArgs e)
    {
        Aircraft acOriginal = new Aircraft(MfbEditAircraft1.AircraftID);

        // See if there is a generic for the model
        string szTailNumGeneric = Aircraft.AnonymousTailnumberForModel(acOriginal.ModelID);
        Aircraft acGeneric = new Aircraft(szTailNumGeneric);
        if (acGeneric.AircraftID == Aircraft.idAircraftUnknown)
        {
            acGeneric.TailNumber = szTailNumGeneric;
            acGeneric.ModelID = acOriginal.ModelID;
            acGeneric.InstanceType = AircraftInstanceTypes.RealAircraft;
            acGeneric.Commit();
        }

        Aircraft.MergeDupeAircraft(acGeneric, acOriginal);
        Response.Redirect(Request.Url.PathAndQuery);
    }
}
