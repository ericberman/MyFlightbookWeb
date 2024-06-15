using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Image;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class Member_EditProfileBase : Page
    {
        protected Profile m_pf;

        protected static tabID HandleUnknownSidebar(string szPref)
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

        #region Sharing
        protected void lnkAuthGPhotos_Click(object sender, EventArgs e)
        {
            new GooglePhoto().Authorize(Request, Request.Url.AbsolutePath, GooglePhoto.szParamGPhotoAuth);
        }

        protected bool HasGooglePhotos
        {
            get { return m_pf.PreferenceExists(GooglePhoto.PrefKeyAuthToken); }
        }
        #endregion

        #region Image Utilities
        protected byte[] ScaledHeadshotFromStream(System.IO.Stream s)
        {
            return (s == null) ? Array.Empty<byte>() : MFBImageInfo.ScaledImage(s, 90, 90);
        }
        #endregion

        protected void SetUpPageScript()
        {
            Page.ClientScript.RegisterClientScriptInclude("ListDrag", ResolveClientUrl("~/Public/Scripts/listdrag.js?v=6"));
            Page.ClientScript.RegisterClientScriptInclude("copytoClip", ResolveClientUrl("~/public/Scripts/CopyClipboard.js"));
        }

        private bool fHasSetUpFields = false;

        protected void SetUpCoreFields(HiddenField hdn, Control divCore, Control divUnused)
        {
            if (divUnused == null)
                throw new ArgumentNullException(nameof(divUnused));
            if (divCore == null)
                throw new ArgumentNullException(nameof(divCore));
            if (hdn == null)
                throw new ArgumentNullException(nameof(hdn));

            if (fHasSetUpFields)
                throw new InvalidOperationException("Setting up core fields a 2nd time");

            fHasSetUpFields = true;

            // set up permutations.  Initially from preference, but then store locally in the hdn field.
            IEnumerable<int> rgPerms = (IsPostBack && !String.IsNullOrWhiteSpace(hdn.Value) && hdn.Value.StartsWith("[", StringComparison.InvariantCulture)) ? JsonConvert.DeserializeObject<int[]>(hdn.Value) : m_pf.GetPreferenceForKey<IEnumerable<int>>(MFBConstants.keyCoreFieldsPermutation);
            if (rgPerms != null && rgPerms.Any())
            {
                IEnumerable<Control> unused = divCore.PermuteChildren<Label>(rgPerms);
                foreach (Control c in unused)
                    divUnused.Controls.Add(c);

                hdn.Value = JsonConvert.SerializeObject(rgPerms);
            }
            else
            {
                // No permutations set - specify the default one.
                List<int> lst = new List<int>();
                int i = 0;
                foreach (Control c in divCore.Controls)
                    if (c is Label)
                        lst.Add(i++);
                hdn.Value = JsonConvert.SerializeObject(lst);
            }
        }

        protected void VerifyEmail(string szEmail)
        {
            m_pf.SendVerificationEmail(szEmail, "~/Member/EditProfile.aspx/pftAccount".ToAbsoluteURL(Request).ToString() + "?ve={0}");
        }

        protected void AddCurrencyExpirations(DropDownList cmb)
        {
            if (cmb == null)
                throw new ArgumentNullException(nameof(cmb));

            foreach (CurrencyExpiration.Expiration exp in Enum.GetValues(typeof(CurrencyExpiration.Expiration)))
            {
                ListItem li = new ListItem(CurrencyExpiration.ExpirationLabel(exp), exp.ToString()) { Selected = m_pf.CurrencyExpiration == exp };
                cmb.Items.Add(li);
            }
        }
    }

    public partial class Member_EditProfile : Member_EditProfileBase
    {
        #region Page Lifecycle, setup

        private void SetSidebarTab(tabID sidebarTab, string szPref)
        {
            switch (sidebarTab)
            {
                case tabID.pftAccount:
                case tabID.pftName:
                case tabID.pftPass:
                case tabID.pftQA:
                case tabID.pft2fa:
                case tabID.pftBigRedButtons:
                    mvProfile.SetActiveView(vwAccount);
                    Master.SelectedTab = tabID.pftAccount;
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
                    Response.Redirect("~/mvc/Training/Endorsements");
                    break;
                case tabID.instSignFlights:
                    Response.Redirect("~/mvc/Training/RequestSigs");
                    break;
                case tabID.instStudents:
                    Response.Redirect("~/mvc/Training/Students");
                    break;
                case tabID.instInstructors:
                    Response.Redirect("~/mvc/Training/Instructors");
                    break;
                case tabID.pftDonate:
                    Response.Redirect("~/mvc/donate" + Request.Url.Query);
                    break;
            }
        }

        private tabID SetUpSidebar()
        {
            tabID sidebarTab = tabID.tabUnknown;

            string szPrefPath = String.IsNullOrWhiteSpace(Request.PathInfo) ? string.Empty : Request.PathInfo.Substring(1);
            string[] rgPrefPath = szPrefPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string szPref = util.GetStringParam(Request, "pref");

            if (rgPrefPath.Length > 0 && !String.IsNullOrEmpty(rgPrefPath[0]) && Enum.TryParse(rgPrefPath[0], out tabID tid))
                sidebarTab = tid;

            if (sidebarTab == tabID.tabUnknown && !String.IsNullOrEmpty(szPref))
            {
                // Backwards compatibility - redirect if using the szPref method 
                if (Enum.TryParse(szPref, out tabID tid2))
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
            txtFirst.Attributes["oninput"] = "javascript:updateGreeting(this);";
            txtLast.Text = m_pf.LastName;
            lblStaticEmail.Text = HttpUtility.HtmlEncode(txtEmail2.Text = txtEmail.Text = m_pf.Email);
            txtPreferredGreeting.SetPlaceholder(String.IsNullOrEmpty(m_pf.FirstName) ? Resources.Profile.accountPreferredGreetingWatermark : m_pf.FirstName);
            string szPreferredGreeting = m_pf.PreferredGreeting.Trim();
            if (szPreferredGreeting.CompareCurrentCultureIgnoreCase(m_pf.UserFirstName.Trim()) == 0)
            {
                txtPreferredGreeting.Text = string.Empty;
                lblFullName.Text = HttpUtility.HtmlEncode(m_pf.UserFullName);
            }
            else
            {
                txtPreferredGreeting.Text = szPreferredGreeting;
                lblFullName.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, "{0} ({1})", m_pf.UserFullName, szPreferredGreeting));
            }
            lblQuestion.Text = HttpUtility.HtmlEncode(m_pf.SecurityQuestion);
            lblAddress.Text = HttpUtility.HtmlEncode(txtAddress.Text = m_pf.Address);
            dateDOB.Date = m_pf.DateOfBirth ?? DateTime.MinValue;
            switch (sidebarTab)
            {
                default:
                    accordianAccount.SelectedIndex = 0;
                    break;
                case tabID.pftPass:
                    accordianAccount.SelectedIndex = 1;
                    break;
                case tabID.pftQA:
                    accordianAccount.SelectedIndex = 2;
                    break;
                case tabID.pft2fa:
                    accordianAccount.SelectedIndex = 3;
                    break;
                case tabID.pftBigRedButtons:
                    accordianAccount.SelectedIndex = 4;
                    break;
            }

            string szEmailVerify = util.GetStringParam(Request, "ve");
            if (!String.IsNullOrEmpty(szEmailVerify))
            {
                if (m_pf.VerifyEmail(szEmailVerify, out string verifiedEmail, out string szerr))
                {
                    m_pf.AddVerifiedEmail(verifiedEmail);
                    lblVerifyResult.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.Profile.accountVerifyValidated, verifiedEmail));
                    lblVerifyResult.CssClass = "success";
                }
                else
                {
                    lblVerifyResult.Text = szerr;
                    lblVerifyResult.CssClass = "error";
                }
            }
            lblVerifyPrimaryEmail.Visible = !(lblPrimaryVerified.Visible = m_pf.IsVerifiedEmail(m_pf.Email));
            rptAlternateEmailsRO.DataSource = gvAlternateEmails.DataSource = m_pf.AlternateEmailsForUser();
            rptAlternateEmailsRO.DataBind();
            gvAlternateEmails.DataBind();
            pnlAlternateEmails.Visible = pnlExistingAlternateEmails.Visible = rptAlternateEmailsRO.Items.Count > 0;

            fuHdSht.Attributes["onchange"] = "javascript:hdshtUpdated();";
            SetHeadShot();
            txtCell.Text = m_pf.MobilePhone;
        }

        private void InitFeaturePrefs()
        {
            ckShowTimes.Checked = m_pf.DisplayTimesByDefault;
            ckSIC.Checked = m_pf.TracksSecondInCommandTime;
            ckTrackCFITime.Checked = m_pf.IsInstructor;
            ckUseArmyCurrency.Checked = m_pf.UsesArmyCurrency;
            ckUse117DutyTime.Checked = m_pf.UsesFAR117DutyTime;
            rb117AllFlights.Checked = m_pf.UsesFAR117DutyTimeAllFlights;
            rb117OnlyDutyTimeFlights.Checked = !m_pf.UsesFAR117DutyTimeAllFlights;
            ckUse135DutyTime.Checked = m_pf.UsesFAR135DutyTime;
            ckUse1252xxCurrency.Checked = m_pf.UsesFAR1252xxCurrency;
            ckUse13529xCurrency.Checked = m_pf.UsesFAR13529xCurrency;
            ckUse13526xCurrency.Checked = m_pf.UsesFAR13526xCurrency;
            ckUse61217Currency.Checked = m_pf.UsesFAR61217Currency;

            CurrencyJurisdiction cj = m_pf.CurrencyJurisdiction;
            rbAustraliaRules.Checked = cj == CurrencyJurisdiction.Australia;
            rbCanadianRules.Checked = cj == CurrencyJurisdiction.Canada;
            rbEASARules.Checked = cj == CurrencyJurisdiction.EASA;
            rbFAARules.Checked = cj == CurrencyJurisdiction.FAA;
            ckAllowNightTouchAndGo.Checked = m_pf.AllowNightTouchAndGoes;
            ckDayLandingsForDayCurrency.Checked = m_pf.OnlyDayLandingsForDayCurrency;
            switch (m_pf.TotalsGroupingMode)
            {
                case TotalsGrouping.Model:
                    rbTotalsModeModel.Checked = true;
                    break;
                case TotalsGrouping.CatClass:
                    rbTotalsModeCatClass.Checked = true;
                    break;
                case TotalsGrouping.Family:
                    rbTotalsModeICAO.Checked = true;
                    break;
            }
            ckIncludeModelFeatureTotals.Checked = !m_pf.SuppressModelFeatureTotals;
            rbCurrencyModeModel.Checked = m_pf.UsesPerModelCurrency;
            if (m_pf.UsesHHMM)
                rbDecimalHHMM.Checked = true;
            else
            {
                DecimalFormat df = m_pf.PreferenceExists(MFBConstants.keyDecimalSettings) ? m_pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings) : DecimalFormat.Adaptive;
                if (df == DecimalFormat.Adaptive)
                    rbDecimalAdaptive.Checked = true;
                else if (df == DecimalFormat.OneDecimal)
                    rbDecimal1.Checked = true;
                else
                    rbDecimal2.Checked = true;
            }
            if (m_pf.MathRoundingUnit == 60)
                rbPrecisionMinutes.Checked = true;
            else
                rbPrecisionHundredths.Checked = true;
            if (m_pf.UsesUTCDateOfFlight)
                rbDateUTC.Checked = true;
            else
                rbDateLocal.Checked = true;

            prefTimeZone.SelectedTimeZone = m_pf.PreferredTimeZone;
            AddCurrencyExpirations(cmbExpiredCurrency);
            cmbAircraftMaintWindow.SelectedValue = m_pf.GetPreferenceForKey(MFBConstants.keyWindowAircraftMaintenance, MFBConstants.DefaultMaintenanceWindow).ToString(CultureInfo.InvariantCulture);
            ckPreserveOriginal.Checked = m_pf.PreferenceExists(MFBConstants.keyTrackOriginal);
        }

        private void InitSocialNetworking()
        {
            // Sharing
            lnkMyFlights.Text = HttpUtility.HtmlEncode(m_pf.PublicFlightsURL(Request.Url.Host).AbsoluteUri);
            imgCopyMyFlights.OnClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:copyClipboard(null, '{0}', true, '{1}');return false;", lnkMyFlights.ClientID, lblMyFlightsCopied.ClientID);

            lblGPhotosDesc.Text = Branding.ReBrand(Resources.LocalizedText.PrefSharingGooglePhotosDesc);
            lnkAuthGPhotos.Text = Branding.ReBrand(Resources.LocalizedText.PrefSharingGooglePhotosAuthorize);
            lblGPhotosEnabled.Text = Branding.ReBrand(Resources.LocalizedText.PrefSharingGooglePhotosEnabled);
            lnkDeAuthGPhotos.Text = Branding.ReBrand(Resources.LocalizedText.PrefSharingGooglePhotosDisable);

            mvGPhotos.SetActiveView(HasGooglePhotos ? vwGPhotosEnabled : vwGPhotosDisabled);
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
                    case "debrief":
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
            m_pf = Profile.GetUser(User.Identity.Name);

            lblName.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileHeader, HttpUtility.HtmlEncode(m_pf.UserFullName));

            this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

            // sidebar doesn't store it's state, so just set the currenttab each time.
            tabID sidebarTab = SetUpSidebar();

            SetUpPageScript();

            if (!IsPostBack)
            {
                // Set up per-section information
                switch (sidebarTab)
                {
                    case tabID.pftAccount:
                    case tabID.pftName:
                    case tabID.pftPass:
                    case tabID.pftQA:
                    case tabID.pft2fa:
                    case tabID.pftBigRedButtons:
                        InitAccount(sidebarTab);
                        break;

                    case tabID.pftPilotInfo:
                        mfbPilotInfo.InitPilotInfo();
                        break;

                    case tabID.pftPrefs:
                        InitPrefs();
                        break;

                    case tabID.pftDonate:
                        Response.Redirect("~/mvc/donate" + Request.Url.Query);
                        break;
                }
            }

            // reset permuted fields every time.
            if (sidebarTab == tabID.pftPrefs)
                SetUpCoreFields(hdnPermutation, divCoreFields, divUnusedFields);
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
                m_pf.DateOfBirth = dateDOB.Date;
                m_pf.FCommit();
                m_pf.PreferredGreeting = txtPreferredGreeting.Text.Trim().CompareCurrentCultureIgnoreCase(m_pf.UserFirstName) == 0 ? string.Empty : txtPreferredGreeting.Text.Trim();
                m_pf.MobilePhone = txtCell.Text.Trim();
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
            Profile pf = Profile.GetUser(User.Identity.Name, true);
            args.IsValid = true;
            if (txtEmail.Text.CompareOrdinalIgnoreCase(pf.Email) != 0)
            {
                // see if it's available
                pf.Email = txtEmail.Text;
                args.IsValid = pf.IsValid();
            }
        }

        protected void lnkVerifyEmail_Click(object sender, EventArgs e)
        {
            if (!IsValid)
                return;

            VerifyEmail(txtEmail.Text);
            lblVerificationSent.Visible = true;
        }

        protected void lnkAddAltEmail_Click(object sender, EventArgs e)
        {
            Validate("valAltEmail");
            if (IsValid)
            {
                VerifyEmail(txtAltEmail.Text);
                lblAltEmailSent.Visible = true;
            }
        }

        protected void gvAlternateEmails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareCurrentCultureIgnoreCase("_Delete") == 0)
            {
                m_pf.DeleteVerifiedEmail((string) e.CommandArgument);
                gvAlternateEmails.DataSource = m_pf.AlternateEmailsForUser();
                gvAlternateEmails.DataBind();
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

        protected void SetHeadShot()
        {
            bool fHasHeadshot = m_pf.HasHeadShot;
            imgHdSht.Src = m_pf.HeadShotHRef;
            imgDelHdSht.Visible = fHasHeadshot;
            Master.RefreshHeader();
        }

        protected void btnUpdHdSht_Click(object sender, EventArgs e)
        {
            if (fuHdSht.HasFile)
            {
                byte[] rgb = ScaledHeadshotFromStream(fuHdSht.PostedFile.InputStream);
                if (rgb != null && rgb.Length > 0)
                {
                    m_pf.HeadShot = rgb;
                    m_pf.FCommit();
                    SetHeadShot();
                }
            }
        }

        protected void imgDelHdSht_Click(object sender, ImageClickEventArgs e)
        {
            m_pf.HeadShot = null;
            SetHeadShot();
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

        #region Sharing
        protected void lnkDeAuthGPhotos_Click(object sender, EventArgs e)
        {
            m_pf.SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, null, true);
            mvGPhotos.SetActiveView(vwGPhotosDisabled);
        }
        #endregion
    }
}