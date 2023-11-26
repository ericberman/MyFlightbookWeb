using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class Member_Admin : AdminPage
    {
        private static bool IsAuthorizedForTab(tabID sidebarTab, Profile pf)
        {
            switch (sidebarTab)
            {
                case tabID.admUsers:
                    return pf.CanSupport;
                case tabID.admModels:
                case tabID.admAirports:
                case tabID.admMisc:
                    return pf.CanManageData;
                default:
                    return false;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            CheckAdmin(pf.CanDoSomeAdmin);

            if (!IsPostBack)
            {
                CheckAdmin(IsAuthorizedForTab(tabID.admMisc, pf));
                DisplayMemStats();
            }

            Master.SelectedTab = tabID.admMisc;
        }

        #region Misc
        protected void DisplayMemStats()
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            {
                string szClass = entry.Value.GetType().ToString();
                d[szClass] = d.TryGetValue(szClass, out int value) ? ++value : 1;
            }
            gvCacheData.DataSource = d;
            gvCacheData.DataBind();
            lblMemStats.Text = String.Format(CultureInfo.CurrentCulture, "Cache has {0:#,##0} items", Cache.Count);
        }

        protected async void btnRefreshInvalidSigs_Click(object sender, EventArgs e)
        {
            await Task.Run(() => { UpdateInvalidSigs(); }).ConfigureAwait(true);
        }

        private const string szVSAutoFixed = "autoFixed";
        private const string szVSFlightsToFix = "InvalidSigs";

        private List<LogbookEntryBase> lstToFix
        {
            get
            {
                if (ViewState[szVSFlightsToFix] == null)
                    ViewState[szVSFlightsToFix] = new List<LogbookEntryBase>();
                return (List<LogbookEntryBase>)ViewState[szVSFlightsToFix];
            }
            set { ViewState[szVSFlightsToFix] = value; }
        }

        private List<LogbookEntryBase> lstAutoFix
        {
            get
            {
                if (ViewState[szVSAutoFixed] == null)
                    ViewState[szVSAutoFixed] = new List<LogbookEntryBase>();
                return (List<LogbookEntryBase>)ViewState[szVSAutoFixed];
            }
            set { ViewState[szVSAutoFixed] = value; }
        }

        protected void UpdateInvalidSigs()
        {
            // Pick up where we left off.
            int offset = Convert.ToInt32(hdnSigOffset.Value, CultureInfo.InvariantCulture);

            int additionalFlights = LogbookEntryBase.AdminGetProblemSignedFlights(offset, lstToFix, lstAutoFix);
            offset += additionalFlights;

            lblSigResults.Text = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", offset, lstToFix.Count, lstAutoFix.Count);

            if (additionalFlights > 0)
            {
                // we have more to go, so show the progress view that auto-clicks for the next chunk.
                mvCheckSigs.SetActiveView(vwSigProgress);
                hdnSigOffset.Value = offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                mvCheckSigs.SetActiveView(vwInvalidSigs);   // stop pressing 
                hdnSigOffset.Value = 0.ToString(CultureInfo.InvariantCulture);  // and reset the offset so you can press it again.
                gvInvalidSignatures.DataSource = lstToFix;
                gvInvalidSignatures.DataBind();
                gvAutoFixed.DataSource = lstAutoFix;
                gvAutoFixed.DataBind();
            }
        }

        protected void gvInvalidSignatures_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int idFlight = e.CommandArgument.ToString().SafeParseInt(LogbookEntryCore.idFlightNone);
            if (idFlight != LogbookEntryCore.idFlightNone)
            {
                LogbookEntryBase le = new LogbookEntry();
                le.FLoadFromDB(idFlight, string.Empty, LogbookEntryCore.LoadTelemetryOption.None, true);
                if (le.AdminSignatureSanityFix(e.CommandName.CompareOrdinalIgnoreCase("ForceValidity") == 0))
                {
                    List<LogbookEntryBase> lst = lstToFix;
                    lst.RemoveAll(l => l.FlightID == idFlight);
                    gvInvalidSignatures.DataSource = lstToFix = lst;
                    gvInvalidSignatures.DataBind();
                }
            }
        }

        protected void btnRefreshProps_Click(object sender, EventArgs e)
        {
            gvEmptyProps.DataSourceID = "sqlDSEmptyProps";
            gvEmptyProps.DataBind();
            gvDupeProps.DataSourceID = "sqlDSDupeProps";
            gvDupeProps.DataBind();
        }

        protected void sql_SelectingLongTimeout(object sender, SqlDataSourceSelectingEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            e.Command.CommandTimeout = 300;
        }

        protected void btnFlushCache_Click(object sender, EventArgs e)
        {
            lblCacheFlushResults.Text = String.Format(CultureInfo.CurrentCulture, "Cache flushed, {0:#,##0} items removed.", FlushCache());
        }

        protected void btnNightlyRun_Click(object sender, EventArgs e)
        {
            string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/TotalsAndcurrencyEmail.aspx"));
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szContent = Encoding.UTF8.GetString(rgdata);
                    if (szContent.Contains("-- SuccessToken --"))
                    {
                        lblNightlyRunResult.Text = "Started";
                        lblNightlyRunResult.CssClass = "success";
                        btnNightlyRun.Enabled = false;
                    }
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                lblNightlyRunResult.Text = ex.Message;
                lblNightlyRunResult.CssClass = "error";
            }
        }
        #endregion
    }
}