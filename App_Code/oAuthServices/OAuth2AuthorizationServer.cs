#define SAMPLESONLY
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.ChannelElements;
using DotNetOpenAuth.OAuth2.Messages;
using MyFlightbook;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 * 
 * Much of the code in this file came from DotNetOpenAuth.
 *
*******************************************************/

namespace OAuthAuthorizationServer.Code
{    
    class MySqlNonceStore : INonceStore
    {
        public MySqlNonceStore()
        {
        }

        bool INonceStore.StoreNonce(string context, string nonce, DateTime timestampUtc)
        {
            if (context == null)
                throw new MyFlightbookException("Nonce: Context cannot be null");
            if (String.IsNullOrEmpty(nonce))
                throw new MyFlightbookException("Nonce: nonce cannot be null or empty");
            DBHelper dbh = new DBHelper("INSERT INTO nonce SET context=?c, nonce=?n, timestampUtc=?ts");
            bool fResult;

            try
            {
                fResult = dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("c", context);
                        comm.Parameters.AddWithValue("n", nonce);
                        comm.Parameters.AddWithValue("ts", timestampUtc);
                    });
            }
            catch
            {
                fResult = false;
            }
            return fResult;
        }
    }

    class MySqlCryptoKeyStore : ICryptoKeyStore
    {
        public MySqlCryptoKeyStore()
        {
        }

        CryptoKey keyFromDataReader(MySqlDataReader dr)
        {
            return new CryptoKey(Convert.FromBase64String(Convert.ToString(dr["keyData"], CultureInfo.InvariantCulture)), DateTime.SpecifyKind(Convert.ToDateTime(dr["ExpiresUtc"], CultureInfo.InvariantCulture), DateTimeKind.Utc));
        }

        CryptoKey ICryptoKeyStore.GetKey(string bucket, string handle)
        {
            DBHelper dbh = new DBHelper("SELECT * FROM cryptokeys WHERE bucket=?bucket AND handle=?handle");
            CryptoKey k = null;
            dbh.ReadRow((comm) => 
                {
                    comm.Parameters.AddWithValue("bucket", bucket);
                    comm.Parameters.AddWithValue("handle", handle);
                },
                (dr) =>
                    {
                        k = keyFromDataReader(dr);
                    });
            return k;
        }

        IEnumerable<KeyValuePair<string, CryptoKey>> ICryptoKeyStore.GetKeys(string bucket)
        {
            DBHelper dbh = new DBHelper("SELECT * FROM cryptokeys WHERE bucket=?bucket ORDER BY ExpiresUtc DESC");
            List<KeyValuePair<string, CryptoKey>> lst = new List<KeyValuePair<string,CryptoKey>>();
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("bucket", bucket); },
                (dr) =>
                {
                    lst.Add(new KeyValuePair<string, CryptoKey>(Convert.ToString(dr["handle"]), keyFromDataReader(dr)));
                });
            return lst;
        }

        void ICryptoKeyStore.RemoveKey(string bucket, string handle)
        {
            DBHelper dbh = new DBHelper("DELETE FROM cryptokeys WHERE bucket=?bucket AND handle=?handle");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("bucket", bucket);
                    comm.Parameters.AddWithValue("handle", handle);
                });
        }

        void ICryptoKeyStore.StoreKey(string bucket, string handle, CryptoKey key)
        {
            DBHelper dbh = new DBHelper("REPLACE INTO cryptokeys SET keyData=?keydata, bucket=?bucket, handle=?handle, ExpiresUtc=?exp");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("keydata", Convert.ToBase64String(key.Key));
                    comm.Parameters.AddWithValue("exp", key.ExpiresUtc);
                    comm.Parameters.AddWithValue("bucket", bucket);
                    comm.Parameters.AddWithValue("handle", handle);
                });
        }
    }

    /// <summary>
    /// An OAuth 2.0 Client that has registered with this Authorization Server.
    /// Adapted from DotNetOpenAuth sample code.
    /// </summary>
    [Serializable]
    public sealed partial class MFBOauth2Client : IClientDescription
    {
        public MFBOauth2Client(string clientIdentifier, string clientSecret, string callback, string name, string scope, string szuser)
        {
            ClientName = name;
            ClientIdentifier = clientIdentifier;
            ClientSecret = clientSecret;
            Callback = callback;
            ClientType = DotNetOpenAuth.OAuth2.ClientType.Public;
            Scope = scope;
            OwningUser = szuser;
        }

        private MFBOauth2Client(MySqlDataReader dr) : this((string)dr["ClientID"], (string)dr["ClientSecret"], (string)dr["Callback"], (string)dr["ClientName"], (string)dr["Scopes"], (string)dr["owningUserName"])
        {
            
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
            if (String.IsNullOrWhiteSpace(ClientSecret))
                throw new MyFlightbookValidationException("Client secret cannot be empty");
            if (String.IsNullOrWhiteSpace(ClientIdentifier))
                throw new MyFlightbookValidationException("Client identifier cannot be empty");
            if (String.IsNullOrWhiteSpace(Callback))
                throw new MyFlightbookValidationException("Callback URL cannot be empty");

            if (!Callback.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new MyFlightbookValidationException("Callback URL MUST be https");

            if (!Uri.IsWellFormedUriString(Callback, UriKind.Absolute))
                throw new MyFlightbookValidationException("Callback URL MUST be a valid https URL");

            if (String.IsNullOrWhiteSpace(Scope))
                throw new MyFlightbookValidationException("No scopes provided - this client will not be userful!");

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
            DBHelper dbh = new DBHelper("REPLACE INTO allowedoauthclients SET ClientID=?id, ClientSecret=?secret, CallBack=?callback, ClientName=?name, Scopes=?scopes, owningUserName=?user, ClientType=1");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ClientIdentifier);
                comm.Parameters.AddWithValue("secret", ClientSecret);
                comm.Parameters.AddWithValue("callback", Callback);
                comm.Parameters.AddWithValue("name", ClientName);
                comm.Parameters.AddWithValue("scopes", Scope);
                comm.Parameters.AddWithValue("user", OwningUser);
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
                throw new ArgumentNullException("id");
            if (user == null)
                throw new ArgumentNullException("user");

            DBHelper dbh = new DBHelper("DELETE FROM allowedoauthclients WHERE owningUserName=?user AND ClientID=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("user", user);
                comm.Parameters.AddWithValue("id", id);
            });
            OAuth2AuthorizationServer.RefreshClients();
        }

        #region Properties
        public string ClientIdentifier {get; set;}
        public string ClientName { get; set; }
        public string ClientSecret { get; set; }
        public string Callback { get; set; }
        public string Scope { get; set; }
        public ClientType ClientType { get; set; }
        public string OwningUser { get; set; }
        #endregion

        #region IConsumerDescription Members

        /// <summary>
        /// Gets the callback to use when an individual authorization request
        /// does not include an explicit callback URI.
        /// </summary>
        /// <value>
        /// An absolute URL; or <c>null</c> if none is registered.
        /// </value>
        Uri IClientDescription.DefaultCallback
        {
            get { return string.IsNullOrEmpty(this.Callback) ? null : new Uri(this.Callback); }
        }

        /// <summary>
        /// Gets the type of the client.
        /// </summary>
        ClientType IClientDescription.ClientType
        {
            get { return (ClientType)this.ClientType; }
        }

        /// <summary>
        /// Gets a value indicating whether a non-empty secret is registered for this client.
        /// </summary>
        bool IClientDescription.HasNonEmptySecret
        {
            get { return !string.IsNullOrEmpty(this.ClientSecret); }
        }

        /// <summary>
        /// Checks to see if the callback is from a set of local/debug domains as defined in the localconfig "DebugDomains" key, which is a comma-separated list of domains INCLUDING scheme.
        /// E.g., "http://localhost,http://dev.mydomain.com,http://127.0.0.1"
        /// </summary>
        /// <param name="callback">The Uri to test</param>
        /// <returns>True if it's in the allowed list</returns>
        bool IsDeveloperCallback(Uri callback)
        {
            string szDomains = LocalConfig.SettingForKey("DebugDomains");
            string[] rgDomains = szDomains.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string szLeftPart = callback.GetLeftPart(UriPartial.Authority);

            foreach (string sz in rgDomains)
            {
                if (szLeftPart.CompareOrdinalIgnoreCase(new Uri(sz).GetLeftPart(UriPartial.Authority)) == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a callback URI included in a client's authorization request
        /// is among those allowed callbacks for the registered client.
        /// </summary>
        /// <param name="callback">The absolute URI the client has requested the authorization result be received at.</param>
        /// <returns>
        ///   <c>true</c> if the callback URL is allowable for this client; otherwise, <c>false</c>.
        /// </returns>
        bool IClientDescription.IsCallbackAllowed(Uri callback)
        {
            if (string.IsNullOrEmpty(this.Callback))
            {
                // No callback rules have been set up for this client.
                return true;
            }

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (IsDeveloperCallback(callback))
                return true;

            // In this sample, it's enough of a callback URL match if the scheme and host match.
            // In a production app, it is advisable to require a match on the path as well.
            Uri acceptableCallbackPattern = new Uri(this.Callback);
            if (string.Equals(acceptableCallbackPattern.GetLeftPart(UriPartial.Authority), callback.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the specified client secret is correct.
        /// </summary>
        /// <param name="secret">The secret obtained from the client.</param>
        /// <returns><c>true</c> if the secret matches the one in the authorization server's record for the client; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// All string equality checks, whether checking secrets or their hashes,
        /// should be done using <see cref="MessagingUtilities.EqualsConstantTime"/> to mitigate timing attacks.
        /// </remarks>
        bool IClientDescription.IsValidClientSecret(string secret)
        {
            return MessagingUtilities.EqualsConstantTime(secret, this.ClientSecret);
        }

        #endregion
    }

    /// <summary>
    /// Keeps track of issued client authorizations
    /// </summary>
    public class MFBOauthClientAuth
    {
        public MFBOauthClientAuth()
        {
        }

        public MFBOauthClientAuth(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            AuthorizationId = Convert.ToInt32(dr["AuthorizationId"], CultureInfo.InvariantCulture);
            CreatedOnUtc = Convert.ToDateTime(dr["CreatedOnUtc"], CultureInfo.InvariantCulture);
            ClientId = Convert.ToString(dr["ClientId"], CultureInfo.InvariantCulture);
            UserId = Convert.ToString(dr["UserId"], CultureInfo.InvariantCulture);
            Scope = Convert.ToString(dr["Scope"], CultureInfo.InvariantCulture);
            ExpirationDateUtc = Convert.ToDateTime(dr["ExpirationDateUtc"], CultureInfo.InvariantCulture);
        }

        #region properties
        public int AuthorizationId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string ClientId { get; set; }
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

            DBHelper dbh = new DBHelper("SELECT * FROM oauthclientauthorization WHERE clientID=?clientId AND createdonUTC < ?issuedUtc AND (ExpirationDateUtc IS NULL OR ExpirationDateUtc > Now()) AND userID=?uname");
            dbh.ReadRows((comm) =>
                {
                    comm.Parameters.AddWithValue("clientId", clientID);
                    comm.Parameters.AddWithValue("issuedUtc", issuedUtc);
                    comm.Parameters.AddWithValue("uname", username);
                }, 
                (dr)=> {lst.Add(new MFBOauthClientAuth(dr));});
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

            DBHelper dbh = new DBHelper("SELECT * FROM oauthclientauthorization WHERE (ExpirationDateUtc IS NULL OR ExpirationDateUTC > UTC_TIMESTAMP()) AND userID=?uname GROUP BY clientID");
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
        }
    }

    /// <summary>
    /// Adapted from DotNetOpenAuth sample code
    /// </summary>
    public class OAuth2AuthorizationServer : IAuthorizationServerHost
    {
        static MySqlNonceStore _nonceStore = null;
        static MySqlCryptoKeyStore _cryptoStore = null;

        static List<MFBOauth2Client> _lstClients = new List<MFBOauth2Client>(MFBOauth2Client.GetAvailableClients());

        protected class KeyPair
        {
            public RSAParameters publicKey { get; set; }
            public RSAParameters privateKey { get; set; }

            public KeyPair() {  }

            public KeyPair(RSAParameters pubkey, RSAParameters privkey) : this()
            {
                publicKey = pubkey;
                privateKey = privkey;
            }
        }

        public static void RefreshClients()
        {
            _lstClients = new List<MFBOauth2Client>(MFBOauth2Client.GetAvailableClients());
        }
            
        private const string KeyContainerName = "MyFlightbookContainer";

        #region Implementation of IAuthorizationServerHost

        public ICryptoKeyStore CryptoKeyStore
        {
            get { return _cryptoStore ?? (_cryptoStore = new MySqlCryptoKeyStore()); }
        }

        public INonceStore NonceStore
        {
            get { return _nonceStore ?? (_nonceStore = new MySqlNonceStore()); }
        }

        public AccessTokenResult CreateAccessToken(IAccessTokenRequest accessTokenRequestMessage)
        {
            // Just for the sake of the sample, we use a short-lived token.  This can be useful to mitigate the security risks
            // of access tokens that are used over standard HTTP.
            // But this is just the lifetime of the access token.  The client can still renew it using their refresh token until
            // the authorization itself expires.
            var accessToken = new AuthorizationServerAccessToken() { Lifetime = TimeSpan.FromDays(14) };

            // Also take into account the remaining life of the authorization and artificially shorten the access token's lifetime
            // to account for that if necessary.
            //// TODO: code here

            // For this sample, we assume just one resource server.
            // If this authorization server needs to mint access tokens for more than one resource server,
            // we'd look at the request message passed to us and decide which public key to return.
            // accessToken.ResourceServerEncryptionKey = new RSACryptoServiceProvider();
            accessToken.ResourceServerEncryptionKey = CreateRSA();
            // string szPubKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC32W+vIY9eZYY13Z0TqONE5LG3BHH6x4EWgb/LSEV674eFRw/AOvxphM7FjvoS4auZ1Yom4G6oFjDCR917gttma2b+7IuEhV5XdHM3lbH0dSrglASKtM6uHR0qjW0FPQR6rCKMHC1xSytAudI46nr/OkpcPM8KeXgJYvp+BYE0E6gjbwydcrgULCtcC0A3mZABixshjSaxzxUWCxA9RC7hSKPp9JptEcHcrJddaWzVORZHW+lUiNcFqXsm1K4CxoXE/KHenaz7d9GtA2vAvk1miueA6tsH1UOmZUY9rNVTKLoig5kKtYePSaa9/CZTEFYnhPkQtHZNZDoiN/e327ld ericbe@myflightbook.com";
            // accessToken.ResourceServerEncryptionKey.ImportCspBlob(System.Text.Encoding.ASCII.GetBytes(szPubKey));
            // accessToken.ResourceServerEncryptionKey.ImportParameters(ResourceServerEncryptionPublicKey);

            accessToken.AccessTokenSigningKey = CreateRSA();

            var result = new AccessTokenResult(accessToken);
            return result;
        }

        public IClientDescription GetClient(string clientIdentifier)
        {
            List<MFBOauth2Client> lst = _lstClients.FindAll(ic => ic.ClientIdentifier.CompareTo(clientIdentifier) == 0);
            if (lst.Count == 0)
                throw new MyFlightbookException(String.Format("GetClient: client '{0}' not found.", clientIdentifier));

            if (lst.Count > 1)
                throw new MyFlightbookException(String.Format("GetClient: multiple clients with ID '{0}' found", clientIdentifier));

            return lst[0];
        }

        public bool IsAuthorizationValid(IAuthorizationDescription authorization)
        {
            if (authorization == null)
                throw new ArgumentNullException("authorization");
            return this.IsAuthorizationValid(authorization.Scope, authorization.ClientIdentifier, authorization.UtcIssued, authorization.User);
        }

        public AutomatedUserAuthorizationCheckResponse CheckAuthorizeResourceOwnerCredentialGrant(string userName, string password, IAccessTokenRequest accessRequest)
        {
            // This web site delegates user authentication to OpenID Providers, and as such no users have local passwords with this server.
            throw new NotSupportedException();
        }

        public AutomatedAuthorizationCheckResponse CheckAuthorizeClientCredentialsGrant(IAccessTokenRequest accessRequest)
        {
            // Find the client
            var client = _lstClients.Single(consumerCandidate => consumerCandidate.ClientIdentifier == accessRequest.ClientIdentifier);

            // Parse the scopes the client is authorized for
            var scopesClientIsAuthorizedFor = OAuthUtilities.SplitScopes(client.Scope);

            // Check if the scopes that are being requested are a subset of the scopes the user is authorized for.
            // If not, that means that the user has requested at least one scope it is not authorized for
            var clientIsAuthorizedForRequestedScopes = accessRequest.Scope.IsSubsetOf(scopesClientIsAuthorizedFor);

            // The token request is approved when the client is authorized for the requested scopes
            var isApproved = clientIsAuthorizedForRequestedScopes;

            return new AutomatedAuthorizationCheckResponse(accessRequest, isApproved);
        }

        #endregion

        public bool CanBeAutoApproved(EndUserAuthorizationRequest authorizationRequest)
        {
            if (authorizationRequest == null)
            {
                throw new ArgumentNullException("authorizationRequest");
            }

            return false;
        }

#if NOCONTAINER
#if SAMPLESONLY
        /// <summary>
        /// The FOR SAMPLE ONLY hard-coded public key of the authorization server that is used to verify the signature on access tokens.
        /// </summary>
        /// <remarks>
        /// In a real app, the authorization server public key would likely come from the server's HTTPS certificate,
        /// but in any case would be determined by the authorization server and its policies.
        /// The hard-coded value used here is so this sample works well with the OAuthAuthorizationServer sample,
        /// which has the corresponding sample private key. 
        /// </remarks>
        public static readonly RSAParameters AuthorizationServerSigningPublicKey = new RSAParameters
        {
            /*
            Exponent = new byte[] { 1, 0, 1 },
            Modulus = new byte[] { 210, 95, 53, 12, 203, 114, 150, 23, 23, 88, 4, 200, 47, 219, 73, 54, 146, 253, 126, 121, 105, 91, 118, 217, 182, 167, 140, 6, 67, 112, 97, 183, 66, 112, 245, 103, 136, 222, 205, 28, 196, 45, 6, 223, 192, 76, 56, 180, 90, 120, 144, 19, 31, 193, 37, 129, 186, 214, 36, 53, 204, 53, 108, 133, 112, 17, 133, 244, 3, 12, 230, 29, 243, 51, 79, 253, 10, 111, 185, 23, 74, 230, 99, 94, 78, 49, 209, 39, 95, 213, 248, 212, 22, 4, 222, 145, 77, 190, 136, 230, 134, 70, 228, 241, 194, 216, 163, 234, 52, 1, 64, 181, 139, 128, 90, 255, 214, 60, 168, 233, 254, 110, 31, 102, 58, 67, 201, 33 },
             */
            Exponent = new byte[] {1, 0, 1},
            Modulus = new byte[] {180, 39, 237, 229, 222, 155, 51, 90, 15, 44, 103, 224, 73, 209, 91, 25, 97, 131, 87, 12, 96, 192, 176, 252, 149, 135, 142, 155, 219, 248, 8, 168, 144, 81, 92, 102, 221, 40, 0, 23, 173, 104, 134, 53, 224, 142, 190, 191, 249, 246, 251, 228, 87, 94, 144, 128, 162, 70, 138, 61, 182, 45, 197, 65, 69, 147, 138, 152, 52, 181, 217, 13, 156, 234, 231, 122, 252, 175, 255, 163, 109, 194, 106, 255, 201, 24, 56, 66, 172, 240, 190, 46, 16, 171, 252, 177, 206, 37, 203, 193, 244, 62, 166, 244, 244, 138, 244, 246, 76, 197, 23, 222, 41, 7, 74, 41, 23, 119, 8, 89, 47, 210, 24, 209, 83, 100, 110, 41}  
        };
#else
		[Obsolete("You must use a real key for a real app.", true)]
		public static readonly RSAParameters AuthorizationServerSigningPublicKey = new RSAParameters();
#endif
#else
        public static RSAParameters AuthorizationServerSigningPublicKey
        {
            get
            {
                return GetKeyFromContainer(KeyContainerName).publicKey;
            }
        }
#endif

        /// <summary>
        /// Creates the RSA key used by all the crypto service provider instances we create.
        /// </summary>
        /// <returns>RSA data that includes the private key.</returns>
        public static RSAParameters CreateAuthorizationServerSigningKey()
        {
#if NOCONTAINER
#if SAMPLESONLY
			// Since the sample authorization server and the sample resource server must work together,
			// we hard-code a FOR SAMPLE USE ONLY key pair.  The matching public key information is hard-coded into the OAuthResourceServer sample.
			// In a real app, the RSA parameters would typically come from a certificate that may already exist.  It may simply be the HTTPS certificate for the auth server.
			return new RSAParameters {
                /*
				Exponent = new byte[] { 1, 0, 1 },
				Modulus = new byte[] { 210, 95, 53, 12, 203, 114, 150, 23, 23, 88, 4, 200, 47, 219, 73, 54, 146, 253, 126, 121, 105, 91, 118, 217, 182, 167, 140, 6, 67, 112, 97, 183, 66, 112, 245, 103, 136, 222, 205, 28, 196, 45, 6, 223, 192, 76, 56, 180, 90, 120, 144, 19, 31, 193, 37, 129, 186, 214, 36, 53, 204, 53, 108, 133, 112, 17, 133, 244, 3, 12, 230, 29, 243, 51, 79, 253, 10, 111, 185, 23, 74, 230, 99, 94, 78, 49, 209, 39, 95, 213, 248, 212, 22, 4, 222, 145, 77, 190, 136, 230, 134, 70, 228, 241, 194, 216, 163, 234, 52, 1, 64, 181, 139, 128, 90, 255, 214, 60, 168, 233, 254, 110, 31, 102, 58, 67, 201, 33 },
				P = new byte[] { 237, 238, 79, 75, 29, 57, 145, 201, 57, 177, 215, 108, 40, 77, 232, 237, 113, 38, 157, 195, 174, 134, 188, 175, 121, 28, 11, 236, 80, 146, 12, 38, 8, 12, 104, 46, 6, 247, 14, 149, 196, 23, 130, 116, 141, 137, 225, 74, 84, 111, 44, 163, 55, 10, 246, 154, 195, 158, 186, 241, 162, 11, 217, 77 },
				Q = new byte[] { 226, 89, 29, 67, 178, 205, 30, 152, 184, 165, 15, 152, 131, 245, 141, 80, 150, 3, 224, 136, 188, 248, 149, 36, 200, 250, 207, 156, 224, 79, 150, 191, 84, 214, 233, 173, 95, 192, 55, 123, 124, 255, 53, 85, 11, 233, 156, 66, 14, 27, 27, 163, 108, 199, 90, 37, 118, 38, 78, 171, 80, 26, 101, 37 },
				DP = new byte[] { 108, 176, 122, 132, 131, 187, 50, 191, 203, 157, 84, 29, 82, 100, 20, 205, 178, 236, 195, 17, 10, 254, 253, 222, 226, 226, 79, 8, 10, 222, 76, 178, 106, 230, 208, 8, 134, 162, 1, 133, 164, 232, 96, 109, 193, 226, 132, 138, 33, 252, 15, 86, 23, 228, 232, 54, 86, 186, 130, 7, 179, 208, 217, 217 },
				DQ = new byte[] { 175, 63, 252, 46, 140, 99, 208, 138, 194, 123, 218, 101, 101, 214, 91, 65, 199, 196, 220, 182, 66, 73, 221, 128, 11, 180, 85, 198, 202, 206, 20, 147, 179, 102, 106, 170, 247, 245, 229, 127, 81, 58, 111, 218, 151, 76, 154, 213, 114, 2, 127, 21, 187, 133, 102, 64, 151, 7, 245, 229, 34, 50, 45, 153 },
				InverseQ = new byte[] { 137, 156, 11, 248, 118, 201, 135, 145, 134, 121, 14, 162, 149, 14, 98, 84, 108, 160, 27, 91, 230, 116, 216, 181, 200, 49, 34, 254, 119, 153, 179, 52, 231, 234, 36, 148, 71, 161, 182, 171, 35, 182, 46, 164, 179, 100, 226, 71, 119, 23, 0, 16, 240, 4, 30, 57, 76, 109, 89, 131, 56, 219, 71, 206 },
				D = new byte[] { 108, 15, 123, 176, 150, 208, 197, 72, 23, 53, 159, 63, 53, 85, 238, 197, 153, 187, 156, 187, 192, 226, 186, 170, 26, 168, 245, 196, 65, 223, 248, 81, 170, 79, 91, 191, 83, 15, 31, 77, 39, 119, 249, 143, 245, 183, 49, 105, 115, 15, 122, 242, 87, 221, 94, 230, 196, 146, 59, 7, 103, 94, 9, 223, 146, 180, 189, 86, 190, 94, 242, 59, 32, 54, 23, 181, 124, 170, 63, 172, 90, 158, 169, 140, 6, 102, 170, 0, 135, 199, 35, 196, 212, 238, 196, 56, 14, 0, 140, 197, 169, 240, 156, 43, 182, 123, 102, 79, 89, 20, 120, 171, 43, 223, 58, 190, 230, 166, 185, 162, 186, 226, 31, 206, 196, 188, 104, 1 },
                 */
                D = new byte[] {73, 181, 164, 146, 78, 119, 245, 148, 189, 108, 143, 187, 166, 184, 47, 171, 188, 199, 254, 15, 164, 110, 114, 123, 133, 186, 134, 208, 162, 57, 99, 97, 132, 90, 165, 145, 184, 158, 171, 27, 4, 234, 37, 47, 90, 7, 77, 104, 66, 159, 153, 4, 29, 243, 36, 240, 92, 116, 188, 14, 239, 192, 222, 89, 92, 149, 6, 75, 217, 117, 18, 29, 114, 144, 80, 45, 108, 179, 235, 119, 200, 215, 224, 128, 168, 61, 235, 178, 42, 93, 81, 193, 20, 142, 59, 57, 36, 214, 139, 47, 47, 43, 161, 203, 140, 17, 212, 159, 30, 172, 109, 94, 176, 209, 75, 101, 216, 211, 0, 115, 162, 78, 1, 142, 216, 112, 193, 89},
                DP = new byte[] {77, 231, 146, 209, 123, 106, 90, 126, 121, 205, 209, 156, 71, 251, 243, 61, 4, 241, 212, 141, 181, 195, 224, 192, 117, 152, 30, 242, 230, 172, 211, 217, 124, 26, 25, 0, 181, 202, 193, 102, 98, 191, 98, 108, 149, 230, 175, 7, 104, 71, 42, 130, 158, 248, 200, 110, 42, 72, 7, 48, 60, 24, 102, 225},
                DQ = new byte[] {81, 52, 76, 136, 199, 209, 1, 58, 45, 31, 102, 143, 185, 248, 125, 165, 154, 181, 107, 143, 251, 79, 87, 15, 153, 38, 204, 103, 137, 133, 85, 130, 235, 168, 157, 178, 218, 121, 199, 191, 186, 34, 51, 81, 147, 136, 64, 162, 112, 164, 147, 206, 75, 46, 90, 234, 122, 116, 37, 162, 215, 99, 79, 37},
                Exponent = new byte[] {1, 0, 1},
                InverseQ = new byte[] {170, 94, 179, 238, 248, 91, 32, 121, 194, 227, 54, 253, 180, 121, 25, 150, 10, 207, 57, 92, 171, 65, 142, 245, 2, 22, 228, 82, 195, 98, 56, 157, 17, 89, 84, 120, 98, 233, 242, 14, 98, 138, 33, 124, 15, 5, 175, 7, 168, 103, 209, 125, 62, 208, 215, 129, 77, 26, 9, 34, 242, 123, 126, 33},
                Modulus = new byte[] {180, 39, 237, 229, 222, 155, 51, 90, 15, 44, 103, 224, 73, 209, 91, 25, 97, 131, 87, 12, 96, 192, 176, 252, 149, 135, 142, 155, 219, 248, 8, 168, 144, 81, 92, 102, 221, 40, 0, 23, 173, 104, 134, 53, 224, 142, 190, 191, 249, 246, 251, 228, 87, 94, 144, 128, 162, 70, 138, 61, 182, 45, 197, 65, 69, 147, 138, 152, 52, 181, 217, 13, 156, 234, 231, 122, 252, 175, 255, 163, 109, 194, 106, 255, 201, 24, 56, 66, 172, 240, 190, 46, 16, 171, 252, 177, 206, 37, 203, 193, 244, 62, 166, 244, 244, 138, 244, 246, 76, 197, 23, 222, 41, 7, 74, 41, 23, 119, 8, 89, 47, 210, 24, 209, 83, 100, 110, 41},
                P = new byte[] {219, 157, 178, 12, 125, 102, 64, 239, 34, 35, 183, 43, 114, 156, 193, 6, 20, 153, 234, 128, 6, 137, 82, 83, 194, 64, 181, 242, 81, 191, 116, 135, 124, 199, 173, 34, 132, 248, 173, 232, 201, 249, 92, 161, 77, 142, 241, 45, 227, 158, 63, 46, 203, 68, 248, 128, 30, 50, 58, 194, 230, 50, 17, 231},
                Q = new byte[] {210, 0, 170, 1, 13, 219, 133, 215, 54, 98, 68, 102, 143, 21, 141, 171, 48, 86, 95, 137, 111, 13, 107, 64, 17, 91, 225, 173, 130, 4, 225, 38, 210, 173, 185, 113, 144, 247, 163, 82, 47, 230, 235, 249, 216, 179, 198, 0, 158, 119, 156, 36, 111, 228, 232, 110, 73, 0, 129, 137, 13, 44, 157, 111} 
			};
#else
			// This is how you could generate your own public/private key pair.  
			// As we generate a new random key, we need to set the UseMachineKeyStore flag so that this doesn't
			// crash on IIS. For more information: 
			// http://social.msdn.microsoft.com/Forums/en-US/clr/thread/7ea48fd0-8d6b-43ed-b272-1a0249ae490f?prof=required
			var cspParameters = new CspParameters();
			cspParameters.Flags = CspProviderFlags.UseArchivableKey | CspProviderFlags.UseMachineKeyStore;
			var keyPair = new RSACryptoServiceProvider(cspParameters);

			// After exporting the private/public key information, read the information out and store it somewhere
			var privateKey = keyPair.ExportParameters(true);
			var publicKey = keyPair.ExportParameters(false);

			// Ultimately the private key information must be what is returned through the AccessTokenSigningPrivateKey property.
			return privateKey;
#endif
#else
            return GetKeyFromContainer(KeyContainerName).privateKey;
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static RSACryptoServiceProvider CreateRSA()
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(CreateAuthorizationServerSigningKey());
            return rsa;
        }

        private bool IsAuthorizationValid(HashSet<string> requestedScopes, string clientIdentifier, DateTime issuedUtc, string username)
        {
            // If db precision exceeds token time precision (which is common), the following query would
            // often disregard a token that is minted immediately after the authorization record is stored in the db.
            // To compensate for this, we'll increase the timestamp on the token's issue date by 1 second.
            issuedUtc += TimeSpan.FromSeconds(1);
            IEnumerable<MFBOauthClientAuth> lst = MFBOauthClientAuth.GrantedAuths(clientIdentifier, issuedUtc, username);
            /*
            var grantedScopeStrings = from auth in MvcApplication.DataContext.ClientAuthorizations
                                      where
                                          auth.Client.ClientIdentifier == clientIdentifier &&
                                          auth.CreatedOnUtc <= issuedUtc &&
                                          (!auth.ExpirationDateUtc.HasValue || auth.ExpirationDateUtc.Value >= DateTime.UtcNow) &&
                                          auth.User.OpenIDClaimedIdentifier == username
                                      select auth.Scope;
            */
            if (lst.Count() == 0)
            {
                // No granted authorizations prior to the issuance of this token, so it must have been revoked.
                // Even if later authorizations restore this client's ability to call in, we can't allow
                // access tokens issued before the re-authorization because the revoked authorization should
                // effectively and permanently revoke all access and refresh tokens.
                return false;
            }

            var grantedScopes = new HashSet<string>(OAuthUtilities.ScopeStringComparer);
            foreach (MFBOauthClientAuth clientauth in lst)
            {
                grantedScopes.UnionWith(OAuthUtilities.SplitScopes(clientauth.Scope));
            }

            return requestedScopes.IsSubsetOf(grantedScopes);
        }

        #region keystore
        private static CspParameters GetCspParams(string ContainerName)
        {
            // This is how you could generate your own public/private key pair.  
            // As we generate a new random key, we need to set the UseMachineKeyStore flag so that this doesn't
            // crash on IIS. For more information: 
            // http://social.msdn.microsoft.com/Forums/en-US/clr/thread/7ea48fd0-8d6b-43ed-b272-1a0249ae490f?prof=required
            CspParameters cp = new CspParameters()
            {
                Flags = CspProviderFlags.UseArchivableKey | CspProviderFlags.UseMachineKeyStore,
                KeyContainerName = ContainerName
            };
            return cp;
        }


        // Following are from http://msdn.microsoft.com/en-us/library/tswxhw92%28v=vs.110%29.aspx

        public static void GenKey_SaveInContainer(string ContainerName)
        {
            // Create a new instance of RSACryptoServiceProvider that accesses
            // the key container MyKeyContainerName.
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(ContainerName))) { }
        }


        protected static KeyPair GetKeyFromContainer(string ContainerName)
        {
            // Create a new instance of RSACryptoServiceProvider that accesses
            // the key container MyKeyContainerName.
            KeyPair kp;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(ContainerName)))
            {
                kp = new KeyPair(rsa.ExportParameters(false), rsa.ExportParameters(true));
            }
            return kp;
        }

        public static void DeleteKeyFromContainer(string ContainerName)
        {
            // Create a new instance of RSACryptoServiceProvider that accesses
            // the key container.
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(ContainerName)))
            {
                // Delete the key entry in the container.
                rsa.PersistKeyInCsp = false;

                // Call Clear to release resources and delete the key from the container.
                rsa.Clear();
            }
        }
        #endregion
    }
}