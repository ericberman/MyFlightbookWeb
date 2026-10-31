using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Payments;
using MyFlightbook.SharedUtility.EventRecorder;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MyFlightbook.Encryptors;
using static MyFlightbook.CloudStorage.BoxDrive;
using System.Collections.Specialized;
using System.Net.Http;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
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
    public partial class EmailSubscriptionManager
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

        public UInt32 SetSubscription(SubscriptionType st, bool value)
        {
            foreach (EmailSubscription es in this.Subscriptions)
            {
                if (es.Type == st)
                {
                    es.IsSubscribed = value;
                    break;
                }
            }
            return ToUint();
        }

        /// <summary>
        /// Issue #1406 - strip off the query in the exception for nightly backups.
        /// </summary>
        /// <param name="msg">The exception message.</param>
        /// <returns>Simple "MySQL error" if it's a MySQL error (i.e., doesn't include the actual query)</returns>
        private static string SanitizeExceptionMsg(string msg)
        {
            if (String.IsNullOrEmpty(msg))
                return string.Empty;
            int i = msg.IndexOf(" thrown in ReadRows");
            return i > 0 ? msg.Substring(0, i) : msg;
        }

        private async Task<bool> BackupDropbox(LogbookBackup lb, Profile pf, StringBuilder sb, StringBuilder sbFailures)
        {
            try
            {
                MFBDropbox.TokenStatus ts = new MFBDropbox(pf).ValidateDropboxToken();
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
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (System.UnauthorizedAccessException) {0}: {1}\r\n\r\n{2}\r\n\r\n", pf.UserName, ex.Message, ex.StackTrace);
                if (ex.InnerException != null)
                    sbFailures.AppendFormat(CultureInfo.CurrentCulture, "  Inner Exception (type {0}): {1}\r\n\r\n{2}\r\n\r\n", ex.InnerException.GetType().ToString(), ex.InnerException.Message ?? "(no message)", ex.InnerException.StackTrace ?? string.Empty);

                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, ex.Message, string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (MyFlightbookException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user (MyFlightbookException) {0}: {1}\r\n\r\n", pf.UserName, ex.Message);
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.DropboxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.DropboxFailure, pf.PreferredGreeting, SanitizeExceptionMsg(ex.Message), string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
            }
            catch (FileNotFoundException ex)
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Dropbox FAILED for user: FileNotFoundException, no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            catch (Exception ex) when (ex.IsNonFatal())
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
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.OneDriveFailure, pf.PreferredGreeting, SanitizeExceptionMsg(ex.Message), string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
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
            catch (Exception ex) when (ex.IsNonFatal())
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "OneDrive FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n{3}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
            return true;
        }

        private async Task<bool> BackupBox(LogbookBackup lb, Profile pf, StringBuilder sb, StringBuilder sbFailures)
        {
            string result = string.Empty;
            try
            {
                result = await lb.BackupBox(true).ConfigureAwait(false);
                sb.AppendFormat(CultureInfo.CurrentCulture, "Box: user {0} Logbooked backed up for user {0}", pf.UserName);
            }
            catch (BoxResponseException ex)
            {
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.BoxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.BoxFailure, pf.PreferredGreeting, ex.BoxError?.message ?? ex.Message), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Box FAILED for user {0}: {1} {2}\r\n\r\n{3}\r\n\r\n", pf.UserName, ex.BoxError?.message ?? ex.Message, ex.BoxError.code ?? "(no code)", ex.BoxError?.status ?? "(no status)");
            }
            catch (MyFlightbookException ex)
            {
                util.NotifyUser(Branding.ReBrand(Resources.EmailTemplates.BoxFailureSubject, ActiveBrand),
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.BoxFailure, pf.PreferredGreeting, SanitizeExceptionMsg(ex.Message), string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Box FAILED for user: {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Box FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n{3}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
            return String.IsNullOrEmpty(result);
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
                    Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.GoogleDriveFailure, pf.PreferredGreeting, SanitizeExceptionMsg(ex.Message), string.Empty), ActiveBrand), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
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
            catch (Exception ex) when (ex.IsNonFatal())
            {
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "GoogleDrive FAILED for user (Unknown Exception), no notification sent {0}: {1} {2}\r\n\r\n", pf.UserName, ex.GetType().ToString(), ex.Message);
            }
            return true;
        }

        /// <summary>
        /// Sends mail for the specified user, using the specified subject.  Returns true if it's able to get the data to send.
        /// </summary>
        /// <param name="pf">The user to whom the mail should be sent</param>
        /// <param name="szSubject">The subject line of the mail</param>
        /// <param name="szParam">Additional parameter to pass</param>
        /// <returns>True if the mail is successfully sent</returns>
        private async Task<bool> SendMailForUser(Profile pf, string szSubject, string szParam)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["k"] = new AdminAuthEncryptor().Encrypt(DateTime.Now.ToString("s", CultureInfo.InvariantCulture));
            nvc["u"] = pf.UserName;
            nvc["p"] = szParam;

            string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?{2}", ActiveBrand.HostName, "~/mvc/flights/NightlyEmail".ToAbsolute(), nvc.ToString());

            string szResult = string.Empty;
            return (bool)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(szURL), null, HttpMethod.Get, (response) =>
            {
                try
                {
                    szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    if (szResult.Contains("-- SuccessToken --"))
                    {
                        util.NotifyUser(szSubject, szResult, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true, String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?u={2}", Branding.CurrentBrand.HostName, "~/mvc/pub/unsubscribe".ToAbsolute(), HttpUtility.UrlEncode(new UserAccessEncryptor().Encrypt(pf.UserName))));
                        EventRecorder.LogCall($"SendMailForUser - sent for {pf.UserName}");
                        return true;
                    }
                    return !String.IsNullOrEmpty(szResult);
                }
                catch (Exception ex) when (ex.IsNonFatal())  // EAT ANY ERRORS so that we don't skip subsequent users.  NotifyUser shouldn't cause any, though.
                {
                    EventRecorder.LogCall($"SendMailForUser - FAILURE for {pf.UserName} - {ex.Message}");
                    return false;
                }
            });
        }
    }
}