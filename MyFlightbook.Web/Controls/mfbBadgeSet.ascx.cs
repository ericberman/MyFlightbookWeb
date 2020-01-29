using MyFlightbook;
using MyFlightbook.Achievements;
using System;

/******************************************************
 * 
 * Copyright (c) 2014-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbBadgeSet : System.Web.UI.UserControl
{
    private BadgeSet m_badgeset = null;

    public BadgeSet BadgeSet
    {
        get { return m_badgeset; }
        set
        {
            m_badgeset = value;
            if (value != null)
            {
                lblCategory.Text = value.CategoryName;
                repeaterBadges.DataSource = value.Badges;
                repeaterBadges.DataBind();
            }
        }
    }

    public bool IsReadOnly { get; set; }

    protected void Page_Load(object sender, EventArgs e) {  }

    protected int ViewIndexForBadge(Badge b)
    {
        if (b == null)
            throw new ArgumentNullException(nameof(b));

        if (b.Level == Badge.AchievementLevel.None)
            return 0;
        else
            return (b.IDFlightEarned == LogbookEntry.idFlightNone || IsReadOnly) ? 1 : 2;
    }
}