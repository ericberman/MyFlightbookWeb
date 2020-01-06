using MyFlightbook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbImportAircraft : System.Web.UI.UserControl
{
    #region Properties
    private const string szvsMatchRowsKey = "vsMatchRowsKey";
    private const string szvsModelMapping = "vsModelMappingDict";

    public IEnumerable<AircraftImportMatchRow> CandidatesForImport
    {
        get { return (IEnumerable<AircraftImportMatchRow>) ViewState[szvsMatchRowsKey]; }
        set
        {
            ViewState[szvsMatchRowsKey] = value;
            gvAircraftCandidates.DataSource = value;
            gvAircraftCandidates.DataBind();
        }
    }

    public  IDictionary<string, MakeModel> ModelMapping
    {
        get
        {
            Dictionary<string, MakeModel> d = (Dictionary<string, MakeModel>)ViewState[szvsModelMapping];
            if (d == null)
                ViewState[szvsModelMapping] = d = new Dictionary<string, MakeModel>();
            return d;
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("jquery1", ResolveClientUrl("https://code.jquery.com/jquery-1.10.1.min.js"));
        Page.ClientScript.RegisterClientScriptInclude("jquery2", ResolveClientUrl("~/public/Scripts/jquery.json-2.4.min.js"));
        Page.ClientScript.RegisterClientScriptInclude("MFBAircraftImportScript", ResolveClientUrl("~/Public/Scripts/ImpAircraft.js?v=1"));
        if (IsPostBack)
        {
            // fix up the state of any aircraft that might have been added asynchronously (i.e., via web service call, in which case the viewstate is out of date)
            if (CandidatesForImport != null)
                AircraftImportMatchRow.RefreshRecentlyAddedAircraftForUser(CandidatesForImport, Page.User.Identity.Name);
            UpdateGrid();
        }
    }

    protected void UpdateGrid()
    {
        gvAircraftCandidates.DataSource = CandidatesForImport;
        gvAircraftCandidates.DataBind();
    }

    protected void gvAircraftCandidates_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            AircraftImportMatchRow mr = (AircraftImportMatchRow)e.Row.DataItem;
            GridViewRow gvr = e.Row;

            // Column 0: Aircraft tail.  Link to registration, if necessary
            HyperLink lnkFAA = (HyperLink)e.Row.FindControl("lnkFAA");
            lnkFAA.NavigateUrl = Aircraft.LinkForTailnumberRegistry(mr.TailNumber);
            ((Label)gvr.FindControl("lblGivenTail")).Visible = !(lnkFAA.Visible = !String.IsNullOrEmpty(lnkFAA.NavigateUrl));
            ((Label)gvr.FindControl("lblAircraftVersion")).Text = (mr.BestMatchAircraft != null && mr.BestMatchAircraft.Version > 0) ? Resources.Aircraft.ImportAlternateVersion : string.Empty;

            // Column 1 - Best match
            DropDownList cmbInst = (DropDownList)e.Row.FindControl("cmbInstType");

            HiddenField hdnContext = (HiddenField)e.Row.FindControl("hdnContext");
            if (mr.State == AircraftImportMatchRow.MatchState.UnMatched)
            {
                Label lblInstType = (Label)e.Row.FindControl("lblType");
                cmbInst.DataSource = AircraftInstance.GetInstanceTypes();
                cmbInst.DataBind();
                cmbInst.SelectedValue = mr.BestMatchAircraft.InstanceTypeID.ToString(CultureInfo.InvariantCulture);

                cmbInst.Attributes["onchange"] = String.Format(CultureInfo.InvariantCulture, "javascript:updateInstanceDesc('{0}', '{1}', '{2}');", cmbInst.ClientID, lblInstType.ClientID, hdnContext.ClientID);
            }

            // column 2 - "Add this" and status
            Button btnAddThis = (Button)gvr.FindControl("btnAddThis");
            Label lProblem = (Label)e.Row.FindControl("lblACErr");
            Label lblAllGood = (Label)e.Row.FindControl("lblAllGood");
            lblAllGood.Style["display"] = (mr.State == AircraftImportMatchRow.MatchState.JustAdded) ? "block" : "none";
            ((Label)gvr.FindControl("lblAlreadyInProfile")).Visible = mr.State == AircraftImportMatchRow.MatchState.MatchedInProfile;

            Panel pnlStaticMake = (Panel)e.Row.FindControl("pnlStaticMake");
            Panel pnlEditMake = (Panel)e.Row.FindControl("pnlEditMake");
            Image imgEdit = (Image)e.Row.FindControl("imgEdit");
            imgEdit.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:toggleModelEdit('{0}', '{1}');", pnlStaticMake.ClientID, pnlEditMake.ClientID);
            TextBox textBox = (TextBox)e.Row.FindControl("txtSearch");

            HiddenField hdnModel = (HiddenField)e.Row.FindControl("hdnSelectedModel");
            Label lblModel = (Label)e.Row.FindControl("lblSelectedMake");
            Dictionary<string, object> d = new Dictionary<string, object>() {
                { "lblID", lblModel.ClientID },         // ID of the label to display the selected model
                { "lblErr", lProblem.ClientID },        // ID of the label for displaying an error
                { "lblAllGood", lblAllGood.ClientID },  // ID of the label for displaying success
                { "mdlID", hdnModel.ClientID },         // ID of the hidden control with the selected model ID
                { "cmbInstance", cmbInst.ClientID },    // ID of the drop-down with the instance type specified
                { "progressID", popupAddingInProgress.BehaviorID }, // ID of the progress behavior ID
                { "btnAdd", btnAddThis.ClientID },      // ID of the "Add this" button
                { "pnlStaticMake", pnlStaticMake.ClientID },    // ID of the static view of the model/instance type to import
                { "pnlEditMake", pnlEditMake.ClientID },    // ID of the edit view to import
                { "matchRow", mr }                      // The match row with any additional context
            };
            AjaxControlToolkit.AutoCompleteExtender autoCompleteExtender = (AjaxControlToolkit.AutoCompleteExtender)e.Row.FindControl("autocompleteModel");
            hdnContext.Value = autoCompleteExtender.ContextKey = JsonConvert.SerializeObject(d);

            switch (mr.State)
            {
                case AircraftImportMatchRow.MatchState.JustAdded:
                case AircraftImportMatchRow.MatchState.MatchedInProfile:
                    btnAddThis.Visible = false;
                    btnAddThis.Attributes["onclick"] = string.Empty;
                    break;
                case AircraftImportMatchRow.MatchState.MatchedExisting:
                    btnAddThis.Visible = true;
                    btnAddThis.Text = Resources.Aircraft.ImportExistingAircraft;
                    btnAddThis.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "addExistingAircraft(JSON.parse(document.getElementById('{0}').value)); return false;", hdnContext.ClientID);
                    break;
                case AircraftImportMatchRow.MatchState.UnMatched:
                    imgEdit.Visible = true;
                    e.Row.FindControl("pnlEditMake").Visible = true;
                    btnAddThis.Visible = true;
                    btnAddThis.Text = Resources.Aircraft.ImportAddNewAircraft;
                    hdnModel.Value = mr.BestMatchAircraft.ModelID.ToString(CultureInfo.InvariantCulture);
                    if (mr.SuggestedModel == null || !String.IsNullOrEmpty(mr.BestMatchAircraft.ErrorString))
                    {
                        textBox.Text = mr.ModelGiven;
                        pnlEditMake.Style["display"] = "block";
                        pnlStaticMake.Style["display"] = "none";
                        btnAddThis.Style["display"] = "none";
                    }
                    else
                    {
                        textBox.Text = string.Empty;
                        pnlEditMake.Style["display"] = "none";
                        pnlStaticMake.Style["display"] = "block";
                        btnAddThis.Style["display"] = "block";
                    }

                    btnAddThis.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "addNewAircraft(JSON.parse(document.getElementById('{0}').value)); return false;", hdnContext.ClientID);
                    break;
            }

            if (mr.BestMatchAircraft != null && mr.BestMatchAircraft.ErrorString.Length > 0)
            {
                lProblem.Text = mr.BestMatchAircraft.ErrorString;
                btnAddThis.Style["display"] = "none";
            }
            else
            {
                lProblem.Text = string.Empty;

                if (mr.State == AircraftImportMatchRow.MatchState.JustAdded)
                {
                    lblAllGood.Style["display"] = "block";
                    btnAddThis.Style["display"] = "none";
                }
            }
        }
    }

    protected void gvAircraftCandidates_RowCommand(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentException("null gridviewcommandeventargs for new aircraft to import", "e");

        int idRow = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
        if (e.CommandName.CompareOrdinalIgnoreCase("AddNew") == 0)
        {
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            AircraftImportMatchRow mr = CandidatesForImport.FirstOrDefault<AircraftImportMatchRow>(mr2 => mr2.ID == idRow);
            mr.BestMatchAircraft.Commit(Page.User.Identity.Name);

            ModelMapping[mr.ModelGiven] = MakeModel.GetModel(mr.BestMatchAircraft.ModelID);   // remember the mapping.

            // hack, but the commit above can result in the instance type being cleared out, so restore it.
            if (String.IsNullOrEmpty(mr.BestMatchAircraft.InstanceTypeDescription))
                mr.BestMatchAircraft.InstanceTypeDescription = AircraftInstance.GetInstanceTypes()[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;

            mr.State = AircraftImportMatchRow.MatchState.JustAdded;
            UpdateGrid();
        }
    }
}