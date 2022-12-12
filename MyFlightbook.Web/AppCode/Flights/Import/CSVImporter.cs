using JouniHeikniemi.Tools.Text;
using MyFlightbook.Telemetry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2008-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    [Serializable]
    public class CSVImporter
    {
        private ImportContext m_ImportContext;
        private List<AircraftImportMatchRow> m_missingAircraft = new List<AircraftImportMatchRow>();

        #region properties
        /// <summary>
        /// Result of last import file run
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Any errors in the last run?
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// The set of flights to import.
        /// </summary>
        public List<LogbookEntry> FlightsToImport { get; private set; }

        /// <summary>
        /// Set of flights to be modified, in original state (for comparison)
        /// </summary>
        public Dictionary<int, LogbookEntry> OriginalFlightsToModify { get; private set; }

        /// <summary>
        /// The aircraft that were encountered that are not yet in the user's profile
        /// </summary>
        public IEnumerable<AircraftImportMatchRow> MissingAircraft
        {
            get { return m_missingAircraft; }
            set { m_missingAircraft = new List<AircraftImportMatchRow>(value); }
        }

        /// <summary>
        /// The primary name used for the tail number column
        /// </summary>
        public static string TailNumberColumnName
        {
            get { return colTail[0]; }
        }

        /// <summary>
        /// The primary name used for the model column
        /// </summary>
        public static string ModelColumnName
        {
            get { return colModelName[0]; }
        }

        /// <summary>
        /// A set of mappings between a user-supplied name for a model (e.g., "Cheyenne") and the model to which it was ultimately assigned (e.g., "PA31")
        /// </summary>
        public IReadOnlyDictionary<string, MakeModel> ModelNameMappings { get; private set; }
        #endregion

        //  TODO: Pull these out of here so that we can reference them from externalformat.
        #region column names
        /*
         * The following are the column headers for known columns for import.  Each is an array of headers, in prioritized order.  
         * So, for example, the comments field has "Comments" as the highest priority column; if that is not found, then "Remarks" will be used next, and so forth.
        */
        private readonly static string[] colFlightID = { "Flight ID" };
        private readonly static string[] colDate = { "Date", "FLT_DATE", "Date Flown", "flight_date" };
        private readonly static string[] colTail = { "Tail Number", "Registration", "Tail", "Ident", "SERIAL_NUM", "ACFT", "Aircraft ID", "Aircraft Ident", "Reg", "Reg.", "AIRCRAFT REGISTRATION", "aircraft_registration", "tailNo", "aircraft_tail_number", "Tail #" };
        private readonly static string[] colAircraftID = { "Aircraft ID" };
        private readonly static string[] colTotal = { "Total Flight Time", "Total Time", "TotalDuration", "Total Duration", "Flt Time", "Block", "HRS", "TIME FLOWN", "FltTime", "Total", "total_time", "totalFlightTime", "flight_time", "total_flight_time", "Duration", "Flight Time" };
        private readonly static string[] colApproaches = { "Approaches", "NumApproaches", "Inst App (D/N)", "Inst App", "IAP", "APPROACHES & TYPE", "instrumentApproaches", "Approaches Instrument" };
        private readonly static string[] colHold = { "Hold", "Holds", "Holding", "Holding Patterns", "nbr_hold" };
        private readonly static string[] colLandings = { "Landings", "LAND_STD", "Lnd" };
        private readonly static string[] colNightLandings = { "FS Night Landings", "flight_nightLandings", "Night Ldg", "Night Ldgs", "Ngt Ldgs", "Full-Stop Night Landings", "LANDINGS NIGHT", "Night Landings", "LandsNight", "landings_night", "ldgs_night", "Ldg Night", "nightLandings", "night_landings", "Night Landing" };
        private readonly static string[] colFullStopLandings = { "FS Day Landings", "flight_dayLandings", "Day Ldg", "Day Ldgs", "Full-Stop Day Landings", "LANDINGS DAY", "Day Landings", "LandsDay", "landings_day", "ldgs_day", "dayLandings", "day_landings", "Ldg Day", "Day Landing" };
        private readonly static string[] colCrossCountry = { "X-Country", "flight_crossCountry", "XCountry", "XC", "X-C", "X CNTY", "X/Ctry", "X/C", "CROSS COUNTRY", "CrossCountry", "Cross Country Duration", "cross_country" };
        private readonly static string[] colNight = { "Night", "flight_night", "Night Duration", "nightTime", "Night Time" };
        private readonly static string[] colIMC = { "IMC", "flight_actualInstrument", "Actual Inst", "INSTRUMENT", "Actual Instrument Duration", "Instr", "Instr.", "Inst", "Actual Instrument", "ifr", "actualInstrument", "actual_instrument" };
        private readonly static string[] colSimIFR = { "Simulated Instrument", "flight_simulatedInstrument", "Hood", "Sim Inst", "Simulated Instrument Duration", "simulated_instrument", "Simulated Instrument (Hood)" };
        private readonly static string[] colGroundSim = { "Ground Simulator", "flight_simulator", "Sim/FTD", "SIMULATOR", "Simulator Duration", "SimTime", "synthetic_training", "simulator_time", "Flight Simulator" };
        private readonly static string[] colDual = { "Dual Received", "flight_dualReceived", "Dualreceived", "Dual Recd", "Dual", "Dual Received Duration", "dual_received", "Dual Received (Student)" };
        private readonly static string[] colCFI = { "CFI", "flight_dualGiven", "DualGiven", "Dual Given", "Dual Given Duration", "flight_instructor", "instructor", "as_instructor", "As Instr.", "IP", "Instructor (PIC)" };
        private readonly static string[] colSIC = { "SIC", "flight_sic", "SECOND IN COMMAND", "SIC Duration", "CoPlt", "Co-Plt", "co_pilot", "copilot", "Co-Pilot" };
        private readonly static string[] colPIC = { "PIC", "flight_pic", "PILOT IN COMMAND", "PIC Duration", "pilot_in_command_time", "Pilot in CMD" };
        private readonly static string[] colRoute = { "Route", "flight_route", "Via", "ROUTE OF FLIGHT", "LOC_INTM", "Route of Flight Via" };
        private readonly static string[] colFrom = { "From", "flight_from", "Departure", "Origin", "LOC_FROM", "DepPlace", "departure_airport_name", "departure_airport", "departureAirport", "Route of Flight From", "ADEP" };
        private readonly static string[] colTo = { "To", "flight_to", "Arrival", "Dest", "Destination", "LOC_TO", "ArrPlace", "arrival_airport_name", "arrival_airport", "arrivalAirport", "Route of Flight To", "ADES" };
        private readonly static string[] colComment = { "Comments", "Remarks", "remarks_and_endorsements", "Comment", "Remark" };
        private readonly static string[] colCatClassOverride = { "Alternate Cat/Class", "Cat/Class Override", "CatClassOverride" };
        private readonly static string[] colEngineStart = { "Engine Start", "Depart" };
        private readonly static string[] colEngineEnd = { "Engine End", "Arrive" };
        private readonly static string[] colFlightStart = { "Flight Start", "FLT_BEGIN", "Time Off", "departure_time", "Flight Departure" };
        private readonly static string[] colFlightEnd = { "Flight End", "FLT_END", "Time On", "arrival_time", "Flight Arrival" };
        private readonly static string[] colHobbsStart = { "Hobbs Start", "Hobbs Out" };
        private readonly static string[] colHobbsEnd = { "Hobbs End", "Hobbs In" };
        private readonly static string[] colPublic = { "Public" };
        private readonly static string[] colModelName = { "Model", "Aircraft Type", "MakeModel", "MAKE & MODEL", "A/C Type", "AIRCRAFT MAKE & MODEL", "ACFT_MDS", "ACFT_MDS", "ACType", "type_of_aircraft", "acModel", "aircraft_type" };
        private readonly static string[] colFlightConditions = { "FS_ID", "CONDITION" };  // For CAFRS - specifies flight conditions
        private readonly static string[] colPilotRole = { "DS_ID", "DUTY", "Duty Posn", "Duty Position" };    // For CAFRS - specifies role of pilot ("Duty Position")
        private readonly static string[] colPilotMission = { "MISSION", "MI_ID"};  // For CAFRS - specifies the mission for the pilot

        /// <summary>
        /// Common aliases for property names
        /// TODO: THIS CURRENTLY IS NOT LOCALIZABLE; SHOULD ADD THE LOCALIZED NAME OF THE PROPERTY TO THE LIST!
        /// </summary>
        private readonly static Dictionary<string, string[]> PropNameAliases = new Dictionary<string, string[]>()
        {
            { "Solo Time", new string[] {"Solo Time", "flight_solo", "Solo", "1"}},
            { "Name of PIC", new string[] {"Name of PIC", "flight_selectedCrewPIC", "PIC/P1 Crew", "Captain", "PIC Name", "PICName", "name_of_pilot_in_command", "pic_name", "Name PIC", "Crew Captain", "COM" }},
            { "Name of SIC", new string[] {"Name of SIC", "SIC/P2 Crew", "First Officer" }},
            { "Instructor Name", new string[] {"Instructor Name", "instructor_name" } },
            { "Takeoffs - Night", new string[] {"Takeoffs - Night", "flight_nightTakeoffs", "Night T/O", "Night Takeoffs", "TKoffsNight", "to_night", "T/O Night", "Night T-O" }},
            { "Landings - Water", new string[] {"Landings - Water", "flight_waterLandings"}},
            { "Takeoffs - Water", new string[] {"Takeoffs - Water", "flight_waterTakeoffs"}},
            { "Takeoffs (any)", new string[] {"Takeoffs (any)", "to_day", "T/O Day", "Day T-O", "Day Takeoffs" } },
            { "Night Vision Time (Goggles)", new string[] { "Night Vision Time (Goggles)", "NVG" } },
            {"Night Vision - Landing", new string[] {"Night Vision - Landing", "flight_nightVisionGoggleLandings"}},
            {"Night Vision - Takeoff", new string[] {"Night Vision - Takeoff", "flight_nightVisionGoggleTakeoffs"}},
            {"Go-arounds", new string[] {"Go-arounds", "flight_goArounds"}},
            {"Duty Time End (UTC)", new string[] {"Duty Time End (UTC)", "flight_offDutyTime"}},
            {"Duty Time Start (UTC)", new string[] {"Duty Time Start (UTC)", "flight_onDutyTime"}},
            {"Part 91 Flight", new string[] {"Part 91 Flight", "flight_faaPart91", "PT91" }},
            {"Part 121 Flight", new string[] {"Part 121 Flight", "flight_faaPart121", "PT121"}},
            {"Part 135 Flight", new string[] {"Part 135 Flight", "flight_faaPart135", "PT135"}},
            {"Tachometer End", new string[] {"Tachometer End", "Tach In"}},
            {"Tachometer Start", new string[] {"Tachometer Start", "Tach Out"}},
            {"Flight Number", new string[] {"Flight Number", "Flight", "flightNo", "Flight No." } },
            {"Block Out Time", new string[] {"Block Out Time", "Block Out", "Off Block", "DepTime", "Departure Time", "off_block", "block_start", "departureTime", "Block Off", "Off", "Time Out" } },
            {"Block In Time", new string[] {"Block In Time", "Block In", "On Block", "ArrTime", "Arrival Time", "on_block", "block_end", "arrivalTime", "Block On", "On", "Time In" } },
            {"Scheduled Departure Time", new string[] { "Scheduled Departure Time", "ETD" } },
            {"Scheduled Arrival Time", new string[] { "Scheduled Arrival Time", "ETA" } },
            {"Ground Instruction Received", new string[] { "Ground Instruction Received", "Ground Instruction Duration" } },
            {"Fuel Remaining At Landing", new string[] { "Fuel Remaining At Landing", "Fuel Remaining" } },
            {"Maximum Altitude", new string[] { "Maximum Altitude", "Max Altitude" } },
            {"Flight Attendant Name(s)", new string[] { "Flight Attendant Name(s)", "Flight Attendant" } },
            {"Military Pilot Primary Time", new string[] {"Military Pilot Primary Time", "PRI", "Primary" } },
            {"Military Pilot Secondary Time", new string[] {"Military Pilot Secondary Time", "SEC", "Secondary" } },
            {"Mission and Sortie", new string[] { "Mission and Sortie", "Mission #" } },
            {"Aircraft Commander Time", new string[] { "Aircraft Commander Time", "A/C Cmdr", "A/C Cdr"} },
            {"Number of Passengers", new string[] {"Number of Passengers", "PAX" } },
            {"Student Name", new string[] {"Student Name", "Student"} },
            {"Approaches - ILS", new string[] {"Approaches - ILS", "nbr_ils"}},
            {"Approaches - LDA", new string[] {"Approaches - LDA", "nbr_lda"}},
            {"Approaches - Localizer", new string[] {"Approaches - Localizer", "nbr_loc"}},
            {"Approaches - MLS", new string[] {"Approaches - MLS", "nbr_mls"}},
            {"Approaches - NDB", new string[] {"Approaches - NDB", "nbr_ndb"}},
            {"Approaches - PAR", new string[] {"Approaches - PAR", "nbr_par"}},
            {"Approaches - RNAV/GPS", new string[] {"Approaches - RNAV/GPS", "nbr_gps"}},
            {"Approaches - SDF", new string[] {"Approaches - SDF", "nbr_sdf"}},
            {"Approaches - SRA/ASR", new string[] {"Approaches - SRA/ASR", "nbr_asr"}},
            {"Approaches - TACAN", new string[] {"Approaches - TACAN", "nbr_tacan"}},
            {"Approaches - VOR", new string[] {"Approaches - VOR", "nbr_vor"}},
            {"Approaches - Localizer Backcourse", new string[] {"Approaches - Localizer Backcourse", "nbr_bc"}},
            {"Approaches - RNAV/GPS (LP)", new string[] {"Approaches - RNAV/GPS (LP)", "nbr_rnav"}},
        };

        public static void InitializeDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            dt.Columns.Add(new DataColumn(colFlightID[0], typeof(int)));
            dt.Columns.Add(new DataColumn(colDate[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colTail[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colAircraftID[0], typeof(int)));
            dt.Columns.Add(new DataColumn(colTotal[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colApproaches[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colHold[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colNightLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colFullStopLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colCrossCountry[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colNight[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colIMC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colSimIFR[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colGroundSim[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colDual[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colCFI[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colSIC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colPIC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colRoute[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colComment[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colCatClassOverride[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colEngineStart[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colEngineEnd[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colFlightStart[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colFlightEnd[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colHobbsStart[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colHobbsEnd[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colModelName[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colPublic[0], typeof(string)));
        }

        public static void WriteEntryToDataTable(LogbookEntryCore le, DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            DataRow dr = dt.NewRow();
            dt.Rows.Add(dr);

            dr[colFlightID[0]] = le.FlightID;
            dr[colDate[0]] = le.Date.ToShortDateString();
            dr[colTail[0]] = le.TailNumDisplay;
            dr[colAircraftID[0]] = le.AircraftID;
            dr[colTotal[0]] = le.TotalFlightTime;
            dr[colApproaches[0]] = le.Approaches;
            dr[colHold[0]] = le.fHoldingProcedures ? 1.FormatBooleanInt() : string.Empty;
            dr[colLandings[0]] = le.Landings;
            dr[colNightLandings[0]] = le.NightLandings;
            dr[colFullStopLandings[0]] = le.FullStopLandings;
            dr[colCrossCountry[0]] = le.CrossCountry;
            dr[colNight[0]] = le.Nighttime;
            dr[colIMC[0]] = le.IMC;
            dr[colSimIFR[0]] = le.SimulatedIFR;
            dr[colGroundSim[0]] = le.GroundSim;
            dr[colDual[0]] = le.Dual;
            dr[colCFI[0]] = le.CFI;
            dr[colSIC[0]] = le.SIC;
            dr[colPIC[0]] = le.PIC;
            dr[colRoute[0]] = le.Route;
            dr[colComment[0]] = le.Comment;
            dr[colCatClassOverride[0]] = le.CatClassOverride;
            dr[colEngineStart[0]] = le.EngineStart.FormatDateZulu();
            dr[colEngineEnd[0]] = le.EngineEnd.FormatDateZulu();
            dr[colFlightStart[0]] = le.FlightStart.FormatDateZulu();
            dr[colFlightEnd[0]] = le.FlightEnd.FormatDateZulu();
            dr[colHobbsStart[0]] = le.HobbsStart;
            dr[colHobbsEnd[0]] = le.HobbsEnd;
            dr[colModelName[0]] = le.ModelDisplay;
            dr[colPublic[0]] = le.fIsPublic ? 1.FormatBooleanInt() : string.Empty;

            if (le.CustomProperties != null)
            {
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    if (cfp.PropertyType == null)
                        cfp.InitPropertyType(CustomPropertyType.GetCustomPropertyTypes());

                    if (dt.Columns[cfp.PropertyType.Title] == null)
                    {
                        DataColumn dc = new DataColumn(cfp.PropertyType.Title, typeof(string));
                        dt.Columns.Add(dc);
                    }

                    dr[cfp.PropertyType.Title] = cfp.ValueString;
                }
            }
        }

        // Resolve a prop name that could be referenced by alias.
        private static string[] ResolvePropNameAlias(string szPropname)
        {
            if (PropNameAliases.TryGetValue(szPropname, out string[] retValue))
                return retValue;
            else
                return new string[] { szPropname };
        }
        #endregion

        #region Helper classes

        [Serializable]
        private class ImportColumn
        {
            public CustomPropertyType m_cpt;
            public int m_iCol = -1;

            public ImportColumn(CustomPropertyType cpt, int iCol)
            {
                m_cpt = cpt;
                m_iCol = iCol;
            }
        }

        private class RowReader
        {
            private readonly ImportContext m_cm;
            private readonly Dictionary<string, List<Aircraft>> dictFoundAircraft = new Dictionary<string, List<Aircraft>>();
            private string[] m_rgszRow;

            public RowReader(ImportContext columnmapper)
            {
                m_cm = columnmapper;
            }

            #region CAFRS helpers
            /*
             * CAFRS is the army reporting system, and it imports more or less directly, but instead of having 
             * separate columns for things like night or PIC, they have a column for flight conditions and a column for role
             */
            private static void AddCrossFilledPropertyWithID(LogbookEntry le, int idPropType, List<CustomFlightProperty> lstProps)
            {
                if (le == null || lstProps == null || le.TotalFlightTime == 0)
                    return;

                // Don't add anything if the property has already been specified explicitly
                if (lstProps.FirstOrDefault(prop => prop.PropTypeID == idPropType) != null)
                    return;

                CustomFlightProperty cfp = le.CustomProperties.GetEventWithTypeIDOrNew(idPropType);
                if (cfp.DecValue == 0)  // might not be the case if we are re-using a property
                    cfp.DecValue = le.TotalFlightTime;
                lstProps.Add(cfp);
            }

            /// <summary>
            /// Adjusts tail number based on containing "(FS)" or "(BS)" (for Front Seat/Back Seat), adding front-seat/back-seat time as appropriate
            /// </summary>
            /// <param name="szTail">Assumed already upper case</param>
            /// <returns></returns>
            private static string CAFRSAdjustTail(LogbookEntry le, string szTail, List<CustomFlightProperty> lstProps)
            {
                if (String.IsNullOrEmpty(szTail) || le == null || lstProps == null)
                    return szTail;

                if (szTail.Contains("(FS)"))
                {
                    AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropFrontSeatTime, lstProps);
                    return szTail.Replace("(FS)", string.Empty);
                }
                if (szTail.Contains("(BS)"))
                {
                    AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropBackSeatTime, lstProps);
                    return szTail.Replace("(BS)", string.Empty);
                }

                return szTail;
            }

            private static void SetCAFRSFlightCondition(LogbookEntry le, string szFlightConditions, List<CustomFlightProperty> lstProps)
            {
                if (String.IsNullOrEmpty(szFlightConditions) || le == null || le.TotalFlightTime == 0)
                    return;

                szFlightConditions = szFlightConditions.ToUpperInvariant().Trim();

                switch (szFlightConditions)
                {
                    default:
                    case "D":   // day - no action
                        break;
                    case "NG":  // Night vision goggles
                    case "NS":  // Night System - FLIR
                    case "N":   // night unaided
                        if (le.Nighttime == 0)
                            le.Nighttime = le.TotalFlightTime;
                        if (szFlightConditions.CompareCurrentCultureIgnoreCase("NG") == 0)
                            AddCrossFilledPropertyWithID(le, (int) CustomPropertyType.KnownProperties.IDPropNVGoggleTime, lstProps);
                        else if (szFlightConditions.CompareCurrentCultureIgnoreCase("NS") == 0)
                            AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropNVFLIRTime, lstProps);
                        break;
                    case "W":   // "Weather" (imc)
                        if (le.IMC == 0)
                            le.IMC = le.TotalFlightTime;
                        break;
                    case "H":   // "Hood"
                        if (le.SimulatedIFR == 0)
                            le.SimulatedIFR = le.TotalFlightTime;
                        break;
                }
            }

            private static void SetCAFRSMissionRole(LogbookEntry le, string szMission, List<CustomFlightProperty> lstProps)
            {
                if (String.IsNullOrEmpty(szMission) || le == null || le.TotalFlightTime == 0)
                    return;

                szMission = szMission.ToUpperInvariant().Trim();

                switch (szMission)
                {
                    default:
                        break;
                    case "C":   // combat
                        AddCrossFilledPropertyWithID(le, (int) CustomPropertyType.KnownProperties.IDPropCombatTime, lstProps);
                        break;
                    case "D":   // imminent danger
                        AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropImminentDanger, lstProps);
                        break;
                    case "F":   // test flight
                        AddCrossFilledPropertyWithID(le, (int) CustomPropertyType.KnownProperties.IDPropTestPilotTime, lstProps);
                        break;
                    case "S":   // Service
                        AddCrossFilledPropertyWithID(le, (int) CustomPropertyType.KnownProperties.IDPropMissionCrewTime, lstProps);
                        break;
                    case "T":   // training - map to dual
                        le.Dual = le.TotalFlightTime;
                        break;
                }
            }

            private static void CrossFillPIC(LogbookEntryCore le)
            {
                if (le.PIC == 0)
                    le.PIC = le.TotalFlightTime;
            }

            private static void CrossFillSIC(LogbookEntryCore le)
            {
                if (le.SIC == 0)
                    le.SIC = le.TotalFlightTime;
            }

            private static void CrossFillCFI(LogbookEntryCore le)
            {
                if (le.CFI == 0)
                    le.CFI = le.TotalFlightTime;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
            private static void SetCAFRSPilotRole(LogbookEntry le, string szPilotRole, List<CustomFlightProperty> lstProps)
            {
                if (String.IsNullOrEmpty(szPilotRole) || le == null || le.TotalFlightTime == 0)
                    return;

                // Look for Primary or Secondary time - disambiguates PIC/SIC in air-force (not really used in Army, I don't think.)
                bool fPrimary = lstProps.Find(cfp => cfp.PropTypeID == (int) CustomPropertyType.KnownProperties.IDPropMilitaryPrimaryTime) != null;
                bool fSecondary = lstProps.Find(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMilitarySecondaryTime) != null;
                bool fCommander = lstProps.Find(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMilitaryAircraftCommander) != null;
                bool fAirForce = fPrimary || fSecondary;    // use this as a proxy for air force rules vs. Army rules.

                szPilotRole = ((szPilotRole.Length > 2) ? szPilotRole.Substring(0, 2) : szPilotRole).ToUpperInvariant();
                switch (szPilotRole)
                {
                    case "PI":  // Second in command
                        CrossFillSIC(le);
                        break;
                    case "XP":  // Experimental Test Pilot
                    case "UT":  // Unit Trainer
                    case "PC":  // PIC
                        CrossFillPIC(le);
                        break;
                    case "CP":  // Co-pilot
                        AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropMilitaryCoPilottime, lstProps);
                        break;
                    case "SP":  // Standardization Instructor Pilot (Army), Student pilot (Air force) - inherently ambiguous and contradictory, but I'm told air force doesn't use this much, so we'll go with Standardization Pilot.
                    case "IP":  // Instructor Pilot.  PIC in army, but PIC/SIC in air-force depending on whether primary or secondary.
                    case "ME":  // Maintenance Test Flight Evaluator
                        CrossFillCFI(le);
                        if (fAirForce && !fCommander)
                            CrossFillSIC(le);
                        else
                            CrossFillPIC(le);
                        break;
                    case "IE":  // Instrument evaluator (Flight examiner)
                        AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropInstrumentExaminer, lstProps);
                        CrossFillCFI(le);
                        CrossFillPIC(le);
                        break;
                    case "MP":  // Maintenance Pilot evaluator (Army) or Mission Pilot (Air Force)
                        if (fAirForce)
                        {
                            if (fCommander)
                                CrossFillPIC(le);
                            else
                                CrossFillSIC(le);
                        }
                        else
                        {
                            AddCrossFilledPropertyWithID(le, (int)CustomPropertyType.KnownProperties.IDPropMaintTestPilot, lstProps);
                            CrossFillPIC(le);
                        }
                        break;
                    case "EP":  // Air-force Evaluator Pilot
                        if (fAirForce || fCommander)
                            CrossFillPIC(le);
                        else
                            CrossFillSIC(le);
                        CrossFillCFI(le);   // it's CFI time regardless.
                        break;
                    case "FP":  // Air-force - first pilot
                    case "MC":  // Air-foce - Mission co-pilot
                        if (fAirForce)
                        {
                            CrossFillSIC(le);
                            if (le.Dual == 0)
                                le.Dual = le.TotalFlightTime;
                        }
                        break;
                    case "UP":  // Air Force - unqualified pilot - just treat as dual if primary or secondary time is logged
                        if (fAirForce && le.Dual == 0)
                            le.Dual = le.TotalFlightTime;
                        break;
                    case "UC":  // Unqualified co-pilot
                    case "AO":  // Aerial observer - doesn't map to anything
                    default:
                        break;
                }
            }
            #endregion

            #region FlightFromRow helpers
            private static readonly Regex regGarminApproaches = new Regex("\\((?<count>\\d+)\\)$", RegexOptions.Compiled);
            private static readonly Regex regForeFlightApproaches = new Regex("^(?<count>\\d+)", RegexOptions.Compiled);
            private void InitFlightFromRowMainFields(LogbookEntry le)
            {
                // Handle approaches where approach might be a string and properly mapped to approach name(s) property
                string szApproaches = GetMappedString(m_cm.iColApproaches);
                int parsedApproaches = 0;
                le.Approaches = String.IsNullOrEmpty(szApproaches) ? 0 :
                        (int.TryParse(szApproaches, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsedApproaches) ? parsedApproaches : 
                        (regForeFlightApproaches.IsMatch(szApproaches) ? Convert.ToInt32(regForeFlightApproaches.Matches(szApproaches)[0].Groups["count"].Value, CultureInfo.CurrentCulture) : 
                        (regGarminApproaches.IsMatch(szApproaches) ? Convert.ToInt32(regGarminApproaches.Matches(szApproaches)[0].Groups["count"].Value, CultureInfo.CurrentCulture) : 0)));

                // If we pulled the approach count out from the approaches above, then add the full string to the approach Name(s) property
                if (parsedApproaches != le.Approaches && !String.IsNullOrEmpty(szApproaches))
                {
                    CustomFlightProperty cfpApproachDesc = le.CustomProperties.GetEventWithTypeIDOrNew(CustomPropertyType.KnownProperties.IDPropApproachName);
                    cfpApproachDesc.TextValue = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, cfpApproachDesc.TextValue, szApproaches).Trim();
                    le.CustomProperties.Add(cfpApproachDesc);
                }

                le.fHoldingProcedures = GetMappedBoolean(m_cm.iColHold);
                le.Landings = GetMappedInt(m_cm.iColLandings);
                le.NightLandings = GetMappedInt(m_cm.iColNightLandings);
                le.FullStopLandings = GetMappedInt(m_cm.iColFullStopLandings);
                le.CrossCountry = GetMappedDecimal(m_cm.iColCrossCountry);
                le.Nighttime = GetMappedDecimal(m_cm.iColNight);
                le.IMC = GetMappedDecimal(m_cm.iColIMC);
                le.SimulatedIFR = GetMappedDecimal(m_cm.iColSimIFR);
                le.GroundSim = GetMappedDecimal(m_cm.iColGroundSim);
                le.Dual = GetMappedDecimal(m_cm.iColDual);
                le.CFI = GetMappedDecimal(m_cm.iColCFI);
                le.SIC = GetMappedDecimal(m_cm.iColSIC);
                le.PIC = GetMappedDecimal(m_cm.iColPIC);

                string szRoute = GetMappedString(m_cm.iColRoute);
                string szFrom = GetMappedString(m_cm.iColFrom);
                string szTo = GetMappedString(m_cm.iColTo);

                // Route is concatenation of From + (Route/Via) + To, if From/To fields are present AND if not redundant.
                if (!String.IsNullOrEmpty(szFrom) && !szRoute.StartsWith(szFrom, StringComparison.CurrentCultureIgnoreCase))
                    szRoute = szFrom + " " + szRoute;
                if (!String.IsNullOrEmpty(szTo) && !szRoute.EndsWith(szTo, StringComparison.CurrentCultureIgnoreCase))
                    szRoute = szRoute + " " + szTo;
                le.Route = szRoute.Trim();

                le.Comment = GetMappedString(m_cm.iColComment);

                le.EngineStart = GetMappedUTCDate(m_cm.iColEngineStart, le.Date);
                le.EngineEnd = GetMappedUTCDate(m_cm.iColEngineEnd, le.Date);
                le.FlightStart = GetMappedUTCDate(m_cm.iColFlightStart, le.Date);
                le.FlightEnd = GetMappedUTCDate(m_cm.iColFlightEnd, le.Date);
                if (le.EngineEnd.CompareTo(le.EngineStart) < 0)
                    le.EngineEnd = le.EngineEnd.AddDays(1);
                if (le.FlightEnd.CompareTo(le.FlightStart) < 0)
                    le.FlightEnd = le.FlightEnd.AddDays(1);
                le.HobbsStart = GetMappedDecimal(m_cm.iColHobbsStart);
                le.HobbsEnd = GetMappedDecimal(m_cm.iColHobbsEnd);
                if (m_cm.iColPublic >= 0)   // only set public flag if the column is present in the file being imported.
                    le.fIsPublic = GetMappedBoolean(m_cm.iColPublic);
            }

            private void InitFlightFromRowProperties(LogbookEntry le, out string szTail)
            {
                List<CustomFlightProperty> lstCustPropsForFlight = new List<CustomFlightProperty>();
                foreach (ImportColumn ic in m_cm.CustomPropertiesToImport)
                {
                    string szVal = m_rgszRow[ic.m_iCol];
                    if (!String.IsNullOrWhiteSpace(szVal))
                    {
                        try
                        {
                            // Re-use the existing property if possible.
                            CustomFlightProperty cfp = le.CustomProperties.GetEventWithTypeIDOrNew(ic.m_cpt.PropTypeID);

                            cfp.InitFromString(szVal, le.Date);
                            if (!cfp.IsDefaultValue)
                                lstCustPropsForFlight.Add(cfp);
                        }
                        catch
                        {
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotImportProperty, ic.m_cpt.Title, szVal));
                        }
                    }
                }

                // Fix up block times too
                CustomFlightProperty blockIn = lstCustPropsForFlight.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDBlockIn);
                CustomFlightProperty blockOut = lstCustPropsForFlight.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDBlockOut);

                if (blockIn != null && blockOut != null && blockIn.DateValue.CompareTo(blockOut.DateValue) < 0)
                    blockIn.DateValue = blockIn.DateValue.AddDays(1);

                // CAFRS support - read pilot role and flight conditions IF not already filled in above.
                SetCAFRSFlightCondition(le, GetMappedString(m_cm.iColFlightConditions), lstCustPropsForFlight);
                SetCAFRSPilotRole(le, GetMappedString(m_cm.iColPilotRole), lstCustPropsForFlight);
                SetCAFRSMissionRole(le, GetMappedString(m_cm.iColMission), lstCustPropsForFlight);

                // we now have, from above, a set of custom properties to import.
                // BUT...if this is an existing flight, then some of those flights may already
                // have props.  If so, we want to hold on to those props so that we delete them
                // when we add these.
                // BUT...since we commit the flight before updating the new properties, we don't
                // want to delete properties that have had new values assigned.  SO, those are NOT considered orphans
                if (!le.IsNewFlight)
                {
                    List<CustomFlightProperty> lstExisting = new List<CustomFlightProperty>(le.CustomProperties);
                    lstExisting.RemoveAll(cfp => lstCustPropsForFlight.Contains(cfp));   // remove any flight props that are in the ones to import - these are updates, not orphans.
                    m_cm.OrphanedPropsByFlightID[le.FlightID] = lstExisting;
                }

                // check that we know about the aircraft or, if not, if it's in the system then add it for the user.
                szTail = Aircraft.NormalizeTail(le.TailNumDisplay = CAFRSAdjustTail(le, m_rgszRow[m_cm.iColTail].Trim().ToUpperInvariant(), lstCustPropsForFlight));

                le.CustomProperties.SetItems(lstCustPropsForFlight);
            }

            private void InitFlightAircraftFromRow(LogbookEntry le, string szTail, int idAircraft)
            {
                Aircraft ac = null;
                if (String.IsNullOrWhiteSpace(szTail.Trim()))
                    szTail = CountryCodePrefix.szAnonPrefix;

                if (m_cm.AircraftForUser.ContainsKey(szTail))
                {
                    if (idAircraft > 0)
                    {
                        UserAircraft ua = new UserAircraft(m_cm.User);
                        Aircraft acByID = ua.GetUserAircraftByID(idAircraft);
                        if (acByID != null && Aircraft.NormalizeTail(acByID.TailNumber).CompareCurrentCultureIgnoreCase(szTail) == 0)   // it matches - use aircraft ID for disambiguation
                            ac = acByID;
                    }
                }
                else
                {
                    if (!dictFoundAircraft.ContainsKey(szTail)) // Avoid more than one DB hit per aircraft
                        dictFoundAircraft[szTail] = Aircraft.AircraftMatchingTail(szTail);
                    List<Aircraft> lst = dictFoundAircraft[szTail];
                    if (lst.Count == 1) // it exists and there are no alternative versions (i.e., no ambiguity) - just go ahead and add it.
                    {
                        m_cm.AircraftForUser[szTail] = lst[0];
                        new UserAircraft(m_cm.User).FAddAircraftForUser(lst[0]);
                    }
                    else
                    {
                        /* 
                         * Aircraft not found - 3 scenarios
                         *  a) No model column or no model specified - just throw a "no aircraft found" exception.
                         *  b) Model column specified - add it to the list of aircraft to import.  Still throw the "no aircraft found" exception, because we need to resolve this before import
                         *  c) anonymous or sim prefix - look it up in the user's profile and match to that if found, and then continue.
                         */
                        bool fFoundAnonOrSim = false;
                        string szModel = string.Empty;

                        if (m_cm.iColModel >= 0 && !String.IsNullOrEmpty(szModel = m_rgszRow[m_cm.iColModel]))
                        {
                            MakeModel mappedModel = (m_cm.ModelMapping != null && m_cm.ModelMapping.ContainsKey(szModel)) ? m_cm.ModelMapping[szModel] : null;   // see if we have a mapping for this, BEFORE trimming the comma

                            // trim anything after a comma, if necessary
                            int i = szModel.IndexOf(",", StringComparison.CurrentCulture);
                            if (i > 0)
                                szModel = szModel.Substring(0, i);

                            if (CountryCodePrefix.IsNakedSim(szTail) || CountryCodePrefix.IsNakedAnon(szTail))
                            {
                                string szModelNormal = AircraftImportMatchRow.NormalizeModel(szModel);
                                foreach (string szExistingTail in m_cm.AircraftForUser.Keys)
                                {
                                    if (szExistingTail.StartsWith(szTail, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        Aircraft acExisting = m_cm.AircraftForUser[szExistingTail];
                                        int modelIDExisting = acExisting.ModelID;
                                        if ((mappedModel != null && mappedModel.MakeModelID == modelIDExisting) ||
                                            (AircraftImportMatchRow.NormalizeModel(MakeModel.GetModel(modelIDExisting).Model).StartsWith(szModelNormal, StringComparison.CurrentCultureIgnoreCase)))
                                        {
                                            fFoundAnonOrSim = true;     // don't throw an exception
                                            szTail = szExistingTail;    // Map to this aircraft
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!fFoundAnonOrSim)
                                m_cm.AircraftToImport.AddMatchCandidate(szTail, szModel);
                        }
                        else
                            m_cm.AircraftToImport.AddMatchCandidate(szTail, szModel, false);

                        if (!fFoundAnonOrSim)
                        {
                            le.LastError = LogbookEntryCore.ErrorCode.InvalidAircraft;
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportUnknownAircraft, szTail));
                        }
                    }
                }

                if (ac == null)
                    ac = m_cm.AircraftForUser[szTail];

                le.AircraftID = ac.AircraftID;
                le.TailNumDisplay = ac.DisplayTailnumber;
                le.ModelDisplay = ac.ModelDescription;  // for display
                le.CatClassOverride = GetMappedInt(m_cm.iColCatClassOverride);
                le.CatClassDisplay = (le.CatClassOverride == 0) ? ac.CategoryClassDisplay : CategoryClass.CategoryClassFromID((CategoryClass.CatClassID)le.CatClassOverride).CatClass;
            }
            #endregion

            public LogbookEntry FlightFromRow(LogbookEntry le, string[] rgszRow, AutoFillOptions afo)
            {
                m_rgszRow = rgszRow ?? throw new ArgumentNullException(nameof(rgszRow));

                // Check integrity of the row
                if (m_cm.ColumnCount != m_rgszRow.Length)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportIncorrectColumns, m_cm.ColumnCount, m_rgszRow.Length));

                // if it is an existing flight, try loading it; we will modify it from there.  Could generate an exception if the id doesn't exist, or if the user doesn't own it.
                if (m_cm.iColFlightID >= 0)
                {
                    if (!le.FLoadFromDB(GetMappedInt(m_cm.iColFlightID), m_cm.User))
                    {
                        // NotFound is OK - just treat it as a new flight.
                        if (le.LastError != LogbookEntry.ErrorCode.NotFound)
                            throw new MyFlightbookException(le.ErrorString);
                    }
                }
                else
                    le.FLoadFromDB(LogbookEntry.idFlightNew, m_cm.User);

                if (m_cm.iColTail >= m_rgszRow.Length)
                {
                    le.LastError = LogbookEntryCore.ErrorCode.InvalidAircraft;
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTail);
                }
                if (m_cm.iColTotal >= m_rgszRow.Length)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTotal);
                if (m_cm.iColDate >= m_rgszRow.Length)
                {
                    le.LastError = LogbookEntryCore.ErrorCode.InvalidDate;
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoDate);
                }

                // see if an aircraft ID is present; if so, AND if it matches the specified aircraft, we'll use that
                // (Provides disambiguation if you have two versions of the same aircraft in the account.)
                int idAircraft = (m_cm.iColAircraftID >= 0) ? GetMappedInt(m_cm.iColAircraftID) : Aircraft.idAircraftUnknown;
                if (idAircraft == 0)
                    idAircraft = Aircraft.idAircraftUnknown;

                if (m_rgszRow[m_cm.iColDate].Length > 0)
                {
                    if (DateTime.TryParse(m_rgszRow[m_cm.iColDate], CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtFlight))
                        le.Date = dtFlight;
                    else
                    {
                        le.LastError = LogbookEntryCore.ErrorCode.InvalidDate;
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotReadDate, m_rgszRow[m_cm.iColDate]));
                    }
                }

                le.TotalFlightTime = GetMappedDecimal(m_cm.iColTotal);
                if (le.TotalFlightTime < 0.0M)
                {
                    le.LastError = LogbookEntryCore.ErrorCode.NegativeTime;
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotReadTotalTime, le.TotalFlightTime));
                }

                // Get the other fields, if present.
                InitFlightFromRowMainFields(le);

                InitFlightFromRowProperties(le, out string szTail);
                                
                // Do any autofill here
                if (afo != null && le.CrossCountry == 0.0M && le.Nighttime == 0.0M)
                {
                    using (FlightData fd = new FlightData())
                        fd.AutoFill(le, afo);
                }

                // See if the aircraft exists
                InitFlightAircraftFromRow(le, szTail, idAircraft);

                if (!le.IsValid())
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportFlightIsInvalid, le.ErrorString));

                return le;
            }

            #region GetMappedValues
            private Boolean GetMappedBoolean(int iCol)
            {
                return iCol >= 0 && m_rgszRow[iCol].SafeParseBoolean();
            }

            private string GetMappedString(int iCol)
            {
                return (iCol < 0) ? string.Empty : m_rgszRow[iCol];
            }

            private DateTime GetMappedUTCDate(int iCol, DateTime? dtNakedTime = null)
            {
                if (iCol < 0)
                    return DateTime.MinValue;

                string sz = m_rgszRow[iCol];

                return sz.ParseUTCDateTime(dtNakedTime);
            }

            private Decimal GetMappedDecimal(int iCol)
            {
                if (iCol < 0)
                    return 0.0M;

                string sz = m_rgszRow[iCol].ToUpperInvariant().Trim();

                if (sz.Length == 0)
                    return 0.0M;

                return sz.SafeParseDecimal();
            }

            private Int32 GetMappedInt(int iCol)
            {
                if (iCol < 0)
                    return 0;

                string sz = m_rgszRow[iCol].ToUpperInvariant().Trim();

                if (Int32.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out int i))
                    return i;

                return 0;
            }
            #endregion
        }

        [Serializable]
        private class ImportContext
        {
            #region Column Indices for known columns
            public int iColFlightID { get; set; }
            public int iColDate { get; set; }
            public int iColTail { get; set; }
            public int iColAircraftID { get; set; }
            public int iColTotal { get; set; }
            public int iColApproaches { get; set; }
            public int iColHold { get; set; }
            public int iColLandings { get; set; }
            public int iColNightLandings { get; set; }
            public int iColFullStopLandings { get; set; }
            public int iColCrossCountry { get; set; }
            public int iColNight { get; set; }
            public int iColIMC { get; set; }
            public int iColSimIFR { get; set; }
            public int iColGroundSim { get; set; }
            public int iColDual { get; set; }
            public int iColCFI { get; set; }
            public int iColSIC { get; set; }
            public int iColPIC { get; set; }
            public int iColRoute { get; set; }
            public int iColComment { get; set; }
            public int iColCatClassOverride { get; set; }
            public int iColEngineStart { get; set; }
            public int iColEngineEnd { get; set; }
            public int iColFlightStart { get; set; }
            public int iColFlightEnd { get; set; }
            public int iColHobbsStart { get; set; }
            public int iColHobbsEnd { get; set; }
            public int iColModel { get; set; }
            public int iColFrom { get; set; }
            public int iColTo { get; set; }
            public int iColFlightConditions { get; set; }
            public int iColPilotRole { get; set; }

            public int iColMission { get; set; }
            public int iColPublic { get; set; }
            #endregion

            #region public properties
            private readonly IDictionary<string, Aircraft> _dictAircraft;
            public IDictionary<string, Aircraft> AircraftForUser
            {
                get { return _dictAircraft; }
            }
            public IReadOnlyDictionary<string, MakeModel> ModelMapping { get; set; }

            public string User { get; set; }

            /// <summary>
            /// A dictionary of lists of custom flight properties, indexed by flightID, that represent properties to delete
            /// </summary>
            public Dictionary<int, List<CustomFlightProperty>> OrphanedPropsByFlightID { get; set; }

            public List<ImportColumn> CustomPropertiesToImport { get; set; }
            #endregion

            private readonly Hashtable m_htHeader = new Hashtable();

            /// <summary>
            /// Find the index of a column, given a prioritized array of column headers.
            /// </summary>
            /// <param name="rgsz">Array of column headers</param>
            /// <returns>The index of the first column header found in the file</returns>
            private int ColumnIndex(string[] rgsz)
            {
                for (int i = 0; i < rgsz.Length; i++)
                {
                    object o = m_htHeader[rgsz[i].ToUpperInvariant().Trim()];
                    if (o != null)
                        return (int)o;
                }
                return -1;
            }

            public AircraftImportParseContext AircraftToImport { get; set; }

            public int ColumnCount
            {
                get { return m_htHeader.Count; }
            }

            private void InitializeColumns()
            {
                iColDate = ColumnIndex(colDate);
                iColTail = ColumnIndex(colTail);
                iColTotal = ColumnIndex(colTotal);

                // verify that required fields are present
                if (iColDate < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoDate);
                if (iColTail < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTail);
                if (iColTotal < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTotal);

                iColAircraftID = ColumnIndex(colAircraftID);
                iColFlightID = ColumnIndex(colFlightID);
                iColApproaches = ColumnIndex(colApproaches);
                iColHold = ColumnIndex(colHold);
                iColLandings = ColumnIndex(colLandings);
                iColNightLandings = ColumnIndex(colNightLandings);
                iColFullStopLandings = ColumnIndex(colFullStopLandings);
                iColCrossCountry = ColumnIndex(colCrossCountry);
                iColNight = ColumnIndex(colNight);
                iColIMC = ColumnIndex(colIMC);
                iColSimIFR = ColumnIndex(colSimIFR);
                iColGroundSim = ColumnIndex(colGroundSim);
                iColDual = ColumnIndex(colDual);
                iColCFI = ColumnIndex(colCFI);
                iColSIC = ColumnIndex(colSIC);
                iColPIC = ColumnIndex(colPIC);
                iColRoute = ColumnIndex(colRoute);
                iColComment = ColumnIndex(colComment);
                iColCatClassOverride = ColumnIndex(colCatClassOverride);
                iColEngineStart = ColumnIndex(colEngineStart);
                iColEngineEnd = ColumnIndex(colEngineEnd);
                iColFlightStart = ColumnIndex(colFlightStart);
                iColFlightEnd = ColumnIndex(colFlightEnd);
                iColHobbsStart = ColumnIndex(colHobbsStart);
                iColHobbsEnd = ColumnIndex(colHobbsEnd);
                iColModel = ColumnIndex(colModelName);
                iColPublic = ColumnIndex(colPublic);
                iColFrom = ColumnIndex(colFrom);
                iColTo = ColumnIndex(colTo);
                iColFlightConditions = ColumnIndex(colFlightConditions);
                iColPilotRole = ColumnIndex(colPilotRole);
                iColMission = ColumnIndex(colPilotMission);

                // Now, see which custom properties are present
                CustomPropertyType[] rgCpt = CustomPropertyType.GetCustomPropertyTypes();
                foreach (CustomPropertyType cpt in rgCpt)
                {
                    int iCol = ColumnIndex(ResolvePropNameAlias(cpt.Title));
                    if (iCol >= 0)
                        CustomPropertiesToImport.Add(new ImportColumn(cpt, iCol));
                }
            }

            public ImportContext(string[] rgszHeader, string szUser)
            {
                User = szUser;
                _dictAircraft = new UserAircraft(szUser).DictAircraftForUser();
                AircraftToImport = new AircraftImportParseContext();

                OrphanedPropsByFlightID = new Dictionary<int, List<CustomFlightProperty>>();
                CustomPropertiesToImport = new List<ImportColumn>();

                for (int i = 0; i < rgszHeader.Length; i++)
                {
                    if (String.IsNullOrWhiteSpace(rgszHeader[i]))
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportEmptyColumn, i + 1));
                    string szNormal = rgszHeader[i].ToUpperInvariant().Trim();
                    if (m_htHeader[szNormal] != null)
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportDuplicateColumn, szNormal));
                    m_htHeader[szNormal] = i;    // set up to find each column by name
                }

                InitializeColumns();
            }
        }
        #endregion

        #region Constructors
        public CSVImporter()
        {
            FlightsToImport = new List<LogbookEntry>();
            ModelNameMappings = new Dictionary<string, MakeModel>();
        }

        public CSVImporter(IReadOnlyDictionary<string, MakeModel> mappings) : this()
        {
            if (mappings != null)
                ModelNameMappings = mappings;
        }
        #endregion

        /// <summary>
        /// Reads a CSV file and returns a list of LogbookEntry objects from it. DOES NOT WRITE THE FLIGHTS!!!
        /// </summary>
        /// <param name="fileContent">The CSV file stream</param>
        /// <param name="szUser">The username for whom the import is being performed</param>
        /// <param name="rowHasError">Delegate called for a row that has an error.  Has the entry (error string indicates the error), the raw row data, and the index of the row</param>
        /// <param name="rowOK">Delegate called for a row that does not have an error.  Has the entry and the row index</param>
        /// <param name="afo">If not null, contains the options for performing autofill on flights.</param>
        /// <returns>false for an error (look at "ErrorString" for information).</returns>
        public bool FInitFromStream(Stream fileContent, string szUser, Action<LogbookEntryCore, int> rowOK, Action<LogbookEntryCore, string, int> rowHasError, AutoFillOptions afo)
        {
            using (CSVReader csvr = new CSVReader(fileContent))
            {
                FlightsToImport.Clear();
                OriginalFlightsToModify = new Dictionary<int, LogbookEntry>();
                int iRow = 0;

                HasErrors = false;
                ErrorMessage = string.Empty;

                try
                {
                    try
                    {
                        m_ImportContext = new ImportContext(csvr.GetCSVLine(true), szUser) { ModelMapping = ModelNameMappings };
                    }
                    catch (CSVReaderInvalidCSVException ex)
                    {
                        throw new MyFlightbookException(ex.Message);
                    }

                    RowReader rr = new RowReader(m_ImportContext);

                    string[] rgszRow = null;
                    while ((rgszRow = csvr.GetCSVLine()) != null)
                    {
                        iRow++;

                        // Check for empty row; skip it if necessary
                        bool fHasData = false;
                        Array.ForEach<string>(rgszRow, (sz) => { if (sz.Trim().Length > 0) fHasData = true; });
                        if (!fHasData)
                            continue;

                        LogbookEntry le = new LogbookEntry();

                        try
                        {
                            le = rr.FlightFromRow(le, rgszRow, afo);
                            rowOK?.Invoke(le, iRow);
                        }
                        catch (MyFlightbookException ex)
                        {
                            HasErrors = true;
                            le.ErrorString = ex.Message;
                            rowHasError?.Invoke(le, String.Join(",", rgszRow), iRow);
                        }

                        FlightsToImport.Add(le);
                    }
                    m_ImportContext.AircraftToImport.ProcessParseResultsForUser(szUser);
                    m_missingAircraft.AddRange(m_ImportContext.AircraftToImport.AllMissing);

                    // Collect base versions of any flights being modified.
                    HashSet<int> hsModifiedFlightIDs = new HashSet<int>();
                    foreach (LogbookEntry le in FlightsToImport)
                        if (!le.IsNewFlight)
                            hsModifiedFlightIDs.Add(le.FlightID);

                    if (hsModifiedFlightIDs.Count > 0)
                    {
                        IEnumerable<LogbookEntryDisplay> lstFlightsToModify = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(new FlightQuery(szUser)), szUser, "FlightID", System.Web.UI.WebControls.SortDirection.Ascending, false, false);
                        foreach (LogbookEntry le in lstFlightsToModify)
                            if (hsModifiedFlightIDs.Contains(le.FlightID))
                                OriginalFlightsToModify.Add(le.FlightID, le);
                    }

                }
                catch (MyFlightbookException ex)
                {
                    HasErrors = true;
                    ErrorMessage = ex.Message;
                    FlightsToImport.Clear();
                    return false;
                }
            }

            return !HasErrors;
        }

        /// <summary>
        /// Commits the initialized flights
        /// </summary>
        /// <param name="OnRowAdded">Delegate called for each flight that is commited</param>
        /// <param name="OnRowFailure">Delegate called for each flight that fails</param>
        /// <param name="fAllowErrors">Indicates whether or not errors are allowed</param>
        /// <returns>True for success</returns>
        public bool FCommit(Action<LogbookEntry, bool> OnRowAdded, Action<LogbookEntry, Exception> OnRowFailure, bool fAllowErrors)
        {
            if (FlightsToImport == null || (!fAllowErrors && HasErrors))
                return false;

            int cFlightsImported = 0;

            // Write the data to the db.
            foreach (LogbookEntry le in FlightsToImport)
            {
                le.ErrorString = string.Empty;
                List<CustomFlightProperty> lstCFPToDelete = le.IsNewFlight ? null : m_ImportContext.OrphanedPropsByFlightID[le.FlightID];
                try
                {
                    bool fInsert = le.IsNewFlight;
                    if (le.FCommit() && lstCFPToDelete != null)
                    {
                        foreach (CustomFlightProperty cfp in lstCFPToDelete)
                            cfp.DeleteProperty();
                    }

                    OnRowAdded?.Invoke(le, fInsert);

                    cFlightsImported++;
                }
                catch (MyFlightbookException ex)
                {
                    le.ErrorString = ex.Message;
                    OnRowFailure?.Invoke(le, ex);
                }
            }

            EventRecorder.UpdateCount(EventRecorder.MFBCountID.ImportedFlight, cFlightsImported);
            return true;
        }

        public bool InitWithBytes(byte[] rgb, string szUser, Action<LogbookEntryCore, int> rowOK, Action<LogbookEntryCore, string, int> rowHasError, bool fAutofill)
        {
            AutoFillOptions afo = fAutofill ? AutoFillOptions.DefaultOptionsForUser(szUser) : null;
            if (afo != null)
                afo.IncludeHeliports = true;

            using (MemoryStream ms2 = new MemoryStream(rgb))
            {
                return FInitFromStream(ms2, szUser, rowOK, rowHasError, afo);
            }
        }
    }
}