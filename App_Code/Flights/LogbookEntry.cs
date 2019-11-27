using MyFlightbook.Airports;
using MyFlightbook.Encryptors;
using MyFlightbook.Histogram;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.SocialMedia;
using MyFlightbook.Telemetry;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Represents a logbook entry, including validation.  Supports retrieval from the database, updates, and entry of new rows
    /// </summary>
    [Serializable]
    public abstract class LogbookEntryBase
    {
        public const int idFlightNew = -1;
        public const int idFlightNone = -1;

        private int idAircraft;
        private String szError;
        private DateTime dtFlightStart;
        private DateTime dtFlightEnd;
        private DateTime dtEngineStart;
        private DateTime dtEngineEnd;
        private Boolean fHasDataStream;

        public enum SignatureState { None, Valid, Invalid }

        /// <summary>
        /// Options for loading telemetry:
        ///  - None = do not load
        ///  - MetadataOrDB = load any metadata OR load raw data if it's in the DB.  (If telemetry only, don't bother loading from the file)
        ///  - LoadAll - actually load the telemetry.
        /// </summary>
        public enum LoadTelemetryOption { None, MetadataOrDB, LoadAll}

        #region Properties
        /// <summary>
        /// User who took the flight
        /// </summary>
        public String User { get; set; }

        /// <summary>
        /// ID of aircraft used in the flight
        /// </summary>
        public int AircraftID
        {
            get { return idAircraft; }
            set { if (value > 0) idAircraft = value; }
        }

        /// <summary>
        /// The cat/class for the flight, which overrides the one specified in the model
        /// </summary>
        public int CatClassOverride { get; set; }

        /// <summary>
        /// Number of night-time full-stop landings
        /// </summary>
        public int NightLandings { get; set; }

        /// <summary>
        /// Number of full-stop landings (assumed day)
        /// </summary>
        public int FullStopLandings { get; set; }

        /// <summary>
        /// # of instrument approaches
        /// </summary>
        public int Approaches { get; set; }

        /// <summary>
        /// Hack for backwards compatibility with Android
        /// </summary>
        public int PrecisionApproaches { get; set; }

        /// <summary>
        /// Hack for backwards compatibility with Android
        /// </summary>
        public int NonPrecisionApproaches { get; set; }

        /// <summary>
        /// # of landings
        /// </summary>
        public int Landings { get; set; }

        /// <summary>
        /// Cross-country flight time
        /// </summary>
        public Decimal CrossCountry { get; set; }

        /// <summary>
        /// Night flight time
        /// </summary>
        public Decimal Nighttime { get; set; }

        /// <summary>
        /// Actual instrument conditions
        /// </summary>
        public Decimal IMC { get; set; }

        /// <summary>
        /// Time under the hood
        /// </summary>
        public Decimal SimulatedIFR { get; set; }

        /// <summary>
        /// Ground simulator time
        /// </summary>
        public Decimal GroundSim { get; set; }

        /// <summary>
        /// Time receiving instruction
        /// </summary>
        public Decimal Dual { get; set; }

        /// <summary>
        /// Time spent instructing
        /// </summary>
        public Decimal CFI { get; set; }

        /// <summary>
        /// Pilot in command time
        /// </summary>
        public Decimal PIC { get; set; }

        /// <summary>
        /// Second in command time
        /// </summary>
        public Decimal SIC { get; set; }

        /// <summary>
        /// Total time of flight
        /// </summary>
        public Decimal TotalFlightTime { get; set; }

        /// <summary>
        /// Was there a hold?
        /// </summary>
        public Boolean fHoldingProcedures { get; set; }

        /// <summary>
        /// Route of the flight
        /// </summary>
        public String Route { get; set; }

        /// <summary>
        /// Any comments about the flight
        /// </summary>
        public String Comment { get; set; }

        /// <summary>
        /// Share flight details with everyone?
        /// </summary>
        public Boolean fIsPublic { get; set; }

        /// <summary>
        /// Date of the flight
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// An error message for the last operation that failed
        /// </summary>
        public String ErrorString
        {
            get { return szError; }
            set { szError = value; }
        }

        public enum ErrorCode { None, Unknown, NotFound, NotOwned, InvalidUser, InvalidAircraft, NegativeTime, NegativeCount, InvalidHobbs, InvalidEngine, InvalidFlight, InvalidDate, InvalidLandings, InvalidApproaches, InvalidNightTakeoffs, DataTooLong, MissingNight }

        private ErrorCode m_lastErr = ErrorCode.None;
        /// <summary>
        /// Error code for last error
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ErrorCode LastError
        {
            get { return m_lastErr; }
            set { m_lastErr = value; }
        }

        /// <summary>
        /// ID of the flight.  You shouldn't set this.
        /// </summary>
        public Int32 FlightID { get; set; }

        /// <summary>
        /// UTC time that the flight started
        /// </summary>
        public DateTime FlightStart
        {
            get { return dtFlightStart; }
            set { dtFlightStart = DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        }

        /// <summary>
        /// UTC time that the flight ended
        /// </summary>
        public DateTime FlightEnd
        {
            get { return dtFlightEnd; }
            set { dtFlightEnd = DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        }

        /// <summary>
        /// UTC time that the engine was fired up
        /// </summary>
        public DateTime EngineStart
        {
            get { return dtEngineStart; }
            set { dtEngineStart = DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        }

        /// <summary>
        /// UTC time that the engine was shut down
        /// </summary>
        public DateTime EngineEnd
        {
            get { return dtEngineEnd; }
            set { dtEngineEnd = DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        }

        /// <summary>
        /// Hobbs time of the engine at startup
        /// </summary>
        public Decimal HobbsStart { get; set; }

        /// <summary>
        /// Hobbs time of the engine at shutdown
        /// </summary>
        public Decimal HobbsEnd { get; set; }

        /// <summary>
        /// Display name of the model type; this cannot be persisted via logbook entry
        /// </summary>
        public string ModelDisplay { get; set; }

        /// <summary>
        /// Tailnumber of the airplane; this cannot be persisted via a logbook entry (it's a property of the airplane)
        /// </summary>
        public string TailNumDisplay { get; set; }

        /// <summary>
        /// Human readable string for Category/class (and type, if relevant); this cannot be persisted via a logbook entry (it's a property of the airplane)
        /// </summary>
        public string CatClassDisplay { get; set; }

        /// <summary>
        /// Indicates whether or not this flight has a data stream associated with it.
        /// </summary>
        public Boolean HasFlightData
        {
            get { return fHasDataStream; }
        }

        /// <summary>
        /// Flight data for the flight.
        /// </summary>
        public string FlightData
        {
            get { return Telemetry.RawData; }
            set
            {
                Telemetry = new TelemetryReference() { RawData = value, FlightID = this.FlightID }; // don't parse!
                fHasDataStream = !String.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Summary data about the flight's telemetry.  CAN BE NULL
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public TelemetryReference Telemetry { get; set; }

        /// <summary>
        /// Custom properties for the entry.  
        /// </summary>
        public CustomPropertyCollection CustomProperties { get; set; }

        /// <summary>
        /// Images associated with the flight.  For efficiency, this MUST BE EXPLICITLY POPULATED AND SET.
        /// </summary>
        public MFBImageInfo[] FlightImages { get; set; }

        /// <summary>
        /// Videos associated with the flight.
        /// </summary>
        public VideoRef[] Videos { get; set; }

        #region Signature properties
        /// <summary>
        /// The hash of the flight that was signed.  This must equal the newly computed hash of the flight in order for the signature to be valid.
        /// </summary>
        private string FlightHash { get; set; }

        /// <summary>
        /// The hash of the flight signature details.
        /// </summary>
        private string SignatureHash { get; set; }

        /// <summary>
        /// Comments from the instructor
        /// </summary>
        public string CFIComments { get; set; }

        /// <summary>
        /// The date that the flight was signed
        /// </summary>
        public DateTime CFISignatureDate { get; set; }

        /// <summary>
        /// The CFI's certificate
        /// </summary>
        public string CFICertificate { get; set; }

        /// <summary>
        /// Expiration date of the CFI's certificate
        /// </summary>
        public DateTime CFIExpiration { get; set; }

        /// <summary>
        /// The username of the signing CFI.  Can be null/empty if ad-hoc (handwritten) signed.
        /// </summary>
        private string CFIUsername { get; set; }

        /// <summary>
        /// CACHED email of the CFI at the point of signing.  May no longer be their email.
        /// </summary>
        public string CFIEmail { get; set; }

        /// <summary>
        /// CACHED name of CFI at the point of signing.  May no longer be their name.
        /// </summary>
        public string CFIName { get; set; }

        /// <summary>
        /// State of the current signature
        /// </summary>
        public SignatureState CFISignatureState { get; set; }

        /// <summary>
        /// Digitized PNG of the signature.  Not read by default; call LoadDigitalSig to get it.
        /// </summary>
        public byte[] DigitizedSignature { get; set; }

        /// <summary>
        /// Does this have a digitized sig?
        /// </summary>
        public bool HasDigitizedSig { get; set; }

        /// <summary>
        /// Quick & Dirty display string
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayString
        {
            get { return this.ToString(); }
        }

        #endregion

        // These are overridden in child classes; declared here so that we don't have to expose LogbookEntryBase serializable
        public virtual string SendFlightLink { get; set; }
        public virtual string SocialMediaLink { get; set; }
        #endregion

        #region SignedFlights

        public void LoadDigitalSig()
        {
            DBHelper dbh = new DBHelper(String.Format("SELECT DigitizedSignature, LENGTH(DigitizedSignature) AS FileSize FROM flights WHERE idFlight={0}", FlightID));
            int FileSize = 0;
            dbh.ReadRow((comm) => { }, (dr) =>
            {
                if (!(dr["FileSize"] is DBNull))
                {
                    FileSize = Convert.ToInt32(dr["FileSize"]);
                    DigitizedSignature = new byte[FileSize];
                    dr.GetBytes(dr.GetOrdinal("DigitizedSignature"), 0, DigitizedSignature, 0, FileSize);
                }
                else
                {
                    FileSize = 0;
                    DigitizedSignature = new byte[0];
                }
            });
        }

        /// <summary>
        /// Computes the hash for a flight so that we can tell if it is modified in a meaningful way.
        /// ONLY VALID IF PROPERTIES HAVE BEEN LOADED FOR A FLIGHT
        /// Limited to 2048 characters.  It is also encrypted.  The underlying data is not really much of a hash - it's a pretty direct serialization, so most changes should be caught.
        /// </summary>
        /// <returns>The hash</returns>
        public string ComputeFlightHash()
        {
            if (String.IsNullOrEmpty(User))
                throw new MyFlightbookException("Can't sign the flight without a username specified");

            // Sort the properties by propertytypeID so that there is a deterministic hash (i.e., not affected by order of properties.)
            List<CustomFlightProperty> lstProps = new List<CustomFlightProperty>(CustomProperties);
            lstProps.Sort((cfp1, cfp2) => { return cfp1.PropTypeID - cfp2.PropTypeID; });

            StringBuilder sbPropHash = new StringBuilder();
            CustomPropertyType[] rgcpt = null;
            foreach (CustomFlightProperty cfp in lstProps)
            {
                if (cfp.PropertyType == null || cfp.PropertyType.PropTypeID == (int) CustomPropertyType.KnownProperties.IDPropInvalid)
                {
                    if (rgcpt == null)
                        rgcpt = CustomPropertyType.GetCustomPropertyTypes(this.User);
                    cfp.InitPropertyType(rgcpt);
                }
                sbPropHash.AppendFormat(CultureInfo.InvariantCulture, "ID{0}V{1}", cfp.PropTypeID, cfp.ValueStringInvariant);
            }

            string szHash = String.Format(CultureInfo.InvariantCulture, "ID{0}DT{1}AC{2}A{3}H{4}L{5}NL{6}XC{7:0.0}N{8:0.0}SI{9:0.0}IM{10:0.0}GS{11:0.0}DU{12:0.0}CF{13:0.0}SI{14:0.0}PI{15:0.0}TT{16:0.0}PR{17}CC{18}CM{19}",
                FlightID, Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), AircraftID, Approaches, fHoldingProcedures ? 1 : 0, Landings, NightLandings, CrossCountry,
                Nighttime, SimulatedIFR, IMC, GroundSim,
                Dual, CFI, SIC, PIC, TotalFlightTime, sbPropHash.ToString(), CatClassOverride, Comment);

            UserEncryptor ue = new UserEncryptor(User);
            string szSig = ue.Encrypt(szHash);
            // it won't decrypt if we go too long, so trim from the end (to remove long comments, if needed and recompute)
            // We'll go 20 chars at a time.
            while (szSig.Length > 2048 && szHash.Length > 20)
            {
                szHash = szHash.LimitTo(szHash.Length - 20);
                szSig = ue.Encrypt(szHash);
            }

            return szSig;
        }

        public static LogbookEntry LogbookEntryFromHash(string szHash)
        {
            if (String.IsNullOrEmpty(szHash))
                throw new ArgumentNullException("szHash");

            Regex rFlight = new Regex("^ID(?<ID>\\d+)DT(?<Date>[0-9-]+)AC(?<Aircraft>\\d+)A(?<Approaches>\\d+)H(?<Hold>[01])L(?<Landings>\\d+)NL(?<NightLandings>\\d+)XC(?<XC>[0-9.]+)N(?<Night>[0-9.]+)SI(?<SimInst>[0-9.]+)IM(?<IMC>[0-9.]+)GS(?<GroundSim>[0-9.]+)DU(?<Dual>[0-9.]+)CF(?<CFI>[0-9.]+)SI(?<SIC>[0-9.]+)PI(?<PIC>[0-9.]+)TT(?<Total>[0-9.]+)PR(?<props>.*)CC(?<CatClassOver>\\d+)CM(?<Comments>.*)$", RegexOptions.Compiled);

            LogbookEntry le = new LogbookEntry();

            MatchCollection mc = rFlight.Matches(szHash);
            if (mc.Count == 1)
            {
                GroupCollection gc = mc[0].Groups;
                le.FlightID = Convert.ToInt32(gc["ID"].Value, CultureInfo.InvariantCulture);
                le.Date = Convert.ToDateTime(gc["Date"].Value, CultureInfo.InvariantCulture);
                le.AircraftID = Convert.ToInt32(gc["Aircraft"].Value, CultureInfo.InvariantCulture);
                le.Approaches = Convert.ToInt32(gc["Approaches"].Value, CultureInfo.InvariantCulture);
                le.fHoldingProcedures = gc["Hold"].Value.CompareOrdinal("1") == 0;
                le.Landings = Convert.ToInt32(gc["Landings"].Value, CultureInfo.InvariantCulture);
                le.NightLandings = Convert.ToInt32(gc["NightLandings"].Value, CultureInfo.InvariantCulture);
                le.CrossCountry = Convert.ToDecimal(gc["XC"].Value, CultureInfo.InvariantCulture);
                le.Nighttime = Convert.ToDecimal(gc["Night"].Value, CultureInfo.InvariantCulture);
                le.SimulatedIFR = Convert.ToDecimal(gc["SimInst"].Value, CultureInfo.InvariantCulture);
                le.IMC = Convert.ToDecimal(gc["IMC"].Value, CultureInfo.InvariantCulture);
                le.GroundSim = Convert.ToDecimal(gc["GroundSim"].Value, CultureInfo.InvariantCulture);
                le.Dual = Convert.ToDecimal(gc["Dual"].Value, CultureInfo.InvariantCulture);
                le.CFI = Convert.ToDecimal(gc["CFI"].Value, CultureInfo.InvariantCulture);
                le.SIC = Convert.ToDecimal(gc["SIC"].Value, CultureInfo.InvariantCulture);
                le.PIC = Convert.ToDecimal(gc["PIC"].Value, CultureInfo.InvariantCulture);
                le.TotalFlightTime = Convert.ToDecimal(gc["Total"].Value, CultureInfo.InvariantCulture);
                le.CatClassOverride = Convert.ToInt32(gc["CatClassOver"].Value, CultureInfo.InvariantCulture);
                le.Comment = gc["Comments"].Value;

                Aircraft ac = new Aircraft(le.AircraftID);
                le.TailNumDisplay = ac.DisplayTailnumber;

                string[] rgProps = gc["props"].Value.Split(new string[] { "ID" }, StringSplitOptions.RemoveEmptyEntries);

                if (rgProps.Length > 0)
                {
                    Regex rProp = new Regex("(?<PropID>\\d+)V(?<Value>.+)", RegexOptions.Compiled);
                    List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
                    foreach (string szProp in rgProps)
                    {
                        MatchCollection mProp = rProp.Matches(szProp);
                        if (mProp.Count > 0)
                        {
                            GroupCollection gcProp = mProp[0].Groups;
                            if (gcProp.Count == 3)
                            {
                                CustomPropertyType cpt = CustomPropertyType.GetCustomPropertyType(Convert.ToInt32(gcProp["PropID"].Value, CultureInfo.InvariantCulture));
                                CustomFlightProperty cfp = new CustomFlightProperty(cpt) { FlightID = le.FlightID };
                                cfp.InitFromString(gcProp["Value"].Value);
                                lst.Add(cfp);
                            }
                        }
                    }
                    le.CustomProperties.SetItems(lst);
                }
            }

            return le;
        }

        public enum SignatureSanityCheckState { OK, ValidButShouldBeInvalid, InvalidButShouldBeValid, LocalizedButShouldBeInvariant, NoneButHasData };

        public bool AdminSignatureSanityFix(bool fForceValid)
        {
            if (fForceValid)
            {
                CFISignatureState = SignatureState.Valid;
                FlightHash = ComputeFlightHash();
            }
            else
            {
                switch (AdminSignatureSanityCheckState)
                {
                    case SignatureSanityCheckState.OK:
                        return false;
                    case SignatureSanityCheckState.InvalidButShouldBeValid:
                        CFISignatureState = SignatureState.Valid;
                        break;
                    case SignatureSanityCheckState.ValidButShouldBeInvalid:
                        CFISignatureState = SignatureState.Invalid;
                        break;
                    case SignatureSanityCheckState.LocalizedButShouldBeInvariant:
                        CFISignatureState = SignatureState.Valid;
                        FlightHash = ComputeFlightHash();
                        break;
                }
            }

            DBHelper dbh = new DBHelper("UPDATE Flights SET signaturestate=?sigstate, FlightHash=?fhash WHERE idFlight=?idflight");
            dbh.DoNonQuery((comm) =>
            {
                int sigstate = (int)CFISignatureState;
                comm.Parameters.AddWithValue("sigstate", sigstate);
                comm.Parameters.AddWithValue("fhash", FlightHash);
                comm.Parameters.AddWithValue("idflight", FlightID);
            });

            return String.IsNullOrEmpty(dbh.LastError);
        }

        /// <summary>
        /// Repair some common errors that seem to arise with signatures
        /// </summary>
        /// <returns>True if errors found and fixed</returns>
        [Newtonsoft.Json.JsonIgnore]
        public SignatureSanityCheckState AdminSignatureSanityCheckState
        {
            get
            {
                SignatureSanityCheckState fResult = SignatureSanityCheckState.OK;

                // We see some cases where the flight is marked as invalid, but the current flight hash and the saved one match
                string szCurrentHash = DecryptedCurrentHash;
                string szFlightHash = DecryptedFlightHash;

                // We had a bug where the signature string was in a localized form (e.g., pi is 3,14 instead of 3.14), so these may be treated as invalid if subsequently saved from another locale
                // This bug should no longer appear because we are now always using invariant culture.
                // This assumes that the signature hash template above has not changed.  It shouldn't!
                // NOTE: It DOESN'T MATTER the current state - we could be valid or invalid.
                Regex r = new Regex("^(.*)(XC[0-9., ]+N[0-9., ]+SI[0-9., ]+IM[0-9., ]+GS[0-9., ]+DU[0-9., ]+CF[0-9., ]+SI[0-9., ]+PI[0-9., ]+TT[0-9., ]+)(.*)$", RegexOptions.Compiled);
                Match mCurrent = r.Match(szCurrentHash);
                Match mNew = r.Match(szFlightHash);
                if (mCurrent.Captures.Count == 1 && mNew.Captures.Count == 1 &&   // should always match
                    mCurrent.Groups.Count == 4 && mNew.Groups.Count == 4)
                {
                    if (mCurrent.Groups[1].Value.CompareOrdinal(mNew.Groups[1].Value) == 0 &&    // 1st & 3rd sections should always match
                        mCurrent.Groups[3].Value.CompareOrdinal(mNew.Groups[3].Value) == 0 &&
                        mCurrent.Groups[2].Value.CompareOrdinal(mNew.Groups[2].Value) != 0 &&    // middle part differs, but not if we substitute periods for commas/spaces.
                        mCurrent.Groups[2].Value.CompareOrdinal(mNew.Groups[2].Value.Replace(",", ".").Replace(" ", ".")) == 0)  // convert , or space as decimal into period before comparing.
                        return SignatureSanityCheckState.LocalizedButShouldBeInvariant;
                }

                if (CFISignatureState == SignatureState.Invalid && IsValidSignature())
                    return SignatureSanityCheckState.InvalidButShouldBeValid;

                if (CFISignatureState == SignatureState.Valid && !IsValidSignature())
                    return SignatureSanityCheckState.ValidButShouldBeInvalid;

                return fResult;
            }
        }

        /// <summary>
        /// Return the decrypted flight hash as persisted in the DB
        /// </summary>
        /// <returns>The decrypted flighthash</returns>
        [Newtonsoft.Json.JsonIgnore]
        public string DecryptedFlightHash
        {
            get
            {
                if (String.IsNullOrEmpty(User))
                    throw new System.InvalidOperationException("Can't decrypt the flight without a username specified");
                if (String.IsNullOrEmpty(FlightHash))
                    return string.Empty;
                return (new UserEncryptor(User)).Decrypt(FlightHash);
            }
        }

        /// <summary>
        /// Returns a decrypted hash of the current flight
        /// </summary>
        /// <returns>The currently computed flight hash</returns>
        [Newtonsoft.Json.JsonIgnore]
        public string DecryptedCurrentHash
        {
            get
            {
                if (String.IsNullOrEmpty(User))
                    throw new InvalidOperationException("Can't decrypt the flight without a username specified");
                if (String.IsNullOrEmpty(FlightHash))
                    return string.Empty;
                return (new UserEncryptor(User)).Decrypt(ComputeFlightHash());
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public bool CurrentValidSig
        {
            get { return IsValidSignature(); }
        }

        /// <summary>
        /// Computes an encrypted signature of the signature itself (as opposed to the flight it signs); this detects modification of the signature details (CFI details)
        /// </summary>
        /// <returns>The encrypted signature.</returns>
        public string ComputeSignatureHash()
        {
            string szSigHash = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}", CFIUsername ?? (CFIEmail ?? ""), CFICertificate, CFIExpiration.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), CFISignatureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            string szSig = (new UserEncryptor(User)).Encrypt(szSigHash);
            return szSig.Substring(0, Math.Min(255, szSig.Length));
        }

        /// <summary>
        /// Is the signature valid?  I.e., is the flight still semantically the same as it was in when it was signed?
        /// </summary>
        /// <returns>True if the flight is still semantically the same</returns>
        public bool IsValidSignature()
        {
            return String.Compare(ComputeFlightHash(), FlightHash, StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Has the signature itself been modified?
        /// </summary>
        /// <returns>True if it's OK</returns>
        public bool IsValidSigningDetails()
        {
            return (String.Compare(ComputeSignatureHash(), SignatureHash, StringComparison.Ordinal) == 0);
        }

        /// <summary>
        /// Determines and sets the status of the signature for the flight based on the fields that are present.
        /// </summary>
        /// <returns>The updated signature state (which is reflected in the object)</returns>
        public SignatureState UpdateSignatureState()
        {
            if (!String.IsNullOrEmpty(FlightHash) &&
                !String.IsNullOrEmpty(SignatureHash) &&
                !String.IsNullOrEmpty(CFICertificate) &&
                (!String.IsNullOrEmpty(CFIUsername) || !String.IsNullOrEmpty(CFIEmail)))
                CFISignatureState = (IsValidSignature() && IsValidSigningDetails()) ? SignatureState.Valid : SignatureState.Invalid;
            else
                CFISignatureState = SignatureState.None;
            return CFISignatureState;
        }

        /// <summary>
        /// Determines if a named user is authorized to view/sign a specific flight
        /// DOES NOT determine if the user can sign flights at all.
        /// </summary>
        /// <param name="szCFIUsername">The CFI's username</param>
        /// <param name="err">The resulting error message</param>
        /// <returns>True if the pilot is authorized; else, false.  szError contains the message</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public bool CanSignThisFlight(string szCFIUsername, out string err)
        {
            err = String.Empty;

            // Return false if we're not an instructor of this student
            CFIStudentMap sm = new CFIStudentMap(szCFIUsername);
            if (!sm.IsInstructorOf(this.User))
            {
                err = Resources.SignOff.errSignNotInstructorOfStudent;
                return false;
            }

            // If the student has given us permission to view the logbook, we can sign it.
            foreach (InstructorStudent stud in sm.Students)
                if (String.Compare(stud.UserName, this.User, StringComparison.OrdinalIgnoreCase) == 0 && stud.CanViewLogbook)
                    return true; // we have permission to view the logbook.

            // Otherwise, look for evidence that the user has requested us to sign it - look in the email or username field for an outstanding request.
            Profile pfCFI = Profile.GetUser(szCFIUsername);
            if (String.Compare(pfCFI.UserName, this.CFIUsername, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(pfCFI.Email, this.CFIEmail, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            // Otherwise, we're simply not authorized
            err = Resources.SignOff.errSignNotAuthorized;
            return false;
        }

        /// <summary>
        /// Determines if the named CFI can edit this flight (i.e., if it is pending a signature, or if he has already signed it)
        /// </summary>
        /// <param name="szCFIUsername">The username of the CFI</param>
        /// <returns>True if they can edit it</returns>
        public bool CanEditThisFlight(string szCFIUsername)
        {
            return (this.CFISignatureState != SignatureState.Valid && this.CFIUsername != null && !String.IsNullOrEmpty(szCFIUsername) && String.Compare(this.CFIUsername, szCFIUsername, StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Sign a flight with an ad-hoc signature (i.e., no relationship to instructor)
        /// </summary>
        /// <param name="szCFIEmail">Email address of the signing CFI</param>
        /// <param name="szCFICertificate">CFI Certificate of signing CFI</param>
        /// <param name="dtCertificateExp">Expiration date of the certificate</param>
        /// <param name="szCFIComments">Comments</param>
        /// <param name="szCFIName">CFI's Name</param>
        /// <param name="fAllowEmptyCertificateExpiration">True to suppress the certificate expiration</param>
        /// <param name="rgPngDigitization">byte array of the PNG of the CFI's signature.</param>
        public void SignFlightAdHoc(string szCFIName, string szCFIEmail, string szCFICertificate, DateTime dtCertificateExp, string szCFIComments, byte[] rgPngDigitization, bool fAllowEmptyCertificateExpiration)
        {
            if (String.IsNullOrEmpty(szCFIName) || String.IsNullOrEmpty(szCFIEmail))
                throw new MyFlightbookException(Resources.SignOff.errNoInstructor);
            if (String.IsNullOrEmpty(szCFICertificate))
                throw new MyFlightbookException(Resources.SignOff.errNeedCertificate);
            // Two possible errors around certificate expiration:
            // (a) There is no value provided and we aren't allowing empty certificate expiration OR
            // (b) there IS a value and it has expired.
            if ((!dtCertificateExp.HasValue() && !fAllowEmptyCertificateExpiration) || (dtCertificateExp.HasValue() && dtCertificateExp.AddDays(1).CompareTo(DateTime.Now) < 0))
                throw new MyFlightbookException(Resources.SignOff.errSignExpiredCertificate);
            if (rgPngDigitization == null || rgPngDigitization.Length < 100)
                throw new MyFlightbookException(Resources.SignOff.errSignNoDigitizedSignature);

            DigitizedSignature = rgPngDigitization;
            CFIUsername = String.Empty;
            CFICertificate = szCFICertificate;
            CFIExpiration = dtCertificateExp;
            CFIComments = szCFIComments;
            CFIEmail = szCFIEmail;
            CFIName = szCFIName;
            CFISignatureDate = DateTime.Now.ToUniversalTime();
            CFISignatureState = SignatureState.Valid;
            FlightHash = ComputeFlightHash();
            SignatureHash = ComputeSignatureHash();
            FCommit(false, true);
        }

        /// <summary>
        /// Signs a flight, updating the relevant fields.  Throws exceptions if the user is not eligible to sign.
        /// </summary>
        /// <param name="szCFIUsername">Name of the signing user</param>
        /// <param name="szComment">Any additional comment from the CFI</param>
        public void SignFlight(string szCFIUsername, string szComment)
        {
            Profile pfCFI = Profile.GetUser(szCFIUsername);

            // Validate that the named user can sign this flight.
            string szErr;
            if (!CanSignThisFlight(szCFIUsername, out szErr))
                throw new MyFlightbookException(szErr);

            if (!pfCFI.CanSignFlights(out szErr))
                throw new MyFlightbookException(szErr);

            DigitizedSignature = null;
            CFIUsername = pfCFI.UserName;
            CFICertificate = pfCFI.Certificate;
            CFIExpiration = pfCFI.CertificateExpiration;
            CFIComments = szComment;
            CFIEmail = pfCFI.Email;
            CFIName = pfCFI.UserFullName;
            CFISignatureDate = DateTime.Now.ToUniversalTime();
            CFISignatureState = SignatureState.Valid;
            FlightHash = ComputeFlightHash();
            SignatureHash = ComputeSignatureHash();
            if (!FCommit(false, true))
                throw new MyFlightbookValidationException(ErrorString);
        }

        #region PendingSignatures
        /// <summary>
        /// Gets a list of all unsigned flights waiting for signatures by the CFI from the named student
        /// </summary>
        /// <param name="pfCFI">The CFI object. Pass null for ALL pending signatures fo the student.</param>
        /// <param name="szStudentUserName">The student's username</param>
        /// <returns>A list of SPARSELY FILLED LogbookEntries.  Date, ID, Comments, and Route are all that is filled in.</returns>
        public static List<LogbookEntry> PendingSignaturesForStudent(Profile pfCFI, Profile pfStudent)
        {
            const string szAnyPending = "((f.CFIUsername IS NOT NULL AND f.CFIUserName <> '') OR (f.CFIEmail IS NOT NULL AND f.CFIEmail <> ''))";
            const string szJustForCFI = "(f.CFIUsername=?user OR f.CFIEmail=?email)";
            DBHelper dbh = new DBHelper(String.Format("SELECT f.Date, f.idFlight, f.Comments, f.Route FROM Flights f WHERE f.username=?student AND {0} AND f.SignatureState=0 ORDER BY f.Date DESC", pfCFI == null ? szAnyPending : szJustForCFI));
            List<LogbookEntry> lstFlightsToSign = new List<LogbookEntry>();
            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("student", pfStudent.UserName);

                    if (pfCFI != null)
                    {
                        comm.Parameters.AddWithValue("email", pfCFI.Email);
                        comm.Parameters.AddWithValue("user", pfCFI.UserName);
                    }
                },
                (dr) =>
                {
                    LogbookEntry le = new LogbookEntry()
                    {
                        FlightID = Convert.ToInt32(dr["idFlight"]),
                        Date = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture),
                        Comment = dr["Comments"].ToString(),
                        Route = dr["Route"].ToString(),
                        User = pfStudent.UserName
                    };
                    lstFlightsToSign.Add(le);
                });
            return lstFlightsToSign;
        }

        /// <summary>
        /// Ignores a pending signature request.  ONLY AFFECTS CFIEMail, CFIName field.
        /// </summary>
        public void ClearPendingSignature()
        {
            if (this.CFISignatureState == SignatureState.None && !IsNewFlight)
                RequestSignature(null, null);
        }

        /// <summary>
        /// Requests a signature.  Can also be used to clear it by passing NULL for both fields.
        /// </summary>
        /// <param name="szCFIUserName">The username of the CFI</param>
        /// <param name="szCFIEmail">The email for the CFI</param>
        public void RequestSignature(string szCFIUserName, string szCFIEmail)
        {
            if ((this.CFISignatureState == SignatureState.None || this.CFISignatureState == SignatureState.Invalid) && !IsNewFlight)
            {
                DBHelper dbh = new DBHelper("UPDATE Flights f SET f.CFIEmail=?email, f.CFIUserName=?name, f.flighthash=NULL, f.SignatureHash=NULL, f.CFIComment=NULL, f.SignatureDate=NULL, f.CFICertificate=NULL, f.CFIExpiration=NULL, f.CFIName=NULL, f.DigitizedSignature=NULL, f.SignatureState=0 WHERE idFlight=?idFlight");
                dbh.CommandArgs.AddWithValue("email", szCFIEmail);
                dbh.CommandArgs.AddWithValue("name", szCFIUserName);
                dbh.CommandArgs.AddWithValue("idFlight", FlightID);
                dbh.DoNonQuery();
            }
        }

        /// <summary>
        /// Sets up to enable a CFI to sign a flight - in memory only
        /// </summary>
        /// <param name="szCFIUserName"></param>
        public void SetPendingSignature(string szCFIUserName)
        {
            if (this.CFISignatureState == SignatureState.None && !IsNewFlight)
            {
                this.CFIUsername = szCFIUserName;
            }
        }
        #endregion
        #endregion

        public Boolean IsNewFlight
        {
            get { return IsNewFlightID(FlightID); }
        }

        public static Boolean IsNewFlightID(int idFlight)
        {
            return idFlight == LogbookEntry.idFlightNew || idFlight == LogbookEntry.idFlightNone || idFlight < 0;
        }

        #region Comparison
        private bool HasEqualDates(LogbookEntry le)
        {
            return Date.Year == le.Date.Year &&
                Date.Month == le.Date.Month &&
                Date.Day == le.Date.Day &&
                DateTime.Compare(EngineEnd, le.EngineEnd) == 0 &&
                DateTime.Compare(EngineStart, le.EngineStart) == 0 &&
                DateTime.Compare(FlightEnd, le.FlightEnd) == 0 &&
                DateTime.Compare(FlightStart, le.FlightStart) == 0;
        }

        private bool HasEqualStrings(LogbookEntry le)
        {
            return String.Compare(Comment, le.Comment, StringComparison.CurrentCultureIgnoreCase) == 0 && 
                   String.Compare(this.Route, le.Route, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                   String.Compare(this.User, le.User, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private bool HasEqualFields(LogbookEntry le)
        {
            return Approaches == le.Approaches &&
                CatClassOverride == le.CatClassOverride &&
                CFI.EqualsToPrecision(le.CFI) &&
                CrossCountry.EqualsToPrecision(le.CrossCountry) &&
                Dual.EqualsToPrecision(le.Dual) &&
                fHoldingProcedures == le.fHoldingProcedures &&
                FullStopLandings == le.FullStopLandings &&
                GroundSim.EqualsToPrecision(le.GroundSim) &&
                HobbsEnd.EqualsToPrecision(le.HobbsEnd) &&
                HobbsStart.EqualsToPrecision(le.HobbsStart) &&
                IMC.EqualsToPrecision(le.IMC) &&
                Landings == le.Landings &&
                NightLandings == le.NightLandings &&
                Nighttime.EqualsToPrecision(le.Nighttime) &&
                PIC.EqualsToPrecision(le.PIC) &&
                SIC.EqualsToPrecision(le.SIC) &&
                SimulatedIFR.EqualsToPrecision(le.SimulatedIFR) &&
                TotalFlightTime.EqualsToPrecision(le.TotalFlightTime);
        }

        /// <summary>
        /// Determines if this is semantically the same as another LogbookEntry object.
        /// </summary>
        /// <param name="le">The LogbookEntry to compare</param>
        /// <returns>true if they are semantically the same</returns>
        public bool IsEqualTo(LogbookEntry le)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(this, le))
                return true;

            // if both are null, they are equal
            if (this == null && le == null)
                return true;

            // if one is null and the other is not, they are not equal
            if ((this == null) ^ (le == null))
                return false;

            if (le == null)
                throw new ArgumentNullException("le");  // shouldn't ever happen at this point!  But we suppress a warning...

            // OK, now we're to the case where both are non-null but different references.
            // See if the main properties are equal.
            if (AircraftID != le.AircraftID ||
                !HasEqualFields(le) || 
                !HasEqualStrings(le) || 
                !HasEqualDates(le))
                return false;

            if (this.CustomProperties.Count != le.CustomProperties.Count)
                return false;

            foreach (CustomFlightProperty cfp1 in this.CustomProperties)
            {
                // find the matching property in le
                CustomFlightProperty cfp2 = null;
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                    if (cfp.PropTypeID == cfp1.PropTypeID)
                    {
                        cfp2 = cfp;
                        break;
                    }

                if (cfp2 == null || cfp2.ValueString.CompareCurrentCultureIgnoreCase(cfp1.ValueString) != 0)
                    return false;
            }

            // Ignore images, etc.

            return true;
        }
        #endregion

        /// <summary>
        /// Clones the current flight as a new flight
        /// </summary>
        /// <param name="le">The target logbookentry object.  A new one is allocated if this is null</param>
        /// <returns>A new LogbookEntry object that is the same as this one but which will not overwrite the old one.</returns>
        public LogbookEntry Clone(LogbookEntry le = null)
        {
            if (le == null)
                le = new LogbookEntry();

            util.CopyObject(this, le);
            le.FlightID = LogbookEntry.idFlightNew;

            // CustomProperties need to be copied separately; the copyobject above copies by reference below the top object
            List<CustomFlightProperty> lstCFP = new List<CustomFlightProperty>();
            foreach (CustomFlightProperty cfp in this.CustomProperties)
            {
                CustomFlightProperty cfpNew = new CustomFlightProperty(cfp.PropertyType);
                util.CopyObject(cfp, cfpNew);
                cfpNew.FlightID = LogbookEntry.idFlightNew;
                cfpNew.PropID = CustomFlightProperty.idCustomFlightPropertyNew;
                lstCFP.Add(cfpNew);
            }
            le.CustomProperties.SetItems(lstCFP);
            return le;
        }

        /// <summary>
        /// Returns a command object with the query string for the user and any optional restriction
        /// INCLUDES ALL RELEVANT PARAMETERS
        /// </summary>
        /// <param name="fq">FlightQuery object (will be refreshed)</param>
        /// <param name="limit">Maximum results to return, 0 or less for no limit</param>
        /// <param name="offset">Offset to first result</param>
        /// <param name="fAsc">True to sort in ascending order (vs. descending)</param>
        /// <param name="fIncludeTelemetry">True to include telemetry</param>
        /// <returns>Command object.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static DBHelperCommandArgs QueryCommand(FlightQuery fq, int offset = -1, int limit = -1, bool fAsc = false, LoadTelemetryOption lto = LoadTelemetryOption.None)
        {
            if (fq == null)
                throw new ArgumentNullException("fq");
            DBHelperCommandArgs comm = new DBHelperCommandArgs();

            // Add the current locale
            comm.AddWithValue("localecode", System.Globalization.CultureInfo.CurrentCulture.Name.Replace("-", "_"));
            comm.AddWithValue("shortDate", DBHelper.CSharpDateFormatToMySQLDateFormat());

            // And any query parameters
            fq.Refresh();
            foreach (MySqlParameter p in fq.QueryParameters())
                comm.Parameters.Add(p);

            string szTemplate = ConfigurationManager.AppSettings["LogbookForUserQuery"].ToString();

            StringBuilder sbAdditionalColumns = new StringBuilder(fq.SearchColumns);
            if (lto != LoadTelemetryOption.None)
                sbAdditionalColumns.Append("CAST(UNCOMPRESS(flights.Telemetry) AS CHAR) AS FlightData, ");

            StringBuilder sbAdditionalJoins = new StringBuilder();
            if (fq.NeedsUserAircraft)
                sbAdditionalJoins.Append(" INNER JOIN useraircraft ON (flights.username=useraircraft.username AND flights.idaircraft = useraircraft.idaircraft) ");

            comm.QueryString = String.Format(CultureInfo.InvariantCulture, szTemplate,
                sbAdditionalColumns.ToString(),                                                             // FlightData column and/or extra search columns
                sbAdditionalJoins.ToString(),                                                              // Join on user aircraft or images if needed
                fq.RestrictClause,                                                                          // WHERE clause
                fq.HavingClause.Length > 0 ? String.Format(CultureInfo.InvariantCulture, " HAVING {0} ", fq.HavingClause) : string.Empty, // HAVING clause
                fAsc ? "ASC" : "DESC",                                                                      // Sort direction
                limit > 0 || offset > 0 ? String.Format(CultureInfo.InvariantCulture, " LIMIT {0},{1}", offset, limit) : string.Empty);   // LIMIT clause

            return comm;
        }

        #region Validation
        private bool ValidateMainFields()
        {
            if (CrossCountry < 0 || Nighttime < 0 || IMC < 0 || SimulatedIFR < 0 || GroundSim < 0 || Dual < 0 || PIC < 0 || TotalFlightTime < 0 || CFI < 0 || SIC < 0)
            {
                m_lastErr = ErrorCode.NegativeTime;
                szError = Resources.LogbookEntry.errInvalidTime;
                return false;
            }

            if (Landings < 0 || NightLandings < 0 || FullStopLandings < 0 || Approaches < 0)
            {
                m_lastErr = ErrorCode.NegativeCount;
                szError = Resources.LogbookEntry.errCantBeNegativeCount;
                return false;
            }

            if (HobbsEnd < 0 || HobbsStart < 0)
            {
                m_lastErr = ErrorCode.InvalidHobbs;
                szError = Resources.LogbookEntry.errHobbsTimesNegative;
                return false;
            }

            if (HobbsStart > 0 && HobbsEnd > 0 && (HobbsEnd < HobbsStart))
            {
                m_lastErr = ErrorCode.InvalidHobbs;
                szError = Resources.LogbookEntry.errInvalidHobbs;
                return false;
            }

            // If FS landings are specified, but no general landings, then set the general landings
            if (Landings == 0 && (FullStopLandings + NightLandings) > 0)
                Landings = FullStopLandings + NightLandings;

            if (FullStopLandings + NightLandings > Landings)
            {
                m_lastErr = ErrorCode.InvalidLandings;
                szError = Resources.LogbookEntry.errTooManyFSLandings;
                return false;
            }

            if (Comment.Length > 13000)
            {
                m_lastErr = ErrorCode.DataTooLong;
                szError = Resources.LogbookEntry.errCommentsTooLong;
                return false;
            }

            if (Route.Length > 128)
            {
                m_lastErr = ErrorCode.DataTooLong;
                szError = Resources.LogbookEntry.errRouteTooLong;
                return false;
            }

            return true;
        }

        private bool ValidateDateTimeFields()
        {
            if (DateTime.Compare(dtEngineStart, dtEngineEnd) > 0 && dtEngineStart.HasValue() && dtEngineEnd.HasValue())
            {
                m_lastErr = ErrorCode.InvalidEngine;
                szError = Resources.LogbookEntry.errInvalidEngineTimes;
                return false;
            }

            if (DateTime.Compare(dtFlightStart, dtFlightEnd) > 0 && dtFlightStart.HasValue() && dtFlightEnd.HasValue())
            {
                m_lastErr = ErrorCode.InvalidFlight;
                szError = Resources.LogbookEntry.errInvalidFlightTimes;
                return false;
            }

            if (Date.Year < 1900)
            {
                m_lastErr = ErrorCode.InvalidDate;
                szError = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errInvalidFlightDateEarly, Date.ToShortDateString());
                return false;
            }

            if (DateTime.Compare(Date, DateTime.Now.AddDays(2)) > 0)
            {
                m_lastErr = ErrorCode.InvalidDate;
                szError = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errInvalidFlightDateFuture, Date.ToShortDateString());
                return false;
            }

            return true;
        }

        private bool ValidateApproaches()
        {
            // sum up total approaches in properties and night takeoffs
            int cApproachProperties = 0;
            int cNightTakeoffs = 0, cDescribedNightTakeoffs = 0;
            CustomFlightProperty cfpNightTakeoffs = null;
            foreach (CustomFlightProperty cfp in CustomProperties)
            {
                if (cfp.PropertyType.IsApproach)
                    cApproachProperties += cfp.IntValue;
                else if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTakeoff)
                {
                    cfpNightTakeoffs = cfp;
                    cNightTakeoffs = cfp.IntValue;
                }
                else if (cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffToweredNight || cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTakeoffUntoweredNight)
                    cDescribedNightTakeoffs += cfp.IntValue;
            }

            // No approach count specified - just set it as a convenience (as we did above for landings); otherwise, be conservative and report an error.
            if (Approaches == 0 && cApproachProperties > 0)
                Approaches = cApproachProperties;

            if (cApproachProperties > Approaches)
            {
                m_lastErr = ErrorCode.InvalidApproaches;
                szError = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errApproachPropertiesExceedApproachCount, Approaches, cApproachProperties);
                return false;
            }

            // do the same for night takeoffs
            if (cfpNightTakeoffs == null && cNightTakeoffs == 0 && cDescribedNightTakeoffs > 0)
            {
                cNightTakeoffs = cDescribedNightTakeoffs;
                CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNightTakeoff, cNightTakeoffs));
            }

            if (cNightTakeoffs < cDescribedNightTakeoffs)
            {
                m_lastErr = ErrorCode.InvalidNightTakeoffs;
                szError = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errNightTakeoffPropertiesExceedNightTakeoffCount, cNightTakeoffs, cDescribedNightTakeoffs);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Performs auto-fill based on the pilot role specified in the aircraft.
        /// </summary>
        /// <param name="ac"></param>
        public void AutofillForAircraft(Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException("ac");

            if (IsNewFlight)
            {
                switch (ac.RoleForPilot)
                {
                    default:
                    case Aircraft.PilotRole.None:
                        break;
                    case Aircraft.PilotRole.CFI:
                        if (CFI == 0)
                            CFI = TotalFlightTime;
                        break;
                    case Aircraft.PilotRole.PIC:
                        if (PIC == 0)
                            PIC = TotalFlightTime;
                        break;
                    case Aircraft.PilotRole.SIC:
                        if (SIC == 0)
                            SIC = TotalFlightTime;
                        break;
                }

                if (ac.CopyPICNameWithCrossfill && !CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropNameOfPIC))
                    CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropNameOfPIC, Profile.GetUser(User).UserFullName));
            }
        }

        /// <summary>
        /// Validates the aircraft for the flight.  THIS PERFORMS AUTOFILL based on aircraft preferences, so it can modify the flight.
        /// </summary>
        /// <returns></returns>
        private bool ValidateAircraft()
        {
            if (idAircraft < 0)
            {
                m_lastErr = ErrorCode.InvalidAircraft;
                szError = Resources.LogbookEntry.errInvalidAircraft;
                return false;
            }

            // Check that the aircraft is actually known for the user and, if flags are set, auto-set CFI/PIC/SIC
            UserAircraft ua = new UserAircraft(User);
            Aircraft ac = ua.GetUserAircraftByID(this.AircraftID);
            if (ac == null)
            {
                m_lastErr = ErrorCode.InvalidAircraft;
                szError = Resources.LogbookEntry.errInvalidAircraft;
                return false;
            }

            if (NightLandings > 0 && Nighttime == 0.0M && ac.InstanceType == AircraftInstanceTypes.RealAircraft)
            {
                m_lastErr = ErrorCode.MissingNight;
                szError = Resources.LogbookEntry.errNoNightFlight;
                return false;
            }

            AutofillForAircraft(ac);

            return true;
        }

        /// <summary>
        /// Verify that this logbook row is valid.  Calls ValidateAircraft, which can modify the flight (by performing autofill)
        /// </summary>
        /// <returns>True if it is OK, false if not</returns>
        public Boolean IsValid()
        {
            if (String.IsNullOrEmpty(User))
            {
                m_lastErr = ErrorCode.InvalidUser;
                szError = Resources.LogbookEntry.errInvalidUser;
                return false;
            }

            return ValidateMainFields() &&
                ValidateDateTimeFields() &&
                ValidateApproaches() &&
                ValidateAircraft();
        }
        #endregion

        #region Object Creation
        protected LogbookEntryBase()
        {
            InitObject();
        }
        #endregion

        #region Commit/Delete
        public static Boolean FDeleteEntry(int id, string szUser)
        {
            DBHelper dbh = new DBHelper("DELETE FROM flights WHERE idFlight=?idFlight AND username=?UserName");
            dbh.DoNonQuery(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("idFlight", id);
                    comm.Parameters.AddWithValue("UserName", szUser);
                });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(String.Format("Error attempting to delete flight: {0} parameters - (flightID = {1} user = {2}): {3}", dbh.CommandText, id, szUser, dbh.LastError));

            Profile.GetUser(szUser).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);

            // Now delete any associated images.
            try
            {
                ImageList il = new ImageList(MFBImageInfo.ImageClass.Flight, id.ToString());
                il.Refresh(true);
                foreach (MFBImageInfo mfbii in il.ImageArray)
                    mfbii.DeleteImage();

                DirectoryInfo di = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(il.VirtPath));
                di.Delete(true);
            }
            catch
            { }

            // And any associated telemetry
            TelemetryReference.DeleteFile(id);

            // Flights have changed, so aircraft stats are invalid.
            new UserAircraft(szUser).FlushStatsForUser();

            return true;
        }

        /// <summary>
        /// Bulk migrates flights for a user from one aircraft to another.
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="idAircraftOld">The ID of the aircraft being replaced</param>
        /// <param name="idAircraftNew">The ID of the new aircraft</param>
        public static void UpdateFlightAircraftForUser(string szUser, int idAircraftOld, int idAircraftNew)
        {
            // This method can render valid signatures invalid (due to aircraft change), so 
            // we need to find such flights and - IF THEY ARE VALID - recompute the flight hash so 
            // that they stay valid.
            FlightQuery q = new FlightQuery(szUser) { IsSigned = true, AircraftIDList = new int[] { idAircraftOld } };
            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(q));
            dbh.ReadRows((comm) => { }, dr =>
            {
                LogbookEntry le = new LogbookEntry(dr, szUser);
                if (le.IsValidSignature())
                {
                    le.AircraftID = idAircraftNew;
                    le.FlightHash = le.ComputeFlightHash();
                    le.FCommit(false, true);
                }
            });

            dbh = new DBHelper("UPDATE Flights f SET f.idAircraft=?idNew WHERE f.username=?user AND f.idAircraft=?idOld");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("idNew", idAircraftNew);
                comm.Parameters.AddWithValue("idOld", idAircraftOld);
                comm.Parameters.AddWithValue("user", szUser);
            });
        }

        public const int MaxTelemetrySize = 300000;

        /// <summary>
        /// Commits this entry to the database.  This overwrites the existing entry if it was initially loaded from the DB, otherwise it is a new entry
        /// </summary>
        /// <param name="fUpdateFlightData">True if telemetry is to be updated</param>
        /// <param name="fUpdateSignature">True if the signature is to be recomputed and saved</param>
        /// <returns>True if success, else false</returns>
        public virtual bool FCommit(bool fUpdateFlightData = false, bool fUpdateSignature = false)
        {
            Boolean fResult = false;
            szError = "";

            if (!IsValid())
                return fResult;

            DBHelper dbh = new DBHelper();

            const string szSetSig = @", FlightHash=?newflightHash, SignatureHash=?sighash, CFIComment=?cficomment, SignatureDate=?sigDate,  
                CFICertificate=?cficert, CFIExpiration=?cfiExpiration, CFIUserName=?cfiUsername, 
                CFIEmail=?cfiEmail, CFIName=?cfiName, DigitizedSignature=?digitizedsig, SignatureState=?sigState";
            const string szSetTelemetry = ", Telemetry=COMPRESS(?flightdata) ";

            // How to refresh the signature state.  If setting the signature, blast it in (above)
            // Otherwise, check to see if the current hash is different than the existing one.
            const string szRefreshSig = ", SignatureState=IF((flightHash='' OR flightHash IS NULL), ?sigstateNone, IF(flightHash=?newflighthash, ?sigstateValid, ?sigstateInvalid)) ";

            const string szSetTemplate = @"SET date = ?Date, idAircraft = ?idAircraft, idCatClassOverride=?idCatClassOverride, cInstrumentApproaches=?cInstrumentApproaches, cLandings=?cLandings, 
                cFullStopLandings=?cFullStopLandings, cNightLandings=?cNightLandings, crosscountry=?crosscountry, night=?night, IMC=?IMC, 
                simulatedInstrument=?simulatedInstrument, groundSim=?groundSim, dualReceived=?dualReceived, PIC=?PIC, totalFlightTime=?totalFlightTime, 
                fHold=?fHold, Route=?Route, Comments=?Comments, username=?userName, fPublic=?fPublic, hobbsStart=?hobbsStart, hobbsEnd=?hobbsEnd, 
                dtFlightStart=?dtFlightStart, dtFlightEnd=?dtFlightEnd, dtEngineStart=?dtEngineStart, dtEngineEnd=?dtEngineEnd, cfi=?cfi, SIC=?SIC
                {0} {1} ";

            string szSet = String.Format(szSetTemplate, (fUpdateSignature ? szSetSig : szRefreshSig), (fUpdateFlightData ? szSetTelemetry : ""));

            dbh.DoNonQuery(
                (comm) =>
                {
                    if (this.IsNewFlight)
                    {
                        comm.CommandText = String.Format("INSERT INTO flights {0}", szSet);
                    }
                    else
                    {
                        comm.CommandText = String.Format("UPDATE flights {0} WHERE idFlight = ?idFlight AND username = ?UserName", szSet);
                        comm.Parameters.AddWithValue("idFlight", FlightID);
                    }

                    comm.Parameters.AddWithValue("Date", this.Date);
                    comm.Parameters.AddWithValue("idAircraft", this.AircraftID);
                    comm.Parameters.AddWithValue("idCatClassOverride", this.CatClassOverride);
                    comm.Parameters.AddWithValue("cInstrumentApproaches", this.Approaches);
                    comm.Parameters.AddWithValue("cLandings", this.Landings);
                    comm.Parameters.AddWithValue("cFullStopLandings", this.FullStopLandings);
                    comm.Parameters.AddWithValue("cNightLandings", this.NightLandings);
                    comm.Parameters.AddWithValue("crosscountry", this.CrossCountry);
                    comm.Parameters.AddWithValue("night", this.Nighttime);
                    comm.Parameters.AddWithValue("IMC", this.IMC);
                    comm.Parameters.AddWithValue("simulatedInstrument", this.SimulatedIFR);
                    comm.Parameters.AddWithValue("groundSim", this.GroundSim);
                    comm.Parameters.AddWithValue("dualReceived", this.Dual);
                    comm.Parameters.AddWithValue("PIC", this.PIC);
                    comm.Parameters.AddWithValue("totalFlightTime", this.TotalFlightTime);
                    comm.Parameters.AddWithValue("fHold", this.fHoldingProcedures);
                    comm.Parameters.AddWithValue("Route", this.Route);
                    comm.Parameters.AddWithValue("Comments", this.Comment);
                    comm.Parameters.AddWithValue("userName", this.User);
                    comm.Parameters.AddWithValue("fPublic", this.fIsPublic);
                    comm.Parameters.AddWithValue("hobbsStart", this.HobbsStart);
                    comm.Parameters.AddWithValue("hobbsEnd", this.HobbsEnd);
                    comm.Parameters.AddWithValue("dtFlightStart", this.FlightStart.HasValue() ? (object) this.FlightStart : DBNull.Value);
                    comm.Parameters.AddWithValue("dtFlightEnd", this.FlightEnd.HasValue() ? (object) this.FlightEnd : DBNull.Value);
                    comm.Parameters.AddWithValue("dtEngineStart", this.EngineStart.HasValue() ? (object) this.EngineStart : DBNull.Value);
                    comm.Parameters.AddWithValue("dtEngineEnd", this.EngineEnd.HasValue() ? (object) this.EngineEnd : DBNull.Value);
                    comm.Parameters.AddWithValue("cfi", this.CFI);
                    comm.Parameters.AddWithValue("SIC", this.SIC);

                    // IF we are updating the signature (i.e., re-signing), then we call UpdatesignatureState to initialize and set the sigstate.
                    // If we are NOT updating the signature, we simply pass in the new flight hash and let the DB see if the state needs to be 
                    // changed.
                    // I.e., if we're signing: blast in the new signature state and hash.  WE TELL the DB what the state and hash are.
                    // if we're NOT signing, we pass in a new hash but only for comparison: the DB computes the new state and does NOT update the hash.
                    // Note that the signature hash, by comparison, is only ever updated when we are updating the signature itself (i.e., at signing), but we read it always.
                    comm.Parameters.AddWithValue("newflighthash", this.ComputeFlightHash());
                    if (fUpdateSignature)
                    {
                        comm.Parameters.AddWithValue("sigState", UpdateSignatureState());
                        comm.Parameters.AddWithValue("cficomment", this.CFIComments == null ? null : this.CFIComments.LimitTo(255));
                        comm.Parameters.AddWithValue("sigDate", this.CFISignatureDate);
                        comm.Parameters.AddWithValue("cficert", this.CFICertificate);
                        comm.Parameters.AddWithValue("cfiExpiration", this.CFIExpiration);
                        comm.Parameters.AddWithValue("cfiUserName", this.CFIUsername);
                        comm.Parameters.AddWithValue("cfiEmail", this.CFIEmail);
                        comm.Parameters.AddWithValue("cfiName", this.CFIName);
                        comm.Parameters.AddWithValue("sighash", ComputeSignatureHash());
                        comm.Parameters.AddWithValue("digitizedSig", DigitizedSignature);
                    }
                    else
                    {
                        comm.Parameters.AddWithValue("sigstateNone", SignatureState.None);
                        comm.Parameters.AddWithValue("sigstateValid", SignatureState.Valid);
                        comm.Parameters.AddWithValue("sigstateInvalid", SignatureState.Invalid);
                    }

                    // Hack: we have a bug in the android version (that I should fix) where the flight data consists entirely of the header row.  Kill the telemetry in this case
                    if (!String.IsNullOrEmpty(this.FlightData) && this.FlightData.StartsWith("LAT,") && this.FlightData.Length < 60)
                        this.FlightData = string.Empty;

                    if (fUpdateFlightData)
                        comm.Parameters.AddWithValue("FlightData", String.IsNullOrEmpty(this.FlightData) ? null : this.FlightData);
                });
            if (dbh.LastError.Length > 0)
            {
                szError += "Exception: " + dbh.LastError;
                return false;
            }

            FlightID = (FlightID >= 0) ? FlightID : dbh.LastInsertedRowId; // set the flight ID to the previous ID or else the newly inserted one

            // save the custom properties.
            foreach (CustomFlightProperty cfp in CustomProperties)
            {
                cfp.FlightID = FlightID;
                cfp.FCommit();

                // If you add a property that you haven't used before, flush the local cache of properties.
                if (!cfp.PropertyType.IsFavorite)
                    CustomPropertyType.FlushUserCache(this.User);
            }

            // Update the telemetry record, if any
            if (fUpdateFlightData)
            {
                Telemetry.FlightID = FlightID;  // just in case it had been a new flight.
                Telemetry.Delete(); // we saved the data above, so simply delete it now; we can recreate it later.  This will be a no-op if there is no current data.  Keep things as fast as possible.

                if (!String.IsNullOrEmpty(this.FlightData))
                    new System.Threading.Thread(() => 
                    { 
                        System.Threading.Thread.Sleep(1000);
                        try
                        {
                            this.MoveTelemetryFromFlightEntry();
                        }
                        catch (Exception ex)
                        {
                            util.NotifyAdminException(String.Format(CultureInfo.CurrentCulture, "Exception moving telemetry for flight {0}", FlightID), ex);
                        }
                    }).Start();
            }

            // Save any videos
            foreach (VideoRef vid in Videos)
            {
                vid.FlightID = FlightID; 
                vid.Commit();
            }

            FlightStats.FlightStats.RefreshForFlight(this);

            // Flights have changed, so aircraft stats are invalid.
            new UserAircraft(User).FlushStatsForUser();

            return (szError.Length == 0);
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the LogbookEntry object
        /// </summary>
        private void InitObject()
        {
            FlightID = LogbookEntry.idFlightNew; // Assume this is a new entry
            Date = DateTime.Now;    //today
            User = String.Empty;
            CrossCountry = Nighttime = IMC = SimulatedIFR = GroundSim = Dual = PIC = TotalFlightTime = CFI = SIC = 0.0M;
            idAircraft = Aircraft.idAircraftUnknown;
            Approaches = Landings = NightLandings = FullStopLandings = 0;
            fHoldingProcedures = fIsPublic = false;
            Comment = Route = szError = String.Empty;
            m_lastErr = ErrorCode.None;
            dtEngineStart = dtEngineEnd = dtFlightStart = dtFlightEnd = DateTime.MinValue;
            HobbsStart = HobbsEnd = 0.0M;
            fHasDataStream = false;
            CatClassOverride = 0;
            CatClassDisplay = ModelDisplay = TailNumDisplay = String.Empty;
            CFISignatureState = SignatureState.None;
            CustomProperties = new CustomPropertyCollection();
            Videos = new VideoRef[0];
            Telemetry = new TelemetryReference();
        }

        /// <summary>
        /// Given a row in a result set, initializes a logbook entry from the row.  You have to provide the expected username for this to work (weak security)
        /// </summary>
        /// <param name="dr">The data row containing the logbook entry</param>
        /// <param name="szUser">The expected username</param>
        /// <returns>True if successful</returns>
        protected Boolean InitFromDataReader(MySqlDataReader dr, string szUser, LoadTelemetryOption lto)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            try
            {
                if (string.Compare(dr["username"].ToString().Trim(), szUser, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    FlightID = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
                    User = dr["username"].ToString();
                    Date = Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture);
                    idAircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
                    CatClassOverride = Convert.ToInt32(dr["idCatClassOverride"], CultureInfo.InvariantCulture);
                    Approaches = Convert.ToInt32(dr["cInstrumentApproaches"], CultureInfo.InvariantCulture);
                    Landings = Convert.ToInt32(dr["cLandings"], CultureInfo.InvariantCulture);
                    FullStopLandings = Convert.ToInt32(dr["cFullStopLandings"], CultureInfo.InvariantCulture);
                    NightLandings = Convert.ToInt32(dr["cNightLandings"], CultureInfo.InvariantCulture);
                    CrossCountry = Convert.ToDecimal(dr["crosscountry"], CultureInfo.InvariantCulture);
                    Nighttime = Convert.ToDecimal(dr["night"], CultureInfo.InvariantCulture);
                    IMC = Convert.ToDecimal(dr["IMC"], CultureInfo.InvariantCulture);
                    SimulatedIFR = Convert.ToDecimal(dr["simulatedInstrument"], CultureInfo.InvariantCulture);
                    GroundSim = Convert.ToDecimal(util.ReadNullableField(dr, "groundSim", 0.0M), CultureInfo.InvariantCulture);
                    Dual = Convert.ToDecimal(dr["dualReceived"], CultureInfo.InvariantCulture);
                    PIC = Convert.ToDecimal(dr["PIC"], CultureInfo.InvariantCulture);
                    TotalFlightTime = Convert.ToDecimal(dr["totalFlightTime"], CultureInfo.InvariantCulture);
                    CFI = Convert.ToDecimal(util.ReadNullableField(dr, "cfi", 0.0M), CultureInfo.InvariantCulture);
                    SIC = Convert.ToDecimal(util.ReadNullableField(dr, "SIC", 0.0M), CultureInfo.InvariantCulture);
                    fHoldingProcedures = Convert.ToBoolean(dr["fHold"], CultureInfo.InvariantCulture);
                    Comment = util.ReadNullableString(dr, "Comments");
                    Route = util.ReadNullableString(dr, "Route");
                    fIsPublic = Convert.ToBoolean(dr["fPublic"], CultureInfo.InvariantCulture);
                    fHasDataStream = (Convert.ToInt32(dr["FlightDataLength"], CultureInfo.InvariantCulture) > 0);
                    Telemetry = new TelemetryReference(dr);   // will check for DBNull

                    if (fHasDataStream && lto != LoadTelemetryOption.None)
                    {
                        string szData = dr["FlightData"].ToString();
                        if (String.IsNullOrEmpty(szData) && lto == LoadTelemetryOption.LoadAll)
                        {
                            try
                            {
                                Telemetry.LoadData();
                            }
                            catch (FileNotFoundException)
                            {
                                Telemetry.RawData = string.Empty;
                            }
                        }
                        else
                            Telemetry.RawData = szData;
                    }

                    HobbsStart = Convert.ToDecimal(util.ReadNullableField(dr, "hobbsStart", 0.0M), CultureInfo.InvariantCulture);
                    HobbsEnd = Convert.ToDecimal(util.ReadNullableField(dr, "hobbsEnd", 0.0M), CultureInfo.InvariantCulture);
                    dtEngineStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    dtEngineEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtEngineEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    dtFlightStart = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightStart", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    dtFlightEnd = DateTime.SpecifyKind(Convert.ToDateTime(util.ReadNullableField(dr, "dtFlightEnd", DateTime.MinValue), CultureInfo.InvariantCulture), DateTimeKind.Utc);

                    // Signature fields
                    FlightHash = (string)util.ReadNullableField(dr, "FlightHash", null);
                    SignatureHash = (string)util.ReadNullableField(dr, "SignatureHash", null);
                    CFIComments = (string)util.ReadNullableField(dr, "CFIComment", null);
                    CFISignatureDate = Convert.ToDateTime(util.ReadNullableField(dr, "SignatureDate", null), CultureInfo.InvariantCulture);
                    CFICertificate = (string)util.ReadNullableField(dr, "CFICertificate", null);
                    CFIExpiration = Convert.ToDateTime(util.ReadNullableField(dr, "CFIExpiration", null), CultureInfo.InvariantCulture);
                    CFIUsername = (string)util.ReadNullableField(dr, "CFIUserName", null);
                    CFIEmail = (string)util.ReadNullableField(dr, "CFIEmail", null);
                    CFIName = (string)util.ReadNullableField(dr, "CFIName", null);
                    CFISignatureState = (SignatureState)Convert.ToInt32(dr["SignatureState"], CultureInfo.InvariantCulture);
                    HasDigitizedSig = Convert.ToBoolean(dr["HasDigitizedSignature"], CultureInfo.InvariantCulture);

                    // Get descriptor fields.  These are read but never written
                    ModelDisplay = util.ReadNullableField(dr, "ModelDisplay", "").ToString();
                    CatClassDisplay = util.ReadNullableField(dr, "CatClassDisplay", string.Empty).ToString();
                    TailNumDisplay = util.ReadNullableField(dr, "TailNumberdisplay", string.Empty).ToString();

                    // Load properties, if available.
                    CustomProperties.SetItems(CustomFlightProperty.PropertiesFromJSONTuples((string)util.ReadNullableField(dr, "CustomPropsJSON", string.Empty), FlightID));

                    string szVids = dr["FlightVids"].ToString();
                    if (!String.IsNullOrEmpty(szVids))
                    {
                        VideoRef[] vids = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoRef[]>(szVids);
                        List<VideoRef> lst = new List<VideoRef>();
                        foreach (VideoRef vid in vids)
                            lst.Add(vid);
                        Videos = lst.ToArray();
                    }

                    return true;
                }
                else
                {
                    m_lastErr = ErrorCode.NotOwned;
                    szError = String.Format(CultureInfo.InvariantCulture, "User {0} does not have access to this object (flight {1}).", szUser, dr["idFlight"]);
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Exception initializing Logbook Entry from DR: " + ex.Message, ex);
            }
        }
        #endregion

        #region Static Get Methods
        /// <summary>
        /// Returns the public flights for a user
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="limit">Maximum # of entries to return</param>
        /// <param name="offset">The offset to the first entry to return</param>
        /// <returns>An array of logbook entries</returns>
        public static LogbookEntry[] GetPublicFlightsForUser(string szUser, int offset, int limit)
        {
            FlightQuery fq = new FlightQuery(szUser) { IsPublic = true };
            DBHelper dbh = new DBHelper(QueryCommand(fq, offset, limit));

            List<LogbookEntry> lstFlights = new List<LogbookEntry>();
            dbh.ReadRows(
                (comm) => { },
                (dr) => { lstFlights.Add(new LogbookEntry(dr, szUser)); }
            );

            return lstFlights.ToArray();
        }
        #endregion

        /// <summary>
        /// Returns the username that owns a flight - useful to check authorization, or for details so that next/previous can work
        /// </summary>
        /// <param name="idFlight">The ID of the flight</param>
        /// <returns>The name of the user, empty string if the flight doesn't exist.</returns>
        public static string OwnerForFlight(int idFlight)
        {
            string szUser = string.Empty;
            DBHelper dbh = new DBHelper("SELECT username FROM flights WHERE idflight=?id");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("id", idFlight); },
                (dr) => { szUser = (string)dr["username"]; });
            return szUser;
        }

        /// <summary>
        /// Returns the ordered IDs for flights for the given user and query
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="fq"></param>
        /// <returns>Just the ID's</returns>
        public static IEnumerable<int> FlightIDsForUser(string szUser, FlightQuery fq = null)
        {
            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper(QueryCommand(fq ?? new FlightQuery(szUser)));
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });
            return lst;
        }

        /// <summary>
        /// Read the specified logbook row from the database and initialize the object from that row.
        /// The username of the database entry MUST match the username that is supplied unless fForceLoad is true.
        /// We use additionalInit so that this can be called by LogbookEntryDisplay without making this method virtual (which generates
        /// a warning because this is called by a constructor for LogbookEntry)
        /// </summary>
        /// <param name="idRow">Row to read.  Pass idNewFlight for a new logbook entry</param>
        /// <param name="szUserName">User (owner) of the requested row</param>
        /// <param name="lto">Specify whether or not to load telemetry for the flight.</param>
        /// <param name="fForceLoad">True to load even if the requested user doesn't own the row</param>
        /// <param name="additionalInit">If provided, allows additional initialization from the datareader.</param>
        /// <returns>True if successful</returns>
        protected Boolean FLoadFromDB(int idRow, string szUserName, LoadTelemetryOption lto, Boolean fForceLoad, Action<MySqlDataReader> additionalInit)
        {
            Boolean fResult = false;

            User = szUserName;    // in case we don't actually load anything, we should at least set this so that subsequent saves do the right thing.
            if (idRow > 0)
            {
                FlightQuery fq = new FlightQuery(string.Empty) { CustomRestriction = String.Format(" (flights.idflight={0}) ", idRow) };
                DBHelper dbh = new DBHelper(QueryCommand(fq, 0, 1, false, lto));

                bool fRowFound = false;

                if (!dbh.ReadRow(
                    (comm) => { },
                    (dr) =>
                    {
                        fRowFound = true;

                        // just use the owning username if we are forcing the load.
                        if (fForceLoad && dr["username"] != null && dr["username"].ToString().Length > 0)
                            szUserName = dr["username"].ToString().Trim();

                        fResult = InitFromDataReader(dr, szUserName, lto);
                        if (additionalInit != null)
                            additionalInit(dr);
                    }))
                    szError += dbh.LastError;

                if (!fRowFound)
                    m_lastErr = ErrorCode.NotFound;
            }
            else
                fResult = true; // always succeed for a new flight

            return fResult;
        }

        /// <summary>
        /// Read the specified logbook row from the database and initialize the object from that row.
        /// The username of the database entry MUST match the username that is supplied unless fForceLoad is true.
        /// We use additionalInit so that this can be called by LogbookEntryDisplay without making this method virtual (which generates
        /// a warning because this is called by a constructor for LogbookEntry)
        /// </summary>
        /// <param name="idRow">Row to read.  Pass idNewFlight for a new logbook entry</param>
        /// <param name="szUserName">User (owner) of the requested row</param>
        /// <param name="lto">Specify whether or not to load telemetry for the flight.</param>
        /// <param name="fForceLoad">True to load even if the requested user doesn't own the row</param>
        /// <returns>True if successful</returns>
        public Boolean FLoadFromDB(int idRow, string szUserName, LoadTelemetryOption lto = LoadTelemetryOption.None, Boolean fForceLoad = false)
        {
            return FLoadFromDB(idRow, szUserName, lto, fForceLoad, null);
        }

        #region Telemetry
        /// <summary>
        /// Moves flight telemetry into the Flights table from the disk.  Deletes the disk-based telemetry object
        /// </summary>
        public void MoveTelemetryToFlightEntry()
        {
            string sz = Telemetry.LoadData();
            DBHelper dbh = new DBHelper("UPDATE Flights SET telemetry=Compress(?t) WHERE idFlight=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("t", sz);
                comm.Parameters.AddWithValue("id", FlightID);
            });
            if (String.IsNullOrEmpty(dbh.LastError))
                Telemetry.Delete();
            else
                throw new MyFlightbookException("Error moving telemetry into flights table: " + dbh.LastError);
        }

        /// <summary>
        /// Moves flight telemetry from the flights table to the disk, creating a disk-based telemetry object.
        /// </summary>
        public void MoveTelemetryFromFlightEntry()
        {
            if (!String.IsNullOrEmpty(FlightData))
            {
                Telemetry.FlightID = FlightID;
                Telemetry.Commit();
                DBHelper dbh = new DBHelper("UPDATE Flights SET telemetry=null WHERE idFlight=?id");
                dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", FlightID); });
            }
        }

        /// <summary>
        /// Returns the distances and average speed flown on this flight
        /// </summary>
        /// <param name="PathDistance">The pre-computed TELEMETRY distance in NM, if known (performance imprevement), or null</param>
        /// <returns></returns>
        public string GetPathDistanceDescription(double? PathDistance)
        {
            if (!PathDistance.HasValue && !String.IsNullOrEmpty(FlightData))
            {
                using (FlightData fd = new FlightData())
                {
                    fd.ParseFlightData(FlightData);
                    if (fd.HasLatLongInfo)
                        PathDistance = fd.ComputePathDistance();
                }
            }

            MyFlightbook.Airports.AirportList al = new MyFlightbook.Airports.AirportList(Route);
            double dRoute = al.DistanceForRoute();
            double dMaxSegment = al.MaxSegmentForRoute();
            double dMaxDistanceFromStart = al.MaxDistanceFromStartingAirport();
            double dPath = PathDistance ?? 0.0;

            double time = (FlightStart.HasValue() && FlightEnd.HasValue()) ? FlightEnd.Subtract(FlightStart).TotalHours : (double) TotalFlightTime;

            List<string> lst = new List<string>();

            if (dRoute + dPath > 0)
            {
                if (time > 0)
                    lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightAverageSpeed, Math.Max(dPath, dRoute) / time));

                if (dPath > 0)
                    lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDistancePathOnly, dPath));

                if (dRoute > 0)
                {
                    lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDistanceRouteOnly, dRoute));

                    if (al.GetNormalizedAirports().Count() > 2)
                    {
                        lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDistanceLongestSegment, dMaxSegment));
                        if (dMaxSegment != dMaxDistanceFromStart)
                            lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDistanceFurthestFromDeparture, dMaxDistanceFromStart));

                        // Find the farthest airports from one another.
                        Dictionary<string, airport> dictGroupedAirports = new Dictionary<string, airport>();
                        // Group ports (no navaids, ad-hoc fixes, etc.) geographically
                        foreach (airport ap in al.UniqueAirports)
                            if (ap.IsPort)
                                dictGroupedAirports[String.Format(CultureInfo.InvariantCulture, "{0:#.#00}{1:#.#00}", ap.LatLong.Latitude, ap.LatLong.Longitude)] = ap;

                        List<airport> uniques = new List<airport>(dictGroupedAirports.Values);
                        if (uniques.Count > 2)
                        {
                            airport ap1 = null, ap2 = null;
                            double maxDist = 0;

                            for (int i = 0; i < uniques.Count; i++)
                            {
                                for (int j = i + 1; j < uniques.Count; j++)
                                {
                                    double dist = uniques[i].DistanceFromAirport(uniques[j]);
                                    if (dist > maxDist)
                                    {
                                        maxDist = dist;
                                        ap1 = uniques[i];
                                        ap2 = uniques[j];
                                    }
                                }
                            }

                            if (ap1 != null && ap2 != null && maxDist > 0)
                                lst.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDistanceFurthestPoints, ap1.Code, ap2.Code, maxDist));
                        }
                    }
                }

                return String.Join(Resources.LocalizedText.LocalizedSpace, lst);
            }
            return string.Empty;
        }
        #endregion

        #region subtotals (for printing, mostly)
        /// <summary>
        /// Adds totals of core time, landing, and approach values from one logbookentry object to this one; MODIFIES THE TARGET OBJECT (this)
        /// Does addition via rounding method in the database (rounding to nearest minutes)
        /// </summary>
        /// <param name="le1"></param>
        public virtual void AddFrom(LogbookEntry le)
        {
            if (le == null)
                throw new ArgumentNullException("le");

            Approaches += le.Approaches;

            Landings += le.Landings;
            NightLandings += le.NightLandings;
            FullStopLandings += le.FullStopLandings;

            CrossCountry = CrossCountry.AddMinutes(le.CrossCountry);
            Dual = Dual.AddMinutes(le.Dual);
            GroundSim = GroundSim.AddMinutes(le.GroundSim);
            IMC = IMC.AddMinutes(le.IMC);
            SimulatedIFR = SimulatedIFR.AddMinutes(le.SimulatedIFR);
            Nighttime = Nighttime.AddMinutes(le.Nighttime);
            CFI = CFI.AddMinutes(le.CFI);
            SIC = SIC.AddMinutes(le.SIC);
            PIC = PIC.AddMinutes(le.PIC);
            TotalFlightTime = TotalFlightTime.AddMinutes(le.TotalFlightTime);
        }

        private void MergeProperty(CustomFlightProperty cfpExisting, CustomFlightProperty cfp)
        {
            if (cfpExisting == null || cfp == null || cfp.PropertyType == null)
                return;

            switch (cfp.PropertyType.Type)
            {
                default:
                case CFPPropertyType.cfpBoolean:
                    break;
                case CFPPropertyType.cfpCurrency:
                    cfpExisting.DecValue = cfp.DecValue;
                    break;
                case CFPPropertyType.cfpDate:
                case CFPPropertyType.cfpDateTime:
                    switch (cfp.PropertyType.PropTypeID)
                    {
                        case (int)CustomPropertyType.KnownProperties.IDBlockOut:
                        case (int)CustomPropertyType.KnownProperties.IDPropTachStart:
                        case (int)CustomPropertyType.KnownProperties.IDPropDutyStart:
                        case (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart:
                            cfpExisting.DateValue = cfp.DateValue.EarlierDate(cfpExisting.DateValue);
                            break;
                        case (int)CustomPropertyType.KnownProperties.IDBlockIn:
                        case (int)CustomPropertyType.KnownProperties.IDPropTachEnd:
                        case (int)CustomPropertyType.KnownProperties.IDPropDutyEnd:
                        case (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd:
                            cfpExisting.DateValue = cfp.DateValue.LaterDate(cfpExisting.DateValue);
                            break;
                    }
                    break;
                case CFPPropertyType.cfpDecimal:
                    if (cfp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachStart)
                        cfpExisting.DecValue = Math.Min(cfpExisting.DecValue, cfp.DecValue);
                    else if (cfp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachEnd)
                        cfpExisting.DecValue = Math.Max(cfpExisting.DecValue, cfp.DecValue);
                    else if (cfp.PropertyType.IsNoSum)
                        cfpExisting.DecValue = cfp.DecValue;
                    else
                        cfpExisting.DecValue += cfp.DecValue;
                    break;
                case CFPPropertyType.cfpInteger:
                    if (cfp.PropertyType.IsNoSum)
                        cfpExisting.IntValue = cfp.IntValue;
                    else
                        cfpExisting.IntValue += cfp.IntValue;
                    break;
                case CFPPropertyType.cfpString:
                    cfpExisting.TextValue = cfp.TextValue;
                    break;
            }
        }

        private void MergePropertiesFrom(List<CustomFlightProperty> lstCfpThis, LogbookEntry le)
        {
            if (le == null)
                return;
            // Merge properties, being smart on tach, block, duty, and flight duty time.
            if (le.CustomProperties != null)
            {
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    CustomFlightProperty cfpExisting = lstCfpThis.FirstOrDefault(fp => fp.PropTypeID == cfp.PropTypeID);
                    if (cfpExisting == null)
                    {
                        cfp.FlightID = FlightID;
                        lstCfpThis.Add(cfp);
                    }
                    else
                        MergeProperty(cfpExisting, cfp);
                }
            }
        }

        private void MergeImagesFrom(LogbookEntry le)
        {
            if (le.FlightImages != null)
            {
                foreach (MFBImageInfo mfbii in le.FlightImages)
                    mfbii.MoveImage(FlightID.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Merges this flight with a set of other flights.  All flights should have telemetry and images populated.
        /// THIS IS DESTRUCTIVE - the other flights will be deleted!!!
        /// </summary>
        /// <param name="lst">The enumerable of other flights with which to merge</param>
        public void MergeFrom(IEnumerable<LogbookEntry> lst)
        {
            if (lst == null || lst.Count() == 0)
                return;

            List<CustomFlightProperty> lstCfpThis = new List<CustomFlightProperty>(CustomProperties);

            List<TelemetryReference> lstPaths = new List<TelemetryReference>();

            if (HasFlightData)
                lstPaths.Add(Telemetry);

            foreach (LogbookEntry le in lst)
            {
                AddFrom(le);
                Comment = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Comment, le.Comment);
                string[] newAirports = Regex.Split(le.Route, "\\W", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (string airport in newAirports)
                    if (!Route.TrimEnd().EndsWith(airport, StringComparison.CurrentCultureIgnoreCase))
                        Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Route, airport);
                HobbsStart = (HobbsStart == 0.0M || le.HobbsStart == 0.0M) ? Math.Max(HobbsStart, le.HobbsStart) : Math.Min(HobbsStart, le.HobbsStart);
                HobbsEnd = Math.Max(this.HobbsEnd, le.HobbsEnd);
                EngineStart = (!EngineStart.HasValue() || !le.EngineStart.HasValue()) ? EngineStart.LaterDate(le.EngineStart) : EngineStart.EarlierDate(le.EngineStart);
                EngineEnd = EngineEnd.LaterDate(le.EngineEnd);
                FlightStart = (!FlightStart.HasValue() || !le.FlightStart.HasValue()) ? FlightStart.LaterDate(le.FlightStart) : FlightStart.EarlierDate(le.FlightStart);
                FlightEnd = FlightEnd.LaterDate(le.FlightEnd);

                MergePropertiesFrom(lstCfpThis, le);
                MergeImagesFrom(le);
                if (le.HasFlightData)
                    lstPaths.Add(le.Telemetry);
            }

            CustomProperties.SetItems(lstCfpThis);
            TelemetryReference tr = TelemetryReference.MergedTelemetry(lstPaths, FlightID);
            if (tr != null)
                FlightData = tr.RawData;

            // Commit this flight
            ((LogbookEntry) this).FCommit(true, false);

            // And delete the input flights
            foreach (LogbookEntry le in lst)
                LogbookEntry.FDeleteEntry(le.FlightID, le.User);
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0:d}: {1}{2} {3}", this.Date, String.IsNullOrEmpty(this.TailNumDisplay) ? String.Empty : String.Format(CultureInfo.CurrentCulture, "({0}) ", this.TailNumDisplay), this.Comment, this.Route);
        }

        /// <summary>
        /// Displays just date, tail, and route (no comments)
        /// </summary>
        /// <returns>A string with just date, tail, and route.</returns>
        public string ToShortString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0:d}: {1}{2}", this.Date, String.IsNullOrEmpty(this.TailNumDisplay) ? String.Empty : String.Format(CultureInfo.CurrentCulture, "({0}) ", this.TailNumDisplay), this.Route);
        }
    }

    [Serializable]
    public class LogbookEntry : LogbookEntryBase, IPostable
    {
        #region IPostable
        [Newtonsoft.Json.JsonIgnore]
        public string SocialMediaComment
        {
            get
            {
                string sz1 = Route.Trim();
                string sz2 = Comment.Trim();
                return (sz1.Length > 0 && sz2.Length > 0) ? sz1 + Resources.LocalizedText.ColonConnector + sz2 : sz1 + sz2;
            }
        }

        public Uri SocialMediaItemUri(string szHost = null)
        {
            return SocialMediaLinks.ShareFlightUri(this, szHost);
        }

        public Uri SendFlightUri(string szHost = null, string szTarget = null)
        {
            return SocialMediaLinks.SendFlightUri(new UserAccessEncryptor().Encrypt(String.Format(CultureInfo.InvariantCulture, "{0} {1}", this.FlightID, this.User)), szHost, szTarget);
        }

        public MFBImageInfo SocialMediaImage(string szHost = null)
        {
            PopulateImages();
            if (FlightImages.Length > 0)
                return FlightImages[0].ImageType == MFBImageInfo.ImageFileType.JPEG ? FlightImages[0] : FlightImages[0];    // use video thumbnail if it's for a video, since we can't use the video link itself.
            else
            {
                // Use the preferred aircraft, if one is specified.
                UserAircraft ua = new UserAircraft(User);
                Aircraft ac = ua.GetUserAircraftByID(AircraftID) ?? new Aircraft(AircraftID);
                ac.PopulateImages();
                if (ac.AircraftImages.Count > 0)
                    return ac.AircraftImages[0];
            }
            return null;
        }

        [Newtonsoft.Json.JsonIgnore]
        public bool CanPost
        {
            get { return (fIsPublic || Route.Length > 0); }
        }

        #region Sharing
        // Send flight and social media links are
        // done as overrides so that the WSDL doesn't have to refer to LogbookEntryBase.
        public override string SendFlightLink
        {
            get { return SendFlightUri().ToString(); }
            set { } // to enable serialization
        }

        public override string SocialMediaLink
        {
            get { return CanPost ? SocialMediaItemUri().AbsoluteUri : string.Empty; }
            set { } // to enable serialization
        }
        #endregion
        #endregion IPostable

        #region Constructors
        public LogbookEntry() : base() { }

        /// <summary>
        /// Create a new LogbookEntry object, loading from the database using the specified ID.  Loads ALL telemetry.
        /// <param name="flightID">The ID of the flight to load</param>
        /// <param name="szUser">The name of the owner of the flight.</param>
        /// <param name="lto">Options for loading telemetry; default is to load all telemetry</param>
        /// <param name="fForceLoad">Specifies whether the flight should be loaded even if its username doesn't match szuser</param>
        /// </summary>
        public LogbookEntry(int flightID, string szUser = null, LoadTelemetryOption lto = LoadTelemetryOption.None, bool fForceLoad = false)
            : this()
        {
            if (flightID != idFlightNew && !String.IsNullOrEmpty(szUser))
                FLoadFromDB(flightID, szUser, lto, fForceLoad);
        }

        /// <summary>
        /// Creates a new flight initialized from another user's flight, as created using EncodeShareKey
        /// </summary>
        /// <param name="szKey">The key - MUST be from EncodeShareKey</param>
        /// <param name="szTargetUser">The target user.  MUST MATCH the target user passed to EncodeShareKeyForUser</param>
        /// <returns>A new LogbookEntry object - Check the ErrorString for any potential errors.</returns>
        public LogbookEntry(string szKey) : this()
        {
            string sz = new UserAccessEncryptor().Decrypt(szKey);
            string[] rgSz = sz.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                if (rgSz.Length == 2)
                {
                    LogbookEntry leSrc = new LogbookEntry(Convert.ToInt32(rgSz[0], CultureInfo.InvariantCulture), rgSz[1], LoadTelemetryOption.LoadAll);
                    if (!String.IsNullOrEmpty(leSrc.ErrorString))
                        throw new MyFlightbookException(leSrc.ErrorString);

                    leSrc.Clone(this);

                    // clear out any role like PIC/SIC that likely doesn't carry over to the target pilot.
                    CFI = Dual = PIC = SIC = 0.0M;
                }
            }
            catch (MyFlightbookException ex)
            {
                LastError = ErrorCode.Unknown;
                ErrorString = ex.Message;
            }
        }

        /// <summary>
        /// Create an entry from a datareader row for the specified user
        /// </summary>
        /// <param name="dr">The data reader</param>
        /// <param name="szUser">The user - MUST own the object!</param>
        /// <param name="lto">Whether or not to load telemetry - NOT LOADED BY DEFAULT</param>
        public LogbookEntry(MySqlDataReader dr, string szUser, LoadTelemetryOption lto = LoadTelemetryOption.None)
            : this()
        {
            InitFromDataReader(dr, szUser, lto);
        }
        #endregion

        #region images
        /// <summary>
        /// Fill FlightImages with images from the flight.
        /// <param name="fOnlyImages">Indicates if only images should be returned (i.e., no PDF or movie)</param>
        /// </summary>
        public void PopulateImages(bool fOnlyImages = false)
        {
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Flight, FlightID.ToString(CultureInfo.InvariantCulture));
            il.Refresh(true, null, !fOnlyImages);
            FlightImages = il.ImageArray.ToArray();
        }
        #endregion

        #region Comparison
        /// <summary>
        /// Get the differences between this and another entry, which is considered the NEW entry
        /// </summary>
        /// <param name="le"></param>
        /// <returns></returns>
        public IEnumerable<PropertyDelta> CompareTo(LogbookEntryBase le, bool fUseHHMM)
        {
            if (le == null)
                return null;

            List<PropertyDelta> lst = new List<PropertyDelta>();

            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.PrintHeaderDate, Date.ToShortDateString(), le.Date.ToShortDateString(), lst);
            if (AircraftID != le.AircraftID)
                PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldTail, TailNumDisplay, le.TailNumDisplay, lst);

            if (CatClassOverride != le.CatClassOverride)
            {
                CategoryClass ccThis = CatClassOverride == 0 ? null : CategoryClass.CategoryClassFromID((CategoryClass.CatClassID)CatClassOverride);
                CategoryClass ccNew = le.CatClassOverride == 0 ? null : CategoryClass.CategoryClassFromID((CategoryClass.CatClassID)le.CatClassOverride);
                PropertyDelta.AddPotentialChange(Resources.LogbookEntry.PrintHeaderCategory2, ccThis == null ? null : ccThis.CatClass, ccNew == null ? null : ccNew.CatClass, lst);
            }

            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.HobbsStart, HobbsStart.FormatDecimal(fUseHHMM), le.HobbsStart.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.HobbsEnd, HobbsEnd.FormatDecimal(fUseHHMM), le.HobbsEnd.FormatDecimal(fUseHHMM), lst);

            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldEngineStart, EngineStart.UTCFormattedStringOrEmpty(false), le.EngineStart.UTCFormattedStringOrEmpty(false), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldEngineEnd, EngineEnd.UTCFormattedStringOrEmpty(false), le.EngineEnd.UTCFormattedStringOrEmpty(false), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldFlightStart, FlightStart.UTCFormattedStringOrEmpty(false), le.FlightStart.UTCFormattedStringOrEmpty(false), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldFlightEnd, FlightEnd.UTCFormattedStringOrEmpty(false), le.FlightEnd.UTCFormattedStringOrEmpty(false), lst);

            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldComments, Comment, le.Comment, lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldRoute, Route, le.Route, lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldApproaches, Approaches.FormatInt(), le.Approaches.FormatInt(), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldHold, fHoldingProcedures ? Resources.LogbookEntry.PropertyYes : string.Empty, le.fHoldingProcedures ? Resources.LogbookEntry.PropertyYes : string.Empty, lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldLanding, Landings.FormatInt(), le.Landings.FormatInt(), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldNightLandings, NightLandings.FormatInt(), le.NightLandings.FormatInt(), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldDayLandings, FullStopLandings.FormatInt(), le.FullStopLandings.FormatInt(), lst);

            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldXCountry, CrossCountry.FormatDecimal(fUseHHMM), le.CrossCountry.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldNight, Nighttime.FormatDecimal(fUseHHMM), le.Nighttime.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldSimIMCFull, IMC.FormatDecimal(fUseHHMM), le.IMC.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldIMC, SimulatedIFR.FormatDecimal(fUseHHMM), le.SimulatedIFR.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldGroundSimFull, GroundSim.FormatDecimal(fUseHHMM), le.GroundSim.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldDual, Dual.FormatDecimal(fUseHHMM), le.Dual.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldSIC, SIC.FormatDecimal(fUseHHMM), le.SIC.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldPIC, PIC.FormatDecimal(fUseHHMM), le.PIC.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldCFI, CFI.FormatDecimal(fUseHHMM), le.CFI.FormatDecimal(fUseHHMM), lst);
            PropertyDelta.AddPotentialChange(Resources.LogbookEntry.FieldTotal, TotalFlightTime.FormatDecimal(fUseHHMM), le.TotalFlightTime.FormatDecimal(fUseHHMM), lst);

            List<CustomFlightProperty> lstPropsThis = new List<CustomFlightProperty>(CustomProperties);
            List<CustomFlightProperty> lstPropsNew = new List<CustomFlightProperty>(le.CustomProperties);

            lstPropsThis.Sort((cfp1, cfp2) => { return cfp1.PropTypeID.CompareTo(cfp2.PropTypeID); });
            lstPropsNew.Sort((cfp1, cfp2) => { return cfp1.PropTypeID.CompareTo(cfp2.PropTypeID); });

            foreach (CustomFlightProperty cfp in lstPropsThis)
            {
                CustomFlightProperty cfpNew = lstPropsNew.FirstOrDefault(c => c.PropTypeID == cfp.PropTypeID);
                if (cfpNew == null)
                    lst.Add(new PropertyDelta(cfp.PropertyType.Title, cfp.ValueString, string.Empty));
                else
                {
                    PropertyDelta.AddPotentialChange(cfp.PropertyType.Title, cfp.DisplayString, cfpNew.DisplayString, lst);
                    lstPropsNew.Remove(cfpNew);
                }
            }
            foreach (CustomFlightProperty cfp in lstPropsNew)
                PropertyDelta.AddPotentialChange(cfp.PropertyType.Title, string.Empty, cfp.ValueString, lst);

            lst.Sort();

            return lst;
        }
        #endregion

        #region Auto-fill utility functions
        /// <summary>
        /// Computes auto hobbs based on the specified options.
        /// </summary>
        /// <param name="opt">The autofill options</param>
        public void AutoHobbs(AutoFillOptions opt)
        {
            if (opt == null)
                throw new ArgumentNullException("opt");

            if (HobbsStart != 0 && HobbsEnd <= HobbsStart)
            {
                switch (opt.AutoFillHobbs)
                {
                    case AutoFillOptions.AutoFillHobbsOption.None:
                        break;
                    case AutoFillOptions.AutoFillHobbsOption.EngineTime:
                        if (EngineStart.CompareTo(DateTime.MinValue) != 0 && EngineEnd.CompareTo(DateTime.MinValue) != 0)
                        {
                            HobbsEnd = HobbsStart + Convert.ToDecimal(TimeSpan.FromTicks(EngineEnd.Ticks - EngineStart.Ticks).TotalHours);
                            break;
                        }
                        // else Fall through to flight time
                        goto case AutoFillOptions.AutoFillHobbsOption.FlightTime;
                    case AutoFillOptions.AutoFillHobbsOption.FlightTime:
                        if (FlightStart.CompareTo(DateTime.MinValue) != 0 && FlightEnd.CompareTo(DateTime.MinValue) != 0)
                        {
                            HobbsEnd = HobbsStart + Convert.ToDecimal(TimeSpan.FromTicks(FlightEnd.Ticks - FlightStart.Ticks).TotalHours);
                            break;
                        }
                        // else fall through to total time.
                        goto case AutoFillOptions.AutoFillHobbsOption.TotalTime;
                    case AutoFillOptions.AutoFillHobbsOption.TotalTime:
                        HobbsEnd = HobbsStart + TotalFlightTime;
                        break;
                }
            }
        }

        /// <summary>
        /// Computes auto totals based on the specified options.  Only makes changes if TotalFlighttime is 0.
        /// </summary>
        /// <param name="opt"></param>
        public void AutoTotals(AutoFillOptions opt)
        {
            if (opt == null)
                throw new ArgumentNullException("opt");

            // Compute total time based on autofill options
            if (TotalFlightTime == 0)
            {
                switch (opt.AutoFillTotal)
                {
                    case AutoFillOptions.AutoFillTotalOption.None:
                        break;
                    case AutoFillOptions.AutoFillTotalOption.EngineTime:
                        if (EngineStart.HasValue() && EngineEnd.HasValue() && EngineStart.CompareTo(EngineEnd) < 0)
                        {
                            TotalFlightTime = (decimal)EngineEnd.Subtract(EngineStart).TotalHours;
                            break;
                        }
                        goto case AutoFillOptions.AutoFillTotalOption.FlightTime;
                    // else fall through and do flight time.
                    case AutoFillOptions.AutoFillTotalOption.FlightTime:
                        if (FlightStart.HasValue() && FlightEnd.HasValue() && FlightStart.CompareTo(FlightEnd) < 0)
                        {
                            TotalFlightTime = (decimal)FlightEnd.Subtract(FlightStart).TotalHours;
                            break;
                        }
                        goto case AutoFillOptions.AutoFillTotalOption.HobbsTime;
                    // else fall through and do hobbs time.
                    case AutoFillOptions.AutoFillTotalOption.HobbsTime:
                        if (HobbsStart > 0 && HobbsEnd > HobbsStart)
                        {
                            TotalFlightTime = HobbsEnd - HobbsStart;
                            break;
                        }
                        goto case AutoFillOptions.AutoFillTotalOption.BlockTime;
                    // else fall through and do block time.
                    case AutoFillOptions.AutoFillTotalOption.BlockTime:
                        {
                            CustomFlightProperty cfpBlockOut = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut);
                            CustomFlightProperty cfpBlockIn = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn);

                            if (cfpBlockIn != null && cfpBlockOut != null && !cfpBlockIn.IsDefaultValue && !cfpBlockOut.IsDefaultValue && cfpBlockIn.DateValue.CompareTo(cfpBlockOut.DateValue) > 0)
                                TotalFlightTime = (decimal)cfpBlockIn.DateValue.Subtract(cfpBlockOut.DateValue).TotalHours;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Performs final tasks after autofill.  Specifically does auto-cross country, closes off the last landing (if needed, and adds a night landing if it was at night)
        /// </summary>
        /// <param name="opt">The autofill options</param>
        public void AutoFillFinish(AutoFillOptions opt)
        {
            if (opt == null)
                throw new ArgumentNullException("opt");
            AirportList al = new AirportList(Route);
            if (TotalFlightTime > 0)
            {
                if (al.MaxDistanceForRoute() > opt.CrossCountryThreshold)
                    CrossCountry = TotalFlightTime;
            }

            airport[] rgap = al.GetNormalizedAirports();
            if (String.IsNullOrEmpty(FlightData) && TotalFlightTime > 0 && rgap.Length > 1)
            {
                // add a landing if there is total time and no other landings detected.
                if (Landings == 0)
                    Landings = Math.Max(1, FullStopLandings + NightLandings);

                // if only one landing is specified, and the flight-end time is known, and it is night, add a night landing.
                if (rgap.Length == 2 && Landings == 1 && NightLandings == 0 && FlightEnd.HasValue())
                {
                    MyFlightbook.Geography.LatLong ll = rgap[1].LatLong;
                    MyFlightbook.Solar.SunriseSunsetTimes sst = new MyFlightbook.Solar.SunriseSunsetTimes(FlightEnd, ll.Latitude, ll.Longitude);
                    if (sst.IsFAANight)
                        NightLandings = 1;
                }
            }

            // Issue #411: check for ground instruction given or received.
            if ((Dual > 0 && CFI == 0) || (CFI > 0 && Dual == 0)) // no point if we can't tell whether it's given or received...
            {
                CustomFlightProperty cfpLessonStart = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropLessonStart);
                CustomFlightProperty cfpLessonEnd = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropLessonEnd);

                CustomPropertyType.KnownProperties idTargetID = (Dual > 0) ? CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived : CustomPropertyType.KnownProperties.IDPropGroundInstructionGiven;
                CustomFlightProperty cfpTarget = CustomProperties.GetEventWithTypeID(idTargetID);

                if (cfpTarget == null && cfpLessonStart != null && cfpLessonEnd != null && cfpLessonEnd.DateValue.CompareTo(cfpLessonStart.DateValue) > 0)
                {
                    TimeSpan tsGroundTraining = cfpLessonEnd.DateValue.Subtract(cfpLessonStart.DateValue);

                    // Pull out any flight or engine time, whichever is greater.
                    TimeSpan tsEngine = (EngineEnd.HasValue() && EngineStart.HasValue()) ? EngineEnd.Subtract(EngineStart) : TimeSpan.MinValue;
                    TimeSpan tsFlight = (FlightEnd.HasValue() && FlightStart.HasValue()) ? FlightEnd.Subtract(FlightStart) : TimeSpan.MinValue;
                    tsGroundTraining = tsGroundTraining.Subtract(tsEngine.CompareTo(tsFlight) > 0 ? tsEngine : tsFlight);

                    CustomProperties.Add(CustomFlightProperty.PropertyWithValue(idTargetID, (decimal)tsGroundTraining.TotalHours));
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Specifies the kind of additional columns that can be displayed for printing.
    /// </summary>
    public enum OptionalColumnType { None, Complex, Retract, Tailwheel, HighPerf, TAA, Turbine, Jet, TurboProp, ATD, FTD, FFS, ASEL, ASES, AMEL, AMES, Helicopter, Glider, CustomProp }

    public enum OptionalColumnValueType { Decimal, Integer, Time }

    /// <summary>
    /// An additional print column that can be displayed.
    /// </summary>
    [Serializable]
    public class OptionalColumn
    {
        #region Properties
        /// <summary>
        /// What kind of property is this?
        /// </summary>
        public OptionalColumnType ColumnType { get; set; }

        /// <summary>
        /// If this is a property, what is the ID of the property?
        /// </summary>
        public int IDPropType { get; set; }

        /// <summary>
        /// The title for the column
        /// </summary>
        public string Title { get; set; }

        public OptionalColumnValueType ValueType { get; set; }
        #endregion

        #region Constructors
        public OptionalColumn()
        {
            ColumnType = OptionalColumnType.None;
            IDPropType = (int)CustomPropertyType.KnownProperties.IDPropInvalid;
            Title = string.Empty;
            ValueType = OptionalColumnValueType.Decimal;
        }

        public OptionalColumn(OptionalColumnType type) : this()
        {
            ColumnType = type;
            if (type == OptionalColumnType.CustomProp || type == OptionalColumnType.None)
                throw new ArgumentOutOfRangeException("type");

            ValueType = OptionalColumnValueType.Decimal;
            Title = TitleForType(type);
        }

        public OptionalColumn(int idPropType) : this()
        {
            ColumnType = OptionalColumnType.CustomProp;
            IDPropType = idPropType;
            CustomPropertyType cpt = CustomPropertyType.GetCustomPropertyType(idPropType);
            switch (cpt.Type)
            {
                case CFPPropertyType.cfpDecimal:
                    ValueType = (cpt.IsBasicDecimal) ? OptionalColumnValueType.Decimal : OptionalColumnValueType.Time;
                    break;
                case CFPPropertyType.cfpInteger:
                    ValueType = OptionalColumnValueType.Integer;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("idPropType");
            }
            Title = cpt.Title;
        }
        #endregion

        public static string TitleForType(OptionalColumnType type)
        {
            switch (type)
            {
                case OptionalColumnType.CustomProp:
                case OptionalColumnType.None:
                    return string.Empty;
                case OptionalColumnType.Complex:
                    return Resources.Makes.IsComplex;
                case OptionalColumnType.Retract:
                    return Resources.Makes.IsRetract;
                case OptionalColumnType.Tailwheel:
                    return Resources.Makes.IsTailwheel;
                case OptionalColumnType.HighPerf:
                    return Resources.Makes.IsHighPerf;
                case OptionalColumnType.TAA:
                    return Resources.Makes.IsTAA;
                case OptionalColumnType.Turbine:
                    return Resources.Makes.IsTurbine;
                case OptionalColumnType.Jet:
                    return Resources.Makes.IsJet;
                case OptionalColumnType.TurboProp:
                    return Resources.Makes.IsTurboprop;
                case OptionalColumnType.ASEL:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASEL).CatClass;
                case OptionalColumnType.AMEL:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMEL).CatClass;
                case OptionalColumnType.ASES:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.ASES).CatClass;
                case OptionalColumnType.AMES:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.AMES).CatClass;
                case OptionalColumnType.Glider:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Glider).CatClass;
                case OptionalColumnType.Helicopter:
                    return CategoryClass.CategoryClassFromID(CategoryClass.CatClassID.Helicopter).CatClass;
                case OptionalColumnType.ATD:
                case OptionalColumnType.FTD:
                case OptionalColumnType.FFS:
                    return type.ToString();
            }
            throw new ArgumentOutOfRangeException("type", "Unknown OptionalColumnType: " + type.ToString());
        }

        public bool IsCatClass
        {
            get
            {
                switch (ColumnType)
                {
                    default:
                        return false;
                    case OptionalColumnType.AMEL:
                    case OptionalColumnType.AMES:
                    case OptionalColumnType.ASEL:
                    case OptionalColumnType.ASES:
                    case OptionalColumnType.Glider:
                    case OptionalColumnType.Helicopter:
                        return true;
                }
            }
        }

        public static int CatClassColumnCount(IEnumerable<OptionalColumn> optionalColumns)
        {
            return (optionalColumns == null) ? 0 : optionalColumns.Count(oc => oc.IsCatClass);
        }

        public override string ToString() { return Title; }

        public enum OptionalColumnRestriction { None, CatClassOnly, NotCatClass };

        /// <summary>
        /// Indicates whether or not to show a column at the specified index.  Can restrict to category/class columns if the layout would group those separate from other optional columns
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="index"></param>
        /// <param name="restriction"></param>
        /// <returns></returns>
        public static bool ShowOptionalColumn(IEnumerable<OptionalColumn> OptionalColumns, int index, OptionalColumnRestriction restriction)
        {
            if (OptionalColumns == null || index < 0 || index >= OptionalColumns.Count())
                return false;

            switch (restriction)
            {
                default:
                case OptionalColumnRestriction.None:
                    return true;
                case OptionalColumnRestriction.CatClassOnly:
                    return OptionalColumns.ElementAt(index).IsCatClass;
                case OptionalColumnRestriction.NotCatClass:
                    return !OptionalColumns.ElementAt(index).IsCatClass;
            }
        }

        /// <summary>
        /// Title for a given column to display
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string OptionalColumnName(IEnumerable<OptionalColumn> OptionalColumns, int index)
        {
            return ShowOptionalColumn(OptionalColumns, index, OptionalColumnRestriction.None) ? OptionalColumns.ElementAt(index).Title : string.Empty;
        }

        /// <summary>
        /// For layouts that have a column for all times not otherwise in columns, answers whether or not the given catclass has its own columnID.  If true, should show this in the "other" bucket, if false, should suppress
        /// </summary>
        /// <param name="OptionalColumns"></param>
        /// <param name="catClassID"></param>
        /// <returns></returns>
        public static bool ShowOtherCatClass(IEnumerable<OptionalColumn> OptionalColumns, CategoryClass.CatClassID catClassID)
        {
            if (OptionalColumns == null)
                return true;

            foreach (OptionalColumn oc in OptionalColumns)
            {
                switch (catClassID)
                {
                    default:
                        break;
                    case CategoryClass.CatClassID.AMEL:
                        if (oc.ColumnType == OptionalColumnType.AMEL)
                            return false;
                        break;
                    case CategoryClass.CatClassID.ASEL:
                        if (oc.ColumnType == OptionalColumnType.ASEL)
                            return false;
                        break;
                    case CategoryClass.CatClassID.ASES:
                        if (oc.ColumnType == OptionalColumnType.ASES)
                            return false;
                        break;
                    case CategoryClass.CatClassID.AMES:
                        if (oc.ColumnType == OptionalColumnType.AMES)
                            return false;
                        break;
                    case CategoryClass.CatClassID.Helicopter:
                        if (oc.ColumnType == OptionalColumnType.Helicopter)
                            return false;
                        break;
                    case CategoryClass.CatClassID.Glider:
                        if (oc.ColumnType == OptionalColumnType.Glider)
                            return false;
                        break;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// LogbookEntry that has additional read-only fields, optimized for display purposes vs. data interchange
    /// </summary>
    [Serializable]
    public class LogbookEntryDisplay : LogbookEntry, IHistogramable
    {
        public enum LogbookRowType { Flight, PageTotal, PreviousTotal, Subtotal, RunningTotal }

        #region properties
        /// <summary>
        /// Is the category/class overridden?
        /// </summary>
        public bool IsOverridden { get; set; }

        /// <summary>
        /// A read-only display string for custom properties (efficiency for display)
        /// </summary>
        public string CustPropertyDisplay { get; set; }

        /// <summary>
        /// Display category/class but without any type.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string CategoryClassNoType
        {
            get { return RowType == LogbookRowType.Flight ? CategoryClass.CategoryClassFromID((CategoryClass.CatClassID)EffectiveCatClass).CatClass : string.Empty; }
        }

        public string ShortModelName { get; set; }

        public string FamilyName { get; set; }

        /// <summary>
        /// The ID of the model of aircraft
        /// </summary>
        public int ModelID { get; set; }

        #region Printing support
        /// <summary>
        /// Row type for this flight.
        /// </summary>
        public LogbookRowType RowType { get; set; }

        /// <summary>
        /// For printing - true if this flight should have a page break after it
        /// </summary>
        public bool IsPageBreak { get; set; }

        /// <summary>
        /// # of flight rows needed to properly display this entry.
        /// </summary>
        public int RowHeight { get; set; }

        /// <summary>
        /// CatClassOverride in base class can be 0 if it's not overridden; this is the net category class.
        /// </summary>
        public int EffectiveCatClass { get; set; }

        /// <summary>
        /// Quick access to the PIC Name, if specified - for printing.
        /// </summary>
        public string PICName
        {
            get { return CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropNameOfPIC); }
        }

        /// <summary>
        /// Quick access to the SIC Name, if specified - for printing.
        /// </summary>
        public string SICName
        {
            get { return CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropNameOfSIC); }
        }

        /// <summary>
        /// Quick access to the SIC Name, if specified - for printing.
        /// </summary>
        public string StudentName
        {
            get { return CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropStudentName); }
        }

        public decimal PICUSTime
        {
            get { return CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropPICUS); }
        }

        public decimal SoloTime
        {
            get { return CustomProperties.TotalTimeForPredicate(fp => fp.PropertyType.IsSolo); }
        }

        public decimal IFRTime
        {
            get { return CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropIFRTime); }
        }

        public int NightTakeoffs
        {
            get { return CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTakeoff); }
        }

        public int DayTakeoffs
        {
            get
            {
                return Math.Max(CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropTakeoffAny) - NightTakeoffs, 0);
            }
        }

        /// <summary>
        /// EASA layout uses IFR time, not IMC/Simulated, so show IFR time but parenthetically add actual/simulated)
        /// </summary>
        public string InstrumentTimeDisplay
        {
            get
            {
                decimal instrumenttime = IMC + SimulatedIFR;

                if (instrumenttime == 0)
                    return string.Empty;

                return String.Format(CultureInfo.CurrentCulture, "({0} / {1})", (IMC == 0) ? "0" : IMC.FormatDecimal(UseHHMM), (SimulatedIFR == 0) ? "0" : SimulatedIFR.FormatDecimal(UseHHMM));
            }
        }

        /// <summary>
        /// Instance type used for the flight.
        /// </summary>
        public AircraftInstanceTypes InstanceType { get; set; }

        #region Glider properties
        public int GroundLaunches
        {
            get
            {
                int result = 0;
                if (CustomProperties != null)
                {
                    foreach (CustomFlightProperty cfp in CustomProperties)
                    {
                        if (cfp.PropertyType.IsGliderGroundLaunch)
                            result += cfp.IntValue;
                    }
                }
                return result;
            }
        }

        public int SelfLaunches
        {
            get { return CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropMotorgliderSelfLaunch); }
        }

        public int AeroLaunches
        {
            get { return CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropGliderTowedLaunch); }
        }

        public int MaxAltitude
        {
            get { return Math.Max(CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropMaximumAltitude), CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropGliderMaxAltitude)); }
        }

        public int ReleaseAltitude
        {
            get { return CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropGliderReleaseAltitude); }
        }

        public decimal GroundInstruction
        {
            get { return CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived); }
        }
        #endregion

        /// <summary>
        /// For totals rows, indicates the # of flights
        /// </summary>
        public int FlightCount { get; set; }

        #region Other fields for printing
        public bool IsFSTD
        {
            get { return TailNumDisplay.StartsWith(CountryCodePrefix.SimCountry.Prefix, StringComparison.OrdinalIgnoreCase); }
        }

        public decimal NightXC
        {
            get { return Math.Min(Nighttime, CrossCountry); }
        }

        public decimal DayXC
        {
            get { return Math.Max(CrossCountry - NightXC, 0.0M); }
        }

        public decimal SoloTotal { get; set; }
        public decimal PICUSTotal { get; set; }
        public decimal NightDualTotal { get; set; }
        public decimal NightPICTotal { get; set; }
        public decimal NightPICUSTotal { get; set; }
        public decimal NightSICTotal { get; set; }
        public decimal XCDualTotal { get; set; }
        public decimal XCPICTotal { get; set; }

        public decimal XCSICTotal { get; set; }
        public decimal XCNightDualTotal { get; set; }
        public decimal XCNightPICTotal { get; set; }

        public decimal XCNightSICTotal { get; set; }
        public int DayTakeoffTotal { get; set; }
        public int NightTakeoffTotal { get; set; }
        public decimal InstrumentAircraftTotal { get; set; }
        public decimal InstrumentFSTDTotal { get; set; }
        public decimal GroundInstructionTotal { get; set; }
        public decimal IFRTimeTotal { get; set; }

        public decimal[] OptionalColumnTotals { get; set; }

        /// <summary>
        /// Any additional columns to display.  Use OptionalColumnValue to get the value, or, after using AddFrom, use OptionalColumnTotalValue to retrieve the total
        /// </summary>
        public OptionalColumn[] OptionalColumns { get; set; }

        // Glider counts
        public int SelfLaunchTotal { get; set; }
        public int AeroLaunchTotal { get; set; }
        public int GroundLaunchTotal { get; set; }

        public int LandingsTotal { get; set; }

        public int NightTouchAndGoLandings { get; set; }

        /// <summary>
        /// EASA specific property to segregate day/night
        /// </summary>
        public int NetDayLandings
        {
            get { return Landings - NetNightLandings; }
        }

        /// <summary>
        /// EASA specific property estimating net night landings (FS + T&G)
        /// </summary>
        public int NetNightLandings
        {
            get
            {
                // NightTouchAndGoLandings is initialized if we have totals, but if it's zero, then we may need to look for the property
                int cTouchAndGoes = NightTouchAndGoLandings > 0 ? NightTouchAndGoLandings : CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo);
                return Math.Min(cTouchAndGoes + NightLandings, Landings);
            }
        }
        #endregion
        #endregion

        #region Display Formatting Helper
        /// <summary>
        /// Whether to format in HHMM or decimal
        /// </summary>
        public bool UseHHMM { get; set; }

        /// <summary>
        /// Whether the date of flight is assumed to be local or UTC
        /// </summary>
        public bool UseUTCDates { get; set; }

        public static string LandingDisplayForFlight(LogbookEntryBase le)
        {
            if (le == null)
                return string.Empty;

            return String.Format(CultureInfo.CurrentCulture, "{0} {1}", le.Landings.FormatInt(),
                (le.FullStopLandings > 0 && le.NightLandings > 0) ? String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.DayAndNightLandingTemplate, le.FullStopLandings, le.NightLandings) :
                (le.FullStopLandings == 0 && le.NightLandings > 0) ? String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.NightLandingTemplate, le.NightLandings) :
                (le.FullStopLandings > 0 && le.NightLandings == 0) ? String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.DayLandingTemplate, le.FullStopLandings) :
                string.Empty).Trim();
        }

        /// <summary>
        /// Shows total landings, and subtotals (as appropriate) for full-stop-day and full-stop-night
        /// </summary>
        public string LandingDisplay
        {
            get { return LandingDisplayForFlight(this); }
        }

        public static string FormatHobbs(decimal HobbsStart, decimal HobbsEnd)
        {
            if (HobbsStart > 0 || HobbsEnd > 0)
            {
                if (HobbsStart > 0.0M && HobbsEnd > 0.0M)
                    return String.Format(CultureInfo.CurrentCulture, "{3}: {0:#,#.0} {4} {1:#,#.0} ({2:#,#.0})", HobbsStart, HobbsEnd, HobbsEnd - HobbsStart, Resources.LogbookEntry.Hobbs, Resources.LogbookEntry.RangeSeparator);
                else if (HobbsStart > 0.0M)
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0:#,#.0}", HobbsStart, Resources.LogbookEntry.HobbsStart);
                else
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0:#,#.0}", HobbsEnd, Resources.LogbookEntry.HobbsEnd);
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Formats any hobbs values in a readable manner, including the total hobbs if both are present.
        /// </summary>
        public string HobbsDisplay
        {
            get { return FormatHobbs(HobbsStart, HobbsEnd); }
        }

        public static string FormatFlightTime(DateTime FlightStart, DateTime FlightEnd, bool UseUTCDates, bool UseHHMM)
        {
            if (FlightStart.HasValue() || FlightEnd.HasValue())
            {

                if (FlightStart.HasValue() && FlightEnd.HasValue())
                {
                    TimeSpan dtFlight = FlightEnd.Subtract(FlightStart);
                    return String.Format(CultureInfo.CurrentCulture, "{3}: {0} {4} {1} ({2})", FlightStart.UTCFormattedStringOrEmpty(UseUTCDates), FlightEnd.UTCFormattedStringOrEmpty(UseUTCDates), dtFlight.TotalHours.FormatDecimal(UseHHMM), Resources.LogbookEntry.FieldFlightTime, Resources.LogbookEntry.RangeSeparator);
                }
                else if (FlightStart.HasValue())
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0}", FlightStart.UTCFormattedStringOrEmpty(UseUTCDates), Resources.LogbookEntry.FieldFlightStart);
                else
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0}", FlightEnd.UTCFormattedStringOrEmpty(UseUTCDates), Resources.LogbookEntry.FieldFlightEnd);

            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Formats any flight times in a readable manner, including the total flight time if start and end are both known.
        /// </summary>
        public string FlightTimeDisplay
        {
            get { return FormatFlightTime(FlightStart, FlightEnd, UseUTCDates, UseHHMM); }
        }

        public static string FormatEngineTime(DateTime EngineStart, DateTime EngineEnd, bool UseUTCDates, bool UseHHMM)
        {
            if (EngineStart.HasValue() || EngineEnd.HasValue())
            {
                if (EngineStart.HasValue() && EngineEnd.HasValue())
                {
                    TimeSpan dtEngine = EngineEnd.Subtract(EngineStart);
                    return String.Format(CultureInfo.CurrentCulture, "{3}: {0} {4} {1} ({2})", EngineStart.UTCFormattedStringOrEmpty(UseUTCDates), EngineEnd.UTCFormattedStringOrEmpty(UseUTCDates), dtEngine.TotalHours.FormatDecimal(UseHHMM), Resources.LogbookEntry.FieldEngine, Resources.LogbookEntry.RangeSeparator);
                }
                else if (EngineStart.HasValue())
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0}", EngineStart.UTCFormattedStringOrEmpty(UseUTCDates), Resources.LogbookEntry.FieldEngineStart);
                else
                    return String.Format(CultureInfo.CurrentCulture, "{1}: {0}", EngineEnd.UTCFormattedStringOrEmpty(UseUTCDates), Resources.LogbookEntry.FieldEngineEnd);
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Formats any engine times in a readable manner, including the total engine time, if start and end are both known.
        /// </summary>
        public string EngineTimeDisplay
        {
            get { return FormatEngineTime(EngineStart, EngineEnd, UseUTCDates, UseHHMM); }
        }

        /// <summary>
        /// Determines if a given flight is collapsible.
        /// </summary>
        /// <param name="showTimes">Whether or not to show times (hobbs/flight/engine)</param>
        /// <returns></returns>
        public bool CanCollapse(bool showTimes)
        {
            return CFISignatureState != SignatureState.None || PropertiesWithReplacedApproaches.Count() > 0 || (showTimes && !String.IsNullOrEmpty(HobbsDisplay + EngineTimeDisplay + FlightTimeDisplay));
        }

        /// <summary>
        /// Convenience check for a valid signature
        /// </summary>
        public bool HasValidSig
        {
            get { return CFISignatureState == SignatureState.Valid; }
        }

        /// <summary>
        /// Indicates if a signature can be requested
        /// Basically, can request signature if it is unsigned without a pending signature, or if it is invalid.
        /// </summary>
        public bool CanRequestSig
        {
            get
            {
                return CFISignatureState == SignatureState.Invalid ||
                    (CFISignatureState == SignatureState.None && String.IsNullOrEmpty(CFIEmail) && String.IsNullOrEmpty(CFIName));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string SignatureMainLine
        {
            get { return CFIExpiration.HasValue() ?
                    String.Format(CultureInfo.CurrentCulture, Resources.SignOff.FlightSignatureTemplate, CFISignatureDate.ToShortDateString(), CFIName, CFICertificate, CFIExpiration.ToShortDateString()) :
                    String.Format(CultureInfo.CurrentCulture, Resources.SignOff.FlightSignatureTemplateNoExpiration, CFISignatureDate.ToShortDateString(), CFIName, CFICertificate); }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string SignatureCommentLine
        {
            get { return String.IsNullOrEmpty(CFIComments) ? String.Empty : String.Format(CultureInfo.InvariantCulture, "\"{0}\"", CFIComments); }
        }

        public string SignatureStatus
        {
            get
            {
                return String.Format(CultureInfo.CurrentCulture, "{0}{1}", SignatureMainLine, SignatureCommentLine);
            }
        }

        public string SignatureStateDescription
        {
            get { return (CFISignatureState == LogbookEntry.SignatureState.Valid) ? Resources.SignOff.FlightSignatureValid : Resources.SignOff.FlightSignatureInvalid; }
        }

        private string[] m_airports = null;
        public IEnumerable<string> Airports
        {
            get
            {
                if (String.IsNullOrEmpty(Route))
                    return new string[0];
                if (m_airports == null)
                    m_airports = AirportList.NormalizeAirportList(Route);
                return m_airports;
            }
        }

        /// <summary>
        /// Quick-access display of the departure airport for the flight
        /// </summary>
        public string Departure
        {
            get { return Airports.Count() > 0 ? Airports.ElementAt(0) : string.Empty; }
        }

        public string Routing
        {
            get
            {
                if (Airports.Count() <= 2)
                    return string.Empty;
                List<string> lst = new List<string>(Airports);
                lst.RemoveAt(lst.Count - 1);
                lst.RemoveAt(0);
                return string.Join(" ", lst);
            }
        }

        /// <summary>
        /// Quick-access display of the destination for the flight
        /// </summary>
        public string Destination
        {
            get { return Airports.Count() > 0 ? Airports.ElementAt(Airports.Count() - 1) : string.Empty; }
        }

        /// <summary>
        /// Quick-access display of the departure time for the flight
        /// </summary>
        public DateTime DepartureTime
        {
            get
            {
                CustomFlightProperty cfpBlockOut;
                if (FlightStart.HasValue())
                    return FlightStart;
                else if (EngineStart.HasValue())
                    return EngineStart;
                else if ((cfpBlockOut = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockOut)) != null)
                    return cfpBlockOut.DateValue;
                else
                    return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Quick-access display of the arrival time for the flight
        /// </summary>
        public DateTime ArrivalTime
        {
            get
            {
                CustomFlightProperty cfpBlockIn;
                if (FlightEnd.HasValue())
                    return FlightEnd;
                else if (EngineEnd.HasValue())
                    return EngineEnd;
                else if ((cfpBlockIn = CustomProperties.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDBlockIn)) != null)
                    return cfpBlockIn.DateValue;
                else
                    return DateTime.MinValue;
            }
        }

        /// <summary>
        /// An assigned index (e.g., "flight 54 of 288") for the flight
        /// </summary>
        public int Index { get; set; }

        public IEnumerable<ApproachDescription> ApproachDescriptions
        {
            get { return ApproachDescription.ExtractApproaches(Comment); }
        }

        /// <summary>
        /// Comment with everything after "///" removed.
        /// </summary>
        public string RedactedComment
        {
            get
            {
                int index = Comment.IndexOf("///", StringComparison.CurrentCulture);
                return (index >= 0) ? Comment.Substring(0, index) : Comment;
            }
        }

        /// <summary>
        /// Comment with approaches underlined, links highlighted, and simpel markdown applied
        /// </summary>
        public string CommentWithReplacedApproaches
        {
            get { return ApproachDescription.ReplaceApproaches(Comment.Linkify()).Trim(); }
        }

        /// <summary>
        /// Same as CommentWithReplacedApproaches except that it uses a redacted comment.
        /// </summary>
        public string RedactedCommentWithReplacedApproaches
        {
            get { return ApproachDescription.ReplaceApproaches(RedactedComment.Linkify()).Trim(); }
        }

        public IEnumerable<string> PropertiesWithReplacedApproaches
        {
            get { return CustomFlightProperty.PropDisplayAsList(CustomProperties, UseHHMM, true, true); }
        }
        #endregion
        #endregion

        #region Object creation
        public LogbookEntryDisplay()
            : base()
        {
            RowHeight = 1;
            IsOverridden = false;
            CustPropertyDisplay = string.Empty;
            ModelID = MakeModel.UnknownModel;
            RowType = LogbookRowType.Flight;
        }

        private void InitPropertiesFromDB(MySqlDataReader dr)
        {
            ModelID = Convert.ToInt32(dr["idModel"], CultureInfo.InvariantCulture);
            IsOverridden = Convert.ToBoolean(dr["IsOverridden"], CultureInfo.InvariantCulture);
            EffectiveCatClass = (IsOverridden) ? Convert.ToInt32(dr["CatClassOverride"], CultureInfo.InvariantCulture) : EffectiveCatClass = Convert.ToInt32(dr["idcategoryclass"], CultureInfo.InvariantCulture);

            CustPropertyDisplay = CustomFlightProperty.PropListDisplay(CustomProperties, UseHHMM);

            InstanceType = (AircraftInstanceTypes) Convert.ToInt32(dr["InstanceType"], CultureInfo.InvariantCulture);
            ShortModelName = (string)dr["ShortModelDisplay"];
            FamilyName = (string)dr["FamilyDisplay"];
        }

        protected LogbookEntryDisplay(MySqlDataReader dr, string szUser, bool fUseHHMM, bool fUseUTCDate)
            : base(dr, szUser)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");

            RowHeight = 1;  // takes one slot by default.
            RowType = LogbookRowType.Flight;

            UseHHMM = fUseHHMM;
            UseUTCDates = fUseUTCDate;

            InitPropertiesFromDB(dr);
        }

        public LogbookEntryDisplay(int flightID, string szUser = null, LoadTelemetryOption lto = LoadTelemetryOption.None, bool fForceLoad = false): this()
        {
            if (flightID != idFlightNew && !String.IsNullOrEmpty(szUser))
                FLoadFromDB(flightID, szUser, lto, fForceLoad, (dr) => { InitPropertiesFromDB(dr); });
        }
        #endregion

        /// <summary>
        /// Get a list of flights for the specified user, using a prepared command (prepared from LogbookEntry.QueryCommand)
        /// </summary>
        /// <param name="args">The DBHeloperCommandArgs to use, prepared from LogbookEntry.QueryCommand</param>
        /// <param name="szUser">The username being loaded - provides a security check</param>
        /// <param name="szSortExpr">Sort expression (name of a LogbookEntry property)</param>
        /// <param name="sd">The sort direction</param>
        /// <param name="fUseHHMM">Indicates whether to display times in decimal or hh:mm format</param>
        /// <param name="fUseUTCDate">Indicates whether to treat dates as UTC or as local dates</param>
        /// <returns>List of LogbookEntry objects</returns>
        public static List<LogbookEntryDisplay> GetFlightsForQuery(DBHelperCommandArgs args, string szUser, string szSortExpr, SortDirection sd, bool fUseHHMM, bool fUseUTCDate)
        {
            DBHelper dbh = new DBHelper(args);
            args.Timeout = 120; // give it up to 120 seconds.
            List<LogbookEntryDisplay> lst = new List<LogbookEntryDisplay>();
            if (!dbh.ReadRows((c) => { }, (dr) => { lst.Add(new LogbookEntryDisplay(dr, szUser, fUseHHMM, fUseUTCDate)); }))
                util.NotifyAdminEvent("Error in RefreshData", String.Format(CultureInfo.InvariantCulture, "{0}\r\n{1}\r\n{2}", szUser, dbh.LastError, args.QueryString), ProfileRoles.maskSiteAdminOnly);

            // Sort the list by the sort expression and direction
            return SortLogbook(lst, szSortExpr, sd);
        }

        /// <summary>
        /// Sorts the list using the specified property name and reflection
        /// </summary>
        /// <param name="lst">A list of LogbookEntry objects</param>
        /// <param name="szSortExpr">The name of a property on which to sort. MUST IMPLEMENT ICOMPARABLE!</param>
        /// <param name="sd">Sort direction</param>
        /// <returns>The sorted list</returns>
        public static List<LogbookEntryDisplay> SortLogbook(List<LogbookEntryDisplay> lst, string szSortExpr, SortDirection sd)
        {
            if (string.IsNullOrEmpty(szSortExpr))
                return lst;

            lst.Sort((l1, l2) =>
            {
                int dir = (sd == SortDirection.Ascending ? 1 : -1);
                int comp = dir * ((IComparable)l1.GetType().GetProperty(szSortExpr).GetValue(l1)).CompareTo(((IComparable)l2.GetType().GetProperty(szSortExpr).GetValue(l2)));

                if (comp == 0)  // subsort by date.
                {
                    // by date first
                    comp = dir * l1.Date.CompareTo(l2.Date);

                    if (comp == 0 && (l1.FlightStart.HasValue() || l2.FlightStart.HasValue()))
                        comp = dir * l1.FlightStart.CompareTo(l2.FlightStart);

                    if (comp == 0 && (l1.EngineStart.HasValue() || l2.EngineStart.HasValue()))
                        comp = dir * l1.EngineStart.CompareTo(l2.EngineStart);

                    if (comp == 0)
                        comp = dir * l1.HobbsStart.CompareTo(l2.HobbsStart);

                    if (comp == 0)
                        comp = dir * l1.FlightID.CompareTo(l2.FlightID);
                }
                return comp;
            });
            return lst;
        }

        /// <summary>
        /// Sanity check to prevent commit of LogbookEntryDisplay object - this should ALWAYS be read-only
        /// </summary>
        /// <param name="fUpdateFlightData"></param>
        /// <param name="fUpdateSignature"></param>
        /// <exception cref="MyFlightbookException">ALWAYS Throws this exception</exception>
        /// <returns></returns>
        public override bool FCommit(bool fUpdateFlightData = false, bool fUpdateSignature = false)
        {
            throw new MyFlightbookException("Attempt to commit read-only LogbookEntryDisplay object");
        }

        #region Printing support
        private void AddComboTotalsFromEntry(LogbookEntry le, LogbookEntryDisplay led)
        {
            NightDualTotal = NightDualTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, le.Dual) : led.NightDualTotal);
            NightPICTotal = NightPICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, le.PIC) : led.NightPICTotal);
            NightSICTotal = NightSICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, le.SIC) : led.NightSICTotal);
            XCDualTotal = XCDualTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.CrossCountry, le.Dual) : led.XCDualTotal);
            XCNightDualTotal = XCNightDualTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, Math.Min(le.CrossCountry, le.Dual)) : led.XCNightDualTotal);
            XCPICTotal = XCPICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.CrossCountry, le.PIC) : led.XCPICTotal);
            XCSICTotal = XCSICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.CrossCountry, le.SIC) : led.XCSICTotal);
            XCNightPICTotal = XCNightPICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, Math.Min(le.CrossCountry, le.PIC)) : led.XCNightPICTotal);
            XCNightSICTotal = XCNightSICTotal.AddMinutes(led == null || led.RowType == LogbookRowType.Flight ? Math.Min(le.Nighttime, Math.Min(le.CrossCountry, le.SIC)) : led.XCNightSICTotal);
        }

        public override void AddFrom(LogbookEntry le)
        {
            base.AddFrom(le);
            LogbookEntryDisplay led = le as LogbookEntryDisplay;
            if (led != null)
            {
                if (led.RowType == LogbookRowType.Flight)
                {
                    PICUSTotal = PICUSTotal.AddMinutes(led.PICUSTime);
                    NightPICUSTotal = NightPICUSTotal.AddMinutes(Math.Min(led.Nighttime, led.PICUSTime));
                    InstrumentAircraftTotal = InstrumentAircraftTotal.AddMinutes((led.IsFSTD ? 0 : led.IMC));
                    InstrumentFSTDTotal = InstrumentFSTDTotal.AddMinutes((led.IsFSTD ? led.SimulatedIFR : 0));
                    SoloTotal = SoloTotal.AddMinutes(led.SoloTime);
                    GroundInstructionTotal = GroundInstruction.AddMinutes(led.GroundInstruction);
                    IFRTimeTotal = IFRTimeTotal.AddMinutes(led.IFRTime);
                    NightTouchAndGoLandings += led.CustomProperties.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo);

                    SelfLaunchTotal += led.SelfLaunches;
                    AeroLaunchTotal += led.AeroLaunches;
                    GroundLaunchTotal += led.GroundLaunches;
                    LandingsTotal += led.Landings;
                    DayTakeoffTotal += led.DayTakeoffs;
                    NightTakeoffTotal += led.NightTakeoffs;
                }
                else
                {
                    PICUSTotal = PICUSTotal.AddMinutes(led.PICUSTotal);
                    NightPICUSTotal = NightPICUSTotal.AddMinutes(led.NightPICUSTotal);
                    InstrumentAircraftTotal = InstrumentAircraftTotal.AddMinutes(led.InstrumentAircraftTotal);
                    InstrumentFSTDTotal = InstrumentFSTDTotal.AddMinutes(led.InstrumentFSTDTotal);
                    SoloTotal = SoloTime.AddMinutes(led.SoloTotal);
                    GroundInstructionTotal = GroundInstruction.AddMinutes(led.GroundInstructionTotal);
                    IFRTimeTotal = IFRTimeTotal.AddMinutes(led.IFRTimeTotal);
                    NightTouchAndGoLandings += led.NightTouchAndGoLandings;

                    SelfLaunchTotal += led.SelfLaunchTotal;
                    AeroLaunchTotal += led.AeroLaunchTotal;
                    GroundLaunchTotal += led.GroundLaunchTotal;
                    LandingsTotal += led.LandingsTotal;
                    DayTakeoffTotal += led.DayTakeoffTotal;
                    NightTakeoffTotal += led.NightTakeoffTotal;
                }

                if (OptionalColumns != null)
                {
                    if (OptionalColumnTotals == null || OptionalColumnTotals.Length != OptionalColumns.Length)
                        OptionalColumnTotals = new decimal[OptionalColumns.Length];

                    for (int i = 0; i < OptionalColumns.Length; i++)
                    {
                        if (OptionalColumns[i].ValueType == OptionalColumnValueType.Integer)
                            OptionalColumnTotals[i] += led.RowType == LogbookRowType.Flight ? led.OptionalColumnValue(i) : led.OptionalColumnTotalValue(i);
                        else
                            OptionalColumnTotals[i] = OptionalColumnTotals[i].AddMinutes(led.RowType == LogbookRowType.Flight ? led.OptionalColumnValue(i) : led.OptionalColumnTotalValue(i));
                    }
                }
            }

            AddComboTotalsFromEntry(le, led);

            FlightCount++;
        }

        #region Optional Columns
        private decimal OptionalColumnTotalIfCondition(bool f)
        {
            return f ? TotalFlightTime : 0.0M;
        }

        private decimal OptionalColumnGroundSimIfType(AircraftInstanceTypes instanceType)
        {
            return instanceType == InstanceType ? GroundSim : 0.0M;
        }

        private decimal OptionalColumnPropertyTotal(OptionalColumn oc)
        {
            if (oc == null || CustomProperties == null)
                return 0.0M;
            foreach (CustomFlightProperty cfp in CustomProperties)
            {
                if (cfp.PropTypeID == oc.IDPropType)
                    return oc.ValueType == OptionalColumnValueType.Integer ? cfp.IntValue : cfp.DecValue;
            }
            return 0.0M;
        }

        /// <summary>
        /// Determines the value for an optional column from a logbookentrydisplay object
        /// </summary>
        /// <param name="columnIndex">The index of the optional column</param>
        /// <returns></returns>
        public decimal OptionalColumnValue(int columnIndex)
        {
            if (OptionalColumns == null || columnIndex < 0 || columnIndex >= OptionalColumns.Length)
                return 0.0M;

            OptionalColumn oc = OptionalColumns[columnIndex];

            switch (oc.ColumnType)
            {
                case OptionalColumnType.Complex:
                    return OptionalColumnTotalIfCondition(MakeModel.GetModel(ModelID).IsComplex);
                case OptionalColumnType.Retract:
                    return OptionalColumnTotalIfCondition(MakeModel.GetModel(ModelID).IsRetract);
                case OptionalColumnType.Tailwheel:
                    return OptionalColumnTotalIfCondition(MakeModel.GetModel(ModelID).IsTailWheel);
                case OptionalColumnType.HighPerf:
                    {
                        MakeModel m = MakeModel.GetModel(ModelID);
                        return OptionalColumnTotalIfCondition(m.PerformanceType == MakeModel.HighPerfType.HighPerf || (m.PerformanceType == MakeModel.HighPerfType.Is200HP && Date.CompareTo(Convert.ToDateTime(MakeModel.Date200hpHighPerformanceCutoverDate, CultureInfo.InvariantCulture)) < 0));
                    }
                case OptionalColumnType.TAA:
                    {
                        bool fIsTAA = MakeModel.GetModel(ModelID).AvionicsTechnology == MakeModel.AvionicsTechnologyType.TAA;
                        if (!fIsTAA)
                        {
                            UserAircraft ua = new UserAircraft(User);
                            Aircraft ac = ua.GetUserAircraftByID(AircraftID);
                            fIsTAA = ac.AvionicsTechnologyUpgrade == MakeModel.AvionicsTechnologyType.TAA && Date.CompareTo(ac.GlassUpgradeDate) > 0;
                        }
                        return OptionalColumnTotalIfCondition(fIsTAA);
                    }
                case OptionalColumnType.Jet:
                    return OptionalColumnTotalIfCondition(MakeModel.GetModel(ModelID).EngineType == MakeModel.TurbineLevel.Jet);
                case OptionalColumnType.Turbine:
                    {
                        MakeModel m = MakeModel.GetModel(ModelID);
                        return OptionalColumnTotalIfCondition(m.EngineType == MakeModel.TurbineLevel.TurboProp || m.EngineType == MakeModel.TurbineLevel.UnspecifiedTurbine || m.EngineType == MakeModel.TurbineLevel.Jet);
                    }
                case OptionalColumnType.TurboProp:
                    return OptionalColumnTotalIfCondition(MakeModel.GetModel(ModelID).EngineType == MakeModel.TurbineLevel.TurboProp);
                case OptionalColumnType.ASEL:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.ASEL);
                case OptionalColumnType.AMEL:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.AMEL);
                case OptionalColumnType.ASES:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.ASES);
                case OptionalColumnType.AMES:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.AMES);
                case OptionalColumnType.Glider:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.Glider);
                case OptionalColumnType.Helicopter:
                    return OptionalColumnTotalIfCondition(EffectiveCatClass == (int)CategoryClass.CatClassID.Helicopter);
                case OptionalColumnType.ATD:
                    return OptionalColumnGroundSimIfType(AircraftInstanceTypes.CertifiedATD);
                case OptionalColumnType.FTD:
                    return OptionalColumnGroundSimIfType(AircraftInstanceTypes.CertifiedIFRSimulator);
                case OptionalColumnType.FFS:
                    return OptionalColumnGroundSimIfType(AircraftInstanceTypes.CertifiedIFRAndLandingsSimulator);
                case OptionalColumnType.CustomProp:
                    return OptionalColumnPropertyTotal(oc);
                default:
                    return 0.0M;
            }
        }

        /// <summary>
        /// Returns an index value or a total value formatted per the settings of the optional column
        /// </summary>
        /// <param name="value">The decimal value, retrieved from somewhere (e.g., could be a total)</param>
        /// <param name="columnIndex">To which optional column does this belong?</param>
        /// <param name="fUseHHMM">Use HHMM or decimal?</param>
        /// <returns>Formatted string</returns>
        public string OptionalColumnDisplayValue(decimal value, int columnIndex)
        {
            if (OptionalColumns == null || columnIndex < 0 || columnIndex >= OptionalColumns.Length)
                return string.Empty;

            switch (OptionalColumns[columnIndex].ValueType)
            {
                case OptionalColumnValueType.Integer:
                    return ((int)value).FormatInt();
                case OptionalColumnValueType.Decimal:
                    return value.FormatDecimal(false);
                default:
                case OptionalColumnValueType.Time:
                    return value.FormatDecimal(UseHHMM);
            }
        }

        public string OptionalColumnDisplayValue(int columnIndex)
        {
            return OptionalColumnDisplayValue(OptionalColumnValue(columnIndex), columnIndex);
        }

        public decimal OptionalColumnTotalValue(int columnIndex)
        {
            return (OptionalColumnTotals == null || columnIndex < 0 || columnIndex > OptionalColumnTotals.Length) ? 0.0M : OptionalColumnTotals[columnIndex];
        }

        public string OptionalColumnTotalDisplayValue(int columnIndex, bool fUseHHMM)
        {
            UseHHMM = fUseHHMM;
            return OptionalColumnTotals == null || columnIndex < 0 || columnIndex >= OptionalColumnTotals.Length ? string.Empty : OptionalColumnDisplayValue(OptionalColumnTotals[columnIndex], columnIndex);
        }
        #endregion
        #endregion

        #region IHistogramable support
        public const string HistogramContextSelectorKey = "hgContextSelector";
        public enum HistogramSelector { TotalFlightTime, Landings, Approaches, Night, IMC, SimulatedIMC, Dual, PIC, SIC, CFI, XC, GroundSim, Flights, FlightDays}

        public IComparable BucketSelector
        {
            get { return Date; }
        }

        public double HistogramValue(IDictionary<string, object> context = null)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (!context.ContainsKey(HistogramContextSelectorKey))
                throw new MyFlightbookException("HistogramValue: context doesn't specify which field you want to sum!");

            HistogramSelector sel = (HistogramSelector)context[HistogramContextSelectorKey];

            switch (sel)
            {
                case HistogramSelector.TotalFlightTime:
                default:
                    return (double)TotalFlightTime;
                case HistogramSelector.Approaches:
                    return Approaches;
                case HistogramSelector.Landings:
                    return Landings;
                case HistogramSelector.Dual:
                    return (double)Dual;
                case HistogramSelector.Night:
                    return (double)Nighttime;
                case HistogramSelector.SimulatedIMC:
                    return (double)SimulatedIFR;
                case HistogramSelector.IMC:
                    return (double)IMC;
                case HistogramSelector.PIC:
                    return (double)PIC;
                case HistogramSelector.SIC:
                    return (double)SIC;
                case HistogramSelector.CFI:
                    return (double)CFI;
                case HistogramSelector.XC:
                    return (double)CrossCountry;
                case HistogramSelector.GroundSim:
                    return (double)GroundSim;
                case HistogramSelector.Flights:
                    return 1;
                case HistogramSelector.FlightDays:
                    {
                        const string FlightDaysContextKey = "hgFlightDays";
                        HashSet<DateTime> hs;
                        if (context.ContainsKey(FlightDaysContextKey))
                            hs = (HashSet<DateTime>)context[FlightDaysContextKey];
                        else
                            context[FlightDaysContextKey] = hs = new HashSet<DateTime>();
                        if (hs.Contains(Date))
                            return 0;
                        else
                        {
                            hs.Add(Date);
                            return 1;
                        }
                    }
            }
        }
        #endregion
    }

    /// <summary>
    /// Events for a logbookentry
    /// </summary>
    public class LogbookEventArgs : EventArgs
    {
        public int FlightID { get; set; }

        public LogbookEntry Flight {get; set;}

        /// <summary>
        /// If there is a flight to which we should navigate, this includes it.
        /// </summary>
        public int IDNextFlight { get; set; }

        public LogbookEventArgs() : base()
        {
            FlightID = IDNextFlight = LogbookEntry.idFlightNone;
            Flight = null;
        }

        public LogbookEventArgs(int idFlight, int idNextFlight = LogbookEntry.idFlightNone) : this()
        {
            FlightID = idFlight;
            IDNextFlight = idNextFlight;
        }

        public LogbookEventArgs(LogbookEntry le) : this()
        {
            Flight = le;
            if (le != null)
                FlightID = le.FlightID;
        }
    }

    /// <summary>
    /// An HTML row for a flight (used in MyFlights.aspx).
    /// </summary>
    [Serializable]
    public class FlightRow
    {
        public FlightRow()
        {
        }

        public string HTMLRowText { get; set; }
        public string FBDivID { get; set; }
    }

    /// <summary>
    /// Parses an approach description into something displayable
    /// </summary>
    public class ApproachDescription
    {
        #region properties
        /// <summary>
        /// Number of approaches performed
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Description of the approach (e.g., ILS, VOR, GPS, etc.)
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Runway to which the approch was made
        /// </summary>
        public string Runway { get; private set; }

        private string CanonicalRunway
        {
            get { return (String.IsNullOrEmpty(Runway) ? string.Empty : (Runway.StartsWith("RWY", StringComparison.CurrentCultureIgnoreCase) ? Runway : "RWY" + Runway)); }
        }

        /// <summary>
        /// Airport Identifier where the approach was performed
        /// </summary>
        public string AirportCode { get; private set; }

        public string ApproachString
        {
            get { return String.Format(CultureInfo.CurrentCulture, Count == 1 ? Resources.LogbookEntry.ApproachDescApproach : Resources.LogbookEntry.ApproachDescApproaches, Count, Description); }
        }
        #endregion

        #region Constructors
        public ApproachDescription(Match m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            Count = Convert.ToInt32(m.Groups["count"].Value, CultureInfo.CurrentCulture);
            Description = m.Groups["desc"].Value;
            Runway = m.Groups["rwy"].Value;
            AirportCode = m.Groups["airport"].Value;
        }
        #endregion

        #region Creation and parsing
        private static readonly Regex regApproach = new Regex("\\b(?<count>\\d{1,2})[-.:/ ]?(?<desc>[-a-zA-Z/]{3,}?(?:-[abcxyzABCXYZ])?)[-.:/ ]?(?:RWY)?(?<rwy>[0-3]?\\d[LRC]?)[-.:/ @](?<airport>[a-zA-Z0-9]{3,4})\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IEnumerable<ApproachDescription> ExtractApproaches(string szSource)
        {
            List<ApproachDescription> lst = new List<ApproachDescription>();

            if (szSource != null)
            {
                MatchCollection mc = regApproach.Matches(szSource);
                foreach (Match m in mc)
                    lst.Add(new ApproachDescription(m));
            }

            return lst;
        }

        public static string ReplaceApproaches(string szSource)
        {
            if (szSource == null)
                return string.Empty;

            return regApproach.Replace(szSource, (m) => { return String.Format(CultureInfo.InvariantCulture, "<span title='{0}' style=\"display:inline-block; border-bottom: 1px dotted #000; \">{1}</span>", new ApproachDescription(m).ToString(), m.Groups[0].Value); });
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ApproachDescTemplate, ApproachString, Runway, AirportCode);
        }

        public string ToCanonicalString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}@{3}", Count, Description, CanonicalRunway, AirportCode);
        }
    }

    [Serializable]
    public class PropertyDelta : IComparable
    {
        public enum ChangeType { Unchanged, Added, Deleted, Modified }

        #region Properties
        /// <summary>
        /// The display name of the property/field that was changed
        /// </summary>
        public string PropName { get; set; }

        /// <summary>
        /// The old value for the property/field, null or empty if it wasn't previously present (has been added)
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// The new value for the property/field, null or empty if it has been deleted
        /// </summary>
        public string NewValue { get; set; }

        public ChangeType Change
        {
            get
            {
                if (String.IsNullOrEmpty(OldValue) && String.IsNullOrEmpty(NewValue))
                    return ChangeType.Unchanged;
                else if (String.IsNullOrEmpty(OldValue) && !String.IsNullOrEmpty(NewValue))
                    return ChangeType.Added;
                else if (String.IsNullOrEmpty(NewValue) && !String.IsNullOrEmpty(OldValue))
                    return ChangeType.Deleted;
                else if (NewValue.Trim().CompareCurrentCulture(OldValue.Trim()) == 0)
                    return ChangeType.Unchanged;
                else
                    return ChangeType.Modified;
            }
        }
        #endregion

        #region Constructors
        public PropertyDelta()
        {
            PropName = string.Empty;
        }

        public PropertyDelta(string name, string oldVal, string newVal) : this()
        {
            PropName = name;
            OldValue = oldVal;
            NewValue = newVal;
        }
        #endregion

        public static void AddPotentialChange(string name, string oldVal, string newVal, IList<PropertyDelta> lst)
        {
            if (lst == null)
                throw new ArgumentNullException("lst");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException("name");

            PropertyDelta pd = new PropertyDelta(name, oldVal, newVal);
            if (pd.Change != ChangeType.Unchanged)
                lst.Add(pd);
        }

        public override string ToString()
        {
            switch (Change)
            {
                default:
                case ChangeType.Unchanged:
                    return string.Empty;
                case ChangeType.Added:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", Resources.LogbookEntry.CompareAdded, PropName, NewValue);
                case ChangeType.Deleted:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", Resources.LogbookEntry.CompareDeleted, PropName, OldValue);
                case ChangeType.Modified:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1}: {2} ==> {3}", Resources.LogbookEntry.CompareModified, PropName, OldValue, NewValue);
            }
        }

        public int CompareTo(object obj)
        {
            PropertyDelta pd = (PropertyDelta)obj;

            if (pd.Change == Change)
                return PropName.CompareCurrentCultureIgnoreCase(pd.PropName);
            else
                return Change.CompareTo(pd.Change);
        }
    }
}

