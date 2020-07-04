using MyFlightbook;
using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_EditProfile : System.Web.UI.Page
{
    private Profile m_pf = null;

    #region Page Lifecycle, setup
    private static tabID HandleUnknownSidebar(string szPref)
    {
        // have a less fragile way of linking.
        switch (szPref)
        {
            default:
            case "name":
                return tabID.pftName;
            case "pass":
                return tabID.pftPass;
            case "qa":
                return tabID.pftQA;
            case "pref":
            case "email":
                return tabID.pftPrefs;
            case "pilotinfo":
                return tabID.pftPilotInfo;
            case "instruction":
                return tabID.instEndorsements;
        }
    }

    private void SetSidebarTab(tabID sidebarTab, string szPref)
    {
        switch (sidebarTab)
        {
            case tabID.pftAccount:
            case tabID.pftName:
            case tabID.pftPass:
            case tabID.pftQA:
                mvProfile.SetActiveView(vwAccount);
                this.Master.SelectedTab = tabID.pftAccount;
                break;
            case tabID.pftPrefs:
                mvProfile.SetActiveView(vwPrefs);
                break;
            case tabID.pftPilotInfo:
                mvProfile.SetActiveView(vwPilotInfo);
                break;
            // Backwards compatibility: we moved instructors/students/endorsements/signing to InstStudent, but may still be requested here.
            // Redirect for any of those
            case tabID.instEndorsements:
            case tabID.instSignFlights:
            case tabID.instStudents:
            case tabID.instInstructors:
                Response.Redirect(Request.Url.PathAndQuery.Replace("EditProfile.aspx", String.Format(CultureInfo.InvariantCulture, "Training.aspx/{0}", szPref)).Replace(String.Format(CultureInfo.InvariantCulture, "pref={0}", szPref), string.Empty).Replace("?&", "?"));
                break;
            case tabID.pftDonate:
                mvProfile.SetActiveView(vwDonate);
                break;
        }
    }

    private tabID SetUpSidebar()
    {
        tabID sidebarTab = tabID.tabUnknown;

        string szPrefPath = String.IsNullOrWhiteSpace(Request.PathInfo) ? string.Empty : Request.PathInfo.Substring(1);
        string[] rgPrefPath = szPrefPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        string szPref = util.GetStringParam(Request, "pref");

        if (rgPrefPath.Length > 0 && !String.IsNullOrEmpty(rgPrefPath[0]) && Enum.TryParse<tabID>(rgPrefPath[0], out tabID tid))
            sidebarTab = tid;

        if (sidebarTab == tabID.tabUnknown && !String.IsNullOrEmpty(szPref))
        {
            // Backwards compatibility - redirect if using the szPref method 
            if (Enum.TryParse<tabID>(szPref, out tabID tid2))
            {
                sidebarTab = tid2;

                // Redirect now using PathInfo scheme
                string szNew = Request.Url.PathAndQuery.Replace(".aspx", String.Format(CultureInfo.InvariantCulture, ".aspx/{0}", szPref)).Replace(String.Format(CultureInfo.InvariantCulture, "pref={0}", szPref), string.Empty).Replace("?&", "?");
                Response.Redirect(szNew, true);
                return sidebarTab;
            }
        }

        if (sidebarTab == tabID.tabUnknown)
            sidebarTab = HandleUnknownSidebar(szPref);

        this.Master.SelectedTab = sidebarTab;

        SetSidebarTab(sidebarTab, szPref);

        return sidebarTab;
    }

    private void InitAccount(tabID sidebarTab)
    {
        lblMemberSince.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberSince, m_pf.CreationDate);
        lblLastLogin.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastLogon, m_pf.LastLogon);
        lblLastActivity.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastActivity, m_pf.LastActivity);
        itemLastActivity.Visible = m_pf.LastActivity.Date.CompareTo(m_pf.LastLogon.Date) != 0;
        lblPasswordStatus.Text = m_pf.LastPasswordChange.CompareTo(m_pf.CreationDate) == 0 ? Resources.LocalizedText.MemberOriginalPassword : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastPassword, m_pf.LastPasswordChange);
        txtFirst.Text = m_pf.FirstName;
        txtLast.Text = m_pf.LastName;
        lblStaticEmail.Text = txtEmail2.Text = txtEmail.Text = m_pf.Email;
        wmeGreeting.WatermarkText = String.IsNullOrEmpty(m_pf.FirstName) ? Resources.Profile.accountPreferredGreetingWatermark : m_pf.FirstName;
        string szPreferredGreeting = m_pf.PreferredGreeting.Trim();
        if (szPreferredGreeting.CompareCurrentCultureIgnoreCase(m_pf.UserFirstName.Trim()) == 0)
        {
            txtPreferredGreeting.Text = string.Empty;
            lblFullName.Text = m_pf.UserFullName;
        }
        else
        {
            txtPreferredGreeting.Text = szPreferredGreeting;
            lblFullName.Text = String.Format(CultureInfo.CurrentCulture, "{0} ({1})", m_pf.UserFullName, szPreferredGreeting);
        }
        lblQuestion.Text = m_pf.SecurityQuestion;
        lblAddress.Text = txtAddress.Text = m_pf.Address;
        accordianAccount.SelectedIndex = (sidebarTab == tabID.pftQA) ? 2 : (sidebarTab == tabID.pftPass ? 1 : 0);
    }

    private void InitFeaturePrefs()
    {
        ckShowTimes.Checked = m_pf.DisplayTimesByDefault;
        ckSIC.Checked = m_pf.TracksSecondInCommandTime;
        ckTrackCFITime.Checked = m_pf.IsInstructor;
        ckUseArmyCurrency.Checked = m_pf.UsesArmyCurrency;
        ckUse117DutyTime.Checked = m_pf.UsesFAR117DutyTime;
        rbl117Rules.SelectedIndex = m_pf.UsesFAR117DutyTimeAllFlights ? 1 : 0;
        ckUse135DutyTime.Checked = m_pf.UsesFAR135DutyTime;
        ckUse13529xCurrency.Checked = m_pf.UsesFAR13529xCurrency;
        ckUse13526xCurrency.Checked = m_pf.UsesFAR13526xCurrency;
        ckUse61217Currency.Checked = m_pf.UsesFAR61217Currency;
        ckLAPLCurrency.Checked = m_pf.UsesLAPLCurrency;
        ck6157c4Pref.Checked = m_pf.UsesLooseIFRCurrency;
        pnlLoose6157.Visible = DateTime.Now.CompareTo(ExaminerFlightRow.Nov2018Cutover) < 0;
        ckCanadianCurrency.Checked = m_pf.UseCanadianCurrencyRules;
        rblTotalsOptions.SelectedValue = m_pf.TotalsGroupingMode.ToString();
        ckIncludeModelFeatureTotals.Checked = !m_pf.SuppressModelFeatureTotals;
        rblCurrencyPref.SelectedIndex = (m_pf.UsesPerModelCurrency ? 1 : 0);
        rblTimeEntryPreference.SelectedIndex = (m_pf.UsesHHMM ? 1 : 0);
        rblDateEntryPreferences.SelectedIndex = (m_pf.UsesUTCDateOfFlight ? 1 : 0);
        prefTimeZone.SelectedTimeZone = m_pf.PreferredTimeZone;
        foreach (CurrencyExpiration.Expiration exp in Enum.GetValues(typeof(CurrencyExpiration.Expiration)))
        {
            ListItem li = new ListItem(CurrencyExpiration.ExpirationLabel(exp), exp.ToString()) { Selected = m_pf.CurrencyExpiration == exp };
            cmbExpiredCurrency.Items.Add(li);
        }
    }

    private void InitSocialNetworking()
    {
        // Sharing
        lnkMyFlights.Text = HttpUtility.HtmlEncode(m_pf.PublicFlightsURL(Request.Url.Host).AbsoluteUri);
        ClientScript.RegisterClientScriptInclude("copytoClip", ResolveClientUrl("~/public/Scripts/CopyClipboard.js"));
        imgCopyMyFlights.OnClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:copyClipboard(null, '{0}', true, '{1}');return false;", lnkMyFlights.ClientID, lblMyFlightsCopied.ClientID);
    }

    private void InitDeadlinesAndCurrencies()
    {
        // Custom Currencies.
        mfbCustomCurrencyList1.RefreshCustomCurrencyList();

        mfbDeadlines1.UserName = Page.User.Identity.Name;
        mfbDeadlines1.Refresh();
    }

    private void InitPrefs()
    {
        // features
        InitFeaturePrefs();

        // Property blacklist
        UpdateBlacklist();

        // Social networking.
        InitSocialNetworking();

        // Set up status of cloud providers and ability to pick a default.
        mfbCloudStorage.InitCloudProviders();
        mfbCloudStorage.HandleOAuthRedirect();

        acpoAuthApps.Visible = oAuthAuthorizationManager.Refresh();

        string szPane = Request["pane"];
        if (!String.IsNullOrEmpty(szPane))
        {
            acpLocalPrefs.Visible = String.IsNullOrEmpty(Request["nolocalprefs"]);
            int paneIndexOffset = acpLocalPrefs.Visible ? 0 : -1;

            AjaxControlToolkit.AccordionPane acpTarget = null;
            switch (szPane)
            {
                case "backup":
                    acpTarget = acpBackup;
                    break;
                case "social":
                    acpTarget = acpSocialNetworking;
                    break;
                case "deadlines":
                    acpTarget = acpCustomDeadlines;
                    break;
                case "custcurrency":
                    acpTarget = acpCustomCurrencies;
                    break;
                case "cloudahoy":
                    acpTarget = acpCloudAhoy;
                    break;
                case "props":
                    acpTarget = acpProperties;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < accordianPrefs.Panes.Count; i++)
                if (accordianPrefs.Panes[i] == acpTarget)
                {
                    accordianPrefs.SelectedIndex = i + paneIndexOffset;
                    break;
                }
        }

        InitDeadlinesAndCurrencies();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        m_pf = MyFlightbook.Profile.GetUser(User.Identity.Name);

        if (!Request.IsSecureConnection && !Request.IsLocal)
            Response.Redirect(Request.Url.AbsoluteUri.Replace("http://", "https://"));

        lblName.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileHeader, m_pf.UserFullName);

        this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

        // sidebar doesn't store it's state, so just set the currenttab each time.
        tabID sidebarTab = SetUpSidebar();

        Page.ClientScript.RegisterClientScriptInclude("ListDrag", ResolveClientUrl("~/Public/Scripts/listdrag.js?v=5"));

        if (!IsPostBack)
        {
            // Set up per-section information
            switch (sidebarTab)
            {
                case tabID.pftAccount:
                case tabID.pftName:
                case tabID.pftPass:
                case tabID.pftQA:
                    InitAccount(sidebarTab);
                    break;

                case tabID.pftPilotInfo:
                    mfbPilotInfo.InitPilotInfo();
                    break;

                case tabID.pftPrefs:
                    InitPrefs();
                    break;

                case tabID.pftDonate:
                    mfbDonate.InitDonations();
                    break;
            }
        }
    }
    #endregion

    #region Name and Email
    protected Boolean FCommitName()
    {
        if (!IsValid)
            return false;

        try
        {
            m_pf.ChangeNameAndEmail(txtFirst.Text, txtLast.Text, txtEmail.Text, txtAddress.Text);
            m_pf.FCommit();
            m_pf.PreferredGreeting = txtPreferredGreeting.Text.Trim().CompareCurrentCultureIgnoreCase(m_pf.UserFirstName) == 0 ? string.Empty : txtPreferredGreeting.Text.Trim();
            Response.Redirect("~/Member/EditProfile.aspx/pftAccount");
        }
        catch (MyFlightbookException ex)
        {
            lblNameUpdated.Visible = true;
            lblNameUpdated.Text = ex.Message;
            lblNameUpdated.CssClass = "error";
            return false;
        }

        return true;
    }

    protected void btnUpdatename_Click(object sender, EventArgs e)
    {
        if (FCommitName())
            lblNameUpdated.Visible = true;
    }

    public void VerifyEmailAvailable(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));
        Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
        args.IsValid = true;
        if (String.Compare(txtEmail.Text, pf.Email, StringComparison.OrdinalIgnoreCase) != 0)
        {
            // see if it's available
            pf.Email = txtEmail.Text;
            args.IsValid = pf.IsValid();
        }
    }

    protected void tfaEmail_TFACodeFailed(object sender, EventArgs e)
    {
        lblInvalidTFAEmail.Visible = true;
    }

    protected void tfaEmail_TFACodeVerified(object sender, EventArgs e)
    {
        mvNameEmail.SetActiveView(vwChangeNameEmail);
    }

    protected void btnEditNameEmail_Click(object sender, EventArgs e)
    {
        mvNameEmail.SetActiveView(m_pf.PreferenceExists(MFBConstants.keyTFASettings) ? vwVerifyTFAEmail : vwChangeNameEmail);
        tfaEmail.AuthCode = m_pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
    }
    #endregion

    #region Password
    protected Boolean FCommitPass()
    {
        try
        {
            if (NewPassword.Text != ConfirmNewPassword.Text) // should never happen - validation should have caught this.
                throw new MyFlightbookException(Resources.Profile.errPasswordsDontMatch);
            m_pf.ChangePassword(CurrentPassword.Text, NewPassword.Text);
        }
        catch (MyFlightbookException ex)
        {
            lblPassChanged.Visible = true;
            lblPassChanged.Text = ex.Message;
            lblPassChanged.CssClass = "error";
            return false;
        }

        return true;
    }

    protected void btnUpdatePass_Click(object sender, EventArgs e)
    {
        if (FCommitPass())
        {
            lblPassChanged.Visible = true;
            mvChangePass.SetActiveView(vwStaticPass);
        }
    }

    public void ValidateCurrentPassword(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));
        if (CurrentPassword.Text.Length == 0 && NewPassword.Text.Length > 0)
            args.IsValid = false;
    }

    protected void btnChangePass_Click(object sender, EventArgs e)
    {
        mvChangePass.SetActiveView(m_pf.PreferenceExists(MFBConstants.keyTFASettings) ? vwVerifyTFAPass : vwChangePass);
        tfaChangePass.AuthCode = m_pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
    }

    protected void tfaChangePass_TFACodeFailed(object sender, EventArgs e)
    {
        lblTFACheckPass.Visible = true;
    }

    protected void tfaChangePass_TFACodeVerified(object sender, EventArgs e)
    {
        mvChangePass.SetActiveView(vwChangePass);
    }
    #endregion

    #region Q and A
    protected void btnChangeQA_Click(object sender, EventArgs e)
    {
        try
        {
            m_pf.ChangeQAndA(txtPassQA.Text, txtQuestion.Text, txtAnswer.Text);
            lblQAChangeSuccess.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblQAChangeSuccess.Visible = true;
            lblQAChangeSuccess.Text = ex.Message;
            lblQAChangeSuccess.CssClass = "error";
        }
    }

    protected void btnChangeQA_Click1(object sender, EventArgs e)
    {
        mvQA.SetActiveView(m_pf.PreferenceExists(MFBConstants.keyTFASettings) ? vwVerifyTFAQA : vwChangeQA);
        tfaChangeQA.AuthCode = m_pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
    }

    protected void tfaChangeQA_TFACodeFailed(object sender, EventArgs e)
    {
        lblTFAErrQA.Visible = true;
    }

    protected void tfaChangeQA_TFACodeVerified(object sender, EventArgs e)
    {
        mvQA.SetActiveView(vwChangeQA);
    }
    #endregion

    #region Account closure and bulk delete
    protected void btnDeleteFlights_Click(object sender, EventArgs e)
    {
        try
        {
            ProfileAdmin.DeleteFlightsForUser(Page.User.Identity.Name);
            lblDeleteFlightsCompleted.Visible = true;
        }
        catch (MyFlightbookException ex) { lblDeleteErr.Text = ex.Message; }
    }

    protected void btnCloseAccount_Click(object sender, EventArgs e)
    {
        try
        {
            ProfileAdmin.DeleteEntireUser(Page.User.Identity.Name);
            Response.Redirect("~");
        }
        catch (MyFlightbookException ex) { lblDeleteErr.Text = ex.Message; }
    }

    protected void btnDeleteUnusedAircraft_Click(object sender, EventArgs e)
    {
        int i = ProfileAdmin.DeleteUnusedAircraftForUser(Page.User.Identity.Name);
        lblDeleteErr.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileBulkDeleteAircraftDeleted, i);
        lblDeleteErr.CssClass = "success";
    }
    #endregion

    #region Preferences
    protected void btnUpdateLocalPrefs_Click(object sender, EventArgs e)
    {
        m_pf.DisplayTimesByDefault = ckShowTimes.Checked;
        m_pf.TracksSecondInCommandTime = ckSIC.Checked;
        m_pf.IsInstructor = ckTrackCFITime.Checked;
        m_pf.UsesHHMM = (rblTimeEntryPreference.SelectedIndex > 0);
        m_pf.UsesUTCDateOfFlight = (rblDateEntryPreferences.SelectedIndex > 0);
        m_pf.PreferredTimeZoneID = prefTimeZone.SelectedTimeZoneId;

        try
        {
            m_pf.FCommit();
            lblLocalPrefsUpdated.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblLocalPrefsUpdated.Visible = true;
            lblLocalPrefsUpdated.Text = ex.Message;
            lblLocalPrefsUpdated.CssClass = "error";
        }
    }

    protected void btnUpdateCurrencyPrefs_Click(object sender, EventArgs e)
    {
        m_pf.UsesArmyCurrency = ckUseArmyCurrency.Checked;
        m_pf.UsesFAR117DutyTime = ckUse117DutyTime.Checked;
        m_pf.UsesFAR117DutyTimeAllFlights = rbl117Rules.SelectedIndex != 0;
        m_pf.UsesFAR135DutyTime = ckUse135DutyTime.Checked;
        m_pf.UsesFAR13529xCurrency = ckUse13529xCurrency.Checked;
        m_pf.UsesFAR13526xCurrency = ckUse13526xCurrency.Checked;
        m_pf.UsesFAR61217Currency = ckUse61217Currency.Checked;
        m_pf.UsesLooseIFRCurrency = ck6157c4Pref.Checked;
        m_pf.UseCanadianCurrencyRules = ckCanadianCurrency.Checked;
        m_pf.UsesLAPLCurrency = ckLAPLCurrency.Checked;
        m_pf.UsesPerModelCurrency = (rblCurrencyPref.SelectedIndex > 0);
        m_pf.TotalsGroupingMode = (TotalsGrouping) Enum.Parse(typeof(TotalsGrouping), rblTotalsOptions.SelectedValue);
        m_pf.SuppressModelFeatureTotals = !ckIncludeModelFeatureTotals.Checked;
        m_pf.CurrencyExpiration = (CurrencyExpiration.Expiration)Enum.Parse(typeof(CurrencyExpiration.Expiration), cmbExpiredCurrency.SelectedValue);

        try
        {
            m_pf.FCommit();
            lblCurrencyPrefsUpdated.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblCurrencyPrefsUpdated.Visible = true;
            lblCurrencyPrefsUpdated.Text = ex.Message;
            lblCurrencyPrefsUpdated.CssClass = "error";
        }
    }

    #region Property Blacklist
    protected void UpdateBlacklist(bool fForceDB = false)
    {
        rptUsedProps.DataSource = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name, fForceDB)).FindAll(cpt => cpt.IsFavorite);
        rptUsedProps.DataBind();
        List<CustomPropertyType> lstBlacklist = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(m_pf.BlacklistedProperties));
        lstBlacklist.Sort((cpt1, cpt2) => { return cpt1.SortKey.CompareCurrentCultureIgnoreCase(cpt2.SortKey); });
        rptBlackList.DataSource = lstBlacklist;
        rptBlackList.DataBind();
    }

    protected void btnWhiteList_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
        {
            try
            {
                int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                m_pf.BlacklistedProperties.RemoveAll(id => id == idPropType);
                m_pf.FCommit();
                UpdateBlacklist(true);
            }
            catch
            {
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for whitelist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
            }
        }
    }

    protected void btnBlackList_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
        {
            try
            {
                int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                if (!m_pf.BlacklistedProperties.Contains(idPropType))
                {
                    m_pf.BlacklistedProperties.Add(idPropType);
                    m_pf.FCommit();
                    UpdateBlacklist(true);
                }
            }
            catch
            {
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for whitelist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
            }
    }
}
    #endregion

    #endregion // Preferences
}
