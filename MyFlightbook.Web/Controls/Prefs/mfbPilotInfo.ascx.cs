using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbPilotInfo : System.Web.UI.UserControl
    {
        protected Profile m_pf { get { return Profile.GetUser(Page.User.Identity.Name); } }
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
            dateMedical.Date = m_pf.LastMedical;
            dateMedical.TextControl.ValidationGroup = "valPilotInfo";
            cmbMonthsMedical.SelectedValue = m_pf.MonthsToMedical.ToString(CultureInfo.CurrentCulture);
            rblMedicalDurationType.SelectedIndex = m_pf.UsesICAOMedical ? 1 : 0;
            txtCertificate.Text = m_pf.Certificate;
            txtLicense.Text = m_pf.License;
            txtLicense.Attributes["dir"] = txtCertificate.Attributes["dir"] = "auto";
            mfbTypeInDateCFIExpiration.Date = m_pf.CertificateExpiration;
            mfbDateEnglishCheck.Date = m_pf.EnglishProficiencyExpiration;
            UpdateNextMedical(m_pf);
            BasicMedManager.RefreshBasicMedEvents();

            gvIPC.DataSource = ProfileEvent.GetIPCEvents(Page.User.Identity.Name);
            gvIPC.DataBind();

            MyFlightbook.Achievements.UserRatings ur = new Achievements.UserRatings(m_pf.UserName);
            gvRatings.DataSource = ur.Licenses;
            gvRatings.DataBind();

            ProfileEvent[] rgpfeBFR = ProfileEvent.GetBFREvents(Page.User.Identity.Name, m_pf.LastBFREvent);

            gvBFR.DataSource = rgpfeBFR;
            gvBFR.DataBind();

            if (rgpfeBFR.Length > 0) // we have at least one BFR event, so the last one should be the most recent.
            {
                lblNextBFR.Text = Profile.NextBFR(rgpfeBFR[rgpfeBFR.Length - 1].Date).ToShortDateString();
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
            m_pf.MonthsToMedical = Convert.ToInt32(cmbMonthsMedical.SelectedValue, CultureInfo.InvariantCulture);
            m_pf.UsesICAOMedical = rblMedicalDurationType.SelectedIndex > 0;

            try
            {
                m_pf.FCommit();
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

        protected void btnUpdatePilotInfo_Click(object sender, EventArgs e)
        {
            if (FCommitPilotInfo())
            {
                lblPilotInfoUpdated.Visible = true;
            }
        }

        protected void btnUpdateMedical_Click(object sender, EventArgs e)
        {
            if (FCommitMedicalInfo())
            {
                lblMedicalInfo.Visible = true;
                UpdateNextMedical(m_pf);
            }
        }

        private void UpdateNextMedical(Profile pf)
        {
            if (pf.LastMedical.CompareTo(DateTime.MinValue) != 0)
            {
                lblNextMedical.Text = pf.NextMedical.ToShortDateString();
                pnlNextMedical.Visible = true;
            }
            else
                pnlNextMedical.Visible = false;
        }

        public void DurationIsValid(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            args.IsValid = (dateMedical.Date == DateTime.MinValue || cmbMonthsMedical.SelectedValue != "0");
        }
    }
}