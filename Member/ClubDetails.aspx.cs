using MyFlightbook;
using MyFlightbook.Clubs;
using MyFlightbook.Instruction;
using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ClubDetails : System.Web.UI.Page
{
    /// <summary>
    /// The current club being viewed.  Actually delegates to the ViewClub control, since this saves it in its viewstate.
    /// </summary>
    protected Club CurrentClub
    {
        get { return ViewClub1.ActiveClub; }
        set { ViewClub1.ActiveClub = value; }
    }

    public string NowUTCInClubTZ
    {
        get
        {
            DateTime d = DateTime.UtcNow;
            Club c = CurrentClub;
            if (c != null)
                d = TimeZoneInfo.ConvertTimeFromUtc(d, c.TimeZone);
            return d.ToString("yyyy-MM-ddThh:mm:ss", CultureInfo.InvariantCulture);
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.actMyClubs;

        if (Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsPostBack)
            {
                try
                {
                    CurrentClub = Club.ClubWithID(Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture));
                    if (CurrentClub == null)
                        throw new MyFlightbookException(Resources.Club.errNoSuchClub);

                    Master.Title = CurrentClub.Name;
                    lblClubHeader.Text = CurrentClub.Name;

                    ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);

                    DateTime dtClub = ScheduledEvent.FromUTC(DateTime.UtcNow, CurrentClub.TimeZone);
                    lblCurTime.Text = String.Format(CultureInfo.InvariantCulture, Resources.LocalizedText.LocalizedJoinWithSpace, dtClub.ToShortDateString(), dtClub.ToShortTimeString());
                    lblTZDisclaimer.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.TimeZoneDisclaimer, CurrentClub.TimeZone.StandardName);

                    bool fIsAdmin = util.GetIntParam(Request, "a", 0) != 0 && (MyFlightbook.Profile.GetUser(Page.User.Identity.Name)).CanManageData;
                    if (fIsAdmin && cm == null)
                        cm = new ClubMember(CurrentClub.ID, Page.User.Identity.Name, ClubMember.ClubMemberRole.Admin);
                    bool fIsManager = fIsAdmin || (cm != null && cm.IsManager);
                    lnkManageClub.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/ClubManage.aspx/{0}", CurrentClub.ID);
                    mvMain.SetActiveView(cm == null ? vwMainGuest : vwSchedules);
                    mvTop.SetActiveView(cm == null ? vwTopGuest : (fIsManager ? vwTopAdmin : vwTopMember));
                    pnlLeaveGroup.Visible = (cm != null && !cm.IsManager);

                    switch (CurrentClub.Status)
                    {
                        case Club.ClubStatus.Promotional:
                            mvPromoStatus.SetActiveView(vwPromotional);
                            string szTemplate = (Page.User.Identity.Name.CompareOrdinal(Page.User.Identity.Name) == 0) ? Resources.Club.clubStatusTrialOwner : Resources.Club.clubStatusTrial;
                            lblPromo.Text = String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(szTemplate), CurrentClub.ExpirationDate.Value.ToShortDateString());
                            break;
                        case Club.ClubStatus.Expired:
                        case Club.ClubStatus.Inactive:
                            mvPromoStatus.SetActiveView(vwInactive);
                            lblInactive.Text = Branding.ReBrand(CurrentClub.Status == Club.ClubStatus.Inactive ? Resources.Club.errClubInactive : Resources.Club.errClubPromoExpired);
                            break;
                        default:
                            mvPromoStatus.Visible = false;
                            break;
                    }

                    // Initialize from the cookie, if possible.
                    rbScheduleMode.SelectedValue = SchedulePreferences.DefaultScheduleMode.ToString();

                    if (CurrentClub.PrependsScheduleWithOwnerName)
                        mfbEditAppt1.DefaultTitle = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName;

                    RefreshAircraft();
                }
                catch (MyFlightbookException ex)
                {
                    lblErr.Text = ex.Message;
                }
            }

            // Do this every time - if it's a postback in an update panel, it's a non-issue, but if it's full-page, this keeps things from going away.
            RefreshSummary();
        }
    }

    #region Guest
    protected void btnSendMessage_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
        {
            mpuGuestContact.Show();
            return;
        }

        Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        IEnumerable<ClubMember> lst = ClubMember.AdminsForClub(CurrentClub.ID);
        using (MailMessage msg = new MailMessage())
        {
            MailAddress maFrom = new MailAddress(pf.Email, pf.UserFullName);
            msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pf.UserFullName));
            msg.ReplyToList.Add(maFrom);
            foreach (ClubMember cm in lst)
                msg.To.Add(new MailAddress(cm.Email, cm.UserFullName));
            msg.Subject = String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Club.ContactSubjectTemplate), CurrentClub.Name);
            msg.Body = txtContact.Text + "\r\n\r\n" + String.Format(CultureInfo.CurrentCulture, Resources.Club.MessageSenderTemplate, pf.UserFullName, pf.Email);
            msg.IsBodyHtml = false;
            util.SendMessage(msg);
        }
        if (ckRequestMembership.Checked)
            foreach (ClubMember admin in lst)
                new CFIStudentMapRequest(Page.User.Identity.Name, admin.Email, CFIStudentMapRequest.RoleType.RoleRequestJoinClub, CurrentClub).Send();

        mpuGuestContact.Hide();
        txtContact.Text = string.Empty;
        ckRequestMembership.Checked = false;
        lblMessageStatus.Visible = true;
    }
    #endregion

    protected void lnkLeaveGroup_Click(object sender, EventArgs e)
    {
        // Find the current user in the club members.
        ClubMember cm = CurrentClub.Members.FirstOrDefault(pf => String.Compare(pf.UserName, Page.User.Identity.Name, StringComparison.Ordinal) == 0);
        if (cm.RoleInClub == ClubMember.ClubMemberRole.Member)
        {
            cm.FDeleteClubMembership();
            Club.ClearCachedClub(cm.ClubID);
            Response.Redirect(Request.Path);
        }
    }

    protected void RefreshAircraft()
    {
        ScheduleDisplayMode sdm = (ScheduleDisplayMode)Enum.Parse(typeof(ScheduleDisplayMode), rbScheduleMode.SelectedValue);
        SchedulePreferences.DefaultScheduleMode = sdm;

        IEnumerable<ClubAircraft> lstAc = CurrentClub.MemberAircraft;
        if (lstAc.Count() == 0)
        {
            mvClubAircraft.SetActiveView(vwNoAircraft);
            divCalendar.Visible = false;
        }
        else if (lstAc.Count() == 1)
        {
            divCalendar.Visible = true;
            mvClubAircraft.SetActiveView(vwOneAircraft);
            casSingleAircraft.Mode = sdm;
            casSingleAircraft.Aircraft = lstAc.ElementAt(0);
        }
        else
        {
            divCalendar.Visible = true;
            mvClubAircraft.SetActiveView(vwMultipleAircraft);
            tcAircraftSchedules.Tabs.Clear();
            foreach (ClubAircraft ac in lstAc)
            {
                AjaxControlToolkit.TabPanel tp = new AjaxControlToolkit.TabPanel();
                tcAircraftSchedules.Tabs.Add(tp);
                tp.HeaderText = ac.TailNumber;
                Controls_ClubControls_ClubAircraftSchedule cas = (Controls_ClubControls_ClubAircraftSchedule)LoadControl("~/Controls/ClubControls/ClubAircraftSchedule.ascx");
                cas.Mode = sdm;
                tp.CssClass = "mfbDefault";
                tp.ID = ac.AircraftID.ToString(CultureInfo.InvariantCulture);
                tp.Controls.Add(cas);
                cas.Aircraft = ac;
                tcAircraftSchedules.ActiveTabIndex = 0;
            }
        }
    }

    protected void rbScheduleMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        RefreshAircraft();
    }

    protected void RefreshSummary()
    {
        SchedSummary1.UserName = ckSummaryScope.Checked ? Page.User.Identity.Name : null;
        SchedSummary1.ClubID = CurrentClub.ID;
        SchedSummary1.ResourceName = null;
        SchedSummary1.Refresh();
    }

    protected void ckSummaryScope_CheckedChanged(object sender, EventArgs e)
    {
        RefreshSummary();
    }
}