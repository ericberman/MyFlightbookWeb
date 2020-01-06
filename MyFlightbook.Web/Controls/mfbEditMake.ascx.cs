using MyFlightbook;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditMake : System.Web.UI.UserControl
{
    public MakeModel Model { get; set; }

    public int MakeID
    {
        get { return Convert.ToInt32(hdnID.Value, CultureInfo.InvariantCulture); }
        set { hdnID.Value = value.ToString(CultureInfo.InvariantCulture); InitFormForMake(); }
    }

    protected MakeModel.HighPerfType HighPerfType
    {
        get
        {
            if (ckHighPerf.Checked)
                return (ckLegacyHighPerf.Checked ? MakeModel.HighPerfType.Is200HP : MakeModel.HighPerfType.HighPerf);
            else
                return MakeModel.HighPerfType.NotHighPerf;
        }
        set
        {
            switch (value)
            {
                case MakeModel.HighPerfType.HighPerf:
                    ckHighPerf.Checked = true;
                    ckLegacyHighPerf.Checked = false;
                    break;
                case MakeModel.HighPerfType.Is200HP:
                    ckHighPerf.Checked = ckLegacyHighPerf.Checked = true;
                    break;
                case MakeModel.HighPerfType.NotHighPerf:
                default:
                    ckHighPerf.Checked = ckLegacyHighPerf.Checked = false;
                    break;
            }
        }
    }

    protected MakeModel.AvionicsTechnologyType AvionicsTechnology
    {
        get
        {
            return rbAvionicsAny.Checked ? MakeModel.AvionicsTechnologyType.None : (rbAvionicsGlass.Checked ? MakeModel.AvionicsTechnologyType.Glass : MakeModel.AvionicsTechnologyType.TAA);
        }
        set
        {
            switch (value)
            {
                case MakeModel.AvionicsTechnologyType.None:
                    rbAvionicsAny.Checked = true;
                    break;
                case MakeModel.AvionicsTechnologyType.Glass:
                    rbAvionicsGlass.Checked = true;
                    break;
                case MakeModel.AvionicsTechnologyType.TAA:
                    rbAvionicsTAA.Checked = true;
                    break;
            }
        }
    }

    public event System.EventHandler MakeUpdated = null;

    protected void RepopulateManufacturerDropdown(string defVal = null)
    {
        // Set up the manufacturer list every time, since it's large and consumes viewstate
        cmbManufacturer.SelectedIndex = -1;
        cmbManufacturer.SelectedValue = null;
        cmbManufacturer.DataSource = Manufacturer.CachedManufacturers();
        cmbManufacturer.DataBind();
        if (defVal != null)
            SelectIfPresent(defVal);
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        RepopulateManufacturerDropdown(Request.Form[cmbManufacturer.UniqueID]);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Model = null;
        ckComplex.Attributes.Add("onclick", "javascript:ComplexClicked()");
        ckConstantProp.Attributes.Add("onclick", "javascript:ComplexElementClicked()");
        ckRetract.Attributes.Add("onclick", "javascript:ComplexElementClicked()");
        ckCowlFlaps.Attributes.Add("onclick", "javascript:ComplexElementClicked()");
        cmbCatClass.Attributes.Add("onchange", "javascript:ComplexElementClicked()");
        ckHighPerf.Attributes.Add("onchange", "javascript:HighPerfClicked()");
        ckLegacyHighPerf.Attributes.Add("onchange", "javascript:LegacyHighPerfClicked()");
        foreach (ListItem li in rblTurbineType.Items)
        {
            MakeModel.TurbineLevel tl = (MakeModel.TurbineLevel)Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
            li.Attributes.Add("onclick", tl.IsTurbine() ? "javascript:showSinglePilotCertification()" : "javascript:hideSinglePilotCertification()");
        }

        lblAddNewManufacturer.Attributes.Add("onclick", "fnSetFocus('" + txtManufacturer.ClientID + "');");
        btnManOK.OnClientClick = String.Format(CultureInfo.InvariantCulture, "fnClickOK('{0}', '{1}')", btnManOK.UniqueID, "");
        pnlManufacturer.Style["display"] = "none";
        pnlDupesFound.Style["display"] = "none";

        if (!IsPostBack)
        {
            cmbCatClass.DataSource = CategoryClass.CategoryClasses();
            cmbCatClass.DataBind();
            divIsSimOnly.Visible = ((MyFlightbook.Profile.GetUser(Page.User.Identity.Name)).CanManageData);
            InitFormForMake();
        }
    }

    protected void InitFormForMake()
    {
        if (MakeID == MakeModel.UnknownModel)
            {
                btnAddMake.Text = Resources.LocalizedText.EditMakeAddMake;
                Model = new MakeModel();
            }
            else
            {
                btnAddMake.Text = Resources.LocalizedText.EditMakeUpdateMake;
                Model = MakeModel.GetModel(MakeID);
            }

        txtModel.Text = Model.Model;
        txtName.Text = Model.ModelName;
        txtType.Text = Model.TypeName;
        txtFamilyName.Text = Model.FamilyName;
        if (Model.CategoryClassID > 0)
            cmbCatClass.SelectedValue = Model.CategoryClassID.ToString();
        if (Model.ManufacturerID > 0)
            cmbManufacturer.SelectedValue = Model.ManufacturerID.ToString(CultureInfo.InvariantCulture);
        ckComplex.Checked = Model.IsComplex;
        HighPerfType = Model.PerformanceType;
        ckTailwheel.Checked = Model.IsTailWheel;
        ckConstantProp.Checked = Model.IsConstantProp;
        ckCowlFlaps.Checked = Model.HasFlaps;
        ckRetract.Checked = Model.IsRetract;
        AvionicsTechnology = Model.AvionicsTechnology;
        rblTurbineType.SelectedIndex = (int)Model.EngineType;
        rblAircraftAllowedTypes.SelectedIndex = (int) Model.AllowedTypes;
        ckTMG.Checked = ((Model.CategoryClassID == CategoryClass.CatClassID.Glider) && Model.IsMotorGlider);
        ckMultiHeli.Checked = ((Model.CategoryClassID == CategoryClass.CatClassID.Helicopter) && Model.IsMultiEngineHelicopter);
        ckSinglePilot.Checked = Model.IsCertifiedSinglePilot;
        pnlSinglePilotOps.Style["display"] = Model.EngineType.IsTurbine() ? "block" : "none";
        UpdateRowsForCatClass(Model.CategoryClassID);

        rowArmyCurrency.Visible = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesArmyCurrency;
        txtArmyMDS.Text = Model.ArmyMDS;
    }

    protected void UpdateRowsForCatClass(CategoryClass.CatClassID idCatClass)
    {
        bool fIsGlider = idCatClass == CategoryClass.CatClassID.Glider;
        divTMG.Visible = fIsGlider;
        divTailwheel.Visible = CategoryClass.IsAirplane(idCatClass) && !CategoryClass.IsSeaClass(idCatClass);
        divMultiHeli.Visible = (idCatClass == CategoryClass.CatClassID.Helicopter);

        bool hasEngine = CategoryClass.HasEngine(idCatClass);
        rowEngineType.Visible = hasEngine;
        pnlHighPerfBlock.Visible = ckConstantProp.Enabled = ckComplex.Enabled = hasEngine;

        divComplex.Style["display"] = hasEngine || fIsGlider ? "inline-block" : "none";
        if (!hasEngine)
        {
            ckComplex.Checked = ckConstantProp.Checked = ckCowlFlaps.Checked = ckRetract.Checked = false;
            rblTurbineType.SelectedIndex = 0;
        }

        bool fIsLegacyEligible = (idCatClass == CategoryClass.CatClassID.AMEL || idCatClass == CategoryClass.CatClassID.AMES || idCatClass == CategoryClass.CatClassID.Helicopter);
        pnlLegacyHighPerf.Style["display"] = fIsLegacyEligible ? "inline" : "none";
        if (ckLegacyHighPerf.Checked && !fIsLegacyEligible)
            HighPerfType = MakeModel.HighPerfType.NotHighPerf;
    }

    protected MakeModel MakeFromForm()
    {
        CategoryClass.CatClassID ccId = (CategoryClass.CatClassID) Enum.Parse(typeof(CategoryClass.CatClassID), cmbCatClass.SelectedValue, true);
        Boolean fSea = CategoryClass.IsSeaClass(ccId);

        if (ckComplex.Checked)
        {
            if (fSea)
                ckRetract.Checked = true;
            ckCowlFlaps.Checked = true;
            ckConstantProp.Checked = true;
        }

        // if we were going to be really anal here, there would be no "complex" field in the database; 
        // it would be entirely derived.  but we're not being really anal.
        // Complex is FAR 61.31.
        if ((fSea || ckRetract.Checked) &&
            ckConstantProp.Checked && ckCowlFlaps.Checked)
            ckComplex.Checked = true;
        
        MakeModel mk = (MakeID == -1) ? new MakeModel() : new MakeModel(MakeID);

        mk.Model = txtModel.Text.Trim();
        mk.ModelName = txtName.Text.Trim();
        mk.TypeName = txtType.Text.Trim();
        mk.FamilyName = txtFamilyName.Text.Trim().ToUpper(CultureInfo.InvariantCulture).Replace("-", string.Empty).Replace(" ", string.Empty);
        mk.CategoryClassID = ccId;
        mk.ManufacturerID = Convert.ToInt32(cmbManufacturer.SelectedValue, CultureInfo.InvariantCulture);
        mk.IsComplex = ckComplex.Checked;
        mk.PerformanceType = HighPerfType;
        mk.IsTailWheel = ckTailwheel.Checked;
        mk.IsConstantProp = ckConstantProp.Checked;
        mk.HasFlaps = ckCowlFlaps.Checked;
        mk.IsRetract = ckRetract.Checked;
        mk.AvionicsTechnology = AvionicsTechnology;
        mk.EngineType = (MakeModel.TurbineLevel) rblTurbineType.SelectedIndex;
        mk.ArmyMDS = txtArmyMDS.Text;
        mk.AllowedTypes = (AllowedAircraftTypes) rblAircraftAllowedTypes.SelectedIndex;
        mk.IsMotorGlider = (ckTMG.Checked && (ccId == CategoryClass.CatClassID.Glider));
        mk.IsMultiEngineHelicopter = (ckMultiHeli.Checked && (ccId == CategoryClass.CatClassID.Helicopter));
        mk.IsCertifiedSinglePilot = ckSinglePilot.Checked && mk.EngineType.IsTurbine() && !String.IsNullOrEmpty(mk.TypeName);

        // Sanity check - no complex for a jet
        if (mk.EngineType == MakeModel.TurbineLevel.Jet)
            mk.IsComplex = mk.IsConstantProp = false;

        // these don't get persisted, but help with dupe detection.
        mk.CategoryClassDisplay = cmbCatClass.SelectedItem.Text;
        mk.ManufacturerDisplay = cmbManufacturer.SelectedItem.Text;

        UpdateRowsForCatClass(mk.CategoryClassID);

        return mk;
    }

    protected void CommitChanges(object sender, EventArgs e, Boolean fIgnoreDupes)
    {
        if (!Page.IsValid)
            return;

        Model = MakeFromForm();

        if (!fIgnoreDupes && MakeID < 0)
        {
            MakeModel[] rgmmDupes = Model.PossibleMatches();
            if (rgmmDupes.Length > 0)
            {
                gvDupes.DataSource = rgmmDupes;
                gvDupes.DataBind();
                modalPopupDupes.Show();
                return;
            }
        }

        // If this is a new make, inherit the manufacturer's sim-only status.
        if (MakeID == -1) // creation event
            Model.AllowedTypes = (new Manufacturer(Model.ManufacturerID)).AllowedTypes;

        try
        {
            string szOriginalDesc = string.Empty;
            bool fIsNew = Model.IsNew;
            if (!fIsNew)
                szOriginalDesc = new MakeModel(MakeID).ToString();

            Model.Commit();

            // use fIsNew because Model.IsNew may have been true and not now.
            string szLinkEditModel = String.Format(CultureInfo.InvariantCulture, "{0}?id={1}", "~/Member/EditMake.aspx".ToAbsoluteURL(Request), Model.MakeModelID);
            string szNewDesc = Model.ToString();
            if (fIsNew)
                util.NotifyAdminEvent("New Model created", String.Format(CultureInfo.InvariantCulture, "User: {0}\r\n\r\n{1}\r\n{2}", MyFlightbook.Profile.GetUser(Page.User.Identity.Name).DetailedName, szNewDesc, szLinkEditModel), ProfileRoles.maskCanManageData);
            else
            {
                if (String.Compare(szNewDesc, szOriginalDesc, StringComparison.Ordinal) != 0)
                    util.NotifyAdminEvent("Model updated", String.Format(CultureInfo.InvariantCulture, "User: {0}\r\n\r\nWas:\r\n{1}\r\n\r\nIs Now: \r\n{2}\r\n \r\nID: {3}, {4}", 
                        MyFlightbook.Profile.GetUser(Page.User.Identity.Name).DetailedName, 
                        szOriginalDesc, 
                        szNewDesc, 
                        Model.MakeModelID, szLinkEditModel), ProfileRoles.maskCanManageData);
            }

            MakeID = Model.MakeModelID;

            ClearDupes();
            if (MakeUpdated != null)
                MakeUpdated(sender, e);
        }
        catch (MyFlightbookException ex)
        {
            lblError.Text = ex.Message;
        }
    }

    protected void ClearDupes()
    {
        modalPopupDupes.Hide();
        pnlDupesFound.Style["display"] = "none";
        gvDupes.DataSource = new MakeModel[0];
        gvDupes.DataBind();
    }

    protected void gvDupes_RowCommand(Object sender, CommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "_Use", StringComparison.OrdinalIgnoreCase) == 0)
        {
            MakeID = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            ClearDupes();
            if (MakeUpdated != null)
                MakeUpdated(sender, e);
        }
    }

    protected void btnAddMake_Click(object sender, EventArgs e)
    {
        CommitChanges(sender, e, false);
    }

    protected void btnIReallyMeanIt_Click(object sender, EventArgs e)
    {
        CommitChanges(sender, e, true);
    }

    /// <summary>
    /// Selects the named manufacturer if in the list, returns true if it hit.
    /// </summary>
    /// <param name="sz">The name of the manufacturer to select</param>
    /// <returns></returns>
    protected Boolean SelectIfPresent(string sz)
    {
        if (sz == null)
            return false;

        for (int i = 0; i < cmbManufacturer.Items.Count; i++)
        {
            if (String.Compare(cmbManufacturer.Items[i].Text.Trim(), sz.Trim(), StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                cmbManufacturer.SelectedIndex = i;
                return true;
            }
        }
        return false;
    }

    protected void btnManOK_Click(object sender, EventArgs e)
    {
        // Add the manufacturer
        // Re-bind the combo box
        // Clear out the manufacturer.
        string szNew = txtManufacturer.Text;

        if (txtManufacturer.Text.Length > 0)
        {
            // first see if this string is already present; if so, just select it.
            if (!SelectIfPresent(szNew))
            {
                Manufacturer m = new Manufacturer(szNew);
                if (m.IsNew)
                    m.FCommit();
                RepopulateManufacturerDropdown(m.ManufacturerName);
                SelectIfPresent(m.ManufacturerName);
            }
        }

        txtManufacturer.Text = "";
        ModalPopupExtender1.Hide();
    }

    protected void ManufacturerChanged(object sender, EventArgs e)
    {
        int idMan = Convert.ToInt32(cmbManufacturer.SelectedValue, CultureInfo.InvariantCulture);
        if (idMan > 0)
        {
            Manufacturer man = new Manufacturer(idMan);
            if (man.AllowedTypes != AllowedAircraftTypes.Any)
                rblAircraftAllowedTypes.SelectedIndex = (int)man.AllowedTypes;
        }
    }

    protected void cmbCatClass_SelectedIndexChanged(object sender, EventArgs e)
    {
        MakeFromForm(); 
    }
}
