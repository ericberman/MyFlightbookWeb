using MySql.Data.MySqlClient;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Represents a row from a currency query.  Primarily serves to encapsulate data about a flight that is used in currency computations
    /// </summary>
    public class ExaminerFlightRow
    {
        #region Properties
        public DateTime dtFlight { get; set; }
        public int flightID { get; set; }
        public int cLandingsThisFlight { get; set; }
        public int cFullStopLandings { get; set; }
        public int cFullStopNightLandings { get; set; }
        public int cApproaches { get; set; }
        public int cHolds { get; set; }
        public int idModel { get; set; }
        public int idAircraft { get; set; }
        public string szType { get; set; }
        public string szCategory { get; set; }
        public CategoryClass.CatClassID idCatClassOverride { get; set; }
        public string szCatClassType { get; set; }
        public string szCatClassBase { get; set; }
        public string szArmyMDS { get; set; }
        public string szFamily { get; set; }
        public string szModel { get; set; }
        public string Comments { get; set; }
        public string Route { get; set; }
        public Boolean fTailwheel { get; set; }
        public Boolean fNight { get; set; }
        public Boolean fIsCertifiedIFR { get; set; }
        public Boolean fIsCertifiedLanding { get; set; }
        public Boolean fIsFullMotion { get; set; }
        public Boolean fIsATD { get; set; }
        public Boolean fIsFTD { get; set; }
        public Boolean fIsComplex { get; set; }
        public Boolean fIsTAA { get; set; }
        public MakeModel.TurbineLevel turbineLevel { get; set; }
        public Boolean fIsCertifiedSinglePilot { get; set; }
        public Decimal IMC { get; set; }
        public Decimal IMCSim { get; set; }
        public Decimal PIC { get; set; }
        public Decimal SIC { get; set; }
        public Decimal CFI { get; set; }
        public Decimal Total { get; set; }
        public Decimal Dual { get; set; }
        public Decimal GroundSim { get; set; }
        public Decimal Night { get; set; }
        public Decimal XC { get; set; }
        public Boolean fIsRealAircraft { get; set; }
        public Boolean fIsGlider { get; set; }
        public Boolean fMotorGlider { get; set; }
        public Boolean fIsSingleEngine { get; set; }
        public Boolean fIsR44 { get; set; }
        public Boolean fIsR22 { get; set; }
        public DateTime dtEngineStart { get; set; }
        public DateTime dtEngineEnd { get; set; }
        public DateTime dtFlightStart { get; set; }
        public DateTime dtFlightEnd { get; set; }

        private readonly static DateTime s_Aug2018Cutover = new DateTime(2018, 8, 27);
        private readonly static DateTime s_Nov2018Cutover = new DateTime(2018, 11, 26);

        public static DateTime Aug2018Cutover { get { return s_Aug2018Cutover; } }
        public static DateTime Nov2018Cutover { get { return s_Nov2018Cutover; } }

        private readonly CustomPropertyCollection m_flightprops;    // backing list for FlightEvents

        /// <summary>
        /// Set of flightproperties (could include profile events, though these are rare now) for the flight.  Will not be null.
        /// </summary>
        public CustomPropertyCollection FlightProps { get { return m_flightprops; } }
        #endregion

        public ExaminerFlightRow(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            dtFlight = (DateTime)dr["date"];
            flightID = Convert.ToInt32(dr["FlightID"], CultureInfo.InvariantCulture);
            cLandingsThisFlight = Convert.ToInt32(dr["cLandings"], CultureInfo.InvariantCulture);
            cFullStopLandings = Convert.ToInt32(dr["cFullStopLandings"], CultureInfo.InvariantCulture);
            cFullStopNightLandings = Convert.ToInt32(dr["cNightLandings"], CultureInfo.InvariantCulture);
            cApproaches = Convert.ToInt32(dr["cInstrumentApproaches"], CultureInfo.InvariantCulture);
            cHolds = Convert.ToInt32(dr["fHold"], CultureInfo.InvariantCulture);
            idModel = Convert.ToInt32(dr["idModel"], CultureInfo.InvariantCulture);
            idAircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
            szType = dr["TypeName"].ToString();
            szCategory = dr["Category"].ToString();
            szCatClassType = dr["CatClassWithType"].ToString();
            szCatClassBase = dr["BaseCatClass"].ToString();
            idCatClassOverride = (CategoryClass.CatClassID)Convert.ToInt32(dr["CatClassOverride"], CultureInfo.InvariantCulture);
            fIsGlider = idCatClassOverride == CategoryClass.CatClassID.Glider;
            szArmyMDS = dr["ArmyMDS"].ToString();
            szModel = dr["model"].ToString();
            szFamily = dr["Family"].ToString();
            if (String.IsNullOrEmpty(szFamily))
                szFamily = szModel;
            Route = dr["Route"].ToString();
            Comments = dr["Comments"].ToString();
            fTailwheel = Convert.ToBoolean(dr["fTailwheel"], CultureInfo.InvariantCulture);
            Night = (Convert.ToDecimal(dr["night"], CultureInfo.InvariantCulture));
            fNight = (Night > 0.0M);
            fIsCertifiedIFR = Convert.ToBoolean(dr["IsCertifiedIFR"], CultureInfo.InvariantCulture);
            fIsCertifiedLanding = Convert.ToBoolean(dr["IsCertifiedLanding"], CultureInfo.InvariantCulture);
            fIsFullMotion = Convert.ToBoolean(dr["IsFullMotion"], CultureInfo.InvariantCulture);
            fIsATD = Convert.ToBoolean(dr["IsATD"], CultureInfo.InvariantCulture);
            fIsFTD = Convert.ToBoolean(dr["IsFTD"], CultureInfo.InvariantCulture);
            fIsComplex = Convert.ToBoolean(dr["fComplex"], CultureInfo.InvariantCulture);
            fIsTAA = Convert.ToBoolean(dr["IsTAA"], CultureInfo.InvariantCulture);
            turbineLevel = (MakeModel.TurbineLevel)Convert.ToInt32(dr["fTurbine"], CultureInfo.InvariantCulture);
            fMotorGlider = Convert.ToBoolean(dr["fMotorGlider"], CultureInfo.InvariantCulture);
            fIsCertifiedSinglePilot = Convert.ToBoolean(dr["fCertifiedSinglePilot"], CultureInfo.InvariantCulture);
            IMC = Convert.ToDecimal(util.ReadNullableField(dr, "IMC", 0.0M), CultureInfo.InvariantCulture);
            IMCSim = Convert.ToDecimal(util.ReadNullableField(dr, "simulatedInstrument", 0.0M), CultureInfo.InvariantCulture);
            PIC = Convert.ToDecimal(dr["PIC"], CultureInfo.InvariantCulture);
            SIC = (Decimal)util.ReadNullableField(dr, "SIC", 0.0M);
            CFI = (Decimal)util.ReadNullableField(dr, "CFI", 0.0M);
            Total = Convert.ToDecimal(dr["totalFlightTime"], CultureInfo.InvariantCulture);
            Dual = Convert.ToDecimal(dr["dualReceived"], CultureInfo.InvariantCulture);
            XC = Convert.ToDecimal(dr["crosscountry"], CultureInfo.InvariantCulture);
            GroundSim = Convert.ToDecimal(util.ReadNullableField(dr, "groundSim", 0.0M), CultureInfo.InvariantCulture);
            fIsRealAircraft = ((AircraftInstanceTypes)Convert.ToInt32(dr["InstanceTypeID"], CultureInfo.InvariantCulture)) == AircraftInstanceTypes.RealAircraft;
            fIsSingleEngine = (String.Compare(szCatClassBase, "ASEL", StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(szCatClassBase, "ASES", StringComparison.OrdinalIgnoreCase) == 0) && fIsCertifiedIFR && fIsCertifiedLanding;
            fIsR22 = Convert.ToBoolean(dr["IsR22"], CultureInfo.InvariantCulture);
            fIsR44 = Convert.ToBoolean(dr["IsR44"], CultureInfo.InvariantCulture);
            dtEngineStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtEngineEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtFlightStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
            dtFlightEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);

            m_flightprops = new CustomPropertyCollection(CustomFlightProperty.PropertiesFromJSONTuples((string)util.ReadNullableField(dr, "CustomPropsJSON", string.Empty), flightID));
        }
    }

    /// <summary>
    /// Interface for any class that wishes to examine all of a user's flights sequentially
    /// </summary>
    public interface IFlightExaminer
    {
        /// <summary>
        /// Examine a flight for whatever information it has.  Called on each flight sequentially
        /// </summary>
        /// <param name="cfr">The flight to examine</param>
        void ExamineFlight(ExaminerFlightRow cfr);
    }


}
