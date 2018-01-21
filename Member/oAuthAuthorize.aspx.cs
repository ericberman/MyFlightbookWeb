using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using DotNetOpenAuth.OAuth2.Messages;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Secure_oAuthAuthorize : System.Web.UI.Page
{
    private readonly AuthorizationServer authorizationServer = new AuthorizationServer(new OAuth2AuthorizationServer());
    private EndUserAuthorizationRequest m_pendingRequest;
    private const string szVSKeyPendingRequest = "pendingRequest";

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            if (!Request.IsSecureConnection)
                throw new HttpException((int)HttpStatusCode.Forbidden, Resources.LocalizedText.oAuthErrNotSecure);

            if (!IsPostBack)
            {
                if ((m_pendingRequest = this.authorizationServer.ReadAuthorizationRequest()) == null)
                    throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrMissingRequest);

                IEnumerable<string> lstScopes = MFBOauthServer.ScopeDescriptions(MFBOauthServer.ScopesFromStrings(m_pendingRequest.Scope));
                mvScopesRequested.SetActiveView(lstScopes.Count() == 0 ? vwNoScopes : vwRequestedScopes);
                rptPermissions.DataSource = lstScopes;
                rptPermissions.DataBind();

                ViewState[szVSKeyPendingRequest] = m_pendingRequest;

                lblClientName.Text = ((MFBOauth2Client)authorizationServer.AuthorizationServerServices.GetClient(m_pendingRequest.ClientIdentifier)).ClientName;
            }
            else
                m_pendingRequest = (EndUserAuthorizationRequest)ViewState[szVSKeyPendingRequest];
        }
        catch (HttpException ex)
        {
            RejectWithError(ex.Message);
        }
    }

    protected void RejectWithError(string szError)
    {
        EndUserAuthorizationFailedResponse resp = authorizationServer.PrepareRejectAuthorizationRequest(m_pendingRequest);
        resp.Error = szError;
        OutgoingWebResponse wr = authorizationServer.Channel.PrepareResponse(resp);
        wr.Send();
    }

    protected void btnYes_Click(object sender, EventArgs e)
    {
        if (m_pendingRequest == null)
            throw new HttpException((int)HttpStatusCode.BadRequest, Resources.LocalizedText.oAuthErrMissingRequest);

        MFBOauth2Client client = (MFBOauth2Client) authorizationServer.AuthorizationServerServices.GetClient(m_pendingRequest.ClientIdentifier);
        MFBOauthClientAuth ca = new MFBOauthClientAuth { Scope = OAuthUtilities.JoinScopes(m_pendingRequest.Scope), ClientId = m_pendingRequest.ClientIdentifier, UserId = Page.User.Identity.Name, ExpirationDateUtc = DateTime.UtcNow.AddDays(14) };
        if (ca.fCommit())
        {
            EndUserAuthorizationSuccessResponseBase resp = authorizationServer.PrepareApproveAuthorizationRequest(m_pendingRequest, Page.User.Identity.Name);
            OutgoingWebResponse wr = authorizationServer.Channel.PrepareResponse(resp);
            wr.Send();
        }
        else
            RejectWithError(Resources.LocalizedText.oAuthErrCreationFailed);
    }

    protected void btnNo_Click(object sender, EventArgs e)
    {
        RejectWithError(Resources.LocalizedText.oAuthErrNotAuthorized);
    }
}