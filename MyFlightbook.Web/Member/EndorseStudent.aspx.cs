using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    public partial class EndorseStudent : Page
    {

        private const string keyTargetUser = "keyViewStateTargetUser";

        private string m_szTargetUser = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.tabTraining;
            Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

            pnlError.Visible = false;

            if (!IsPostBack)
            {
                try
                {
                    if (Request.PathInfo.Length > 0)
                        m_szTargetUser = Request.PathInfo.Substring(1);

                    bool fMemberEndorsement = util.GetIntParam(Request, "extern", 0) == 0;

                    if (m_szTargetUser.Length == 0 && fMemberEndorsement)
                    {
                        pnlEndorsement.Visible = false; // hide the new endorsement panel - just show all endorsements given
                    }
                    else
                    {
                        mfbEditEndorsement1.StudentType = fMemberEndorsement ? Endorsement.StudentTypes.Member : Endorsement.StudentTypes.External;

                        if (fMemberEndorsement)
                        {
                            CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
                            if (!sm.IsInstructorOf(m_szTargetUser))
                                throw new MyFlightbookValidationException(Resources.Profile.errNotAuthorizedToEndorse);
                            InstructorStudent student = CFIStudentMap.GetInstructorStudent(sm.Students, m_szTargetUser);
                            if (student.CanViewLogbook)
                            {
                                pnlNonOwnedEndorsements.Visible = true;
                                mfbNonOwnedEndorsements.Student = m_szTargetUser;
                                mfbNonOwnedEndorsements.ExcludeInstructor = Page.User.Identity.Name;
                                mfbNonOwnedEndorsements.RefreshEndorsements();

                                mfbIlNonOwnedEndorsementScans.ImageClass = Image.MFBImageInfoBase.ImageClass.Endorsement;
                                mfbIlNonOwnedEndorsementScans.Key = m_szTargetUser;
                                mfbIlNonOwnedEndorsementScans.Refresh();

                                // show unowned and uploaded endorsements as well
                                if (student.CanAddLogbook)
                                {
                                    // allow for upload of additional scanned endorsements.
                                    mfbMultiFileUpload.Visible = true;
                                    mfbMultiFileUpload.ImageKey = m_szTargetUser;
                                }
                            }

                            Profile pfTarget = Profile.GetUser(m_szTargetUser);
                            string szTargetUser = pfTarget.UserFullName;
                            string szEncodedUser = HttpUtility.HtmlEncode(szTargetUser);

                            lblPageHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EndorsementsHeader, szEncodedUser);
                            lblNewEndorsementHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EndorsementsNewEndorsementHeader, szEncodedUser);
                            lblExistingEndorsementHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EndorsementsExistingEndorsementHeader, szEncodedUser);
                            lblNonOwnedHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EndorsementsNotOwnedEndorsementHeader, szEncodedUser);
                            mfbEditEndorsement1.TargetUser = pfTarget;
                            mfbEditEndorsement1.Mode = EndorsementMode.InstructorPushAuthenticated;
                        }
                        else
                            mfbEditEndorsement1.Mode = EndorsementMode.InstructorOfflineStudent;

                        Profile pf = Profile.GetUser(User.Identity.Name);

                        if (pf.Certificate.Length == 0)
                            throw new MyFlightbookValidationException(Resources.Profile.errNoCFICertificate);

                        mfbEditEndorsement1.SourceUser = pf;

                        RefreshTemplateList();

                        ViewState[keyTargetUser] = m_szTargetUser;
                    }
                }
                catch (MyFlightbookValidationException ex)
                {
                    lblError.Text = ex.Message;
                    pnlError.Visible = true;
                    pnlEndorsement.Visible = false;
                    pnlExistingEndorsements.Visible = false;
                }
            }
            else
            {
                m_szTargetUser = (string)ViewState[keyTargetUser];
            }

            // need to reconstitute the form from the template every time to ensure postback works.
            if (!pnlError.Visible && pnlEndorsement.Visible)
                mfbEditEndorsement1.EndorsementID = Convert.ToInt32(hdnLastTemplate.Value, CultureInfo.InvariantCulture);

            mfbEndorsementList1.Instructor = Page.User.Identity.Name;
            mfbEndorsementList1.Student = m_szTargetUser;
            mfbEndorsementList1.RefreshEndorsements();

            if (mfbMultiFileUpload.Visible)
            {
                mfbMultiFileUpload.ProcessUploadedImages();
                if (mfbIlNonOwnedEndorsementScans.Visible)
                    mfbIlNonOwnedEndorsementScans.Refresh();
            }
        }

        protected void UpdateTemplate()
        {
            mfbEditEndorsement1.EndorsementID = Convert.ToInt32(cmbTemplates.SelectedValue, CultureInfo.InvariantCulture);
            hdnLastTemplate.Value = cmbTemplates.SelectedValue;
        }

        protected void cmbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTemplate();
        }

        public void OnNewEndorsement(object sender, EndorsementEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            try
            {
                e.Endorsement.FCommit();
                cmbTemplates.SelectedIndex = 0;
                mfbEditEndorsement1.EndorsementID = Convert.ToInt32(cmbTemplates.SelectedValue, CultureInfo.InvariantCulture);
                hdnLastTemplate.Value = cmbTemplates.SelectedValue.ToString(CultureInfo.InvariantCulture);

                mfbEndorsementList1.RefreshEndorsements();
            }
            catch (MyFlightbookException ex)
            {
                lblError.Text = ex.Message;
                pnlError.Visible = true;
            }
        }

        public void mfbEndorsementList1_CopyEndorsement(object sender, EndorsementEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            mfbEditEndorsement1.SetEndorsement(e.Endorsement);
        }

        protected void RefreshTemplateList()
        {
            List<EndorsementType> lst = new List<EndorsementType>(EndorsementType.LoadTemplates(mfbSearchTemplates.SearchText));
            if (lst.Count == 0) // if nothing found, use the custom template
                lst.Add(EndorsementType.GetEndorsementByID(1));

            cmbTemplates.DataSource = lst;
            cmbTemplates.DataValueField = "id";
            cmbTemplates.DataTextField = "FullTitle";
            cmbTemplates.DataBind();

            int currentEndorsementID = mfbEditEndorsement1.EndorsementID;
            if (lst.Find(et => et.ID == currentEndorsementID) == null)  // restricted list doesn't have active template - select the first one.
            {
                cmbTemplates.SelectedIndex = 0;
                UpdateTemplate();
            }
            else if (currentEndorsementID.ToString(CultureInfo.InvariantCulture) != cmbTemplates.SelectedValue)
                UpdateTemplate();

            hdnLastTemplate.Value = cmbTemplates.SelectedValue;
        }

        protected void mfbSearchbox_SearchClicked(object sender, EventArgs e)
        {
            RefreshTemplateList();
        }
    }
}