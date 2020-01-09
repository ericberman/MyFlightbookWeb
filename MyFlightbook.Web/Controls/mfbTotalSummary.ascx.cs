using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTotalSummary : System.Web.UI.UserControl
{
    public enum TotalRecency { AllTime, TrailingYear, YearToDate};
    private FlightQuery m_fq;
    private bool m_LinkTotalsToQuery = true;
    private const string szCookieDefaultTotalsMode = "cookieDefaultTotalsMode";

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

    public bool DefaultGroupMode
    {
        get
        {
            bool fGrouped;
            if (Request.Cookies[szCookieDefaultTotalsMode] != null && bool.TryParse(Request.Cookies[szCookieDefaultTotalsMode].Value, out fGrouped))
                return fGrouped;
            return false;
        }
        set
        {
            Response.Cookies[szCookieDefaultTotalsMode].Value = value.ToString(CultureInfo.InvariantCulture);
            Response.Cookies[szCookieDefaultTotalsMode].Expires = DateTime.Now.AddYears(20);
        }
    }

    public bool IsGrouped
    {
        get { return mvTotals.GetActiveView() == vwGrouped; }
        set { mvTotals.SetActiveView(value ? vwGrouped : vwFlat); }
    }
    #endregion

    protected void Page_Init(object sender, EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated)
        {
            Username = Page.User.Identity.Name;
            UseHHMM = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM;
        }
        IsGrouped = DefaultGroupMode;
    }

    protected void Bind()
    {
        UserTotals ut = new UserTotals(Username, m_fq, true);
        ut.DataBind();

        IEnumerable<TotalsItemCollection> tic = TotalsItemCollection.AsGroups(ut.Totals);
        rptGroups.DataSource = tic;
        rptGroups.DataBind();
        lblNoTotals.Visible = tic.Count() == 0;
        gvTotals.DataSource = ut.Totals;
        gvTotals.DataBind();
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
                Bind();
        }
    }

    public FlightQuery.DateRanges Recency
    {
        get { return CustomRestriction.DateRange; }
        set
        {
            if (String.IsNullOrEmpty(Username))
                Username = Page.User.Identity.Name;

            FlightQuery fq = new FlightQuery(Username) { DateRange = value };
            CustomRestriction = fq;
        }
    }
}
