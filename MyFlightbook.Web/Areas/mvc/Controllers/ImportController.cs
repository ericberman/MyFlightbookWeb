using MyFlightbook.Currency;
using MyFlightbook.StartingFlights;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class ImportController : AdminControllerBase
    {
        #region Web services
        #region Starting Totals
        [Authorize]
        [HttpPost]
        public ActionResult StartingTotalsFormForMode(RepresentativeAircraft.RepresentativeTypeMode mode)
        {
            return SafeOp(() =>
            {
                ViewBag.startingFlights = StartingFlight.StartingFlightsForUser(User.Identity.Name, mode);
                ViewBag.mode = mode;
                return PartialView("_startingTotalsForm");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewStartingTotals(string startingTotalRows, RepresentativeAircraft.RepresentativeTypeMode mode, string startingTotalDate)
        {
            return SafeOp(() =>
            {
                IEnumerable<StartingFlight> flights = StartingFlight.DeserializeStartingFlights(startingTotalRows, User.Identity.Name, mode, DateTime.TryParse(startingTotalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.Now.AddYears(-1));
                IEnumerable<TotalsItem>[] rgti = StartingFlight.BeforeAndAfterTotalsForUser(flights, User.Identity.Name);
                ViewBag.beforeTotals = rgti[0];
                ViewBag.afterTotals = rgti[1];

                // add the flights
                return PartialView("_startingTotalsPreview");
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitStartingTotals(string startingTotalRows, RepresentativeAircraft.RepresentativeTypeMode mode, string startingTotalDate)
        {
            return SafeOp(() =>
            {
                StartingFlight.CommitStartingFlights(StartingFlight.DeserializeStartingFlights(startingTotalRows, User.Identity.Name, mode, DateTime.TryParse(startingTotalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.Now.AddYears(-1)));
                return new EmptyResult();
            });
        }
        #endregion
        #endregion

        #region Child Views
        #region Starting Totals
        #endregion
        #endregion

        [Authorize]
        public ActionResult StartingTotals()
        {
            return View("startingTotals");
        }

        [Authorize]
        // GET: mvc/Import
        public ActionResult Index()
        {
            throw new NotImplementedException();
        }
    }
}