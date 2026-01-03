using MyFlightbook.Airports;
using MyFlightbook.Currency;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Lint;
using MyFlightbook.Templates;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.ServiceModel;
using System.Web;
using System.Web.Security;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public enum AuthStatus { Failed, TwoFactorCodeRequired, Success }

    [Serializable]
    public class AuthResult
    {
        public AuthStatus Result { get; set; } = AuthStatus.Failed;
        public string AuthToken { get; set; }

        public AuthResult() { }

        public AuthResult(string authToken)
        {
            AuthToken = authToken;
        }

        public static AuthResult ProcessResult(string authToken, string szUser, string sz2FactorAuth, string szEmulator)
        {
            AuthResult result = new AuthResult();
            if (String.IsNullOrEmpty(authToken) || String.IsNullOrEmpty(szUser))
                return result;

            // Issue #654: Check for 2factor requirement on the user's account UNLESS emulating, in which case check for 2fa on the *emulator's* account.
            Profile pf = Profile.GetUser(String.IsNullOrEmpty(szEmulator) ? szUser : szEmulator);
            if (String.IsNullOrEmpty(pf.UserName))
                return result;

            if (pf.PreferenceExists(MFBConstants.keyTFASettings))
            {
                if (String.IsNullOrEmpty(sz2FactorAuth))
                {
                    result.Result = AuthStatus.TwoFactorCodeRequired;
                    return result;
                }

                Google.Authenticator.TwoFactorAuthenticator tfa = new Google.Authenticator.TwoFactorAuthenticator();
                if (!tfa.ValidateTwoFactorPIN(pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string, sz2FactorAuth ?? string.Empty, new TimeSpan(0, 2, 0)))
                {
                    System.Threading.Thread.Sleep(1000); // pause for a second to thwart dictionary attacks.
                    throw new AuthenticationException(Resources.Profile.TFACodeFailed);
                }
            }

            result.AuthToken = authToken;
            result.Result = AuthStatus.Success;
            return result;
        }
    }

    public class PostingOptions
    {
        /// <summary>
        /// Post the flight to Facebook?
        /// </summary>
        [Obsolete("Posting via oAuth to Facebook is no longer supported")]
        public Boolean PostToFacebook { get; set; }

        /// <summary>
        /// Post to Twitter?
        /// </summary>
        [Obsolete("Posting via oAuth to Twitter is no longer supported")]
        public Boolean PostToTwitter { get; set; }

        /// <summary>
        /// Creates a new PostingOptions object
        /// </summary>
        public PostingOptions()
        {
        }

        /// <summary>
        /// Creates a new PostingOptions object
        /// </summary>
        /// <param name="fTwitter">Post to Twitter?</param>
        /// <param name="fFacebook">Post to Facebook?</param>
        [Obsolete("Posting via oAuth to Facebook/Twitter is no longer supported")]
        public PostingOptions(Boolean fTwitter, Boolean fFacebook)
        {
            PostToFacebook = fFacebook;
            PostToTwitter = fTwitter;
        }
    }

    public static class EventRecorder
    {
        public enum MFBEventID { None, AuthUser = 1, GetAircraft, FlightsByDate, CommitFlightDEPRECATED, CreateAircraft, CreateUser, CreateUserAttemptDEPRECATED, CreateUserError, ExpiredToken, ObsoleteAPI };
        public enum MFBCountID { WSFlight, ImportedFlight };

        private static readonly ILogger wslogger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(System.IO.Path.GetTempPath() + "logs/MyFlightbook.log", formatProvider:CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, encoding: System.Text.Encoding.UTF8).CreateLogger();

        public static void LogCall(string sz, params object[] list)
        {
            if (util.RequestContext.CurrentRequestUrl != null)
            {
                List<object> lstNewArgs = new List<object>() { util.RequestContext.CurrentRequestHostAddress, util.RequestContext.CurrentRequestUserAgent };
                lstNewArgs.AddRange(list);
                wslogger.Information("({ip}, {ua}) " + sz, lstNewArgs.ToArray());
            } 
            else
                wslogger.Information(sz, list);
        }

        public static void WriteEvent(MFBEventID eventID, string szUser, string szDescription)
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery("INSERT INTO wsevents SET Date=?date, eventType=?eventType, user=?user, description=?description",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("eventType", eventID);
                    comm.Parameters.AddWithValue("user", szUser);
                    comm.Parameters.AddWithValue("description", szDescription.LimitTo(126));
                    comm.Parameters.AddWithValue("date", DateTime.Now);
                });
        }

        public static void UpdateCount(MFBCountID id, int value)
        {
            string szQTemplate = "UPDATE EventCounts SET {0}={0}+{1} WHERE id=1";
            string szField = "";

            switch (id)
            {
                case MFBCountID.WSFlight:
                    szField = "WSCommittedFlights";
                    break;
                case MFBCountID.ImportedFlight:
                    szField = "ImportedFlights";
                    break;
                default:
                    break;
            }

            new DBHelper(String.Format(CultureInfo.InvariantCulture, szQTemplate, szField, value)).DoNonQuery();
        }

        /// <summary>
        /// Removes old webservice items of the specified events.
        /// </summary>
        /// <param name="eventID"></param>
        /// <exception cref="MyFlightbookException"></exception>
        public static int ADMINTrimOldItems(MFBEventID eventID)
        {
            // below is too slow
            // string szDelete = String.Format("DELETE w1 FROM wsevents w1 JOIN wsevents w2 ON (w1.eventType=w2.eventType AND w1.user=w2.user AND w1.eventID < w2.eventID) WHERE w1.eventType={0}", (int)eventID);
            Hashtable htUsers = new Hashtable();
            List<int> lstRowsToDelete = new List<int>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM wsevents w WHERE w.eventType={0} ORDER BY w.Date DESC", (int)eventID));
            if (!dbh.ReadRows(
                (comm) => { comm.CommandTimeout = 120; },
                (dr) =>
                {
                    string szUser = dr["user"].ToString();
                    DateTime dtLastEntered = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
                    if (htUsers[szUser] == null)
                        htUsers[szUser] = dtLastEntered;
                    else
                        lstRowsToDelete.Add(Convert.ToInt32(dr["eventID"], CultureInfo.InvariantCulture));
                }))
                throw new MyFlightbookException("Error trimming data: " + dbh.LastError);

            if (lstRowsToDelete.Any())
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM wsevents WHERE eventID IN ({0})", String.Join(",", lstRowsToDelete));
                dbh.CommandArgs.Timeout = 300;
                dbh.DoNonQuery();
            }
            return lstRowsToDelete.Count;
        }
    }

    /// <summary>
    /// The main SOAP service for mobile use
    /// NOTE: iPhone sends HTML entities URL encoded, so we need to decode them on receipt.  Bleah.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    public class MFBWebService : WebService
    {
        private const long SecondsPerDay = (24 * 3600);
        private const long TicksPerSecond = (1000 * 10000);

        private static string[] rgszAuthorizedService;

        private static string[] AuthorizedServices
        {
            get
            {
                if (rgszAuthorizedService == null)
                    rgszAuthorizedService = LocalConfig.SettingForKey("AuthorizedWebServiceClients").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return rgszAuthorizedService;
            }
        }

        /// <summary>
        /// Determines if the specified app token is authorized to use the web service.
        /// </summary>
        /// <param name="szAppToken"></param>
        /// <returns></returns>
        public static Boolean IsAuthorizedService(string szAppToken)
        {
            if (String.IsNullOrEmpty(szAppToken))
                return false;

            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);

            foreach (string sz in AuthorizedServices)
                if (sz.CompareCurrentCultureIgnoreCase(szAppToken) == 0)
                    return true;
            return false;
        }

        public static string GetEncryptedUser(string szAuthUserToken)
        {
            Encryptors.WebServiceEncryptor e = new Encryptors.WebServiceEncryptor();

            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);

            if (szAuthUserToken == null || szAuthUserToken.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadSignIn);

            string sz = e.Decrypt(szAuthUserToken);

            if (sz.IndexOf(";", StringComparison.Ordinal) > 0)
            {
                string[] rgsz = sz.Split(';');
                // TODO: look at the 2nd part of the string, see if things have expired.
                // it's ticks.
                if (rgsz.Length < 2)
                    throw new MyFlightbookException(Resources.WebService.errBadSignIn);
                Int64 ticks = Convert.ToInt64(rgsz[1], CultureInfo.InvariantCulture);
                Int64 elapsedTicks = DateTime.Now.Ticks - ticks;
                Int64 ticksPerDay = SecondsPerDay * TicksPerSecond;
                int elapsedTicksDays = (int)(elapsedTicks / ticksPerDay);

                // see if this is older than 4 weeks
                if (elapsedTicksDays > 14)
                    EventRecorder.WriteEvent(EventRecorder.MFBEventID.ExpiredToken, rgsz[0], String.Format(CultureInfo.InvariantCulture, "Expired token for user (2-week expiration): {0} days", elapsedTicksDays.ToString(CultureInfo.InvariantCulture)));

                string szUser = rgsz[0];

                // Issue #920: force new sign-in if authtoken was issued prior to last password change.
                // Validate the user and if this was issued before the last password change, don't allow it.
                Profile pf = Profile.GetUser(szUser);
                if (pf.LastPasswordChange.Ticks > ticks)
                    throw new MyFlightbookException(Resources.Profile.AuthTokenExpiredPassword);
                return szUser;
            }
            else
            {
                EventRecorder.LogCall("Web Service: GetEncryptedUser failed for token {token}", szAuthUserToken);
                return string.Empty;
            }
        }

        public MFBWebService() { }

        /// <summary>
        /// Returns a list of the aircraft that the specified user flies, including the tailnumber, summary, and aircraft ID.
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <returns>An array of aircraft, null if auth fails</returns>
        [WebMethod]
        public Aircraft[] AircraftForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.LogCall("Web Service: Aircraft for User: {user}", szUser);
            if (szUser.Length > 0)
            {
                // EventRecorder.WriteEvent(EventRecorder.MFBEventID.GetAircraft, szUser, "Aircraft for user");
                UserAircraft ua = new UserAircraft(szUser);
                IEnumerable<Aircraft> rgAc = ua.GetAircraftForUser();
                if (rgAc == null)
                    rgAc = Array.Empty<Aircraft>(); // return an empty array rather than null
                else
                    foreach (Aircraft ac in rgAc)
                    {
                        ac.PopulateImages();
                        ac.LastAltimeter = DateTime.SpecifyKind(ac.LastAltimeter, DateTimeKind.Utc);
                        ac.LastVOR = DateTime.SpecifyKind(ac.LastVOR, DateTimeKind.Utc);
                        ac.LastStatic = DateTime.SpecifyKind(ac.LastStatic, DateTimeKind.Utc);
                        ac.LastTransponder = DateTime.SpecifyKind(ac.LastTransponder, DateTimeKind.Utc);
                        ac.LastAnnual = DateTime.SpecifyKind(ac.LastAnnual, DateTimeKind.Utc);
                        ac.LastELT = DateTime.SpecifyKind(ac.LastELT, DateTimeKind.Utc);

                        // Hack - pass down a human readable tailnumber that still has the anon prefix.
                        if (ac.IsAnonymous)
                            ac.TailNumber = ac.HackDisplayTailnumber;
                    }
                return rgAc.ToArray();
            }
            else
                return null;
        }

        /// <summary>
        /// Adds the specified aircraft to the database and to the user's list of aircraft.  If the tailnumber already exists, that aircraft is re-used and
        /// added to the user's list.
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <param name="szTail">Tailnumber for the aircraft</param>
        /// <param name="idModel">Model ID</param>
        /// <param name="idInstanceType">Instance type (real airplane, etc.)</param>
        /// <returns>Null if there's a problem, else an updated list of aircraft for the user</returns>
        [WebMethod]
        public Aircraft[] AddAircraftForUser(string szAuthUserToken, string szTail, int idModel, int idInstanceType)
        {
            if (szAuthUserToken == null)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.LogCall("Web Service: AddAircraftForUser {tail}, model {model} for User: {user}", szTail, idModel, szUser);

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            if (String.IsNullOrEmpty(szTail))
                throw new MyFlightbookException(Resources.WebService.errBadTailNumber);

            MakeModel m = MakeModel.GetModel(idModel);
            if (m == null || m.MakeModelID == MakeModel.UnknownModel)
                throw new MyFlightbookException(Resources.WebService.errInvalidModel);

            if (idInstanceType > (int)AircraftInstanceTypes.MaxType || idInstanceType < (int)AircraftInstanceTypes.Mintype)
                throw new MyFlightbookException(Resources.WebService.errInvalidInstanceType);

            AircraftInstanceTypes ait = (AircraftInstanceTypes)idInstanceType;

            // Always auto-assign tails for non-real aircraft
            if (ait != AircraftInstanceTypes.RealAircraft)
                szTail = Aircraft.SuggestSims(idModel, ait).First().TailNumber; // we'll create the aircraft itself below.
            else if (szTail.StartsWith(CountryCodePrefix.szAnonPrefix, StringComparison.OrdinalIgnoreCase))
                // Fix up the anonymous tailnumber for an anonymous aircraft
                szTail = Aircraft.AnonymousTailnumberForModel(idModel);
            else
                // fix up hyphenation
                szTail = Aircraft.NormalizeTail(szTail, CountryCodePrefix.BestMatchCountryCode(szTail));

            Aircraft ac = new Aircraft()
            {
                TailNumber = szTail,
                InstanceTypeID = idInstanceType,
                ModelID = idModel
            };
            ac.Commit(szUser); // this will check for dupes and will add 
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateAircraft, szUser, String.Format(CultureInfo.InvariantCulture, "Create Aircraft - {0}", szTail));

            if (ac.AircraftID < 0)
                throw new MyFlightbookException(Resources.WebService.errNewAircraftFailed);

            return AircraftForUser(szAuthUserToken);
        }

        /// <summary>
        /// Returns a set of aircraft matching a prefix for a given user
        /// </summary>
        /// <param name="szAuthToken">User (helps prevent scraping)</param>
        /// <param name="szPrefix">Prefix.  Requires at least 3 characters</param>
        /// <returns>Matching aircraft</returns>
        [WebMethod]
        public Aircraft[] AircraftMatchingPrefix(string szAuthToken, string szPrefix)
        {
            if (string.IsNullOrWhiteSpace(szPrefix))
                return Array.Empty<Aircraft>();

            if (szAuthToken == null)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            string szUser = GetEncryptedUser(szAuthToken);

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            return Aircraft.AircraftWithPrefix(szPrefix).ToArray();
        }

        /// <summary>
        /// Updates the maintenance for an aircraft, performing appropriate logging.  Still called by windows phone
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="idAircraft">The aircraft to update</param>
        [WebMethod]
        public void UpdateMaintenanceForAircraft(string szAuthUserToken, Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            UpdateMaintenanceForAircraftWithFlagsInternal(szAuthUserToken, ac);
        }

        /// <summary>
        /// Synonym for UpdateMaintenanceForAircraftWithFlags, but also updates public/private notes
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="ac">The aircraft to update</param>
        [WebMethod]
        public void UpdateMaintenanceForAircraftWithFlagsAndNotes(string szAuthUserToken, Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            UpdateMaintenanceForAircraftWithFlagsInternal(szAuthUserToken, ac, true, true);
        }


        /// <summary>
        /// Updates maintenance for an aircraft, performing appropriate logging. Also udpates the aircraft's flags (exclude from list, etc.)
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="ac">The aircraft to update</param>
        /// <param name="fUpdateFlags">True if we want to update the flags for the user as well</param>
        /// <param name="fUpdateNotes">True to update the public/private notes</param>
        private static void UpdateMaintenanceForAircraftWithFlagsInternal(string szAuthUserToken, Aircraft ac, bool fUpdateFlags = false, bool fUpdateNotes = false)
        {
            if (szAuthUserToken == null)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            string szUser = GetEncryptedUser(szAuthUserToken);

            EventRecorder.LogCall("Web Service: UpdateMaintenanceForAircraftWithFlagsInternal aircraft: {tail} for User: {user}", ac.TailNumber, szUser);

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            if (ac.AircraftID <= 0)
                throw new MyFlightbookException(Resources.WebService.errAircraftDoesntExist);

            // fix for anonymous aircraft - iOS passes up bogus info
            if (ac.TailNumber.StartsWith(CountryCodePrefix.AnonymousCountry.Prefix, StringComparison.OrdinalIgnoreCase))
                ac.TailNumber = Aircraft.AnonymousTailnumberForModel(ac.ModelID);

            if (!ac.IsValid())
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.WebService.errInvalidAircraft, ac.ErrorString));

            // fix up any dates that aren't quite datetime.minvalue
            ac.LastAltimeter = DateTime.SpecifyKind(ac.LastAltimeter.NormalizeDate(), DateTimeKind.Utc);
            ac.LastAnnual = DateTime.SpecifyKind(ac.LastAnnual.NormalizeDate(), DateTimeKind.Utc);
            ac.LastELT = DateTime.SpecifyKind(ac.LastELT.NormalizeDate(), DateTimeKind.Utc);
            ac.LastStatic = DateTime.SpecifyKind(ac.LastStatic.NormalizeDate(), DateTimeKind.Utc);
            ac.LastTransponder = DateTime.SpecifyKind(ac.LastTransponder.NormalizeDate(), DateTimeKind.Utc);
            ac.LastVOR = DateTime.SpecifyKind(ac.LastVOR.NormalizeDate(), DateTimeKind.Utc);
            ac.RegistrationDue = DateTime.SpecifyKind(ac.RegistrationDue.NormalizeDate(), DateTimeKind.Utc);

            UserAircraft ua = new UserAircraft(szUser);

            ac.AircraftID = AircraftTombstone.MapAircraftID(ac.AircraftID);
            Aircraft acCheck = new Aircraft(ac.AircraftID);
            if (!ua.CheckAircraftForUser(acCheck))
                throw new MyFlightbookException(Resources.WebService.errNotYourAirplane);

            Aircraft acExisting = new Aircraft(ac.AircraftID);

            if (acExisting.AircraftID < 0)
                throw new MyFlightbookException(Resources.WebService.errAircraftNotFound);

            // Revision < 0 indicates an old app that isn't setting the Revision property - disallow and tell people to upgrade
            if (ac.Revision < 0)
                throw new MyFlightbookException(Resources.Aircraft.errNotEditingMostRecentVersionWebServiceUpgrade);

            // Otherwise, if we don't match the current revision, tell them they're out of date.
            if (acExisting.Revision != ac.Revision)
                throw new MyFlightbookException(Resources.Aircraft.errNotEditingMostRecentVersionWebService);

            if (fUpdateNotes)
            {
                acExisting.PrivateNotes = ac.PrivateNotes;
                acExisting.PublicNotes = ac.PublicNotes;
            }

            // Don't allow maintenace updates for anonymous aircraft, but do update private notes and flags
            if (!ac.IsAnonymous)
            {
                acExisting.UpdateMaintenanceForUser(ac.Maintenance, szUser);
                acExisting.Commit(szUser);
            }

            if (fUpdateFlags)
                ua.FAddAircraftForUser(ac);
        }

        /// <summary>
        /// Delete's a user's aircraft.  Throws an exception if there is a problem.
        /// </summary>
        /// <param name="szAuthUserToken">Auth token for the user</param>
        /// <param name="idAircraft">The ID of the aircraft to delete</param>
        /// <returns>The new list of aircraft for the user</returns>
        [WebMethod]
        public Aircraft[] DeleteAircraftForUser(string szAuthUserToken, int idAircraft)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.LogCall("Web Service: DeleteAircraftForUser aircraft: {id} for User: {user}", idAircraft, szUser);
            UserAircraft ua = new UserAircraft(szUser);
            ua.FDeleteAircraftforUser(idAircraft);
            return AircraftForUser(szAuthUserToken);
        }

        /// <summary>
        /// Returns a list of simplemakemodels, i.e., mappings between an idmodel and a human-readable description.
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public SimpleMakeModel[] MakesAndModels()
        {
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);
            EventRecorder.LogCall("MakesAndModels - anonymous method");
            return SimpleMakeModel.GetAllMakeModels();
        }

        /// <summary>
        /// Updates the culture for the duration of the request
        /// </summary>
        private static void SetCultureForRequest()
        {
            IEnumerable<string> languages = util.RequestContext?.CurrentRequestLanguages;
            if (languages?.Any() ?? false)
                util.SetCulture(languages.First());
        }

        /// <summary>
        /// Returns a list of currencystatusitems for the current use, including: (a) flying currency (e.g., passengers or instrument flight),
        /// (b) maintenance currency (e.g., annual inspections due), and (c) personal events such as BFR or medical due.
        /// </summary>
        /// <param name="szAuthToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <returns>An array of currency status items, null if there's a problem.</returns>
        [WebMethod]
        public CurrencyStatusItem[] GetCurrencyForUser(string szAuthToken)
        {
            string szUser = GetEncryptedUser(szAuthToken);
            EventRecorder.LogCall("GetCurrencyForUser - user {user}", szUser);
            SetCultureForRequest();

            if (szUser.Length > 0)
                return CurrencyStatusItem.GetCurrencyItemsForUser(szUser).ToArray();
            else
                return null;
        }

        /// <summary>
        /// Returns the user's flying totals from the specified (minimum) date to the present.  Currently called by the iOS WatchExtension.
        /// </summary>
        /// <param name="szAuthToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <param name="dtSince">The starting date for the query</param>
        /// <returns>An array of TotalsItems, null if there's a problem</returns>
        [WebMethod]
        public TotalsItem[] TotalsForUser(string szAuthToken, DateTime dtMin)
        {
            string szUser = GetEncryptedUser(szAuthToken);
            if (szUser.Length == 0)
                return null;

            EventRecorder.LogCall("TotalsForUser - user {user}", szUser);
            SetCultureForRequest();

            FlightQuery fq = new FlightQuery(szUser)
            {
                DateRange = FlightQuery.DateRanges.Custom,
                DateMin = dtMin
            };
            UserTotals ut = new UserTotals(szUser, fq, true);
            ut.DataBind();
            return ut.Totals.ToArray();
        }

        /// <summary>
        /// Returns the user's flying totals using the specified query
        /// </summary>
        /// <param name="szAuthToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <param name="fq">The query paramteters</param>
        /// <returns>An array of TotalsItems, null if there's a problem</returns>
        [WebMethod]
        public TotalsItem[] TotalsForUserWithQuery(string szAuthToken, FlightQuery fq)
        {
            string szUser = GetEncryptedUser(szAuthToken);
            if (szUser.Length == 0)
                return null;

            SetCultureForRequest();
            if (fq == null)
                fq = new FlightQuery(szUser);
            EventRecorder.LogCall("TotalsForUserWithQuery - user {user}", szUser);

            util.UnescapeObject(fq);    // fix for HTML encoding issues.
            fq.UserName = szUser; // just to be safe
            UserTotals ut = new UserTotals(szUser, fq, true);
            ut.DataBind();
            return ut.Totals.ToArray();
        }

        /// <summary>
        /// Get a list of airports visited by the user
        /// </summary>
        /// <param name="szAuthToken">Authtoken for user</param>
        /// <returns>The list of airports visited by the user</returns>
        [WebMethod]
        public VisitedAirport[] VisitedAirports(string szAuthToken)
        {
            string szUser = GetEncryptedUser(szAuthToken);
            if (szUser.Length == 0)
                return null;
            EventRecorder.LogCall("VisitedAirports - user {user}", szUser);
            return VisitedAirport.VisitedAirportsForUser(szUser).ToArray();
        }

        /// <summary>
        /// Non-exposed base method for flightswithquery (no authtoken provided; FlightQuery MUST have a valid username)
        /// </summary>
        /// <param name="fq">FlightQuery object with valid username provided.</param>
        /// <param name="maxCount">Maximum number of results, -1 for all</param>
        /// <returns>Matching flights.</returns>
        private static LogbookEntry[] FlightsWithQuery(FlightQuery fq, int offset, int maxCount)
        {
            if (String.IsNullOrEmpty(fq.UserName))
                throw new MyFlightbookException("FlightsWithQuery - no valid username specified");

            util.UnescapeObject(fq);    // fix for iPhone HTML encoding issues.
            EventRecorder.LogCall("FlightsWithQuery - user {user}, offset {offset}, maxCount {maxCount}", fq.UserName, offset, maxCount);
            IEnumerable<LogbookEntry> rgFlights = LogbookEntry.GetFlightsForUser(fq, offset, maxCount);
            IEnumerable<CannedQuery> colorQueryMap = FlightColor.QueriesToColor(fq.UserName);
            // Color the flights
            foreach (LogbookEntry le in rgFlights)
            {
                foreach (CannedQuery cq in colorQueryMap)
                {
                    if (cq.MatchesFlight(le))
                    {
                        // Break on a match, doing these in order - this needs to be deterministic
                        le.FlightColorHex = cq.ColorString;
                        break;
                    }
                }
            }
            return rgFlights.ToArray();
        }

        /// <summary>
        /// Returns flights for the specified user that match a particular query, using an offset and a limit to support infinite scrolling
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to acces a user</param>
        /// <param name="fq">FlightQuery</param>
        /// <param name="offset">The starting offset</param>
        /// <param name="maxCount">Maximum number of results to return, -1 for all</param>
        /// <returns></returns>
        [WebMethod]
        public LogbookEntry[] FlightsWithQueryAndOffset(string szAuthUserToken, FlightQuery fq, int offset, int maxCount)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);
            if (fq == null)
                fq = new FlightQuery(szUser);
            fq.UserName = szUser; //just to be sure.
            return FlightsWithQuery(fq, offset, maxCount);
        }

        /// <summary>
        /// Returns flights for the specified user that match a particular query
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to acces a user</param>
        /// <param name="fq">FlightQuery</param>
        /// <param name="maxCount">Maximum number of results to return, -1 for all</param>
        /// <returns>The matching flights</returns>
        [WebMethod]
        public LogbookEntry[] FlightsWithQuery(string szAuthUserToken, FlightQuery fq, int maxCount)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);
            if (fq == null)
                fq = new FlightQuery(szUser);
            fq.UserName = szUser; //just to be sure.
            return FlightsWithQuery(fq, 0, maxCount);
        }

        /// <summary>
        /// Delete a logbook entry for the named user.  The user MUST be the owner of the flight
        /// </summary>
        /// <param name="szAuthUserToken">Auth token for the user</param>
        /// <param name="idFlight">Row of the flight to delete</param>
        /// <returns>true for success</returns>
        [WebMethod]
        public Boolean DeleteLogbookEntry(string szAuthUserToken, int idFlight)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (idFlight <= 0)
                throw new MyFlightbookException(Resources.WebService.errInvalidFlight);
            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);
            EventRecorder.LogCall("DeleteLogbookEntry - user {user}, idFlight {idFlight}", szUser, idFlight);
            LogbookEntry le = new LogbookEntry();
            if (le.FLoadFromDB(idFlight, szUser))
            {
                // if we're here, it's a valid flight for the user
                try
                {
                    return LogbookEntry.FDeleteEntry(idFlight, szUser);
                }
                catch (Exception ex)
                {
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.WebService.errDeleteFlightFailed, ex.Message));
                }
            }
            else
            {
                return false;
            }
        }

        #region Flight Commit preparation/after-commit tasks
        private readonly static System.Object idempotencyLock = new System.Object();

        /// <summary>
        /// Validates a flight for submission and does any appropriate cleanup.  Throws MyFlightbookException on any validation failure
        /// </summary>
        /// <param name="le"></param>
        /// <param name="szUser"></param>
        private static void PrepareFlightForSubmission(LogbookEntry le, string szUser)
        {
            // clean up user name in case an email address is passed in.
            if (le.User.Contains("@"))
                le.User = Membership.GetUserNameByEmail(le.User);

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            // should never happen
            if (String.IsNullOrEmpty(le.User) || szUser.CompareCurrentCultureIgnoreCase(le.User) != 0)
                le.User = szUser;

            // fCommit from logbook (below) will do normal validation, but two more layers are needed for external edits:
            // a) make sure that an edit to an existing row actually belongs to the user, and
            // b) make sure that we have referential integrity w.r.t. the aircraft ID.

            // editing an existing row - make sure it belongs to the user!
            if (!le.IsNewFlight)
            {
                LogbookEntry leExisting = new LogbookEntry();
                if (!leExisting.FLoadFromDB(le.FlightID, szUser) || String.Compare(leExisting.User, szUser, StringComparison.OrdinalIgnoreCase) != 0)
                    throw new MyFlightbookException(Resources.WebService.errFlightNotYours);

                // In case this is being submitted without attached properties, copy over the properties that we just loaded
                if (le.CustomProperties.Count == 0 && leExisting.CustomProperties.Count > 0)
                    le.CustomProperties.SetItems(leExisting.CustomProperties);
                else
                    CustomFlightProperty.FixUpDuplicateProperties(leExisting.CustomProperties, le.CustomProperties);
            }

            // Validate that this aircraft is one of the user's aircraft
            UserAircraft ua = new UserAircraft(szUser);
            le.AircraftID = AircraftTombstone.MapAircraftID(le.AircraftID);
            Aircraft ac = new Aircraft(le.AircraftID);
            if (ac.AircraftID == Aircraft.idAircraftUnknown || !ac.IsValid(false))
                throw new MyFlightbookException(Resources.WebService.errAircraftDoesntExist);

            if (!ua.CheckAircraftForUser(ac))
                throw new MyFlightbookException(Resources.WebService.errAircraftNotFound);

            // Do any autofill now - this will also allow the idempotency check to work.
            le.AutofillForAircraft(ua[le.AircraftID]);

            // fix up any dates
            le.EngineStart = le.EngineStart.NormalizeDate();
            le.EngineEnd = le.EngineEnd.NormalizeDate();
            le.FlightStart = le.FlightStart.NormalizeDate();
            le.FlightEnd = le.FlightEnd.NormalizeDate();

            foreach (CustomFlightProperty cfp in le.CustomProperties)
            {
                if (cfp.TextValue.Length > 0)
                    cfp.TextValue = cfp.TextValue;
                cfp.DateValue = DateTime.SpecifyKind(cfp.DateValue, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Handles any tasks to be done after a successful commit of a flight
        /// </summary>
        /// <param name="le"></param>
        private static void AfterFlightCommitTasks(LogbookEntry le)
        {
            Profile pf = Profile.GetUser(le.User);

            // EventRecorder.WriteEvent(EventRecorder.MFBEventID.CommitFlight, szUser, "Commit Flight");
            EventRecorder.UpdateCount(EventRecorder.MFBCountID.WSFlight, 1);

            pf.SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);
        }
        #endregion

        [WebMethod]
        public LogbookEntry CommitFlightWithOptions(string szAuthUserToken, LogbookEntry le, PostingOptions po)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            po?.ToString();  // avoid warning about unused po

            string szUser = GetEncryptedUser(szAuthUserToken);

            EventRecorder.LogCall("CommitFlightWithOptions - user {user}", szUser);

            PrepareFlightForSubmission(le, szUser);

            bool fSuccess = false;

            // We are now ready to commit, so we did all of the processing we could above - we now need to do a quick psuedo-idempotency check, if this is a new flight, and then commit
            // We want to lock on this so that the idempotency check doesn't have a race condition.
            lock (idempotencyLock)
            {
                if (le.IsNewFlight)
                {
                    // Idempotency check: if this is a new flight, see if this is the same as any existing flight
                    // If so, return that flight.
                    FlightQuery fq = new FlightQuery(szUser)
                    {
                        DateRange = FlightQuery.DateRanges.Custom,
                        DateMax = le.Date,
                        DateMin = le.Date
                    };

                    LogbookEntry[] rgleDupes = FlightsWithQuery(fq, 0, -1);
                    if (rgleDupes != null && rgleDupes.Length > 0)
                    {
                        // Fix up property type - this isn't passed in consistently and breaks equality check!
                        if (le.CustomProperties == null)
                            le.CustomProperties = new CustomPropertyCollection();
                        else
                        {
                            IEnumerable<CustomPropertyType> allProps = CustomPropertyType.GetCustomPropertyTypes();
                            foreach (CustomFlightProperty cfp in le.CustomProperties)
                                cfp.InitPropertyType(allProps);
                        }

                        foreach (LogbookEntry l in rgleDupes)
                        {
                            if (le.IsEqualTo(l))
                                return l;
                        }
                    }
                }

                fSuccess = le.FCommit(le.FlightData != null && le.FlightData.Length > 0);
            }

            // If it was successfully saved, then we can do post-processing.  This can be done outside of the lock since there isn't really any idempotency issue here.
            if (fSuccess)
            {
                AfterFlightCommitTasks(le);
                return le;
            }
            else
                throw new MyFlightbookException(le.ErrorString);
        }

        /// <summary>
        /// Returns the flight path for the flight as a series of lat/long coordinates
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken for the user</param>
        /// <param name="idFlight">The flight being requested</param>
        /// <returns>An array of LatLongs, null if no data is found.</returns>
        [WebMethod]
        public LatLong[] FlightPathForFlight(string szAuthUserToken, int idFlight)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            LatLong[] rgResult = null;
            if (szUser.Length > 0)
            {
                EventRecorder.LogCall("FlightPathForFlight - user {user}", szUser);
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idFlight, szUser, LogbookEntry.LoadTelemetryOption.MetadataOrDB) && le.Telemetry.HasPath)
                    return le.Telemetry.Path().ToArray<LatLong>();
            }
            return rgResult;
        }

        /// <summary>
        /// Returns the flight path for the flight in GPX format
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken for the user</param>
        /// <param name="idFlight">The flight being requested</param>
        /// <returns>A GPX file, if one can be produced.</returns>
        [WebMethod]
        public string FlightPathForFlightGPX(string szAuthUserToken, int idFlight)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            if (szUser.Length > 0)
            {
                EventRecorder.LogCall("FlightPathForFlightGPX - user {user}", szUser);
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idFlight, szUser, LogbookEntry.LoadTelemetryOption.LoadAll) && le.Telemetry.HasPath)
                    return le.TelemetryAsGPX();
            }
            return null;
        }

        [WebMethod]
        public string[] CheckFlight(string szAuthUserToken, LogbookEntry le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));

            // slam in the authenticated user.
            le.User = GetEncryptedUser(szAuthUserToken);

            List<string> result = new List<string>();

            foreach (FlightWithIssues f in (new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, le.User, FlightLint.DefaultOptionsForLocale)))
                foreach (FlightIssue fi in f.Issues)
                    result.Add(fi.IssueDescription);

            return result.ToArray();
        }

        #region Pending Flights support
        private static void ValidatePendingFlightForUser(string szUser, PendingFlight pf)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new UnauthorizedAccessException(Resources.WebService.errFlightNotYours);

            // clean up user name in case an email address is passed in.
            if (pf.User.Contains("@"))
                pf.User = Membership.GetUserNameByEmail(pf.User);

            if (pf.User.CompareCurrentCultureIgnoreCase(szUser) != 0)
                throw new MyFlightbookException(Resources.WebService.errFlightNotYours);

            PendingFlight pfOwned = PendingFlight.PendingFlightsForUser(szUser).FirstOrDefault(pf2 => pf2.PendingID.CompareOrdinal(pf.PendingID) == 0) ?? throw new MyFlightbookException(Resources.WebService.errFlightNotYours);
        }

        [WebMethod]
        public PendingFlight[] CreatePendingFlight(string szAuthUserToken, LogbookEntry le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            EventRecorder.LogCall("CreatePendingFlight - user {user}", szUser);
            // Create a pending flight, but update the user field...just to be safe.
            new PendingFlight(le) { User = szUser }.Commit();

            return PendingFlightsForUser(szAuthUserToken).ToArray();
        }

        [WebMethod]
        public PendingFlight[] PendingFlightsForUser(string szAuthUserToken)
        {
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            EventRecorder.LogCall("PendingFlightsForUser - user {user}", szUser);
            return PendingFlight.PendingFlightsForUser(szUser).ToArray();
        }

        [WebMethod]
        public PendingFlight[] UpdatePendingFlight(string szAuthUserToken, PendingFlight pf)
        {
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            // Verify that pending flight exists and is owned by this user
            ValidatePendingFlightForUser(szUser, pf);

            EventRecorder.LogCall("UpdatePendingFlight - user {user}", szUser);
            pf.Commit();

            return PendingFlight.PendingFlightsForUser(szUser).ToArray();
        }

        [WebMethod]
        public PendingFlight[] DeletePendingFlight(string szAuthUserToken, string idpending)
        {
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            if (idpending == null)
                throw new ArgumentNullException(nameof(idpending));
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            PendingFlight pf = (PendingFlight.PendingFlightsForUser(szUser)).FirstOrDefault(pf2 => pf2.PendingID.CompareOrdinal(idpending) == 0);

            if (pf == null || pf.User.CompareOrdinal(szUser) != 0)
                throw new MyFlightbookException(Resources.WebService.errFlightNotYours);

            EventRecorder.LogCall("DeletePendingFlight - user {user}", szUser);
            pf.Delete();

            return PendingFlight.PendingFlightsForUser(szUser).ToArray();
        }

        [WebMethod]
        public PendingFlight[] CommitPendingFlight(string szAuthUserToken, PendingFlight pf)
        {
            if (szAuthUserToken == null)
                throw new ArgumentNullException(nameof(szAuthUserToken));
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            string szUser = GetEncryptedUser(szAuthUserToken);

            if (String.IsNullOrEmpty(szUser))
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            // Verify that pending flight exists and is owned by this user
            ValidatePendingFlightForUser(szUser, pf);

            EventRecorder.LogCall("CommitPendingFlight - user {user}", szUser);
            if (pf.FCommit())
                return PendingFlight.PendingFlightsForUser(szUser).ToArray();
            else
                throw new MyFlightbookException(pf.ErrorString);
        }
        #endregion

        /// <summary>
        /// Returns a list of the available custom property types.
        /// </summary>
        /// <returns>An array of custom property types</returns>
        [WebMethod]
        public CustomPropertyType[] AvailablePropertyTypes()
        {
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);
            EventRecorder.LogCall("AvailableProepertyTypes - anonymous (why do we still have this???)");
            return CustomPropertyType.GetCustomPropertyTypes("");
        }

        [WebMethod]
        public CustomPropertyType[] AvailablePropertyTypesForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.LogCall("AvailablePropertyTypesForUser - user {user}", szUser);
            return CustomPropertyType.GetCustomPropertyTypes(szUser);
        }

        [WebMethod]
        public TemplatePropTypeBundle PropertiesAndTemplatesForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.LogCall("PropertiesAndTemplatesForUser - user {user}", szUser);
            return new TemplatePropTypeBundle(szUser);
        }

        /// <summary>
        /// Returns the custom properties for the flight
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken for the owner of the flight</param>
        /// <param name="idFlight">Flight to load</param>
        /// <returns>An array of custom flight properties, none if error or user is not the owner.</returns>
        [WebMethod]
        public CustomFlightProperty[] PropertiesForFlight(string szAuthUserToken, int idFlight)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            if (szUser.Length > 0)
            {
                EventRecorder.LogCall("PropertiesForFlight - user {user}", szUser);
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idFlight, szUser))
                    return le.CustomProperties.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Delete multiple properties for a given flight.
        /// </summary>
        /// <param name="szAuthUserToken"></param>
        /// <param name="idFlight"></param>
        /// <param name="rgPropIds"></param>
        [WebMethod]
        public void DeletePropertiesForFlight(string szAuthUserToken, int idFlight, int[] rgPropIds)
        {
            if (rgPropIds == null)
                return;

            string szUser = GetEncryptedUser(szAuthUserToken);
            if (szUser.Length > 0)
            {
                EventRecorder.LogCall("DeletePropertiesforFlight - user {user}, idFlight {idFlight}", szUser, idFlight);
                LogbookEntry le = new LogbookEntry();
                // FLoadFromDB will load custom properties but will
                // also verify for us that the user owns the flight, and hence the properties.
                // we will only delete those properties in the flight which are in the array of property IDs.
                if (le.FLoadFromDB(idFlight, szUser))
                {
                    foreach (int id in rgPropIds)
                    {
                        CustomFlightProperty cfp = le.CustomProperties.FindEvent(c => c.PropID == id);
                        if (cfp != null)
                        {
                            le.CustomProperties.RemoveItem(cfp.PropTypeID);
                            cfp.DeleteProperty();
                        }
                    }
                }
                if (le.CFISignatureState != LogbookEntry.SignatureState.None)
                    le.FCommit(); // forces a refresh of the signature state
                Profile.GetUser(szUser).SetAchievementStatus(Achievements.Achievement.ComputeStatus.NeedsComputing);
            }
        }

        /// <summary>
        /// Delete a single property - easier to call from a web service.
        /// </summary>
        /// <param name="szAuthUserToken"></param>
        /// <param name="idFlight"></param>
        /// <param name="propId"></param>
        [WebMethod]
        public void DeletePropertyForFlight(string szAuthUserToken, int idFlight, int propId)
        {
            DeletePropertiesForFlight(szAuthUserToken, idFlight, new int[] { propId });
        }

        /// <summary>
        /// Deletes the specified image
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken for the user</param>
        /// <param name="mfbii">MFBImageInfo object filled out sufficiently to uniquely identify the image to delete</param>
        [WebMethod]
        public void DeleteImage(string szAuthUserToken, MFBImageInfo mfbii)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));
            string szUser = GetEncryptedUser(szAuthUserToken);

            // Hack to work around a bug: the incoming mfbii (at least from iPhone) specifies JPG as image type...always (not initialized). So...
            // do a sanity check.
            mfbii.FixImageType();

            ImageAuthorization.ValidateAuth(mfbii, szUser, ImageAuthorization.ImageAction.Delete);

            EventRecorder.LogCall("DeleteImage - user {user}, thumb={thumb}", szUser, mfbii.URLThumbnail);
            mfbii.DeleteImage();
        }

        /// <summary>
        /// Updates the comment for the specified image
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken for the user</param>
        /// <param name="mfbii">MFBImageInfo object filled out sufficiently to uniquely identify the image to update.  The comment parameter will be used.</param>
        [WebMethod]
        public void UpdateImageAnnotation(string szAuthUserToken, MFBImageInfo mfbii)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));
            string szUser = GetEncryptedUser(szAuthUserToken);

            ImageAuthorization.ValidateAuth(mfbii, szUser, ImageAuthorization.ImageAction.Annotate);

            EventRecorder.LogCall("UpdateImageAnnotation - user {user}, thumb={thumb}", szUser, mfbii.URLThumbnail);
            if (mfbii.ThumbnailFile.Length > 0 && mfbii.VirtualPath.Length > 0)
                mfbii.UpdateAnnotation(mfbii.Comment);
        }

        /// <summary>
        /// Determines if we are on a secure connection, or if we are exempt from a secure connection (local or 192 request)
        /// </summary>
        /// <param name="r">The request</param>
        /// <returns>True if we are secure OR exempt from security</returns>
        public static bool CheckSecurity()
        {
            return (util.RequestContext.IsSecure || util.RequestContext.CurrentRequestUrl.Host.StartsWith("192.168", StringComparison.OrdinalIgnoreCase) || util.RequestContext.CurrentRequestUrl.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) || util.RequestContext.IsLocal);
        }

        private static string AuthTokenForValidatedUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            return new Encryptors.WebServiceEncryptor().Encrypt(szUser + ";" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        }

        public static string AuthTokenFromOAuthToken(DotNetOpenAuth.OAuth2.ChannelElements.AuthorizationDataBag token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            return AuthTokenForValidatedUser(token.User);
        }

        /// <summary>
        /// Returns an authorization token to authenticate a particular user, which can be used in subsequent calls for that user.
        /// </summary>
        /// <param name="szAppToken">Application key that authorizes access to any user</param>
        /// <param name="szUser">Email to validate</param>
        /// <param name="szPass">Password to validate</param>
        /// <returns>Authorization token, if successful, else null</returns>
        [WebMethod]
        public string AuthTokenForUser(string szAppToken, string szUser, string szPass)
        {
            return AuthTokenForUser(szAppToken, szUser, szPass, out _);
        } 

        private static string AuthTokenForUser(string szAppToken, string szUser, string szPass, out string szEmulator)
        {
            szEmulator = string.Empty;
            // Only speak to authorized clients.
            if (String.IsNullOrEmpty(szAppToken) || !IsAuthorizedService(szAppToken))
                return null;

            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (szPass == null)
                throw new ArgumentNullException(nameof(szPass));

            // look for admin emulation in the form of 
            string[] rgUsers = szUser.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            string szEmulate = string.Empty;
            if (rgUsers != null && rgUsers.Length == 2)
            {
                szEmulate = rgUsers[0];
                szUser = rgUsers[1];
            }

            // Convert an email address to a username
            if (szUser.Contains("@"))
                szUser = System.Web.Security.Membership.GetUserNameByEmail(szUser);

            // make sure that we actually have something.
            if (szUser.Length == 0)
                return null;

            if (!CheckSecurity())
                throw new MyFlightbookException(Resources.WebService.errNotSecureConnection);

            // In case the password has an html entity in it, may need to compare using that as well.  Most of the time, though, shouldn't need to.
            // Slight efficiency gain by not doing two validations if the unescaped password is the same as the escaped one
            string szUnescapedPass = szPass.UnescapeHTML();
            bool fValid = (UserEntity.ValidateUser(szUser, szPass).CompareOrdinal(szUser) == 0) ||
                            (szPass.CompareOrdinal(szUnescapedPass) != 0 && (UserEntity.ValidateUser(szUser, szUnescapedPass).CompareOrdinal(szUser) == 0));

            if (fValid)
            {
                if (!String.IsNullOrEmpty(szEmulate))   // emulation requested - validate that the authenticated user is actually authorized!!!
                {
                    Profile pf = Profile.GetUser(szUser);
                    if (pf.CanSupport || pf.CanManageData)
                    {
                        // see if the emulated user actually exists
                        pf = Profile.GetUser(szEmulate);
                        if(!pf.IsValid())
                            throw new MyFlightbookException("No such user: " + szEmulate);

                        // At this point, we have AUTHENTICATED on szUser, but we are signing in (returning an authtoken) as szEmulator, so capture that.
                        szEmulator = szUser;    // The accountname of the user doing the emulation
                        szUser = szEmulate;     // the resulting emulated account is szUser = szEmulate
                    }
                    else
                        throw new UnauthorizedAccessException();
                }
                EventRecorder.WriteEvent(EventRecorder.MFBEventID.AuthUser, szUser, String.Format(CultureInfo.InvariantCulture, "Auth User - {0} from {1}.", util.RequestContext.IsSecure ? "Secure" : "NOT SECURE", szAppToken));

                return AuthTokenForValidatedUser(szUser);
            }

            return null;
        }

        [WebMethod]
        public AuthResult AuthTokenForUserNew(string szAppToken, string szUser, string szPass, string sz2FactorAuth = null)
        {
            string szAuth = AuthTokenForUser(szAppToken, szUser, szPass, out string szEmulator);
            AuthResult result = AuthResult.ProcessResult(szAuth, GetEncryptedUser(szAuth), sz2FactorAuth, szEmulator);
            EventRecorder.LogCall("AuthTokenForUserNew - user {user}, szApptoken={appToken}, emulator={emulator}", szUser, szAppToken, szEmulator ?? "(no emulation)");
            return result;
        }

        [WebMethod]
        public string RefreshAuthToken(string szAppToken, string szUser, string szPass, string szPreviousToken)
        {
            if (szPreviousToken == null)
                throw new ArgumentNullException(nameof(szPreviousToken));

            string szUserOld;
            try
            {
                szUserOld = GetEncryptedUser(szPreviousToken);
                if (szUserOld == null)
                    return null;
            } catch (MyFlightbookException)
            {
                // Most likely exception is that the password was changed but refresh is using old password.
                return null;
            }

            string szTokenNew = AuthTokenForUser(szAppToken, szUser, szPass);
            if (szTokenNew == null)
                return null;

            EventRecorder.LogCall("RefreshAuthToken - user {user}", szUser);
            return (GetEncryptedUser(szTokenNew).CompareOrdinal(szUserOld) == 0) ? szTokenNew : null;
        }

        private readonly static System.Object lockObject = new System.Object();

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="szAppToken">Application token that authorizes access to the web seervice</param>
        /// <param name="szFirst">User's first name (optional)</param>
        /// <param name="szLast">User's last name (optional)</param>
        /// <param name="szPass">Password</param>
        /// <param name="szEmail">User email address</param>
        /// <param name="szQuestion">Password reset question</param>
        /// <param name="szAnswer">Password reset answer</param>
        /// <returns></returns>
        [WebMethod]
        public UserEntity CreateUser(string szAppToken, string szEmail, string szPass, string szFirst, string szLast, string szQuestion, string szAnswer)
        {
            if (!IsAuthorizedService(szAppToken))
                throw new MyFlightbookException("Unauthorized!");

            // do a few simple validations
            if (!CheckSecurity())
                throw new MyFlightbookException(Resources.WebService.errNotSecureConnection);

            // create a username from the email address
            string szUser = UserEntity.UserNameForEmail(szEmail);

            EventRecorder.LogCall("New user to create (may fail): source: {appToken}, email {email}, user {user}", szEmail, szAppToken, szUser);

            szFirst = szFirst ?? string.Empty;
            szLast = szLast ?? string.Empty;

            UserEntity result = null;

            lock (lockObject)
            {
                try
                {
                    result = UserEntity.CreateUser(szUser, szPass.UnescapeHTML(), szEmail, szQuestion.UnescapeHTML(), szAnswer.UnescapeHTML());
                }
                catch (UserEntityException ex)
                {
                    // fix up the error message if there is none for some reason.
                    if (String.IsNullOrEmpty(ex.Message))
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errGenericCreateAccountError, result.mcs.ToString()));
                    throw;
                }
            }

            if (result == null)
            {
                // should never be here.
                string szErr = String.Format(CultureInfo.CurrentCulture, "Error creating new user - uncaught error: {0}", result == null ? "null result from createuser" : result.mcs.ToString());
                util.NotifyAdminEvent("Error creating new user " + szUser, szErr, ProfileRoles.maskSiteAdminOnly);
                throw new MyFlightbookException(szErr);
            }

            // some of the errors below were probably already thrown above (as a userentityexception), but format the string here in case they weren't
            // e.g., invalid answer probably already thrown, but duplicateEmail will return without throwing an exception
            // so we catch them all here just in case, to ensure that something meaningful gets returned to the user
            switch (result.mcs)
            {
                case MembershipCreateStatus.Success:
                    result.szAuthToken = AuthTokenForUser(szAppToken, szUser, szPass);
                    try
                    {
                        // set the first/last name for the user
                        ProfileAdmin.FinalizeUser(szUser, szFirst, szLast);
                        EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateUser, szUser, String.Format(CultureInfo.InvariantCulture, "User '{0}' was created at {1}, connection {2} - {3}", szUser, DateTime.Now.ToShortDateString(), util.RequestContext.IsSecure ? "Secure" : "NOT SECURE", szAppToken));
                    }
                    catch (Exception ex)
                    {
                        EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateUserError, szUser, "Exception creating user: " + ex.Message);
                        util.NotifyAdminEvent("Error creating new user: ", "Username: " + szUser + "\r\n\r\n" + ex.Message, ProfileRoles.maskSiteAdminOnly);
                        throw new MyFlightbookException("Error creating new user: " + ex.Message);
                    }
                    return result;
                case MembershipCreateStatus.InvalidAnswer:
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidAnswer, szAnswer));
                case MembershipCreateStatus.DuplicateEmail:
                case MembershipCreateStatus.DuplicateUserName:
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errEmailInUse, szEmail));
                case MembershipCreateStatus.InvalidEmail:
                case MembershipCreateStatus.InvalidUserName:
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, szEmail));
                case MembershipCreateStatus.InvalidPassword:
                    throw new MyFlightbookException(Resources.Profile.errInvalidPassword);
                case MembershipCreateStatus.InvalidQuestion:
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidQuestion, szQuestion));
                case MembershipCreateStatus.UserRejected:
                case MembershipCreateStatus.ProviderError:
                case MembershipCreateStatus.DuplicateProviderUserKey:
                case MembershipCreateStatus.InvalidProviderUserKey:
                default:
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errGenericCreateAccountError, result.mcs.ToString()));
            }
        }

        #region Named (canned) queries:
        [WebMethod]
        public CannedQuery[] GetNamedQueriesForUser(string szAuthToken)
        {
            string szUser = GetEncryptedUser(szAuthToken);

            return String.IsNullOrEmpty(szUser) ? null : CannedQuery.QueriesForUser(szUser).ToArray();
        }

        [WebMethod]
        public CannedQuery[] AddNamedQueryForUser(string szAuthToken, FlightQuery fq, string szName)
        {
            if (szAuthToken == null)
                throw new ArgumentNullException(nameof(szAuthToken));
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (szName == null)
                throw new ArgumentNullException(nameof(szName));
            if (String.IsNullOrWhiteSpace(szName))
                throw new InvalidOperationException("Missing or empty name passed to AddNamedQueryForUser");

            string szUser = GetEncryptedUser(szAuthToken);
            if (!String.IsNullOrEmpty(szUser) && ! fq.IsDefault)
            {
                fq.UserName = szUser;   // just to be safe
                new CannedQuery(fq, szName).Commit();
            }

            return GetNamedQueriesForUser(szAuthToken);
        }

        [WebMethod]
        public CannedQuery[] DeleteNamedQueryForUser(string szAuthToken, CannedQuery cq)
        {
            if (szAuthToken == null)
                throw new ArgumentNullException(nameof(szAuthToken));
            if (cq == null)
                throw new ArgumentNullException(nameof(cq));

            string szUser = GetEncryptedUser(szAuthToken);
            if (!String.IsNullOrEmpty(szUser) && cq != null)
            {
                cq.UserName = szUser;   // just to be safe
                if (String.IsNullOrEmpty(cq.QueryName))
                    throw new UnauthorizedAccessException("No query name specified");

                cq.Delete();
                return GetNamedQueriesForUser(szAuthToken);
            }
            return null;
        }
        #endregion
    }
}