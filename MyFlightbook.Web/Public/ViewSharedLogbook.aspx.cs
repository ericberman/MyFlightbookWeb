using MyFlightbook.Achievements;
using MyFlightbook.Airports;
using MyFlightbook.Telemetry;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Public
{
    public partial class ViewSharedLogbook : System.Web.UI.Page
    {
        private const string keyVSRestriction = "vsCurrentRestriction";
        private const string keyVSShareKey = "vsCurrentShareKey";
        private const string szKeyVAState = "VisitedAirports";

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

        protected ShareKey CurrentShareKey
        {
            get { return (ShareKey)ViewState[keyVSShareKey]; }
            set { ViewState[keyVSShareKey] = value; }
        }

        protected void SetAccordionPane(string ID)
        {
            for (int i = 0; i < AccordionCtrl.Panes.Count; i++)
            {
                if (AccordionCtrl.Panes[i].ID == ID)
                {
                    AccordionCtrl.SelectedIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// We set up the sharekey here because mfblogbook could try to load first - if it's an invalid sharekey, then we'll do the redirect without a db hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(object sender, EventArgs e)
        {
            string guid = util.GetStringParam(Request, "g");
            ShareKey sk = ShareKey.ShareKeyWithID(guid);
            if (sk == null)
                Response.Redirect("~/HTTP403.htm");
            CurrentShareKey = sk;
            mfbLogbook.User = sk.Username;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ShareKey sk = CurrentShareKey;  // set in Page_Init
                Profile pf = Profile.GetUser(sk.Username);

                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, pf.UserFullName);

                // Indicate success.
                sk.FUpdateAccess();

                int privCount = sk.PrivilegeCount;

                apcAnalysis.Container.Style["display"] = apcCurrency.Container.Style["display"] = apcTotals.Container.Style["display"] = 
                    apcFilter.Container.Style["display"] = apcAchievements.Container.Style["display"] = apcAirports.Container.Style["display"] = "none";

                if (sk.CanViewCurrency)
                {
                    apcCurrency.Container.Style["display"] = "inline-block";
                    mfbCurrency.Visible = true;
                    mfbCurrency.UserName = sk.Username;
                    mfbCurrency.RefreshCurrencyTable();
                    if (privCount == 1) // if ONLY showing currency, expand it and hide the accordion
                    {
                        pnlAccordionMenuContainer.Visible = false;
                        SetAccordionPane(acpPaneCurrency.ID);
                    }
                }

                if (sk.CanViewTotals)
                {
                    apcTotals.Container.Style["display"] = "inline-block";
                    mfbTotalSummary.Visible = true;
                    mfbTotalSummary.Username = sk.Username;
                    mfbTotalSummary.CustomRestriction = new FlightQuery(sk.Username);   // will call bind

                    if (privCount == 1) // if ONLY showing totals, expand it and hide the accordion
                    {
                        pnlAccordionMenuContainer.Visible = false;
                        SetAccordionPane(acpPaneTotals.ID);
                    }
                }

                if (sk.CanViewFlights)
                {
                    apcAnalysis.Container.Style["display"] = apcFilter.Container.Style["display"] = "inline-block";
                    mfbLogbook.Visible = true;
                    mfbSearchForm.Username = mfbLogbook.User = sk.Username;
                    mfbLogbook.RefreshData();

                    mfbChartTotals.Visible = true;
                }

                if (sk.CanViewAchievements)
                {
                    apcAchievements.Container.Style["display"] = "inline-block";
                    mfbRecentAchievements.Visible = mvBadges.Visible = true;

                    List<Badge> lst = new Achievement(sk.Username).BadgesForUser();
                    if (lst == null || lst.Count == 0)
                        mvBadges.SetActiveView(vwNoBadges);
                    else
                    {
                        mvBadges.SetActiveView(vwBadges);
                        rptBadgeset.DataSource = BadgeSet.BadgeSetsFromBadges(lst);
                        rptBadgeset.DataBind();
                    }
                    mfbRecentAchievements.AutoDateRange = true;
                    mfbRecentAchievements.Refresh(sk.Username, DateTime.MaxValue, DateTime.MinValue, false);
                    lblRecentAchievementsTitle.Text = mfbRecentAchievements.Summary;
                    if (privCount == 1) // if ONLY showing achievements, expand it
                        SetAccordionPane(acpPaneAchievements.ID);
                }

                if (sk.CanViewVisitedAirports)
                {
                    apcAirports.Container.Style["display"] = "inline-block";
                    Restriction.UserName = mfbSearchForm.Username = mfbLogbook.User = sk.Username;
                    if (privCount == 1) // if ONLY showing airports, expand it
                        SetAccordionPane(acpPaneAirports.ID);
                }
            }

            if (mfbLogbook.Visible && apcAnalysis.Visible)
            {
                mfbChartTotals.HistogramManager = LogbookEntryDisplay.GetHistogramManager(mfbLogbook.Data, CurrentShareKey.Username);   // do this every time, since charttotals doesn't persist its data.
                mfbChartTotals.Refresh();
            }

            if (CurrentShareKey.CanViewVisitedAirports)
                RefreshVisitedAirports();
        }

        protected void UpdateForUser(string szUser)
        {
            FlightQuery r = Restriction;

            mfbTotalSummary.Username = mfbCurrency.UserName = mfbLogbook.User = szUser;

            if (CurrentShareKey.CanViewTotals)
                mfbTotalSummary.CustomRestriction = mfbLogbook.Restriction = r;

            bool fRestrictionIsDefault = r.IsDefault;
            mfbQueryDescriptor.DataSource = fRestrictionIsDefault ? null : r;
            mfbQueryDescriptor.DataBind();
            apcFilter.LabelControl.Font.Bold = !fRestrictionIsDefault;
            apcFilter.IsEnhanced = !fRestrictionIsDefault;
            pnlFilter.Visible = !fRestrictionIsDefault;

            if (CurrentShareKey.CanViewFlights)
                mfbLogbook.RefreshData();

            mfbChartTotals.HistogramManager = LogbookEntryDisplay.GetHistogramManager(mfbLogbook.Data, r.UserName);
            mfbChartTotals.Refresh();

            if (CurrentShareKey.CanViewVisitedAirports)
                RefreshVisitedAirports();
        }

        protected void UpdateQuery()
        {
            Restriction = mfbSearchForm.Restriction;
            CurrentVisitedAirports = null;
            UpdateForUser(CurrentShareKey.Username);
            AccordionCtrl.SelectedIndex = -1;
        }

        public void ClearForm(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void ShowResults(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
            mfbSearchForm.Restriction = Restriction.ClearRestriction(fic.FilterItem);
            UpdateQuery();
        }

        #region Visited Airports
        private VisitedAirport[] CurrentVisitedAirports
        {
            get { return (VisitedAirport[])ViewState[szKeyVAState]; }
            set { ViewState[szKeyVAState] = value; }
        }

        protected int LastSortDirection
        {
            get { return Convert.ToInt32(hdnLastSortDirection.Value, CultureInfo.InvariantCulture); }
            set { hdnLastSortDirection.Value = value.ToString(CultureInfo.InvariantCulture); }
        }

        protected string LastSortExpression
        {
            get { return hdnLastSortExpression.Value; }
            set { hdnLastSortExpression.Value = value; }
        }

        protected void RefreshVisitedAirports()
        {
            if (CurrentVisitedAirports == null)
                CurrentVisitedAirports = VisitedAirport.VisitedAirportsForQuery(Restriction);
            lblNumAirports.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.VisitedAirportsNumAirports, CurrentVisitedAirports.Length);

            gvAirports.DataSource = CurrentVisitedAirports;
            gvAirports.DataBind();

            mfbGoogleMapManager1.Visible = CurrentVisitedAirports.Length > 0;   //  Avoid excessive map loads.

            AirportList alMatches = new AirportList(CurrentVisitedAirports);

            // get an airport list of the airports
            mfbGoogleMapManager1.Map.SetAirportList(alMatches);

            lnkZoomOut.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;
            lnkZoomOut.Visible = (CurrentVisitedAirports.Length > 0);
        }

        protected void gvAirports_DataBound(Object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
            {
                PlaceHolder p = (PlaceHolder)e.Row.FindControl("plcZoomCode");
                HtmlAnchor a = new HtmlAnchor();
                p.Controls.Add(a);
                VisitedAirport va = (VisitedAirport)e.Row.DataItem;

                string szLink = String.Format(CultureInfo.InvariantCulture, "javascript:{0}.gmap.setCenter(new google.maps.LatLng({1}, {2}));{0}.gmap.setZoom(14);",
                    mfbGoogleMapManager1.MapID, va.Airport.LatLong.Latitude, va.Airport.LatLong.Longitude);
                a.InnerText = va.Code;
                a.HRef = szLink;
            }
        }

        protected void gvAirports_Sorting(Object sender, GridViewSortEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.SortExpression.CompareOrdinalIgnoreCase(LastSortExpression) != 0)
            {
                LastSortDirection = 1;
                LastSortExpression = e.SortExpression;
            }
            else if (LastSortDirection != 1)
                LastSortDirection = 1;
            else
                LastSortDirection = -1;

            int Direction = LastSortDirection;

            switch (e.SortExpression.ToUpperInvariant())
            {
                case "CODE":
                    Array.Sort(CurrentVisitedAirports, delegate (VisitedAirport va1, VisitedAirport va2) { return Direction * va1.Code.CompareCurrentCultureIgnoreCase(va2.Code); });
                    break;
                case "FACILITYNAME":
                    Array.Sort(CurrentVisitedAirports, delegate (VisitedAirport va1, VisitedAirport va2) { return Direction * va1.FacilityName.CompareCurrentCultureIgnoreCase(va2.FacilityName); });
                    break;
                case "NUMBEROFVISITS":
                    Array.Sort(CurrentVisitedAirports, delegate (VisitedAirport va1, VisitedAirport va2) { return Direction * va1.NumberOfVisits.CompareTo(va2.NumberOfVisits); });
                    break;
                case "EARLIESTVISITDATE":
                    Array.Sort(CurrentVisitedAirports, delegate (VisitedAirport va1, VisitedAirport va2) { return Direction * va1.EarliestVisitDate.CompareTo(va2.EarliestVisitDate); });
                    break;
                case "LATESTVISITDATE":
                    Array.Sort(CurrentVisitedAirports, delegate (VisitedAirport va1, VisitedAirport va2) { return Direction * va1.LatestVisitDate.CompareTo(va2.LatestVisitDate); });
                    break;
            }
            gvAirports.DataSource = CurrentVisitedAirports;
            gvAirports.DataBind();
        }

        protected void btnEstimateDistance_Click(object sender, EventArgs e)
        {
            lblErr.Text = string.Empty;
            double distance = VisitedAirport.DistanceFlownByUser(Restriction, out string szErr);

            if (String.IsNullOrEmpty(szErr))
            {
                btnEstimateDistance.Visible = false;
                pnlDistanceResults.Visible = true;
                lblDistanceEstimate.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.VisitedAirportsDistanceEstimate, distance);
            }
            else
                lblErr.Text = szErr;
        }

        protected void btnGetTotalKML(object sender, EventArgs e)
        {
            DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
            Response.Clear();
            Response.ContentType = dst.Mimetype;
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename={0}-AllFlights.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));
            VisitedAirport.AllFlightsAsKML(Restriction, Response.OutputStream, out _);
            Response.End();
        }
        #endregion
    }
}