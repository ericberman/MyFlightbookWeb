using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSearchAndTotals : System.Web.UI.UserControl
{
    public event System.EventHandler<FlightQueryEventArgs> QuerySubmitted;

    public Boolean InitialCollapseState
    {
        get { return mfbSearchForm1.InitialCollapseState; }
        set { mfbSearchForm1.InitialCollapseState = value; }
    }

    public FlightQuery Restriction
    {
        get { return mfbSearchForm1.Restriction; }
        set { mfbSearchForm1.Restriction = value; }
    }

    public string SimpleQuery
    {
        get { return mfbSearchForm1.SimpleSearchText; }
        set { mfbSearchForm1.SimpleSearchText = value; }
    }

    public string AirportQuery
    {
        get { return mfbSearchForm1.AirportSearch; }
        set { mfbSearchForm1.AirportSearch = value; }
    }

    public bool LinkTotalsToQuery
    {
        get { return mfbTotalSummary1.LinkTotalsToQuery; }
        set { mfbTotalSummary1.LinkTotalsToQuery = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            UpdateRestriction();
            UpdateDescription();
        }
    }

    public void SetUser(string szUser)
    {
        mfbSearchForm1.Username = mfbTotalSummary1.Username = szUser;
    }

    protected void UpdateRestriction()
    {
        mfbTotalSummary1.CustomRestriction = Restriction;
    }

    public void ClearForm(object sender, FlightQueryEventArgs e)
    {
        ShowResults(sender, e);
    }

    protected void ShowResults(object sender, FlightQueryEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        mfbTotalSummary1.CustomRestriction = Restriction = e.Query;
        mvQuery.ActiveViewIndex = 0; // go back to the results.
        UpdateDescription();

        QuerySubmitted?.Invoke(sender, e);
    }

    protected void btnEditQuery_Click(object sender, EventArgs e)
    {
        mvQuery.ActiveViewIndex = 1;
    }

    protected void UpdateDescription()
    {
        mfbQueryDescriptor1.DataSource = Restriction;
        mfbQueryDescriptor1.DataBind();
    }

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
    {
        if (fic == null)
            throw new ArgumentNullException(nameof(fic));
        FlightQuery fq = Restriction.ClearRestriction(fic.FilterItem);
        ShowResults(sender, new FlightQueryEventArgs(fq));
        UpdateDescription();

        QuerySubmitted?.Invoke(sender, new FlightQueryEventArgs(fq));
    }
}
