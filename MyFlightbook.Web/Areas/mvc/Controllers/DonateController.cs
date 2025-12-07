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
        /// https://docs.stripe.com/webhooks/versioning has a great guide on how to do version upgrades.
        /// There are 3 webhooks in LocalConfig that reference three webhooks on the live site:
        /// Webhook configuration strategy:
        ///   a) StripeLiveWebhook - Production webhook for the *CURRENT* API version (enabled).
        ///   b) StripeLiveWebhookStandby - Production webhook for the *NEXT* API version (created but disabled until migration).
        ///   c) StripeTestWebhook - Development webhook for the *NEXT* API version (points to developer.myflightbook.com / localhost).
        ///      This is disabled by default; used only for local testing with `stripe listen`.
        ///
        /// Notes:
        ///   - `stripe login -i` let's you specify a specific key
        ///   - `stripe listen` always creates an ephemeral webhook that inherits the account’s default API version
        ///   - `stripe listen` can use another test API key by using --api-key followed by the key
        ///   - `stripe listen --latest` will use the latest version of the API, to verify that things work with that too.
        ///   
        /// So when there is an upgrade to do, I think the way to test it is, per the versioning link above:
        ///  - Live site: set up the standby webhook with the new API version, and disable it.  Put it into standby or live in the localconfig database
        ///  - Set up the sandbox key, if needed
        ///  - Open *2* DOS boxes.  One will run the new webhook, one will run the old webhook.  Do stripe login -i with the sandbox key in each
        ///  - In the 1st DOS box, run `stripe listen --forward-to https://developer.myflightbook.com/logbook/mvc/donate/StripeNotify?version=xxxxold`.  This emulates the CURRENT webhook
        ///  
        ///  - Step 1: verify that things work in the "before" state: running old code, you only have the one current webhook.  Verify that it works.
        ///  - Step 2: 
        ///      - in the 2nd DOS box, run `stripe listen --latest --forward-to https://developer.myflightbook.com/logbook/mvc/donate/StripeNotify?version=yyynew`.  This emulates the NEW (currently disabled live) webhook
        ///      - Do a test transaction:
        ///        *** Verify that the version=xxxold processes the notificationn
        ///        *** Verify that the version=yyynew returns 200 (204).
        ///   - Step 3: Update the code to the new version AND update the CURRENT_API_VERSION
        ///      - Do a test transaction: 
        ///        *** Verify that the version=xxxold return 400
        ///        *** Verify that the version yyynew processes the notification
        ///   - Step 4: deploy
        ///   - Step 5: enable the new webhook, disable/delete the old webhook.
        ///   - Step 6: copy the webhook key for the new webhook into main/standby as needed.
        ///   - Step 7: update the default version under Developer->Workbench->OverView->API Versions.
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

            return new HttpStatusCodeResult(StripeUtility.ProcessStripeNotification(Request.IsLocal, Request.Headers["Stripe-Signature"] ?? string.Empty, Request["version"] ?? StripeUtility.ORIGINAL_API_VERSION, json));
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