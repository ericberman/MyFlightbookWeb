using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Achievements;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbBadgeSet : System.Web.UI.UserControl
{
    /// <summary>
    /// Binds to the subset of the specified list that matches the given category
    /// </summary>
    /// <param name="bc">The badge category, Unknown for all</param>
    /// <param name="lstSource">The master list of badges</param>
    public void ShowBadgesForCategory(Badge.BadgeCategory bc, List<Badge> lstSource)
    {
        lblCategory.Text = Badge.GetCategoryName(bc);
        List<Badge> lst = (bc == Badge.BadgeCategory.BadgeCategoryUnknown) ? lstSource : lstSource.FindAll(b => b.Category == bc);
        if (lst.Count == 0)
            pnlBadges.Visible = false;
        else
        {
            repeaterBadges.DataSource = lst;
            repeaterBadges.DataBind();
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}