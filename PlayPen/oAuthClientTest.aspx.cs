using DotNetOpenAuth.OAuth2;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_oAuthClientTest : System.Web.UI.Page
{
    [Serializable]
    protected class PageState
    {
        public string AuthURL { get; set; }
        public string TokenURL { get; set; }
        public string ResourceURL { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectURL { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }
        public string Authorization { get; set; }
        public string Token { get; set; }
    }

    protected PageState CurrentPageState
    {
        get
        {
            try
            {
                return (PageState)Session["oAuthClientPageState"];
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }
        set { Session["oAuthClientPageState"] = value; }
    }

    protected void FromSession()
    {
        PageState ps = CurrentPageState;
        if (ps == null)
            return;

        txtAuthURL.Text = ps.AuthURL;
        txtTokenURL.Text = ps.TokenURL;
        txtResourceURL.Text = ps.ResourceURL;
        txtClientID.Text = ps.ClientID;
        txtClientSecret.Text = ps.ClientSecret;
        txtRedirectURL.Text = ps.RedirectURL;
        txtScope.Text = ps.Scope;
        txtState.Text = ps.State;
        lblAuthorization.Text = ps.Authorization;
        lblToken.Text = ps.Token;
    }

    protected void ToSession()
    {
        PageState ps = new PageState()
        {

            AuthURL = txtAuthURL.Text,
            TokenURL = txtTokenURL.Text,
            ResourceURL = txtResourceURL.Text,
            ClientID = txtClientID.Text,
            ClientSecret = txtClientSecret.Text,
            RedirectURL = txtRedirectURL.Text,
            Scope = txtScope.Text,
            State = txtState.Text,
            Authorization = lblAuthorization.Text,
            Token = lblToken.Text
        };
        CurrentPageState = ps;
    }

    protected IEnumerable<string> Scopes
    {
        get { return txtScope.Text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries); }
    }

    protected Uri RedirURL
    {
        get { return String.IsNullOrEmpty(txtRedirectURL.Text) ? null : new Uri(txtRedirectURL.Text); }
    }

    private string FixHost(string szCurrent)
    {
        return szCurrent.Replace("://" + MyFlightbook.Branding.CurrentBrand.HostName, "://" + Request.Url.Host);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            FromSession();
            if (Request["code"] != null)
                lblAuthorization.Text = Request["code"].ToString();
            if (Request["state"] != null)
                txtState.Text = Request["state"].ToString();
            if (Request["access_token"] != null)
                lblToken.Text = Request["access_token"].ToString();

            txtAuthURL.Text = FixHost(txtAuthURL.Text);
            txtRedirectURL.Text = FixHost(txtRedirectURL.Text);
            txtResourceURL.Text = FixHost(txtResourceURL.Text);
            txtTokenURL.Text = FixHost(txtTokenURL.Text);
            txtImgUploadURL.Text = FixHost(txtImgUploadURL.Text);

            List<KeyValuePair<string, string>> lst = new List<KeyValuePair<string,string>>();
            foreach (string szKey in Request.Params.Keys)
                lst.Add(new KeyValuePair<string, string>(szKey, Request.Params[szKey]));
            gvResults.DataSource = lst;
            gvResults.DataBind();

            if (Request["error"] != null)
                lblErr.Text = Request["error"].ToString();
        }
    }

    protected AuthorizationServerDescription Description()
    {
        AuthorizationServerDescription desc = new AuthorizationServerDescription()
        {
            AuthorizationEndpoint = new Uri(txtAuthURL.Text),
            ProtocolVersion = ProtocolVersion.V20,
            TokenEndpoint = new Uri(txtTokenURL.Text)
        };
        return desc;
    }

    protected WebServerClient Client()
    {
        WebServerClient client = new WebServerClient(Description(), txtClientID.Text, txtClientSecret.Text);
        return client;
    }

    protected void btnGetAuth_Click(object sender, EventArgs e)
    {
        if (Page.IsValid)
        {
            try
            {
                ToSession();
                WebServerClient client = Client();
                client.RequestUserAuthorization(Scopes, RedirURL);
            }
            catch (MyFlightbook.MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
            catch (DotNetOpenAuth.Messaging.ProtocolException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }

    protected void AppendQueryString(string szKey, string szVal, StringBuilder sb)
    {
        if (sb == null)
            throw new ArgumentNullException("sb");
        if (sb.Length > 0)
            sb.Append("&");
        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}={1}", szKey, HttpUtility.UrlEncode(szVal));
    }

    protected void btnGetToken_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;
        try
        {
            ToSession();
            WebServerClient consumer = new WebServerClient(Description(), CurrentPageState.ClientID, CurrentPageState.ClientSecret) { ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(CurrentPageState.ClientSecret) };
            IAuthorizationState grantedAccess = consumer.ProcessUserAuthorization(new HttpRequestWrapper(Request));

            if (grantedAccess == null)
                throw new MyFlightbook.MyFlightbookValidationException("Null access token returned - invalid authorization passed?");
            lblToken.Text = grantedAccess.AccessToken;
        }
        catch (MyFlightbook.MyFlightbookValidationException ex)
        {
            lblErr.Text = ex.Message;
        }
        catch (DotNetOpenAuth.Messaging.ProtocolException ex)
        {
            lblErr.Text = ex.Message;
        }
    }

    protected string ResourcePath
    {
        get
        {
            OAuthServiceID action = SelectedAction;

            return String.Format(CultureInfo.InvariantCulture, "{0}{1}?access_token={2}&json={3}{4}",
                action == OAuthServiceID.UploadImage ? txtImgUploadURL.Text : txtResourceURL.Text,
                action == OAuthServiceID.none ? txtCustomVerb.Text : (action == OAuthServiceID.UploadImage ? string.Empty : action.ToString()),
                lblToken.Text,
                ckJSON.Checked ? "1" : "0",
                String.IsNullOrEmpty(txtCallBack.Text) ? string.Empty : "&callback=" + txtCallBack.Text);
        }
    }

    protected void btnGetResource_Click(object sender, EventArgs e)
    {
        ToSession();
        StringBuilder sb = new StringBuilder(ResourcePath);

        switch (SelectedAction)
        {
            case OAuthServiceID.addFlight:
                sb.AppendFormat(CultureInfo.InvariantCulture, "&flight={0}&format={1}", HttpUtility.UrlEncode(txtFlightToAdd.Text), cmbFlightFormat.SelectedValue);
                break;
            case OAuthServiceID.FlightsWithQueryAndOffset:
                // These are post-only
                break;
            case OAuthServiceID.currency:
            case OAuthServiceID.FlightPathForFlight:
            case OAuthServiceID.FlightPathForFlightGPX:
            case OAuthServiceID.DeleteLogbookEntry:
            case OAuthServiceID.PropertiesForFlight:
                sb.AppendFormat(CultureInfo.InvariantCulture, "&idFlight={0}", decFlightID.IntValue);
                break;
            case OAuthServiceID.AddAircraftForUser:
                sb.AppendFormat(CultureInfo.InvariantCulture, "&szTail={0}&idModel={1}&idInstanceType={2}", HttpUtility.UrlEncode(txtTail.Text), decModelID.IntValue, txtInstanceType.Text);
                break;
            case OAuthServiceID.VisitedAirports:
            case OAuthServiceID.AircraftForUser:
            case OAuthServiceID.AvailablePropertyTypesForUser:
            case OAuthServiceID.MakesAndModels:
            case OAuthServiceID.GetNamedQueries:
                // no parameters
                break;
            case OAuthServiceID.totals:
                if (!String.IsNullOrEmpty(txtFlightQuery.Text))
                    sb.AppendFormat(CultureInfo.InvariantCulture, "&fq={0}", HttpUtility.UrlEncode(txtFlightQuery.Text));
                break;
            case OAuthServiceID.none:
                if (!String.IsNullOrEmpty(txtCustomData.Text))
                    sb.Append("&" + txtCustomData.Text);
                break;
        }

        Response.Redirect(sb.ToString());
    }
    protected void btnClearState_Click(object sender, EventArgs e)
    {
        CurrentPageState = null;
        Response.Redirect(Request.Path);
    }

    protected void AddPostParams(NameValueCollection postParams)
    {
        if (postParams == null)
            throw new ArgumentNullException("postParams");

        switch (SelectedAction)
        {
            case OAuthServiceID.addFlight:
                postParams.Add("flight", txtFlightToAdd.Text);
                postParams.Add("format", cmbFlightFormat.SelectedValue);
                break;
            case OAuthServiceID.totals:
                postParams.Add("fq", txtFlightQuery.Text);
                break;
            case OAuthServiceID.FlightPathForFlight:
            case OAuthServiceID.FlightPathForFlightGPX:
            case OAuthServiceID.DeleteLogbookEntry:
            case OAuthServiceID.PropertiesForFlight:
                postParams.Add("idFlight", decFlightID.IntValue.ToString(CultureInfo.InvariantCulture));
                break;
            case OAuthServiceID.AddAircraftForUser:
                postParams.Add("szTail", txtTail.Text);
                postParams.Add("idModel", decModelID.IntValue.ToString(CultureInfo.InvariantCulture));
                postParams.Add("idInstanceType", txtInstanceType.Text);
                break;
            case OAuthServiceID.FlightsWithQueryAndOffset:
                postParams.Add("fq", txtFlightQuery2.Text);
                postParams.Add("offset", decOffset.IntValue.ToString(CultureInfo.InvariantCulture));
                postParams.Add("maxCount", decLimit.IntValue.ToString(CultureInfo.InvariantCulture));
                break;
            case OAuthServiceID.UploadImage:
                postParams.Add("txtComment", txtImgComment.Text);
                decimal lat = decImgLat.Value;
                decimal lon = decImgLon.Value;

                if (lat != 0 || lon != 0)
                {
                    postParams.Add("txtLat", decImgLat.Value.ToString(CultureInfo.InvariantCulture));
                    postParams.Add("txtLon", decImgLon.Value.ToString(CultureInfo.InvariantCulture));
                }
                if (!String.IsNullOrEmpty(txtImageParamName1.Text))
                    postParams.Add(txtImageParamName1.Text, txtImageParam1.Text);
                if (!String.IsNullOrEmpty(txtImageParamName2.Text))
                    postParams.Add(txtImageParamName2.Text, txtImageParam2.Text);
                if (!String.IsNullOrEmpty(txtImageParamName3.Text))
                    postParams.Add(txtImageParamName3.Text, txtImageParam3.Text);
                break;
            case OAuthServiceID.VisitedAirports:
            case OAuthServiceID.AircraftForUser:
            case OAuthServiceID.currency:
            case OAuthServiceID.AvailablePropertyTypesForUser:
            case OAuthServiceID.MakesAndModels:
                // no parameters required here.
                break;
        }
    }

    protected void btnPostResource_Click(object sender, EventArgs e)
    {
        ToSession();
        NameValueCollection postParams = new NameValueCollection()
        {
            { "locale", "en_US" },
        };

        AddPostParams(postParams);

        using (MultipartFormDataContent form = new MultipartFormDataContent())
        {
            // Add each of the parameters
            foreach (string key in postParams.Keys)
            {
                StringContent sc = new StringContent(postParams[key]);
                form.Add(sc);
                sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
            }

                if (fuImage.HasFile)
                {
                    StreamContent sc = new StreamContent(fuImage.FileContent);
                    form.Add(sc, "imgPicture", fuImage.FileName);
                    sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fuImage.PostedFile.ContentType);
                }

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = null;
                string szError = string.Empty;
                try
                {
                    response = httpClient.PostAsync(new Uri(ResourcePath), form).Result;
                    if (!response.IsSuccessStatusCode)
                        szError = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();

                    Page.Response.Clear();
                    Response.ContentType = ckJSON.Checked ? (String.IsNullOrEmpty(txtCallBack.Text) ? "application/json; charset=utf-8" : "application/javascript; charset=utf-8") : "text/xml; charset=utf-8";
                    System.Threading.Tasks.Task.Run(async () => { await response.Content.CopyToAsync(Page.Response.OutputStream); }).Wait();
                    Page.Response.Flush();

                    // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                    Page.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                }
                catch (System.Threading.ThreadAbortException ex)
                {
                    lblErr.Text = ex.Message;
                }
                catch (HttpUnhandledException ex)
                {
                    lblErr.Text = String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szError);
                }
                catch (HttpException ex)
                {
                    lblErr.Text = String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szError);
                }
                catch (HttpRequestException ex)
                {
                    lblErr.Text = String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szError);
                }
                catch (System.Net.WebException ex)
                {
                    lblErr.Text = String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szError);
                }
            }
        }
    }

    protected OAuthServiceID SelectedAction
    {
        get
        {
            OAuthServiceID action;
            if (Enum.TryParse(cmbResourceAction.SelectedValue, out action))
                return action;
            return OAuthServiceID.currency;
        }
    }

    protected void cmbResourceAction_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (SelectedAction)
        {
            case OAuthServiceID.addFlight:
                mvService.SetActiveView(vwAddFlight);
                break;
            case OAuthServiceID.totals:
            case OAuthServiceID.TotalsForUserWithQuery:
                mvService.SetActiveView(vwFlightQuery);
                break;
            case OAuthServiceID.currency:
            case OAuthServiceID.VisitedAirports:
            case OAuthServiceID.AircraftForUser:
            case OAuthServiceID.AvailablePropertyTypesForUser:
            case OAuthServiceID.MakesAndModels:
                mvService.SetActiveView(vwNoParams);
                break;
            case OAuthServiceID.FlightPathForFlight:
            case OAuthServiceID.FlightPathForFlightGPX:
            case OAuthServiceID.DeleteLogbookEntry:
            case OAuthServiceID.PropertiesForFlight:
                mvService.SetActiveView(vwFlightID);
                break;
            case OAuthServiceID.AddAircraftForUser:
                mvService.SetActiveView(vwAddAircraft);
                break;
            case OAuthServiceID.FlightsWithQueryAndOffset:
                mvService.SetActiveView(vwGetFlights);
                break;
            case OAuthServiceID.UploadImage:
                mvService.SetActiveView(vwImage);
                break;
            case OAuthServiceID.none:
            default:
                mvService.SetActiveView(vwCustom);
                break;
        }
    }
}