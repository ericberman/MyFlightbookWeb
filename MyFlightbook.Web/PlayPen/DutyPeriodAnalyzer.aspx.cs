using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.PlayPen
{
    public partial class DutyPeriodAnalyzer : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.User.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException();
        }

        protected void btnViewDutyPeriods_Click(object sender, EventArgs e)
        {
            pnlResults.Visible = true;

            FAR117Currency fAR117Currency = new FAR117Currency(Profile.GetUser(Page.User.Identity.Name).UsesFAR117DutyTimeAllFlights, false);
            DBHelper dbh = new DBHelper(FlightCurrency.CurrencyQuery(FlightCurrency.CurrencyQueryDirection.Descending));
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("UserName", Page.User.Identity.Name);
                    comm.Parameters.AddWithValue("langID", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                },
                (dr) => { fAR117Currency.ExamineFlight(new ExaminerFlightRow(dr)); });

            TimeSpan ts = new TimeSpan(decDayTimeSpan.IntValue, 0, 0, 0);
            DateTime dtStart = DateTime.UtcNow.Subtract(ts);
            lblCutoffDate.Text = String.Format(CultureInfo.CurrentCulture, "Starting date/time: {0}", dtStart.UTCDateFormatString(false));

            IEnumerable<EffectiveDutyPeriod> effectiveDutyPeriods = fAR117Currency.EffectiveDutyPeriods;

            decimal dutyTime = EffectiveDutyPeriod.DutyTimeSince(ts, effectiveDutyPeriods);
            decimal flightdutyTime = EffectiveDutyPeriod.FlightDutyTimeSince(ts, effectiveDutyPeriods);
            decimal rest = (decimal) ((decimal) ts.TotalHours - dutyTime);
            lblTotalFlightDuty.Text = String.Format(CultureInfo.CurrentCulture, "Total Flight Duty: {0:#,##0.0}hrs ({1})", flightdutyTime, flightdutyTime.ToHHMM());
            lblTotalDuty.Text = String.Format(CultureInfo.CurrentCulture, "Total Duty (non-rest): {0:#,##0.0}hrs ({1})", dutyTime, dutyTime.ToHHMM());
            lblTotalRest.Text = String.Format(CultureInfo.CurrentCulture, "Total Rest: {0:#,##0.0}hrs ({1})", rest, rest.ToHHMM());

            // Fill in Rest Since
            DateTime dtLastStart = DateTime.UtcNow;
            foreach (EffectiveDutyPeriod edp in effectiveDutyPeriods)
            {
                edp.RestSince = Math.Max(0, dtLastStart.Subtract(edp.EffectiveDutyEnd).TotalHours);
                dtLastStart = edp.EffectiveDutyStart;
            }

            gvDutyPeriods.DataSource = fAR117Currency.EffectiveDutyPeriods;
            gvDutyPeriods.DataBind();
        }
    }
}