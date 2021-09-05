using MyFlightbook.Lint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    public partial class CheckFlights : Page
    {
        #region WebMethods
        [WebMethod(EnableSession = true)]
        public static void ToggleIgnore(int idFlight, bool fIgnore)
        {

            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException();
            LogbookEntryBase le = new LogbookEntry();
            if (!le.FLoadFromDB(idFlight, HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException();

            if (le.IsNewFlight) // should never happen.
                throw new InvalidOperationException("Flight doesn't exist");

            le.Route = String.Format(CultureInfo.CurrentCulture, "{0}{1}", le.Route.Trim(), fIgnore ? FlightLint.IgnoreMarker : string.Empty);
            le.CommitRoute();  // Save the change, but hold on to the flight in the list for now so that you can uncheck it.
        }
        #endregion

        protected const string szCookieLastCheck = "cookieLastCheck";

        private const string szVsFlightIssues = "vsFlightIssues"; 

        private IEnumerable<FlightWithIssues> CheckedFlights
        {
            get { return (IEnumerable<FlightWithIssues>)ViewState[szVsFlightIssues]; }
            set { ViewState[szVsFlightIssues] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            mfbDateLastCheck.DefaultDate = DateTime.MinValue;

            if (!IsPostBack)
            {
                lblCheckFlightsCategories.Text = Branding.ReBrand(Resources.FlightLint.CheckFlightsCategoriesHeader);
                SetOptions(FlightLint.DefaultOptionsForLocale);

                mfbDateLastCheck.Date = DateTime.MinValue;

                if (Request.Cookies[szCookieLastCheck] != null)
                {
                    string szLastCheck = Request.Cookies[szCookieLastCheck].Value;
                    if (DateTime.TryParse(szLastCheck, out DateTime dtLastCheck))
                    {
                        spanLastCheck.Visible = true;
                        lblLastCheck.Text = String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.PromptLastCheckDate, dtLastCheck);
                        hdnLastDateCheck.Value = dtLastCheck.ToString("d", CultureInfo.CurrentCulture);
                    }
                }
            }
        }

        protected UInt32 SelectedOptions
        {
            get
            {
                UInt32 val = 0;
                val |= (ckAirports.Checked ? (UInt32) LintOptions.AirportIssues : 0);
                val |= (ckDateTime.Checked ? (UInt32)LintOptions.DateTimeIssues : 0);
                val |= (ckIFR.Checked ? (UInt32)LintOptions.IFRIssues : 0);
                val |= (ckMisc.Checked ? (UInt32)LintOptions.MiscIssues : 0);
                val |= (ckPICSICDualMath.Checked ? (UInt32)LintOptions.PICSICDualMath : 0);
                val |= (ckSim.Checked ? (UInt32)LintOptions.SimIssues : 0);
                val |= (ckTimes.Checked ? (UInt32)LintOptions.TimeIssues : 0);
                val |= (ckXC.Checked ? (UInt32)LintOptions.XCIssues : 0);
                val |= (ckIgnored.Checked ? (UInt32)LintOptions.IncludeIgnored : 0);
                return val;
            }
        }

        protected void SetOptions(UInt32 options)
        {
            ckAirports.Checked = (options & (UInt32) LintOptions.AirportIssues) != 0;
            ckDateTime.Checked = (options & (UInt32)LintOptions.DateTimeIssues) != 0;
            ckIFR.Checked = (options & (UInt32)LintOptions.IFRIssues) != 0;
            ckMisc.Checked = (options & (UInt32)LintOptions.MiscIssues) != 0;
            ckPICSICDualMath.Checked = (options & (UInt32)LintOptions.PICSICDualMath) != 0;
            ckSim.Checked = (options & (UInt32)LintOptions.SimIssues) != 0;
            ckTimes.Checked = (options & (UInt32)LintOptions.TimeIssues) != 0;
            ckXC.Checked = (options & (UInt32)LintOptions.XCIssues) != 0;
            ckIgnored.Checked = (options & (UInt32)LintOptions.IncludeIgnored) != 0;
        }

        protected void BindFlights(IEnumerable<FlightWithIssues> rgf, int cFlightsChecked)
        {
            gvFlights.DataSource = CheckedFlights = rgf;
            gvFlights.DataBind();
            if (cFlightsChecked > 0)
                lblSummary.Text = String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.SummaryFlightsFound, cFlightsChecked, CheckedFlights.Count());
        }

        protected void btnCheckAll_Click(object sender, EventArgs e)
        {
            UInt32 selectedOptions = SelectedOptions;
            if (selectedOptions == 0)
            {
                lblErr.Text = Resources.FlightLint.errNoOptionsSelected;
                return;
            }

            FlightQuery fq = new FlightQuery(Page.User.Identity.Name);
            if (mfbDateLastCheck.Date.HasValue())
            {
                fq.DateRange = FlightQuery.DateRanges.Custom;
                fq.DateMin = mfbDateLastCheck.Date;
            }
            DBHelperCommandArgs dbhq = LogbookEntryBase.QueryCommand(fq, fAsc:true);
            IEnumerable<LogbookEntryBase> rgle = LogbookEntryDisplay.GetFlightsForQuery(dbhq, Page.User.Identity.Name, "Date", SortDirection.Ascending, false, false);

            BindFlights(new FlightLint().CheckFlights(rgle, Page.User.Identity.Name, selectedOptions, mfbDateLastCheck.Date), rgle.Count());

            Response.Cookies[szCookieLastCheck].Value = DateTime.Now.YMDString();
            Response.Cookies[szCookieLastCheck].Expires = DateTime.Now.AddYears(5);
        }

        protected void ckAll_CheckedChanged(object sender, EventArgs e)
        {
            ckAirports.Checked = ckDateTime.Checked = ckIFR.Checked = ckMisc.Checked = ckPICSICDualMath.Checked = ckSim.Checked = ckTimes.Checked = ckXC.Checked = ckAll.Checked;
        }

        protected void mfbEditFlight_FlightEditCanceled(object sender, EventArgs e)
        {
            mvCheckFlight.SetActiveView(vwIssues);
        }

        protected void mfbEditFlight_FlightUpdated(object sender, LogbookEventArgs e)
        {
            mvCheckFlight.SetActiveView(vwIssues);

            // Recheck the flight that was updated.
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookEntryBase le = new LogbookEntry(e.FlightID, Page.User.Identity.Name);
            IEnumerable<FlightWithIssues> updated = new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, Page.User.Identity.Name, SelectedOptions);

            List<FlightWithIssues> lst = new List<FlightWithIssues>(CheckedFlights);
            int index = lst.FindIndex(fwi => fwi.Flight.FlightID == e.FlightID);
            if (updated.Any())
                lst[index] = updated.ElementAt(0);
            else
                lst.RemoveAt(index);

            BindFlights(lst, -1);
        }

        protected void lnkEditFlight_Click(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            LinkButton lb = sender as LinkButton;
            if (Int32.TryParse(lb.CommandArgument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idflight))
            {
                mvCheckFlight.SetActiveView(vwEdit);
                mfbEditFlight.SetUpNewOrEdit(idflight);
            }
        }

        protected void gvFlights_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                FlightWithIssues le = CheckedFlights.ElementAt(e.Row.RowIndex);
                CheckBox ck = (CheckBox)e.Row.FindControl("ckIgnore");
                ck.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:toggleIgnore(this, {0});", le.Flight.FlightID);
                ck.Checked = le.Flight.Route.EndsWith(FlightLint.IgnoreMarker, StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}