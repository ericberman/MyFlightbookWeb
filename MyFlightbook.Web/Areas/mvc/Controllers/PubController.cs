using System;
using System.Globalization;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Controller for simple public pages that are nothing but HTML content; renders the localized html content into a layout page.  Examples include Privacy or Terms and Conditions
    /// </summary>
    public class PubController : Controller
    {
        public ActionResult TandC()
        {
            ViewBag.Title = Resources.LocalizedText.TermsAndConditionsHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.TermsAndConditions);
            return View("_localizedContent");
        }

        public ActionResult Privacy()
        {
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.Privacy);
            return View("_localizedContent");
        }

        public ActionResult FeatureChart()
        {
            ViewBag.Title = Resources.LocalizedText.FeaturesHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.FeatureTable);
            return View("_localizedContent");
        }

        // GET: mvc/Pub - shouldn't ever call.
        public ActionResult Index()
        {
            return View();
        }
    }
}