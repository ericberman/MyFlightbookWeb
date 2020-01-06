using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{

    public class MccPilot : ExternalFormat
    {
        #region properties
        public DateTime mcc_DATE { get; set; }
        public string FlightNumber { get; set; }
        public string AF_DEP { get; set; }
        public DateTime TIME_DEP { get; set; }
        public string AF_ARR { get; set; }
        public DateTime TIME_ARR { get; set; }
        public string AC_MODEL { get; set; }
        public string AC_REG { get; set; }
        public string PILOT1_ID { get; set; }
        public string PILOT1_NAME { get; set; }
        public string PILOT1_PHONE { get; set; }
        public string PILOT1_EMAIL { get; set; }
        public string PILOT2_ID { get; set; }
        public string PILOT2_NAME { get; set; }
        public string PILOT2_PHONE { get; set; }
        public string PILOT2_EMAIL { get; set; }
        public string PILOT3_ID { get; set; }
        public string PILOT3_NAME { get; set; }
        public string PILOT3_PHONE { get; set; }
        public string PILOT3_EMAIL { get; set; }
        public string PILOT4_ID { get; set; }
        public string PILOT4_NAME { get; set; }
        public string PILOT4_PHONE { get; set; }
        public string PILOT4_EMAIL { get; set; }
        public int TIME_TOTAL { get; set; }
        public int TIME_PIC { get; set; }
        public int TIME_PICUS { get; set; }
        public int TIME_SIC { get; set; }
        public int TIME_DUAL { get; set; }
        public int TIME_INSTRUCTOR { get; set; }
        public int TIME_EXAMINER { get; set; }
        public int TIME_NIGHT { get; set; }
        public int TIME_RELIEF { get; set; }
        public int TIME_IFR { get; set; }
        public int TIME_ACTUAL { get; set; }
        public int TIME_HOOD { get; set; }
        public int TIME_XC { get; set; }
        public bool PF { get; set; }
        public int TO_DAY { get; set; }
        public int TO_NIGHT { get; set; }
        public int LDG_DAY { get; set; }
        public int LDG_NIGHT { get; set; }
        public int AUTOLAND { get; set; }
        public int HOLDING { get; set; }
        public string LIFT { get; set; }
        public string INSTRUCTION { get; set; }
        public string REMARKS { get; set; }
        public string APP_1 { get; set; }
        public string APP_2 { get; set; }
        public string APP_3 { get; set; }
        public int PAX { get; set; }
        public string DEICE { get; set; }
        public string FUEL { get; set; }
        public string FUELUSED { get; set; }
        public string DELAY { get; set; }
        public string FLIGHTLOG { get; set; }
        public DateTime TIME_TO { get; set; }
        public DateTime TIME_LDG { get; set; }
        public string TIME_AIR { get; set; }
        public string RentCost { get; set; }
        public string PilotPayCost { get; set; }
        public string PerDiem { get; set; }
        public string IS_PREVEXP { get; set; }
        public bool AC_ISSIM { get; set; }
        #endregion

        public MccPilot()
        {
            FlightNumber = AF_DEP = AF_ARR = AC_MODEL = AC_REG = PILOT1_ID = PILOT1_NAME = PILOT1_PHONE = PILOT1_EMAIL = PILOT2_ID = PILOT2_NAME = PILOT2_PHONE = PILOT2_EMAIL = PILOT3_ID = PILOT3_NAME = PILOT3_PHONE = PILOT3_EMAIL = PILOT4_ID = PILOT4_NAME = PILOT4_PHONE = PILOT4_EMAIL = LIFT = INSTRUCTION = REMARKS = APP_1 = APP_2 = APP_3 = DEICE = FUEL = FUELUSED = DELAY = FLIGHTLOG = TIME_AIR = RentCost = PilotPayCost = PerDiem = IS_PREVEXP = string.Empty;
        }

        public MccPilot(DataRow dr) : base(dr) { }

        private string FormattedPilotInfo(string name, string phone, string email, string ID)
        {
            if (String.IsNullOrEmpty(name))
                return string.Empty;

            string phone_email = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, phone, email).Trim();
            if (!String.IsNullOrWhiteSpace(ID))
                phone_email = String.Format(CultureInfo.CurrentCulture, "{0} ID: {1}", phone_email, ID).Trim();

            return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, name, String.IsNullOrWhiteSpace(phone_email) ? string.Empty : String.Format(CultureInfo.CurrentCulture, " ({0})", phone_email)).Trim();
        }

        private decimal FromMinutes(int minutes)
        {
            return Math.Round(((decimal) minutes) / 60.0M, 2);
        }

        private DateTime DateOrEmpty(DateTime dt)
        {
            return (dt.Hour == 0 && dt.Minute == 0) ? DateTime.MinValue : dt;
        }

        public override LogbookEntry ToLogbookEntry()
        {
            TIME_DEP = FixedUTCDateFromTime(mcc_DATE, TIME_DEP);
            TIME_ARR = FixedUTCDateFromTime(mcc_DATE, TIME_ARR, TIME_DEP);

            TIME_TO = FixedUTCDateFromTime(mcc_DATE, TIME_TO);
            TIME_LDG = FixedUTCDateFromTime(mcc_DATE, TIME_LDG, TIME_TO);

            string[] rgApproaches = new string[] { APP_1, APP_2, APP_3 };
            int cApproaches = 0;
            foreach (string sz in rgApproaches)
                if (!String.IsNullOrWhiteSpace(sz))
                    cApproaches++;
            
            LogbookEntry le = new LogbookEntry()
            {
                Date = mcc_DATE,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, AF_DEP, AF_ARR).Trim(),
                ModelDisplay = AC_MODEL,
                TailNumDisplay = AC_REG,
                TotalFlightTime = FromMinutes(TIME_TOTAL),
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
                fHoldingProcedures = (HOLDING != 0),
                Approaches = cApproaches,
                Comment = REMARKS,
                FlightStart = DateOrEmpty(TIME_TO),
                FlightEnd = DateOrEmpty(TIME_LDG)
            };

            le.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew1, FormattedPilotInfo(PILOT1_NAME, PILOT1_PHONE, PILOT1_EMAIL, PILOT1_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew2, FormattedPilotInfo(PILOT2_NAME, PILOT2_PHONE, PILOT2_EMAIL, PILOT2_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew3, FormattedPilotInfo(PILOT3_NAME, PILOT3_PHONE, PILOT3_EMAIL, PILOT3_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAdditionalCrew, FormattedPilotInfo(PILOT4_NAME, PILOT4_PHONE, PILOT4_EMAIL, PILOT4_ID)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, TIME_DEP, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, TIME_ARR, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, FlightNumber),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPICUS, FromMinutes(TIME_PICUS)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropReliefPilotTime, FromMinutes(TIME_RELIEF)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropIFRTime, FromMinutes(TIME_IFR)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPilotFlyingTime, FromMinutes(PF ? TIME_TOTAL : 0)),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, TO_NIGHT),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPassengerCount, PAX),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropApproachName, String.Join(" ", rgApproaches).Trim())
            });

            return le;
        }
    }

    public class MccPilotImporter: ExternalFormatImporter
    {
        public override string Name { get { return "MccPilot"; } }

        public override bool CanParse(byte[] rgb)
        {
            if (rgb == null || rgb.Length == 0)
                return false;

            string sz = Encoding.UTF8.GetString(rgb);
            sz = sz.Substring(0, Math.Min(sz.Length, 1000));
            return sz.ToUpperInvariant().Contains("MCC_DATE");
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach (DataRow dr in dt.Rows)
                {
                    MccPilot mcc = new MccPilot(dr);
                    CSVImporter.WriteEntryToDataTable(mcc.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            throw new NotImplementedException();
        }
    }
}