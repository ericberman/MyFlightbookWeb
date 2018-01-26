using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.SocialMedia;
using MyFlightbook.Telemetry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public class PostingOptions
    {
        /// <summary>
        /// Post the flight to Facebook?
        /// </summary>
        public Boolean PostToFacebook { get; set; }

        /// <summary>
        /// Post to Twitter?
        /// </summary>
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
        public PostingOptions(Boolean fTwitter, Boolean fFacebook)
        {
            PostToFacebook = fFacebook;
            PostToTwitter = fTwitter;
        }
    }

    /// <summary>
    /// A bit of a hack to deal with the fact that we post a flight with pictures as two separate posts.
    /// We delay posting to facebook to give some time for at least the first picture to be uploaded.
    /// </summary>
    public class DelayedPost
    {
        public DelayedPost()
        {
        }

        public LogbookEntry Entry { get; set; }

        public string HostName { get; set; }

        public DelayedPost(LogbookEntry le, string host)
        {
            Entry = le;
            HostName = host;
        }
    }

    public static class EventRecorder
    {
        public enum MFBEventID { None, AuthUser = 1, GetAircraft, FlightsByDate, CommitFlightDEPRECATED, CreateAircraft, CreateUser, CreateUserAttemptDEPRECATED, CreateUserError, ExpiredToken, ObsoleteAPI };
        public enum MFBCountID { WSFlight, ImportedFlight };

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
    }


    /// <summary>
    /// The main SOAP service for mobile use
    /// NOTE: iPhone sends HTML entities URL encoded, so we need to decode them on receipt.  Bleah.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    public class MFBWebService : System.Web.Services.WebService
    {

        private const long SecondsPerDay = (24 * 3600);
        private const long TicksPerSecond = (1000 * 10000);

        private static string[] rgszAuthorizedService = null;
        private string[] AuthorizedServices
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
        private Boolean IsAuthorizedService(string szAppToken)
        {
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);

            foreach (string sz in AuthorizedServices)
                if (sz.CompareCurrentCultureIgnoreCase(szAppToken) == 0)
                    return true;
            return false;
        }

        public string GetEncryptedUser(string szAuthUserToken)
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

                return rgsz[0];
            }
            else
                return "";
        }

        public MFBWebService()
        {

            //Uncomment the following line if using designed components 
            //InitializeComponent(); 
        }

        /*
        /// <summary>
        /// Returns a list of airports, given an input of airport codes (3- and 4- letter acronyms).
        /// Each airport contains airport name, code, and latitude/longitude.  The list is NOT deduped, and
        /// airports are returned in the order in which they are passed.  I.e., this is suitable for a route.
        /// </summary>
        /// <param name="szAirports">A delimited string containing airport codes.  Any non-alphanumeric character is a separater.  
        /// Anything that is not a 3- or 4- letter code, or which is not found, is simply ignored.
        /// </param>
        /// <returns>An array of airports - each one contains the airport code, name, and latitude/longitude</returns>
        [WebMethod]
        [OperationContract]
        [WebGet(UriTemplate = "~/Airports/{szAirports}", ResponseFormat = WebMessageFormat.Xml)]
        public airport[] ResolveAirports(string szAirports)
        {
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, "Unknown", "Obsolete API: ResolveAirports");
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);
            AirportList apl = new AirportList(szAirports);
            return apl.GetNormalizedAirports();
        }
        */

        /// <summary>
        /// Returns a list of the aircraft that the specified user flies, including the tailnumber, summary, and aircraft ID.
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <returns>An array of aircraft, null if auth fails</returns>
        [WebMethod]
        public Aircraft[] AircraftForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            if (szUser.Length > 0)
            {
                // EventRecorder.WriteEvent(EventRecorder.MFBEventID.GetAircraft, szUser, "Aircraft for user");
                UserAircraft ua = new UserAircraft(szUser);
                Aircraft[] rgAc = ua.GetAircraftForUser();
                if (rgAc == null)
                    rgAc = new Aircraft[0]; // return an empty array rather than null
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
                return rgAc;
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

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            if (String.IsNullOrEmpty(szTail))
                throw new MyFlightbookException(Resources.WebService.errBadTailNumber);

            try
            {
                MakeModel m = new MakeModel(idModel);
                if (m == null)
                    throw new MyFlightbookException(Resources.WebService.errInvalidModel);
            }
            catch
            {
                throw new MyFlightbookException(Resources.WebService.errInvalidModel);
            }

            if (idInstanceType > (int)AircraftInstanceTypes.MaxType || idInstanceType < (int)AircraftInstanceTypes.Mintype)
                throw new MyFlightbookException(Resources.WebService.errInvalidInstanceType);

            AircraftInstanceTypes ait = (AircraftInstanceTypes)idInstanceType;

            // Always auto-assign tails for non-real aircraft
            if (ait != AircraftInstanceTypes.RealAircraft)
                szTail = Aircraft.SuggestSims(idModel, ait)[0].TailNumber; // we'll create the aircraft itself below.

            // Fix up the anonymous tailnumber for an anonymous aircraft
            if (szTail.StartsWith(CountryCodePrefix.szAnonPrefix, StringComparison.OrdinalIgnoreCase))
                szTail = Aircraft.AnonymousTailnumberForModel(idModel);

            Aircraft ac = new Aircraft();
            ac.TailNumber = szTail;
            ac.InstanceTypeID = idInstanceType;
            ac.ModelID = idModel;
            ac.Commit(szUser); // this will check for dupes and will add 
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateAircraft, szUser, String.Format(CultureInfo.InvariantCulture,"Create Aircraft - {0}", szTail));

            if (ac.AircraftID < 0)
                throw new MyFlightbookException(Resources.WebService.errNewAircraftFailed);

            return AircraftForUser(szAuthUserToken);
        }

        /// <summary>
        /// Updates the maintenance for an aircraft, performing appropriate logging.  Still called by windows phone
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="idAircraft">The aircraft to update</param>
        [WebMethod]
        public void UpdateMaintenanceForAircraft(string szAuthUserToken, Aircraft ac)
        {
            UpdateMaintenanceForAircraftWithFlagsInternal(szAuthUserToken, ac);
        }

        /*
        /// <summary>
        /// Updates maintenance for an aircraft, performing appropriate logging. Also udpates the aircraft's flags (exclude from list, etc.)
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="ac">The aircraft to update</param>
        [WebMethod]
        public void UpdateMaintenanceForAircraftWithFlags(string szAuthUserToken, Aircraft ac)
        {
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, GetEncryptedUser(szAuthUserToken), "Obsolete API: UpdateMaintenanceForAircraftWithFlags");
            UpdateMaintenanceForAircraftWithFlagsInternal(szAuthUserToken, ac, true);
        }
        */

        /// <summary>
        /// Synonym for UpdateMaintenanceForAircraftWithFlags, but also updates public/private notes
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="ac">The aircraft to update</param>
        [WebMethod]
        public void UpdateMaintenanceForAircraftWithFlagsAndNotes(string szAuthUserToken, Aircraft ac)
        {
            UpdateMaintenanceForAircraftWithFlagsInternal(szAuthUserToken, ac, true, true);
        }


        /// <summary>
        /// Updates maintenance for an aircraft, performing appropriate logging. Also udpates the aircraft's flags (exclude from list, etc.)
        /// </summary>
        /// <param name="szAuthUserToken">Authtoken of authorized user</param>
        /// <param name="ac">The aircraft to update</param>
        /// <param name="fUpdateFlags">True if we want to update the flags for the user as well</param>
        /// <param name="fUpdateNotes">True to update the public/private notes</param>
        private void UpdateMaintenanceForAircraftWithFlagsInternal(string szAuthUserToken, Aircraft ac, bool fUpdateFlags = false, bool fUpdateNotes = false)
        {
            if (szAuthUserToken == null)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            string szUser = GetEncryptedUser(szAuthUserToken);

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

            if (fUpdateNotes)
            {
                acExisting.PrivateNotes = ac.PrivateNotes;
                acExisting.PublicNotes = ac.PublicNotes;
            }

            // Don't allow maintenace updates for anonymous aircraft, but do update private notes and flags
            if (!ac.IsAnonymous)
            {
                acExisting.UpdateMaintenanceForUser(ac.Maintenance, szUser);
                acExisting.Commit();
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
            return SimpleMakeModel.GetAllMakeModels();
        }

        /// <summary>
        /// Updates the culture for the duration of the request
        /// </summary>
        private void SetCultureForRequest()
        {
            if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length > 0)
                util.SetCulture(HttpContext.Current.Request.UserLanguages[0]);
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

            SetCultureForRequest();

            if (szUser.Length > 0)
                return CurrencyStatusItem.GetCurrencyItemsForUser(szUser, false).ToArray();
            else
                return null;
        }

        /*
        /// <summary>
        /// Returns a list of the nearest airports (within 1 degree each way), sorted by distance from the specified lat/long (+/- 1 degree max)
        /// </summary>
        /// <param name="szAuthToken">Authorization token - to prevent overuse</param>
        /// <param name="lat">Latitude of desired position</param>
        /// <param name="lon">Longitude of desired position</param>
        /// <param name="limit">Maximum number of results to return (capped here at 50)</param>
        /// <param name="fIncludeHeliports">True to include heliports, false to exclude them</param>
        /// <returns>The airports</returns>
        [WebMethod]
        public airport[] AirportsNearPosition(string szAuthToken, double lat, double lon, int limit, Boolean fIncludeHeliports)
        {
            string szUser = GetEncryptedUser(szAuthToken);
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, szUser, "Obsolete API: LogbookEntryForUserLE");
            if (szUser.Length > 0)
                return airport.AirportsNearPosition(lat, lon, Math.Min(limit, 50), fIncludeHeliports);
            else
                return null;
        }
        */

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

            SetCultureForRequest();

            FlightQuery fq = new FlightQuery(szUser);
            fq.DateRange = FlightQuery.DateRanges.Custom;
            fq.DateMin = dtMin;
            UserTotals ut = new UserTotals(szUser, fq, true);
            ut.DataBind();
            return ut.TotalsArray();
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
            fq.CustomRestriction = string.Empty;    // security sanity check
            util.UnescapeObject(fq);    // fix for HTML encoding issues.
            fq.UserName = szUser; // just to be safe
            UserTotals ut = new UserTotals(szUser, fq, true);
            ut.DataBind();
            return ut.TotalsArray();
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

            return VisitedAirport.VisitedAirportsForUser(szUser);
        }

        /// <summary>
        /// Non-exposed base method for flightswithquery (no authtoken provided; FlightQuery MUST have a valid username)
        /// </summary>
        /// <param name="fq">FlightQuery object with valid username provided.</param>
        /// <param name="maxCount">Maximum number of results, -1 for all</param>
        /// <returns>Matching flights.</returns>
        public LogbookEntry[] FlightsWithQuery(FlightQuery fq, int offset, int maxCount)
        {
            if (String.IsNullOrEmpty(fq.UserName))
                throw new MyFlightbookException("FlightsWithQuery - no valid username specified");

            fq.CustomRestriction = string.Empty;    // sanity check for security
            util.UnescapeObject(fq);    // fix for iPhone HTML encoding issues.
            List<LogbookEntry> lstLe = new List<LogbookEntry>();

            DBHelper dbh = new DBHelper(LogbookEntry.QueryCommand(fq, offset, maxCount));
            dbh.ReadRows(
                (comm) =>
                { },
                (dr) =>
                {
                    LogbookEntry le = new LogbookEntry(dr, fq.UserName); // Note: this doesn't initialize any properties, has no telemetry
                    le.FlightData = null; // don't even bother sending this field down the wire.

                    le.PopulateImages();
                    lstLe.Add(le);
                }
            );

            return lstLe.ToArray();
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
            fq.CustomRestriction = string.Empty;    // sanity check for security
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
            fq.CustomRestriction = string.Empty;    // sanity check for security
            fq.UserName = szUser; //just to be sure.
            return FlightsWithQuery(fq, 0, maxCount);
        }

        /*
        /// <summary>
        /// Returns all flights for a particular user within a specified date range
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <param name="dtStart">Start date of range</param>
        /// <param name="dtEnd">End date of range</param>
        /// <returns>An array of flights, null if auth fails</returns>
        [WebMethod]
        public LogbookEntry[] FlightsByDate(string szAuthUserToken, DateTime dtStart, DateTime dtEnd)
        {
            LogbookEntry[] rgle = new LogbookEntry[0];

            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, szUser, "Obsolete API: FlightsByDate");
            if (szUser.Length > 0)
            {
                FlightQuery fq = new FlightQuery(szUser);
                fq.DateRange = FlightQuery.DateRanges.Custom;
                fq.DateMin = dtStart;
                fq.DateMax = dtEnd;
                rgle = FlightsWithQuery(fq, 0, -1);
            }

            return rgle;
        }
        */

        /*
        /// <summary>
        /// Returns all flights for a particular user.
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <returns>An array of flights, null if auth fails</returns>
        [WebMethod]
        public LogbookEntry[] Flights(string szAuthUserToken)
        {
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, GetEncryptedUser(szAuthUserToken), "Obsolete API: Flights");
            DateTime dtNow = DateTime.Now;
            DateTime dtEarliest = new DateTime(1900, 1, 1);
            return FlightsByDate(szAuthUserToken, dtEarliest, dtNow);
        }
        */

        /*
        /// <summary>
        /// Creates or edits a logbook entry for a user
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token to access a user (retrieved from AuthTokenForUser method)</param>
        /// <param name="FlightID">The ID for the flight (if already exists), -1 to create a new entry</param>
        /// <param name="AircraftID">The ID of the aircraft used in the flight - must be one that belongs to the user</param>
        /// <param name="Approaches">The # of instrument approaches on the flight</param>
        /// <param name="Landings">The # of landings on the flight</param>
        /// <param name="cNightLandings">The # of landings which were to a full-stop at night; must be &lt;= total number of landings</param>
        /// <param name="cFullStopLandings">The # of landings which were to a full-stop; must be &lt;= total number of landings</param>
        /// <param name="CrossCountry">The amount of cross-country flight time</param>
        /// <param name="Nighttime">The amount of nighttime flight time.</param>
        /// <param name="IMC">The amount of actual instrument time</param>
        /// <param name="SimulatedIFR">The amount of simulated instrument time</param>
        /// <param name="GroundSim">Time spent on a ground simulator</param>
        /// <param name="Dual">Instruction time received</param>
        /// <param name="PIC">Pilot in command time</param>
        /// <param name="TotalFlightTime">Total Flight time</param>
        /// <param name="HoldingProcedures">True of the flight included holding procedures</param>
        /// <param name="Route">The route of the flight</param>
        /// <param name="Comment">Any comments about the flight</param>
        /// <param name="isPublic">Is this flight public?</param>
        /// <param name="DateOfFlight">Date of the flight</param>
        /// <param name="HobbsStart">Starting Hobbs time of the flight</param>
        /// <param name="HobbsEnd">Ending Hobbs time of the flight</param>
        /// <param name="EngineStart">UTC time of engine start</param>
        /// <param name="EngineEnd">UTC time of engine stop</param>
        /// <param name="FlightStart">UTC time of first take-off</param>
        /// <param name="FlightEnd">UTC time of final landing</param>
        /// <returns>Null if failure, else the entry</returns>
        [WebMethod]
        public LogbookEntry LogbookEntryForUserParams(string szAuthUserToken,
            int FlightID,
            int AircraftID,
            int Approaches,
            int Landings,
            int cFullStopLandings,
            int cNightLandings,
            decimal CrossCountry,
            decimal Nighttime,
            decimal IMC,
            decimal SimulatedIFR,
            decimal GroundSim,
            decimal Dual,
            decimal PIC,
            decimal TotalFlightTime,
            Boolean HoldingProcedures,
            string Route,
            string Comment,
            Boolean isPublic,
            DateTime DateOfFlight,
            decimal HobbsStart,
            decimal HobbsEnd,
            DateTime EngineStart,
            DateTime EngineEnd,
            DateTime FlightStart,
            DateTime FlightEnd,
            Boolean fTweet
           )
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, szUser, "Obsolete API: LogbookEntryForUserParams");
            if (szUser.Length > 0)
            {
                LogbookEntry le = new LogbookEntry();

                le.User = szUser;
                le.AircraftID = AircraftID;
                le.Approaches = Approaches;
                le.Landings = Landings;
                le.FullStopLandings = cFullStopLandings;
                le.NightLandings = cNightLandings;
                le.CrossCountry = CrossCountry;
                le.Nighttime = Nighttime;
                le.IMC = IMC;
                le.SimulatedIFR = SimulatedIFR;
                le.GroundSim = GroundSim;
                le.Dual = Dual;
                le.PIC = PIC;
                le.TotalFlightTime = TotalFlightTime;
                le.fHoldingProcedures = HoldingProcedures;
                le.Route = Route;
                le.Comment = Comment;
                le.fIsPublic = isPublic;
                le.Date = DateOfFlight;
                le.FlightID = FlightID;
                le.HobbsStart = HobbsStart;
                le.HobbsEnd = HobbsEnd;
                le.EngineStart = EngineStart;
                le.EngineEnd = EngineEnd;
                le.FlightStart = FlightStart;
                le.FlightEnd = FlightEnd;

                return LogbookEntryForUserLE(szAuthUserToken, le, fTweet);
            }

            return null;
        }
        */

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
        private static System.Object idempotencyLock = new System.Object();

        /// <summary>
        /// Validates a flight for submission and does any appropriate cleanup.  Throws MyFlightbookException on any validation failure
        /// </summary>
        /// <param name="le"></param>
        /// <param name="szUser"></param>
        private void PrepareFlightForSubmission(LogbookEntry le, string szUser)
        {
            // clean up user name in case an email address is passed in.
            if (le.User.Contains("@"))
                le.User = Membership.GetUserNameByEmail(le.User);

            if (szUser.Length == 0)
                throw new MyFlightbookException(Resources.WebService.errBadAuth);

            // should never happen
            if (String.IsNullOrEmpty(le.User))
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
                if (le.CustomProperties.Length == 0 && leExisting.CustomProperties.Length > 0)
                    le.CustomProperties = leExisting.CustomProperties;
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
            le.AutofillForAircraft(ua.GetUserAircraftByID(le.AircraftID));

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
        private void AfterFlightCommitTasks(LogbookEntry le, PostingOptions po)
        {
            Profile pf = Profile.GetUser(le.User);
            if (po != null)
            {
                if (po.PostToTwitter)
                {
                    if (pf.CanTweet())
                        new TwitterPoster().PostToSocialMedia(le, pf.UserName);
                    else
                        util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.WebService.NotifyTwitterSetup, Branding.CurrentBrand.AppName),
                            String.Format(CultureInfo.CurrentCulture, Resources.EmailTemplates.EnableTwitter, pf.UserFullName),
                            new MailAddress(pf.Email, pf.UserFullName),
                            false, false);
                }

                if (po.PostToFacebook)
                {
                    if (pf.CanPostFacebook())
                        ThreadPool.QueueUserWorkItem(new WaitCallback(PostFacebookAfterDelay), new DelayedPost(le, HttpContext.Current.Request.Url.Host));
                    else
                        MFBFacebook.NotifyFacebookNotSetUp(pf.UserName);
                }
            }

            // EventRecorder.WriteEvent(EventRecorder.MFBEventID.CommitFlight, szUser, "Commit Flight");
            EventRecorder.UpdateCount(EventRecorder.MFBCountID.WSFlight, 1);

            pf.SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);
        }
        #endregion

        [WebMethod]
        public LogbookEntry CommitFlightWithOptions(string szAuthUserToken, LogbookEntry le, PostingOptions po)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            if (szAuthUserToken == null)
                throw new ArgumentNullException("szAuthUserToken");

            string szUser = GetEncryptedUser(szAuthUserToken);

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
                    FlightQuery fq = new FlightQuery(szUser);
                    fq.DateRange = FlightQuery.DateRanges.Custom;
                    fq.DateMax = fq.DateMin = le.Date;

                    LogbookEntry[] rgleDupes = FlightsWithQuery(fq, 0, -1);
                    if (rgleDupes != null && rgleDupes.Length > 0)
                    {
                        // Fix up property type - this isn't passed in consistently and breaks equality check!
                        if (le.CustomProperties == null)
                            le.CustomProperties = new CustomFlightProperty[0];
                        else
                        {
                            IEnumerable<CustomPropertyType> allProps = CustomPropertyType.GetCustomPropertyTypes();
                            foreach (CustomFlightProperty cfp in le.CustomProperties)
                                cfp.InitPropertyType(allProps);
                        }

                        foreach (LogbookEntry l in rgleDupes)
                        {
                            l.CustomProperties = CustomFlightProperty.LoadPropertiesForFlight(l.FlightID);
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
                AfterFlightCommitTasks(le, po);
                return le;
            }
            else
                throw new MyFlightbookException(le.ErrorString);
        }

        static void PostFacebookAfterDelay(object delayedPost)
        {
            DelayedPost dp = (DelayedPost)delayedPost;
            Thread.Sleep(60 * 1000); // sleep for a minute to allow any pictures to be posted
            new FacebookPoster().PostToSocialMedia(dp.Entry, dp.Entry.User, Branding.CurrentBrand.HostName);
        }

        /*
        /// <summary>
        /// Updates or creates a logbook entry for the authenticated user
        /// </summary>
        /// <param name="szAuthUserToken">Authorization token identifying the user</param>
        /// <param name="le">The logbook entry</param>
        /// <returns>The entry, if successful</returns>
        [WebMethod]
        public LogbookEntry LogbookEntryForUserLE(string szAuthUserToken, LogbookEntry le, Boolean fTweet)
        {
            EventRecorder.WriteEvent(EventRecorder.MFBEventID.ObsoleteAPI, GetEncryptedUser(szAuthUserToken), "Obsolete API: LogbookEntryForUserLE");
            return CommitFlightWithOptions(szAuthUserToken, le, new PostingOptions(fTweet, false));
        }
        */

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
            string szResult = null;
            if (szUser.Length > 0)
            {
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idFlight, szUser, LogbookEntry.LoadTelemetryOption.LoadAll) && le.Telemetry.HasPath)
                {
                    using (FlightData fd = new FlightData())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            if (fd.ParseFlightData(le.FlightData) && fd.HasLatLongInfo)
                                fd.WriteGPXData(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            szResult = new StreamReader(ms).ReadToEnd();
                        }
                    }
                }
            }
            return szResult;
        }

        /// <summary>
        /// Returns a list of the available custom property types.
        /// </summary>
        /// <returns>An array of custom property types</returns>
        [WebMethod]
        public CustomPropertyType[] AvailablePropertyTypes()
        {
            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);
            return CustomPropertyType.GetCustomPropertyTypes("");
        }

        [WebMethod]
        public CustomPropertyType[] AvailablePropertyTypesForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
            return CustomPropertyType.GetCustomPropertyTypes(szUser);
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
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(idFlight, szUser))
                    return CustomFlightProperty.LoadPropertiesForFlight(idFlight);
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
                LogbookEntry le = new LogbookEntry();
                // FLoadFromDB will load custom properties but will
                // also verify for us that the user owns the flight, and hence the properties.
                // we will only delete those properties in the flight which are in the array of property IDs.
                if (le.FLoadFromDB(idFlight, szUser))
                {
                    System.Collections.Generic.List<CustomFlightProperty> lstCFP = new System.Collections.Generic.List<CustomFlightProperty>(le.CustomProperties);
                    for (int i = le.CustomProperties.Length - 1; i >= 0; i--)
                    {
                        for (int j = 0; j < rgPropIds.Length; j++)
                            if (le.CustomProperties[i].PropID == rgPropIds[j])
                            {
                                lstCFP.Remove(le.CustomProperties[i]);
                                le.CustomProperties[i].DeleteProperty();
                            }
                    }
                    le.CustomProperties = lstCFP.ToArray();
                }
                if (le.CFISignatureState != LogbookEntry.SignatureState.None)
                    le.FCommit(); // forces a refresh of the signature state
                Profile.GetUser(szUser).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);
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
            int[] rgPropIds = new int[1];
            rgPropIds[0] = propId;
            DeletePropertiesForFlight(szAuthUserToken, idFlight, rgPropIds);
        }

        /// <summary>
        /// Determines if the specified user is authorized to modify/delete an image
        /// </summary>
        /// <param name="mfbii">The image</param>
        /// <param name="szuser">The user</param>
        /// <exception cref="UnauthorizedAccessException">Throws UnauthorizedAccessException if user isn't authorized </exception>
        /// <exception cref="ArgumentNullException"></exception>"
        protected void CheckUserOwnsImage(MFBImageInfo mfbii, string szuser)
        {
            if (mfbii == null)
                throw new ArgumentNullException("mfbii");

            if (String.IsNullOrEmpty(szuser))
                throw new UnauthorizedAccessException();

            switch (mfbii.Class)
            {
                case MFBImageInfo.ImageClass.Aircraft:
                    if (!new UserAircraft(szuser).CheckAircraftForUser(new Aircraft(Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture))))
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfo.ImageClass.BasicMed:
                    {
                        int idBME = Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture);
                        List<MyFlightbook.Basicmed.BasicMedEvent> lst = new List<Basicmed.BasicMedEvent>(Basicmed.BasicMedEvent.EventsForUser(szuser));
                        if (!lst.Exists(bme => bme.ID == idBME))
                            throw new UnauthorizedAccessException();
                    }
                    break;
                case MFBImageInfo.ImageClass.Endorsement:
                    if (szuser.CompareCurrentCultureIgnoreCase(mfbii.Key) != 0)
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfo.ImageClass.Flight:
                    if (!new LogbookEntry().FLoadFromDB(Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture), szuser))
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfo.ImageClass.Unknown:
                default:
                    throw new UnauthorizedAccessException();
            }
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
                throw new ArgumentNullException("mfbii");
            string szUser = GetEncryptedUser(szAuthUserToken);

            // Hack to work around a bug: the incoming mfbii (at least from iPhone) specifies JPG as image type...always (not initialized). So...
            // do a sanity check.
            mfbii.FixImageType();

            CheckUserOwnsImage(mfbii, szUser);
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
                throw new ArgumentNullException("mfbii");
            string szUser = GetEncryptedUser(szAuthUserToken);

            CheckUserOwnsImage(mfbii, szUser);

            if (mfbii.ThumbnailFile.Length > 0 && mfbii.VirtualPath.Length > 0)
                mfbii.UpdateAnnotation(mfbii.Comment);
        }

        /// <summary>
        /// Determines if we are on a secure connection, or if we are exempt from a secure connection (local or 192 request)
        /// </summary>
        /// <param name="r">The request</param>
        /// <returns>True if we are secure OR exempt from security</returns>
        public static bool CheckSecurity(HttpRequest r)
        {
            if (r == null)
                throw new ArgumentNullException("r");
            return (r.IsSecureConnection || r.Url.Host.StartsWith("192.168", StringComparison.OrdinalIgnoreCase) || r.Url.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) || r.IsLocal);
        }

        private static string AuthTokenForValidatedUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException("szUser");
            return new Encryptors.WebServiceEncryptor().Encrypt(szUser + ";" + DateTime.Now.Ticks);
        }

        public static string AuthTokenFromOAuthToken(DotNetOpenAuth.OAuth2.ChannelElements.AuthorizationDataBag token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

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
            // Only speak to authorized clients.
            if (String.IsNullOrEmpty(szAppToken) || !IsAuthorizedService(szAppToken))
                return null;

            if (szUser == null)
                throw new ArgumentNullException("szUser");
            if (szPass == null)
                throw new ArgumentNullException("szPass");

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

            if (!CheckSecurity(HttpContext.Current.Request))
                throw new MyFlightbookException(Resources.WebService.errNotSecureConnection);

            // In case the password has an html entity in it, may need to compare using that as well.  Most of the time, though, shouldn't need to.
            // Slight efficiency gain by not doing two validations if the unescaped password is the same as the escaped one
            string szUnescapedPass = szPass.UnescapeHTML();
            bool fValid = (util.ValidateUser(szUser, szPass).CompareOrdinal(szUser) == 0) ||
                            (szPass.CompareOrdinal(szUnescapedPass) != 0 && (util.ValidateUser(szUser, szUnescapedPass).CompareOrdinal(szUser) == 0));

            if (fValid)
            {
                if (!String.IsNullOrEmpty(szEmulate))   // emulation requested - validate that the authenticated user is actually authorized!!!
                {
                    Profile pf = Profile.GetUser(szUser);
                    if (pf.CanSupport || pf.CanManageData)
                    {
                        // see if the emulated user actually exists
                        pf = new Profile();
                        if (pf.LoadUser(szEmulate))
                            szUser = szEmulate;
                        else
                            throw new MyFlightbookException("No such user: " + szEmulate);
                    }
                    else
                        throw new UnauthorizedAccessException();
                }
                EventRecorder.WriteEvent(EventRecorder.MFBEventID.AuthUser, szUser, String.Format(CultureInfo.InvariantCulture,"Auth User - {0} from {1}.", HttpContext.Current.Request.IsSecureConnection ? "Secure" : "NOT SECURE", szAppToken));

                return AuthTokenForValidatedUser(szUser);
            }

            return null;
        }

        private static System.Object lockObject = new System.Object();

        #region new user validation
        /// <summary>
        /// Checks the password - throws an exception if there is an issue with it.
        /// </summary>
        /// <param name="szPass"></param>
        private void ValidatePassword(string szPass)
        {
            if (String.IsNullOrEmpty(szPass))
                throw new MyFlightbookException(Resources.Profile.errNoPassword);

            if (szPass.Length < 6 || szPass.Length > 20)
                throw new MyFlightbookException(Resources.Profile.errBadPasswordLength);
        }

        /// <summary>
        /// Checks the email - throws an exception if there is an issue.
        /// </summary>
        /// <param name="szEmail"></param>
        private void ValidateEmail(string szEmail)
        {
            Regex r = new Regex("\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m = r.Match(szEmail);
            if (m.Captures.Count != 1 || m.Captures[0].Value.CompareOrdinal(szEmail) != 0)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Profile.errInvalidEmail, szEmail));
        }

        /// <summary>
        /// Checks the question and answer - throws an exception if there is an issue
        /// </summary>
        /// <param name="szQuestion"></param>
        /// <param name="szAnswer"></param>
        private void ValidateQandA(string szQuestion, string szAnswer)
        {
            if (String.IsNullOrEmpty(szQuestion))
                throw new MyFlightbookException(Resources.Profile.errNoQuestion);

            if (String.IsNullOrEmpty(szAnswer))
                throw new MyFlightbookException(Resources.Profile.errNoAnswer);

            if (szQuestion.Length > 80)
                throw new MyFlightbookException(Resources.Profile.errQuestionTooLong);
            if (szAnswer.Length > 80)
                throw new MyFlightbookException(Resources.Profile.errAnswerTooLong);
        }
        #endregion

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
            if (IsAuthorizedService(szAppToken))
            {
                // do a few simple validations
                if (!CheckSecurity(HttpContext.Current.Request))
                    throw new MyFlightbookException(Resources.WebService.errNotSecureConnection);

                ValidateEmail(szEmail);
                ValidatePassword(szPass);
                ValidateQandA(szQuestion, szAnswer);

                // create a username from the email address
                string szUser = util.UserNameForEmail(szEmail);

                szFirst = szFirst ?? string.Empty;
                szLast = szLast ?? string.Empty;

                UserEntity ue = new UserEntity();

                lock (lockObject)
                {
                    ue.mcs = util.CreateUser(szUser, szPass.UnescapeHTML(), szEmail, szQuestion.UnescapeHTML(), szAnswer.UnescapeHTML());
                }

                string szErr = "";
                switch (ue.mcs)
                {
                    case MembershipCreateStatus.Success:
                        ue.szUsername = szUser;
                        ue.szAuthToken = AuthTokenForUser(szAppToken, szUser, szPass);

                        try
                        {
                            // set the first/last name for the user
                            util.FinalizeUser(szUser, szFirst, szLast, true);
                            EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateUser, szUser, String.Format(CultureInfo.InvariantCulture,"User '{0}' was created at {1}, connection {2} - {3}", szUser, DateTime.Now.ToShortDateString(), HttpContext.Current.Request.IsSecureConnection ? "Secure" : "NOT SECURE", szAppToken));
                        }
                        catch (Exception ex)
                        {
                            EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateUserError, szUser, "Exception creating user: " + ex.Message);
                            util.NotifyAdminEvent("Error creating new user: ", "Username: " + szUser + "\r\n\r\n" + ex.Message, ProfileRoles.maskSiteAdminOnly);
                            throw new MyFlightbookException("Error creating new user: " + ex.Message);
                        }

                        return ue;
                    case MembershipCreateStatus.InvalidAnswer:
                        szErr = String.Format(CultureInfo.CurrentCulture,Resources.Profile.errInvalidAnswer, szAnswer);
                        break;
                    case MembershipCreateStatus.DuplicateEmail:
                    case MembershipCreateStatus.DuplicateUserName:
                        szErr = String.Format(CultureInfo.CurrentCulture,Resources.Profile.errEmailInUse, szEmail);
                        break;
                    case MembershipCreateStatus.InvalidEmail:
                    case MembershipCreateStatus.InvalidUserName:
                        szErr = String.Format(CultureInfo.CurrentCulture,Resources.Profile.errInvalidEmail, szEmail);
                        break;
                    case MembershipCreateStatus.InvalidPassword:
                        szErr = Resources.Profile.errInvalidPassword;
                        break;
                    case MembershipCreateStatus.InvalidQuestion:
                        szErr = String.Format(CultureInfo.CurrentCulture,Resources.Profile.errInvalidQuestion, szQuestion);
                        break;
                    default:
                        szErr = String.Format(CultureInfo.CurrentCulture,Resources.Profile.errGenericCreateAccountError, ue.mcs.ToString());
                        break;
                }

                if (szErr.Length == 0)
                    return null;
                else
                    throw new MyFlightbookException(szErr);
            }
            else
                throw new MyFlightbookException("Unauthorized!");
        }

        /// <summary>
        /// Determines if the calling referrer is authorized to call us
        /// </summary>
        /// <returns></returns>
        private bool IsValidCaller()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
                return false;
            if (HttpContext.Current.Request.IsLocal)
                return true;
            if (HttpContext.Current.Request.UrlReferrer == null)
                return false;
            if (HttpContext.Current.Request.UrlReferrer.Host.EndsWith(Branding.CurrentBrand.HostName, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        #region Autocomplete
        private const string keyColumn = "ColumnKey";

        public string[] GetKeysFromDB(string szQ, string prefixText)
        {
            ArrayList al = new ArrayList();
            DBHelper dbo = new DBHelper(szQ);

            if (dbo.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("prefix", prefixText); },
                (dr) => { al.Add(dr[keyColumn].ToString()); }))
                return (string[])al.ToArray(typeof(string));
            return new string[0];
        }

        private string[] DoSuggestion(string szQ, string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText) || string.IsNullOrEmpty(szQ) || prefixText.Length <= 2)
                return new string[0];

            string[] rgsz = GetKeysFromDB(String.Format(CultureInfo.InvariantCulture,szQ, keyColumn, count), prefixText);

            ArrayList responses = new ArrayList(count);

            int i = 0;
            while (responses.Count < count && i < rgsz.Length)
            {
                if (rgsz[i].StartsWith(prefixText, StringComparison.CurrentCultureIgnoreCase))
                    responses.Add(rgsz[i]);
                i++;
            }

            return (string[])responses.ToArray(typeof(string));
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestAircraft(string prefixText, int count)
        {
            return DoSuggestion("SELECT tailnumber AS {0} FROM aircraft WHERE tailnumber LIKE CONCAT(?prefix, '%') ORDER BY tailnumber ASC LIMIT {1}", prefixText, count);
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestModels(string prefixText, int count)
        {
            return DoSuggestion("SELECT model AS {0} FROM models WHERE model LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", prefixText, count);
        }

        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] PreviouslyUsedTextProperties(string prefixText, int count, string contextKey)
        {
            string[] rgResultDefault = new string[0];

            if (String.IsNullOrEmpty(contextKey))
                return rgResultDefault;

            string[] rgsz = contextKey.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (rgsz.Length != 2 || String.IsNullOrEmpty(rgsz[0]) || string.IsNullOrEmpty(rgsz[1]))
                return rgResultDefault;

            int idPropType;

            if (!Int32.TryParse(rgsz[1], out idPropType))
                return rgResultDefault;

            Dictionary<int, List<string>> d = CustomFlightProperty.PreviouslyUsedTextValuesForUser(rgsz[0]);

            List<string> lst = (d.ContainsKey(idPropType)) ? d[idPropType] : null;
            if (lst == null)
                return new string[0];

            string szSearch = prefixText.ToUpperInvariant();

            return lst.FindAll(sz => sz.ToUpperInvariant().StartsWith(szSearch)).ToArray();
        }
        #endregion

        #region Other Ajax
        [WebMethod]
        public airport[] AirportsInBoundingBox(double latSouth, double lonWest, double latNorth, double lonEast)
        {
            return (IsValidCaller()) ? airport.AirportsWithinBounds(latSouth, lonWest, latNorth, lonEast).ToArray() : new airport[0];
        }
        #endregion
    }
}