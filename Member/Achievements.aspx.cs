using MyFlightbook;
using MyFlightbook.Achievements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Achievements : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.instAchievements;
        if (!IsPostBack)
        {
            lblAchievementsHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AchievementsForUserHeader, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName);
            if (util.GetIntParam(Request, "f", 0) != 0)
                ForceRefresh();

            RefreshPage();
            Master.ShowSponsoredAd = false;
        }
    }

    protected void RefreshPage()
    {
        List<Badge> lst = new Achievement(Page.User.Identity.Name).BadgesForUser();
        if (lst == null || lst.Count == 0)
            mvBadges.SetActiveView(vwNoBadges);
        else
        {
            mvBadges.SetActiveView(vwBadges);
            rptBadgeset.DataSource = BadgeSet.BadgeSetsFromBadges(lst);
            rptBadgeset.DataBind();
        }
        mfbRecentAchievements.AutoDateRange = true;
        mfbRecentAchievements.Refresh(Page.User.Identity.Name, DateTime.MaxValue, DateTime.MinValue, false);
    }

    protected void ForceRefresh()
    {
        MyFlightbook.Profile.GetUser(Page.User.Identity.Name).SetAchievementStatus(Achievement.ComputeStatus.NeedsComputing);
    }

    protected void lnkRecompute_Click(object sender, EventArgs e)
    {
        ForceRefresh();
        RefreshPage();
    }

    protected void lnkShowCalendar_Click(object sender, EventArgs e)
    {
        mfbRecentAchievements.ShowCalendar = true;
        lnkShowCalendar.Visible = false;
        RefreshPage();
    }
}