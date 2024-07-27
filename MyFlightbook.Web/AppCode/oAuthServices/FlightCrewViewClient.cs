using DotNetOpenAuth.OAuth2;
using MyFlightbook.ImportFlights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.OAuth.FlightCrewView
{
    public class FlightCrewViewClient : OAuthClientBase
    {
        private const string authEndpoint = "https://www.flightcrewview2.com/logbook/logbookuserauth/";
        private const string tokenEndpoint = "https://www.flightcrewview2.com/logbook/api/token/";
        private const string disableEndpoint = "https://www.flightcrewview2.com/logbook/api/revokeToken/";
        private const string flightsEndpoint = "https://www.flightcrewview2.com/logbook/api/flights/";
        public const string AccessTokenPrefKey = "FlightCrewViewAuthToken";
        public const string LastAccessPrefKey = "FlightCrewViewLastDate";

        public FlightCrewViewClient() : base("FlightCrewViewClientID", "FlightCrewViewClientSecret", authEndpoint, tokenEndpoint, null, null, disableEndpoint)
        {
        }

        public FlightCrewViewClient(IAuthorizationState authstate) : this()
        {
            AuthState = authstate;
        }

        public static async Task<FlightCrewViewClient> RefreshedClientForUser(string szUsername)
        {
            if (String.IsNullOrEmpty(szUsername))
                throw new ArgumentNullException(nameof(szUsername));

            Profile pf = Profile.GetUser(szUsername);

            FlightCrewViewClient fcv = new FlightCrewViewClient(pf.GetPreferenceForKey<AuthorizationState>(AccessTokenPrefKey));
            if (fcv.AuthState == null)
                throw new UnauthorizedAccessException("No FlightCrewView Authorization State");

            if (!fcv.CheckAccessToken() && !String.IsNullOrEmpty(fcv.AuthState.RefreshToken))
            {
                fcv.AuthState = await fcv.RefreshAccessToken(fcv.AuthState.RefreshToken, fcv.AuthState.Callback.ToString());
                pf.SetPreferenceForKey(AccessTokenPrefKey, fcv.AuthState);
            }

            return fcv;
        }

        public async Task<IEnumerable<PendingFlight>> FlightsFromDate(string szUser, DateTime? dtFrom, DateTime? dtTo)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            if (dtFrom.HasValue && dtFrom.Value.HasValue())
                nvc["start_datetime_utc"] = dtFrom.Value.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            if (dtTo.HasValue && dtTo.Value.HasValue())
                nvc["end_datetime_utc"] = dtTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 23:59:59";
            string endpoint = String.Format(CultureInfo.InvariantCulture, "{0}?{1}", flightsEndpoint, nvc.ToString());

            return (IEnumerable<PendingFlight>)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(endpoint), AuthState.AccessToken, HttpMethod.Get, null, (response) =>
            {
                string szResult = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();

                FlightCrewFlightsResult fr = JsonConvert.DeserializeObject< FlightCrewFlightsResult >(szResult);

                List<PendingFlight> lst = new List<PendingFlight>();
                foreach (FlightCrewViewFlight fcvf in fr.flights)
                {
                    PendingFlight pf = fcvf.ToLogbookEntry() as PendingFlight;
                    pf.User = szUser;
                    lst.Add(pf);
                    pf.Commit();
                }

                return lst;
            });
        }
    }
}