using MyFlightbook;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2011-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_8710Form : System.Web.UI.Page
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
        this.Master.SelectedTab = tabID.lbt8710;
        Master.SuppressMobileViewport = true;

        Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
        UseHHMM = pf.UsesHHMM;

        if (!IsPostBack)
        {
            lblUserName.Text = Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText._8710FormForUserHeader, pf.UserFullName);
            RefreshFormData();
            MfbLogbook1.Visible = !this.Master.IsMobileSession();
            Master.ShowSponsoredAd = false;
        }
    }

    private Dictionary<string, List<ClassTotal>> ClassTotals { get; set; }

    protected void RefreshFormData()
    {

        FlightQuery fq = mfbSearchForm1.Restriction;

        fq.Refresh();

        string szRestrict = fq.RestrictClause;
        string szQueryTemplate = ConfigurationManager.AppSettings["8710ForUserQuery"].ToString();
        string szHaving = String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause;
        string szQueryClassTotals = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.InstanceTypeID, f.CatClassID"); 
        string szQueryMain = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.category");

        string szQueryRollup = String.Format(CultureInfo.InvariantCulture, ConfigurationManager.AppSettings["RollupGridQuery"].ToString(), szRestrict, szHaving);


        DBHelperCommandArgs args = new DBHelperCommandArgs(szQueryClassTotals);

        if (fq != null)
        {
            args.AddWithValue("localecode", System.Globalization.CultureInfo.CurrentCulture.Name.Replace("-", "_"));
            args.AddWithValue("shortDate", DBHelper.CSharpDateFormatToMySQLDateFormat());
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
            using (comm.Connection)
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
                finally
                {
                    if (comm.Connection != null && comm.Connection.State != ConnectionState.Closed)
                        comm.Connection.Close();
                }
            }

            UpdateDescription();
            if (!this.Master.IsMobileSession())
            {
                MfbLogbook1.Restriction = fq;
                MfbLogbook1.RefreshData();
            }
        }
    }

    public void ClearForm(object sender, FlightQueryEventArgs fqe)
    {
        if (fqe == null)
            throw new ArgumentNullException("fqe");
        mfbSearchForm1.Restriction = fqe.Query;
        UpdateDescription();
        ShowResults(sender, fqe);
    }

    protected void ShowResults(object sender, FlightQueryEventArgs fqe)
    {
        if (fqe == null)
            throw new ArgumentNullException("fqe");
        mfbSearchForm1.Restriction = fqe.Query;
        mvQuery.ActiveViewIndex = 0; // go back to the results.
        UpdateDescription();
        RefreshFormData();
    }

    protected void btnEditQuery_Click(object sender, EventArgs e)
    {
        mvQuery.ActiveViewIndex = 1;
    }

    protected void UpdateDescription()
    {
        mfbQueryDescriptor1.DataSource = mfbSearchForm1.Restriction;
        mfbQueryDescriptor1.DataBind();
    }

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);   // need to set the restriction in order to persist it (since it updates the view)
        ShowResults(sender, new FlightQueryEventArgs(mfbSearchForm1.Restriction));
    }

    protected void gv8710_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow && ClassTotals != null)
        {
            string szCategory = (string) DataBinder.Eval(e.Row.DataItem, "Category");
            if (!String.IsNullOrEmpty(szCategory) && ClassTotals.ContainsKey(szCategory)) {
                ((Control)e.Row.FindControl("pnlClassTotals")).Visible = true;
                Repeater rptClasstotals = (Repeater)e.Row.FindControl("rptClassTotals");
                rptClasstotals.DataSource = ClassTotals[szCategory];
                rptClasstotals.DataBind();
            }

        }
    }

    protected string ModelDisplay(object o)
    {
        System.Data.Common.DbDataRecord dr = o as System.Data.Common.DbDataRecord;

        if (o == null || o == DBNull.Value || dr == null || dr["Family"] == DBNull.Value)
            return Resources.LogbookEntry.FieldTotal;

        string szFamily = (string)dr["Family"];
        return szFamily.StartsWith("(", StringComparison.CurrentCultureIgnoreCase) ? szFamily : (string)dr["ModelDisplay"];
    }

    protected string FormatMultiDecimal(bool fUseHHMM, string separator, params object[] values)
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

    protected string FormatMultiInt(string separator, params object[] values)
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

    protected void rblReport_SelectedIndexChanged(object sender, EventArgs e)
    {
        mvReport.ActiveViewIndex = rblReport.SelectedIndex;
    }
}