using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

public partial class Controls_mfbImportAircraft : System.Web.UI.UserControl
{
    #region Properties

    private const string szsessMakesKey = "sessMakesKey";
    private const string szsessAllManufacturers = "sessManufacturers";
    private const string szvsMatchRowsKey = "vsMatchRowsKey";

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

    private Collection<MakeModel> AllMakes
    {
        get
        {
            if (Session[szsessMakesKey] == null)
                Session[szsessMakesKey] = MakeModel.MatchingMakes();
            return (Collection<MakeModel>)Session[szsessMakesKey];
        }
        set { Session[szsessMakesKey] = value; }
    }

    private IEnumerable<Manufacturer> AllManufacturers
    {
        get
        {
            if (Session[szsessAllManufacturers] == null)
                Session[szsessAllManufacturers] = Manufacturer.AllManufacturers();
            return (IEnumerable<Manufacturer>)Session[szsessAllManufacturers];
        }
        set { Session[szsessAllManufacturers] = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("MFBAircraftImportScript", ResolveClientUrl("~/Public/ImpAircraft.js"));
        if (!IsPostBack)
        {
        }
        else
        {
            // fix up the state of any aircraft that might have been added asynchronously (i.e., via web service call, in which case the viewstate is out of date)
            if (CandidatesForImport != null)
                AircraftImportMatchRow.RefreshRecentlyAddedAircraftForUser(CandidatesForImport, Page.User.Identity.Name);
        }
    }

    protected void UpdateGrid()
    {
        gvAircraftCandidates.DataSource = CandidatesForImport;
        gvAircraftCandidates.DataBind();
    }

    public void Refresh()
    {
        AllMakes = null;
        AllManufacturers = null;
    }

    protected void cmbManufacturer_DataChanged(object sender, EventArgs e)
    {
        GridViewRow grow = (GridViewRow)((Control)sender).NamingContainer;
        AircraftImportMatchRow mr = MatchRowFromGridviewRow(grow);
        DropDownList cmbModel = (DropDownList)grow.FindControl("cmbModel");
        int idMan = Convert.ToInt32(((DropDownList)sender).SelectedValue, CultureInfo.InvariantCulture);
        List<MakeModel> lst = ModelsForManufacturer(idMan);
        mr.SpecifiedModel = lst[0];
        mr.BestMatchAircraft.ModelID = lst[0].MakeModelID;
        mr.BestMatchAircraft.ModelCommonName = lst[0].DisplayName;
        mr.BestMatchAircraft.FixTailAndValidate();
        UpdateGrid();
    }

    protected void cmbModel_DataChanged(object sender, EventArgs e)
    {
        GridViewRow grow = (GridViewRow)((Control)sender).NamingContainer;
        DropDownList cmbModel = (DropDownList)grow.FindControl("cmbModel");
        DropDownList cmbInst = (DropDownList)grow.FindControl("cmbInstType");
        AircraftImportMatchRow mr = MatchRowFromGridviewRow(grow);
        mr.BestMatchAircraft.InstanceTypeID = Convert.ToInt32(cmbInst.SelectedValue, CultureInfo.InvariantCulture);
        mr.BestMatchAircraft.InstanceTypeDescription = cmbInst.SelectedItem.Text;
        mr.BestMatchAircraft.ModelID = Convert.ToInt32(cmbModel.SelectedValue, CultureInfo.InvariantCulture);
        mr.SpecifiedModel = null;
        foreach (MakeModel mm in AllMakes)
        {
            if (mm.MakeModelID == mr.BestMatchAircraft.ModelID)
            {
                mr.SpecifiedModel = mm;
                break;
            }
        }
        if (mr.SpecifiedModel == null)
            mr.BestMatchAircraft.ModelID = MakeModel.UnknownModel;
        else
            mr.BestMatchAircraft.ModelCommonName = mr.SpecifiedModel.DisplayName;

        mr.BestMatchAircraft.FixTailAndValidate();

        UpdateGrid();
    }

    protected List<MakeModel> ModelsForManufacturer(int idMan)
    {
        List<MakeModel> lstModelsForManufacturer = new List<MakeModel>(AllMakes).FindAll(mm => mm.ManufacturerID == idMan);
        lstModelsForManufacturer.Sort((m1, m2) => { return m1.ModelDisplayName.CompareTo(m2.ModelDisplayName); });
        return lstModelsForManufacturer;
    }

    protected AircraftImportMatchRow MatchRowFromGridviewRow(GridViewRow grow)
    {
        HiddenField h = (HiddenField)grow.FindControl("hdnMatchRowID");
        int id = Convert.ToInt32(h.Value);
        return CandidatesForImport.FirstOrDefault<AircraftImportMatchRow>(mr => mr.ID == id);
    }

    protected void gvAircraftCandidates_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateGrid();
    }

    protected void gvAircraftCandidates_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            AircraftImportMatchRow mr = (AircraftImportMatchRow)e.Row.DataItem;
            GridView gv = (GridView)sender;
            GridViewRow gvr = e.Row;

            // Column 0: select or not
            LinkButton lnkEdit = (LinkButton)gvr.FindControl("lnkEdit");
            lnkEdit.Visible = mr.State == AircraftImportMatchRow.MatchState.UnMatched;
            lnkEdit.CommandArgument = gvr.RowIndex.ToString(CultureInfo.InvariantCulture);

            // Column 1: Aircraft tail.  Link to registration, if necessary
            HyperLink lnkFAA = (HyperLink)e.Row.FindControl("lnkFAA");
            lnkFAA.NavigateUrl = Aircraft.LinkForTailnumberRegistry(mr.TailNumber);
            ((Label)gvr.FindControl("lblGivenTail")).Visible = !(lnkFAA.Visible = !String.IsNullOrEmpty(lnkFAA.NavigateUrl));
            ((Label)gvr.FindControl("lblAircraftVersion")).Text = (mr.BestMatchAircraft != null && mr.BestMatchAircraft.Version > 0) ? Resources.Aircraft.ImportAlternateVersion : string.Empty;

            // Column 2 - Best match
            MultiView mvMatch = (MultiView)e.Row.FindControl("mvMatch");
            mvMatch.ActiveViewIndex = 0;

            DropDownList cmbInst = (DropDownList)e.Row.FindControl("cmbInstType");
            DropDownList cmbModel = (DropDownList)e.Row.FindControl("cmbModel");
            DropDownList cmbManufacturer = (DropDownList)e.Row.FindControl("cmbManufacturer");
            if (e.Row.RowIndex == gv.SelectedIndex && mr.State == AircraftImportMatchRow.MatchState.UnMatched)
            {
                cmbInst.DataSource = AircraftInstance.GetInstanceTypes();
                cmbInst.DataBind();
                cmbInst.SelectedValue = mr.BestMatchAircraft.InstanceTypeID.ToString(CultureInfo.InvariantCulture);

                cmbManufacturer.DataSource = AllManufacturers;
                cmbManufacturer.DataBind();
                int idMan = (mr.SpecifiedModel == null) ? Manufacturer.UnsavedID : mr.SpecifiedModel.ManufacturerID;
                int idModel = (mr.SpecifiedModel == null) ? MakeModel.UnknownModel : mr.SpecifiedModel.MakeModelID;
                cmbManufacturer.SelectedValue = idMan.ToString(CultureInfo.InvariantCulture);

                cmbModel.DataSource = ModelsForManufacturer(idMan);
                cmbModel.DataBind();
                cmbModel.SelectedValue = idModel.ToString(CultureInfo.InvariantCulture);

                mvMatch.ActiveViewIndex = 1;

                e.Row.BackColor = System.Drawing.Color.FromArgb(0xCC, 0xCC, 0xCC);
                e.Row.BorderColor = System.Drawing.Color.Black;
                e.Row.BorderStyle = BorderStyle.Solid;
                e.Row.BorderWidth = Unit.Pixel(1);
            }
            else
            {
                cmbInst.Items.Clear();
                cmbModel.Items.Clear();
                cmbManufacturer.Items.Clear();
            }

            // column 3 - "Add this" and status
            Button btnAddThis = (Button)gvr.FindControl("btnAddThis");
            Label lProblem = (Label)e.Row.FindControl("lblACErr");
            Label lblAllGood = (Label)e.Row.FindControl("lblAllGood");
            lblAllGood.Style["display"] = (mr.State == AircraftImportMatchRow.MatchState.JustAdded) ? "block" : "none";
            ((Label)gvr.FindControl("lblAlreadyInProfile")).Visible = mr.State == AircraftImportMatchRow.MatchState.MatchedInProfile;

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
                    btnAddThis.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "addExistingAircraft({0}, '{1}', '{2}'); return false;", mr.BestMatchAircraft.AircraftID, btnAddThis.ClientID, lblAllGood.ClientID);
                    break;
                case AircraftImportMatchRow.MatchState.UnMatched:
                    btnAddThis.Visible = true;
                    btnAddThis.Text = Resources.Aircraft.ImportAddNewAircraft;
                    if (e.Row.RowIndex != gv.SelectedIndex && mr.State == AircraftImportMatchRow.MatchState.UnMatched && mr.BestMatchAircraft != null)
                    {
                        // Bypass a postback if it is an error-free non-selected row.  This should avoid some of the viewstate errors we've been getting for click-click-click down the list of buttons
                        btnAddThis.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "addNewAircraft('{0}', '{1}', {2}, '{3}', {4}); return false;",
                            btnAddThis.ClientID,
                            lblAllGood.ClientID,
                            mr.BestMatchAircraft.ModelID,
                            mr.BestMatchAircraft.TailNumber,
                            mr.BestMatchAircraft.InstanceTypeID);
                    }
                    break;
            }

            if (mr.BestMatchAircraft != null && mr.BestMatchAircraft.ErrorString.Length > 0)
            {
                lProblem.Text = mr.BestMatchAircraft.ErrorString;
                btnAddThis.Visible = false;
            }
            else
            {
                lProblem.Text = string.Empty;

                if (mr.State == AircraftImportMatchRow.MatchState.JustAdded)
                {
                    lblAllGood.Style["display"] = "block";
                    btnAddThis.Visible = false;
                    lnkEdit.Visible = false; // disable the Edit button
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

            // hack, but the commit above can result in the instance type being cleared out, so restore it.
            if (String.IsNullOrEmpty(mr.BestMatchAircraft.InstanceTypeDescription))
                mr.BestMatchAircraft.InstanceTypeDescription = AircraftInstance.GetInstanceTypes()[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;

            mr.State = AircraftImportMatchRow.MatchState.JustAdded;
            gvAircraftCandidates.SelectedIndex = -1; // don't select anything.
            UpdateGrid();
        }
        else if (e.CommandName.CompareOrdinalIgnoreCase("Select") == 0)
        {
            gvAircraftCandidates.SelectedIndex = idRow;
        }
    }
}