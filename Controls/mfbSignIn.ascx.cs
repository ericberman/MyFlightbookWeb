using MyFlightbook;
using System;
using System.Web.Security;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSignIn : System.Web.UI.UserControl
{
    public string DefButtonUniqueID
    {
        get
        {
            Button btn = (Button)ctlSignIn.FindControl("LoginButton");
            return btn == null ? string.Empty : btn.UniqueID;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ctlSignIn.Focus();
        if (!IsPostBack)
        {
            HyperLink hl = (HyperLink) ctlSignIn.FindControl("CreateUserLink");
            hl.NavigateUrl = hl.NavigateUrl + Request.Url.Query;
        }
    }

    protected void OnLoggingIn(object sender, LoginCancelEventArgs e)
    {
        TextBox txtEmail = (TextBox)ctlSignIn.FindControl("txtEmail");
        TextBox txtUser = (TextBox)ctlSignIn.FindControl("UserName");

        if (txtEmail == null)
            throw new MyFlightbookException("No email field");
        if (txtUser == null)
            throw new MyFlightbookException("No username field");

        // Set the Username field based on the email address provided.
        string szUser = Membership.GetUserNameByEmail(txtEmail.Text);

        txtUser.Text = szUser;
    }
}
