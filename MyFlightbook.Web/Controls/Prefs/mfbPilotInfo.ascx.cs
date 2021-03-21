using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbPilotInfo : UserControl
    {
        #region Properties
        protected Profile m_pf { get { return Profile.GetUser(Page.User.Identity.Name); } }

        protected MedicalType SelectedMedicalType
        {
            get { return (MedicalType)Enum.Parse(typeof(MedicalType), cmbMedicalType.SelectedValue); }
            set { cmbMedicalType.SelectedValue = value.ToString(); }
        }

        protected int MonthsToMedical
        {
            get { return Convert.ToInt32(cmbMonthsMedical.SelectedValue, CultureInfo.InvariantCulture); }
            set { cmbMonthsMedical.SelectedValue = value.ToString(CultureInfo.InvariantCulture); }
        }

        protected bool UsesICAOMedical
        {
            get { return rblMedicalDurationType.SelectedIndex == 1; }
            set { rblMedicalDurationType.SelectedIndex = value ? 1 : 0; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Set pilot info validation group
                util.SetValidationGroup(mfbTypeInDateCFIExpiration, btnUpdatePilotInfo.ValidationGroup);
                util.SetValidationGroup(dateMedical, btnUpdatePilotInfo.ValidationGroup);
            }
        }

        public void InitPilotInfo()
        {
            mfbEASATip.BodyContent = Branding.ReBrand(Resources.Preferences.MedicalEASATip);
            dateMedical.Date = m_pf.LastMedical;
            dateMedical.TextControl.ValidationGroup = "valPilotInfo";
            MonthsToMedical = m_pf.MonthsToMedical;
            MedicalType mt = new ProfileCurrency(m_pf).TypeOfMedical;
            SelectedMedicalType = mt;
            dateDOB.Date = m_pf.DateOfBirth ?? DateTime.MinValue;
            UpdateForMedicalType(mt);
            UsesICAOMedical = m_pf.UsesICAOMedical;
            UpdateNextMedical();
            txtCertificate.Text = m_pf.Certificate;
            txtLicense.Text = m_pf.License;
            txtLicense.Attributes["dir"] = txtCertificate.Attributes["dir"] = "auto";
            txtMedicalNotes.Text = m_pf.GetPreferenceForKey(MFBConstants.keyMedicalNotes) ?? string.Empty;
            mfbTypeInDateCFIExpiration.Date = m_pf.CertificateExpiration;
            mfbDateEnglishCheck.Date = m_pf.EnglishProficiencyExpiration;
            BasicMedManager.RefreshBasicMedEvents();

            gvIPC.DataSource = ProfileEvent.GetIPCEvents(Page.User.Identity.Name);
            gvIPC.DataBind();

            Achievements.UserRatings ur = new Achievements.UserRatings(m_pf.UserName);
            gvRatings.DataSource = ur.Licenses;
            gvRatings.DataBind();

            ProfileCurrency mc = new ProfileCurrency(m_pf);

            ProfileEvent[] rgpfeBFR = ProfileEvent.GetBFREvents(Page.User.Identity.Name, mc.LastBFREvent);

            gvBFR.DataSource = rgpfeBFR;
            gvBFR.DataBind();

            if (rgpfeBFR.Length > 0) // we have at least one BFR event, so the last one should be the most recent.
            {
                lblNextBFR.Text = ProfileCurrency.NextBFR(rgpfeBFR[rgpfeBFR.Length - 1].Date).ToShortDateString();
                pnlNextBFR.Visible = true;
            }

            string szPane = Request["pane"];
            if (!String.IsNullOrEmpty(szPane))
            {
                for (int i = 0; i < accordianPilotInfo.Panes.Count; i++)
                {
                    switch (szPane)
                    {
                        case "medical":
                            if (accordianPilotInfo.Panes[i] == acpMedical)
                                accordianPilotInfo.SelectedIndex = i;
                            break;
                        case "certificates":
                            if (accordianPilotInfo.Panes[i] == acpCertificates)
                                accordianPilotInfo.SelectedIndex = i;
                            break;
                        case "flightreview":
                            if (accordianPilotInfo.Panes[i] == acpFlightReviews)
                                accordianPilotInfo.SelectedIndex = i;
                            break;
                        case "ipc":
                            if (accordianPilotInfo.Panes[i] == acpIPCs)
                                accordianPilotInfo.SelectedIndex = i;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        protected Boolean FCommitPilotInfo()
        {
            m_pf.License = txtLicense.Text;
            m_pf.Certificate = txtCertificate.Text;
            if (m_pf.Certificate.Length > 30)
                m_pf.Certificate = m_pf.Certificate.Substring(0, 30);
            m_pf.CertificateExpiration = mfbTypeInDateCFIExpiration.Date;
            m_pf.EnglishProficiencyExpiration = mfbDateEnglishCheck.Date;

            try
            {
                m_pf.FCommit();
            }
            catch (MyFlightbookException ex)
            {
                lblPilotInfoUpdated.Visible = true;
                lblPilotInfoUpdated.Text = ex.Message;
                lblPilotInfoUpdated.CssClass = "error";
                return false;
            }

            return true;
        }

        protected bool FCommitMedicalInfo()
        {
            Page.Validate("valPilotInfo");
            if (!Page.IsValid)
                return false;

            m_pf.LastMedical = dateMedical.Date;
            m_pf.MonthsToMedical = MonthsToMedical;
            m_pf.UsesICAOMedical = UsesICAOMedical;

            try
            {
                // Type of medical, notes, and date of birth are all set synchronously.
                ProfileCurrency _ = new ProfileCurrency(m_pf) { TypeOfMedical = (MedicalType)Enum.Parse(typeof(MedicalType), cmbMedicalType.SelectedValue) };
                m_pf.SetPreferenceForKey(MFBConstants.keyMedicalNotes, txtMedicalNotes.Text, String.IsNullOrWhiteSpace(txtMedicalNotes.Text));
                m_pf.DateOfBirth = dateDOB.Date;

                m_pf.FCommit();
                UpdateNextMedical();
            }
            catch (MyFlightbookException ex)
            {
                lblMedicalInfo.Visible = true;
                lblMedicalInfo.Text = ex.Message;
                lblMedicalInfo.CssClass = "error";
                return false;
            }

            return true;
        }

        protected string CSSForItem(CurrencyState cs)
        {
            switch (cs)
            {
                case CurrencyState.GettingClose:
                    return "currencynearlydue";
                case CurrencyState.NotCurrent:
                    return "currencyexpired";
                case CurrencyState.OK:
                    return "currencyok";
            }
            return string.Empty;
        }

        protected void btnUpdatePilotInfo_Click(object sender, EventArgs e)
        {
            if (FCommitPilotInfo())
                lblPilotInfoUpdated.Visible = true;
        }

        protected void btnUpdateMedical_Click(object sender, EventArgs e)
        {
            if (FCommitMedicalInfo())
                lblMedicalInfo.Visible = true;
        }

        private void UpdateNextMedical()
        {
            Page.Validate("valPilotInfo");
            if (!Page.IsValid)
                return;

            IEnumerable<CurrencyStatusItem> rgcs = ProfileCurrency.MedicalStatus(dateMedical.Date, MonthsToMedical, SelectedMedicalType, dateDOB.Date, UsesICAOMedical);
            if (rgcs.Any())
            {
                pnlNextMedical.Visible = true;
                rptNextMedical.DataSource = rgcs;
                rptNextMedical.DataBind();
            }
            else
                pnlNextMedical.Visible = false;
        }

        public void DurationIsValid(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            args.IsValid = !dateMedical.Date.HasValue() || MonthsToMedical > 0;
        }

        protected void valDOBRequired_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            // all is good if we have no medical (by definition doesn't require DOB), a medical type that doesn't require a birthday, or if we have a DOB.
            args.IsValid = (!dateMedical.Date.HasValue() || !ProfileCurrency.RequiresBirthdate(SelectedMedicalType) || dateDOB.Date.HasValue()); ;
        }

        protected void UpdateForMedicalType(MedicalType mt)
        {
            bool fNeedsDOB = ProfileCurrency.RequiresBirthdate(mt);
            rowDOB.Visible = valDOBRequired.Enabled = fNeedsDOB;
            rowOtherMedical.Visible = valMonthsMedical.Enabled = !fNeedsDOB;
        }

        protected void cmbMedicalType_SelectedIndexChanged(object sender, EventArgs e)
        {
            MedicalType mt = SelectedMedicalType;
            UpdateForMedicalType(mt);
            UpdateNextMedical();
        }

        protected void rblMedicalDurationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateNextMedical();
        }

        protected void cmbMonthsMedical_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateNextMedical();
        }
    }
}