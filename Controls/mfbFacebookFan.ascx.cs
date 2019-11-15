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
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            pnlFacebook.Visible = !String.IsNullOrEmpty(Branding.CurrentBrand.FacebookFeed);

        if (pnlFacebook.Visible)
        {
            string szScript = String.Format(System.Globalization.CultureInfo.InvariantCulture, @"<script>
  window.fbAsyncInit = function() {{
    FB.init({{
      appId            : '{0}',
      autoLogAppEvents : true,
      xfbml            : true,
      version          : 'v5.0'
    }});
  }};
</script>
<script async defer src=""https://connect.facebook.net/en_US/sdk.js""></script>", LocalConfig.SettingForKey("facebookAppId"));
            Page.ClientScript.RegisterClientScriptBlock(GetType(), "fbFeed", szScript, false);
        }
    }
}
