using DotNetOpenAuth.OAuth2;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.ImportFlights;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Code;
using System;
using System.Globalization;
using System.IO;
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
        protected MFBOauthServer.MFBOAuthScope ScopeForService()
        {
            switch (ServiceCall)
            {
                case OAuthServiceID.AddAircraftForUser:
                case OAuthServiceID.DeleteAircraftForUser:
                case OAuthServiceID.UpdateMaintenanceForAircraftWithFlagsAndNotes:
                    return MFBOauthServer.MFBOAuthScope.addaircraft;
                case OAuthServiceID.addFlight:
                case OAuthServiceID.CommitFlightWithOptions:
                case OAuthServiceID.DeletePropertiesForFlight:
                case OAuthServiceID.DeleteLogbookEntry:
                    return MFBOauthServer.MFBOAuthScope.addflight;
                case OAuthServiceID.MakesAndModels:
                case OAuthServiceID.AircraftForUser:
                    return MFBOauthServer.MFBOAuthScope.readaircraft;
                case OAuthServiceID.FlightsWithQueryAndOffset:
                case OAuthServiceID.AvailablePropertyTypesForUser:
                case OAuthServiceID.PropertiesForFlight:
                case OAuthServiceID.FlightPathForFlight:
                case OAuthServiceID.FlightPathForFlightGPX:
                    return MFBOauthServer.MFBOAuthScope.readflight;
                case OAuthServiceID.currency:
                case OAuthServiceID.GetCurrencyForUser:
                    return MFBOauthServer.MFBOAuthScope.currency;
                case OAuthServiceID.DeleteImage:
                case OAuthServiceID.UpdateImageAnnotation:
                    return MFBOauthServer.MFBOAuthScope.images;
                case OAuthServiceID.totals:
                case OAuthServiceID.TotalsForUserWithQuery:
                    return MFBOauthServer.MFBOAuthScope.totals;
                case OAuthServiceID.VisitedAirports:
                    return MFBOauthServer.MFBOAuthScope.visited;
                case OAuthServiceID.none:
                default:
                    throw new InvalidOperationException();
            }
        }

        private void CheckAuth()
        {
            MFBOauthServer.MFBOAuthScope sc = ScopeForService();
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