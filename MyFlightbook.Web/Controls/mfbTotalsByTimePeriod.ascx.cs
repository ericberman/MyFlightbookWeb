using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

        protected bool Past7Days { get; set; }

        protected bool MonthToDate { get; set; }

        protected bool PreviousMonth { get; set; }

        protected bool PreviousYear { get; set; }

        protected bool YearToDate { get; set; }

        protected int ColumnCount { get; set; }


        protected void Page_Load(object sender, EventArgs e)
        {

        }

        private static Dictionary<string, TotalsItem> TotalsForQuery(FlightQuery fq, bool fBind, Dictionary<string, TotalsItem> d)
        {
            d = d ?? new Dictionary<string, TotalsItem>();

            if (fBind)
            {
                UserTotals ut = new UserTotals(fq.UserName, fq, false);
                ut.DataBind();

                foreach (TotalsItem ti in ut.Totals)
                    if (ti.Value > 0)
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

        private decimal AddCellForTotalsItem(TableRow tr, TotalsItem ti, bool fInclude)
        {
            if (!fInclude)
                return 0.0M;

            TableCell tc = new TableCell();
            tr.Cells.Add(tc);
            tc.CssClass = "totalsByTimeCell";

            // Empty totals item = empty cell.
            if (ti == null)
                return 0.0M;

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

            return ti.Value;    // Indicate if we actually had a non-zero value.  A row of all empty cells or zero cells should be deleted.
        }

        public void BindTotalsForUser(string szUser, bool fLast7Days, bool fMonthToDate, bool fPreviousMonth, bool fPreviousYear, bool fYearToDate, bool fTrailing12, FlightQuery fqSupplied = null)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            UseHHMM = pf.UsesHHMM;

            // Get All time totals.  This will also give us the entire space of totals items
            FlightQuery fq = (fqSupplied == null) ? new FlightQuery(szUser) : new FlightQuery(fqSupplied);
            UserTotals ut = new UserTotals(szUser, new FlightQuery(fq), false);

            // if the supplied query has a date range, then don't do any of the subsequent queries; the date range overrides.
            bool fSuppliedQueryHasDates = fq.DateRange != FlightQuery.DateRanges.AllTime;
            if (fSuppliedQueryHasDates)
                fLast7Days = fMonthToDate = fPreviousMonth = fPreviousYear = fYearToDate = fTrailing12 = false;

            Dictionary<string, TotalsItem> dMonthToDate = new Dictionary<string, TotalsItem>();
            Dictionary<string, TotalsItem> dPrevMonth = new Dictionary<string, TotalsItem>();
            Dictionary<string, TotalsItem> dYTD = new Dictionary<string, TotalsItem>();
            Dictionary<string, TotalsItem> dTrailing12 = new Dictionary<string, TotalsItem>();
            Dictionary<string, TotalsItem> dPrevYear = new Dictionary<string, TotalsItem>();
            Dictionary<string, TotalsItem> dLast7 = new Dictionary<string, TotalsItem>();

            // Get all of the results asynchronously, but block until they're all done.
            Task.WaitAll(
                Task.Run(() => { ut.DataBind(); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.ThisMonth }, fMonthToDate, dMonthToDate); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.PrevMonth }, fPreviousMonth, dPrevMonth); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.YTD }, fYearToDate, dYTD); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.Trailing12Months }, fTrailing12, dTrailing12); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.PrevYear }, fPreviousYear, dPrevYear); }),
                Task.Run(() => { TotalsForQuery(new FlightQuery(fq) { DateRange = FlightQuery.DateRanges.Custom, DateMin = DateTime.Now.Date.AddDays(-7), DateMax = DateTime.Now.Date.AddDays(1) }, fLast7Days, dLast7); })
                );

            IEnumerable <TotalsItemCollection> allTotals = TotalsItemCollection.AsGroups(ut.Totals);

            tblTotals.Controls.Clear();

            // Determine which columns we'll show
            ColumnCount = 2;   // All time is always shown, as are its labels (in adjacent table column)
            if (fLast7Days &= dLast7.Any())
                ColumnCount++;
            if (fMonthToDate &= dMonthToDate.Any())
                ColumnCount++;
            if (fPreviousMonth &= dPrevMonth.Any())
                ColumnCount++;
            if (fTrailing12 &= dTrailing12.Any())
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
                TableRow trGroup = new TableRow() { CssClass = "totalsGroupHeaderRow" };
                tblTotals.Rows.Add(trGroup);
                TableCell tcGroup = new TableCell() { ColumnSpan = ColumnCount, Text = tic.GroupName };
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
                AddTextCellToRow(trHeader, Resources.FlightQuery.DatesPrev12Month, fTrailing12, cssDateRange);
                AddTextCellToRow(trHeader, szPreviousYear, fPreviousYear, cssDateRange);

                foreach (TotalsItem ti in tic.Items)
                {
                    TableRow tr = new TableRow() { CssClass = "totalsGroupRow" };
                    tblTotals.Rows.Add(tr);

                    // Add the description
                    tr.Cells.Add(new TableCell() { Text = ti.Description });

                    decimal rowTotal = AddCellForTotalsItem(tr, ti, true) +
                    AddCellForTotalsItem(tr, dLast7.ContainsKey(ti.Description) ? dLast7[ti.Description] : null, fLast7Days) +
                    AddCellForTotalsItem(tr, dMonthToDate.ContainsKey(ti.Description) ? dMonthToDate[ti.Description] : null, fMonthToDate) +
                    AddCellForTotalsItem(tr, dPrevMonth.ContainsKey(ti.Description) ? dPrevMonth[ti.Description] : null, fPreviousMonth) +
                    AddCellForTotalsItem(tr, dYTD.ContainsKey(ti.Description) ? dYTD[ti.Description] : null, fYearToDate) +
                    AddCellForTotalsItem(tr, dTrailing12.ContainsKey(ti.Description) ? dTrailing12[ti.Description] : null, fTrailing12) +
                    AddCellForTotalsItem(tr, dPrevYear.ContainsKey(ti.Description) ? dPrevYear[ti.Description] : null, fPreviousYear);

                    // Remove rows of empty data
                    if (rowTotal == 0)
                        tblTotals.Rows.Remove(tr);
                }
            }
        }
    }
}