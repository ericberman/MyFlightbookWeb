using DotNetOpenAuth.OAuth2;
using MyFlightbook.ImportFlights.Leon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2022-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.Leon
{
    public class LeonClient : OAuthClientBase, IExternalFlightSource
    {
        private const string leonDevHost = "daily.leon.aero";
        private const string leonLiveHost = "leon.aero";
        public const string TokenPrefKey = "LeonPrefKey";
        public const string SubDomainPrefKey = "LeonSubDomainPrefKey";

        #region IExternalFlightSource
        async Task<string> IExternalFlightSource.ImportFlights(string username, DateTime? startDate, DateTime? endDate, HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            try
            {
                Profile pf = Profile.GetUser(username);
                bool fNeedsRefresh = !CheckAccessToken();

                await ImportFlights(username, startDate, endDate);
                if (fNeedsRefresh)
                    pf.SetPreferenceForKey(TokenPrefKey, AuthState, AuthState == null);

                return string.Empty;    // no issues!
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion

        private string FlightsEndpoint { get; set; }

        private string RefreshEndpoint { get; set; }

        private string SubDomain { get; set; } = "man"; // default subdomain

        private static readonly string[] leonScopes = new string[] { /* "GRAPHQL_FLIGHT", "GRAPHQL_FLIGHT_EDIT", "GRAPHQL_FLIGHT_PERMITS_EDIT", "GRAPHQL_FLIGHT_WATCH_EDIT", "GRAPHQL_FLIGHT_WATCH", "GRAPHQL_SCHEDULE_ORDER_SEE", "GRAPHQL_RESERVATION_SEE" */ "LOGBOOK" };

        public static bool UseSandbox(string host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            return !Branding.CurrentBrand.MatchesHost(host);
        }

        public LeonClient(string subDomain, bool fUseSandbox = false) : base("LeonClientID",
            "LeonClientSecret",
            String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/oauth2/code/authorize/", subDomain, fUseSandbox ? leonDevHost : leonLiveHost),
            String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/oauth2/code/token/", subDomain, fUseSandbox ? leonDevHost : leonLiveHost),
            leonScopes)
        {
            SubDomain = subDomain;
            FlightsEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/api/graphql/", SubDomain, fUseSandbox ? leonDevHost : leonLiveHost);
            RefreshEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/access_token/refresh/", SubDomain, fUseSandbox ? leonDevHost : leonLiveHost);
        }

        public LeonClient(IAuthorizationState authstate, string subDomain, bool fUseSandbox = false) : this(subDomain, fUseSandbox)
        {
            AuthState = authstate;
        }

        /// <summary>
        /// Returns flights from Leon
        /// </summary>
        /// <param name="szUserName">Username</param>
        /// <param name="dtStart">Starting date</param>
        /// <param name="dtEnd">Ending date</param>
        /// <returns>An enumerable of LeonFlightEntry objects</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MyFlightbookValidationException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public async Task<IEnumerable<LeonFlightEntry>> GetFlights(string szUserName, DateTime? dtStart, DateTime? dtEnd)
        {
            if (szUserName == null)
                throw new ArgumentNullException(nameof(szUserName));

            if (dtStart.HasValue && dtEnd.HasValue && dtEnd.Value.CompareTo(dtStart.Value) <= 0)
                throw new MyFlightbookValidationException("Invalid date range");

            if (AuthState == null)
                throw new InvalidOperationException("No valid authstate for Leon operation");

            UriBuilder builder = new UriBuilder(FlightsEndpoint);
            NameValueCollection nvc = HttpUtility.ParseQueryString(builder.Query);

            LeonQuery lq = new LeonQuery(dtStart, dtEnd);

            Profile profile = Profile.GetUser(szUserName);

            // hack: Leon's refresh endpoint is NOT the same as the authorization token, so we can't simply call refresh
            if (!CheckAccessToken() && !String.IsNullOrEmpty(AuthState.RefreshToken))
            {
                HttpWebRequest hr = (HttpWebRequest)WebRequest.Create(new Uri(RefreshEndpoint));
                hr.Method = "POST";
                hr.ContentType = "application/x-www-form-urlencoded";
                string szPostData = "refresh_token=" + AuthState.RefreshToken;
                byte[] rgbData = Encoding.UTF8.GetBytes(szPostData);

                hr.ContentLength = rgbData.Length;
                using (Stream s = hr.GetRequestStream())
                {
                    s.Write(rgbData, 0, rgbData.Length);
                }

                using (WebResponse response = hr.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        AuthState.AccessToken = sr.ReadToEnd();
                        AuthState.AccessTokenExpirationUtc = DateTime.UtcNow.AddMinutes(29);// good for 30 minutes actually
                        profile.SetPreferenceForKey(TokenPrefKey, AuthState);
                    }
                }
            }

            string szResult = string.Empty;
            Dictionary<string, object> d = new Dictionary<string, object>() { { "query", lq.ToString() } };
            string szD = JsonConvert.SerializeObject(d);
            using (StringContent sc = new StringContent(szD, Encoding.UTF8, "application/json"))
            {
                return (IEnumerable<LeonFlightEntry>) await SharedHttpClient.GetResponseForAuthenticatedUri(builder.Uri, AuthState.AccessToken, sc, (response) =>
                {
                    try
                    {
                        szResult = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        IEnumerable<LeonFlightEntry> flights = LeonFlightEntry.FromLeonRootJSON(szResult);
                        // set the username for each of these.
                        foreach (LeonFlightEntry entry in flights)
                            entry.Username = szUserName;
                        return flights;
                    }
                    catch (HttpRequestException ex)
                    {
                        if (response == null)
                            throw new MyFlightbookException("Unknown error in LeonClient GetFlights", ex);
                        else
                            throw new MyFlightbookException("Error in LeonClient GetFlights: " + response.ReasonPhrase, ex);
                    }
                });
            }
        }

        /// <summary>
        /// Imports flights from Leon.  Throws an exception if any issue occurs.
        /// </summary>
        /// <param name="szUserName">Username</param>
        /// <param name="dtStart">Starting date</param>
        /// <param name="dtEnd">Ending date</param>
        /// <returns>True</returns>
        public async Task<bool> ImportFlights(string szUserName, DateTime? dtStart, DateTime? dtEnd)
        {
            IEnumerable<LeonFlightEntry> result = await GetFlights(szUserName, dtStart, dtEnd);
            foreach (LeonFlightEntry entry in result)
            {
                if (entry.ToLogbookEntry() is PendingFlight pf)
                    pf.Commit();
            }
            return true;
        }
    }
}
