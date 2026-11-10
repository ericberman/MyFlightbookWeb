using MyFlightbook;
using MyFlightbook.OAuth;
using MyFlightbook.OAuth.Legacy;
using MyFlightbook.OAuth.Server;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2016-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 * 
 * The OAuth 2.1 server itself is MyFlightbook.OAuth.Server.OAuthServer; this file holds MyFlightbook's client registry
 * (allowedoauthclients), user authorizations (oauthclientauthorization), and the glue that builds the server.
 *
*******************************************************/

namespace OAuthAuthorizationServer.Code
{
    /// <summary>
    /// An OAuth 2.0 Client that has registered with this Authorization Server.
    /// </summary>
    [Serializable]
    public sealed partial class MFBOauth2Client
    {
        public MFBOauth2Client(string clientIdentifier, string clientSecret, string callback, string name, string scope, string szuser, ClientType clientType = ClientType.Confidential, byte[] logo = null)
        {
            ClientName = name;
            ClientIdentifier = clientIdentifier;
            ClientSecret = clientType == ClientType.Confidential ? clientSecret : string.Empty;
            Callbacks = String.IsNullOrEmpty(callback) ? Array.Empty<string>() : AllowedCallbacksFromString(callback);
            ClientType = clientType;
            Scope = scope;
            OwningUser = szuser;
            Logo = logo;
        }

        private MFBOauth2Client(MySqlDataReader dr) : this((string)dr["ClientID"], (string)dr["ClientSecret"], (string)dr["Callback"], (string)dr["ClientName"], (string)dr["Scopes"], (string)dr["owningUserName"], (ClientType)util.ReadNullableField(dr, "ClientType", 0), (byte[])util.ReadNullableField(dr, "logo", null))
        {
        }

        /// <summary>
        /// The server's view of this client.  Pre-registered clients keep today's lenient redirect comparison (host+port+path+query,
        /// case-insensitive), and public ones may still use "plain" PKCE, which the old PKCE proxy accepted.
        /// </summary>
        public OAuthClient ToOAuthClient()
        {
            OAuthClient c = new OAuthClient()
            {
                ClientId = ClientIdentifier,
                ClientName = ClientName,
                IsPublic = ClientType == ClientType.Public,
                ClientSecret = ClientSecret,
                UseLegacyRedirectComparison = true,       // today's lenient host+path+query match
                AllowPlainPkce = ClientType == ClientType.Public
            };
            foreach (string cb in Callbacks) c.RedirectHREFs.Add(cb);
            c.AllowedScopes.UnionWith(OAuthScopes.SplitScopes(Scope));
            return c;
        }

        /// <summary>
        /// Reads all available authorized clients from the database.  NOT CACHED.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MFBOauth2Client> GetAvailableClients()
        {
            List<MFBOauth2Client> lst = new List<MFBOauth2Client>();
            new DBHelper("SELECT * FROM allowedoauthclients").ReadRows((comm) => { },
                (dr) => { lst.Add(new MFBOauth2Client(dr)); });
            return lst;
        }

        public static IEnumerable<MFBOauth2Client> GetClientsForUser(string szUser)
        {
            List<MFBOauth2Client> lst = new List<MFBOauth2Client>();
            new DBHelper("SELECT * FROM allowedoauthclients WHERE owningUserName=?user").ReadRows(
                (comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { lst.Add(new MFBOauth2Client(dr)); });
            return lst;
        }

        public static IEnumerable<MFBOauth2Client> GetClientByID(string id)
        {
            List<MFBOauth2Client> lst = new List<MFBOauth2Client>();
            new DBHelper("SELECT * FROM allowedoauthclients WHERE ClientID=?id").ReadRows(
                (comm) => { comm.Parameters.AddWithValue("id", id); },
                (dr) => { lst.Add(new MFBOauth2Client(dr)); });
            return lst;
        }

        private void Validate()
        {
            if (String.IsNullOrWhiteSpace(ClientName))
                throw new MyFlightbookValidationException("Client Name cannot be empty");
            if (String.IsNullOrWhiteSpace(ClientSecret) && ClientType == ClientType.Confidential)
                throw new MyFlightbookValidationException("Client secret cannot be empty");
            if (String.IsNullOrWhiteSpace(ClientIdentifier))
                throw new MyFlightbookValidationException("Client identifier cannot be empty");
            if (ClientIdentifier.Contains("://"))
                throw new MyFlightbookValidationException("Client identifier cannot be a URL (URL client IDs are reserved for Client ID Metadata Documents)");
            if (Callbacks == null || !Callbacks.Any())
                throw new MyFlightbookValidationException("Callback URL cannot be empty");

            foreach (string callback in Callbacks)
            {
                if (!callback.StartsWith("https:", StringComparison.OrdinalIgnoreCase) && !callback.StartsWith("com.", StringComparison.OrdinalIgnoreCase))
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.InvariantCulture, "Callback URL {0} is not https", callback));
                if (!Uri.IsWellFormedUriString(callback, UriKind.Absolute))
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.InvariantCulture, "Callback URL {0} is not a valid https URL", callback));
            }


            if (String.IsNullOrWhiteSpace(Scope))
                throw new MyFlightbookValidationException("No scopes provided - this client will not be useful!");

            if (String.IsNullOrWhiteSpace(OwningUser))
                throw new MyFlightbookValidationException("No user provided!!!");

            foreach (MFBOauth2Client client in GetClientByID(ClientIdentifier))
                if (client.OwningUser.CompareCurrentCultureIgnoreCase(OwningUser) != 0)
                    throw new UnauthorizedAccessException("This clientID is already in use by another owner");
        }

        /// <summary>
        /// Saves a client to the database
        /// </summary>
        public void Commit()
        {
            Validate(); // will throw an exception as appropriate
            DBHelper dbh = new DBHelper("REPLACE INTO allowedoauthclients SET ClientID=?id, ClientSecret=?secret, CallBack=?callback, ClientName=?name, Scopes=?scopes, owningUserName=?user, ClientType=?clientType, logo=?logo");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ClientIdentifier.LimitTo(45));
                comm.Parameters.AddWithValue("secret", ClientSecret.LimitTo(255));
                comm.Parameters.AddWithValue("callback", CallbacksAsString.LimitTo(1024));
                comm.Parameters.AddWithValue("name", ClientName.LimitTo(255));
                comm.Parameters.AddWithValue("scopes", Scope.LimitTo(255));
                comm.Parameters.AddWithValue("user", OwningUser.LimitTo(255));
                comm.Parameters.AddWithValue("clientType", (int)ClientType);
                comm.Parameters.AddWithValue("logo", Logo);
            });
            OAuth2AuthorizationServer.RefreshClients();
        }

        /// <summary>
        /// Deletes the specified client.  Owning user MUST be passed too, as a security precaution.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        public static void DeleteForUser(string id, string user)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            DBHelper dbh = new DBHelper("DELETE FROM allowedoauthclients WHERE owningUserName=?user AND ClientID=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", user);
                comm.Parameters.AddWithValue("id", id);
            });
            OAuth2AuthorizationServer.RefreshClients();
        }

        #region Properties
        public string ClientIdentifier { get; set; }
        public string ClientName { get; set; }
        public string ClientSecret { get; set; }

        /// <summary>
        /// Allowed callbacks.  Each must be a valid URL
        /// </summary>
        public IEnumerable<string> Callbacks { get; set; }

        public string CallbacksAsString
        {
            get { return Callbacks == null ? string.Empty : String.Join(" ", Callbacks); }
        }

        /// <summary>
        /// Set of scopes, space delimited
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Is this a public or a confidential client?  Public clients have no secret and must use PKCE; confidential clients MUST use their secret
        /// </summary>
        public ClientType ClientType { get; set; }

        /// <summary>
        /// Username of the owning user for this client
        /// </summary>
        public string OwningUser { get; set; }

        /// <summary>
        /// PNG file for a logo
        /// </summary>
        public byte[] Logo { get; set; }

        private Uri AuthLinkForCallback(string szCallback)
        {
            if (szCallback == null)
                throw new ArgumentNullException(nameof(szCallback));
            return String.Format(CultureInfo.InvariantCulture, "~/mvc/oAuth/Authorize?client_id={0}&redirect_uri={1}&scope={2}&response_type=code",
                        WebUtility.UrlEncode(ClientIdentifier),
                        WebUtility.UrlEncode(szCallback),
                        WebUtility.UrlEncode(Scope)).ToAbsoluteURL(util.RequestContext.CurrentRequestUrl);
        }

        /// <summary>
        /// Allowed callbacks asn an enumerable list
        /// </summary>
        public IDictionary<string, Uri> AuthLinks
        {
            get
            {
                Dictionary<string, Uri> links = new Dictionary<string, Uri>();
                if (Callbacks != null)
                {
                    foreach (string sz in Callbacks)
                        links[sz] = AuthLinkForCallback(sz);
                }
                return links;
            }
        }
        #endregion

        private const char charCallBackSeparator = ' ';
        public static IEnumerable<string> AllowedCallbacksFromString(string sz)
        {
            return sz?.Split(new char[] { charCallBackSeparator, '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    /// <summary>
    /// Keeps track of issued client authorizations
    /// </summary>
    public class MFBOauthClientAuth
    {
        public MFBOauthClientAuth() { }

        public MFBOauthClientAuth(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            AuthorizationId = Convert.ToInt32(dr["AuthorizationId"], CultureInfo.InvariantCulture);
            CreatedOnUtc = Convert.ToDateTime(dr["CreatedOnUtc"], CultureInfo.InvariantCulture);
            ClientId = Convert.ToString(dr["ClientId"], CultureInfo.InvariantCulture);
            UserId = Convert.ToString(dr["UserId"], CultureInfo.InvariantCulture);
            Scope = Convert.ToString(dr["Scope"], CultureInfo.InvariantCulture);
            ExpirationDateUtc = Convert.ToDateTime(dr["ExpirationDateUtc"], CultureInfo.InvariantCulture);
            ClientName = (string)dr["ClientName"];
        }

        #region properties
        public int AuthorizationId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string ClientId { get; set; }

        public string ClientName { get; set; }
        public string UserId { get; set; }
        public string Scope { get; set; }
        public DateTime ExpirationDateUtc { get; set; }
        #endregion

        public Boolean fCommit()
        {
            DBHelper dbh = new DBHelper("INSERT INTO oauthclientauthorization SET AuthorizationId=?authid, CreatedOnUtc=NOW(), ClientId=?clientId, UserId=?userId, Scope=?scope, ExpirationDateUtc=?expDate");
            bool fResult = dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("authid", AuthorizationId);
                    comm.Parameters.AddWithValue("clientId", ClientId);
                    comm.Parameters.AddWithValue("userId", UserId);
                    comm.Parameters.AddWithValue("scope", Scope);
                    comm.Parameters.AddWithValue("expDate", ExpirationDateUtc);
                });
            if (fResult)
            {
                AuthorizationId = dbh.LastInsertedRowId;
                CreatedOnUtc = DateTime.UtcNow;
            }
            return fResult;
        }

        public static IEnumerable<MFBOauthClientAuth> GrantedAuths(string clientID, DateTime issuedUtc, string username)
        {
            List<MFBOauthClientAuth> lst = new List<MFBOauthClientAuth>();

            DBHelper dbh = new DBHelper("SELECT oca.*, aoc.ClientName FROM oauthclientauthorization oca INNER JOIN allowedoauthclients aoc ON oca.ClientId=aoc.clientid WHERE oca.clientID=?clientId AND createdonUTC < ?issuedUtc AND (ExpirationDateUtc IS NULL OR ExpirationDateUtc > utc_timestamp()) AND userID=?uname");
            dbh.ReadRows((comm) =>
                {
                    comm.Parameters.AddWithValue("clientId", clientID);
                    comm.Parameters.AddWithValue("issuedUtc", issuedUtc);
                    comm.Parameters.AddWithValue("uname", username);
                },
                (dr) => { lst.Add(new MFBOauthClientAuth(dr)); });
            return lst;
        }

        /// <summary>
        /// Retrieve a list of all granted authorizations for a user.  This is grouped by clientID, so it could represent multiple rows in a single one.
        /// </summary>
        /// <param name="username">The user</param>
        /// <returns>A list of gratned auths</returns>
        public static IEnumerable<MFBOauthClientAuth> GrantedAuthsForUser(string username)
        {
            List<MFBOauthClientAuth> lst = new List<MFBOauthClientAuth>();

            DBHelper dbh = new DBHelper(@"SELECT 
    0 AS AuthorizationID,
    ANY_VALUE(CreatedOnUtc) AS CreatedOnUTC,
    oca.ClientId,
    oca.UserId,
    ANY_VALUE(oca.Scope) AS Scope,
    ANY_VALUE(oca.ExpirationDateUtc) AS ExpirationDateUtc,
    aoc.ClientName
FROM
    oauthclientauthorization oca
        INNER JOIN
    allowedoauthclients aoc ON oca.ClientId = aoc.clientid
WHERE
    (ExpirationDateUtc IS NULL
        OR ExpirationDateUTC > UTC_TIMESTAMP())
        AND userID = ?uname
GROUP BY clientID;");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("uname", username); },
                (dr) => { lst.Add(new MFBOauthClientAuth(dr)); });
            return lst;
        }

        /// <summary>
        /// Delete all authorizations for a particular client for a particular user
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="clientID">The client</param>
        public static void RevokeAuthorization(string username, string clientID)
        {
            DBHelper dbh = new DBHelper("DELETE FROM oauthclientauthorization WHERE userID=?uname AND clientID=?clientID");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("uname", username); comm.Parameters.AddWithValue("clientID", clientID); });

            // New-format tokens die immediately.  (Legacy DNOA tokens die too: they require a live authorization row.)
            MySqlOAuthServerStore.RevokeTokensForUserAndClient(username, clientID);
        }
    }

    /// <summary>
    /// Builds MyFlightbook's OAuth server (MyFlightbook.OAuth.Server.OAuthServer) and holds the remaining DotNetOpenAuth-era compatibility pieces.
    /// </summary>
    public static class OAuth2AuthorizationServer
    {
        /// <summary>
        /// The client ID of the old PKCE proxy.  Refresh tokens minted through it carry this ID instead of the real client's; the server
        /// accepts them only from a public client that holds a live authorization for the user.
        /// </summary>
        public const string publicClientProxyID = "publicClientProxy";

        private static List<MFBOauth2Client> _lstClients = new List<MFBOauth2Client>(MFBOauth2Client.GetAvailableClients());

        public static void RefreshClients()
        {
            _lstClients = new List<MFBOauth2Client>(MFBOauth2Client.GetAvailableClients());
        }

        /// <summary>
        /// Looks up a registered client (case-insensitive, as before).  Returns null if not found (or if, somehow, ambiguous).
        /// </summary>
        public static MFBOauth2Client RegisteredClient(string clientId)
        {
            List<MFBOauth2Client> lst = _lstClients.FindAll(ic => ic.ClientIdentifier.CompareOrdinalIgnoreCase(clientId) == 0);
            return lst.Count == 1 ? lst[0] : null;
        }

        /// <summary>
        /// The scopes MyFlightbook defines (MFBOAuthScope, minus "none")
        /// </summary>
        public static IEnumerable<string> SupportedScopes
        {
            get { return Enum.GetNames(typeof(Services.MFBOAuthScope)).Where(s => s != Services.MFBOAuthScope.none.ToString()); }
        }

        private static readonly IOAuthClientRegistry _registry = new CompositeClientRegistry(id => RegisteredClient(id)?.ToOAuthClient(), null);  // null => no metadata-document (MCP) clients yet

        /// <summary>
        /// Builds the server for the current request.  Cheap (no I/O); the issuer and endpoints follow the host the request came in on.
        /// </summary>
        public static OAuthServer ServerForRequest(HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            OAuthServerOptions options = new OAuthServerOptions()
            {
                Issuer = request.Url.GetLeftPart(UriPartial.Authority),
                AuthorizationEndpoint = "~/mvc/oAuth/Authorize".ToAbsoluteURL(request),
                TokenEndpoint = "~/mvc/oAuth/OAuthToken".ToAbsoluteURL(request),
                RevocationEndpoint = "~/mvc/oAuth/Revoke".ToAbsoluteURL(request),
                LegacyProxyClientId = publicClientProxyID,
                LegacyAccessTokenReader = ReadLegacyAccessToken,
                OnLegacyTokenUsed = (kind, clientId, user) => MyFlightbook.SharedUtility.EventRecorder.EventRecorder.LogCall("Legacy (DotNetOpenAuth) OAuth {kind} token used: client {client}, user {user}", kind, clientId, user)
            };
            options.Resources.Add("~/mvc/oAuth/OAuthResource".ToAbsoluteURL(request).AbsoluteUri);  // the MyFlightbook API
            options.ScopesSupported.UnionWith(SupportedScopes);

            return new OAuthServer(options, new MySqlOAuthServerStore(), _registry);
        }

        #region Legacy (DotNetOpenAuth) access tokens - REMOVE 14+ days after cutover
        // DNOA access tokens were RSA-signed and -encrypted with the key in the machine key container below, and lived 14 days.
        // Once the last of them has expired, delete this region and set LegacyAccessTokenReader = null above.
        private const string KeyContainerName = "MyFlightbookContainer";

        private static LegacyTokenContents ReadLegacyAccessToken(string token)
        {
            CspParameters cp = new CspParameters()
            {
                Flags = CspProviderFlags.UseArchivableKey | CspProviderFlags.UseMachineKeyStore,
                KeyContainerName = KeyContainerName
            };
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cp))
            {
                // Same key pair signs (public half verifies) and encrypts (private half decrypts), exactly as DNOA used it
                return LegacyDnoaTokenReader.TryReadAccessToken(token, rsa, rsa, out LegacyTokenContents contents) ? contents : null;
            }
        }
        #endregion
    }
}
