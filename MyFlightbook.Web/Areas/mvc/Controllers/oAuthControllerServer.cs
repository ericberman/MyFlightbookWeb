using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;
using MyFlightbook.Image;
using MyFlightbook.OAuth;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Intermediate class which implements the server functionality for the site; 3rd-party oAuth clients and endpoints are in derived subclasses of this, but all end up being served under the same /mvc/oAuth endpoint
    /// </summary>
    public class oAuthControllerServer : AdminControllerBase
    {
        #region oAuth server functions
        [Authorize]
        [HttpGet]
        public ActionResult Authorize()
        {
            ViewBag.error = string.Empty;
            try
            {
                if (!Request.IsSecureConnection)
                    throw new HttpException((int)HttpStatusCode.Forbidden, Resources.LocalizedText.oAuthErrNotSecure);

                AuthorizationServer authorizationServer = new AuthorizationServer(new OAuth2AuthorizationServer());
                EndUserAuthorizationRequest pendingRequest;

                if ((pendingRequest = authorizationServer.ReadAuthorizationRequest()) == null)
                    throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrMissingRequest);

                MFBOauth2Client client = (MFBOauth2Client)authorizationServer.AuthorizationServerServices.GetClient(pendingRequest.ClientIdentifier);

                bool fIsValidCallback = false;
                foreach (string callback in client.Callbacks)
                {
                    if (Uri.Compare(pendingRequest.Callback, new Uri(callback), UriComponents.HostAndPort | UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        fIsValidCallback = true;
                        break;
                    }
                }
                if (!fIsValidCallback)
                    throw new HttpException((int)HttpStatusCode.BadRequest, String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.oAuthErrBadRedirectURL, pendingRequest.Callback.ToString()));

                HashSet<string> allowedScopes = OAuthUtilities.SplitScopes(client.Scope);

                if (!pendingRequest.Scope.IsSubsetOf(allowedScopes))
                    throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrUnauthorizedScopes);

                IEnumerable<MFBOAuthScope> requestedScopes = MFBOauthServer.ScopesFromStrings(pendingRequest.Scope);

                // In Authorize, after ValidateClient:
                if (client.ClientType == ClientType.Public)
                    throw new HttpException((int)HttpStatusCode.BadRequest,
                        "Public clients must use the PKCE flow");

                // See if there are any scopes that are requested that are not allowed.

                ViewBag.scopesList = MFBOauthServer.ScopeDescriptions(requestedScopes);
                ViewBag.clientName = client.ClientName;
                ViewBag.clientLogoHref = (client.Logo?.Length ?? 0) == 0 ? null : $"~/mvc/oAuth/ViewOAuthLogo?clientID={WebUtility.UrlEncode(client.ClientIdentifier)}".ToAbsolute();
            }
            catch (Exception ex) when (ex is HttpException || ex is ProtocolException || ex is ProtocolFaultResponseException || ex is MyFlightbookException)
            {
                ViewBag.error = ex.Message;
            }

            return View("authorize");
        }

        [Authorize]
        [HttpGet]
        public ActionResult AuthorizePKCE()
        {
            ViewBag.error = string.Empty;
            try
            {
                if (!Request.IsSecureConnection)
                    throw new HttpException((int)HttpStatusCode.Forbidden, Resources.LocalizedText.oAuthErrNotSecure);

                string clientId = Request["client_id"];
                string redirectUri = Request["redirect_uri"];
                string codeChallenge = Request["code_challenge"];
                string method = Request["code_challenge_method"] ?? "plain";

                // Validate the REAL client (not the proxy)
                MFBOauth2Client realClient = MFBOauth2Client.GetClientByID(clientId)?.FirstOrDefault()
                    ?? throw new HttpException((int)HttpStatusCode.BadRequest, "Unknown client");

                // In AuthorizePKCE, after loading realClient:
                if (realClient.ClientType != ClientType.Public)
                    throw new HttpException((int)HttpStatusCode.BadRequest,
                        "PKCE flow is only available for public clients");

                // Validate redirect URI against real client
                bool fIsValidCallback = false;
                foreach (string callback in realClient.Callbacks)
                {
                    if (Uri.Compare(new Uri(redirectUri), new Uri(callback),
                        UriComponents.HostAndPort | UriComponents.PathAndQuery,
                        UriFormat.SafeUnescaped, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        fIsValidCallback = true;
                        break;
                    }
                }
                if (!fIsValidCallback)
                    throw new HttpException((int)HttpStatusCode.BadRequest,
                        String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.oAuthErrBadRedirectURL, redirectUri));

                // Parse and validate scopes against the real client
                string scopeParam = Request["scope"] ?? string.Empty;
                HashSet<string> requestedScopes = OAuthUtilities.SplitScopes(scopeParam);
                HashSet<string> allowedScopes = OAuthUtilities.SplitScopes(realClient.Scope);
                if (!requestedScopes.IsSubsetOf(allowedScopes))
                    throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrUnauthorizedScopes);

                // Store PKCE state - keyed by our state value, NOT the one from the client
                string state = Guid.NewGuid().ToString("N");
                new PkceTempRecord
                {
                    State = state,
                    ClientId = clientId,
                    RedirectEndpoint = redirectUri,
                    CodeChallenge = codeChallenge,
                    CodeChallengeMethod = method,
                    Scope = scopeParam,    // store the requested scope too - see CommitAuth below
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
                }.Commit();

                // Show the consent page using the REAL client's identity
                ViewBag.scopesList = MFBOauthServer.ScopeDescriptions(MFBOauthServer.ScopesFromStrings(requestedScopes));
                ViewBag.clientName = realClient.ClientName;
                ViewBag.pkceState = state;  // pass our state to the view so CommitAuth can find the record

                MFBOauth2Client proxy = OAuth2AdHocClient.ConfidentialProxyClient;
                ViewBag.proxyClientId = proxy.ClientIdentifier;
                ViewBag.proxyRedirectUri = proxy.Callbacks.First();
                ViewBag.proxyScope = scopeParam;  // scope stays the same
                ViewBag.pkceState = state;
            }
            catch (Exception ex) when (ex is HttpException || ex is MyFlightbookException)
            {
                ViewBag.error = ex.Message;
            }

            return View("authorize");  // same view as normal Authorize
        }

        private static void RejectWithError(AuthorizationServer authorizationServer, EndUserAuthorizationRequest pendingRequest, string szError)
        {
            EndUserAuthorizationFailedResponse resp = authorizationServer.PrepareRejectAuthorizationRequest(pendingRequest);
            resp.Error = szError;
            OutgoingWebResponse wr = authorizationServer.Channel.PrepareResponse(resp);
            wr.Send();
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult CommitAuth()
        {
            AuthorizationServer authorizationServer = new AuthorizationServer(new OAuth2AuthorizationServer());
            EndUserAuthorizationRequest pendingRequest = authorizationServer.ReadAuthorizationRequest();

            string authAction = Request["authAction"];
            if (authAction == "authorize")
            {
                if (pendingRequest == null)
                    throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrMissingRequest);

                PkceTempRecord pkce = PkceTempRecord.LoadFromState(Request["pkcestate"] ?? string.Empty);

                // For PKCE, record auth against the REAL client (pkce.ClientId).
                // For normal flow, record auth against the requesting client.
                // Either way the proxy client is pre-authorized via IsAuthorizationValid short-circuit.
                string clientIdToRecord = pkce != null ? pkce.ClientId : pendingRequest.ClientIdentifier;

                MFBOauthClientAuth ca = new MFBOauthClientAuth { Scope = OAuthUtilities.JoinScopes(pendingRequest.Scope), ClientId = clientIdToRecord, UserId = User.Identity.Name, ExpirationDateUtc = DateTime.UtcNow.AddYears(10) };

                if (!ca.fCommit())
                {
                    RejectWithError(authorizationServer, pendingRequest, Resources.LocalizedText.oAuthErrCreationFailed);
                    return new EmptyResult();
                }

                EndUserAuthorizationSuccessResponseBase resp = authorizationServer.PrepareApproveAuthorizationRequest(pendingRequest, User.Identity.Name);
                OutgoingWebResponse wr = authorizationServer.Channel.PrepareResponse(resp);
                string location = wr.Headers["Location"];

                if (pkce != null)
                {
                    var uri = new Uri(location);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string legacyCode = query["code"];
                    string publicCode = Guid.NewGuid().ToString("N");

                    new PkceTempRecord
                    {
                        State = Request["state"],
                        PublicCode = publicCode,
                        LegacyCode = legacyCode,
                        ClientId = pkce.ClientId,
                        RedirectEndpoint = pkce.RedirectEndpoint,
                        CodeChallenge = pkce.CodeChallenge,
                        CodeChallengeMethod = pkce.CodeChallengeMethod,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
                    }.Commit();

                    query["code"] = publicCode;
                    var builder = new UriBuilder(pkce.RedirectEndpoint)
                    {
                        Query = query.ToString()
                    };
                    wr.Headers["Location"] = builder.Uri.ToString();
                }

                wr.Send();
            }
            else if (authAction == "reject")
            {
                RejectWithError(authorizationServer, pendingRequest, Resources.LocalizedText.oAuthErrNotAuthorized);
            }
            return new EmptyResult();
        }

        public ActionResult OAuthResource(string id = null)
        {
            OAuthServiceCall.ProcessRequest(Request, Response, id);
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult OAuthToken()
        {
            try
            {
                // Check to see if this is a PKCE token request first - if so, validate the code verifier and swap the legacy code for the public code before processing the request.
                string clientId = Request["client_id"];
                if (String.IsNullOrEmpty(clientId))
                    throw new ArgumentNullException(nameof(clientId));
                if (!Request.IsSecureConnection)
                    throw new HttpException((int)HttpStatusCode.Forbidden, "Token requests MUST be on a secure channel");


                MFBOauth2Client client = MFBOauth2Client.GetClientByID(clientId)?.FirstOrDefault() ?? throw new InvalidOperationException("Unknown client");

                AuthorizationServer authorizationServer = new AuthorizationServer(new OAuth2AuthorizationServer());

                HttpRequestBase requestBase = null;

                if (client.ClientType == ClientType.Public)
                {
                    string publicCode = Request["code"];
                    if (String.IsNullOrEmpty(publicCode))
                        throw new ArgumentNullException(nameof(publicCode));
                    string verifier = Request["code_verifier"];
                    if (String.IsNullOrEmpty(verifier))
                        throw new ArgumentNullException(nameof(verifier));

                    var pkce = PkceTempRecord.LoadFromPublicCode(publicCode) ?? throw new InvalidOperationException("invalid grant");
                    pkce.ValidateGrant(verifier, clientId);

                    // Look up the clientID
                    MFBOauth2Client confidentialProxyClient = OAuth2AdHocClient.ConfidentialProxyClient;

                    OAuth2AdHocClient proxyClient = new OAuth2AdHocClient(confidentialProxyClient.ClientIdentifier, confidentialProxyClient.ClientSecret, string.Empty, Request.Url.GetLeftPart(UriPartial.Path), confidentialProxyClient.Callbacks.First());

                    // Fake the request environment DNOA needs
                    // by using DNOA's AuthorizationServer directly
                    // against a NameValueCollection instead of HttpRequest
                    requestBase = new Injection.SyntheticTokenRequest(
                        new Uri($"https://{Request.Url.Host}/logbook/mvc/oAuth/OAuthToken"),
                        new NameValueCollection
                        {
                        { "grant_type", "authorization_code" },
                        { "client_id", proxyClient.clientID },
                        { "client_secret", proxyClient.clientSecret },
                        { "code", pkce.LegacyCode },
                        { "redirect_uri", confidentialProxyClient.Callbacks.First() }
                        });
                }
                OutgoingWebResponse wr = authorizationServer.HandleTokenRequest(requestBase);
                // At this point, wr.Body should contain the JSON response
                return Content(wr.Body, "application/json; charset=utf-8");
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.TrySkipIisCustomErrors = true;
                return Content(e.Message);
            }
        }
        #endregion

        #region Developer Page
        [HttpPost]
        [Authorize]
        public ActionResult SetOAuthLogo(string clientID)
        {
            return SafeOp(() =>
            {
                MFBOauth2Client client = MFBOauth2Client.GetClientByID(clientID)?.FirstOrDefault();
                if (User.Identity.Name.CompareCurrentCultureIgnoreCase(client?.OwningUser ?? string.Empty) != 0)
                    throw new UnauthorizedAccessException("Not authorized to update this client");

                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                Stream s = Request.Files[0].InputStream;
                byte[] rgb = s == null ? Array.Empty<byte>() : MFBImageInfo.ScaledPNGImage(s, 150, 150);
                if (rgb != null && rgb.Length > 0)
                {
                    if (rgb.Length > 256000)
                        throw new InvalidOperationException("Image too large");
                    client.Logo = rgb;
                    client.Commit();
                }
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteOAuthLogo(string clientID)
        {
            return SafeOp(() =>
            {
                MFBOauth2Client client = MFBOauth2Client.GetClientByID(clientID)?.FirstOrDefault();
                if (User.Identity.Name.CompareCurrentCultureIgnoreCase(client?.OwningUser ?? string.Empty) != 0)
                    throw new UnauthorizedAccessException("Not authorized to update this client");
                client.Logo = null;
                client.Commit();
                return new EmptyResult();
            });
        }

        [Authorize]
        public ActionResult ViewOAuthLogo(string clientID)
        {
            MFBOauth2Client client = MFBOauth2Client.GetClientByID(clientID)?.FirstOrDefault();
            return (client?.Logo != null && client.Logo.Length > 0) ? (ActionResult)File(client.Logo, "image/png") : Redirect("~/images/1x1.png");
        }

        [HttpPost]
        [Authorize]
        public ActionResult UpdateOauthClient(string clientID, string clientSecret, string clientName, string clientCallBack, string clientScopes, string szOwner, bool isPublic = false)
        {
            return SafeOp(() =>
            {
                MFBOauth2Client originalClient = MFBOauth2Client.GetClientByID(clientID)?.FirstOrDefault(); // Pick up the pre-existing logo.
                MFBOauth2Client client = new MFBOauth2Client(clientID, clientSecret, clientCallBack, clientName, (clientScopes ?? string.Empty).Replace(",", " "), szOwner, isPublic ? ClientType.Public : ClientType.Confidential, originalClient?.Logo);
                client.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteOAuthClient(string clientID)
        {
            return SafeOp(() =>
            {
                MFBOauth2Client.DeleteForUser(clientID, User.Identity.Name);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string clientID, string clientSecret, string clientName, string clientCallBack, string clientOwner)
        {
            MFBOauth2Client client = new MFBOauth2Client(clientID, clientSecret, clientCallBack, clientName, (Request["clientScopes"] ?? string.Empty).Replace(",", " "), clientOwner, Request["isPublic"] == null ? ClientType.Confidential : ClientType.Public);
            try
            {
                client.Commit();
                util.NotifyAdminEvent("oAuth client created", String.Format(CultureInfo.CurrentCulture, "User: {0}, Name: {1}", User.Identity.Name, client.ClientName), ProfileRoles.maskCanReport);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is MyFlightbookValidationException || ex is ArgumentOutOfRangeException)
            {
                ViewBag.client = client;
                ViewBag.error = ex.Message;
            }
            return View("developer");
        }

        // GET: mvc/oAuth - return the developer page
        [HttpGet]
        public ActionResult Index()
        {
            return View("developer");
        }
        #endregion

        #region User management of Client apps
        [Authorize]
        [HttpPost]
        public ActionResult DeAuthClient(string idClient)
        {
            return SafeOp(() =>
            {
                MFBOauthClientAuth.RevokeAuthorization(User.Identity.Name, idClient);
                return new EmptyResult();
            });
        }
        #endregion
    }
}