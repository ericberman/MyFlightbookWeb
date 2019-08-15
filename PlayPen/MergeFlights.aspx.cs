using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class PlayPen_MergeFlights : System.Web.UI.Page
{
    public string GetClassForWizardStep(object wizardStep)
    {
        WizardStep step = wizardStep as WizardStep;

        if (step == null)
            return "";

        int stepIndex = wzMerge.WizardSteps.IndexOf(step);

        if (stepIndex < wzMerge.ActiveStepIndex)
            return "wizStepCompleted";
        else if (stepIndex > wzMerge.ActiveStepIndex)
            return "wizStepFuture";
        else
            return "wizStepInProgress";
    }

    protected void wzMerges_PreRender(object sender, EventArgs e)
    {
        Repeater SideBarList = wzMerge.FindControl("HeaderContainer").FindControl("SideBarList") as Repeater;

        SideBarList.DataSource = wzMerge.WizardSteps;
        SideBarList.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.lbtImport;
        wzMerge.PreRender += new EventHandler(wzMerges_PreRender);

        if (!Page.User.Identity.IsAuthenticated)
            Response.Redirect("~/Default.aspx");

        if (!IsPostBack)
            RefreshFlightsList();
    }

    protected void RefreshFlightsList()
    {
        List<LogbookEntryDisplay> lstFlights = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(new FlightQuery(User.Identity.Name)), User.Identity.Name, "Date", SortDirection.Descending, false, false);

        rptSelectedFlights.DataSource = lstFlights;
        rptSelectedFlights.DataBind();
    }

    protected IList<string> SelectedFlightIDs
    {
        get
        {
            List<string> lstIds = new List<string>();

            foreach (RepeaterItem ri in rptSelectedFlights.Items)
            {
                CheckBox ck = (CheckBox)ri.FindControl("ckFlight");
                if (ck.Checked)
                {
                    HiddenField h = (HiddenField)ri.FindControl("hdnFlightID");
                    lstIds.Add(h.Value);
                }
            }
            return lstIds;
        }
    }

    private FlightQuery Query
    {
        get
        {
            IList<string> lstIds = SelectedFlightIDs;
            FlightQuery fq = new FlightQuery(User.Identity.Name) { CustomRestriction = String.Format(CultureInfo.InvariantCulture, " (flights.idFlight IN ({0})) ", String.Join(", ", lstIds)) };
            return fq;
        }
    }

    private const string vsSelectedFlights = "vsSelectedFlights";
    private List<LogbookEntryDisplay> SelectedFlights
    {
        get
        {
            if (ViewState[vsSelectedFlights] == null)
            {
                if (SelectedFlightIDs.Count() == 0)
                    return new List<LogbookEntryDisplay>();
                DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(Query));
                Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
                ViewState[vsSelectedFlights] = LogbookEntryDisplay.GetFlightsForQuery(dbh.CommandArgs, Page.User.Identity.Name, "Date", SortDirection.Descending, pf.UsesHHMM, pf.UsesUTCDateOfFlight);
            }
            return (List<LogbookEntryDisplay>)ViewState[vsSelectedFlights];
        }
        set
        {
            ViewState[vsSelectedFlights] = value;
        }
    }

    private bool IsValidToMerge()
    {
        IEnumerable<LogbookEntry> flights = SelectedFlights;
        if (flights.Count() < 2)
            lblNeed2Flights.Visible = true;
        else
        {
            HashSet<int> hsAircraft = new HashSet<int>();
            foreach (LogbookEntry le in flights)
                hsAircraft.Add(le.AircraftID);
            if (hsAircraft.Count > 1)
                lblHeterogeneousAircraft.Visible = true;

        }
        return !lblNeed2Flights.Visible && !lblHeterogeneousAircraft.Visible;
    }

    protected void wzMerge_FinishButtonClick(object sender, WizardNavigationEventArgs e)
    {
        List<LogbookEntry> lst = new List<LogbookEntry>();

        // Need to use LogbookEntry, not LogbookEntryDisplay, since you can't commit LogbookEntryDisplay
        // Also need to do ascending so 1st element is target.
        DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(Query, -1, -1, true, LogbookEntry.LoadTelemetryOption.LoadAll));
        dbh.ReadRows((comm) => { },
            (dr) =>
            {
                LogbookEntry le = new LogbookEntry(dr, Query.UserName, LogbookEntryBase.LoadTelemetryOption.LoadAll); // Note: this has no telemetry
                le.PopulateImages();
                lst.Add(le);
            });

        LogbookEntry target = lst[0];
        lst.RemoveAt(0);
        foreach (LogbookEntry le in lst)
            le.PopulateImages();

        target.MergeFrom(lst);
        Response.Redirect("~/Member/LogbookNew.aspx");
    }

    protected void wzMerge_NextButtonClick(object sender, WizardNavigationEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (wzMerge.ActiveStep == wsSelectFlights)
        {
            if (!IsValidToMerge())
            {
                e.Cancel = true;
                SelectedFlights = null;
                return;
            }

            mfbLogbookPreview.Restriction = Query;
            mfbLogbookPreview.RefreshData();
        }
    }
}