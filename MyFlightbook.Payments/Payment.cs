using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2013-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Payments
{
    /// <summary>
    /// MyFlightbook Purchases (donations, etc.) and payment history
    /// </summary>
    public class Payment
    {
        public enum TransactionType { Payment = 0, Refund = 1, Adjustment = 2, TestTransaction = 3 };

        public const int idUnknown = -1;

        #region Properties
        /// <summary>
        /// The ID for the payment
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The date/time of the payment
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The name of the user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The amount of the transaction
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// The transaction fee
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// The type of the transaction (payment, refund, etc.)
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Any notes about the transaction
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// The transaction ID, per the provider
        /// </summary>
        public string TransactionID { get; set; }

        /// <summary>
        /// Additional data from the provider
        /// </summary>
        public string TransactionNotes { get; set; }

        /// <summary>
        /// The amount which credits towards a gratuity
        /// == 0 for test transactions, negative for refunds
        /// </summary>
        public decimal CreditedAmount
        {
            get
            {
                switch (Type)
                {
                    case TransactionType.Payment:
                    case TransactionType.Adjustment:
                    default:
                        return Amount;
                    case TransactionType.Refund:
                        return -Math.Abs(Amount);   // ensure refunds are negative
                    case TransactionType.TestTransaction:
                        return 0;
                }
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} {1:C} {2} {3}", Timestamp.ToShortDateString(), Amount, Username, Type.ToString());
        }

        #region DB support
        private void InitFromDatareader(MySqlDataReader dr)
        {
            ID = Convert.ToInt32(dr["idPayments"], CultureInfo.InvariantCulture);
            Timestamp = DateTime.SpecifyKind(Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
            Username = (string)dr["Username"];
            Amount = Convert.ToDecimal(dr["Amount"], CultureInfo.InvariantCulture);
            Fee = Convert.ToDecimal(dr["Fee"], CultureInfo.InvariantCulture);
            Type = (TransactionType)Convert.ToInt32(dr["TransactionType"], CultureInfo.InvariantCulture);
            Notes = (string)dr["Notes"];
            TransactionID = (string)dr["TransactionID"];
            TransactionNotes = (string)dr["TransactionData"];
        }

        public void Commit()
        {
            if (String.IsNullOrEmpty(Username))
                throw new InvalidOperationException("No username specified for transaction");
            if (Timestamp.Year == DateTime.MinValue.Year)
                throw new InvalidOperationException("Invalid timestamp");
            if (Amount > 0 && Type == TransactionType.Refund)
                throw new InvalidOperationException("Refunds need to be negative");
            if (Amount < 0 && Type == TransactionType.Payment)
                throw new InvalidOperationException("Payments need to be positive");
            if (String.IsNullOrEmpty(TransactionID))
                throw new InvalidOperationException("No Transaction ID specified");
            if (TransactionNotes == null)
                TransactionNotes = string.Empty;

            const string szSet = "SET Date=?dateval, Username=?user, Amount=?amount, Fee=?feeval, TransactionType=?type, Notes=?notes, TransactionID=?txID, TransactionData=?txNotes";
            string szQ = (ID == idUnknown) ?
                String.Format(CultureInfo.InvariantCulture, "INSERT INTO payments {0}", szSet) :
                String.Format(CultureInfo.InvariantCulture, "UPDATE payments {0} WHERE idPayments=?id", szSet);

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("dateval", Timestamp);
                comm.Parameters.AddWithValue("user", Username);
                comm.Parameters.AddWithValue("amount", Amount);
                comm.Parameters.AddWithValue("feeval", Fee);
                comm.Parameters.AddWithValue("type", Type);
                comm.Parameters.AddWithValue("notes", Notes);
                comm.Parameters.AddWithValue("txID", TransactionID);
                comm.Parameters.AddWithValue("txNotes", TransactionNotes);
                comm.Parameters.AddWithValue("id", ID);
            });

            if (ID == idUnknown)
                ID = dbh.LastInsertedRowId;
        }
        #endregion

        #region Constructors
        public Payment()
        {
            ID = idUnknown;
            Amount = Fee = 0.0M;
            Type = TransactionType.Payment;
            Notes = Username = TransactionID = TransactionNotes = string.Empty;
            Timestamp = DateTime.MinValue;
        }

        protected Payment(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            InitFromDatareader(dr);
        }

        /// <summary>
        /// Initializes the payment record
        /// </summary>
        /// <param name="dt">Timestamp</param>
        /// <param name="szUser">Username</param>
        /// <param name="amount">Amount of the transaction</param>
        /// <param name="fee">The fee amount</param>
        /// <param name="type">Type of transaction</param>
        /// <param name="szNotes">Additional notes.</param>
        /// <param name="txID">The transaction ID from the provider</param>
        /// <param name="txNotes">Additional transaction notes</param>
        public Payment(DateTime dt, string szUser, decimal amount, decimal fee, TransactionType type, string szNotes, string txID, string txNotes) : this()
        {
            Timestamp = dt;
            Username = szUser;
            Amount = amount;
            Fee = fee;
            Type = type;
            Notes = szNotes;
            TransactionID = txID;
            TransactionNotes = txNotes;
        }
        #endregion

        #region Getting transaction records
        private static List<Payment> RecordsForQuery(string szQ, Action<MySqlCommand> initCommand)
        {
            DBHelper dbh = new DBHelper(szQ);
            List<Payment> lst = new List<Payment>();
            dbh.ReadRows(
                (comm) => { initCommand(comm); },
                (dr) => { lst.Add(new Payment(dr)); });
            return lst;
        }

        /// <summary>
        /// Get all of the payment records for a specified user
        /// </summary>
        /// <param name="szUser">The name of the user.  Can be null.</param>
        /// <param name="types">The transaction types; if null, all transactions are returned.</param>
        /// <returns>A list containing the records in reverse chronological order</returns>
        public static IEnumerable<Payment> RecordsForUser(string szUser, IEnumerable<TransactionType> types = null, int offset = -1, int limit = -1)
        {
            List<string> lstRestrictions = new List<string>();
            if (!String.IsNullOrEmpty(szUser))
                lstRestrictions.Add("Username=?szUser");
            if (types != null && types.Any())
            {
                List<int> ints = new List<int>();
                foreach (TransactionType t in types)
                    ints.Add((int)t);
                lstRestrictions.Add(String.Format(CultureInfo.InvariantCulture, "TransactionType IN ({0})", String.Join(", ", ints)));
            }

            string szWHERE = lstRestrictions.Count != 0 ? String.Format(CultureInfo.InvariantCulture, " WHERE {0}", String.Join(" AND ", lstRestrictions)) : string.Empty;

            string szLimit = string.Empty;
            if (limit > 0)
                szLimit = offset > 0 ? String.Format(CultureInfo.InvariantCulture, " LIMIT {0},{1} ", offset, limit) : String.Format(CultureInfo.InvariantCulture, "LIMIT {0}", limit);

            return RecordsForQuery(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM payments {0} ORDER BY Date DESC {1}", szWHERE, szLimit),
                (comm) => { comm.Parameters.AddWithValue("szUser", szUser ?? string.Empty); });
        }

        public static IEnumerable<Payment> RecordsForMonth(int month, int year)
        {
            return RecordsForQuery("SELECT * FROM payments WHERE YEAR(date)=?year AND MONTH(date)=?month ORDER BY Date DESC", (comm) => { comm.Parameters.AddWithValue("year", year); comm.Parameters.AddWithValue("month", month); });
        }

        /// <summary>
        /// Returns all records matching a specific transaction ID
        /// </summary>
        /// <param name="txID">The ID</param>
        /// <returns></returns>
        public static IEnumerable<Payment> RecordsWithID(string txID)
        {
            return RecordsForQuery("SELECT * FROM payments WHERE TransactionID=?txID ORDER BY Date DESC", (comm) => { comm.Parameters.AddWithValue("txID", txID); });
        }

        /// <summary>
        /// Returns all payments made in the system.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Payment> AllRecords()
        {
            return RecordsForQuery("SELECT * FROM payments ORDER BY Date DESC", (comm) => { });
        }

        public static IEnumerable<Payment> AllRecordsForValidUsers()
        {
            return RecordsForQuery("SELECT p.* FROM payments p LEFT JOIN users u ON p.username = u.username WHERE u.username IS NOT NULL ORDER BY Date DESC", (comm) => { });
        }

        /// <summary>
        /// Returns the total amount paid by the specified user, net of refunds, since a specified date
        /// </summary>
        /// <param name="dt">Date in question</param>
        /// <param name="szUser">User in question</param>
        /// <returns>Total amount paid</returns>
        public static decimal TotalPaidSinceDate(DateTime dt, string szUser)
        {
            decimal total = 0.0M;
            DBHelper dbh = new DBHelper("SELECT SUM(Amount) AS totalPayment FROM payments WHERE Username=?szUser AND Date >= ?dateVal");
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("szUser", szUser);
                comm.Parameters.AddWithValue("dateVal", dt);
            },
                (dr) => { total = dr["totalPayment"].ToString().SafeParseDecimal(); });
            return total;
        }

        /// <summary>
        /// Returns running totals for the given period.  E.g,. if you pass 12 months of 30 days, it returns the running 30-day total for 12 months
        /// </summary>
        /// <param name="cDays">Number of days back to look</param>
        /// <param name="cMonths">Number of months back to look</param>
        /// <returns></returns>
        public static IDictionary<DateTime, decimal> RunningPaymentsForTrailingPeriod(int cMonths = 12, int cDays = 30)
        {
            DateTime dtMin = DateTime.Now.AddMonths(-cMonths).Date;
            DateTime dtMax = DateTime.Now.Date;

            List<Payment> lstPayments = new List<Payment>(RecordsForUser(null, new TransactionType[] { TransactionType.Payment, TransactionType.Adjustment, TransactionType.TestTransaction }));
            lstPayments.RemoveAll(pmt => pmt.Timestamp.CompareTo(dtMin) < 0);   // Remove all the oldest
            lstPayments.Reverse();  // go in ascending order

            // Now compute a year's worth of 30-day averages.
            DateTime dtFirstRunning = dtMin.AddDays(cDays).Date;
            Queue<Payment> queue = new Queue<Payment>();

            Dictionary<DateTime, decimal> dReturn = new Dictionary<DateTime, decimal>();

            int iPayment = 0;
            decimal runningTotal = 0.0M;
            for (DateTime dt = dtMin; dt.CompareTo(dtMax) <= 0; dt = dt.AddDays(1))
            {
                // while there are payments to add to the rolling 30-day window, add them
                while (iPayment < lstPayments.Count)
                {
                    Payment p = lstPayments[iPayment];
                    if (p.Timestamp.Date.CompareTo(dt) <= 0)
                    {
                        queue.Enqueue(p);   // add it to the queue
                        runningTotal += (p.Amount - p.Fee);
                        iPayment++;
                    }
                    else
                    {
                        break;
                    }
                }

                // And de-queue anything that's fallen out of the cDays day window
                while (queue.Count > 0 && queue.Peek().Timestamp.Date.CompareTo(dt.AddDays(-cDays)) < 0)
                {
                    Payment p = queue.Dequeue();
                    runningTotal -= (p.Amount - p.Fee);
                }

                // OK, we should have our running total.
                if (dt.CompareTo(dtFirstRunning) >= 0)
                    dReturn[dt.Date] = runningTotal;
            }
            return dReturn;
        }

        public static void ADMINFixFees()
        {
            IEnumerable<Payment> lst = AllRecords();
            foreach (Payment p in lst)
            {
                if (String.IsNullOrEmpty(p.TransactionID) || String.IsNullOrEmpty(p.TransactionNotes))
                    continue;
                System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(p.TransactionNotes);
                p.Fee = Math.Abs(Convert.ToDecimal(nvc["mc_fee"], CultureInfo.InvariantCulture));
                if (p.Type == TransactionType.Payment || p.Type == TransactionType.Refund)
                    p.Commit();
            }
        }
        #endregion
    }
}
