using Ionic.Zip;
using MyFlightbook;
using MyFlightbook.Telemetry;
using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFlightInfo : System.Web.UI.UserControl
{
    private const string keyFlightData = "PendingFlightData";
    private const string keyCookieSpeed = "autoFillDefaultSpeed";
    private const string keyCookieHeliport = "autoFillDefaultHeliport";
    private const string keyCookieEstimateNight = "autoFillEstimateNight";
    private const string keyCookieNightDef = "autoFillNightDefinition";
    private const string keyCookieNightLandingDef = "autoFillNightLandingDef";

    #region properties
    /// <summary>
    /// The ID of the associated flight.
    /// </summary>
    public int FlightID
    {
        get { return Convert.ToInt32(hdnFlightID.Value, CultureInfo.InvariantCulture); }
        set 
        {
            hdnFlightID.Value = value.ToString(CultureInfo.InvariantCulture);
            lnkFlightData.NavigateUrl = LogbookEntry.IsNewFlightID(value) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}", value);
        }
    }

    public event EventHandler<AutofillEventArgs> AutoFill = null;

    public short InitialTabIndex
    {
        get { return decHobbsStart.TabIndex; }
        set
        {
            short i = value;
            decHobbsStart.TabIndex = i++;
            decHobbsEnd.TabIndex = i++;
            mfbEngineStart.TabIndex = i++;
            mfbEngineEnd.TabIndex = i++;
            mfbFlightStart.TabIndex = i++;
            mfbFlightEnd.TabIndex = i;
        }
    }

    public decimal HobbsStart
    {
        get { return decHobbsStart.Value; }
        set { decHobbsStart.Value = value; }
    }

    public decimal HobbsEnd
    {
        get { return decHobbsEnd.Value; }
        set { decHobbsEnd.Value = value; }
    }

    public DateTime EngineStart
    {
        get { return mfbEngineStart.DateAndTime; }
        set { mfbEngineStart.DateAndTime = value; }
    }

    public DateTime EngineEnd
    {
        get { return mfbEngineEnd.DateAndTime; }
        set { mfbEngineEnd.DateAndTime = value; }
    }

    public DateTime FlightStart
    {
        get { return mfbFlightStart.DateAndTime; }
        set { mfbFlightStart.DateAndTime = value; }
    }

    public DateTime FlightEnd
    {
        get { return mfbFlightEnd.DateAndTime; }
        set { mfbFlightEnd.DateAndTime = value; }
    }

    public Boolean HasFlightData
    {
        set { mvData.SetActiveView(value ? vwData : vwNoData); }
        get { return mvData.GetActiveView() == vwData; }
    }

    /// <summary>
    /// The default date to use when only a time is specified
    /// </summary>
    public DateTime DefaultDate
    {
        get { return mfbEngineStart.DefaultDate; }
        set { mfbEngineEnd.DefaultDate = mfbEngineStart.DefaultDate = mfbFlightEnd.DefaultDate = mfbFlightStart.DefaultDate = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            int defaultSpeed;
            if (Request.Cookies[keyCookieSpeed] == null || !int.TryParse(Request.Cookies[keyCookieSpeed].Value, out defaultSpeed))
                defaultSpeed = AutoFillOptions.DefaultTakeoffSpeed;
            bool includeHeliports;
            if (Request.Cookies[keyCookieHeliport] == null || !bool.TryParse(Request.Cookies[keyCookieHeliport].Value, out includeHeliports))
                includeHeliports = false;
            bool estimateNight = true;
            if (Request.Cookies[keyCookieEstimateNight] == null || !bool.TryParse(Request.Cookies[keyCookieEstimateNight].Value, out estimateNight))
                estimateNight = true;
            AutoFillOptions.NightCritera nightCritera = AutoFillOptions.NightCritera.EndOfCivilTwilight;
            if (Request.Cookies[keyCookieNightDef] == null || !Enum.TryParse<AutoFillOptions.NightCritera>(Request.Cookies[keyCookieNightDef].Value, out nightCritera))
                nightCritera = AutoFillOptions.NightCritera.EndOfCivilTwilight;
            AutoFillOptions.NightLandingCriteria nightLandingCriteria = AutoFillOptions.NightLandingCriteria.SunsetPlus60;
            if (Request.Cookies[keyCookieNightLandingDef] == null || !Enum.TryParse<AutoFillOptions.NightLandingCriteria>(Request.Cookies[keyCookieNightLandingDef].Value, out nightLandingCriteria))
                nightLandingCriteria = AutoFillOptions.NightLandingCriteria.SunsetPlus60;

            foreach (int speed in AutoFillOptions.DefaultSpeeds)
            {
                ListItem li = new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.FlightData.KnotsTemplate, speed), speed.ToString(CultureInfo.InvariantCulture)) { Selected = (speed == defaultSpeed) };
                rblTakeOffSpeed.Items.Add(li);
            }
            ckIncludeHeliports.Checked = includeHeliports;
            ckEstimateNight.Checked = estimateNight;
            rblNightCriteria.SelectedValue = nightCritera.ToString();
            rblNightLandingCriteria.SelectedValue = nightLandingCriteria.ToString();
        }
    }

    public string Telemetry
    {
        get
        {
            string sz = string.Empty;
            if (mfbUploadFlightData.HasFile && mfbUploadFlightData.PostedFile.ContentLength > 0)
            {
                // check for zip
                try
                {
                    using (ZipFile z = ZipFile.Read(mfbUploadFlightData.PostedFile.InputStream))
                    {
                        foreach (ZipEntry ze in z.Entries)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                ze.Extract(ms);
                                StreamReader sr = new StreamReader(ms);
                                ms.Seek(0, SeekOrigin.Begin);
                                ViewState[keyFlightData] = sz = sr.ReadToEnd();
                                return sz;
                            }
                        }
                    }
                }
                catch (ZipException) { }
                catch (IOException) { }
                catch (ArgumentException) { }

                byte[] rgbytes = new byte[mfbUploadFlightData.PostedFile.ContentLength];
                mfbUploadFlightData.PostedFile.InputStream.Read(rgbytes, 0, mfbUploadFlightData.PostedFile.ContentLength);
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                sz = enc.GetString(rgbytes);
                mfbUploadFlightData.PostedFile.InputStream.Close();
                ViewState[keyFlightData] = sz;
            }
            else if (!String.IsNullOrEmpty((string)ViewState[keyFlightData]))
                sz = ViewState[keyFlightData].ToString();

            return sz;
        }
        set 
        { 
            ViewState[keyFlightData] = value;
            HasFlightData = !String.IsNullOrEmpty(value);
        }
    }

    protected void DeleteData()
    {
        Telemetry = string.Empty;
        HasFlightData = false;
    }

    protected void lnkUploadNewData_Click(object sender, EventArgs e)
    {
        DeleteData();
    }

    protected void lnkDeletedata_Click(object sender, EventArgs e)
    {
        if (FlightID >= 0)
        {
            LogbookEntry le = new LogbookEntry();
            if (le.FLoadFromDB(FlightID, Page.User.Identity.Name))
            {
                le.FlightData = null;
                le.FCommit(true);
                DeleteData();
            }
        }
        else
            DeleteData();
    }

    protected void onAutofill(object sender, EventArgs e)
    {
        if (this.AutoFill != null)
        {
            int takeoffSpeed = Convert.ToInt32(rblTakeOffSpeed.SelectedValue, CultureInfo.InvariantCulture);
            AutoFillOptions afo = new AutoFillOptions()
            {
                TimeZoneOffset = mfbTimeZone1.TimeZoneOffset,
                TakeOffSpeed = takeoffSpeed,
                LandingSpeed = AutoFillOptions.BestLandingSpeedForTakeoffSpeed(takeoffSpeed),
                IncludeHeliports = ckIncludeHeliports.Checked,
                AutoSynthesizePath = ckEstimateNight.Checked,
                Night = (AutoFillOptions.NightCritera) Enum.Parse(typeof(AutoFillOptions.NightCritera), rblNightCriteria.SelectedValue, true),
                NightLanding = (AutoFillOptions.NightLandingCriteria)Enum.Parse(typeof(AutoFillOptions.NightLandingCriteria), rblNightLandingCriteria.SelectedValue, true),
                IgnoreErrors = true
            };

            Response.Cookies[keyCookieSpeed].Value = takeoffSpeed.ToString(CultureInfo.InvariantCulture);
            Response.Cookies[keyCookieHeliport].Value = ckIncludeHeliports.Checked.ToString(CultureInfo.InvariantCulture);
            Response.Cookies[keyCookieEstimateNight].Value = ckEstimateNight.Checked.ToString(CultureInfo.InvariantCulture);
            Response.Cookies[keyCookieNightDef].Value = afo.Night.ToString();
            Response.Cookies[keyCookieNightLandingDef].Value = afo.NightLanding.ToString();

            Response.Cookies[keyCookieSpeed].Expires =
                Response.Cookies[keyCookieHeliport].Expires =
                Response.Cookies[keyCookieEstimateNight].Expires = 
                Response.Cookies[keyCookieNightDef].Expires =
                Response.Cookies[keyCookieNightLandingDef].Expires =
                    DateTime.Now.AddYears(10);
            

            string szTelemetry = Telemetry;
            // Load from the DB if needed
            if (String.IsNullOrEmpty(szTelemetry) && !LogbookEntry.IsNewFlightID(FlightID))
                szTelemetry = new LogbookEntry(FlightID, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll).FlightData;

            this.AutoFill(this, new AutofillEventArgs(afo, szTelemetry));
        }
    }
}
