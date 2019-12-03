using MyFlightbook;
using MyFlightbook.Achievements;
using MyFlightbook.MilestoneProgress;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
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
    /// True to show the calendar of flying
    /// </summary>
    public bool ShowCalendar
    {
        get { return pnlCal.Visible; }
        set { pnlCal.Visible = value; }
    }

    /// <summary>
    /// Number of achievements found
    /// </summary>
    public int AchievementCount { get; private set; }

    /// <summary>
    /// Description of the recent achievements, including date range
    /// </summary>
    public string Summary { get; private set; }
    #endregion

    #region Calendar drawing
    protected void AddDayCell(TableRow parent, DateTime dt, int value, string cssCell)
    {
        TableCell tc = new TableCell() { CssClass = cssCell };
        parent.Cells.Add(tc);
        tc.Controls.Add(new Label() { Text = dt.Day.ToString(CultureInfo.CurrentCulture), CssClass = "dayOfMonth" });

        if (value == 0)
            tc.Controls.Add(new Label() { Text = Resources.LocalizedText.NonBreakingSpace, CssClass = "dateContent" });
        else
        {
            FlightQuery fq = new FlightQuery(Page.User.Identity.Name) { DateRange = FlightQuery.DateRanges.Custom, DateMax = dt.Date, DateMin = dt.Date };
            tc.Controls.Add(new HyperLink()
            {
                Text = value.ToString(CultureInfo.CurrentCulture),
                CssClass = "dateContent dateContentValue",
                NavigateUrl = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}{2}", Branding.CurrentBrand.HostName, ResolveUrl("~/Member/LogbookNew.aspx?fq="), HttpUtility.UrlEncode(fq.ToBase64CompressedJSONString()))
            });
        }
    }

    protected void AddMonth(DateTime dt, WebControl parent, RecentAchievements ra)
    {
        DateTime dtDay = new DateTime(dt.Year, dt.Month, 1);
        DateTime dtNextMonth = dtDay.AddMonths(1);

        while (dtDay.DayOfWeek > CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)
            dtDay = dtDay.AddDays(-1);

        Table table = new Table();
        parent.Controls.Add(table);
        TableRow trMonth = new TableRow();
        table.Rows.Add(trMonth);
        trMonth.Cells.Add(new TableCell() { ColumnSpan = 7, Text = dt.ToString("MMMM - yyyy", CultureInfo.CurrentCulture), CssClass = "monthHeader" });

        TableRow trWeek = new TableRow();
        table.Rows.Add(trWeek);

        int iDayOfWeek = 0;
        // Add each of the prior-month days
        while (dtDay.Day > 1)
        {
            AddDayCell(trWeek, dtDay, 0, "adjacentDay");
            dtDay = dtDay.AddDays(1);
            iDayOfWeek++;
        }

        // Now add each of this month's days to the row
        while (dtDay.CompareTo(dtNextMonth) < 0)
        {
            while (iDayOfWeek++ < 7 && dtDay.CompareTo(dtNextMonth) < 0)
            {
                int cFlights = ra.FlightCountOnDate(dtDay);
                AddDayCell(trWeek, dtDay, cFlights, "includedDay");
                dtDay = dtDay.AddDays(1);
            }

            if (dtDay.Day == 1 || dtDay.CompareTo(dtNextMonth) >= 0)
                break;

            if (iDayOfWeek >= 7)
            {
                iDayOfWeek = 0;
                trWeek = new TableRow();
                table.Rows.Add(trWeek);
            }
        }

        // Now add each of the days of the next month to round things out.
        while (trWeek.Cells.Count < 7)
        {
            AddDayCell(trWeek, dtDay, 0, "adjacentDay");
            dtDay = dtDay.AddDays(1);
        }
    }

    protected void RefreshCalendar(RecentAchievements ra)
    {
        DateTime dtMonthStart = new DateTime(ra.StartDate.Year, ra.StartDate.Month, 1);
        DateTime dtMonthEnd = new DateTime(ra.EndDate.Year, ra.EndDate.Month, 1);

        plcFlyingCalendar.Controls.Add(new Literal() { Text = String.Format(CultureInfo.InvariantCulture, "<h2>{0}</h2>", Resources.Achievements.RecentAchievementsCalendarHeader) });

        for (DateTime dtCurrentMonth = dtMonthStart; dtCurrentMonth.CompareTo(dtMonthEnd) <= 0; dtCurrentMonth = dtCurrentMonth.AddMonths(1))
        {
            Panel pMonth = new Panel();
            plcFlyingCalendar.Controls.Add(pMonth);
            pMonth.CssClass = "monthContainer";
            AddMonth(dtCurrentMonth, pMonth, ra);
        }
    }
    #endregion

    public int Refresh(string szUser, DateTime dtStart, DateTime dtEnd, bool fIncludeBadges)
    {
        if (szUser == null)
            throw new ArgumentNullException("szUser");
        if (String.IsNullOrWhiteSpace(szUser))
            throw new MyFlightbookValidationException("Invalid user");

        RecentAchievements ra = new RecentAchievements(dtStart, dtEnd) { Username = szUser, AutoDateRange = AutoDateRange };
        Collection<MilestoneItem> c = ra.Refresh();
        rptRecentAchievements.DataSource = c;
        rptRecentAchievements.DataBind();

        AchievementCount = c.Count();

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

        pnlStatsAndAchievements.Visible = (lstBadges != null && lstBadges.Count > 0) || (c != null && c.Count() > 0);
        if (ShowCalendar)
            RefreshCalendar(ra);

        return AchievementCount;
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}