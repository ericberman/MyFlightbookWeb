using DotNetOpenAuth.OAuth2;
using MyFlightbook.AircraftSupport.Maintenance;
using MyFlightbook.AircraftSupport.Maintenance.MyTailLog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.MyTailLog
{
    [Serializable]
    public class MTLAircraftList
    {
        [JsonProperty("aircraft")]
        public List<MTLAircraftRef> Aircraft { get; set; } = new List<MTLAircraftRef>();
    }

    [Serializable]
    public class MTLAircraftRef
    {
        [JsonProperty("id")] 
        public string Id { get; set; }

        [JsonProperty("tail_number")] 
        public string TailNumber { get; set; }
    }

    public class MyTailLogClient : OAuthClientBase
    {
        private const string clientConfigKey = "MyTailLogClientID";
        private const string clientSecretConfigKey = "MyTailLogClientSecret";
        private const string authEndpoint = "https://mytaillog.com/api/oidc/auth";
        private const string tokenEndpoint = "https://mytaillog.com/api/oidc/token";
        private const string revokeEndpoint = "https://mytaillog.com/api/oidc/revoke";
        private static readonly string[] scopes = new string[] { "airworthiness:read", "aircraft:read", "hours:read", "weightbalance:read", "equipment:read", "oil:read", "offline_access", "Confidential" };
        private const string szCachedCodeVerifier = "myTailLogCodeVerifier";
        private const string dataEndpointBase = "https://mytaillog.com/api/v1/aircraft";

        private string TokenPrefKey => ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey();

        #region constructors
        public MyTailLogClient() : base(clientConfigKey, clientSecretConfigKey, authEndpoint, tokenEndpoint, scopes, null, revokeEndpoint)
        {
        }

        public MyTailLogClient(IAuthorizationState state) : this()
        {
            AuthState = state;
        }
        #endregion

        #region oAuth flow
        /// <summary>
        /// Uri to generate authorization
        /// </summary>
        /// <param name="szRedir">The redirect URI to use for the request</param>
        /// <param name="state">Optional - any new state to pass to the request</param>
        /// <param name="pf">The user profile to store the code_verifier in</param>
        /// <returns>A redirect url</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Uri AuthorizationUri(string szRedir, string state, IUserProfile pf)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            // Build a code_verifier
            PKCEPair pkce = new PKCEPair();
            pf.AssociatedData[szCachedCodeVerifier] = pkce;

            UriBuilder uriBuilder = new UriBuilder(oAuth2AuthorizeEndpoint);
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["code_challenge"] = pkce.CodeChallenge;
            nvc["code_challenge_method"] = "S256";
            nvc["response_type"] = "code";
            nvc["client_id"] = AppKey;
            nvc["scope"] = String.Join(" ", scopes);
            nvc["redirect_uri"] = szRedir;
            nvc["prompt"] = "consent";
            if (!String.IsNullOrEmpty(state))
                nvc["state"] = state;
            uriBuilder.Query = nvc.ToString();
            return uriBuilder.Uri;
        }

        public static PKCEPair PendingCodeVerifier(IUserProfile pf)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            if (!pf.AssociatedData.TryGetValue(szCachedCodeVerifier, out object oCodeVerifier))
                throw new InvalidOperationException("No pending authorization request for user.");
            return (PKCEPair)oCodeVerifier;
        }

        public static void Revoke(string username) => ExternalMaintenanceRecord.FDeleteForUser(username, ExternalMaintenanceSourceID.MyTailLog);

        private async Task<bool> RefreshAsNeeded(string username)
        {
            if (String.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("No username provided");

            if (!CheckAccessToken())
            {
                Profile pf = Profile.GetUser(username);
                try
                {
                    if (await RefreshAccessToken())
                        pf.SetPreferenceForKey(TokenPrefKey, AuthState);
                }
                catch (UnauthorizedAccessException)
                {
                    pf.SetPreferenceForKey(TokenPrefKey, null, true);
                    throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Aircraft.ExternalMaintenanceRefreshAuthFailed), ExternalMaintenanceSourceID.MyTailLog.SourceName()));
                }
            }
            return true;
        }
        #endregion

        #region Pulling Data
        // Preferences → Maintenance "Sync now". Per-tail summary, like TachTime.
        public async Task<IDictionary<string, IEnumerable<string>>> UpdateMaintenanceFromMyTailLog(string username)
        {
            Dictionary<string, IEnumerable<string>> dResult = new Dictionary<string, IEnumerable<string>>();
            try
            {
                await RefreshAsNeeded(username);   // refresh via offline_access, persist to TokenPrefKey

                MTLAircraftList acList = await GetJson<MTLAircraftList>(new Uri(dataEndpointBase));

                UserAircraft ua = new UserAircraft(username);
                List<string> unknown = new List<string>();

                MyTailLogRecord.FDeleteForUser(username, ExternalMaintenanceSourceID.MyTailLog);  // wipe old, then re-add

                foreach (MTLAircraftRef mtl in acList.Aircraft)
                {
                    Aircraft ac = ua.FindMatching(a => !a.HideFromSelection &&
                        a.NormalizedTail.CompareCurrentCultureIgnoreCase(Aircraft.NormalizeTail(mtl.TailNumber)) == 0)
                        .FirstOrDefault();
                    if (ac == null) { unknown.Add(mtl.TailNumber); continue; }

                    MTLAirworthiness aw = await GetJson<MTLAirworthiness>(
                        new Uri($"{dataEndpointBase}/{mtl.Id}/airworthiness"));

                    new MyTailLogRecord(username, ac.AircraftID, aw).FCommit();   // raw JSON + high-water tach

                    MaintenanceRecord mr = aw.ToMaintenanceRecord(ac.Maintenance);
                    ac.UpdateMaintenanceForUser(mr, ac.Maintenance, username);
                    List<string> summary = new List<string>(ac.GetMaintenanceChanges().Select(ml => $"{ml.Description}{(String.IsNullOrEmpty(ml.Comment) ? string.Empty : $" ({ml.Comment})")}"));
                    ac.Commit(username);
                    summary.AddRange((aw.ADs ?? new List<MTLDirective>()).Select(ad => ad.Name));
                    dResult[ac.TailNumber] = summary;
                }
                if (unknown.Any())
                    dResult[String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ExternalMaintenanceUnknownAircraft, ExternalMaintenanceSourceID.MyTailLog.SourceName())] = unknown;  // or a MyTailLog-specific string
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                dResult[Resources.Aircraft.ExternalMaintenanceError] = new[] { e.Message };
            }
            if (dResult.Count == 0)
                dResult[Resources.Aircraft.ExternalMaintenanceSuccess] = new string[] { Resources.Aircraft.ExternalMaintenanceNothingImported };
            return dResult;
        }
        #endregion
    }
}