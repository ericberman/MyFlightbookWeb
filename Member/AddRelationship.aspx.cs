using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Instruction;
using MyFlightbook.Clubs;
using System.Net.Mail;
using System.Globalization;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_AddRelationship : System.Web.UI.Page
{
    private CFIStudentMapRequest m_smr = null;
    private CFIStudentMap m_sm = null;

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.tabTraining;
        this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

        string szReq = util.GetStringParam(Request, "req");

        try
        {
            if (szReq.Length == 0)
                throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidRequest);

            m_smr = new CFIStudentMapRequest();
            m_smr.DecryptRequest(szReq);

            MyFlightbook.Profile pfRequestor = MyFlightbook.Profile.GetUser(m_smr.RequestingUser);
            if (!pfRequestor.IsValid())
                throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidUser);

            MyFlightbook.Profile pfCurrent = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (String.Compare(m_smr.TargetUser, pfCurrent.Email, StringComparison.OrdinalIgnoreCase) != 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrNotTargetUser, m_smr.TargetUser, pfCurrent.Email));

            m_sm = new CFIStudentMap(User.Identity.Name);

            switch (m_smr.Requestedrole)
            {
                case CFIStudentMapRequest.RoleType.RoleStudent:
                    lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestStudent, Branding.CurrentBrand.AppName, pfRequestor.UserFullName);
                    if (m_sm.IsStudentOf(m_smr.RequestingUser))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyStudent, pfRequestor.UserFullName));
                    break;
                case CFIStudentMapRequest.RoleType.RoleCFI:
                    lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestInstructor, Branding.CurrentBrand.AppName, pfRequestor.UserFullName);
                    if (m_sm.IsInstructorOf(m_smr.RequestingUser))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyInstructor, pfRequestor.UserFullName));
                    break;
                case CFIStudentMapRequest.RoleType.RoleInviteJoinClub:
                    if (m_smr.ClubToJoin == null)
                        throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                    if (m_smr.ClubToJoin.HasMember(pfCurrent.UserName))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyMember, m_smr.ClubToJoin.Name));
                    lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromInvitation, m_smr.ClubToJoin.Name);
                    break;
                case CFIStudentMapRequest.RoleType.RoleRequestJoinClub:
                    if (m_smr.ClubToJoin == null)
                        throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                    if (m_smr.ClubToJoin.HasMember(pfRequestor.UserName))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyAddedMember, pfRequestor.UserFullName, m_smr.ClubToJoin.Name));
                    lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromRequest, pfRequestor.UserFullName, m_smr.ClubToJoin.Name);
                    break;
            }
        }
        catch (MyFlightbookValidationException ex)
        {
            pnlReviewRequest.Visible = false;
            lblError.Text = ex.Message;
            pnlConfirm.Visible = false;
        }
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Default.aspx");
    }

    protected void btnConfirm_Click(object sender, EventArgs e)
    {
        try
        {
            m_sm.ExecuteRequest(m_smr);
            switch (m_smr.Requestedrole)
            {
                case CFIStudentMapRequest.RoleType.RoleCFI:
                case CFIStudentMapRequest.RoleType.RoleStudent:
                    Response.Redirect("~/Member/Training.aspx/" + (m_smr.Requestedrole == CFIStudentMapRequest.RoleType.RoleCFI ? tabID.instStudents.ToString() : tabID.instInstructors.ToString()));
                    break;
                case CFIStudentMapRequest.RoleType.RoleInviteJoinClub:
                    {
                        // Let the requestor know that the invitation has been accepted.
                        Profile pfTarget = MyFlightbook.Profile.GetUser(m_smr.TargetUser.Contains("@") ? System.Web.Security.Membership.GetUserNameByEmail(m_smr.TargetUser) : m_smr.TargetUser);
                        string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberInvitationAccepted, m_smr.ClubToJoin.Name);
                        string szBody = Branding.ReBrand(Resources.Club.ClubInvitationAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% ClubInvitee %>", pfTarget.UserFullName);
                        foreach (ClubMember cm in ClubMember.AdminsForClub(m_smr.ClubToJoin.ID))
                            util.NotifyUser(szSubject, szBody.Replace("<% FullName %>", cm.UserFullName), new MailAddress(cm.Email, cm.UserFullName), false, false);
                        Response.Redirect("~/Member/ClubDetails.aspx/" + m_smr.ClubToJoin.ID);
                    }
                    break;
                case CFIStudentMapRequest.RoleType.RoleRequestJoinClub:
                    {
                        // Let the requestor know that the request has been approved.
                        Profile pfRequestor = MyFlightbook.Profile.GetUser(m_smr.RequestingUser);
                        string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberRequestAccepted, m_smr.ClubToJoin.Name);
                        string szBody = Branding.ReBrand(Resources.Club.ClubRequestAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% FullName %>", pfRequestor.UserFullName);
                        util.NotifyUser(szSubject, szBody, new MailAddress(pfRequestor.Email, pfRequestor.UserFullName), false, false);
                        Response.Redirect("~/Member/ClubDetails.aspx/" + m_smr.ClubToJoin.ID);
                    }
                    break;
            }
        }
        catch (MyFlightbookValidationException ex)
        {
            pnlReviewRequest.Visible = false;
            lblError.Text = ex.Message;
            pnlConfirm.Visible = false;
        }
    }
}