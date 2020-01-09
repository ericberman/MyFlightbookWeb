using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
public partial class Controls_ClubControls_FlyingReport : System.Web.UI.UserControl, IReportable
{
    #region Formatting Helpers
    protected string FullName(string szFirst, string szLast, string szEmail)
    {
        Profile pf = new Profile()
        {
            FirstName = szFirst,
            LastName = szLast,
            Email = szEmail
        };
        return pf.UserFullName;
    }

    protected string MonthForDate(DateTime dt)
    {
        return String.Format(CultureInfo.InvariantCulture, "{0}-{1} ({2})", dt.Year, dt.Month.ToString("00", CultureInfo.InvariantCulture), dt.ToString("MMM", CultureInfo.CurrentCulture));
    }

    protected string FormattedUTCDate(object o)
    {
        if (o == null)
            return string.Empty;
        if (o is DateTime)
            return ((DateTime)o).UTCFormattedStringOrEmpty(false);
        return string.Empty;
    }
#endregion

    public string CSVData
    {
        get { return gvFlyingReport.CSVFromData(); }
    }

    protected void SetParams(int ClubID, DateTime dateStart, DateTime dateEnd)
    {
        sqlDSReports.SelectParameters.Clear();
        sqlDSReports.SelectParameters.Add(new Parameter("idclub", System.Data.DbType.Int32, ClubID.ToString(CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("startDate", System.Data.DbType.Date, dateStart.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("endDate", System.Data.DbType.Date, dateEnd.Date.HasValue() ? dateEnd.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    public void WriteKMLToStream(Stream s, int ClubID, DateTime dateStart, DateTime dateEnd)
    {
        if (s == null)
            throw new ArgumentNullException("s");

        // Get the flight IDs that contribute to the report
        SetParams(ClubID, dateStart, dateEnd);
        List<int> lstIds = new List<int>();
        using (DataView dv = (DataView)sqlDSReports.Select(DataSourceSelectArguments.Empty))
        {
            foreach (DataRowView dr in dv)
                lstIds.Add(Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture));
        }

        string szErr;
        VisitedAirport.AllFlightsAsKML(new FlightQuery(), s, out szErr, lstIds);
        if (!String.IsNullOrEmpty(szErr))
            throw new MyFlightbookException("Error writing KML to stream: " + szErr);
    }

    public void Refresh(int ClubID, DateTime dateStart, DateTime dateEnd)
    {
        SetParams(ClubID, dateStart, dateEnd);
        gvFlyingReport.DataSourceID = sqlDSReports.ID;
        gvFlyingReport.DataBind();
    }

    public void Refresh(int ClubID)
    {
        Refresh(ClubID, new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1), DateTime.Now);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}