using DotNetOpenAuth.OAuth2;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.ImportFlights;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Services;


/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace OAuthAuthorizationServer.Services
{
    public enum MFBOAuthScope { none, currency, totals, addflight, readflight, addaircraft, readaircraft, visited, images, namedqueries }

    public static class MFBOauthServer
    {
        /// <summary>
        /// Returns a localized human description of each specified scope
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static string ScopeDescription(MFBOAuthScope scope)
        {
            switch (scope)
            {
                case MFBOAuthScope.currency:
                    return Resources.oAuth.currencyScopeDescription;
                case MFBOAuthScope.totals:
                    return Resources.oAuth.totalsScopeDescription;
                case MFBOAuthScope.addflight:
                    return Resources.oAuth.addflightScopeDescription;
                case MFBOAuthScope.readflight:
                    return Resources.oAuth.readflightScopeDescription;
                case MFBOAuthScope.addaircraft:
                    return Resources.oAuth.addaircraftScopeDescription;
                case MFBOAuthScope.readaircraft:
                    return Resources.oAuth.readaircraftScopeDescription;
                case MFBOAuthScope.visited:
                    return Resources.oAuth.visitedScopeDescription;
                case MFBOAuthScope.images:
                    return Resources.oAuth.modifyImagesScopeDescription;
                case MFBOAuthScope.namedqueries:
                    return Resources.oAuth.namedqueriesScopeDescription;
                case MFBOAuthScope.none:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException("scope");
            }
        }

        /// <summary>
        /// Returns an enumerable of descriptions from an enumerable of scopes
        /// </summary>
        /// <param name="lstsc"></param>
        /// <returns></returns>
        public static IEnumerable<string> ScopeDescriptions(IEnumerable<MFBOAuthScope> lstsc)
        {
            List<string> lst = new List<string>();
            if (lstsc == null)
                return lst;
            foreach (MFBOAuthScope sc in lstsc)
                lst.Add(ScopeDescription(sc));
            lst.Sort();
            return lst;
        }

        /// <summary>
        /// Returns an enumerable of MFBOauthScope from an enumerable of strings.
        /// </summary>
        /// <param name="lstsc"></param>
        /// <returns></returns>
        public static IEnumerable<MFBOAuthScope> ScopesFromStrings(IEnumerable<string> lstsc)
        {
            List<MFBOAuthScope> lst = new List<MFBOAuthScope>();
            if (lstsc == null)
                return lst;
            foreach (string sz in lstsc)
            {
                MFBOAuthScope sc;
                if (Enum.TryParse<MFBOAuthScope>(sz, out sc) && Enum.IsDefined(typeof(MFBOAuthScope), sc))
                    lst.Add(sc);
                else
                    throw new ArgumentOutOfRangeException(sz);
            }
            return lst;
        }

        public static IEnumerable<MFBOAuthScope> ScopesFromString(string sz)
        {
            List<MFBOAuthScope> lst = new List<MFBOAuthScope>();
            if (sz == null)
                return lst;

            return ScopesFromStrings(OAuthUtilities.SplitScopes(sz));
        }

        /// <summary>
        /// Checks if the specified scope is allowed in the list of scopes
        /// </summary>
        public static bool CheckScope(IEnumerable<MFBOAuthScope> lst, MFBOAuthScope sc)
        {
            if (lst == null)
                return false;
            return lst.Contains(sc);
        }

        /// <summary>
        /// Checks if the specified scope is allowed in the list of scopes (by string)
        /// </summary>
        public static bool CheckScope(IEnumerable<string> lst, MFBOAuthScope sc)
        {
            if (lst == null)
                return false;
            return CheckScope(ScopesFromStrings(lst), sc);
        }

        /// <summary>
        /// Checks if the specified scope is allowed in the space-separated of scopes
        /// </summary>
        public static bool CheckScope(string sz, MFBOAuthScope sc)
        {
            if (String.IsNullOrEmpty(sz))
                return false;
            return CheckScope(ScopesFromString(sz), sc);
        }
    }

    #region Available Services
    public enum OAuthServiceID
    {
        /* 0 = invalid */
        none,
        /* Aircraft Services */
        AddAircraftForUser, AircraftForUser, MakesAndModels, UpdateMaintenanceForAircraftWithFlagsAndNotes, DeleteAircraftForUser,
        /* Flight Services */
        CommitFlightWithOptions, addFlight, FlightsWithQueryAndOffset, FlightPathForFlight, FlightPathForFlightGPX, PropertiesForFlight, AvailablePropertyTypesForUser, DeleteLogbookEntry, DeletePropertiesForFlight,
        /* Currency */
        GetCurrencyForUser, currency,
        /* Totals */
        TotalsForUserWithQuery, totals,
        /* Visited Airports */
        VisitedAirports,
        /* Images */
        UpdateImageAnnotation, DeleteImage, UploadImage,
        /* Canned Queries */
        AddNamedQuery, DeleteNamedQuery, GetNamedQueries
    }

    public class OAuthServiceCall : WebService
    {
        /// <summary>
        /// Determines the requested service from the request path.
        /// </summary>
        /// <param name="szRequest"></param>
        /// <returns></returns>
        private OAuthServiceID ServiceFromString(string szRequest)
        {
            if (szRequest.StartsWith("/", StringComparison.CurrentCultureIgnoreCase))
                szRequest = szRequest.Substring(1);

            if (String.IsNullOrEmpty(szRequest))
                return OAuthServiceID.none;

            return (OAuthServiceID)Enum.Parse(typeof(OAuthServiceID), szRequest);
        }

        /// <summary>
        /// Determine the required scope for the specified call
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        protected MFBOAuthScope ScopeForService()
        {
            switch (ServiceCall)
            {
                case OAuthServiceID.AddAircraftForUser:
                case OAuthServiceID.DeleteAircraftForUser:
                case OAuthServiceID.UpdateMaintenanceForAircraftWithFlagsAndNotes:
                    return MFBOAuthScope.addaircraft;
                case OAuthServiceID.addFlight:
                case OAuthServiceID.CommitFlightWithOptions:
                case OAuthServiceID.DeletePropertiesForFlight:
                case OAuthServiceID.DeleteLogbookEntry:
                    return MFBOAuthScope.addflight;
                case OAuthServiceID.MakesAndModels:
                case OAuthServiceID.AircraftForUser:
                    return MFBOAuthScope.readaircraft;
                case OAuthServiceID.FlightsWithQueryAndOffset:
                case OAuthServiceID.AvailablePropertyTypesForUser:
                case OAuthServiceID.PropertiesForFlight:
                case OAuthServiceID.FlightPathForFlight:
                case OAuthServiceID.FlightPathForFlightGPX:
                    return MFBOAuthScope.readflight;
                case OAuthServiceID.currency:
                case OAuthServiceID.GetCurrencyForUser:
                    return MFBOAuthScope.currency;
                case OAuthServiceID.DeleteImage:
                case OAuthServiceID.UpdateImageAnnotation:
                case OAuthServiceID.UploadImage:
                    return MFBOAuthScope.images;
                case OAuthServiceID.totals:
                case OAuthServiceID.TotalsForUserWithQuery:
                    return MFBOAuthScope.totals;
                case OAuthServiceID.VisitedAirports:
                    return MFBOAuthScope.visited;
                case OAuthServiceID.AddNamedQuery:
                case OAuthServiceID.DeleteNamedQuery:
                case OAuthServiceID.GetNamedQueries:
                    return MFBOAuthScope.namedqueries;
                case OAuthServiceID.none:
                default:
                    throw new InvalidOperationException();
            }
        }

        private void CheckAuth()
        {
            MFBOAuthScope sc = ScopeForService();
            if (!MFBOauthServer.CheckScope(Token.Scope, sc))
                throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, "Requested action requires scope \"{0}\", which is not granted.", sc.ToString()));
        }
        #endregion

        #region Output
        protected enum OutputFormat { XML, JSON, JSONP }

        private string ObjectToJSon(object o)
        {
            return (o == null) ? string.Empty : JsonConvert.SerializeObject(o, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        private void WriteObject(Stream s, object o)
        {
            string sz = string.Empty;

            switch (ResultFormat)
            {
                case OutputFormat.JSON:
                    sz = ObjectToJSon(o);
                    break;
                case OutputFormat.JSONP:
                    sz = String.Format(CultureInfo.InvariantCulture, "{0}({1})", ResponseCallback, ObjectToJSon(o));
                    break;
                case OutputFormat.XML:
                    sz = o.SerializeXML();
                    break;
            }
            byte[] rgb = Encoding.UTF8.GetBytes(sz);
            s.Write(rgb, 0, rgb.Length);
        }
        #endregion

        #region Properties
        /// <summary>
        /// The function that is being requested
        /// </summary>
        protected OAuthServiceID ServiceCall { get; set; }

        /// <summary>
        /// A pseudo authtoken generated from the OAuth token.
        /// </summary>
        public string GeneratedAuthToken { get; set; }

        /// <summary>
        /// The oAuth token itself
        /// </summary>
        public AccessToken Token { get; set; }

        /// <summary>
        /// Result format - JSON or XML
        /// </summary>
        protected OutputFormat ResultFormat { get; set; }

        /// <summary>
        /// The request object passed in
        /// </summary>
        protected HttpRequest OriginalRequest { get; set; }

        /// <summary>
        /// For JSONP support rather than JSON, here's the callback function name.
        /// </summary>
        protected string ResponseCallback { get; set; }

        /// <summary>
        /// The content type we should use for output.
        /// </summary>
        public string ContentType
        {
            get
            {
                switch (ResultFormat)
                {
                    case OutputFormat.JSON:
                        return "application/json; charset=utf-8";
                    case OutputFormat.JSONP:
                        return "application/javascript; charset=utf-8";
                    default:
                    case OutputFormat.XML:
                        return "text/xml; charset=utf-8";
                }
            }
        }
        #endregion

        #region Constructor
        public OAuthServiceCall(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            OriginalRequest = request;
            ServiceCall = ServiceFromString(request.PathInfo);
            ResponseCallback = util.GetStringParam(request, "callback");
            ResultFormat = (util.GetIntParam(request, "json", 0) != 0) ? (String.IsNullOrEmpty(ResponseCallback) ? OutputFormat.JSON : OutputFormat.JSONP) : OutputFormat.XML;

            using (RSACryptoServiceProvider rsaSigning = new RSACryptoServiceProvider())
            {
                using (RSACryptoServiceProvider rsaEncryption = new RSACryptoServiceProvider())
                {
                    rsaSigning.ImportParameters(OAuth2AuthorizationServer.AuthorizationServerSigningPublicKey);
                    rsaEncryption.ImportParameters(OAuth2AuthorizationServer.CreateAuthorizationServerSigningKey());
                    ResourceServer server = new ResourceServer(new StandardAccessTokenAnalyzer(rsaSigning, rsaEncryption));
                    Token = server.GetAccessToken();

                    if (Token.Lifetime.HasValue && Token.UtcIssued.Add(Token.Lifetime.Value).CompareTo(DateTime.UtcNow) < 0)
                        throw new MyFlightbookException("oAuth2 - Token has expired!");
                    if (String.IsNullOrEmpty(Token.User))
                        throw new MyFlightbookException("Invalid oAuth token - no user");

                    GeneratedAuthToken = MFBWebService.AuthTokenFromOAuthToken(Token);
                }
            }
        }
        #endregion

        #region Making calls
        protected string GetOptionalParam(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            string s = OriginalRequest[name];
            return s;
        }

        protected string GetRequiredParam(string name)
        {
            string s = GetOptionalParam(name);
            if (String.IsNullOrEmpty(s))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Required parameter '{0}' missing", name));
            return s;
        }

        protected T GetOptionalParam<T>(string name)
        {
            string s = GetOptionalParam(name);
            if (String.IsNullOrEmpty(s))
                return default(T);
            return JsonConvert.DeserializeObject<T>(s);
        }

        protected T GetRequiredParam<T>(string name)
        {
            return JsonConvert.DeserializeObject<T>(GetRequiredParam(name));
        }

        #region service calls
        private void AddFlight(Stream s, MFBWebService mfbSvc)
        {
            string szFormat = GetOptionalParam("format") ?? "Native";
            LogbookEntry le = (szFormat.CompareCurrentCultureIgnoreCase("LTP") == 0) ?
                JsonConvert.DeserializeObject<LogTenPro>(GetRequiredParam("flight"), new JsonConverter[] { new MFBDateTimeConverter() }).ToLogbookEntry() :
                le = GetRequiredParam<LogbookEntry>("le");
            WriteObject(s, mfbSvc.CommitFlightWithOptions(GeneratedAuthToken, le, GetRequiredParam<PostingOptions>("po")));
        }
        #endregion

        /// <summary>
        /// Executes the requested service, writing any result to the specified output stream
        /// </summary>
        /// <param name="s">The output stream to which to write</param>
        public void Execute(Stream s)
        {
            string szResult = string.Empty;

            CheckAuth();

            using (MFBWebService mfbSvc = new MFBWebService())
            {
                switch (ServiceCall)
                {
                    case OAuthServiceID.AddAircraftForUser:
                        WriteObject(s, mfbSvc.AddAircraftForUser(GeneratedAuthToken, GetRequiredParam("szTail"), GetRequiredParam<int>("idModel"), GetRequiredParam<int>("idInstanceType")));
                        break;
                    case OAuthServiceID.AircraftForUser:
                        WriteObject(s, mfbSvc.AircraftForUser(GeneratedAuthToken));
                        break;
                    case OAuthServiceID.AvailablePropertyTypesForUser:
                        WriteObject(s, mfbSvc.AvailablePropertyTypesForUser(GeneratedAuthToken));
                        break;
                    case OAuthServiceID.CommitFlightWithOptions:
                    case OAuthServiceID.addFlight:
                        AddFlight(s, mfbSvc);
                        break;
                    case OAuthServiceID.currency:
                    case OAuthServiceID.GetCurrencyForUser:
                        WriteObject(s, mfbSvc.GetCurrencyForUser(GeneratedAuthToken));
                        break;
                    case OAuthServiceID.DeleteAircraftForUser:
                        WriteObject(s, mfbSvc.DeleteAircraftForUser(GeneratedAuthToken, GetRequiredParam<int>("idAircraft")));
                        break;
                    case OAuthServiceID.DeleteImage:
                        mfbSvc.DeleteImage(GeneratedAuthToken, GetRequiredParam<MFBImageInfo>("mfbii"));
                        break;
                    case OAuthServiceID.DeleteLogbookEntry:
                        WriteObject(s, mfbSvc.DeleteLogbookEntry(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                        break;
                    case OAuthServiceID.DeletePropertiesForFlight:
                        mfbSvc.DeletePropertiesForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight"), GetRequiredParam<int[]>("rgPropIds"));
                        break;
                    case OAuthServiceID.FlightPathForFlight:
                        WriteObject(s, mfbSvc.FlightPathForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                        break;
                    case OAuthServiceID.FlightPathForFlightGPX:
                        WriteObject(s, mfbSvc.FlightPathForFlightGPX(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                        break;
                    case OAuthServiceID.FlightsWithQueryAndOffset:
                        WriteObject(s, mfbSvc.FlightsWithQueryAndOffset(GeneratedAuthToken, GetRequiredParam<FlightQuery>("fq"), GetRequiredParam<int>("offset"), GetRequiredParam<int>("maxCount")));
                        break;
                    case OAuthServiceID.MakesAndModels:
                        WriteObject(s, mfbSvc.MakesAndModels());
                        break;
                    case OAuthServiceID.PropertiesForFlight:
                        WriteObject(s, mfbSvc.PropertiesForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                        break;
                    case OAuthServiceID.totals:
                    case OAuthServiceID.TotalsForUserWithQuery:
                        WriteObject(s, mfbSvc.TotalsForUserWithQuery(GeneratedAuthToken, GetOptionalParam<FlightQuery>("fq")));
                        break;
                    case OAuthServiceID.UpdateImageAnnotation:
                        mfbSvc.UpdateImageAnnotation(GeneratedAuthToken, GetRequiredParam<MFBImageInfo>("mfbii"));
                        break;
                    case OAuthServiceID.UpdateMaintenanceForAircraftWithFlagsAndNotes:
                        mfbSvc.UpdateMaintenanceForAircraftWithFlagsAndNotes(GeneratedAuthToken, GetRequiredParam<Aircraft>("ac"));
                        break;
                    case OAuthServiceID.VisitedAirports:
                        WriteObject(s, mfbSvc.VisitedAirports(GeneratedAuthToken));
                        break;
                    case OAuthServiceID.GetNamedQueries:
                        WriteObject(s, mfbSvc.GetNamedQueriesForUser(GeneratedAuthToken));
                        break;
                    case OAuthServiceID.AddNamedQuery:
                        WriteObject(s, mfbSvc.AddNamedQueryForUser(GeneratedAuthToken, GetRequiredParam<CannedQuery>("fq"), GetRequiredParam<string>("szName")));
                        break;
                    case OAuthServiceID.DeleteNamedQuery:
                        WriteObject(s, mfbSvc.DeleteNamedQueryForUser(GeneratedAuthToken, GetRequiredParam<CannedQuery>("cq")));
                        break;
                    case OAuthServiceID.none:
                    case OAuthServiceID.UploadImage:    // not serviced here.
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
        #endregion
    }

    public abstract class UploadImagePage : System.Web.UI.Page
    {
        /// <summary>
        /// Method to actually upload the image; this method will decide what to look for.
        /// Basic validation and authentication has already been performed, but that's it.
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="pf"></param>
        /// <returns></returns>
        protected abstract MFBImageInfo UploadForUser(string szUser, HttpPostedFile pf, string szComment);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (String.Compare(Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) == 0)
                return;

            if (!Request.IsSecureConnection)
                throw new HttpException((int)HttpStatusCode.Forbidden, "Image upload MUST be on a secure channel");

            if (ShuntState.IsShunted)
                throw new MyFlightbookException(ShuntState.ShuntMessage);

            System.Web.UI.HtmlControls.HtmlInputFile imgPicture = (System.Web.UI.HtmlControls.HtmlInputFile)FindControl("imgPicture");
            if (imgPicture == null)
                throw new MyFlightbookException("No control named 'imgPicture' found!");

            string szErr = "OK";

            try
            {
                string szUser = string.Empty;
                string szAuth = Request.Form["txtAuthToken"];
                if (String.IsNullOrEmpty(szAuth))
                {
                    // check for an oAuth token
                    using (OAuthServiceCall service = new OAuthServiceCall(Request))
                    {
                        szAuth = service.GeneratedAuthToken;

                        // Verify that you're allowed to modify images.
                        if (!MFBOauthServer.CheckScope(service.Token.Scope, MFBOAuthScope.images))
                            throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, "Requested action requires scope \"{0}\", which is not granted.", MFBOAuthScope.images.ToString()));
                    }
                }

                using (MFBWebService ws = new MFBWebService())
                {
                    szUser = ws.GetEncryptedUser(szAuth);
                }

                if (string.IsNullOrEmpty(szUser))
                    throw new MyFlightbookException(Resources.WebService.errBadAuth);

                HttpPostedFile pf = imgPicture.PostedFile;
                if (pf == null || pf.ContentLength == 0)
                    throw new MyFlightbookException(Resources.WebService.errNoImageProvided);

                // Upload the image, and then perform a pseudo idempotency check on it.
                MFBImageInfo mfbii = UploadForUser(szUser, pf, Request.Form["txtComment"] ?? string.Empty);
                mfbii.IdempotencyCheck();
            }
            catch (MyFlightbookException ex)
            {
                szErr = ex.Message;
            }

            Response.Clear();
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Write(szErr);
        }
    }
}