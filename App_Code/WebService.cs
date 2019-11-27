using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using MyFlightbook.Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.Security;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
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
    /// Static class to encapsulate authoriztion to modify images.
    /// Want to keep MFBImageInfo (relatively) clean w.r.t. MyFlightbook classes; this encapsulates the semantic
    /// knowledge needed for services.
    /// </summary>
    public static class ImageAuthorization
    {
        /// <summary>
        /// Determines if the specified user is authorized to modify/delete an image
        /// </summary>
        /// <param name="mfbii">The image</param>
        /// <param name="szuser">The user</param>
        /// <exception cref="UnauthorizedAccessException">Throws UnauthorizedAccessException if user isn't authorized </exception>
        /// <exception cref="ArgumentNullException"></exception>"
        public static void ValidateAuth(MFBImageInfo mfbii, string szuser)
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
                return new Aircraft[0];

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
            fq.CustomRestriction = string.Empty;    // security sanity check
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
                    LogbookEntry le = new LogbookEntry(dr, fq.UserName)
                    {
                        // Note: this has no telemetry
                        // don't even bother sending this field down the wire.
                        FlightData = null
                    }; 

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
        private readonly static System.Object idempotencyLock = new System.Object();

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
        private void AfterFlightCommitTasks(LogbookEntry le)
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
                throw new ArgumentNullException("le");
            if (szAuthUserToken == null)
                throw new ArgumentNullException("szAuthUserToken");
            if (po != null)
                po.ToString();  // avoid warning about unused po

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
                        MemoryStream ms = new MemoryStream();

                        try
                        {
                            if (fd.ParseFlightData(le.FlightData) && fd.HasLatLongInfo)
                                fd.WriteGPXData(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            using (StreamReader sr = new StreamReader(ms))
                            {
                                ms = null;  // for CA2202
                                szResult = sr.ReadToEnd();
                            }
                        }
                        finally
                        {
                            if (ms != null)
                                ms.Dispose();
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

        [WebMethod]
        public TemplatePropTypeBundle PropertiesAndTemplatesForUser(string szAuthUserToken)
        {
            string szUser = GetEncryptedUser(szAuthUserToken);
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
                            le.CustomProperties.RemoveItem(id);
                            cfp.DeleteProperty();
                        }
                    }
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
                throw new ArgumentNullException("mfbii");
            string szUser = GetEncryptedUser(szAuthUserToken);

            // Hack to work around a bug: the incoming mfbii (at least from iPhone) specifies JPG as image type...always (not initialized). So...
            // do a sanity check.
            mfbii.FixImageType();

            ImageAuthorization.ValidateAuth(mfbii, szUser);
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

            ImageAuthorization.ValidateAuth(mfbii, szUser);

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
                        szUser = szEmulate;
                    }
                    else
                        throw new UnauthorizedAccessException();
                }
                EventRecorder.WriteEvent(EventRecorder.MFBEventID.AuthUser, szUser, String.Format(CultureInfo.InvariantCulture, "Auth User - {0} from {1}.", HttpContext.Current.Request.IsSecureConnection ? "Secure" : "NOT SECURE", szAppToken));

                return AuthTokenForValidatedUser(szUser);
            }

            return null;
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
            if (!CheckSecurity(HttpContext.Current.Request))
                throw new MyFlightbookException(Resources.WebService.errNotSecureConnection);

            // create a username from the email address
            string szUser = UserEntity.UserNameForEmail(szEmail);

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
                        ProfileAdmin.FinalizeUser(szUser, szFirst, szLast, true);
                        EventRecorder.WriteEvent(EventRecorder.MFBEventID.CreateUser, szUser, String.Format(CultureInfo.InvariantCulture, "User '{0}' was created at {1}, connection {2} - {3}", szUser, DateTime.Now.ToShortDateString(), HttpContext.Current.Request.IsSecureConnection ? "Secure" : "NOT SECURE", szAppToken));
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
                throw new ArgumentNullException("szAuthToken");
            if (fq == null)
                throw new ArgumentNullException("fq");
            if (szName == null)
                throw new ArgumentNullException("szName");
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
                throw new ArgumentNullException("szAuthToken");
            if (cq == null)
                throw new ArgumentNullException("cq");

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

            lst = lst.FindAll(sz => sz.ToUpperInvariant().StartsWith(szSearch));
            if (lst.Count > count && count >= 1)
                lst.RemoveRange(count - 1, lst.Count - count);

            return lst.ToArray();
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