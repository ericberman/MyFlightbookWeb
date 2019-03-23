using DotNetOpenAuth.OAuth2;
using MyFlightbook.ImportFlights.CloudAhoy;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.CloudAhoy
{
    /// <summary>
    /// CloudAhow class
    /// </summary>
    public class CloudAhoyClient : OAuthClientBase
    {
        private const string cloudAhoyDebugHost = "sandbox.cloudahoy.com";
        private const string cloudAhoyLiveHost = "cloudahoy.com";

        private string FlightsEndpoint { get; set; }

        public CloudAhoyClient(bool fUseSandbox = false) : base("CloudAhoyID",
            "CloudAhoySecret",
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/auth", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/token", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            new string[] { "flights:read" })
        {
            FlightsEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/flights", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost);
        }

        public CloudAhoyClient(IAuthorizationState authstate) : this()
        {
            AuthState = authstate;
        }

        /// <summary>
        /// Retrieves flights from cloudahoy
        /// </summary>
        /// <param name="szUserName">User for whome the flights are being retrieved</param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public async Task<IEnumerable<CloudAhoyFlight>> GetFlights(string szUserName)
        {
            HttpResponseMessage response = null;

            string szResult = string.Empty;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthState.AccessToken);

                try
                {
                    response = await httpClient.GetAsync(FlightsEndpoint);
                    szResult = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    CloudAhoyFlight[] rgFlights = JsonConvert.DeserializeObject<CloudAhoyFlight[]>(szResult, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
                    if (szUserName != null)
                        foreach (CloudAhoyFlight caf in rgFlights)
                            caf.UserName = szUserName;
                    return rgFlights;
                }
                catch (HttpRequestException)
                {
                    throw new MyFlightbookException(response.ReasonPhrase + " " + szResult);
                }
            }
        }
    }
}