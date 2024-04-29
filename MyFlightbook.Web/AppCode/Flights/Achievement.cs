using MyFlightbook.Airports;
using MyFlightbook.Currency;
using MyFlightbook.Geography;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2014-2024 MyFlightbook LLC
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
        public Dictionary<string, Object> BadgeContext { get; private set; }
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
            catch (Exception ex) when (!(ex is OutOfMemoryException))
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
    public class BadgeSet : IComparable, IEquatable<BadgeSet>
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
        public bool HasBadges { get { return Badges != null && Badges.Any(); } }
        #endregion

        public BadgeSet(Badge.BadgeCategory category, IEnumerable<Badge> badges)
        {
            Category = category;
            Badges = badges;
        }

        public static IEnumerable<BadgeSet> BadgeSetsFromBadges(IEnumerable<Badge> lstIn)
        {
            if (lstIn == null)
                throw new ArgumentNullException(nameof(lstIn));
            Dictionary<Badge.BadgeCategory, BadgeSet> d = new Dictionary<Badge.BadgeCategory, BadgeSet>();
            foreach (Badge b in lstIn)
            {
                if (!d.TryGetValue(b.Category, out BadgeSet bs))
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

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is BadgeSet bs))
                throw new ArgumentException("object is not a badgeset");
            return ((int)Category).CompareTo((int)bs.Category);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BadgeSet);
        }

        public bool Equals(BadgeSet other)
        {
            return other != null &&
                   Category == other.Category &&
                   CategoryName == other.CategoryName &&
                   EqualityComparer<IEnumerable<Badge>>.Default.Equals(Badges, other.Badges) &&
                   HasBadges == other.HasBadges;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1901650559;
                hashCode = hashCode * -1521134295 + Category.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CategoryName);
                hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<Badge>>.Default.GetHashCode(Badges);
                hashCode = hashCode * -1521134295 + HasBadges.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(BadgeSet left, BadgeSet right)
        {
            return EqualityComparer<BadgeSet>.Default.Equals(left, right);
        }

        public static bool operator !=(BadgeSet left, BadgeSet right)
        {
            return !(left == right);
        }

        public static bool operator <(BadgeSet left, BadgeSet right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(BadgeSet left, BadgeSet right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(BadgeSet left, BadgeSet right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(BadgeSet left, BadgeSet right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    [Serializable]
    /// <summary>
    /// Abstract class for a badge that is awarded for meeting an achievement
    /// </summary>
    public abstract class Badge : IComparable, IEquatable<Badge>
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
            NumberOfCountries,
            NumberOfStatesUS,
            NumberOfProvincesCanada,
            NumberOfStatesBrazil,
            NumberOfStatesAustralia,
            NumberOfStatesMexico,

            Antarctica = BadgeCategory.Miscellaneous,
            FlightsInJanuary,
            FlightsInFebruary,
            FlightsInMarch,
            FlightsInApril,
            FlightsInMay,
            FlightsInJune,
            FlightsInJuly,
            FlightsInAugust,
            FlightsInSeptember,
            FlightsInOctober,
            FlightsInNovember,
            FlightsInDecember,
            FlightsInYear,
            EclipseChaser2024,

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
                throw new ArgumentNullException(nameof(dr));
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
                    new EclipseChaser2024(),
                    // Flights-in-month
                    new FlightsInMonth(1),
                    new FlightsInMonth(2),
                    new FlightsInMonth(3),
                    new FlightsInMonth(4),
                    new FlightsInMonth(5),
                    new FlightsInMonth(6),
                    new FlightsInMonth(7),
                    new FlightsInMonth(8),
                    new FlightsInMonth(9),
                    new FlightsInMonth(10),
                    new FlightsInMonth(11),
                    new FlightsInMonth(12),
                    new MultiLevelBadgeFlightDaysInYear(),

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
                    new MultiLevelBadgeNightVision(),
                    new MultiLevelBadgeCountries(),
                    new VisitedStatesUS(),
                    new VisitedProvincesCanada(),
                    new VisitedStatesMexico(),
                    new VisitedStatesBrazil(),
                    new VisitedStatesAustralia()
                };

            List<Badge> lst = new List<Badge>(rgAchievements);
            lst.AddRange(AirportListBadge.GetAirportListBadges());
            lst.ForEach((b) => { b.UserName = szUser; });
            return lst;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Badge bCompare = obj as Badge;
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

        public override bool Equals(object obj)
        {
            return Equals(obj as Badge);
        }

        public bool Equals(Badge other)
        {
            return other != null &&
                   DateEarned == other.DateEarned &&
                   DateComputed == other.DateComputed &&
                   UserName == other.UserName &&
                   ID == other.ID &&
                   Level == other.Level &&
                   Name == other.Name &&
                   IsAchieved == other.IsAchieved &&
                   DisplayString == other.DisplayString &&
                   EarnedDateString == other.EarnedDateString &&
                   IDFlightEarned == other.IDFlightEarned &&
                   CanEarnInSim == other.CanEarnInSim &&
                   BadgeImage == other.BadgeImage &&
                   BadgeImageAltText == other.BadgeImageAltText &&
                   BadgeImageOverlay == other.BadgeImageOverlay &&
                   Category == other.Category &&
                   CategoryName == other.CategoryName;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -63878919;
                hashCode = hashCode * -1521134295 + DateEarned.GetHashCode();
                hashCode = hashCode * -1521134295 + DateComputed.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + ID.GetHashCode();
                hashCode = hashCode * -1521134295 + Level.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + IsAchieved.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DisplayString);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EarnedDateString);
                hashCode = hashCode * -1521134295 + IDFlightEarned.GetHashCode();
                hashCode = hashCode * -1521134295 + CanEarnInSim.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BadgeImage);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BadgeImageAltText);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BadgeImageOverlay);
                hashCode = hashCode * -1521134295 + Category.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CategoryName);
                return hashCode;
            }
        }

        public static bool operator ==(Badge left, Badge right)
        {
            return EqualityComparer<Badge>.Default.Equals(left, right);
        }

        public static bool operator !=(Badge left, Badge right)
        {
            return !(left == right);
        }

        public static bool operator <(Badge left, Badge right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Badge left, Badge right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Badge left, Badge right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Badge left, Badge right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion

        /// <summary>
        /// Groups badges by flight
        /// </summary>
        /// <param name="rgBadges">A set of badges</param>
        /// <returns>A dictionary of badge enumerations.</returns>
        public static IDictionary<int, IList<Badge>> BadgesByFlight(IEnumerable<Badge> rgBadges)
        {
            Dictionary<int, IList<Badge>> d = new Dictionary<int, IList<Badge>>();
            foreach (Badge b in (rgBadges ?? Array.Empty<Badge>()))
                {
                if (b.IDFlightEarned == LogbookEntryBase.idFlightNone)
                    continue;
                if (d.TryGetValue(b.IDFlightEarned, out IList<Badge> lst))
                    lst.Add(b);
                else
                    d[b.IDFlightEarned] = new List<Badge>() { b };
            }
            return d;
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
        bool fSeen1stFlight;

        public TrainingBadgeBegan()
            : base(BadgeID.FirstLesson, Resources.Achievements.nameFirstLesson)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
        bool fSeenFirstLanding;

        public TrainingBadgeFirstNightLanding()
            : base(BadgeID.FirstNightLanding, Resources.Achievements.nameFirstNightLanding)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
        bool fSeen1stXC;

        public TrainingBadgeFirstXC()
            : base(BadgeID.FirstXC, Resources.Achievements.nameFirstXC)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
        bool fSeen1stXC;

        public TrainingBadgeFirstSoloXC()
            : base(BadgeID.FirstSoloXC, Resources.Achievements.nameFirstSoloXCFlight)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(context));
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


    [Serializable]
    public class EclipseChaser2024 : Badge
    {
        private const string szEclipseAirports = @"0B7 0G7 1E8 1H1 1I1 21M 39B 3B1 44B 4B6 4B7 4C4 52B 59B 5B1 60B 6B0 6B9 70B 78B 83B 85B 87B 8B0 B06 B16 B21 CYSL K03 K16 KART KBML KBTV KCAR KCDA KEFK KERR KFSO KFZY KGTB KHUL KLKP KLRG KMAL KMLT KMPV KMSS KMVL KOGS KPBG KPQI KPTD KRME KSLK KSYR M57 1I8 5I4 6I4 8I3 C40 I72 KMZZ KTYQ 0G7 1D4 1G0 1G1 1G3 1G5 2D1 2G1 3D8 3G3 3G4 3T7 3W2 3W9 3X5 4G1 4G2 4G3 4G8 4N2 5A1 5D9 5G0 5G7 6D7 6G1 7D8 7D9 7G0 7G8 7W5 8G1 8G2 9G0 9G3 9G5 9G6 01G 03G 12G 14G 15G 16G 17G 41N 56D 62D 85N 88D 89D 92G D23 D51 D52 D59 D79 D88 D91 I64 KAKR KBJJ KBKL KBQR KBUF KCAK KCGF KCLE KDFI KDKK KDSV KERI KFDY KFZI KGKJ KGQQ KGVQ KHTF KHZY KIAG KIUA KJHW KLNN KLPR KMFD KMNN KOLE KOWX KPCW KPEO KPOV KROC KSDC KTDZ KTOL KYNG N56 P15 R47 S24 0G7 1D4 1G0 07F 1F7 20T 2F7 30F 3F9 3T8 4F7 4O4 50F 5M8 68F 6F1 73F 76F 7F3 7F5 7F7 7M3 80F 8F5 90F 9F0 9F1 9F9 9S1 F00 F41 F44 F46 F51 F53 F69 KACT KADS KCNW KCPT KCRS KDAL KDEQ KDFW KFTW KFWS KGDJ KGKY KGOP KGPM KGVT KHHW KHOT KHQZ KINJ KJDD KJWY KJXI KLBR KLNC KLXY KMNZ KOSA KPRX KPWG KRBD KSLR KTKI KTRL KTXK KTYR M18 M77 T13 T14 T15 T31 T37 T48 T56 T80 10X 1T7 20R 23R 2G5 2KL 2TX 3R9 49R 5C1 5T9 81R KAQO KBBD KBMQ KCVB KCZT KDLF KDRT KDZB KECU KEDC KERV KGRK KGTU KHDO KILE KJCT KLZZ KRYW KTPL KUVA T35 T70 T74 T82 T92 T94 0D7 0I2 1H8 1I3 1WF 2R2 38I 3EV 3FK 3I3 3I7 3R8 4I3 4I9 5I4 6CM 6G4 6I4 78I 7I2 7I4 7L8 I17 I20 I22 I34 I42 I44 I54 I61 I67 I68 I72 I73 I74 I80 I83 I91 I95 I99 KAID KAJG KAOH KAXV KBAK KBFR KBMG KCEV KCFJ KCQA KCUL KDAY KDCY KDLZ KEDJ KEHR KEVV KEYE KFFO KFRH KGDK KGEZ KGFD KGPC KHAO KHBE KHFY KHLB KHNB KHUF KIND KLWV KMGY KMIE KMQJ KMRT KMWO KOSU KOVO KOXD KPLD KRID KRSV KSCA KSER KSGH KSIV KUMP KUWL KUYF KVES KVNW O74 1H2 2T2 H57 H88 H96 HSB KENL KFAM KFOA KFWC KMDH KMVN KMWA KOLY KPCD KPJY KSAR KSLO 12A 2A2 32A 34M 37M 37T 3M0 42A 42M 4A5 4M2 4M9 6M2 7M2 7M3 7M5 7M6 7M7 7M8 7M9 8M2 H35 KADF KARG KBDQ KBPK KBVX KCCA KCGI KCHQ KCIR KCVK KCXW KDXE KEIW KFLP KGDA KHBZ KJBR KLIT KMAW KMEZ KMPJ KORK KPAH KPGR KPOF KPYN KRKR KRUE KSIK KSRC KSUZ KTKX KTWT KUNO M19 M27 M30 M60 M70 M74 M78 M85 MO5 X33";

        private static AirportList _al = null;

        private static AirportList eclipseAirports
        {
            get
            {
                if (_al == null)
                    _al = new AirportList(szEclipseAirports);

                return _al;
            }
        }

        private static readonly DateTime dtEclipse = new DateTime(2024, 04, 08);

        public override string BadgeImageOverlay { get { return "~/Images/BadgeOverlays/eclipse.png".ToAbsolute(); } }

        public EclipseChaser2024() : base(BadgeID.EclipseChaser2024, Resources.Achievements.name2024EclipseChaser) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.dtFlight.Date.CompareTo(dtEclipse.Date) == 0 && cfr.fIsRealAircraft && !IsAchieved)
            {
                AirportList eclipseMaster = eclipseAirports;
                AirportList al = new AirportList(cfr.Route);
                foreach (airport ap in al.UniqueAirports)
                {
                    if (ap.IsPort && eclipseMaster.GetAirportByCode(ap.Code) != null)
                    {
                        Level = AchievementLevel.Achieved;
                        DateEarned = cfr.dtFlight;
                        IDFlightEarned = cfr.flightID;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Issue #1191 : Badge for flying on every day of a month (with a special one on leap year February)
    /// </summary>
    [Serializable]
    public class FlightsInMonth : Badge
    {
        private int _month;

        private static BadgeID IDForMonth(int month)
        {
            return (Badge.BadgeID)(month - 1 + (int)Badge.BadgeID.FlightsInJanuary);
        }

        public int Month
        {
            get { return _month; }
            set
            {
                _month = value;
                ID = IDForMonth(value);
            }
        }

        private bool fHoldOutForLeapMonth = false;

        public FlightsInMonth() { }

        public FlightsInMonth(int month) : base(IDForMonth(month), string.Empty) 
        {
            Month = month;
        }

        private readonly Dictionary<int, HashSet<int>> dFlights = new Dictionary<int, HashSet<int>>();

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.dtFlight.Month != Month || (Level == AchievementLevel.Achieved && !fHoldOutForLeapMonth))
                return; // we've met this - nothing more to do

            HashSet<int> flights = dFlights.TryGetValue(cfr.dtFlight.Year, out HashSet<int> hs) ? hs : (dFlights[cfr.dtFlight.Year] = new HashSet<int>());
            flights.Add(cfr.dtFlight.Day);  // using a hashset handles multiple flights on a single day.

            // check if we completed a month
            // February is special: 
            int daysInMonth = DateTime.DaysInMonth(cfr.dtFlight.Year, cfr.dtFlight.Month);
            if (flights.Count == daysInMonth)
            {
                // it's not a match if we achieved this already, UNLESS we are holding out for a leap month
                // fHoldOutForLeapMonth is only ever set for February, so we don't need to double check it here. 
                // 4 possibilities for Feb (fHoldOutForLeapMonth only ever gets set for Feb):
                // a) Saw a 28 day Feb, we are seeing a new 28-day feb: return; nothing to do
                // b) Saw a 28 day Feb, we are seeing a 29-day feb: contrinue on!
                // c) Saw a 29 day Feb, this is a 28 day Feb: return
                // d) Saw a 29 day Feb, we are seeing a new 29 day Feb: return
                if (Level == AchievementLevel.Achieved && (!fHoldOutForLeapMonth || daysInMonth == 28))
                    return;

                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
                if (Month == 2)
                    fHoldOutForLeapMonth = (daysInMonth == 28);
            }
        }

        public override string Name
        {
            get
            {
                // Set the name to indicate whether we have met the month or a leap month!
                return Level == AchievementLevel.None ?
                    base.Name : (DateEarned.Month == 2 && DateEarned.Day == 29 ? Resources.Achievements.AchievementAllDaysInMonthLeapYear :
                    String.Format(CultureInfo.InvariantCulture, Resources.Achievements.AchievementAllDaysInMonth, CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[DateEarned.Month - 1]));
            }
            set => base.Name = value;
        }
    }
    #endregion

    #region Multi-level badges based on integer counts
    [Serializable]
    public abstract class MultiLevelCountBadgeBase : Badge
    {
        protected Collection<int> Levels {get; private set; }
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
            Levels = new Collection<int>(new List<int> { Bronze, Silver, Gold, Platinum });
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
            for (int i = 0; i < Levels.Count; i++)
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
    /// Issue #1191: Badge for flying lots of days in a year
    /// </summary>
    [Serializable]
    public class MultiLevelBadgeFlightDaysInYear : MultiLevelCountBadgeBase
    {
        private readonly Dictionary<int, HashSet<int>> dFlights = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, ExaminerFlightRow> dLastMatch = new Dictionary<int, ExaminerFlightRow>();

        public MultiLevelBadgeFlightDaysInYear() : base(BadgeID.FlightsInYear, Resources.Achievements.nameDaysInYear, 250, 300, 350, 360) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            HashSet<int> flights = dFlights.TryGetValue(cfr.dtFlight.Year, out HashSet<int> hs) ? hs : (dFlights[cfr.dtFlight.Year] = new HashSet<int>());
            int cFlights = flights.Count;
            flights.Add(cfr.dtFlight.DayOfYear);
            if (flights.Count > cFlights)   // it incremented - save the flight ID that incremented it
                dLastMatch[cfr.dtFlight.Year] = cfr;
        }

        public override void PostFlight(IDictionary<string, object> context)
        {
            Level = AchievementLevel.None;  // default
            // find the highest value
            List<int> lstYears = new List<int>(dFlights.Keys);
            lstYears.Sort();
            int highestDays = 0;
            ExaminerFlightRow cfr = null;
            foreach (int key in lstYears)
            {
                if (dFlights[key].Count > highestDays)
                {
                    highestDays = dFlights[key].Count;
                    cfr = dLastMatch[key];
                }
            }

            for (int i = 0; i < Levels.Count; i++)
            {
                if (highestDays >= Levels[i])
                {
                    Level = (AchievementLevel)((int)AchievementLevel.Bronze + i);
                    IDFlightEarned = cfr.flightID;
                    DateEarned = cfr.dtFlight;
                }
            }
        }
    }

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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));

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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(context));
            if (context.TryGetValue(Achievement.KeyVisitedAirports, out object value))
            {
                VisitedAirport[] rgva = (VisitedAirport[])value;

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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));

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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(cfr));
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
                throw new ArgumentNullException(nameof(context));
            if (context.TryGetValue(Achievement.KeyVisitedAirports, out object value))
            {
                VisitedAirport[] rgva = (VisitedAirport[])value;
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
    public class MultiLevelBadgeCountries : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeCountries() : base(BadgeID.NumberOfCountries, Resources.Achievements.NameNumberCountries, 3, 10, 30, 80) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(IDictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.TryGetValue(Achievement.KeyVisitedAirports, out object value))
            {
                VisitedAirport[] rgva = (VisitedAirport[])value;
                HashSet<string> hsCountries = new HashSet<string>();

                if (rgva != null)
                    foreach (VisitedAirport va in rgva)
                    {
                        if (va.Airport == null || String.IsNullOrWhiteSpace(va.Airport.CountryDisplay) || hsCountries.Contains(va.Airport.CountryDisplay))
                            continue;

                        hsCountries.Add(va.Airport.CountryDisplay);
                        if (AddToCount(1, null))
                        {
                            DateEarned = va.EarliestVisitDate;
                            IDFlightEarned = va.FlightIDOfFirstVisit;
                        }
                    }

            }
            base.PostFlight(context);
        }
    }

    [Serializable]
    public abstract class MultiLevelBadgeAdmin1 : MultiLevelCountBadgeBase
    {
        protected string CountryName { get; set; }

        protected MultiLevelBadgeAdmin1(string szCountry, string szNameTemplate, BadgeID badge, int Bronze, int Silver, int Gold, int Platinum) :
            base(badge, szNameTemplate, Bronze, Silver, Gold, Platinum)
        {
            CountryName = szCountry;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(IDictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.TryGetValue(Achievement.KeyVisitedAirports, out object value))
            {
                VisitedAirport[] rgva = (VisitedAirport[])value;
                HashSet<string> hsAdmin1 = new HashSet<string>();

                if (rgva != null)
                    foreach (VisitedAirport va in rgva)
                    {
                        if (va.Airport == null || String.IsNullOrWhiteSpace(va.Airport.CountryDisplay))
                            continue;

                        if (va.Airport.CountryDisplay.CompareCurrentCultureIgnoreCase(CountryName) == 0 && !hsAdmin1.Contains(va.Airport.Admin1Display))
                        {
                            hsAdmin1.Add(va.Airport.Admin1Display);
                            if (AddToCount(1, null))
                            {
                                DateEarned = va.EarliestVisitDate;
                                IDFlightEarned = va.FlightIDOfFirstVisit;
                            }
                        }
                    }

            }
            base.PostFlight(context);
        }
    }

    #region Concrete Admin1 (state/province) badges
    [Serializable]
    public class VisitedStatesUS : MultiLevelBadgeAdmin1
    {
        public VisitedStatesUS() : base("United States", Resources.Achievements.NameNumberStatesUS, BadgeID.NumberOfStatesUS, 5, 15, 30, 50) { }
    }

    [Serializable]
    public class VisitedProvincesCanada : MultiLevelBadgeAdmin1
    {
        public VisitedProvincesCanada() : base("Canada", Resources.Achievements.NameNumberProvincesCanada, BadgeID.NumberOfProvincesCanada, 3, 8, 10, 13) { }
    }

    [Serializable]
    public class VisitedStatesBrazil : MultiLevelBadgeAdmin1
    {
        public VisitedStatesBrazil() : base("Brazil", Resources.Achievements.NameNumberStatesBrazil, BadgeID.NumberOfStatesBrazil, 5, 10, 20, 27) { }
    }

    [Serializable]
    public class VisitedStatesAustralia : MultiLevelBadgeAdmin1
    {
        public VisitedStatesAustralia() : base("Australia", Resources.Achievements.NameNumberStatesAustralia, BadgeID.NumberOfStatesAustralia, 2, 3, 5, 7) { }
    }

    [Serializable]
    public class VisitedStatesMexico : MultiLevelBadgeAdmin1
    {
        public VisitedStatesMexico() : base("Mexico", Resources.Achievements.NameNumberStatesMexico, BadgeID.NumberOfStatesMexico, 5, 10, 20, 30) { }
    }
    #endregion

    [Serializable]
    public class MultiLevelBadgeFlyingStreak : MultiLevelCountBadgeBase
    {
        int daysInARow;
        DateTime dtLastSeen = DateTime.MinValue;
        
        public MultiLevelBadgeFlyingStreak() : base(BadgeID.FlyingStreak, Resources.Achievements.nameFlyingStreak, 10, 20, 30, 50) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
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
        [Required]
        public string Name { get; set; }
        [Required]
        [MinLength(3)]
        public string AirportsRaw { get; set; }

        public string OverlayName { get; set; }
        public AirportList Airports { get; set; }
        public LatLongBox BoundingFrame { get; set; }
        public int[] Levels { get; private set; } = new int[] { 0, 0, 0, 0 };
        public bool BinaryOnly { get; set; }

        public AirportListBadgeData()
        {
        }

        public AirportListBadgeData(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            Name = dr["name"].ToString();
            ID = (Badge.BadgeID)Convert.ToInt32(dr["idachievement"], CultureInfo.InvariantCulture);
            AirportsRaw = dr["airportcodes"].ToString();
            Airports = new AirportList(AirportsRaw);
            BoundingFrame = Airports.LatLongBox(true).Inflate(0.1); // allow for a little slop
            OverlayName = util.ReadNullableString(dr, "overlayname");
            BinaryOnly = Convert.ToInt32(dr["fBinaryOnly"], CultureInfo.InvariantCulture) != 0;
            Levels = new int[] {
                Convert.ToInt32(dr["thresholdBronze"], CultureInfo.InvariantCulture),
                Convert.ToInt32(dr["thresholdSilver"], CultureInfo.InvariantCulture),
                Convert.ToInt32(dr["thresholdGold"], CultureInfo.InvariantCulture),
                Convert.ToInt32(dr["thresholdPlatinum"], CultureInfo.InvariantCulture) };
        }

        /// <summary>
        /// Adds a new airport-list badge
        /// </summary>
        public void Commit()
        {
            const string szSet = "SET name=?name, airportcodes=?airportcodes, overlayname=?overlay, fBinaryOnly=?fbinary, thresholdBronze=?bronze, thresholdSilver=?silver, thresholdGold=?gold, thresholdPlatinum=?platinum";
            string szQ = String.Format(CultureInfo.InvariantCulture, (ID == Badge.BadgeID.NOOP) ? "INSERT INTO airportlistachievement {0}" : "UPDATE airportlistachievement {0} WHERE idachievement=?id", szSet);

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("name", Name);
                comm.Parameters.AddWithValue("airportcodes", AirportsRaw);
                comm.Parameters.AddWithValue("overlay", OverlayName);
                comm.Parameters.AddWithValue("fbinary", BinaryOnly ? 1 : 0);
                comm.Parameters.AddWithValue("bronze", Levels[0]);
                comm.Parameters.AddWithValue("silver", Levels[1]);
                comm.Parameters.AddWithValue("gold", Levels[2]);
                comm.Parameters.AddWithValue("platinum", Levels[3]);
                comm.Parameters.AddWithValue("id", (int) ID);
            });
            AirportListBadge.FlushCache();
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
            m_badgeData = BadgeData.Find(albd => albd.ID == ID);
            ProgressTemplate = Name = m_badgeData.Name;
            Levels.Clear();
            foreach (int i in m_badgeData.Levels)
                Levels.Add(i);
        }

        private const string szCacheDataKey = "keyAirportListBadgesDataList";

        public static void FlushCache()
        {
            HttpRuntime.Cache.Remove(szCacheDataKey);
        }

        public static List<AirportListBadgeData> BadgeData
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
            if (context == null)
                throw new ArgumentNullException(nameof(context));
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
                        List<airport> apMatches = lstAirports.FindAll(ap => ap.LatLong.IsSameLocation(va.Airport.LatLong, 0.02) && String.Compare(ap.FacilityTypeCode, va.Airport.FacilityTypeCode, StringComparison.InvariantCultureIgnoreCase) == 0);
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