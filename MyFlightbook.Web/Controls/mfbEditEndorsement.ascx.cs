using AjaxControlToolkit;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    public partial class mfbEditEndorsement : UserControl
    {
        private const string ctlIDPrefix = "endrsTemplCtl";
        private const string keyVSTargetUser = "vsTarget";
        private const string keyVSSourceUser = "vsSource";
        private const string keyVSStudentType = "vsStudentType";

        public event EventHandler<EndorsementEventArgs> NewEndorsement;

        private Profile m_TargetUser;
        private Profile m_SourceUser;

        /// <summary>
        /// Student
        /// </summary>
        public Profile TargetUser
        {
            get { return m_TargetUser; }
            set
            {
                m_TargetUser = value ?? throw new ArgumentNullException(nameof(value));
                lblStudent.Text = HttpUtility.HtmlEncode(value.UserFullName);
                ViewState[keyVSTargetUser] = value.UserName;
            }
        }

        /// <summary>
        /// Type of student endorsement - member or external
        /// </summary>
        public Endorsement.StudentTypes StudentType
        {
            get
            {
                if (ViewState[keyVSStudentType] == null)
                    ViewState[keyVSStudentType] = (int)Endorsement.StudentTypes.Member;
                return (Endorsement.StudentTypes)ViewState[keyVSStudentType];
            }
            set
            {
                ViewState[keyVSStudentType] = (int)value;
                mvStudent.SetActiveView(value == Endorsement.StudentTypes.Member ? vwStudentAuthenticated : vwStudentOffline);
            }
        }

        protected const string szVSMode = "vsSigningMode";
        /// <summary>
        /// Signing mode
        /// </summary>
        public EndorsementMode Mode
        {
            get
            {
                return ViewState[szVSMode] == null ? EndorsementMode.InstructorPushAuthenticated : (EndorsementMode)ViewState[szVSMode];
            }
            set
            {
                ViewState[szVSMode] = value;

                if (value == EndorsementMode.StudentPullAuthenticated)
                {
                    rowPassword.Visible = true;
                    valCorrectPassword.Enabled = valPassword.Enabled = true;
                }
                else
                {
                    rowPassword.Visible = false;
                    valCorrectPassword.Enabled = valPassword.Enabled = false;
                }

                mvCFI.SetActiveView(value == EndorsementMode.StudentPullAdHoc ? vwAdhocCFI : vwStaticCFI);
                mvCFICert.SetActiveView(value == EndorsementMode.StudentPullAdHoc ? vwAdhocCert : vwStaticCert);
                mvCertExpiration.SetActiveView(value == EndorsementMode.StudentPullAdHoc ? vwAdhocCertExpiration : vwStaticCertExpiration);

                rowScribble.Visible = (value == EndorsementMode.StudentPullAdHoc);
                valRequiredCFI.Enabled = valRequiredCert.Enabled = (value == EndorsementMode.StudentPullAdHoc);
                mfbScribbleSignature.Enabled = (value == EndorsementMode.StudentPullAdHoc);
            }
        }

        /// <summary>
        /// CFI
        /// </summary>
        public Profile SourceUser
        {
            get { return m_SourceUser; }
            set
            {
                m_SourceUser = value ?? throw new ArgumentNullException(nameof(value));
                lblCFI.Text = HttpUtility.HtmlEncode(value.UserFullName);
                lblCFICert.Text = HttpUtility.HtmlEncode(value.Certificate);
                lblCFIExp.Text = value.CertificateExpiration.ToShortDateString();
                ViewState[keyVSSourceUser] = value.UserName;
                lblPassPrompt.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.SignReEnterPassword, HttpUtility.HtmlEncode(value.PreferredGreeting));
                mvCertExpiration.SetActiveView(Mode != EndorsementMode.StudentPullAdHoc && value.CertificateExpiration.HasValue() ? vwStaticCertExpiration : vwAdhocCertExpiration);
            }
        }

        /// <summary>
        /// Just previewing?
        /// </summary>
        public bool PreviewMode
        {
            get { return !btnAddEndorsement.Visible; }
            set { btnAddEndorsement.Visible = !value; }
        }

        /// <summary>
        /// ID of the endorsment to use.  Forces a redraw.
        /// </summary>
        public int EndorsementID
        {
            get { return Convert.ToInt32(hdnEndorsementID.Value, CultureInfo.InvariantCulture); }
            set
            {
                int currentID = Convert.ToInt32(hdnEndorsementID.Value, CultureInfo.InvariantCulture);
                bool fResetTitle = (currentID != value);
                hdnEndorsementID.Value = value.ToString(CultureInfo.InvariantCulture);
                UpdateFormForTemplate(value, fResetTitle);
            }
        }

        public void SetEndorsement(Endorsement e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            int id = 1;  // freeform
            hdnEndorsementID.Value = id.ToString(CultureInfo.InvariantCulture);
            UpdateFormForTemplate(id, true, e.EndorsementText);
            txtTitle.Text = e.Title;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                mfbTypeInDate1.Date = DateTime.Now;
                mfbDateCertExpiration.Date = mfbDateCertExpiration.DefaultDate = DateTime.MinValue;
            }
            else
            {
                SourceUser = MyFlightbook.Profile.GetUser((string)ViewState[keyVSSourceUser] ?? Page.User.Identity.Name);
                string szTarget = (string)ViewState[keyVSTargetUser];
                if (!String.IsNullOrEmpty(szTarget))
                    TargetUser = MyFlightbook.Profile.GetUser(szTarget);
            }
        }

        protected void NewTextBox(Control parent, string id, string szDefault, Boolean fMultiline, Boolean fRequired, string szName)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            TextBox tb = new TextBox() { ID = id, Text = szDefault };
            parent.Controls.Add(tb);

            if (fMultiline)
            {
                tb.Rows = 4;
                tb.Width = Unit.Percentage(100);
                tb.TextMode = TextBoxMode.MultiLine;
            }
            else
                tb.SetPlaceholder(szName);

            // no validations for preview mode
            if (fRequired && !PreviewMode)
                plcValidations.Controls.Add(new RequiredFieldValidator() { ID = "val" + id, ControlToValidate = tb.ID, ErrorMessage = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EditEndorsementRequiredField, szName), CssClass = "error", Display = ValidatorDisplay.Dynamic });
        }

        protected void UpdateFormForTemplate(int id, bool fResetTitle, string defaultText = null)
        {
            EndorsementType et = EndorsementType.GetEndorsementByID(id);
            if (et == null)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "EndorsementTemplate with ID={0} not found", id));

            plcTemplateForm.Controls.Clear();
            plcValidations.Controls.Clear();

            // don't change the title unless we're changing from a prior template.
            if (fResetTitle)
                txtTitle.Text = et.Title;
            lblEndorsementFAR.Text = HttpUtility.HtmlEncode(et.FARReference);

            // Find each of the substitutions
            Regex r = new Regex("\\{[^}]*\\}");
            MatchCollection matches = r.Matches(et.BodyTemplate);

            int cursor = 0;
            foreach (Match m in matches)
            {
                // compute the base ID for a control that we create here, before anything gets added, since the result depends on how many controls are in the placeholder already
                string idNewControl = ctlIDPrefix + plcTemplateForm.Controls.Count.ToString(CultureInfo.InvariantCulture);

                if (m.Index > cursor) // need to catch up on some literal text
                {
                    LiteralControl lt = new LiteralControl();
                    plcTemplateForm.Controls.Add(lt);
                    lt.Text = et.BodyTemplate.Substring(cursor, m.Index - cursor);
                    lt.ID = "ltCatchup" + idNewControl;
                }

                string szMatch = m.Captures[0].Value;

                switch (szMatch)
                {
                    case "{Date}":
                        {
                            Controls_mfbTypeInDate tid = (Controls_mfbTypeInDate)LoadControl("~/Controls/mfbTypeInDate.ascx");
                            plcTemplateForm.Controls.Add(tid);
                            tid.Date = DateTime.Now;
                            tid.ID = idNewControl;
                            tid.TextControl.BorderColor = System.Drawing.Color.Black;
                            tid.TextControl.BorderStyle = BorderStyle.Solid;
                            tid.TextControl.BorderWidth = Unit.Pixel(1);
                        }
                        break;
                    case "{FreeForm}":
                        NewTextBox(plcTemplateForm, idNewControl, defaultText ?? string.Empty, true, true, "Free-form text");
                        break;
                    case "{Student}":
                        NewTextBox(plcTemplateForm, idNewControl, TargetUser == null ? Resources.SignOff.EditEndorsementStudentNamePrompt : TargetUser.UserFullName, false, true, Resources.SignOff.EditEndorsementStudentNamePrompt);
                        break;
                    default:
                        // straight textbox, unless it is strings separated by slashes, in which case it's a drop-down
                        {
                            string szMatchInner = szMatch.Substring(1, szMatch.Length - 2);  // get the inside bits - i.e., strip off the curly braces
                            if (szMatchInner.Contains("/"))
                            {
                                string[] rgItems = szMatchInner.Split('/');
                                DropDownList dl = new DropDownList();
                                plcTemplateForm.Controls.Add(dl);
                                foreach (string szItem in rgItems)
                                    dl.Items.Add(new ListItem(szItem, szItem));
                                dl.ID = idNewControl;
                                dl.BorderColor = System.Drawing.Color.Black;
                                dl.BorderStyle = BorderStyle.Solid;
                                dl.BorderWidth = Unit.Pixel(1);
                            }
                            else
                                NewTextBox(plcTemplateForm, idNewControl, String.Empty, false, true, szMatchInner);
                        }
                        break;
                }

                cursor = m.Captures[0].Index + m.Captures[0].Length;
            }

            if (cursor < et.BodyTemplate.Length)
            {
                LiteralControl lt = new LiteralControl();
                plcTemplateForm.Controls.Add(lt);
                lt.Text = et.BodyTemplate.Substring(cursor);
                lt.ID = "ltEnding";
            }
        }

        protected string TemplateText()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Control c in plcTemplateForm.Controls)
            {
                if (c is LiteralControl l)
                    sb.Append(l.Text);
                else if (c is TextBox t)
                    sb.Append(t.Text);
                else if (c is Controls_mfbTypeInDate d)
                    sb.Append(d.Date.ToShortDateString());
                else if (c is DropDownList dd)
                    sb.Append(dd.SelectedValue);
            }

            return sb.ToString();
        }

        protected void btnAddEndorsement_Click(object sender, EventArgs e)
        {
            if (Page.IsValid && NewEndorsement != null)
            {
                Endorsement endorsement;

                switch (Mode)
                {
                    default:
                    case EndorsementMode.InstructorOfflineStudent:
                    case EndorsementMode.InstructorPushAuthenticated:
                        {
                            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
                            endorsement = new Endorsement(Page.User.Identity.Name) { CFICertificate = pf.Certificate, CFIExpirationDate = pf.CertificateExpiration };
                            if (!pf.CertificateExpiration.HasValue() && mfbDateCertExpiration.Date.HasValue())
                            {
                                endorsement.CFIExpirationDate = pf.CertificateExpiration = mfbDateCertExpiration.Date;
                                pf.FCommit();   // save it to the profile.
                            }
                        }
                        break;
                    case EndorsementMode.StudentPullAdHoc:
                        endorsement = new Endorsement(string.Empty) { CFICachedName = txtCFI.Text, CFICertificate = txtCFICert.Text, CFIExpirationDate = mfbDateCertExpiration.Date };
                        endorsement.SetDigitizedSig(mfbScribbleSignature.Base64Data());
                        if (endorsement.GetDigitizedSig() == null)
                            return;
                        break;
                    case EndorsementMode.StudentPullAuthenticated:
                        endorsement = new Endorsement(SourceUser.UserName) { CFICertificate = SourceUser.Certificate, CFIExpirationDate = SourceUser.CertificateExpiration };
                        break;
                }

                endorsement.Date = mfbTypeInDate1.Date;
                endorsement.EndorsementText = TemplateText();
                endorsement.StudentType = StudentType;
                endorsement.StudentName = StudentType == Endorsement.StudentTypes.Member ? TargetUser.UserName : txtOfflineStudent.Text;
                endorsement.Title = txtTitle.Text;
                endorsement.FARReference = lblEndorsementFAR.Text;

                NewEndorsement(this, new EndorsementEventArgs(endorsement));
            }
        }

        protected void valNoPostDate_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            // Don't allow any post dating, but allow 1 day of slop due to time zone
            if (DateTime.Now.AddDays(1).CompareTo(mfbTypeInDate1.Date) < 0)
                args.IsValid = false;
        }

        protected void valCorrectPassword_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (String.IsNullOrEmpty(txtPassConfirm.Text))
                return; // other validator will catch that
            if (SourceUser == null || !System.Web.Security.Membership.ValidateUser(SourceUser.UserName, txtPassConfirm.Text))
                args.IsValid = false;
        }
    }
}