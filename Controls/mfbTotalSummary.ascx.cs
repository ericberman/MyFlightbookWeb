using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTotalSummary : System.Web.UI.UserControl
{
    public enum TotalRecency { AllTime, TrailingYear, YearToDate};
    protected Boolean m_fUseHHMM;
    private FlightQuery m_fq;
    private bool m_LinkTotalsToQuery = true;

    public string Username { get; set; }

    /// <summary>
    /// True (default) to linkify totals
    /// </summary>
    public bool LinkTotalsToQuery
    {
        get { return m_LinkTotalsToQuery; }
        set { m_LinkTotalsToQuery = value; }
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated)
            m_fUseHHMM = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM;
    }
    
    protected void Page_Load(object sender, EventArgs e)
    {
    }

    public FlightQuery CustomRestriction
    {
        get { return m_fq; }
        set
        {
            if (String.IsNullOrEmpty(Username))
                Username = Page.User.Identity.Name;

            m_fq = value;
            if (m_fq != null)
                m_fq.UserName = Username;
            UserTotals ut = new UserTotals(Username, m_fq, true);
            ut.DataBind();
            gvTotals.DataSource = ut.Totals;
            gvTotals.DataBind();
        }
    }

    public FlightQuery.DateRanges Recency
    {
        get { return CustomRestriction.DateRange; }
        set
        {
            if (String.IsNullOrEmpty(Username))
                Username = Page.User.Identity.Name;

            FlightQuery fq = new FlightQuery(Username);
            fq.DateRange = value;
            CustomRestriction = fq;
        }
    }

    protected void gvTotals_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            TotalsItem ti = (TotalsItem)e.Row.DataItem;

            Label lblValue = (Label)e.Row.FindControl("lblValue");

            switch (ti.NumericType)
            {
                case TotalsItem.NumType.Integer:
                    lblValue.Text = String.Format(CultureInfo.CurrentCulture, "{0:#,##0}", (int)ti.Value);
                    break;
                case TotalsItem.NumType.Decimal:
                    lblValue.Text = String.Format(CultureInfo.CurrentCulture, "{0:F2}", ti.Value);
                    break;
                case TotalsItem.NumType.Time:
                    lblValue.Text = ti.Value.FormatDecimal(ti.IsTime && m_fUseHHMM);
                    break;
                case TotalsItem.NumType.Currency:
                    lblValue.Text = String.Format(CultureInfo.CurrentCulture, "{0:C}", ti.Value);
                    break;
            }
        }
    }
}
