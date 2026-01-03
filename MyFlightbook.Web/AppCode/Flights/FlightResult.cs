using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Class to contain a set of flight results.  Each distinct query performed by a user is cached here.
    /// You can request results for a query; if in the cache, it will be returned from there.  If not, the database will be hit.
    /// Each result set will be stored in the cache, so can be reclaimed (this also provides a time limit that may be less than the owning user's existance in the cache).
    /// </summary>
    public class FlightResultManager
    {
        #region Properties
        private readonly Dictionary<string, string> dictResultReferences = new Dictionary<string, string>();
        #endregion

        public FlightResultManager()
        {
        }

        public FlightResult ResultsForQuery(FlightQuery fq)
        {
            if (fq == null) 
                throw new ArgumentNullException(nameof(fq));

            string szKey = fq.ToJSONString();
            FlightResult fr = null;
            if (dictResultReferences.TryGetValue(szKey, out string cacheKey))
                fr = (FlightResult) util.GlobalCache.Get(cacheKey);

            if (fr == null)
            {
                fr = new FlightResult(Profile.GetUser(fq.UserName), fq);
                string szCacheKey = String.IsNullOrEmpty(cacheKey) ? Guid.NewGuid().ToString() : cacheKey;
                dictResultReferences[szKey] = szCacheKey;   // keep a reference to the cache itself
                util.GlobalCache.Set(szCacheKey, fr, DateTimeOffset.UtcNow.AddMinutes(10));
            }
            return fr;
        }

        #region User management, fetching/caching objects
        private const string userFlightResultManagerKey = "flightResultManager";

        /// <summary>
        /// Retrieves the flight result manager for the specified user, creating it if needed.
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static FlightResultManager FlightResultManagerForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            Profile pf = Profile.GetUser(szUser);
            if (String.IsNullOrEmpty(pf.UserName))
                throw new InvalidOperationException("No such user - " + szUser);
            if (pf.AssociatedData.TryGetValue(userFlightResultManagerKey, out object o))
                return o as FlightResultManager;
            else
            {
                FlightResultManager frm = new FlightResultManager();
                pf.AssociatedData[userFlightResultManagerKey] = frm;
                return frm;
            }
        }

        /// <summary>
        /// If ANYTHING changes in user's logbook, need to invalidate and discard all cached objects.
        /// </summary>
        public void Invalidate()
        {
            // remove any potentially dangling references
            foreach (string key in dictResultReferences.Keys)
                util.GlobalCache.Remove(dictResultReferences[key]);

            dictResultReferences.Clear();
        }

        /// <summary>
        /// Gets the flightResultManager for the specified user and then invalidates based on that.
        /// </summary>
        /// <param name="szUser"></param>
        public static void InvalidateForUser(string szUser)
        {
            FlightResultManagerForUser(szUser).Invalidate();
        }
        #endregion
    }

    /// <summary>
    /// Class to contain a set of flight results.  Do not create directly (hits the database); use FlightResultManager to get this.
    /// Handles:
    ///  - Actual list flights (Flights property)
    ///  - Sorting
    ///  - Paging
    /// </summary>
    public class FlightResult
    {
        #region Properties
        private readonly List<LogbookEntryDisplay> FlightsList;

        /// <summary>
        /// The flights in this result set.
        /// </summary>
        public IEnumerable<LogbookEntryDisplay> Flights { get { return FlightsList; } }

        /// <summary>
        /// Number of flights
        /// </summary>
        public int FlightCount { get { return FlightsList.Count; } }

        /// <summary>
        /// The current sort key for the table; if this doesn't match the requested sort, we will sort.
        /// </summary>
        public string CurrentSortKey { get; set; }

        /// <summary>
        /// The current sort direction
        /// </summary>
        public SortDirection CurrentSortDir { get; set; }
        #endregion

        #region sorting

        /// <summary>
        /// Sorts the list on the specified key, reversing sort if the specified key is already the sort key
        /// </summary>
        /// <param name="sortKey"></param>
        public void SortFlights(string sortKey)
        {
            SortDirection sortDir = CurrentSortKey.CompareCurrentCultureIgnoreCase(sortKey) == 0 ? (CurrentSortDir == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending) : SortDirection.Ascending;
            SortFlights(sortKey, sortDir);
        }

        public string CssForSort(string sortKey)
        {
            if (sortKey == null)
                throw new ArgumentNullException(nameof(sortKey));
            return sortKey.CompareCurrentCultureIgnoreCase(CurrentSortKey) == 0 ? (CurrentSortDir == SortDirection.Ascending ? "headerSortAsc" : "headerSortDesc") : string.Empty;
        }

        /// <summary>
        /// Sorts the list on the specified key and direction.
        /// </summary>
        /// <param name="sortKey"></param>
        /// <param name="sortDir"></param>
        public void SortFlights(string sortKey, SortDirection sortDir)
        {
            FlightsList.Sort((l1, l2) => { return LogbookEntryCore.CompareFlights(l1, l2, sortKey, sortDir); });
            CurrentSortKey = sortKey;
            CurrentSortDir = sortDir;
        }

        /// <summary>
        /// Find the flight with the given index in the results.
        /// </summary>
        /// <param name="idFlight"></param>
        /// <param name="idFlightMinus1">The id of the flight at index minus 1</param>
        /// <param name="idFlightPlus1">The id of the flight at index plus 1</param>
        /// <returns>The index of the found flight, if any</returns>
        public int IndexOfFlightID(int idFlight, out int idFlightPlus1, out int idFlightMinus1)
        {
            int index = FlightsList.FindIndex(le => le.FlightID == idFlight);
            idFlightPlus1 = (index >= 0 && index < FlightsList.Count - 1) ? FlightsList[index + 1].FlightID : LogbookEntryCore.idFlightNone;
            idFlightMinus1 = (index > 0) ? FlightsList[index - 1].FlightID : LogbookEntryCore.idFlightNone;
            return index;
        }
        #endregion

        #region Range and page management
        /// <summary>
        /// Returns the number of pages in the result for the specified page size.
        /// Note that no elements = 1 page, 1 element = 1 page, pageSize elements = 1 page, pageSize + 1 elements = 2 pages, and so forth.
        /// </summary>
        /// <param name="pageSize"># of flights on a page.  -1 (or any non-positive number) = no limit (i.e., all flights on 1 page)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int PageCount(int pageSize)
        {
            return (pageSize <= 0) ? 1 : Math.Max(1, (FlightsList.Count + pageSize - 1) / pageSize);
        }

        /// <summary>
        /// Returns the range of elements for the specified *0-based* page.
        /// </summary>
        /// <param name="page">The page (0-based).  i.e., the first element is on page 0</param>
        /// <param name="pageSize"># of elements on a page, -1 for all</param>
        /// <returns>A FlightResultRange describing the requested range.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private FlightResultRange RangeForPage(int page, int pageSize)
        {
            if (pageSize <= 0)
                return new FlightResultRange() { StartIndex = 0, Count = FlightsList.Count, PageCount = 1, PageNum = 0 };

            int cPages = PageCount(pageSize);

            // keep page between 0 and cPages - 1
            page = Math.Min(Math.Max(page, 0), cPages - 1);

            FlightResultRange range = new FlightResultRange() { StartIndex = page * pageSize, PageNum = page, PageCount = PageCount(pageSize) };
            range.Count = Math.Min(range.StartIndex + pageSize, FlightsList.Count) - range.StartIndex;
            return range;
        }

        /// <summary>
        /// Finds the page containing the specified flight.
        /// </summary>
        /// <param name="pageSize">Page size (# of flights per page)</param>
        /// <param name="idFlight">The flight being sought</param>
        /// <returns>A FlightResultRange that contains the requested flight</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public FlightResultRange RangeContainingFlight(int pageSize, int idFlight)
        {
            int index = Math.Max(0, FlightsList.FindIndex(led => led.FlightID == idFlight));

            return pageSize <= 0 ? RangeForPage(0, pageSize) : RangeForPage(index / pageSize, pageSize);
        }

        /// <summary>
        /// Returns the ID's of next/previous flight to the specified flight, or idFlightNone if not found or at end of list.
        /// </summary>
        /// <param name="idFlight">The flight to find</param>
        /// <returns>An enumerable of 2 ids.  The first is the "previous" flight or idFlightNone, the 2nd is the "next" flight or idFlightNone</returns>
        public int[] NeighborsOfFlight(int idFlight)
        {
            int[] result = new int[] { LogbookEntryCore.idFlightNone, LogbookEntryCore.idFlightNone };
            int index = FlightsList.FindIndex(led => led.FlightID == idFlight);
            if (index > 0)
                result[0] = FlightsList[index - 1].FlightID;
            if (index < FlightsList.Count - 1)
                result[1] = FlightsList[index  + 1].FlightID;
            return result;
        }

        /// <summary>
        /// Finds the ID's of the flights immediately adjacent to the specified flight ID in the list (given its current sort order and restriction)
        /// </summary>
        /// <param name="idFlight">The ID of the flight whose neighbors are being sought</param>
        /// <param name="idPrev">The ID (if any) of the previous flight in the list</param>
        /// <param name="idNext">The ID (if any) of the next flight in the list.</param>
        public void GetNeighbors(int idFlight, out int idPrev, out int idNext)
        {
            idPrev = idNext = LogbookEntryCore.idFlightNone;

            int index = FlightsList.FindIndex(led => led.FlightID == idFlight);

            if (index > 0)
                idNext = FlightsList[index - 1].FlightID;
            if (index < FlightsList.Count - 1)
                idPrev = FlightsList[index + 1].FlightID;
        }

        /// <summary>
        /// Return the 0-based page number that corresponds to what the user types into the edit field in the pager for flights.
        /// Allowed values are:
        ///  - Small integers: indicates to go to that page directly
        ///  - Years (integers between 1900 and 2200) - goes to the first page of flights that contain that year
        ///  - Date - goes to the first page that contains that date.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="maxPage"></param>
        /// <returns></returns>
        private int PageFromSearch(string query, int pageSize, int maxPage, int curPage)
        {
            DateTime? dtSought = null;
            bool fIsYear = false;
            if (int.TryParse(query, NumberStyles.Integer, CultureInfo.CurrentCulture, out var integerVal))
            {
                if (fIsYear = (integerVal > 1900 && integerVal < 2200))
                    // treat it as a year
                    dtSought = new DateTime(integerVal, 12, 31); 
                else                                        
                    // treat it as a page #, but subtract 1 to make it 0 based
                    return Math.Max(Math.Min(integerVal, maxPage), 1) - 1;
            }
            else if (DateTime.TryParse(query, out DateTime dateVal))
                dtSought = dateVal;

            if (dtSought == null)
                return curPage;

            int yearAttempted = fIsYear ? integerVal : 0;

            // If we're going to do a date-based search, better sort on date!
            SortFlights(LogbookEntry.DefaultSortKey, LogbookEntry.DefaultSortDir);

            int distance = Int32.MaxValue;
            int iRowMatch = 0;

            // Find the entry with the date or year closest to the typed date.
            for (int iRow = 0; iRow < FlightsList.Count; iRow++)
            {
                DateTime dtEntry = FlightsList[iRow].Date;

                int distThis = (fIsYear) ? Math.Abs(dtEntry.Year - yearAttempted) : (int)Math.Abs(dtEntry.Subtract(dtSought.Value).TotalDays);

                if (distThis < distance)
                {
                    distance = distThis;
                    iRowMatch = iRow;
                }
            }

            return iRowMatch / pageSize;
        }

        /// <summary>
        /// Gets a page of results.  Enforces the correct sorting, in case multiple instances have viewed the same data with differing sorts.
        /// </summary>
        /// <param name="curPage">The CURRENT page, for requesting next/previous.  MUST be between 0 and the number of pages - 1</param>
        /// <param name="pageSize">Page size, <= 0 indicates one page (i.e., "show all")</param>
        /// <param name="query">Query from user; only relevant for FlightRangeType=Search.  E.g., this might be a page number, a year, or a date</param>
        /// <param name="rangeType">The kind of range being sought</param>
        /// <param name="sortDir">Direction for sorting</param>
        /// <param name="sortKey">Key for sorting</param>
        /// <returns></returns>
        public FlightResultRange GetResultRange(int pageSize, FlightRangeType rangeType, string sortKey, SortDirection sortDir, int curPage = 0, string query = null)
        {
            if (pageSize <= 0)
                pageSize = FlightsList.Count;

            if (rangeType == FlightRangeType.Search && query == null)
                throw new ArgumentNullException(nameof(query), "Query must be provided with rangeType = search");

            // Ensure we're working with the correct sort order
            if (sortKey.CompareCurrentCultureIgnoreCase(CurrentSortKey) != 0 || sortDir != CurrentSortDir)
                SortFlights(sortKey, sortDir);

            int cPages = PageCount(pageSize);

            switch (rangeType)
            {
                case FlightRangeType.First:
                    return RangeForPage(0, pageSize);
                case FlightRangeType.Last:
                    return RangeForPage(cPages - 1, pageSize);
                case FlightRangeType.Next:
                    return RangeForPage(curPage + 1, pageSize);
                case FlightRangeType.Prev:
                    return RangeForPage(curPage - 1, pageSize);
                case FlightRangeType.Page:
                    return RangeForPage(Math.Max(0, Math.Min(curPage, cPages - 1)), pageSize);
                case FlightRangeType.Search:
                    return RangeForPage(PageFromSearch(query, pageSize, cPages, curPage), pageSize);
                default:
                    throw new NotImplementedException("unknown range type: " + rangeType.ToString());
            }
        }

        /// <summary>
        /// Returns the subset of flights in the specified range.  The range should have been retrieved from GetResultRange
        /// </summary>
        /// <param name="range">The range</param>
        /// <returns></returns>
        public IEnumerable<LogbookEntryDisplay> FlightsInRange(FlightResultRange range)
        {
            if (range == null)
                throw new ArgumentNullException(nameof(range));
            return FlightsList.GetRange(range.StartIndex, range.Count);
        }
        #endregion

        #region Object creation
        public FlightResult(Profile pf, FlightQuery fq, string sortKey = LogbookEntry.DefaultSortKey, SortDirection sortDir = LogbookEntry.DefaultSortDir)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            CurrentSortKey = sortKey;
            CurrentSortDir = sortDir;

            FlightsList = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryBase.QueryCommand(fq), fq.UserName, CurrentSortKey, CurrentSortDir, pf.UsesHHMM, pf.UsesUTCDateOfFlight) as List<LogbookEntryDisplay>;
        }
        #endregion
    }

    /// <summary>
    /// Kinds of ways to page through results: 1st page, last page, previous page, next page, or search for page number, year, or date
    /// </summary>
    public enum FlightRangeType { First, Last, Prev, Next, Page, Search }

    /// <summary>
    /// Provides a view onto a flightresult so that you know which flights to display and what page they are on.
    /// </summary>
    [Serializable]
    public class FlightResultRange
    {
        #region Properties
        /// <summary>
        /// Index of the first result
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Number of results
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 0-based Page number of the start index
        /// </summary>
        public int PageNum { get; set; }


        public int PageCount { get; set; }
        #endregion

        public FlightResultRange() { }
    }
}