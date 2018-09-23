using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_AddEndorsement : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleTraining, Branding.CurrentBrand.AppName);
        Master.SelectedTab = tabID.tabTraining;

        if (!IsPostBack)
        {
            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            mfbEditEndorsement1.StudentType = Endorsement.StudentTypes.External;
            mfbEditEndorsement1.TargetUser =  pf;
            cmbTemplates.DataSource = EndorsementType.LoadTemplates();
            cmbTemplates.DataValueField = "id";
            cmbTemplates.DataTextField = "FullTitle";
            cmbTemplates.DataBind();
            hdnLastTemplate.Value = cmbTemplates.SelectedValue;

            lblDisclaimerResponse.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement1);
            lblDisclaimerResponse2.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement2);

            CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
            if (sm.Instructors.Count() == 0)
            {
                mfbEditEndorsement1.Mode = EndorsementMode.StudentPullAdHoc;
                mvAddEndorsement.SetActiveView(vwAcceptTerms);
            }
            else
            {
                cmbInstructors.DataSource = sm.Instructors;
                cmbInstructors.DataBind();
                mvAddEndorsement.SetActiveView(vwPickInstructor);
            }

            mfbEditEndorsement1.StudentType = Endorsement.StudentTypes.Member;
            mfbEditEndorsement1.TargetUser = pf;
        }

        // need to reconstitute the form from the template every time to ensure postback works.
        mfbEditEndorsement1.EndorsementID = Convert.ToInt32(hdnLastTemplate.Value, CultureInfo.InvariantCulture);
    }

    protected void cmbTemplates_SelectedIndexChanged(object sender, EventArgs e)
    {
        mfbEditEndorsement1.EndorsementID = Convert.ToInt32(cmbTemplates.SelectedValue, CultureInfo.InvariantCulture);
        hdnLastTemplate.Value = cmbTemplates.SelectedValue;
    }

    protected void ChooseInstructor()
    {
        bool fIsNewInstructor = String.IsNullOrEmpty(cmbInstructors.SelectedValue);
        if (fIsNewInstructor)
        {
            mfbEditEndorsement1.Mode = EndorsementMode.StudentPullAdHoc;
            mvAddEndorsement.SetActiveView(vwAcceptTerms);
        }
        else
        {
            mfbEditEndorsement1.Mode = EndorsementMode.StudentPullAuthenticated;
            mfbEditEndorsement1.SourceUser = MyFlightbook.Profile.GetUser(cmbInstructors.SelectedValue);
            mvAddEndorsement.SetActiveView(vwEndorse);
        }
    }

    protected void cmbInstructors_SelectedIndexChanged(object sender, EventArgs e)
    {
        ChooseInstructor();
    }

    protected void btnAcceptResponsibility_Click(object sender, EventArgs e)
    {
        if (ckAccept.Checked)
            mvAddEndorsement.SetActiveView(vwEndorse);
        else
            lblErr.Text = Resources.SignOff.errAcceptDisclaimer;
    }

    protected void btnNewInstructor_Click(object sender, EventArgs e)
    {
        ChooseInstructor();
    }

    protected void mfbEditEndorsement1_NewEndorsement(object sender, EndorsementEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        try
        {
            e.Endorsement.FCommit();
            Response.Redirect("~/Member/Training.aspx/instEndorsements");
        }
        catch (MyFlightbookException ex)
        {
            lblErr.Text = ex.Message;
        }
    }
}