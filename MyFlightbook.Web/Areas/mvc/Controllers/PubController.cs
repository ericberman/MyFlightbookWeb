using MyFlightbook.Telemetry;
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
        public ActionResult PropertyTable()
        {
            ViewBag.props = CustomPropertyType.GetCustomPropertyTypes();
            return View("propertyTable");
        }

        public ActionResult MobileApps(string selectedOS = "")
        {
            ViewBag.SelectedOS = selectedOS;
            return View("mobileApps");
        }

        public ActionResult TandC()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.TermsAndConditionsHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.TermsAndConditions);
            return View("_localizedContent");
        }

        public ActionResult Privacy()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.Privacy);
            return View("_localizedContent");
        }

        public ActionResult FeatureChart()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.FeaturesHeader;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.FeatureTable);
            return View("_localizedContent");
        }

        public ActionResult CFISigs()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.CFISigsTitle;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.CFISigs);
            return View("_localizedContent");
        }

        public ActionResult ClubManual()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.LocalizedText.ClubsManualTitle;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LocalizedText.ClubsManual);
            return View("_localizedContent");
        }

        public ActionResult CurrencyNotes()
        {
            ViewBag.defaultTab = tabID.tabUnknown;
            ViewBag.Title = Resources.Currency.CurrencyImportantNotes;
            ViewBag.HTMLContent = Branding.ReBrand(Resources.Currency.CurrencyDisclaimer);
            return View("_localizedContent");
        }

        public ActionResult ImportTable()
        {
            ViewBag.defaultTab = tabID.tabLogbook;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.importTableHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.LogbookEntry.ImportTableDescription);
            ViewBag.PropTypeList = CustomPropertyType.GetCustomPropertyTypes();
            return View("ImportTable");
        }

        public ActionResult FlightDataKey()
        {
            ViewBag.defaultTab = tabID.tabLogbook;
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.FlightData.FlightDataHeader, Branding.CurrentBrand.AppName);
            ViewBag.HTMLContent = Branding.ReBrand(Resources.FlightData.FlightDataKey);
            ViewBag.Columns = KnownColumn.GetKnownColumns();
            return View("flightdatakey");
        }

        public ActionResult About()
        {
            ViewBag.defaultTab = tabID.tabHome;
            ViewBag.Title = Branding.ReBrand(Resources.LocalizedText.AboutTitle);
            return View("about");
        }

        // GET: mvc/Pub - shouldn't ever call.
        public ActionResult Index()
        {
            Response.Redirect("~");
            Response.End();
            return null;
        }
    }
}