using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2020-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class DonateController : Controller
    {
        // GET: mvc/Donate
        public ActionResult Index()
        {

            List<Gratuity> lstKnownGratuities = new List<Gratuity>(Gratuity.KnownGratuities);
            lstKnownGratuities.Sort((g1, g2) => { return g1.Threshold.CompareTo(g2.Threshold); });

            ViewBag.Gratuities = lstKnownGratuities;
            ViewBag.Cancelled = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("canceled") == 0;
            ViewBag.Success  = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("success") == 0;
            ViewBag.PaymentHistory = Payment.RecordsForUser(User.Identity.Name);
            ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileHeader, HttpUtility.HtmlEncode(MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName));

            List<EarnedGratuity> lst = EarnedGratuity.GratuitiesForUser(User.Identity.Name, Gratuity.GratuityTypes.Unknown);
            lst.RemoveAll(eg => eg.CurrentStatus == EarnedGratuity.EarnedGratuityStatus.Expired);
            ViewBag.GratuityHistory = lst;
            ViewBag.ShowGratuityHistory = lst.Count > 0;

            return View("donate");
        }
    }
}