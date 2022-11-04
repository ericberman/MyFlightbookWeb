using MyFlightbook.Instruction;
using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Controls;

/******************************************************
 * 
 * Copyright (c) 2014-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs
{
    public partial class ClubDetails : System.Web.UI.Page
    {
        #region WebServices
        /// <summary>
        /// Web service to send a message to a club user 
        /// <paramref name="szTarget">Name of the target user</paramref>
        /// <paramref name="szSubject">Subject</paramref>
        /// <paramref name="szText">Body of the message</paramref>
        /// </summary>
        [WebMethod(EnableSession = true)]
        public static void SendMsgToClubUser(string szTarget, string szSubject, string szText)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException();

            Club.ContactMember(HttpContext.Current.User.Identity.Name, szTarget, szSubject, szText);
        }

        /// <summary>
        /// Web service to contact the club if you are a guest
        /// </summary>
        [WebMethod(EnableSession = true)]
        public static void ContactClub(int idClub, string szMessage, bool fRequestMembership)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException();

            Club.ContactClubAdmins(HttpContext.Current.User.Identity.Name, idClub, szMessage, fRequestMembership);
        }
        #endregion

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

        protected void InitStatusDisplay()
        {
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
        }

        protected void InitDownload()
        {
            // set up for download.
            dateDownloadFrom.Date = DateTime.Now.Date;
            dateDownloadTo.Date = DateTime.Now.Date.AddMonths(12);
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

                        Master.Title = lblClubHeader.Text = HttpUtility.HtmlEncode(CurrentClub.Name);

                        ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);

                        DateTime dtClub = ScheduledEvent.FromUTC(DateTime.UtcNow, CurrentClub.TimeZone);
                        lblCurTime.Text = String.Format(CultureInfo.InvariantCulture, Resources.LocalizedText.LocalizedJoinWithSpace, dtClub.ToShortDateString(), dtClub.ToShortTimeString());
                        lblTZDisclaimer.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.TimeZoneDisclaimer, CurrentClub.TimeZone.StandardName);

                        bool fIsAdmin = util.GetIntParam(Request, "a", 0) != 0 && Profile.GetUser(Page.User.Identity.Name).CanManageData;
                        if (fIsAdmin && cm == null)
                            cm = new ClubMember(CurrentClub.ID, Page.User.Identity.Name, ClubMember.ClubMemberRole.Admin);
                        lnkManageClub.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/ClubManage.aspx/{0}", CurrentClub.ID);
                        mvMain.SetActiveView(cm == null ? vwMainGuest : vwSchedules);
                        pnlGuest.Visible = cm == null;
                        pnlManage.Visible = fIsAdmin || (cm != null && cm.IsManager);

                        if (cm == null)
                        {
                            accClub.SelectedIndex = 0;
                            acpMembers.Visible = acpSchedules.Visible = false;
                        }

                        pnlLeaveGroup.Visible = (cm != null && !cm.IsManager);

                        InitStatusDisplay();

                        // Initialize from the cookie, if possible.
                        rbScheduleMode.SelectedValue = SchedulePreferences.DefaultScheduleMode.ToString();

                        if (CurrentClub.PrependsScheduleWithOwnerName)
                            mfbEditAppt1.DefaultTitle = Profile.GetUser(Page.User.Identity.Name).UserFullName;

                        RefreshAircraft();

                        if (cm != null)
                        {
                            gvMembers.DataSource = CurrentClub.Members;
                            gvMembers.DataBind();
                            // Hack - trim the last column if not showing mobiles.  This is fragile.
                            if (CurrentClub.HideMobileNumbers)
                                gvMembers.Columns[gvMembers.Columns.Count - 1].Visible = false;
                        }

                        InitDownload();
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
            if (!lstAc.Any())
            {
                mvClubAircraft.SetActiveView(vwNoAircraft);
                pnlAvailMap.Style["display"] = "none";
                divCalendar.Visible = false;
            }
            else if (lstAc.Count() == 1)
            {
                divCalendar.Visible = true;
                pnlAvailMap.Style["display"] = "none";
                mvClubAircraft.SetActiveView(vwOneAircraft);
                casSingleAircraft.Mode = sdm;
                casSingleAircraft.Aircraft = lstAc.ElementAt(0);
            }
            else
            {
                divCalendar.Visible = true;
                pnlAvailMap.Style["display"] = "block";
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
            if (CurrentClub == null)
                return;
            SchedSummary1.UserName = ckSummaryScope.Checked ? Page.User.Identity.Name : null;
            SchedSummary1.ClubID = CurrentClub.ID;
            SchedSummary1.ResourceName = null;
            SchedSummary1.Refresh();
        }

        protected void ckSummaryScope_CheckedChanged(object sender, EventArgs e)
        {
            RefreshSummary();
        }

        protected void btnDownloadSchedule_Click(object sender, EventArgs e)
        {
            if (dateDownloadTo.Date.Subtract(dateDownloadFrom.Date).TotalDays < 1)
                lblDownloadErr.Text = Resources.Club.DownloadClubScheduleBadDateRange;
            else
            {
                IEnumerable<ScheduledEvent> rgevents = ScheduledEvent.AppointmentsInTimeRange(dateDownloadFrom.Date, dateDownloadTo.Date, CurrentClub.ID, CurrentClub.TimeZone);
                CurrentClub.MapAircraftAndUsers(rgevents);  // fix up aircraft, usernames
                gvScheduleDownload.DataSource = rgevents;
                gvScheduleDownload.DataBind();

                Response.Clear();
                Response.ContentType = "text/csv";
                // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
                string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.Club.DownloadClubScheduleFileName, CurrentClub.Name.Replace(" ", "-"));
                string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
                Response.AddHeader("Content-Disposition", szDisposition);
                gvScheduleDownload.ToCSV(Response.OutputStream);
                Response.End();
            }
        }
    } 
}