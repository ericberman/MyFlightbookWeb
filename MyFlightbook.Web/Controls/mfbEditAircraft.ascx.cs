using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftControls
{
    /// <summary>
    /// base class to simplify class coupling for mfbEditAircraft; handles a lot of the stuff that either 
    /// (a) doesn't require direct access to controls on the page, or 
    /// (b) doesn't really directly relate to aircraft
    /// </summary>
    public partial class mfbEditAircraftBase : UserControl
    {
        private const string szKeyVSAircraftStats = "AircraftStats";
        private const string szKeyVSAdminMode = "AdminMode";

        /// <summary>
        /// Returns whether or not we are in admin mode.  In admin mode, we can edit airplanes but don't add/remove to the user's list.
        /// </summary>
        public bool AdminMode
        {
            get { return Convert.ToBoolean(ViewState[szKeyVSAdminMode] ?? false, CultureInfo.CurrentCulture); }
            set { ViewState[szKeyVSAdminMode] = value; }
        }

        #region Schedule stuff
        /// <summary>
        /// Displays any club schedules for this aircraft
        /// <paramref name="ac">The aircraft</paramref>
        /// <paramref name="rowClubSchedules">The row to show/hide if there are clubs found</paramref>
        /// <paramref name="rptSchedules">The repeater control for any clubs</paramref>
        /// <paramref name="setOwner">An action to take if the club has an owner name prepend policy</paramref>
        /// </summary>
        protected void SetUpSchedules(Aircraft ac, Repeater rptSchedules, System.Web.UI.HtmlControls.HtmlControl rowClubSchedules, Action<string> setOwner)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));

            if (!Page.User.Identity.IsAuthenticated || ac.IsNew)
                return;

            if (rptSchedules == null)
                throw new ArgumentNullException(nameof(rptSchedules));
            if (rowClubSchedules == null)
                throw new ArgumentNullException(nameof(rowClubSchedules));

            List<MyFlightbook.Clubs.Club> lstClubs = new List<MyFlightbook.Clubs.Club>(MyFlightbook.Clubs.Club.ClubsForAircraft(ac.AircraftID, Page.User.Identity.Name));
            if (lstClubs.Count > 0)
            {
                rowClubSchedules.Visible = true;
                rptSchedules.DataSource = lstClubs;
                rptSchedules.DataBind();

                // If *any* club has policy PrependsScheduleWithOwnerName, set the default text for it
                foreach (MyFlightbook.Clubs.Club c in lstClubs)
                {
                    if (c.PrependsScheduleWithOwnerName)
                    {
                        setOwner?.Invoke(Profile.GetUser(Page.User.Identity.Name).UserFullName);
                        break;
                    }
                }
            }
        }

        protected static void BindSchedules(RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            Controls_mfbResourceSchedule sched = (Controls_mfbResourceSchedule)e.Item.FindControl("schedAircraft");
            Controls_SchedSummary summ = (Controls_SchedSummary)sched.SubNavContainer.Controls[0].FindControl("schedSummary");
            summ.Refresh();
        }
        #endregion

        protected void RedirectForId(int aircraftID)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("~/Member/EditAircraft.aspx?id=" + aircraftID.ToString(CultureInfo.InvariantCulture));
            foreach (var key in Request.QueryString.AllKeys)
            {
                if (key.CompareCurrentCultureIgnoreCase("id") == 0)
                    continue;
                sb.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}", key, HttpUtility.UrlEncode(Request.QueryString[key]));
            }
            Response.Redirect(sb.ToString());
        }

        #region Aircraft Stats
        /// <summary>
        /// Stats for this aircraft
        /// </summary>
        protected AircraftStats Stats
        {
            get { return (AircraftStats)ViewState[szKeyVSAircraftStats]; }
            set { ViewState[szKeyVSAircraftStats] = value; }
        }

        protected bool CanMakeImageDefault { get { return Stats != null && Stats.Users > 1; } }

        protected bool IsOnlyUserOfAircraft { get { return Stats != null && Stats.Users == 1; } }

        /// <summary>
        /// Display the statistics for this aircraft
        /// </summary>
        protected IEnumerable<LinkedString> StatsForUser(Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));

            if (ac.IsNew)
                return Array.Empty<LinkedString>(); // nothing to add for a new aircraft

            List<LinkedString> lst = new List<LinkedString>();

            Stats = new AircraftStats(Page.User.Identity.Name, ac.AircraftID);
            // Add overall stats
            lst.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.EditAircraftUserStats, Stats.Users, Stats.Flights)));
            // And add personal stats
            lst.Add(Stats.UserStatsDisplay);

            if (AdminMode && util.GetStringParam(Request, "listUsers").Length > 0)
                lst.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, "Users = {0}", String.Join(", ", Stats.UserNames))));

            return lst;
        }
        #endregion

        #region Maintenance
        protected bool ShouldShowMaintenance(int aircraftID)
        {
            MaintenanceLog[] rgml = MaintenanceLog.ChangesByAircraftID(aircraftID);
            if (rgml != null)
            {
                foreach (MaintenanceLog ml in rgml)
                {
                    if (ml.User.CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) == 0)
                        return true;
                }
            }
            return false;
        }

        protected static void SetValidationGroup(Control c, string szGroup)
        {
            util.SetValidationGroup(c, szGroup);
        }
        #endregion

        #region Admin
        protected void MergeClone(Aircraft ac, Aircraft acClone)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (acClone == null)
                throw new ArgumentNullException(nameof(acClone));

            if (!acClone.IsNew)
            {
                AircraftUtility.AdminMergeDupeAircraft(ac, acClone);
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", ac.AircraftID));
            }
        }
        #endregion

        protected static void RefreshImages(Control c)
        {
            Controls_mfbHoverImageList il = c as Controls_mfbHoverImageList;
            if (c != null)
                il.Refresh();
        }
    }

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
    | Existing, Real, Registered, single user    | Yes                              | No (but see issue #80         | Yes                            |
    | Existing, Real, Registered, Multiple users | Yes, AFTER confirming minor edit | No (but see issue #80)        | If model isn't glass/TAA-only  |
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

    public partial class mfbEditAircraft : mfbEditAircraftBase
    {
        private const string szKeyVSAircraftInProgress = "AircraftInProgress";

        public event System.EventHandler AircraftUpdated;

        #region Properties
        /// <summary>
        /// The ID of the aircraft being edited
        /// </summary>
        public int AircraftID
        {
            get { return m_ac.AircraftID; }
            set
            {
                ViewState[szKeyVSAircraftInProgress] = m_ac = new Aircraft(value);
                hdnAcRev.Value = m_ac.Revision.ToString(CultureInfo.InvariantCulture);
            }
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
            get { return rbTrainingDevice.Checked ? (AircraftInstanceTypes)Convert.ToInt32(rblTrainingDevices.SelectedValue, CultureInfo.InvariantCulture) : AircraftInstanceTypes.RealAircraft; }
            set
            {
                m_ac.InstanceType = value;
                rbRealRegistered.Checked = rbRealAnonymous.Checked = rbTrainingDevice.Checked = false;
                switch (value)
                {
                    case AircraftInstanceTypes.RealAircraft:
                        if (IsAnonymous)
                            rbRealAnonymous.Checked = true;
                        else
                            rbRealRegistered.Checked = true;
                        break;
                    case AircraftInstanceTypes.CertifiedATD:
                    case AircraftInstanceTypes.CertifiedIFRAndLandingsSimulator:
                    case AircraftInstanceTypes.CertifiedIFRSimulator:
                    case AircraftInstanceTypes.UncertifiedSimulator:
                    default:
                        rbTrainingDevice.Checked = true;
                        rblTrainingDevices.SelectedValue = ((int)value).ToString(CultureInfo.InvariantCulture);
                        break;
                }
            }
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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (ViewState[szKeyVSAircraftInProgress] != null)
                m_ac = (Aircraft)ViewState[szKeyVSAircraftInProgress];

            if (!IsPostBack)
            {
                cmbCountryCode.DataSource = CountryCodePrefix.CountryCodes();
                cmbCountryCode.DataBind();

                List<AircraftInstance> lst = new List<AircraftInstance>(AircraftInstance.GetInstanceTypes());
                lst.RemoveAll(aic => aic.IsRealAircraft);
                rblTrainingDevices.DataSource = lst;
                rblTrainingDevices.DataBind();
                rblTrainingDevices.SelectedIndex = 0;

                InitFormForAircraft();
            }
            else
            {
                // If it's an existing aircraft and images may have been uploaded, process them
                if (!m_ac.IsNew)
                    ProcessImages();
            }

            SyncTailToCountry();
            SetValidationGroup(mfbMaintainAircraft, "EditAircraft");
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
            mfbIl.CanMakeDefault = CanMakeImageDefault;
            mfbIl.DefaultImage = m_ac.DefaultImage;
            mfbIl.Refresh();
            pnlImageNote.Visible = mfbIl.Images.ImageArray.Count > 0;
        }

        protected void mfbIl_MakeDefault(object sender, MFBImageInfoEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            m_ac.DefaultImage = e.Image.ThumbnailFile;
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            Aircraft acToUpdate = ua.GetUserAircraftByID(m_ac.AircraftID);
            if (acToUpdate != null)
            {
                acToUpdate.DefaultImage = m_ac.DefaultImage;
                ua.FAddAircraftForUser(acToUpdate);
            }
            AddPicturesForAircraft();   // refresh.
        }
        #endregion

        #region Validation
        protected void ValidateTailNum(object sender, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

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
                throw new ArgumentNullException(nameof(args));
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
        /// find the longest country prefix that matches for this aircraft
        /// </summary>
        protected void SetUpCountryCode()
        {
            string szDefLocale = Request.UserLanguages != null && Request.UserLanguages.Length > 0 ? Request.UserLanguages[0] : string.Empty;
            if (szDefLocale.Length > 4)
                szDefLocale = szDefLocale.Substring(3, 2);

            if (m_ac.IsNew)
            {
                if (szDefLocale.Length > 0)
                    cmbCountryCode.SelectedValue = CountryCodePrefix.DefaultCountryCodeForLocale(szDefLocale).HyphenatedPrefix;
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
                    cmbCountryCode.SelectedValue = (ccp.IsSim) ? CountryCodePrefix.DefaultCountryCodeForLocale(szDefLocale).HyphenatedPrefix : szPrefix;
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

            expandoMaintenance.ExpandoControl.Collapsed = !ShouldShowMaintenance(m_ac.AircraftID);
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

            mvInstanceType.SetActiveView(fIsNew ? vwInstanceNew : vwInstanceExisting);

            // disable editing of sims and anonymous aircraft.
            bool fIsLocked = (!AdminMode && m_ac.IsLocked);
            bool fCanEdit = IsRealAircraft && !IsAnonymous && !fIsLocked;
            rbAvionicsNone.Enabled = rbAvionicsGlass.Enabled = rbAvionicsTAA.Enabled = fCanEdit;

            lblStep1.Visible = lblStep2.Visible = lblStep3.Visible = lblStep4.Visible = fIsNew;

            pnlLockedExplanation.Visible = m_ac.IsLocked;
            ckLocked.Visible = AdminMode && IsRealAircraft;
            ckLocked.Checked = m_ac.IsLocked;

            SelectedInstanceType = m_ac.InstanceType;

            SetUpCountryCode();

            lblReadTail.Visible = !(txtTail.Visible = cmbCountryCode.Enabled = fIsNew);    // Issue #724/80.  Now that we have aircraft mapping, shouldn't need to support editing?

            List<LinkedString> lstAttrib = new List<LinkedString>();
            if (!fIsNew)
            {
                lstAttrib.Add(new LinkedString(rbTrainingDevice.Checked ? rblTrainingDevices.SelectedItem.Text : (rbRealRegistered.Checked ? lblRealRegistered.Text : lblAnonymous.Text)));
                lstAttrib.AddRange(StatsForUser(m_ac));
            }
            SelectMake1.AircraftAttributes = lstAttrib;
            SelectMake1.SelectedModelID = m_ac.ModelID;

            AvionicsUpgrade = m_ac.AvionicsTechnologyUpgrade;

            mfbIl.Key = txtTail.Text = lblReadTail.Text = m_ac.TailNumber;

            SetUpNotes();

            SetUpMaintenance();

            SetUpAlternativeVersions();

            SetUpSchedules(m_ac, rptSchedules, rowClubSchedules, (string sz) => { mfbEditAppt1.DefaultTitle = sz; });

            bool fIsKnown = AdminMode || (!fIsNew && (new UserAircraft(Page.User.Identity.Name)).GetUserAircraftByID(m_ac.AircraftID) != null);

            btnAddAircraft.Text = fIsKnown ? Resources.LocalizedText.EditAircraftUpdateAircraft : Resources.LocalizedText.EditAircraftAddAircraft;

            // Set the edit mode, based on chart above, which breaks down as:
            // Editing automatically enabled IF new aircraft OR real, registered, unlocked, single user.
            // Editing offered (with confirmation) if real, registered, (unlocked or admin), multiple-user
            // Otherwise, no editing
            if (fIsNew || (fCanEdit && IsOnlyUserOfAircraft))
                SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.Edit;
            else if (fCanEdit && !fIsLocked)
                SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.EditWithConfirm;
            else
                SelectMake1.EditMode = Controls_AircraftControls_SelectMake.MakeEditMode.Locked;

            mfbDateOfGlassUpgrade.Date = m_ac.GlassUpgradeDate ?? DateTime.MinValue;
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
                lblTailnumber.Text = m_ac.IsAnonymous ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, m_ac.TailNumber, m_ac.DisplayTailnumber) : m_ac.TailNumber;
        }

        /// <summary>
        /// Shows/hides functionality based on instance type, new/existing, sim, or anonymous.  Does NOT bind any data
        /// </summary>
        protected void AdjustForAircraftType()
        {
            MakeModel mm = MakeModel.GetModel(m_ac.ModelID);

            bool fRealAircraft = IsRealAircraft;
            CountryCodePrefix cc = CountryCodePrefix.BestMatchCountryCode(m_ac.TailNumber);

            bool fIsNew = m_ac.IsNew;
            bool fHasModelSpecified = (m_ac.ModelID > 0);

            bool fIsAnonymous = rbRealAnonymous.Checked;

            rbRealRegistered.Enabled = rbRealAnonymous.Enabled = true;
            switch (mm.AllowedTypes)
            {
                case AllowedAircraftTypes.Any:
                    break;
                case AllowedAircraftTypes.SimOrAnonymous:
                    if (fRealAircraft)
                    {
                        fIsAnonymous = true;
                        m_ac.TailNumber = CountryCodePrefix.AnonymousCountry.Prefix;   // so that the selected instance will do anonymous correctly
                        SelectedInstanceType = AircraftInstanceTypes.RealAircraft;
                    }
                    rbRealRegistered.Enabled = false;
                    break;
                case AllowedAircraftTypes.SimulatorOnly:
                    cc = CountryCodePrefix.SimCountry;
                    if (fRealAircraft)
                    {
                        // migrate to an ATD.
                        m_ac.InstanceType = SelectedInstanceType = AircraftInstanceTypes.CertifiedATD;
                        fRealAircraft = false;
                    }
                    rbRealRegistered.Enabled = rbRealAnonymous.Enabled = false;
                    break;
            }

            mvTailnumber.SetActiveView(fRealAircraft ? vwRealAircraft : vwSimTail);

            // Show glass option if this is not, by model, a glass cockpit AND if it's not anonymous
            bool fRealRegistered = fRealAircraft && !fIsAnonymous;

            // All avionics upgrade options are limited to real, registered aircraft.  Beyond that:
            // - Can upgrade to glass if the model is not already glass or TAA (i.e., = steam)
            // - Can upgrade to TAA if the model is not already TAA-only AND this is an airplane (TAA is airplane only).
            // - So we will hide the overall avionics upgrade panel if no upgrade is possible.
            bool fAvionicsGlassvisible = fRealRegistered && mm.AvionicsTechnology == MakeModel.AvionicsTechnologyType.None;
            bool fTAAVisible = fRealRegistered && mm.AvionicsTechnology != MakeModel.AvionicsTechnologyType.TAA;
            rbAvionicsGlass.Visible = fAvionicsGlassvisible;
            pnlTAA.Visible = fTAAVisible;
            pnlGlassCockpit.Visible = fAvionicsGlassvisible || fTAAVisible;

            // Sanity check
            if ((rbAvionicsGlass.Checked && !rbAvionicsGlass.Visible) || (rbAvionicsTAA.Checked && !rbAvionicsTAA.Visible))
                AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;

            rowCountry.Visible = fRealAircraft && !fIsAnonymous;
            rowMaintenance.Visible = rowCountry.Visible && !fIsNew;

            pnlTrainingDeviceTypes.Visible = fIsNew && !fRealAircraft;

            AdjustForRealOrAnonymous(fRealAircraft, fIsAnonymous, fHasModelSpecified, cc);
        }

        private void AdjustForRealOrAnonymous(bool fRealAircraft, bool fIsAnonymous, bool fHasModelSpecified, CountryCodePrefix cc)
        {
            if (fRealAircraft)
            {
                if (fIsAnonymous)
                {
                    lblAnonTailDisplay.Text = m_ac.TailNumber = txtTail.Text = lblReadTail.Text = (fHasModelSpecified) ? Aircraft.AnonymousTailnumberForModel(m_ac.ModelID) : String.Empty;
                    pnlAnonTail.Visible = (fHasModelSpecified);
                }
                else
                {
                    if (cc.IsSim || cc.IsAnonymous)   // reset tail if switching from sim or anonymous
                        m_ac.TailNumber = cmbCountryCode.SelectedItem.Value;
                    else
                        m_ac.TailNumber = CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(cmbCountryCode.SelectedItem.Text, cmbCountryCode.SelectedValue), txtTail.Text);
                    lblReadTail.Text = txtTail.Text = HttpUtility.HtmlEncode(m_ac.TailNumber);
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

                lblSimTail.Text = lblReadTail.Text = txtTail.Text = m_ac.TailNumber;
            }
        }
        #endregion

        #region Committing Changes
        /// <summary>
        /// Gets the aircraft to be saved, set to the correct version so that we can detect versioning issues.
        /// </summary>
        private Aircraft AircraftToEdit
        {
            get
            {
                Aircraft ac = (AircraftID == Aircraft.idAircraftUnknown) ? new Aircraft() : new Aircraft(AircraftID);  // if editing an existing airplane, init to the existing data (only modify what's in the UI)
                ac.Revision = String.IsNullOrEmpty(hdnAcRev.Value) ? 0 : Convert.ToInt32(hdnAcRev.Value, CultureInfo.InvariantCulture); ;  // make sure, though, that we don't overwrite later revisions.
                return ac;
            }
        }

        protected void SaveChanges()
        {
            lblError.Text = string.Empty;
            string szTailNew = txtTail.Text.ToUpper(CultureInfo.CurrentCulture);
            int AircraftIDOrig = AircraftID;

            Aircraft ac = AircraftToEdit;

            // Rules for how we want to handle a tailnumber edit
            // a) New aircraft - no issue (Commit will match to an existing aircraft as needed)
            // b) Existing aircraft with no other users: go ahead and change the aircraft.
            // c) Existing aircraft with other users: Treat as a new aircraft with the specified model; after commit, do any replacement as necessary
            // In other words, migrate the user to another aircraft if we are editing an existing aircraft, changing the tail, and there are other users.
            bool fChangedTail = (String.Compare(Aircraft.NormalizeTail(ac.TailNumber), Aircraft.NormalizeTail(szTailNew), StringComparison.CurrentCultureIgnoreCase) != 0);
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
            ac.InstanceTypeID = (int)this.SelectedInstanceType;
            ac.AvionicsTechnologyUpgrade = AvionicsUpgrade;
            ac.GlassUpgradeDate = (ac.AvionicsTechnologyUpgrade != MakeModel.AvionicsTechnologyType.None && mfbDateOfGlassUpgrade.Date.HasValue()) ? mfbDateOfGlassUpgrade.Date : (DateTime?)null;
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
            catch (Exception ex) when (!(ex is OutOfMemoryException))
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
                    AircraftUpdated?.Invoke(sender, e);
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
                CountryCodePrefix ccp = CountryCodePrefix.BestMatchCountryCode(txtTail.Text);

                // sync the countrycode to the tail.  Note that we set hdnLastCountry.Value here as well to prevent a potential loop.
                if (!ccp.IsSim && !ccp.IsAnonymous)
                    cmbCountryCode.SelectedValue = hdnLastCountry.Value = ccp.HyphenatedPrefix;
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
            txtTail.Text = HttpUtility.HtmlEncode(CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(cmbCountryCode.SelectedItem.Text, cmbCountryCode.SelectedValue), txtTail.Text));
        }

        protected void lnkPopulateAircraft_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtTail.Text) && int.TryParse(hdnSelectedAircraftID.Value, out int aircraftID))
                RedirectForId(aircraftID);
        }
        #endregion

        #region Instance Type
        protected void UpdateInstanceType(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtTail.Text) && !rbRealAnonymous.Checked && m_ac.IsNew)
                SetUpCountryCode();

            m_ac.InstanceType = SelectedInstanceType;
            pnlTrainingDeviceTypes.Visible = m_ac.InstanceType != AircraftInstanceTypes.RealAircraft;
            UpdateMask();
            AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;  // reset this since model changed.

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
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareCurrentCultureIgnoreCase("_switchMigrate") == 0)
                SwitchToAlternateVersion(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture), true);
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("_switchNoMigrate") == 0)
                SwitchToAlternateVersion(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture), false);
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("_merge") == 0)
            {
                Aircraft acClone = new Aircraft(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
                MergeClone(m_ac, acClone);
            }
        }

        protected void gvAlternativeVersions_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
                RefreshImages(e.Row.FindControl("mfbHoverThumb"));
        }
        #endregion

        #region Schedules
        protected void rptSchedules_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            BindSchedules(e);
        }
        #endregion

        protected void rbGlassUpgrade_CheckedChanged(object sender, EventArgs e)
        {
            pnlGlassUpgradeDate.Visible = AvionicsUpgrade != MakeModel.AvionicsTechnologyType.None;
        }

        protected void SelectMake1_ModelChanged(object sender, MakeSelectedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            m_ac.ModelID = e.SelectedModel;
            AvionicsUpgrade = MakeModel.AvionicsTechnologyType.None;  // reset this since model changed.
            AdjustForAircraftType();
        }

        protected void SelectMake1_MajorChangeRequested(object sender, EventArgs e)
        {
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Public/ContactMe.aspx?subj={0}", System.Web.HttpUtility.UrlEncode(String.Format(CultureInfo.CurrentCulture, "Incorrect definition for aircraft {0}", m_ac.TailNumber))));
        }
    }
}