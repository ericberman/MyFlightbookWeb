using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public partial class mfbTotalsByTimePeriod : System.Web.UI.UserControl
    {
        protected bool UseHHMM { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {

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

        private decimal AddCellForTotalsItem(TableRow tr, TotalsItem ti, bool fInclude, bool linkQuery)
        {
            if (!fInclude)
                return 0.0M;

            TableCell tc = new TableCell();
            tr.Cells.Add(tc);
            tc.CssClass = "totalsByTimeCell";

            // Empty totals item = empty cell.
            if (ti == null)
                return 0.0M;

            string szText = HttpUtility.HtmlEncode(ti.ValueString(UseHHMM));

            // Otherwise, add the cell to the table, following the design in mfbTotalSummary
            // Link the *value* here, not the description, since we will have multiple columns
            // Add the values div (panel) to the totals box
            if (ti.Query == null || !linkQuery)
                tc.Controls.Add(new Label() { Text = szText });
            else
                tc.Controls.Add(new HyperLink()
                {
                    Text = szText,
                    NavigateUrl = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}",
                                                    Branding.CurrentBrand.HostName,
                                                    ResolveUrl("~/Member/LogbookNew.aspx?fq=" + HttpUtility.UrlEncode(ti.Query.ToBase64CompressedJSONString())))
                });

            Panel p = new Panel();
            tc.Controls.Add(p);
            Label l = new Label() { CssClass = "fineprint", Text = HttpUtility.HtmlEncode(ti.SubDescription) };
            p.Controls.Add(l);

            return ti.Value;    // Indicate if we actually had a non-zero value.  A row of all empty cells or zero cells should be deleted.
        }

        /// <summary>
        /// Reconstructs the table for the built-in rollup.
        /// </summary>
        /// <param name="rollup"></param>
        public void RefreshTable(TimeRollup r, bool fLinkQuery)
        {
            tblTotals.Controls.Clear();

            TimeRollup Rollup = r;

            if (Rollup == null)
                return;

            Profile pf = Profile.GetUser(Rollup.User);
            UseHHMM = pf.UsesHHMM;

            // Determine which columns we'll show
            int ColumnCount = 2;   // All time is always shown, as are its labels (in adjacent table column)
            if (Rollup.IncludeLast7Days &= Rollup.Last7.Any())
                ColumnCount++;
            if (Rollup.IncludeMonthToDate &= Rollup.MonthToDate.Any())
                ColumnCount++;
            if (Rollup.IncludePreviousMonth &= Rollup.PrevMonth.Any())
                ColumnCount++;
            if (Rollup.IncludeTrailing12 &= Rollup.Trailing12.Any())
                ColumnCount++;
            if (Rollup.IncludeTrailing24 &= Rollup.Trailing24.Any())
                ColumnCount++;
            if (Rollup.IncludePreviousYear &= Rollup.PrevYear.Any())
                ColumnCount++;
            if (Rollup.IncludeYearToDate &= Rollup.YTD.Any())
                ColumnCount++;

            mvTotals.SetActiveView(Rollup.allTotals.Any() ? vwTotals : vwNoTotals);

            string szPreviousMonth = DateTime.Now.AddCalendarMonths(-1).ToString("MMM yyyy", CultureInfo.CurrentCulture);
            string szPreviousYear = (DateTime.Now.Year - 1).ToString(CultureInfo.CurrentCulture);

            foreach (TotalsItemCollection tic in Rollup.allTotals)
            {
                TableRow trGroup = new TableRow() { CssClass = "totalsGroupHeaderRow" };
                tblTotals.Rows.Add(trGroup);
                TableCell tcGroup = new TableCell() { ColumnSpan = ColumnCount, Text = HttpUtility.HtmlEncode(tic.GroupName) };
                trGroup.Cells.Add(tcGroup);

                TableRow trHeader = new TableRow() { CssClass = "totalsGroupDateRanges" };
                tblTotals.Rows.Add(trHeader);
                const string cssDateRange = "totalsDateRange";
                AddTextCellToRow(trHeader, string.Empty, true); // no header above the total description itself.
                AddTextCellToRow(trHeader, Rollup.SuppliedQueryHasDates ? string.Empty : Resources.FlightQuery.DatesAll, true, cssDateRange);
                AddTextCellToRow(trHeader, Resources.Profile.EmailWeeklyTotalsLabel, Rollup.IncludeLast7Days, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesThisMonth, Rollup.IncludeMonthToDate, cssDateRange);
                AddTextCellToRow(trHeader, szPreviousMonth, Rollup.IncludePreviousMonth, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesYearToDate, Rollup.IncludeYearToDate, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesPrev12Month, Rollup.IncludeTrailing12, cssDateRange);
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesPrev24Month, Rollup.IncludeTrailing24, cssDateRange);
                AddTextCellToRow(trHeader, szPreviousYear, Rollup.IncludePreviousYear, cssDateRange);

                foreach (TotalsItem ti in tic.Items)
                {
                    TableRow tr = new TableRow() { CssClass = "totalsGroupRow" };
                    tblTotals.Rows.Add(tr);

                    // Add the description
                    tr.Cells.Add(new TableCell() { Text = HttpUtility.HtmlEncode(ti.Description) });

                    decimal rowTotal = AddCellForTotalsItem(tr, ti, true, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.Last7.ContainsKey(ti.Description) ? Rollup.Last7[ti.Description] : null, Rollup.IncludeLast7Days, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.MonthToDate.ContainsKey(ti.Description) ? Rollup.MonthToDate[ti.Description] : null, Rollup.IncludeMonthToDate, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.PrevMonth.ContainsKey(ti.Description) ? Rollup.PrevMonth[ti.Description] : null, Rollup.IncludePreviousMonth, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.YTD.ContainsKey(ti.Description) ? Rollup.YTD[ti.Description] : null, Rollup.IncludeYearToDate, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.Trailing12.ContainsKey(ti.Description) ? Rollup.Trailing12[ti.Description] : null, Rollup.IncludeTrailing12, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.Trailing24.ContainsKey(ti.Description) ? Rollup.Trailing24[ti.Description] : null, Rollup.IncludeTrailing24, fLinkQuery) +
                    AddCellForTotalsItem(tr, Rollup.PrevYear.ContainsKey(ti.Description) ? Rollup.PrevYear[ti.Description] : null, Rollup.IncludePreviousYear, fLinkQuery);

                    // Remove rows of empty data
                    if (rowTotal == 0)
                        tblTotals.Rows.Remove(tr);
                }
            }
        }
    }
}