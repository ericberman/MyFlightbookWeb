using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class DefaultMini : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Keep it mobile for this session!
        this.Master.SetMobile(true);
        if (User.Identity.IsAuthenticated)
            this.Title = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultTitle, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
        else
            this.Title = Branding.CurrentBrand.AppName;
    }
}
