using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
 * Some testing notes:
 * 
 * We have at least the following scenarios to handle/test
 * 
| Scenario                                   | Change model?                    | Edit Tail?                    | Edit Glass/TAA?                |
| -------------------------------------------|----------------------------------|-------------------------------|--------------------------------|
| New, Real, Registered                      | Yes                              | Yes                           | IF model isn't glass/TAA-only  |
| New, Real, Anonymous                       | Yes                              | No                            | No                             |
| New, Sim                                   | Yes                              | No - Read-only, with Sim note | No (**currently allowed**)     |
| Existing, Real, Registered, single user    | Yes                              | Yes (but see issue #80        | Yes                            |
| Existing, Real, Registered, Multiple users | Yes, AFTER confirming minor edit | Yes (but see issue #80)       | If model isn't glass/TAA-only  |
| Existing, Real, Anonymous                  | No                               | No                            | No (**currently allowed**)     |
| Existing, Sim                              | No                               | No                            | No (**currently allowed**)     |
| Existing - Locked, non-admin               | No                               | No                            | IF model isn't glass/TAA-only  |
| Existing - Locked, Admin                   | Yes, AFTER confirmation          | No                            | IF model isn't glass/TAA-only  |

And a few action scenarios to test (preserve existing behavior):
 * New Aircraft, type-ahead on tail number, select => Should redirect (or refill) using the selected aircraft 
 * Clicking Anonymous needs to hide the tailnumber
 * Switching between real/training device should show/hide ability to edit tail number
 * Changing model needs to reset glass checkbox
 * Verify cloning, locking (admin functions) still work)
 
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
        get { return SelectMake1.SelectedModelID; }
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

    protected MakeModel.AvionicsTechnologyType AvionicsUpgrade
    {
        get
        {
            return rbAvionicsNone.Checked ? MakeModel.AvionicsTechnologyType.None : (rbAvionicsGlass.Checked ? MakeModel.AvionicsTechnologyType.Glass : MakeModel.AvionicsTechnologyType.TAA);
        }
        set
        {
            switch (value)
            {
                case MakeModel.AvionicsTechnologyType.None:
                    rbAvionicsNone.Checked = true;
                    rbAvionicsGlass.Checked = rbAvionicsTAA.Checked = false;
                    break;
                case MakeModel.AvionicsTechnologyType.Glass:
                    rbAvionicsGlass.Checked = true;
                    rbAvionicsTAA.Checked = rbAvionicsNone.Checked = false;
                    break;
                case MakeModel.AvionicsTechnologyType.TAA:
                    rbAvionicsTAA.Checked = true;
                    rbAvionicsGlass.Checked = rbAvionicsNone.Checked = false;
                    break;
            }
            pnlGlassUpgradeDate.Visible = value != MakeModel.AvionicsTechnologyType.None;
        }
    }
    #endregion

    #region Page setup
    protected void Page_Init(object sender, EventArgs e)
    {
        // For efficiency of viewstate, we repopulate country code on each postback.
        cmbCountryCode.DataSource = CountryCodePrefix.CountryCodes();
        cmbCountryCode.DataBind();
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

        string szNormalTail = Aircraft.NormalizeTail(txtTail.Text);
        // ensure that there is more than just the country code that is specified by the drop-down
        if (txtTail.Text.Length == 0 || String.Compare(szNormalTail, Aircraft.NormalizeTail(cmbCountryCode.SelectedValue), StringComparison.CurrentCultureIgnoreCase) == 0)
            args.IsValid = false;

        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);
        if (cc.Prefix.Length == 0 && !String.IsNullOrEmpty(cmbCountryCode.SelectedValue))
            args.IsValid = false;

        // Ensure that what's there is more than the country code
        if (String.Compare(szNormalTail, cc.NormalizedPrefix, StringComparison.CurrentCultureIgnoreCase) == 0)
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
    protected IEnumerable<LinkedString> StatsForUser
    {
        get
        {
            if (m_ac.IsNew)
                return new LinkedString[0]; // nothing to add for a new aircraft

            List<LinkedString> lst = new List<LinkedString>();

            Stats = new AircraftStats(Page.User.Identity.Name, m_ac.AircraftID);
            // Add overall stats
            lst.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftUserStats, Stats.Users, Stats.Flights)));
            // And add personal stats
            lst.Add(Stats.UserStatsDisplay);

            if (AdminMode && util.GetStringParam(Request, "listUsers").Length > 0)
                lst.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, "Users = {0}", String.Join(", ", Stats.UserNames))));

            return lst;
        }
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
                cmbCountryCode.SelectedValue = CountryCodePrefix.DefaultCountryCodeForLocale(Request.UserLanguages[0].Substring(3, 2)).HyphenatedPrefix;
            else
                cmbCountryCode.SelectedIndex = 0;
            UpdateMask();
        }
        else
        {
            string szTail = m_ac.TailNumber;

            CountryCodePrefix ccp = CountryCodePrefix.BestMatchCountryCode(szTail);
            string szPrefix = ccp.HyphenatedPrefix;

            if (szPrefix.Length > 0) // Should be!
            {
                szTail = szTail.Substring(szPrefix.Length);
                cmbCountryCode.SelectedValue = (ccp.IsSim) ? CountryCodePrefix.DefaultCountryCodeForLocale(Request.UserLanguages[0].Substring(3, 2)).HyphenatedPrefix : szPrefix;
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

        // disable editing of sims and anonymous aircraft.
        bool fIsLocked = (!AdminMode && m_ac.IsLocked);
        bool fCanEdit = IsRealAircraft && !IsAnonymous && !fIsLocked;
        rbAvionicsNone.Enabled = rbAvionicsGlass.Enabled = rbAvionicsTAA.Enabled = ckAnonymous.Enabled = fCanEdit;
        pnlAnonymous.Visible = fIsNew;


        pnlLockedExplanation.Visible = m_ac.IsLocked;
        ckLocked.Visible = AdminMode && IsRealAircraft;
        ckAnonymous.Checked = IsRealAircraft && IsAnonymous;
        ckLocked.Checked = m_ac.IsLocked;

        SelectedInstanceType = m_ac.InstanceType;

        SetUpCountryCode();

        List<LinkedString> lstAttrib = new List<LinkedString>();
        if (!fIsNew)
        {
            lstAttrib.Add(new LinkedString(cmbAircraftInstance.SelectedItem.Text));
            lstAttrib.AddRange(StatsForUser);
        }
        SelectMake1.AircraftAttributes = lstAttrib;
        SelectMake1.SelectedModelID = m_ac.ModelID;

        AvionicsUpgrade = m_ac.AvionicsTechnologyUpgrade;

        mfbIl.Key = txtTail.Text = m_ac.TailNumber;

        SetUpNotes();

        SetUpMaintenance();

        SetUpAlternativeVersions();

        SetUpSchedules();

        bool fIsKnown = AdminMode || (!fIsNew && (new UserAircraft(Page.User.Identity.Name)).GetUserAircraftByID(m_ac.AircraftID) != null);

        btnAddAircraft.Text = fIsKnown ? Resources.LocalizedText.EditAircraftUpdateAircraft : Resources.LocalizedText.EditAircraftAddAircraft;

        // Set the edit mode, based on chart above, which breaks down as:
        // Editing automatically enabled IF new aircraft OR real, registered, unlocked, single user.
        // Editing offered (with confirmation) if real, registered, (unlocked or admin), multiple-user
        // Otherwise, no editing
        if (fIsNew || (fCanEdit && Stats.Users == 1))
            SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.Edit; 
        else if (fCanEdit && !fIsLocked)
            SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.EditWithConfirm;
        else
            SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.Locked;

        mfbDateOfGlassUpgrade.Date = m_ac.GlassUpgradeDate.HasValue ? m_ac.GlassUpgradeDate.Value : DateTime.MinValue;
        pnlGlassUpgradeDate.Visible = !rbAvionicsNone.Checked;

        AdjustForAircraftType();

        AddPicturesForAircraft();

        // So that we can detect a tailnumber change (which should force a change of country code) vs. a country-code change 
        // (which should force a change of tail number), preserve the last value for each.
        // We have to do this since we rebuild the country code dropdown on each postback (to save viewstate size), which makes it
        // hard to detect true changes of one vs. the other.
        hdnLastCountry.Value = cmbCountryCode.SelectedValue;
        hdnLastTail.Value = txtTail.Text;

        pnlReuseWarning.Visible = fIsNew;
        if (!fIsNew)
            lblTailnumber.Text = m_ac.TailNumber;
    }

    /// <summary>
    /// Shows/hides functionality based on instance type, new/existing, sim, or anonymous.  Does NOT bind any data
    /// </summary>
    protected void AdjustForAircraftType()
    {
        MakeModel mm = MakeModel.GetModel(m_ac.ModelID);

        Boolean fRealAircraft = IsRealAircraft;
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(m_ac.TailNumber);

        if (mm.AllowedTypes == AllowedAircraftTypes.SimulatorOnly)
        {
            fRealAircraft = false;
            cc = CountryCodePrefix.SimCountry;
        }

        Boolean fIsNew = m_ac.IsNew;
        Boolean fHasModelSpecified = (m_ac.ModelID > 0);

        Boolean fIsAnonymous = ckAnonymous.Checked;

        mvTailnumber.SetActiveView(fRealAircraft ? vwRealAircraft : vwSimTail);

        // Show glass option if this is not, by model, a glass cockpit AND if it's not anonymous
        bool fRealRegistered = fRealAircraft && !m_ac.IsAnonymous;

        // All avionics upgrade options are limited to real, registered aircraft.  Beyond that:
        // - Can upgrade to glass if the model is not already glass or TAA (i.e., = steam)
        // - Can upgrade to TAA if the model is not already TAA-only AND this is an airplane (TAA is airplane only).
        // - So we will hide the overall avionics upgrade panel if no upgrade is possible.
        rbAvionicsGlass.Visible = fRealRegistered && mm.AvionicsTechnology == MakeModel.AvionicsTechnologyType.None;
        pnlTAA.Visible = fRealRegistered && mm.AvionicsTechnology != MakeModel.AvionicsTechnologyType.TAA;
        pnlGlassCockpit.Visible = rbAvionicsGlass.Visible || pnlTAA.Visible;

        // Sanity check
        if ((rbAvionicsGlass.Checked && !rbAvionicsGlass.Visible) || (rbAvionicsTAA.Checked && !rbAvionicsTAA.Visible))
            AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;

        ckAnonymous.Enabled = true;
        if (mm.AllowedTypes == AllowedAircraftTypes.SimOrAnonymous && fRealAircraft)
        {
            fIsAnonymous = ckAnonymous.Checked = true;
            ckAnonymous.Enabled = false;
        }

        rowCountry.Visible = fRealAircraft && !fIsAnonymous;
        rowMaintenance.Visible = rowCountry.Visible && !fIsNew;

        mfbATDFTD1.Visible = fIsNew && !fRealAircraft;

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
            valTailNumber.Enabled = fIsAnonymous;
            FixFAALink();
        }
        else
        {
            // Sim 
            if (fHasModelSpecified)
                m_ac.TailNumber = Aircraft.SuggestSims(m_ac.ModelID, m_ac.InstanceType)[0].TailNumber;
            vwSimTailDisplay.SetActiveView(fHasModelSpecified ? vwHasModel : vwNoModel);

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
        bool fIsNew = ac.IsNew;
        bool fChangedModel = ac.ModelID != SelectedModelID;

        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);

        // check if this isn't already in our aircraft list
        int addToUserCount = (!fIsNew && fChangedModel && !AdminMode && ua.GetUserAircraftByID(m_ac.AircraftID) == null) ? 1 : 0;

        bool fOtherUsers = !fIsNew && (fChangedTail || fChangedModel) && (new AircraftStats(Page.User.Identity.Name, m_ac.AircraftID)).Users + addToUserCount > 1;   // no need to compute user stats if new, or if tail and model are both unchanged
        bool fMigrateUser = (!fIsNew && fChangedTail && fOtherUsers);

        // Check for model change without tail number change on an existing aircraft
        if (!AdminMode && !fIsNew && !fChangedTail && fChangedModel && fOtherUsers && ac.HandlePotentialClone(SelectedModelID, Page.User.Identity.Name))
        {
            Response.Redirect("~/Member/Aircraft.aspx");
            return;
        }

        // set the tailnumber regardless, using appropriate hyphenation (if hyphenation is specified)
        CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(szTailNew);
        ac.TailNumber = (cc.HyphenPref == CountryCodePrefix.HyphenPreference.None) ? szTailNew.ToUpperInvariant() : Aircraft.NormalizeTail(szTailNew, CountryCodePrefix.BestMatchCountryCode(szTailNew));
        if (fMigrateUser)
            ac.AircraftID = Aircraft.idAircraftUnknown; // treat this as if we are adding a new aircraft.
        else
            ac.UpdateMaintenanceForUser(mfbMaintainAircraft.MaintenanceForAircraft(), Page.User.Identity.Name);

        ac.ModelID = SelectedModelID;
        ac.InstanceTypeID = (int) this.SelectedInstanceType;
        ac.AvionicsTechnologyUpgrade = AvionicsUpgrade;
        ac.GlassUpgradeDate = (ac.AvionicsTechnologyUpgrade != MakeModel.AvionicsTechnologyType.None && mfbDateOfGlassUpgrade.Date.HasValue()) ? mfbDateOfGlassUpgrade.Date : (DateTime?) null;
        ac.IsLocked = ckLocked.Checked;
        ac.PrivateNotes = txtPrivateNotes.Text;
        ac.PublicNotes = txtPublicNotes.Text;
        ac.DefaultImage = mfbIl.DefaultImage;

        // Preserve any flags
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
            FixFAALink();
        }
        else if (fChangedTail)
        {
            // sync the countrycode to the tail.  Note that we set hdnLastCountry.Value here as well to prevent a potential loop.
            cmbCountryCode.SelectedValue = hdnLastCountry.Value = CountryCodePrefix.BestMatchCountryCode(txtTail.Text).HyphenatedPrefix;
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
        txtTail.Text = CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(cmbCountryCode.SelectedItem.Text, cmbCountryCode.SelectedValue), txtTail.Text);
    }

    protected void lnkPopulateAircraft_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtTail.Text) && m_ac.IsNew)
        {
            int aircraftID = Aircraft.idAircraftUnknown;
            if (int.TryParse(hdnSelectedAircraftID.Value, out aircraftID))
            {
                Response.Redirect("~/Member/EditAircraft.aspx?id=" + aircraftID.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Aircraft ac = new Aircraft(txtTail.Text);
                if (!ac.IsNew)
                {
                    AircraftID = ac.AircraftID;
                    InitFormForAircraft();
                }
            }
        }
    }

    protected void ckAnonymous_CheckedChanged(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(txtTail.Text) && !ckAnonymous.Checked && m_ac.IsNew)
            SetUpCountryCode();
        AdjustForAircraftType();
    }
    #endregion

    #region Instance Type
    protected void cmbAircraftInstance_SelectedIndexChanged(object sender, EventArgs e)
    {
        m_ac.InstanceType = SelectedInstanceType;
        UpdateMask();
        AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;  // reset this since model changed.
        ckAnonymous.Checked = false;    // reset this as well.

        AdjustForAircraftType();
    }
    #endregion

    #region Alternative Versions
    protected void SwitchToAlternateVersion(int idAircraft, bool fMigrate)
    {
        Aircraft ac = new Aircraft(idAircraft);
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        ua.ReplaceAircraftForUser(ac, m_ac, fMigrate);
        Response.Redirect(fMigrate ? String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", ac.AircraftID) : "~/Member/Aircraft.aspx");
    }

    protected void gvAlternativeVersions_RowCommand(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.CommandName.CompareCurrentCultureIgnoreCase("_switchMigrate") == 0)
            SwitchToAlternateVersion(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture), true);
        else if (e.CommandName.CompareCurrentCultureIgnoreCase("_switchNoMigrate") == 0)
            SwitchToAlternateVersion(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture), false);
        else if (e.CommandName.CompareCurrentCultureIgnoreCase("_merge") == 0)
        {
            Aircraft acClone = new Aircraft(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
            if (!acClone.IsNew)
            {
                Aircraft.AdminMergeDupeAircraft(m_ac, acClone);
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", m_ac.AircraftID));
            }
        }
    }

    protected void gvAlternativeVersions_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            ((Controls_mfbHoverImageList)e.Row.FindControl("mfbHoverThumb")).Refresh();
        }
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

    protected void rbGlassUpgrade_CheckedChanged(object sender, EventArgs e)
    {
        pnlGlassUpgradeDate.Visible = AvionicsUpgrade != MakeModel.AvionicsTechnologyType.None;
    }

    protected void SelectMake1_ModelChanged(object sender, MakeSelectedEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        m_ac.ModelID = e.SelectedModel;
        AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;  // reset this since model changed.
        AdjustForAircraftType();
    }

    protected void SelectMake1_MajorChangeRequested(object sender, EventArgs e)
    {
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Public/ContactMe.aspx?subj={0}", System.Web.HttpUtility.UrlEncode(String.Format(CultureInfo.CurrentCulture, "Incorrect definition for aircraft {0}", m_ac.TailNumber))));
    }
}
