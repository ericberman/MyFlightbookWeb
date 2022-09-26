using DotNetOpenAuth.OAuth2;
using MyFlightbook.ImportFlights;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.RosterBuster
{
    #region Flight format
    /// <summary>
    /// An airport that can be an arrival or destination
    /// </summary>
    [Serializable]
    public class RosterBusterEndpoint
    {
        public DateTime? Time { get; set; }
        public long UNIX { get; set; }
        public string IATA { get; set; } = string.Empty;
        public string ICAO { get; set; } = string.Empty;
        public string Name { get; set; }
        public string City { get; set; }
        public string ISO { get; set; }
        public string ISO_region { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Timezone { get; set; }
    }

    /// <summary>
    /// Flightstats portion of Rosterbuster flight
    /// </summary>
    [Serializable]
    public class RosterBusterFlightStats
    {
        public string pk { get; set; }
        public DateTime? date { get; set; }
        public string carrierFsCode { get; set; }
        public string flightnumber { get; set; }
        public string iata { get; set; }
        public string icao { get; set; }
        public string updated_at { get; set; }
        public string turboProp { get; set; }
        public string equipmentName { get; set; }
        public string equipmentIata { get; set; }
        public string jet { get; set; }
        public string widebody { get; set; }
        public string regional { get; set; }
        public string status { get; set; }
        public string tailNumber { get; set; }
        public DateTime? publishedDeparture { get; set; }
        public DateTime? publishedDepartureLocal { get; set; }
        public DateTime? publishedArrival { get; set; }
        public DateTime? publishedArrivalLocal { get; set; }
        public DateTime? scheduledGateDeparture { get; set; }
        public DateTime? scheduledGateDepartureLocal { get; set; }
        public DateTime? scheduledGateArrival { get; set; }
        public DateTime? scheduledGateArrivalLocal { get; set; }
        public int? scheduledBlockMinutes { get; set; }
        public int? blockMinutes { get; set; }
        public int? scheduledTaxiOutMinutes { get; set; }
        public int? taxiOutMinutes { get; set; }
        public int? scheduledTaxiInMinutes { get; set; }
        public int? taxiInMinutes { get; set; }
        public int? departureGateDelayMinutes { get; set; }
        public int? departureRunwayDelayMinutes { get; set; }
        public int? arrivalGateDelayMinutes { get; set; }
        public int? arrivalRunwayDelayMinutes { get; set; }
        public string departureTerminal { get; set; }
        public string departureGate { get; set; }
        public string arrivalTerminal { get; set; }
        public string arrivalGate { get; set; }
        public DateTime? actualRunwayArrival { get; set; }
        public DateTime? actualRunwayArrivalLocal { get; set; }
    }

    [Serializable]
    public class RosterBusterAircraft
    {
        public string Type { get; set; }
    }

    [Serializable]
    public class RosterBusterAirline
    {
        public string IATA { get; set; }
        public string ICAO { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class RosterBusterFlightDetails
    {
        public string flightKey { get; set; }
        public string number { get; set; }
        public string tailnumber { get; set; }
        public string duration { get; set; }
        public int? distance { get; set; }
        public string crew { get; set; }
    }

    [Serializable]
    public class RosterBusterEntry : ExternalFormat
    {
        public string uid { get; set; }
        public string type { get; set; }
        public bool active { get; set; }
        public bool allday { get; set; }
        public RosterBusterFlightDetails flight { get; set; }
        public RosterBusterAirline airline { get; set; }
        public RosterBusterAircraft aircraft { get; set; }
        public RosterBusterEndpoint departure { get; set; }
        public RosterBusterEndpoint arrival { get; set; }

        public RosterBusterFlightStats flightstats { get; set; }

        public bool IsFlight
        {
            get { return type.CompareCurrentCultureIgnoreCase("flight") == 0; }
        }

        public override LogbookEntry ToLogbookEntry()
        {
            if (!IsFlight)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Attempt to convert entry of type {0} to a flight", type));

            decimal totalFlight = !String.IsNullOrEmpty(flight.duration) ? flight.duration.DecimalFromHHMM() : (flightstats.blockMinutes != null ? flightstats.blockMinutes.Value / 60.0M : 0);

            PendingFlight pf = new PendingFlight()
            {
                TailNumDisplay = flight.tailnumber ?? flightstats.tailNumber ?? string.Empty,
                Route = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, 
                    String.IsNullOrEmpty(departure.ICAO) ? departure.IATA : departure.ICAO, 
                    String.IsNullOrEmpty(arrival.ICAO) ? arrival.IATA : arrival.ICAO).Trim(),
                Date = departure.Time ?? (flightstats.date ?? DateTime.MinValue),
                FlightEnd = flightstats.actualRunwayArrival ?? DateTime.MinValue,
                TotalFlightTime = totalFlight
            };

            DateTime? blockOut = flightstats.scheduledGateDeparture ?? departure.Time;
            DateTime? blockIn = flightstats.scheduledGateArrival ?? arrival.Time;

            CustomFlightProperty cfpBlockOut = blockOut.HasValue ? CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockOut, DateTime.SpecifyKind(blockOut.Value, DateTimeKind.Utc), true) : null;
            CustomFlightProperty cfpBlockIn = blockIn.HasValue ? CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDBlockIn, DateTime.SpecifyKind(blockIn.Value, DateTimeKind.Utc), true) : null;

            if (cfpBlockOut != null && cfpBlockIn != null && flightstats.blockMinutes == 0)
                pf.TotalFlightTime = (decimal) Math.Max(blockIn.Value.Subtract(blockOut.Value).TotalHours, 0);

            string flightnum = !String.IsNullOrEmpty(flight.number) ? flight.number : flightstats.flightnumber ?? String.Empty;
            if (!String.IsNullOrEmpty(flightnum))
                flightnum = (airline.IATA ?? string.Empty) + flightnum;

            pf.CustomProperties.SetItems(new CustomFlightProperty[]
            {
                cfpBlockOut, 
                cfpBlockIn,
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightNumber, flightnum),
                CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropCrew1, flight.crew),
            });

            return pf;
        }
    }
    #endregion


    public class RosterBusterTokenResponse
    {
        public string status { get; set; }
        public Dictionary<string, string> data;
    }
    public class RosterBusterClient : OAuthClientBase
    {
        private const string rbDevHost = "app-dev.rosterbuster.aero";
        private const string rbLiveHost = "app.rosterbuster.aero";
        private const string rbDevAuthHost = "auth-dev.rbgroup.aero";
        private const string rbLiveAuthHost = "auth.rbgroup.aero";
        private const string rbAPIDev = "devapi.rosterbuster.com";
        private const string rbAPILive = "api.rosterbuster.com";
        private const string localConfigKey = "rbClientID";
        public const string szCachedPrefStateKey = "rbRequestState";
        private const string szCachedCodeVerifier = "rbCodeVerifier";
        public const string TokenPrefKey = "rosterBusterAuth";
        public const string RBDownloadLink = "https://k5745.app.goo.gl/WoQcV";

        private static bool UseSandbox(string host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            return !Branding.CurrentBrand.MatchesHost(host);
        }

        private string flightsEndpoint { get; set; }
        private string refreshEndpoint { get; set; }

        private static IAuthorizationState AuthStateFromJSON(string szJSON)
        {
            if (String.IsNullOrEmpty(szJSON))
                return null;
            RosterBusterTokenResponse resp = JsonConvert.DeserializeObject<RosterBusterTokenResponse>(szJSON);
            return new AuthorizationState
            {
                AccessToken = resp.data["access_token"],
                RefreshToken = resp.data["refresh_token"],
                AccessTokenIssueDateUtc = DateTime.UtcNow,
                AccessTokenExpirationUtc = DateTime.SpecifyKind(Convert.ToDateTime(resp.data["expired_at"], CultureInfo.InvariantCulture), DateTimeKind.Utc)
            };
        }

        #region importFlights
        /// <summary>
        /// Retrieves a set of flights.  Be sure to refresh token first!
        /// </summary>
        /// <param name="dtStart"></param>
        /// <param name="dtEnd"></param>
        /// <returns></returns>
        public async Task<bool> GetFlights(string szUser, DateTime? dtStart, DateTime? dtEnd)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            if (dtStart.HasValue)
                nvc.Add("startTime", dtStart.Value.Date.YMDString());
            if (dtEnd.HasValue)
                nvc.Add("endTime", dtEnd.Value.Date.AddDays(1).YMDString());    // end at the start of the next day.

            UriBuilder uriBuilder = new UriBuilder(flightsEndpoint)
            {
                Query = nvc.ToString()
            };

            return (bool)await SharedHttpClient.GetResponseForAuthenticatedUri(uriBuilder.Uri, AuthState.AccessToken, HttpMethod.Get, (response) =>
            {
                try
                {
                    string szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    IEnumerable<RosterBusterEntry> flights = JsonConvert.DeserializeObject<IEnumerable<RosterBusterEntry>>(szResult);
                    List<PendingFlight> lst = new List<PendingFlight>();
                    // set the username for each of these.
                    foreach (RosterBusterEntry entry in flights)
                    {
                        if (!entry.IsFlight)
                            continue;

                        if (entry.ToLogbookEntry() is PendingFlight pf)
                        {
                            pf.User = szUser;

                            // Date is likely UTC at this point because it comes from a UTC value like departure time.
                            // Autofill will reset it to match the first (pseudo) time stamp, using the AutoFillOptions offset, but that's
                            // generally a bad offset to use because that's global, not based on the departure airport.
                            DateTime dtSave = pf.Date;
                            using (FlightData fd = new FlightData())
                                fd.AutoFill(pf, AutoFillOptions.DefaultOptionsForUser(szUser));
                            pf.Date = dtSave;
                            pf.Commit();
                        }
                    }
                    return true;
                }
                catch (HttpRequestException ex)
                {
                    if (response == null)
                        throw new MyFlightbookException("Unknown error in RosterBuster GetFlights", ex);
                    else
                        throw new MyFlightbookException("Error in RosterBuster GetFlights: " + response.ReasonPhrase, ex);
                }
            });
        }
        #endregion

        #region oAuthFlow
        public Uri AuthorizationUri(string szRedir, string szUser)
        {
            if (String.IsNullOrEmpty(szRedir))
                throw new ArgumentNullException(nameof(szRedir));
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            // Generate a state variable and store it.
            Guid guid = Guid.NewGuid();
            string szState = Regex.Replace(guid.ToString(), "[^a-zA-Z0-9]", string.Empty);
            Profile pf = Profile.GetUser(szUser);
            pf.AssociatedData[szCachedPrefStateKey] = szState;  // save state so we can check it later.

            // Build a code_verifier
            string szCodeVerifier = Regex.Replace(Guid.NewGuid().ToString() + Guid.NewGuid().ToString(), "[^a-zA-Z0-9]", string.Empty);
            pf.AssociatedData[szCachedCodeVerifier] = szCodeVerifier;


            UriBuilder uriBuilder = new UriBuilder(oAuth2AuthorizeEndpoint);
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["code_challenge"] = GenerateCodeChallenge(szCodeVerifier);
            nvc["code_challenge_method"] = "S256";
            nvc["response_type"] = "code";
            nvc["consumer_key"] = AppKey;
            nvc["redirect_uri"] = szRedir;
            nvc["state"] = szState;
            uriBuilder.Query = nvc.ToString();
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Refreshes the token; no-op if at least 5 minutes before expiration
        /// </summary>
        /// <param name="authstate"></param>
        /// <returns>a usable authstate</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<IAuthorizationState> RefreshToken()
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>()
            {
                { "consumer_key", AppKey },
                { "refresh_token", AuthState.RefreshToken }
            };

            List<IDisposable> objectsToDispose = new List<IDisposable>();

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                try
                {
                    // Add each of the parameters
                    foreach (string key in postParams.Keys)
                    {
                        StringContent sc = new StringContent(postParams[key]);
                        form.Add(sc);
                        sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
                        objectsToDispose.Add(sc);
                    }

                    IAuthorizationState authstateNew = null;

                    string error = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(refreshEndpoint), null, HttpMethod.Post, form, (response) =>
                    {
                        string szResult = string.Empty;
                        try
                        {
                            szResult = response.Content.ReadAsStringAsync().Result;
                            response.EnsureSuccessStatusCode();

                            authstateNew = AuthStateFromJSON(szResult);
                            return string.Empty;
                        }
                        catch (System.Threading.ThreadAbortException ex)
                        {
                            return ex.Message;
                        }
                        catch (Exception ex) when (ex is HttpUnhandledException || ex is HttpException || ex is HttpRequestException || ex is System.Net.WebException)
                        {
                            return String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szResult);
                        }
                    });

                    if (String.IsNullOrEmpty(error))
                    {
                        if (authstateNew == null)
                            throw new UnauthorizedAccessException("Refresh failed!!!");
                        else
                            return AuthState = authstateNew;
                    }
                    else
                        throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.RosterBusterTokenFailed, error));
                }
                finally
                {
                    foreach (IDisposable disposable in objectsToDispose)
                        disposable.Dispose();
                }
            }
        }

        public async Task<IAuthorizationState> ConvertToken(string szRedir, string szAuthCode, string szState, string szUser)
        {
            if (String.IsNullOrEmpty(szRedir))
                throw new ArgumentNullException(nameof(szRedir));
            if (String.IsNullOrEmpty(szAuthCode))
                throw new ArgumentNullException(nameof(szAuthCode));
            if (String.IsNullOrEmpty(szState))
                throw new ArgumentNullException(nameof(szState));
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            if (!pf.AssociatedData.TryGetValue(szCachedPrefStateKey, out object oState) || !pf.AssociatedData.TryGetValue(szCachedCodeVerifier, out object oCodeVerifier))
                throw new InvalidOperationException("No pending authorization request for user.");
            string szSavedState = oState as string;
            if (szSavedState.CompareTo(szState) != 0)
                throw new InvalidOperationException("State has been modified - invalid request!");

            Dictionary<string, string> postParams = new Dictionary<string, string>()
            {
                { "consumer_key", AppKey },
                { "grant_type", "authorization_code" },
                { "state", szSavedState },
                { "code", szAuthCode },
                { "code_verifier", (string) oCodeVerifier },
                { "redirect_uri", szRedir }
            };

            List<IDisposable> objectsToDispose = new List<IDisposable>();

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                try
                {
                    // Add each of the parameters
                    foreach (string key in postParams.Keys)
                    {
                        StringContent sc = new StringContent(postParams[key]);
                        form.Add(sc);
                        sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
                        objectsToDispose.Add(sc);
                    }

                    IAuthorizationState authstate = null;
                    string error = (string) await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(oAuth2TokenEndpoint), null, HttpMethod.Post, form, (response) =>
                    {
                        string szResult = string.Empty;
                        try
                        {
                            szResult = response.Content.ReadAsStringAsync().Result;
                            response.EnsureSuccessStatusCode();

                            authstate = AuthStateFromJSON(szResult);

                            // if we're here, we can free up the state and challenge
                            pf.AssociatedData.Remove(szCachedCodeVerifier);
                            pf.AssociatedData.Remove(szCachedPrefStateKey);

                            return string.Empty;
                        }
                        catch (System.Threading.ThreadAbortException ex)
                        {
                            return ex.Message;
                        }
                        catch (Exception ex) when (ex is HttpUnhandledException || ex is HttpException || ex is HttpRequestException || ex is System.Net.WebException)
                        {
                            return String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szResult);
                        }
                    });

                    if (String.IsNullOrEmpty(error))
                    {
                        if (authstate == null)
                            throw new UnauthorizedAccessException("No authstate returned!!!");
                        else
                            return authstate;
                    }
                    else
                        throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.RosterBusterTokenFailed, error));
                }
                finally
                {
                    foreach (IDisposable disposable in objectsToDispose)
                        disposable.Dispose();
                }
            }
        }

        public RosterBusterClient(string host) : base(localConfigKey, localConfigKey,
            String.Format(CultureInfo.InvariantCulture, "https://{0}/thirdparty/login", UseSandbox(host) ? rbDevHost : rbLiveHost),
            String.Format(CultureInfo.InvariantCulture, "https://{0}/rb/v1/thirdparty/token", UseSandbox(host) ? rbDevAuthHost : rbLiveAuthHost),
            Array.Empty<string>())
        {
            flightsEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}/v2/thirdparty/flight-data", UseSandbox(host) ? rbAPIDev : rbAPILive);
            refreshEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}/rb/v1/thirdparty/refresh", UseSandbox(host) ? rbDevAuthHost : rbLiveAuthHost);
        }
    #endregion

    public RosterBusterClient(IAuthorizationState authstate, string szHost) : this(szHost)
        {
            AuthState = authstate;
        }
    }
}