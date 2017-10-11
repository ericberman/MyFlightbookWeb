using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Net.Http;
using System.Web.UI;
using DotNetOpenAuth.OAuth2;
using OAuthAuthorizationServer.Code;

/******************************************************
 * 
 * Copyright (c) 2015-2016 MyFlightbook LLC
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
        get { return (PageState)Session["oAuthClientPageState"]; }
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
        PageState ps = new PageState();

        ps.AuthURL = txtAuthURL.Text;
        ps.TokenURL = txtTokenURL.Text;
        ps.ResourceURL = txtResourceURL.Text;
        ps.ClientID = txtClientID.Text;
        ps.ClientSecret = txtClientSecret.Text;
        ps.RedirectURL = txtRedirectURL.Text;
        ps.Scope = txtScope.Text;
        ps.State = txtState.Text;
        ps.Authorization = lblAuthorization.Text;
        ps.Token = lblToken.Text;
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
            if (!Page.User.Identity.IsAuthenticated || !MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanDoAllAdmin)
                Response.Redirect("~/Default.aspx");

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

            List<KeyValuePair<string, string>> lst = new List<KeyValuePair<string,string>>();
            foreach (string szKey in Request.Params.Keys)
                lst.Add(new KeyValuePair<string, string>(szKey, Request.Params[szKey]));
            gvResults.DataSource = lst;
            gvResults.DataBind();
        }
    }

    protected AuthorizationServerDescription Description()
    {
        AuthorizationServerDescription desc = new AuthorizationServerDescription();
        desc.AuthorizationEndpoint = new Uri(txtAuthURL.Text);
        desc.ProtocolVersion = ProtocolVersion.V20;
        desc.TokenEndpoint = new Uri(txtTokenURL.Text);
        return desc;
    }

    protected WebServerClient Client()
    {
        WebServerClient client = new WebServerClient(Description(), txtClientID.Text, txtClientSecret.Text);
        return client;
    }

    protected void btnGetAuth_Click(object sender, EventArgs e)
    {
        ToSession();
        WebServerClient client = Client();
        client.RequestUserAuthorization(Scopes, RedirURL);
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
        ToSession();
        WebServerClient consumer = new WebServerClient(Description(), CurrentPageState.ClientID, CurrentPageState.ClientSecret);
        consumer.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(CurrentPageState.ClientSecret);
        IAuthorizationState grantedAccess = consumer.ProcessUserAuthorization(new HttpRequestWrapper(Request));

        lblToken.Text = grantedAccess.AccessToken;
    }

    protected string ResourcePath
    {
        get
        {
            MFBOauthServer.MFBOAuthScope scope = SelectedScope;

            return String.Format(CultureInfo.InvariantCulture, "{0}{1}?access_token={2}&json={3}",
                txtResourceURL.Text,
                scope == MFBOauthServer.MFBOAuthScope.none ? txtCustomVerb.Text : scope.ToString(),
                lblToken.Text,
                ckJSON.Checked ? "1" : "0");
        }
    }

    protected void btnGetResrouce_Click(object sender, EventArgs e)
    {
        ToSession();
        StringBuilder sb = new StringBuilder(ResourcePath);

        switch (SelectedScope)
        {
            case MFBOauthServer.MFBOAuthScope.addflight:
                sb.AppendFormat(CultureInfo.InvariantCulture, "&flight={0}&format={1}", HttpUtility.UrlEncode(txtFlightToAdd.Text), cmbFlightFormat.SelectedValue);
                break;
            case MFBOauthServer.MFBOAuthScope.currency:
                // These are post-only
                break;
            case MFBOauthServer.MFBOAuthScope.totals:
                if (!String.IsNullOrEmpty(txtFlightQuery.Text))
                    sb.AppendFormat(CultureInfo.InvariantCulture, "&q={0}", HttpUtility.UrlEncode(txtFlightQuery.Text));
                break;
            case MFBOauthServer.MFBOAuthScope.none:
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification ="FXCop is reporting a false positive for stringcontent below")]
    protected void btnPostResource_Click(object sender, EventArgs e)
    {
        ToSession();
        NameValueCollection postParams = new NameValueCollection()
               {
                   { "locale", "en_US" },
               };

        switch (SelectedScope)
        {
            case MFBOauthServer.MFBOAuthScope.addflight:
                postParams.Add("flight", txtFlightToAdd.Text);
                postParams.Add("format", cmbFlightFormat.SelectedValue);
                break;
            case MFBOauthServer.MFBOAuthScope.currency:
                break;
            case MFBOauthServer.MFBOAuthScope.totals:
                postParams.Add("q", txtFlightQuery.Text);
                break;
            case MFBOauthServer.MFBOAuthScope.none:
                break;
        }

        using (MultipartFormDataContent form = new MultipartFormDataContent())
        {
            // Add each of the parameters
            foreach (string key in postParams.Keys)
            {
                StringContent sc = new StringContent(postParams[key]);
                form.Add(sc);
                sc.Headers.ContentDisposition = (new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = key });
            }

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = httpClient.PostAsync(new Uri(ResourcePath), form).Result;
                    response.EnsureSuccessStatusCode();

                    Page.Response.Clear();
                    System.Threading.Tasks.Task.Run(async () => { await response.Content.CopyToAsync(Page.Response.OutputStream); }).Wait();
                    Page.Response.Flush();

                    // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                    Page.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                }
                catch (System.Threading.ThreadAbortException)
                {
                }
                catch (HttpUnhandledException)
                {
                }
                catch (HttpException)
                {
                }
                catch (System.Net.WebException)
                {
                }
            }
        }
    }

    protected MFBOauthServer.MFBOAuthScope SelectedScope
    {
        get
        {
            MFBOauthServer.MFBOAuthScope scope = MFBOauthServer.MFBOAuthScope.none;
            if (Enum.TryParse(cmbResourceAction.SelectedValue, out scope))
                return scope;
            return MFBOauthServer.MFBOAuthScope.none;
        }
    }

    protected void cmbResourceAction_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (SelectedScope)
        {
            case MFBOauthServer.MFBOAuthScope.addflight:
                mvService.SetActiveView(vwAddFlight);
                break;
            case MFBOauthServer.MFBOAuthScope.currency:
                mvService.SetActiveView(vwCurrency);
                break;
            case MFBOauthServer.MFBOAuthScope.totals:
                mvService.SetActiveView(vwTotals);
                break;
            case MFBOauthServer.MFBOAuthScope.none:
            default:
                mvService.SetActiveView(vwCustom);
                break;
        }
    }
}