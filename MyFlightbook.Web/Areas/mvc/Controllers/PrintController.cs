using MyFlightbook.Printing;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrintController : Controller
    {
        #region Child Views
        /// <summary>
        /// Generates the print options subsection for which sections to include
        /// </summary>
        /// <param name="fq">The active query</param>
        /// <param name="po">Any existing print options; if null, a default set will be used</param>
        /// <param name="paramList">Additional parameters to include in a link (e.g., user name)</param>
        /// <param name="includeFlightsSection">Whether or not to offer flights as a toggle option</param>
        /// <param name="onChange">Name of a javascript function to call on any change.  The function receives one argument, which contains an object containing the selected sections.  If null or empty, then a local link to a printview will be provided and updated.</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult PrintPrefsSections(FlightQuery fq, PrintingOptions po, bool includeFlightsSection, NameValueCollection paramList = null, string onChange = null)
        {
            ViewBag.paramList = paramList ?? HttpUtility.ParseQueryString(string.Empty);
            ViewBag.query = fq;
            ViewBag.includeFlightsSection = includeFlightsSection;
            ViewBag.onChange = onChange;
            ViewBag.po = po ?? new PrintingOptions() { Sections = new PrintingSections() { Endorsements = PrintingSections.EndorsementsLevel.DigitalAndPhotos, IncludeCoverPage = true, IncludeFlights = true, IncludeTotals = true } };
            return PartialView("_printPrefsSections");
        }
        #endregion

        // GET: mvc/Print
        public ActionResult Index()
        {
            return View();
        }
    }
}