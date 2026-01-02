using MyFlightbook.Clubs;
using MySql.Data.MySqlClient;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2013-2025 MyFlightbook LLC
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
            DateTime dtMin = DateTime.Now.AddMonths(- cMonths).Date;
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

    #region Admin reporting
    /// <summary>
    /// Provides an admin report about the number of users who have made 1 payment, 2 payments, etc.
    /// </summary>
    public class UserTransactionSummary
    {
        public int NumPayments { get; set; }
        public int NumUsers { get; set; }

        public static IEnumerable<UserTransactionSummary> Refresh()
        {
            List<UserTransactionSummary> lst = new List<UserTransactionSummary>();
            DBHelper dbh = new DBHelper(@"SELECT 
    numpayments AS 'Number of payments',
    COUNT(numpayments) AS 'Number of Users'
FROM
    (SELECT 
        COUNT(username) AS numpayments
    FROM
        payments
    WHERE
        TransactionType = 0
    GROUP BY username) p
GROUP BY p.numpayments
ORDER BY p.numpayments");

            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new UserTransactionSummary() { NumPayments = Convert.ToInt32(dr["Number of payments"], CultureInfo.InvariantCulture), NumUsers = Convert.ToInt32(dr["Number of Users"], CultureInfo.InvariantCulture) }); });
            return lst;
        }
    }

    /// <summary>
    /// Provides an admin report about the number of users who have donated $10, $15, ...
    /// </summary>
    public class AmountTransactionSummary
    {
        public int NumTransactions { get; set; }
        public double TransactionValue { get; set; }

        public static IEnumerable<AmountTransactionSummary> Refresh()
        {
            List<AmountTransactionSummary> lst = new List<AmountTransactionSummary>();
            DBHelper dbh = new DBHelper(@"SELECT 
    COUNT(Amount) AS 'Number of transactions', Amount
FROM
    payments
WHERE
    TransactionType = 0
GROUP BY amount
ORDER BY Amount ASC");

            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new AmountTransactionSummary() { NumTransactions = Convert.ToInt32(dr["Number of transactions"], CultureInfo.InvariantCulture), TransactionValue = Convert.ToDouble(dr["Amount"], CultureInfo.InvariantCulture) }); });
            return lst;
        }
    }
    #endregion

    /// <summary>
    /// Abstract class for gratuities.
    /// </summary>
    public abstract class Gratuity
    {
        protected Gratuity()
        {
            MaxReminders = 0;
            GratuityType = GratuityTypes.Unknown;
            Window = TimeSpan.Zero;
            Threshold = 0;
            Name = ThankYouTemplate = string.Empty;
        }

        protected Gratuity(GratuityTypes gt, Decimal threshold, TimeSpan window, int maxreminders, string szName, string szThankyouTemplate, string szDescription) : this()
        {
            GratuityType = gt;
            Threshold = threshold;
            Window = window;
            MaxReminders = maxreminders;
            Name = szName;
            ThankYouTemplate = szThankyouTemplate;
            Description = szDescription;
        }

        #region properties
        /// <summary>
        /// How much does the user have to spend to get this gratuity?
        /// </summary>
        public Decimal Threshold { get; set; }

        /// <summary>
        /// Within what timeframe does the user have to have spent it?
        /// </summary>
        public TimeSpan Window { get; set; }

        /// <summary>
        /// The type of the gratuity
        /// </summary>
        public GratuityTypes GratuityType { get; set; }

        /// <summary>
        /// Maximum # of reminders for this gratuity
        /// </summary>
        public int MaxReminders { get; set; }

        /// <summary>
        /// Name of the gratuity
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Text to display for "Thank-you"
        /// </summary>
        public string ThankYouTemplate { get; set; }

        /// <summary>
        /// Text to display that describes the gratuity in more detail.
        /// </summary>
        public string Description { get; set; }
        #endregion

        #region Reminders
        /// <summary>
        /// Get the subject line for a reminder email
        /// </summary>
        public virtual string ReminderSubject(EarnedGratuity eg) { return string.Empty; }

        /// <summary>
        /// Get the body for a reminder email
        /// </summary>
        public virtual string ReminderBody(EarnedGratuity eg) { return string.Empty; }
        #endregion

        #region GratuityFactory
        public enum GratuityTypes { Unknown, CloudBackup, Videos, CreateClub, EternalGratitude, CurrencyAlerts, PartnerDiscount };

        /// <summary>
        /// Return a concrete gratuity from a specified gratuitytype.
        /// </summary>
        /// <param name="gt">The gratuity type</param>
        /// <returns>A new gratuity</returns>
        public static Gratuity GratuityFromType(GratuityTypes gt)
        {
            switch (gt)
            {
                case GratuityTypes.CloudBackup:
                    return new NightlyDropbox();
                case GratuityTypes.Videos:
                    return new StoreVideosGratuity();
                case GratuityTypes.CreateClub:
                    return new CreateClubGratuity();
                case GratuityTypes.EternalGratitude:
                    return new EternalGratitudeGratuity();
                case GratuityTypes.CurrencyAlerts:
                    return new CurrencyAlertGratuity();
                case GratuityTypes.PartnerDiscount:
                    return new PartnerDiscountGratuity();
                case GratuityTypes.Unknown:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Return an array of the known gratuity types
        /// </summary>
        public static IEnumerable<GratuityTypes> KnownGratuityTypes
        {
            get { return (GratuityTypes[])Enum.GetValues(typeof(GratuityTypes)); }
        }

        /// <summary>
        /// Return an array of the known gratuities
        /// </summary>
        public static IEnumerable<Gratuity> KnownGratuities
        {
            get
            {
                List<Gratuity> lstGratuities = new List<Gratuity>();
                foreach (Gratuity.GratuityTypes gt in KnownGratuityTypes)
                    if (gt != Gratuity.GratuityTypes.Unknown)
                        lstGratuities.Add(Gratuity.GratuityFromType(gt));
                return lstGratuities;
            }
        }
        #endregion    /// <summary>
        /// Subclassable restriction on users - they may earn a gratuity, but not otherwise be eligible for any number of reasons.
        /// </summary>
        public virtual string UserRestriction { get { return string.Empty; } }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1:C} in {2} days", GratuityType.ToString(), Threshold, Window.Days);
        }

        /// <summary>
        /// Notification when this gratuity has been earned, in case there is any action to take at the point of earning it
        /// </summary>
        /// <param name="dt"></param>
        public virtual void GratuityWasEarned(EarnedGratuity eg) { }
    }

    #region Concrete Gratuities
    /// <summary>
    /// You get nightly dropbox
    /// </summary>
    public class NightlyDropbox : Gratuity
    {
        public NightlyDropbox()
            : base(GratuityTypes.CloudBackup, 25.0M, new TimeSpan(366, 0, 0, 0), 2, Resources.LocalizedText.GratuityNameDropbox, Resources.LocalizedText.GratuityThanksDropbox, Resources.LocalizedText.GratuityDescriptionDropbox)
        {
        }

        public override string UserRestriction
        {
            get { return "  (users.DropboxAccessToken <> '' || users.OneDriveAccessToken <> '' || users.GoogleDriveAccessToken <> '') "; }
        }

        public override string ReminderSubject(EarnedGratuity eg)
        {
            if (eg == null)
                throw new ArgumentNullException(nameof(eg));

            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGratuity.EarnedGratuityStatus.OK: // shouldn't even be called in this case
                    return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityCloudStorageExpiring);
                    else
                        return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityCloudStorageExpired);
                    else
                        return string.Empty;
            }
        }

        public override string ReminderBody(EarnedGratuity eg)
        {
            if (eg == null)
                throw new ArgumentNullException(nameof(eg));

            Profile pf = eg.UserProfile ?? Profile.GetUser(eg.Username);
            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGratuity.EarnedGratuityStatus.OK:
                    return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DropboxExpiring), pf.PreferredGreeting);
                    else
                        return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DropboxExpired), pf.PreferredGreeting);
                    else
                        return string.Empty;
            }
        }
    }

    public class StoreVideosGratuity : Gratuity
    {
        public StoreVideosGratuity()
            : base(GratuityTypes.Videos, 10.0M, new TimeSpan(366, 0, 0, 0), 0, Resources.LocalizedText.GratuityNameVideo, Resources.LocalizedText.GratuityThanksVideo, Resources.LocalizedText.GratuityDescriptionVideo)
        {
        }
    }

    public class CreateClubGratuity : Gratuity
    {
        public CreateClubGratuity()
            : base(GratuityTypes.CreateClub, 40.0M, new TimeSpan(366, 0, 0, 0), 0, Resources.LocalizedText.GratuityNameClub, Resources.LocalizedText.GratuityThanksClub, Resources.LocalizedText.GratuityDescriptionClub)
        {
        }

        /// <summary>
        /// If you earn a club-creation activity and you have promotional or inactive clubs, we will re-activate the clubs
        /// </summary>
        /// <param name="eg">The earned gratuity</param>
        public override void GratuityWasEarned(EarnedGratuity eg)
        {
            if (eg == null)
                throw new ArgumentNullException(nameof(eg));

            base.GratuityWasEarned(eg);

            foreach (Club c in Club.AllClubsForUser(eg.Username))
            {
                bool fIsOwner = c.Creator.CompareCurrentCultureIgnoreCase(eg.Username) == 0 || (c.GetMember(eg.Username)?.RoleInClub ?? ClubMember.ClubMemberRole.Member) == ClubMember.ClubMemberRole.Owner;
                if (fIsOwner && (c.Status == Club.ClubStatus.Promotional || c.Status == Club.ClubStatus.Expired))
                    c.ChangeStatus(Club.ClubStatus.OK);
            }
        }
    }

    public class EternalGratitudeGratuity : Gratuity
    {
        public EternalGratitudeGratuity() : base(GratuityTypes.EternalGratitude, 0.01M, new TimeSpan(366, 0, 0, 0), 2, Resources.LocalizedText.GratuityNameEternalGratitude, Resources.LocalizedText.GratuityThanksEternalGratitude, Resources.LocalizedText.GratuityDescriptionGratitude) { }

        public override string ReminderSubject(EarnedGratuity eg)
        {
            if (eg == null)
                throw new ArgumentNullException(nameof(eg));

            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGratuity.EarnedGratuityStatus.OK: // shouldn't even be called in this case
                    return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityEternalGratitudeExpiring);
                    else
                        return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityEternalGratitudeExpired);
                    else
                        return string.Empty;
            }
        }

        public override string ReminderBody(EarnedGratuity eg)
        {
            if (eg == null)
                throw new ArgumentNullException(nameof(eg));

            Profile pf = eg.UserProfile ?? Profile.GetUser(eg.Username);
            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGratuity.EarnedGratuityStatus.OK:
                    return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.EternalGratitudeExpiring), pf.UserFullName);
                    else
                        return string.Empty;
                case EarnedGratuity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.EternalGratitudeExpired), pf.UserFullName);
                    else
                        return string.Empty;
            }
        }
    }

    public class CurrencyAlertGratuity : Gratuity
    {
        public CurrencyAlertGratuity() : base(GratuityTypes.CurrencyAlerts, 15, new TimeSpan(366, 0, 0, 0), 0, Resources.LocalizedText.GratuityNameCurrencyNotifications, Resources.LocalizedText.GratuityThanksCurrencyNotification, Resources.LocalizedText.GratuityDescriptionCurrencyNotification) { }
    }

    public class PartnerDiscountGratuity : Gratuity
    {
        public PartnerDiscountGratuity() : base(GratuityTypes.PartnerDiscount, 15, new TimeSpan(366, 0, 0, 0), 0, Branding.ReBrand(Resources.LocalizedText.GratuityNamePartnerDiscount), LocalConfig.SettingForKey("GroundSchoolDiscountLink"), Branding.ReBrand(Resources.LocalizedText.GratuityDescriptionPartnerDiscount)) { }
    }
    #endregion

    /// <summary>
    /// An instance of a gratuity that has been earned by a user and which expires on a particular date.
    /// EarnedGratuities encapsulate the business rules for determining qualification and fulfillment of a gratuity
    /// </summary>
    public class EarnedGratuity
    {
        public enum EarnedGratuityStatus { OK, ExpiringSoon, Expired };

        #region properties
        /// <summary>
        /// The type of the gratuity
        /// </summary>
        public Gratuity.GratuityTypes GratuityType { get; set; }

        /// <summary>
        /// The user who earned the gratuity
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The date the gratuity was MOST RECENTLY earned
        /// </summary>
        public DateTime EarnedDate { get; set; }

        /// <summary>
        /// The date the gratuity expires
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Number of expiration reminders sent for this gratuity
        /// </summary>
        public int ReminderCount { get; set; }

        /// <summary>
        /// Date that last reminder was sent
        /// </summary>
        public DateTime LastReminderDate { get; set; }

        /// <summary>
        /// The gratuity object associated with this
        /// </summary>
        public Gratuity GratuityEarned { get; set; }

        /// <summary>
        /// Optionally filled in profile for the user; COULD BE NULL
        /// </summary>
        public Profile UserProfile { get; set; }

        /// <summary>
        /// Any state information for the gratuity - CAN BE NULL
        /// </summary>
        public string State { get; set; }

        public string ThankYou
        {
            get { return GratuityEarned == null ? string.Empty : String.Format(CultureInfo.CurrentCulture, GratuityEarned.ThankYouTemplate, ExpirationDate); }
        }

        /// <summary>
        /// Get the current status of the earned gratuity
        /// </summary>
        public EarnedGratuityStatus CurrentStatus
        {
            get
            {
                if (ExpirationDate.CompareTo(DateTime.Now.AddDays(2)) <= 0)
                    return EarnedGratuityStatus.Expired;
                else if (ExpirationDate.CompareTo(DateTime.Now.AddDays(7)) <= 0)
                    return EarnedGratuityStatus.ExpiringSoon;
                else
                    return EarnedGratuityStatus.OK;
            }
        }
        #endregion

        #region Constructors
        public EarnedGratuity()
        {
            Username = string.Empty;
            EarnedDate = ExpirationDate = LastReminderDate = DateTime.MinValue;
            ReminderCount = 0;
            GratuityType = Gratuity.GratuityTypes.Unknown;
            GratuityEarned = null;
            UserProfile = null;
            State = null;
        }

        protected EarnedGratuity(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            Username = (string)dr["username"];
            GratuityType = (Gratuity.GratuityTypes)(Convert.ToInt32(dr["idGratuityType"], CultureInfo.InvariantCulture));
            GratuityEarned = Gratuity.GratuityFromType(GratuityType);
            EarnedDate = Convert.ToDateTime(dr["dateEarned"], CultureInfo.InvariantCulture);
            ExpirationDate = Convert.ToDateTime(dr["dateExpired"], CultureInfo.InvariantCulture);
            ReminderCount = Convert.ToInt32(dr["remindersSent"], CultureInfo.InvariantCulture);
            LastReminderDate = Convert.ToDateTime(dr["dateLastReminder"], CultureInfo.InvariantCulture);
            State = (string) util.ReadNullableField(dr, "state", null);
            UserProfile = null;
            try
            {
                UserProfile = new Profile(dr);
            }
            catch (MyFlightbookException)
            {
                UserProfile = null;
                throw;
            }
        }

        public EarnedGratuity(Gratuity g, Payment p) : this()
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            GratuityEarned = g ?? throw new ArgumentNullException(nameof(g));
            GratuityType = g.GratuityType;
            Username = p.Username;
            EarnedDate = p.Timestamp;
            ExpirationDate = p.Timestamp.Add(g.Window);
        }
        #endregion

        #region DB
        public void Commit()
        {
            DBHelper dbh = new DBHelper("REPLACE INTO earnedgratuities SET username=?user, idGratuityType=?gt, dateEarned=?earnedDate, dateExpired=?expireDate, remindersSent=?reminderCount, dateLastReminder=?lastReminderDate, state=?state");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", Username);
                comm.Parameters.AddWithValue("gt", (int)GratuityType);
                comm.Parameters.AddWithValue("earnedDate", EarnedDate);
                comm.Parameters.AddWithValue("expireDate", ExpirationDate);
                comm.Parameters.AddWithValue("reminderCount", ReminderCount);
                comm.Parameters.AddWithValue("lastReminderDate", LastReminderDate);
                comm.Parameters.AddWithValue("state", State);
            });
        }

        protected void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM earnedgratuities where username=?user AND idGratuityType=?idgt");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", Username);
                comm.Parameters.AddWithValue("idgt", (int)GratuityType);
            });
        }

        /// <summary>
        /// Returns a list of gratuities earned by the specified user
        /// </summary>
        /// <param name="szUser">Username - pass an empty string or null for all users</param>
        /// <param name="gt">Type of gratuity - pass Unknown for all</param>
        /// <returns>The gratuityties earned for the userc</returns>
        public static List<EarnedGratuity> GratuitiesForUser(string szUser, Gratuity.GratuityTypes gt)
        {
            List<string> lstRestrictions = new List<string>();

            if (!String.IsNullOrEmpty(szUser))
                lstRestrictions.Add(" eg.username=?user ");
            if (gt != Gratuity.GratuityTypes.Unknown)
            {
                lstRestrictions.Add(" eg.idGratuityType=?gt ");
                string szUserRestriction = Gratuity.GratuityFromType(gt).UserRestriction;
                if (!String.IsNullOrEmpty(szUserRestriction))
                    lstRestrictions.Add(String.Format(CultureInfo.InvariantCulture, " ({0}) ", szUserRestriction));
            }
            string szWhereClause = String.Join(" AND ", lstRestrictions.ToArray()).Trim();

            string szQ = @"SELECT *, uir.Rolename AS Role 
                        FROM earnedgratuities eg 
INNER JOIN users ON eg.username=users.username
LEFT JOIN usersinroles uir ON users.username=uir.username
{0} 
ORDER BY dateEarned ASC ";
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szQ, String.IsNullOrEmpty(szWhereClause) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE {0}", szWhereClause)));
            List<EarnedGratuity> lst = new List<EarnedGratuity>();
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("gt", (int)gt);
            },
                (dr) => { lst.Add(new EarnedGratuity(dr)); });
            return lst;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - User '{1}', earned on {2} expires on {3}", GratuityType.ToString(), Username, EarnedDate.ToShortDateString(), ExpirationDate.ToShortDateString());
        }

        /// <summary>
        /// Goes through all earned gratuities and sends out reminders as needed.
        /// </summary>
        /// <param name="userRestriction">Username desired; null or empty for all users.</param>
        public static void SendRemindersOfExpiringGratuities(string userRestriction)
        {
            List<EarnedGratuity> lstEg = EarnedGratuity.GratuitiesForUser(userRestriction ?? string.Empty, Gratuity.GratuityTypes.Unknown);

            // Group these by user
            Dictionary<string, List<EarnedGratuity>> dict = new Dictionary<string, List<EarnedGratuity>>();
            foreach (EarnedGratuity eg in lstEg)
            {
                // Don't waste time or space with OK gratuities
                if (eg.CurrentStatus == EarnedGratuityStatus.OK)
                    continue;

                if (!dict.ContainsKey(eg.Username))
                    dict[eg.Username] = new List<EarnedGratuity>();
                dict[eg.Username].Add(eg);
            }

            foreach (string szuser in dict.Keys)
            {
                List<EarnedGratuity> lstForUser = dict[szuser];
                // sort by descending threshold values.
                lstForUser.Sort((eg1, eg2) => { return eg2.GratuityEarned.Threshold.CompareTo(eg1.GratuityEarned.Threshold); });

                // Now find the highest value gratuity that needs a reminder - send the reminder for that, then set the last reminder date for any other (presumably lower-value) gratuities.
                foreach (EarnedGratuity eg in lstForUser)
                {
                    if (eg.SendReminderIfNeeded())
                        break;  // don't send any more reminders for this gratuity
                }
            }
        }

        protected bool SendReminderIfNeeded()
        {
            // do nothing if we aren't getting close to expiration, or if we've sent the max # of reminders
            if (CurrentStatus == EarnedGratuityStatus.OK || ReminderCount >= GratuityEarned.MaxReminders || DateTime.Now.Subtract(LastReminderDate).TotalDays < 1)
                return false;

            // ensure the profile is filled in; the gratuity might want to use it.
            if (UserProfile == null)
                UserProfile = Profile.GetUser(Username);

            // Get the subject/body from the gratuity
            // THE GRATUITY CONTROLS whether anything is sent.  If either of these are empty, we abort.
            string szSubject = GratuityEarned.ReminderSubject(this);
            string szBody = GratuityEarned.ReminderBody(this);
            if (String.IsNullOrEmpty(szSubject) || String.IsNullOrEmpty(szBody))
                return false;

            util.NotifyUser(szSubject, util.ApplyHtmlEmailTemplate(szBody, false), new System.Net.Mail.MailAddress(UserProfile.Email, UserProfile.UserFullName), true, true);
            // Update the # of reminders sent
            ReminderCount++;
            LastReminderDate = DateTime.Now;
            Commit();
            return true;
        }

        /// <summary>
        /// Sets the expiration date to the date of the qualifying payment
        /// </summary>
        /// <param name="p">The payment that extends the date</param>
        /// <param name="fUpdateReminderCount">Reset the reminder regime?</param>
        /// <returns>True if the object was modified</returns>
        protected virtual bool ExtendExpiration(Payment p, Boolean fUpdateReminderCount)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            DateTime dtFromPayment = p.Timestamp.Add(GratuityEarned.Window);

            // No later than now + window
            DateTime dtNew = dtFromPayment.EarlierDate(DateTime.Now.Add(GratuityEarned.Window));
            if ((ExpirationDate.CompareTo(dtNew) != 0))
            {
                ExpirationDate = dtNew;
                if (fUpdateReminderCount)
                {
                    ReminderCount = 0;
                    LastReminderDate = DateTime.MinValue;
                }
                return true;
            }
            else
                return false;
        }

        protected static string SessionKeyForUser(string szUser, Gratuity.GratuityTypes gt)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}-Gratuity-{1}", szUser, gt.ToString());
        }

        /// <summary>
        /// Determines if the specified user is eligible for this gratuity.  Cached for a session.
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <param name="gt">The type of gratuity</param>
        /// <returns>True if they have spent (net of refunds) the required amount within the required window</returns>
        public static bool UserQualifies(string szUser, Gratuity.GratuityTypes gt)
        {
            Profile pf = Profile.GetUser(szUser);
            string szSessionKey = SessionKeyForUser(szUser, gt);
            if (pf.AssociatedData.TryGetValue(szSessionKey, out object value))
                return (bool)value;

            List<EarnedGratuity> lst = GratuitiesForUser(szUser, gt);
            return (bool) (pf.AssociatedData[szSessionKey] = lst.Count != 0 && lst[0].ExpirationDate.CompareTo(DateTime.Now) > 0);
        }

        private static void ResetSessionForGratuities(string szUser)
        {
            Profile pf = Profile.GetUser(szUser);

            foreach (Gratuity.GratuityTypes gt in (Gratuity.GratuityTypes[])Enum.GetValues(typeof(Gratuity.GratuityTypes)))
            {
                string szSessionKey = SessionKeyForUser(szUser, gt);
                if (pf.AssociatedData.ContainsKey(szSessionKey))
                    pf.AssociatedData.Remove(szSessionKey);
            }
        }

        /// <summary>
        /// Deletes all gratuities for the specified user
        /// </summary>
        /// <param name="szUser">Username.  If null or empty, deletes all gratuities</param>
        public static void DeleteGratuitiesForUser(string szUser)
        {
            // Delete all gratuities for the specified user, or all gratuities for all users.
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "DELETE FROM earnedgratuities WHERE {0} AND idGratuityType > 0", String.IsNullOrEmpty(szUser) ? "username <> ''" : "username = ?user"));
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("user", szUser); });
        }

        #region UpdateEarnedGratuities Helper
        /// <summary>
        /// Returns a dictionary of payments, keyed by user.  Each element in the dictionary is a list of that users payments
        /// </summary>
        /// <param name="szUser">The requested user, null for all users</param>
        /// <returns></returns>
        private static Dictionary<string, Collection<Payment>> PaymentListsForUser(string szUser)
        {
            List<Payment> lstAllPayments = new List<Payment>(String.IsNullOrEmpty(szUser) ? Payment.AllRecordsForValidUsers() : Payment.RecordsForUser(szUser));

            // Sort the list in Ascending order by date
            lstAllPayments.Sort((p1, p2) => { return p1.Timestamp.CompareTo(p2.Timestamp); });

            // Now splice them up into individual payment lists by user
            Dictionary<string, Collection<Payment>> dictPayments = new Dictionary<string, Collection<Payment>>();
            foreach (Payment p in lstAllPayments)
                (dictPayments.TryGetValue(p.Username, out Collection<Payment> value) ? value : (dictPayments[p.Username] = new Collection<Payment>())).Add(p);

            return dictPayments;
        }

        /// <summary>
        /// Compress the payments list by merging refunds into the prior payment.
        /// E.g., a series of payments that is: 
        /// $100, $40, -$25, -15 ==> $100
        /// $100, $40, -$25 ==> $100, $15
        /// -$25, $40, -$50 ==> -$35
        /// The business rule here is that the refund goes against the most recent prior payment
        /// This ensures that we don't over-extend the gratuity.
        /// </summary>
        /// <param name="lstPayments">The list of payments for the user; modified in place</param>
        private static void CompressPaymentsForUser(List<Payment> lstPayments)
        {
            int cPayments = 0;
            while (cPayments != lstPayments.Count)
            {
                cPayments = lstPayments.Count;
                // Remove all 0-value transactions anyhow
                lstPayments.RemoveAll(p => p.CreditedAmount == 0.0M);

                // Now merge refunds into prior purchases
                for (int i = 1; i < lstPayments.Count; i++)
                {
                    // Move the refund to the prior payment if it is negative
                    decimal ca = lstPayments[i].CreditedAmount;
                    if (ca < 0)
                    {
                        lstPayments[i - 1].Amount += ca;
                        lstPayments.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Update earned gratuities for users to ensure that the expiration dates, etc., are correct.
        /// </summary>
        /// <param name="szUser">The user for whom gratuities should be updated; null or empty for all users</param>
        /// <param name="fResetReminders">True to reset reminders to 0 (i.e., no reminders sent)</param>
        public static void UpdateEarnedGratuities(string szUser, bool fResetReminders)
        {
            Dictionary<string, Collection<Payment>> dictPayments = PaymentListsForUser(szUser);

            IEnumerable<Gratuity> lstKnownGratuities = Gratuity.KnownGratuities;
            List<EarnedGratuity> lstUpdatedGratuityList = new List<EarnedGratuity>();

            // Go through each user's payment history (could be just the one user) and determine their gratuities.
            // Treat this as a flight currency - look back however far is necessary to meet the threshold and compute expiration from that.
            foreach (string szKey in dictPayments.Keys)
            {
                ResetSessionForGratuities(szUser);

                List<Payment> lstPayments = new List<Payment>(dictPayments[szKey]);

                CompressPaymentsForUser(lstPayments);

                // Reverse sort
                lstPayments.Sort((p1, p2) => { return p2.Timestamp.CompareTo(p1.Timestamp); });

                // OK, payments should be compacted - go through them and apply the gratuities
                foreach (Gratuity g in lstKnownGratuities)
                {
                    // Go backwards in time until we either hit the threshold or the window.
                    DateTime dtEarliest = DateTime.Now.Subtract(g.Window).Date;

                    decimal total = 0;
                    foreach (Payment p in lstPayments)
                    {
                        if (p.Timestamp.CompareTo(dtEarliest) < 0)
                            break;  // not earned.

                        total += p.Amount;
                        if (total >= g.Threshold)
                        {
                            EarnedGratuity eg = lstUpdatedGratuityList.Find(eg2 => (eg2.GratuityType == g.GratuityType && eg2.Username == p.Username));
                            if (eg == null)
                            {
                                eg = new EarnedGratuity(g, p);
                                lstUpdatedGratuityList.Add(eg);
                            }
                            else
                            {
                                eg.EarnedDate = p.Timestamp.Date;
                                eg.ExtendExpiration(p, fResetReminders);
                            }
                            g.GratuityWasEarned(eg);
                            break;  // earned; no need to continue processing payments
                        }
                    }
                }
            }

            // We now have a list of gratuities that should be correct
            List<EarnedGratuity> lstExistingEarnedGratuities = EarnedGratuity.GratuitiesForUser(szUser, Gratuity.GratuityTypes.Unknown);

            foreach (EarnedGratuity eg in lstUpdatedGratuityList)
            {
                // save this one
                eg.Commit();
                // And remove it from the list of prior earned gratuities (we'll remove any stragglers below)
                lstExistingEarnedGratuities.RemoveAll(eg2 => (eg2.GratuityType == eg.GratuityType && eg2.Username == eg.Username));
            }

            // Anything that is left is no longer found (e.g., if a payment was deleted)
            // Shouldn't happen, but clean up just in case
            foreach (EarnedGratuity eg in lstExistingEarnedGratuities)
                eg.Delete();
        }
    }

    /// <summary>
    /// Process a Paypal IPN notification, getting the response.
    /// </summary>
    public static class PayPalIPN
    {
        public static string VerifyResponse(bool fSandbox, string strRequest)
        {
            //Post back to either sandbox or live
            const string strSandbox = "https://www.sandbox.paypal.com/cgi-bin/webscr";
            const string strLive = "https://www.paypal.com/cgi-bin/webscr";

            // Issue #511: https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel
            // Need to set security protocol BEFORE WebRequest.Create, not after.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(new Uri(fSandbox ? strSandbox : strLive));

            //Set values for the request back
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            strRequest += "&cmd=_notify-validate";
            req.ContentLength = strRequest.Length;
            string strResponse = string.Empty;

            //for proxy
            //WebProxy proxy = new WebProxy(new Uri("http://url:port#"));
            //req.Proxy = proxy;

            //Send the request to PayPal and get the response
            using (StreamWriter streamOut = new StreamWriter(req.GetRequestStream(), System.Text.Encoding.ASCII))
            {
                streamOut.Write(strRequest);
            }
            using (StreamReader streamIn = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                strResponse = streamIn.ReadToEnd();
            }
            return strResponse;
        }

        public static string SafeRequestParam(HttpRequestBase Request, string paramName)
        {
            if (Request == null)
                throw new ArgumentNullException(nameof(Request));
            return Request[paramName] ?? string.Empty;
        }

        public static void ProcessIPNNotification(HttpRequestBase Request)
        {
            if (Request == null)
                throw new ArgumentNullException(nameof(Request));
            byte[] param = Request.BinaryRead(Request.ContentLength);
            string strRequest = Encoding.ASCII.GetString(param);

            bool fSandbox = !String.IsNullOrEmpty(Request["sandbox"]);
            string strResponse = PayPalIPN.VerifyResponse(fSandbox, strRequest);

            string szUser = SafeRequestParam(Request, "custom");
            string szProductID = Request["os1"] ?? string.Empty;
            string szAmount = SafeRequestParam(Request, "mc_gross"); 
            string szTransactionID = SafeRequestParam(Request, "txn_id"); 
            string szTransactionType = SafeRequestParam(Request, "txn_type"); 
            string szCurrency = SafeRequestParam(Request, "mc_currency"); 
            string szReasonCode = SafeRequestParam(Request, "reason_code"); 
            string szParentTxnID = SafeRequestParam(Request, "parent_txn_id"); 
            string szFee = SafeRequestParam(Request, "mc_fee");

            if (strResponse == "VERIFIED")
            {
                //check the payment_status is Completed
                //check that txn_id has not been previously processed
                //check that receiver_email is your Primary PayPal email
                //check that payment_amount/payment_currency are correct
                //process payment

                StringBuilder sbErr = new StringBuilder();
                if (!Decimal.TryParse(szAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                {
                    sbErr.AppendFormat(CultureInfo.CurrentCulture, "Invalid payment amount: {0}\r\n\r\n", szAmount);
                    d = 0.0M;
                }

                if (d == 0.0M)
                    sbErr.AppendFormat(CultureInfo.CurrentCulture, "Payment amount of 0.0!\r\n\r\n");
                Payment.TransactionType transType = (d > 0) ? Payment.TransactionType.Payment : Payment.TransactionType.Refund;

                if (!Decimal.TryParse(szFee, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fee))
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
                            cPayments = lst.Count();
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
                            util.NotifyAdminEvent("Sandbox Transaction", "payment = " + Newtonsoft.Json.JsonConvert.SerializeObject(p), ProfileRoles.maskSiteAdminOnly);
                            return;
                        }
                        p.Commit();

                        EarnedGratuity.UpdateEarnedGratuities(p.Username, true);
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
                            util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle), util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DonationThankYou), pf.PreferredGreeting), false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
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

            Profile pf = Profile.GetUser(szUser);
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
        private const string CURRENT_API_VERSION = "2025-11-17";
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
        public static HttpStatusCode ProcessStripeNotification(bool testMode, string webHookSignature, string versionParam, string json)
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

                            EarnedGratuity.UpdateEarnedGratuities(p.Username, true);
                            Profile pf = Profile.GetUser(p.Username);

                            if (String.IsNullOrEmpty(pf.UserName))
                                throw new InvalidOperationException($"Transaction request for invalid user: {p.Username}\r\n\r\n");

                            // Send a thank-you for payment to the donor
                            if (p.Type == Payment.TransactionType.Payment)
                                util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle), util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DonationThankYou), pf.PreferredGreeting), false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);

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

    public class YearlyPayments : IComparable
    {
        public int Year { get; set; }
        public IList<PeriodPaymentStat> MonthlyPayments { get; private set; }
        public PeriodPaymentStat AnnualPayment { get; set; }

        public YearlyPayments(int year)
        {
            Year = year;
            PeriodPaymentStat[] rgPayments = new PeriodPaymentStat[12];
            MonthlyPayments = new List<PeriodPaymentStat>(rgPayments);
            for (int i = 0; i < 12; i++)
                MonthlyPayments[i] = new PeriodPaymentStat();
            AnnualPayment = new PeriodPaymentStat();
        }

        public static IEnumerable<YearlyPayments> PaymentsByYearAndMonth()
        {
            Dictionary<int, YearlyPayments> d = new Dictionary<int, YearlyPayments>();

            DBHelper dbh = new DBHelper(@"SELECT 
    YEAR(Date) AS Year,
    MONTH(Date) AS Month,
    SUM(Amount) AS Gross,
    SUM(Fee) AS Fee,
    SUM(Amount - Fee) AS Net,
    SUM(IF(TransactionID = '', 0, Amount - FEE)) AS NetPaypal
FROM
    Payments
WHERE
    TransactionType IN (0 , 1)
GROUP BY Year , Month
ORDER BY Year ASC , Month ASC;");

            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    int year = Convert.ToInt32(dr["Year"], CultureInfo.InvariantCulture);
                    int month = Convert.ToInt32(dr["Month"], CultureInfo.InvariantCulture);
                    if (month <= 0 || month > 12)
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Invalid month in donations: {0}", month));
                    string MonthPeriod = String.Format(CultureInfo.InvariantCulture, "{0:0000}-{1:00}", year, month);
                    if (!d.ContainsKey(year))
                        d[year] = new YearlyPayments(year);
                    d[year].MonthlyPayments[month - 1] = new PeriodPaymentStat()
                    {
                        Net = Convert.ToDouble(dr["NetPaypal"], CultureInfo.InvariantCulture),
                        Gross = Convert.ToDouble(dr["Gross"], CultureInfo.InvariantCulture),
                        Fee = Convert.ToDouble(dr["Fee"], CultureInfo.InvariantCulture)
                    };
                });

            List<YearlyPayments> lst = new List<YearlyPayments>(d.Values);
            lst.Sort();

            PeriodPaymentStat[] highWaters = new PeriodPaymentStat[13];
            

            // Now go through and compute stats.
            for (int i = 0; i < lst.Count; i++)
            {
                YearlyPayments thisYear = lst[i];

                // First year is special because we can't compare to previous year.
                if (i == 0)
                {

                    foreach (PeriodPaymentStat stat in thisYear.MonthlyPayments)
                    {
                        thisYear.AnnualPayment.Fee += stat.Fee;
                        thisYear.AnnualPayment.Net += stat.Net;
                        thisYear.AnnualPayment.Gross += stat.Gross;
                    }
                    continue;
                }

                if (lst[i - 1].Year != lst[i].Year - 1) // ignore non-contiguous years (shouldn't happen)
                    continue;

                double annual = 0;
                YearlyPayments lastYear = lst[i - 1];

                for (int j = 0; j < 12; j++)
                {
                    PeriodPaymentStat lastYearMonth = lastYear.MonthlyPayments[j];
                    PeriodPaymentStat thisYearMonth = thisYear.MonthlyPayments[j];
                    PeriodPaymentStat highWater = highWaters[j];
                    if (thisYearMonth.Net > (highWater?.Net ?? 0))
                        highWaters[j] = thisYearMonth;

                    annual += thisYearMonth.Net;
                    thisYearMonth.YOYGross = thisYearMonth.Net - lastYearMonth.Net;
                    thisYearMonth.YOYPercent = (lastYearMonth.Net == 0) ? 0 : thisYearMonth.YOYGross / lastYearMonth.Net;
                }

                thisYear.AnnualPayment.Net = annual;
                thisYear.AnnualPayment.YOYGross = thisYear.AnnualPayment.Net - lastYear.AnnualPayment.Net;
                thisYear.AnnualPayment.YOYPercent = (lastYear.AnnualPayment.Net == 0) ? 0 : thisYear.AnnualPayment.YOYGross / lastYear.AnnualPayment.Net;

                if (annual > (highWaters[12]?.Net ?? 0))
                    highWaters[12] = thisYear.AnnualPayment;
            }

            foreach (PeriodPaymentStat pps in highWaters)
                pps.IsHighWater = true;

            return lst;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            YearlyPayments yp = obj as YearlyPayments;
            return Year.CompareTo(yp.Year);
        }
        public override bool Equals(object obj)
        {
            return obj is YearlyPayments payments &&
                   Year == payments.Year &&
                   EqualityComparer<IList<PeriodPaymentStat>>.Default.Equals(MonthlyPayments, payments.MonthlyPayments) &&
                   EqualityComparer<PeriodPaymentStat>.Default.Equals(AnnualPayment, payments.AnnualPayment);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -296683479;
                hashCode = hashCode * -1521134295 + Year.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<IList<PeriodPaymentStat>>.Default.GetHashCode(MonthlyPayments);
                hashCode = hashCode * -1521134295 + EqualityComparer<PeriodPaymentStat>.Default.GetHashCode(AnnualPayment);
                return hashCode;
            }
        }

        public static bool operator ==(YearlyPayments left, YearlyPayments right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(YearlyPayments left, YearlyPayments right)
        {
            return !(left == right);
        }

        public static bool operator <(YearlyPayments left, YearlyPayments right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(YearlyPayments left, YearlyPayments right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(YearlyPayments left, YearlyPayments right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(YearlyPayments left, YearlyPayments right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    public class PeriodPaymentStat
    {
        public double Net { get; set; }
        public double Gross { get; set; }
        public double Fee { get; set; }
        public double YOYPercent { get; set; }
        public double YOYGross { get; set; }
        public bool IsHighWater { get; set; }
    }
}