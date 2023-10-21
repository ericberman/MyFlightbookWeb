using MyFlightbook.Charting;
using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminPaymentController : Controller
    {
        private static HashSet<Payment.TransactionType> XactionTypes(bool fPayments, bool fRefunds, bool fAdjustments, bool fTestTransactions)
        {
            HashSet<Payment.TransactionType> xactionTypes = new HashSet<Payment.TransactionType>();
            xactionTypes.Clear();
            if (fPayments)
                xactionTypes.Add(Payment.TransactionType.Payment);
            if (fRefunds)
                xactionTypes.Add(Payment.TransactionType.Refund);
            if (fAdjustments)
                xactionTypes.Add(Payment.TransactionType.Adjustment);
            if (fTestTransactions)
                xactionTypes.Add(Payment.TransactionType.TestTransaction);

            return xactionTypes;
        }

        /// <summary>
        /// Internal child view 
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult PaymentDetailRows(string szUser, HashSet<Payment.TransactionType> xactions, int offset, int limit)
        {
            ViewBag.PaymentList = Payment.RecordsForUser(szUser, xactions, offset, limit);
            return PartialView("_pmtDetailRows");
        }

        /// <summary>
        /// Support for web service (infinite scroll)
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="fPayments"></param>
        /// <param name="fRefunds"></param>
        /// <param name="fAdjustments"></param>
        /// <param name="fTestTransactions"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [Authorize]
        [HttpPost]
        public ActionResult PaymentSet(string szUser, bool fPayments, bool fRefunds, bool fAdjustments, bool fTestTransactions, int offset, int limit)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageMoney)
                throw new UnauthorizedAccessException();

            return PaymentDetailRows(szUser, XactionTypes(fPayments, fRefunds, fAdjustments, fTestTransactions), offset, limit);
        }

        [Authorize]
        [HttpPost]
        public void ResetGratuities(string szUser, bool fResetReminders)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageMoney)
                throw new UnauthorizedAccessException();

            EarnedGratuity.UpdateEarnedGratuities(szUser, fResetReminders);
        }

        [Authorize]
        [HttpPost]
        public void FixFees()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageMoney)
                throw new UnauthorizedAccessException();
            Payment.ADMINFixFees();
        }

        private ActionResult CompositeView(string szUser = null, bool fPayments = false, bool fRefunds = false, bool fAdjustments = false, bool fTestTransactions = false)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageMoney)
                throw new UnauthorizedAccessException();

            ViewBag.UserRestriction = szUser ?? string.Empty;
            ViewBag.TypeRestriction = XactionTypes(fPayments, fRefunds, fAdjustments, fTestTransactions);

            ViewBag.PaymentsByYearMonth = YearlyPayments.PaymentsByYearAndMonth();

            GoogleChartData gcd = new GoogleChartData() { ChartType = GoogleChartType.LineChart, Title = "Running 30 donations", YDataType = GoogleColumnDataType.number, Width = 800, YLabel = "Donations", XDataType = GoogleColumnDataType.date, XLabel = "Date", ContainerID = "runningTotals", Height = 300 };
            IDictionary<DateTime, decimal> d = Payment.RunningPaymentsForTrailingPeriod();
            foreach (DateTime dtKey in d.Keys)
            {
                gcd.XVals.Add(dtKey);
                gcd.YVals.Add(d[dtKey]);
            }
            ViewBag.ChartData = gcd;
            return View("adminPayment");
        }

        // GET: mvc/AdminPayment
        [Authorize]
        public ActionResult Index(string szUser = null, bool fPayments = false, bool fRefunds = false, bool fAdjustments = false, bool fTestTransactions = false)
        {
            return CompositeView(szUser, fPayments, fRefunds, fAdjustments, fTestTransactions);
        }

        // POST: mvc/AdminPayment/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageMoney)
                throw new UnauthorizedAccessException();

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            try
            {
                string szUser = collection["txtTestTransactionUsername"];
                if (string.IsNullOrEmpty(szUser))
                    throw new InvalidOperationException("Username is required");
                if (MyFlightbook.Profile.GetUser(szUser) == null)
                    throw new InvalidOperationException("Invalid user specified");

                decimal amount = Convert.ToDecimal(collection["decTestTransactionAmount"], CultureInfo.InvariantCulture);
                decimal fee = Convert.ToDecimal(collection["decTestTransactionFee"], CultureInfo.InvariantCulture);
                if (amount + fee <= 0)
                    throw new InvalidOperationException("Invalid amount/fee");
                if (!Enum.TryParse<Payment.TransactionType>(collection["cmbTestTransactionType"], out Payment.TransactionType type))
                    throw new InvalidOperationException("Unknown transaction type: " + collection["cmbTestTransactionType"]);
                DateTime dt = Convert.ToDateTime(collection["txtDate"], CultureInfo.InvariantCulture);

                switch (type)
                {
                    case Payment.TransactionType.Payment:
                        amount = Math.Abs(amount);
                        break;
                    case Payment.TransactionType.Refund:
                        amount = -1 * Math.Abs(amount);
                        break;
                    default:
                        break;
                }
                Payment p = new Payment(dt, szUser, amount, fee, type, collection["txtTestTransactionNotes"], "Manual Entry", string.Empty);
                p.Commit();
                EarnedGratuity.UpdateEarnedGratuities(szUser, true);

                return RedirectToAction("Index");
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ViewBag.CreateError = ex.Message;
                return CompositeView();
            }
        }
    }
}
