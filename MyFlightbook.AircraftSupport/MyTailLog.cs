using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftSupport.Maintenance.MyTailLog
{
    [Serializable]
    public class MTLAirworthiness
    {
        [JsonProperty("aircraft_id")] 
        public string AircraftId { get; set; }
        [JsonProperty("tail_number")] 
        public string TailNumber { get; set; }
        [JsonProperty("current_hours")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal CurrentHours { get; set; } // = tach
        [JsonProperty("summary")] 
        public MTLSummary Summary { get; set; }
        [JsonProperty("inspections")] 
        public List<MTLInspection> Inspections { get; set; } = new List<MTLInspection>();
        [JsonProperty("airworthiness_directives")] 
        public List<MTLDirective> ADs { get; set; } = new List<MTLDirective>();

        // Monotonic merge into MFB's native record — advances a deadline, never
        // regresses it. Same shape/contract as TachTimeCompliance.ToMaintenanceRecord.
        public MaintenanceRecord ToMaintenanceRecord(MaintenanceRecord mr = null)
        {
            MaintenanceRecord r = new MaintenanceRecord(mr);
            foreach (MTLInspection i in Inspections)
            {
                switch (i.Kind)
                {
                    case "annual": 
                        r.LastAnnual = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastAnnual); 
                        break;
                    case "transponder": 
                        r.LastTransponder = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastTransponder); 
                        break;
                    case "pitot_static":
                        r.LastStatic = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastStatic);
                        r.LastAltimeter = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastAltimeter); 
                        break;
                    case "elt": 
                        r.LastELT = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastELT); 
                        break;
                    case "vor": 
                        r.LastVOR = ExternalMaintenanceRecord.DateIfLater(i.LastDoneDate, r.LastVOR); 
                        break;
                    case "hundred_hour": 
                        if (i.LastDoneHours > r.Last100) r.Last100 = i.LastDoneHours; 
                        break;
                    case "oil_change": 
                        if (i.LastDoneHours > r.LastOilChange) r.LastOilChange = i.LastDoneHours; 
                        break;
                    case "engine_tbo": 
                        if (i.LastDoneHours > r.LastNewEngine) r.LastNewEngine = i.LastDoneHours; 
                        break;
                }
            }
            return r;
        }
    }

    [Serializable]
    public class MTLSummary
    {
        [JsonProperty("airworthy")]
        public bool Airworthy { get; set; }
        [JsonProperty("overdue")]
        public int Overdue { get; set; }
        [JsonProperty("due_soon")]
        public int DueSoon { get; set; }
    }

    [Serializable]
    public class MTLInspection
    {
        // annual|transponder|pitot_static|elt|vor|hundred_hour|oil_change|engine_tbo|prop_overhaul
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("last_done_date")]
        public DateTime? LastDoneDate { get; set; }

        [JsonProperty("last_done_hours")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal LastDoneHours { get; set; }

        [JsonProperty("next_due_date")]
        public DateTime? NextDueDate { get; set; }

        [JsonProperty("next_due_hours")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal NextDueHours { get; set; }

        [JsonProperty("urgency")]
        public string Urgency { get; set; } // overdue|due_soon|upcoming|none
    }

    // ADs (and any advisory item) implement IExternalCurrencyStatus, exactly like
    // TachTimeAdditionalInspection — surfaced via GetExternalCurrencies(), NOT
    // written as native DeadlineCurrency rows.
    [Serializable]
    public class MTLDirective : IExternalCurrencyStatus
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("last_done_date")]
        public DateTime? DateDone { get; set; }

        [JsonConverter(typeof(FlexibleDecimalConverter))]
        [JsonProperty("last_done_hours")]
        public decimal HoursDone { get; set; }
        
        [JsonProperty("next_due_date")]
        public DateTime? DateDue { get; set; }
        
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        [JsonProperty("next_due_hours")]
        public decimal HoursDue { get; set; }
        
        [JsonProperty("urgency")]
        public string Urgency { get; set; }
        
        public bool UsesHours => !DateDue.HasValue && HoursDue > 0;
        
        public string Name => string.IsNullOrEmpty(Title) ? Reference : $"{Reference} — {Title}";
    }

    public class MyTailLogRecord : ExternalMaintenanceRecord
    {
        #region Properties
        private MTLAirworthiness Data { get; set; }
        public override object DataAsType => Data;
        #endregion
        
        #region constructors
        public MyTailLogRecord() : base()
        {
            DataSource = ExternalMaintenanceSourceID.MyTailLog;
        }
        public MyTailLogRecord(string username, int aircraftID, string json) : this()
        {
            Username = username;
            AircraftID = aircraftID; JSONData = json;
            Data = JsonConvert.DeserializeObject<MTLAirworthiness>(json);
        }

        public MyTailLogRecord(string username, int aircraftID, MTLAirworthiness data) : this()
        {
            Username = username; AircraftID = aircraftID; Data = data;
            JSONData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            HighWaterTach = data.CurrentHours;   // MTL current tach → MFB current tach
            HighWaterHobbs = 0;                  // (optional: set from /hours readings)
        }
        #endregion

        #region ExternalMaintenanceRecord overrides
        public override IEnumerable<IExternalCurrencyStatus> GetExternalCurrencies() =>
            Data?.ADs ?? Enumerable.Empty<IExternalCurrencyStatus>();

        public override IEnumerable<MaintenanceLog> GetMaintenanceLog()
        {
            return Enumerable.Empty<MaintenanceLog>();
        }
        #endregion
    }
}
