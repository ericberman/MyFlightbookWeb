using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTotalSummary : System.Web.UI.UserControl
{
    public enum TotalRecency { AllTime, TrailingYear, YearToDate};
    private FlightQuery m_fq;
    private bool m_LinkTotalsToQuery = true;

    #region properties
    public string Username { get; set; }

    public Boolean UseHHMM { get; set; }

    /// <summary>
    /// True (default) to linkify totals
    /// </summary>
    public bool LinkTotalsToQuery
    {
        get { return m_LinkTotalsToQuery; }
        set { m_LinkTotalsToQuery = value; }
    }
    #endregion

    protected void Page_Init(object sender, EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated)
            UseHHMM = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM;
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

            // Only refresh if the incoming value is different from the previous value.
            if (value != null)
                value.UserName = Username;

            FlightQuery fqOld = m_fq;
            m_fq = value;
            if (m_fq == null || !m_fq.IsSameAs(fqOld))
            {
                UserTotals ut = new UserTotals(Username, m_fq, true);
                ut.DataBind();
                gvTotals.DataSource = ut.Totals;
                gvTotals.DataBind();
            }
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
}
