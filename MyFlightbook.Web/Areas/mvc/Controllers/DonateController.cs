using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2020-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class DonateController : Controller
    {
        #region Stripe donations
        /// <summary>
        /// Endpoint for a form to make a donation
        /// </summary>
        /// <returns>Redirects to the stripe website with the requested donation</returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        [ActionName("create-checkout-session")]
        public ActionResult CreateCheckoutSession()
        {
            if (!int.TryParse(Request["selectedDonation"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int price))
                throw new InvalidOperationException($"Invalid selected donation amount: {Request["selectedDonation"]}");

            Response.Headers.Add("Location", StripeUtility.StripeCheckoutSessionLink(
                Request.IsLocal,
                User.Identity.Name, Request["selectedDonationName"], price, 
                $"https://{Request.Url.Host}{"~/mvc/Donate".ToAbsolute()}?pp=success",
                $"https://{Request.Url.Host}{"~/mvc/Donate".ToAbsolute()}?pp=canceled"));
            Response.StatusCode = (int)HttpStatusCode.RedirectMethod;
            return new EmptyResult();
        }

        /// <summary>
        /// Endpoint that is called by stripe as the payment progresses.
        /// If developing on, say, developer.myflightbook.com, you can open the CLI and do "stripe login", and then do "stripe listen --forward-to https://developer.myflightbook.com/logbook/mvc/donate/StripeNotify" (or http://localhost or whatever) to redirect these.
        /// </summary>
        /// <returns>Emptyresult</returns>
        /// <exception cref="InvalidOperationException"></exception>
        [HttpPost]
        public async Task<ActionResult> StripeNotify()
        {
            Request.InputStream.Seek(0, SeekOrigin.Begin);
            string json = string.Empty;
            using (StreamReader sr = new StreamReader(Request.InputStream))
            {
                json = await sr.ReadToEndAsync();
            }

            StripeUtility.ProcessStripeNotification(Request.IsLocal, Request.Headers["Stripe-Signature"] ?? string.Empty, json);

            return new EmptyResult();
        }
        #endregion

        // GET: mvc/Donate
        [Authorize]
        public ActionResult Index()
        {
            List<Gratuity> lstKnownGratuities = new List<Gratuity>(Gratuity.KnownGratuities);
            lstKnownGratuities.Sort((g1, g2) => { return g1.Threshold.CompareTo(g2.Threshold); });

            ViewBag.Gratuities = lstKnownGratuities;
            ViewBag.Cancelled = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("canceled") == 0;
            ViewBag.Success = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("success") == 0;
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