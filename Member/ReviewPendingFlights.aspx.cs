using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ReviewPendingFlights : System.Web.UI.Page
{
    private MyFlightbook.Profile m_pfUser = null;
    const string szKeyLastSortExpr = "LastSort";
    const string szKeylastSortDir = "LastSortDir";
    const string szVSKeyFlights = "vsPendingFlights";

    #region properties
    protected MyFlightbook.Profile Viewer
    {
        get { return m_pfUser ?? (m_pfUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name)); }
    }

    protected IEnumerable<PendingFlight> Flights
    {
        get { return (IEnumerable<PendingFlight>)ViewState[szVSKeyFlights]; }
        set
        {
            ViewState[szVSKeyFlights] = value;
            lnkDeleteAll.Visible = (value != null && value.Count() > 0);
        }
    }

    protected Boolean HasPrevSort
    {
        get { return ViewState[szKeylastSortDir + ID] != null && ViewState[szKeyLastSortExpr + ID] != null; }
    }

    public string LastSortExpr
    {
        get
        {
            object o = ViewState[szKeyLastSortExpr + ID];
            return (o == null) ? string.Empty : o.ToString();
        }
        set { ViewState[szKeyLastSortExpr + ID] = value; }
    }

    public SortDirection LastSortDir
    {
        get
        {
            object o = ViewState[szKeylastSortDir + ID];
            return (o == null) ? SortDirection.Descending : (SortDirection)o;
        }
        set { ViewState[szKeylastSortDir + ID] = value; }
    }
    #endregion

    protected void Refresh()
    {
        gvPendingFlights.DataSource = Flights = PendingFlight.PendingFlightsForUser(Viewer.UserName);
        gvPendingFlights.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.lbtPending;

        if (!IsPostBack)
        {
            Page.Title = Resources.LogbookEntry.ReviewPendingFlightsHeader;
            Refresh();
        }
    }

    protected void gvPendingFlights_Sorting(object sender, GridViewSortEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (sender == null)
            throw new ArgumentNullException("sender");

        GridView gv = (GridView)sender;
        List<PendingFlight> lst = new List<PendingFlight>(Flights);

        if (lst != null)
        {
            if (HasPrevSort)
            {
                string PrevSortExpr = LastSortExpr;
                SortDirection PrevSortDir = LastSortDir;

                if (PrevSortExpr == e.SortExpression)
                    e.SortDirection = (PrevSortDir == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }

            LastSortExpr = e.SortExpression;
            LastSortDir = e.SortDirection;

            foreach (DataControlField dcf in gv.Columns)
                dcf.HeaderStyle.CssClass = "headerBase" + ((dcf.SortExpression.CompareCurrentCultureIgnoreCase(e.SortExpression) == 0) ? (e.SortDirection == SortDirection.Ascending ? " headerSortAsc" : " headerSortDesc") : string.Empty);

            lst.Sort((l1, l2) => { return (LastSortDir == SortDirection.Ascending ? 1 : -1) * ((IComparable)l1.GetType().GetProperty(LastSortExpr).GetValue(l1)).CompareTo(((IComparable)l2.GetType().GetProperty(LastSortExpr).GetValue(l2))); });

            gv.DataSource = Flights = lst;
            gv.DataBind();
        }
    }

    protected void EditPendingFlightInList(PendingFlight pendingFlight, List<PendingFlight> lst)
    {
        mvPendingFlights.SetActiveView(vwEdit);
        mfbEditFlight.SetPendingFlight(pendingFlight);
        int index = lst.IndexOf(pendingFlight);
        mfbEditFlight.SetNextFlight(index > 0 ? index - 1 : LogbookEntry.idFlightNone);
        mfbEditFlight.SetPrevFlight(index < lst.Count - 1 ? index : LogbookEntry.idFlightNone); // since save will ultimately remove this from the list.
    }

    protected void gvPendingFlights_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (String.Compare(e.CommandName, "Sort", StringComparison.OrdinalIgnoreCase) == 0)
            return;

        string idFlight = e.CommandArgument.ToString();

        if (String.IsNullOrEmpty(idFlight))
            throw new MyFlightbookValidationException("Invalid ID!");

        List<PendingFlight> lst = new List<PendingFlight>(Flights);
        PendingFlight pendingFlight = lst.FirstOrDefault(pf => pf.PendingID.CompareCurrentCultureIgnoreCase(idFlight) == 0);

        if (pendingFlight == null)
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Pending flight with ID {0} not found.", idFlight));

        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            pendingFlight.Delete();
            lst.Remove(pendingFlight);
            gvPendingFlights.DataSource = Flights = lst;
            gvPendingFlights.DataBind();
        }
        else if (String.Compare(e.CommandName, "_Edit", StringComparison.OrdinalIgnoreCase) == 0)
            EditPendingFlightInList(pendingFlight, lst);
    }

    protected void mfbEditFlight_FlightEditCanceled(object sender, EventArgs e)
    {
        mvPendingFlights.SetActiveView(vwList);
    }

    protected void mfbEditFlight_FlightUpdated(object sender, LogbookEventArgs e)
    {
        Refresh();
        mvPendingFlights.SetActiveView(vwList);
        if (e.IDNextFlight >= 0)
        {
            List<PendingFlight> lst = new List<PendingFlight>(Flights);
            PendingFlight pf = lst[e.IDNextFlight];
            EditPendingFlightInList(pf, lst);
        }
    }

    protected void lnkDeleteAll_Click(object sender, EventArgs e)
    {
        PendingFlight.DeletePendingFlightsForUser(Page.User.Identity.Name);
        Refresh();
    }
}