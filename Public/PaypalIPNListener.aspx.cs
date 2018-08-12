using MyFlightbook;
using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2013-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_PaypalIPNListener : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        byte[] param = Request.BinaryRead(Request.ContentLength);
        string strRequest = Encoding.ASCII.GetString(param);

        bool fSandbox = util.GetStringParam(Request, "sandbox").Length > 0;
        string strResponse = PayPalIPN.VerifyResponse(fSandbox, strRequest);

        string szUser = util.GetStringParam(Request, "custom");
        string szProductID = util.GetStringParam(Request, "os1");
        string szAmount = util.GetStringParam(Request, "mc_gross");
        string szTransactionID = util.GetStringParam(Request, "txn_id");
        string szTransactionType = util.GetStringParam(Request, "txn_type");
        string szCurrency = util.GetStringParam(Request, "mc_currency");
        string szReasonCode = util.GetStringParam(Request, "reason_code");
        string szParentTxnID = util.GetStringParam(Request, "parent_txn_id");
        string szFee = util.GetStringParam(Request, "mc_fee");

        if (strResponse == "VERIFIED")
        {
            //check the payment_status is Completed
            //check that txn_id has not been previously processed
            //check that receiver_email is your Primary PayPal email
            //check that payment_amount/payment_currency are correct
            //process payment

            StringBuilder sbErr = new StringBuilder();
            decimal d;
            if (!Decimal.TryParse(szAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "Invalid payment amount: {0}\r\n\r\n", szAmount);
                d = 0.0M;
            }

            if (d == 0.0M)
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "Payment amount of 0.0!\r\n\r\n");
            Payment.TransactionType transType = (d > 0) ? Payment.TransactionType.Payment : Payment.TransactionType.Refund;

            decimal fee = 0.0M;
            if (!Decimal.TryParse(szFee, NumberStyles.Any, CultureInfo.InvariantCulture, out fee))
                fee = 0.0M;

            if (String.IsNullOrEmpty(szTransactionID))
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "No transaction ID is specified!\r\n\r\n");

            IEnumerable<Payment> lst = Payment.RecordsWithID(szTransactionID);
            // Note: it's possible via e-check to get two notifications, one that is pending and one that is completed.
            // Pending shows as payment_status: Pending, completed shows as payment_status=Completed
            int cPayments = lst.Count();
            if (cPayments > 0)
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "Duplicate transaction ID: {0}\r\n\r\n", szTransactionID);

            switch (transType)
            {
                case Payment.TransactionType.Payment:
                    if (String.Compare(szTransactionType, "web_accept", StringComparison.OrdinalIgnoreCase) != 0)
                        sbErr.AppendFormat(CultureInfo.CurrentCulture, "Unknown transaction type: {0}\r\n\r\n", szTransactionType);
                    break;
                case Payment.TransactionType.Refund:
                    {
                        if (String.IsNullOrEmpty(szReasonCode))
                            sbErr.AppendFormat(CultureInfo.CurrentCulture, "Refund with no reason code?\r\n\r\n");
                        if (String.IsNullOrEmpty(szParentTxnID))
                            sbErr.AppendFormat(CultureInfo.CurrentCulture, "Refund with no parent transaction\r\n\r\n");
                        lst = Payment.RecordsWithID(szParentTxnID);
                        if (cPayments > 1)
                            sbErr.AppendFormat(CultureInfo.CurrentCulture, "Multiple records found for parent transaction of refund\r\n\r\n");
                        else if (cPayments == 0)
                            sbErr.AppendFormat(CultureInfo.CurrentCulture, "No parent record found for parent transaction of refund\r\n\r\n");
                        szUser = lst.ElementAt(0).Username;
                    }
                    break;
                default:
                    break;
            }

            // Check for a valid user
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            if (String.IsNullOrEmpty(pf.UserName))
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "Transaction request for invalid user: {0}\r\n\r\n", szUser);

            if (String.Compare(szCurrency, "USD", StringComparison.OrdinalIgnoreCase) != 0)
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "Invalid currency: {0}\r\n\r\n", szCurrency);

            if (sbErr.Length > 0)
            {
                sbErr.AppendFormat(CultureInfo.CurrentCulture, "\r\n\r\nData:{0}", strRequest);
                util.NotifyAdminEvent("Paypal Payment failed", sbErr.ToString(), ProfileRoles.maskSiteAdminOnly);
            }
            else
            {
                try
                {
                    Payment p = new Payment(DateTime.Now, szUser, d, fee, transType, string.Empty, szTransactionID, strRequest);
                    if (fSandbox)
                    {
                        util.NotifyAdminEvent("Sandbox Trnasaction", "payment = " + Newtonsoft.Json.JsonConvert.SerializeObject(p), ProfileRoles.maskSiteAdminOnly);
                        return;
                    }
                    p.Commit();

                    EarnedGrauity.UpdateEarnedGratuities(p.Username, true);
                }
                catch (InvalidOperationException ex)
                {
                    util.NotifyAdminEvent("Paypal payment failed", String.Format(CultureInfo.InvariantCulture, "User: {0}, Amount: {1:C} TransactionID: {2} \r\n\r\nRaw data:\r\n\r\n {3}\r\n\r\nException:{4}\r\n{5}", szUser, d, szTransactionID, strRequest, ex.Message, ex.StackTrace), ProfileRoles.maskSiteAdminOnly);
                }
                finally
                {
                    util.NotifyAdminEvent(String.Format(CultureInfo.CurrentCulture, "{1}: {0:C}!", d, transType.ToString()), String.Format(CultureInfo.CurrentCulture, "User '{0}' ({1}, {2}) has donated {3:C}!\r\n\r\nAdditional Data:\r\n\r\n{4}", pf.UserName, pf.UserFullName, pf.Email, d, strRequest), ProfileRoles.maskCanManageMoney);

                    // Send a thank-you for payment to the donor
                    if (transType == Payment.TransactionType.Payment)
                        util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle), String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DonationThankYou), pf.UserFullName), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
                }
            }
        }
        else if (strResponse == "INVALID")
        {
            //log for manual investigation
            util.NotifyAdminEvent("Paypal Event INVALID", String.Format(CultureInfo.InvariantCulture, "Paypal event was Invalid: User={1}, productID={2}, amount=${3:#,#.00}\r\n\r\nstrRequest=\"{0}\"", strRequest, szUser, szProductID, szAmount), ProfileRoles.maskSiteAdminOnly);
        }
        else
        {
            //log response/ipn data for manual investigation
            util.NotifyAdminEvent("Paypal Event UNKNOWN", String.Format(CultureInfo.InvariantCulture, "Paypal event was UNKNOWN:\r\n\r\nstrRequest=\"{0}\"", strRequest), ProfileRoles.maskSiteAdminOnly);
        }
    }
}