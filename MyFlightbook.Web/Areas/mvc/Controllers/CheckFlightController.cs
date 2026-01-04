using MyFlightbook.Lint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class CheckFlightController : AdminControllerBase
    {
        protected const string szCookieLastCheck = "cookieLastCheck";

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleIgnore()
        {
            return SafeOp(() =>
            {
                int idFlight = Convert.ToInt32(Request["idFlight"], CultureInfo.InvariantCulture);
                bool fIgnore = Convert.ToBoolean(Request["fIgnore"] ?? "false", CultureInfo.InvariantCulture);

                LogbookEntryBase le = new LogbookEntry();
                if (!le.FLoadFromDB(idFlight, User.Identity.Name))
                    throw new UnauthorizedAccessException();

                if (le.IsNewFlight) // should never happen.
                    throw new InvalidOperationException("Flight doesn't exist");

                FlightLint.SetIgnoreFlagForFlight(le, fIgnore);
                le.CommitRoute();  // Save the change, but hold on to the flight in the list for now so that you can uncheck it.
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult CheckFlights(DateTime? dtSince, uint options)
        {
            return SafeOp(() =>
            {
                if ((options & (~(uint) LintOptions.IncludeIgnored)) == 0)
                    throw new InvalidOperationException(Resources.FlightLint.errNoOptionsSelected);

                FlightQuery fq = new FlightQuery(User.Identity.Name);
                if (dtSince.HasValue)
                {
                    fq.DateRange = FlightQuery.DateRanges.Custom;
                    fq.DateMin = dtSince.Value;
                }
                FlightResult fr = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(fq);
                fr.SortFlights("Date", SortDirection.Ascending);
                ViewBag.flightsChecked = fr.Flights.Count();
                ViewBag.checkedFlights = new FlightLint().CheckFlights(fr.Flights, User.Identity.Name, options, dtSince);
                Response.Cookies[szCookieLastCheck].Value = DateTime.Now.YMDString();
                Response.Cookies[szCookieLastCheck].Expires = DateTime.Now.AddYears(5);
                return PartialView("_checkFlightTable");
            });
        }

        // GET: mvc/CheckFlight
        [Authorize]
        public ActionResult Index()
        {
            string szLastCheck = Request.Cookies[szCookieLastCheck]?.Value ?? String.Empty;
            if (DateTime.TryParse(szLastCheck, out DateTime dtLastCheck))
                ViewBag.lastCheck = dtLastCheck;
            return View("checkFlights");
        }
    }
}