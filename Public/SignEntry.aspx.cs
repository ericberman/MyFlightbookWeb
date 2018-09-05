using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_SignEntry : System.Web.UI.Page
{
    private const string szVSPrevSignedFlights = "vsPrevSignedFlights";

    protected string Username
    {
        get { return hdnUser.Value; }
        set { hdnUser.Value = value; }
    }


    protected Dictionary<string, LogbookEntry> PreviouslySignedAdhocFlights
    {
        get
        {
            if (ViewState[szVSPrevSignedFlights] == null)
            {
                FlightQuery fq = new FlightQuery(Username) { IsSigned = true };
                DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq));
                Dictionary<string, LogbookEntry> d = new Dictionary<string, LogbookEntry>();
                dbh.ReadRows(
                    (comm) => { }, 
                    (dr) => 
                    {
                        LogbookEntry le = new LogbookEntry(dr, Username);
                        // Add it to the dictionary if:
                        // a) It has a digitized signature (i.e., scribble)
                        // b) it isn't already in there, or
                        // c) this flight has a later CFI expiration than the one we found (overwriting it).
                        if (le.HasDigitizedSig)
                        {
                            string szKey = le.CFIName.ToUpper(CultureInfo.CurrentCulture);
                            if (!d.ContainsKey(szKey))
                                d[szKey] = le;
                            else
                            {
                                LogbookEntry lePrev = d[szKey];
                                if (lePrev.CFIExpiration.CompareTo(le.CFIExpiration) < 0)
                                    d[szKey] = le;
                            }
                        }
                    });
                ViewState[szVSPrevSignedFlights] = d;
            }
            return (Dictionary<string, LogbookEntry>) ViewState[szVSPrevSignedFlights];
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;

        if (!IsPostBack)
        {
            lblErr.Text = String.Empty;
            string szAuthToken = util.GetStringParam(Request, "auth");
            if (!String.IsNullOrEmpty(szAuthToken))
            {
                using (MFBWebService ws = new MFBWebService())
                    Username = ws.GetEncryptedUser(szAuthToken);
            }

            bool fIsLocalOrSecure = MFBWebService.CheckSecurity(Request);

            // If no valid auth token, fall back to the authenticated name.
            if (String.IsNullOrEmpty(Username) && Page.User.Identity.IsAuthenticated && fIsLocalOrSecure)
                Username = Page.User.Identity.Name;

            // Require a secure connection for other than debugging.
            if (!fIsLocalOrSecure && !Request.IsSecureConnection)
                Username = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(Username))
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                int idFlight = util.GetIntParam(Request, "idFlight", LogbookEntry.idFlightNew);
                if (idFlight == LogbookEntry.idFlightNew)
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                LogbookEntry le = new LogbookEntry();
                if (!le.FLoadFromDB(idFlight, Username))
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                mfbSignFlight.Flight = le;
                CFIStudentMap sm = new CFIStudentMap(Username);

                if (Username == null)
                    throw new MyFlightbookValidationException("No username for previously signed flights");

                Dictionary<string, LogbookEntry> d = PreviouslySignedAdhocFlights;

                // If no instructors, and no previously signed flights, assume ad-hoc and go straight to accept terms.
                if (sm.Instructors.Count() == 0 && d.Keys.Count == 0)
                {
                    mfbSignFlight.SigningMode = Controls_mfbSignFlight.SignMode.AdHoc;
                    mfbSignFlight.CFIProfile = null;
                    mvSignFlight.SetActiveView(vwAcceptTerms);
                }
                else
                {
                    rptInstructors.DataSource = sm.Instructors;
                    rptInstructors.DataBind();

                    List<string> lstKeys = new List<string>(d.Keys);
                    lstKeys.Sort();
                    List<LogbookEntry> lstPrevInstructors = new List<LogbookEntry>();

                    foreach (string sz in lstKeys)
                        lstPrevInstructors.Add(d[sz]);

                    rptPriorInstructors.DataSource = lstPrevInstructors;
                    rptPriorInstructors.DataBind();

                    mvSignFlight.SetActiveView(vwPickInstructor);
                }


                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignFlightHeader, MyFlightbook.Profile.GetUser(le.User).UserFullName);
                lblDisclaimerResponse.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement1);
                lblDisclaimerResponse2.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement2);
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }

    #region Selecting an instructor
    protected void lnkNewInstructor_Click(object sender, EventArgs e)
    {
        mfbSignFlight.CFIProfile = null;
        mvSignFlight.SetActiveView(vwAcceptTerms);
    }

    protected void chooseInstructor(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        string szInstructor = e.CommandArgument.ToString();
        if (String.IsNullOrEmpty(szInstructor))
            throw new ArgumentException("Invalid instructor - empty!");

        if (e.CommandName.CompareCurrentCultureIgnoreCase("Existing") == 0)
        {
            mfbSignFlight.CFIProfile = MyFlightbook.Profile.GetUser(szInstructor);
            mvSignFlight.SetActiveView(vwSign);
        }
        else if (e.CommandName.CompareCurrentCultureIgnoreCase("Prior") == 0)
        {
            if (Username == null)
                throw new MyFlightbookValidationException("No username for previously signed flights");

            Dictionary<string, LogbookEntry> d = PreviouslySignedAdhocFlights;
            string szKey = szInstructor.ToUpper(CultureInfo.CurrentCulture);
            if (d.ContainsKey(szKey))  // should always be true
            {
                LogbookEntry le = PreviouslySignedAdhocFlights[szKey];
                mfbSignFlight.CFIName = le.CFIName;
                mfbSignFlight.CFIEmail = le.CFIEmail;
                mfbSignFlight.CFICertificate = le.CFICertificate;
                mfbSignFlight.CFIExpiration = le.CFIExpiration;
            }

            mvSignFlight.SetActiveView(vwAcceptTerms);
        }
    }
    #endregion

    protected void SigningFinished(object sender, EventArgs e)
    {
        mvSignFlight.SetActiveView(vwSuccess);
    }

    protected void btnAcceptResponsibility_Click(object sender, EventArgs e)
    {
        if (ckAccept.Checked)
            mvSignFlight.SetActiveView(vwSign);
        else
            lblErr.Text = Resources.SignOff.errAcceptDisclaimer;
    }
}