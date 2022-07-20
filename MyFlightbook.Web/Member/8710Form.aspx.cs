using MyFlightbook.Currency;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2011-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class FAA8710Form : Page
    {
        protected bool UseHHMM { get; set; }

        protected bool HasRefreshedTimePeriod { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.inst8710;
            Master.SuppressMobileViewport = true;

            Profile pf = Profile.GetUser(User.Identity.Name);
            UseHHMM = pf.UsesHHMM;

            if (!IsPostBack)
            {
                lblUserName.Text = Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText._8710FormForUserHeader, pf.UserFullName);
                RefreshFormData();
                MfbLogbook1.Visible = !this.Master.IsMobileSession();
                Master.ShowSponsoredAd = false;

                string szPath = (Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/", StringComparison.OrdinalIgnoreCase)) ? Request.PathInfo.Substring(1) : string.Empty;
                switch (szPath.ToUpperInvariant())
                {
                    default:
                    case "8710":
                        accReports.SelectedIndex = 1;
                        break;
                    case "MODEL":
                        accReports.SelectedIndex = 2;
                        break;
                    case "TIME":
                        accReports.SelectedIndex = 3;
                        break;
                }
            }
            else

            // rollup by time has no viewstate so must be refreshed on each page load.  BUT...was done in RefreshFormData so may not need to be done here
            if (!HasRefreshedTimePeriod)
                RefreshTimePeriodRollup();
        }

        private IDictionary<string, IList<Form8710ClassTotal>> ClassTotals { get; set; }

        protected void RefreshTimePeriodRollup()
        {
            bool fNewYearsDay = DateTime.Now.Day == 1 && DateTime.Now.Month == 1;
            mfbTotalsByTimePeriod.BindTotalsForUser(Page.User.Identity.Name, false, DateTime.Now.Day > 1, true, true, !fNewYearsDay, !fNewYearsDay, mfbSearchForm1.Restriction);
            HasRefreshedTimePeriod = true;
        }

        protected void RefreshFormData()
        {

            FlightQuery fq = mfbSearchForm1.Restriction;
            MfbLogbook1.Restriction = new FlightQuery(fq);  // before we muck with the query below, copy it here.

            fq.Refresh();
            DBHelperCommandArgs args = new DBHelperCommandArgs() { Timeout = 120 };
            args.AddFrom(fq.QueryParameters());

            UpdateDescription();

            IEnumerable<Form8710Row> lst8710 = null;
            IEnumerable<ModelRollupRow> lstModels = null;

            // get the various reports.  This can be a bit slow, so do all of the queries in parallel asynchronously.
            try
            {
                Task.WaitAll(
                    Task.Run(() => { ClassTotals = Form8710ClassTotal.ClassTotalsForQuery(fq, args); }),
                    Task.Run(() => { lst8710 = Form8710Row.Form8710ForQuery(fq, args); }),
                    Task.Run(() => { lstModels = ModelRollupRow.ModelRollupForQuery(fq, args); }),
                    Task.Run(() => { RefreshTimePeriodRollup(); }),
                    Task.Run(() =>
                    {
                        if (!Master.IsMobileSession())
                            MfbLogbook1.RefreshData();
                    })
                );
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error getting 8710 data for user {0}: {1}", Page.User.Identity.Name, ex.Message), ex, Page.User.Identity.Name);
            }

            // Do the databinding itself AFTER the async queries, since databinding may not be thread safe.
            gvRollup.DataSource = lstModels;
            gvRollup.DataBind();
            if (gvRollup.Rows.Count > 0)
                gvRollup.Rows[gvRollup.Rows.Count - 1].Font.Bold = true;

            gv8710.DataSource = lst8710;
            gv8710.DataBind();
        }

        public void ClearForm(object sender, FlightQueryEventArgs fqe)
        {
            if (fqe == null)
                throw new ArgumentNullException(nameof(fqe));
            mfbSearchForm1.Restriction = fqe.Query;
            UpdateDescription();
            ShowResults(sender, fqe);
        }

        protected void OnQuerySubmitted(object sender, FlightQueryEventArgs fqe)
        {
            ShowResults(sender, fqe);
        }

        protected void ShowResults(object sender, FlightQueryEventArgs fqe)
        {
            if (fqe == null)
                throw new ArgumentNullException(nameof(fqe));
            mfbSearchForm1.Restriction = fqe.Query;
            UpdateDescription();
            RefreshFormData();

            if (Int32.TryParse(hdnLastViewedPaneIndex.Value, out int idxLast))
                accReports.SelectedIndex = idxLast;
        }

        protected void UpdateDescription()
        {
            bool fRestrictionIsDefault = mfbSearchForm1.Restriction.IsDefault;
            mfbQueryDescriptor1.DataSource = fRestrictionIsDefault ? null : mfbSearchForm1.Restriction;
            mfbQueryDescriptor1.DataBind();
            pnlFilter.Visible = !fRestrictionIsDefault;
            apcFilter.LabelControl.Font.Bold = !fRestrictionIsDefault;
            apcFilter.IsEnhanced = !fRestrictionIsDefault;
        }

        protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
            mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);   // need to set the restriction in order to persist it (since it updates the view)
            ShowResults(sender, new FlightQueryEventArgs(mfbSearchForm1.Restriction));
        }

        protected void gv8710_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow && ClassTotals != null)
            {
                string szCategory = (string)DataBinder.Eval(e.Row.DataItem, "Category");
                if (!String.IsNullOrEmpty(szCategory) && ClassTotals.ContainsKey(szCategory))
                {
                    ((Control)e.Row.FindControl("pnlClassTotals")).Visible = true;
                    Repeater rptClasstotals = (Repeater)e.Row.FindControl("rptClassTotals");
                    rptClasstotals.DataSource = ClassTotals[szCategory];
                    rptClasstotals.DataBind();
                }

            }
        }

        protected static string FormatMultiDecimal(bool fUseHHMM, string separator, params object[] values)
        {
            if (values == null)
                return string.Empty;

            bool fHasValue = false;

            List<string> lst = new List<string>();
            foreach (object value in values)
            {
                if (value == null || value == DBNull.Value)
                    continue;

                decimal d = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                lst.Add(d == 0.0M ? "0" : d.FormatDecimal(fUseHHMM));
                fHasValue = fHasValue || d != 0.0M;
            }

            return fHasValue ? String.Join(separator, lst) : string.Empty;
        }

        protected static string FormatMultiInt(string separator, params object[] values)
        {
            if (values == null)
                return string.Empty;

            bool fHasValue = false;

            List<string> lst = new List<string>();
            foreach (object value in values)
            {
                if (values == null)
                    continue;

                int i = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                lst.Add(i == 0 ? "0" : i.FormatInt());
                fHasValue = fHasValue || i != 0;
            }

            return fHasValue ? String.Join(separator, lst) : string.Empty;
        }
    }
}