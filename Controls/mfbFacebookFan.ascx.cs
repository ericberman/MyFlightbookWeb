using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFacebookFan : System.Web.UI.UserControl
{
    public Boolean ShowStream { get; set; }


    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            pnlFacebook.Visible = !String.IsNullOrEmpty(Branding.CurrentBrand.FacebookFeed);
    }
}
