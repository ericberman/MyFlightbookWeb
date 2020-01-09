using MyFlightbook;
using System;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        HtmlForm mainform = (HtmlForm)Master.FindControl("form1");
        if (mainform != null && mfbSignIn1.DefButtonUniqueID.Length > 0)
            mainform.DefaultButton = mfbSignIn1.DefButtonUniqueID;

        Master.Title = locHeader.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
    }
}
