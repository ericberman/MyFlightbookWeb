using MyFlightbook.CSV;
using System;
using System.Data;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2019-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{

    public class CrewLounge : ExternalFormat
    {
        #region properties
        public DateTime PILOTLOG_DATE { get; set; }
        public bool IS_PREVEXP { get; set; }
        public bool AC_ISSIM { get; set; }
        public string FLIGHTNUMBER { get; set; } = string.Empty;
        public string PAIRING { get; set; } = string.Empty;
        public string AF_DEP { get; set; } = string.Empty;
        public string DEP_RWY { get; set; } = string.Empty;
        public string AF_ARR { get; set; } = string.Empty;
        public string ARR_RWY { get; set; } = string.Empty;
        public DateTime TIME_DEP { get; set; } = DateTime.MinValue;
        public DateTime TIME_DEPSCH { get; set; } = DateTime.MinValue;
        public DateTime TIME_ARR { get; set; } = DateTime.MinValue;
        public DateTime TIME_ARRSCH { get; set; } = DateTime.MinValue;
        public DateTime TIME_TO { get; set; } = DateTime.MinValue;
        public DateTime TIME_LDG { get; set; } = DateTime.MinValue;
        public string TIME_AIR { get; set; } = string.Empty;
        public string TIME_MODE { get; set; } = string.Empty;
        public int TIME_TOTAL { get; set; }
        public int TIME_TOTALSIM { get; set; }
        public int TIME_PIC { get; set; }
        public int TIME_SIC { get; set; }
        public int TIME_DUAL { get; set; }
        public int TIME_PICUS { get; set; }
        public int TIME_INSTRUCTOR { get; set; }
        public int TIME_EXAMINER { get; set; }
        public int TIME_NIGHT { get; set; }
        public int TIME_XC { get; set; }
        public int TIME_IFR { get; set; }
        public int TIME_HOOD { get; set; }
        public int TIME_ACTUAL { get; set; }
        public int TIME_RELIEF { get; set; }
        public int TIME_USER1 { get; set; }
        public int TIME_USER2 { get; set; }
        public int TIME_USER3 { get; set; }
        public int TIME_USER4 { get; set; }
        public string CAPACITY { get; set; } = string.Empty;
        public string OPERATOR { get; set; } = string.Empty;
        public string PILOT1_ID { get; set; } = string.Empty;
        public string PILOT1_NAME { get; set; } = string.Empty;
        public string PILOT1_PHONE { get; set; } = string.Empty;
        public string PILOT1_EMAIL { get; set; } = string.Empty;
        public string PILOT2_ID { get; set; } = string.Empty;
        public string PILOT2_NAME { get; set; } = string.Empty;
        public string PILOT2_PHONE { get; set; } = string.Empty;
        public string PILOT2_EMAIL { get; set; } = string.Empty;
        public string PILOT3_ID { get; set; } = string.Empty;
        public string PILOT3_NAME { get; set; } = string.Empty;
        public string PILOT3_PHONE { get; set; } = string.Empty;
        public string PILOT3_EMAIL { get; set; } = string.Empty;
        public string PILOT4_ID { get; set; } = string.Empty;
        public string PILOT4_NAME { get; set; } = string.Empty;
        public string PILOT4_PHONE { get; set; } = string.Empty;
        public string PILOT4_EMAIL { get; set; } = string.Empty;
        public int TO_DAY { get; set; }
        public int TO_NIGHT { get; set; }
        public int LDG_DAY { get; set; }
        public int LDG_NIGHT { get; set; }
        public string LIFT { get; set; } = string.Empty;
        public bool PF { get; set; }
        public int HOLDING { get; set; }
        public string TAG_APP { get; set; } = string.Empty;
        public string TAG_OPS { get; set; } = string.Empty;
        public string TAG_LAUNCH { get; set; } = string.Empty;
        public string INSTRUCTION { get; set; } = string.Empty;
        public string TRAINING { get; set; } = string.Empty;
        public string REMARKS { get; set; } = string.Empty;
        public string CREWLIST { get; set; } = string.Empty;
        public string FLIGHTLOG { get; set; } = string.Empty;
        public int PAX { get; set; }
        public decimal FUEL { get; set; }
        public decimal FUELPLANNED { get; set; }
        public decimal FUELUSED { get; set; }
        public string TAG_DELAY { get; set; } = string.Empty;
        public string DEICE { get; set; } = string.Empty;
        public string USER_NUMERIC { get; set; } = string.Empty;
        public string USER_TEXT { get; set; } = string.Empty;
        public string USER_YESNO { get; set; } = string.Empty;
        public string AC_MAKE { get; set; } = string.Empty;
        public string AC_MODEL { get; set; } = string.Empty;
        public string AC_VARIANT { get; set; } = string.Empty;
        public string AC_REG { get; set; } = string.Empty;
        public string AC_FIN { get; set; } = string.Empty;
        public string AC_RATING { get; set; } = string.Empty;
        public string AC_SP { get; set; } = string.Empty;
        public string AC_MP { get; set; } = string.Empty;
        public string AC_ME { get; set; } = string.Empty;
        public string AC_SPSE { get; set; } = string.Empty;
        public string AC_SPME { get; set; } = string.Empty;
        public string AC_CLASS { get; set; } = string.Empty;
        public string AC_GLIDER { get; set; } = string.Empty;
        public string AC_ULTRALIGHT { get; set; } = string.Empty;
        public string AC_SEA { get; set; } = string.Empty;
        public string AC_ENGINES { get; set; } = string.Empty;
        public string AC_ENGTYPE { get; set; } = string.Empty;
        public string AC_TAILWHEEL { get; set; } = string.Empty;
        public string AC_COMPLEX { get; set; } = string.Empty;
        public string AC_TMG { get; set; } = string.Empty;
        public string AC_HEAVY { get; set; } = string.Empty;
        public string AC_HIGHPERF { get; set; } = string.Empty;
        public string AC_AEROBATIC { get; set; } = string.Empty;
        public int AC_SEATS { get; set; }
        #endregion

        public CrewLounge() { }

        public CrewLounge(DataRow dr) : base(dr) { }


        public override LogbookEntry ToLogbookEntry()
        {
            TIME_DEP = FixedUTCDateFromTime(PILOTLOG_DATE, TIME_DEP);
            TIME_ARR = FixedUTCDateFromTime(PILOTLOG_DATE, TIME_ARR, TIME_DEP);

            TIME_TO = FixedUTCDateFromTime(PILOTLOG_DATE, TIME_TO);
            TIME_LDG = FixedUTCDateFromTime(PILOTLOG_DATE, TIME_LDG, TIME_TO);

            LogbookEntry le = new LogbookEntry()
            {
                Date = PILOTLOG_DATE,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, AF_DEP, AF_ARR).Trim(),
                ModelDisplay = AC_MODEL,
                TailNumDisplay = AC_REG,
                TotalFlightTime = FromMinutes(TIME_TOTAL),
                GroundSim = FromMinutes(TIME_TOTALSIM),
                PIC = FromMinutes(TIME_PIC),
                SIC = FromMinutes(TIME_SIC),
                Dual = FromMinutes(TIME_DUAL),
                CFI = FromMinutes(TIME_INSTRUCTOR),
                Nighttime = FromMinutes(TIME_NIGHT),
                IMC = FromMinutes(TIME_ACTUAL),
                SimulatedIFR = FromMinutes(TIME_HOOD),
                CrossCountry = FromMinutes(TIME_XC),
                FullStopLandings = LDG_DAY,
                NightLandings = LDG_NIGHT,
                fHoldingProcedures = HOLDING > 0,
                Approaches = String.IsNullOrEmpty(TAG_APP) ? 0 : 1,
                Comment = REMARKS,
                FlightStart = DateOrEmpty(TIME_TO),
                FlightEnd = DateOrEmpty(TIME_LDG)
            };

            le.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew1, FormattedPilotInfo(PILOT1_NAME, PILOT1_PHONE, PILOT1_EMAIL, PILOT1_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew2, FormattedPilotInfo(PILOT2_NAME, PILOT2_PHONE, PILOT2_EMAIL, PILOT2_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew3, FormattedPilotInfo(PILOT3_NAME, PILOT3_PHONE, PILOT3_EMAIL, PILOT3_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew4, FormattedPilotInfo(PILOT4_NAME, PILOT4_PHONE, PILOT4_EMAIL, PILOT4_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAdditionalCrew, CREWLIST),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, TIME_DEP, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, TIME_ARR, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FLIGHTNUMBER),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPICUS, FromMinutes(TIME_PICUS)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropReliefPilotTime, FromMinutes(TIME_RELIEF)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropIFRTime, FromMinutes(TIME_IFR)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPilotFlyingTime, FromMinutes(PF ? TIME_TOTAL : 0)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, TO_NIGHT),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPassengerCount, PAX),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropApproachName, TAG_APP),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropOperatorName, OPERATOR),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledDeparture, DateOrEmpty(TIME_DEPSCH), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledArrival, DateOrEmpty(TIME_ARRSCH), true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFuelConsumed, FUELUSED),
            });

            return le;
        }
    }

    public class CrewLoungeImporter : ExternalFormatImporter
    {
        public override string Name { get { return "CrewLounge"; } }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);
            sz = sz.Substring(0, Math.Min(sz.Length, 1000));
            return sz.ToUpperInvariant().Contains("PILOTLOG_DATE");
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    CrewLounge mcc = new CrewLounge(dr);
                    CSVImporter.WriteEntryToDataTable(mcc.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}