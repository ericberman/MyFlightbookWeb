using System;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class OldLogbookPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // This page is obsolete - just redirect
            if (!IsPostBack)
                Response.Redirect("~/Member/LogbookNew.aspx");
        }
    }
}