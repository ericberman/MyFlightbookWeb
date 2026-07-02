using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftSupport.Maintenance.TachTime
{
    #region TachTime data structures
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TachTimeMaintenanceDeadlineType { calendar, time_in_service }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TachTimeMaintenanceStatus { current, due_soon, overdue, unknown }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TachTimeMaintenanceEventType { airframe, engine, propeller, avionics }

    [Serializable]
    public class TachTimeAircraft
    {
        #region Properties
        public string id { get; set; } = string.Empty;
        public string n_number { get; set; } = string.Empty;
        public string make { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public int year { get; set; }
        public string serial_number { get; set; } = string.Empty;
        public object registration_type { get; set; } = null;

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal current_tach_time { get; set; }
        public object current_hobbs_time { get; set; }
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal current_total_time { get; set; }
        public bool tach_equals_airframe { get; set; } = true;
        public DateTime? last_entry_at { get; set; }
        public int needs_review_count { get; set; }
        public int total_entry_count { get; set; }
        #endregion

        public TachTimeAircraft() { }
    }

    [Serializable]
    public class TachTimeAircraftCollectionResponse
    {
        #region Properties
        public IEnumerable<TachTimeAircraft> data { get; set; } = Array.Empty<TachTimeAircraft>();
        public string next_cursor { get; set; } = string.Empty;
        public bool has_more { get; set; } = true;
        #endregion

        public TachTimeAircraftCollectionResponse() { }
    }

    [Serializable]
    public class TachTimeStandardInspectionItem
    {
        #region properties
        public DateTime? last_completed_at { get; set; }

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal last_completed_tach { get; set; }
        public DateTime? due_at { get; set; }

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal due_at_tach { get; set; }
        public TachTimeMaintenanceDeadlineType due_type { get; set; } = TachTimeMaintenanceDeadlineType.calendar;

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal remaining_hours { get; set; }
        public DateTime? battery_replacement_due_at { get; set; }
        public TachTimeMaintenanceStatus status { get; set; } = TachTimeMaintenanceStatus.current;
        #endregion

        public TachTimeStandardInspectionItem() { }
    }

    [Serializable]
    public class TachTimeStandardInspections
    {
        #region properties
        public TachTimeStandardInspectionItem annual_inspection { get; set; }
        public TachTimeStandardInspectionItem inspection_100hr { get; set; }
        public TachTimeStandardInspectionItem altimeter_check { get; set; }
        public TachTimeStandardInspectionItem transponder_check { get; set; }
        public TachTimeStandardInspectionItem pitot_static_check { get; set; }
        public TachTimeStandardInspectionItem elt_inspection { get; set; }
        #endregion

        public TachTimeStandardInspections() { }
    }

    [Serializable]
    public class TachTimeAdditionalInspection : IExternalCurrencyStatus
    {
        #region properties
        [JsonProperty("name")]
        public string InternalName { get; set; } = string.Empty;

        [JsonProperty("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonProperty("ad_number")]
        public string AD_Number { get; set; } = string.Empty;

        [JsonProperty("last_completed_at")]
        public DateTime? DateDone { get; set; }

        [JsonProperty("last_completed_tach")]
        [JsonConverter(typeof(FlexibleDecimalConverter))] 
        public decimal TachDone { get; set; }

        [JsonProperty("due_at")]
        public DateTime? DateDue { get; set; }

        [JsonProperty("due_at_tach")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal TachDue { get; set; }

        [JsonProperty("due_type")]
        public TachTimeMaintenanceDeadlineType Due_Type { get; set; } = TachTimeMaintenanceDeadlineType.calendar;

        [JsonProperty("remaining_hours")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal RemainingHours { get; set; }

        [JsonProperty("status")]
        public TachTimeMaintenanceStatus Status { get; set; } = TachTimeMaintenanceStatus.current;

        #region remaining IExternalCurrencyStatus
        public bool UsesHours { get { return Due_Type == TachTimeMaintenanceDeadlineType.time_in_service; } }

        public decimal HoursDue { get { return TachDue; } }
        public decimal HoursDone { get { return TachDone; } }
        public string Name { get { return $"{InternalName}{(String.IsNullOrEmpty(AD_Number) ? string.Empty : $" ({AD_Number})")}"; } }
        #endregion
        #endregion

        public TachTimeAdditionalInspection() { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{Name}: ");
            if (!String.IsNullOrEmpty(Kind))
                sb.AppendLine($"{Kind} ");
            if (!String.IsNullOrEmpty(AD_Number))
                sb.AppendLine($"AD {AD_Number} ");
            if (DateDone.HasValue)
                sb.AppendLine($"Done: {DateDone.Value}");
            if (TachDone > 0)
                sb.AppendLine($"Done at tach: {TachDone.FormatDecimal(false, true)}");
            if (DateDue.HasValue)
                sb.AppendLine($"Due at: {DateDue.Value.ToShortDateString()}");
            if (TachDue > 0)
                sb.AppendLine($"Due at tach: {TachDue.FormatDecimal(false, true)}");

            return sb.ToString();
        }
    }

    [Serializable]
    public class TachTimeCompliance
    {
        #region properties
        public string aircraft_id { get; set; }
        public string n_number { get; set; }
        public DateTime? as_of { get; set; }
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal current_tach_time { get; set; }
        public object current_hobbs_time { get; set; }
        [JsonConverter(typeof(FlexibleDecimalConverter))] 
        public decimal current_total_time { get; set; }
        public int needs_review_count { get; set; }
        public int total_entry_count { get; set; }
        public TachTimeStandardInspections standard_inspections { get; set; }
        public IEnumerable<TachTimeAdditionalInspection> additional_items { get; set; } = Array.Empty<TachTimeAdditionalInspection>();
        public IEnumerable<string> alerts { get; set; } = Array.Empty<string>();
        #endregion

        public TachTimeCompliance() { }

        private DateTime DateIfLater(DateTime? proposed, DateTime def)
        {
            if (proposed == null)
                return def;
            DateTime dt = DateTime.SpecifyKind(proposed.Value, DateTimeKind.Utc).Date;
            return dt.CompareTo(def) > 0 ? dt : def;
        }

        public MaintenanceRecord ToMaintenanceRecord(MaintenanceRecord mr = null)
        {
            MaintenanceRecord r = new MaintenanceRecord(mr);

            r.Last100 = standard_inspections.inspection_100hr.last_completed_tach > (mr?.Last100 ?? 0) ? standard_inspections.inspection_100hr.last_completed_tach : r.Last100;
            r.LastAnnual = DateIfLater(standard_inspections.annual_inspection.last_completed_at, r.LastAnnual);
            r.LastAltimeter = DateIfLater(standard_inspections.altimeter_check.last_completed_at, r.LastAltimeter);
            r.LastELT = DateIfLater(standard_inspections.elt_inspection.last_completed_at, r.LastELT);
            r.LastStatic = DateIfLater(standard_inspections.pitot_static_check.last_completed_at, r.LastStatic);
            r.LastTransponder = DateIfLater(standard_inspections.transponder_check.last_completed_at, r.LastTransponder);

            return r;
        }
    }

    [Serializable]
    public class TachTimeMaintenanceEvent
    {
        public string id { get; set; } = string.Empty;
        public DateTime date { get; set; }

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal tach_time { get; set; }
        public object hobbs_time { get; set; }

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal total_time { get; set; }
        public TachTimeMaintenanceEventType kind { get; set; } = TachTimeMaintenanceEventType.airframe;
        public string description { get; set; } = string.Empty;
        public string signed_by { get; set; } = string.Empty;
        public DateTime? signature_date { get; set; }
        public IEnumerable<string> entry_categories { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{date.ToShortDateString()}: ");
            if (tach_time > 0)
                sb.AppendLine($"Tach: {tach_time.FormatDecimal(false, true)} ");
            if (total_time > 0 && total_time != tach_time)
                sb.AppendLine($"Total time: {total_time.FormatDecimal(false, true)} ");
            sb.Append($"({kind}) ");
            if (!String.IsNullOrWhiteSpace(description))
                sb.AppendLine($"{description} ");
            if (!String.IsNullOrWhiteSpace(signed_by))
                sb.AppendLine($"Signed by: {signed_by} ");
            if (signature_date.HasValue)
                sb.AppendLine($"({signature_date.Value.ToShortDateString()}) ");
            if (entry_categories.Count() > 0)
                sb.AppendLine($"Categories: {String.Join("; ", entry_categories)}");
            return sb.ToString();
        }

        public MaintenanceLog ToMaintenanceLog(int idAircraft = Aircraft.idAircraftUnknown, string user = null)
        {
            return new MaintenanceLog()
            {
                ChangeDate = date,
                Description = ToString(),
                User = user,
                UserFullName = String.IsNullOrEmpty(user) ? user : util.RequestContext.GetUser(user).UserFullName,
                AircraftID = idAircraft
            };
        }
    }


    [Serializable]
    public class TachTimeMaintenanceEventCollectionResponse
    {
        #region Properties
        public IEnumerable<TachTimeMaintenanceEvent> data { get; set; } = Array.Empty<TachTimeMaintenanceEvent>();
        public string next_cursor { get; set; } = string.Empty;
        public bool has_more { get; set; } = true;
        #endregion

        public TachTimeMaintenanceEventCollectionResponse() { }
    }

    [Serializable]
    public class TachTimeAggregatedDataForAircraft
    {
        #region Properties
        public TachTimeAircraft AircraftDetails { get; set; }
        public TachTimeCompliance Inspections { get; set; }
        public IEnumerable<TachTimeMaintenanceEvent> MaintenanceHistory { get; set; }

        public IEnumerable<TachTimeAircraft> AllTachTimeAircraft { get; set; }
        #endregion
    }
    #endregion

    public class TachTimeRecord : ExternalMaintenanceRecord
    {
        public TachTimeRecord() : base()
        {
            DataSource = ExternalMaintenanceSourceID.TachTime;
        }

        public override object DataAsType => AggregatedData;

        private TachTimeAggregatedDataForAircraft AggregatedData { get; set; }

        /// <summary>
        /// Create a new TachTime record for the specified user/aircraft pair, initialized with the provided data
        /// </summary>
        /// <param name="username"></param>
        /// <param name="aircraftID"></param>
        /// <param name="jsonData"></param>
        public TachTimeRecord(string username, int aircraftID, string jsonData) : this()
        {
            Username = username;
            AircraftID = aircraftID;
            JSONData = jsonData;
            AggregatedData = JsonConvert.DeserializeObject<TachTimeAggregatedDataForAircraft>(jsonData);
        }

        public TachTimeRecord(string username, int aircraftID, TachTimeAggregatedDataForAircraft data) : this()
        {
            Username = username;
            AircraftID = aircraftID;
            AggregatedData = data;
            JSONData = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented });
            HighWaterHobbs = 0;
            HighWaterTach = data.AircraftDetails.current_tach_time;
        }

        public override string SourceName { get { return "TachTime"; } }

        public override IEnumerable<MaintenanceLog> GetMaintenanceLog()
        {
            List<MaintenanceLog> lst = new List<MaintenanceLog>();
            foreach (TachTimeMaintenanceEvent ttme in AggregatedData.MaintenanceHistory)
                lst.Add(ttme.ToMaintenanceLog(AircraftID, Username));
            return lst;
        }

        public override IEnumerable<IExternalCurrencyStatus> GetExternalCurrencies()
        {
            return AggregatedData.Inspections.additional_items;
        }
    }
}
