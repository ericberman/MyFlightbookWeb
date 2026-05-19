using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;
using MyFlightbook.CloudStorage;
using MyFlightbook.OAuth;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.OAuth.FlightCrewView;
using MyFlightbook.OAuth.Leon;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
    public class oAuthController : AdminControllerBase
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
                    requestBase = new SyntheticTokenRequest(
                        new Uri("https://developer.myflightbook.com/logbook/mvc/oAuth/OAuthToken"),
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

        #region FlightCrewView

        [Authorize]
        public async Task<ActionResult> FlightCrewViewRedir()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            IAuthorizationState auth = await new FlightCrewViewClient().ConvertToken(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, "~/mvc/oauth/flightcrewviewredir".ToAbsolute()), Request["code"]);
            pf.SetPreferenceForKey(FlightCrewViewClient.AccessTokenPrefKey, auth);
            return Redirect("~/mvc/oauth/ManageFlightCrewView");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageFlightCrewView(string action)
        {
            if (action.CompareOrdinal("deAuth") == 0)
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pf.PreferenceExists(FlightCrewViewClient.AccessTokenPrefKey))
                {
                    try
                    {
                        var _ = await new FlightCrewViewClient(pf.GetPreferenceForKey<AuthorizationState>(FlightCrewViewClient.AccessTokenPrefKey)).RevokeTokeBasicAuth();
                    }
                    catch (HttpRequestException) { }
                    pf.SetPreferenceForKey(FlightCrewViewClient.AccessTokenPrefKey, null, true);
                }
                return Redirect("ManageFlightCrewView");
            }
            else if (action.CompareOrdinal("auth") == 0)
            {
                new FlightCrewViewClient().Authorize("~/mvc/oauth/flightcrewviewredir".ToAbsoluteURL(Request));
                return new EmptyResult();
            }
            throw new InvalidOperationException("Unknown action: " + action);
        }

        [Authorize]
        public ActionResult ManageFlightCrewView()
        {
            return View("flightcrewviewmanage");
        }
        #endregion

        #region Leon
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageLeon(string action)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (action.CompareOrdinal("deAuth") == 0)
            {
                pf.SetPreferenceForKey(LeonClient.TokenPrefKey, null, true);
                pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, null, true);
                return Redirect(Request.Path);
            }
            else if (action.CompareOrdinal("auth") == 0)
            {
                string subDomain = Request["leonSubDomain"];
                if (String.IsNullOrEmpty(subDomain))
                {
                    ViewBag.error = Resources.LogbookEntry.LeonSubDomainRequired;
                    return View("leonmanage");
                }

                pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, subDomain, String.IsNullOrEmpty(subDomain));
                new LeonClient(subDomain, LeonClient.UseSandbox(Request.Url.Host)).Authorize("~/mvc/oauth/ManageLeon".ToAbsoluteURL(Request));
                return new EmptyResult();
            }
            throw new InvalidOperationException("Unknown action: " + action);
        }

        [Authorize]
        public ActionResult ManageLeon()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            string szSubDomain = pf.GetPreferenceForKey<string>(LeonClient.SubDomainPrefKey);
            string code = Request["code"];
            if (!String.IsNullOrEmpty(code) && !String.IsNullOrWhiteSpace(szSubDomain))
            {
                IAuthorizationState AuthState = new LeonClient(szSubDomain, LeonClient.UseSandbox(Request.Url.Host)).ConvertToken(Request);
                pf.SetPreferenceForKey(LeonClient.TokenPrefKey, AuthState, AuthState == null);
            }
            return View("leonmanage");
        }
        #endregion

        #region Google Photos
        [Authorize]
        public ActionResult GooglePhotoRedir()
        {
            string szErr = GetStringParam("error");
            if (String.IsNullOrEmpty(szErr))
            {
                IAuthorizationState token = new GooglePhoto().ConvertToken(Request);

                // Remove old token if we successfully converted the new one.
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pf.PreferenceExists(GooglePhoto.ObsoletePrefKeyAuthToken))
                    pf.SetPreferenceForKey(GooglePhoto.ObsoletePrefKeyAuthToken, null, true);

                pf.SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, token);
            }
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["pane"] = "social";
            if (!String.IsNullOrEmpty(szErr))
                nvc["oauthErr"] = szErr;
            return Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/prefs?{0}", nvc.ToString()));
        }

        [Authorize]
        public ActionResult AuthorizeGPhotoNew()
        {
            new GooglePhoto().Authorize(String.Format(CultureInfo.InvariantCulture, "~/mvc/oAuth/GooglePhotoRedir?{0}=1", GooglePhoto.szParamGPhotoAuth).ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeGPhotoNew()
        {
            MyFlightbook.Profile.GetUser(User.Identity.Name).SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, null, true);
            return Redirect("~/mvc/prefs?pane=social");
        }

        [Authorize]
        public async Task<ActionResult> GooglePhotoPickerSession()
        {
            return await SafeOp(async () =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                GooglePhoto gpn = new GooglePhoto(pf);
                return Json(await gpn.GetSession());
            });
        }
        #endregion

        #region Cloud Storage
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCloudPrefs()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.OverwriteCloudBackup = Request["prefCloudOverwrite"].CompareCurrentCultureIgnoreCase("overwrite") == 0;
            if (Enum.TryParse(Request["prefCloudDefault"], out StorageID sid))
                pf.DefaultCloudStorage = sid;
            pf.FCommit();
            CloudStorageBase.SetUsesFlatHierarchy(pf, Request["prefCloudGroup"] == null);

            return Redirect("~/mvc/prefs?pane=backup");
        }

        #region Dropbox
        [Authorize]
        public ActionResult DeAuthorizeDropbox()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.DropboxAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeDropbox()
        {
            new MFBDropbox().Authorize(Request, "~/mvc/oAuth/DropboxRedir".ToAbsolute(), MFBDropbox.szParamDropboxAuth);
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DropboxRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.DropboxAccessToken = JsonConvert.SerializeObject(new MFBDropbox().ConvertToken(Request));
                pf.FCommit();
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region Box
        [Authorize]
        public ActionResult DeAuthorizeBox()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(BoxDrive.PrefKeyBoxAuthToken, null, true);
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeBox()
        {
            new BoxDrive().Authorize("~/mvc/oAuth/BoxRedir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult BoxRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.SetPreferenceForKey(BoxDrive.PrefKeyBoxAuthToken, new BoxDrive().ConvertToken(Request));
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region GoogleDrive
        [Authorize]
        public ActionResult DeAuthorizeGDrive()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.GoogleDriveAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeGDrive()
        {
            new GoogleDrive().Authorize(Request, "~/mvc/oAuth/GDriveRedir".ToAbsolute(), GoogleDrive.szParamGDriveAuth);
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult GDriveRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (String.IsNullOrEmpty(GetStringParam("error")))
                {
                    pf.GoogleDriveAccessToken = new GoogleDrive().ConvertToken(Request);
                    pf.FCommit();
                }
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region OneDrive
        [Authorize]
        public ActionResult DeAuthorizeOneDrive()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.OneDriveAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeOneDrive()
        {
            // Note - can configure onedrive redir at https://apps.dev.microsoft.com/#/application/sapi/{applicationID}
            new OneDrive().Authorize("~/mvc/oauth/onedriveredir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult OneDriveRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.OneDriveAccessToken = new OneDrive().ConvertToken(Request);
                pf.FCommit();
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion
        #endregion

        #region Debriefing
        #region CloudAhoy
        [Authorize]
        public ActionResult AuthorizeCloudAhoy()
        {
            new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).Authorize("~/mvc/oAuth/CloudAhoyRedir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeCloudAhoy()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.CloudAhoyToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=debrief");
        }

        [Authorize]
        public ActionResult CloudAhoyRedir()
        {
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(Request["error"]))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.CloudAhoyToken = new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).ConvertToken(Request);
                pf.FCommit();
            }
            else
                nvc["debriefErr"] = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Request["error"], Request["error_description"] ?? string.Empty);

            return Redirect("~/mvc/prefs?pane=debrief" + (nvc.Count > 0 ? "&" + nvc.ToString() : string.Empty));
        }
        #endregion

        #region FlySto
        [Authorize]
        public ActionResult AuthorizeFlySto()
        {
            new FlyStoClient().Authorize("~/mvc/oauth/flystoredir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeFlySto()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(FlyStoClient.AccessTokenPrefKey, null, true);
            return Redirect("~/mvc/prefs?pane=debrief");
        }

        [Authorize]
        public ActionResult FlyStoRedir()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(FlyStoClient.AccessTokenPrefKey, new FlyStoClient().ConvertToken(Request));
            return Redirect("~/mvc/prefs?pane=debrief");
        }
        #endregion
        #endregion

        #region Client apps
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

        #region Developer Page
        [HttpPost]
        [Authorize]
        public ActionResult UpdateOauthClient(string clientID, string clientSecret, string clientName, string clientCallBack, string clientScopes, string szOwner, bool isPublic = false)
        {
            return SafeOp(() =>
            {
                MFBOauth2Client client = new MFBOauth2Client(clientID, clientSecret, clientCallBack, clientName, (clientScopes ?? string.Empty).Replace(",", " "), szOwner, isPublic ? ClientType.Public : ClientType.Confidential);
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
    }
}