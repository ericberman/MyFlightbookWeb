using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightDeckScan
{
    /// <summary>
    /// Calls the Anthropic Messages API with a forced tool-use request so the
    /// response is always the RawExtraction shape, then hands off to
    /// FlightDeckScanNormalizer for the actual date/time interpretation.
    ///
    /// Configuration (via the existing LocalConfig "localconfig" DB table, same
    /// place other secrets/keys live - NOT web.config):
    ///   AnthropicApiKey    (required) - your key from console.anthropic.com
    ///   AnthropicScanModel (optional) - defaults to claude-sonnet-4-5-20250929
    ///
    /// Note: uses its own static HttpClient rather than MyFlightbook.SharedHttpClient,
    /// since this call needs full async JSON-body handling (auth is x-api-key/
    /// anthropic-version headers, not the Bearer-token shape SharedHttpClient is
    /// built around). Straightforward to switch to SharedHttpClient later if you'd
    /// rather centralize all outbound calls through it.
    /// </summary>
    public static class FlightDeckScanClient
    {
        private const string DefaultModel = "claude-sonnet-4-5-20250929";
        private const string AnthropicVersion = "2023-06-01";
        private const string MessagesEndpoint = "https://api.anthropic.com/v1/messages";

        // A single shared, long-lived HttpClient, per standard .NET guidance (avoids
        // socket exhaustion from creating one per request).
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        private static string ModelName
        {
            get
            {
                try
                {
                    string szModel = LocalConfig.SettingForKey("AnthropicScanModel");
                    return string.IsNullOrWhiteSpace(szModel) ? DefaultModel : szModel;
                }
                catch (KeyNotFoundException)
                {
                    return DefaultModel;
                }
            }
        }

        private static string ApiKey
        {
            get
            {
                try
                {
                    return LocalConfig.SettingForKey("AnthropicApiKey");
                }
                catch (KeyNotFoundException ex)
                {
                    throw new MyFlightbookException(
                        "Flight-deck scanning isn't configured yet (missing AnthropicApiKey in localconfig).", ex);
                }
            }
        }

        /// <summary>
        /// Sends one image to the vision model and returns the raw, unnormalized
        /// extraction. Throws MyFlightbookException on any failure to reach/parse
        /// the model's response - callers should let that propagate to SafeOp.
        /// </summary>
        /// <param name="imageBytes">The raw image bytes (jpeg/png/webp).</param>
        /// <param name="mediaType">e.g. "image/jpeg", "image/png", "image/webp".</param>
        public static async Task<RawExtraction> ExtractRawAsync(byte[] imageBytes, string mediaType)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentNullException(nameof(imageBytes));
            if (string.IsNullOrEmpty(mediaType))
                throw new ArgumentNullException(nameof(mediaType));

            var requestBody = new JObject
            {
                ["model"] = ModelName,
                ["max_tokens"] = 1024,
                ["system"] = FlightDeckScanPrompt.SystemPrompt,
                ["tools"] = new JArray(FlightDeckScanSchema.BuildToolDefinition()),
                ["tool_choice"] = new JObject { ["type"] = "tool", ["name"] = FlightDeckScanSchema.ToolName },
                ["messages"] = new JArray(
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = new JArray(
                            new JObject
                            {
                                ["type"] = "image",
                                ["source"] = new JObject
                                {
                                    ["type"] = "base64",
                                    ["media_type"] = mediaType,
                                    ["data"] = Convert.ToBase64String(imageBytes)
                                }
                            },
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = FlightDeckScanPrompt.UserInstruction
                            })
                    })
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, MessagesEndpoint))
            {
                request.Headers.Add("x-api-key", ApiKey);
                request.Headers.Add("anthropic-version", AnthropicVersion);
                request.Content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response;
                string responseBody;
                try
                {
                    response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw new MyFlightbookException("Could not reach the image-scanning service. Please try again.", ex);
                }
                catch (TaskCanceledException ex)
                {
                    throw new MyFlightbookException("The image-scanning service took too long to respond. Please try again.", ex);
                }

                if (!response.IsSuccessStatusCode)
                    throw new MyFlightbookException($"Image-scanning service returned an error (HTTP {(int)response.StatusCode}).");

                JObject parsed;
                try
                {
                    parsed = JObject.Parse(responseBody);
                }
                catch (JsonException ex)
                {
                    throw new MyFlightbookException("Image-scanning service returned an unreadable response.", ex);
                }

                JToken toolUseInput = parsed["content"]?
                    .FirstOrDefault(c => (string)c["type"] == "tool_use")?["input"];

                if (toolUseInput == null)
                    throw new MyFlightbookException("Image-scanning service did not return a structured result.");

                return toolUseInput.ToObject<RawExtraction>();
            }
        }
    }
}
