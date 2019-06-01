using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Training : System.Web.UI.Page
{

    private CFIStudentMap m_sm = null;
    private Profile m_pf = null;

    protected void Page_Load(object sender, EventArgs e)
    {
        m_pf = MyFlightbook.Profile.GetUser(User.Identity.Name);

        if (!Request.IsSecureConnection && !Request.IsLocal)
            Response.Redirect(Request.Url.AbsoluteUri.Replace("http://", "https://"));

        lblName.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.TrainingHeader, m_pf.UserFullName);

        this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleTraining, Branding.CurrentBrand.AppName);

        // sidebar doesn't store it's state, so just set the currenttab each time.
        tabID sidebarTab = tabID.tabUnknown;

        string szPrefPath = String.IsNullOrWhiteSpace(Request.PathInfo) ? string.Empty : Request.PathInfo.Substring(1);
        string[] rgPrefPath = szPrefPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (rgPrefPath.Length > 0 && !String.IsNullOrEmpty(rgPrefPath[0]))
        {
            try { sidebarTab = (tabID)Enum.Parse(typeof(tabID), rgPrefPath[0]); }
            catch (ArgumentNullException) { }
            catch (ArgumentException) { }
            catch (OverflowException) { }
        }

        if (sidebarTab == tabID.tabUnknown)
            sidebarTab = tabID.instInstructors;

        this.Master.SelectedTab = sidebarTab;

        switch (sidebarTab)
        {
            case tabID.instEndorsements:
                mvProfile.SetActiveView(vwEndorsements);
                break;
            case tabID.instSignFlights:
                mvProfile.SetActiveView(vwSignFlights);
                break;
            case tabID.instStudents:
                mvProfile.SetActiveView(vwStudents);
                break;
            case tabID.instInstructors:
                mvProfile.SetActiveView(vwInstructors);
                break;
        }

        if (!IsPostBack)
        {
            switch (sidebarTab)
            {
                case tabID.instEndorsements:
                    if (Request.IsMobileDevice())
                        mfbIlEndorsements.Columns = 1;
                    mfbIlEndorsements.Key = User.Identity.Name;
                    mfbIlEndorsements.Refresh();

                    mfbEndorsementList1.Student = Page.User.Identity.Name;
                    mfbEndorsementList1.RefreshEndorsements();

                    if (!Request.IsMobileDeviceOrTablet())
                        mfbMultiFileUpload1.Mode = Controls_mfbMultiFileUpload.UploadMode.Ajax;

                    lnkPrintFriendly.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "{0}?naked=1&print=1", Request.Url.AbsolutePath);
                    if (util.GetIntParam(Request, "print", 0) != 0)
                    {
                        lnkPrintFriendly.Visible = mfbIlEndorsements.Visible = mfbMultiFileUpload1.Visible = false;
                        List<MyFlightbook.Image.MFBImageInfo> lstInlineImages = new List<MyFlightbook.Image.MFBImageInfo>(mfbIlEndorsements.Images.ImageArray);
                        lstInlineImages.RemoveAll(mfbii => mfbii.ImageType != MyFlightbook.Image.MFBImageInfo.ImageFileType.JPEG);
                        rptEndorsementImages.DataSource = lstInlineImages;
                        rptEndorsementImages.DataBind();
                    }
                    break;
                case tabID.instSignFlights:
                    RefreshFlightsToBeSigned();
                    break;
            }
        }

        // Need to do these on every page load to recreate links, etc.
        switch (sidebarTab)
        {
            case tabID.instSignFlights:
            case tabID.instStudents:
            case tabID.instInstructors:
            case tabID.instEndorsements:
                this.Master.ShowSponsoredAd = false;
                m_sm = new CFIStudentMap(User.Identity.Name);
                RefreshStudentsAndInstructors();
                break;
        }
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        // show an upload control in case the user switched from ajax to legacy upload
        btnUploadImages.Visible = mfbMultiFileUpload1.Mode == Controls_mfbMultiFileUpload.UploadMode.Legacy;
        base.OnLoadComplete(e);
    }


    #region Endorsements students and instructors
    protected void RefreshStudentsAndInstructors()
    {
        gvInstructors.DataSource = m_sm.Instructors;
        gvStudents.DataSource = m_sm.Students;
        pnlViewAllEndorsements.Visible = m_sm.Students.Count() > 0;
        gvInstructors.DataBind();
        gvStudents.DataBind();
    }

    protected void RefreshFlightsToBeSigned()
    {
        gvFlightsAwaitingSignatures.DataSource = LogbookEntry.PendingSignaturesForStudent(null, m_pf);
        gvFlightsAwaitingSignatures.DataBind();
    }

    protected void btnAddStudent_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;
        try
        {
            CFIStudentMapRequest smr = m_sm.GetRequest(CFIStudentMapRequest.RoleType.RoleStudent, txtStudentEmail.Text);
            smr.Send();
            lblAddStudentSuccess.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, txtStudentEmail.Text);
            lblAddStudentSuccess.CssClass = "success";
            txtStudentEmail.Text = "";
        }
        catch (MyFlightbookException ex)
        {
            lblAddStudentSuccess.Text = ex.Message;
            lblAddStudentSuccess.CssClass = "error";
        }
    }

    protected void btnAddInstructor_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;
        try
        {
            CFIStudentMapRequest smr = m_sm.GetRequest(CFIStudentMapRequest.RoleType.RoleCFI, txtInstructorEmail.Text);
            smr.Send();
            lblAddInstructorSuccess.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, txtInstructorEmail.Text);
            lblAddInstructorSuccess.CssClass = "success";
            txtInstructorEmail.Text = "";
        }
        catch (MyFlightbookException ex)
        {
            lblAddInstructorSuccess.Text = ex.Message;
            lblAddInstructorSuccess.CssClass = "error";
        }
    }

    protected void gvStudents_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        // Show link to view logbook IF can view student's book
        // set the checkbox to set the permission to view the logbook
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            // Get a list of pending un-signed flights for this student
            GridView gvPendingFlightsToSign = (GridView)e.Row.FindControl("gvPendingFlightsToSign");
            gvPendingFlightsToSign.DataSource = LogbookEntry.PendingSignaturesForStudent(m_pf, (InstructorStudent)e.Row.DataItem);
            gvPendingFlightsToSign.DataBind();
        }
    }

    protected void gvStudents_Delete(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            m_sm.RemoveStudent(e.CommandArgument.ToString());
            RefreshStudentsAndInstructors();
        }
    }

    protected void gvInstructors_Delete(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            m_sm.RemoveInstructor(e.CommandArgument.ToString());
            RefreshStudentsAndInstructors();
        }
    }

    protected void ProcessImages()
    {
        mfbIlEndorsements.Key = mfbMultiFileUpload1.ImageKey = User.Identity.Name;
        mfbMultiFileUpload1.ProcessUploadedImages();
        mfbIlEndorsements.Refresh();
    }

    protected void btnUploadImages_Click(object sender, EventArgs e)
    {
        ProcessImages();
    }

    protected void mfbMultiFileUpload1_OnUploadComplete(object sender, EventArgs e)
    {
        ProcessImages();
    }

    protected void LinkToPendingFlightDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            LogbookEntry le = (LogbookEntry)e.Row.DataItem;

            if (String.Compare(le.User, User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                HyperLink lnkPendingFlight = (HyperLink)e.Row.FindControl("lnkFlightToSign");
                lnkPendingFlight.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/SignFlight.aspx?idFlight={0}&ret={1}", le.FlightID, HttpUtility.UrlEncode(Request.Url.PathAndQuery));
            }

            ImageButton btnIgnore = (ImageButton)e.Row.FindControl("btnIgnore");
            btnIgnore.CommandArgument = le.FlightID.ToString(CultureInfo.InvariantCulture);
        }
    }

    protected void ClearSignature(int idFlight)
    {
        LogbookEntry le = new LogbookEntry() { FlightID = idFlight };
        le.ClearPendingSignature();
        RefreshStudentsAndInstructors();
    }

    protected void DeletePendingFlightSignatureForStudent(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.CommandName.CompareOrdinalIgnoreCase("Ignore") == 0)
        {
            ClearSignature(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
            RefreshStudentsAndInstructors();
        }
    }

    protected void DeletePendingFlightSignature(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.CommandName.CompareOrdinalIgnoreCase("Ignore") == 0)
        {
            ClearSignature(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
            RefreshFlightsToBeSigned();
        }
    }

    protected void ckCanViewLogbook_CheckedChanged(object sender, EventArgs e)
    {
        CheckBox ck = (CheckBox)sender;
        GridViewRow gvr = (GridViewRow)ck.NamingContainer;
        int iRow = gvr.RowIndex;

        if (iRow >= 0 && iRow < m_sm.Instructors.Count())
        {
            InstructorStudent instructorStudent = m_sm.Instructors.ElementAt(iRow);
            instructorStudent.CanViewLogbook = ck.Checked;
            if (!ck.Checked)
                instructorStudent.CanAddLogbook = false;
            m_sm.SetCFIPermissions(instructorStudent);
        }
        gvInstructors.DataSource = m_sm.Instructors;
        gvInstructors.DataBind();
    }

    protected void ckCanAddLogbook_CheckedChanged(object sender, EventArgs e)
    {
        CheckBox ck = (CheckBox)sender;
        GridViewRow gvr = (GridViewRow)ck.NamingContainer;
        int iRow = gvr.RowIndex;

        if (iRow >= 0 && iRow < m_sm.Instructors.Count())
        {
            InstructorStudent instructorStudent = m_sm.Instructors.ElementAt(iRow);
            instructorStudent.CanAddLogbook = ck.Checked;
            m_sm.SetCFIPermissions(instructorStudent);
        }
        gvInstructors.DataSource = m_sm.Instructors;
        gvInstructors.DataBind();
    }
    #endregion
}