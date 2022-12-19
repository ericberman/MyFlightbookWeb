using MyFlightbook.Telemetry;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls.FlightEditing
{
    public partial class mfbFlightInfo : UserControl
    {
        private const string keyFlightData = "PendingFlightData";

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

        public event EventHandler<AutofillEventArgs> AutoFill;

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

        protected TimeZoneInfo UserTimeZone { get; set; }
        #endregion

        protected void Page_Init(object sender, EventArgs e)
        {
            UserTimeZone = Page.User.Identity.IsAuthenticated ? MyFlightbook.Profile.GetUser(Page.User.Identity.Name).PreferredTimeZone : TimeZoneInfo.Utc;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                lblEngine.Text = Resources.LogbookEntry.FieldEngineUTC.IndicateUTCOrCustomTimeZone(UserTimeZone);
                lblFlight.Text = Resources.LogbookEntry.FieldFlightUTC.IndicateUTCOrCustomTimeZone(UserTimeZone);
                lblEngine.ToolTip = lblFlight.ToolTip = (UserTimeZone.Id.CompareCurrentCultureIgnoreCase(TimeZoneInfo.Utc.Id) == 0) ? string.Empty : UserTimeZone.DisplayName;
            }

            decHobbsStart.CrossFillScript = String.Format(CultureInfo.InvariantCulture, "getHobbsFill(currentlySelectedAircraft, '{0}')", ResolveClientUrl("~/Member/Ajax.asmx"));
        }

        public string Telemetry
        {
            get
            {
                string sz = string.Empty;
                if (mfbUploadFlightData.HasFile && mfbUploadFlightData.PostedFile.ContentLength > 0)
                {
                    // check for zip - grab the first entry if found.
                    try
                    {
                        using (ZipArchive zipArchive = new ZipArchive(mfbUploadFlightData.PostedFile.InputStream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in zipArchive.Entries)
                            {
                                using (StreamReader sr = new StreamReader(entry.Open()))
                                {
                                    ViewState[keyFlightData] = sz = sr.ReadToEnd();
                                    return sz;
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidDataException) { }

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
                string szTelemetry = Telemetry;
                // Load from the DB if needed
                if (String.IsNullOrEmpty(szTelemetry) && !LogbookEntry.IsNewFlightID(FlightID))
                    szTelemetry = new LogbookEntry(FlightID, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll).FlightData;

                afOptions.TimeZoneOffset = mfbTimeZone1.TimeZoneOffset;
                this.AutoFill(this, new AutofillEventArgs(afOptions.Options, szTelemetry));
                afOptions.Options.SaveForUser(Page.User.Identity.Name);
            }
        }
    }
}