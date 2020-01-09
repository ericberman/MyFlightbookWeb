using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Net;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class OAuth_oAuthToken : System.Web.UI.Page
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Member_oAuthToken.#Page_Load(System.Object,System.EventArgs)")]
    protected void Page_Load(object sender, EventArgs e)
    {
        try
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
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
            }
            else
            {
                using (OAuthServiceCall service = new OAuthServiceCall(Request))
                {
                    Response.Clear();
                    Response.ContentType = service.ContentType;
                    service.Execute(Response.OutputStream);
                    HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                    HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                }
            }
        }
        catch (Exception ex)
        {
            Response.Clear();
            Response.ContentType = "text/plain";
            Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Write("Error: " + ex.Message + "\r\n");
            Response.Write(ex.ToStringDescriptive() + "\r\n");
            Response.Flush();
            Response.End();
        }
    }
}