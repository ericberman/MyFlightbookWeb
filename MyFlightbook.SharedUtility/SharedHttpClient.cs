using System;
using System.Collections.Generic;
using System.Net.Http;
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
        public async static Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpMethod method, HttpContent content, Func<HttpResponseMessage, object> OnResult, IDictionary<string, string> dictHeaders = null)
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

                using (HttpResponseMessage response = await _client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    result = OnResult(response);
                }
            }
            return result;
        }
    }
}
