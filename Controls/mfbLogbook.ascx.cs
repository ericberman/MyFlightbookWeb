using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2012-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbLogbook : System.Web.UI.UserControl
{
    const string szFQViewStateKeyPrefix = "FQ";
    const string szKeyLastSortExpr = "LastSort";
    const string szKeylastSortDir = "LastSortDir";
    const string szKeyAllowsPaging = "allowsPaging";
    const string szKeyMiniMode = "minimode";
    private Boolean m_fMiniMode = false;
    private MyFlightbook.Profile m_pfPilot = null;
    private MyFlightbook.Profile m_pfUser = null;
    private Dictionary<int, string> m_dictAircraftHoverIDs = new Dictionary<int,string>();

    #region Properties
    public event EventHandler<LogbookEventArgs> ItemDeleted = null; 

    private string RestrictionVSKey { get { return szFQViewStateKeyPrefix + ID; } }

    /// <summary>
    /// Sets/Gets a FlightQuery to restrict which flights are shown
    /// </summary>
    public FlightQuery Restriction
    {
        get {
            string key = RestrictionVSKey;
            if (ViewState[key] == null)
                ViewState[key] = new FlightQuery(String.IsNullOrEmpty(User) ? Page.User.Identity.Name : this.User);
            return (FlightQuery)ViewState[key];
            }
        set { ViewState[RestrictionVSKey] = value; }
    }

    public IEnumerable<LogbookEntryDisplay> DirectData { get; set; }

    /// <summary>
    /// True for this to be "print view": suppresses images
    /// </summary>
    public Boolean SuppressImages 
    {
        get { return !String.IsNullOrEmpty((string) ViewState["vsSI"]); }
        set { ViewState["vsSI"] = value ? value.ToString() : string.Empty; }
    }

    private string keyVSUser = "logbookUser";
    /// <summary>
    /// Specifies the user for whom we are displaying results.
    /// </summary>
    public string User
    {
        get { return (string) ViewState[keyVSUser]; }
        set { ViewState[keyVSUser] = value; }
    }

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
    /// Show/hide the "Show in Pages" link.
    /// </summary>
    public Boolean PagingLinkVisible
    {
        get { return rowShowPages.Visible; }
        set { rowShowPages.Visible = value; }
    }

    /// <summary>
    /// Specifies whether or not paging should be offered
    /// </summary>
    public Boolean AllowPaging
    {
        get { return gvFlightLogs.AllowPaging; }
        set { ViewState[szKeyAllowsPaging] = gvFlightLogs.AllowPaging = value; }
    }

    #region URL templates for clickable items.
    // backing variables for where to go when clicking date, paper clip, or menu items
    private string m_szSendPageTarget = "~/Member/LogbookNew.aspx";
    private string m_szDetailsPageTemplate = "~/Member/FlightDetail.aspx/{0}";
    private string m_szEditPageTemplate = "~/member/LogbookNew.aspx/{0}";
    private string m_szAnalysisPageTemplate = "~/member/FlightDetail.aspx/{0}?tabID=Chart";
    private string m_szPublicRouteTemplate = "~/Public/ViewPublicFlight.aspx/{0}";

    /// <summary>
    /// Path to which flights should be sent.
    /// </summary>
    public string SendPageTarget
    {
        get { return m_szSendPageTarget; }
        set { m_szSendPageTarget = value; }
    }

    /// <summary>
    /// The URL for details page, ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "member", Justification = "this is a format string and thus is a string, not a URI")]
    public string DetailsPageUrlFormatString {
        get { return m_szDetailsPageTemplate; }
        set { m_szDetailsPageTemplate = value; }
    }

    /// <summary>
    /// URL template to edit the flight; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "member", Justification = "this is a format string and thus is a string, not a URI")]
    public string EditPageUrlFormatString
    {
        get { return m_szEditPageTemplate; }
        set { m_szEditPageTemplate = value; }
    }

    /// <summary>
    /// URL template for flight analysis; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "member", Justification = "this is a format string and thus is a string, not a URI")]
    public string AnalysisPageUrlFormatString
    {
        get { return m_szAnalysisPageTemplate; }
        set { m_szAnalysisPageTemplate = value; }
    }

    /// <summary>
    /// URL Template for public page; ID of the flight replaces {0}
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "member", Justification ="this is a format string and thus is a string, not a URI")]
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

    #region Protected/Private Properties
    /// <summary>
    /// cached for performance
    /// </summary>
    protected List<Aircraft> AircraftForUser { get; set; }

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
        get { 
            object o = ViewState[szKeylastSortDir + ID];
            return (o == null) ? SortDirection.Descending : (SortDirection)o;
        }
        set { ViewState[szKeylastSortDir + ID] = value; }
    }

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
    private IEnumerable<LogbookEntryDisplay> CachedData
    {
        get { 
            Dictionary<string, IEnumerable<LogbookEntryDisplay>> cachedValues = (Dictionary<string, IEnumerable<LogbookEntryDisplay>>) Cache[CacheKey]; 
            if (cachedValues != null)
            {
                string key = Restriction.ToJSONString();
                return (cachedValues.ContainsKey(key)) ? cachedValues[key] : null;
            }
            return null;
        }
        set 
        {
            Dictionary<string, IEnumerable<LogbookEntryDisplay>> cachedValues = (Dictionary<string, IEnumerable<LogbookEntryDisplay>>)Cache[CacheKey];
            if (cachedValues == null)
                cachedValues = new Dictionary<string, IEnumerable<LogbookEntryDisplay>>();
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

                MyFlightbook.Profile pfUser = MyFlightbook.Profile.GetUser(this.User);

                lst = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(Restriction), this.User, LastSortExpr, LastSortDir, Viewer.UsesHHMM, pfUser.UsesUTCDateOfFlight);
                CachedData = lst;
            }
            return lst;
        }
    }
    #endregion
    #endregion

    /// <summary>
    /// Get the index of the column containing the specied header text
    /// </summary>
    /// <param name="gv">Gridview</param>
    /// <param name="szColName">Header text</param>
    /// <returns>column id, or -1</returns>
    private int FindColumn(GridView gv, string szColName)
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

        AircraftForUser = new List<Aircraft>(new UserAircraft(User).GetAircraftForUser());
        gvFlightLogs.DataBind();
        HasBeenBound = true;
    }

    /// <summary>
    /// Publicly visible force-refresh method.  ALWAYS bypasses the cache and uses provided data or hits the database
    /// </summary>
    public void RefreshData()
    {
        FlushCache();
        BindData(Data);
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        gvFlightLogs.PageSize = util.GetIntParam(Request, "pageSize", 25);   // will set gvflightlogs.pagesize
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

            if (util.GetIntParam(Request, "dupesOnly", 0) != 0)
            {
                RestrictToDupes();
                FlushCache();
                HasBeenBound = false;
            }
        }

        if (!HasBeenBound)
            BindData(Data);

        gvFlightLogs.Columns[FindColumn(gvFlightLogs, "CFI")].Visible = Pilot.IsInstructor && !MiniMode;
        gvFlightLogs.Columns[FindColumn(gvFlightLogs, "SIC")].Visible = Pilot.TracksSecondInCommandTime && !MiniMode;
    }

    #region Restrict to show only potentially duplicate flights
    /// <summary>
    /// Customer-facing utility function: if you add "dupesOnly=1" to the URL, we add a custom restriction that limits flights to ONLY flights that look like potential duplicates
    /// </summary>
    public void RestrictToDupes()
    {
        FlightQuery fq = Restriction;
        fq.CustomRestriction = @" (flights.idflight IN (select distinct f1.idflight 
from flights f1 inner join flights f2 on
f1.username = ?uName AND
f1.username=f2.username and
f1.date = f2.date AND
f1.idflight <> f2.idflight AND 
f1.idaircraft = f2.idaircraft AND
f1.cLandings = f2.cLandings AND
f1.cInstrumentApproaches = f2.cInstrumentApproaches AND
f1.Comments = f2.comments AND
f1.route = f2.route AND
f1.night = f2.night AND
f1.crossCountry = f2.crossCountry AND
f1.IMC = f2.IMC AND
f1.simulatedInstrument = f2.simulatedInstrument AND
f1.groundSim = f2.groundSim AND
f1.dualReceived = f2.dualReceived AND
f1.cfi = f2.cfi AND
f1.pic = f2.pic AND
f1.SIC = f2.SIC AND
f1.totalFlightTime = f2.totalFlightTime AND
f1.hobbsStart = f2.hobbsStart AND
f1.hobbsEnd = f2.hobbsEnd AND
f1.dtEngineStart = f2.dtEngineStart AND
f1.dtEngineEnd = f2.dtEngineEnd AND
f1.dtFlightStart = f2.dtFlightStart AND
f1.dtFlightEnd = f2.dtFlightEnd)) ";
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

    protected string EditPath(object idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, EditPageUrlFormatString, idFlight);
    }

    protected string PublicPath(object idFlight)
    {
        return String.Format(CultureInfo.InvariantCulture, PublicPageUrlFormatString, idFlight);
    }

    protected string ClonePath(object idFlight)
    {
        return EditPath(idFlight) + "?Clone=1";
    }

    protected string ReversePath(object idFlight)
    {
        return EditPath(idFlight) + "?Clone=1&Reverse=1";
    }
    #endregion

    protected void gvFlightLogs_RowCommand(Object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        int idFlight;
        if (!int.TryParse(e.CommandArgument.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out idFlight))
            idFlight = LogbookEntry.idFlightNew;

        if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            LogbookEntryDisplay.FDeleteEntry(idFlight, this.User);
            FlushCache();
            BindData(Data);
            if (ItemDeleted != null)
                ItemDeleted(this, new LogbookEventArgs(idFlight));
        }
        else if (String.Compare(e.CommandName, "SendFlight", StringComparison.OrdinalIgnoreCase) == 0)
        {
            hdnFlightToSend.Value = (string)e.CommandArgument;
            txtSendFlightEmail.Text = txtSendFlightMessage.Text = string.Empty;
            modalPopupSendFlight.Show();
        }
        else if (String.Compare(e.CommandName, "CloneFlight", StringComparison.OrdinalIgnoreCase) == 0)
        {
            Response.Redirect(ClonePath(idFlight));
        }
        else if (String.Compare(e.CommandName, "ReverseFlight", StringComparison.OrdinalIgnoreCase) == 0)
        {
            Response.Redirect(ReversePath(idFlight));
        }
        else if (String.Compare(e.CommandName, "EditFlight", StringComparison.OrdinalIgnoreCase) == 0)
        {
            if (idFlight == LogbookEntry.idFlightNew)
                throw new MyFlightbookValidationException("Why is there an edit option for an empty flight?");
            Response.Redirect(EditPath(idFlight));
        }
    }

    protected void gvFlightLogs_Sorting(Object sender, GridViewSortEventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");
        if (e == null)
            throw new ArgumentNullException("e");

        GridView gv = (GridView)sender;
        List<LogbookEntryDisplay> lst = (List<LogbookEntryDisplay>)gv.DataSource;

        if (lst != null)
        {
            if (HasPrevSort)
            {
                string PrevSortExpr = LastSortExpr;
                SortDirection PrevSortDir = LastSortDir;

                if (PrevSortExpr == e.SortExpression)
                {
                    e.SortDirection = (PrevSortDir == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
                }
            }

            LastSortExpr = e.SortExpression;
            LastSortDir = e.SortDirection;

            foreach (DataControlField dcf in gv.Columns)
                dcf.HeaderStyle.Font.Bold = dcf.SortExpression.CompareCurrentCultureIgnoreCase(e.SortExpression) == 0;

            LogbookEntryDisplay.SortLogbook(lst, LastSortExpr, LastSortDir);
            BindData();
        }
    }

    protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        GridView gv = (GridView)sender;
        gv.PageIndex = e.NewPageIndex;
        BindData();
    }

    public void gvFlightLogs_RowDataBound(Object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.Footer)
        {
            int cCols = e.Row.Cells.Count;

            for (int i = cCols - 1; i >= 1; i--)
                e.Row.Cells.RemoveAt(i);
            e.Row.Cells[0].ColumnSpan = cCols;
        }
        else if (e.Row.RowType == DataControlRowType.Pager)
        {
            ((Label) e.Row.FindControl("lblNumFlights")).Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.NumberOfFlights, CachedData.Count());
        }
        else if (e.Row.RowType == DataControlRowType.DataRow)
        {
            LogbookEntryDisplay le = (LogbookEntryDisplay) e.Row.DataItem;

            // Wire up the drop-menu.  We have to do this here because it is an iNamingContainer and can't access the gridviewrow
            Controls_popmenu popup = (Controls_popmenu)e.Row.FindControl("popmenu1");
            ((Controls_mfbMiniFacebook)popup.FindControl("mfbMiniFacebook")).FlightEntry = le;
            ((Controls_mfbTweetThis)popup.FindControl("mfbTweetThis")).FlightToTweet = le;
            ((LinkButton)popup.FindControl("lnkReverse")).CommandArgument = ((LinkButton)popup.FindControl("lnkClone")).CommandArgument = le.FlightID.ToString(CultureInfo.InvariantCulture);
            ((LinkButton)popup.FindControl("lnkSendFlight")).CommandArgument = le.FlightID.ToString(CultureInfo.InvariantCulture);
            ((HyperLink)popup.FindControl("lnkEditThisFlight")).NavigateUrl = EditPath(le.FlightID);
            HyperLink h = (HyperLink)popup.FindControl("lnkRequestSignature");
            h.Visible = le.CanRequestSig;
            h.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/RequestSigs.aspx?id={0}", le.FlightID);

            // fix the ID of the delete button to prevent replay attacks
            string szDelID = String.Format(CultureInfo.InvariantCulture, "lnkDel{0}", le.FlightID);
            LinkButton lbDelete = (LinkButton)e.Row.FindControl("lnkDelete");
            lbDelete.ID = szDelID;
            // If the host wants notifications of deletions, register full postback.
            if (ItemDeleted != null)
                ScriptManager.GetCurrent(Page).RegisterPostBackControl(lbDelete);
            ((AjaxControlToolkit.ConfirmButtonExtender)e.Row.FindControl("ConfirmButtonExtender1")).TargetControlID = szDelID;

            // Bind to images.
            Controls_mfbImageList mfbIl = (Controls_mfbImageList)e.Row.FindControl("mfbilFlights");
            if (!SuppressImages)
            {
                // Flight images
                mfbIl.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
                mfbIl.Refresh();

                // wire up images
                if (mfbIl.Images.ImageArray.Count > 0 || le.Videos.Count() > 0)
                    e.Row.FindControl("pnlImagesHover").Visible = true;
                else
                    e.Row.FindControl("pnlFlightImages").Visible = false;

                Aircraft ac = AircraftForUser.Find(a => a.AircraftID == le.AircraftID);
                string szInstTypeDescription = ac == null ? string.Empty : AircraftInstance.ShortNameForInstanceType(ac.InstanceType);
                ((Label)e.Row.FindControl("lblInstanceTypeDesc")).Text = szInstTypeDescription;

                // And aircraft
                // for efficiency, see if we've already done this tail number; re-use if already done
                if (!m_dictAircraftHoverIDs.ContainsKey(le.AircraftID))
                {
                    if (ac != null)
                        mfbilAircraft.DefaultImage = ac.DefaultImage;

                    mfbilAircraft.Key = le.AircraftID.ToString(CultureInfo.InvariantCulture);
                    mfbilAircraft.Refresh();

                    // cache the attributes string - there's a bit of computation involved in it.
                    string szAttributes = ((Label)e.Row.FindControl("lblModelAttributes")).Text.EscapeHTML();

                    // and the image table.
                    m_dictAircraftHoverIDs[le.AircraftID] = szInstTypeDescription + " " + szAttributes + mfbilAircraft.AsHTMLTable();
                }

                e.Row.FindControl("plcTail").Controls.Add(new LiteralControl(m_dictAircraftHoverIDs[le.AircraftID]));
            }

            // Set style for the row
            HtmlGenericControl divDate = (HtmlGenericControl)e.Row.FindControl("divDateAndRoute");
            switch (le.RowType)
            {
                case LogbookEntryDisplay.LogbookRowType.Flight:
                    if (le.IsPageBreak)
                        e.Row.CssClass = e.Row.CssClass + " pageBreakRow";
                    break;
                case LogbookEntryDisplay.LogbookRowType.RunningTotal:
                    e.Row.CssClass = "runningTotalRow";
                    divDate.Visible = false;
                    break;
                case LogbookEntryDisplay.LogbookRowType.Subtotal:
                    e.Row.CssClass = (le.IsPageBreak) ? "subtotalRowPageBreak" : "subtotalRow";
                    divDate.Visible = false;
                    break;
            }
        }
    }

    protected void ShowButton(Control lnk, Boolean fShow)
    {
        if (lnk != null)
            lnk.Visible = fShow;
    }

    protected void UpdatePagerRow(TableRow gvr)
    {
        if (gvr == null)
            return;

        Label lbl = (Label) gvr.FindControl("lblPage");
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

        Control lnkShowAll = gvr.Cells[0].FindControl("lnkShowAll");
        
        if (lnkShowAll != null)
            lnkShowAll.Visible = AllowPaging;

        rowShowPages.Visible = !AllowPaging;
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
            gvFlightLogs.HeaderRow.TableSection = System.Web.UI.WebControls.TableRowSection.TableHeader;
            gvFlightLogs.FooterRow.TableSection = System.Web.UI.WebControls.TableRowSection.TableFooter;
        }
    }

    protected void lnkShowAll_Click(object sender, EventArgs e)
    {
        ViewState[szKeyAllowsPaging] = AllowPaging = false;
        BindData();
    }
    
    protected void lnkShowInPages_Click(object sender, EventArgs e)
    {
        ViewState[szKeyAllowsPaging] = AllowPaging = true;
        BindData();
    }

    /// <summary>
    /// Let the user type in a page, a date, or a year to jump quickly to that page/date/year
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btnSetPage_Click(object sender, EventArgs e)
    {
        TextBox decPageNum = null;

        decPageNum = (TextBox)((sender == gvFlightLogs.TopPagerRow.FindControl("btnSetPage") ? gvFlightLogs.TopPagerRow.FindControl("decPage") : gvFlightLogs.BottomPagerRow.FindControl("decPage")));

        try
        {
            int iPage = 0;

            bool fIsYear = Int32.TryParse(decPageNum.Text, out iPage) && iPage > 1900 && iPage < 2200;

            // See if a date works
            DateTime dtAttempted = DateTime.MinValue;
            int yearAttempted = fIsYear ? iPage : 0;
            if (fIsYear || DateTime.TryParse(decPageNum.Text, out dtAttempted))
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
                iPage = Convert.ToInt32(decPageNum.Text, CultureInfo.InvariantCulture) - 1;

            if (iPage < gvFlightLogs.PageCount && iPage >= 0)
            {
                gvFlightLogs.PageIndex = iPage;
                BindData();
            }

            decPageNum.Text = (gvFlightLogs.PageIndex + 1).ToString(CultureInfo.CurrentCulture);
        }
        catch (ArgumentException) { }
        catch (FormatException) { }
    }

    protected void btnSendFlight_Click(object sender, EventArgs e)
    {
        Page.Validate("valSendFlight");
        if (Page.IsValid)
        {
            LogbookEntry le = new LogbookEntry(Convert.ToInt32(hdnFlightToSend.Value, CultureInfo.InvariantCulture), Page.User.Identity.Name);
            MyFlightbook.Profile pfSender = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            string szRecipient = txtSendFlightEmail.Text;

            using (MailMessage msg = new MailMessage())
            {
                msg.Body = Branding.ReBrand(Resources.LogbookEntry.SendFlightBody.Replace("<% Sender %>", HttpUtility.HtmlEncode(pfSender.UserFullName))
                    .Replace("<% Message %>", HttpUtility.HtmlEncode(txtSendFlightMessage.Text))
                    .Replace("<% Date %>", le.Date.ToShortDateString())
                    .Replace("<% Aircraft %>", HttpUtility.HtmlEncode(le.TailNumDisplay))
                    .Replace("<% Route %>", HttpUtility.HtmlEncode(le.Route))
                    .Replace("<% Comments %>", HttpUtility.HtmlEncode(le.Comment))
                    .Replace("<% Time %>", le.TotalFlightTime.FormatDecimal(pfSender.UsesHHMM))
                    .Replace("<% FlightLink %>", le.SendFlightUri(Branding.CurrentBrand.HostName, SendPageTarget).ToString()));

                msg.Subject = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.SendFlightSubject, pfSender.UserFullName);
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pfSender.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pfSender.Email));
                msg.To.Add(new MailAddress(szRecipient));
                msg.IsBodyHtml = true;
                util.SendMessage(msg);
            }

            modalPopupSendFlight.Hide();
        }
        else
            modalPopupSendFlight.Show();
    }
}
