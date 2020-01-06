using System;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_MiniRecents : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            this.Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFirstName);

    }
}