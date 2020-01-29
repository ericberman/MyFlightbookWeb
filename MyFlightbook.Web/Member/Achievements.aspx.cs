using MyFlightbook;
using MyFlightbook.Achievements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2014-2020 MyFlightbook LLC
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
        ClientScript.RegisterClientScriptInclude("copytoClip", ResolveClientUrl("~/public/Scripts/CopyClipboard.js"));
        imgCopy.OnClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:copyClipboard('{0}', 'raContainer', false, '{1}');return false;", lblRecentAchievementsTitle.ClientID, lblCopied.ClientID);
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

        DateTime dtMin, dtMax = DateTime.Now;

        if (!Enum.TryParse<FlightQuery.DateRanges>(cmbAchievementDates.SelectedValue, out FlightQuery.DateRanges dr))
            throw new MyFlightbookValidationException("Invalid date range: " + cmbAchievementDates.SelectedValue);
        mfbRecentAchievements.AutoDateRange = false;

        switch (dr)
        {
            default:
            case FlightQuery.DateRanges.AllTime:
                mfbRecentAchievements.AutoDateRange = true;
                dtMin = DateTime.MaxValue;
                dtMax = DateTime.MinValue;
                break;
            case FlightQuery.DateRanges.PrevMonth:
                dtMax = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                dtMin = new DateTime(dtMax.Year, dtMax.Month, 1);
                break;
            case FlightQuery.DateRanges.PrevYear:
                dtMin = new DateTime(DateTime.Now.Year - 1, 1, 1);
                dtMax = new DateTime(DateTime.Now.Year - 1, 12, 31);
                break;
            case FlightQuery.DateRanges.Tailing6Months:
                dtMin = DateTime.Now.AddMonths(-6);
                break;
            case FlightQuery.DateRanges.ThisMonth:
                dtMin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                break;
            case FlightQuery.DateRanges.Trailing12Months:
                dtMin = DateTime.Now.AddYears(-1);
                break;
            case FlightQuery.DateRanges.Trailing30:
                dtMin = DateTime.Now.AddDays(-30);
                break;
            case FlightQuery.DateRanges.Trailing90:
                dtMin = DateTime.Now.AddDays(-90);
                break;
            case FlightQuery.DateRanges.YTD:
                dtMin = new DateTime(DateTime.Now.Year, 1, 1);
                break;
            case FlightQuery.DateRanges.Custom:
                dtMin = mfbTypeInDateFrom.Date;
                dtMax = mfbTypeInDateTo.Date;
                if (dtMax.CompareTo(dtMin) < 0)
                {
                    mfbRecentAchievements.AutoDateRange = true;
                    dtMin = DateTime.MaxValue;
                    dtMax = DateTime.MinValue;
                    cmbAchievementDates.SelectedValue = FlightQuery.DateRanges.AllTime.ToString();
                }
                break;

        }
        lblNoStats.Visible = mfbRecentAchievements.Refresh(Page.User.Identity.Name, dtMin, dtMax, false) == 0;
        lblRecentAchievementsTitle.Text = mfbRecentAchievements.Summary;
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

    protected void cmbAchievementDates_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbAchievementDates.SelectedValue.CompareCurrentCultureIgnoreCase("Custom") == 0)
            pnlCustomDates.Visible = true;
        else
        {
            pnlCustomDates.Visible = false;
            RefreshPage();
        }
    }

    protected void btnOK_Click(object sender, EventArgs e)
    {
        RefreshPage();
    }
}