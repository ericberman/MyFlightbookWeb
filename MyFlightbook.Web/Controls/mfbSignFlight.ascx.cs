using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
 * Handles signing of flights.  LOTS of different scenarios:
 * 1) Ad-hoc vs. authenticated
 *   A Ad-hoc (unauthenticated, use a scribble) - Certificate, expiration are all editable
 *     - Instructor name is editable, can be pre-filled with previously used value
 *     - EMail field is editable, can be pre-filled with previously used value
 *     - Certificate field is editable, can be pre-filled with previously used value.
 *     - Expiration field is editable, might be pre-filled previous used value
 *     - Password field IS NOT shown
 *     - Scribble field IS shown
 *     - Option to copy to signer's logbook is NOT shown
 *   B Authenticated - NOT editable CFI details unless expiration is invalid or missing
 *     - No name field (in the "Signed by..." text)
 *     - No email field (not needed)
 *     - Certificate field is read-only UNLESS it is missing or expired
 *     - Expiration field is read-only UNLESS it is missing or expired
 *     - Password field IS shown
 *     - Scribble field IS NOT shown
 *     - Authenticated has option to copy to signer's logbook
 *  2) Signing as ATP (per 61.167(2))
 *    A Ad-hoc - Always show this checkbox
 *    B Authenticated - show this checkbox ONLY if unexpired or missing expiration
 *  3) AC 135-43 signing - show IF SIC and turbine or AMEL
 */

namespace MyFlightbook.Instruction
{
    public partial class mfbSignFlight : UserControl
    {
        private LogbookEntry m_le;
        private const string szKeyVSIDFlight = "keyVSEntryToSign";
        private const string szKeyVSIDStudent = "keyVSStudentName";
        private const string szKeyCFIUserName = "keyVSCFIUserName";
        private const string szKeyCookieCopy = "cookieCopy";

        public event EventHandler Cancel;
        public event EventHandler SigningFinished;
        private Profile m_pf;

        public enum SignMode { Authenticated, AdHoc };

        #region properties
        /// <summary>
        /// Show or hide the cancel button
        /// </summary>
        public bool ShowCancel
        {
            get { return btnCancel.Visible; }
            set { btnCancel.Visible = value; }
        }

        /// <summary>
        /// Is this AdHoc (no relationship with the CFI, handwritten signature?) or Authenticated (relationship)?
        /// </summary>
        public SignMode SigningMode
        {
            get { return (rowEmail.Visible ? SignMode.AdHoc : SignMode.Authenticated); }
            set
            {
                switch (value)
                {
                    case SignMode.AdHoc:
                        // Show the editable fields and hide the pre-filled ones.
                        rowSignature.Visible = rowEmail.Visible = true;
                        dropDateCFIExpiration.Visible = true;
                        ckATP.Visible = true;
                        txtCFICertificate.Visible = true;

                        lblCFIDate.Visible = false;
                        lblCFICertificate.Visible = false;

                        pnlRowPassword.Visible = false;
                        valCorrectPassword.Enabled = valPassword.Enabled = false;
                        valCertificateRequired.Enabled = valCFIExpiration.Enabled = valNameRequired.Enabled = valBadEmail.Enabled = valEmailRequired.Enabled = mfbScribbleSignature.Enabled = true;

                        // Can't copy the flight
                        pnlCopyFlight.Visible = false;

                        // Can't edit the flight
                        lblCFIName.Text = string.Empty;

                        break;
                    case SignMode.Authenticated:
                        // Show the static fields and hide the editable ones.
                        bool fValidCFIInfo = CFIProfile != null && CFIProfile.CanSignFlights(out string _, Flight.IsGroundOnly);

                        lblCFIDate.Visible = fValidCFIInfo;
                        lblCFICertificate.Visible = fValidCFIInfo;
                        dropDateCFIExpiration.Visible = !fValidCFIInfo;
                        ckATP.Visible = !fValidCFIInfo;
                        txtCFICertificate.Visible = !fValidCFIInfo;

                        rowSignature.Visible = rowEmail.Visible = false;

                        // need to collect and validate a password if the currently logged is NOT the person signing.
                        bool fNeedPassword = (CFIProfile == null || CFIProfile.UserName.CompareOrdinal(CurrentUser.UserName) != 0);
                        lblCFIName.Text = CFIProfile == null ? string.Empty : HttpUtility.HtmlEncode(CFIProfile.UserFullName);
                        pnlRowPassword.Visible = fNeedPassword;
                        valCorrectPassword.Enabled = valPassword.Enabled = fNeedPassword;

                        // Offer to copy the flight for the CFI
                        pnlCopyFlight.Visible = CFIProfile != null;
                        ckCopyFlight.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignFlightCopy, CFIProfile == null ? String.Empty : HttpUtility.HtmlEncode(CFIProfile.UserFullName));
                        valCertificateRequired.Enabled = valCFIExpiration.Enabled = valNameRequired.Enabled = valBadEmail.Enabled = valEmailRequired.Enabled = mfbScribbleSignature.Enabled = false;
                        break;
                }
            }
        }

        protected Profile CurrentUser
        {
            get { return m_pf ?? (m_pf = Profile.GetUser(Page.User.Identity.Name)); }
        }

        private Profile m_pfCFI;
        /// <summary>
        /// Set the profile of the CFI.  If null, automatically switches to AdHoc mode.
        /// </summary>
        public Profile CFIProfile
        {
            get
            {
                if (m_pfCFI == null)
                {
                    string szCFIUser = (string)ViewState[szKeyCFIUserName];
                    if (!String.IsNullOrEmpty(szCFIUser))
                        m_pfCFI = Profile.GetUser(szCFIUser);
                }
                return m_pfCFI;
            }
            set
            {
                m_pfCFI = value;
                if (value == null)
                {
                    ViewState[szKeyCFIUserName] = null;
                    txtCFIName.Text = txtCFIEmail.Text = lblCFIDate.Text = txtCFICertificate.Text = lblCFICertificate.Text = String.Empty;
                    dropDateCFIExpiration.Date = DateTime.Now.AddCalendarMonths(0);
                    SigningMode = SignMode.AdHoc;
                }
                else
                {
                    bool fIsNew = !IsPostBack || (ViewState[szKeyCFIUserName] != null && String.Compare((string)ViewState[szKeyCFIUserName], value.UserName, StringComparison.OrdinalIgnoreCase) != 0);
                    ViewState[szKeyCFIUserName] = value.UserName;
                    if (!String.IsNullOrEmpty(m_pfCFI.Certificate))
                        txtCFICertificate.Text = lblCFICertificate.Text = m_pfCFI.Certificate;
                    if (fIsNew)
                        dropDateCFIExpiration.Date = m_pfCFI.CertificateExpiration;
                    lblCFIDate.Text = m_pfCFI.CertificateExpiration.ToShortDateString();
                    txtCFIEmail.Text = m_pfCFI.Email;
                    txtCFIName.Text = m_pfCFI.UserFullName;
                    lblPassPrompt.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignReEnterPassword, value.PreferredGreeting);
                    SigningMode = SignMode.Authenticated;
                }
            }
        }

        /// <summary>
        /// The current flight
        /// </summary>
        public LogbookEntry Flight
        {
            // Note: we only store the username/flightID in the view state to keep the view state light for mobile devices.
            get
            {
                if (m_le == null && ViewState[szKeyVSIDFlight] != null && ViewState[szKeyVSIDStudent] != null)
                {
                    int idFlight = (int)ViewState[szKeyVSIDFlight];
                    string szUser = (string)ViewState[szKeyVSIDStudent];
                    if (idFlight > 0 && idFlight != LogbookEntryCore.idFlightNew)
                    {
                        m_le = new LogbookEntry();
                        m_le.FLoadFromDB(idFlight, szUser);
                    }
                }
                return m_le;
            }
            set
            {
                m_le = value ?? throw new ArgumentNullException(nameof(value));
                ViewState[szKeyVSIDFlight] = value.FlightID;
                ViewState[szKeyVSIDStudent] = value.User;

                fvEntryToSign.DataSource = new List<LogbookEntry> { value };
                fvEntryToSign.DataBind();

                if (value.SIC > 0)  // see if this is possibly an SIC endorsement per AC 135-43
                {
                    Aircraft ac = new Aircraft(value.AircraftID);
                    MakeModel m = MakeModel.GetModel(ac.ModelID);
                    if (m.CategoryClassID == CategoryClass.CatClassID.AMEL || m.EngineType.IsTurbine())
                    {
                        ckSignSICEndorsement.Visible = true;
                        lblSICTemplate.Text = AC135_43Text;
                    }
                }
            }
        }

        private string AC135_43Text
        {
            get
            {
                return Resources.SignOff.AC135_43SICSignoffTemplate
                            .Replace("[SIC]", Profile.GetUser(m_le.User).UserFullName)
                            .Replace("[Route]", String.IsNullOrWhiteSpace(m_le.Route) ? Resources.SignOff.AC135_43NoRoute : m_le.Route)
                            .Replace("[Date]", m_le.Date.ToShortDateString());
            }
        }

        private string SigningComments
        {
            get { return ckSignSICEndorsement.Checked ? lblSICTemplate.Text : txtComments.Text; }
        }

        public string CFIName
        {
            get { return txtCFIName.Text; }
            set { txtCFIName.Text = value; }
        }

        public string CFICertificate
        {
            get { return txtCFICertificate.Text; }
            set { txtCFICertificate.Text = value; }
        }

        public DateTime CFIExpiration
        {
            get { return dropDateCFIExpiration.Date; }
            set { dropDateCFIExpiration.Date = value; }
        }

        public string CFIEmail
        {
            get { return txtCFIEmail.Text; }
            set { txtCFIEmail.Text = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lblSignatureDisclaimer.Text = Branding.ReBrand(Resources.SignOff.SignedFlightDisclaimer);

                if (Request.Cookies[szKeyCookieCopy] != null)
                {
                    if (Boolean.TryParse(Request.Cookies[szKeyCookieCopy].Value, out bool copyFlight))
                        ckCopyFlight.Checked = copyFlight;
                }
            }
            else
            {
                LogbookEntry _ = Flight;    // force the flight to load.
            }

            UpdateDateState();
            dropDateCFIExpiration.DefaultDate = DateTime.MinValue;
        }

        protected void UpdateDateState()
        {
            valCFIExpiration.Enabled = dropDateCFIExpiration.TextControl.Enabled = !ckATP.Checked && !ckSignSICEndorsement.Checked;
            if (!dropDateCFIExpiration.TextControl.Enabled)
                dropDateCFIExpiration.Date = dropDateCFIExpiration.DefaultDate;
        }

        protected void fvEntryToSign_OnDataBound(object sender, EventArgs e)
        {
            Repeater r = (Repeater)fvEntryToSign.FindControl("rptProps");
            r.DataSource = Flight.CustomProperties;
            r.DataBind();
        }

        protected void btnSign_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            try
            {
                bool fIsGroundOrATP = ckATP.Checked || Flight.IsGroundOnly;

                switch (SigningMode)
                {
                    case SignMode.AdHoc:
                        {
                            byte[] rgSig = mfbScribbleSignature.Base64Data();
                            if (rgSig != null)
                                Flight.SignFlightAdHoc(txtCFIName.Text, txtCFIEmail.Text, txtCFICertificate.Text, dropDateCFIExpiration.Date, SigningComments, rgSig, ckSignSICEndorsement.Checked || fIsGroundOrATP);
                            else
                                return;
                        }
                        break;
                    case SignMode.Authenticated:
                        string szError = String.Empty;

                        bool needProfileRefresh = !CFIProfile.CanSignFlights(out szError, Flight.IsGroundOnly);
                        if (needProfileRefresh)
                        {
                            CFIProfile.Certificate = txtCFICertificate.Text;
                            CFIProfile.CertificateExpiration = dropDateCFIExpiration.Date;
                        }

                        // Do ANOTHER check to see if you can sign - setting these above may have fixed the problem.
                        if (!CFIProfile.CanSignFlights(out szError, fIsGroundOrATP))
                        {
                            lblErr.Text = szError;
                            return;
                        }

                        // If we are here, then we were successful - update the profile if it needed it
                        if (needProfileRefresh)
                            CFIProfile.FCommit();

                        // Prepare for signing
                        Flight.SignFlightAuthenticated(CFIProfile.UserName, SigningComments, fIsGroundOrATP);

                        // Copy the flight to the CFI's logbook if needed.
                        // We modify a new copy of the flight; this avoids modifying this.Flight, but ensures we get every property
                        if (ckCopyFlight.Checked)
                        {
                            // Issue #593 Load any telemetry, if necessary
                            LogbookEntry le = new LogbookEntry(Flight.FlightID, Flight.User, LogbookEntryCore.LoadTelemetryOption.LoadAll);
                            Flight.FlightData = le.FlightData;
                            Flight.CopyToInstructor(CFIProfile.UserName, SigningComments);
                        }

                        Response.Cookies[szKeyCookieCopy].Value = ckCopyFlight.Checked.ToString(CultureInfo.InvariantCulture);
                        Response.Cookies[szKeyCookieCopy].Expires = DateTime.Now.AddYears(10);
                        break;
                }
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
                return;
            }
            catch (MyFlightbookValidationException ex)
            {
                lblErr.Text = ex.Message;
                return;
            }

            SigningFinished?.Invoke(sender, e);
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Cancel?.Invoke(sender, e);
        }

        protected void valCFIExpiration_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (SigningMode == SignMode.AdHoc)
            {
                if ((!dropDateCFIExpiration.Date.HasValue() && !Flight.IsGroundOnly) ||
                    (dropDateCFIExpiration.Date.HasValue() && dropDateCFIExpiration.Date.AddDays(1).CompareTo(DateTime.Now) <= 0))
                    args.IsValid = false;
            }
        }

        protected void valCorrectPassword_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (String.IsNullOrEmpty(txtPassConfirm.Text) ||
                CFIProfile == null ||
                !System.Web.Security.Membership.ValidateUser(CFIProfile.UserName, txtPassConfirm.Text))
                args.IsValid = false;
            else if (Flight != null)
                Flight.SetPendingSignature(CFIProfile.UserName);    // successfully authenticated - now set up to expect the signature.
        }

        protected void lnkEditFlightToSign_Click(object sender, EventArgs e)
        {
            mvFlightToSign.SetActiveView(vwEntryEdit);
            mfbEditFlight1.SetUpNewOrEdit(Flight.FlightID, true);
        }

        protected void mfbEditFlight1_FlightUpdated(object sender, EventArgs e)
        {
            mvFlightToSign.SetActiveView(vwEntrySummary);
            int idFlight = (int)ViewState[szKeyVSIDFlight];
            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, string.Empty, LogbookEntry.LoadTelemetryOption.None, true);
            Flight = le;    // force a refresh
        }

        protected void ckSignSICEndorsement_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            bool fUseTemplate = ((CheckBox)sender).Checked;
            mvComments.SetActiveView(fUseTemplate ? vwTemplate : vwEdit);

            CFIExpiration = (m_pfCFI == null || fUseTemplate) ? DateTime.MinValue : m_pfCFI.CertificateExpiration;

            UpdateDateState();
        }

        protected void ckATP_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDateState();
        }
    }
}