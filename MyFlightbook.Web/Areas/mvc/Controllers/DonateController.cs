using MyFlightbook.Payments;
using Stripe;
using Stripe.Checkout;
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
            StripeConfiguration.ApiKey = LocalConfig.SettingForKey(Request.IsLocal ? "StripeTestKey" : "StripeLiveKey");

            if (!int.TryParse(Request["selectedDonation"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int price))
                throw new InvalidOperationException($"Invalid selected donation amount: {Request["selectedDonation"]}");
            string donationLevel = Request["selectedDonationName"];

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions> 
                {
                  new SessionLineItemOptions { PriceData = new SessionLineItemPriceDataOptions { UnitAmount = price, Currency = "usd", ProductData = new SessionLineItemPriceDataProductDataOptions { Name = donationLevel, }, }, Quantity = 1, },
                },
                Metadata = new Dictionary<string, string> { { "username", User.Identity.Name } },
                Mode = "payment", 
                CustomerEmail = MyFlightbook.Profile.GetUser(User.Identity.Name).Email,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string> { { "username", User.Identity.Name } }
                },
                ClientReferenceId = User.Identity.Name,
                SuccessUrl = $"https://{Request.Url.Host}{"~/mvc/Donate".ToAbsolute()}?pp=success",
                CancelUrl = $"https://{Request.Url.Host}{"~/mvc/Donate".ToAbsolute()}?pp=canceled",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
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

            string whsig = Request.Headers["Stripe-Signature"] ?? string.Empty;

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, whsig, LocalConfig.SettingForKey(Request.IsLocal ? "StripeTestWebhook" : "StripeLiveWebhook"));

                // Handle the event
                // If on SDK version < 46, use class Events instead of EventTypes
                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    Payment p = new Payment(stripeEvent.Created, paymentIntent.Metadata["username"], paymentIntent.Amount / 100, paymentIntent.ApplicationFeeAmount ?? 0, Payment.TransactionType.Payment, string.Empty, paymentIntent.Id, json);

                    // Then define and call a method to handle the successful payment intent.
                    // handlePaymentIntentSucceeded(paymentIntent);
                    p.Commit();

                    EarnedGratuity.UpdateEarnedGratuities(p.Username, true);
                    Profile pf = MyFlightbook.Profile.GetUser(p.Username);

                    if (String.IsNullOrEmpty(pf.UserName))
                        throw new InvalidOperationException($"Transaction request for invalid user: {p.Username}\r\n\r\n");

                    // Send a thank-you for payment to the donor
                    if (p.Type == Payment.TransactionType.Payment)
                        util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle), util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DonationThankYou), pf.PreferredGreeting), false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);

                    // And notify any money admins
                    util.NotifyAdminEvent($"{p.Type}: {p.Amount.ToString("C", CultureInfo.CurrentCulture)}!", $"User '{pf.UserName}' ({pf.UserFullName}, {pf.Email}) has donated {p.Amount.ToString("C", CultureInfo.CurrentCulture)}!\r\n\r\nAdditional Data:\r\n\r\n{json}", ProfileRoles.maskCanManageMoney);
                }
                else
                {
                    // Unexpected event type
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }
            }
            catch (StripeException e)
            {
                util.NotifyAdminException($"Unsuccessful Stripe notification: {e.Message}\r\n\r\n{json}\r\n\r\nStripe Sig: {whsig.LimitTo(8)}...{(whsig.Length > 15 ? whsig.Substring(whsig.Length - 5) : string.Empty)}", e);
                /*
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Content(e.Message);
                */
            }
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