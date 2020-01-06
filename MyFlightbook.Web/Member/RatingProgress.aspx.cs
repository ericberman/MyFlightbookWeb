using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using MyFlightbook;
using MyFlightbook.Instruction;
using MyFlightbook.MilestoneProgress;

/******************************************************
 * 
 * Copyright (c) 2013-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_RatingProgress : System.Web.UI.Page
{
    private const string szVSMilestones = "MilestonesViewstateKey";
    private const string szVSTargetUser = "VSTargetUser";
    private const string szCookieLastGroup = "cookMilestoneLastGroup";
    private const string szCookieLastMilestone = "cookMilestoneLastMilestone";
    private const string szCommaCookieSub = "#COMMA#";

    protected IEnumerable<MilestoneGroup> Milestones
    {
        get
        {
            IEnumerable<MilestoneGroup> lstMilestones = (IEnumerable<MilestoneGroup>)ViewState[szVSMilestones];
            if (lstMilestones == null)
                ViewState[szVSMilestones] = lstMilestones = MilestoneProgress.AvailableProgressItems();
            return lstMilestones;
        }
    }

    protected string TargetUser
    {
        get
        {
            string szUser = (string) ViewState[szVSTargetUser];

            if (String.IsNullOrEmpty(szUser))
            {
                // Assume we're doing it for the target user; we can override below
                szUser = Page.User.Identity.Name;

                string szTargetUser = util.GetStringParam(Request, "user");
                if (!String.IsNullOrEmpty(szTargetUser))
                {
                    // Two scenarios:
                    // a) Support person (a=1 and current user is support)
                    // b) Instructor
                    // Check for admin
                    if (util.GetStringParam(Request, "a").Length > 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport)
                        szUser = szTargetUser;
                    else
                    {
                        CFIStudentMap csm = new CFIStudentMap(Page.User.Identity.Name);
                        foreach (InstructorStudent student in csm.Students)
                            if (String.Compare(student.UserName, szTargetUser, StringComparison.Ordinal) == 0 && student.CanViewLogbook)
                                szUser = szTargetUser;
                    }
                }
                ViewState[szVSTargetUser] = szUser;
            }
            return szUser;
        }
    }

    protected void ClearCookie()
    {
        Response.Cookies[szCookieLastGroup].Value = Response.Cookies[szCookieLastMilestone].Value = string.Empty;
    }

    protected void SetCookie()
    {
        Response.Cookies[szCookieLastGroup].Value = cmbMilestoneGroup.SelectedValue.Replace(",", szCommaCookieSub);
        Response.Cookies[szCookieLastMilestone].Value = cmbMilestones.SelectedValue.Replace(",", szCommaCookieSub);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.instProgressTowardsMilestones;

        if (!IsPostBack)
        {
            Master.ShowSponsoredAd = false;
            lblTitle.Text = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.PageTitle, MyFlightbook.Profile.GetUser(TargetUser).UserFullName);
            lblOverallProgressDisclaimer.Text = Branding.ReBrand(Resources.MilestoneProgress.OverallProgressDisclaimer);
            cmbMilestoneGroup.DataSource = Milestones;
            cmbMilestoneGroup.DataBind();

            HttpCookie cookieLastGroup = Request.Cookies[szCookieLastGroup];
            HttpCookie cookieLastMilestone = Request.Cookies[szCookieLastMilestone];
            if (cookieLastGroup != null && cookieLastMilestone != null && !String.IsNullOrEmpty(cookieLastGroup.Value) && !String.IsNullOrEmpty(cookieLastMilestone.Value))
            {
                try
                {
                    cmbMilestoneGroup.SelectedValue = cookieLastGroup.Value.Replace(szCommaCookieSub, ",");
                    cmbMilestoneGroup_SelectedIndexChanged(cmbMilestoneGroup, e);
                    cmbMilestones.SelectedValue = cookieLastMilestone.Value.Replace(szCommaCookieSub, ",");
                    Refresh();
                }
                catch (ArgumentOutOfRangeException)
                {
                    ClearCookie();
                }
            }
        }

    }

    protected void Refresh()
    {
        // Empty the page defensively
        gvMilestoneProgress.DataSource = null;
        gvMilestoneProgress.DataBind();

        pnlOverallProgress.Visible = false;
        ClearCookie();

        if (cmbMilestones.SelectedIndex == 0 || String.IsNullOrEmpty(cmbMilestoneGroup.SelectedValue) || Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0) == null)
            return;

        MilestoneGroup mgSel = Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0);

        if (mgSel == null || cmbMilestones.SelectedIndex > mgSel.Milestones.Length || cmbMilestones.SelectedIndex == 0)
            return;

        MilestoneProgress mp = mgSel.Milestones[cmbMilestones.SelectedIndex - 1];
        SetCookie();

        if (!mp.HasData)
            mp.Username = TargetUser;

        lblRatingOverallDisclaimer.Text = mp.GeneralDisclaimer;
        pnlRatingDisclaimer.Visible = !String.IsNullOrEmpty(lblRatingOverallDisclaimer.Text);

        gvMilestoneProgress.DataSource = mp.ComputedMilestones;
        gvMilestoneProgress.DataBind();

        pnlOverallProgress.Visible = true;
        int cMilestones = 0;
        int cMetMilestones = 0;
        foreach (MilestoneItem mi in mp.ComputedMilestones)
        {
            cMilestones++;
            if (mi.IsSatisfied)
                cMetMilestones++;
        }
        lblOverallProgress.Text = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.OverallProgressTemplate, cMetMilestones, cMilestones);
    }

    protected void cmbMilestones_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void gvMilestoneProgress_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            MilestoneItem mi = (MilestoneItem)e.Row.DataItem;
            MultiView mv = (MultiView) e.Row.FindControl("mvProgress");
            if (mi.Type == MilestoneItem.MilestoneType.AchieveOnce)
            {
                mv.ActiveViewIndex = 1;
                HyperLink l = (HyperLink)e.Row.FindControl("lnkFlight");
                Label lblNotMet = (Label)e.Row.FindControl("lblNotDone");
                l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Public/ViewPublicFlight.aspx/{0}", mi.MatchingEventID);

                MultiView mvAchievedStatus = (MultiView)e.Row.FindControl("mvAchievement");
                mvAchievedStatus.ActiveViewIndex = (mi.IsSatisfied) ? 0 : 1;
            }
            else
            {
                mv.ActiveViewIndex = 0;
                HtmlControl c = (HtmlControl) mv.FindControl("divPercent");
                int cappedPercentage = (int) Math.Min(mi.Percentage, 100);
                c.Style["width"] = String.Format(CultureInfo.InvariantCulture, "{0}%", cappedPercentage);
                c.Style["background-color"] = (cappedPercentage < 100) ? "#CCCCCC" : "limegreen";
            }

            Panel pnlNote = (Panel)e.Row.FindControl("pnlNote");
            pnlNote.Visible = !String.IsNullOrEmpty(mi.Note);
        }
    }

    protected void cmbMilestoneGroup_SelectedIndexChanged(object sender, EventArgs e)
    {
        cmbMilestones.Items.Clear();
        ListItem li = new ListItem(Resources.MilestoneProgress.RatingUnknown, String.Empty);
        li.Selected = true;
        cmbMilestones.Items.Add(li);

        MilestoneGroup mgSel = Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0);
        if (mgSel != null)
        {
            cmbMilestones.DataSource = mgSel.Milestones;
            cmbMilestones.DataBind();
        }

        Refresh();
    }
}