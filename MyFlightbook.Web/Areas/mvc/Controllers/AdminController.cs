using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminController : AdminControllerBase
    {
        #region Admin Web Services
        [HttpPost]
        [Authorize]
        public ActionResult InvalidProps()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () =>
            {
                ViewBag.emptyProps = CustomFlightProperty.ADMINEmptyProps();
                return PartialView("_miscInvalidProps");
            });
        }

        [HttpPost]
        [Authorize]
        public string DeleteEmptyProp(int propid)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CustomFlightProperty cfp = new CustomFlightProperty() { PropID = propid };
                cfp.DeleteProperty();
                return string.Empty;
            });
        }

        private const string szSessKeyInvalidSigProgress = "sessSignedflightsAutoFixed";

        [HttpPost]
        [Authorize]
        public ActionResult UpdateInvalidSigs()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () => {
                if (Session[szSessKeyInvalidSigProgress] == null)
                    Session[szSessKeyInvalidSigProgress] = new { offset = 0, lstToFix = new List<LogbookEntryBase>(), lstAutoFix = new List<LogbookEntryBase>(), progress = string.Empty, additionalFlights = 0 };

                dynamic state = Session[szSessKeyInvalidSigProgress];

                int cFlights = LogbookEntryBase.AdminGetProblemSignedFlights(state.offset, state.lstToFix, state.lstAutoFix);
                dynamic newState = new
                {
                    additionalFlights = cFlights,
                    offset = state.offset + cFlights,
                    lstToFix = state.lstToFix,
                    lstAutoFix = state.lstAutoFix,
                    progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count)
                };
                Session[szSessKeyInvalidSigProgress] = newState;
                return (ActionResult) Json(newState);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult InvalidSigsResult()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () => {
                dynamic state = Session[szSessKeyInvalidSigProgress];
                Session[szSessKeyInvalidSigProgress] = null;
                ViewBag.lstToFix = state.lstToFix;
                ViewBag.lstAutoFix = state.lstAutoFix;
                ViewBag.progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count);
                return PartialView("_invalidSigs");
            });
        }

        [HttpPost]
        [Authorize]
        public string KickOffNightlyRun()
        {
            return SafeOp(ProfileRoles.maskSiteAdminOnly, () =>
            {
                string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/TotalsAndcurrencyEmail.aspx"));
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szContent = Encoding.UTF8.GetString(rgdata);
                    if (!szContent.Contains("-- SuccessToken --"))
                        throw new InvalidOperationException();
                    return string.Empty;
                }
            });
        }

        [HttpPost]
        [Authorize]
        public string FlushCache()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return String.Format(CultureInfo.CurrentCulture, "Cache flushed, {0:#,##0} items removed.", util.FlushCache());
            });
        }
        #endregion

        [Authorize]
        public ActionResult Misc()
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ability to do any admin

            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            {
                string szClass = entry.Value.GetType().ToString();
                d[szClass] = d.TryGetValue(szClass, out int value) ? ++value : 1;
            }

            ViewBag.cacheSummary = d;
            ViewBag.memStats = String.Format(CultureInfo.CurrentCulture, "Cache has {0:#,##0} items", HttpRuntime.Cache.Count);
            return View("adminMisc");
        }

        [Authorize]
        // GET: mvc/Admin
        public ActionResult Index()
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ANY admin, just to generate the exception, but otherwise nothing to do here.
            return View();
        }
    }
}