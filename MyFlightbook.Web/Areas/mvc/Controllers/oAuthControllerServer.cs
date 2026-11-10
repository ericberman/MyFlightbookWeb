using MyFlightbook.Image;
using MyFlightbook.OAuth;
using MyFlightbook.OAuth.Server;
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
        /// <summary>
        /// Writes one of the server's responses verbatim (status, headers, body).
        /// </summary>
        private ActionResult OAuthResult(OAuthHttpResult r)
        {
            Response.StatusCode = r.StatusCode;
            Response.TrySkipIisCustomErrors = true;
            Response.SuppressFormsAuthenticationRedirect = true;    // otherwise Forms auth turns a 401 (e.g., invalid_client) into a 302 to the login page
            foreach (KeyValuePair<string, string> kv in r.Headers)
                Response.AppendHeader(kv.Key, kv.Value);
            return Content(r.Body, r.ContentType);
        }

        /// <summary>
        /// Redirects to an exact URL.  Redirect() would try to resolve non-http schemes (e.g., "com.example.app:/oauth") as relative paths.
        /// </summary>
        private ActionResult RedirectExact(string href)
        {
            Response.RedirectLocation = href;
            return new HttpStatusCodeResult(HttpStatusCode.Found);
        }

        /// <summary>
        /// Consent screen, or the error page (or an error redirect back to the client, when the client and redirect URI are trustworthy).
        /// </summary>
        private ActionResult AuthorizeView(AuthorizeValidationResult v, NameValueCollection originalRequest)
        {
            if (!v.IsValid)
            {
                if (v.ErrorRedirectHREF != null)
                    return RedirectExact(v.ErrorRedirectHREF);
                ViewBag.error = v.ErrorDescription;
                return View("authorize");
            }

            OAuthClient client = v.Request.Client;
            MFBOauth2Client registered = OAuth2AuthorizationServer.RegisteredClient(client.ClientId);
            ViewBag.clientName = client.ClientName;
            ViewBag.clientLogoHref = (registered?.Logo?.Length ?? 0) == 0 ? null : $"~/mvc/oAuth/ViewOAuthLogo?clientID={WebUtility.UrlEncode(client.ClientId)}".ToAbsolute();
            ViewBag.clientHost = client.IsMetadataDocumentClient ? new Uri(client.ClientId).Host : null;   // the name is self-asserted for these; show where it came from
            ViewBag.scopesList = MFBOauthServer.ScopeDescriptions(MFBOauthServer.ScopesFromStrings(v.Request.Scope));
            ViewBag.redirectHost = Uri.TryCreate(v.Request.RedirectHREF, UriKind.Absolute, out Uri redir) && !String.IsNullOrEmpty(redir.Host) ? redir.Host : redir?.Scheme ?? v.Request.RedirectHREF;
            ViewBag.authRequest = originalRequest.ToString();   // re-posted verbatim and re-validated by CommitAuth
            return View("authorize");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Authorize()
        {
            if (!Request.IsSecureConnection)
            {
                ViewBag.error = Resources.LocalizedText.oAuthErrNotSecure;
                return View("authorize");
            }
            NameValueCollection query = HttpUtility.ParseQueryString(Request.Url.Query);
            return AuthorizeView(OAuth2AuthorizationServer.ServerForRequest(Request).ValidateAuthorizeRequest(query), query);
        }

        /// <summary>
        /// The old PKCE-proxy entry point.  Existing public clients still send users here; PKCE is native now, so it's the same as Authorize.
        /// </summary>
        [Authorize]
        [HttpGet]
        public ActionResult AuthorizePKCE()
        {
            return Authorize();
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult CommitAuth(string authRequest, string authAction)
        {
            if (!Request.IsSecureConnection)
                throw new HttpException((int)HttpStatusCode.Forbidden, Resources.LocalizedText.oAuthErrNotSecure);

            OAuthServer server = OAuth2AuthorizationServer.ServerForRequest(Request);
            NameValueCollection query = HttpUtility.ParseQueryString(authRequest ?? string.Empty);
            AuthorizeValidationResult v = server.ValidateAuthorizeRequest(query);
            if (!v.IsValid)
                return AuthorizeView(v, query);

            return RedirectExact(authAction == "authorize" ? server.ApproveAuthorizeRequest(v.Request, User.Identity.Name) : server.DenyAuthorizeRequest(v.Request));
        }

        public ActionResult OAuthResource(string id = null)
        {
            OAuthServiceCall.ProcessRequest(Request, Response, id);
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult OAuthToken()
        {
            if (!Request.IsSecureConnection)
                return OAuthResult(new OAuthHttpResult() { StatusCode = (int)HttpStatusCode.Forbidden, Body = "{\"error\":\"invalid_request\",\"error_description\":\"Token and refresh requests MUST be on a secure channel\"}" });
            return OAuthEndpoint(() => OAuth2AuthorizationServer.ServerForRequest(Request).HandleTokenRequest(Request.Form, Request.Headers["Authorization"]));
        }

        /// <summary>
        /// Runs an endpoint, guaranteeing a JSON response even for an unexpected exception (rather than an HTML error page)
        /// </summary>
        private ActionResult OAuthEndpoint(Func<OAuthHttpResult> f)
        {
            try
            {
                return OAuthResult(f());
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                return OAuthResult(new OAuthHttpResult()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string>() { { "error", "server_error" }, { "error_description", ex.Message } })
                });
            }
        }

        /// <summary>
        /// RFC 7009 token revocation
        /// </summary>
        [HttpPost]
        public ActionResult Revoke()
        {
            if (!Request.IsSecureConnection)
                return OAuthResult(new OAuthHttpResult() { StatusCode = (int)HttpStatusCode.Forbidden, Body = "{\"error\":\"invalid_request\"}" });
            return OAuthEndpoint(() => OAuth2AuthorizationServer.ServerForRequest(Request).HandleRevocationRequest(Request.Form, Request.Headers["Authorization"]));
        }

        /// <summary>
        /// RFC 8414 authorization server metadata.  To be discoverable it must ALSO be reachable at /.well-known/oauth-authorization-server
        /// on the site root (see Support/OAuthMigration/README.md); this is the target of that rewrite.
        /// </summary>
        [HttpGet]
        public ActionResult Metadata()
        {
            return OAuthEndpoint(() => OAuth2AuthorizationServer.ServerForRequest(Request).AuthorizationServerMetadata());
        }
        #endregion

        #region Developer Page
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
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
        [ValidateHeaderAntiForgeryToken]
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
        [ValidateHeaderAntiForgeryToken]
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
        [ValidateHeaderAntiForgeryToken]
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
        [ValidateHeaderAntiForgeryToken]
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