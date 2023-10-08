using MyFlightbook.Histogram;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class Member_Stats : AdminPage
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.admStats;
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanReport);

            if (!IsPostBack)
            {
                await Task.WhenAll(Task.Run(() => { UpdateUserActivity(); }),
                    Task.Run(() => { UpdateFlightsByDate(); }),
                    Task.Run(() => { UpdateFlightsPerUser(); }));
            }
        }

        protected void UpdateUserActivity()
        {
            DataView dv = (DataView)sqlUserActivity.Select(DataSourceSelectArguments.Empty);
            gcUserActivity.ChartData.Clear();
            foreach (DataRowView dr in dv)
            {
                gcUserActivity.ChartData.XVals.Add(new DateTime(Convert.ToInt32(dr["ActivityYear"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["ActivityMonth"], CultureInfo.InvariantCulture), 1));
                gcUserActivity.ChartData.YVals.Add(Convert.ToInt32(dr["UsersWithSessions"], CultureInfo.InvariantCulture));
            }
        }

        protected void UpdateFlightsByDate()
        {
            YearMonthBucketManager bmFlights = new YearMonthBucketManager() { BucketSelectorName = "DateRange" };

            List<SimpleDateHistogrammable> lstFlightsByDate = new List<SimpleDateHistogrammable>();

            DBHelper dbh = new DBHelper(@"SELECT 
    COUNT(*) AS numflights,
    DATE_FORMAT(f.date, '%Y-%m-01') AS creationbucket
FROM
    flights f
GROUP BY YEAR(f.date), MONTH(f.date)
ORDER BY creationbucket ASC");
            dbh.CommandArgs.Timeout = 300;
            dbh.ReadRows((comm) => { }, (dr) => { lstFlightsByDate.Add(new SimpleDateHistogrammable() { Date = Convert.ToDateTime(dr["creationbucket"], CultureInfo.InvariantCulture), Count = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture) }); });

            HistogramManager hmFlightsByDate = new HistogramManager()
            {
                SourceData = lstFlightsByDate,
                SupportedBucketManagers = new BucketManager[] { bmFlights },
                Values = new HistogramableValue[] { new HistogramableValue("DateRange", "Flights", HistogramValueTypes.Time) }
            };

            bmFlights.ScanData(hmFlightsByDate);

            using (DataTable dt = bmFlights.ToDataTable(hmFlightsByDate))
            {
                gcFlightsOnSite.ChartData.Clear();
                foreach (DataRow dr in dt.Rows)
                {
                    gcFlightsOnSite.ChartData.XVals.Add((string)dr["DisplayName"]);
                    gcFlightsOnSite.ChartData.YVals.Add((int)Convert.ToDouble(dr["Flights"], CultureInfo.InvariantCulture));
                    gcFlightsOnSite.ChartData.Y2Vals.Add((int)Convert.ToDouble(dr["Flights Running Total"], CultureInfo.InvariantCulture));
                }
                // gcFlightsOnSite.TickSpacing = Math.Max(1, gcFlightsOnSite.XVals.Count / 15);
                gvFlightsData.DataSource = dt;
                gvFlightsData.DataBind();
            }
        }

        protected void UpdateFlightsPerUser()
        {
            NumericBucketmanager bmFlightsPerUser = new NumericBucketmanager() { BucketForZero = true, BucketWidth = 100, BucketSelectorName = "FlightCount" };
            List<SimpleCountHistogrammable> lstFlightsPerUser = new List<SimpleCountHistogrammable>();
            HistogramManager hmFlightsPerUser = new HistogramManager()
            {
                SourceData = lstFlightsPerUser,
                SupportedBucketManagers = new BucketManager[] { bmFlightsPerUser },
                Values = new HistogramableValue[] { new HistogramableValue("Range", "Flights", HistogramValueTypes.Integer) }
            };

            // For performance, MUCH faster to find the users without flights first, then find those with flights (use an inner join rather than the overall left join).
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT 
    COUNT(u.username) AS numusers, 0 AS numflights
FROM
    users u
        LEFT JOIN
    flights f ON u.username = f.username
WHERE {0}
    f.username IS NULL;", String.IsNullOrEmpty(cmbNewUserAge.SelectedValue) ? string.Empty : "u.creationdate > ?creationDate AND"));

            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("creationDate", String.IsNullOrEmpty(cmbNewUserAge.SelectedValue) ? DateTime.MinValue : DateTime.Now.AddMonths(-Convert.ToInt32(cmbNewUserAge.SelectedValue, CultureInfo.InvariantCulture))); },
                (dr) => { lstFlightsPerUser.Add(new SimpleCountHistogrammable() { Count = Convert.ToInt32(dr["numusers"], CultureInfo.InvariantCulture), Range = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture) }); });

            // Now get the counts for those WITH flights using an inner join
            dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT 
    COUNT(f2.usercount) AS numusers, f2.numflights
FROM
    (SELECT 
        u.username AS usercount,
            FLOOR((COUNT(f.idflight) + 99) / 100) * 100 AS numflights
    FROM
        users u
    INNER JOIN flights f ON u.username = f.username
    {0}
    GROUP BY u.username
    ORDER BY numflights ASC) f2
GROUP BY f2.numflights
ORDER BY f2.numflights ASC;", String.IsNullOrEmpty(cmbNewUserAge.SelectedValue) ? string.Empty : "WHERE u.creationdate > ?creationDate"));
            dbh.CommandArgs.Timeout = 300;
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("creationDate", String.IsNullOrEmpty(cmbNewUserAge.SelectedValue) ? DateTime.MinValue : DateTime.Now.AddMonths(-Convert.ToInt32(cmbNewUserAge.SelectedValue, CultureInfo.InvariantCulture))); },
                (dr) => { lstFlightsPerUser.Add(new SimpleCountHistogrammable() { Count = Convert.ToInt32(dr["numusers"], CultureInfo.InvariantCulture), Range = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture) }); });

            gcFlightsPerUser.ChartData.TickSpacing = (uint)((lstFlightsPerUser.Count < 20) ? 1 : (lstFlightsPerUser.Count < 100 ? 5 : 10));

            bmFlightsPerUser.ScanData(hmFlightsPerUser);

            using (DataTable dt = bmFlightsPerUser.ToDataTable(hmFlightsPerUser))
            {
                gcFlightsPerUser.ChartData.Clear();
                foreach (DataRow dr in dt.Rows)
                {
                    gcFlightsPerUser.ChartData.XVals.Add((string)dr["DisplayName"]);
                    gcFlightsPerUser.ChartData.YVals.Add((int)Convert.ToDouble(dr["Flights"], CultureInfo.InvariantCulture));
                }

                gvFlightPerUser.DataSource = dt;
                gvFlightPerUser.DataBind();
            }
        }

        protected void cmbNewUserAge_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFlightsPerUser();
        }
    }
}