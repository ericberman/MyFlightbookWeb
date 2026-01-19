using MyFlightbook.SharedUtility.Properties;
using Newtonsoft.Json;
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
    /// Tool to validate recaptcha tokens
    /// </summary>
    public static class RecaptchaUtil
    {
        /// <summary>
        /// Validates a recaptcha token
        /// </summary>
        /// <param name="token">The token</param>
        /// <param name="action">Optional, the action being validated</param>
        /// <param name="referringDomain">The referring domain</param>
        /// <returns>The score (0 to 1.0); 1.0 if any error</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="HttpRequestException"></exception>
        static async public Task<double> ValidateRecaptcha(string token, string action, string referringDomain)
        {
            if (String.IsNullOrEmpty(token))
                throw new InvalidOperationException(SharedUtilityResources.ValidationRecaptchaRequired);

            Dictionary<string, string> dInner = new Dictionary<string, string>()
            {
                {"token", token },
                { "expectedAction", (action ?? string.Empty) },
                { "siteKey", LocalConfig.SettingForKey("recaptchaKey") }
            };
            Dictionary<string, object> dOuter = new Dictionary<string, object>() { { "event", dInner } };

            string szResult = string.Empty;
            using (StringContent sc = new StringContent(JsonConvert.SerializeObject(dOuter)))
            {
                try
                {
                    string r = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(LocalConfig.SettingForKey("recaptchaValidateEndpoint")), null, HttpMethod.Post, sc, (response) =>
                    {
                        szResult = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        return szResult;
                    }, new Dictionary<string, string> { { "referer", referringDomain } });

                    dynamic d = JsonConvert.DeserializeObject<dynamic>(r);

                    if (d?.riskAnalysis?.score == null) // let's debug this
                        util.NotifyAdminEvent("Unrecognized recaptcha response", r, ProfileRoles.maskSiteAdminOnly);

                    return d?.riskAnalysis?.score ?? 1.0;   // if null, pass 1 - treat as accepted.
                }
                catch (HttpRequestException e)
                {
                    // pass up the exception, along with whatever detail we can.
                    throw new HttpRequestException($"{e.Message} - szResult = {szResult ?? "(not set)"}, referringDomain = '{referringDomain}'", e);
                }
            }
        }
    }
}
