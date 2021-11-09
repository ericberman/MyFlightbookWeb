using DotNetOpenAuth.OAuth2;
using HtmlAgilityPack;
using MyFlightbook.ImportFlights.CloudAhoy;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2019-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.CloudAhoy
{
    public class CloudAhoyFlightCollection : List<CloudAhoyFlight>
    {

    }

    [Serializable]
    internal class CloudAhoyPostFileMetaData
    {
        public string importerVersion { get; set; }
        public string tail { get; set; }
        public string remarks { get; set; }

        public CloudAhoyPostFileMetaData()
        {
            importerVersion = "1.0";
            tail = remarks = string.Empty;
        }

        public CloudAhoyPostFileMetaData(LogbookEntryCore le) : this()
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            tail = le.TailNumDisplay;
            remarks = le.Comment;
        }
    }

    /// <summary>
    /// CloudAhow class
    /// </summary>
    public class CloudAhoyClient : OAuthClientBase
    {
        private const string cloudAhoyDebugHost = "www.cloudahoy.com";
        private const string cloudAhoyLiveHost = "www.cloudahoy.com";

        private string FlightsEndpoint { get; set; }

        public CloudAhoyClient(bool fUseSandbox = false) : base("CloudAhoyID",
            "CloudAhoySecret",
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/auth", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/token", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            new string[] { "flights:read",  "flights:import" })
        {
            FlightsEndpoint = String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/flights", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost);
        }

        public CloudAhoyClient(IAuthorizationState authstate) : this()
        {
            AuthState = authstate;
        }

        private static string TextFromHTML(string sz)
        {
            if (sz == null || !sz.Contains("<"))
                return sz;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(sz);
            StringBuilder sb = new StringBuilder();
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//text()"))
                sb.Append(node.InnerText.Trim() + " ");

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Pushes flight data to CloudAhoy
        /// </summary>
        /// <param name="s">The KML or GPX stream</param>
        /// <param name="le">The parent flight (for metadata)</param>
        public async Task<bool> PutStream(Stream s, LogbookEntryCore le)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                using (StringContent sc = new StringContent(JsonConvert.SerializeObject(new CloudAhoyPostFileMetaData(le))))
                {
                    form.Add(sc, "METADATA");

                    using (StreamContent streamContent = new StreamContent(s))
                    {
                        form.Add(streamContent, "IMPORT", "data.gpx");

                        string szResult = (string) await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(FlightsEndpoint), AuthState.AccessToken, form, (response) =>
                        {
                            string result = string.Empty;
                            try
                            {
                                result = response.Content.ReadAsStringAsync().Result;
                                response.EnsureSuccessStatusCode();
                                return result;
                            }
                            catch (HttpRequestException ex)
                            {
                                throw new MyFlightbookException(ex.Message + " " + response.ReasonPhrase + " " + TextFromHTML(result), ex);
                            }
                        });
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Retrieves flights from cloudahoy
        /// </summary>
        /// <param name="szUserName">User for whome the flights are being retrieved</param>
        /// <param name="dtEnd">End date for date range</param>
        /// <param name="dtStart">Start date for date range</param>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public async Task<CloudAhoyFlightCollection> GetFlights(string szUserName, DateTime? dtStart, DateTime? dtEnd)
        {
            if (dtStart.HasValue && dtEnd.HasValue && dtEnd.Value.CompareTo(dtStart.Value) <= 0)
                throw new MyFlightbookValidationException("Invalid date range");

            string szResult = string.Empty;

            UriBuilder builder = new UriBuilder(FlightsEndpoint);
            NameValueCollection nvc = HttpUtility.ParseQueryString(builder.Query);

            if (dtStart.HasValue)
                nvc["start"] = (new DateTime(dtStart.Value.Year, dtStart.Value.Month, dtStart.Value.Day, 0, 0, 0, DateTimeKind.Utc)).UnixSeconds().ToString(CultureInfo.InvariantCulture);
            if (dtEnd.HasValue)
                nvc["end"] = (new DateTime(dtEnd.Value.Year, dtEnd.Value.Month, dtEnd.Value.Day, 23, 59, 59, DateTimeKind.Utc)).UnixSeconds().ToString(CultureInfo.InvariantCulture);

            CloudAhoyFlightCollection lstResult = new CloudAhoyFlightCollection();

            for (int iPage = 1; iPage < 20; iPage++)
            {
                nvc["page"] = iPage.ToString(CultureInfo.InvariantCulture);

                builder.Query = nvc.ToString();

                bool fHasMore = false;
                IEnumerable<CloudAhoyFlight> flights = (IEnumerable<CloudAhoyFlight>)
                await SharedHttpClient.GetResponseForAuthenticatedUri(builder.Uri, AuthState.AccessToken, HttpMethod.Get, (response) =>
                {
                    try
                    {
                        fHasMore = false;
                        if (response.Headers.TryGetValues("ca-has-more", out IEnumerable<string> values))
                            fHasMore = Convert.ToBoolean(values.First(), CultureInfo.InvariantCulture);

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
                        throw new MyFlightbookException(response.ReasonPhrase + " " + TextFromHTML(szResult));
                    }
                });

                lstResult.AddRange(flights);

                if (!fHasMore)
                    break;  // don't go infnite loop!
            }

            return lstResult;
        }

        /// <summary>
        /// Imports cloudahoy flights for the specified user between the specified dates
        /// </summary>
        /// <param name="szUsername">The user for whom to import flights</param>
        /// <param name="fSandbox">True to use the sandbox</param>
        /// <param name="dtStart">Optional start date</param>
        /// <param name="dtEnd">Optional end date</param>
        /// <returns>Error message if failure, else empty string for success</returns>
        public async static Task<string> ImportCloudAhoyFlights(string szUsername, bool fSandbox, DateTime? dtStart, DateTime? dtEnd)
        {
            Profile pf = Profile.GetUser(szUsername);
            CloudAhoyClient client = new CloudAhoyClient(fSandbox) { AuthState = pf.CloudAhoyToken };
            try
            {
                IEnumerable<CloudAhoyFlight> rgcaf = await client.GetFlights(szUsername, dtStart, dtEnd);
                foreach (CloudAhoyFlight caf in rgcaf)
                {
                    if (caf.ToLogbookEntry() is PendingFlight pendingflight)
                        pendingflight.Commit();
                }
                return string.Empty;
            }
            catch (Exception ex) when (ex is MyFlightbookException || ex is MyFlightbookValidationException)
            {
                return ex.Message;
            }
        }

        public async static Task<bool> PushCloudAhoyFlight(string szUsername, LogbookEntryCore flight, FlightData fd, bool fSandbox)
        {
            if (szUsername == null)
                throw new ArgumentNullException(nameof(szUsername));
            if (flight == null)
                throw new ArgumentNullException(nameof(flight));
            if (fd == null)
                throw new ArgumentNullException(nameof(fd));

            Profile pf = Profile.GetUser(szUsername);
            CloudAhoyClient client = new CloudAhoyClient(fSandbox) { AuthState = pf.CloudAhoyToken };
            bool fUseRawData = (flight.Telemetry.TelemetryType == DataSourceType.FileType.GPX || flight.Telemetry.TelemetryType == DataSourceType.FileType.KML);
            using (MemoryStream ms = fUseRawData ? new MemoryStream(Encoding.UTF8.GetBytes(flight.Telemetry.RawData)) : new MemoryStream())
            {
                if (!fUseRawData)
                    fd.WriteGPXData(ms);

                ms.Seek(0, SeekOrigin.Begin);
                return await client.PutStream(ms, flight);
            }
        }
    }
}