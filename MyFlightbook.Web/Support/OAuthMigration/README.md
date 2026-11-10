# Migrating MyFlightbook off DotNetOpenAuth

Status (2026-11-05): legacy-token reader + server core built and tested; not yet wired in. Nothing here changes runtime behavior yet.

## TL;DR

* **Yes, existing refresh tokens can be carried forward indefinitely, with no time-boxed migration.**
  A DNOA refresh token is just `{handle}!{base64url}`: an HMAC-SHA256-signed, AES-256-CBC-encrypted
  query string (`ClientIdentifier`, `User`, `Scope`, `ts`, `t`). The key is the `cryptokeys` row with
  `bucket = 'https://localhost/dnoa/oauth_refresh_token'` and `handle = {handle}`. DNOA never checks
  `cryptokeys.ExpiresUtc` when *decoding* (only when picking a key to *encode* with), so every refresh
  token ever issued is decodable as long as those rows exist.
  `MyFlightbook.OAuth/Legacy/LegacyDnoaTokenReader.cs` does this without DNOA, and `LegacyTokenHarness.cs`
  (in this folder) proves it against tokens minted by the real DNOA 4.3.4 DLLs from `bin/`.
* **Plan: upgrade on use.** When a legacy refresh token shows up, validate it the way DNOA did, then hand
  back a *new-format* refresh token. Clients that store the returned token migrate on their own. Clients
  that don't keep working through the legacy path. Log legacy hits; retire the path whenever the count is ~0
  (or never; it's about 300 lines with no dependencies).
* **Client side is easy.** `IAuthorizationState` JSON is plain property names (prefs are serialized without
  `TypeNameHandling`), so a look-alike class deserializes old JSON as-is. Verified both directions, including the
  prefs `object -> dynamic -> T` round trip. Rolling back stays safe because new JSON loads into DNOA's class too.

## What DNOA actually does today (verified against the 4.3.3 source and the 4.3.4 binaries)

| Artifact | Format | Key | Lifetime |
|---|---|---|---|
| Refresh token | `handle!b64url([len][HMAC-SHA256][len][0x01][IV][AES-CBC(querystring)])` | `cryptokeys` (symmetric, 256-bit), bucket `.../oauth_refresh_token` | Forever, until the authorization row is gone. **A new refresh token is issued on every refresh, and the old ones stay valid.** |
| Access token | `b64url([len][RSA-SHA1 sig][len][len][RSA-PKCS1(AESkey‖IV)][AES-CBC(querystring)])` | RSA key in machine CSP container `MyFlightbookContainer` (sign + encrypt with the same pair) | 14 days (`Lifetime=1209600`) |
| Auth code | symmetric, bucket `.../oauth_authorization_code`, one-time via `nonce` table | `cryptokeys` | minutes |

Inner querystring fields: `ClientIdentifier`, `User`, `Scope` (space-delimited), `ts` (unix seconds),
`t` (`DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken` or `DotNetOpenAuth.OAuth2.AccessToken`), `Lifetime` (access only).

Validation DNOA did on refresh, which the new code must replicate:
1. Authenticate the client (HTTP Basic **or** `client_id`/`client_secret` in the form).
2. The token's `ClientIdentifier` equals the authenticated client.
3. `IsAuthorizationValid(scope, client, ts, user)`: an unexpired `oauthclientauthorization` row for (client, user)
   with `CreatedOnUtc < ts + 1s`, and requested scopes ⊆ granted scopes.

The resource server checked **only** signature + expiry on access tokens, not authorization validity.
So revoking an app today doesn't cut it off until its access token expires (up to 14 days).

### Problems found along the way

1. **`GrantedAuthsForUser` hard-coded `userID = 'ericbe'`** (the `?uname` parameter was unused). Every user's
   "Authorized apps" pref page listed *your* authorized apps. **Fixed on this branch** (one line).
2. **PKCE-proxy tokens can't be revoked.** Tokens issued through the proxy carry `ClientIdentifier = publicClientProxy`,
   and `IsAuthorizationValid` returns `true` for the proxy without checking anything. If a user revokes a public
   client, its refresh token keeps working. The new server fixes this for legacy tokens too: a proxy token is only
   accepted from a *public* client that has a live authorization for that user (see the refresh algorithm below).
3. `PkceTempRecord.ValidateGrant` does read-check-then-write. Two concurrent redemptions of the same code can
   both succeed. The new code uses `UPDATE ... SET Used=1 WHERE Used=0` and checks rows affected.
4. `Authorize` vs. `IsCallbackAllowed` disagree: the former requires host+path+query match, the latter
   only scheme+authority. The new code does exact-string match against registered callbacks (OAuth 2.1), keeping
   the `DebugDomains` exception.

## Server: in-house, built to the current MCP authorization spec

Decision (2026-11-05): in-house server, no library. The core is **built and tested** (see "Status" below).

### Why in-house still holds up with MCP in the picture
* What the MCP spec (revision 2026-07-28) asks of an authorization server is a short list of small RFCs. Each one is
  a few dozen lines here. OpenIddict would add OWIN + DI + its store model on top of the same list.
* The core (`MyFlightbook.OAuth.Server`) has **no System.Web/MVC dependency**: input is `NameValueCollection` + headers,
  output is `OAuthHttpResult`. The MVC actions are thin adapters, so the move to ASP.NET Core later is a re-wire, not
  a rewrite. `MySqlOAuthServerStore` is the only MFB-specific part.
* Revisit OpenIddict only if you need OIDC (ID tokens/userinfo) or JWT access tokens validated by a third party.

### What MCP (2026-07-28) requires, and where it lives
| Requirement | Implementation |
|---|---|
| RFC 8414 metadata, incl. `code_challenge_methods_supported` (MCP clients refuse to proceed without it) | `AuthorizationServerMetadata()`; advertises S256 only |
| PKCE S256 | Required for public clients; `plain` accepted only for pre-registered clients flagged `AllowPlainPkce` (today's code accepts it) |
| RFC 8707 `resource` → audience-bound tokens; MCP server MUST reject tokens not issued for it | `resource` is validated against `Options.Resources`, stored on code + tokens. `ValidateAccessToken(token, resource)` only accepts matching audience. API tokens use `Resource = NULL`, same as every legacy token |
| No token passthrough (MCP server can't forward the client's token to the API) | RFC 8693 token exchange: the sidecar trades the MCP-bound token for a 5-minute API-bound one. Only clients listed in `TokenExchangeClients` may do this |
| Client ID Metadata Documents (SHOULD; Dynamic Client Registration is deprecated) | `MetadataDocumentClientResolver`: https-only, path required, no query/fragment/port, DNS-name hosts only, rejects private/link-local/metadata IPs (SSRF), no redirects, 32 KB cap, 5 s timeout, honors Cache-Control (5 min–24 h), `client_id` must match the URL, redirect URIs exact-match. Public clients only (`token_endpoint_auth_method: none`) |
| RFC 9207 `iss` in authorization responses | Added to success and error redirects |
| OAuth 2.1 refresh rotation for public clients | Public + CIMD clients rotate. Reuse within 60 s is allowed (lost response); later reuse revokes the whole family |
| RFC 9728 protected-resource metadata + `WWW-Authenticate: Bearer resource_metadata=...` | Served by the **MCP sidecar**; `OAuthServer.BearerChallenge()` builds the header |
| Introspection (RFC 7662) for the sidecar | `HandleIntrospectionRequest`, restricted to `IntrospectionClientIds` |

**Write-access policy for AI clients:** metadata-document clients get only `MetadataDocumentOptions.AllowedScopes`,
intended as read-only (`currency totals readflight ...`). Asking for `addflight` gets `invalid_scope`. You can also
restrict hosts with `IsHostAllowed`. The consent page should show the client_id's **host** next to `client_name`,
because the name is self-asserted.

### Tokens
Opaque, 32 random bytes, base64url, prefixed `mfb.at.` / `mfb.rt.` (`.` isn't in the base64url alphabet, so no
legacy token can collide). Stored **only as SHA-256 hashes**. Access tokens: 14 days for pre-registered clients
(unchanged), 1 hour for metadata-document clients. Confidential clients' refresh tokens don't rotate (unchanged
behavior). Schema: `OAuthServer-Schema.sql`.

### Behaviors worth knowing (all covered by tests)
* Codes are claimed atomically (`UPDATE ... WHERE Used=0`). A **replayed code revokes every token issued from it**.
* PKCE downgrade protection: a `code_verifier` for a code issued without a challenge is rejected.
* `redirect_uri` at the token endpoint must exactly match. It may be omitted only when PKCE was used (OAuth 2.1).
* Pre-registered clients keep today's lenient redirect comparison (host+port+path+query, case-insensitive) at /Authorize.
  Metadata-document clients get exact match. Loopback `http://127.0.0.1` may use any port (RFC 8252).
* Errors at /Authorize: bad `client_id`/`redirect_uri` are shown, never redirected. Everything else is redirected with `error`, `state`, `iss`.
* Client auth: `client_secret_basic` (URL-decoding tolerated, as DNOA did) or `client_secret_post`. Public clients must not send a secret.
* Legacy refresh tokens: accepted, logged via `OnLegacyTokenUsed`, upgraded to `mfb.rt.` (the old token keeps working).
  Proxy-minted tokens are accepted only from a *public* client **with a live authorization**.
  A re-authorization after revocation doesn't resurrect older legacy tokens (same rule as DNOA).
* Revoking a refresh token (RFC 7009) revokes its family. `MySqlOAuthServerStore.RevokeTokensForUserAndClient` should be
  called from `MFBOauthClientAuth.RevokeAuthorization`, so the user's "revoke app" kills tokens immediately.
* `client_credentials` is dropped (those tokens had no user and `OAuthServiceCall` rejected them anyway).

### Status
* **Project layout:** `MyFlightbook.OAuth` (netstandard2.0; depends on MyFlightbook.Data + MyFlightbook.SharedUtility; **no DNOA**)
  holds `Legacy/LegacyDnoaTokenReader.cs` and `Server/{OAuthServer,OAuthServerModels,OAuthClientRegistry,MySqlOAuthServerStore}.cs`.
  `MyFlightbook.OAuth.Tests` (net8.0, MSTest) holds the tests. Legacy tests use genuine DNOA tokens with fixed test keys
  (`LegacyFixtures.cs`, generated by `GenerateLegacyFixtures.cs` in this folder), since DNOA can't load on net8.
* **Done, tested (Mono, 11 test methods / ~140 assertions):** core + legacy reader. Mutation-checked: disabling the proxy fix, audience
  check, PKCE check, or reuse detection each makes tests fail. The metadata-document fetch through SharedHttpClient was checked
  against a local HTTP server: 302 not followed, size cap holds (incl. chunked), timeout fires.
* **Compiled against the real DBHelper/SharedHttpClient, not yet run against MySQL:** `MySqlOAuthServerStore.cs`.
* **Not done yet (next):**
  1. Wiring: build `OAuthServer` once (options from config: issuer, endpoints, resources, scopes, the sidecar client), map
     `MFBOauth2Client` → `OAuthClient` (`IsPublic`, `AllowPlainPkce` = public, `UseLegacyRedirectComparison = true`),
     and swap the bodies of `Authorize`/`CommitAuth`/`OAuthToken` (+ new `Revoke`, `Introspect`, metadata). The consent view
     re-posts the original query string so `CommitAuth` re-validates it. The `AuthorizePKCE` endpoint and the proxy go away.
  2. `OAuthServiceCall.init`: replace `ResourceServer`/`StandardAccessTokenAnalyzer` with `ValidateAccessToken`
     (+ `LegacyAccessTokenReader` for 14 days). Find the token the way DNOA did: Bearer header, then form, then query.
  3. Metadata URL: if the issuer is `https://myflightbook.com`, `/.well-known/oauth-authorization-server` must be served
     at the **site root**, not under `/logbook`. That's an IIS rewrite rule from the root site to the MVC action.
     MCP clients also try `/.well-known/openid-configuration`; serving the same JSON there is harmless.
  4. `GrantedAuthsForUser`: `LEFT JOIN oauthmetadataclients` so CIMD clients (Claude) appear and can be revoked.
  5. Nightly: `MySqlOAuthServerStore.PurgeExpired(TimeSpan.FromDays(1))`.

## Client side (`OAuthClientBase` and friends) - DONE (2026-11-07), pending provider testing

* `MyFlightbook.OAuth/Client/AuthorizationState.cs`: `IAuthorizationState`, `AuthorizationState` (same JSON shape as DNOA's, tested both
  directions against DNOA-serialized JSON), `OAuthProtocolException` (replaces `ProtocolException`; carries `Error` and the raw `ResponseBody`,
  which `ThrowGDriveError`/`HandleInvalidAuth` parse as they used to parse DNOA's inner WebException).
* `MyFlightbook.OAuth/Client/OAuthClientProtocol.cs`: authorization URL, state cookie, code exchange, refresh, and response handling.
  **Verified against DNOA's own `WebServerClient` on the wire** (local token endpoint, Mono): refresh and code-exchange requests are
  byte-identical (headers, Content-Type, body), and the resulting state is identical.
  - Refresh: preemptive `Basic base64(id:secret)` (not URL-encoded), credentials not in the body (DNOA's default).
  - Code exchange: `client_id`/`client_secret` in the body (`ConvertToken` used DNOA's PostParameter applicator).
  - Response: refresh token kept unless a new one is sent; scope adopted only if the state had none; no `expires_in` means no expiration.
  - Any failure, including network errors, throws `OAuthProtocolException`, like DNOA's `ProtocolException`.
* `OAuthClientBase`: `Authorize` sets cookie `mfb-oauth-client-state` (state + the exact callback) and ends the response, as DNOA did.
  `ConvertToken(HttpRequestBase)` checks the state, clears the cookie, then exchanges the code using the stored callback as `redirect_uri`.
  It returns null if the request isn't an authorization response, and a token-less `IsDeleted` state if the user declined (DNOA's contract).
  A user caught mid-authorization during the deploy gets "doesn't match this browser session" once (different cookie name).
* Also fixed: `RefreshAccessToken(refreshToken, ...)` (hand-rolled path) no longer drops the refresh token when the server doesn't send a new one.
* Still on DNOA's types, deliberately, until the server cutover: `ClientType` (aliased in OAuthClientBase.cs; used by `OAuth2AdHocClient`,
  Playpen, developer.cshtml, and `MFBOauth2Client`).

### Testing checklist (dev, then prod right after deploy)
For each provider: connect (fresh authorization), use it, then **force a refresh** by editing the stored token's `AccessTokenExpirationUtc`
into the past and use it again. Confirm the refresh token survives in the stored JSON. Also try declining at the provider's consent screen.
| Provider | Connect flow | Refresh path |
|---|---|---|
| Dropbox, OneDrive, Box, Leon, CloudAhoy, FlySto | base `Authorize` + base `ConvertToken` (new state check) | base `RefreshAccessToken()` |
| Google Drive, Google Photos | base `Authorize` + Google's own `ConvertToken` override | base `RefreshAccessToken()` |
| TachTime, MyTailLog, FlightCrewView, RosterBuster | their own flows (PKCE / hand-rolled) | their own (hand-rolled) |
Nightly cloud backups exercise the Dropbox/Google Drive/OneDrive/Box refresh paths for real users, so watch the first few nightly runs after deploy.

## Server cutover - DONE in code (2026-11-10), pending dev testing

* DNOA packages and the `dotNetOpenAuth` config section are gone. `MyFlightbook.OAuth.ClientType` replaces DNOA's (same 0/1 values).
* `OAuth2AuthorizationServer.cs`: `MFBOauth2Client` (no `IClientDescription`; `ToOAuthClient()` maps it), `MFBOauthClientAuth`
  (revoking now also deletes the app's new-format tokens), and a static `OAuth2AuthorizationServer.ServerForRequest(request)` factory:
  issuer = scheme+host of the request, the API resource = `~/mvc/oAuth/OAuthResource`, scopes = `MFBOAuthScope`, legacy access tokens
  via the `MyFlightbookContainer` RSA key, and legacy-token use logged through `EventRecorder.LogCall`.
  Metadata-document (MCP) clients are OFF: the registry has no resolver, and `SupportsMetadataDocumentClients` is false.
* `oAuthControllerServer`: `Authorize` (and `AuthorizePKCE`, now just an alias, for existing public clients) validates and shows consent.
  The consent form re-posts the original query string (`authRequest`), and `CommitAuth` re-validates it before approving or denying.
  New: `OAuthToken` (JSON errors per RFC 6749), `Revoke` (RFC 7009), `Metadata` (RFC 8414). Removed: the PKCE proxy, `PkceTempRecord`, `SyntheticTokenRequest`.
* `OAuthServiceCall`: bearer token from the header, then the form, then the query (as DNOA did), checked by `ValidateAccessToken`. Failures are now **401** with
  `WWW-Authenticate: Bearer error="invalid_token"`, where DNOA failures used to surface as 500s.
* Nightly run purges expired access tokens, rotated refresh tokens, and stale codes.

### Testing on dev
1. Playpen OAuth client test, **confidential**: authorize, consent, token, call `currency`/`totals`, refresh, and decline once.
2. Same with **PKCE (public)**: note that the refresh token rotates for public clients.
3. User's "Authorized apps" page: revoke, then confirm the access token AND the refresh token stop working immediately.
4. **Legacy rehearsal:** check out the commit before the DNOA removal, mint tokens with the Playpen page (one confidential, one public), save them,
   switch back, then: refresh with each (you should get `mfb.rt.` back, the old token keeps working, and the log shows "Legacy ... token used");
   call the API with the old access token; revoke the app and confirm the old refresh token is rejected.
5. Partners' error parsing: errors from `OAuthToken` are JSON now (`{"error":"invalid_grant",...}`) instead of plain text.

### Follow-ups
* **+14 days after deploy:** delete the "Legacy (DotNetOpenAuth) access tokens" region in `OAuth2AuthorizationServer.cs` and the
  `LegacyAccessTokenReader` line. That retires the RSA key container.
* **Whenever:** the `publicClientProxy` row in `allowedoauthclients` is no longer used (proxy-minted refresh tokens are accepted from the real public client).
  `pkceauthorization` and `nonce` can be dropped. **Never** delete `cryptokeys` rows in bucket `https://localhost/dnoa/oauth_refresh_token`.
* **For MCP:** serve `/.well-known/oauth-authorization-server` at the site ROOT (IIS URL Rewrite on the root site to `/logbook/mvc/oAuth/Metadata`),
  turn on the metadata-document resolver, set `SupportsMetadataDocumentClients`, add the MCP resource, and `LEFT JOIN oauthmetadataclients` in `GrantedAuthsForUser`.

## Suggested sequence

1. **Client swap** (OAuthClientBase, CloudStorage, the partner clients, Profile, controllers, Playpen, developer.cshtml).
   Test each provider's connect + refresh on dev. Deploy alone.
2. **Server, behind the same URLs**: new `/mvc/oAuth/Authorize` + `CommitAuth` + `OAuthToken` + resource validation;
   schema changes; legacy paths live. Before deploying, pull one real refresh token (yours) from a test client and run it
   through `TryReadRefreshToken` against prod `cryptokeys` (a tiny admin-only page is fine). That's the one thing the
   harness can't prove for you.
3. **+14 days**: remove legacy access-token path and RSA container code.
4. **Remove DNOA** packages/config. Leave `cryptokeys` (bucket `.../oauth_refresh_token`) **read-only, forever-ish**.
   Never delete those rows. `nonce` and the `.../oauth_authorization_code` bucket can go.
5. **Later**: decide on rotation; add RFC 8414 metadata / RFC 7009 revocation for the MCP connector.

## Running the tests

`MyFlightbook.OAuth.Tests` is a normal MSTest project (Test Explorer / `dotnet test`). It doesn't reference DNOA;
`GenerateLegacyFixtures.cs` (here) is the only thing that does, and you only need it to regenerate fixtures.

## Running the harness

`LegacyTokenHarness.cs` mints tokens with the real DNOA DLLs, decodes them with the new reader, and checks
tamper/wrong-key rejection and `AuthorizationState` JSON round-trips. On Windows: make a net48 console project
referencing `bin\DotNetOpenAuth.*.dll` + Newtonsoft.Json, and add the harness, `LegacyDnoaTokenReader.cs`, and `AuthorizationState.cs`.
(It was run under Mono: `mcs -r:System.Web.dll -r:DotNetOpenAuth.Core.dll ... && mono h.exe` → 27/27 pass.)
Note: the harness pre-seeds a crypto key so DNOA doesn't try to log via log4net, which isn't present.
