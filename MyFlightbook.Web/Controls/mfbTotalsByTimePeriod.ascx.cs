using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls
{
    public partial class mfbTotalsByTimePeriod : System.Web.UI.UserControl
    {
        protected bool UseHHMM { get; set; }

        protected bool Past7Days { get; set; }

        protected bool MonthToDate { get; set; }

        protected bool PreviousMonth { get; set; }

        protected bool PreviousYear { get; set; }

        protected bool YearToDate { get; set; }

        protected int ColumnCount { get; set; }


        protected void Page_Load(object sender, EventArgs e)
        {

        }

        private static Dictionary<string, TotalsItem> TotalsForQuery(FlightQuery fq, bool fBind)
        {
            Dictionary<string, TotalsItem> d = new Dictionary<string, TotalsItem>();

            if (fBind)
            {
                UserTotals ut = new UserTotals(fq.UserName, fq, true);
                ut.DataBind();

                foreach (TotalsItem ti in ut.Totals)
                    d[ti.Description] = ti;
            }

            return d;
        }

        private static void AddTextCellToRow(TableRow tr, string szContent, bool fInclude = true, string cssClass = null)
        {
            if (fInclude)
            {
                TableCell tc = new TableCell();
                tr.Cells.Add(tc);
                tc.Text = szContent;
                if (cssClass != null)
                    tc.CssClass = cssClass;
            }
        }

        private void AddCellForTotalsItem(TableRow tr, TotalsItem ti, bool fInclude)
        {
            if (!fInclude)
                return;

            TableCell tc = new TableCell();
            tr.Cells.Add(tc);
            tc.CssClass = "totalsByTimeCell";

            // Empty totals item = empty cell.
            if (ti == null)
                return;

            // Otherwise, add the cell to the table, following the design in mfbTotalSummary
            // Link the *value* here, not the description, since we will have multiple columns
            // Add the values div (panel) to the totals box
            if (ti.Query == null)
                tc.Controls.Add(new Label() { Text = ti.ValueString(UseHHMM) });
            else
                tc.Controls.Add(new HyperLink()
                {
                    Text = ti.ValueString(UseHHMM),
                    NavigateUrl = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}",
                                                    Branding.CurrentBrand.HostName,
                                                    ResolveUrl("~/Member/LogbookNew.aspx?fq=" + HttpUtility.UrlEncode(ti.Query.ToBase64CompressedJSONString())))
                });

            Panel p = new Panel();
            tc.Controls.Add(p);
            Label l = new Label() { CssClass = "fineprint", Text = ti.SubDescription };
            p.Controls.Add(l);
        }

        public void BindTotalsForUser(string szUser, bool fLast7Days, bool fMonthToDate, bool fPreviousMonth, bool fPreviousYear, bool fYearToDate, FlightQuery fqSupplied = null)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            UseHHMM = pf.UsesHHMM;

            // Get All time totals.  This will also give us the entire space of totals items
            FlightQuery fq = fqSupplied == null ? new FlightQuery(szUser) : new FlightQuery(fqSupplied);
            UserTotals ut = new UserTotals(szUser, fq, true);
            ut.DataBind();

            IEnumerable<TotalsItemCollection> allTotals = TotalsItemCollection.AsGroups(ut.Totals);

            // if the supplied query has a date range, then don't do any of the subsequent queries; the date range overrides.
            bool fSuppliedQueryHasDates = fqSupplied != null && fqSupplied.DateRange != FlightQuery.DateRanges.AllTime;
            if (fSuppliedQueryHasDates)
                fLast7Days = fMonthToDate = fPreviousMonth = fPreviousYear = fYearToDate = false;

            // Get grouped totals for each of the requested time periods.
            fq.DateRange = FlightQuery.DateRanges.ThisMonth;
            Dictionary<string, TotalsItem> dMonthToDate = TotalsForQuery(fq, fMonthToDate);
            fq.DateRange = FlightQuery.DateRanges.PrevMonth;
            Dictionary<string, TotalsItem> dPrevMonth = TotalsForQuery(fq, fPreviousMonth);
            fq.DateRange = FlightQuery.DateRanges.YTD;
            Dictionary<string, TotalsItem> dYTD = TotalsForQuery(fq, fYearToDate);
            fq.DateRange = FlightQuery.DateRanges.PrevYear;
            Dictionary<string, TotalsItem> dPrevYear = TotalsForQuery(fq, fPreviousYear);
            fq.DateRange = FlightQuery.DateRanges.Custom;
            fq.DateMin = DateTime.Now.Date.AddDays(-7);
            fq.DateMax = DateTime.Now.Date.AddDays(1);
            Dictionary<string, TotalsItem> dLast7 = TotalsForQuery(fq, fLast7Days);

            tblTotals.Controls.Clear();

            // Determine which columns we'll show
            ColumnCount = 2;   // All time is always shown, as are its labels (in adjacent table column)
            if (fLast7Days &= dLast7.Any())
                ColumnCount++;
            if (fMonthToDate &= dMonthToDate.Any())
                ColumnCount++;
            if (fPreviousMonth &= dPrevMonth.Any())
                ColumnCount++;
            if (fPreviousYear &= dPrevYear.Any())
                ColumnCount++;
            if (fYearToDate &= dYTD.Any())
                ColumnCount++;

            mvTotals.SetActiveView(allTotals.Any() ? vwTotals : vwNoTotals);

            string szPreviousMonth = DateTime.Now.AddCalendarMonths(-1).ToString("MMM yyyy", CultureInfo.CurrentCulture);
            string szPreviousYear = (DateTime.Now.Year - 1).ToString(CultureInfo.CurrentCulture);

            foreach (TotalsItemCollection tic in allTotals)
            {
                TableRow trGroup = new TableRow();
                tblTotals.Rows.Add(trGroup);
                TableCell tcGroup = new TableCell() { ColumnSpan = ColumnCount, Text = tic.GroupName, CssClass = "totalsGroupHeaderCell" };
                trGroup.Cells.Add(tcGroup);

                TableRow trHeader = new TableRow();
                tblTotals.Rows.Add(trHeader);
                const string cssDateRange = "totalsDateRange";
                AddTextCellToRow(trHeader, string.Empty, true); // no header above the total description itself.
                AddTextCellToRow(trHeader, fSuppliedQueryHasDates ? string.Empty : Resources.FlightQuery.DatesAll, true, cssDateRange);
                AddTextCellToRow(trHeader, Resources.Profile.EmailWeeklyTotalsLabel, fLast7Days, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesThisMonth, fMonthToDate, cssDateRange);
                AddTextCellToRow(trHeader, szPreviousMonth, fPreviousMonth, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesYearToDate, fYearToDate, cssDateRange);
                AddTextCellToRow(trHeader, szPreviousYear, fPreviousYear, cssDateRange);

                foreach (TotalsItem ti in tic.Items)
                {
                    TableRow tr = new TableRow();
                    tblTotals.Rows.Add(tr);

                    // Add the description
                    tr.Cells.Add(new TableCell() { Text = ti.Description });

                    AddCellForTotalsItem(tr, ti, true);
                    AddCellForTotalsItem(tr, dLast7.ContainsKey(ti.Description) ? dLast7[ti.Description] : null, fLast7Days);
                    AddCellForTotalsItem(tr, dMonthToDate.ContainsKey(ti.Description) ? dMonthToDate[ti.Description] : null, fMonthToDate);
                    AddCellForTotalsItem(tr, dPrevMonth.ContainsKey(ti.Description) ? dPrevMonth[ti.Description] : null, fPreviousMonth);
                    AddCellForTotalsItem(tr, dYTD.ContainsKey(ti.Description) ? dYTD[ti.Description] : null, fYearToDate);
                    AddCellForTotalsItem(tr, dPrevYear.ContainsKey(ti.Description) ? dPrevYear[ti.Description] : null, fPreviousYear);
                }
            }
        }
    }
}