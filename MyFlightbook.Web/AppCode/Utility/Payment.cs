using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
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
                    comm.Parameters.AddWithValue("txNotes", TransactionNotes.LimitTo(1023));
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

        /// <summary>
        /// Initializes the payment object from the database with the specified ID
        /// </summary>
        /// <param name="id">The id of the payment to load</param>
        public Payment(int id) : this()
        {
            DBHelper dbh = new DBHelper("SELECT * FROM payments WHERE idPayments=?id");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("id", ID); },
                (dr) => { InitFromDatareader(dr); }
            );
        }

        protected Payment(MySqlDataReader dr) : this()
        {
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
        private static IEnumerable<Payment> RecordsForQuery(string szQ, Action<MySqlCommand> initCommand)
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
        /// <param name="szUser">The name of the user</param>
        /// <returns>A list containing the records in reverse chronological order</returns>
        public static IEnumerable<Payment> RecordsForUser(string szUser)
        {
            return RecordsForQuery("SELECT * FROM payments WHERE Username=?szUser ORDER BY Date DESC", (comm) => { comm.Parameters.AddWithValue("szUser", szUser); });
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
        /// Finds the total of payments since a given date in a list.
        /// </summary>
        /// <param name="dtStart">The threshold date</param>
        /// <param name="dtEnd">The latest date to contribute</param>
        /// <param name="lst">The list of payments</param>
        /// <returns>The total amount of the payments</returns>
        public static decimal PaymentsInDateRange(DateTime dtStart, DateTime dtEnd, List<Payment> lst)
        {
            decimal total = 0;
            lst.ForEach((p) => { if (p.Timestamp.Date.CompareTo(dtStart) >= 0 && p.Timestamp.Date.CompareTo(dtEnd) <= 0) total += p.CreditedAmount; });
            return total;
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
        #endregion
    }

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
            Name = ThankYou = string.Empty;
        }

        protected Gratuity(GratuityTypes gt, Decimal threshold, TimeSpan window, int maxreminders, string szName, string szThankyou) : this()
        {
            GratuityType = gt;
            Threshold = threshold;
            Window = window;
            MaxReminders = maxreminders;
            Name = szName;
            ThankYou = szThankyou;
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
        public string ThankYou { get; set; }
        #endregion

        #region Reminders
        /// <summary>
        /// Get the subject line for a reminder email
        /// </summary>
        public virtual string ReminderSubject(EarnedGrauity eg) { return string.Empty; }

        /// <summary>
        /// Get the body for a reminder email
        /// </summary>
        public virtual string ReminderBody(EarnedGrauity eg) { return string.Empty; }
        #endregion

        #region GratuityFactory
        public enum GratuityTypes { Unknown, CloudBackup, Videos, CreateClub };

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
                case GratuityTypes.Unknown:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Return an array of the known gratuity types
        /// </summary>
        public static System.Collections.ObjectModel.ReadOnlyCollection<GratuityTypes> KnownGratuityTypes
        {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<GratuityTypes>((GratuityTypes[])Enum.GetValues(typeof(GratuityTypes))); }
        }

        /// <summary>
        /// Return an array of the known gratuities
        /// </summary>
        public static ReadOnlyCollection<Gratuity> KnownGratuities
        {
            get
            {
                List<Gratuity> lstGratuities = new List<Gratuity>();
                foreach (Gratuity.GratuityTypes gt in KnownGratuityTypes)
                    if (gt != Gratuity.GratuityTypes.Unknown)
                        lstGratuities.Add(Gratuity.GratuityFromType(gt));
                return new ReadOnlyCollection<Gratuity>(lstGratuities);
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
        public virtual void GratuityWasEarned(EarnedGrauity eg) { }
    }

    /// <summary>
    /// You get nightly dropbox
    /// </summary>
    public class NightlyDropbox : Gratuity
    {
        public NightlyDropbox()
            : base(GratuityTypes.CloudBackup, 25.0M, new TimeSpan(366, 0, 0, 0), 2, Resources.LocalizedText.GratuityNameDropbox, Resources.LocalizedText.GratuityThanksDropbox)
        {
        }

        public override string UserRestriction
        {
            get
            {
                return "  (users.DropboxAccessToken <> '' || users.OneDriveAccessToken <> '' || users.GoogleDriveAccessToken <> '' || users.ICloudAccessToken <> '') ";
            }
        }

        public override string ReminderSubject(EarnedGrauity eg)
        {
            if (eg == null)
                throw new ArgumentNullException("eg");

            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGrauity.EarnedGratuityStatus.OK: // shouldn't even be called in this case
                    return string.Empty;
                case EarnedGrauity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityCloudStorageExpiring);
                    else
                        return string.Empty;
                case EarnedGrauity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return Branding.ReBrand(Resources.LocalizedText.gratuityCloudStorageExpired);
                    else
                        return string.Empty;
            }
        }

        public override string ReminderBody(EarnedGrauity eg)
        {
            if (eg == null)
                throw new ArgumentNullException("eg");

            Profile pf = eg.UserProfile ?? Profile.GetUser(eg.Username);
            switch (eg.CurrentStatus)
            {
                default:
                case EarnedGrauity.EarnedGratuityStatus.OK:
                    return string.Empty;
                case EarnedGrauity.EarnedGratuityStatus.ExpiringSoon:
                    if (eg.ReminderCount == 0)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DropboxExpiring), pf.UserFullName);
                    else
                        return string.Empty;
                case EarnedGrauity.EarnedGratuityStatus.Expired:
                    if (eg.ReminderCount <= 1)
                        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.DropboxExpired), pf.UserFullName);
                    else
                        return string.Empty;
            }
        }
    }

    public class StoreVideosGratuity : Gratuity
    {
        public StoreVideosGratuity()
            : base(GratuityTypes.Videos, 10.0M, new TimeSpan(366, 0, 0, 0), 0, Resources.LocalizedText.GratuityNameVideo, Resources.LocalizedText.GratuityThanksVideo)
        {
        }
    }

    public class CreateClubGratuity : Gratuity
    {
        public CreateClubGratuity()
            : base(GratuityTypes.CreateClub, 40.0M, new TimeSpan(366, 0, 0, 0), 0, Resources.LocalizedText.GratuityNameClub, Resources.LocalizedText.GratuityThanksClub)
        {
        }

        /// <summary>
        /// If you earn a club-creation activity and you have promotional or inactive clubs, we will re-activate the clubs
        /// </summary>
        /// <param name="eg">The earned gratuity</param>
        public override void GratuityWasEarned(EarnedGrauity eg)
        {
            if (eg == null)
                throw new ArgumentNullException("eg");

            base.GratuityWasEarned(eg);

            foreach (Clubs.Club c in Clubs.Club.ClubsCreatedByUser(eg.Username))
            {
                if (c.Status == Clubs.Club.ClubStatus.Promotional || c.Status == Clubs.Club.ClubStatus.Expired)
                    c.ChangeStatus(Clubs.Club.ClubStatus.OK);
            }
        }
    }

    /// <summary>
    /// An instance of a gratuity that has been earned by a user and which expires on a particular date.
    /// EarnedGratuities encapsulate the business rules for determining qualification and fulfillment of a gratuity
    /// </summary>
    public class EarnedGrauity
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
        public EarnedGrauity()
        {
            Username = string.Empty;
            EarnedDate = ExpirationDate = LastReminderDate = DateTime.MinValue;
            ReminderCount = 0;
            GratuityType = Gratuity.GratuityTypes.Unknown;
            GratuityEarned = null;
            UserProfile = null;
        }

        protected EarnedGrauity(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            Username = (string)dr["username"];
            GratuityType = (Gratuity.GratuityTypes)(Convert.ToInt32(dr["idGratuityType"], CultureInfo.InvariantCulture));
            GratuityEarned = Gratuity.GratuityFromType(GratuityType);
            EarnedDate = Convert.ToDateTime(dr["dateEarned"], CultureInfo.InvariantCulture);
            ExpirationDate = Convert.ToDateTime(dr["dateExpired"], CultureInfo.InvariantCulture);
            ReminderCount = Convert.ToInt32(dr["remindersSent"], CultureInfo.InvariantCulture);
            LastReminderDate = Convert.ToDateTime(dr["dateLastReminder"], CultureInfo.InvariantCulture);
            UserProfile = null;
            try
            {
                UserProfile = new Profile(dr);
            }
            catch (MyFlightbookException)
            {
                UserProfile = null;
            }
        }

        public EarnedGrauity(Gratuity g, Payment p) : this()
        {
            if (g == null)
                throw new ArgumentNullException("g");
            if (p == null)
                throw new ArgumentNullException("p");

            GratuityEarned = g;
            GratuityType = g.GratuityType;
            Username = p.Username;
            EarnedDate = p.Timestamp;
            ExpirationDate = p.Timestamp.Add(g.Window);
        }
        #endregion

        #region DB
        protected virtual void Commit()
        {
            DBHelper dbh = new DBHelper("REPLACE INTO earnedgratuities SET username=?user, idGratuityType=?gt, dateEarned=?earnedDate, dateExpired=?expireDate, remindersSent=?reminderCount, dateLastReminder=?lastReminderDate");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", Username);
                comm.Parameters.AddWithValue("gt", (int)GratuityType);
                comm.Parameters.AddWithValue("earnedDate", EarnedDate);
                comm.Parameters.AddWithValue("expireDate", ExpirationDate);
                comm.Parameters.AddWithValue("reminderCount", ReminderCount);
                comm.Parameters.AddWithValue("lastReminderDate", LastReminderDate);
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
        public static List<EarnedGrauity> GratuitiesForUser(string szUser, Gratuity.GratuityTypes gt)
        {
            List<string> lstRestrictions = new List<string>();

            if (!String.IsNullOrEmpty(szUser))
                lstRestrictions.Add(" eg.username=?user ");
            if (gt != Gratuity.GratuityTypes.Unknown)
            {
                lstRestrictions.Add(" eg.idGratuityType=?gt ");
                string szUserRestriction = Gratuity.GratuityFromType(gt).UserRestriction;
                if (!String.IsNullOrEmpty(szUserRestriction))
                    lstRestrictions.Add(String.Format(" ({0}) ", szUserRestriction));
            }
            string szWhereClause = String.Join(" AND ", lstRestrictions.ToArray()).Trim();

            string szQ = @"SELECT *, uir.Rolename AS Role 
                        FROM earnedgratuities eg 
INNER JOIN users ON eg.username=users.username
LEFT JOIN usersinroles uir ON users.username=uir.username
{0} 
ORDER BY dateEarned ASC ";
            DBHelper dbh = new DBHelper(String.Format(szQ, String.IsNullOrEmpty(szWhereClause) ? string.Empty : String.Format("WHERE {0}", szWhereClause)));
            List<EarnedGrauity> lst = new List<EarnedGrauity>();
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("user", szUser);
                comm.Parameters.AddWithValue("gt", (int)gt);
            },
                (dr) => { lst.Add(new EarnedGrauity(dr)); });
            return lst;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - User '{1}', earned on {2} expires on {3}", GratuityType.ToString(), Username, EarnedDate.ToShortDateString(), ExpirationDate.ToShortDateString());
        }

        public bool SendReminderIfNeeded()
        {
            // do nothing if we aren't getting close to expiration, or if we've sent the max # of reminders
            if (CurrentStatus == EarnedGratuityStatus.OK || ReminderCount >= GratuityEarned.MaxReminders)
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

            util.NotifyUser(szSubject, szBody, new System.Net.Mail.MailAddress(UserProfile.Email, UserProfile.UserFullName), true, false);
            // Update the # of reminders sent
            ReminderCount++;
            LastReminderDate = DateTime.Now;
            Commit();
            return true;
        }

        /// <summary>
        /// Extends the expiration date the specified timespan, extending an existing 
        /// expiration or setting a new one as appropriate.  E.g., if you are just earning 
        /// a 1-year gratuity, sets it for 1 year from today.  If it is June 5 and your existing
        /// gratuity expires on Aug 12, you get until Aug 12 one year beyond.
        /// </summary>
        /// <param name="p">The payment that extends the date</param>
        /// <param name="fUpdateReminderCount">Reset the reminder regime?</param>
        /// <returns>True if the object was modified</returns>
        protected virtual bool ExtendExpiration(Payment p, Boolean fUpdateReminderCount)
        {
            if (p == null)
                throw new ArgumentNullException("p");

            DateTime dtFromPayment = p.Timestamp.Add(GratuityEarned.Window);
            DateTime dtFromExistingExpiration = ExpirationDate.Add(GratuityEarned.Window);

            // Our business rule here is that if the payment is LESS than the amount required for the gratuity, we will extend from the date of the payment
            // (Assumes that this is a top-off.  E.g., $10/month will extend month by month).
            // BUT if the payment triggers a new gratuity match, we will extend.  E.g., pay $25 in April gets you to the following April.  But pay another $25
            // in June and we'll extend you to the april after that.
            DateTime dtNew = (p.Amount >= GratuityEarned.Threshold) ? dtFromPayment.LaterDate(dtFromExistingExpiration) : dtFromPayment;
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
            Boolean f = false;
            string szSessionKey = SessionKeyForUser(szUser, gt);
            System.Web.SessionState.HttpSessionState sess = ((HttpContext.Current != null) ? HttpContext.Current.Session : null);
            if (sess != null)
            {
                object o = sess[szSessionKey];
                if (o != null)
                    return Convert.ToBoolean(o, CultureInfo.InvariantCulture);
            }
            List<EarnedGrauity> lst = EarnedGrauity.GratuitiesForUser(szUser, gt);
            if (lst.Count == 0)
                f = false;
            else
                f = lst[0].ExpirationDate.CompareTo(DateTime.Now) > 0;
            if (sess != null)
                sess[szSessionKey] = f.ToString();
            return f;
        }

        private static void ResetSessionForGratuities(string szUser)
        {
            System.Web.SessionState.HttpSessionState sess = ((HttpContext.Current != null) ? HttpContext.Current.Session : null);
            if (sess == null)
                return;

            foreach (Gratuity.GratuityTypes gt in (Gratuity.GratuityTypes[]) Enum.GetValues(typeof(Gratuity.GratuityTypes)))
                sess.Remove(SessionKeyForUser(szUser, gt));
        }

        /// <summary>
        /// Deletes all gratuities for the specified user
        /// </summary>
        /// <param name="szUser">Username.  If null or empty, deletes all gratuities</param>
        public static void DeleteGratuitiesForUser(string szUser)
        {
            // Delete all gratuities for the specified user, or all gratuities for all users.
            DBHelper dbh = new DBHelper(String.Format("DELETE FROM earnedgratuities WHERE {0} AND idGratuityType > 0", String.IsNullOrEmpty(szUser) ? "username <> ''" : "username = ?user"));
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
                (dictPayments.ContainsKey(p.Username) ? dictPayments[p.Username] : (dictPayments[p.Username] = new Collection<Payment>())).Add(p);

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
        /// So if I pay $100 on June 1 2014, then $40 on Sept 1, I expire my 1-year gratuity on June 1 2016 (2 full years).
        /// But if I then get a refund of $25 on Sept 15, I now expire on June 1 2015.
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
            if (String.IsNullOrEmpty(szUser))
                return;

            Dictionary<string, Collection<Payment>> dictPayments = PaymentListsForUser(szUser);

            ReadOnlyCollection<Gratuity> lstKnownGratuities = Gratuity.KnownGratuities;
            List<EarnedGrauity> lstUpdatedGratuityList = new List<EarnedGrauity>();

            ResetSessionForGratuities(szUser);

            // Go through each user's payment history (could be just the one user) and determine their gratuities.
            foreach (string szKey in dictPayments.Keys)
            {
                List<Payment> lstPayments = new List<Payment>(dictPayments[szKey]);

                CompressPaymentsForUser(lstPayments);

                // OK, payments should be compacted - go through them and apply the gratuities
                foreach (Payment p in lstPayments)
                {
                    // Now process the payment: 
                    // Enumerate each known gratuity and, if this qualifies for the gratuity, create/extend the gratuity.
                    foreach (Gratuity g in lstKnownGratuities)
                    {
                        // see if this payment + priors qualifies
                        if (Payment.PaymentsInDateRange(p.Timestamp.Subtract(g.Window).Date, p.Timestamp.Date, lstPayments) >= g.Threshold)
                        {
                            // Find the existing gratuity, if any, for the user
                            // Add it to the list to update if we either have a new one, or if we have extended the epxiration
                            EarnedGrauity eg = lstUpdatedGratuityList.Find(eg2 => (eg2.GratuityType == g.GratuityType && eg2.Username == p.Username));
                            if (eg == null)
                            {
                                eg = new EarnedGrauity(g, p);
                                lstUpdatedGratuityList.Add(eg);
                            }
                            else
                                eg.ExtendExpiration(p, fResetReminders);    // don't extend expiration for new earned gratuities; correct date is already set above.
                            eg.EarnedDate = p.Timestamp.LaterDate(eg.EarnedDate);

                            g.GratuityWasEarned(eg);   // take any action that is appropriate for the gratuity now that it is earned.
                        }
                    }
                }
            }

            // We now have a list of gratuities that should be correct
            List<EarnedGrauity> lstExistingEarnedGratuities = EarnedGrauity.GratuitiesForUser(szUser, Gratuity.GratuityTypes.Unknown);

            foreach (EarnedGrauity eg in lstUpdatedGratuityList)
            {
                // if this exists as an existing earned gratuity, update the expiration date on that one and save it OR if reseting
                EarnedGrauity egExisting = lstExistingEarnedGratuities.Find(eg2 => (eg2.GratuityType == eg.GratuityType && eg2.Username == eg.Username));

                if (egExisting == null) // not found - this one is new
                    eg.Commit();
                else
                {
                    int prevReminderCount = egExisting.ReminderCount;
                    if (fResetReminders)
                        egExisting.ReminderCount = 0;
                    egExisting.ExpirationDate = eg.ExpirationDate;
                    // Only save to database if expiration date or reminders changed
                    if (eg.ReminderCount != egExisting.ReminderCount || egExisting.ReminderCount != prevReminderCount || eg.ExpirationDate.CompareTo(egExisting.ExpirationDate) != 0 || eg.EarnedDate.CompareTo(egExisting.EarnedDate) != 0)
                        egExisting.Commit();
                    lstExistingEarnedGratuities.Remove(egExisting);
                }
            }

            // Anything that is left is no longer found (e.g., if a payment was deleted)
            // Shouldn't happen, but clean up just in case
            foreach (EarnedGrauity eg in lstExistingEarnedGratuities)
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
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(new Uri(fSandbox ? strSandbox : strLive));

            //Set values for the request back
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            strRequest += "&cmd=_notify-validate";
            req.ContentLength = strRequest.Length;
            string strResponse = string.Empty;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // allows for validation of SSL conversations
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

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
    }
}