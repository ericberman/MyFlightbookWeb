using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2020-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class FAQController : Controller
    {
        // GET: mvc/FAQ
        public ActionResult Index(int id = 0, string searchText = "")
        {
            ViewBag.selectedIndex = id == 0 ? util.GetIntParam(Request, "q", 0) : id;
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Branding.ReBrand(Resources.LocalizedText.FAQHeader);

            bool fBypassCache = util.GetIntParam(Request, "ut", 0) != 0;

            IEnumerable<FAQGroup> results = string.IsNullOrEmpty(searchText) ?
                (fBypassCache ? FAQGroup.CategorizeFAQItems(FAQItem.AllFAQItems) : FAQGroup.CategorizedFAQs) :
                FAQGroup.CategorizedFAQItemsContainingWords(searchText);
            ViewBag.faqList = results;
            ViewBag.error = results.Any() ? string.Empty : Resources.LocalizedText.FAQSearchNoResults; return View("_faq");
        }
    }
}