using System;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Public
{
    public partial class About : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lnkFeatures.Text = Branding.ReBrand(Resources.LocalizedText.AboutViewFeatures);
                lnkFacebook.Visible = !String.IsNullOrEmpty(lnkFacebook.NavigateUrl = Branding.CurrentBrand.FacebookFeed);
                lnkTwitter.Visible = !String.IsNullOrEmpty(lnkTwitter.NavigateUrl = Branding.CurrentBrand.TwitterFeed);
                lblFollowFacebook.Text = Branding.ReBrand(Resources.LocalizedText.FollowOnFacebook);
                lblFollowTwitter.Text = Branding.ReBrand(Resources.LocalizedText.FollowOnTwitter);
            }
        }
    }
}