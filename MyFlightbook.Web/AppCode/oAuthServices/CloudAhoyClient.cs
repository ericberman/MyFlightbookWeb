using AjaxControlToolkit;
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
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
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

    [Serializable]
    internal class FlyStoClientPushResponse
    {
        public string fileID { get; set; }
    }

    [Serializable]
    internal class FlyStoStatusResponse
    {
        public bool processed { get; set; }
        public string[] logs { get; set; }
    }

    public class FlyStoClient: OAuthClientBase
    {
        private const string uploadEndpoint = "https://www.flysto.net/public-api/log-upload";
        private const string statusEndpointTemplate = "https://www.flysto.net/public-api/log-files/{0}";

        public const string AccessTokenPrefKey = "FlyStoAccessToken";

        public FlyStoClient() : base("FlyStoAccessID", "FlyStoClientSecret", "https://www.flysto.net/oauth/authorize", "https://www.flysto.net/oauth/token") { }

        protected FlyStoClient(IAuthorizationState authstate) : this()
        {
            AuthState = authstate;
        }

        public static async Task<FlyStoClient> RefreshedClientForUser(string szUsername)
        {
            if (String.IsNullOrEmpty(szUsername))
                throw new ArgumentNullException(nameof(szUsername));

            Profile pf = Profile.GetUser(szUsername);

            FlyStoClient fsc = new FlyStoClient(pf.GetPreferenceForKey<AuthorizationState>(AccessTokenPrefKey));
            if (fsc.AuthState == null)
                throw new UnauthorizedAccessException("No FlySto Authorization State");

            if (!fsc.CheckAccessToken() && !String.IsNullOrEmpty(fsc.AuthState.RefreshToken))
            {
                fsc.AuthState = await fsc.RefreshAccessToken(fsc.AuthState.RefreshToken, fsc.AuthState.Callback.ToString());
                pf.SetPreferenceForKey(AccessTokenPrefKey, fsc.AuthState);
            }

            return fsc;
        }

        public async Task<string> LogIDFromFileID(string fileID)
        {
            if (String.IsNullOrEmpty(fileID))
                throw new ArgumentNullException(nameof(fileID));

            string statusEndpoint = String.Format(CultureInfo.InvariantCulture, statusEndpointTemplate, fileID);

            return (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(statusEndpoint), AuthState.AccessToken, HttpMethod.Get, (response) =>
            {
                string result = string.Empty;
                try
                {
                    result = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    FlyStoStatusResponse fsr = JsonConvert.DeserializeObject<FlyStoStatusResponse>(result);
                    return fsr.processed && fsr.logs.Length > 0 ? fsr.logs[0] : string.Empty;
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    // any error means processing has not finished - just return an empty string
                    return string.Empty;
                }
            });
        }

        public async Task<string> PushGPX(LogbookEntry le, FlightData fd)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (fd == null)
                throw new ArgumentNullException(nameof(fd));

            string result = string.Empty;

            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    ZipArchiveEntry zipArchiveEntry = zip.CreateEntry("path.gpx");

                    using (Stream entryStream = zipArchiveEntry.Open())
                    {
                        using (StreamWriter swGPX = new StreamWriter(entryStream))
                        {
                            swGPX.Write(le.TelemetryAsGPX());
                        }
                    }
                }

                using (ByteArrayContent bac = new ByteArrayContent(ms.ToArray()))
                {
                    bac.Headers.ContentType = new MediaTypeHeaderValue("application/x-zip");
                    bac.Headers.ContentDisposition = (new ContentDispositionHeaderValue("form-data") { Name = "path.zip", FileName = "path.zip" });

                    string szResult = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(uploadEndpoint), AuthState.AccessToken, bac, (response) =>
                    {
                        try
                        {
                            result = response.Content.ReadAsStringAsync().Result;
                            response.EnsureSuccessStatusCode();
                            return JsonConvert.DeserializeObject<FlyStoClientPushResponse>(result).fileID;
                        }
                        catch (HttpRequestException ex)
                        {
                            throw new MyFlightbookException(ex.Message + " " + response.ReasonPhrase + " " + result, ex);
                        }
                    });

                    return szResult;
                }
            }
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

        private static readonly string[] scopes = new string[] { "flights:read",  "flights:import" };

        public CloudAhoyClient(bool fUseSandbox = false) : base("CloudAhoyID",
            "CloudAhoySecret",
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/auth", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            String.Format(CultureInfo.InvariantCulture, "https://{0}/integration/v1/token", fUseSandbox ? cloudAhoyDebugHost : cloudAhoyLiveHost),
            scopes)
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