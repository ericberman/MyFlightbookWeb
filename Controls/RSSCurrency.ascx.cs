using System;
using System.Web;
using System.Web.UI;
using MyFlightbook;
using MyFlightbook.Encryptors;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_RSSCurrency : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        SharedDataEncryptor ec = new SharedDataEncryptor("mfb");
        string szEncrypted = ec.Encrypt(Page.User.Identity.Name);
        lnkGeneric.NavigateUrl = String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://{0}{1}?uid={2}", Branding.CurrentBrand.HostName, ResolveUrl("~/Public/RSSCurrency.aspx"), HttpUtility.UrlEncode(szEncrypted));
    }
}
