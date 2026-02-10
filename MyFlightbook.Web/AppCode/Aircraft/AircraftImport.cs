using MyFlightbook.CSV;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2012-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Encapsulates the data provided during import for a given aircraft to import:
    ///  - the tail number
    ///  - the free-form text model as given
    ///  - the id of the model
    ///  - the integer-based instance type of the model
    /// </summary>
    [Serializable]
    public class AircraftImportSpec
    {
        public string TailNum { get; set; } = string.Empty;
        public string ProposedModel { get; set; } = string.Empty;
        public int ModelID { get; set; } = MakeModel.UnknownModel;
        public int InstanceType { get; set; } = (int)AircraftInstanceTypes.RealAircraft;

        public int AircraftID { get; set; } = Aircraft.idAircraftUnknown;

        public static string KeyForTailModel(string tail, string model)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}", tail, model);
        }

        public string Key { get { return KeyForTailModel(TailNum, ProposedModel); } }
    }

    /// <summary>
    /// For importing, this is a placeholder object for a potential aircraft to an aircraft
    /// Note that this does NOT inherit from Aircraft because we set the BestMatchAircraft after the fact.
    /// </summary>
    [Serializable]
    public class AircraftImportMatchRow : IComparable, IEquatable<AircraftImportMatchRow>
    {
        public enum MatchState { MatchedExisting, UnMatched, MatchedInProfile, JustAdded }

        #region Comparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            AircraftImportMatchRow aim = (AircraftImportMatchRow)obj ?? throw new InvalidCastException("object passed to CompareTo is not an AircraftImportMatchRow");
            if (State == aim.State)
                return TailNumber.CompareCurrentCultureIgnoreCase(aim.TailNumber);
            else
                return ((int)State) - ((int)aim.State);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AircraftImportMatchRow);
        }

        public bool Equals(AircraftImportMatchRow other)
        {
            return other != null &&
                   TailNumber == other.TailNumber &&
                   NormalizedModelGiven == other.NormalizedModelGiven &&
                   EqualityComparer<Aircraft>.Default.Equals(BestMatchAircraft, other.BestMatchAircraft) &&
                   EqualityComparer<Collection<MakeModel>>.Default.Equals(MatchingModels, other.MatchingModels) &&
                   EqualityComparer<MakeModel>.Default.Equals(SuggestedModel, other.SuggestedModel) &&
                   EqualityComparer<MakeModel>.Default.Equals(SpecifiedModel, other.SpecifiedModel) &&
                   State == other.State &&
                   ID == other.ID;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1409820271;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TailNumber);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NormalizedModelGiven);
                hashCode = hashCode * -1521134295 + EqualityComparer<Aircraft>.Default.GetHashCode(BestMatchAircraft);
                hashCode = hashCode * -1521134295 + EqualityComparer<Collection<MakeModel>>.Default.GetHashCode(MatchingModels);
                hashCode = hashCode * -1521134295 + EqualityComparer<MakeModel>.Default.GetHashCode(SuggestedModel);
                hashCode = hashCode * -1521134295 + EqualityComparer<MakeModel>.Default.GetHashCode(SpecifiedModel);
                hashCode = hashCode * -1521134295 + State.GetHashCode();
                hashCode = hashCode * -1521134295 + ID.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return EqualityComparer<AircraftImportMatchRow>.Default.Equals(left, right);
        }

        public static bool operator !=(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return !(left == right);
        }

        public static bool operator <(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(AircraftImportMatchRow left, AircraftImportMatchRow right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        #region Constructors
        public AircraftImportMatchRow()
        {
            TailNumber = ModelGiven = NormalizedModelGiven = string.Empty;
            BestMatchAircraft = null;
            SuggestedModel = SpecifiedModel = null;
            State = AircraftImportMatchRow.MatchState.UnMatched;
            ID = -1;
        }

        public AircraftImportMatchRow(string szTail, string modelGiven) : this()
        {
            TailNumber = szTail;
            ModelGiven = modelGiven;
            NormalizedModelGiven = NormalizeModel(modelGiven);
        }
        #endregion

        public static string NormalizeModel(string sz)
        {
            return RegexUtility.NormalizedTailChars.Replace(sz, string.Empty).ToUpperInvariant();
        }

        /// <summary>
        /// Looks for items that are technically unmatched but now are found in the user's profile - switch them to "JustAdded"
        /// We are looking for anything that is UnMatched but which is in the user's aircraft list; this inconsistency can most easily be explained as "JustAdded"
        /// </summary>
        /// <param name="lstMatches">The current set of matches</param>
        /// <param name="szUser">The username against which to match</param>
        public static void RefreshRecentlyAddedAircraftForUser(IEnumerable<AircraftImportMatchRow> lstMatches, string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser), "AddAllExistingAircraftForUser - no user specified");
            if (lstMatches == null)
                throw new ArgumentNullException(nameof(lstMatches));

            List<Aircraft> rgac = new List<Aircraft>(new UserAircraft(szUser).GetAircraftForUser());
            foreach (AircraftImportMatchRow mr in lstMatches)
            {
                if (mr.State == MatchState.UnMatched)
                {
                    string szNormal = RegexUtility.NormalizedTailChars.Replace(mr.TailNumber, string.Empty);
                    if (rgac.Exists(ac => String.Compare(RegexUtility.NormalizedTailChars.Replace(ac.TailNumber, string.Empty), szNormal, StringComparison.CurrentCultureIgnoreCase) == 0))
                        mr.State = MatchState.JustAdded;
                }
            }
        }

        #region Properties
        /// <summary>
        /// Tailnumber to which we want to match
        /// </summary>
        public string TailNumber { get; set; }

        /// <summary>
        /// The model AS SPECIFIED by the user
        /// </summary>
        public string ModelGiven { get; set; }

        /// <summary>
        /// The normalized model name (i.e., non-alphanumeric chars stripped)
        /// </summary>
        public string NormalizedModelGiven { get; set; }

        /// <summary>
        /// The best match we've found so far, could be null
        /// </summary>
        public Aircraft BestMatchAircraft { get; set; }

        /// <summary>
        /// A set of potential models which match
        /// </summary>
        public Collection<MakeModel> MatchingModels { get; } = new Collection<MakeModel>();

        public void SetMatchingModels(IEnumerable<MakeModel> rgmm)
        {
            MatchingModels.Clear();
            if (rgmm == null)
                return;
            foreach (MakeModel mm in rgmm)
                MatchingModels.Add(mm);
        }

        /// <summary>
        /// The model that was suggested automatically by the system
        /// </summary>
        public MakeModel SuggestedModel { get; set; }

        /// <summary>
        /// The model that was specified by the user (i.e., overridden)
        /// </summary>
        public MakeModel SpecifiedModel { get; set; }

        /// <summary>
        /// The state of the match - new aircraft, in profile already, or existing?
        /// </summary>
        public MatchState State { get; set; }

        /// <summary>
        /// Unique ID for the AircraftImportMatchRow.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Instance type description that is safe to bind too (checks for null)
        /// </summary>
        public string InstanceTypeDescriptionDisplay
        {
            get { return (BestMatchAircraft == null) ? string.Empty : BestMatchAircraft.InstanceTypeDescription; }
        }

        /// <summary>
        /// Model description that is safe to bind too (checks for null)
        /// </summary>
        public string SpecifiedModelDisplay
        {
            get { return (SpecifiedModel == null) ? string.Empty : SpecifiedModel.DisplayName; }
        }
        #endregion
    }

    /// <summary>
    /// Results from parsing the CSV file
    /// </summary>
    [Serializable]
    public class AircraftImportParseContext
    {
        #region Properties
        private readonly List<AircraftImportMatchRow> _matchResults = new List<AircraftImportMatchRow>();

        /// <summary>
        /// The AircraftImportMatchRow results from the import
        /// </summary>
        public Collection<AircraftImportMatchRow> MatchResults
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults); }
        }

        private readonly List<string> _tailsFound = new List<string>();

        /// <summary>
        /// The tails that were found (to avoid duplicates)
        /// </summary>
        private Collection<string> TailsFound
        {
            get { return new Collection<string>(_tailsFound); }
        }

        /// <summary>
        /// User for whom we are importing
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// True if rows were found.
        /// </summary>
        public bool RowsFound { get; set; }

        #region internal - column indices
        private int iColTail = -1;  // column index for tailnumber
        private int iColModel = -1; // column index for model
        private int iColPrivatenotes = -1; // column index for private notes
        private int iColFrequentlyUsed = -1; // column index for frequently used
        private int iColAircraftID = -1; // column index for aircraft ID
        private int iColRole = -1;  // column index for the aircraft's role
        private int iColCopyPIC = -1;   // column index for the yes/no field to copy name-of-pic
        #endregion

        #region Subsets of matches
        /// <summary>
        /// Returns all unmatched aircraft
        /// </summary>
        [JsonIgnore]
        public Collection<AircraftImportMatchRow> AllUnmatched
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched)); }
        }

        /// <summary>
        /// All candidates that were not found in the user's profile.
        /// </summary>
        [JsonIgnore]
        public Collection<AircraftImportMatchRow> AllMissing
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched || mr.State == AircraftImportMatchRow.MatchState.MatchedExisting)); }
        }

        /// <summary>
        /// Returns unmatched or just-added aircraft (I.e., ones to display)
        /// </summary>
        [JsonIgnore]
        public Collection<AircraftImportMatchRow> UnmatchedOrJustAdded
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched || mr.State == AircraftImportMatchRow.MatchState.JustAdded)); }
        }

        /// <summary>
        /// Returns all aircraft that are not unmatched (matched to existing aircraft, or to profile, or just added)
        /// </summary>
        [JsonIgnore]
        public Collection<AircraftImportMatchRow> AllMatched
        {
            get { return new Collection<AircraftImportMatchRow>(_matchResults.FindAll(mr => mr.State != AircraftImportMatchRow.MatchState.UnMatched)); }
        }
        #endregion

        private readonly List<AircraftInstance> m_rgAircraftInstances;
        #endregion

        #region Initialization/constructors
        /// <summary>
        /// Adds a match candidate to the context, ignoring dupes
        /// </summary>
        /// <param name="szTail">The tailnumber</param>
        /// <param name="szModelGiven">The given model</param>
        /// <param name="fRequireModel">True (default) if a model MUST be provided.</param>
        public AircraftImportMatchRow AddMatchCandidate(string szTail, string szModelGiven, bool fRequireModel = true)
        {
            // Look for missing data.  If both are empty, that's OK - just continue
            if (String.IsNullOrEmpty(szTail) && String.IsNullOrEmpty(szModelGiven))
                return null;

            // But if only one is empty, that's a problem.
            if (String.IsNullOrEmpty(szTail) || (fRequireModel && String.IsNullOrEmpty(szModelGiven)))
                throw new MyFlightbookException(Resources.Aircraft.ImportNotValidCSV);

            string szTailNormal = RegexUtility.NonAlphaNumeric.Replace(szTail, string.Empty);

            /* Ignore this if 
             * a) (sim or anonymous) AND there is already a matchrow containing this tail AND model OR
             * b) NOT sim or anonymous AND tail is already in TailsFound
             */
            if (CountryCodePrefix.IsNakedAnon(szTail) || CountryCodePrefix.IsNakedSim(szTail))
            {
                if (MatchResults.FirstOrDefault<AircraftImportMatchRow>(matchrow => { return matchrow.TailNumber.CompareOrdinalIgnoreCase(szTail) == 0 && matchrow.ModelGiven.CompareOrdinalIgnoreCase(szModelGiven) == 0; }) != null)
                    return null;
            }
            else if (TailsFound.Contains(szTailNormal))
                return null;
            else if (RegexUtility.AnonymousTail.IsMatch(szTail) && int.TryParse(szTailNormal, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idmodel) && !MakeModel.GetModel(idmodel).IsNew)    // issue #1306 - need to properly handle #123456
            {
                // Restore the tail - we'll match it up elsehwere
                TailsFound.Add(szTail);
            }

            RowsFound = true;

            AircraftImportMatchRow mr = new AircraftImportMatchRow(szTail, szModelGiven);
            MatchResults.Add(mr);
            if (!String.IsNullOrEmpty(szTailNormal))
                TailsFound.Add(szTailNormal);
            return mr;
        }

        private void InitHeaders(string[] rgszRow)
        {
            if (rgszRow == null)
                return;

            if (rgszRow.Length < 2)
                throw new MyFlightbookException(Resources.Aircraft.ImportNotValidCSV);

            for (int i = 0; i < rgszRow.Length; i++)
            {
                if (rgszRow[i].CompareCurrentCultureIgnoreCase(ImportFlights.CSVImporter.TailNumberColumnName) == 0)
                    iColTail = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase(ImportFlights.CSVImporter.ModelColumnName) == 0)
                    iColModel = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Private Notes") == 0)
                    iColPrivatenotes = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Frequently Used") == 0)
                    iColFrequentlyUsed = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Aircraft ID") == 0)
                    iColAircraftID = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("Autofill") == 0)
                    iColRole = i;
                if (rgszRow[i].CompareCurrentCultureIgnoreCase("FillPICName") == 0)
                    iColCopyPIC = i;
            }

            if (iColTail < 0)
                throw new MyFlightbookException(Resources.Aircraft.ImportNoTailNumberColumnFound);
            if (iColModel < 0)
                throw new MyFlightbookException(Resources.Aircraft.ImportNoModelColumnFound);
        }

        private void InitFromCSV(Stream CSVToParse)
        {
            string[] rgszRow;
            bool fFirstRow = true;  // detect list separator on first read row.

            UserAircraft ua = String.IsNullOrEmpty(Username) ? null : new UserAircraft(Username);

            using (CSVReader csvr = new CSVReader(CSVToParse))
            {
                try
                {
                    InitHeaders(csvr.GetCSVLine(true));
                    while ((rgszRow = csvr.GetCSVLine(fFirstRow)) != null)
                    {
                        if (rgszRow == null || rgszRow.Length == 0)
                            continue;

                        string szTail = rgszRow[iColTail].Trim().ToUpper(CultureInfo.CurrentCulture);
                        string szModelGiven = rgszRow[iColModel].Trim();

                        // trim anything after a comma, if necessary
                        int iComma = szModelGiven.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                        if (iComma > 0)
                            szModelGiven = szModelGiven.Substring(0, iComma);

                        AircraftImportMatchRow mr = AddMatchCandidate(szTail, szModelGiven);

                        if (mr != null && iColAircraftID >= 0 && ua != null)
                        {
                            int idAircraft = Aircraft.idAircraftUnknown;
                            if (int.TryParse(rgszRow[iColAircraftID], NumberStyles.Integer | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, CultureInfo.InvariantCulture, out idAircraft))
                            {
                                Aircraft ac = ua.GetUserAircraftByID(idAircraft);
                                if (ac != null && Aircraft.NormalizeTail(szTail).CompareCurrentCultureIgnoreCase(Aircraft.NormalizeTail(ac.TailNumber)) == 0)   // double check that the tails match too!
                                {
                                    mr.BestMatchAircraft = ac;
                                    bool fChanged = false;
                                    if (iColFrequentlyUsed >= 0)
                                    {
                                        bool newVal = !rgszRow[iColFrequentlyUsed].SafeParseBoolean();  // meaning is inverted - internally it's "hide", externally it's "show" (i.e., frequently used).
                                        fChanged = (newVal != ac.HideFromSelection);
                                        ac.HideFromSelection = newVal;
                                    }
                                    if (iColPrivatenotes >= 0)
                                    {
                                        fChanged = fChanged || ac.PrivateNotes.CompareCurrentCultureIgnoreCase(rgszRow[iColPrivatenotes]) != 0;
                                        ac.PrivateNotes = rgszRow[iColPrivatenotes];
                                    }

                                    if (iColRole >= 0 && Enum.TryParse(rgszRow[iColRole], true, out Aircraft.PilotRole role))
                                    {
                                        fChanged = fChanged || role != ac.RoleForPilot;
                                        ac.RoleForPilot = role;
                                        if (role != Aircraft.PilotRole.PIC)
                                            ac.CopyPICNameWithCrossfill = false;
                                    }

                                    if (iColCopyPIC >= 0)
                                    {
                                        bool newVal = ac.RoleForPilot == Aircraft.PilotRole.PIC && rgszRow[iColCopyPIC].SafeParseBoolean();
                                        fChanged = fChanged || ac.CopyPICNameWithCrossfill != newVal;
                                        ac.CopyPICNameWithCrossfill = newVal;
                                    }

                                    if (fChanged)
                                        ua.FAddAircraftForUser(ac);
                                }
                            }
                        }
                    }
                }
                catch (CSVReaderInvalidCSVException ex)
                {
                    throw new MyFlightbookException(ex.Message);
                }
                catch (MyFlightbookException)
                {
                    throw;
                }
            }
        }

        public AircraftImportParseContext()
        {
            RowsFound = false;
            m_rgAircraftInstances = new List<AircraftInstance>(AircraftInstance.GetInstanceTypes());
        }

        /// <summary>
        /// Creates a new AirportImportParseResults object, initializing it from the specified CSV string.
        /// </summary>
        /// <param name="szCSV">String representing the aircraft to import, in CSV format.</param>
        public AircraftImportParseContext(string szCSVToParse, string szUser) : this()
        {
            Username = szUser;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(szCSVToParse)))
                InitFromCSV(ms);
        }
        #endregion

        public void CleanUpBestMatchAircraft()
        {
            _matchResults.RemoveAll(mr => mr.BestMatchAircraft == null);
        }

        protected void SetModelMatch(AircraftImportMatchRow mr, AircraftImportMatchRow.MatchState ms)
        {
            if (mr == null)
                throw new ArgumentNullException(nameof(mr));
            mr.State = ms;
            mr.BestMatchAircraft.InstanceTypeDescription = mr.BestMatchAircraft.InstanceTypeID == (int)AircraftInstanceTypes.RealAircraft ? String.Empty : m_rgAircraftInstances[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;
            mr.SpecifiedModel = mr.SuggestedModel = MakeModel.GetModel(mr.BestMatchAircraft.ModelID);
        }

        /// <summary>
        /// Determines if there is a match on the aircraft spec.
        /// </summary>
        /// <param name="mr"></param>
        /// <param name="spec"></param>
        /// <param name="lstUserAircraft"></param>
        /// <param name="makes"></param>
        /// <returns></returns>
        public bool CheckNakedSimOrAnon(AircraftImportMatchRow mr, AircraftImportSpec spec, List<Aircraft> lstUserAircraft, IEnumerable<MakeModel> makes)
        {
            if (mr == null)
                throw new ArgumentNullException(nameof(mr));
            if (lstUserAircraft == null)
                throw new ArgumentNullException(nameof(lstUserAircraft));

            if (spec?.AircraftID > 0)
            {
                // If we already matched up a specification to a specific aircraft, awesome - we're done.
                mr.BestMatchAircraft = lstUserAircraft.FirstOrDefault(ac => ac.AircraftID == spec.AircraftID);
                SetModelMatch(mr, AircraftImportMatchRow.MatchState.MatchedInProfile);
                return true;
            }

            if (mr.TailNumber.CompareCurrentCultureIgnoreCase(CountryCodePrefix.szSimPrefix) == 0)
            {
                // See if we have a sim in the user's profile that matches one of the models; if so, re-use that.
                mr.SetMatchingModels(MakeModel.MatchingMakes(makes, mr.NormalizedModelGiven));
                if (mr.MatchingModels.Count > 0)
                {
                    if ((mr.BestMatchAircraft = lstUserAircraft.Find(ac => ac.InstanceType != AircraftInstanceTypes.RealAircraft && (mr.MatchingModels.FirstOrDefault(mm => mm.MakeModelID == ac.ModelID) != null))) != null)
                    {
                        SetModelMatch(mr, AircraftImportMatchRow.MatchState.MatchedInProfile);
                        return true;
                    }
                }
            }
            else if (String.IsNullOrEmpty(mr.TailNumber) || mr.TailNumber.CompareCurrentCultureIgnoreCase(CountryCodePrefix.szAnonPrefix) == 0)
            {
                mr.SetMatchingModels(MakeModel.MatchingMakes(makes, mr.NormalizedModelGiven));
                if ((mr.BestMatchAircraft = lstUserAircraft.FirstOrDefault(ac => ac.IsAnonymous && mr.MatchingModels.FirstOrDefault(m => m.MakeModelID == ac.ModelID) != null)) != null)
                {
                    SetModelMatch(mr, AircraftImportMatchRow.MatchState.MatchedInProfile);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// After loading up the tailnumber/modelname pairs, this sets up best matches and sets the status for each match
        /// </summary>
        /// <param name="szUser">The user for whom we are doing this.</param>
        /// <param name="d">If not null, provides the mapping from what the user provided</param>
        public void ProcessParseResultsForUser(string szUser, IDictionary<string, AircraftImportSpec> d = null)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser), "ProcessParseResultsForUser - no user specified");

            d = d ?? new Dictionary<string, AircraftImportSpec>();

            // Now, get a list of user aircraft and of all potential matching aircraft (at most 2 DB hits, rather than 1 per aircraft)
            List<Aircraft> lstUserAircraft = new List<Aircraft>(new UserAircraft(szUser).GetAircraftForUser());
            List<Aircraft> lstAllAircraft = Aircraft.AircraftByTailListQuery(_tailsFound);
            lstAllAircraft.Sort((ac1, ac2) => { return String.Compare(ac1.TailNumber, ac2.TailNumber, StringComparison.OrdinalIgnoreCase) == 0 ? ac1.Version - ac2.Version : String.Compare(ac1.TailNumber, ac2.TailNumber, StringComparison.OrdinalIgnoreCase); });
            Collection<MakeModel> makes = MakeModel.MatchingMakes();

            List<AircraftImportMatchRow> lstMatchesToDelete = new List<AircraftImportMatchRow>();
            List<AircraftImportMatchRow> lstMatchesToAdd = new List<AircraftImportMatchRow>();

            // Now make a second pass through the list, looking for:
            // a) If it's already in your profile - awesome, easy.
            // b) if it's already in the system - easy, if the model matches
            // c) if it's not in the system - (delayed) best match from the specified model
            foreach (AircraftImportMatchRow mr in MatchResults)
            {
                // check if this aircraft is ALREADY in the user's profile
                mr.BestMatchAircraft = lstUserAircraft.Find(ac => String.Compare(RegexUtility.NonAlphaNumeric.Replace(ac.TailNumber, string.Empty), RegexUtility.NonAlphaNumeric.Replace(mr.TailNumber, string.Empty), StringComparison.CurrentCultureIgnoreCase) == 0);
                if (mr.BestMatchAircraft != null)
                {
                    SetModelMatch(mr, AircraftImportMatchRow.MatchState.MatchedInProfile);
                    continue;
                }

                // Special case naked "SIM" or naked "#"
                if (mr.BestMatchAircraft == null && CheckNakedSimOrAnon(mr, d.TryGetValue(AircraftImportSpec.KeyForTailModel(mr.TailNumber, mr.ModelGiven), out AircraftImportSpec s) ? s : null, lstUserAircraft, makes))
                    continue;

                // If not in the profile, see if it is in the list of ALL aircraft
                List<Aircraft> lstExistingMatches = lstAllAircraft.FindAll(ac => String.Compare(RegexUtility.NonAlphaNumeric.Replace(ac.TailNumber, string.Empty), RegexUtility.NonAlphaNumeric.Replace(mr.TailNumber, string.Empty), StringComparison.OrdinalIgnoreCase) == 0);
                if (lstExistingMatches != null && lstExistingMatches.Count > 0)
                {
                    lstMatchesToDelete.Add(mr);
                    foreach (Aircraft ac in lstExistingMatches)
                    {
                        AircraftImportMatchRow mr2 = new AircraftImportMatchRow(ac.TailNumber, mr.ModelGiven) { BestMatchAircraft = ac };
                        SetModelMatch(mr2, AircraftImportMatchRow.MatchState.MatchedExisting);
                        lstMatchesToAdd.Add(mr2);
                    }
                    continue;
                }

                // No match, make a best guess based on the provided model information
                mr.BestMatchAircraft = new Aircraft()
                {
                    TailNumber = mr.TailNumber,
                    ModelID = MakeModel.UnknownModel,
                    InstanceTypeDescription = m_rgAircraftInstances[0].DisplayName
                };
            }

            // Now delete the ones that had multiple versions and add the individual versions
            foreach (AircraftImportMatchRow mr in lstMatchesToDelete)
                MatchResults.Remove(mr);
            _matchResults.AddRange(lstMatchesToAdd);

            // Assign each MatchRow a unique ID
            for (int i = 0; i < MatchResults.Count; i++)
                MatchResults[i].ID = i;

            // And sort
            _matchResults.Sort();

            // Finally, assign a reasonable model for each candidate
            foreach (AircraftImportMatchRow mr in _matchResults)
            {
                if (mr.State != AircraftImportMatchRow.MatchState.UnMatched)
                    continue;

                if (!String.IsNullOrEmpty(mr.ModelGiven))
                {
                    mr.SetMatchingModels(MakeModel.MatchingMakes(makes, mr.NormalizedModelGiven));
                    if (mr.MatchingModels.Count > 0)
                    {
                        mr.SuggestedModel = mr.SpecifiedModel = mr.MatchingModels[0];
                        mr.BestMatchAircraft.ModelID = mr.SuggestedModel.MakeModelID;
                    }
                }

                mr.BestMatchAircraft.FixTailAndValidate();
                mr.BestMatchAircraft.InstanceTypeDescription = m_rgAircraftInstances[mr.BestMatchAircraft.InstanceTypeID - 1].DisplayName;
            }
        }

        /// <summary>
        /// Bulk Adds all of the aircraft to import that hit existing aircraft
        /// </summary>
        /// <param name="szUser">The user on whose behalf we are doing this.</param>
        public void AddAllExistingAircraftForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser), "AddAllExistingAircraftForUser - no user specified");

            UserAircraft ua = new UserAircraft(szUser);
            foreach (AircraftImportMatchRow mr in _matchResults)
            {
                if (mr.State == AircraftImportMatchRow.MatchState.MatchedExisting && mr.BestMatchAircraft.Version == 0) // only take the 0th version when doing a bulk import.
                {
                    ua.FAddAircraftForUser(mr.BestMatchAircraft);
                    mr.State = AircraftImportMatchRow.MatchState.MatchedInProfile;
                }
            }
        }

        /// <summary>
        /// Bulk adds all NEW aircraft (i.e., don't hit existing aircraft) into the user's profile
        /// </summary>
        /// <param name="szUser">The username on whose behalf we are doing this.</param>
        /// <returns>True for success</returns>
        public bool AddAllNewAircraftForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser), "AddAllNewAircraftForUser - no user specified");

            bool fErrorsFound = false;
            foreach (AircraftImportMatchRow mr in AllUnmatched)
            {
                if (mr.State == AircraftImportMatchRow.MatchState.UnMatched && String.IsNullOrEmpty(mr.BestMatchAircraft.ErrorString))
                {
                    mr.BestMatchAircraft.Commit(szUser);
                    mr.State = AircraftImportMatchRow.MatchState.MatchedInProfile;
                }
                else
                    fErrorsFound = true;
            }

            return !fErrorsFound;
        }
    }

}