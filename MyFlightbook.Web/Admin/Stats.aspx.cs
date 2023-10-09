using MyFlightbook.Histogram;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
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
                AdminStats astats = new AdminStats();

                // Pound the database to gather all of the stats - can be slow
                if (await astats.Refresh(true))
                {
                    adminStats1.Refresh(astats);
                    UpdateUserActivity(astats.UserActivity);
                    UpdateFlightsByDate(astats.FlightsByDate);
                    UpdateFlightsPerUser(astats.FlightsPerUser);  // astats.Refresh computed flights per user for all time.
                }
            }
        }

        protected void UpdateUserActivity(IEnumerable<UserActivityStats> stats)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            foreach (UserActivityStats uas in stats)
            {
                gcUserActivity.ChartData.XVals.Add(uas.Date);
                gcUserActivity.ChartData.YVals.Add(uas.Count);
            }
        }

        protected void UpdateFlightsByDate(IEnumerable<FlightsByDateStats> lstFlightsByDate)
        {
            if (lstFlightsByDate == null)
                throw new ArgumentNullException(nameof(lstFlightsByDate));

            YearMonthBucketManager bmFlights = new YearMonthBucketManager() { BucketSelectorName = "DateRange" };

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

        protected void UpdateFlightsPerUser(IEnumerable<FlightsPerUserStats> lstFlightsPerUser)
        {
            if (lstFlightsPerUser == null)
                throw new ArgumentNullException(nameof(lstFlightsPerUser));
            NumericBucketmanager bmFlightsPerUser = new NumericBucketmanager() { BucketForZero = true, BucketWidth = 100, BucketSelectorName = "FlightCount" };
            HistogramManager hmFlightsPerUser = new HistogramManager()
            {
                SourceData = lstFlightsPerUser,
                SupportedBucketManagers = new BucketManager[] { bmFlightsPerUser },
                Values = new HistogramableValue[] { new HistogramableValue("Range", "Flights", HistogramValueTypes.Integer) }
            };

           gcFlightsPerUser.ChartData.TickSpacing = (uint)((lstFlightsPerUser.Count() < 20) ? 1 : (lstFlightsPerUser.Count() < 100 ? 5 : 10));

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

        protected async void cmbNewUserAge_SelectedIndexChanged(object sender, EventArgs e)
        {
            IEnumerable<FlightsPerUserStats> flightsPerUserStats = null;
            DateTime? creationDate = null;
            if (!String.IsNullOrEmpty(cmbNewUserAge.SelectedValue))
                creationDate = DateTime.Now.AddMonths(-Convert.ToInt32(cmbNewUserAge.SelectedValue, CultureInfo.InvariantCulture));
            
            await Task.Run(() => { flightsPerUserStats = FlightsPerUserStats.Refresh(creationDate); });
            
            UpdateFlightsPerUser(flightsPerUserStats);
        }
    }
}