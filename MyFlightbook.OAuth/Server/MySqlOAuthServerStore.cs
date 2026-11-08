using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * MySQL persistence for MyFlightbook.OAuth.Server.  Tables: oauthtokens, oauthauthcodes, oauthclientauthorization (existing),
 * cryptokeys (existing; read-only, legacy refresh tokens).  See Support/OAuthMigration/OAuthServer-Schema.sql.
 *
*******************************************************/

namespace MyFlightbook.OAuth.Server
{
    public class MySqlOAuthServerStore : IOAuthServerStore
    {
        private static DateTime Utc(object o)
        {
            return DateTime.SpecifyKind(Convert.ToDateTime(o, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        }

        private static DateTime? NullableUtc(MySqlDataReader dr, string field)
        {
            object o = dr[field];
            return (o == null || o == DBNull.Value) ? (DateTime?)null : Utc(o);
        }

        private static string NullableString(MySqlDataReader dr, string field)
        {
            object o = dr[field];
            return (o == null || o == DBNull.Value) ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static void Check(DBHelper dbh)
        {
            if (!String.IsNullOrEmpty(dbh.LastError))
                throw new MyFlightbookException("OAuth store error: " + dbh.LastError);
        }

        #region Authorization codes
        public void SaveAuthorizationCode(AuthorizationCodeRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            DBHelper dbh = new DBHelper(@"INSERT INTO oauthauthcodes SET CodeHash=?hash, ClientId=?client, UserId=?user, RedirectUri=?redir, Scope=?scope, Resource=?resource,
                CodeChallenge=?challenge, CodeChallengeMethod=?method, ExpiresUtc=?exp, FamilyId=?family, Used=0");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("hash", record.CodeHash);
                comm.Parameters.AddWithValue("client", record.ClientId);
                comm.Parameters.AddWithValue("user", record.UserId);
                comm.Parameters.AddWithValue("redir", record.RedirectHREF);
                comm.Parameters.AddWithValue("scope", record.Scope ?? string.Empty);
                comm.Parameters.AddWithValue("resource", record.Resource);
                comm.Parameters.AddWithValue("challenge", record.CodeChallenge);
                comm.Parameters.AddWithValue("method", record.CodeChallengeMethod);
                comm.Parameters.AddWithValue("exp", record.ExpiresUtc);
                comm.Parameters.AddWithValue("family", record.FamilyId);
            });
            Check(dbh);
        }

        public AuthorizationCodeRecord ConsumeAuthorizationCode(string codeHash, out bool wasAlreadyUsed)
        {
            // Atomic claim: only one caller can flip Used from 0 to 1
            DBHelper dbhClaim = new DBHelper("UPDATE oauthauthcodes SET Used=1 WHERE CodeHash=?hash AND Used=0");
            dbhClaim.DoNonQuery((comm) => { comm.Parameters.AddWithValue("hash", codeHash); });
            Check(dbhClaim);
            wasAlreadyUsed = dbhClaim.AffectedRowCount == 0;

            AuthorizationCodeRecord result = null;
            DBHelper dbh = new DBHelper("SELECT * FROM oauthauthcodes WHERE CodeHash=?hash");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("hash", codeHash); },
                (dr) =>
                {
                    result = new AuthorizationCodeRecord()
                    {
                        CodeHash = (string)dr["CodeHash"],
                        ClientId = (string)dr["ClientId"],
                        UserId = (string)dr["UserId"],
                        RedirectHREF = (string)dr["RedirectUri"],
                        Scope = (string)dr["Scope"],
                        Resource = NullableString(dr, "Resource"),
                        CodeChallenge = NullableString(dr, "CodeChallenge"),
                        CodeChallengeMethod = NullableString(dr, "CodeChallengeMethod"),
                        ExpiresUtc = Utc(dr["ExpiresUtc"]),
                        FamilyId = (string)dr["FamilyId"]
                    };
                });
            Check(dbh);
            return result;
        }
        #endregion

        #region Tokens
        public void SaveToken(TokenRecord token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            DBHelper dbh = new DBHelper(@"INSERT INTO oauthtokens SET TokenHash=?hash, TokenType=?type, ClientId=?client, UserId=?user, Scope=?scope, Resource=?resource,
                FamilyId=?family, IssuedUtc=?issued, ExpiresUtc=?exp, RotatedUtc=NULL");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("hash", token.TokenHash);
                comm.Parameters.AddWithValue("type", token.TokenType == OAuthTokenType.Refresh ? "refresh" : "access");
                comm.Parameters.AddWithValue("client", token.ClientId);
                comm.Parameters.AddWithValue("user", token.UserId);
                comm.Parameters.AddWithValue("scope", token.Scope ?? string.Empty);
                comm.Parameters.AddWithValue("resource", token.Resource);
                comm.Parameters.AddWithValue("family", token.FamilyId);
                comm.Parameters.AddWithValue("issued", token.IssuedUtc);
                comm.Parameters.AddWithValue("exp", token.ExpiresUtc);
            });
            Check(dbh);
        }

        public TokenRecord FindToken(string tokenHash)
        {
            TokenRecord result = null;
            DBHelper dbh = new DBHelper("SELECT * FROM oauthtokens WHERE TokenHash=?hash");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("hash", tokenHash); },
                (dr) =>
                {
                    result = new TokenRecord()
                    {
                        TokenHash = (string)dr["TokenHash"],
                        TokenType = ((string)dr["TokenType"]) == "refresh" ? OAuthTokenType.Refresh : OAuthTokenType.Access,
                        ClientId = (string)dr["ClientId"],
                        UserId = (string)dr["UserId"],
                        Scope = (string)dr["Scope"],
                        Resource = NullableString(dr, "Resource"),
                        FamilyId = NullableString(dr, "FamilyId"),
                        IssuedUtc = Utc(dr["IssuedUtc"]),
                        ExpiresUtc = NullableUtc(dr, "ExpiresUtc"),
                        RotatedUtc = NullableUtc(dr, "RotatedUtc")
                    };
                });
            Check(dbh);
            return result;
        }

        public bool MarkRotated(string tokenHash, DateTime utcNow)
        {
            DBHelper dbh = new DBHelper("UPDATE oauthtokens SET RotatedUtc=?now WHERE TokenHash=?hash AND RotatedUtc IS NULL");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("now", utcNow); comm.Parameters.AddWithValue("hash", tokenHash); });
            Check(dbh);
            return dbh.AffectedRowCount == 1;
        }

        public void RevokeToken(string tokenHash)
        {
            DBHelper dbh = new DBHelper("DELETE FROM oauthtokens WHERE TokenHash=?hash");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("hash", tokenHash); });
            Check(dbh);
        }

        public void RevokeFamily(string familyId)
        {
            if (String.IsNullOrEmpty(familyId))
                return;
            DBHelper dbh = new DBHelper("DELETE FROM oauthtokens WHERE FamilyId=?family");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("family", familyId); });
            Check(dbh);
        }

        /// <summary>
        /// Call from MFBOauthClientAuth.RevokeAuthorization so that revoking an app kills its tokens immediately.
        /// </summary>
        public static void RevokeTokensForUserAndClient(string userId, string clientId)
        {
            DBHelper dbh = new DBHelper("DELETE FROM oauthtokens WHERE UserId=?user AND ClientId=?client");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("user", userId); comm.Parameters.AddWithValue("client", clientId); });
        }

        /// <summary>
        /// Housekeeping for the nightly run: expired access tokens, rotated refresh tokens past any grace, and stale codes.
        /// </summary>
        public static void PurgeExpired(TimeSpan rotatedRetention)
        {
            new DBHelper("DELETE FROM oauthtokens WHERE ExpiresUtc IS NOT NULL AND ExpiresUtc < UTC_TIMESTAMP()").DoNonQuery((comm) => { });
            new DBHelper("DELETE FROM oauthtokens WHERE RotatedUtc IS NOT NULL AND RotatedUtc < ?cutoff").DoNonQuery((comm) => { comm.Parameters.AddWithValue("cutoff", DateTime.UtcNow.Subtract(rotatedRetention)); });
            new DBHelper("DELETE FROM oauthauthcodes WHERE ExpiresUtc < (UTC_TIMESTAMP() - INTERVAL 1 DAY)").DoNonQuery((comm) => { });
        }
        #endregion

        #region Authorizations (consent)
        public void RecordAuthorization(string clientId, string userId, string scope)
        {
            // Same table and semantics as MFBOauthClientAuth.fCommit (10-year expiration), but UTC_TIMESTAMP() rather than NOW()
            DBHelper dbh = new DBHelper("INSERT INTO oauthclientauthorization SET CreatedOnUtc=UTC_TIMESTAMP(), ClientId=?clientId, UserId=?userId, Scope=?scope, ExpirationDateUtc=?expDate");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("clientId", clientId);
                comm.Parameters.AddWithValue("userId", userId);
                comm.Parameters.AddWithValue("scope", (scope ?? string.Empty).LimitTo(255));
                comm.Parameters.AddWithValue("expDate", DateTime.UtcNow.AddYears(10));
            });
            Check(dbh);
        }

        public bool HasActiveAuthorization(string clientId, string userId, DateTime issuedUtc, IEnumerable<string> scopes)
        {
            // DNOA rule (see OAuth2AuthorizationServer.IsAuthorizationValid), including its 1-second precision slop.
            HashSet<string> granted = new HashSet<string>(OAuthScopes.ScopeStringComparer);
            bool fAny = false;
            DBHelper dbh = new DBHelper(@"SELECT Scope FROM oauthclientauthorization WHERE ClientId=?clientId AND UserId=?userId AND CreatedOnUtc < ?issued
                AND (ExpirationDateUtc IS NULL OR ExpirationDateUtc > UTC_TIMESTAMP())");
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("clientId", clientId);
                comm.Parameters.AddWithValue("userId", userId);
                comm.Parameters.AddWithValue("issued", issuedUtc.AddSeconds(1));
            },
            (dr) =>
            {
                fAny = true;
                granted.UnionWith(OAuthScopes.SplitScopes(NullableString(dr, "Scope")));
            });
            Check(dbh);
            return fAny && (scopes ?? Enumerable.Empty<string>()).All(s => granted.Contains(s));
        }
        #endregion

        public byte[] GetLegacyCryptoKey(string bucket, string handle)
        {
            byte[] key = null;
            DBHelper dbh = new DBHelper("SELECT keyData FROM cryptokeys WHERE bucket=?bucket AND handle=?handle");
            dbh.ReadRow((comm) =>
            {
                comm.Parameters.AddWithValue("bucket", bucket);
                comm.Parameters.AddWithValue("handle", handle);
            },
            (dr) => { key = Convert.FromBase64String(Convert.ToString(dr["keyData"], CultureInfo.InvariantCulture)); });
            return key;
        }
    }
}
