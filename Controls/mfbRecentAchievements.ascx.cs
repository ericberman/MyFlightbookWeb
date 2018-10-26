using MyFlightbook;
using MyFlightbook.Achievements;
using MyFlightbook.MilestoneProgress;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbRecentAchievements : System.Web.UI.UserControl
{
    public void Refresh(string szUser, DateTime dtStart, DateTime dtEnd, bool fIncludeBadges)
    {
        if (szUser == null)
            throw new ArgumentNullException("szUser");
        if (String.IsNullOrWhiteSpace(szUser))
            throw new MyFlightbookValidationException("Invalid user");

        lblTitle.Text = String.Format(CultureInfo.CurrentCulture, Resources.Achievements.RecentAchievementsTitle, dtStart, dtEnd);

        RecentAchievements ra = new RecentAchievements(dtStart, dtEnd) { Username = szUser };
        Collection<MilestoneItem> c = ra.Refresh();
        rptRecentAchievements.DataSource = c;
        rptRecentAchievements.DataBind();

        List<Badge> lstBadges = null;
        if (fIncludeBadges)
        {
            lstBadges = new Achievement(szUser).BadgesForUser();
            lstBadges.RemoveAll(b => b.DateEarned.CompareTo(dtStart) < 0 || b.DateEarned.CompareTo(dtEnd) > 0);
            rptRecentlyearnedBadges.DataSource = BadgeSet.BadgeSetsFromBadges(lstBadges);
            rptRecentlyearnedBadges.DataBind();
        }

        pnlStatsAndAchievements.Visible = (lstBadges != null && lstBadges.Count > 0) || (c != null && c.Count() > 0);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}