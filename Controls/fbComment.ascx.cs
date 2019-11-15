using System;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_fbComment : System.Web.UI.UserControl
{
    private Uri m_szURI = null;

    /// <summary>
    /// The ABSOLUTE URI to use.  If it is empty, the current page's URL will be used
    /// </summary>
    public Uri URI 
    {
        get { return m_szURI; }
        set
        {
            m_szURI = value;
            litCommentBox.Text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "<fb:comments href=\"{0}\"{1}></fb:comments>",
            (value == null) ? Request.Url.OriginalString : value.AbsoluteUri,
            NumberOfPosts > 0 ? String.Format(System.Globalization.CultureInfo.InvariantCulture, " num_posts=\"{0}\"", NumberOfPosts) : String.Empty);
        }
    }

    /// <summary>
    /// Number of posts to show.  If 0, uses the default.
    /// </summary>
    public int NumberOfPosts { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        string szComment = String.Format(System.Globalization.CultureInfo.InvariantCulture, @"    (function (d, s, id) {{
        var js, fjs = d.getElementsByTagName(s)[0];
        if (d.getElementById(id)) return;
        js = d.createElement(s); js.id = id;
        js.src = ""//connect.facebook.net/en_US/sdk.js#xfbml=1&version=v2.9&appId={0}"";
        fjs.parentNode.insertBefore(js, fjs);
    }}
    (document, 'script', 'facebook-jssdk'));", MyFlightbook.LocalConfig.SettingForKey("facebookAppId"));

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "fbComment", szComment, true);
    }
}