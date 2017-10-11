using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySql.Data.MySqlClient;
using System.Globalization;
using System.Configuration;
using System.Data;
using MyFlightbook;
using MyFlightbook.FlightCurrency;

/******************************************************
 * 
 * Copyright (c) 2011-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Member_8710Form : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.lbt8710;
        Master.SuppressMobileViewport = true;

        if (!IsPostBack)
        {
            lblUserName.Text = Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText._8710FormForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
            RefreshFormData();
            MfbLogbook1.Visible = !this.Master.IsMobileSession();
            Master.ShowSponsoredAd = false;
        }
    }

    private Dictionary<string, List<TotalsItem>> ClassTotals { get; set; }

    protected void RefreshFormData()
    {

        FlightQuery fq = mfbSearchForm1.Restriction;

        fq.Refresh();

        string szRestrict = fq.RestrictClause;
        string szQueryTemplate = ConfigurationManager.AppSettings["8710ForUserQuery"].ToString();
        string szHaving = String.IsNullOrEmpty(fq.HavingClause) ? string.Empty : "HAVING " + fq.HavingClause;
        string szQueryClassTotals = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.InstanceTypeID, f.CatClassID"); 
        string szQueryMain = String.Format(CultureInfo.InvariantCulture, szQueryTemplate, szRestrict, szHaving, "f.category");

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
            ClassTotals = new Dictionary<string, List<TotalsItem>>();
            DBHelper dbh = new DBHelper(args);
            dbh.ReadRows((c) => { }, (d) =>
            {
                string szCategory = (string)d["Category"];
                string szClass = (string)d["Class"];
                string szCatClass = (string)d["CatClass"];
                if (!String.IsNullOrEmpty(szCategory) && !String.IsNullOrEmpty(szClass) && !String.IsNullOrEmpty(szCatClass))
                {
                    if (!ClassTotals.ContainsKey(szCategory))
                        ClassTotals[szCategory] = new List<TotalsItem>();
                    ClassTotals[szCategory].Add(new TotalsItem(szCatClass, Convert.ToDecimal(d["TotalTime"], CultureInfo.InvariantCulture)));
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
                        UpdateDescription();
                        if (!this.Master.IsMobileSession())
                        {
                            MfbLogbook1.Restriction = fq;
                            MfbLogbook1.RefreshData();
                        }
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
                GridView gvClassTotals = (GridView)e.Row.FindControl("gvClassTotals");
                gvClassTotals.DataSource = ClassTotals[szCategory];
                gvClassTotals.DataBind();
            }

        }
    }
}