using MyFlightbook;
using MyFlightbook.Achievements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_MFBLogbookBase : UserControl
{
    const string szFQViewStateKeyPrefix = "FQ";
    const string szKeyLastSortExpr = "LastSort";
    const string szKeylastSortDir = "LastSortDir";
    private Profile m_pfPilot;
    private Profile m_pfUser;

    #region Properties that DON'T Depend on controls on the page
    private string RestrictionVSKey { get { return szFQViewStateKeyPrefix + ID; } }

    /// <summary>
    /// Sets/Gets a FlightQuery to restrict which flights are shown
    /// </summary>
    public FlightQuery Restriction
    {
        get
        {
            string key = RestrictionVSKey;
            if (ViewState[key] == null)
                ViewState[key] = new FlightQuery(String.IsNullOrEmpty(User) ? Page.User.Identity.Name : this.User);
            return (FlightQuery)ViewState[key];
        }
        set { ViewState[RestrictionVSKey] = value; }
    }

    protected IEnumerable<CannedQuery> colorQueryMap { get; set; }

    private const string szViewStateSuppressImages = "vsSI";
    /// <summary>
    /// True for this to be "print view": suppresses images
    /// </summary>
    public Boolean SuppressImages
    {
        get { return !String.IsNullOrEmpty((string)ViewState[szViewStateSuppressImages]); }
        set { ViewState[szViewStateSuppressImages] = value ? value.ToString(CultureInfo.InvariantCulture) : string.Empty; }
    }
    public IEnumerable<LogbookEntryDisplay> DirectData { get; set; }

    private const string keyVSUser = "logbookUser";
    /// <summary>
    /// Specifies the user for whom we are displaying results.
    /// </summary>
    public string User
    {
        get { return (string)ViewState[keyVSUser]; }
        set { ViewState[keyVSUser] = value; }
    }

    /// <summary>
    /// cached for performance
    /// </summary>
    protected List<Aircraft> AircraftForUser { get; } = new List<Aircraft>();

    protected Boolean HasPrevSort
    {
        get { return ViewState[szKeylastSortDir + ID] != null && ViewState[szKeyLastSortExpr + ID] != null; }
    }

    public string LastSortExpr
    {
        get
        {
            object o = ViewState[szKeyLastSortExpr + ID];
            return (o == null) ? string.Empty : o.ToString();
        }
        set { ViewState[szKeyLastSortExpr + ID] = value; }
    }

    public SortDirection LastSortDir
    {
        get
        {
            object o = ViewState[szKeylastSortDir + ID];
            return (o == null) ? LogbookEntry.DefaultSortDir : (SortDirection)o;
        }
        set { ViewState[szKeylastSortDir + ID] = value; }
    }

    #region URL templates for clickable items.
    // backing variables for where to go when clicking date, paper clip, or menu items
    private string m_szDetailsPageTemplate = "~/Member/FlightDetail.aspx/{0}";
    private string m_szEditPageTemplate = "~/member/LogbookNew.aspx/{0}";
    private string m_szAnalysisPageTemplate = "~/member/FlightDetail.aspx/{0}?tabID=Chart";
    private string m_szPublicRouteTemplate = "~/Public/ViewPublicFlight.aspx/{0}";

    /// <summary>
    /// The URL for details page, ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
    public string DetailsPageUrlFormatString
    {
        get { return m_szDetailsPageTemplate; }
        set { m_szDetailsPageTemplate = value; }
    }

    /// <summary>
    /// URL template to edit the flight; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
    public string EditPageUrlFormatString
    {
        get { return m_szEditPageTemplate; }
        set { m_szEditPageTemplate = value; }
    }

    /// <summary>
    /// URL template for flight analysis; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
    public string AnalysisPageUrlFormatString
    {
        get { return m_szAnalysisPageTemplate; }
        set { m_szAnalysisPageTemplate = value; }
    }

    /// <summary>
    /// URL Template for public page; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
    public string PublicPageUrlFormatString
    {
        get { return m_szPublicRouteTemplate; }
        set { m_szPublicRouteTemplate = value; }
    }

    /// <summary>
    /// parameter string to append to the URL for edit/target/analysis
    /// </summary>
    public string DetailsParam { get; set; }
    #endregion


    protected MyFlightbook.Profile Pilot
    {
        get { return m_pfPilot ?? (m_pfPilot = MyFlightbook.Profile.GetUser(this.User)); }
    }

    protected MyFlightbook.Profile Viewer
    {
        get { return m_pfUser ?? (m_pfUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name)); }
    }

    protected bool IsViewingOwnFlights
    {
        get { return String.Compare(Page.User.Identity.Name, this.User, StringComparison.Ordinal) == 0; }
    }

    private const string szVSReSignFlights = "vsReSign";
    /// <summary>
    /// Determines if we allow for re-signing of flights that have valid signatures (requires add permission).
    /// </summary>
    public bool CanResignValidFlights
    {
        get { return (bool)(ViewState[szVSReSignFlights] ?? false); }
        set { ViewState[szVSReSignFlights] = value; }
    }

    public bool IsReadOnly { get; set; }

    protected bool ShowModifiedFlights { get; set; }

    #region Data
    protected bool CacheFlushed { get; set; }

    /// <summary>
    /// Avoid redundant binding by checking in Page_Load if we've already bound by some other event.  Still can find ourselves binding twice here and there, but shouldn't happen by simply loading the page.
    /// </summary>
    protected bool HasBeenBound { get; set; }

    /// <summary>
    /// key to access the logbook data within the session state
    /// </summary>
    protected string CacheKey
    {
        get
        {
            return Page.User.Identity.Name + "mfbLogbookAscx";
        }
    }

    protected void FlushCache()
    {
        Cache.Remove(CacheKey);
        CacheFlushed = true;
    }

    /// <summary>
    /// Caches flights striped by query and user.  So we'll cache all recent queries for this user, flushCache can will simultaneously flush ALL cached results for this user.
    /// I.e., each user has ONE cache object, which contains MULTIPLE results, striped by query.
    /// </summary>
    protected IEnumerable<LogbookEntryDisplay> CachedData
    {
        get
        {
            Dictionary<string, IEnumerable<LogbookEntryDisplay>> cachedValues = (Dictionary<string, IEnumerable<LogbookEntryDisplay>>)Cache[CacheKey];
            if (cachedValues != null)
            {
                string key = Restriction.ToJSONString();
                return (cachedValues.TryGetValue(key, out IEnumerable<LogbookEntryDisplay> value)) ? value : null;
            }
            return null;
        }
        set
        {
            Dictionary<string, IEnumerable<LogbookEntryDisplay>> cachedValues = (Dictionary<string, IEnumerable<LogbookEntryDisplay>>)Cache[CacheKey] ?? new Dictionary<string, IEnumerable<LogbookEntryDisplay>>();
            cachedValues[Restriction.ToJSONString()] = value;
            Cache.Add(CacheKey, cachedValues, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 10, 0), System.Web.Caching.CacheItemPriority.Normal, null);
        }
    }

    /// <summary>
    /// Returns the data, from the cache or via database call if cache misses, or DirectData if that's what's available
    /// </summary>
    public IEnumerable<LogbookEntryDisplay> Data
    {
        get
        {
            if (DirectData != null)
                return DirectData;

            IEnumerable<LogbookEntryDisplay> lst = CachedData;
            if (lst == null)
            {
                if (String.IsNullOrEmpty(this.User))
                    this.User = Page.User.Identity.Name;

                Profile pfUser = MyFlightbook.Profile.GetUser(this.User);
                ShowModifiedFlights = pfUser.PreferenceExists(MFBConstants.keyTrackOriginal);

                lst = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(Restriction), this.User, LastSortExpr, LastSortDir, Viewer.UsesHHMM, pfUser.UsesUTCDateOfFlight);
                CachedData = lst;
            }
            return lst;
        }
    }
    #endregion

    #region Paths for editing, cloning, reversing
    protected string DetailsPath(object idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, DetailsPageUrlFormatString, idFlight) + (String.IsNullOrEmpty(DetailsParam) ? string.Empty : "?" + DetailsParam);
    }

    protected string AnalyzePath(object idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, AnalysisPageUrlFormatString, idFlight);
    }

    protected string PublicPath(object idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, PublicPageUrlFormatString, idFlight);
    }
    #endregion
    #endregion

    /// <summary>
    /// Parameters to add to edit link to preserve context on return.
    /// </summary>
    protected string EditContextParams(int PageIndex)
    {
        System.Collections.Specialized.NameValueCollection dictParams = new System.Collections.Specialized.NameValueCollection();
        // Add the existing keys first - since we may overwrite them below!
        foreach (string szKey in Request.QueryString.Keys)
            dictParams[szKey] = Request.QueryString[szKey];

        // Issue #458: clone and reverse are getting duplicated and the & is getting url encoded, so even edits look like clones
        dictParams.Remove("Clone");
        dictParams.Remove("Reverse");

        if (!Restriction.IsDefault)
            dictParams["fq"] = Restriction.ToBase64CompressedJSONString();
        if (HasPrevSort)
        {
            if (!String.IsNullOrEmpty(LastSortExpr))
                dictParams["se"] = LastSortExpr;
            if (LastSortDir != SortDirection.Descending || !String.IsNullOrEmpty(LastSortExpr))
                dictParams["so"] = LastSortDir.ToString();
        }
        if (PageIndex != 0)
            dictParams["pg"] = PageIndex.ToString(CultureInfo.InvariantCulture);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (string szkey in dictParams)
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}={2}", sb.Length == 0 ? string.Empty : "&", szkey, HttpUtility.UrlEncode(dictParams[szkey]));

        return sb.ToString();
    }

    #region Badges
    private Dictionary<int, List<Badge>> m_cachedBadges;

    private Dictionary<int, List<Badge>> CachedBadgesByFlight
    {
        get
        {
            if (m_cachedBadges != null)
                return m_cachedBadges;

            // Set up cache of badges.
            m_cachedBadges = new Dictionary<int, List<Badge>>();

            IEnumerable<Badge> cachedBadges = Pilot.CachedBadges;
            if (cachedBadges != null)
            {
                foreach (Badge b in cachedBadges)
                {
                    if (b.IDFlightEarned == LogbookEntry.idFlightNone)
                        continue;
                    if (m_cachedBadges.TryGetValue(b.IDFlightEarned, out List<Badge> lst))
                        lst.Add(b);
                    else
                        m_cachedBadges[b.IDFlightEarned] = new List<Badge>() { b };
                }
            }
            return m_cachedBadges;
        }
    }

    protected void SetUpBadgesForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        if (row == null)
            throw new ArgumentNullException(nameof(row));
        if (le == null)
            throw new ArgumentNullException(nameof(le));

        if (Pilot != null && Pilot.AchievementStatus == Achievement.ComputeStatus.UpToDate)
        {
            Repeater rptBadges = (Repeater)row.FindControl("rptBadges");
            if (CachedBadgesByFlight.TryGetValue(le.FlightID, out List<Badge> value))
            {
                IEnumerable<Badge> badges = value;
                if (badges != null)
                {
                    rptBadges.DataSource = badges;
                    rptBadges.DataBind();
                }
            }
        }
    }
    #endregion
}

public partial class Controls_mfbLogbook : Controls_MFBLogbookBase
{
    const string szKeyAllowsPaging = "allowsPaging";
    const string szKeyMiniMode = "minimode";
    private Boolean m_fMiniMode;
    private readonly Dictionary<int, string> m_dictAircraftHoverIDs = new Dictionary<int, string>();

    #region Properties
    public event EventHandler<LogbookEventArgs> ItemDeleted;

    /// <summary>
    /// Display in mini (mobile) mode?
    /// </summary>
    public Boolean MiniMode
    {
        get { return m_fMiniMode; }
        set
        {
            for (int iCol = 2; iCol < gvFlightLogs.Columns.Count - 2; iCol++)
                gvFlightLogs.Columns[iCol].Visible = !m_fMiniMode;
            ViewState[szKeyMiniMode] = m_fMiniMode = value;
        }
    }

    /// <summary>
    /// Provides access to the gridview so that things like page size, etc., can be specified.
    /// </summary>
    public GridView GridView
    {
        get { return gvFlightLogs; }
    }

    /// <summary>
    /// Specifies whether or not paging should be offered
    /// </summary>
    public Boolean AllowPaging
    {
        get { return gvFlightLogs.AllowPaging; }
        set { ViewState[szKeyAllowsPaging] = gvFlightLogs.AllowPaging = value; }
    }

    /// <summary>
    /// Path to which flights should be sent.
    /// </summary>
    public string SendPageTarget
    {
        get { return mfbSendFlight.SendPageTarget; }
        set { mfbSendFlight.SendPageTarget = value; }
    }

    protected bool IsInSelectMode
    {
        get { return ckSelectFlights.Checked; }
        set { divMulti.Visible = ckSelectFlights.Checked = value; }
    }

    private const string szVSSelectAllState = "szvsSelectALL";

    protected bool AllSelected
    {
        get { return (bool)(ViewState[szVSSelectAllState] ?? false); }
        set { ViewState[szVSSelectAllState] = value; }
    }

    protected IEnumerable<int> SelectedItems
    {
        get
        {
            List<int> lst = new List<int>();
            if (!String.IsNullOrWhiteSpace(hdnSelectedItems.Value))
            {
                string[] rgIDs = hdnSelectedItems.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sz in rgIDs)
                    lst.Add(Convert.ToInt32(sz, CultureInfo.InvariantCulture));
            }
            return lst;
        }
        set
        {
            hdnSelectedItems.Value = (value == null) ? string.Empty : String.Join(",", value);
        }
    }

    protected HashSet<int> BoundItems { get; private set; }

    /// <summary>
    /// Finds the ID's of the flights immediately adjacent to the specified flight ID in the list (given its current sort order and restriction)
    /// </summary>
    /// <param name="idFlight">The ID of the flight whose neighbors are being sought</param>
    /// <param name="idPrev">The ID (if any) of the previous flight in the list</param>
    /// <param name="idNext">The ID (if any) of the next flight in the list.</param>
    public void GetNeighbors(int idFlight, out int idPrev, out int idNext)
    {
        idPrev = idNext = LogbookEntry.idFlightNone;

        if (!(Data is List<LogbookEntryDisplay> lst))
            return;

        int index = lst.FindIndex(led => led.FlightID == idFlight);

        if (index > 0)
            idNext = lst[index - 1].FlightID;
        if (index < lst.Count - 1)
            idPrev = lst[index + 1].FlightID;
    }

    /// <summary>
    /// Ensure that the specified flight is in view, if it is in the list of flights.
    /// </summary>
    /// <param name="idFlight"></param>
    public void ScrollToFlight(int idFlight)
    {
        if (!(Data is List<LogbookEntryDisplay> lst))
            return;

        int index = lst.FindIndex(led => led.FlightID == idFlight);

        if (index >= 0 && index < lst.Count)
            gvFlightLogs.PageIndex = (index / gvFlightLogs.PageSize);
    }

    #region Display Options
    const string szCookieCompact = "mfbLogbookDisplayCompact";
    const string szCookieImages = "mfbLogbookDisplayImages";
    const string szCookiesFlightsPerPage = "mfbLogbookDisplayFlightsPerPage";

    private bool m_isCompact;
    private bool m_showImagesInline;

    protected bool IsCompact
    {
        get { return m_isCompact; }
        set { Viewer.SetPreferenceForKey(szCookieCompact, m_isCompact = value, value == false); }
    }

    protected bool ShowImagesInline
    {
        get { return m_showImagesInline; }
        set { Viewer.SetPreferenceForKey(szCookieImages, m_showImagesInline = value, value == false); }
    }

    protected int FlightsPerPage
    {
        get { return gvFlightLogs.PageSize; }
        set { Viewer.SetPreferenceForKey(szCookiesFlightsPerPage, gvFlightLogs.PageSize = value, value == 0 || value == 25); }
    }
    #endregion
    #endregion

    /// <summary>
    /// Get the index of the column containing the specied header text
    /// </summary>
    /// <param name="gv">Gridview</param>
    /// <param name="szColName">Header text</param>
    /// <returns>column id, or -1</returns>
    private static int FindColumn(GridView gv, string szColName)
    {
        int i = gv.Columns.Count;
        while (--i >= 0)
        {
            if (String.Compare(gv.Columns[i].HeaderText, szColName, StringComparison.CurrentCultureIgnoreCase) == 0)
                return i;
        }
        return i;
    }

    protected void BindData(IEnumerable<LogbookEntryDisplay> lst = null)
    {
        // Reset the cache for aircraft hover
        m_dictAircraftHoverIDs.Clear();

        if (lst != null)
            gvFlightLogs.DataSource = lst;

        AircraftForUser.Clear();
        AircraftForUser.AddRange(new UserAircraft(User).GetAircraftForUser());
        BoundItems = new HashSet<int>();
        colorQueryMap = null;   // fetch on each databind
        gvFlightLogs.DataBind();
        HasBeenBound = true;
    }

    public void RefreshNumFlights()
    {
        IEnumerable<LogbookEntryDisplay> flights = CachedData;  // should always be non-null, but...
        int cFlights = flights == null ? 0 : flights.Count();
        pnlHeader.Visible = cFlights > 0;
        lblNumFlights.Text = cFlights == 1 ? Resources.LogbookEntry.NumberOfFlightsOne : String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.NumberOfFlights, cFlights);
    }

    /// <summary>
    /// Publicly visible force-refresh method.  ALWAYS bypasses the cache and uses provided data or hits the database
    /// </summary>
    public void RefreshData()
    {
        // Reset any cached paging/sorting
        LastSortDir = LogbookEntry.DefaultSortDir;
        LastSortExpr = string.Empty;
        gvFlightLogs.PageIndex = 0;

        FlushCache();
        BindData(Data);
        RefreshNumFlights();
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        // Set display options
        // Compact and inline images default to cookies, if not explicitly set in database; otherwise
        // Pagesize only uses database
        m_isCompact = Viewer.PreferenceExists(szCookieCompact) ? Viewer.GetPreferenceForKey<bool>(szCookieCompact) : (Request.Cookies[szCookieCompact] != null && bool.TryParse(Request.Cookies[szCookieCompact].Value, out bool f) && f);
        m_showImagesInline = Viewer.PreferenceExists(szCookieCompact) ? Viewer.GetPreferenceForKey<bool>(szCookieImages) : (Request.Cookies[szCookieImages] != null && bool.TryParse(Request.Cookies[szCookieImages].Value, out bool f2) && f2);

        // Clear the cookies so that from now one we will only use the database preference
        if (Request.Cookies[szCookieCompact] != null)
            IsCompact = m_isCompact;    // force save to profile
        if (Request.Cookies[szCookieImages] != null)
            ShowImagesInline = m_showImagesInline; // force save to profile
        Response.Cookies[szCookieCompact].Expires = Response.Cookies[szCookieImages].Expires = Response.Cookies[szCookiesFlightsPerPage].Expires = DateTime.Now.AddDays(-1);

        int defPageSize = (int)Viewer.GetPreferenceForKey<int>(szCookiesFlightsPerPage);
        gvFlightLogs.PageSize = (defPageSize == 0) ? 25 : defPageSize;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(this.User))
            User = Page.User.Identity.Name;

        if (ViewState[szKeyMiniMode] != null)
            MiniMode = (Boolean)ViewState[szKeyMiniMode];

        if (ViewState[szKeyAllowsPaging] != null)
            AllowPaging = Convert.ToBoolean(ViewState[szKeyAllowsPaging], CultureInfo.InvariantCulture);

        // need to do bind to the data every time or else the comments and images (dynamically added) disappear.
        // We also don't want to overuse viewstate - can get quite big quite quickly.
        // So we cache the logbook in the Session and use it on postbacks.  
        // always hit the DB on a GET unless host page has already called RefreshData (in which case we have already hit the DB)
        if (!IsPostBack)
        {
            lblAddress.Text = Pilot.Address;
            pnlAddress.Visible = Pilot.Address.Trim().Length > 0;

            if (!CacheFlushed)
                FlushCache();

            // Customer-facing utility function: if you add "dupesOnly=1" to the URL, we add a custom restriction that limits flights to ONLY flights that look like potential duplicates
            if (util.GetIntParam(Request, "dupesOnly", 0) != 0)
            {
                Restriction.EnumeratedFlights = LogbookEntry.DupeCandidatesForUser(Restriction.UserName);
                FlushCache();
                HasBeenBound = false;
            }

            ckCompactView.Checked = m_isCompact;
            ckIncludeImages.Checked = m_showImagesInline;
            rblShowInPages.Checked = gvFlightLogs.AllowPaging;
            rblShowAll.Checked = !rblShowInPages.Checked;
            ckSelectFlights.Visible = IsViewingOwnFlights;

            decPageSize.IntValue = gvFlightLogs.PageSize;
            decPageSize.EditBox.MaxLength = 2;

            // Refresh state from params.  
            // fq is handled at the host level.
            string szLastSort = util.GetStringParam(Request, "so");
            if (!String.IsNullOrEmpty(szLastSort) && Enum.TryParse<SortDirection>(szLastSort, true, out SortDirection sortDirection))
                LastSortDir = sortDirection;
            string szSortExpr = util.GetStringParam(Request, "se");
            if (!String.IsNullOrEmpty(szSortExpr))
                LastSortExpr = szSortExpr;
            gvFlightLogs.PageIndex = util.GetIntParam(Request, "pg", gvFlightLogs.PageIndex);

            if (!String.IsNullOrEmpty(LastSortExpr) || gvFlightLogs.PageIndex > 0)
                SortGridview(gvFlightLogs, Data as List<LogbookEntryDisplay>);
        }

        if (!HasBeenBound && !String.IsNullOrEmpty(User))
            BindData(Data);

        // Issue #972: gridview databind performance BLOWS once we get over a few hundred rows, so disable update panel if more than, say, 500 rows
        if (Data.Count() > 500)
            updLogbook.Triggers.Add(new PostBackTrigger() { ControlID = rblShowAll.ID });

        gvFlightLogs.Columns[FindColumn(gvFlightLogs, Resources.LogbookEntry.FieldCFI)].Visible = Pilot.IsInstructor && !MiniMode;
        gvFlightLogs.Columns[FindColumn(gvFlightLogs, Resources.LogbookEntry.FieldSIC)].Visible = Pilot.TracksSecondInCommandTime && !MiniMode;
    }

    protected void SortGridview(GridView gv, List<LogbookEntryDisplay> lst)
    {
        if (!String.IsNullOrEmpty(LastSortExpr))
        {
            if (gv == null)
                throw new ArgumentNullException(nameof(gv));
            foreach (DataControlField dcf in gv.Columns)
                dcf.HeaderStyle.CssClass = "headerBase" + ((dcf.SortExpression.CompareCurrentCultureIgnoreCase(LastSortExpr) == 0) ? (LastSortDir == SortDirection.Ascending ? " headerSortAsc" : " headerSortDesc") : string.Empty) + (dcf.SortExpression.CompareCurrentCultureIgnoreCase("Date") == 0 ? " gvhLeft" : " gvhCentered");
        }

        LogbookEntryDisplay.SortLogbook(lst, LastSortExpr, LastSortDir);
        BindData(lst);
    }

    protected void gvFlightLogs_Sorting(Object sender, GridViewSortEventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        GridView gv = (GridView)sender;
        List<LogbookEntryDisplay> lst = (List<LogbookEntryDisplay>)gv.DataSource;

        if (lst != null)
        {
            if (HasPrevSort)
            {
                string PrevSortExpr = LastSortExpr;
                SortDirection PrevSortDir = LastSortDir;

                if (PrevSortExpr == e.SortExpression)
                    e.SortDirection = (PrevSortDir == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }

            LastSortExpr = e.SortExpression;
            LastSortDir = e.SortDirection;

            SortGridview(gv, lst);
        }
    }

    protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        GridView gv = (GridView)sender;
        gv.PageIndex = e.NewPageIndex;
        BindData();
    }

    #region Gridview RowDatabound Helpers
    private void SetUpContextMenuForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        // Wire up the drop-menu.  We have to do this here because it is an iNamingContainer and can't access the gridviewrow
        MyFlightbook.Controls.mfbFlightContextMenu cm = (MyFlightbook.Controls.mfbFlightContextMenu)row.FindControl("popmenu1").FindControl("mfbfcm");

        string szEditContext = EditContextParams(gvFlightLogs.PageIndex);

        cm.EditTargetFormatString = (EditPageUrlFormatString == null) ? string.Empty : (EditPageUrlFormatString + (String.IsNullOrEmpty(szEditContext) ? string.Empty : (EditPageUrlFormatString.Contains("?") ? "&" : "?" + szEditContext)));
        cm.Flight = le;
    }

    private void SetUpSelectionForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        if (IsInSelectMode && IsViewingOwnFlights)
        {
            ((CheckBox)row.FindControl("ckSelected")).Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:toggleSelectedFlight('{0}');", le.FlightID);
            BoundItems.Add(le.FlightID);
        }
    }

    private void SetUpImagesForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        // Bind to images.
        Controls_mfbImageList mfbIl = (Controls_mfbImageList)row.FindControl("mfbilFlights");
        if (!SuppressImages)
        {
            // Flight images
            mfbIl.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
            mfbIl.Refresh();
            if (!le.FlightImages.Any()) // populate images, so that flight coloring can work
                foreach (MyFlightbook.Image.MFBImageInfo mfbii in mfbIl.Images.ImageArray)
                    le.FlightImages.Add(mfbii);

            // wire up images
            if (mfbIl.Images.ImageArray.Count > 0 || le.Videos.Count > 0)
                row.FindControl("pnlImagesHover").Visible = true;
            else
                row.FindControl("pnlFlightImages").Visible = false;

            Aircraft ac = AircraftForUser.Find(a => a.AircraftID == le.AircraftID);
            string szInstTypeDescription = ac == null ? string.Empty : AircraftInstance.ShortNameForInstanceType(ac.InstanceType);
            ((Label)row.FindControl("lblInstanceTypeDesc")).Text = szInstTypeDescription;

            // And aircraft
            // for efficiency, see if we've already done this tail number; re-use if already done
            if (!m_dictAircraftHoverIDs.ContainsKey(le.AircraftID))
            {
                if (ac != null)
                    mfbilAircraft.DefaultImage = ac.DefaultImage;

                mfbilAircraft.Key = le.AircraftID.ToString(CultureInfo.InvariantCulture);
                mfbilAircraft.Refresh();

                // cache the attributes string - there's a bit of computation involved in it.
                string szAttributes = ((Label)row.FindControl("lblModelAttributes")).Text.EscapeHTML();

                // and the image table.
                m_dictAircraftHoverIDs[le.AircraftID] = szInstTypeDescription + " " + szAttributes + mfbilAircraft.AsHTMLTable();
            }

            row.FindControl("plcTail").Controls.Add(new LiteralControl(m_dictAircraftHoverIDs[le.AircraftID]));
        }
    }

    private static void SetStyleForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        HtmlGenericControl divDate = (HtmlGenericControl)row.FindControl("divDateAndRoute");
        switch (le.RowType)
        {
            case LogbookEntryDisplay.LogbookRowType.Flight:
                if (le.IsPageBreak)
                    row.CssClass += " pageBreakRow";
                break;
            case LogbookEntryDisplay.LogbookRowType.RunningTotal:
                row.CssClass = "runningTotalRow";
                divDate.Visible = false;
                break;
            case LogbookEntryDisplay.LogbookRowType.Subtotal:
                row.CssClass = le.IsPageBreak ? "subtotalRowPageBreak" : "subtotalRow";
                divDate.Visible = false;
                break;
        }
    }

    private void SetSigForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        switch (le.CFISignatureState)
        {
            case LogbookEntryCore.SignatureState.Invalid:
                {
                    Repeater rptDiffs = (Repeater)row.FindControl("rptDiffs");
                    rptDiffs.DataSource = le.DiffsSinceSigned(Viewer.UsesHHMM);
                    rptDiffs.DataBind();
                }
                break;
            case LogbookEntryCore.SignatureState.None:
                if (ShowModifiedFlights && le.HasFlightHash)
                {
                    Repeater rptMods = (Repeater)row.FindControl("rptMods");
                    IEnumerable<object> lst = le.DiffsSinceSigned(Viewer.UsesHHMM);
                    rptMods.DataSource = lst;
                    rptMods.DataBind();
                    row.FindControl("pnlUnsignedMods").Visible = lst.Any();
                }
                break;
            default:
                break;
        }
    }
    #endregion

    protected void gvFlightLogs_RowDataBound(Object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (e.Row.RowType == DataControlRowType.Footer)
        {
            int cCols = e.Row.Cells.Count;

            for (int i = cCols - 1; i >= 1; i--)
                e.Row.Cells.RemoveAt(i);
            e.Row.Cells[0].ColumnSpan = cCols;
        }
        else if (e.Row.RowType == DataControlRowType.DataRow)
        {
            LogbookEntryDisplay le = (LogbookEntryDisplay)e.Row.DataItem;

            SetUpContextMenuForRow(le, e.Row);
            SetUpBadgesForRow(le, e.Row);
            SetUpSelectionForRow(le, e.Row);
            SetUpImagesForRow(le, e.Row);
            SetStyleForRow(le, e.Row);
            SetSigForRow(le, e.Row);

            if (colorQueryMap == null)
                colorQueryMap = FlightColor.QueriesToColor(Pilot.UserName);

            foreach (CannedQuery cq in colorQueryMap)
            {
                if (cq.MatchesFlight(le))
                {
                    e.Row.BackColor = FlightColor.TryParseColor(cq.ColorString);
                    break;
                }
            }
        }
    }

    protected static void ShowButton(Control lnk, Boolean fShow)
    {
        if (lnk != null)
            lnk.Visible = fShow;
    }

    protected void UpdatePagerRow(TableRow gvr)
    {
        if (gvr == null)
            return;

        Label lbl = (Label)gvr.FindControl("lblPage");
        lbl.Text = gvFlightLogs.PageCount.ToString(CultureInfo.CurrentCulture);

        TextBox decCurPage = (TextBox)gvr.FindControl("decPage");
        decCurPage.Text = (gvFlightLogs.PageIndex + 1).ToString(CultureInfo.CurrentCulture);

        Control lnkFirst = gvr.Cells[0].FindControl("lnkFirst");
        Control lnkPrev = gvr.Cells[0].FindControl("lnkPrev");
        Control lnkNext = gvr.Cells[0].FindControl("lnkNext");
        Control lnkLast = gvr.Cells[0].FindControl("lnkLast");

        ShowButton(lnkFirst, gvFlightLogs.PageIndex > 0);
        ShowButton(lnkPrev, gvFlightLogs.PageIndex > 0);
        ShowButton(lnkNext, gvFlightLogs.PageIndex < gvFlightLogs.PageCount - 1);
        ShowButton(lnkLast, gvFlightLogs.PageIndex < gvFlightLogs.PageCount - 1);
    }

    protected void gvFlightLogs_DataBound(Object sender, EventArgs e)
    {
        if (gvFlightLogs == null)
            return;

        UpdatePagerRow(gvFlightLogs.TopPagerRow);
        UpdatePagerRow(gvFlightLogs.BottomPagerRow);

        // This will enable printing to work properly.
        if (AllowPaging == false && gvFlightLogs.HeaderRow != null && gvFlightLogs.FooterRow != null)
        {
            gvFlightLogs.HeaderRow.TableSection = TableRowSection.TableHeader;
            gvFlightLogs.FooterRow.TableSection = TableRowSection.TableFooter;
        }
    }

    #region Layout control
    protected void ckCompactView_CheckedChanged(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        ckCompactView.Checked = IsCompact = ((CheckBox)sender).Checked;
        BindData();
    }

    protected void ckIncludeImages_CheckedChanged(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        ckIncludeImages.Checked = ShowImagesInline = ((CheckBox)sender).Checked;
        BindData();
    }

    protected void lnkShowAll_Click(object sender, EventArgs e)
    {
        ViewState[szKeyAllowsPaging] = AllowPaging = false;
        BindData();
    }

    protected void btnSetPageSize_Click(object sender, EventArgs e)
    {
        if (decPageSize.IntValue <= 0 || decPageSize.IntValue > 100)
            return;

        FlightsPerPage = decPageSize.IntValue;
        BindData();
    }

    protected void lnkShowInPages_Click(object sender, EventArgs e)
    {
        ViewState[szKeyAllowsPaging] = AllowPaging = true;
        BindData();
    }

    protected void ckSelectFlights_CheckedChanged(object sender, EventArgs e)
    {
        divMulti.Visible = ckSelectFlights.Checked;
        SelectedItems = null;  // reset any selection
        BindData();
    }

    protected void lnkDeleteFlights_Click(object sender, EventArgs e)
    {
        foreach (int idFlight in SelectedItems)
            DeleteFlight(idFlight);

        IsInSelectMode = false;
        if (!SelectedItems.Any())
            BindData();
        SelectedItems = null;
    }

    protected void lnkRestrictSelected_Click(object sender, EventArgs e)
    {
        IsInSelectMode = false;
        Restriction.EnumeratedFlights = SelectedItems;
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/Logbooknew.aspx?fq={0}", HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString())));
    }

    protected void lnkInvertSelected_Click(object sender, EventArgs e)
    {
        if (BoundItems == null)
            return;

        HashSet<int> hs = new HashSet<int>(SelectedItems);
        HashSet<int> hsAll = new HashSet<int>(BoundItems);
        SelectedItems = hsAll.Except(hs);
        BindData();
    }

    protected void lnkReqSigs_Click(object sender, EventArgs e)
    {
        IsInSelectMode = false;
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/RequestSigs.aspx?id={0}", String.Join(",", SelectedItems)));
    }

    protected void ckSelectAll_CheckedChanged(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        CheckBox ck = sender as CheckBox;
        AllSelected = ck.Checked;
        if (BoundItems == null)
            return;

        HashSet<int> hs = new HashSet<int>(SelectedItems);
        SelectedItems = (ck.Checked) ? hs.Union(BoundItems) : hs.Except(BoundItems);
        BindData();
    }
    #endregion

    /// <summary>
    /// Let the user type in a page, a date, or a year to jump quickly to that page/date/year
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnSetPage_Click(object sender, EventArgs e)
    {
        TextBox decPageNum = (TextBox)gvFlightLogs.BottomPagerRow.FindControl("decPage");

        bool fParsesAsInt = Int32.TryParse(decPageNum.Text, out int iPage);

        bool fIsYear = fParsesAsInt && iPage > 1900 && iPage < 2200;

        // See if a date works
        bool fParsesAsDate = DateTime.TryParse(decPageNum.Text, out DateTime dtAttempted);
        int yearAttempted = fIsYear ? iPage : 0;
        if (fIsYear || fParsesAsDate)
        {
            int distance = Int32.MaxValue;
            int iRowMatch = 0;

            // Find the entry with the date or year closest to the typed date.
            List<LogbookEntryDisplay> lst = (List<LogbookEntryDisplay>)gvFlightLogs.DataSource;
            for (int iRow = 0; iRow < lst.Count; iRow++)
            {
                DateTime dtEntry = lst[iRow].Date;

                int distThis = (fIsYear) ? Math.Abs(dtEntry.Year - yearAttempted) : (int)Math.Abs(dtEntry.Subtract(dtAttempted).TotalDays);

                if (distThis < distance)
                {
                    distance = distThis;
                    iRowMatch = iRow;
                }
            }

            iPage = iRowMatch / gvFlightLogs.PageSize;
        }
        else
            iPage--;    // since we need to be 0 based

        if (iPage < gvFlightLogs.PageCount && iPage >= 0)
        {
            gvFlightLogs.PageIndex = iPage;
            BindData();
        }

        decPageNum.Text = (gvFlightLogs.PageIndex + 1).ToString(CultureInfo.CurrentCulture);
    }

    #region Context menu operations
    protected void DeleteFlight(int id)
    {
        LogbookEntryDisplay.FDeleteEntry(id, this.User);
        FlushCache();
        BindData(Data);
        RefreshNumFlights();
        ItemDeleted?.Invoke(this, new LogbookEventArgs(id));
    }

    protected void mfbFlightContextMenu_DeleteFlight(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        DeleteFlight(e.FlightID);
    }
    #endregion

    protected void gvFlightLogs_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        if (e.CommandName.CompareOrdinal("_revSig") == 0)
        {
            int idFlight = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            // Can't edit a LogbookEntryDisplay, so load a logbookentry
            LogbookEntry le = new LogbookEntry();
            if (le.FLoadFromDB(idFlight, User))
            {
                try
                {
                    le.RevokeSignature(Page.User.Identity.Name);
                    FlushCache();
                    BindData(Data);
                }
                catch (InvalidOperationException ex)
                {
                    ((Label) ((Control)e.CommandSource).NamingContainer.FindControl("lblErr")).Text = ex.Message;
                }
            }
        }
    }
}