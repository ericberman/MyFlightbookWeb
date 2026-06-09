using MyFlightbook.Web.Sharing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{

    /// <summary>
    /// Represents a stored query.
    /// </summary>
    [Serializable]
    public class CannedQuery : FlightQuery, IComparable
    {
        const string szUserQueriesKey = "queriesForUser";

        #region Properties
        /// <summary>
        /// Name of the query.  Not serialized in the JSON itself because there's not need to do so (and screws up IsDefault hack anyhow)
        /// </summary>
        [JsonIgnore]
        public string QueryName { get; set; }

        /// <summary>
        /// Optional string representation for flightcoloring
        /// </summary>
        public string ColorString { get; set; }
        #endregion

        #region Constructors
        public CannedQuery() : base()
        {
            QueryName = string.Empty;
        }

        public CannedQuery(string szName) : base(szName)
        {
            QueryName = string.Empty;
        }

        public CannedQuery(FlightQuery fq, string szName) : base(fq)
        {
            QueryName = szName;
        }
        #endregion

        #region Overrides
        protected override FlightQuery Clone()
        {
            return new CannedQuery(this, QueryName);
        }

        public override bool IsDefault
        {
            get
            {
                // Use base IsDefault, but colorstring shouldn't factor if otherwise default.
                string szColor = ColorString;
                ColorString = null;
                bool fResult = base.IsDefault;
                ColorString = szColor;
                return fResult;
            }
        }
        #endregion

        #region database
        /// <summary>
        /// Retrieve the canned queries for a given user
        /// </summary>
        /// <param name="szUser">Username for whom to retrieve</param>
        /// <returns>A set of canned queries.</returns>
        public static IEnumerable<CannedQuery> QueriesForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            if (String.IsNullOrWhiteSpace(szUser))
                return Array.Empty<CannedQuery>();

            Profile pf = Profile.GetUser(szUser);
            List<CannedQuery> lst = (List<CannedQuery>) pf.CachedObject(szUserQueriesKey);
            if (lst != null)
                return lst;
            else
                lst = new List<CannedQuery>();

            // Important - preserve "ORDER BY name ASC" so that multiple canned queries can work for flight coloring.
            DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT * FROM storedQueries WHERE username=?uname ORDER BY name ASC");
            dba.AddWithValue("uname", szUser);
            DBHelper dbh = new DBHelper(dba);
            dbh.ReadRows((comm) => { }, (dr) =>
            {
                CannedQuery cq = JsonConvert.DeserializeObject<CannedQuery>((string)dr["queryjson"], new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
                cq.QueryName = (string)dr["name"];
                lst.Add(cq);
            });
            pf.AssociatedData[szUserQueriesKey] = lst;    // cache 'em!
            return lst;
        }

        private void FlushCachedQueries()
        {
            Profile.GetUser(UserName).AssociatedData.Remove(szUserQueriesKey);
        }

        /// <summary>
        /// Saves this cannedquery to the database
        /// </summary>
        public void Commit(bool fPreserveColor = true)
        {
            if (String.IsNullOrWhiteSpace(QueryName) || IsDefault)
                return;

            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookValidationException("No username provided for canned query!");

            if (fPreserveColor && String.IsNullOrWhiteSpace(ColorString))
            {
                List<CannedQuery> lst = new List<CannedQuery>(CannedQuery.QueriesForUser(UserName));
                CannedQuery cq = lst.FirstOrDefault(cq2 => cq2.QueryName.CompareCurrentCultureIgnoreCase(QueryName) == 0);
                if (cq != null)
                    ColorString = cq.ColorString;
            }

            DBHelperCommandArgs dba = new DBHelperCommandArgs("REPLACE INTO storedQueries SET username=?uname, name=?qname, queryjson=?fqjson");
            dba.AddWithValue("uname", UserName);
            dba.AddWithValue("qname", QueryName);
            dba.AddWithValue("fqjson", ToJSONString());
            new DBHelper(dba).DoNonQuery();

            FlushCachedQueries();
        }

        public void Delete()
        {
            DBHelperCommandArgs dba = new DBHelperCommandArgs("DELETE FROM storedQueries WHERE username=?uname AND name=?qname");
            dba.AddWithValue("uname", UserName);
            dba.AddWithValue("qname", QueryName);
            new DBHelper(dba).DoNonQuery();
            FlushCachedQueries();

            // Delete any sharekeys that reference this too
            ShareKey.DeleteForQueryName(UserName, QueryName);
        }

        /// <summary>
        /// Returns the specified named query for the specified named user, or null if not found.  Case sensitive.
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CannedQuery QueryForUser(string szUser, string name)
        {
            if (String.IsNullOrEmpty(name))
                return null;
            return new List<CannedQuery>(QueriesForUser(szUser)).FirstOrDefault(cq => cq.QueryName.CompareCurrentCulture(name) == 0);
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            CannedQuery cq = obj as CannedQuery;
            return (UserName + QueryName).CompareCurrentCultureIgnoreCase(cq.UserName + cq.QueryName);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1285204636;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(QueryName);
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is null || !(obj is CannedQuery))
                return false;

            return CompareTo(obj) == 0;
        }

        public static bool operator ==(CannedQuery left, CannedQuery right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(CannedQuery left, CannedQuery right)
        {
            return !(left == right);
        }

        public static bool operator <(CannedQuery left, CannedQuery right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CannedQuery left, CannedQuery right)
        {
            return 
                left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CannedQuery left, CannedQuery right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CannedQuery left, CannedQuery right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #region Matching LogbookEntry Objects
        #region Handling conjuctives
        private static void AddResultIfCondition(List<bool> lst, bool condition, bool result)
        {
            if (condition)
                lst.Add(result);
        }

        private static bool TestConditionsForConjunction(GroupConjunction groupConjunction, List<bool> lst)
        {
            // empty list is always a match
            if (lst == null || lst.Count == 0)
                return true;

            // count the number of true elements
            int cTrue = 0;
            lst.ForEach((b) => { if (b) cTrue++; });

            switch (groupConjunction)
            {
                case GroupConjunction.All:
                    return cTrue == lst.Count;
                case GroupConjunction.Any:
                    return cTrue > 0;
                case GroupConjunction.None:
                    return cTrue == 0;
                default:
                    throw new InvalidOperationException("Unknown conjunction: " + groupConjunction.ToString());
            }
        }
        #endregion

        private bool IsDateMatch(LogbookEntryCore le)
        {
            switch (DateRange)
            {
                case DateRanges.AllTime:
                    return true;
                case DateRanges.Tailing6Months:
                    return le.Date.CompareTo(DateTime.Now.AddMonths(-6)) >= 0;
                case DateRanges.Trailing12Months:
                    return le.Date.CompareTo(DateTime.Now.AddYears(-1)) >= 0;
                case DateRanges.Trailing30:
                    return le.Date.CompareTo(DateTime.Now.AddDays(-30)) >= 0;
                case DateRanges.Trailing90:
                    return le.Date.CompareTo(DateTime.Now.AddDays(-90)) >= 0;
                case DateRanges.YTD:
                    return le.Date.CompareTo(dtStartOfYear) >= 0;
                case DateRanges.ThisMonth:
                    return le.Date.CompareTo(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)) >= 0;
                case DateRanges.PrevMonth:
                    DateTime dtPrevMonthEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                    DateTime dtPrevMonthStart = new DateTime(dtPrevMonthEnd.Year, dtPrevMonthEnd.Month, 1);
                    return le.Date.CompareTo(dtPrevMonthStart.Date) >= 0 && le.Date.Date.CompareTo(dtPrevMonthEnd.Date) <= 0;
                case DateRanges.PrevYear:
                    DateTime dtPrevYearStart = new DateTime(DateTime.Now.Year - 1, 1, 1);
                    DateTime dtPrevYearEnd = new DateTime(DateTime.Now.Year - 1, 12, 31);
                    return le.Date.CompareTo(dtPrevYearStart.Date) >= 0 && le.Date.Date.CompareTo(dtPrevYearEnd.Date) <= 0;
                case DateRanges.Custom:
                    bool fHasStartDate = DateMin.HasValue();
                    bool fHasEndDate = DateMax.HasValue();
                    if (!fHasStartDate && fHasEndDate)
                        return le.Date.Date.CompareTo(DateMax.Date) <= 0;
                    else if (fHasStartDate & !fHasEndDate)
                        return le.Date.Date.CompareTo(DateMin.Date) >= 0;
                    else
                        return le.Date.Date.CompareTo(DateMin.Date) >= 0 && le.Date.Date.CompareTo(DateMax.Date) <= 0;
                default:
                    throw new MyFlightbookValidationException("Unknown date range!");
            }
        }

        private bool IsAircraftMatch(LogbookEntryCore le)
        {
            return AircraftList.Count == 0 || (AircraftList.FirstOrDefault(ac => ac.AircraftID == le.AircraftID) != null);
        }

        public bool IsAirportMatch(LogbookEntryCore le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (String.IsNullOrWhiteSpace(le.Route) && (Distance != FlightDistance.AllFlights || AirportList.Count != 0))
                return false;

            string szRoute = le.Route.ToUpper(CultureInfo.CurrentCulture).Trim();

            switch (Distance)
            {
                case FlightDistance.AllFlights:
                    break;
                case FlightDistance.LocalOnly:
                    if (!RegexUtility.LocalFlight.IsMatch(szRoute))
                        return false;
                    break;
                case FlightDistance.NonLocalOnly:
                    if (RegexUtility.LocalFlight.IsMatch(szRoute))
                        return false;
                    break;
                default:
                    break;
            }

            foreach (string szAirport in AirportList)
            {
                if (String.IsNullOrWhiteSpace(szAirport))
                    continue;

                string szTerm = szAirport.ToUpper(CultureInfo.CurrentCulture).Replace(SearchFullStopAnchor, string.Empty);
                string szAltTerm = Airports.airport.USPrefixConvenienceAlias(szTerm);

                if (szAirport.StartsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase) && !szRoute.StartsWith(szTerm) && !szRoute.StartsWith(szAltTerm))
                    return false;
                else if (szAirport.EndsWith(SearchFullStopAnchor, StringComparison.CurrentCultureIgnoreCase) && !szRoute.EndsWith(szTerm) && !szRoute.EndsWith(szAltTerm))
                    return false;
                else if (!szRoute.Contains(szTerm) && !szRoute.Contains(szAltTerm))
                    return false;
            }

            return true;
        }

        private bool IsModelMatch(LogbookEntry le)
        {
            if (MakeList.Count != 0 && MakeList.FirstOrDefault(mm => le.ModelID == mm.MakeModelID) == null)
                return false;

            string[] rgModelFragment = RegexUtility.ModelFragmentBoundary.Split(ModelName);
            foreach (string sz in rgModelFragment)
            {
                string szSearch = sz.Trim().ToUpper(CultureInfo.CurrentCulture);
                bool fICAO = szSearch.StartsWith(szICAOPrefix, StringComparison.CurrentCultureIgnoreCase);
                if (fICAO)
                    szSearch = szSearch.Substring(szICAOPrefix.Length).Trim();
                if (szSearch.Length > 0)
                {
                    // ICAO has to match the icao, exactly
                    if (fICAO && le.FamilyName.CompareCurrentCultureIgnoreCase(szSearch) != 0)
                        return false;
                    // Otherwise, must contain the words
                    if (!le.FamilyName.Contains(szSearch) && !le.ModelDisplay.ToUpper(CultureInfo.CurrentCulture).Contains(szSearch))
                        return false;
                }
            }

            if (TypeNames.Count != 0)
            {
                // Need to get the actual model to do this.
                MakeModel mm = MakeModel.GetModel(le.ModelID);
                if (String.IsNullOrEmpty(TypeNames.ElementAt(0)) && !String.IsNullOrWhiteSpace(mm.TypeName))
                    return false;

                foreach (string szType in TypeNames)
                    if (!String.IsNullOrWhiteSpace(szType) && !mm.TypeName.ToUpperInvariant().Contains(szType.ToUpperInvariant()))
                        return false;
            }

            return true;
        }

        private bool IsInstanceTypeMatch(Aircraft ac)
        {
            switch (AircraftInstanceTypes)
            {
                default:
                case AircraftInstanceRestriction.AllAircraft:
                    break;
                case AircraftInstanceRestriction.RealOnly:
                    if (ac.InstanceType != MyFlightbook.AircraftInstanceTypes.RealAircraft)
                        return false;
                    break;
                case AircraftInstanceRestriction.TrainingOnly:
                    if (ac.InstanceType == MyFlightbook.AircraftInstanceTypes.RealAircraft)
                        return false;
                    break;
            }
            return true;
        }

        private bool IsEngineTypeMatch(MakeModel mm, LogbookEntry le)
        {
            if (CategoryClass.IsPowered(le.IsOverridden ? (CategoryClass.CatClassID)le.CatClassOverride : mm.CategoryClassID) || mm.IsMotorGlider)
            {
                switch (EngineType)
                {
                    default:
                    case EngineTypeRestriction.AllEngines:
                        return true;
                    case EngineTypeRestriction.AnyTurbine:
                        return mm.EngineType == MakeModel.TurbineLevel.Jet || mm.EngineType == MakeModel.TurbineLevel.UnspecifiedTurbine || mm.EngineType == MakeModel.TurbineLevel.TurboProp;
                    case EngineTypeRestriction.Piston:
                        return mm.EngineType == MakeModel.TurbineLevel.Piston;
                    case EngineTypeRestriction.Turboprop:
                        return mm.EngineType == MakeModel.TurbineLevel.TurboProp;
                    case EngineTypeRestriction.Jet:
                        return mm.EngineType == MakeModel.TurbineLevel.Jet;
                    case EngineTypeRestriction.Electric:
                        return mm.EngineType == MakeModel.TurbineLevel.Electric;
                }
            }
            else
                return EngineType == EngineTypeRestriction.AllEngines;  // if unpowered, then it's not a match unless "allengines" is specified; issue #1305
        }

        private bool IsAircraftCharacteristicsMatch(LogbookEntry le)
        {
            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID);

            if (!IsInstanceTypeMatch(ac))
                return false;

            MakeModel mm = MakeModel.GetModel(ac.ModelID);

            List<bool> lst = new List<bool>();
            AddResultIfCondition(lst, IsTailwheel, mm.IsTailWheel); 
            AddResultIfCondition(lst, IsHighPerformance, mm.IsHighPerf || (mm.Is200HP && le.Date.CompareTo(Convert.ToDateTime(MakeModel.Date200hpHighPerformanceCutoverDate, CultureInfo.InvariantCulture)) < 0));
            AddResultIfCondition(lst, IsGlass, mm.AvionicsTechnology != MakeModel.AvionicsTechnologyType.None || (ac.AvionicsTechnologyUpgrade != MakeModel.AvionicsTechnologyType.None && le.Date.CompareTo(ac.GlassUpgradeDate ?? DateTime.MinValue) >= 0));
            AddResultIfCondition(lst, IsTechnicallyAdvanced, mm.AvionicsTechnology == MakeModel.AvionicsTechnologyType.TAA || (ac.AvionicsTechnologyUpgrade == MakeModel.AvionicsTechnologyType.TAA && le.Date.CompareTo(ac.GlassUpgradeDate ?? DateTime.MinValue) >= 0));
            AddResultIfCondition(lst, IsComplex, mm.IsComplex);
            AddResultIfCondition(lst,IsMotorglider, mm.IsMotorGlider);
            AddResultIfCondition(lst, IsMultiEngineHeli, mm.IsMultiEngineHelicopter);
            AddResultIfCondition(lst, HasFlaps, mm.HasFlaps);
            AddResultIfCondition(lst, IsConstantSpeedProp, mm.IsConstantProp);
            AddResultIfCondition(lst, IsRetract, mm.IsRetract);

            if (!TestConditionsForConjunction(GroupConjunction.All, lst))
                return false;

            if (!IsEngineTypeMatch(mm, le))
                return false;

            // Do category class here, since we have aircraft already loaded
            if (CatClasses != null && CatClasses.Count != 0 && !CatClasses.Contains(CategoryClass.CategoryClassFromID(le.IsOverridden ? (CategoryClass.CatClassID)le.CatClassOverride : mm.CategoryClassID)))
                return false;

            return true;
        }

        private bool IsFlightCharacteristicsMatch(LogbookEntryBase le)
        {
            List<bool> lst = new List<bool>();
            AddResultIfCondition(lst, HasNightLandings, le.NightLandings != 0);
            AddResultIfCondition(lst, HasFullStopLandings, le.FullStopLandings != 0);
            AddResultIfCondition(lst, HasLandings, le.Landings != 0);
            AddResultIfCondition(lst, HasApproaches, le.Approaches != 0);
            AddResultIfCondition(lst, HasHolds, le.fHoldingProcedures);
            AddResultIfCondition(lst, HasXC, le.CrossCountry != 0);
            AddResultIfCondition(lst, HasSimIMCTime, le.SimulatedIFR != 0);
            AddResultIfCondition(lst, HasGroundSim, le.GroundSim != 0);
            AddResultIfCondition(lst, HasIMC, le.IMC != 0);
            AddResultIfCondition(lst, HasAnyInstrument, le.IMC + le.SimulatedIFR > 0);
            AddResultIfCondition(lst, HasNight, le.Nighttime != 0);
            AddResultIfCondition(lst, HasDual, le.Dual != 0);
            AddResultIfCondition(lst, HasCFI, le.CFI != 0);
            AddResultIfCondition(lst, HasSIC, le.SIC != 0);
            AddResultIfCondition(lst, HasPIC, le.PIC != 0);
            AddResultIfCondition(lst, HasTotalTime, le.TotalFlightTime != 0);
            AddResultIfCondition(lst, IsPublic, le.fIsPublic);
            AddResultIfCondition(lst, IsSigned, le.CFISignatureState != LogbookEntryCore.SignatureState.None);
            AddResultIfCondition(lst, HasTelemetry, le.HasFlightData);
            AddResultIfCondition(lst, HasImages, le.FlightImages.Count != 0);

            return TestConditionsForConjunction(FlightCharacteristicsConjunction, lst);
        }

        private bool IsPropertiesMatch(LogbookEntryCore le)
        {
            if (PropertyTypes == null || PropertyTypes.Count == 0)
                return true;

            List<bool> lst = new List<bool>();
            foreach (CustomPropertyType cpt in PropertyTypes)
                AddResultIfCondition(lst, true, le.CustomProperties.GetEventWithTypeID(cpt.PropTypeID) != null);

            return TestConditionsForConjunction(PropertiesConjunction, lst);
        }

        private bool IsEnumerated(LogbookEntryCore le)
        {
            return (EnumeratedFlights == null || EnumeratedFlights.Count == 0 || EnumeratedFlights.Contains(le.FlightID));
        }

        private bool IsGeneralTextMatch(LogbookEntry le)
        {
            // This is the most complicated to match what the database does.
            if (String.IsNullOrWhiteSpace(GeneralText))
                return true;

            string szGeneral = GeneralText; // so that we can modify this without modifying the underlying query.

            // First, look for trailing ##.
            MatchCollection mcTrailing = RegexUtility.QueryTrailingDate.Matches(szGeneral);
            if (mcTrailing.Count > 0)
            {
                string szType = mcTrailing[0].Groups["rangetype"].Value;
                if (int.TryParse(mcTrailing[0].Groups["quantity"].Value, out int count))
                {
                    switch (szType.ToUpperInvariant())
                    {
                        case "D":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddDays(-count)) < 0)
                                return false;
                            break;
                        case "CM":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddCalendarMonths(-count)) < 0)
                                return false;
                            break;
                        case "M":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddMonths(-count)) < 0)
                                return false;
                            break;
                        case "W":
                            if (le.Date.Date.CompareTo(DateTime.Now.Date.AddDays(-count * 7)) < 0)
                                return false;
                            break;
                    }

                    // Now remove this from szGeneral because we're not actually searching for the text.
                    szGeneral = RegexUtility.QueryTrailingDate.Replace(szGeneral, string.Empty);
                }
            }

            UserAircraft ua = new UserAircraft(le.User);
            Aircraft ac = ua.GetUserAircraftByID(le.AircraftID);

            // OK, now we're on to general search.  First, generate - once - the string to search
            string szMatch = le.SearchStringForFlight(ac.PrivateNotes);

            // Split phrases at OR, everything within them is an AND.  E.g., "cat dog or horse buggy" should be treated as "cat and dog" OR "horse and buggy".
            // So each PHRASE is an "AND", and the phrases themselves will be OR'd together.
            string[] phrases = RegexUtility.QueryORPhrases.Split(GeneralText.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            // No phrases = no condition = call it a match
            if (phrases.Length == 0)
                return true;

            foreach (string phrase in phrases)
            {
                MatchCollection mTerms = RegexUtility.QuerySearchTerms.Matches(phrase);
                bool matchesWholePhrase = true;
                foreach (Match mTerm in mTerms)
                {
                    string szTerm = mTerm.Groups["term"].Value.Trim();
                    bool fNegate = !String.IsNullOrEmpty(mTerm.Groups["negate"].Value);

                    if (string.IsNullOrWhiteSpace(szTerm))
                        continue;

                    if (!(fNegate ^ IsPhraseMatch(szTerm, szMatch, le)))
                    {
                        matchesWholePhrase = false;
                        break;
                    }
                }

                if (matchesWholePhrase)
                    return true;
            }

            return false;
        }

        private static bool IsPhraseMatch(string szPhrase, string szMatch, LogbookEntryBase le)
        {
            if (String.IsNullOrEmpty(szMatch))
                return false;

            // Issue #802 - Quick hack to direct a specific query to comment or route
            MatchCollection mcSpecific = RegexUtility.QuerySpecificField.Matches(szPhrase);
            if (mcSpecific.Count > 0)    // user is requesting a specific match on a specific field
            {
                GroupCollection gc = mcSpecific[0].Groups;
                string szValue = gc["value"].Value.Trim();
                // Convert the wildcards to something unique, escape, the put the wildcards back
                string rMatch = String.IsNullOrEmpty(szValue) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "^{0}$", szValue.ConvertToRegexWildcards());
                string szTest;
                switch (gc["field"].Value.ToUpperInvariant())
                {
                    case RegexUtility.szPrefixLimitComments:
                        szTest = le.Comment;
                        break;
                    case RegexUtility.szPrefixLimitRoute:
                        szTest = le.Route;
                        break;
                    default:
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unknown prefix '{0}' matched RegexUtility.QuerySpecificField", gc["field"].Value));
                }
                return Regex.IsMatch(szTest.Trim(), rMatch, RegexOptions.IgnoreCase);
            }
            else
                return Regex.IsMatch(szMatch.Trim(), szPhrase.ConvertToRegexWildcards(), RegexOptions.IgnoreCase);
        }

        public bool MatchesFlight(LogbookEntry le)
        {
            if (le == null)
                return false;

            return IsEnumerated(le) &&
                IsAircraftMatch(le) && 
                IsPropertiesMatch(le) &&
                IsDateMatch(le) &&
                IsAirportMatch(le) &&
                IsGeneralTextMatch(le) && 
                IsFlightCharacteristicsMatch(le) &&
                IsAircraftCharacteristicsMatch(le) &&
                IsModelMatch(le);
        }
        #endregion
    }

}