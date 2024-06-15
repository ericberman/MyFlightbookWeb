using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;
using MyFlightbook.OAuth.CloudAhoy;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Net;
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
    public class oAuthController : Controller
    {
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

        [Authorize]
        public ActionResult FlyStoRedir()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(FlyStoClient.AccessTokenPrefKey, new FlyStoClient().ConvertToken(Request));
            pf.FCommit();
            return Redirect("~/Member/EditProfile.aspx/pftPrefs?pane=debrief");
        }

        // GET: mvc/oAuth
        public ActionResult Index()
        {
            return View();
        }
    }
}