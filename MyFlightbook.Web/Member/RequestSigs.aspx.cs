using MyFlightbook.Web.WizardPage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    public partial class Member_RequestSigs : MFBWizardPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            InitWizard(wzRequestSigs);
            Master.SelectedTab = tabID.instSignFlights;
            if (!IsPostBack)
            {
                lblName.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.TrainingHeader, HttpUtility.HtmlEncode(Profile.GetUser(Page.User.Identity.Name).UserFullName));

                Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleTraining, Branding.CurrentBrand.AppName);
               
                SetUpInstructorList(RefreshFlightsList(util.GetStringParam(Request, "id").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)));
                lblSignatureDisclaimer.Text = Branding.ReBrand(Resources.SignOff.SignedFlightDisclaimer);
            }
        }

        protected string RefreshFlightsList(string[] rgFlightIds)
        {
            if (rgFlightIds == null)
                throw new ArgumentNullException(nameof(rgFlightIds));

            HashSet<int> lstIds = new HashSet<int>();
            foreach (string sz in rgFlightIds)
            {
                if (int.TryParse(sz, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idFlight))
                    lstIds.Add(idFlight);
            }

            bool fPreSelected = false;  // true if the flights should be pre-selected (i.e., passed in)

            List<LogbookEntryDisplay> lstFlights = new List<LogbookEntryDisplay>();

            foreach (int idFlight in lstIds)
            {
                LogbookEntryDisplay le = new LogbookEntryDisplay(idFlight, User.Identity.Name);
                if (!le.IsNewFlight && le.CanRequestSig)   // it loaded (is owned by the user) and signable
                    lstFlights.Add(le);
                fPreSelected = (lstFlights.Count > 0);
            }
            if (lstFlights.Count == 0)
            {
                lstFlights =new List<LogbookEntryDisplay>(LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(new FlightQuery(User.Identity.Name)), User.Identity.Name, "Date", SortDirection.Descending, false, false));
                lstFlights.RemoveAll(le => !le.CanRequestSig);
            }

            rptSelectedFlights.DataSource = lstFlights;
            rptSelectedFlights.DataBind();

            // Select the items that were passed in.
            if (fPreSelected)
                foreach (RepeaterItem ri in rptSelectedFlights.Items)
                    ((CheckBox)ri.FindControl("ckFlight")).Checked = true;

            // See if there's an obvious best-guess instructor name and return that.
            HashSet<string> hs = new HashSet<string>();
            foreach (LogbookEntryBase le in lstFlights)
                if (le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropInstructorName))
                    hs.Add(le.CustomProperties[CustomPropertyType.KnownProperties.IDPropInstructorName].TextValue.ToUpper(CultureInfo.CurrentCulture));
            hs.RemoveWhere(s => String.IsNullOrEmpty(s));   // SHOULD be a no-op.
            return (hs.Count == 1) ? String.Join(string.Empty, hs) : string.Empty;
        }

        protected void SetUpInstructorList(string szBestGuessInstructor)
        {
            CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
            cmbInstructors.DataSource = sm.Instructors;
            cmbInstructors.DataBind();

            if (!String.IsNullOrEmpty(szBestGuessInstructor))
            {
                foreach (ListItem li in cmbInstructors.Items)
                    if (li.Text.CompareCurrentCultureIgnoreCase(szBestGuessInstructor) == 0)
                    {
                        cmbInstructors.SelectedValue = li.Value;
                        rowEmail.Visible = valBadEmail.Enabled = valEmailRequired.Enabled = false;
                        break;
                    }
            }
        }

        protected void cmbInstructors_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool fNeedsEmail = String.IsNullOrEmpty(cmbInstructors.SelectedValue);
            rowEmail.Visible = valBadEmail.Enabled = valEmailRequired.Enabled = fNeedsEmail;
        }

        protected IList<string> SelectedFlightIDs
        {
            get
            {
                List<string> lstIds = new List<string>();

                foreach (RepeaterItem ri in rptSelectedFlights.Items)
                {
                    CheckBox ck = (CheckBox)ri.FindControl("ckFlight");
                    if (ck.Checked)
                    {
                        HiddenField h = (HiddenField)ri.FindControl("hdnFlightID");
                        lstIds.Add(h.Value);
                    }
                }
                return lstIds;
            }
        }

        protected IEnumerable<LogbookEntry> SelectedFlights
        {
            get
            {
                IList<string> lstIds = SelectedFlightIDs;
                List<int> lst = new List<int>();
                foreach (string szID in lstIds)
                    lst.Add(Convert.ToInt32(szID, CultureInfo.InvariantCulture));
                FlightQuery fq = new FlightQuery(User.Identity.Name) { EnumeratedFlights = lst };
                DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq));
                List<LogbookEntry> lstFlights = new List<LogbookEntry>();
                dbh.ReadRows(
                    (comm) => { },
                    (dr) =>
                    { lstFlights.Add(new LogbookEntry(dr, Page.User.Identity.Name)); });
                return lstFlights;
            }
        }

        protected void VerifySeparateEmail(object sender, ServerValidateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (String.Compare(MyFlightbook.Profile.GetUser(User.Identity.Name).Email, txtCFIEmail.Text, StringComparison.CurrentCultureIgnoreCase) == 0)
                e.IsValid = false;
        }

        protected void wzRequestSigs_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            IEnumerable<LogbookEntry> lstFlights = SelectedFlights;

            string szCFIUsername = String.Empty;
            string szCFIEmail = String.Empty;

            MyFlightbook.Profile pfCFI = null;


            bool fIsNewCFI = String.IsNullOrEmpty(cmbInstructors.SelectedValue);

            // Check In case the named email is already an instructor.
            CFIStudentMap sm = new CFIStudentMap(User.Identity.Name);
            if (fIsNewCFI && sm.IsStudentOf(txtCFIEmail.Text))
            {
                fIsNewCFI = false;
                foreach (InstructorStudent inst in sm.Instructors)
                    if (String.Compare(inst.Email, txtCFIEmail.Text, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        pfCFI = inst;
                        break;
                    }
            }
            else
                pfCFI = MyFlightbook.Profile.GetUser(cmbInstructors.SelectedValue);

            if (fIsNewCFI)
                szCFIEmail = txtCFIEmail.Text;
            else
                szCFIUsername = pfCFI.UserName;

            // check if we already know the email

            foreach (LogbookEntry le in lstFlights)
                le.RequestSignature(szCFIUsername, szCFIEmail);

            // Now send the email
            if (fIsNewCFI)
            {
                CFIStudentMapRequest smr = sm.GetRequest(CFIStudentMapRequest.RoleType.RoleCFI, szCFIEmail);
                smr.Send();
            }
            else
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                using (MailMessage msg = new MailMessage())
                {
                    msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pf.UserFullName));
                    msg.ReplyToList.Add(new MailAddress(pf.Email, pf.UserFullName));
                    msg.To.Add(new MailAddress(pfCFI.Email, pfCFI.UserFullName));
                    msg.Subject = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.SignOff.SignRequestSubject, pf.UserFullName, Branding.CurrentBrand.AppName);

                    string szURL = String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}{1}/{2}", Request.Url.Host, ResolveUrl("~/Member/Training.aspx"), tabID.instStudents.ToString());

                    msg.Body = Branding.ReBrand(Resources.SignOff.SignInvitationExisting).Replace("<% SignPendingFlightsLink %>", szURL).Replace("<% Requestor %>", pf.UserFullName);
                    util.SendMessage(msg);
                }
            }

            Response.Redirect(String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/Training.aspx/{0}", tabID.instSignFlights));
        }

        protected void wzRequestSigs_NextButtonClick(object sender, WizardNavigationEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.CurrentStepIndex == 0 && SelectedFlightIDs.Count == 0)
            {
                lblErrNoSelection.Visible = true;
                e.Cancel = true;
            }
            if (e.CurrentStepIndex == 1)
            {
                lblInstructorNameSummary.Text = String.IsNullOrEmpty(cmbInstructors.SelectedValue) ? txtCFIEmail.Text : MyFlightbook.Profile.GetUser(cmbInstructors.SelectedValue).UserFullName;

                gvFlightsToSign.DataSource = SelectedFlights;
                gvFlightsToSign.DataBind();
            }
        }
    }
}