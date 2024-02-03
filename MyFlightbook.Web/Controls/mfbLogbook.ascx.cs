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
 * Copyright (c) 2012-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_MFBLogbookBase : UserControl
{
    const string szFQViewStateKeyPrefix = "FQ";
    const string szKeyLastSortExpr = "LastSort";
    const string szKeylastSortDir = "LastSortDir";
    const string szKeyCurrentRange = "currRange";
    private const string szKeyAllowsPaging = "allowsPaging";
    private Profile m_pfPilot;
    private Profile m_pfUser;

    #region Properties that DON'T Depend on controls on the page
    /// <summary>
    /// Specifies whether or not paging should be offered
    /// </summary>
    protected bool AllowPaging
    {
        get
        {
            object o = ViewState[szKeyAllowsPaging];
            return o == null || (bool)o;
        }
        set { ViewState[szKeyAllowsPaging] = value; }
    }

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
                ViewState[key] = new FlightQuery(User);
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
    private const string keyVSUser = "logbookUser";
    /// <summary>
    /// Specifies the user for whom we are displaying results.
    /// </summary>
    public string User
    {
        get { return (string)ViewState[keyVSUser] ?? Page.User.Identity.Name; }
        set { ViewState[keyVSUser] = value; }
    }

    /// <summary>
    /// cached for performance
    /// </summary>
    protected List<Aircraft> AircraftForUser { get; } = new List<Aircraft>();

    protected string LastSortExpr
    {
        get {
            string o = (string) ViewState[szKeyLastSortExpr + ID];
            return String.IsNullOrEmpty(o) ? LogbookEntry.DefaultSortKey : o; }
        set { ViewState[szKeyLastSortExpr + ID] = value; }
    }

    protected SortDirection LastSortDir
    {
        get {
            object o = ViewState[szKeylastSortDir + ID];
            return (o == null) ? LogbookEntry.DefaultSortDir : (SortDirection)o;
        }
        set { ViewState[szKeylastSortDir + ID] = value; }
    }

    protected FlightResultManager CurrentResultManager
    {
        get { return FlightResultManager.FlightResultManagerForUser(User); }
    }

    public FlightResult CurrentResult
    {
        get { return CurrentResultManager.ResultsForQuery(Restriction); }
    }

    protected FlightResultRange CurrentRange
    {
        get { return (FlightResultRange)ViewState[szKeyCurrentRange]; }
        set { ViewState[szKeyCurrentRange] = value; }
    }

    #region URL templates for clickable items.
    // backing variables for where to go when clicking date, paper clip, or menu items
    private string m_szDetailsPageTemplate = "~/Member/FlightDetail.aspx/{0}";
    private string m_szEditPageTemplate = "~/member/LogbookNew.aspx/{0}";
    private string m_szAnalysisPageTemplate = "~/member/FlightDetail.aspx/{0}?tabID=Chart";
    private string m_szPublicRouteTemplate = "~/mvc/pub/ViewFlight/{0}";

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

    protected Profile Pilot
    {
        get { return m_pfPilot ?? (m_pfPilot = Profile.GetUser(this.User)); }
    }

    protected Profile Viewer
    {
        get { return m_pfUser ?? (m_pfUser = Profile.GetUser(Page.User.Identity.Name)); }
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

    #region Data
    /// <summary>
    /// Avoid redundant binding by checking in Page_Load if we've already bound by some other event.  Still can find ourselves binding twice here and there, but shouldn't happen by simply loading the page.
    /// </summary>
    protected bool HasBeenBound { get; set; }

    /// <summary>
    /// Returns the data, from the cache or via database call if cache misses, or DirectData if that's what's available
    /// </summary>
    public IEnumerable<LogbookEntryDisplay> Data
    {
        get { return CurrentResult.Flights; }
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
        var dictParams = HttpUtility.ParseQueryString(Request.Url.Query);

        // Issue #458: clone and reverse are getting duplicated and the & is getting url encoded, so even edits look like clones
        dictParams.Remove("Clone");
        dictParams.Remove("Reverse");

        if (!Restriction.IsDefault)
            dictParams["fq"] = Restriction.ToBase64CompressedJSONString();
        if (LastSortExpr.CompareCurrentCultureIgnoreCase(LogbookEntry.DefaultSortKey) != 0)
            dictParams["se"] = LastSortExpr;
        if (LastSortDir != LogbookEntry.DefaultSortDir)
            dictParams["so"] = LastSortDir.ToString();

        if (PageIndex != 0)
            dictParams["pg"] = PageIndex.ToString(CultureInfo.InvariantCulture);

        return dictParams.ToString();
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

    private static readonly char[] selectedItemsSeparator = new char[] { ',' };
    protected IEnumerable<int> SelectedItems
    {
        get
        {
            List<int> lst = new List<int>();
            if (!String.IsNullOrWhiteSpace(hdnSelectedItems.Value))
            {
                string[] rgIDs = hdnSelectedItems.Value.Split(selectedItemsSeparator, StringSplitOptions.RemoveEmptyEntries);
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
    /// Ensure that the specified flight is in view, if it is in the list of flights.
    /// </summary>
    /// <param name="idFlight"></param>
    public void ScrollToFlight(int idFlight)
    {
        BindData(CurrentResult.RangeContainingFlight(PageSize, idFlight).PageNum);
    }

    #region Display Options
    const string szCookieCompact = "mfbLogbookDisplayCompact";
    const string szCookieImages = "mfbLogbookDisplayImages";
    private const string szCookiesFlightsPerPage = "mfbLogbookDisplayFlightsPerPage";
    private const int defaultFlightsPerPage = 25;

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

    /// <summary>
    /// # of flights per page to show in paged mode
    /// </summary>
    protected int FlightsPerPage
    {
        get { return Viewer.GetPreferenceForKey(szCookiesFlightsPerPage, defaultFlightsPerPage); }
        set { Viewer.SetPreferenceForKey(szCookiesFlightsPerPage, value, value == 0 || value == defaultFlightsPerPage); }
    }

    /// <summary>
    /// Page size: 0 (show all) if no paging, else flights per page
    /// </summary>
    protected int PageSize { get { return AllowPaging ? FlightsPerPage : 0; } }
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

    protected void Page_PreRender(object sender, EventArgs e)
    {
        // take care of last minute things to ensure that everything is in order.
        UpdatePagerRow();

        decPage.Text = (CurrentRange.PageNum + 1).ToString(CultureInfo.InvariantCulture);

        // Get the correct headers for the current sort
        foreach (DataControlField dcf in gvFlightLogs.Columns)
            dcf.HeaderStyle.CssClass = "headerBase" + ((dcf.SortExpression.CompareCurrentCultureIgnoreCase(LastSortExpr) == 0) ? (LastSortDir == SortDirection.Ascending ? " headerSortAsc" : " headerSortDesc") : string.Empty) + (dcf.SortExpression.CompareCurrentCultureIgnoreCase("Date") == 0 ? " gvhLeft" : " gvhCentered");
    }

    protected void BindData(int defaultPage)
    {
        // Reset the cache for aircraft hover
        m_dictAircraftHoverIDs.Clear();

        FlightResult fr = CurrentResult;
        CurrentRange = fr.GetResultRange(PageSize, FlightRangeType.Page, LastSortExpr, LastSortDir, defaultPage);

        AircraftForUser.Clear();
        AircraftForUser.AddRange(new UserAircraft(User).GetAircraftForUser());
        BoundItems = new HashSet<int>();
        colorQueryMap = null;   // fetch on each databind
        gvFlightLogs.DataSource = fr.FlightsInRange(CurrentRange);
        gvFlightLogs.DataBind();
        HasBeenBound = true;
    }

    protected void BindData()
    {
        BindData(CurrentRange?.PageNum ?? 0);
    }

    public void RefreshNumFlights()
    {
        int cFlights = CurrentResult.FlightCount;
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
        LastSortExpr = LogbookEntry.DefaultSortKey;

        BindData(0);
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
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(this.User))
            User = Page.User.Identity.Name;

        if (ViewState[szKeyMiniMode] != null)
            MiniMode = (bool)ViewState[szKeyMiniMode];

        // need to do bind to the data every time or else the comments and images (dynamically added) disappear.
        // We also don't want to overuse viewstate - can get quite big quite quickly.
        // So we cache the logbook in the Session and use it on postbacks.  
        // always hit the DB on a GET unless host page has already called RefreshData (in which case we have already hit the DB)
        if (!IsPostBack)
        {
            lblAddress.Text = Pilot.Address;
            pnlAddress.Visible = Pilot.Address.Trim().Length > 0;

            // Customer-facing utility function: if you add "dupesOnly=1" to the URL, we add a custom restriction that limits flights to ONLY flights that look like potential duplicates
            if (util.GetIntParam(Request, "dupesOnly", 0) != 0)
            {
                Restriction.EnumeratedFlights = LogbookEntryBase.DupeCandidatesForUser(Restriction.UserName);
                HasBeenBound = false;
            }

            ckCompactView.Checked = m_isCompact;
            ckIncludeImages.Checked = m_showImagesInline;
            rblShowInPages.Checked = AllowPaging;
            rblShowAll.Checked = !rblShowInPages.Checked;
            ckSelectFlights.Visible = IsViewingOwnFlights;

            decPageSize.IntValue = FlightsPerPage;
            decPageSize.EditBox.MaxLength = 2;

            // Refresh state from params.  
            // fq is handled at the host level.
            string szLastSort = util.GetStringParam(Request, "so");
            if (!String.IsNullOrEmpty(szLastSort) && Enum.TryParse<SortDirection>(szLastSort, true, out SortDirection sortDirection))
            {
                LastSortDir = sortDirection;
                HasBeenBound = false;
            }
            string szSortExpr = util.GetStringParam(Request, "se");
            if (!String.IsNullOrEmpty(szSortExpr))
            {
                LastSortExpr = szSortExpr;
                HasBeenBound = false;
            }

            int curPage = util.GetIntParam(Request, "pg", 0);
            // Ensure we have the correct requested sort and page.  Note that this will also preheat the cache.
            CurrentRange = CurrentResult.GetResultRange(PageSize, FlightRangeType.Page, LastSortExpr, LastSortDir, curPage);
        }

        if (!HasBeenBound)
            BindData();

        // Issue #972: gridview databind performance BLOWS once we get over a few hundred rows, so disable update panel if more than, say, 300 rows in the resulting databind (should only be an issue if allow paging is off)
        if (CurrentResult.FlightCount > 300)
            updLogbook.Triggers.Add(new PostBackTrigger() { ControlID = rblShowAll.ID });

        gvFlightLogs.Columns[FindColumn(gvFlightLogs, Resources.LogbookEntry.FieldCFI)].Visible = Pilot.IsInstructor && !MiniMode;
        gvFlightLogs.Columns[FindColumn(gvFlightLogs, Resources.LogbookEntry.FieldSIC)].Visible = Pilot.TracksSecondInCommandTime && !MiniMode;
    }

    protected void gvFlightLogs_Sorting(Object sender, GridViewSortEventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        FlightResult fr = CurrentResult;

        fr.SortFlights(e.SortExpression);
        LastSortExpr = fr.CurrentSortKey;
        LastSortDir = fr.CurrentSortDir;
        BindData();
    }
    #region Gridview RowDatabound Helpers
    private void SetUpContextMenuForRow(LogbookEntryDisplay le, GridViewRow row)
    {
        // Wire up the drop-menu.  We have to do this here because it is an iNamingContainer and can't access the gridviewrow
        MyFlightbook.Controls.mfbFlightContextMenu cm = (MyFlightbook.Controls.mfbFlightContextMenu)row.FindControl("popmenu1").FindControl("mfbfcm");

        string szEditContext = EditContextParams(CurrentRange?.PageNum ?? 0);

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
            if (le.FlightImages.Count == 0) // populate images, so that flight coloring can work
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
            if (!m_dictAircraftHoverIDs.TryGetValue(le.AircraftID, out string hoverID))
            {
                if (ac != null)
                    mfbilAircraft.DefaultImage = ac.DefaultImage;

                mfbilAircraft.Key = le.AircraftID.ToString(CultureInfo.InvariantCulture);
                mfbilAircraft.Refresh();

                // cache the attributes string - there's a bit of computation involved in it.
                string szAttributes = ((Label)row.FindControl("lblModelAttributes")).Text.EscapeHTML();
                hoverID = szInstTypeDescription + " " + szAttributes + mfbilAircraft.AsHTMLTable();

                // and the image table.
                m_dictAircraftHoverIDs[le.AircraftID] = hoverID;
            }

            row.FindControl("plcTail").Controls.Add(new LiteralControl(hoverID));
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
                if (Profile.GetUser(User).PreferenceExists(MFBConstants.keyTrackOriginal) && le.HasFlightHash)
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

    protected void gvFlightLogs_DataBound(Object sender, EventArgs e)
    {
        if (gvFlightLogs == null)
            return;

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
        AllowPaging = false;
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
        AllowPaging = true;
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

    #region Paging
    protected void UpdatePagerRow()
    {
        FlightResultRange range = CurrentRange;
        pnlPager.Visible = AllowPaging && range.PageCount > 1;

        lblPage.Text = CurrentRange.PageCount.ToString(CultureInfo.CurrentCulture);
        ShowButton(lnkFirst, range.PageNum > 0);
        ShowButton(lnkPrev, range.PageNum > 0);
        ShowButton(lnkNext, range.PageNum < range.PageCount - 1);
        ShowButton(lnkLast, range.PageNum < range.PageCount - 1);
    }

    /// <summary>
    /// Let the user type in a page, a date, or a year to jump quickly to that page/date/year
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnSetPage_Click(object sender, EventArgs e)
    {
        FlightResult fr = CurrentResult;
        int pageNum = fr.GetResultRange(PageSize, FlightRangeType.Search, LastSortExpr, LastSortDir, query: decPage.Text).PageNum;
        // GetResultRange could have changed the sort order.  E.g., a date requires that the list be sorted by date.
        LastSortExpr = fr.CurrentSortKey;
        LastSortDir = fr.CurrentSortDir;
        BindData(pageNum);
    }

    protected void lnkFirst_Click(object sender, EventArgs e)
    {
        BindData(CurrentResult.GetResultRange(PageSize, FlightRangeType.First, LastSortExpr, LastSortDir).PageNum);
    }

    protected void lnkPrev_Click(object sender, EventArgs e)
    {
        BindData(CurrentResult.GetResultRange(PageSize, FlightRangeType.Prev, LastSortExpr, LastSortDir, CurrentRange.PageNum).PageNum);
    }

    protected void lnkNext_Click(object sender, EventArgs e)
    {
        BindData(CurrentResult.GetResultRange(PageSize, FlightRangeType.Next, LastSortExpr, LastSortDir, CurrentRange.PageNum).PageNum);
    }

    protected void lnkLast_Click(object sender, EventArgs e)
    {
        BindData(CurrentResult.GetResultRange(PageSize, FlightRangeType.Last, LastSortExpr, LastSortDir).PageNum);
    }
    #endregion

    #region Context menu operations
    protected void DeleteFlight(int id)
    {
        LogbookEntryBase.FDeleteEntry(id, this.User);   // this will invalidate flight results.
        BindData();
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
                    BindData();
                }
                catch (InvalidOperationException ex)
                {
                    ((Label) ((Control)e.CommandSource).NamingContainer.FindControl("lblErr")).Text = ex.Message;
                }
            }
        }
    }
}