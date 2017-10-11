using MyFlightbook;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ImportTelemetry : System.Web.UI.Page
{
    private List<TelemetryMatchResult> Results
    {
        get
        {
            List<TelemetryMatchResult> lst = (List<TelemetryMatchResult>)Session[SessionKeyResults];
            if (lst == null)
                Session[SessionKeyResults] = lst = new List<TelemetryMatchResult>();
            return lst;
        }
    }

    private string SessionKeyBase { get { return Page.User.Identity.Name + "telemImport"; } }

    private string SessionKeyResults { get { return SessionKeyBase + "Results"; } }
    private string SessionKeyTZ { get { return SessionKeyBase + "TZ"; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabLogbook;
        lblTitle.Text = Master.Title = Resources.FlightData.ImportHeaderBulkUpload;
        if (IsPostBack)
        {
            Session[SessionKeyTZ] = TimeZone1.SelectedTimeZone;
        }
        else
        {
            gvResults.DataSource = Results;
            gvResults.DataBind();
            Results.Clear();
        }
    }

    protected void AjaxFileUpload1_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            return;

        TimeZoneInfo tz = (TimeZoneInfo)Session[SessionKeyTZ];

        TelemetryMatchResult tmr = new TelemetryMatchResult() { TelemetryFileName = e.FileName };

        if (tz != null)
            tmr.TimeZoneOffset = tz.BaseUtcOffset.TotalHours;


        DateTime? dtFallback = null;
        Regex rDate = new Regex("^(?:19|20)\\d{2}[. _-]?[01]?\\d[. _-]?[012]?\\d", RegexOptions.Compiled);
        MatchCollection mc = rDate.Matches(e.FileName);
        if (mc != null && mc.Count > 0 && mc[0].Groups.Count > 0)
        {
            DateTime dt;
            if (DateTime.TryParse(mc[0].Groups[0].Value, out dt))
            {
                dtFallback = dt;
                tmr.TimeZoneOffset = 0; // do it all in local time.
            }
        }
        tmr.MatchToFlight(System.Text.Encoding.UTF8.GetString(e.GetContents()), Page.User.Identity.Name, dtFallback);
        Results.Add(tmr);

        e.DeleteTemporaryData();
    }
}