using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Achievements
{
    /// <summary>
    /// Indicates if this is a new rating or adding an additional privilege to an existing rating.
    /// </summary>
    public enum CheckrideType { NewRating, AdditionalPrivilege }

    /// <summary>
    /// All kinds of licenses known to the system, in sort order
    /// </summary>
    public enum LicenseKind { Unknown, Sport, Recreational, Private, Commercial, ATP, Instrument, CFI, CFII, MEI, Night }

    /// <summary>
    /// Level of ratings.  Instrument/CFI/CFII/MEI don't have "higher" levels, but you progress on the other ones.
    /// </summary>
    public enum RatingLevel { None, Sport, Recreational, Private, Commercial, ATP }

    internal static class LicenseKindHelper
    {
        public static string Name(this CheckrideType ct)
        {
            return ct == CheckrideType.NewRating ? Resources.Achievements.LicenseNewRating : Resources.Achievements.LicenseNewPrivilege;
        }

        public static string Name(this LicenseKind lk)
        {
            switch (lk)
            {
                default:
                    throw new MyFlightbookException("Unknown kind of license");
                case LicenseKind.Unknown:
                    return string.Empty;
                case LicenseKind.ATP:
                    return Resources.Achievements.LicenseATP;
                case LicenseKind.CFI:
                    return Resources.Achievements.LicenseCFI;
                case LicenseKind.CFII:
                    return Resources.Achievements.LicenseCFII;
                case LicenseKind.Commercial:
                    return Resources.Achievements.LicenseCommercial;
                case LicenseKind.Instrument:
                    return Resources.Achievements.LicenseInstrument;
                case LicenseKind.MEI:
                    return Resources.Achievements.LicenseMEI;
                case LicenseKind.Night:
                    return Resources.Achievements.LicenseNight;
                case LicenseKind.Private:
                    return Resources.Achievements.LicensePPL;
                case LicenseKind.Recreational:
                    return Resources.Achievements.LicenseRecreational;
                case LicenseKind.Sport:
                    return Resources.Achievements.LicenseSport;
            }
        }
    }

    /// <summary>
    /// A certificate (license) with a set of associated privileges
    /// </summary>
    public class PilotLicense : IComparable
    {
        private readonly List<string> m_privs;

        #region Properties
        /// <summary>
        /// Privileges (i.e., category/class/types) associated with this certificate
        /// </summary>
        public IEnumerable<string> Privileges { get { return m_privs; } }

        /// <summary>
        /// What was the name of the license?  (E.g., Private Pilot or Instrument)
        /// </summary>
        public string LicenseName { get { return LicenseKind.Name(); } }

        /// <summary>
        /// The kind of license
        /// </summary>
        public LicenseKind LicenseKind { get; set; }
        #endregion

        public void AddPrivilege(string sz)
        {
            if (sz != null)
            {
                m_privs.Add(sz);
                m_privs.Sort();
            }
        }

        #region Object Creation
        public PilotLicense()
        {
            LicenseKind = LicenseKind.Unknown;
            m_privs = new List<string>();
        }

        public PilotLicense(LicenseKind lk, string szPriv = null) : this()
        {
            LicenseKind = lk;
            AddPrivilege(szPriv);
        }

        internal PilotLicense(Checkride cr) : this()
        {
            if (cr != null)
            {
                LicenseKind = cr.LicenseKind;

                // Merge all CFII/MEI into CFI
                if (cr.LicenseKind == LicenseKind.CFII || cr.LicenseKind == LicenseKind.MEI)
                    LicenseKind = LicenseKind.CFI;
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}: {1}", LicenseName, String.Join(", ", m_privs));
        }

        public int CompareTo(object obj)
        {
            PilotLicense pl = obj as PilotLicense;
            return (pl == null) ? -1 : LicenseKind.CompareTo(pl.LicenseKind);
        }
    }

    /// <summary>
    /// A checkride - earns a license for a single privilege
    /// </summary>
    [Serializable]
    public class Checkride : IComparable
    {
        #region Properties
        /// <summary>
        /// The date that the rating was earned
        /// </summary>
        public DateTime DateEarned { get; set; }

        /// <summary>
        /// The ID of the flight corresponding to earning it
        /// </summary>
        public int FlightID { get; set; }

        /// <summary>
        /// Was this an initial rating, or an additional privilege?
        /// </summary>
        public CheckrideType CheckrideType { get; set; }

        /// <summary>
        /// The kind of license/certificate
        /// </summary>
        public LicenseKind LicenseKind { get; set; }

        /// <summary>
        /// What was the name of the license?  (E.g., Private Pilot or Instrument)
        /// </summary>
        public string LicenseName { get { return LicenseKind.Name(); } }
        
        /// <summary>
        /// The known property that triggered this
        /// </summary>
        public CustomPropertyType.KnownProperties CheckrideProperty { get; set; }

        /// <summary>
        /// The privilege that was earned (e.g., "ASEL", or "AMEL - B737"
        /// </summary>
        public string Privilege { get; set; }

        /// <summary>
        /// Level of the rating, for hierarchical ones like PPL/Commercial
        /// </summary>
        public RatingLevel Level { get; set; } 
        #endregion

        public Checkride()
        {
            FlightID = LogbookEntry.idFlightNone;
            DateEarned = DateTime.MinValue;
            CheckrideType = CheckrideType.NewRating;
            Privilege = string.Empty;
            LicenseKind = LicenseKind.Unknown;
            Level = RatingLevel.None;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0:d} {1} {2} ({3})", DateEarned, LicenseName, CheckrideType.Name(), Privilege);
        }

        public string DisplayString
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", CheckrideType.Name(), LicenseName, Privilege); }
        }

        public int CompareTo(Object obj)
        {
            Checkride cr = obj as Checkride;
            return cr == null ? -1 : DateEarned.CompareTo(cr.DateEarned);
        }
    }

    /// <summary>
    /// Summary description for Checkrides
    /// </summary>
    public class UserRatings
    {
        #region state as we describe ratings
        // level-based: a higher one replaces a lower one
        private readonly Dictionary<string, Checkride> dictSport = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictRecreational = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictPrivate = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictCommercial = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictATP = new Dictionary<string, Checkride>();
        private RatingLevel currentLevel = RatingLevel.None;

        // Not level based
        private readonly Dictionary<string, Checkride> dictInstrument = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictCFI = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictMEI = new Dictionary<string, Checkride>();
        private readonly Dictionary<string, Checkride> dictCFII = new Dictionary<string, Checkride>();

        private readonly Dictionary<string, Checkride> dictNight = new Dictionary<string, Checkride>();
        #endregion

        #region Properties
        /// <summary>
        /// The user for whom we are summarizing checkrides
        /// </summary>
        public string Username { get; set; }

        private List<Checkride> m_lstCheckrides = null;
        public IEnumerable<Checkride> Checkrides
        {
            get
            {
                if (m_lstCheckrides == null)
                    ComputeRatings();
                return m_lstCheckrides;
            }
        }

        private List<PilotLicense> m_lstLicenses = null;
        public IEnumerable<PilotLicense> Licenses
        {
            get
            {
                if (m_lstLicenses == null)
                    ComputeRatings();
                return m_lstLicenses;
            }
        }
        #endregion

        #region Determining checkrides
        private static RatingLevel AddToDictionary(RatingLevel currentLevel, RatingLevel newLevel, Dictionary<string, Checkride> dict, Checkride er)
        {
            newLevel = ((int)newLevel > (int)currentLevel) ? newLevel : currentLevel;
            if (!dict.ContainsKey(er.Privilege))
            {
                er.CheckrideType = (dict.Count == 0) ? CheckrideType.NewRating : CheckrideType.AdditionalPrivilege;
                dict[er.Privilege] = er;
            }
            return newLevel;
        }

        private void DescribeLevelRating(Checkride er, RatingLevel newLevel)
        {
            er.Level = newLevel;
            
            switch (er.Level)
            {
                case RatingLevel.Sport:
                    er.LicenseKind = LicenseKind.Sport;
                    currentLevel = AddToDictionary(currentLevel, newLevel, dictSport, er);
                    break;
                case RatingLevel.Recreational:
                    er.LicenseKind = LicenseKind.Recreational;
                    currentLevel = AddToDictionary(currentLevel, newLevel, dictRecreational, er);
                    break;
                case RatingLevel.Private:
                    er.LicenseKind = LicenseKind.Private;
                    currentLevel = AddToDictionary(currentLevel, newLevel, dictPrivate, er);
                    break;
                case RatingLevel.Commercial:
                    er.LicenseKind = LicenseKind.Commercial;
                    currentLevel = AddToDictionary(currentLevel, newLevel, dictCommercial, er);
                    break;
                case RatingLevel.ATP:
                    er.LicenseKind = LicenseKind.ATP;
                    currentLevel = AddToDictionary(currentLevel, newLevel, dictATP, er);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Given a list of checkride events, parses them to determine what privileges, etc. were earned.
        /// </summary>
        /// <param name="lstIn"></param>
        /// <returns></returns>
        private void DescribeRatings(IEnumerable<Checkride> lstIn)
        {
            foreach (Checkride er in lstIn)
            {
                switch (er.CheckrideProperty)
                {
                    default:
                        throw new MyFlightbookValidationException("Unknown checkride!!!");

                    case CustomPropertyType.KnownProperties.IDPropCheckrideIFR:
                        er.LicenseKind = LicenseKind.Instrument;
                        AddToDictionary(currentLevel, currentLevel, dictInstrument, er);
                        break;

                    case CustomPropertyType.KnownProperties.IDPropCheckrideSport:
                        DescribeLevelRating(er, RatingLevel.Sport);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckrideRecreational:
                        DescribeLevelRating(er, RatingLevel.Recreational);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckridePPL:
                        DescribeLevelRating(er, RatingLevel.Private);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckrideCommercial:
                        DescribeLevelRating(er, RatingLevel.Commercial);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckrideATP:
                        DescribeLevelRating(er, RatingLevel.ATP);
                        break;

                    // the following two are just unspecified checkrides, so they use the highest rating described above.
                    case CustomPropertyType.KnownProperties.IDPropCheckrideNewCatClassType:
                    case CustomPropertyType.KnownProperties.IDPropCheckRide:
                        DescribeLevelRating(er, currentLevel);
                        break;

                    case CustomPropertyType.KnownProperties.IDPropCheckrideCFI:
                        er.LicenseKind = LicenseKind.CFI;
                        AddToDictionary(currentLevel, currentLevel, dictCFI, er);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckrideCFII:
                        er.LicenseKind = LicenseKind.CFII;
                        AddToDictionary(currentLevel, currentLevel, dictCFII, er);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropCheckrideMEI:
                        er.LicenseKind = LicenseKind.MEI;
                        AddToDictionary(currentLevel, currentLevel, dictMEI, er);
                        break;
                    case CustomPropertyType.KnownProperties.IDPropNightRating:
                        er.LicenseKind = LicenseKind.Night;
                        AddToDictionary(RatingLevel.None, RatingLevel.None, dictNight, er);
                        break;
                }
            }

            m_lstCheckrides = new List<Checkride>();
            m_lstCheckrides.AddRange(dictInstrument.Values);
            m_lstCheckrides.AddRange(dictSport.Values);
            m_lstCheckrides.AddRange(dictRecreational.Values);
            m_lstCheckrides.AddRange(dictPrivate.Values);
            m_lstCheckrides.AddRange(dictCommercial.Values);
            m_lstCheckrides.AddRange(dictATP.Values);
            m_lstCheckrides.AddRange(dictCFI.Values);
            m_lstCheckrides.AddRange(dictCFII.Values);
            m_lstCheckrides.AddRange(dictMEI.Values);
            m_lstCheckrides.AddRange(dictNight.Values);
            m_lstCheckrides.Sort();
        }
        #endregion

        #region Determining ratings that are held based on checkrides taken
        private PilotLicense MergeCheckrides(IEnumerable<Checkride> lstIn)
        {
            PilotLicense pl = null;

            if (lstIn == null)
                return pl;

            HashSet<string> hsPrivs = new HashSet<string>();
            foreach (Checkride cr in lstIn)
            {
                if (pl == null)
                    pl = new PilotLicense(cr);
                hsPrivs.Add(cr.Privilege);
            }

            // Sort and add privs
            if (pl != null)
            {
                List<string> lstPrivs = new List<string>(hsPrivs);
                lstPrivs.Sort();
                foreach (string szPriv in lstPrivs)
                    pl.AddPrivilege(szPriv);
            }

            return pl;
        }

        private void AddMergedCheckridesToList(IEnumerable<Checkride> lstIn)
        {
            PilotLicense pl = MergeCheckrides(lstIn);
            if (pl != null)
                m_lstLicenses.Add(pl);
        }

        private void AddCheckridesForPrivileges(Dictionary<string, Checkride> d, IEnumerable<Checkride> rgcr)
        {
            foreach (Checkride cr in rgcr)
                d[cr.Privilege] = cr;   // overwrite since we're calling this from most to least restrictive license.
        }

        private void ComputeNetCertificates()
        {
            m_lstLicenses = new List<PilotLicense>();

            // figure out the highest level for each privilege
            Dictionary<string, Checkride> d = new Dictionary<string, Checkride>();
            AddCheckridesForPrivileges(d, dictSport.Values);
            AddCheckridesForPrivileges(d, dictRecreational.Values);
            AddCheckridesForPrivileges(d, dictPrivate.Values);
            AddCheckridesForPrivileges(d, dictCommercial.Values);
            AddCheckridesForPrivileges(d, dictATP.Values);

            // At this point d should hold the highest-privilege certificate for each privilege
            // Segregate them into individual lists by license kind
            Dictionary<LicenseKind, List<Checkride>> dlevels = new Dictionary<LicenseKind, List<Checkride>>();
            foreach (Checkride cr in d.Values)
            {
                if (dlevels.ContainsKey(cr.LicenseKind))
                    dlevels[cr.LicenseKind].Add(cr);
                else
                    dlevels[cr.LicenseKind] = new List<Checkride>() { cr };
            }

            // And now add each license above to the list.
            foreach (LicenseKind lk in dlevels.Keys)
                AddMergedCheckridesToList(dlevels[lk]);

            AddMergedCheckridesToList(dictInstrument.Values);
            AddMergedCheckridesToList(dictNight.Values);

            // MEI and CFII are really just flavors of CFI, so merge these together.
            List<Checkride> lst = new List<Checkride>(dictCFI.Values);
            lst.AddRange(dictCFII.Values);
            lst.AddRange(dictMEI.Values);
            AddMergedCheckridesToList(lst);
            m_lstLicenses.Sort();
        }
        #endregion

        /// <summary>
        /// Looks at all the checkrides for the user in the database and describes them and determines what ratings you likely hold.
        /// I.e., computes the Checkrides and Ratings properties.
        /// </summary>
        private void ComputeRatings()
        {
            List<Checkride> lst = new List<Checkride>();

            if (String.IsNullOrEmpty(Username))
            {
                m_lstCheckrides = new List<Checkride>();
                return;
            }
           
            DBHelper dbh = new DBHelper(@"SELECT cpt.idproptype, f.date, f.idflight, m.typename, cc.Category, cc.Class, cc.idCatClass
                FROM
                    flightproperties fp
                        INNER JOIN
                    custompropertytypes cpt ON fp.idproptype = cpt.idproptype
                        INNER JOIN
                    flights f ON fp.idflight = f.idflight
                        INNER JOIN
                    aircraft ac ON f.idaircraft = ac.idaircraft
                        INNER JOIN
                    models m ON ac.idmodel = m.idmodel
                        INNER JOIN
                    categoryclass cc ON m.idcategoryclass = cc.idcatclass
                WHERE
                    fp.idproptype IN (39 , 40, 42, 43, 45, 89, 131, 161, 176, 177, 225, 623)
                        AND f.username = ?userName
                ORDER BY date ASC");

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("userName", Username); },
                (dr) =>
                {
                    Checkride cr = new Checkride()
                    {
                        FlightID = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture),
                        DateEarned = Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture),
                        CheckrideProperty = (CustomPropertyType.KnownProperties)Convert.ToInt32(dr["idproptype"], CultureInfo.InvariantCulture)
                    };

                    string szType = (string)dr["typename"];
                    string szCategory = (string)dr["Category"];
                    switch (cr.CheckrideProperty)
                    {
                        case CustomPropertyType.KnownProperties.IDPropCheckrideIFR:
                            cr.Privilege = szCategory;
                            break;
                        case CustomPropertyType.KnownProperties.IDPropCheckrideCFI:
                        case CustomPropertyType.KnownProperties.IDPropCheckrideMEI:
                            {
                                // CFI ratings are category/class, but not land/sea class, just single/multi.
                                CategoryClass.CatClassID ccid = (CategoryClass.CatClassID)Convert.ToInt32(dr["idCatClass"], CultureInfo.InvariantCulture);
                                switch (ccid)
                                {
                                    case CategoryClass.CatClassID.AMEL:
                                    case CategoryClass.CatClassID.AMES:
                                        cr.Privilege = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", szCategory, Resources.Achievements.PrivilegeMultiEngine, szType).Trim();
                                        break;
                                    case CategoryClass.CatClassID.ASEL:
                                    case CategoryClass.CatClassID.ASES:
                                        cr.Privilege = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", szCategory, Resources.Achievements.PrivilegeSingleEngine, szType).Trim();
                                        break;
                                    default:
                                        cr.Privilege = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", dr["Category"], dr["Class"], String.IsNullOrEmpty(szType) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Achievements.ratingTypeTemplate, szType)).Trim();
                                        break;
                                }
                            }
                            break;
                        case CustomPropertyType.KnownProperties.IDPropCheckrideCFII:
                            cr.Privilege = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Resources.Achievements.PrivilegeCFII, szCategory);
                            break;
                        case CustomPropertyType.KnownProperties.IDPropNightRating:
                            cr.Privilege = Resources.Achievements.PrivilegeNight;
                            break;
                        default:
                            cr.Privilege = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", dr["Category"], dr["Class"], String.IsNullOrEmpty(szType) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Achievements.ratingTypeTemplate, szType)).Trim();
                            break;
                    }
                    
                    lst.Add(cr);
                });

            // We now have a set of checkrides - now we need to figure out how they applied.
            DescribeRatings(lst);

            ComputeNetCertificates();
        }

        public UserRatings(string szUser)
        {
            Username = szUser;
        }
    }

    [Serializable]
    public class CheckrideBadge : Badge
    {
        private readonly Checkride m_cr;

        public override void Commit()
        {
            // Don't save
        }

        public override void Delete()
        {
            // don't delete
        }

        public override string Name { get { return m_cr.DisplayString; } }

        public override string BadgeImageOverlay { get { return "~/images/BadgeOverlays/certificate.png"; } }

        public CheckrideBadge(Checkride cr) : base()
        {
            m_cr = cr;
            if (cr != null)
            {
                ID = BadgeID.ComputedRating;
                IDFlightEarned = cr.FlightID;
                DateEarned = cr.DateEarned;
                Level = AchievementLevel.Achieved;  // by definition, we have already achieved this.
            }
        }

        public static IEnumerable<CheckrideBadge> BadgesForUserCheckrides(string szUser)
        {
            List<CheckrideBadge> lst = new List<CheckrideBadge>();
            if (String.IsNullOrEmpty(szUser))
                return lst;

            UserRatings ur = new UserRatings(szUser);
            foreach (Checkride cr in ur.Checkrides)
                lst.Add(new CheckrideBadge(cr));
            return lst;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }
    }
}