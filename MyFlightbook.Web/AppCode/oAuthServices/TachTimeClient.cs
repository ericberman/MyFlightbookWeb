using DotNetOpenAuth.OAuth2;
using MyFlightbook.AircraftSupport.Maintenance;
using MyFlightbook.AircraftSupport.Maintenance.TachTime;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.TachTime
{
    public class TachTimeClient : OAuthClientBase, IPushHighWater
    {
        private const string clientConfigKey = "TachTimeClientID";
        private const string clientConfigKeySandbox = "TachTimeClientIDSandbox";
        private const string clientSecretConfigKey = "TachTimeClientSecret";
        private const string clientSecretConfigKeySandbox = "TachTimeClientSecretSandbox";
        private const string authEndpoint = "https://auth.tachtime.app/oauth/authorize";
        private const string tokenEndpoint = "https://auth.tachtime.app/oauth/token";
        private const string revokeEndpoint = "https://auth.tachtime.app/oauth/revoke";
        private static readonly string[] scopes = new string[] { "tachtime:me:read", "tachtime:aircraft:read", "tachtime:compliance:read", "tachtime:maintenance-events:read" };
        private const string szCachedCodeVerifier = "tachTimeCodeVerifier";
        private const string dataEndpointBase = "https://auth.tachtime.app/v1/";

        private string TokenPrefKey => ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey();

        private static bool UseSandbox(string host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            return !Branding.CurrentBrand.MatchesHost(host);
        }

        #region constructors
        public TachTimeClient(string host) : base(UseSandbox(host) ? clientConfigKeySandbox : clientConfigKey, UseSandbox(host) ? clientSecretConfigKeySandbox : clientSecretConfigKey, authEndpoint, tokenEndpoint, scopes, null, revokeEndpoint)
        {
        }

        public TachTimeClient(IAuthorizationState state, string host) : this(host)
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
        #endregion

        #region Pulling Data
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
                    throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Aircraft.ExternalMaintenanceRefreshAuthFailed), ExternalMaintenanceSourceID.TachTime.SourceName()));
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a dictionary that maps user aircraft (by ID) to tachtime aircraft
        /// </summary>
        /// <param name="username">The user</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<IEnumerable<TachTimeAircraft>> GetMaintainedAircraftForUser()
        {
            if (AuthState == null)
                throw new UnauthorizedAccessException("Invalid authstate - no valid access token!");

            TachTimeAircraftCollectionResponse ttacr = new TachTimeAircraftCollectionResponse();
            List<TachTimeAircraft> lst = new List<TachTimeAircraft>();
            while (ttacr.has_more)
            {
                ttacr = await GetJson<TachTimeAircraftCollectionResponse>(new Uri(dataEndpointBase + $"aircraft?cursor={WebUtility.UrlEncode(ttacr.next_cursor)}"));
                lst.AddRange(ttacr.data);
            }

            return lst;
        }

        private static Dictionary<int, TachTimeAircraft> MapTachTimeToUserAircraft(string username, IEnumerable<TachTimeAircraft> rgtta, IList<TachTimeAircraft> unknownAircraft)
        {
            if (String.IsNullOrWhiteSpace(username)) 
                throw new ArgumentNullException(nameof(username));
            if (rgtta == null)
                throw new ArgumentNullException(nameof(rgtta));

            UserAircraft ua = new UserAircraft(username);
            Dictionary<int, TachTimeAircraft> d = new Dictionary<int, TachTimeAircraft>();
            foreach (TachTimeAircraft tta in rgtta)
            {
                IEnumerable<Aircraft> lstAc = ua.FindMatching(ac => !ac.HideFromSelection && ac.NormalizedTail.CompareCurrentCultureIgnoreCase(Aircraft.NormalizeTail(tta.n_number)) == 0);
                if (!lstAc.Any())
                    unknownAircraft.Add(tta);
                foreach (Aircraft ac in lstAc)
                    d[ac.AircraftID] = tta;
            }
            return d;
        }

        /// <summary>
        /// Gets all of the relevant data for any/all tachtime data
        /// </summary>
        /// <param name="username">The user</param>
        /// <param name="unknownAircraft">A list that will be populated with any unrecognized aircraft</param>
        /// <returns>A dictionary of TachTimeAggregatedDataForAircraft keyed on MyFlightbook aircraftID</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task<IDictionary<int, TachTimeAggregatedDataForAircraft>> RefreshMaintainedAircraftForUser(string username, IList<TachTimeAircraft> unknownAircraft)
        {
            if (AuthState == null)
                throw new UnauthorizedAccessException("Invalid authstate - no valid access token!");

            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username));

            _ = await RefreshAsNeeded(username);

            IEnumerable<TachTimeAircraft> rgtta = await GetMaintainedAircraftForUser();
            Dictionary<int, TachTimeAircraft> d = MapTachTimeToUserAircraft(username, rgtta, unknownAircraft);

            Dictionary<int, TachTimeAggregatedDataForAircraft> dOut = new Dictionary<int, TachTimeAggregatedDataForAircraft>();

            foreach (int idAircraft in d.Keys)  // only pay attention to aircraft that map to useraircraft in their profile
            {
                TachTimeAircraft tta = d[idAircraft];
                TachTimeAggregatedDataForAircraft ttad = new TachTimeAggregatedDataForAircraft
                {
                    AircraftDetails = await GetAircraftDetail(tta.id),
                    Inspections = await GetComplianceForAircraft(tta.id),
                    MaintenanceHistory = await GetMaintenanceHistoryForAircraft(tta.id)
                };

                dOut[idAircraft] = ttad;
            }
            return dOut;
        }

        /// <summary>
        /// Returns detail for a tachtime aircraft - specifically tach time and date of last entry
        /// </summary>
        /// <param name="tachTimeAircraftID"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<TachTimeAircraft> GetAircraftDetail(string tachTimeAircraftID)
        {
            return await GetJson<TachTimeAircraft>(new Uri(dataEndpointBase + $"aircraft/{WebUtility.UrlEncode(tachTimeAircraftID)}"));
        }

        /// <summary>
        /// Returns detail for a tachtime aircraft - specifically tach time and date of last entry
        /// </summary>
        /// <param name="tachTimeAircraftID"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<TachTimeCompliance> GetComplianceForAircraft(string tachTimeAircraftID)
        {
            return await GetJson<TachTimeCompliance>(new Uri(dataEndpointBase + $"aircraft/{WebUtility.UrlEncode(tachTimeAircraftID)}/compliance"));
        }

        private async Task<IEnumerable<TachTimeMaintenanceEvent>> GetMaintenanceHistoryForAircraft(string tachTimeAircraftID)
        {
            TachTimeMaintenanceEventCollectionResponse ttmecr = new TachTimeMaintenanceEventCollectionResponse();
            List<TachTimeMaintenanceEvent> lst = new List<TachTimeMaintenanceEvent>();

            while (ttmecr.has_more)
            {
                ttmecr = await GetJson<TachTimeMaintenanceEventCollectionResponse>(new Uri(dataEndpointBase + $"aircraft/{WebUtility.UrlEncode(tachTimeAircraftID)}/maintenance-events?cursor={WebUtility.UrlEncode(ttmecr.next_cursor)}"));
                lst.AddRange(ttmecr.data);
            }
            return lst;
        }

        public async Task<IDictionary<string, IEnumerable<string>>> UpdateMaintenanceFromTachTime(string username)
        {
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username));

            Dictionary<string, IEnumerable<string>> dResult = new Dictionary<string, IEnumerable<string>>();
            List<TachTimeAircraft> lstUnknown = new List<TachTimeAircraft>();
            try
            {
                // Get updated data first
                IDictionary<int, TachTimeAggregatedDataForAircraft> dAircraft = await RefreshMaintainedAircraftForUser(username, lstUnknown);

                // Now that we have the data, delete the old data and replace it with this.
                TachTimeRecord.FDeleteForUser(username, ExternalMaintenanceSourceID.TachTime);
                foreach (int idaircraft in dAircraft.Keys)
                    new TachTimeRecord(username, idaircraft, dAircraft[idaircraft]).FCommit();

                // Start out with the unknown aircraft
                if (lstUnknown.Count > 0)
                    dResult[String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ExternalMaintenanceUnknownAircraft, ExternalMaintenanceSourceID.TachTime.SourceName())] = lstUnknown.Select(tta => $"{tta.n_number} ({tta.make} {tta.model})");

                UserAircraft ua = new UserAircraft(username);
                // Now try to update as appropriate.
                foreach (int idaircraft in dAircraft.Keys)
                {
                    Aircraft ac = ua[idaircraft];
                    TachTimeAggregatedDataForAircraft ttad = dAircraft[idaircraft];
                    MaintenanceRecord mr = ttad.Inspections.ToMaintenanceRecord(ac.Maintenance);
                    ac.UpdateMaintenanceForUser(mr, ac.Maintenance, username);
                    List<string> inspectionSummary = new List<string>(ac.GetMaintenanceChanges().Select(ml => $"{ml.Description}{(String.IsNullOrEmpty(ml.Comment) ? string.Empty : $" ({ml.Comment})")}"));
                    ac.Commit(username);
                    inspectionSummary.AddRange(ttad.Inspections.additional_items.Select(ttai => ttai.ToString()));
                    inspectionSummary.AddRange(ttad.Inspections.alerts);
                    inspectionSummary.AddRange(ttad.MaintenanceHistory.Select(ttme => ttme.ToString()));
                    dResult[ac.TailNumber] = inspectionSummary;
                }
            }
            catch (Exception e) when (e.IsNonFatal())
            {
                dResult[Resources.Aircraft.ExternalMaintenanceError] = new string[] { e.Message };
            }
            if (dResult.Count == 0)
                dResult[Resources.Aircraft.ExternalMaintenanceSuccess] = new string[] { Resources.Aircraft.ExternalMaintenanceNothingImported };
            return dResult;
        }

        public static void Revoke(string username)
        {
            ExternalMaintenanceRecord.FDeleteForUser(username, ExternalMaintenanceSourceID.TachTime);
        }

        public Task<bool> UpdateHighWaterForAircraft(string userName, int idAircraft, HighWatermarkSet watermarkSet)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            if (watermarkSet == null)
                throw new ArgumentNullException(nameof(watermarkSet));

            foreach (ExternalMaintenanceRecord emr in ExternalMaintenanceRecord.GetExternalMaintenanceRecords(idAircraft))
            {
                if (emr is TachTimeRecord ttr && ttr.DataAsType is TachTimeAggregatedDataForAircraft aggregatedDataForAircraft)
                {
                    string externalID = aggregatedDataForAircraft.AircraftDetails.id;
                    // Update it
                    // TODO: NOT YET IMPLEMENTED
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(true);

        }
        #endregion
    }
}