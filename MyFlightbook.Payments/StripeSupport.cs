using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

/******************************************************
 * 
 * Copyright (c) 2013-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Payments
{
    /// <summary>
    /// Utilities for managing stripe
    /// </summary>
    public static class StripeUtility
    {
        /// <summary>
        /// Return the API key for either test mode or production mode
        /// </summary>
        /// <param name="testMode">True for test mode</param>
        /// <returns></returns>
        public static string APIKey(bool testMode)
        {
            return LocalConfig.SettingForKey(testMode ? "StripeTestKey" : "StripeLiveKey");
        }

        /// <summary>
        /// Creates and returns a stripe checkout session as a URI (to which the user is redirected)
        /// </summary>
        /// <param name="testMode">True if debugging</param>
        /// <param name="szUser">Requesting user</param>
        /// <param name="skuName">Name of the "SKU" (donation level)</param>
        /// <param name="price">Price IN US CENTS.  I.e., for $50.00, pass 5000</param>
        /// <param name="successLink">Redirect link for success</param>
        /// <param name="cancelLink">Redirect link for cancel</param>
        /// <returns>The uri (as a string) to which the user should be redirected</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string StripeCheckoutSessionLink(bool testMode, string szUser, string skuName, int price, string successLink, string cancelLink)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (String.IsNullOrEmpty(skuName))
                throw new ArgumentNullException(nameof(skuName));
            if (String.IsNullOrEmpty(successLink))
                throw new ArgumentNullException(nameof(successLink));
            if (String.IsNullOrEmpty(cancelLink))
                throw new ArgumentNullException(nameof(cancelLink));
            if (price <= 0)
                throw new ArgumentOutOfRangeException($"Invalid price: {price}");

            IUserProfile pf = util.RequestContext.GetUser(szUser);
            if (String.IsNullOrEmpty(pf?.UserName))
                throw new ArgumentOutOfRangeException($"Invalid User: {szUser}");

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                      PriceData = new SessionLineItemPriceDataOptions
                      {
                          UnitAmount = price,
                          Currency = "usd",
                          ProductData = new SessionLineItemPriceDataProductDataOptions { Name = skuName }
                      },
                      Quantity = 1
                  },
                },
                Metadata = new Dictionary<string, string> { { "username", szUser } },
                Mode = "payment",
                CustomerEmail = pf.Email,
                PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = new Dictionary<string, string> { { "username", szUser } } },
                ClientReferenceId = szUser,
                SuccessUrl = successLink,
                CancelUrl = cancelLink
            };

            var service = new SessionService(ClientFor(testMode));
            Session session = service.Create(options);
            return session.Url;
        }

        /// <summary>
        /// Returns a StripeClient bound to the appropriate (test or live) secret key.  Always use this rather than StripeConfiguration.ApiKey, which is global
        /// and may not have been set (e.g., a webhook that arrives right after an app pool recycle).
        /// </summary>
        private static StripeClient ClientFor(bool testMode)
        {
            return new StripeClient(APIKey(testMode));
        }

        /// <summary>
        /// Returns just the date portion of a stripe API version, e.g., "2025-11-17" for "2025-11-17.clover", so that "?version=2025-11-17" matches the SDK's pinned version.
        /// </summary>
        private static string VersionDate(string version)
        {
            return (version ?? string.Empty).Split('.')[0];
        }

        /// <summary>
        /// Records a successful payment intent, sends the thank-you and admin notifications.  Idempotent: if we already have a record for this payment intent
        /// (because Stripe delivered it twice, or because both a snapshot and a thin destination are active), this does nothing.
        /// </summary>
        private static void RecordPaymentIntent(PaymentIntent paymentIntent, DateTime created, string json, Action<string> onSuccess)
        {
            if (paymentIntent == null)
                throw new ArgumentNullException(nameof(paymentIntent));

            if (Payment.RecordsWithID(paymentIntent.Id).Any())
                return;

            Payment p = new Payment(created, paymentIntent.Metadata["username"], paymentIntent.Amount / 100.0M, (paymentIntent.ApplicationFeeAmount ?? 0) / 100.0M, Payment.TransactionType.Payment, string.Empty, paymentIntent.Id, json);
            p.Commit();

            onSuccess?.Invoke(p.Username);
            IUserProfile pf = util.RequestContext.GetUser(p.Username);

            if (String.IsNullOrEmpty(pf.UserName))
                throw new InvalidOperationException($"Transaction request for invalid user: {p.Username}\r\n\r\n");

            // Send a thank-you for payment to the donor
            if (p.Type == Payment.TransactionType.Payment)
                util.NotifyUser(Branding.ReBrand(Properties.Payments.DonateThankYouTitle), util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Properties.Payments.DonationThankYou), pf.PreferredGreeting), false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);

            // And notify any money admins
            util.NotifyAdminEvent($"{p.Type}: {p.Amount.ToString("C", CultureInfo.CurrentCulture)}!", $"User '{pf.UserName}' ({pf.UserFullName}, {pf.Email}) has donated {p.Amount.ToString("C", CultureInfo.CurrentCulture)}!\r\n\r\nAdditional Data:\r\n\r\n{json}", ProfileRoles.maskCanManageMoney);
        }

        /// <summary>
        /// Fills in the stripe fee for the payment associated with the charge, if the charge has its balance transaction yet.
        /// The balance transaction is often attached asynchronously, which is why charge.updated matters.  Idempotent.
        /// </summary>
        private static void RecordFee(IStripeClient client, Charge charge, string eventType)
        {
            if (charge == null)
                throw new ArgumentNullException(nameof(charge));
            if (String.IsNullOrEmpty(charge.BalanceTransactionId))
                return;

            string paymentIntentID = charge.PaymentIntentId;
            var balanceTransaction = new BalanceTransactionService(client).Get(charge.BalanceTransactionId);
            decimal stripeFee = balanceTransaction.Fee / 100.0M;

            IEnumerable<Payment> payments = Payment.RecordsWithID(paymentIntentID);
            if (payments.Count() > 1)
                util.NotifyAdminException($"{eventType} - Multiple records found with id {paymentIntentID}", new InvalidOperationException("Duplicate transaction id"));
            else if (!payments.Any())
                util.NotifyAdminException($"{eventType} - No records found with id {paymentIntentID}; fees may not be recorded", new InvalidOperationException("Missing transaction id"));
            else
            {
                Payment p = payments.First();
                if (p.Fee != stripeFee)
                {
                    p.Fee = stripeFee;
                    p.Commit();
                }
            }
        }


        private enum CurrentWebhookKey { Main, Standby, Test }
        private readonly static Dictionary<CurrentWebhookKey, string> _dictWebHookKeys = new Dictionary<CurrentWebhookKey, string>() { { CurrentWebhookKey.Main, "StripeLiveWebhook" }, { CurrentWebhookKey.Standby, "StripeLiveWebhookStandby" }, { CurrentWebhookKey.Test, "StripeTestWebhook" } };

        /// <summary>
        /// The current stripe API version.  When updating this, switch the webhook key as well between MAIN and STANDBY
        /// </summary>
        public static readonly string CURRENT_API_VERSION = StripeConfiguration.ApiVersion;
        public const string ORIGINAL_API_VERSION = "2025-03-31";

        /// <summary>
        /// Handles a notification from stripe
        /// </summary>
        /// <param name="testMode">True for local events</param>
        /// <param name="webHookSignature">signature parameter</param>
        /// <param name="versionParam">Additional endpoint parameter that indicates the version being used</param>
        /// <param name="json">JSON of the request</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static HttpStatusCode ProcessStripeNotification(bool testMode, string webHookSignature, string versionParam, string json, Action<string> onSuccess)
        {
            if (String.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            if (String.IsNullOrEmpty(webHookSignature))
                throw new ArgumentNullException(nameof(webHookSignature));

            if (string.IsNullOrEmpty(versionParam))
                return HttpStatusCode.BadRequest;

            // Per https://docs.stripe.com/webhooks/versioning, if this request is for LATER than the current version, just return OK.  Well, 204 to indicate success.
            if (String.CompareOrdinal(VersionDate(versionParam), VersionDate(CURRENT_API_VERSION)) > 0)
                return HttpStatusCode.NoContent;
            // Per https://docs.stripe.com/webhooks/versioning, if this request is for EARLIER than the current version, return 400
            if (String.CompareOrdinal(VersionDate(versionParam), VersionDate(CURRENT_API_VERSION)) < 0)
                return HttpStatusCode.BadRequest;

            try
            {
                Event stripeEvent = null;
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(json, webHookSignature, LocalConfig.SettingForKey(_dictWebHookKeys[testMode ? CurrentWebhookKey.Test : CurrentWebhookKey.Main]));
                }
                catch (StripeException)
                {
                    stripeEvent = EventUtility.ConstructEvent(json, webHookSignature, LocalConfig.SettingForKey(_dictWebHookKeys[testMode ? CurrentWebhookKey.Test : CurrentWebhookKey.Standby]));
                }

                // Pick the API key from the event itself, rather than relying on a global StripeConfiguration.ApiKey that may never have been set in this app domain
                IStripeClient client = ClientFor(!stripeEvent.Livemode);

                // Handle the event
                switch (stripeEvent.Type)
                {
                    default:
                        // Unexpected event type
                        break;
                    case EventTypes.PaymentIntentSucceeded:
                        RecordPaymentIntent(stripeEvent.Data.Object as PaymentIntent, stripeEvent.Created, json, onSuccess);
                        break;
                    case EventTypes.ChargeUpdated:
                    case EventTypes.ChargeSucceeded:
                        RecordFee(client, stripeEvent.Data.Object as Charge, stripeEvent.Type);
                        break;
                }
            }
            catch (StripeException e)
            {
                // notify admin, but don't throw the exception.  Otherwise, stripe will keep trying!
                util.NotifyAdminException($"Unsuccessful Stripe notification: {e.Message}\r\n\r\n{json}\r\n\r\nStripe Sig: {webHookSignature.LimitTo(8)}...{(webHookSignature.Length > 15 ? webHookSignature.Substring(webHookSignature.Length - 5) : string.Empty)}", e);
            }

            return HttpStatusCode.OK;
        }

        #region Thin events
        /// <summary>
        /// Handles a THIN event notification from stripe (event destination with "Use thin events" enabled).
        /// Thin notifications are unversioned: they carry only the event type and the ID of the related object, and we fetch the object
        /// using whatever API version the SDK is pinned to.  So upgrading Stripe.net requires NO webhook changes - just update the package and deploy.
        /// We deliberately verify the signature and read the (tiny, stable) JSON ourselves rather than use the SDK's typed event-notification classes,
        /// so that this code doesn't care which SDK version is installed.
        /// Subscribed events: v1.payment_intent.succeeded, v1.charge.succeeded, v1.charge.updated
        /// </summary>
        /// <param name="testMode">True for local (sandbox) events; selects the signing secret</param>
        /// <param name="webHookSignature">The Stripe-Signature header</param>
        /// <param name="json">Raw body of the request</param>
        /// <param name="onSuccess">Action to call with the username on a successful payment</param>
        /// <returns>200 on success, 400 for a bad signature/payload, 500 (so that stripe retries) if we couldn't fetch the related object</returns>
        public static HttpStatusCode ProcessStripeThinNotification(bool testMode, string webHookSignature, string json, Action<string> onSuccess)
        {
            if (String.IsNullOrEmpty(json) || String.IsNullOrEmpty(webHookSignature))
                return HttpStatusCode.BadRequest;

            // In test mode, `stripe listen` uses the same signing secret for thin and snapshot forwarding, so reuse StripeTestWebhook.
            // In production, the thin destination has its own secret.
            string signingSecret = LocalConfig.SettingForKey(testMode ? "StripeTestWebhook" : "StripeLiveThinWebhook");
            if (String.IsNullOrEmpty(signingSecret))
            {
                util.NotifyAdminException("Stripe thin notification received but no signing secret is configured", new InvalidOperationException(testMode ? "StripeTestWebhook is empty" : "StripeLiveThinWebhook is empty"));
                return HttpStatusCode.InternalServerError;   // let stripe retry once it's configured
            }

            try
            {
                EventUtility.ValidateSignature(json, webHookSignature, signingSecret);
            }
            catch (StripeException)
            {
                return HttpStatusCode.BadRequest;
            }

            JObject notification;
            try
            {
                notification = JObject.Parse(json);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return HttpStatusCode.BadRequest;
            }

            string eventType = (string)notification["type"] ?? string.Empty;
            string objectID = (string)notification["related_object"]?["id"];
            bool livemode = (bool?)notification["livemode"] ?? !testMode;
            DateTime created = notification["created"]?.Value<DateTime>() ?? DateTime.UtcNow;
            if (created.Kind != DateTimeKind.Utc)
                created = created.ToUniversalTime();

            IStripeClient client = ClientFor(!livemode);

            try
            {
                switch (eventType)
                {
                    case "v1.payment_intent.succeeded":
                        {
                            PaymentIntent pi = new PaymentIntentService(client).Get(objectID);
                            RecordPaymentIntent(pi, created, pi.ToJson(), onSuccess);
                        }
                        break;
                    case "v1.charge.succeeded":
                    case "v1.charge.updated":
                        RecordFee(client, new ChargeService(client).Get(objectID), eventType);
                        break;
                    default:
                        // Includes v2.core.event_destination.ping; nothing to do.
                        break;
                }
            }
            catch (StripeException e)
            {
                // Fetching the related object failed - likely transient, so let stripe retry.  Processing is idempotent, so retries are safe.
                util.NotifyAdminException($"Unsuccessful Stripe thin notification ({eventType}, {objectID}): {e.Message}\r\n\r\n{json}", e);
                return HttpStatusCode.InternalServerError;
            }

            return HttpStatusCode.OK;
        }
        #endregion
    }

}
