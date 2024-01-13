using ImageMagick;
using MyFlightbook.Airports;
using MyFlightbook.Currency;
using MyFlightbook.Histogram;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2022-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    /// <summary>
    /// A progress item for a custom milestone.
    /// </summary>
    [Serializable]
    public class CustomRatingProgressItem : MilestoneItem
    {
        #region Properties
        /// <summary>
        /// The name of the canned query to match against
        /// </summary>
        public string QueryName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the field or propertyID to sum up
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// The user-displayed name for the field or property to sum up.  Not persisted
        /// </summary>
        [JsonIgnore]
        public string FieldFriendlyName { get; set; } = string.Empty;

        private readonly Dictionary<string, object> Context = new Dictionary<string, object>();
        #endregion

        public CustomRatingProgressItem() : base() 
        {
            // Currently only support time based milestones.
            Type = MilestoneType.Time;
        }

        public void ExamineFlight(LogbookEntryDisplay led, IDictionary<string, CannedQuery> dQueries)
        {
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            if (dQueries == null)
                throw new ArgumentNullException(nameof(dQueries));

            // Empty query name = all flights
            if (!dQueries.TryGetValue(QueryName, out CannedQuery query) && !String.IsNullOrWhiteSpace(QueryName))
            {
                Progress = 0;
                FARRef = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.ErrCustomProgressQueryNotFound.ToUpper(CultureInfo.CurrentCulture), QueryName);
            } else if (query == null || query.MatchesFlight(led))
                Progress += (decimal)led.HistogramValue(FieldName, Context);
        }

        /// <summary>
        /// Returns a marked-down string representation of the item
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CustomProgressItemTemplate,
                FARRef, Title, Threshold, FieldFriendlyName, String.IsNullOrWhiteSpace(QueryName) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.CustomProgressItemTemplateQueryApplied, QueryName));
        }
    }

    /// <summary>
    /// Group for custom ratings: encapsulates all custom ratings
    /// </summary>
    [Serializable]
    public class CustomRatingsGroup : MilestoneGroup
    {
        private readonly Collection<MilestoneProgress> m_ratings = new Collection<MilestoneProgress>();
        public CustomRatingsGroup(string szUser) : base()
        {
            GroupName = Resources.MilestoneProgress.CustomProgressSection;
            IEnumerable<MilestoneProgress> rg = CustomRatingProgress.CustomRatingsForUser(szUser);
            foreach (MilestoneProgress rating in rg)
                m_ratings.Add(rating);
        }

        public override Collection<MilestoneProgress> Milestones => m_ratings;
    }

    /// <summary>
    /// A specific custom rating - encapsulates a bunch of customratingprogressitems (milestone items)
    /// </summary>
    [Serializable]
    public class CustomRatingProgress : MilestoneProgress, IComparable<CustomRatingProgress>
    {
        #region Properties
        public IEnumerable<CustomRatingProgressItem> ProgressItems { get; set; } = Array.Empty<CustomRatingProgressItem>();
        #endregion

        private Dictionary<string, CannedQuery> dQueries;
        private Dictionary<string, CannedQuery> UserQueries
        {
            get
            {
                if (dQueries == null)
                {
                    dQueries = new Dictionary<string, CannedQuery>();
                    IEnumerable<CannedQuery> queries = CannedQuery.QueriesForUser(Username);
                    foreach (CannedQuery cq in queries)
                        dQueries[cq.QueryName] = cq;
                }
                return dQueries;
            }
        }

        public CustomRatingProgress(): base() { }

        private const string szPrefKeyCustomRatings = "customRatings";

        public static IEnumerable<CustomRatingProgress> CustomRatingsForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);

            // build a map of all histogrammable values for naming
            List<HistogramableValue> lst = new List<HistogramableValue>(LogbookEntryDisplay.HistogramableValues);

            foreach (CustomPropertyType cpt in CustomPropertyType.GetCustomPropertyTypes())
            {
                HistogramableValue hv = LogbookEntryDisplay.HistogramableValueForPropertyType(cpt);
                if (hv != null)
                    lst.Add(hv);
            }

            Dictionary<string, HistogramableValue> d = new Dictionary<string, HistogramableValue>();
            foreach (HistogramableValue hv in lst)
                d[hv.DataField] = hv;

            // MUST use the pf.GetPreferenceForKey with one argument because you need the serialize/deserialize to get the correct type conversion.
            IEnumerable<CustomRatingProgress> rgProgressForUser = pf.GetPreferenceForKey<IEnumerable<CustomRatingProgress>>(szPrefKeyCustomRatings);   

            if (rgProgressForUser == null)
                return Array.Empty<CustomRatingProgress>();

            foreach (CustomRatingProgress crp in rgProgressForUser)
            {
                crp.Username = szUser;
                foreach (CustomRatingProgressItem crpi in crp.ProgressItems)
                    crpi.FieldFriendlyName = d.TryGetValue(crpi.FieldName, out HistogramableValue hv) ? hv.DataName : crpi.FieldName;

                List<CustomRatingProgressItem> lstItems = new List<CustomRatingProgressItem>(crp.ProgressItems);
                lstItems.Sort((crpi1, crpi2) => {return crpi1.FARRef.CompareCurrentCultureIgnoreCase(crpi2.FARRef);});
                crp.ProgressItems = lstItems;
            }

            return rgProgressForUser;
        }

        private static List<CustomRatingProgress> RatingsWithNamedRating(string szUser, string szTitle, out CustomRatingProgress crp)
        {
            string szSearch = szTitle.Trim();
            List<CustomRatingProgress> lst = new List<CustomRatingProgress>(CustomRatingsForUser(szUser));
            crp = lst.FirstOrDefault(c => c.Title.Trim().CompareCurrentCulture(szSearch) == 0);
            return lst;
        }

        /// <summary>
        /// Deletes the named custom rating progress for the specified user.
        /// </summary>
        /// <param name="szName"></param>
        /// <param name="szUser"></param>
        /// <returns>An enumerable of the remaining custom ratings progress</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<CustomRatingProgress> DeleteRatingForUser(string szTitle, string szUser)
        {
            if (String.IsNullOrEmpty(szTitle))
                throw new ArgumentNullException(nameof(szTitle));
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            List<CustomRatingProgress> lst = new List<CustomRatingProgress>(CustomRatingsForUser(szUser));
            lst.RemoveAll(crp => crp.Title.CompareCurrentCulture(szTitle) == 0);
            CommitRatingsForUser(szUser, lst);
            return lst;
        }

        /// <summary>
        /// Creates a rating for the user
        /// </summary>
        /// <param name="szTitle">REQUIRED - title.  This is what will be looked up (in case of an update) or used (for a new rating)</param>
        /// <param name="szFAR">Regulatory reference</param>
        /// <param name="szDisclaimer"></param>
        /// <param name="szUser">REQUIRED - username</param>
        /// <param name="szNewTitle">Optional - if presented, this will be the NEW title.  Ignored if szTitle was not found.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<CustomRatingProgress> UpdateRatingForUser(string szUser, string szTitle, string szFAR, string szDisclaimer, string szNewTitle = null)
        {
            if (String.IsNullOrWhiteSpace(szTitle))
                throw new ArgumentNullException(nameof(szTitle));
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            List<CustomRatingProgress> lst = RatingsWithNamedRating(szUser, szTitle, out CustomRatingProgress crp);
            if (crp == null)
                lst.Add(new CustomRatingProgress() { Title = szTitle, FARLink = szFAR ?? string.Empty, GeneralDisclaimer = szDisclaimer ?? string.Empty });
            else
            {
                crp.Title = szNewTitle ?? szTitle;
                crp.FARLink = szFAR ?? string.Empty;
                crp.GeneralDisclaimer = szDisclaimer ?? string.Empty;
            }
            lst.Sort();
            CommitRatingsForUser(szUser, lst);
            return lst;
        }

        public static CustomRatingProgress AddMilestoneForRating(string szUser, string szProgress, string szTitle, string szFARRef, string szNote, decimal threshold, string queryName, string fieldName, string fieldFrendlyName)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (String.IsNullOrEmpty(szProgress))
                throw new ArgumentNullException(nameof(szProgress));
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), Resources.MilestoneProgress.CustomProgressNewMilestoneThresholdRequired);

            IEnumerable<CustomRatingProgress> lst = RatingsWithNamedRating(szUser, szProgress, out CustomRatingProgress crp);
            crp.AddMilestoneItem(new CustomRatingProgressItem()
            {
                Title = szTitle,
                FARRef = szFARRef,
                Note = szNote,
                Threshold = threshold,
                QueryName = queryName,
                FieldName = fieldName,
                FieldFriendlyName = fieldFrendlyName
            });
            CommitRatingsForUser(szUser, lst);
            return crp;
        }

        public static CustomRatingProgress DeleteMilestoneForRating(string szUser, string szProgress, int milestoneIndex)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (String.IsNullOrEmpty(szProgress))
                throw new ArgumentNullException(nameof(szProgress));

            IEnumerable<CustomRatingProgress> lst = RatingsWithNamedRating(szUser, szProgress, out CustomRatingProgress crp);
            crp.RemoveMilestoneAt(milestoneIndex);
            CommitRatingsForUser(szUser, lst);
            return crp;
        }

        public static CustomRatingProgress CustomRatingWithName(string szTitle, string szUser)
        {
            return CustomRatingsForUser(szUser).FirstOrDefault(crp => crp.Title.CompareCurrentCulture(szTitle) == 0);
        }

        public static void CommitRatingsForUser(string szUser, IEnumerable<CustomRatingProgress> rgIn)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);

            pf.SetPreferenceForKey(szPrefKeyCustomRatings, rgIn, rgIn == null || !rgIn.Any());
        }

        public void AddMilestoneItem(CustomRatingProgressItem crpi)
        {
            if (crpi == null)
                throw new ArgumentNullException(nameof(crpi));

            ProgressItems = new List<CustomRatingProgressItem>(ProgressItems) { crpi };
        }

        public void RemoveMilestoneAt(int index)
        {
            List<CustomRatingProgressItem> lst = new List<CustomRatingProgressItem>(ProgressItems);
            lst.RemoveAt(index);    // will throw an exception if out of range
            ProgressItems = lst;
        }

        [JsonIgnore]
        public override Collection<MilestoneItem> Milestones
        {
            get
            {
                Collection<MilestoneItem> c = new Collection<MilestoneItem>();
                foreach (MilestoneItem mi in ProgressItems)
                    c.Add(mi);
                return c;
            }
        }

        /// <summary>
        /// Performs the computation on the milestones to see what progress has been made for each.  We MUST use LogbookEntryDisplays, so we override the base class (which uses ExaminerFlightRow)
        /// </summary>
        /// <returns>The resulting milestones.</returns>
        /// <exception cref="MyFlightbookException"></exception>
        public override Collection<MilestoneItem> Refresh()
        {
            if (String.IsNullOrEmpty(Username))
                throw new MyFlightbookException("Cannot compute milestones on an empty user!");

            StringBuilder sbRoutes = new StringBuilder();
            Profile pf = Profile.GetUser(Username);

            IList<LogbookEntryDisplay> lst = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryBase.QueryCommand(new FlightQuery(Username)), Username, string.Empty, System.Web.UI.WebControls.SortDirection.Descending, pf.UsesHHMM, pf.UsesUTCDateOfFlight);

            // Set up the airport list once for DB efficiency
            foreach (LogbookEntryDisplay led in lst)
                sbRoutes.AppendFormat(CultureInfo.InvariantCulture, "{0} ", led.Route);
            AirportListOfRoutes = new AirportList(sbRoutes.ToString());

            IDictionary<string, CannedQuery> d = UserQueries;
            foreach (LogbookEntryDisplay led in lst)
            {
                foreach (CustomRatingProgressItem cpi in ProgressItems)
                    cpi.ExamineFlight(led, d);
            };

            return Milestones;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(CustomRatingProgress other)
        {
            if (other == null) 
                throw new ArgumentNullException(nameof(other));
            return this.Title.CompareTo(other.Title);
        }
    }
}
