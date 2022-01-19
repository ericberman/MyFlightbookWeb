using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs.ClubControls
{
    public partial class FlyingReport : UserControl, IReportable
    {
        #region Formatting Helpers
        protected static string FullName(string szFirst, string szLast, string szEmail)
        {
            Profile pf = new Profile()
            {
                FirstName = szFirst,
                LastName = szLast,
                Email = szEmail
            };
            return pf.UserFullName;
        }

        protected static string MonthForDate(DateTime dt)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}-{1} ({2})", dt.Year, dt.Month.ToString("00", CultureInfo.InvariantCulture), dt.ToString("MMM", CultureInfo.CurrentCulture));
        }

        protected static string FormattedUTCDate(object o)
        {
            if (o == null)
                return string.Empty;
            if (o is DateTime time)
                return time.UTCFormattedStringOrEmpty(false);
            return string.Empty;
        }
        #endregion

        public void ToStream(Stream s)
        {
            gvFlyingReport.ToCSV(s);
        }

        protected void SetParams(int ClubID, DateTime dateStart, DateTime dateEnd, string szUser, int idAircraft)
        {
            sqlDSReports.SelectParameters.Clear();
            sqlDSReports.SelectParameters.Add(new Parameter("idclub", DbType.Int32, ClubID.ToString(CultureInfo.InvariantCulture)));
            sqlDSReports.SelectParameters.Add(new Parameter("startDate", DbType.Date, dateStart.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            sqlDSReports.SelectParameters.Add(new Parameter("endDate", DbType.Date, dateEnd.Date.HasValue() ? dateEnd.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            if (!String.IsNullOrEmpty(szUser))
                sqlDSReports.SelectParameters.Add(new Parameter("user", DbType.String, szUser));
            if (idAircraft > 0)
                sqlDSReports.SelectParameters.Add(new Parameter("aircraftid", DbType.Int32, idAircraft.ToString(CultureInfo.InvariantCulture)));
        }

        public void WriteKMLToStream(Stream s, int ClubID, DateTime dateStart, DateTime dateEnd, string szUser, int idAircraft)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            RefreshReportQuery(ClubID, dateStart, dateEnd, szUser, idAircraft);

            // Get the flight IDs that contribute to the report
            List<int> lstIds = new List<int>();
            using (DataView dv = (DataView)sqlDSReports.Select(DataSourceSelectArguments.Empty))
            {
                foreach (DataRowView dr in dv)
                    lstIds.Add(Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture));
            }


            if (lstIds.Count == 0)
            {
                using (MyFlightbook.Telemetry.KMLWriter kw = new MyFlightbook.Telemetry.KMLWriter(s))
                {
                    kw.BeginKML();
                    kw.EndKML();
                }
            }
            else
            {
                VisitedAirport.AllFlightsAsKML(new FlightQuery(), s, out string szErr, lstIds);
                if (!String.IsNullOrEmpty(szErr))
                    throw new MyFlightbookException("Error writing KML to stream: " + szErr);
            }
        }

        private void RefreshReportQuery(int ClubID, DateTime dateStart, DateTime dateEnd, string szUser, int idAircraft)
        {
            const string szQTemplate = @"SELECT f.idflight, f.date AS Date, f.TotalFlightTime AS 'Total Time', f.Route, f.HobbsStart AS 'Hobbs Start', f.HobbsEnd AS 'Hobbs End', u.username AS 'Username', u.Firstname, u.LastName, u.Email, ac.Tailnumber AS 'Aircraft',  
fp.decValue AS 'Tach Start', fp2.decValue AS 'Tach End',
f.dtFlightStart AS 'Flight Start', f.dtFlightEnd AS 'Flight End', 
f.dtEngineStart AS 'Engine Start', f.dtEngineEnd AS 'Engine End',
IF (YEAR(f.dtFlightEnd) > 1 AND YEAR(f.dtFlightStart) > 1, (UNIX_TIMESTAMP(f.dtFlightEnd)-UNIX_TIMESTAMP(f.dtFlightStart))/3600, 0) AS 'Total Flight',
IF (YEAR(f.dtEngineEnd) > 1 AND YEAR(f.dtEngineStart) > 1, (UNIX_TIMESTAMP(f.dtEngineEnd)-UNIX_TIMESTAMP(f.dtEngineStart))/3600, 0) AS 'Total Engine',
f.HobbsEnd - f.HobbsStart AS 'Total Hobbs', 
fp2.decValue - fp.decValue AS 'Total Tach',
fp3.decValue AS 'Oil Added',
fp4.decValue AS 'Fuel Added',
fp5.decValue AS 'Fuel Cost',
fp6.decValue AS 'Oil Level',
fp7.decValue AS 'Oil Added 2nd',
fp8.decValue AS 'Fuel Remaining',
IF(f.dualReceived > 0 OR f.cfi > 0, 'Yes', '') AS IsInstruction
FROM flights f 
INNER JOIN clubmembers cm ON f.username = cm.username
INNER JOIN users u ON u.username=cm.username
INNER JOIN clubs c ON c.idclub=cm.idclub
INNER JOIN clubaircraft ca ON (ca.idaircraft=f.idaircraft AND c.idclub=ca.idclub)
INNER JOIN aircraft ac ON (ca.idaircraft=ac.idaircraft AND c.idclub=ca.idclub)
LEFT JOIN flightproperties fp on (fp.idflight=f.idflight AND fp.idproptype=95)
LEFT JOIN flightproperties fp2 on (fp2.idflight=f.idflight AND fp2.idproptype=96)
LEFT JOIN flightproperties fp3 on (fp3.idflight=f.idflight AND fp3.idproptype=365)
LEFT JOIN flightproperties fp4 on (fp4.idflight=f.idflight AND fp4.idproptype=94)
LEFT JOIN flightproperties fp5 on (fp5.idflight=f.idflight AND fp5.idproptype=159)
LEFT JOIN flightproperties fp6 on (fp6.idflight=f.idflight AND fp6.idproptype=650)
LEFT JOIN flightproperties fp7 on (fp7.idflight=f.idflight AND fp7.idproptype=418)
LEFT JOIN flightproperties fp8 on (fp8.idflight=f.idflight AND fp8.idproptype=72)
WHERE
c.idClub = ?idClub AND
f.date >= GREATEST(?startDate, cm.joindate, c.creationDate) AND
f.date <= ?endDate {0} {1}
ORDER BY f.DATE ASC";

            sqlDSReports.SelectCommand = String.Format(CultureInfo.InvariantCulture, szQTemplate, String.IsNullOrEmpty(szUser) ? string.Empty : " AND cm.username=?user ", idAircraft <= 0 ? string.Empty : " AND ca.idaircraft=?aircraftid");

            SetParams(ClubID, dateStart, dateEnd, szUser, idAircraft);
        }

        public void Refresh(int ClubID, DateTime dateStart, DateTime dateEnd, string szUser = null, int idAircraft = Aircraft.idAircraftUnknown)
        {
            RefreshReportQuery(ClubID, dateStart, dateEnd, szUser, idAircraft);
            gvFlyingReport.DataSourceID = sqlDSReports.ID;
            gvFlyingReport.DataBind();
        }

        public void Refresh(int clubID)
        {
            Refresh(clubID, new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1), DateTime.Now);
        }

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}