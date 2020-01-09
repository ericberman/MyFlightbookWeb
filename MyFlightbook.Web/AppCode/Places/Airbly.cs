using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Telemetry
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class AirblyResponse
    {
        #region properties
        public string cmd { get; set; }
        public string error { get; set; }
        public bool authSuccess { get; set; }
        public Int64 timeStamp { get; set; }

        private Dictionary<string, AirblyPoint> m_dictPoints = new Dictionary<string, AirblyPoint>();
        public IDictionary<string, AirblyPoint> points { get { return m_dictPoints; } }
        #endregion

        public AirblyResponse()
        {
            cmd = error = string.Empty;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class AirblyPoint
    {
        #region properties
        public Int32 happenedAt { get; set; }
        public int blesyncedat { get; set; }
        public string type { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
        public int coPPM { get; set; }
        public double cabinAltitude { get; set; }
        public string gForce { get; set; }
        public int pitch { get; set; }
        public int roll { get; set; }
        public double verticalSpeed { get; set; }
        public bool alarmSOS { get; set; }
        public bool alarmAck { get; set; }
        public bool alarmLowPress { get; set; }
        public bool alarmG { get; set; }
        public bool alarmPitchRoll { get; set; }
        public bool alarmClimbDescent { get; set; }

        public DateTime Timestamp { get { return new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(happenedAt); } }

        #endregion

        public AirblyPoint()
        {
            type = gForce = string.Empty;
        }
    }

    public class Airbly : TelemetryParser
    {
        public Airbly() { }

        #region TelemetryParser
        public override bool CanParse(string szData)
        {
            return szData != null && szData.StartsWith("{", StringComparison.CurrentCultureIgnoreCase) && szData.Contains("happenedAt");
        }

        public override bool Parse(string szData)
        {
            if (szData == null)
                throw new ArgumentNullException("szData");

            bool fResult = true;

            AirblyResponse ar = JsonConvert.DeserializeObject<AirblyResponse>(szData);

            ParsedData.Clear();
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.DATE, typeof(DateTime)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.ALT, typeof(Int32)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.LON, typeof(double)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.LAT, typeof(double)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.COMMENT, typeof(string)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.PITCH, typeof(Int32)));
            ParsedData.Columns.Add(new DataColumn(KnownColumnNames.ROLL, typeof(Int32)));

            // ParsedData.Columns.Add(new DataColumn(KnownColumnNames.SPEED, typeof(double)));

            List<AirblyPoint> lstPoints = new List<AirblyPoint>(ar.points.Values);
            lstPoints.Sort((ap1, ap2) => { return ap1.happenedAt.CompareTo(ap2.happenedAt); });

            foreach (AirblyPoint ap in lstPoints)
            {
                DataRow dr = ParsedData.NewRow();
                dr[KnownColumnNames.DATE] = ap.Timestamp;
                dr[KnownColumnNames.ALT] = ap.altitude;
                dr[KnownColumnNames.LAT] = ap.latitude;
                dr[KnownColumnNames.LON] = ap.longitude;
                dr[KnownColumnNames.COMMENT] = ap.type;
                dr[KnownColumnNames.PITCH] = ap.pitch;
                dr[KnownColumnNames.ROLL] = ap.roll;
                ParsedData.Rows.Add(dr);
            }

            return fResult;
        }

        public override FlightData.AltitudeUnitTypes AltitudeUnits
        {
            get { return FlightData.AltitudeUnitTypes.Feet; }
        }

        public override FlightData.SpeedUnitTypes SpeedUnits
        {
            get
            {
                return base.SpeedUnits;
            }
        }
        #endregion
    }
}