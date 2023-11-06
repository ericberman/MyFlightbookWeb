using MyFlightbook.Printing;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /******************************************************
     * 
     * Copyright (c) 2007-2023 MyFlightbook LLC
     * Contact myflightbook-at-gmail.com for more information
     *
    *******************************************************/

    /// <summary>
    /// Controller for pages that are only used internally.  I.e., a user would never (deliberately) hit it, but they
    /// may bounce against it (e.g., ad tracking).
    /// </summary>
    public class InternalController : Controller
    {
        public ActionResult PrintFooter(string id, int page, int topage)
        {
            bool fHasCover = PDFOptions.CoverFromEncodedOptions(id);
            bool fHasTotal = PDFOptions.TotalPagesFromEncodedOptions(id);

            // If we have a cover page, start numbering on the page AFTER the cover.
            if (fHasCover)
            {
                page--;
                topage--;
            }

            ViewBag.page = page;
            ViewBag.topage = topage;
            ViewBag.modifiedFooter = PDFOptions.ShowChangeTrack(id) ? Resources.LogbookEntry.FlightModifiedFooter : string.Empty;
            ViewBag.pageNumber = fHasTotal ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCountWithTotals, page, topage) : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, page);
            return PartialView("_printFooter");
        }

        // GET: mvc/Internal
        public ActionResult Index()
        {
            return View();
        }
    }
}