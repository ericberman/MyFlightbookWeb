using System;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class MiniRecents : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                this.Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, System.Web.HttpUtility.HtmlEncode(Profile.GetUser(User.Identity.Name).PreferredGreeting));
        }
    }
}