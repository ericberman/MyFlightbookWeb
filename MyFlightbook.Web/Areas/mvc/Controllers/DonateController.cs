using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2020-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class DonateController : AdminControllerBase
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
        /// Endpoint for THIN stripe events (an event destination with "Use thin events" on), subscribed to v1.payment_intent.succeeded, v1.charge.succeeded, v1.charge.updated.
        /// Thin notifications are unversioned, so once this is the only active destination, upgrading Stripe.net is just: update the NuGet package, fix any compile/breaking changes, deploy.
        /// No standby webhook, no ?version= parameter, no two-step.
        /// Signing secrets: production uses StripeLiveThinWebhook (from the live thin destination); test mode reuses StripeTestWebhook (stripe listen uses one secret for both).
        /// Local testing: `stripe listen --thin-events "v1.payment_intent.succeeded,v1.charge.succeeded,v1.charge.updated" --forward-thin-to https://developer.myflightbook.com/logbook/mvc/donate/StripeThinNotify`
        ///   (same whsec_ as for snapshot forwarding).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> StripeThinNotify()
        {
            Request.InputStream.Seek(0, SeekOrigin.Begin);
            string json = string.Empty;
            using (StreamReader sr = new StreamReader(Request.InputStream))
            {
                json = await sr.ReadToEndAsync();
            }

            return new HttpStatusCodeResult(StripeUtility.ProcessStripeThinNotification(Request.IsLocal, Request.Headers["Stripe-Signature"] ?? string.Empty, json, (user) => { EarnedGratuity.UpdateEarnedGratuities(user, true); }));
        }
        #endregion

        #region Paypal donations
        [HttpPost]
        public ActionResult PaypalNotify()
        {
            PayPalIPN.ProcessIPNNotification(Request.Params, Encoding.ASCII.GetString(Request.BinaryRead(Request.ContentLength)), (user) => {
                EarnedGratuity.UpdateEarnedGratuities(user, true);
            });
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
            ViewBag.Cancelled = GetStringParam("pp").CompareCurrentCultureIgnoreCase("canceled") == 0;
            ViewBag.Success = GetStringParam("pp").CompareCurrentCultureIgnoreCase("success") == 0;
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