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
    public enum MFBOAuthScope { none, currency, totals, addflight, readflight, addaircraft, readaircraft, visited, images }

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
                if (Enum.TryParse<MFBOAuthScope>(sz, out sc))
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
        UpdateImageAnnotation, DeleteImage
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
                    return MFBOAuthScope.images;
                case OAuthServiceID.totals:
                case OAuthServiceID.TotalsForUserWithQuery:
                    return MFBOAuthScope.totals;
                case OAuthServiceID.VisitedAirports:
                    return MFBOAuthScope.visited;
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
        public enum OutputFormat { XML, JSON }

        private void WriteObject(Stream s, object o)
        {
            string sz = string.Empty;

            switch (ResultFormat)
            {
                case OutputFormat.JSON:
                    sz = JsonConvert.SerializeObject(o, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
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
        protected string GeneratedAuthToken { get; set; }

        /// <summary>
        /// The oAuth token itself
        /// </summary>
        protected AccessToken Token { get; set; }

        /// <summary>
        /// WebService object to actually handle the calls
        /// </summary>
        protected MFBWebService WebService { get; set; }

        /// <summary>
        /// Result format - JSON or XML
        /// </summary>
        protected OutputFormat ResultFormat { get; set; }

        /// <summary>
        /// The request object passed in
        /// </summary>
        protected HttpRequest OriginalRequest { get; set; }

        /// <summary>
        /// The content type we should use for output.
        /// </summary>
        public string ContentType
        {
            get { return (ResultFormat == OAuthServiceCall.OutputFormat.JSON) ? "application/json; charset=utf-8" : "text/xml; charset=utf-8"; }
        }
        #endregion

        #region Constructor
        public OAuthServiceCall(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            OriginalRequest = request;
            ServiceCall = ServiceFromString(request.PathInfo);
            ResultFormat = (util.GetIntParam(request, "json", 0) != 0) ? OutputFormat.JSON : OutputFormat.XML;

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

                    WebService = new MFBWebService();

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

        /// <summary>
        /// Executes the requested service, writing any result to the specified output stream
        /// </summary>
        /// <param name="s">The output stream to which to write</param>
        public void Execute(Stream s)
        {
            string szResult = string.Empty;

            CheckAuth();

            switch (ServiceCall)
            {
                case OAuthServiceID.AddAircraftForUser:
                    WriteObject(s, WebService.AddAircraftForUser(GeneratedAuthToken, GetRequiredParam("szTail"), GetRequiredParam<int>("idModel"), GetRequiredParam<int>("idInstanceType")));
                    break;
                case OAuthServiceID.AircraftForUser:
                    WriteObject(s, WebService.AircraftForUser(GeneratedAuthToken));
                    break;
                case OAuthServiceID.AvailablePropertyTypesForUser:
                    WriteObject(s, WebService.AvailablePropertyTypesForUser(GeneratedAuthToken));
                    break;
                case OAuthServiceID.CommitFlightWithOptions:
                case OAuthServiceID.addFlight:
                    {
                        string szFormat = GetOptionalParam("format") ?? "Native";
                        LogbookEntry le = (szFormat.CompareCurrentCultureIgnoreCase("LTP") == 0) ?
                            JsonConvert.DeserializeObject<LogTenPro>(GetRequiredParam("flight"), new JsonConverter[] { new MFBDateTimeConverter() }).ToLogbookEntry() :
                            le = GetRequiredParam<LogbookEntry>("le");
                        WriteObject(s, WebService.CommitFlightWithOptions(GeneratedAuthToken, le, GetRequiredParam<PostingOptions>("po")));
                    }
                    break;
                case OAuthServiceID.currency:
                case OAuthServiceID.GetCurrencyForUser:
                    WriteObject(s, WebService.GetCurrencyForUser(GeneratedAuthToken));
                    break;
                case OAuthServiceID.DeleteAircraftForUser:
                    WriteObject(s, WebService.DeleteAircraftForUser(GeneratedAuthToken, GetRequiredParam<int>("idAircraft")));
                    break;
                case OAuthServiceID.DeleteImage:
                    WebService.DeleteImage(GeneratedAuthToken, GetRequiredParam<MFBImageInfo>("mfbii"));
                    break;
                case OAuthServiceID.DeleteLogbookEntry:
                    WriteObject(s, WebService.DeleteLogbookEntry(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                    break;
                case OAuthServiceID.DeletePropertiesForFlight:
                    WebService.DeletePropertiesForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight"), GetRequiredParam<int[]>("rgPropIds"));
                    break;
                case OAuthServiceID.FlightPathForFlight:
                    WriteObject(s, WebService.FlightPathForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                    break;
                case OAuthServiceID.FlightPathForFlightGPX:
                    WriteObject(s, WebService.FlightPathForFlightGPX(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                    break;
                case OAuthServiceID.FlightsWithQueryAndOffset:
                    WriteObject(s, WebService.FlightsWithQueryAndOffset(GeneratedAuthToken, GetRequiredParam<FlightQuery>("fq"), GetRequiredParam<int>("offset"), GetRequiredParam<int>("maxCount")));
                    break;
                case OAuthServiceID.MakesAndModels:
                    WriteObject(s, WebService.MakesAndModels());
                    break;
                case OAuthServiceID.PropertiesForFlight:
                    WriteObject(s, WebService.PropertiesForFlight(GeneratedAuthToken, GetRequiredParam<int>("idFlight")));
                    break;
                case OAuthServiceID.totals:
                case OAuthServiceID.TotalsForUserWithQuery:
                    WriteObject(s, WebService.TotalsForUserWithQuery(GeneratedAuthToken, GetOptionalParam<FlightQuery>("fq")));
                    break;
                case OAuthServiceID.UpdateImageAnnotation:
                    WebService.UpdateImageAnnotation(GeneratedAuthToken, GetRequiredParam<MFBImageInfo>("mfbii"));
                    break;
                case OAuthServiceID.UpdateMaintenanceForAircraftWithFlagsAndNotes:
                    WebService.UpdateMaintenanceForAircraftWithFlagsAndNotes(GeneratedAuthToken, GetRequiredParam<Aircraft>("ac"));
                    break;
                case OAuthServiceID.VisitedAirports:
                    WriteObject(s, WebService.VisitedAirports(GeneratedAuthToken));
                    break;
                case OAuthServiceID.none:
                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }
}