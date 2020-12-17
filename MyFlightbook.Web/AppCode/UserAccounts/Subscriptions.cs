using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Payments;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Subscriptions
{
    /// <summary>
    /// Possible email subscriptions
    /// </summary>
    public enum SubscriptionType { Currency, Totals, MonthlyTotals, Expiration };

    /// <summary>
    /// An individual subscription.
    /// </summary>
    public class EmailSubscription
    {
        public SubscriptionType Type { get; set; }
        public string Name { get; set; }
        public bool IsSubscribed { get; set; }

        public EmailSubscription(SubscriptionType type, string name, bool fSubscribed)
        {
            Type = type;
            Name = name;
            IsSubscribed = fSubscribed;
        }

        /// <summary>
        /// Get the localized name for a subscription type.
        /// </summary>
        /// <param name="st">The subscription type</param>
        /// <returns>The name</returns>
        public static string NameForType(SubscriptionType st)
        {
            switch (st)
            {
                case SubscriptionType.Currency:
                    return Resources.Profile.EmailCurrencyName;
                case SubscriptionType.Totals:
                    return Resources.Profile.EmailTotalsName;
                case SubscriptionType.MonthlyTotals:
                    return Resources.Profile.EmailMonthlyName;
                case SubscriptionType.Expiration:
                    return Resources.Profile.EmailCurrencyExpiration;
                default:
                    return String.Empty;
            }
        }

        /// <summary>
        /// Returns a bitmask for each subscription type
        /// </summary>
        /// <param name="st">The subscription type</param>
        /// <returns>A bit flag for it</returns>
        public static UInt32 FlagForType(SubscriptionType st)
        {
            return (0x00000001U << (int)st);
        }

        /// <summary>
        /// Return the bitflag for this type
        /// </summary>
        public UInt32 Flag
        {
            get { return FlagForType(this.Type); }
        }

        /// <summary>
        /// Get a list of all available subscriptions
        /// </summary>
        /// <returns>The list</returns>
        public static IEnumerable<EmailSubscription> AvailableSubscriptions()
        {
            List<EmailSubscription> l = new List<EmailSubscription>();
            foreach (SubscriptionType st in Enum.GetValues(typeof(SubscriptionType)))
                l.Add(new EmailSubscription(st, NameForType(st), false));
            return l;
        }
    }

    /// <summary>
    /// Class for managing email subscriptions
    /// </summary>
    public class EmailSubscriptionManager
    {
        #region Properties
        /// <summary>
        /// The subscriptions for the user
        /// </summary>
        public IEnumerable<EmailSubscription> Subscriptions { get; set; }

        /// <summary>
        /// The flags indicating the subscriptions for the user.
        /// </summary>
        public UInt32 SubscriptionFlags { get; set; }

        /// <summary>
        /// The active brand.
        /// </summary>
        public Brand ActiveBrand { get; set; }

        public enum SelectedTasks { All, EmailOnly, CloudStorageOnly }

        /// <summary>
        /// Which tasks to run?
        /// </summary>
        public SelectedTasks TasksToRun { get; set; }

        /// <summary>
        /// If specified, removes all but the specified user - safer debugging (avoids accidental spam!)
        /// </summary>
        public string UserRestriction { get; set; }
        #endregion

        #region constructors
        public EmailSubscriptionManager()
        {
            TasksToRun = SelectedTasks.All;
        }

        public EmailSubscriptionManager(UInt32 u) : this()
        {
            FromUInt(u);
        }
        #endregion

        /// <summary>
        /// Initializes the subscriptions from a bit-field of subscriptions
        /// </summary>
        /// <param name="u">The bit field</param>
        /// <returns>The array of subscriptions (which is also initialized in the Subscriptions property)</returns>
        public IEnumerable<EmailSubscription> FromUInt(UInt32 u)
        {
            SubscriptionFlags = u;
            Subscriptions = EmailSubscription.AvailableSubscriptions();
            foreach (EmailSubscription es in Subscriptions)
                es.IsSubscribed = ((u & es.Flag) != 0);

            return this.Subscriptions;
        }

        /// <summary>
        /// Packs the set of subscriptions into a bit-field of subscriptions for storage in the database
        /// </summary>
        /// <returns></returns>
        public UInt32 ToUint()
        {
            UInt32 u = 0x00000000;
            foreach (EmailSubscription es in this.Subscriptions)
                if (es.IsSubscribed)
                    u |= es.Flag;
            SubscriptionFlags = u;
            return u;
        }

        /// <summary>
        /// Determines if the user has the specified subscription
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public bool HasSubscription(SubscriptionType st)
        {
            return (SubscriptionFlags & EmailSubscription.FlagForType(st)) != 0;
        }

        public void SetSubscription(SubscriptionType st, bool value)
        {
            foreach (EmailSubscription es in this.Subscriptions)
            {
                if (es.Type == st)
                {
                    es.IsSubscribed = value;
                    break;
                }
            }
            ToUint();
        }

        /// <summary>
        /// Sends the nightly/monthly emails for users that have requested it.
        /// </summary>
        private void SendNightlyEmails()
        {
            // Find all users who qualify for expiring currency notifications - they may trigger an early email.
            List<EarnedGratuity> lstUsersWithExpiringCurrencies = EarnedGratuity.GratuitiesForUser(null, Gratuity.GratuityTypes.CurrencyAlerts);
            if (!String.IsNullOrEmpty(UserRestriction))
                lstUsersWithExpiringCurrencies.RemoveAll(eg => eg.Username.CompareCurrentCultureIgnoreCase(UserRestriction) != 0);

            // And the list of people who have a subscription to expiring currencies
            List<Profile> lstUsersSubscribedForExpiration = new List<Profile>(Profile.UsersWithSubscriptions(EmailSubscription.FlagForType(SubscriptionType.Expiration), DateTime.Now.AddDays(-7)));

            // get the list of people who have a subscription OTHER than simple monthly or expiration
            List<Profile> lstUsersToSend = new List<Profile>(Profile.UsersWithSubscriptions(~(EmailSubscription.FlagForType(SubscriptionType.MonthlyTotals) | EmailSubscription.FlagForType(SubscriptionType.Expiration)), DateTime.Now.AddDays(-7)));
            if (!String.IsNullOrEmpty(UserRestriction))
                lstUsersToSend.RemoveAll(pf => pf.UserName.CompareOrdinalIgnoreCase(UserRestriction) != 0);

            // See who has expiring currencies that require notification.
            foreach (EarnedGratuity eg in lstUsersWithExpiringCurrencies)
            {
                // Skip over anybody that's not also subscribed for expiration notices
                Profile pf = lstUsersSubscribedForExpiration.Find(profile => profile.UserName.CompareCurrentCultureIgnoreCase(eg.Username) == 0);
                if (pf == null)
                    continue;

                IEnumerable<CurrencyStatusItem> expiringCurrencies = CurrencyStatusItem.CheckForExpiringCurrencies(eg.State, eg.Username, out string newState);
                if (newState.CompareOrdinal(eg.State) != 0)
                {
                    eg.State = newState;
                    eg.Commit();
                }
                if (expiringCurrencies.Any())
                {
                    if (SendMailForUser(pf, Resources.Profile.EmailCurrencyExpiringMailSubject, string.Empty))
                    {
                        // Don't send the weekly mail, since we just pre-empted it.
                        lstUsersToSend.RemoveAll(p => pf.UserName.CompareCurrentCultureIgnoreCase(eg.Username) == 0);
                        pf.LastEmailDate = DateTime.Now;
                        pf.FCommit();
                    }
                }
            }

            foreach (Profile pf in lstUsersToSend)
            {
                if (SendMailForUser(pf, Resources.Profile.EmailWeeklyMailSubject, String.Empty))
                {
                    pf.LastEmailDate = DateTime.Now;
                    pf.FCommit();
                }
            }

            // Now do the monthly/annual emails
            if (DateTime.Now.Day == 1)
            {
                lstUsersToSend = new List<Profile>(Profile.UsersWithSubscriptions(EmailSubscription.FlagForType(SubscriptionType.MonthlyTotals), DateTime.Now.AddDays(1)));

                // We don't update the last-email sent on this because this email is asynchronous - i.e., not dependent on any other mail that was sent.
                foreach (Profile pf in lstUsersToSend)
                    SendMailForUser(pf, String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailMonthlySubject, DateTime.Now.AddMonths(-1).ToString("MMMM", CultureInfo.InvariantCulture)), "monthly");
            }
        }

        private async Task<bool> BackupDropbox(LogbookBackup lb, Profile pf, StringBuilder sb, StringBuilder sbFailures)
        {
            try
            {
                MFBDropbox.TokenStatus ts = await new MFBDropbox(pf).ValidateDropboxToken().ConfigureAwait(false);
                if (ts == MFBDropbox.TokenStatus.None)
                    return true;

                System.Threading.Thread.Sleep(1);
                Dropbox.Api.Files.FileMetadata result = null;
                result =await lb.BackupToDropbox(Branding.CurrentBrand).ConfigureAwait(false);
                sb.AppendFormat(CultureInfo.CurrentCulture, "Dropbox: user {0} ", pf.UserName);
                if (ts == MFBDropbox.TokenStatus.oAuth1)
                    sb.Append("Token UPDATED from oauth1! ");
                sb.AppendFormat(CultureInfo.CurrentCulture, "Logbook backed up for user {0}...", pf.UserName);
                System.Threading.Thread.Sleep(1);
                result = await lb.BackupImagesToDropbox(Branding.CurrentBrand).ConfigureAwait(false);
                System.Threading.Thread.Sleep(1);
                sb.AppendFormat(CultureInfo.CurrentCulture, "and images backed up for user {0}.\r\n \r\n", pf.UserName);
            }
            catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                string szMessage = (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath != null && ex.ErrorResponse.AsPath.Value.Reason.IsInsufficientSpace) ? Resources.LocalizedText.DropboxErrorOutOfSpace : ex.Message;
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, szMessage, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (Dropbox.Api.RateLimitException ex)
            {
                // Should try again, but for now just ignore until tomorrow
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, Resources.LocalizedText.DropboxErrorTooManyRequests, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);

            }
            catch (Dropbox.Api.ApiException<Dropbox.Api.Auth.TokenFromOAuth1Error> ex)
            {
                // De-register dropbox.
                pf.DropboxAccessToken = string.Empty;
                pf.FCommit();
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (TokenFromOAuth1Error, token removed) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, Resources.LocalizedText.DropboxErrorDeAuthorized), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (Dropbox.Api.AuthException ex)
            {
                // De-register dropbox.
                pf.DropboxAccessToken = string.Empty;
                pf.FCommit();
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (AuthException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, Resources.LocalizedText.DropboxErrorDeAuthorized), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (Dropbox.Api.BadInputException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (BadInputException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
            }
            catch (Dropbox.Api.HttpException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (HttpException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (Dropbox.Api.AccessException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (AccessException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (Dropbox.Api.DropboxException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (Base dropbox exception) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                // De-register dropbox.
                pf.DropboxAccessToken = string.Empty;
                pf.FCommit();
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (UnauthorizedAccess) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, Resources.LocalizedText.DropboxErrorDeAuthorized), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (MyFlightbookException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (MyFlightbookException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user: FileNotFoundException, no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n{3}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message, ex.StackTrace);
                if (ex.InnerException != null)
                    sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Inner exception: {0}\r\n{1}", ex.InnerException.Message, ex.InnerException.StackTrace);
            }
            return true;
        }

        private async Task<bool> BackupOneDrive(LogbookBackup lb, Profile pf, StringBuilder sb, StringBuilder sbFailures)
        {
            try
            {
                if (pf.OneDriveAccessToken == null)
                    throw new UnauthorizedAccessException();

                OneDrive od = new OneDrive(pf);
                if (await lb.BackupToOneDrive(od).ConfigureAwait(false))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "OneDrive: user {0} Logbook backed up for user {0}...", pf.UserName);

                System.Threading.Thread.Sleep(0);
                if (await lb.BackupImagesToOneDrive(od, Branding.CurrentBrand).ConfigureAwait(false))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "and images backed up for user {0}.\r\n \r\n", pf.UserName);

                System.Threading.Thread.Sleep(0);
                // if we are here we were successful, so save the updated refresh token
                pf.OneDriveAccessToken = od.AuthState;
                pf.FCommit();
            }
            catch (Exception ex) when (ex is OneDriveMFBException || ex is MyFlightbookException)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "OneDrive FAILED for user (OneDriveException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message + " " + ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.OneDriveFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.OneDriveFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                // De-register oneDrive.
                pf.OneDriveAccessToken = null;
                pf.FCommit();
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "OneDrive FAILED for user (UnauthorizedAccess) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.OneDriveFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.OneDriveFailure, pf.PreferredGreeting, ex.Message, Resources.LocalizedText.DropboxErrorDeAuthorized), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "OneDrive FAILED for user: FileNotFoundException, no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "OneDrive FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n{3}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
            return true;
        }

        private async Task<bool> BackupGoogleDrive(LogbookBackup lb, Profile pf, StringBuilder sb, StringBuilder sbFailures)
        {
            try
            {
                if (pf.GoogleDriveAccessToken == null)
                    throw new UnauthorizedAccessException();

                GoogleDrive gd = new GoogleDrive(pf);

                sb.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive: user {0} ", pf.UserName);
                GoogleDriveResultDictionary meta = await lb.BackupToGoogleDrive(gd, Branding.CurrentBrand).ConfigureAwait(false);
                if (meta != null)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Logbook backed up for user {0}...", pf.UserName);
                System.Threading.Thread.Sleep(0);
                meta = await lb.BackupImagesToGoogleDrive(gd, Branding.CurrentBrand).ConfigureAwait(false);
                System.Threading.Thread.Sleep(0);
                if (meta != null)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "and images backed up for user {0}.\r\n \r\n", pf.UserName);

                // if we are here we were successful, so save the updated refresh token
                pf.GoogleDriveAccessToken = gd.AuthState;
                pf.FCommit();
            }
            catch (MyFlightbookException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive FAILED for user (MyFlightbookException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.GoogleDriveFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.GoogleDriveFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (UnauthorizedAccessException ex)
            {
                // De-register GoogleDrive.
                pf.GoogleDriveAccessToken = null;
                pf.FCommit();
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive FAILED for user (UnauthorizedAccess) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.GoogleDriveFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.GoogleDriveFailure, pf.PreferredGreeting, ex.Message, Resources.LocalizedText.DropboxErrorDeAuthorized), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive FAILED for user: FileNotFoundException, no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            return true;
        }

        private async Task<bool> BackupToCloud()
        {
            string szBackups = Path.Combine(Path.GetTempPath(), String.Format(CultureInfo.InvariantCulture, "{0}-CloudBackup.txt", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            string szBackupFailures = Path.Combine(Path.GetTempPath(), String.Format(CultureInfo.InvariantCulture, "{0}-CloudBackup-errors.txt", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            using (StreamWriter swSuccess = new StreamWriter(szBackups, false, Encoding.UTF8))
            using (StreamWriter swFailure = new StreamWriter(szBackupFailures, false, Encoding.UTF8))
            {
                List<EarnedGratuity> lstUsersWithCloudBackup = EarnedGratuity.GratuitiesForUser(string.Empty, Gratuity.GratuityTypes.CloudBackup);

                if (!String.IsNullOrEmpty(UserRestriction))
                    lstUsersWithCloudBackup.RemoveAll(eg => eg.Username.CompareCurrentCultureIgnoreCase(UserRestriction) != 0);

                foreach (EarnedGratuity eg in lstUsersWithCloudBackup)
                {
                    StringBuilder sb = new StringBuilder();
                    StringBuilder sbFailures = new StringBuilder();

                    StorageID sid = StorageID.None;
                    if (eg.UserProfile != null && ((sid = eg.UserProfile.BestCloudStorage) != StorageID.None) && eg.CurrentStatus != EarnedGratuity.EarnedGratuityStatus.Expired)
                    {
                        try
                        {
                            Profile pf = eg.UserProfile;
                            LogbookBackup lb = new LogbookBackup(pf);

                            switch (sid)
                            {
                                case StorageID.Dropbox:
                                    await BackupDropbox(lb, pf, sb, sbFailures).ConfigureAwait(false);
                                    break;
                                case StorageID.OneDrive:
                                    await BackupOneDrive(lb, pf, sb, sbFailures).ConfigureAwait(false);
                                    break;
                                case StorageID.GoogleDrive:
                                    await BackupGoogleDrive(lb, pf, sb, sbFailures).ConfigureAwait(false);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex) when (!(ex is OutOfMemoryException))
                        {
                            string szError = String.Format(CultureInfo.CurrentCulture, "eg user={0}{1}\r\n\r\n{2}\r\n\r\n{3}", eg == null ? "NULL eg!" : eg.Username, (eg != null && eg.UserProfile == null) ? " NULL PROFILE" : string.Empty, sbFailures.ToString(), sb.ToString());
                            util.NotifyAdminException("ERROR running nightly backup", new MyFlightbookException(szError, ex));
                        }
                        finally
                        {
                            // Regardless of what happens, write this to the file (in case something dies).
                            swSuccess.WriteLine(sb.ToString());
                            swFailure.WriteLine(sbFailures.ToString());
                        }
                    }
                }
            }

            using (StreamReader srSuccess = new StreamReader(szBackups, Encoding.UTF8)) 
            using (StreamReader srFailure = new StreamReader(szBackupFailures, Encoding.UTF8)) {
                util.NotifyAdminEvent("Cloud Backup report", srFailure.ReadToEnd() + srSuccess.ReadToEnd(), ProfileRoles.maskCanReport);
            }

            // if we are here, we should be able to delete the temp files
            try
            {
                File.Delete(szBackups);
                File.Delete(szBackupFailures);
            }
            finally {  }

            return true;
        }

        /// <summary>
        /// Batch job to send all of the nightly/monthly email.
        /// </summary>
        public async void NightlyRun()
        {
            if (ActiveBrand == null)
                ActiveBrand = Branding.CurrentBrand;
            if (TasksToRun == SelectedTasks.All || TasksToRun == SelectedTasks.EmailOnly)
                SendNightlyEmails();

            // Do any Cloud Storage updates
            if (TasksToRun == SelectedTasks.All || TasksToRun == SelectedTasks.CloudStorageOnly)
                await BackupToCloud().ConfigureAwait(false);    // we have no httpcontext.current, so no need to save

            // Send out any notices of pending gratuity expirations
            EarnedGratuity.SendRemindersOfExpiringGratuities(UserRestriction);

            if (DateTime.Now.Day == 1)
                Clubs.Club.SendMonthlyClubReports();
        }

        /// <summary>
        /// Sends mail for the specified user, using the specified subject.  Returns true if it's able to get the data to send.
        /// </summary>
        /// <param name="pf">The user to whom the mail should be sent</param>
        /// <param name="szSubject">The subject line of the mail</param>
        /// <param name="szParam">Additional parameter to pass</param>
        /// <returns>True if the mail is successfully sent</returns>
        private bool SendMailForUser(Profile pf, string szSubject, string szParam)
        {
            Encryptors.AdminAuthEncryptor enc = new Encryptors.AdminAuthEncryptor();

            String szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?k={2}&u={3}&p={4}", ActiveBrand.HostName, VirtualPathUtility.ToAbsolute("~/public/TotalsAndcurrencyEmail.aspx"), HttpUtility.UrlEncode(enc.Encrypt(DateTime.Now.ToString("s", CultureInfo.InvariantCulture))), HttpUtility.UrlEncode(pf.UserName), HttpUtility.UrlEncode(szParam));
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szContent = System.Text.UTF8Encoding.UTF8.GetString(rgdata);
                    if (szContent.Contains("-- SuccessToken --"))
                    {
                        util.NotifyUser(szSubject, System.Text.UTF8Encoding.UTF8.GetString(rgdata), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is MyFlightbookException) { }    // EAT ANY ERRORS so that we don't skip subsequent users.  NotifyUser shouldn't cause any, though.
            return false;
        }
    }
}