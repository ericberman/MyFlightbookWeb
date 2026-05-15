using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

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
            StripeConfiguration.ApiKey = APIKey(testMode);
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            if (String.IsNullOrEmpty(szUser))
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

            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }

        private enum CurrentWebhookKey { Main, Standby, Test }
        private readonly static Dictionary<CurrentWebhookKey, string> _dictWebHookKeys = new Dictionary<CurrentWebhookKey, string>() { { CurrentWebhookKey.Main, "StripeLiveWebhook" }, { CurrentWebhookKey.Standby, "StripeLiveWebhookStandby" }, { CurrentWebhookKey.Test, "StripeTestWebhook" } };

        /// <summary>
        /// The current stripe API version.  When updating this, switch the webhook key as well between MAIN and STANDBY
        /// </summary>
        public const string CURRENT_API_VERSION = "2025-11-17";
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

            // Per https://docs.stripe.com/webhooks/versioning, if this request is for LATER than the current version, just return OK.  Well, 204 to indicate success.
            if (versionParam.CompareCurrentCultureIgnoreCase(CURRENT_API_VERSION) > 0)
                return HttpStatusCode.NoContent;
            // Per https://docs.stripe.com/webhooks/versioning, if this request is for EARLIER than the current version, return 400
            if (versionParam.CompareCurrentCultureIgnoreCase(CURRENT_API_VERSION) < 0)
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

                // Handle the event
                switch (stripeEvent.Type)
                {
                    default:
                        // Unexpected event type
                        Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                        break;
                    case EventTypes.PaymentIntentSucceeded:
                        {
                            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                            Payment p = new Payment(stripeEvent.Created, paymentIntent.Metadata["username"], paymentIntent.Amount / 100, paymentIntent.ApplicationFeeAmount ?? 0, Payment.TransactionType.Payment, string.Empty, paymentIntent.Id, json);

                            // Then define and call a method to handle the successful payment intent.
                            // handlePaymentIntentSucceeded(paymentIntent);
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
                        break;
                    case EventTypes.ChargeUpdated:
                    case EventTypes.ChargeSucceeded:
                        // need to get the fee amount.
                        var charge = stripeEvent.Data.Object as Charge;
                        string szUser = charge.Metadata["username"];
                        string paymentIntentID = charge.PaymentIntentId;
                        if (!String.IsNullOrEmpty(charge.BalanceTransactionId))
                        {
                            // Fix up the fee

                            // Retrieve the Balance Transaction
                            var balanceTransactionService = new BalanceTransactionService();
                            var balanceTransaction = balanceTransactionService.Get(charge.BalanceTransactionId);

                            // Get the fee amount
                            var stripeFee = balanceTransaction.Fee;
                            Console.WriteLine($"Stripe Fee: {stripeFee}");

                            IEnumerable<Payment> payments = Payment.RecordsWithID(paymentIntentID);
                            if (payments.Count() > 1)
                            {
                                util.NotifyAdminException($"{stripeEvent.Type.ToString()} - Multiple records found with id {paymentIntentID}", new InvalidOperationException("Duplicate transaction id"));
                            }
                            else if (!payments.Any())
                            {
                                util.NotifyAdminException($"{stripeEvent.Type.ToString()} - No records found with id {paymentIntentID}; fees may not be recorded", new InvalidOperationException("Duplicate transaction id"));
                            }
                            else
                            {
                                Payment p = payments.First();
                                p.Fee = stripeFee / 100.0M;
                                p.Commit();
                            }
                        }
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
    }

}
