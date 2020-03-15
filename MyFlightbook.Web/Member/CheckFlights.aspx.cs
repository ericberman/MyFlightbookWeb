using MyFlightbook.Lint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    public partial class CheckFlights : System.Web.UI.Page
    {
        protected const string szCookieLastCheck = "cookieLastCheck";

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
            DBHelperCommandArgs dbhq = LogbookEntryBase.QueryCommand(fq, fAsc:true);
            IEnumerable<LogbookEntryBase> rgle = LogbookEntryDisplay.GetFlightsForQuery(dbhq, Page.User.Identity.Name, "Date", SortDirection.Ascending, false, false);

            FlightLint fl = new FlightLint();
            IEnumerable<FlightWithIssues> flightsWithIssues = fl.CheckFlights(rgle, Page.User.Identity.Name, selectedOptions, mfbDateLastCheck.Date);
            gvFlights.DataSource = flightsWithIssues;
            gvFlights.DataBind();
            lblSummary.Text = String.Format(CultureInfo.CurrentCulture, Resources.FlightLint.SummaryFlightsFound, rgle.Count(), flightsWithIssues.Count());

            Response.Cookies[szCookieLastCheck].Value = DateTime.Now.YMDString();
            Response.Cookies[szCookieLastCheck].Expires = DateTime.Now.AddYears(5);
        }
    }
}