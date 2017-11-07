using System;
using System.Globalization;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Solar;
using MyFlightbook.Mapping;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2012-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_DayNight : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.tabHome;
        mfbGoogleMapManager1.Map.ClickHandler = "function (point) {clickForAirport(point.latLng);}";

        mfbTypeInDate.DefaultDate = DateTime.Now;

        if (!IsPostBack)
        {
            mfbTypeInDate.Date = DateTime.Now;
        }
    }

    protected double Latitude { get { return txtLat.Text.SafeParseDouble(); } }

    protected double Longitude { get { return txtLon.Text.SafeParseDouble(); } }

    protected DateTime SunRiseUTC { get; set; }
    protected DateTime SunSetUTC { get; set; }

    protected void btnTimes_Click(object sender, EventArgs e)
    {
        bool fValid = pnlDropPin.Visible = pnlKey.Visible = IsValid;
        if (!fValid)
            return;

        DateTime dt = mfbTypeInDate.Date;
        DateTime dtUTC = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
        double lat = Latitude;
        double lon = Longitude;
        SunriseSunsetTimes sst = new SunriseSunsetTimes(dtUTC, lat, lon);
        SunRiseUTC = sst.Sunrise;
        SunSetUTC = sst.Sunset;
        // lblSunRise.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SunriseSunsetTemplate, sst.Sunrise.ToLocalTime().ToLongTimeString(), sst.Sunrise.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
        // lblSunSet.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SunriseSunsetTemplate, sst.Sunset.ToLocalTime().ToLongTimeString(), sst.Sunset.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
        lblUTCDate.Text = dtUTC.ToLongDateString();
        mfbGoogleMapManager1.Map.ZoomFactor = GMap_ZoomLevels.US;
        mfbGoogleMapManager1.Map.MapCenter = new LatLong(lat, lon);

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

                tc.Text = String.Format(CultureInfo.CurrentCulture, "{0}, {1:F1}°", dt2.ToString("HH:mm", CultureInfo.InvariantCulture), sst.SolarAngle);
            }
        }
    }
}