using System;
using System.Globalization;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Solar;

/******************************************************
 * 
 * Copyright (c) 2012-2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_DayNight : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.tabHome;
        mfbGoogleMapManager1.Map.ClickHandler = "function (point) {clickForAirport(point.latLng);}";

        if (!IsPostBack)
            txtDate.Text = DateTime.Now.ToShortDateString();
    }

    protected void btnTimes_Click(object sender, EventArgs e)
    {
        DateTime dt = Convert.ToDateTime(txtDate.Text, CultureInfo.CurrentCulture);
        DateTime dtUTC = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
        double lat = Convert.ToDouble(txtLat.Text, CultureInfo.CurrentCulture);
        double lon = Convert.ToDouble(txtLon.Text, CultureInfo.CurrentCulture);
        SunriseSunsetTimes sst = new SunriseSunsetTimes(dtUTC, lat, lon);
        lblSunRise.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SunriseSunsetTemplate, sst.Sunrise.ToLocalTime().ToLongTimeString(), sst.Sunrise.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
        lblSunSet.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SunriseSunsetTemplate, sst.Sunset.ToLocalTime().ToLongTimeString(), sst.Sunset.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));

        // show 5-minute increments throughout the day
        for (int h = 0; h < 24; h++)
        {
            TableRow tr = new TableRow();
            tblDayNight.Rows.Add(tr);
            for (int m = 0; m < 60; m += 5)
            {
                DateTime dt2 = new DateTime(dtUTC.Year, dtUTC.Month, dtUTC.Day, h, m, 0, DateTimeKind.Utc);

                TableCell tc = new TableCell();
                tr.Cells.Add(tc);
                sst = new SunriseSunsetTimes(dt2, lat, lon);
                if (sst.IsFAANight)
                    tc.BackColor = System.Drawing.Color.DarkGray;
                else if (sst.IsNight)
                    tc.BackColor = sst.IsFAACivilNight ? System.Drawing.Color.BlanchedAlmond : System.Drawing.Color.LightGray;

                tc.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SolarAngleTimeTemplate, dt2.ToString("MM/dd HH:mm", CultureInfo.InvariantCulture), sst.SolarAngle);
            }
        }
    }
}