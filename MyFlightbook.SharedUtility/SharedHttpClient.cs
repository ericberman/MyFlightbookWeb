using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Encapsulates the shared httpclient
    /// </summary>
    public static class SharedHttpClient
    {
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Same as _client, but never follows redirects.  For fetching untrusted URLs (e.g., OAuth Client ID Metadata Documents),
        /// where a redirect could bounce the request to an internal address.
        /// </summary>
        private static readonly HttpClient _clientNoRedirect = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false });

        /// <summary>
        /// GetResponseForAuthenticatedUri - assumes a POST
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, HttpMethod.Post, null, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="content">HttpContent to post (REQUIRES POST/PUT)</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpContent content, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, HttpMethod.Post, content, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="method">The http method to use (GET or POST)</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpMethod method, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, method, null, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="content">HttpContent to post (REQUIRES POST/PUT)</param>
        /// <param name="method">The http method to use (GET or POST)</param>
        /// <param name="dictHeaders">Any additional request headers</param>
        /// <param name="fFollowRedirects">False to return 3xx responses as-is instead of following them</param>
        /// <param name="timeout">Optional per-request timeout (the shared client's default is 100 seconds)</param>
        /// <param name="completionOption">ResponseHeadersRead to let OnResult read the body as a stream (e.g., to cap its size) rather than buffering it all first</param>
        public async static Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpMethod method, HttpContent content, Func<HttpResponseMessage, object> OnResult, IDictionary<string, string> dictHeaders = null,
            bool fFollowRedirects = true, TimeSpan? timeout = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (OnResult == null)
                throw new ArgumentNullException(nameof(OnResult));
            if (content != null && method == HttpMethod.Get)
                throw new InvalidOperationException("Cannot do http GET with content passed.");

            object result = null;
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(method, uri))
            {
                if (!String.IsNullOrEmpty(szAuth))
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", szAuth);

                if (dictHeaders != null)
                {
                    foreach (string szKey in dictHeaders.Keys)
                        requestMessage.Headers.Add(szKey, dictHeaders[szKey]);
                }

                if (content != null)
                    requestMessage.Content = content;

                using (CancellationTokenSource cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource())
                {
                    using (HttpResponseMessage response = await (fFollowRedirects ? _client : _clientNoRedirect).SendAsync(requestMessage, completionOption, cts.Token).ConfigureAwait(false))
                    {
                        result = OnResult(response);
                    }
                }
            }
            return result;
        }
    }
}
