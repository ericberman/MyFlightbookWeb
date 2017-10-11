using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Achievements;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
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
            pnlNoBadges.Visible = true;
        else
        {
            lst.Sort();
            foreach (Badge.BadgeCategory bc in (Badge.BadgeCategory[])Enum.GetValues(typeof(Badge.BadgeCategory)))
            {
                if (bc == Badge.BadgeCategory.BadgeCategoryUnknown)
                    continue;
                Controls_mfbBadgeSet bs = (Controls_mfbBadgeSet)LoadControl("~/Controls/mfbBadgeSet.ascx");
                bs.ShowBadgesForCategory(bc, lst);
                plcBadges.Controls.Add(bs);
            }
        }
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
}