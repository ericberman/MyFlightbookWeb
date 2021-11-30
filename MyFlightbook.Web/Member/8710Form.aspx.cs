using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2011-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class FAA8710Form : Page
    {
        protected class ClassTotal
        {
            public string ClassName { get; set; }
            public decimal Total { get; set; }
            public decimal PIC { get; set; }
            public decimal SIC { get; set; }
        }

        protected bool UseHHMM { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.inst8710;
            Master.SuppressMobileViewport = true;

            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
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

            // rollup by time has no viewstate so must be refreshed by time
            RefreshTimePeriodRollup();
        }

        private Dictionary<string, List<ClassTotal>> ClassTotals { get; set; }

        protected void RefreshTimePeriodRollup()
        {
            mfbTotalsByTimePeriod.BindTotalsForUser(Page.User.Identity.Name, false, DateTime.Now.Day > 1, true, true, DateTime.Now.Day > 1 || DateTime.Now.Month > 1, mfbSearchForm1.Restriction);
        }

        protected void RefreshFormData()
        {

            FlightQuery fq = mfbSearchForm1.Restriction;

            fq.Refresh();

            string szRestrict = fq.RestrictClause;
            string szQueryTemplate = ConfigurationManager.AppSettings["8710ForUserQuery"];
            string szHaving = String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause;
            string szQueryClassTotals = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.InstanceTypeID, f.CatClassID");
            string szQueryMain = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.category");

            string szQueryRollup = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["RollupGridQuery"], szRestrict, szHaving);


            DBHelperCommandArgs args = new DBHelperCommandArgs(szQueryClassTotals) { Timeout = 120 };

            if (fq != null)
            {
                foreach (MySqlParameter p in fq.QueryParameters())
                    args.Parameters.Add(p);
            }

            // get the class totals
            try
            {
                ClassTotals = new Dictionary<string, List<ClassTotal>>();
                DBHelper dbh = new DBHelper(args);
                dbh.ReadRows((c) => { }, (d) =>
                {
                    string szCategory = (string)d["Category"];
                    string szClass = (string)d["Class"];
                    string szCatClass = (string)d["CatClass"];
                    if (!String.IsNullOrEmpty(szCategory) && !String.IsNullOrEmpty(szClass) && !String.IsNullOrEmpty(szCatClass))
                    {
                        if (!ClassTotals.ContainsKey(szCategory))
                            ClassTotals[szCategory] = new List<ClassTotal>();
                        List<ClassTotal> lst = ClassTotals[szCategory];
                        ClassTotal ct = new ClassTotal()
                        {
                            ClassName = szCatClass,
                            Total = Convert.ToDecimal(d["TotalTime"], CultureInfo.InvariantCulture),
                            PIC = Convert.ToDecimal(d["PIC"], CultureInfo.InvariantCulture),
                            SIC = Convert.ToDecimal(d["SIC"], CultureInfo.InvariantCulture)
                        };
                        lst.Add(ct);
                    }
                });
            }
            catch (MySqlException ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error getting 8710 data for user {0}: {1}", Page.User.Identity.Name, ex.Message), ex, Page.User.Identity.Name);
            }

            using (MySqlCommand comm = new MySqlCommand())
            {
                DBHelper.InitCommandObject(comm, args);
                using (comm.Connection = new MySqlConnection(DBHelper.ConnectionString))
                {
                    MySqlDataReader dr = null;
                    try
                    {
                        comm.CommandText = szQueryMain;
                        comm.Connection.Open();
                        using (dr = comm.ExecuteReader())
                        {
                            gv8710.DataSource = dr;
                            gv8710.DataBind();
                        }

                        comm.CommandText = szQueryRollup;
                        using (dr = comm.ExecuteReader())
                        {
                            gvRollup.DataSource = dr;
                            gvRollup.DataBind();
                            if (gvRollup.Rows.Count > 0)
                                gvRollup.Rows[gvRollup.Rows.Count - 1].Font.Bold = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Error getting 8710 data for user {0}: {1}", Page.User.Identity.Name, ex.Message), ex, Page.User.Identity.Name);
                    }
                }
            }

            UpdateDescription();
            if (!this.Master.IsMobileSession())
            {
                MfbLogbook1.Restriction = fq;
                MfbLogbook1.RefreshData();
            }
        }

        public void ClearForm(object sender, FlightQueryEventArgs fqe)
        {
            if (fqe == null)
                throw new ArgumentNullException(nameof(fqe));
            mfbSearchForm1.Restriction = fqe.Query;
            UpdateDescription();
            ShowResults(sender, fqe);
        }

        protected void ShowResults(object sender, FlightQueryEventArgs fqe)
        {
            if (fqe == null)
                throw new ArgumentNullException(nameof(fqe));
            mfbSearchForm1.Restriction = fqe.Query;
            UpdateDescription();
            RefreshFormData();
            RefreshTimePeriodRollup();

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

        protected static string ModelDisplay(object o)
        {
            if (o == null || o == DBNull.Value || !(o is System.Data.Common.DbDataRecord dr) || dr["FamilyDisplay"] == DBNull.Value)
                return Resources.LogbookEntry.FieldTotal;

            string szFamily = (string)dr["FamilyDisplay"];
            return szFamily.StartsWith("(", StringComparison.CurrentCultureIgnoreCase) ? szFamily : (string)dr["ModelDisplay"];
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