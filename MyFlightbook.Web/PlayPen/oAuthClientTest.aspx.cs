using DotNetOpenAuth.OAuth2;
using Newtonsoft.Json;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_oAuthClientTest : System.Web.UI.Page
{
    [Serializable]
    protected class PageState
    {
        public Uri AuthURL { get; set; }
        public Uri TokenURL { get; set; }
        public Uri ResourceURL { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public Uri RedirectURL { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }
        public string Authorization { get; set; }
        public IAuthorizationState AuthState { get; set; }
    }

    protected PageState CurrentPageState
    {
        get
        {
            try
            {
                return (PageState)Session["oAuthClientPageState"];
            }
            catch (Exception ex) when (ex is InvalidCastException)
            {
                return null;
            }
        }
        set { Session["oAuthClientPageState"] = value; }
    }

    protected void SetTokenDisplay(IAuthorizationState state)
    {
        lblToken.Text = state == null ? string.Empty : JsonConvert.SerializeObject(state, Formatting.Indented);
    }

    protected void FromSession()
    {
        PageState ps = CurrentPageState;
        if (ps == null)
            return;

        txtAuthURL.Text = ps.AuthURL.ToString();
        txtTokenURL.Text = ps.TokenURL.ToString();
        txtResourceURL.Text = ps.ResourceURL.ToString();
        txtClientID.Text = ps.ClientID;
        txtClientSecret.Text = ps.ClientSecret;
        txtRedirectURL.Text = ps.RedirectURL.ToString();
        txtScope.Text = ps.Scope;
        txtState.Text = ps.State;
        lblAuthorization.Text = ps.Authorization;
        SetTokenDisplay(ps.AuthState);
    }

    protected void ToSession()
    {
        PageState ps = new PageState()
        {
            AuthURL = new Uri(txtAuthURL.Text),
            TokenURL = new Uri(txtTokenURL.Text),
            ResourceURL = new Uri(txtResourceURL.Text),
            ClientID = txtClientID.Text,
            ClientSecret = txtClientSecret.Text,
            RedirectURL = new Uri(txtRedirectURL.Text),
            Scope = txtScope.Text,
            State = txtState.Text,
            Authorization = lblAuthorization.Text,
            AuthState = JsonConvert.DeserializeObject<AuthorizationState>(lblToken.Text)
        };
        CurrentPageState = ps;
    }

    private readonly string[] scopesDelimiter = new string[] { " " };
    protected IEnumerable<string> Scopes
    {
        get { return txtScope.Text.Split(scopesDelimiter, StringSplitOptions.RemoveEmptyEntries); }
    }

    protected Uri RedirURL
    {
        get { return String.IsNullOrEmpty(txtRedirectURL.Text) ? null : new Uri(txtRedirectURL.Text); }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            FromSession();
            if (Request["code"] != null)
                lblAuthorization.Text = HttpUtility.HtmlEncode((string) Request["code"]);
            if (Request["state"] != null)
                txtState.Text = HttpUtility.HtmlEncode((string) Request["state"]);
            if (Request["access_token"] != null)
                SetTokenDisplay(JsonConvert.DeserializeObject<IAuthorizationState>((string) Request["access_token"]));

            Uri uriBase = new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}", Request.Url.Host));
            txtAuthURL.Text = HttpUtility.HtmlEncode(new Uri(uriBase, VirtualPathUtility.ToAbsolute("~/member/oAuthAuthorize.aspx")).ToString());
            txtRedirectURL.Text = HttpUtility.HtmlEncode(new Uri(uriBase, VirtualPathUtility.ToAbsolute("~/playpen/oAuthClientTest.aspx")).ToString());
            txtResourceURL.Text = HttpUtility.HtmlEncode(new Uri(uriBase, VirtualPathUtility.ToAbsolute("~/OAuth/oAuthToken.aspx")).ToString());
            txtTokenURL.Text = HttpUtility.HtmlEncode(new Uri(uriBase, VirtualPathUtility.ToAbsolute("~/OAuth/oAuthToken.aspx")).ToString());
            txtImgUploadURL.Text = HttpUtility.HtmlEncode(new Uri(uriBase, VirtualPathUtility.ToAbsolute("~/public/UploadPicture.aspx")).ToString());

            List<KeyValuePair<string, string>> lst = new List<KeyValuePair<string,string>>();
            foreach (string szKey in Request.Params.Keys)
                lst.Add(new KeyValuePair<string, string>(szKey, Request.Params[szKey]));
            gvResults.DataSource = lst;
            gvResults.DataBind();

            if (Request["error"] != null)
                lblErr.Text = HttpUtility.HtmlEncode((string) Request["error"]);
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

    protected static void AppendQueryString(string szKey, string szVal, StringBuilder sb)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));
        if (sb.Length > 0)
            sb.Append('&');
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
            IAuthorizationState grantedAccess = consumer.ProcessUserAuthorization(new HttpRequestWrapper(Request)) ?? throw new MyFlightbook.MyFlightbookValidationException("Null access token returned - invalid authorization passed?");
            SetTokenDisplay(grantedAccess);
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

            return String.Format(CultureInfo.InvariantCulture, "{0}/{1}?access_token={2}&json={3}{4}",
                action == OAuthServiceID.UploadImage ? txtImgUploadURL.Text : txtResourceURL.Text,
                action == OAuthServiceID.none ? txtCustomVerb.Text : (action == OAuthServiceID.UploadImage ? string.Empty : action.ToString()),
                CurrentPageState.AuthState.AccessToken,
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
            case OAuthServiceID.CreatePendingFlight:
            case OAuthServiceID.UpdatePendingFlight:
            case OAuthServiceID.CommitPendingFlight:
                // These are post-only
                break;
            case OAuthServiceID.DeletePendingFlight:
                sb.AppendFormat(CultureInfo.InvariantCulture, "&idpending={0}", txtPendingID.Text);
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
            case OAuthServiceID.PendingFlightsForUser:
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
            throw new ArgumentNullException(nameof(postParams));

        switch (SelectedAction)
        {
            case OAuthServiceID.CommitFlightWithOptions:
            case OAuthServiceID.addFlight:
                AddFlightParams(postParams);
                break;
            case OAuthServiceID.CreatePendingFlight:
                postParams.Add("le", txtFlightToAdd.Text);
                break;
            case OAuthServiceID.UpdatePendingFlight:
            case OAuthServiceID.CommitPendingFlight:
                postParams.Add("pf", txtFlightToAdd.Text);
                break;
            case OAuthServiceID.DeletePendingFlight:
                postParams.Add("idpending", txtPendingID.Text);
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
                AddImageParams(postParams);
                break;
            case OAuthServiceID.VisitedAirports:
            case OAuthServiceID.AircraftForUser:
            case OAuthServiceID.currency:
            case OAuthServiceID.AvailablePropertyTypesForUser:
            case OAuthServiceID.MakesAndModels:
            case OAuthServiceID.PendingFlightsForUser:
                // no parameters required here.
                break;
        }
    }

    private void AddFlightParams(NameValueCollection postParams)
    {
        if (cmbFlightFormat.SelectedIndex == 0)
        {
            postParams.Add("po", "{}");
            postParams.Add("le", txtFlightToAdd.Text);
        }
        else
            postParams.Add("flight", txtFlightToAdd.Text);
        postParams.Add("format", cmbFlightFormat.SelectedValue);
    }

    private void AddImageParams(NameValueCollection postParams)
    {
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
    }

    protected async Task PostForm(HttpContent form)
    {
        string error = (string) await MyFlightbook.SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(ResourcePath), null, form, (response) => {
            string szError = string.Empty;
            try
            {
                if (!response.IsSuccessStatusCode)
                    szError = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();

                Page.Response.Clear();
                Response.ContentType = ckJSON.Checked ? (String.IsNullOrEmpty(txtCallBack.Text) ? "application/json; charset=utf-8" : "application/javascript; charset=utf-8") : "text/xml; charset=utf-8";
                response.Content.CopyToAsync(Page.Response.OutputStream).Wait();
                Page.Response.Flush();

                // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                Page.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.

                return szError;
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                return ex.Message;
            }
            catch (Exception ex) when (ex is HttpUnhandledException || ex is HttpException || ex is HttpRequestException || ex is System.Net.WebException)
            {
                return String.Format(CultureInfo.InvariantCulture, "{0} --> {1}", ex.Message, szError);
            }
        });
        lblErr.Text = error;
    }

    protected async void btnPostResource_Click(object sender, EventArgs e)
    {
        ToSession();
        NameValueCollection postParams = new NameValueCollection()
        {
            { "locale", "en_US" },
        };

        AddPostParams(postParams);

        List<IDisposable> objectsToDispose = new List<IDisposable>();

        using (MultipartFormDataContent form = new MultipartFormDataContent())
        {
            try
            {
                // Add each of the parameters
                foreach (string key in postParams.Keys)
                {
                    StringContent sc = new StringContent(postParams[key]);
                    form.Add(sc);
                    sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
                    objectsToDispose.Add(sc);
                }

                if (fuImage.HasFile)
                {
                    StreamContent sc = new StreamContent(fuImage.FileContent);
                    form.Add(sc, "imgPicture", fuImage.FileName);
                    sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fuImage.PostedFile.ContentType);
                    objectsToDispose.Add(sc);
                }

                await PostForm(form).ConfigureAwait(true);
            }
            finally
            {
                foreach (IDisposable disposable in objectsToDispose)
                    disposable.Dispose();
            }
        }
    }

    protected OAuthServiceID SelectedAction
    {
        get
        {
            if (Enum.TryParse(cmbResourceAction.SelectedValue, out OAuthServiceID action))
                return action;
            return OAuthServiceID.currency;
        }
    }

    protected void cmbResourceAction_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (SelectedAction)
        {
            case OAuthServiceID.CommitFlightWithOptions:
            case OAuthServiceID.addFlight:
            case OAuthServiceID.CreatePendingFlight:
            case OAuthServiceID.UpdatePendingFlight:
            case OAuthServiceID.CommitPendingFlight:
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
            case OAuthServiceID.PendingFlightsForUser:
                mvService.SetActiveView(vwNoParams);
                break;
            case OAuthServiceID.FlightPathForFlight:
            case OAuthServiceID.FlightPathForFlightGPX:
            case OAuthServiceID.DeleteLogbookEntry:
            case OAuthServiceID.PropertiesForFlight:
                mvService.SetActiveView(vwFlightID);
                break;
            case OAuthServiceID.DeletePendingFlight:
                mvService.SetActiveView(vwPendingID);
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

    protected void btnRefreshToken_Click(object sender, EventArgs e)
    {
        ToSession();
        Client().RefreshAuthorization(CurrentPageState.AuthState);
        SetTokenDisplay(CurrentPageState.AuthState);
    }
}