using MyFlightbook;
using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTotalSummary : System.Web.UI.UserControl
{
    public enum TotalRecency { AllTime, TrailingYear, YearToDate};
    private FlightQuery m_fq;
    private bool m_LinkTotalsToQuery = true;
    private const string szCookieDefaultTotalsMode = "cookieDefaultTotalsMode2";
    private const string szFlatCookieValue = "flat";

    #region properties
    public string Username { get; set; }

    public Boolean UseHHMM { get; set; }

    public Boolean Use2Digits { get; set; }

    /// <summary>
    /// True (default) to linkify totals
    /// </summary>
    public bool LinkTotalsToQuery
    {
        get { return m_LinkTotalsToQuery; }
        set { m_LinkTotalsToQuery = value; }
    }

    /// <summary>
    /// Default group mode for totals.  True for grouped, false for flat.
    /// </summary>
    public bool DefaultGroupMode
    {
        get
        {
            string szPref = Profile.GetUser(Page.User.Identity.Name).GetPreferenceForKey<string>(szCookieDefaultTotalsMode) ?? string.Empty;
            return szPref.CompareCurrentCultureIgnoreCase(szFlatCookieValue) != 0;
        }
        set
        {
            Profile.GetUser(Page.User.Identity.Name).SetPreferenceForKey(szCookieDefaultTotalsMode, value ? null : szFlatCookieValue);
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
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            UseHHMM = pf.UsesHHMM;
            Use2Digits = pf.Use2DigitTotals;
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
        lblNoTotals.Visible = !tic.Any();
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
