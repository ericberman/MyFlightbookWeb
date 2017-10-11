using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using MyFlightbook;
using MyFlightbook.FlightCurrency;
using MyFlightbook.ImportFlights;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Code;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_oAuthToken : System.Web.UI.Page
{
    private bool JsonRequested { get { return util.GetIntParam(Request, "json", 0) != 0; } }

    private void CheckAuth(AccessToken token, MFBOauthServer.MFBOAuthScope sc)
    {
        if (!MFBOauthServer.CheckScope(token.Scope, sc))
            throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, "Requested action requires scope \"{0}\", which is not granted.", sc.ToString()));
    }

    private void WriteObject<T>(T o)
    {
        if (JsonRequested)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            Response.ContentType = "application/json; charset=utf-8";
            ser.WriteObject(Response.OutputStream, o);
        }
        else
        {
            Response.ContentType = "text/xml; charset=utf-8";
            Response.Write(o.SerializeXML());
        }
    }

    private void returnCurrency(AccessToken token)
    {
        CheckAuth(token, MFBOauthServer.MFBOAuthScope.currency);

        CurrencyStatusItem[] rgcsi = CurrencyStatusItem.GetCurrencyItemsForUser(token.User, false).ToArray();
        WriteObject<CurrencyStatusItem[]>(rgcsi);
    }

    private void returnTotals(AccessToken token)
    {
        CheckAuth(token, MFBOauthServer.MFBOAuthScope.totals);

        string szJsonQuery = Request["q"];
        FlightQuery fq = String.IsNullOrEmpty(szJsonQuery) ? new FlightQuery(token.User) : JsonConvert.DeserializeObject<FlightQuery>(szJsonQuery);
        fq.UserName = token.User;  // ALWAYS, for security.

        UserTotals ut = new UserTotals(token.User, fq, true);
        ut.DataBind();

        WriteObject<Collection<TotalsItem>>(ut.Totals);
    }

    private enum RequestedFormat { Native, LTP }

    private void addFlight(AccessToken token)
    {

        CheckAuth(token, MFBOauthServer.MFBOAuthScope.addflight);

        string szJsonFlight = Request["flight"];

        RequestedFormat rf = RequestedFormat.Native;
        if (Request["format"] != null)
        {
            try
            {
                rf = (RequestedFormat)Convert.ToInt32(Request["format"], CultureInfo.InvariantCulture);
            }
            catch (FormatException) { }
            catch (OverflowException) { }
        }

        if (string.IsNullOrEmpty(szJsonFlight))
            throw new MyFlightbookException("Required \"flight\" parameter missing.");

        
        LogbookEntry le = null;

        switch (rf)
        {
            case RequestedFormat.LTP:
                LogTenPro ltp = JsonConvert.DeserializeObject<LogTenPro>(szJsonFlight, new Newtonsoft.Json.JsonConverter[] { new MFBDateTimeConverter() });
                le = ltp.ToLogbookEntry();
                break;
            case RequestedFormat.Native:
                le = JsonConvert.DeserializeObject<LogbookEntry>(szJsonFlight);
                break;
        }
        
        le.User = token.User;

        // Fix up aircraft ID, if possible.
        UserAircraft ua = new UserAircraft(token.User);
        Aircraft ac = ua.GetUserAircraftByTail(le.TailNumDisplay);
        if (ac != null)
            le.AircraftID = ac.AircraftID;
        else
            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Unknown or missing aircraft: \"{0}\"", le.TailNumDisplay));
        if (!le.FCommit())
            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to save flight: {0}", le.ErrorString));
        Response.Write("OK - flight ID is " + le.FlightID);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Request.IsSecureConnection)
            throw new HttpException((int)HttpStatusCode.Forbidden, "Authorization requests MUST be on a secure channel");

        if (String.IsNullOrEmpty(Request.PathInfo))
        {
            AuthorizationServer authorizationServer = new AuthorizationServer(new OAuth2AuthorizationServer());
            OutgoingWebResponse wr = authorizationServer.HandleTokenRequest();
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(wr.Body);
            Response.End();
        }
        else
        {
            using (RSACryptoServiceProvider rsaSigning = new RSACryptoServiceProvider())
            {
                using (RSACryptoServiceProvider rsaEncryption = new RSACryptoServiceProvider())
                {

                    rsaSigning.ImportParameters(OAuth2AuthorizationServer.AuthorizationServerSigningPublicKey);
                    rsaEncryption.ImportParameters(OAuth2AuthorizationServer.CreateAuthorizationServerSigningKey());
                    ResourceServer server = new ResourceServer(new StandardAccessTokenAnalyzer(rsaSigning, rsaEncryption));
                    AccessToken token = server.GetAccessToken();

                    if (token.Lifetime.HasValue && token.UtcIssued.Add(token.Lifetime.Value).CompareTo(DateTime.UtcNow) < 0)
                        throw new MyFlightbookException("oAuth2 - Token has expired!");
                    if (String.IsNullOrEmpty(token.User))
                        throw new MyFlightbookException("Invalid oAuth token - no user");

                    Response.Clear();
                    if (Request.PathInfo.CompareOrdinalIgnoreCase("/currency") == 0)
                        returnCurrency(token);
                    else if (Request.PathInfo.CompareOrdinalIgnoreCase("/totals") == 0)
                        returnTotals(token);
                    else if (Request.PathInfo.CompareOrdinalIgnoreCase("/addFlight") == 0)
                        addFlight(token);
                    Response.End();
                }
            }
       }
    }
}