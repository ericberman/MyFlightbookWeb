using DotNetOpenAuth.OAuth2;
using MyFlightbook.ImportFlights;
using MyFlightbook.Telemetry;
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
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.OAuth.FlightCrewView
{
    public class FlightCrewViewClient : OAuthClientBase, IExternalFlightSource
    {
        private const string authEndpoint = "https://www.flightcrewview2.com/logbook/logbookuserauth/";
        private const string tokenEndpoint = "https://www.flightcrewview2.com/logbook/api/token/";
        private const string disableEndpoint = "https://www.flightcrewview2.com/logbook/api/revokeToken/";
        private const string flightsEndpoint = "https://www.flightcrewview2.com/logbook/api/flights/";
        public const string AccessTokenPrefKey = "FlightCrewViewAuthToken";
        public const string LastAccessPrefKey = "FlightCrewViewLastDate";

        #region IExternalFlightSource
        async Task<string> IExternalFlightSource.ImportFlights(string username, DateTime? startDate, DateTime? endDate, HttpRequestBase request)
        {
            try
            {
                if (AuthState == null)
                    throw new UnauthorizedAccessException("No FlightCrewView Authorization State");

                if (!CheckAccessToken() && !String.IsNullOrEmpty(AuthState.RefreshToken))
                {
                    Profile pf = Profile.GetUser(username);
                    try
                    {
                        AuthState = await RefreshAccessToken(AuthState.RefreshToken, AuthState.Callback.ToString());
                        pf.SetPreferenceForKey(AccessTokenPrefKey, AuthState);
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        // Issue # 1403 - refresh token only lasts for 3 months, if not used.  So clear the authorization and tell the user.
                        return Branding.ReBrand(Resources.LogbookEntry.FlightCrewViewRefreshFailed);
                    }
                }

                IEnumerable<PendingFlight> _ = await FlightsFromDate(username, startDate, endDate);
                return string.Empty;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion

        public FlightCrewViewClient() : base("FlightCrewViewClientID", "FlightCrewViewClientSecret", authEndpoint, tokenEndpoint, null, null, disableEndpoint)
        {
        }

        public FlightCrewViewClient(IAuthorizationState authstate) : this()
        {
            AuthState = authstate;
        }

        public async Task<IEnumerable<PendingFlight>> FlightsFromDate(string szUser, DateTime? dtFrom, DateTime? dtTo)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            if (dtFrom.HasValue && dtFrom.Value.HasValue())
                nvc["start_datetime_local"] = dtFrom.Value.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            if (dtTo.HasValue && dtTo.Value.HasValue())
                nvc["end_datetime_local"] = dtTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 23:59:59";
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

                    pf.MapTail();   // Map the display tail to a user aircraft.

                    // Date is likely UTC at this point because it comes from a UTC value like departure time.
                    // Autofill will reset it to match the first (pseudo) time stamp, using the AutoFillOptions offset, but that's
                    // generally a bad offset to use because that's global, not based on the departure airport.
                    DateTime dtSave = pf.Date;
                    using (FlightData fd = new FlightData())
                        fd.AutoFill(pf, AutoFillOptions.DefaultOptionsForUser(szUser));
                    pf.Date = dtSave;

                    lst.Add(pf);
                    pf.Commit();
                }

                return lst;
            });
        }
    }
}