using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class AllMakes : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // This page is for search engines by default so if authenticated, go to the makes page
            Response.Redirect(Page.User.Identity.IsAuthenticated && String.IsNullOrEmpty(Request.PathInfo) ? "~/Member/Makes.aspx" : "~/mvc/AllMakes" + Request.PathInfo, true);
        }
    }
}