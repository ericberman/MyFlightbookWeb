using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Net;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_oAuthToken : System.Web.UI.Page
{
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
            using (OAuthServiceCall service = new OAuthServiceCall(Request))
            {
                Response.Clear();
                Response.ContentType = service.ContentType;
                service.Execute(Response.OutputStream);
                Response.End();
            }
        }
    }
}