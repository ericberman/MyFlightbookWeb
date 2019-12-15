using MyFlightbook;
using MyFlightbook.Printing;
using MyFlightbook.SocialMedia;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Web;
using System.Web.Services;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
TODO: Modify the print link in javascript on the client for performance?
 */
public partial class Member_LogbookNew : System.Web.UI.Page
{
    #region Webservices
    /// <summary>
    /// Returns the high-watermark starting hobbs for the specified aircraft.
    /// </summary>
    /// <param name="idAircraft"></param>
    /// <returns>0 if unknown.</returns>
    [WebMethod(EnableSession = true)]
    public static string HighWaterMarkHobbsForAircraft(int idAircraft)
    {
        if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("You must be authenticated to make this call");

        decimal val = 0.0M;
        DBHelper dbh = new DBHelper(@"SELECT  MAX(f.hobbsEnd) AS highWater FROM (SELECT hobbsEnd FROM flights WHERE username = ?user AND idaircraft = ?id ORDER BY flights.date DESC LIMIT 10) f");
        dbh.ReadRow((comm) =>
        {
            comm.Parameters.AddWithValue("user", HttpContext.Current.User.Identity.Name);
            comm.Parameters.AddWithValue("id", idAircraft);
        },
        (dr) =>
        {
            val = Convert.ToDecimal(util.ReadNullableField(dr, "highWater", 0.0));
        });
        return val.ToString("0.0#", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns the high-watermark starting hobbs for the specified aircraft.
    /// </summary>
    /// <param name="idAircraft"></param>
    /// <returns>0 if unknown.</returns>
    [WebMethod(EnableSession = true)]
    public static string HighWaterMarkTachForAircraft(int idAircraft)
    {
        if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("You must be authenticated to make this call");

        decimal val = 0.0M;
        DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT MAX(tach.decvalue) AS highWater FROM
(SELECT decvalue FROM flightproperties fp INNER JOIN flights f ON fp.idflight = f.idflight WHERE f.username = ?user AND f.idaircraft = ?id AND fp.idproptype = {0}
ORDER BY f.date DESC LIMIT 10) tach", (int) CustomPropertyType.KnownProperties.IDPropTachEnd));
        dbh.ReadRow((comm) =>
        {
            comm.Parameters.AddWithValue("user", HttpContext.Current.User.Identity.Name);
            comm.Parameters.AddWithValue("id", idAircraft);
        },
        (dr) =>
        {
            val = Convert.ToDecimal(util.ReadNullableField(dr, "highWater", 0.0));
        });
        return val.ToString("0.0#", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns the current time formatted in UTC or specified time-zone
    /// </summary>
    /// <returns>Now in the specified locale, adjusted for the timezone.</returns>
    [WebMethod(EnableSession = true)]
    public static string NowInUTC()
    {
        // For now, always return true UTC
        if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length > 0)
            util.SetCulture(HttpContext.Current.Request.UserLanguages[0]);
        if (HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("You must be authenticated to make this call");

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MyFlightbook.Profile.GetUser(HttpContext.Current.User.Identity.Name).PreferredTimeZone).UTCDateFormatString();
    }
    #endregion

    /// <summary>
    /// Catches injection errors (e.g., a &lt; in places it's not allowed)
    /// </summary>
    /// <param name="e"></param>
    protected override void OnError(EventArgs e)
    {
        Exception ex = Server.GetLastError();

        if (ex.GetType() == typeof(HttpRequestValidationException))
        {
            Context.ClearError();
            Response.Redirect("~/SecurityError.aspx");
            Response.End();
        }
        else
            base.OnError(e);
    }

    private const string szParamIDFlight = "idFlight";

    public enum FlightsTab { None, Add, Search, Totals, Currency, Analysis, Printing, More }

    private const string keyVSRestriction = "vsCurrentRestriction";
    protected FlightQuery Restriction 
    {
        get
        {
            if (ViewState[keyVSRestriction] == null)
                ViewState[keyVSRestriction] = new FlightQuery(Page.User.Identity.Name);
            return (FlightQuery)ViewState[keyVSRestriction];
        }
        set
        {
            ViewState[keyVSRestriction] = value;
        }
    }

    protected void InitializeRestriction()
    {
        string szSearchParam = util.GetStringParam(Request, "s");
        string szFQParam = util.GetStringParam(Request, "fq");
        string szAirportParam = util.GetStringParam(Request, "ap");
        int month = util.GetIntParam(Request, "m", -1);
        int year = util.GetIntParam(Request, "y", -1);
        int day = util.GetIntParam(Request, "d", -1);
        int week = util.GetIntParam(Request, "w", -1);
        if (!String.IsNullOrEmpty(szFQParam))
        {
            try
            {
                Restriction = mfbSearchForm1.Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
            }
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            catch (JsonSerializationException) { }
            catch (JsonException) { }
        }
        else
            Restriction = mfbSearchForm1.Restriction = new FlightQuery(Page.User.Identity.Name);

        if (!String.IsNullOrEmpty(szSearchParam))
            Restriction.GeneralText = szSearchParam;
        if (!String.IsNullOrEmpty(szAirportParam))
            Restriction.AirportList = MyFlightbook.Airports.AirportList.NormalizeAirportList(szAirportParam);

        if (year > 1900)
        {
            if (month >= 0 && month < 12 && year > 1900)
            {
                DateTime dtStart = new DateTime(year, month + 1, day > 0 ? day : 1);
                DateTime dtEnd = (day > 0) ? (week > 0 ? dtStart.AddDays(6) : dtStart) : dtStart.AddMonths(1).AddDays(-1);
                Restriction.DateRange = FlightQuery.DateRanges.Custom;
                Restriction.DateMin = dtStart;
                Restriction.DateMax = dtEnd;
            }
            else
            {
                Restriction.DateRange = FlightQuery.DateRanges.Custom;
                Restriction.DateMin = new DateTime(year, 1, 1);
                Restriction.DateMax = new DateTime(year, 12, 31);
            }
        }

        Refresh();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabLogbook;

        if (!IsPostBack)
        {
            ModalPopupExtender1.OnCancelScript = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').style.display = 'none';", pnlWelcomeNewUser.ClientID);

            if (Request.Cookies[MFBConstants.keyNewUser] != null && !String.IsNullOrEmpty(Request.Cookies[MFBConstants.keyNewUser].Value) || util.GetStringParam(Request, "sw").Length > 0 || Request.PathInfo.Contains("/sw"))
            {
                Response.Cookies[MFBConstants.keyNewUser].Expires = DateTime.Now.AddDays(-1);
                ModalPopupExtender1.Show();
            }

            rblTotalsMode.SelectedValue = mfbTotalSummary1.DefaultGroupMode.ToString(CultureInfo.InvariantCulture);

            // Handle a requested tab - turning of lazy load as needed
            FlightsTab ft;
            string szReqTab = util.GetStringParam(Request, "ft");
            if (!String.IsNullOrEmpty(szReqTab))
            {
                if (Enum.TryParse<FlightsTab>(szReqTab, out ft))
                {
                    AccordionCtrl.SelectedIndex = (int)ft - 1;
                    switch (ft)
                    {
                        case FlightsTab.Currency:
                            apcCurrency_ControlClicked(apcCurrency, null);
                            break;
                        case FlightsTab.Totals:
                            apcTotals_ControlClicked(apcTotals, null);
                            break;
                        case FlightsTab.Analysis:
                            apcAnalysis_ControlClicked(apcAnalysis, null);
                            break;
                        default:
                            break;
                    }
                }
            }

            int idFlight = util.GetIntParam(Request, szParamIDFlight, LogbookEntry.idFlightNew);

            // Redirect to the non-querystring based page so that Ajax file upload works
            if (idFlight != LogbookEntry.idFlightNew)
            {
                string szNew = Request.Url.PathAndQuery.Replace(".aspx", String.Format(CultureInfo.InvariantCulture, ".aspx/{0}", idFlight)).Replace(String.Format(CultureInfo.InvariantCulture, "{0}={1}", szParamIDFlight, idFlight), string.Empty).Replace("?&", "?");
                Response.Redirect(szNew, true);
                return;
            }

            if (Request.PathInfo.Length > 0)
            {
                try { idFlight = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture); }
                catch (FormatException) { }
            }

            SetUpForFlight(idFlight);

            InitializeRestriction();

            // Expand the New Flight box if we're editing an existing flight
            if (idFlight != LogbookEntry.idFlightNew || !String.IsNullOrEmpty(util.GetStringParam(Request, "src")))
                AccordionCtrl.SelectedIndex = 0;

            string szTitle = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
            lblUserName.Text = Master.Title = szTitle;
        }
        else
        {
        }

        mfbChartTotals1.SourceData = mfbLogbook1.Data;  // do this every time.
    }

    private const string szVSFlightID = "vsFlightID";

    protected int FlightID
    {
        get { return ViewState[szVSFlightID] == null ? LogbookEntry.idFlightNone : (int)ViewState[szVSFlightID]; }
        set { ViewState[szVSFlightID] = value; }
    }

    protected bool IsNewFlight
    {
        get { return FlightID == LogbookEntry.idFlightNew; }
    }

    protected void SetUpForFlight(int idFlight)
    {
        FlightID = idFlight;
        mfbEditFlight1.SetUpNewOrEdit(idFlight);
        mfbEditFlight1.CanCancel = !IsNewFlight;
        pnlAccordionMenuContainer.Visible = mfbLogbook1.Visible = pnlFilter.Visible = IsNewFlight;
    }

    protected void ResolvePrintLink()
    {
        lnkPrintView.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/PrintView.aspx?po={0}&fq={1}",
            HttpUtility.UrlEncode(Convert.ToBase64String(JsonConvert.SerializeObject(PrintOptions1.Options, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }).Compress())), 
            HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString()));
    }

    protected void Refresh()
    {
        bool fRestrictionIsDefault = Restriction.IsDefault;
        mfbLogbook1.DetailsParam = fRestrictionIsDefault ? string.Empty : "fq=" + HttpUtility.UrlEncode(Restriction.ToBase64CompressedJSONString());
        mfbLogbook1.User = Page.User.Identity.Name;
        mfbLogbook1.Restriction = Restriction;
        mfbLogbook1.RefreshData();
        if (mfbChartTotals1.Visible)
            mfbChartTotals1.Refresh(mfbLogbook1.Data);
        if (mfbTotalSummary1.Visible)
            mfbTotalSummary1.CustomRestriction = Restriction;
        ResolvePrintLink();
        pnlFilter.Visible = !fRestrictionIsDefault && IsNewFlight;
        mfbQueryDescriptor1.DataSource = fRestrictionIsDefault ? null : Restriction;
        apcFilter.LabelControl.Font.Bold = !fRestrictionIsDefault;
        apcFilter.IsEnhanced = !fRestrictionIsDefault;

        if (!IsNewFlight)
        {
            int prevFlightID, nextFlightID;
            mfbLogbook1.GetNeighbors(FlightID, out prevFlightID, out nextFlightID);
            mfbEditFlight1.SetPrevFlight(prevFlightID);
            mfbEditFlight1.SetNextFlight(nextFlightID);
        }

        mfbQueryDescriptor1.DataBind();
    }

    protected void UpdateQuery()
    {
        Restriction = mfbSearchForm1.Restriction;
        Refresh();
        AccordionCtrl.SelectedIndex = -1;

        int idxLast;
        if (Int32.TryParse(hdnLastViewedPaneIndex.Value, out idxLast))
        {
            if (idxLast == mfbAccordionProxyExtender1.IndexForProxyID(apcTotals.ID))
            {
                apcTotals_ControlClicked(apcTotals, null);
                AccordionCtrl.SelectedIndex = idxLast;
            }
            else if (idxLast == mfbAccordionProxyExtender1.IndexForProxyID(apcAnalysis.ID))
            {
                apcAnalysis_ControlClicked(apcAnalysis, null);
                AccordionCtrl.SelectedIndex = idxLast;
            }
            else if (idxLast == mfbAccordionProxyExtender1.IndexForProxyID(apcPrintView.ID))
                AccordionCtrl.SelectedIndex = idxLast;
        }
    }

    protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm1.Restriction = Restriction.ClearRestriction(fic.FilterItem);
        Refresh();
    }

    protected void mfbSearchForm1_QuerySubmitted(object sender, EventArgs e)
    {
        UpdateQuery();
    }

    protected void mfbSearchForm1_Reset(object sender, EventArgs e)
    {
        UpdateQuery();
    }

    protected void mfbEditFlight1_FlightUpdated(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        // if we had been editing a flight do a redirect so we have a clean URL
        // OR if there are pending redirects, do them.
        // Otherwise, just clean the page.
        if (Request[szParamIDFlight] != null || SocialNetworkAuthorization.RedirectList.Count > 0)
            Response.Redirect(SocialNetworkAuthorization.PopRedirect(Master.IsMobileSession() ? SocialNetworkAuthorization.DefaultRedirPageMini : SocialNetworkAuthorization.DefaultRedirPage));
        else if (e.IDNextFlight != LogbookEntry.idFlightNone)
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}{1}", e.IDNextFlight, Request.Url.Query), true);
        else
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx{0}", Request.Url.Query), true);
    }

    protected void mfbEditFlight1_FlightEditCanceled(object sender, EventArgs e)
    {
        // Redirect back to eliminate the ID of the flight in the URL.
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx{0}", Request.Url.Query), true);
    }

    protected void PrintOptions1_OptionsChanged(object sender, PrintingOptionsEventArgs e)
    {
        ResolvePrintLink();
    }

    protected void rblTotalsMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender == null)
            throw new ArgumentNullException("sender");

        bool fGrouped;
        if (bool.TryParse(rblTotalsMode.SelectedValue, out fGrouped))
        {
            mfbTotalSummary1.DefaultGroupMode = fGrouped;
            mfbTotalSummary1.IsGrouped = fGrouped;
        }
    }

    #region lazy loading of tab content
    protected void TurnOffLazyLoad(object o)
    {
        Controls_mfbAccordionProxyControl apc = (Controls_mfbAccordionProxyControl)o;
        apc.LazyLoad = false;
        int idx = mfbAccordionProxyExtender1.IndexForProxyID(apc.ID);
        mfbAccordionProxyExtender1.SetJavascriptForControl(apc, true, idx);
        AccordionCtrl.SelectedIndex = idx;
    }

    protected void TurnOnLazyLoad(Controls_mfbAccordionProxyControl apc, Action act)
    {
        if (apc == null)
            throw new ArgumentNullException("apc");
        int idx = mfbAccordionProxyExtender1.IndexForProxyID(apc.ID);
        if (idx == AccordionCtrl.SelectedIndex)
        {
            if (act != null)
                act();
        }
        else
        {
            apc.LazyLoad = true;
            mfbAccordionProxyExtender1.SetJavascriptForControl(apc, idx == AccordionCtrl.SelectedIndex, idx);
        }
    }

    protected int IndexForPane(AjaxControlToolkit.AccordionPane p)
    {
        for (int i = 0; i < AccordionCtrl.Panes.Count; i++)
            if (AccordionCtrl.Panes[i] == p)
                return i;
        return -1;
    }

    protected void apcTotals_ControlClicked(object sender, EventArgs e)
    {
        TurnOffLazyLoad(sender);
        mfbTotalSummary1.Visible = true;
        mfbTotalSummary1.CustomRestriction = Restriction;
        hdnLastViewedPaneIndex.Value = mfbAccordionProxyExtender1.IndexForProxyID(apcTotals.ID).ToString(CultureInfo.InvariantCulture);
    }

    protected void apcAnalysis_ControlClicked(object sender, EventArgs e)
    {
        TurnOffLazyLoad(sender);
        mfbChartTotals1.Visible = true;
        mfbChartTotals1.Refresh(mfbLogbook1.Data);
        hdnLastViewedPaneIndex.Value = mfbAccordionProxyExtender1.IndexForProxyID(apcAnalysis.ID).ToString(CultureInfo.InvariantCulture);
    }
    #endregion

    protected void apcCurrency_ControlClicked(object sender, EventArgs e)
    {
        TurnOffLazyLoad(sender);
        mfbCurrency1.Visible = true;
        mfbCurrency1.RefreshCurrencyTable();
    }

    protected void mfbLogbook1_ItemDeleted(object sender, LogbookEventArgs e)
    {
        // Turn on lazy load for any items that could be affected by the deletion, or else refresh them if already visible.
        TurnOnLazyLoad(apcTotals, () => { mfbTotalSummary1.CustomRestriction = Restriction; });
        TurnOnLazyLoad(apcCurrency, () => { mfbCurrency1.RefreshCurrencyTable(); });
        TurnOnLazyLoad(apcAnalysis, () => { mfbChartTotals1.Refresh(mfbLogbook1.Data); });
    }
}