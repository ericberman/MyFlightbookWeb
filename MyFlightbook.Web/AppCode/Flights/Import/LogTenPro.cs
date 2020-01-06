using JouniHeikniemi.Tools.Text;
using MyFlightbook.CSV;
using MyFlightbook.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    /// <summary>
    /// Implements external format for LogTen Pro
    /// </summary>
    public class LogTenPro : ExternalFormat
    {
        #region Properties
        #region CloudAhoy
        [JsonProperty("flight_key")]
        public string flight_key { get; set; }

        public string flight_selectedAircraftID
        {
            get { return aircraft_aircraftID; }
            set { aircraft_aircraftID = value; }
        }

        public string flight_selectedAircraftType
        {
            get { return aircraftType_model; }
            set { aircraftType_model = value; }
        }
        #endregion

        [JsonProperty("aircraft_aerobatic")]
        public bool aircraft_aerobatic { get; set; }

        [JsonProperty("aircraft_aircraftID")]
        public string aircraft_aircraftID { get; set; }

        [JsonProperty("aircraft_autoEngine")]
        public bool aircraft_autoEngine { get; set; }

        [JsonProperty("aircraft_complex")]
        public bool aircraft_complex { get; set; }

        [JsonProperty("aircraft_customAttribute1")]
        public int aircraft_customAttribute1 { get; set; }

        [JsonProperty("aircraft_customAttribute2")]
        public int aircraft_customAttribute2 { get; set; }

        [JsonProperty("aircraft_customAttribute3")]
        public int aircraft_customAttribute3 { get; set; }

        [JsonProperty("aircraft_customAttribute4")]
        public int aircraft_customAttribute4 { get; set; }

        [JsonProperty("aircraft_customAttribute5")]
        public int aircraft_customAttribute5 { get; set; }

        [JsonProperty("aircraft_customText1")]
        public string aircraft_customText1 { get; set; }

        [JsonProperty("aircraft_customText2")]
        public string aircraft_customText2 { get; set; }

        [JsonProperty("aircraft_customText3")]
        public string aircraft_customText3 { get; set; }

        [JsonProperty("aircraft_customText4")]
        public string aircraft_customText4 { get; set; }

        [JsonProperty("aircraft_efis")]
        public bool aircraft_efis { get; set; }

        [JsonProperty("aircraft_experimental")]
        public bool aircraft_experimental { get; set; }

        [JsonProperty("aircraft_fuelInjection")]
        public bool aircraft_fuelInjection { get; set; }

        [JsonProperty("aircraft_highPerformance")]
        public bool aircraft_highPerformance { get; set; }

        [JsonProperty("aircraft_military")]
        public bool aircraft_military { get; set; }

        [JsonProperty("aircraft_notes")]
        public string aircraft_notes { get; set; }

        [JsonProperty("aircraft_pressurized")]
        public bool aircraft_pressurized { get; set; }

        [JsonProperty("aircraft_radialEngine")]
        public bool aircraft_radialEngine { get; set; }

        [JsonProperty("aircraft_secondaryID")]
        public string aircraft_secondaryID { get; set; }

        [JsonProperty("aircraft_selectedOperatorName")]
        public string aircraft_selectedOperatorName { get; set; }

        [JsonProperty("aircraft_selectedOwnerName")]
        public string aircraft_selectedOwnerName { get; set; }

        [JsonProperty("aircraft_serialNumber")]
        public string aircraft_serialNumber { get; set; }

        [JsonProperty("aircraft_tailwheel")]
        public bool aircraft_tailwheel { get; set; }

        [JsonProperty("aircraft_technicallyAdvanced")]
        public bool aircraft_technicallyAdvanced { get; set; }

        [JsonProperty("aircraft_turboCharged")]
        public bool aircraft_turboCharged { get; set; }

        [JsonProperty("aircraft_undercarriageAmphib")]
        public bool aircraft_undercarriageAmphib { get; set; }

        [JsonProperty("aircraft_undercarriageFloats")]
        public bool aircraft_undercarriageFloats { get; set; }

        [JsonProperty("aircraft_undercarriageRetractable")]
        public bool aircraft_undercarriageRetractable { get; set; }

        [JsonProperty("aircraft_undercarriageSkids")]
        public bool aircraft_undercarriageSkids { get; set; }

        [JsonProperty("aircraft_undercarriageSkis")]
        public bool aircraft_undercarriageSkis { get; set; }

        [JsonProperty("aircraft_warbird")]
        public bool aircraft_warbird { get; set; }

        [JsonProperty("aircraft_weight")]
        public int aircraft_weight { get; set; }

        [JsonProperty("aircraft_wheelConfiguration")]
        public string aircraft_wheelConfiguration { get; set; }

        [JsonProperty("aircraft_year")]
        public int aircraft_year { get; set; }

        [JsonProperty("aircraftType_make")]
        public string aircraftType_make { get; set; }

        [JsonProperty("aircraftType_model")]
        public string aircraftType_model { get; set; }

        [JsonProperty("aircraftType_notes")]
        public string aircraftType_notes { get; set; }

        [JsonProperty("aircraftType_selectedAircraftClass")]
        public string aircraftType_selectedAircraftClass { get; set; }

        [JsonProperty("aircraftType_selectedCategory")]
        public string aircraftType_selectedCategory { get; set; }

        [JsonProperty("aircraftType_selectedEngineType")]
        public string aircraftType_selectedEngineType { get; set; }

        [JsonProperty("aircraftType_type")]
        public string aircraftType_type { get; set; }

        [JsonProperty("flight_actualArrivalTime")]
        [JsonConverter(typeof(MFBDateTimeConverter))]
        public DateTime flight_actualArrivalTime { get; set; }

        [JsonConverter(typeof(MFBDateTimeConverter))]
        [JsonProperty("flight_actualDepartureTime")]
        public DateTime flight_actualDepartureTime { get; set; }

        [JsonProperty("flight_actualInstrument")]
        public decimal flight_actualInstrument { get; set; }

        [JsonProperty("flight_aeroTows")]
        public int flight_aeroTows { get; set; }

        [JsonProperty("flight_arrests")]
        public int flight_arrests { get; set; }

        [JsonProperty("flight_autolands")]
        public int flight_autolands { get; set; }

        [JsonProperty("flight_bolters")]
        public int flight_bolters { get; set; }

        [JsonProperty("flight_catII")]
        public int flight_catII { get; set; }

        [JsonProperty("flight_catIII")]
        public int flight_catIII { get; set; }

        [JsonProperty("flight_cloudbase")]
        public int flight_cloudbase { get; set; }

        [JsonProperty("flight_commandPractice")]
        public int flight_commandPractice { get; set; }

        [JsonProperty("flight_crossCountry")]
        public decimal flight_crossCountry { get; set; }

        [JsonProperty("flight_customLanding1")]
        public int flight_customLanding1 { get; set; }

        [JsonProperty("flight_customLanding10")]
        public int flight_customLanding10 { get; set; }

        [JsonProperty("flight_customLanding2")]
        public int flight_customLanding2 { get; set; }

        [JsonProperty("flight_customLanding3")]
        public int flight_customLanding3 { get; set; }

        [JsonProperty("flight_customLanding4")]
        public int flight_customLanding4 { get; set; }

        [JsonProperty("flight_customLanding5")]
        public int flight_customLanding5 { get; set; }

        [JsonProperty("flight_customLanding6")]
        public int flight_customLanding6 { get; set; }

        [JsonProperty("flight_customLanding7")]
        public int flight_customLanding7 { get; set; }

        [JsonProperty("flight_customLanding8")]
        public int flight_customLanding8 { get; set; }

        [JsonProperty("flight_customLanding9")]
        public int flight_customLanding9 { get; set; }

        [JsonProperty("flight_customNote1")]
        public string flight_customNote1 { get; set; }

        [JsonProperty("flight_customNote2")]
        public string flight_customNote2 { get; set; }

        [JsonProperty("flight_customNote3")]
        public string flight_customNote3 { get; set; }

        [JsonProperty("flight_customNote4")]
        public string flight_customNote4 { get; set; }

        [JsonProperty("flight_customNote5")]
        public string flight_customNote5 { get; set; }

        [JsonProperty("flight_customOp1")]
        public decimal flight_customOp1 { get; set; }

        [JsonProperty("flight_customOp10")]
        public decimal flight_customOp10 { get; set; }

        [JsonProperty("flight_customOp2")]
        public decimal flight_customOp2 { get; set; }

        [JsonProperty("flight_customOp3")]
        public decimal flight_customOp3 { get; set; }

        [JsonProperty("flight_customOp4")]
        public decimal flight_customOp4 { get; set; }

        [JsonProperty("flight_customOp5")]
        public decimal flight_customOp5 { get; set; }

        [JsonProperty("flight_customOp6")]
        public decimal flight_customOp6 { get; set; }

        [JsonProperty("flight_customOp7")]
        public decimal flight_customOp7 { get; set; }

        [JsonProperty("flight_customOp8")]
        public decimal flight_customOp8 { get; set; }

        [JsonProperty("flight_customOp9")]
        public decimal flight_customOp9 { get; set; }

        [JsonProperty("flight_customTakeoff1")]
        public int flight_customTakeoff1 { get; set; }

        [JsonProperty("flight_customTakeoff10")]
        public int flight_customTakeoff10 { get; set; }

        [JsonProperty("flight_customTakeoff2")]
        public int flight_customTakeoff2 { get; set; }

        [JsonProperty("flight_customTakeoff3")]
        public int flight_customTakeoff3 { get; set; }

        [JsonProperty("flight_customTakeoff4")]
        public int flight_customTakeoff4 { get; set; }

        [JsonProperty("flight_customTakeoff5")]
        public int flight_customTakeoff5 { get; set; }

        [JsonProperty("flight_customTakeoff6")]
        public int flight_customTakeoff6 { get; set; }

        [JsonProperty("flight_customTakeoff7")]
        public int flight_customTakeoff7 { get; set; }

        [JsonProperty("flight_customTakeoff8")]
        public int flight_customTakeoff8 { get; set; }

        [JsonProperty("flight_customTakeoff9")]
        public int flight_customTakeoff9 { get; set; }

        [JsonProperty("flight_customTime1")]
        public decimal flight_customTime1 { get; set; }

        [JsonProperty("flight_customTime10")]
        public decimal flight_customTime10 { get; set; }

        [JsonProperty("flight_customTime2")]
        public decimal flight_customTime2 { get; set; }

        [JsonProperty("flight_customTime3")]
        public decimal flight_customTime3 { get; set; }

        [JsonProperty("flight_customTime4")]
        public decimal flight_customTime4 { get; set; }

        [JsonProperty("flight_customTime5")]
        public decimal flight_customTime5 { get; set; }

        [JsonProperty("flight_customTime6")]
        public decimal flight_customTime6 { get; set; }

        [JsonProperty("flight_customTime7")]
        public decimal flight_customTime7 { get; set; }

        [JsonProperty("flight_customTime8")]
        public decimal flight_customTime8 { get; set; }

        [JsonProperty("flight_customTime9")]
        public decimal flight_customTime9 { get; set; }

        [JsonProperty("flight_dayLandings")]
        public int flight_dayLandings { get; set; }

        [JsonProperty("flight_dayTakeoffs")]
        public int flight_dayTakeoffs { get; set; }

        [JsonProperty("flight_dualGiven")]
        public decimal flight_dualGiven { get; set; }

        [JsonProperty("flight_dualReceived")]
        public decimal flight_dualReceived { get; set; }

        [JsonProperty("flight_dualReceivedNight")]
        public decimal flight_dualReceivedNight { get; set; }

        [JsonProperty("flight_duration")]
        public decimal flight_duration { get; set; }

        [JsonProperty("flight_faaPart121")]
        public decimal flight_faaPart121 { get; set; }

        [JsonProperty("flight_faaPart135")]
        public bool flight_faaPart135 { get; set; }

        [JsonProperty("flight_faaPart61")]
        public bool flight_faaPart61 { get; set; }

        [JsonProperty("flight_faaPart91")]
        public bool flight_faaPart91 { get; set; }

        [JsonProperty("flight_far1")]
        public bool flight_far1 { get; set; }

        [JsonProperty("flight_flightEngineer")]
        public decimal flight_flightEngineer { get; set; }

        [JsonProperty("flight_flightEngineerCapacity")]
        public bool flight_flightEngineerCapacity { get; set; }

        [JsonProperty("flight_flightNumber")]
        public string flight_flightNumber { get; set; }

        [JsonProperty("flight_from")]
        public string flight_from { get; set; }

        [JsonProperty("flight_fuelAdded")]
        public decimal flight_fuelAdded { get; set; }

        [JsonProperty("flight_fuelBurned")]
        public decimal flight_fuelBurned { get; set; }

        [JsonProperty("flight_fuelMinimumForDiversion")]
        public decimal flight_fuelMinimumForDiversion { get; set; }

        [JsonProperty("flight_fuelTotalAboard")]
        public decimal flight_fuelTotalAboard { get; set; }

        [JsonProperty("flight_fuelTotalBeforeUplift")]
        public decimal flight_fuelTotalBeforeUplift { get; set; }

        [JsonProperty("flight_fuelUplift")]
        public decimal flight_fuelUplift { get; set; }

        [JsonProperty("flight_fullStops")]
        public int flight_fullStops { get; set; }

        [JsonProperty("flight_goArounds")]
        public int flight_goArounds { get; set; }

        [JsonProperty("flight_ground")]
        public decimal flight_ground { get; set; }

        [JsonProperty("flight_groundLaunches")]
        public int flight_groundLaunches { get; set; }

        [JsonProperty("flight_hobbsStart")]
        public decimal flight_hobbsStart { get; set; }

        [JsonProperty("flight_holds")]
        public int flight_holds { get; set; }

        [JsonProperty("flight_instrumentProficiencyCheck")]
        public bool flight_instrumentProficiencyCheck { get; set; }

        [JsonProperty("flight_landingCapacity")]
        public bool flight_landingCapacity { get; set; }

        [JsonProperty("flight_landingTime")]
        [JsonConverter(typeof(MFBDateTimeConverter))]
        public DateTime flight_landingTime { get; set; }

        [JsonProperty("flight_multiPilot")]
        public decimal flight_multiPilot { get; set; }

        [JsonProperty("flight_night")]
        public decimal flight_night { get; set; }

        [JsonProperty("flight_nightLandings")]
        public int flight_nightLandings { get; set; }

        [JsonProperty("flight_nightTakeoffs")]
        public int flight_nightTakeoffs { get; set; }

        [JsonProperty("flight_nightVisionGoggle")]
        public decimal flight_nightVisionGoggle { get; set; }

        [JsonProperty("flight_nightVisionGoggleLandings")]
        public int flight_nightVisionGoggleLandings { get; set; }

        [JsonProperty("flight_nightVisionGoggleTakeoffs")]
        public int flight_nightVisionGoggleTakeoffs { get; set; }

        [JsonConverter(typeof(MFBDateTimeConverter))]
        [JsonProperty("flight_offDutyTime")]
        public DateTime flight_offDutyTime { get; set; }
        
        [JsonProperty("flight_onDutyTime")]
        [JsonConverter(typeof(MFBDateTimeConverter))]
        public DateTime flight_onDutyTime { get; set; }

        [JsonProperty("flight_p1us")]
        public decimal flight_p1us { get; set; }

        [JsonProperty("flight_p1usNight")]
        public decimal flight_p1usNight { get; set; }

        [JsonProperty("flight_pic")]
        public decimal flight_pic { get; set; }

        [JsonProperty("flight_picCapacity")]
        public bool flight_picCapacity { get; set; }

        [JsonProperty("flight_picNight")]
        public decimal flight_picNight { get; set; }

        [JsonProperty("flight_pilotFlyingCapacity")]
        public bool flight_pilotFlyingCapacity { get; set; }

        [JsonProperty("flight_poweredLaunches")]
        public int flight_poweredLaunches { get; set; }

        [JsonProperty("flight_relief")]
        public decimal flight_relief { get; set; }

        [JsonProperty("flight_reliefCrewCapacity")]
        public bool flight_reliefCrewCapacity { get; set; }

        [JsonProperty("flight_remarks")]
        public string flight_remarks { get; set; }

        [JsonProperty("flight_review")]
        public bool flight_review { get; set; }

        [JsonProperty("flight_route")]
        public string flight_route { get; set; }

        [JsonProperty("flight_scheduledArrivalTime")]
        [JsonConverter(typeof(MFBDateTimeConverter))]
        public DateTime flight_scheduledArrivalTime { get; set; }

        [JsonConverter(typeof(MFBDateTimeConverter))]
        [JsonProperty("flight_scheduledDepartureTime")]
        public DateTime flight_scheduledDepartureTime { get; set; }

        [JsonProperty("flight_scheduledTotalTime")]
        public decimal flight_scheduledTotalTime { get; set; }

        [JsonProperty("flight_selectedApproach1")]
        public string flight_selectedApproach1 { get; set; }

        [JsonProperty("flight_selectedCrewCommander")]
        public string flight_selectedCrewCommander { get; set; }

        [JsonProperty("flight_selectedCrewCustom1")]
        public string flight_selectedCrewCustom1 { get; set; }

        [JsonProperty("flight_selectedCrewCustom2")]
        public string flight_selectedCrewCustom2 { get; set; }

        [JsonProperty("flight_selectedCrewCustom3")]
        public string flight_selectedCrewCustom3 { get; set; }

        [JsonProperty("flight_selectedCrewCustom4")]
        public string flight_selectedCrewCustom4 { get; set; }

        [JsonProperty("flight_selectedCrewCustom5")]
        public string flight_selectedCrewCustom5 { get; set; }

        [JsonProperty("flight_selectedCrewFlightAttendant")]
        public string flight_selectedCrewFlightAttendant { get; set; }

        [JsonProperty("flight_selectedCrewFlightAttendant2")]
        public string flight_selectedCrewFlightAttendant2 { get; set; }

        [JsonProperty("flight_selectedCrewFlightAttendant3")]
        public string flight_selectedCrewFlightAttendant3 { get; set; }

        [JsonProperty("flight_selectedCrewFlightAttendant4")]
        public string flight_selectedCrewFlightAttendant4 { get; set; }

        [JsonProperty("flight_selectedCrewFlightEngineer")]
        public string flight_selectedCrewFlightEngineer { get; set; }

        [JsonProperty("flight_selectedCrewInstructor")]
        public string flight_selectedCrewInstructor { get; set; }

        [JsonProperty("flight_selectedCrewObserver")]
        public string flight_selectedCrewObserver { get; set; }

        [JsonProperty("flight_selectedCrewObserver2")]
        public string flight_selectedCrewObserver2 { get; set; }

        [JsonProperty("flight_selectedCrewPIC")]
        public string flight_selectedCrewPIC { get; set; }

        [JsonProperty("flight_selectedCrewPurser")]
        public string flight_selectedCrewPurser { get; set; }

        [JsonProperty("flight_selectedCrewRelief")]
        public string flight_selectedCrewRelief { get; set; }

        [JsonProperty("flight_selectedCrewRelief2")]
        public string flight_selectedCrewRelief2 { get; set; }

        [JsonProperty("flight_selectedCrewRelief3")]
        public string flight_selectedCrewRelief3 { get; set; }

        [JsonProperty("flight_selectedCrewRelief4")]
        public string flight_selectedCrewRelief4 { get; set; }

        [JsonProperty("flight_selectedCrewSIC")]
        public string flight_selectedCrewSIC { get; set; }

        [JsonProperty("flight_selectedCrewStudent")]
        public string flight_selectedCrewStudent { get; set; }

        [JsonProperty("flight_sfi")]
        public decimal flight_sfi { get; set; }

        [JsonProperty("flight_shipboardLandings")]
        public int flight_shipboardLandings { get; set; }

        [JsonProperty("flight_shipboardTakeoffs")]
        public int flight_shipboardTakeoffs { get; set; }

        [JsonProperty("flight_sic")]
        public decimal flight_sic { get; set; }

        [JsonProperty("flight_sicCapacity")]
        public bool flight_sicCapacity { get; set; }

        [JsonProperty("flight_sicNight")]
        public decimal flight_sicNight { get; set; }

        [JsonProperty("flight_simulatedInstrument")]
        public decimal flight_simulatedInstrument { get; set; }

        [JsonProperty("flight_simulator")]
        public decimal flight_simulator { get; set; }

        [JsonProperty("flight_sky")]
        public string flight_sky { get; set; }

        [JsonProperty("flight_solo")]
        public decimal flight_solo { get; set; }

        [JsonConverter(typeof(MFBDateTimeConverter))]
        [JsonProperty("flight_takeoffTime")]
        public DateTime flight_takeoffTime { get; set; }

        [JsonProperty("flight_to")]
        public string flight_to { get; set; }

        [JsonProperty("flight_totalDutyTime")]
        public decimal flight_totalDutyTime { get; set; }

        [JsonProperty("flight_totalTime")]
        public decimal flight_totalTime { get; set; }

        [JsonProperty("flight_touchAndGoes")]
        public int flight_touchAndGoes { get; set; }

        [JsonProperty("flight_underSupervisionCapacity")]
        public bool flight_underSupervisionCapacity { get; set; }

        [JsonProperty("flight_visibility")]
        public decimal flight_visibility { get; set; }

        [JsonProperty("flight_waterLandings")]
        public int flight_waterLandings { get; set; }

        [JsonProperty("flight_waterTakeoffs")]
        public int flight_waterTakeoffs { get; set; }

        [JsonProperty("flight_weather")]
        public string flight_weather { get; set; }

        [JsonProperty("flight_windDirection")]
        public int flight_windDirection { get; set; }

        [JsonProperty("flight_windVelocity")]
        public int flight_windVelocity { get; set; }

        [JsonProperty("flight_customCapacity1")]
        public bool flight_customCapacity1 { get; set; }

        [JsonProperty("flight_customCapacity2")]
        public bool flight_customCapacity2 { get; set; }

        [JsonProperty("flight_customCapacity3")]
        public bool flight_customCapacity3 { get; set; }

        [JsonProperty("flight_customCapacity4")]
        public bool flight_customCapacity4 { get; set; }

        [JsonProperty("flight_customCapacity5")]
        public bool flight_customCapacity5 { get; set; }

        [JsonProperty("flight_customCapacity6")]
        public bool flight_customCapacity6 { get; set; }

        [JsonProperty("flight_customCapacity7")]
        public bool flight_customCapacity7 { get; set; }

        [JsonProperty("flight_customCapacity8")]
        public bool flight_customCapacity8 { get; set; }

        [JsonProperty("flight_distance")]
        public decimal flight_distance { get; set; }

        [JsonConverter(typeof(MFBDateTimeConverter))]
        [JsonProperty("flight_flightDate")]
        public DateTime flight_flightDate { get; set; }

        [JsonProperty("flight_hobbsStop")]
        public decimal flight_hobbsStop { get; set; }

        [JsonProperty("flight_paxCount")]
        public int flight_paxCount { get; set; }

        [JsonProperty("flight_paxCountBusiness")]
        public int flight_paxCountBusiness { get; set; }

        [JsonProperty("flight_selectedApproach10")]
        public string flight_selectedApproach10 { get; set; }

        [JsonProperty("flight_selectedApproach2")]
        public string flight_selectedApproach2 { get; set; }

        [JsonProperty("flight_selectedApproach3")]
        public string flight_selectedApproach3 { get; set; }

        [JsonProperty("flight_selectedApproach4")]
        public string flight_selectedApproach4 { get; set; }

        [JsonProperty("flight_selectedApproach5")]
        public string flight_selectedApproach5 { get; set; }

        [JsonProperty("flight_selectedApproach6")]
        public string flight_selectedApproach6 { get; set; }

        [JsonProperty("flight_selectedApproach7")]
        public string flight_selectedApproach7 { get; set; }

        [JsonProperty("flight_selectedApproach8")]
        public string flight_selectedApproach8 { get; set; }

        [JsonProperty("flight_selectedApproach9")]
        public string flight_selectedApproach9 { get; set; }

        [JsonProperty("flight_selectedPax1")]
        public string flight_selectedPax1 { get; set; }

        [JsonProperty("flight_selectedPax10")]
        public string flight_selectedPax10 { get; set; }

        [JsonProperty("flight_selectedPax2")]
        public string flight_selectedPax2 { get; set; }

        [JsonProperty("flight_selectedPax3")]
        public string flight_selectedPax3 { get; set; }

        [JsonProperty("flight_selectedPax4")]
        public string flight_selectedPax4 { get; set; }

        [JsonProperty("flight_selectedPax5")]
        public string flight_selectedPax5 { get; set; }

        [JsonProperty("flight_selectedPax6")]
        public string flight_selectedPax6 { get; set; }

        [JsonProperty("flight_selectedPax7")]
        public string flight_selectedPax7 { get; set; }

        [JsonProperty("flight_selectedPax8")]
        public string flight_selectedPax8 { get; set; }

        [JsonProperty("flight_selectedPax9")]
        public string flight_selectedPax9 { get; set; }

        [JsonProperty("flight_tachStart")]
        public decimal flight_tachStart { get; set; }

        [JsonProperty("flight_tachStop")]
        public decimal flight_tachStop { get; set; }

        [JsonProperty("flight_track")]
        public string flight_track { get; set; }
        #endregion

        public LogTenPro(DataRow dr) : base(dr) {  }

        public LogTenPro() : base() { }

        public override LogbookEntry ToLogbookEntry()
        {
            // Fix up UTC times, in case one is before the other.  Also verify that the date is being set to the date of flight.
            flight_takeoffTime = FixedUTCDateFromTime(flight_flightDate, flight_takeoffTime);
            flight_landingTime = FixedUTCDateFromTime(flight_flightDate, flight_landingTime, flight_takeoffTime);

            flight_actualDepartureTime = FixedUTCDateFromTime(flight_flightDate, flight_actualDepartureTime);
            flight_actualArrivalTime = FixedUTCDateFromTime(flight_flightDate, flight_actualArrivalTime, flight_actualDepartureTime);

            flight_scheduledDepartureTime = FixedUTCDateFromTime(flight_flightDate, flight_scheduledDepartureTime);
            flight_scheduledArrivalTime = FixedUTCDateFromTime(flight_flightDate, flight_scheduledArrivalTime, flight_scheduledDepartureTime);

            flight_onDutyTime = FixedUTCDateFromTime(flight_flightDate, flight_onDutyTime);
            flight_offDutyTime = FixedUTCDateFromTime(flight_flightDate, flight_offDutyTime, flight_onDutyTime);

            LogbookEntry le = new LogbookEntry()
            {
                Date = flight_flightDate,
                Route = JoinStrings(new string[] { flight_from, flight_route, flight_to }),
                TotalFlightTime = flight_totalTime,
                PIC = flight_pic,
                SIC = flight_sic,
                Nighttime = flight_night,
                CrossCountry = flight_crossCountry,
                IMC = flight_actualInstrument,
                SimulatedIFR = flight_simulatedInstrument,
                Dual = flight_dualReceived,
                CFI = flight_dualGiven,
                GroundSim = flight_simulator,
                Landings = flight_dayLandings + flight_nightLandings,
                FullStopLandings = Math.Max(flight_fullStops - flight_nightLandings, 0),
                NightLandings = flight_nightLandings,
                fHoldingProcedures = flight_holds != 0,
                Comment = JoinStrings(new string[] { flight_remarks,
                flight_selectedApproach1, flight_selectedApproach2, flight_selectedApproach3, flight_selectedApproach4, flight_selectedApproach5, flight_selectedApproach6, flight_selectedApproach7, flight_selectedApproach8, flight_selectedApproach9, flight_selectedApproach10,
                flight_customNote1, flight_customNote2, flight_customNote3, flight_customNote4, flight_customNote5 }),
                ModelDisplay = aircraftType_model,

                TailNumDisplay = (String.IsNullOrEmpty(aircraft_aircraftID) ? ((flight_simulator > 0) ? CountryCodePrefix.SimCountry.Prefix : CountryCodePrefix.AnonymousCountry.Prefix) : aircraft_aircraftID),

                FlightStart = flight_takeoffTime,
                FlightEnd = flight_landingTime,
                EngineStart = flight_actualDepartureTime,
                EngineEnd = flight_actualArrivalTime,
                HobbsStart = flight_hobbsStart,
                HobbsEnd = flight_hobbsStop,

                FlightData = flight_track
            };

        // Add in known properties
        le.CustomProperties.SetItems(new CustomFlightProperty[] {
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfPIC, flight_selectedCrewPIC),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfSIC, flight_selectedCrewSIC),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachStart, flight_tachStart),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachEnd, flight_tachStop),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, flight_solo),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNVGoggleTime, flight_nightVisionGoggle),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropWaterLandings, flight_waterLandings),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropWaterTakeoffs, flight_waterTakeoffs),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNVLandings, flight_nightVisionGoggleLandings),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart, flight_onDutyTime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd, flight_offDutyTime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, flight_nightTakeoffs),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledArrival, flight_scheduledArrivalTime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropScheduledDeparture, flight_scheduledDepartureTime, true),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightReview, flight_review),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropIPC, flight_instrumentProficiencyCheck),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPassengerNames, JoinStrings(new string[] { flight_selectedPax1, flight_selectedPax2, flight_selectedPax3, flight_selectedPax4, flight_selectedPax5, flight_selectedPax6, flight_selectedPax7, flight_selectedPax8, flight_selectedPax9, flight_selectedPax10 })),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, flight_flightNumber),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPilotFlyingTime, flight_pilotFlyingCapacity ? le.TotalFlightTime : 0),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropInstructorName, flight_selectedCrewInstructor),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived, flight_ground),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPassengerCount, flight_paxCount)
            });

            return le;
        }
    }

    public class LogTenProImporter : ExternalFormatImporter
    {
        public override string Name { get { return "LogTen Pro"; } }

        public override IEnumerable<ExternalFormat> FromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            List<LogTenPro> lst = new List<LogTenPro>();
            foreach (DataRow dr in dt.Rows)
                lst.Add(new LogTenPro(dr));
            return lst;
        }

        public override bool CanParse(byte[] rgb)
        {
            MemoryStream ms = new MemoryStream(rgb);

            // Read the first non-empty row and see if it has LogTenPro headers
            try
            {
                using (CSVReader reader = new CSVReader(ms))
                {
                    ms = null;
                    try
                    {
                        string[] rgszHeader = reader.GetCSVLine(true);
                        if (rgszHeader == null || rgszHeader.Length == 0)
                            return false;
                        HashSet<string> hs = new HashSet<string>();

                        foreach (string sz in rgszHeader)
                            hs.Add(sz.Trim());

                        return hs.Contains("flight_flightDate") && hs.Contains("aircraft_aircraftID");
                    }
                    catch (CSVReaderInvalidCSVException) { }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
            return false;
        }

        public override string CSVFromDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            using (DataTable dtDst = new DataTable())
            {
                dtDst.Locale = dt.Locale;
                CSVImporter.InitializeDataTable(dtDst);
                foreach(DataRow dr in dt.Rows)
                {
                    LogTenPro ltp = new LogTenPro(dr);
                    CSVImporter.WriteEntryToDataTable(ltp.ToLogbookEntry(), dtDst);
                }
                return CsvWriter.WriteToString(dtDst, true, true);
            }
        }
    }
}