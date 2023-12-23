using MyFlightbook;
using MyFlightbook.Achievements;
using MyFlightbook.RatingsProgress;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2018-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbRecentAchievements : System.Web.UI.UserControl
{
    #region Properties
    /// <summary>
    /// True if the date range should be computed from your flying.
    /// </summary>
    public bool AutoDateRange { get; set; }

    /// <summary>
    /// Number of achievements found
    /// </summary>
    public int AchievementCount { get; private set; }

    /// <summary>
    /// Description of the recent achievements, including date range
    /// </summary>
    public string Summary { get; private set; }

    public bool IsReadOnly { get; set; }
    #endregion

    public int Refresh(string szUser, DateTime dtStart, DateTime dtEnd, bool fIncludeBadges)
    {
        if (szUser == null)
            throw new ArgumentNullException(nameof(szUser));
        if (String.IsNullOrWhiteSpace(szUser))
            throw new MyFlightbookValidationException("Invalid user");

        RecentAchievements ra = new RecentAchievements(dtStart, dtEnd) { Username = szUser, AutoDateRange = AutoDateRange };
        Collection<MilestoneItem> c = ra.Refresh();
        rptRecentAchievements.DataSource = c;
        rptRecentAchievements.DataBind();

        AchievementCount = c.Count;

        Summary = String.Format(CultureInfo.CurrentCulture, fIncludeBadges ? Resources.Achievements.RecentAchievementsTitle : Resources.Achievements.RecentStatsTitle, ra.StartDate, ra.EndDate);

        List<Badge> lstBadges = null;
        if (fIncludeBadges)
        {
            lstBadges = new Achievement(szUser).BadgesForUser();
            lstBadges.RemoveAll(b => b.DateEarned.CompareTo(dtStart) < 0 || b.DateEarned.CompareTo(dtEnd) > 0);
            AchievementCount += lstBadges.Count;
            rptRecentlyearnedBadges.DataSource = BadgeSet.BadgeSetsFromBadges(lstBadges);
            rptRecentlyearnedBadges.DataBind();
        }

        pnlStatsAndAchievements.Visible = (lstBadges != null && lstBadges.Count > 0) || (c != null && c.Count > 0);

        return AchievementCount;
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}