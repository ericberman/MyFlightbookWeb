using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSignFlight : System.Web.UI.UserControl
{
    private LogbookEntry m_le = null;
    private const string szKeyVSIDFlight = "keyVSEntryToSign";
    private const string szKeyVSIDStudent = "keyVSStudentName";
    private const string szKeyCFIUserName = "keyVSCFIUserName";
    private const string szKeyCookieCopy = "cookieCopy";

    public event EventHandler Cancel = null;
    public event EventHandler SigningFinished = null;
    protected MyFlightbook.Profile m_pf = null;

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
                    string szError;
                    bool fValidCFIInfo = (CFIProfile == null) ? false : CFIProfile.CanSignFlights(out szError);

                    lblCFIDate.Visible = fValidCFIInfo;
                    lblCFICertificate.Visible = fValidCFIInfo;
                    dropDateCFIExpiration.Visible = !fValidCFIInfo;
                    txtCFICertificate.Visible = !fValidCFIInfo;

                    rowSignature.Visible = rowEmail.Visible = false;

                    // need to collect and validate a password if the currently logged is NOT the person signing.
                    bool fNeedPassword = (CFIProfile == null || CFIProfile.UserName.CompareOrdinal(CurrentUser.UserName) != 0);
                    lblCFIName.Text = CFIProfile == null ? string.Empty : CFIProfile.UserFullName;
                    pnlRowPassword.Visible = fNeedPassword;
                    valCorrectPassword.Enabled = valPassword.Enabled = fNeedPassword;

                    // Offer to copy the flight for the CFI
                    pnlCopyFlight.Visible = CFIProfile != null;
                    ckCopyFlight.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignFlightCopy, CFIProfile == null ? String.Empty : CFIProfile.UserFullName);
                    valCertificateRequired.Enabled = valCFIExpiration.Enabled = valNameRequired.Enabled = valBadEmail.Enabled = valEmailRequired.Enabled = mfbScribbleSignature.Enabled = false;
                    break;
            }
        }
    }

    protected Profile CurrentUser
    {
        get { return m_pf ?? (m_pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name)); }
    }

    private Profile m_pfCFI = null;
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
                    m_pfCFI = MyFlightbook.Profile.GetUser(szCFIUser);
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
                lblPassPrompt.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignReEnterPassword, value.UserFirstName);
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
                int idFlight = (int) ViewState[szKeyVSIDFlight];
                string szUser = (string)ViewState[szKeyVSIDStudent];
                if (idFlight > 0 && idFlight != LogbookEntry.idFlightNew)
                {
                    m_le = new LogbookEntry();
                    m_le.FLoadFromDB(idFlight, szUser);
                }
            }
            return m_le;
        }
        set 
        {
            if (value == null)
                throw new ArgumentNullException("value");
            m_le = value;
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
                        .Replace("[SIC]", MyFlightbook.Profile.GetUser(m_le.User).UserFullName)
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
                bool copyFlight;
                if (Boolean.TryParse(Request.Cookies[szKeyCookieCopy].Value, out copyFlight))
                    ckCopyFlight.Checked = copyFlight;
            }
        }
        dropDateCFIExpiration.DefaultDate = DateTime.MinValue;
    }

    protected void fvEntryToSign_OnDataBound(object sender, EventArgs e)
    {
        Repeater r = (Repeater)fvEntryToSign.FindControl("rptProps");
        r.DataSource = Flight.CustomProperties;
        r.DataBind();
    }

    protected void CopyToInstructor(LogbookEntry le)
    {
        // Now make it look like the CFI's: their username, swap DUAL for CFI time, ensure that PIC time is present.
        le.FlightID = LogbookEntry.idFlightNew;
        string szStudentName = MyFlightbook.Profile.GetUser(le.User).UserFullName;
        List<CustomFlightProperty> lstProps = new List<CustomFlightProperty>(le.CustomProperties);
        lstProps.ForEach(cfp => cfp.FlightID = le.FlightID);

        // Add the student's name as a property
        lstProps.RemoveAll(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropStudentName);
        lstProps.Add(new CustomFlightProperty(new CustomPropertyType(CustomPropertyType.KnownProperties.IDPropStudentName)) { FlightID = le.FlightID, TextValue = szStudentName });

        le.Comment = String.IsNullOrEmpty(le.Comment) ? SigningComments : String.Format(CultureInfo.CurrentCulture, Resources.SignOff.StudentNameTemplate, le.Comment, SigningComments);
        le.User = CFIProfile.UserName;  // Swap username, of course, but do so AFTER adjusting the comment above (where we had the user's name)
        le.PIC = le.CFI = Flight.Dual;  // Assume you were PIC for the time you were giving instruction.
        le.Dual = le.SimulatedIFR = 0.0M;

        // Swap ground instruction given/ground-instruction received
        CustomFlightProperty cfpGIReceived = lstProps.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived);
        if (cfpGIReceived != null)
        {
            CustomFlightProperty cfpGIGiven = lstProps.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven);
            if (cfpGIGiven == null)
                cfpGIGiven = new CustomFlightProperty(new CustomPropertyType(CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven));
            cfpGIGiven.DecValue = cfpGIReceived.DecValue;
            cfpGIGiven.FlightID = le.FlightID;
            cfpGIReceived.DeleteProperty();
            lstProps.Remove(cfpGIReceived);
            lstProps.Add(cfpGIGiven);
        }
        le.CustomProperties.SetItems(lstProps);

        // Add this aircraft to the user's profile if needed
        UserAircraft ua = new UserAircraft(CFIProfile.UserName);
        Aircraft ac = new Aircraft(le.AircraftID);
        if (!ua.CheckAircraftForUser(ac))
            ua.FAddAircraftForUser(ac);

        bool result = le.FCommit(true);
        if (!result || le.LastError != LogbookEntry.ErrorCode.None)
            util.NotifyAdminEvent("Error copying flight to instructor's logbook",
                String.Format(CultureInfo.CurrentCulture, "Flight: {0}, CFI: {1}, Student: {2}, ErrorCode: {3}, Error: {4}", le.ToString(), le.User, szStudentName, le.LastError, le.ErrorString),
                ProfileRoles.maskSiteAdminOnly);
    }

    protected void btnSign_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;

        try
        {
            switch (SigningMode)
            {
                case SignMode.AdHoc:
                    {
                        byte[] rgSig = mfbScribbleSignature.Base64Data();
                        if (rgSig != null)
                            Flight.SignFlightAdHoc(txtCFIName.Text, txtCFIEmail.Text, txtCFICertificate.Text, dropDateCFIExpiration.Date, SigningComments, rgSig, ckSignSICEndorsement.Checked);
                        else
                            return;
                    }
                    break;
                case SignMode.Authenticated:
                    string szError = String.Empty;

                    bool needProfileRefresh = !CFIProfile.CanSignFlights(out szError);
                    if (needProfileRefresh)
                    {
                        CFIProfile.Certificate = txtCFICertificate.Text;
                        CFIProfile.CertificateExpiration = dropDateCFIExpiration.Date;
                    }

                    // Do ANOTHER check to see if you can sign - setting these above may have fixed the problem.
                    if (!CFIProfile.CanSignFlights(out szError))
                    {
                        lblErr.Text = szError;
                        return;
                    }

                    // If we are here, then we were successful - update the profile if it needed it
                    if (needProfileRefresh)
                        CFIProfile.FCommit();

                    // Prepare for signing
                    Flight.SignFlight(CFIProfile.UserName, SigningComments);

                    // Copy the flight to the CFI's logbook if needed.
                    // We modify a new copy of the flight; this avoids modifying this.Flight, but ensures we get every property
                    if (ckCopyFlight.Checked)
                        CopyToInstructor(Flight.Clone());

                    Response.Cookies[szKeyCookieCopy].Value = ckCopyFlight.Checked.ToString();
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

        if (SigningFinished != null)
            SigningFinished(sender, e);
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        if (Cancel != null)
            Cancel(sender, e);
    }

    protected void valCFIExpiration_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (SigningMode == SignMode.AdHoc)
        {
            try
            {
                if (dropDateCFIExpiration.Date.AddDays(1).CompareTo(DateTime.Now) <= 0)
                    args.IsValid = false;
            }
            catch (ArgumentOutOfRangeException)
            {
                args.IsValid = false;
            }
        }
    }

    protected void valCorrectPassword_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
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
        bool fUseTemplate = ((CheckBox)sender).Checked;
        mvComments.SetActiveView(fUseTemplate ? vwTemplate : vwEdit);

        valCFIExpiration.Enabled = !fUseTemplate;
        CFIExpiration = (m_pfCFI == null || fUseTemplate) ? DateTime.MinValue : m_pfCFI.CertificateExpiration;
    }
}