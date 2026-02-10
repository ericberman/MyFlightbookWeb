using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{

    /// <summary>
    /// Contains utility and admin functions for working with aircraft.
    /// </summary>
    public static class AircraftUtility
    {
        private const string keyLastTail = "LastTail"; // the id of the aircraft for the most recently entered flight.

        static public int LastTail
        {
            get { return util.RequestContext.GetCookie(keyLastTail).SafeParseInt(Aircraft.idAircraftUnknown); }

            set
            {
                if (value <= 0)
                    util.RequestContext.RemoveCookie(keyLastTail);
                else
                    util.RequestContext.SetCookie(keyLastTail, value.ToString(CultureInfo.InvariantCulture), DateTime.Now.AddYears(5));
            }
        }

        // Per https://www.faa.gov/licenses_certificates/aircraft_certification/aircraft_registry/forming_nnumber, N1-N99 are allowed but reserved by FAA.  Explicitly allow these.
        public const string RegexValidTail = "^([a-zA-Z0-9]+-?[a-zA-Z0-9]+-?[a-zA-Z0-9]+)|(N[1-9]\\d?)$";
        public static readonly LazyRegex rValidTail = new LazyRegex(RegexValidTail, RegexOptions.IgnoreCase);
        public static readonly LazyRegex rValidNNumber = new LazyRegex("^(N[^inoINO0][^ioIO]+)|(N[1-9]\\d?)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Return the link to edit an aircraft, optionally including an admin link.
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft</param>
        /// <param name="fAdmin">True for the link to be admin-only</param>
        /// <returns></returns>
        public static string EditLink(int idAircraft, bool fAdmin = false)
        {
            return String.Format(CultureInfo.InvariantCulture, "~/mvc/Aircraft/edit/{0}", idAircraft).ToAbsolute() + (fAdmin ? "?a=1" : string.Empty);
        }

        public static decimal HighWaterMarkHobbsForUserInAircraft(int idAircraft, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (idAircraft <= 0)
                return 0.0M;

            decimal val = 0.0M;
            DBHelper dbh = new DBHelper(@"SELECT  MAX(f.hobbsEnd) AS highWater FROM (SELECT hobbsEnd FROM flights WHERE username = ?user AND idaircraft = ?id ORDER BY flights.date DESC LIMIT 10) f");
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("id", idAircraft);
            },
            (dr) =>
            {
                val = Convert.ToDecimal(util.ReadNullableField(dr, "highWater", 0.0), CultureInfo.InvariantCulture);
            });
            return val;
        }

        public static decimal HighWaterMarkTachForUserInAircraft(int idAircraft, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (idAircraft <= 0)
                return 0.0M;

            decimal val = 0.0M;
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT MAX(tach.decvalue) AS highWater FROM
(SELECT decvalue FROM flightproperties fp INNER JOIN flights f ON fp.idflight = f.idflight WHERE f.username = ?user AND f.idaircraft = ?id AND fp.idproptype = {0}
ORDER BY f.date DESC LIMIT 10) tach", (int)CustomPropertyType.KnownProperties.IDPropTachEnd));
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("id", idAircraft);
            },
            (dr) =>
            {
                val = Convert.ToDecimal(util.ReadNullableField(dr, "highWater", 0.0), CultureInfo.InvariantCulture);
            });
            return val;
        }

        public static decimal HighWaterMarkFlightMeter(int idAircraft, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (idAircraft <= 0)
                return 0.0M;

            decimal val = 0.0M;
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT MAX(meter.decvalue) AS highWater FROM
(SELECT decvalue FROM flightproperties fp INNER JOIN flights f ON fp.idflight = f.idflight WHERE f.username = ?user AND f.idaircraft = ?id AND fp.idproptype = {0}
ORDER BY f.date DESC LIMIT 10) meter", (int)CustomPropertyType.KnownProperties.IDPropFlightMeterEnd));
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("id", idAircraft);
            },
            (dr) =>
            {
                val = Convert.ToDecimal(util.ReadNullableField(dr, "highWater", 0.0), CultureInfo.InvariantCulture);
            });
            return val;
        }


        #region Import WebMethods
        /// <summary>
        /// Adds the specified aircraft (by ID) to the specified user's account.
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="aircraftID"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddExistingAircraftForUser(string szUser, int aircraftID)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentNullException(nameof(szUser));

            UserAircraft ua = new UserAircraft(szUser);
            Aircraft ac = new Aircraft(aircraftID);
            if (ac.AircraftID == Aircraft.idAircraftUnknown)
                return;
            ua.FAddAircraftForUser(ac);
        }

        /// <summary>
        /// Adds a new aircraft to the user's account
        /// </summary>
        /// <param name="szUser">The user's username</param>
        /// <param name="spec">The specification for the new aircraft</param>
        /// <param name="d">A mapping dictionary, between the model specified and the matched model</param>
        /// <returns>An updated model mapping</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MyFlightbookValidationException"></exception>
        public static IDictionary<string, AircraftImportSpec> AddNewAircraft(string szUser, AircraftImportSpec spec, IDictionary<string, AircraftImportSpec> d)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (string.IsNullOrEmpty(spec.TailNum))
                throw new ArgumentException("Missing tail in AddNewAircraft");

            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Dictionary<string, AircraftImportSpec> dModelMapping = (Dictionary<string, AircraftImportSpec>)(d ?? new Dictionary<string, AircraftImportSpec>());

            Aircraft ac = new Aircraft() { TailNumber = spec.TailNum, ModelID = spec.ModelID, InstanceTypeID = spec.InstanceType };

            // Issue #296: allow sims to come through without a sim prefix; we can fix it at AddNewAircraft time.
            AircraftInstance aic = AircraftInstance.GetInstanceTypes().FirstOrDefault(it => it.InstanceTypeInt == spec.InstanceType);
            string szSpecifiedTail = spec.TailNum;
            bool fIsNamedSim = !aic.IsRealAircraft && !spec.TailNum.ToUpper(CultureInfo.CurrentCulture).StartsWith(CountryCodePrefix.SimCountry.Prefix.ToUpper(CultureInfo.CurrentCulture), StringComparison.CurrentCultureIgnoreCase);
            if (fIsNamedSim)
                ac.TailNumber = CountryCodePrefix.SimCountry.Prefix;

            if (ac.FixTailAndValidate())
            {
                ac.CommitForUser(szUser);

                UserAircraft ua = new UserAircraft(szUser);
                if (fIsNamedSim)
                    ac.PrivateNotes = String.Format(CultureInfo.InvariantCulture, "{0} #ALT{1}#", ac.PrivateNotes ?? string.Empty, szSpecifiedTail);

                ua.FAddAircraftForUser(ac);
                ua.InvalidateCache();

                if (!String.IsNullOrEmpty(spec.ProposedModel))
                {
                    spec.AircraftID = ac.AircraftID;
                    dModelMapping[spec.Key] = spec;
                }
            }
            else
                throw new MyFlightbookValidationException(ac.ErrorString);

            return dModelMapping;
        }

        /// <summary>
        /// Adds a new aircraft to the user's account
        /// </summary>
        /// <param name="szUser">The user's username</param>
        /// <param name="szTail">Desired tailnumber</param>
        /// <param name="idModel">Model for the aircraft</param>
        /// <param name="instanceType">Instance type for the aircraft</param>
        /// <param name="szModelGiven">Model as given for the aircraft</param>
        /// <param name="szJsonMapping">A mapping dictionary, if present, in JSON between model string and matched model</param>
        /// <returns>An updated model mapping, as a JSON string</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MyFlightbookValidationException"></exception>
        public static string AddNewAircraft(string szUser, string szTail, int idModel, int instanceType, string szModelGiven, string szJsonMapping)
        {
            IDictionary<string, AircraftImportSpec> dModelMapping = String.IsNullOrEmpty(szJsonMapping) ? new Dictionary<string, AircraftImportSpec>() : JsonConvert.DeserializeObject<Dictionary<string, AircraftImportSpec>>(szJsonMapping);

            dModelMapping = AddNewAircraft(szUser, new AircraftImportSpec() { TailNum = szTail, ModelID = idModel, InstanceType = instanceType, ProposedModel = szModelGiven }, dModelMapping);
            return JsonConvert.SerializeObject(dModelMapping);
        }

        /// <summary>
        /// Validates
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="szTail">Tail number for the aircraft</param>
        /// <param name="idModel">Model for the aircraft</param>
        /// <param name="instanceType">Instance type for the aircraft</param>
        /// <returns>Any error - empty indicates success</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ValidateAircraft(string szUser, string szTail, int idModel, int instanceType)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentNullException(nameof(szUser));

            if (string.IsNullOrEmpty(szTail))
                throw new ArgumentException("Empty tail in ValidateAircraft");

            // Issue #296: allow sims to come through without a sim prefix; we can fix it at AddNewAircraft time.
            AircraftInstance aic = AircraftInstance.GetInstanceTypes().FirstOrDefault(it => it.InstanceTypeInt == instanceType);
            if (!aic.IsRealAircraft && !szTail.ToUpper(CultureInfo.CurrentCulture).StartsWith(CountryCodePrefix.SimCountry.Prefix.ToUpper(CultureInfo.CurrentCulture), StringComparison.CurrentCultureIgnoreCase))
                szTail = CountryCodePrefix.SimCountry.Prefix;

            Aircraft ac = new Aircraft() { TailNumber = szTail, ModelID = idModel, InstanceTypeID = instanceType };

            ac.FixTailAndValidate();
            return ac.ErrorString;
        }

        /// <summary>
        /// Migrates flights in one aircraft to another.  Was an admin function once, now happens automatically more than manually.
        /// </summary>
        /// <param name="szUser">User name</param>
        /// <param name="acSrc">Source aircraft</param>
        /// <param name="acTarget">Target aircraft</param>
        /// <returns>The number of flights migrated</returns>
        public static int MigrateFlightsForUser(string szUser, Aircraft acSrc, Aircraft acTarget)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (acSrc == null)
                throw new ArgumentNullException(nameof(acSrc));
            if (acTarget == null)
                throw new ArgumentNullException(nameof(acTarget));
            if (acSrc.AircraftID < 0)
                throw new InvalidOperationException("Source aircraft is not valid");
            if (acTarget.AircraftID < 0)
                throw new InvalidOperationException("Target aircraft is not valid");
            if (acSrc.AircraftID == acTarget.AircraftID)
                throw new InvalidOperationException("Source and target aircraft are identical");

            DBHelper dbh = new DBHelper("UPDATE flights SET idAircraft=?targ WHERE username=?user AND idAircraft=?src");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("targ", acTarget.AircraftID);
                comm.Parameters.AddWithValue("src", acSrc.AircraftID);
            });
            FlightResultManager.InvalidateForUser(szUser);
            return dbh.AffectedRowCount;
        }


        /// <summary>
        /// Suggests completions for the specified prefix text
        /// </summary>
        /// <param name="szUser">The user's name</param>
        /// <param name="prefixText">Prefix text</param>
        /// <param name="count">Max # of results to return</param>
        /// <param name="fRegisteredOnly">If true, filters out any models that are sim-only or sim/anonymous-only</param>
        /// <returns>Matching completions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<IDictionary<string, object>> SuggestFullModelsWithTargets(string szUser, string prefixText, int count, bool fRegisteredOnly = false)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new ArgumentNullException(nameof(szUser));

            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<IDictionary<string, object>>();

            ModelQuery modelQuery = new ModelQuery() { FullText = prefixText.Replace("-", "*"), PreferModelNameMatch = true, Skip = 0, Limit = count };
            List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();
            foreach (MakeModel mm in MakeModel.MatchingMakes(modelQuery))
            {
                if (!fRegisteredOnly || mm.AllowedTypes == AllowedAircraftTypes.Any)
                    lst.Add(new Dictionary<string, object>() { { "label", mm.DisplayName }, { "value", mm.MakeModelID } });
            }
            return lst;
        }

        private static string[] DoSuggestion(string szQ, string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText) || string.IsNullOrEmpty(szQ) || prefixText.Length <= 2)
                return Array.Empty<string>();

            return util.GetKeysFromDB(String.Format(CultureInfo.InvariantCulture, szQ, util.keyColumn, count), prefixText);
        }

        public static IEnumerable<string> ModelsMatchingPrefix(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT model AS {0} FROM models WHERE REPLACE(model, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }

        public static IEnumerable<string> TypeRatingsMatchingPrefix(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT typename AS {0} FROM models WHERE REPLACE(typename, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }
        #endregion

    }

}