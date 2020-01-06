using MyFlightbook;
using OAuthAuthorizationServer.Code;
using OAuthAuthorizationServer.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Developer : System.Web.UI.Page
{

    const string szVSClients = "vsownedclients";
    protected IEnumerable<MFBOauth2Client> OwnedClients
    {
        get
        {
            if (!Page.User.Identity.IsAuthenticated)
                return null;

            if (ViewState[szVSClients] == null) 
                ViewState[szVSClients] = MFBOauth2Client.GetClientsForUser(Page.User.Identity.Name);
            return (IEnumerable<MFBOauth2Client>)ViewState[szVSClients];
        }
        set { ViewState[szVSClients] = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Master.SelectedTab = tabID.tabHome;

            if (Page.User.Identity.IsAuthenticated)
            {
                mvServices.SetActiveView(vwAuthenticated);

                foreach (MFBOAuthScope scope in Enum.GetValues(typeof(MFBOAuthScope)))
                    if (scope != MFBOAuthScope.none)
                        cklScopes.Items.Add(new ListItem(MFBOauthServer.ScopeDescription(scope), scope.ToString()));

                gvMyServices.DataSource = (util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport) ? MFBOauth2Client.GetAvailableClients() : OwnedClients;
                gvMyServices.DataBind();
            }
            else
                mvServices.SetActiveView(vwGuest);
        }
    }

    protected void btnAddClient_Click(object sender, EventArgs e)
    {
        Page.Validate("newClient");
        if (Page.IsValid)
        {
            List<string> lst = new List<string>();
            foreach (ListItem li in cklScopes.Items)
                if (li.Selected)
                    lst.Add(li.Value);
            string szScopes = String.Join(" ", lst);
            MFBOauth2Client client = new MFBOauth2Client(txtClient.Text, txtSecret.Text, "https://" + txtCallback.Text, txtName.Text, szScopes, Page.User.Identity.Name);
            try
            {
                MFBOauthServer.ScopesFromString(szScopes);  // will throw an exception for an invalid scope.
                client.Commit();  // will throw any exception.
                OwnedClients = null;    // force a refresh.
                gvMyServices.DataSource = OwnedClients;
                gvMyServices.DataBind();
                txtCallback.Text = txtClient.Text = txtName.Text = txtSecret.Text = string.Empty;
                foreach (ListItem li in cklScopes.Items)
                    li.Selected = false;
                Expando.ExpandoControl.Collapsed = true;

                util.NotifyAdminEvent("oAuth client created", String.Format(CultureInfo.CurrentCulture, "User: {0}, Name: {1}", Page.User.Identity.Name, client.ClientName), ProfileRoles.maskCanReport);
            }
            catch (UnauthorizedAccessException ex)
            {
                lblErr.Text = ex.Message;
            }
            catch (MyFlightbookValidationException ex)
            {
                lblErr.Text = ex.Message;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }

    protected void gvMyServices_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        gvMyServices.EditIndex = e.NewEditIndex;
        gvMyServices.DataSource = OwnedClients;
        gvMyServices.DataBind();
    }

    protected void gvMyServices_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        gvMyServices.EditIndex = -1;
        gvMyServices.DataSource = OwnedClients;
        gvMyServices.DataBind();
    }

    protected void gvMyServices_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        MFBOauth2Client client = new List<MFBOauth2Client>(OwnedClients)[e.RowIndex];
        client.ClientSecret = (string) e.NewValues["ClientSecret"];
        client.ClientName = (string)e.NewValues["ClientName"];
        client.Callback = (string)e.NewValues["Callback"];
        client.Scope = (string)e.NewValues["Scope"];
        try
        {
            MFBOauthServer.ScopesFromString(client.Scope);  // will throw an exception for an invalid scope.
            client.Commit();
            gvMyServices.EditIndex = -1;
            OwnedClients = null;    // force a refresh.
            gvMyServices.DataSource = OwnedClients;
            gvMyServices.DataBind();
        }
        catch (UnauthorizedAccessException ex)
        {
            lblErrGV.Text = ex.Message;
        }
        catch (MyFlightbookValidationException ex)
        {
            lblErrGV.Text = ex.Message;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            lblErrGV.Text = ex.Message;
        }
    }

    protected void gvMyServices_RowCommand(object sender, CommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            MFBOauth2Client.DeleteForUser(e.CommandArgument.ToString(), Page.User.Identity.Name);
            OwnedClients = null;
            gvMyServices.DataSource = OwnedClients;
            gvMyServices.DataBind();
        }
    }
}