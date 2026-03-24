using MyFlightbook.FlightProperties.Properties;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// An actual instance of a CustomPropertyType - a flight property.  This is ALWAYS tied to a particular flight.
    /// </summary>
    [Serializable]
    public class CustomFlightProperty : IComparable<CustomFlightProperty>
    {
        private const string szDeleteBase = "DELETE FROM flightproperties WHERE idProp=?idProp";
        private const string szReplaceBase = "REPLACE INTO flightproperties SET idFlight=?idFlight, idPropType=?idProptype, intValue=?IntValue, DecValue=?DecValue, DateValue=?DateValue, StringValue=?StringValue";

        private Boolean boolValue = true;
        private CustomPropertyType m_cpt = new CustomPropertyType();

        public const int idCustomFlightPropertyNew = -1;

        #region Properties
        /// <summary>
        /// The primary key for this property
        /// </summary>
        public int PropID { get; set; }

        /// <summary>
        /// The flight with which this property is associated
        /// </summary>
        public int FlightID { get; set; }

        /// <summary>
        /// The ID of the propertytype this represents
        /// </summary>
        public int PropTypeID { get; set; }

        /// <summary>
        /// The integer value for this property, if appropriate
        /// </summary>
        public int IntValue { get; set; }

        /// <summary>
        /// The boolean value for this property, if appropriate
        /// </summary>
        public Boolean BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        /// <summary>
        /// The decimal value for this property, if appropriate
        /// </summary>
        public Decimal DecValue { get; set; }

        /// <summary>
        /// The date or date-time for this property, if appropriate
        /// </summary>
        public DateTime DateValue { get; set; }

        /// <summary>
        /// Any textual value for this property, if appropriate.
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// Return a string containing just the relevant value - read-only
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ValueString
        {
            get
            {
                switch (this.PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return boolValue ? PropertyResource.PropertyYes : PropertyResource.PropertyNo;
                    case CFPPropertyType.cfpDate:
                        return DateValue.ToShortDateString();
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.UTCFormattedStringOrEmpty(false);
                    case CFPPropertyType.cfpDecimal:
                        return DecValue.FormatDecimal(false);
                    case CFPPropertyType.cfpCurrency:
                        return DecValue.ToString("C", CultureInfo.CurrentCulture);
                    case CFPPropertyType.cfpInteger:
                        return IntValue.ToString(CultureInfo.CurrentCulture);
                    case CFPPropertyType.cfpString:
                        return (this.PropertyType.IsAllCaps) ? TextValue.ToUpper(CultureInfo.CurrentCulture) : TextValue;
                    default:
                        return string.Empty;
                }
            }
        }

        public string ValueStringFormatted(bool fHHMM)
        {
            return (fHHMM && PropertyType.Type == CFPPropertyType.cfpDecimal && !PropertyType.IsBasicDecimal) ? DecValue.ToHHMM() : ValueString;
        }

        /// <summary>
        /// Return a string containing just the relevant value - read-only, culture invariant
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ValueStringInvariant
        {
            get
            {
                switch (this.PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return boolValue ? "true" : "false";
                    case CFPPropertyType.cfpDate:
                        return DateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpDecimal:
                        return DecValue.ToString("0.0#", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpCurrency:
                        return DecValue.ToString("0.00", CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpInteger:
                        return IntValue.ToString(CultureInfo.InvariantCulture);
                    case CFPPropertyType.cfpString:
                        return (this.PropertyType.IsAllCaps) ? TextValue.ToUpperInvariant() : TextValue;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Formatted Display string (i.e., including label) for the property - read-only
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayString
        {
            get { return string.Format(CultureInfo.CurrentCulture, PropertyType.FormatString, ValueString); }
        }

        /// <summary>
        /// Formatted Display string using HHMM format, if it's a time.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayStringHHMM
        {
            get { return string.Format(CultureInfo.CurrentCulture, PropertyType.FormatString, (PropertyType.Type == CFPPropertyType.cfpDecimal && !PropertyType.IsBasicDecimal) ? DecValue.ToHHMM() : ValueString); }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "ID: {0} TypeID: {1} Val: \"{2}\"", this.PropID, this.PropTypeID, DisplayString);
        }

        /// <summary>
        /// Cached customproperty type for quick reading.  THIS IS CACHED from the property type; it doesn't get written.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public CustomPropertyType PropertyType
        {
            get { return m_cpt; }
        }

        /// <summary>
        /// Convenience method, since PropertyType is read-only (and we don't want to serialize it).
        /// </summary>
        /// <param name="cpt"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetPropertyType(CustomPropertyType cpt)
        {
            m_cpt = cpt ?? throw new ArgumentNullException(nameof(cpt));
        }

        /// <summary>
        /// Is this a new/unsaved property?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsNew
        {
            get { return PropID == idCustomFlightPropertyNew; }
        }

        /// <summary>
        /// Does this property already exist in the database?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsExisting
        {
            get { return !IsNew; }
        }

        /// <summary>
        /// Is the current value for the property the default value?
        /// </summary>
        [JsonIgnore]
        public Boolean IsDefaultValue
        {
            get
            {
                if (PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInvalid)
                    m_cpt = CustomPropertyType.GetCustomPropertyType(PropTypeID);

                switch (PropertyType.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        return !BoolValue;
                    case CFPPropertyType.cfpInteger:
                        return (IntValue == 0);
                    case CFPPropertyType.cfpCurrency:
                    case CFPPropertyType.cfpDecimal:
                        return DecValue == 0.0M;
                    case CFPPropertyType.cfpDate:
                    case CFPPropertyType.cfpDateTime:
                        return DateValue.CompareTo(DateTime.MinValue) == 0;
                    case CFPPropertyType.cfpString:
                        return String.IsNullOrWhiteSpace(TextValue);
                }
                return false;
            }
        }
        #endregion

        #region Creation
        public CustomFlightProperty()
        {
            TextValue = string.Empty;
            DateValue = DateTime.MinValue;
            IntValue = 0;
            DecValue = 0.0M;
            BoolValue = false;
            FlightID = -1;  // TODO: should be idFlightNone
            PropID = idCustomFlightPropertyNew;
            PropTypeID = (int)CustomPropertyType.KnownProperties.IDPropInvalid;
        }

        /// <summary>
        /// Creates a new custom flight property of the specified type
        /// </summary>
        /// <param name="cpt">The custompropertytype</param>
        public CustomFlightProperty(CustomPropertyType cpt) : this()
        {

            if (cpt != null)
            {
                m_cpt = cpt;
                PropTypeID = cpt.PropTypeID;
            }
        }

        protected CustomFlightProperty(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            try
            {
                object o = dr["dateValue"];
                DateValue = o != null && o != DBNull.Value && o.ToString().Length > 0
                    ? DateTime.SpecifyKind(Convert.ToDateTime(dr["DateValue"], CultureInfo.InvariantCulture), DateTimeKind.Utc)
                    : DateTime.MinValue;

                PropID = Convert.ToInt32(dr["idProp"], CultureInfo.InvariantCulture);
                FlightID = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
                PropTypeID = Convert.ToInt32(dr["idPropType"], CultureInfo.InvariantCulture);
                IntValue = Convert.ToInt32(dr["intValue"], CultureInfo.InvariantCulture);
                boolValue = (IntValue != 0);
                DecValue = Convert.ToDecimal(dr["DecValue"], CultureInfo.InvariantCulture);
                TextValue = Convert.ToString(dr["StringValue"], CultureInfo.InvariantCulture);

                // Get the propertytype values as well.
                m_cpt = new CustomPropertyType(dr);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Exception reading custom property from DR: " + ex.Message, ex, string.Empty);
            }
        }

        /// <summary>
        /// Creates an array of custom properties from a JSON array of tuples, making it efficient to pull directly from the database
        /// Each tuple consists of an array of 3 values: [0] is the property id, [1] is the property type, and [2] is the value.  Everything else is filled in.
        /// 
        /// Bad things will happen if you don't set this all up correctly - no validation is performed!
        /// </summary>
        /// <param name="szJSON">The JSON to parse</param>
        /// <param name="idFlight">The flight for which this is associated</param>
        /// <param name="ci">Culture for parsing text.  If null, invariant culture will be used</param>
        /// <param name="dtDefault">The default date time to use, if a naked time is found.</param>
        /// <returns>An array of fully-formed customflightproperties</returns>
        public static IEnumerable<CustomFlightProperty> PropertiesFromJSONTuples(string szJSON, int idFlight, DateTime? dtDefault = null, CultureInfo ci = null)
        {
            if (String.IsNullOrEmpty(szJSON))
                return Array.Empty<CustomFlightProperty>();

            SimpleFlightProperty[] rgsfp = JsonConvert.DeserializeObject<SimpleFlightProperty[]>(szJSON);
            List<CustomFlightProperty> result = new List<CustomFlightProperty>(rgsfp.Length);
            foreach (SimpleFlightProperty sfp in rgsfp)
                result.Add(sfp.ToProperty(idFlight, ci, dtDefault));

            // tuples are not sorted - sort them.
            result.Sort();
            return result;
        }

        #region Creating specific nullable properties for knownproperties
        public static CustomFlightProperty PropertyWithValue(CustomPropertyType.KnownProperties id, string value)
        {
            if (String.IsNullOrEmpty(value))
                return null;

            CustomFlightProperty cfp = new CustomFlightProperty(CustomPropertyType.GetCustomPropertyType((int)id));
            cfp.InitFromString(value);
            return (cfp.IsDefaultValue) ? null : cfp;
        }

        public static CustomFlightProperty PropertyWithValue(CustomPropertyType.KnownProperties id, int value)
        {
            return (value == 0) ? null : PropertyWithValue(id, value.ToString(CultureInfo.CurrentCulture));
        }

        public static CustomFlightProperty PropertyWithValue(CustomPropertyType.KnownProperties id, decimal value)
        {
            return (value == 0) ? null : PropertyWithValue(id, value.ToString(CultureInfo.CurrentCulture));
        }

        public static CustomFlightProperty PropertyWithValue(CustomPropertyType.KnownProperties id, bool value)
        {
            return (value) ? PropertyWithValue(id, value.ToString(CultureInfo.CurrentCulture)) : null;
        }

        public static CustomFlightProperty PropertyWithValue(CustomPropertyType.KnownProperties id, DateTime value, bool fUTC)
        {
            return (value.HasValue()) ? PropertyWithValue(id, fUTC ? value.FormatDateZulu() : value.ToString(CultureInfo.CurrentCulture)) : null;
        }
        #endregion
        #endregion

        #region Database
        public Boolean FIsValid()
        {
            return (FlightID >= 0 && PropTypeID >= 0);
        }

        public Boolean FCommit()
        {
            Boolean fResult = false;

            if (!FIsValid())
                return fResult;

            // In case the custompropertytype has not been initialized (possible, for example, from web service), initialize it now
            if (!m_cpt.IsValid() || m_cpt.PropTypeID != this.PropTypeID)
                m_cpt = new CustomPropertyType(this.PropTypeID);

            // Don't save properties that have default values; there's no value to doing so.
            if (IsDefaultValue)
                return true;

            if (m_cpt.Type == CFPPropertyType.cfpString && m_cpt.IsAllCaps)
                TextValue = TextValue.ToUpper(CultureInfo.CurrentCulture);

            DBHelper dbh = new DBHelper(string.Empty);
            dbh.DoNonQuery(
                (comm) =>
                {
                    comm.CommandText = szReplaceBase;
                    comm.Parameters.AddWithValue("idFlight", FlightID);
                    comm.Parameters.AddWithValue("idPropType", PropTypeID);

                    // Booleans and ints share the same underlying storage
                    comm.Parameters.AddWithValue("IntValue", (m_cpt.Type == CFPPropertyType.cfpBoolean) ? (boolValue ? 1 : 0) : IntValue);
                    comm.Parameters.AddWithValue("DecValue", DecValue);
                    comm.Parameters.AddWithValue("DateValue", DateValue);
                    comm.Parameters.AddWithValue("StringValue", TextValue.LimitTo(127));
                });

            PropID = (PropID >= 0) ? PropID : dbh.LastInsertedRowId; // set the property ID to the previous ID or else the newly inserted one

            return true;
        }

        public void DeleteProperty()
        {
            if (PropID >= 0)
            {
                DBHelper dbh = new DBHelper(szDeleteBase);
                if (!dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idProp", PropID); }))
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error attempting to delete property: {0} parameters - (idProp = {1}): {2}", dbh.CommandText, PropID, dbh.LastError));
            }
        }
        #endregion

        #region Admin
        /// <summary>
        /// Retrieves all of the properties that, for some reason, are empty.  CUSTOMPROPERTYTYPE IS NOT SET for performance
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomFlightProperty> ADMINEmptyProps()
        {
            List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
            DBHelper dbh = new DBHelper(@"SELECT 
    flightproperties.*,
    '' AS locformatstring,
    '' AS loctitle,
    0 AS type,
    '' AS sortkey,
    0 AS flags,
    0 AS isfavorite,
    '' AS locdescription,
    '' AS prevValues
FROM
    flightproperties
WHERE
    intvalue = 0 AND decvalue = 0.0
        AND (datevalue IS NULL
        OR YEAR(datevalue) < 10)
        AND (stringvalue = '' OR stringvalue IS NULL)");
            dbh.ReadRows((comm) => { comm.CommandTimeout = 900; },
                (dr) => { lst.Add(new CustomFlightProperty(dr)); });
            return lst;
        }
        #endregion

        /// <summary>
        /// Initializes the value from a string value.  The PropertyType MUST be set.  Throws an exception if there is a problem
        /// </summary>
        /// <param name="szVal">The string representation of the value.</param>
        /// <param name="dtDefault">The default date to assume if a datetime value is hh:mm only</param>
        public void InitFromString(String szVal, DateTime? dtDefault = null)
        {
            if (szVal == null)
                throw new ArgumentNullException(nameof(szVal));
            szVal = szVal.Trim();
            switch (PropertyType.Type)
            {
                case CFPPropertyType.cfpBoolean:
                    char ch1st = szVal.ToUpperInvariant()[0];
                    BoolValue = ch1st == 'Y' || ch1st != 'N' && Convert.ToBoolean(szVal, CultureInfo.InvariantCulture);
                    break;
                case CFPPropertyType.cfpInteger:
                    IntValue = Convert.ToInt32(szVal.SafeParseDecimal(), CultureInfo.InvariantCulture);
                    break;
                case CFPPropertyType.cfpCurrency:
                case CFPPropertyType.cfpDecimal:
                    DecValue = szVal.SafeParseDecimal();
                    break;
                case CFPPropertyType.cfpDate:
                    DateValue = DateTime.Parse(szVal, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    break;
                case CFPPropertyType.cfpDateTime:
                    DateValue = szVal.ParseUTCDateTime(dtDefault);
                    break;
                case CFPPropertyType.cfpString:
                    TextValue = szVal;
                    break;
            }
        }

        /// <summary>
        /// Initializes the custompropertytype (PropertyType) object from the specified list of properties
        /// </summary>
        /// <param name="rgcpt">The array of custompropertytype objects</param>
        public void InitPropertyType(IEnumerable<CustomPropertyType> rgcpt)
        {
            if (rgcpt == null)
                throw new ArgumentNullException(nameof(rgcpt));
            foreach (CustomPropertyType cpt in rgcpt)
            {
                if (cpt.PropTypeID == this.PropTypeID)
                {
                    m_cpt = cpt;
                    return;
                }
            }
        }

        /// <summary>
        /// Performs a pseudo-idempotency clean up to avoid duplciate properties on a flight.
        /// We can get duplicates if we have an incoming property with a propID of -1 (unknown/new)
        /// but already have a property with the same flight and proptype on the flight.
        /// The fix is to adjust these new properties to be an update of the existing property by setting the
        /// propID to the existing one.  That way, when it commits it will overwrite the existing one with the new
        /// value rather than creating a dupe.
        /// </summary>
        /// <param name="rgPropsExisting">The array of existing properties for the flight</param>
        /// <param name="rgPropsNew">The array of new properties</param>
        public static void FixUpDuplicateProperties(IEnumerable<CustomFlightProperty> rgPropsExisting, IEnumerable<CustomFlightProperty> rgPropsNew)
        {
            if (rgPropsExisting == null || rgPropsNew == null)
                return;
            if (!rgPropsExisting.Any() || !rgPropsNew.Any())
                return;

            foreach (CustomFlightProperty cfpNew in rgPropsNew)
            {
                if (cfpNew.PropID == CustomFlightProperty.idCustomFlightPropertyNew)
                {
                    foreach (CustomFlightProperty cfpExisting in rgPropsExisting)
                    {
                        if (cfpExisting.PropTypeID == cfpNew.PropTypeID)
                        {
                            cfpNew.PropID = cfpExisting.PropID;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns all events with a non-zero flag for the specified user.
        /// </summary>
        /// <param name="szUser">Username that owns the events</param>
        /// <returns>An array of matching ProfileEvent objects</returns>
        public static IEnumerable<CustomFlightProperty> GetFlaggedEvents(string szUser)
        {
            List<CustomFlightProperty> lst = new List<CustomFlightProperty>();
            DBHelper dbh = new DBHelper(@"SELECT fp.*, cp.*, '' AS LocTitle, '' AS LocFormatString, '' AS LocDescription, '' AS prevValues, 0 AS IsFavorite
FROM flights f
INNER JOIN flightproperties fp ON f.idFlight = fp.idFlight
INNER JOIN custompropertytypes cp ON fp.idPropType = cp.idPropType
WHERE f.username =?User AND (cp.Flags <> 0) AND ((fp.IntValue <> 0) OR (fp.DecValue <> 0.0) OR (cp.Type = 4))
ORDER BY f.Date Desc");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("User", szUser); }, (dr) => { lst.Add(new CustomFlightProperty(dr)); });
            return lst;
        }

        /// <summary>
        /// Return a list of previously used text property values for the specified user (for autocomplete).
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A dictionary, keyed on property type id, each containing a list of previously used strings.</returns>
        public static Dictionary<int, string[]> PreviouslyUsedTextValuesForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return null;

            const string szQ = @"SELECT cp.title, fp.idPropType AS PropTypeID, GROUP_CONCAT(DISTINCT fp.StringValue ORDER BY f.date DESC SEPARATOR '\t') AS PrevVals 
FROM Flightproperties fp
INNER JOIN CustomPropertyTypes cp ON fp.idProptype=cp.idProptype
INNER JOIN Flights f ON fp.idFlight=f.idflight
WHERE cp.Type=5 AND ((cp.flags & 0x02000000) = 0) AND f.username=?user
GROUP BY fp.idPropType;";

            DBHelper dbh = new DBHelper(szQ);
            Dictionary<int, string[]> d = new Dictionary<int, string[]>();

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { d[Convert.ToInt32(dr["PropTypeID"], CultureInfo.InvariantCulture)] = dr["PrevVals"].ToString().Split(CustomPropertyType.PreviouisTextValuesSeparator, StringSplitOptions.RemoveEmptyEntries); });

            return d;
        }

        private static void AddTotalElapsedTime(Dictionary<int, string> d, IEnumerable<CustomFlightProperty> rgprops, int propStart, int propEnd, string szFormat, bool fUseHHMM)
        {
            CustomFlightProperty cfpStart = rgprops.FirstOrDefault(cfp => cfp.PropTypeID == propStart);
            CustomFlightProperty cfpEnd = rgprops.FirstOrDefault(cfp => cfp.PropTypeID == propEnd);

            if (cfpStart != null && cfpEnd != null && cfpEnd.DateValue.CompareTo(cfpStart.DateValue) > 0)
            {
                // we have a complete period.  Coalesce them into a single summary line.
                d[propStart] = String.Empty;
                string szElapsed = ((decimal)cfpEnd.DateValue.StripSeconds().Subtract(cfpStart.DateValue.StripSeconds()).TotalHours).FormatDecimal(fUseHHMM);
                d[propEnd] = String.Format(CultureInfo.CurrentCulture, szFormat, String.Format(CultureInfo.CurrentCulture, PropertyResource.ElapsedTime, cfpStart.ValueString, cfpEnd.ValueString, szElapsed));
            }

        }

        private static Dictionary<int, string> ComputeTotals(IEnumerable<CustomFlightProperty> rgprops, bool fUseHHMM)
        {
            Dictionary<int, string> d = new Dictionary<int, string>();

            // Do Tach total and Time Totals
            CustomFlightProperty cfpTachStart = rgprops.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachStart);
            CustomFlightProperty cfpTachEnd = rgprops.FirstOrDefault(cfp => cfp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachEnd);
            if (cfpTachStart != null && cfpTachEnd != null && cfpTachEnd.DecValue - cfpTachStart.DecValue > 0)
            {
                // We have a complete tach.  Coalesce them into a single summary line.
                d[(int)CustomPropertyType.KnownProperties.IDPropTachStart] = String.Empty;
                d[(int)CustomPropertyType.KnownProperties.IDPropTachEnd] = String.Format(CultureInfo.CurrentCulture, PropertyResource.ElapsedTachSummary,
                    String.Format(CultureInfo.CurrentCulture, PropertyResource.ElapsedTime,
                    cfpTachStart.ValueString,
                    cfpTachEnd.ValueString,
                    (cfpTachEnd.DecValue - cfpTachStart.DecValue).FormatDecimal(false)));
            }

            // Coalesce the (currently 5) pairs of start/stop-time UTC properties: Duty, Flight Duty, Block, Lesson, and Schedule
            AddTotalElapsedTime(d, rgprops, (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeStart, (int)CustomPropertyType.KnownProperties.IDPropFlightDutyTimeEnd, PropertyResource.ElapsedFlightDutySummary, fUseHHMM);
            AddTotalElapsedTime(d, rgprops, (int)CustomPropertyType.KnownProperties.IDPropDutyStart, (int)CustomPropertyType.KnownProperties.IDPropDutyEnd, PropertyResource.ElapsedDutySummary, fUseHHMM); ;
            AddTotalElapsedTime(d, rgprops, (int)CustomPropertyType.KnownProperties.IDBlockOut, (int)CustomPropertyType.KnownProperties.IDBlockIn, PropertyResource.ElapsedBlockSummary, fUseHHMM);
            AddTotalElapsedTime(d, rgprops, (int)CustomPropertyType.KnownProperties.IDPropLessonStart, (int)CustomPropertyType.KnownProperties.IDPropLessonEnd, PropertyResource.ElapsedLessonSummary, fUseHHMM);
            AddTotalElapsedTime(d, rgprops, (int)CustomPropertyType.KnownProperties.IDPropScheduledDeparture, (int)CustomPropertyType.KnownProperties.IDPropScheduledArrival, PropertyResource.ElapsedScheduleSummary, fUseHHMM);

            return d;
        }

        /// <summary>
        /// Formats a collection of properties for display to the user, using current locale
        /// </summary>
        /// <param name="rgprops">An enumerable of properties.</param>
        /// <param name="fUseHHMM">Indicates if HHMM format should be used.</param>
        /// <param name="separator">Separator to use between properties - null to use the default (\r\n) for the environment.</param>
        /// <returns>A human-readable list of properties</returns>
        public static string PropListDisplay(IEnumerable<CustomFlightProperty> rgprops, bool fUseHHMM = false, string separator = null)
        {
            return String.Join(separator ?? Environment.NewLine, PropDisplayAsList(rgprops, fUseHHMM));
        }

        /// <summary>
        /// Returns an enumeration of property display strings, linkifying and replacing approaches as needed
        /// </summary>
        /// <param name="rgprops">The input properties</param>
        /// <param name="fUseHHMM">Indicates if HHMM format should be used</param>
        /// <param name="fLinkify">True to highlight links</param>
        /// <param name="fReplaceApproaches">True to highlight approaches</param>
        /// <returns></returns>
        public static IEnumerable<string> PropDisplayAsList(IEnumerable<CustomFlightProperty> rgprops, bool fUseHHMM, bool fLinkify = false, bool fReplaceApproaches = false, HashSet<int> hsExclusion = null)
        {
            List<string> lst = new List<string>();

            // short-circuit empty properties
            if (rgprops == null || !rgprops.Any())
                return lst;

            Dictionary<int, string> d = ComputeTotals(rgprops, fUseHHMM);

            // Issue #1383 - pull all of the ranges to the top.
            foreach (int proptypeID in d.Keys)
            {
                string s = d[proptypeID];
                if (!String.IsNullOrEmpty(s))
                    lst.Add(s);
            }

            foreach (CustomFlightProperty cfp in rgprops)
            {
                if (d.ContainsKey(cfp.PropTypeID) || (hsExclusion?.Contains(cfp.PropTypeID) ?? false))
                    continue;
                // if this has been coalesced into the dictionary, use that; otherwise, use the display string.
                string sz = (fUseHHMM ? cfp.DisplayStringHHMM : cfp.DisplayString);
                if (String.IsNullOrEmpty(sz))
                    continue;
                if (fLinkify)
                    sz = sz.Linkify();
                if (fReplaceApproaches)
                    sz = ApproachDescription.ReplaceApproaches(sz);
                lst.Add(sz);
            }

            return lst;
        }

        public int CompareTo(CustomFlightProperty other)
        {
            if (other == null) return 1;
            if (PropertyType == null || other.PropertyType == null)
                return 0;   // don't compare if we don't have a formatstring.
            return PropertyType.SortKey.CompareTo(other.PropertyType.SortKey);
        }
    }

}
