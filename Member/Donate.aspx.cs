using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Donate : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        lnkStyleRef.Href = ResolveUrl("~/Public/Stylesheet.css");
        cssBranded.Href = Page.ResolveUrl(Branding.CurrentBrand.StyleSheet);
    }
}