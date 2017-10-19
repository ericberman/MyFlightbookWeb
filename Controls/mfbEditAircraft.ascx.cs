using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;
using System.Web;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.Clubs;

/******************************************************
 * 
 * Copyright (c) 2008-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
 * Some testing notes:
 * 
 * We have at least the following scenarios to handle/test
 * 
 * -	New Aircraft (sim, anonymous, real)
 * -	Existing – Only user (i.e., it's fine to make changes)
 * -	Existing – Other users
 * -	Existing – Sim
 * -	Existing – Anonymous
 * -	Existing – Locked – shared
 * -	Existing – Locked – alone (kinda silly, but hey); 
 * 
 * */

public partial class Controls_mfbEditAircraft : System.Web.UI.UserControl
{
    private const string szKeyVSAircraftInProgress = "AircraftInProgress";
    private const string szKeyVSAircraftStats = "AircraftStats";

    public event System.EventHandler AircraftUpdated = null;

    #region Properties
    /// <summary>
    /// Returns whether or not we are in admin mode.  In admin mode, we can edit airplanes but don't add/remove to the user's list.
    /// </summary>
    public bool AdminMode
    {
        get { return Convert.ToBoolean(hdnAdminMode.Value, CultureInfo.CurrentCulture); }
        set { hdnAdminMode.Value = value.ToString(); }
    }

    protected int LastSelectedManufacturer
    {
        get { return String.IsNullOrEmpty(hdnLastMan.Value) ? Manufacturer.UnsavedID : Convert.ToInt32(hdnLastMan.Value, CultureInfo.InvariantCulture); }
        set { hdnLastMan.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// The ID of the aircraft being edited
    /// </summary>
    public int AircraftID
    {
        get { return m_ac.AircraftID; }
        set { ViewState[szKeyVSAircraftInProgress] = m_ac = new Aircraft(value); }
    }

    private Aircraft m_ac = new Aircraft();
    
    /// <summary>
    /// The aircraft being edited.
    /// </summary>
    public Aircraft Aircraft
    {
        get { return m_ac; }
    }

    /// <summary>
    /// The model that has been selected for this aircraft (used for admin)
    /// </summary>
    public int SelectedModelID
    {
        get { return Convert.ToInt32(cmbMakeModel.SelectedValue, CultureInfo.InvariantCulture); }
    }

    protected AircraftInstanceTypes SelectedInstanceType
    {
        get { return (AircraftInstanceTypes)Convert.ToInt32(cmbAircraftInstance.SelectedValue, CultureInfo.InvariantCulture); }
        set { cmbAircraftInstance.SelectedValue = ((int)value).ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// Stats for this aircraft
    /// </summary>
    protected AircraftStats Stats
    {
        get { return (AircraftStats)ViewState[szKeyVSAircraftStats]; }
        set { ViewState[szKeyVSAircraftStats] = value; }
    }

    /// <summary>
    /// Is this aircraft set to a real aircraft or a sim?
    /// </summary>
    protected bool IsRealAircraft
    {
        get { return (m_ac.InstanceType == AircraftInstanceTypes.RealAircraft); }
    }

    protected bool IsAnonymous
    {
        get { return CountryCodePrefix.BestMatchCountryCode(m_ac.TailNumber).IsAnonymous; }
    }

    protected bool IsEditable
    {
        get { return mvModel.ActiveViewIndex == 1; }
        set
        {
            mvModel.ActiveViewIndex = (value ? 1 : 0);
            lnkNewMake.Visible = value;
        }
    }
    #endregion

    #region Page setup
    protected void RepopulateList(BaseDataBoundControl cmb, IEnumerable<object> lst)
    {
        if (cmb == null)
            throw new ArgumentNullException("cmb");
        cmb.DataSource = lst;
        cmb.DataBind();
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        // For efficiency of viewstate, we repopulate manufactures and country code on each postback.
        RepopulateList(cmbManufacturers, Manufacturer.CachedManufacturers());
        RepopulateList(cmbCountryCode, CountryCodePrefix.CountryCodes());
        hdnSimCountry.Value = CountryCodePrefix.szSimPrefix; // hack, but avoids "CA1303:Do not pass literals as localized parameters" warning.
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (ViewState[szKeyVSAircraftInProgress] != null)
            m_ac = (Aircraft)ViewState[szKeyVSAircraftInProgress];

        if (!IsPostBack)
        {
            cmbAircraftInstance.DataSource = AircraftInstance.GetInstanceTypes();
            cmbAircraftInstance.DataBind();

            InitFormForAircraft();
        }
        else
        {
            // If it's an existing aircraft and images may have been uploaded, process them
            if (!m_ac.IsNew)
                ProcessImages();
        }

        SyncTailToCountry();
        util.SetValidationGroup(mfbMaintainAircraft, "EditAircraft");
    }
    #endregion

    #region Image handling
    protected void ProcessImages()
    {
        if (m_ac.AircraftID != Aircraft.idAircraftUnknown)
        {
            mfbMFUAircraftImages.ImageKey = mfbIl.Key = m_ac.AircraftID.ToString(CultureInfo.InvariantCulture);
            mfbMFUAircraftImages.ProcessUploadedImages();
            mfbIl.Refresh();
        }
    }

    protected void AddPicturesForAircraft()
    {
        mfbIl.AltText = txtTail.Text;
        mfbIl.ImageClass = MFBImageInfo.ImageClass.Aircraft;
        mfbIl.Key = m_ac.AircraftID.ToString(CultureInfo.InvariantCulture);
        mfbIl.CanMakeDefault = Stats != null && Stats.Users > 1;
        mfbIl.DefaultImage = m_ac.DefaultImage;
        mfbIl.Refresh();
        pnlImageNote.Visible = mfbIl.Images.ImageArray.Count > 0;
    }

    protected void mfbIl_MakeDefault(object sender, MFBImageInfoEvent e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        m_ac.DefaultImage = e.Image.ThumbnailFile;
        AddPicturesForAircraft();   // refresh.
    }
    #endregion

    #region Validation
    protected void ValidateTailNum(object sender, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        // ensure that there is more than just a country code
        if (txtTail.Text.Length == 0 || String.Compare(txtTail.Text, cmbCountryCode.SelectedValue, StringComparison.CurrentCultureIgnoreCase) == 0)
            args.IsValid = false;

        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);
        if (String.Compare(txtTail.Text, cc.Prefix, StringComparison.CurrentCultureIgnoreCase) == 0)
            args.IsValid = false;
    }

    protected void ValidateTailNumHasCountry(object sender, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        string szPrefix = cmbCountryCode.SelectedValue;
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);

        // ensure that there is a valid country code, allowing an explicit "OTHER"
        if (cc.Prefix.Length == 0 && szPrefix.Length > 0)
            args.IsValid = false;
    }

    protected void ValidateSim(object sender, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);
        AircraftInstanceTypes ait = SelectedInstanceType;

        if (ait == AircraftInstanceTypes.RealAircraft && cc.IsSim)
            args.IsValid = false;

        if (ait != AircraftInstanceTypes.RealAircraft && !cc.IsSim)
            args.IsValid = false;
    }
    #endregion

    #region Data <-> Form binding
    #region Helper methods for existing aircraft
    /// <summary>
    /// Display the statistics for this aircraft
    /// </summary>
    protected void SetUpStats()
    {
        Stats = m_ac.IsNew ? new AircraftStats() : new AircraftStats(Page.User.Identity.Name, m_ac.AircraftID);
        pnlStats.Visible = !m_ac.IsNew;
        List<string> lst = new List<string>();
        lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftUserStats, Stats.Users, Stats.Flights));

        if (Stats.LatestDate.CompareTo(DateTime.MinValue) != 0)
            lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftYourStats, Stats.UserFlights, Stats.EarliestDate.ToShortDateString(), Stats.LatestDate.ToShortDateString()));

        if (AdminMode)
            lst.Add(String.Format(CultureInfo.CurrentCulture, "Users = {0}", String.Join(", ", Stats.UserNames)));

        lnkViewTotals.Visible = !AdminMode && m_ac.AircraftID != Aircraft.idAircraftUnknown;
        FlightQuery fq = new FlightQuery(Page.User.Identity.Name) { AircraftIDList = new int[] { m_ac.AircraftID } };
        lnkViewTotals.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(Convert.ToBase64String(fq.ToJSONString().Compress())));

        rptStats.DataSource = lst;
        rptStats.DataBind();
    }

    /// <summary>
    /// find the longest country prefix that matches for this aircraft
    /// </summary>
    protected void SetUpCountryCode()
    {
        if (m_ac.IsNew)
        {
            string[] rgLocale = Request.UserLanguages;
            if (rgLocale != null && rgLocale.Length > 0 && rgLocale[0].Length > 4)
                cmbCountryCode.SelectedValue = CountryCodePrefix.DefaultCountryCodeForLocale(Request.UserLanguages[0].Substring(3, 2)).Prefix;
            else
                cmbCountryCode.SelectedIndex = 0;
            UpdateMask();
        }
        else
        {
            string szTail = m_ac.TailNumber;

            CountryCodePrefix ccp = CountryCodePrefix.BestMatchCountryCode(szTail);
            string szPrefix = ccp.Prefix;

            if (szPrefix.Length > 0) // Should be!
            {
                szTail = szTail.Substring(szPrefix.Length);
                cmbCountryCode.SelectedValue = (ccp.IsSim) ? "N" : szPrefix;
                UpdateMask();
            }
            else
                cmbCountryCode.SelectedValue = String.Empty;
        }
    }

    /// <summary>
    /// Sets up and expands any notes for the aircraft
    /// </summary>
    protected void SetUpNotes()
    {
        // Public notes should always be present in the aircraft...
        txtPublicNotes.Text = m_ac.PublicNotes;

        // ... But private notes and default image aren't generally there
        if (m_ac.IsNew)
            txtPrivateNotes.Text = string.Empty;
        else
        {
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            Aircraft acExisting = ua.GetUserAircraftByID(AircraftID);
            if (acExisting != null)
            {
                txtPrivateNotes.Text = acExisting.PrivateNotes;
                mfbIl.DefaultImage = m_ac.DefaultImage = acExisting.DefaultImage;
            }
        }

        // Expand the notes, if something is present.
        expandoNotes.ExpandoControl.Collapsed = String.IsNullOrEmpty(txtPublicNotes.Text + txtPrivateNotes.Text);
        tabNotes.ActiveTab = (String.IsNullOrEmpty(txtPublicNotes.Text) && !String.IsNullOrEmpty(txtPrivateNotes.Text)) ? tabPrivateNotes : tabPublicNotes;

        rowNotes.Visible = !m_ac.IsNew;
    }

    /// <summary>
    /// Expand the maintenance section if this user has ever touched that part of this airplane
    /// </summary>
    protected void SetUpMaintenance()
    {
        rowMaintenance.Visible = IsRealAircraft && !m_ac.IsNew;

        if (!rowMaintenance.Visible)
            return;

        mfbMaintainAircraft.Maintenance = m_ac.Maintenance;
        mfbMaintainAircraft.AircraftID = m_ac.AircraftID;
        mfbMaintainAircraft.InitForm();

        MaintenanceLog[] rgml = MaintenanceLog.ChangesByAircraftID(m_ac.AircraftID);
        if (rgml != null)
        {
            foreach (MaintenanceLog ml in rgml)
            {
                if (ml.User.CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) == 0)
                    expandoMaintenance.ExpandoControl.Collapsed = false;
            }
        }
    }

    /// <summary>
    /// Displays any club schedules for this aircraft
    /// </summary>
    protected void SetUpSchedules()
    {
        if (!Page.User.Identity.IsAuthenticated || m_ac.IsNew)
            return;

        IEnumerable<Club> lstClubs = Club.ClubsForAircraft(m_ac.AircraftID, Page.User.Identity.Name);
        if (lstClubs.Count() > 0)
        {
            rowClubSchedules.Visible = true;
            rptSchedules.DataSource = lstClubs;
            rptSchedules.DataBind();
        }
    }

    /// <summary>
    /// Displays any alternative versions of this aircraft
    /// </summary>
    protected void SetUpAlternativeVersions()
    {
        if (m_ac.IsNew)
            return;

        List<Aircraft> lst = Aircraft.AircraftMatchingTail(m_ac.TailNumber);
        lst.RemoveAll(ac => ac.AircraftID == m_ac.AircraftID);
        if (lst.Count > 0)
        {
            pnlAlternativeVersions.Visible = true;
            gvAlternativeVersions.DataSource = lst;
            gvAlternativeVersions.DataBind();
        }
    }
    #endregion

    protected void InitFormForAircraft()
    {
        bool fIsNew = m_ac.IsNew;
        bool fIsReal = m_ac.InstanceType == AircraftInstanceTypes.RealAircraft;
        bool fIsAnonymous = m_ac.IsAnonymous;

        mvInstanceType.SetActiveView(fIsNew ? vwInstanceNew : vwInstanceExisting);

        if (fIsNew)
            LastSelectedManufacturer = Manufacturer.UnsavedID;

        SetUpStats();

        // disable editing of sims and anonymous aircraft.
        bool fCanEdit = IsRealAircraft && !IsAnonymous && (AdminMode || !m_ac.IsLocked);
        ckIsGlass.Enabled = ckAnonymous.Enabled = cmbMakeModel.Enabled = cmbManufacturers.Enabled = imgEditAircraftModel.Visible = fCanEdit;
        pnlAnonymous.Visible = fIsNew;

        pnlLockedExplanation.Visible = m_ac.IsLocked;
        pnlLocked.Visible = AdminMode && IsRealAircraft;
        ckAnonymous.Checked = IsRealAircraft && IsAnonymous;
        ckLocked.Checked = m_ac.IsLocked;

        SelectedInstanceType = m_ac.InstanceType;
        // don't allow instance type to be changed after it is created; could lead to collisions between aircraft.
        lblInstanceType.Text = cmbAircraftInstance.SelectedItem.Text;

        SetUpCountryCode();

        if (fIsNew)
            UpdateModelList(Manufacturer.UnsavedID);
        else
            SelectCurrentMakeModel();

        ckIsGlass.Checked = m_ac.IsGlass;

        mfbIl.Key = txtTail.Text = m_ac.TailNumber;

        SetUpNotes();

        SetUpMaintenance();

        SetUpAlternativeVersions();

        SetUpSchedules();

        btnAddAircraft.Text = fIsNew ? Resources.LocalizedText.EditAircraftAddAircraft : Resources.LocalizedText.EditAircraftUpdateAircraft;
        IsEditable = m_ac.IsNew || (fIsReal && !m_ac.IsAnonymous && Stats.Users <= 1 && !m_ac.IsLocked);

        mfbDateOfGlassUpgrade.Date = m_ac.GlassUpgradeDate.HasValue ? m_ac.GlassUpgradeDate.Value : DateTime.MinValue;
        pnlGlassUpgradeDate.Visible = ckIsGlass.Checked;

        AdjustForAircraftType();

        AddPicturesForAircraft();

        // So that we can detect a tailnumber change (which should force a change of country code) vs. a country-code change 
        // (which should force a change of tail number), preserve the last value for each.
        // We have to do this since we rebuild the country code dropdown on each postback (to save viewstate size), which makes it
        // hard to detect true changes of one vs. the other.
        hdnLastCountry.Value = cmbCountryCode.SelectedValue;
        hdnLastTail.Value = txtTail.Text;
    }

    /// <summary>
    /// Shows/hides functionality based on instance type, new/existing, sim, or anonymous.  Does NOT bind any data
    /// </summary>
    protected void AdjustForAircraftType()
    {
        Boolean fRealAircraft = IsRealAircraft;
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(m_ac.TailNumber);
        Boolean fIsNew = m_ac.IsNew;
        Boolean fHasModelSpecified = (m_ac.ModelID > 0);

        Boolean fIsAnonymous = ckAnonymous.Checked;

        mvTailnumber.SetActiveView(fRealAircraft ? vwRealAircraft : vwSimTail);

        MakeModel mm = MakeModel.GetModel(m_ac.ModelID);

        // Show glass option if this is not, by model, a glass cockpit AND if it's not anonymous
        pnlGlassCockpit.Visible = !m_ac.IsAnonymous && !mm.IsAllGlass;
        ckAnonymous.Enabled = true;
        if (mm.AllowedTypes == AllowedAircraftTypes.SimOrAnonymous && fRealAircraft)
        {
            fIsAnonymous = ckAnonymous.Checked = true;
            ckAnonymous.Enabled = false;
        }

        rowCountry.Visible = fRealAircraft && !fIsAnonymous;
        rowMaintenance.Visible = rowCountry.Visible && !fIsNew;

        if (fRealAircraft)
        {
            if (fIsAnonymous)
            {
                lblAnonTailDisplay.Text = m_ac.TailNumber = txtTail.Text = (fHasModelSpecified) ? Aircraft.AnonymousTailnumberForModel(m_ac.ModelID) : String.Empty;
                pnlAnonTail.Visible = (fHasModelSpecified);
            }
            else
            {
                if (cc.IsSim || cc.IsAnonymous)   // reset tail if switching from sim or anonymous
                    m_ac.TailNumber = txtTail.Text = cmbCountryCode.SelectedItem.Value;
                else
                    m_ac.TailNumber = txtTail.Text = CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(cmbCountryCode.SelectedItem.Text, cmbCountryCode.SelectedValue), txtTail.Text);
            }

            mvRealAircraft.SetActiveView(fIsAnonymous ? vwAnonTail : vwRegularTail);
            valTailNumber.Enabled = valPrefix.Enabled = fIsAnonymous;
            FixFAALink();
        }
        else
        {
            // Sim 
            if (fIsNew && fHasModelSpecified)
                m_ac.TailNumber = Aircraft.SuggestSims(m_ac.ModelID, m_ac.InstanceType)[0].TailNumber;
            pnlSimTail.Visible = (fHasModelSpecified);

            lblSimTail.Text = txtTail.Text = m_ac.TailNumber;
            ckAnonymous.Checked = false;
        }
    }
    #endregion

    #region Committing Changes
    protected void SaveChanges()
    {
        lblError.Text = string.Empty;
        string szTailNew = txtTail.Text.ToUpper();
        int AircraftIDOrig = AircraftID;

        Aircraft ac = (AircraftID == Aircraft.idAircraftUnknown) ? new Aircraft() : new Aircraft(AircraftID);  // if editing an existing airplane, init to the existing data (only modify what's in the UI)

        // Rules for how we want to handle a tailnumber edit
        // a) New aircraft - no issue (Commit will match to an existing aircraft as needed)
        // b) Existing aircraft with no other users: go ahead and change the aircraft.
        // c) Existing aircraft with other users: Treat as a new aircraft with the specified model; after commit, do any replacement as necessary
        // In other words, migrate the user to another aircraft if we are editing an existing aircraft, changing the tail, and there are other users.
        bool fChangedTail = (String.Compare(Aircraft.NormalizeTail(ac.TailNumber), Aircraft.NormalizeTail(szTailNew), true) != 0);
        bool fMigrateUser = (!ac.IsNew && fChangedTail && (new AircraftStats(Page.User.Identity.Name, m_ac.AircraftID)).Users > 1);

        ac.TailNumber = szTailNew;  // set the tailnumber regardless.
        if (fMigrateUser)
            ac.AircraftID = Aircraft.idAircraftUnknown; // treat this as if we are adding a new aircraft.
        else
            ac.UpdateMaintenanceForUser(mfbMaintainAircraft.MaintenanceForAircraft(), Page.User.Identity.Name);

        ac.ModelID = Convert.ToInt32(cmbMakeModel.SelectedValue, CultureInfo.InvariantCulture);
        ac.InstanceTypeID = (int) this.SelectedInstanceType;
        ac.IsGlass = ckIsGlass.Checked;
        ac.GlassUpgradeDate = (ac.IsGlass && mfbDateOfGlassUpgrade.Date.HasValue()) ? mfbDateOfGlassUpgrade.Date : (DateTime?) null;
        ac.IsLocked = ckLocked.Checked;
        ac.PrivateNotes = txtPrivateNotes.Text;
        ac.PublicNotes = txtPublicNotes.Text;
        ac.DefaultImage = mfbIl.DefaultImage;

        // Preserve any flags
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        if (!ac.IsNew)
        {
            Aircraft[] rgac = ua.GetAircraftForUser();
            Aircraft acExisting = Array.Find(rgac, a => a.AircraftID == AircraftID);
            if (acExisting != null)
                ac.CopyFlags(acExisting);
        }

        try
        {
            if (AdminMode)
                ac.Commit();
            else
                ac.Commit(Page.User.Identity.Name);

            m_ac = ac;
            AircraftID = ac.AircraftID;

            ProcessImages();

            // If we migrated, then move the user's existing flights.
            if (fMigrateUser)
                ua.ReplaceAircraftForUser(ac, new Aircraft(AircraftIDOrig), true);
        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;
        }
    }

    protected void btnAddAircraft_Click(object sender, EventArgs e)
    {
        if (Page.IsValid)
        {
            SaveChanges();

            if (lblError.Text.Length == 0)
            {
                if (AircraftUpdated != null)
                    AircraftUpdated(sender, e);
            }
        }
    }
    #endregion

    #region Tailnumber management
    protected void SyncTailToCountry()
    {
        bool fChangedTail = hdnLastTail.Value.CompareCurrentCultureIgnoreCase(txtTail.Text) != 0;
        bool fChangedCountry = hdnLastCountry.Value.CompareCurrentCultureIgnoreCase(cmbCountryCode.SelectedValue) != 0;

        if (!fChangedTail && !fChangedCountry)
            return;

        if (fChangedCountry)
        {
            UpdateMask();
            txtTail.Text = CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(cmbCountryCode.SelectedItem.Text, cmbCountryCode.SelectedValue), txtTail.Text);
            FixFAALink();
        }
        else if (fChangedTail)
        {
            // sync the countrycode to the tail.  Note that we set hdnLastCountry.Value here as well to prevent a potential loop.
            cmbCountryCode.SelectedValue = hdnLastCountry.Value = CountryCodePrefix.BestMatchCountryCode(txtTail.Text).Prefix;
        }
        hdnLastCountry.Value = cmbCountryCode.SelectedValue;
        hdnLastTail.Value = txtTail.Text;
    }

    protected void cmbCountryCode_SelectedIndexChanged(object sender, EventArgs e)
    {
        SyncTailToCountry();
    }

    protected void FixFAALink()
    {
        lnkAdminFAALookup.NavigateUrl = Aircraft.LinkForTailnumberRegistry(txtTail.Text);
        lnkAdminFAALookup.Visible = !Aircraft.IsNew && lnkAdminFAALookup.NavigateUrl.Length > 0;
    }

    protected void UpdateMask()
    {
        string szCountry = IsRealAircraft ? cmbCountryCode.SelectedValue : hdnSimCountry.Value;
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);
        string szSuffix = txtTail.Text.Substring(cc.Prefix.Length);
        txtTail.Text = szCountry + szSuffix;
    }

    protected void lnkPopulateAircraft_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtTail.Text) && m_ac.IsNew)
        {
            Aircraft ac = new Aircraft(txtTail.Text);
            if (!ac.IsNew)
            {
                AircraftID = ac.AircraftID;
                InitFormForAircraft();
            }
        }
    }

    protected void ckAnonymous_CheckedChanged(object sender, EventArgs e)
    {
        AdjustForAircraftType();
    }
    #endregion

    #region Instance Type
    protected void cmbAircraftInstance_SelectedIndexChanged(object sender, EventArgs e)
    {
        m_ac.InstanceType = SelectedInstanceType;
        UpdateMask();
        ckIsGlass.Checked = false;      // reset this since model changed.
        ckAnonymous.Checked = false;    // reset this as well.

        AdjustForAircraftType();
    }
    #endregion

    #region Alternative Versions
    protected void gvAlternativeVersions_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        GridViewRow grow = (GridViewRow)((LinkButton)e.CommandSource).NamingContainer;
        Aircraft ac = new Aircraft(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        bool fMigrate = e.CommandName.CompareOrdinalIgnoreCase("_switchMigrate") == 0;
        ua.ReplaceAircraftForUser(ac, m_ac, fMigrate);
        Response.Redirect(fMigrate ? String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", ac.AircraftID) : "~/Member/Aircraft.aspx");
    }
    #endregion

    #region Schedules
    protected void rptSchedules_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Controls_mfbResourceSchedule sched = (Controls_mfbResourceSchedule)e.Item.FindControl("schedAircraft");
        Controls_SchedSummary summ = (Controls_SchedSummary)sched.SubNavContainer.Controls[0].FindControl("schedSummary");
        summ.Refresh();
    }
    #endregion

    #region Change Make/Model Functionality
    protected void SelectCurrentMakeModel()
    {
        MakeModel mm = MakeModel.GetModel(m_ac.ModelID);
        cmbManufacturers.SelectedValue = mm.ManufacturerID.ToString(CultureInfo.InvariantCulture);
        LastSelectedManufacturer = mm.ManufacturerID;
        UpdateModelList(mm.ManufacturerID);

        cmbMakeModel.SelectedValue = m_ac.ModelID.ToString(CultureInfo.InvariantCulture);

        // Capture this as well in case not editable.
        lblMakeModel.Text = m_ac.LongModelDescription;
    }

    protected void cmbMakeModel_SelectedIndexChanged(object sender, EventArgs e)
    {
        m_ac.ModelID = Convert.ToInt32(cmbMakeModel.SelectedValue, CultureInfo.InvariantCulture);
        ckIsGlass.Checked = false;  // reset this since model changed.
        AdjustForAircraftType();
    }

    protected void UpdateModelList(int idManufacturer)
    {
        ListItem liSelect = cmbMakeModel.Items[0];  // hold onto the "please select a model" item.
        cmbMakeModel.Items.Clear();
        cmbMakeModel.Items.Add(liSelect);
        cmbMakeModel.DataSource = (idManufacturer == MakeModel.UnknownModel) ? new System.Collections.ObjectModel.Collection<MakeModel>() : MakeModel.MatchingMakes(idManufacturer);
        cmbMakeModel.DataBind();
    }

    protected void cmbManufacturers_SelectedIndexChanged(object sender, EventArgs e)
    {
        int newManId = Convert.ToInt32(cmbManufacturers.SelectedValue, CultureInfo.InvariantCulture);
        if (newManId != LastSelectedManufacturer)
        {
            UpdateModelList(newManId);
            m_ac.ModelID = MakeModel.UnknownModel;
        }
        LastSelectedManufacturer = newManId;
        AdjustForAircraftType();
    }

    protected void ckIsGlass_CheckedChanged(object sender, EventArgs e)
    {
        pnlGlassUpgradeDate.Visible = ckIsGlass.Checked;
    }

    protected void btnChangeModelTweak_Click(object sender, EventArgs e)
    {
        IsEditable = true;
        SelectCurrentMakeModel();   // not quite sure why I need to do this, but otherwise the manufacturer dropdown reverts.
    }

    protected void btnChangeModelClone_Click(object sender, EventArgs e)
    {
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Public/ContactMe.aspx?subj={0}", System.Web.HttpUtility.UrlEncode(String.Format(CultureInfo.CurrentCulture, "Incorrect definition for aircraft {0}", m_ac.TailNumber))));
    }

    #endregion
}
