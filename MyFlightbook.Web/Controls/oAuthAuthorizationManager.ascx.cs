using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_oAuthAuthorizationManager : System.Web.UI.UserControl
{
    /// <summary>
    /// Refreshes the list of granted authorizations and returns true if any were found.
    /// </summary>
    /// <returns></returns>
    public bool Refresh()
    {
        IEnumerable<MFBOauthClientAuth> lstAuths = MFBOauthClientAuth.GrantedAuthsForUser(Page.User.Identity.Name);
        gvOAuthClients.DataSource = lstAuths;
        gvOAuthClients.DataBind();
        return lstAuths.Count() > 0;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void gvOAuthClients_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        MFBOauthClientAuth.RevokeAuthorization(Page.User.Identity.Name, e.Keys[0].ToString());
        Refresh();
    }

    protected void gvOAuthClients_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            MFBOauthClientAuth oauth = (MFBOauthClientAuth)e.Row.DataItem;
            if (oauth.Scope != null)
            {
                IEnumerable<string> lstScopes = MFBOauthServer.ScopeDescriptions(MFBOauthServer.ScopesFromString(oauth.Scope));
                ((MultiView)e.Row.FindControl("mvScopesRequested")).SetActiveView(lstScopes.Count() == 0 ? ((View)e.Row.FindControl("vwNoScopes")) : ((View)e.Row.FindControl("vwRequestedScopes")));
                Repeater rpt = (Repeater)e.Row.FindControl("rptPermissions");
                rpt.DataSource = lstScopes;
                rpt.DataBind();
            }
        }
    }
}