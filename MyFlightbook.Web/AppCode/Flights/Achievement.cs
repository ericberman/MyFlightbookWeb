using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Geography;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Achievements
{
    /// <summary>
    /// Describes something that can be achieved to earn badges.  E.g., record 100 flights, get a new rating, etc.
    /// </summary>
    public class Achievement
    {
        /// <summary>
        /// For efficiency, we only compute a user's status periodically.
        /// </summary>
        public enum ComputeStatus { Never = 0, UpToDate, NeedsComputing, InProgress };

        public const string KeyVisitedAirports = "keyVisitedAirports";

        #region properties
        /// <summary>
        /// The user on whose behalf we are computing the achievements
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// A dictionary with context that can be used by badges to prevent duplicate computation
        /// </summary>
        public Dictionary<string, Object> BadgeContext { get; set; }
        #endregion

        #region Object Creation
        public Achievement()
        {
            UserName = string.Empty;
            BadgeContext = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates an achievement object
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="id"></param>
        public Achievement(string szUser) : this()
        {
            UserName = szUser;
        }
        #endregion

        #region Badge Computation
        /// <summary>
        /// Compute all of the badges for the specified user
        /// </summary>
        /// <param name="lstAdded">The list of badges that are new or changed since the previous computation</param>
        /// <param name="lstRemoved">The list of badges that have been removed</param>
        /// <returns>A list of the ACHIEVED badges (should match the database by the time this returns)</returns>
        public List<Badge> BadgesForUser()
        {
            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookException("Cannot compute milestones on an empty user!");

            Profile pf = Profile.GetUser(UserName);

            List<Badge> lAdded = new List<Badge>();
            List<Badge> lRemoved = new List<Badge>();

            if (pf.AchievementStatus == ComputeStatus.InProgress)
                return null;

            List<Badge> lstInit = Badge.EarnedBadgesForUser(UserName);

            // Checkrides - we used to examine flights to award checkrides, now we can get them directly each time.
            // It's fast, and because potentially unbounded, can't be persisted in database
            IEnumerable<CheckrideBadge> lstCheckrideBadges = CheckrideBadge.BadgesForUserCheckrides(UserName);

            if (pf.AchievementStatus == ComputeStatus.UpToDate)
            {
                lstInit.AddRange(lstCheckrideBadges);
                return lstInit;
            }

            List<Badge> lstTotal = Badge.AvailableBadgesForUser(UserName);

            // OK, if we're here we are either invalid or have never computed.
            // Set In Progress:
            pf.SetAchievementStatus(ComputeStatus.InProgress);

            // Pre-fill context with Visitedairports, since we know we're going to use that a bunch
            List<VisitedAirport> lstVA = new List<VisitedAirport>(VisitedAirport.VisitedAirportsForQuery(new FlightQuery(UserName) { AircraftInstanceTypes = FlightQuery.AircraftInstanceRestriction.RealOnly }));
            lstVA.Sort((va1, va2) => { return va1.EarliestVisitDate.CompareTo(va2.EarliestVisitDate); });
            BadgeContext[KeyVisitedAirports] = lstVA.ToArray();

            try
            {
                // We do 3 passes against the badges:
                // 1st pass: setup/initialize
                lstTotal.ForEach((b) => { b.PreFlight(BadgeContext); });

                // 2nd pass: examine each flight IN CHRONOLOGICAL ORDER
                DBHelper dbh = new DBHelper(CurrencyExaminer.CurrencyQuery(CurrencyExaminer.CurrencyQueryDirection.Ascending));
                dbh.ReadRows(
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("UserName", UserName);
                        comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                    },
                    (dr) =>
                    {
                        ExaminerFlightRow cfr = new ExaminerFlightRow(dr);

                        lstTotal.ForEach((b) => {
                            if (cfr.fIsRealAircraft || b.CanEarnInSim)
                                b.ExamineFlight(cfr, BadgeContext); });
                    });

                // 3rd pass: wrap up
                lstTotal.ForEach((b) => { b.PostFlight(BadgeContext); });

                lstTotal.ForEach((b) =>
                    {
                        // see if this is in the initial set.
                        Badge bMatch = lstInit.Find(bInit => bInit.ID == b.ID);

                        // Save the new badge if it is new (bmatch is null) or if it has changed (e.g., achieved new level
                        if (b.IsAchieved && !b.IsEqualTo(bMatch))
                        {
                            lAdded.Add(b);
                            b.Commit();
                        }
                        // Otherwise, delete the new badge if it exists but is no longer achieved
                        else if (!b.IsAchieved && bMatch != null && bMatch.IsAchieved)
                        {
                            lRemoved.Add(b);
                            b.Delete();
                        }
                    });

                // Now add in all of the checkride badges.
                lstTotal.AddRange(lstCheckrideBadges);
            }
            catch
            {
                pf.SetAchievementStatus(ComputeStatus.NeedsComputing);
            }
            finally
            {
                if (pf.AchievementStatus == ComputeStatus.InProgress)
                    pf.SetAchievementStatus(ComputeStatus.UpToDate);
            }

            return lstTotal.FindAll(b => b.IsAchieved);
        }
        #endregion
    }

    /// <summary>
    /// Represents a collection of badges within a category.
    /// </summary>
    public class BadgeSet : IComparable
    {
        #region Properties
        /// <summary>
        /// The category for this badge set
        /// </summary>
        public Badge.BadgeCategory Category { get; set; }

        /// <summary>
        /// Display name for the category
        /// </summary>
        public string CategoryName { get { return Badge.GetCategoryName(Category); } }

        /// <summary>
        /// The Badges for this category
        /// </summary>
        public IEnumerable<Badge> Badges { get; set; }

        /// <summary>
        /// Anything to show?
        /// </summary>
        public bool HasBadges { get { return Badges != null && Badges.Count() > 0; } }
        #endregion

        public BadgeSet(Badge.BadgeCategory category, IEnumerable<Badge> badges)
        {
            Category = category;
            Badges = badges;
        }

        public static IEnumerable<BadgeSet> BadgeSetsFromBadges(IEnumerable<Badge> lstIn)
        {
            if (lstIn == null)
                throw new ArgumentNullException("lstIn");
            Dictionary<Badge.BadgeCategory, BadgeSet> d = new Dictionary<Badge.BadgeCategory, BadgeSet>();
            foreach (Badge b in lstIn)
            {
                BadgeSet bs;
                if (!d.TryGetValue(b.Category, out bs))
                    d.Add(b.Category, bs = new BadgeSet(b.Category, new List<Badge>()));
                ((List<Badge>)bs.Badges).Add(b);
            }
            List<BadgeSet> lstOut = new List<BadgeSet>();
            foreach (BadgeSet bs in d.Values)
            {
                ((List<Badge>)bs.Badges).Sort();
                lstOut.Add(bs);
            }
            lstOut.Sort();
            return lstOut;
        }

        public int CompareTo(object obj)
        {
            return ((int)Category).CompareTo((int)((BadgeSet)obj).Category);
        }
    }

    [Serializable]
    /// <summary>
    /// Abstract class for a badge that is awarded for meeting an achievement
    /// </summary>
    public abstract class Badge : IComparable
    {
        /// <summary>
        /// Levels of achievement - can be binary (achieved or not achieved), or have levels
        /// </summary>
        public enum AchievementLevel
        {
            None = 0,
            Achieved,
            Bronze,
            Silver,
            Gold,
            Platinum
        }

        /// <summary>
        /// Categories of badges
        /// </summary>
        public enum BadgeCategory
        {
            BadgeCategoryUnknown = 0,
            Training = 100,
            Ratings = 200,
            Milestones = 300,
            Miscellaneous = 500,
            AirportList = 10000
        }

        /// <summary>
        /// IDs for specific achievements.  DO NOT CHANGE ANY VALUES as these are persisted in the DB
        /// </summary>
        public enum BadgeID
        {
            NOOP = 0,   //  does nothing (no-op) DO NOT USE - this is an error condition!
            // First-time events
            FirstLesson = BadgeCategory.Training,
            FirstSolo,
            FirstNightLanding,
            FirstXC,
            FirstSoloXC,

            // Ratings
            ComputedRating = BadgeCategory.Ratings,

            // Multi-level badges (counts)
            NumberOfModels = BadgeCategory.Milestones,
            NumberOfAircraft,
            NumberOfFlights,
            NumberOfAirports,
            NumberOfTotalHours,
            NumberOfPICHours,
            NumberOfSICHours,
            NumberOfCFIHours,
            NumberOfNightHours,
            NumberOfIMCHours,
            NumberOfLandings,
            NumberOfApproaches,
            NumberOfContinents,
            FlyingStreak,
            NumberOfCatClasses,
            NumberOfNVHours,

            Antarctica = BadgeCategory.Miscellaneous,

            AirportList00 = BadgeCategory.AirportList,
            AirportList01, AirportList02, AirportList03, AirportList04, AirportList05, AirportList06, AirportList07, AirportList08, AirportList09, AirportList10,
            AirportList11, AirportList12, AirportList13, AirportList14, AirportList15, AirportList16, AirportList17, AirportList18, AirportList19, AirportList20,
            AirportList21, AirportList22, AirportList23, AirportList24, AirportList25, AirportList26, AirportList27, AirportList28, AirportList29, AirportList30,
            AirportList31, AirportList32, AirportList33, AirportList34, AirportList35, AirportList36, AirportList37, AirportList38, AirportList39, AirportList40,
            AirportList41, AirportList42, AirportList43, AirportList44, AirportList45, AirportList46, AirportList47, AirportList48, AirportList49, AirportList50,
            AirportList51, AirportList52, AirportList53, AirportList54, AirportList55, AirportList56, AirportList57, AirportList58, AirportList59, AirportList60
        };

        #region properties
        /// <summary>
        /// The date when this was earned - could be datetime min if we don't know when it was actually earned.
        /// </summary>
        public DateTime DateEarned { get; set; }

        /// <summary>
        /// Timestamp for when this was computed
        /// </summary>
        public DateTime DateComputed { get; set; }

        /// <summary>
        /// Name of the user to whom this was awarded
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The achievement to which this corresponds
        /// </summary>
        public BadgeID ID { get; set; }

        /// <summary>
        /// The level to which it was achieved
        /// </summary>
        public AchievementLevel Level { get; set; }

        private string m_badgeName = string.Empty;
        /// <summary>
        /// The name of the badge
        /// </summary>
        public virtual string Name
        {
            get { return m_badgeName; }
            set { m_badgeName = value; }
        }

        /// <summary>
        /// Has this badge been achieved?
        /// </summary>
        public bool IsAchieved
        {
            get { return Level != AchievementLevel.None; }
        }

        public string DisplayString
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0} {1}", Name, EarnedDateString); }
        }

        public string EarnedDateString
        {
            get { return !DateEarned.HasValue() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Level == AchievementLevel.Achieved ? Resources.Achievements.EarnedDate : Resources.Achievements.LevelReachedDate, DateEarned); }
        }

        /// <summary>
        /// If appropriate, contains the ID of the flight on which this was earned.
        /// </summary>
        public int IDFlightEarned { get; set; }

        /// <summary>
        /// Says whether or not this badge can be earned in a sim.  False for most.
        /// </summary>
        public virtual bool CanEarnInSim
        {
            get { return false;  }
        }

        /// <summary>
        /// URL to the badge image
        /// </summary>
        public virtual string BadgeImage
        {
            get
            {
                switch (Level)
                {
                    case AchievementLevel.Achieved:
                        return "~/Images/Badge-Achieved.png";
                    case AchievementLevel.Bronze:
                        return "~/Images/Badge-Bronze.png";
                    case AchievementLevel.Gold:
                        return "~/Images/Badge-Gold.png";
                    case AchievementLevel.Platinum:
                        return "~/Images/Badge-Platinum.png";
                    case AchievementLevel.Silver:
                        return "~/Images/Badge-Silver.png";
                    default:
                    case AchievementLevel.None:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Alt text for the badge image
        /// </summary>
        public string BadgeImageAltText
        {
            get
            {
                switch (Level)
                {
                    case AchievementLevel.Achieved:
                        return Resources.Achievements.badgeTitleAchieved;
                    case AchievementLevel.Bronze:
                        return Resources.Achievements.badgeTitleBronze;
                    case AchievementLevel.Gold:
                        return Resources.Achievements.badgeTitleGold;
                    case AchievementLevel.Platinum:
                        return Resources.Achievements.badgeTitlePlatinum;
                    case AchievementLevel.Silver:
                        return Resources.Achievements.badgeTitleSilver;
                    default:
                    case AchievementLevel.None:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Optional Image overlay for the badge
        /// </summary>
        public virtual string BadgeImageOverlay { get { return string.Empty; } }

        /// <summary>
        /// Category of achievement
        /// </summary>
        public BadgeCategory Category
        {
            get
            {
                BadgeCategory bc = BadgeCategory.BadgeCategoryUnknown;
                BadgeCategory[] rgCategoryBoundaries = (BadgeCategory[])Enum.GetValues(typeof(BadgeCategory));

                for (int i = 0; i < rgCategoryBoundaries.Length; i++)
                {
                    if ((int) ID >= (int) rgCategoryBoundaries[i])
                        bc = (BadgeCategory) rgCategoryBoundaries[i];
                }
                return bc;
            }
        }

        /// <summary>
        /// Returns the name of the category (localized)
        /// </summary>
        /// <param name="bc"></param>
        /// <returns></returns>
        public static string GetCategoryName(BadgeCategory bc)
        {
            switch (bc)
            {
                case BadgeCategory.Milestones:
                    return Resources.Achievements.categoryMilestones;
                case BadgeCategory.Ratings:
                    return Resources.Achievements.categoryRatings;
                case BadgeCategory.Training:
                    return Resources.Achievements.categoryTraining;
                case BadgeCategory.AirportList:
                    return Resources.Achievements.categoryVisitedAirports;
                case BadgeCategory.Miscellaneous:
                    return Resources.Achievements.categoryMiscellaneous;
                case BadgeCategory.BadgeCategoryUnknown:
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Display name for the category
        /// </summary>
        public string CategoryName
        {
            get { return GetCategoryName(Category);}
        }

        /// <summary>
        /// Gets last error (not persisted, obviously)
        /// </summary>
        public string ErrorString {get; set;}
        #endregion

        #region Object Creation
        private void Init()
        {
            DateEarned = DateComputed = DateTime.MinValue;
            UserName = string.Empty;
            ID = BadgeID.NOOP;
            Level = AchievementLevel.None;
            IDFlightEarned = LogbookEntry.idFlightNone;
        }

        protected Badge()
        {
            Init();
        }

        protected Badge(BadgeID id, string szName)
        {
            Init();
            ID = id;
            m_badgeName = szName;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({2}) - {1}", UserName, Name, Level.ToString());
        }
        #endregion

        #region Computation
        /// <summary>
        /// Subclassed with logic for actual achievements
        /// </summary>
        /// <param name="cfr">The CurrencyFlightRow of the flight to examine</param>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public abstract void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context);

        /// <summary>
        /// Called before walking through the flights (perhaps to initialize visited airports, or aircraft for user, or such
        /// </summary>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public virtual void PreFlight(IDictionary<string, Object> context) { }

        /// <summary>
        /// Called after walking through the flights (perhaps to do some final computations)
        /// </summary>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public virtual void PostFlight(IDictionary<string, Object> context) { }
        #endregion

        #region Database
        protected virtual void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ID = (BadgeID)Convert.ToInt32(dr["BadgeID"], CultureInfo.InvariantCulture);
            UserName = (string)dr["Username"];
            Level = (AchievementLevel)Convert.ToInt32(dr["AchievementLevel"], CultureInfo.InvariantCulture);
            DateEarned = (DateTime)util.ReadNullableField(dr, "AchievedDate", DateTime.MinValue);
            DateComputed = Convert.ToDateTime(dr["ComputeDate"], CultureInfo.InvariantCulture);
            IDFlightEarned = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
        }

        public bool FIsValid()
        {
            ErrorString = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(UserName))
                    throw new MyFlightbookValidationException("No user specified");
                if (ID == BadgeID.NOOP)
                    throw new MyFlightbookValidationException("Attempt to save invalid badge!");
                if (Level == AchievementLevel.None)
                    throw new MyFlightbookValidationException("Attempt to save un-earned badge!");
            }
            catch (MyFlightbookValidationException ex)
            {
                ErrorString = ex.Message;
            }

            return ErrorString.Length == 0;
        }

        public virtual void Commit()
        {
            if (!FIsValid())
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error saving badge: {0}", ErrorString));

            DBHelper dbh = new DBHelper("REPLACE INTO badges SET BadgeID=?achieveID, UserName=?username, ClassName=?classname, AchievementLevel=?level, AchievedDate=?dateearned, ComputeDate=Now(), idFlight=?flightID");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("achieveID", ID);
                    comm.Parameters.AddWithValue("username", UserName);
                    comm.Parameters.AddWithValue("classname", GetType().ToString());
                    comm.Parameters.AddWithValue("level", (int)Level);
                    if (DateEarned.CompareTo(DateTime.MinValue) == 0)
                        comm.Parameters.AddWithValue("dateearned", null);
                    else
                        comm.Parameters.AddWithValue("dateearned", DateEarned);
                    comm.Parameters.AddWithValue("flightID", IDFlightEarned);
                });
        }

        public virtual void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM badges WHERE BadgeID=?achieveID AND Username=?username");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("achieveID", ID);
                    comm.Parameters.AddWithValue("username", UserName);
                });
        }

        /// <summary>
        /// Get a set of badges earned by the user
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A list containing the relevant badges</returns>
        public static List<Badge> EarnedBadgesForUser(string szUser)
        {
            List<Badge> lst = new List<Badge>();
            DBHelper dbh = new DBHelper("SELECT * FROM badges WHERE username=?user");
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) =>
                {
                    Badge b = (Badge)Activator.CreateInstance(Type.GetType((string)dr["ClassName"]));
                    b.InitFromDataReader(dr);
                    lst.Add(b);
                });
            return lst;
        }

        /// <summary>
        /// Deletes all badges for the specified user
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <param name="fUpdateProfile">Update the user's profile?  E.g., if deleting user's flights, then yes, we want to update the profile; if deleting the whole account, then it's pointless (or could fail if account is already deleted)</param>
        public static void DeleteBadgesForUser(string szUser, bool fUpdateProfile)
        {
            DBHelper dbh = new DBHelper("DELETE FROM badges WHERE username=?user");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("user", szUser); });

            if (fUpdateProfile)
            {
                Profile pf = Profile.GetUser(szUser);
                if (!String.IsNullOrEmpty(pf.UserName))
                    pf.SetAchievementStatus(Achievement.ComputeStatus.Never);
            }
        }
        #endregion

        /// <summary>
        /// Get a list of achievements available for the specified user
        /// </summary>
        /// <param name="szUser">The user name</param>
        /// <returns>An array of all possible Badges</returns>
        public static List<Badge> AvailableBadgesForUser(string szUser)
        {
            Badge[] rgAchievements = 
                {
                    // First-time events
                    new TrainingBadgeBegan(),
                    new TrainingBadgeFirstSolo(),
                    new TrainingBadgeFirstNightLanding(),
                    new TrainingBadgeFirstXC(),
                    new TrainingBadgeFirstSoloXC(),

                    // Ratings - obsolete
                    /*
                    new RatingBadgeATP(),
                    new RatingBadgeCFI(),
                    new RatingBadgeCFII(),
                    new RatingBadgeCommercial(),
                    new RatingBadgeInstrument(),
                    new RatingBadgePPL(),
                    new RatingBadgeRecreational(),
                    new RatingBadgeSport(),
                    new RatingBadgeMEI(),
                    */

                    // Miscellaneous
                    new FlightOfThePenguin(),

                    // Multi-level badges (counts)
                    new MultiLevelBadgeNumberFlights(),
                    new MultiLevelBadgeLandings(),
                    new MultiLevelBadgeApproaches(),
                    new MultiLevelBadgeNumberModels(),
                    new MultiLevelBadgeNumberCategoryClass(),
                    new MultiLevelBadgeNumberAircraft(),
                    new MultiLevelBadgeNumberAirports(),
                    new MultiLevelBadgeContinents(),
                    new MultiLevelBadgeFlyingStreak(),
                    new MultiLevelBadgeTotalTime(),
                    new MultiLevelBadgePICTime(),
                    new MultiLevelBadgeSICTime(),
                    new MultiLevelBadgeCFITime(),
                    new MultiLevelBadgeIMCTime(),
                    new MultiLevelBadgeNightTime(),
                    new MultiLevelBadgeNightVision()
                };

            List<Badge> lst = new List<Badge>(rgAchievements);
            lst.AddRange(AirportListBadge.GetAirportListBadges());
            lst.ForEach((b) => { b.UserName = szUser; });
            return lst;
        }

        public int CompareTo(object obj)
        {
            Badge bCompare = (Badge)obj;
            int datecomp = DateEarned.CompareTo(bCompare.DateEarned);
            return (datecomp == 0) ? String.Compare(Name, bCompare.Name, StringComparison.CurrentCultureIgnoreCase) : datecomp;
        }

        public virtual bool IsEqualTo(Badge bCompare)
        {
            return (bCompare != null &&
                    ID == bCompare.ID &&
                    IsAchieved == bCompare.IsAchieved &&
                    Level == bCompare.Level &&
                    IDFlightEarned == bCompare.IDFlightEarned &&
                    DateEarned.CompareTo(bCompare.DateEarned) == 0);
        }
    }

    #region Concrete Badge Classes
    #region Training Badges
    /// <summary>
    /// First flight is a training flight
    /// </summary>
    [Serializable]
    public class TrainingBadgeBegan : Badge
    {
        bool fSeen1stFlight = false;

        public TrainingBadgeBegan()
            : base(BadgeID.FirstLesson, Resources.Achievements.nameFirstLesson)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stFlight)
            {
                fSeen1stFlight = true;
                if (cfr.Dual > 0 && cfr.PIC == 0 && cfr.Total > 0)
                {
                    Level = AchievementLevel.Achieved;
                    DateEarned = cfr.dtFlight;
                    IDFlightEarned = cfr.flightID;
                }
            }
        }
    }

    /// <summary>
    /// Badge for first solo
    /// </summary>
    [Serializable]
    public class TrainingBadgeFirstSolo : Badge
    {
        public TrainingBadgeFirstSolo() : base(BadgeID.FirstSolo, Resources.Achievements.nameFirstSolo)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (Level == AchievementLevel.None)
            {
                if (cfr.FlightProps.FindEvent(pf => (pf.PropertyType.IsSolo && pf.DecValue > 0)) != null)
                {
                    Level = AchievementLevel.Achieved;
                    DateEarned = cfr.dtFlight;
                    IDFlightEarned = cfr.flightID;
                }
            }
        }
    }

    /// <summary>
    /// Badge for first night landing
    /// </summary>
    [Serializable]
    public class TrainingBadgeFirstNightLanding : Badge
    {
        bool fSeenFirstLanding = false;

        public TrainingBadgeFirstNightLanding()
            : base(BadgeID.FirstNightLanding, Resources.Achievements.nameFirstNightLanding)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeenFirstLanding && cfr.Dual > 0 && cfr.PIC == 0 && cfr.cFullStopNightLandings > 0)
            {
                fSeenFirstLanding = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }

    /// <summary>
    /// Badge for first Cross-country flight
    /// </summary>
    [Serializable]
    public class TrainingBadgeFirstXC : Badge
    {
        bool fSeen1stXC = false;

        public TrainingBadgeFirstXC()
            : base(BadgeID.FirstXC, Resources.Achievements.nameFirstXC)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stXC && cfr.Dual > 0 && cfr.PIC == 0 && cfr.XC > 0)
            {
                fSeen1stXC = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }

    /// <summary>
    /// Badge for first solo cross-country flight
    /// </summary>
    [Serializable]
    public class TrainingBadgeFirstSoloXC : Badge
    {
        bool fSeen1stXC = false;

        public TrainingBadgeFirstSoloXC()
            : base(BadgeID.FirstSoloXC, Resources.Achievements.nameFirstSoloXCFlight)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stXC && cfr.XC > 0 && cfr.FlightProps.FindEvent(pf => (pf.PropertyType.IsSolo && pf.DecValue > 0)) != null)
            {
                fSeen1stXC = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }
    #endregion

    #region Miscellaneous badges
    [Serializable]
    public class FlightOfThePenguin : Badge
    {
        public FlightOfThePenguin() : base(BadgeID.Antarctica, Resources.Achievements.nameAntarctica) { }

        public override string BadgeImageOverlay { get { return "~/Images/BadgeOverlays/penguin.png"; } }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(IDictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];

            if (rgva != null)
            {
                GeoRegionAntarctica antarctica = new GeoRegionAntarctica();
                foreach (VisitedAirport va in rgva)
                {
                    if (antarctica.ContainsLocation(va.Airport.LatLong))
                    {
                        Level = AchievementLevel.Achieved;
                        DateEarned = va.EarliestVisitDate;
                        IDFlightEarned = va.FlightIDOfFirstVisit;
                    }
                }
            }
        }
    }


    #endregion

    #region Multi-level badges based on integer counts
    [Serializable]
    public abstract class MultiLevelCountBadgeBase : Badge
    {
        protected int[] Levels {get; set;}
        protected decimal Quantity { get; set; }
        protected string ProgressTemplate { get; set; }
        
        public override string Name
        {
            get { return String.Format(CultureInfo.CurrentCulture, ProgressTemplate, Levels[(int)Level - (int)AchievementLevel.Bronze]); }
            set { base.Name = value; }
        }

        protected MultiLevelCountBadgeBase(BadgeID id, string nameTemplate, int Bronze, int Silver, int Gold, int Platinum)
            : base(id, nameTemplate)
        {
            ProgressTemplate = nameTemplate;
            Levels = new int[] { Bronze, Silver, Gold, Platinum};
            Level = AchievementLevel.None;
        }

        /// <summary>
        /// Adds the specified amount to the count, checking to see if this bumps us up to a new level.  
        /// If it causes a level change, then IDFlightEarned is assigned to the flightID of the examiner flightrow and the dateearned is similarly set
        /// </summary>
        /// <param name="amount">Amount to add</param>
        /// <param name="cfr">The flight row</param>
        /// <returns>True if a new level was acchieved</returns>
        protected bool AddToCount(decimal amount, ExaminerFlightRow cfr)
        {
            if (amount <= 0)
                return false;

            decimal newAmount = Quantity + amount;
            AchievementLevel newLevel = Level;
            for (int i = 0; i < Levels.Length; i++)
            {
                if (newAmount >= Levels[i])
                    newLevel = (AchievementLevel)((int)AchievementLevel.Bronze + i);
                else
                    break;
            }
            Quantity = newAmount;
            bool fNewLevel = newLevel != Level;
            if (fNewLevel && cfr != null)
            {
                IDFlightEarned = cfr.flightID;
                DateEarned = cfr.dtFlight;
            }
            Level = newLevel;
            return fNewLevel;
        }
    }

    #region Concrete Multi-level badges
    /// <summary>
    /// Multi-level badge for number of flights.
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeNumberFlights : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeNumberFlights()
            : base(BadgeID.NumberOfFlights, Resources.Achievements.nameNumberFlights, 25, 100, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            AddToCount(1, cfr);
        }
    }

    /// <summary>
    /// Multi-level badge for flying a given number of models of aircraft
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeNumberModels : MultiLevelCountBadgeBase
    {
        readonly private HashSet<string> hsModelsFlown = new HashSet<string>();

        public MultiLevelBadgeNumberModels()
            : base(BadgeID.NumberOfModels, Resources.Achievements.nameNumberModels, 25, 50, 100, 200)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!hsModelsFlown.Contains(cfr.szFamily) && cfr.Total > 0 && cfr.fIsRealAircraft)
            {
                AddToCount(1, cfr);
                hsModelsFlown.Add(cfr.szFamily);
            }
        }
    }

    [Serializable]
    public class MultiLevelBadgeNumberCategoryClass : MultiLevelCountBadgeBase
    {
        readonly private HashSet<CategoryClass.CatClassID> hsCatClassesFlown = new HashSet<CategoryClass.CatClassID>();

        public MultiLevelBadgeNumberCategoryClass() : base(BadgeID.NumberOfCatClasses, Resources.Achievements.nameNumberCatClass, 4, 5, 6, 7) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (!hsCatClassesFlown.Contains(cfr.idCatClassOverride) && cfr.Total > 0 && cfr.fIsRealAircraft)
            {
                AddToCount(1, cfr);
                hsCatClassesFlown.Add(cfr.idCatClassOverride);
            }
        }
    }

    /// <summary>
    /// Multi-level badge for flying a given number of distinct aircraft
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeNumberAircraft : MultiLevelCountBadgeBase
    {
        readonly private HashSet<int> lstAircraftFlown = new HashSet<int>();

        public MultiLevelBadgeNumberAircraft()
            : base(BadgeID.NumberOfAircraft, Resources.Achievements.nameNumberAircraft, 20, 50, 100, 200)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!lstAircraftFlown.Contains(cfr.idAircraft) && cfr.Total > 0 && cfr.fIsRealAircraft)
            {
                AddToCount(1, cfr);
                lstAircraftFlown.Add(cfr.idAircraft);
            }
        }
    }

    /// Multi-level badge for number of airports visited
    [Serializable]
    public class MultiLevelBadgeNumberAirports : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeNumberAirports()
            : base(BadgeID.NumberOfAirports, Resources.Achievements.nameNumberAirports, 50, 200, 400, 1000)
        {
        }

        public override void PostFlight(IDictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.ContainsKey(Achievement.KeyVisitedAirports))
            {
                VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];

                if (rgva != null)
                    foreach (VisitedAirport va in rgva)
                    {
                        if (AddToCount(1, null))
                        {
                            DateEarned = va.EarliestVisitDate;
                            IDFlightEarned = va.FlightIDOfFirstVisit;
                        }
                    }
            }
            base.PostFlight(context);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) {  }
    }

    /// Multi-level badge for total time
    [Serializable]
    public class MultiLevelBadgeTotalTime : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeTotalTime()
            : base(BadgeID.NumberOfTotalHours, Resources.Achievements.nameNumberTotal, 40, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) 
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.Total, cfr);
        }
    }

    /// Multi-level badge for PIC time
    [Serializable]
    public class MultiLevelBadgePICTime : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgePICTime()
            : base(BadgeID.NumberOfPICHours, Resources.Achievements.nameNumberPIC, 100, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.PIC, cfr);
        }
    }

    /// Multi-level badge for total time
    [Serializable]
    public class MultiLevelBadgeSICTime : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeSICTime()
            : base(BadgeID.NumberOfSICHours, Resources.Achievements.nameNumberSIC, 40, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.SIC, cfr);
        }
    }

    /// Multi-level badge for total time
    [Serializable]
    public class MultiLevelBadgeCFITime : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeCFITime()
            : base(BadgeID.NumberOfCFIHours, Resources.Achievements.nameNumberCFI, 100, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.CFI, cfr);
        }
    }

    /// Multi-level badge for IMC time
    [Serializable]
    public class MultiLevelBadgeIMCTime : MultiLevelCountBadgeBase
    {
        public override string BadgeImageOverlay
        {
            get { return "~/images/BadgeOverlays/cloud.png"; }
        }

        public MultiLevelBadgeIMCTime()
            : base(BadgeID.NumberOfIMCHours, Resources.Achievements.nameNumberIMC, 50, 200, 500, 1000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.IMC, cfr);
        }
    }

    /// Multi-level badge for Night time
    [Serializable]
    public class MultiLevelBadgeNightTime : MultiLevelCountBadgeBase
    {
        public override string BadgeImageOverlay
        {
            get { return "~/images/BadgeOverlays/nightowl.png"; }
        }

        public MultiLevelBadgeNightTime()
            : base(BadgeID.NumberOfNightHours, Resources.Achievements.nameNumberNight, 50, 200, 500, 1000) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.Night, cfr);
        }
    }

    [Serializable]
    public class MultiLevelBadgeNightVision : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeNightVision() : base(BadgeID.NumberOfNVHours, Resources.Achievements.nameNumberNVHours, 100, 500, 1000, 2000) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            AddToCount(cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropNVFLIRTime) + cfr.FlightProps.TimeForProperty(CustomPropertyType.KnownProperties.IDPropNVGoggleTime), cfr);
        }
    }

    /// <summary>
    /// Multi-level badge for landings
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeLandings : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeLandings() : base(BadgeID.NumberOfLandings, Resources.Achievements.nameNumberLandings, 500, 1000, 2500, 5000) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.cLandingsThisFlight, cfr);
        }
    }

    /// <summary>
    /// Multi-level badge for approaches
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeApproaches : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeApproaches() : base(BadgeID.NumberOfApproaches, Resources.Achievements.nameNumberApproaches, 100, 250, 500, 1000) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            AddToCount(cfr.cApproaches, cfr);
        }
    }

    [Serializable]
    public class MultiLevelBadgeContinents : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeContinents() : base(BadgeID.NumberOfContinents, Resources.Achievements.nameContinents, 2, 3, 4, 6) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(IDictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.ContainsKey(Achievement.KeyVisitedAirports))
            {
                VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];
                HashSet<string> hsContinents = new HashSet<string>();

                if (rgva != null)
                    foreach (VisitedAirport va in rgva)
                        foreach (IPolyRegion ipr in KnownGeoRegions.AllContinents)
                        {
                            if (hsContinents.Contains(ipr.Name))
                                continue;

                            if (ipr.ContainsLocation(va.Airport.LatLong))
                            {
                                hsContinents.Add(ipr.Name); // record a visit to this continent regardless.
                                if (AddToCount(1, null))
                                {
                                    DateEarned = va.EarliestVisitDate;
                                    IDFlightEarned = va.FlightIDOfFirstVisit;
                                }
                                break;  // check next continent
                            }
                        }
            }
            base.PostFlight(context);
        }
    }

    [Serializable]
    public class MultiLevelBadgeFlyingStreak : MultiLevelCountBadgeBase
    {
        int daysInARow = 0;
        DateTime dtLastSeen = DateTime.MinValue;
        
        public MultiLevelBadgeFlyingStreak() : base(BadgeID.FlyingStreak, Resources.Achievements.nameFlyingStreak, 10, 20, 30, 50) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            DateTime dtFlight = cfr.dtFlight.Date;
            if (dtFlight.CompareTo(dtLastSeen.Date) == 0)   // ignore multiple flights on the same day.
                return;
            else if (dtFlight.Subtract(dtLastSeen.Date).Days > 1)
                daysInARow = 1;
            else
            {
                daysInARow++;
                AddToCount(daysInARow - Quantity, cfr);
            }

            dtLastSeen = cfr.dtFlight.Date;
        }
    }
    #endregion
    #endregion

    #region AirportList Badges
    [Serializable]
    public class AirportListBadgeData
    {
        public Badge.BadgeID ID { get; set; }
        public string Name { get; set; }
        public string AirportsRaw { get; set; }
        public string OverlayName { get; set; }
        public AirportList Airports { get; set; }
        public LatLongBox BoundingFrame { get; set; }
        public int[] Levels { get; set; }
        public bool BinaryOnly { get; set; }

        public AirportListBadgeData()
        {
        }

        public AirportListBadgeData(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            Name = dr["name"].ToString();
            ID = (Badge.BadgeID)Convert.ToInt32(dr["idachievement"], CultureInfo.InvariantCulture);
            AirportsRaw = dr["airportcodes"].ToString();
            Airports = new AirportList(AirportsRaw);
            BoundingFrame = Airports.LatLongBox(true).Inflate(0.1); // allow for a little slop
            OverlayName = util.ReadNullableString(dr, "overlayname");
            BinaryOnly = Convert.ToInt32(dr["fBinaryOnly"], CultureInfo.InvariantCulture) != 0;
            Levels = new int[4];
            Levels[0] = Convert.ToInt32(dr["thresholdBronze"], CultureInfo.InvariantCulture);
            Levels[1] = Convert.ToInt32(dr["thresholdSilver"], CultureInfo.InvariantCulture);
            Levels[2] = Convert.ToInt32(dr["thresholdGold"], CultureInfo.InvariantCulture);
            Levels[3] = Convert.ToInt32(dr["thresholdPlatinum"], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds a new airport-list badge
        /// </summary>
        /// <param name="name">Name of the badge</param>
        /// <param name="codes">Airport codes</param>
        /// <param name="overlay">Name of the overlay PNG</param>
        /// <param name="fBinary">True if this is a binary (earn/don't earn) property</param>
        /// <param name="bronze">Threshold for bronze</param>
        /// <param name="silver">Threshold for silver</param>
        /// <param name="gold">Threshold for gold</param>
        /// <param name="platinum">Threshold for platinum</param>
        public static void Add(string name, string codes, string overlay, bool fBinary, int bronze, int silver, int gold, int platinum)
        {
            DBHelper dbh = new DBHelper("INSERT INTO airportlistachievement SET name=?name, airportcodes=?airportcodes, overlayname=?overlay, fBinaryOnly=?fbinary, thresholdBronze=?bronze, thresholdSilver=?silver, thresholdGold=?gold, thresholdPlatinum=?platinum");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("name", name);
                comm.Parameters.AddWithValue("airportcodes", codes);
                comm.Parameters.AddWithValue("overlay", overlay);
                comm.Parameters.AddWithValue("fbinary", fBinary ? 1 : 0);
                comm.Parameters.AddWithValue("bronze", bronze);
                comm.Parameters.AddWithValue("silver", silver);
                comm.Parameters.AddWithValue("gold", gold);
                comm.Parameters.AddWithValue("platinum", platinum);
            });
        }
    }

    [Serializable]
    public class AirportListBadge : MultiLevelCountBadgeBase
    {
        protected AirportListBadgeData m_badgeData { get; set; }

        public AirportListBadge() : base(BadgeID.NOOP, string.Empty, 0, 0, 0, 0) { }

        public override string BadgeImageOverlay
        {
            get { return (m_badgeData == null || String.IsNullOrEmpty(m_badgeData.OverlayName)) ? string.Empty : "~/images/BadgeOverlays/" + m_badgeData.OverlayName; }
        }

        protected AirportListBadge(AirportListBadgeData albd)
            : base(albd == null ? BadgeID.NOOP : albd.ID, albd == null ? string.Empty : albd.Name, albd == null ? 0 : albd.Levels[0], albd == null ? 0 : albd.Levels[1], albd == null ? 0 : albd.Levels[2], albd == null ? 0 : albd.Levels[3])
        {
            m_badgeData = albd;
        }

        protected override void InitFromDataReader(MySqlDataReader dr)
        {
            base.InitFromDataReader(dr);

            // get the name that matches this
            try
            {
                m_badgeData = BadgeData.Find(albd => albd.ID == ID);
                ProgressTemplate = Name = m_badgeData.Name;
                Levels = m_badgeData.Levels;
            }
            catch { }
        }

        private const string szCacheDataKey = "keyAirportListBadgesDataList";

        public static void FlushCache()
        {
            HttpRuntime.Cache.Remove(szCacheDataKey);
        }

        protected static List<AirportListBadgeData> BadgeData
        {
            get
            {
                List<AirportListBadgeData> lst = null;

                lst = (List<AirportListBadgeData>)HttpRuntime.Cache[szCacheDataKey];

                if (lst == null)
                {
                    lst = new List<AirportListBadgeData>();
                    DBHelper dbh = new DBHelper("SELECT * FROM airportlistachievement");
                    dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new AirportListBadgeData(dr)); });

                    HttpRuntime.Cache.Add(szCacheDataKey, lst, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.Default, null);
                }

                return lst;
            }
        }

        /// <summary>
        /// Get a list of all database-defined airportlist badges.
        /// </summary>
        /// <returns>The new badges</returns>
        public static List<AirportListBadge> GetAirportListBadges()
        {
            List<AirportListBadge> l = new List<AirportListBadge>();
            BadgeData.ForEach((albd) => {l.Add(new AirportListBadge(albd));});
            return l;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(IDictionary<string, object> context)
        {
            VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];
            DateTime dtEarned = DateTime.MinValue;
            if (rgva != null)
            {
                List<airport> lstAirports = new List<airport>(m_badgeData.Airports.GetAirportList());
                lstAirports.RemoveAll(ap => !ap.IsAirport && !ap.IsSeaport);
                Array.ForEach<VisitedAirport>(rgva, (va) =>
                {
                    if (m_badgeData.BoundingFrame.ContainsPoint(va.Airport.LatLong))
                    {
                        List<airport> apMatches = lstAirports.FindAll(ap => ap.LatLong.IsSameLocation(va.Airport.LatLong, 0.02) && String.Compare(ap.FacilityTypeCode, va.Airport.FacilityTypeCode) == 0);
                        int cAirportsRemainingToHit = lstAirports.Count;
                        apMatches.ForEach((ap) => { lstAirports.Remove(ap); });
                        if (apMatches.Count > 0)
                        {
                            dtEarned = dtEarned.LaterDate(va.EarliestVisitDate);
                            if (m_badgeData.BinaryOnly)
                            {
                                if (cAirportsRemainingToHit > 0 && lstAirports.Count == 0)
                                {
                                    DateEarned = va.EarliestVisitDate;
                                    IDFlightEarned = va.FlightIDOfFirstVisit;
                                }
                            }
                            else if (AddToCount(1, null))
                            {
                                DateEarned = va.EarliestVisitDate;
                                IDFlightEarned = va.FlightIDOfFirstVisit;
                            }
                        }
                    }
                });
                if (m_badgeData.BinaryOnly)
                    Level = lstAirports.Count == 0 ? AchievementLevel.Achieved : AchievementLevel.None;

                base.PostFlight(context);
            }
        }

        public override string Name
        {
            get { return (m_badgeData.BinaryOnly) ? m_badgeData.Name : base.Name; }
            set { base.Name = value; }
        }
    }
    #endregion

    #endregion
}