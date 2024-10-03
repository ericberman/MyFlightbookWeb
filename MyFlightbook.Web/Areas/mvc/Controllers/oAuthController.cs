using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;
using MyFlightbook.CloudStorage;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.OAuth.FlightCrewView;
using MyFlightbook.OAuth.Leon;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
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

                MFBOauthClientAuth ca = new MFBOauthClientAuth { Scope = OAuthUtilities.JoinScopes(pendingRequest.Scope), ClientId = pendingRequest.ClientIdentifier, UserId = User.Identity.Name, ExpirationDateUtc = DateTime.UtcNow.AddYears(10) };
                if (ca.fCommit())
                {
                    EndUserAuthorizationSuccessResponseBase resp = authorizationServer.PrepareApproveAuthorizationRequest(pendingRequest, User.Identity.Name);
                    OutgoingWebResponse wr = authorizationServer.Channel.PrepareResponse(resp);
                    wr.Send();
                }
                else
                    RejectWithError(authorizationServer, pendingRequest, Resources.LocalizedText.oAuthErrCreationFailed);
            }
            else if (authAction == "reject")
            {
                RejectWithError(authorizationServer, pendingRequest, Resources.LocalizedText.oAuthErrNotAuthorized);
            }
            return new EmptyResult();
        }

        public ActionResult OAuthResource(string id)
        {
            OAuthServiceCall.ProcessRequest(id);
            return new EmptyResult();
        }

        public ActionResult OAuthToken(string id)
        {
            OAuthServiceCall.ProcessRequest(id);
            return new EmptyResult();
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
            string szErr = util.GetStringParam(Request, "error");
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
            string szErr = util.GetStringParam(Request, "error");
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
            string szErr = util.GetStringParam(Request, "error");
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
            string szErr = util.GetStringParam(Request, "error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (String.IsNullOrEmpty(util.GetStringParam(Request, "error")))
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
            string szErr = util.GetStringParam(Request, "error");
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

        // GET: mvc/oAuth
        public ActionResult Index()
        {
            return View();
        }
    }
}