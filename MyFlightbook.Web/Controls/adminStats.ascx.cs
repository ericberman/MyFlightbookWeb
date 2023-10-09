using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class adminStats : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        public void Refresh(AdminStats astats)
        {
            if (astats == null)
                throw new ArgumentNullException(nameof(astats));

            gvMonthlyUsers.DataSource = astats.NewUserStatsMonthly;
            gvMonthlyUsers.DataBind();

            foreach (NewUserStats nus in astats.NewUserStatsMonthly)
            {
                gcNewUsers.ChartData.XVals.Add(nus.DisplayPeriod);
                gcNewUsers.ChartData.YVals.Add(nus.NewUsers);
                gcNewUsers.ChartData.Y2Vals.Add(nus.RunningTotal);
            }

            gvDailyUsers.DataSource = astats.NewUserStatsDaily;
            gvDailyUsers.DataBind();

            gvMiscStats.DataSource = new MiscStats[] { astats.Misc };
            gvMiscStats.DataBind();

            gvMisc.DataSource = new CommunityLogbookStats[] { astats.LogStats };
            gvMisc.DataBind();

            gvAircraft.DataSource = astats.AircraftInstances;
            gvAircraft.DataBind();

            gvPaymentsXActions.DataSource = astats.UserTransactions;
            gvPaymentsXActions.DataBind();

            gvPaymentAmounts.DataSource = astats.AmountTransactions;
            gvPaymentAmounts.DataBind();

            gvUserSources.DataSource = astats.AppSources;
            gvUserSources.DataBind();

            gvWSEvents.DataSource = astats.WSEvents;
            gvWSEvents.DataBind();

            gvOAuthAndPass.DataSource = new OAuthStats[] { astats.OAuth };
            gvOAuthAndPass.DataBind();

            gvUserStats.DataSource = new TotalUserStats[] { astats.UserStats };
            gvUserStats.DataBind();
        }


        protected void btnTrimAuthenticate_Click(object sender, EventArgs e)
        {
            try
            {
                int i = EventRecorder.ADMINTrimOldItems(EventRecorder.MFBEventID.AuthUser);
                i += EventRecorder.ADMINTrimOldItems(EventRecorder.MFBEventID.ExpiredToken);
                lblTrimErr.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:#,##0} items trimmed", i);
            }
            catch (MyFlightbookException ex)
            {
                lblTrimErr.Text = ex.Message;
                lblTrimErr.CssClass = "error";
            }
        }

        protected void btnTrimOAuth_Click(object sender, EventArgs e)
        {
            // Clean up old Nonces and cryptokeys
            new DBHelper("DELETE FROM nonce WHERE timestampUtc < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
            new DBHelper("DELETE FROM cryptokeys WHERE ExpiresUtc < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
            new DBHelper("DELETE FROM passwordresetrequests WHERE expiration < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
        }
    }
}