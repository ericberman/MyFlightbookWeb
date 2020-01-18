using DotNetOpenAuth.OAuth2;
using System.IO;
using HtmlAgilityPack;
using MyFlightbook.ImportFlights.CloudAhoy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.CloudAhoy
{
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

        public CloudAhoyPostFileMetaData(LogbookEntryBase le) : this()
        {
            if (le == null)
                throw new ArgumentNullException("le");

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

        private string TextFromHTML(string sz)
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
        public async Task<bool> PutStream(Stream s, LogbookEntryBase le)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (le == null)
                throw new ArgumentNullException("le");

            HttpResponseMessage response = null;

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(JsonConvert.SerializeObject(new CloudAhoyPostFileMetaData(le))), "METADATA");
                form.Add(new StreamContent(s), "IMPORT", "data.gpx");

                string szResult = string.Empty;

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthState.AccessToken);

                    try
                    {
                        response = await httpClient.PostAsync(FlightsEndpoint, form);

                        szResult = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new MyFlightbookException(ex.Message + " " + response.ReasonPhrase + " " + TextFromHTML(szResult), ex);
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
        public async Task<IEnumerable<CloudAhoyFlight>> GetFlights(string szUserName, DateTime? dtStart, DateTime? dtEnd)
        {
            HttpResponseMessage response = null;

            if (dtStart.HasValue && dtEnd.HasValue && dtEnd.Value.CompareTo(dtStart.Value) <= 0)
                throw new MyFlightbookValidationException("Invalid date range");

            string szResult = string.Empty;

            UriBuilder builder = new UriBuilder(FlightsEndpoint);
            NameValueCollection nvc = HttpUtility.ParseQueryString(builder.Query);

            DateTime dtUnix = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            if (dtStart.HasValue)
                nvc["start"] = (new DateTime(dtStart.Value.Year, dtStart.Value.Month, dtStart.Value.Day, 0, 0, 0, DateTimeKind.Utc)).Subtract(dtUnix).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            if (dtEnd.HasValue)
                nvc["end"] = (new DateTime(dtEnd.Value.Year, dtEnd.Value.Month, dtEnd.Value.Day, 23, 59, 59, DateTimeKind.Utc)).Subtract(dtUnix).TotalSeconds.ToString(CultureInfo.InvariantCulture);

            List<CloudAhoyFlight> lstResult = new List<CloudAhoyFlight>();

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AuthState.AccessToken);

                try
                {
                    for (int iPage = 1; iPage < 20; iPage++)
                    {
                        nvc["page"] = iPage.ToString(CultureInfo.InvariantCulture);

                        builder.Query = nvc.ToString();

                        response = await httpClient.GetAsync(builder.ToString());

                        IEnumerable<string> values = null;
                        bool fHasMore = false;
                        if (response.Headers.TryGetValues("ca-has-more", out values))
                            fHasMore = Convert.ToBoolean(values.First(), CultureInfo.InvariantCulture);

                        szResult = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        CloudAhoyFlight[] rgFlights = JsonConvert.DeserializeObject<CloudAhoyFlight[]>(szResult, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
                        if (szUserName != null)
                            foreach (CloudAhoyFlight caf in rgFlights)
                                caf.UserName = szUserName;

                        lstResult.AddRange(rgFlights);

                        if (!fHasMore)
                            break;  // don't go infnite loop!
                    }
                    return lstResult;
                }
                catch (HttpRequestException)
                {
                    throw new MyFlightbookException(response.ReasonPhrase + " " + TextFromHTML(szResult));
                }
            }
        }
    }
}