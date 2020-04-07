using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class AutofillOptionsChooser : System.Web.UI.UserControl
{
    /// <summary>
    /// Offset to use from Utc
    /// </summary>
    public int TimeZoneOffset { get; set; }

    public AutoFillOptions Options
    {
        get
        {
            int takeoffSpeed = Int32.TryParse(rblTakeOffSpeed.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int toSpeed) ? toSpeed: AutoFillOptions.DefaultTakeoffSpeed;
            AutoFillOptions afo = new AutoFillOptions()
            {
                TimeZoneOffset = TimeZoneOffset,
                TakeOffSpeed = takeoffSpeed,
                LandingSpeed = AutoFillOptions.BestLandingSpeedForTakeoffSpeed(takeoffSpeed),
                IncludeHeliports = ckIncludeHeliports.Checked,
                AutoSynthesizePath = ckEstimateNight.Checked,
                Night = (AutoFillOptions.NightCritera)Enum.Parse(typeof(AutoFillOptions.NightCritera), rblNightCriteria.SelectedValue, true),
                NightLanding = (AutoFillOptions.NightLandingCriteria)Enum.Parse(typeof(AutoFillOptions.NightLandingCriteria), rblNightLandingCriteria.SelectedValue, true),
                RoundToTenth = ckRoundNearest10th.Checked,
                IgnoreErrors = true
            };

            afo.ToCookies(Response.Cookies);
            return afo;
        }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            foreach (int speed in AutoFillOptions.DefaultSpeeds)
            {
                ListItem li = new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.KnotsTemplate, speed), speed.ToString(CultureInfo.InvariantCulture)) { Selected = (speed == value.TakeOffSpeed) };
                rblTakeOffSpeed.Items.Add(li);
            }
            ckIncludeHeliports.Checked = value.IncludeHeliports;
            ckEstimateNight.Checked = value.AutoSynthesizePath;
            ckRoundNearest10th.Checked = value.RoundToTenth;
            rblNightCriteria.SelectedValue = value.Night.ToString();
            rblNightLandingCriteria.SelectedValue = value.NightLanding.ToString();
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Options = new AutoFillOptions(Request.Cookies);
        }
    }
}
