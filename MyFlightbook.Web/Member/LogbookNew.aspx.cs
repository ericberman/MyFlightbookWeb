using MyFlightbook.Currency;
using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2017-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class LogbookNew : Page
    {
        private const string szParamIDFlight = "idFlight";

        public enum FlightsTab { None, Add, Search, Totals, Currency, Analysis, Printing, More }

        protected FlightsTab DefaultTab
        {
            get { return Enum.TryParse(hdnLastViewedPane.Value, out FlightsTab ft) ? ft : FlightsTab.None; }
            set { hdnLastViewedPane.Value = value.ToString(); }
        }

        protected string DefaultPane
        {
            get { return "apc" + DefaultTab.ToString(); }
        }

        protected bool GroupedTotals {  get { return new UserTotals() { Username = Page.User.Identity.Name }.DefaultGroupModeForUser; } }

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

        private void InitPassedQuery(string szFQParam)
        {
            if (!String.IsNullOrEmpty(szFQParam))
            {
                try
                {
                    Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
                    if (Restriction.UserName.CompareOrdinal(User.Identity.Name) != 0)
                        throw new UnauthorizedAccessException();
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is JsonSerializationException || ex is JsonException || ex is UnauthorizedAccessException) 
                {
                    Restriction = new FlightQuery(User.Identity.Name);
                }
            }
            else
                Restriction = new FlightQuery(Page.User.Identity.Name);
        }


        private void InitDateParams(int year, int month, int week, int day)
        {
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
        }

        private void InitAircraftModelRestriction(string szReqTail, string szReqModel, string szReqICAO, string szcc)
        {
            if (!String.IsNullOrEmpty(szReqTail) || !String.IsNullOrEmpty(szReqModel) || !String.IsNullOrEmpty(szReqICAO))
            {
                UserAircraft ua = new UserAircraft(Restriction.UserName);
                Collection<Aircraft> lstac = new Collection<Aircraft>();
                HashSet<int> lstmm = new HashSet<int>();

                foreach (Aircraft ac in ua.GetAircraftForUser())
                {
                    if (ac.DisplayTailnumber.CompareCurrentCultureIgnoreCase(szReqTail) == 0)
                        lstac.Add(ac);

                    MakeModel mm = MakeModel.GetModel(ac.ModelID);
                    if (!lstmm.Contains(mm.MakeModelID) &&
                        ((!String.IsNullOrEmpty(szReqModel) && mm.Model.CompareCurrentCultureIgnoreCase(szReqModel) == 0) ||
                        (!String.IsNullOrEmpty(szReqICAO) && mm.FamilyName.CompareCurrentCultureIgnoreCase(szReqICAO) == 0)))
                        lstmm.Add(mm.MakeModelID);
                }
                if (lstac.Count > 0)
                {
                    Restriction.AirportList.Clear();
                    Restriction.AddAircraft(lstac);
                }
                if (lstmm.Count > 0)
                {
                    Restriction.MakeList.Clear();
                    Restriction.AddModels(lstmm);
                }
            }
            if (!String.IsNullOrEmpty(szcc))
            {
                foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                    if (cc.CatClass.CompareCurrentCultureIgnoreCase(szcc) == 0)
                        Restriction.AddCatClass(cc);
            }
        }

        protected void InitializeRestriction()
        {
            string szSearchParam = util.GetStringParam(Request, "s");
            string szAirportParam = util.GetStringParam(Request, "ap");

            InitPassedQuery(util.GetStringParam(Request, "fq"));

            if (!String.IsNullOrEmpty(szSearchParam))
                Restriction.GeneralText = szSearchParam;
            if (!String.IsNullOrEmpty(szAirportParam))
            {
                Restriction.AirportList.Clear();
                Restriction.AddAirports(MyFlightbook.Airports.AirportList.NormalizeAirportList(szAirportParam));
            }

            InitDateParams(util.GetIntParam(Request, "y", -1), util.GetIntParam(Request, "m", -1), util.GetIntParam(Request, "w", -1), util.GetIntParam(Request, "d", -1));

            InitAircraftModelRestriction(util.GetStringParam(Request, "tn"), util.GetStringParam(Request, "mn"), util.GetStringParam(Request, "icao"), util.GetStringParam(Request, "cc"));

            mfbSearchForm1.Restriction = Restriction;
            Restriction.Refresh();

            Refresh();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.tabLogbook;
            Page.ClientScript.RegisterClientScriptInclude("accordionExtender", ResolveClientUrl("~/public/Scripts/accordionproxy.js"));

            pnlWelcomeNewUser.Visible = false;
            if (!IsPostBack)
            {
                if (Request.Cookies[MFBConstants.keyNewUser] != null && !String.IsNullOrEmpty(Request.Cookies[MFBConstants.keyNewUser].Value) || util.GetStringParam(Request, "sw").Length > 0 || Request.PathInfo.Contains("/sw"))
                {
                    Response.Cookies[MFBConstants.keyNewUser].Expires = DateTime.Now.AddDays(-1);
                    pnlWelcomeNewUser.Visible = true;
                }

                rblTotalsMode.SelectedValue = GroupedTotals.ToString();

                DefaultTab = FlightsTab.None;   // default value;

                // Handle a requested tab
                if (Enum.TryParse(util.GetStringParam(Request, "ft") ?? string.Empty, out FlightsTab ft))
                    DefaultTab = ft;

                int idFlight = util.GetIntParam(Request, szParamIDFlight, LogbookEntryCore.idFlightNew);

                // Redirect to the non-querystring based page so that Ajax file upload works
                if (idFlight != LogbookEntryCore.idFlightNew)
                {
                    string szNew = Request.Url.PathAndQuery.Replace(".aspx", String.Format(CultureInfo.InvariantCulture, ".aspx/{0}", idFlight)).Replace(String.Format(CultureInfo.InvariantCulture, "{0}={1}", szParamIDFlight, idFlight), string.Empty).Replace("?&", "?");
                    Response.Redirect(szNew, true);
                    return;
                }

                if (Request.PathInfo.Length > 0 && Int32.TryParse(Request.PathInfo.Substring(1), out int id))
                    idFlight = id;

                SetUpForFlight(idFlight);

                InitializeRestriction();

                // Expand the New Flight box if we're editing an existing flight
                if (idFlight != LogbookEntryCore.idFlightNew || !String.IsNullOrEmpty(util.GetStringParam(Request, "src")))
                    DefaultTab = FlightsTab.Add;

                string szTitle = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, Profile.GetUser(User.Identity.Name).UserFullName);
                lblUserName.Text = Master.Title = HttpUtility.HtmlEncode(szTitle);

                // See if we just entered a new flight and scroll to it as needed
                if (Session[MFBConstants.keySessLastNewFlight] != null)
                {
                    mfbLogbook1.ScrollToFlight((int)Session[MFBConstants.keySessLastNewFlight]);
                    Session[MFBConstants.keySessLastNewFlight] = null;
                }
            }
        }

        private const string szVSFlightID = "vsFlightID";

        protected int FlightID
        {
            get { return ViewState[szVSFlightID] == null ? LogbookEntryCore.idFlightNone : (int)ViewState[szVSFlightID]; }
            set { ViewState[szVSFlightID] = value; }
        }

        protected bool IsNewFlight
        {
            get { return FlightID == LogbookEntryCore.idFlightNew; }
        }

        protected void SetUpForFlight(int idFlight)
        {
            FlightID = idFlight;
            mfbEditFlight1.SetUpNewOrEdit(idFlight);
            mfbEditFlight1.CanCancel = !IsNewFlight;
            // TODO: why was this here? */
            /* pnlAccordionMenuContainer.Visible = */ mfbLogbook1.Visible = pnlFilter.Visible = IsNewFlight;
        }

        protected void ResolvePrintLink()
        {
            if (!ckEndorsements.Checked)
                ckIncludeEndorsementImages.Checked = false;
            PrintingOptions po = new PrintingOptions()
            {
                Sections = new PrintingSections()
                {
                    Endorsements = ckEndorsements.Checked ? (ckIncludeEndorsementImages.Checked ? PrintingSections.EndorsementsLevel.DigitalAndPhotos : PrintingSections.EndorsementsLevel.DigitalOnly) : PrintingSections.EndorsementsLevel.None,
                    IncludeCoverPage = ckIncludeCoverSheet.Checked,
                    IncludeFlights = true,
                    IncludeTotals = ckTotals.Checked,
                    CompactTotals = ckCompactTotals.Checked
                }
            };
            lnkPrintView.NavigateUrl = PrintingOptions.PermaLink(Restriction, po, Request.Url.Host, Request.Url.Scheme).ToString();
        }

        protected void Refresh()
        {
            bool fRestrictionIsDefault = Restriction.IsDefault;
            mfbLogbook1.DetailsParam = fRestrictionIsDefault ? string.Empty : "fq=" + Restriction.ToBase64CompressedJSONString();
            mfbLogbook1.User = Page.User.Identity.Name;
            mfbLogbook1.Restriction = Restriction;
            mfbLogbook1.RefreshData();
            ResolvePrintLink();
            pnlFilter.Visible = !fRestrictionIsDefault && IsNewFlight;
            mfbQueryDescriptor1.DataSource = fRestrictionIsDefault ? null : Restriction;

            if (!IsNewFlight)
            {
                mfbLogbook1.CurrentResult.GetNeighbors(FlightID, out int prevFlightID, out int nextFlightID);
                mfbEditFlight1.SetPrevFlight(prevFlightID);
                mfbEditFlight1.SetNextFlight(nextFlightID);
            }

            mfbQueryDescriptor1.DataBind();
        }

        protected void UpdateQuery()
        {
            Restriction = mfbSearchForm1.Restriction;
            Refresh();
        }

        protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
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

        /// <summary>
        /// Returns the querystring minus Clone or Reverse or other switches that we don't want to preserve for context (Issue #458)
        /// </summary>
        /// <returns></returns>
        private string SanitizedQuery
        {
            get { return Request.QueryStringWithoutParams(paramsToRemove); }
        }

        private static readonly string[] paramsToRemove = new string[] { "Clone", "Reverse", "Chk" };

        protected void mfbEditFlight1_FlightUpdated(object sender, LogbookEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            // if we had been editing a flight do a redirect so we have a clean URL
            // OR if there are pending redirects, do them.
            // Otherwise, just clean the page.
            if (e.IDNextFlight != LogbookEntryCore.idFlightNone)
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}{1}", e.IDNextFlight, SanitizedQuery), true);
            else
            {
                // If this is a new flight, put its assigned ID into the session so that we can scroll to it.
                if (IsNewFlight)
                    Session[MFBConstants.keySessLastNewFlight] = e.FlightID;
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx{0}", SanitizedQuery), true);
            }
        }

        protected void mfbEditFlight1_FlightEditCanceled(object sender, EventArgs e)
        {
            // Redirect back to eliminate the ID of the flight in the URL.
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx{0}", SanitizedQuery), true);
        }

        protected void rblTotalsMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            if (bool.TryParse(rblTotalsMode.SelectedValue, out bool fGrouped))
            {
                new UserTotals() { Username = Page.User.Identity.Name }.DefaultGroupModeForUser = fGrouped;
                DefaultTab = FlightsTab.Totals;
            }
        }
    }
}