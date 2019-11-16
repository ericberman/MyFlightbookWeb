using MyFlightbook;
using MyFlightbook.SocialMedia;
using System;
using System.Web;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbMiniFacebook : System.Web.UI.UserControl
{
    public LogbookEntry m_le = null;

    const string szFBTemplate = "https://www.facebook.com/dialog/feed?app_id={0}&link={1}&name={2}&caption={3}&description={4}&redirect_uri={5}";

    /// <summary>
    /// Specifies the LogbookEntry to share on facebook.
    /// </summary>
    public LogbookEntry FlightEntry
    {
        get { return m_le; }
        set { m_le = value; UpdateLink(); }
    }

    /// <summary>
    /// Indicates if we should give hints to Facebook (using Meta tags) about which image, URL, etc. to use
    /// </summary>
    public bool AddMetaTagHints { get; set; }

    private void AddMeta(string szName, string szContent)
    {
        HtmlMeta m = new HtmlMeta();
        Page.Header.Controls.Add(m);
        m.Attributes["property"] = szName;
        m.Content = szContent;
    }

    protected void UpdateLink()
    {
        if (FlightEntry != null)
        {
            lnkFBAdd.Visible = FlightEntry.CanPost;
            lnkFBAdd.NavigateUrl = "~/member/FBPost.aspx".ToAbsoluteURL(Request).ToString() + "?id=" + FlightEntry.FlightID.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (!AddMetaTagHints)
                return;

            // Set facebook meta tags
            string szDescription = FlightEntry.SocialMediaComment;
            AddMeta("og:title", szDescription);
            AddMeta("og:type", "website");
            AddMeta("og:description", szDescription);
            AddMeta("og:url", Request.Url.AbsoluteUri);
            AddMeta("fb:app_id", MFBFacebook.FACEBOOK_API_KEY);

            MyFlightbook.Image.MFBImageInfo mfbii = FlightEntry.SocialMediaImage();
            if (mfbii != null && mfbii.URLFullImage != null && !String.IsNullOrEmpty(mfbii.URLFullImage.ToAbsoluteURL(Request).ToString()))
            {
                bool fVideo = mfbii.ImageType == MyFlightbook.Image.MFBImageInfo.ImageFileType.S3VideoMP4;

                string absolutePath = mfbii.ResolveFullImage();
                if (fVideo)
                {
                    string absoluteThumb = mfbii.UriS3VideoThumbnail.ToString();
                    AddMeta("og:image", absoluteThumb);
                    AddMeta("og:image:secure_url", absoluteThumb);
                    AddMeta("og:video", absolutePath);
                    AddMeta("og:video:secure_url", absolutePath);
                    AddMeta("og:video:type", "video/mp4");
                }
                else
                {
                    AddMeta("og:image", absolutePath);
                    AddMeta("og:image:secure_url", absolutePath);
                    AddMeta("og:image:type", "image/jpeg");
                }

                double ratio = (mfbii.WidthThumbnail == 0) ? 1.0 : ((double) mfbii.HeightThumbnail / (double)mfbii.WidthThumbnail);

                const int nominalDimension = 400;
                int nominalWidth = (int) ((ratio < 1) ? nominalDimension : nominalDimension / ratio);
                int nominalHeight = (int) ((ratio < 1) ? nominalDimension * ratio : nominalDimension); 
                AddMeta(fVideo ? "og:video:width" : "og:image:width", nominalWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
                AddMeta(fVideo ? "og:video:height" : "og:image:height", nominalHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// Returns the URL to which the user should be redirected to get to facebook
    /// </summary>
    public Uri FBRedirURL
    {
        get
        {
            if (FlightEntry != null)
            {
                string szFlightURL = FlightEntry.SocialMediaItemUri(Request.Url.Host).ToString();

                string szURL = String.Format(System.Globalization.CultureInfo.InvariantCulture, szFBTemplate,
                    MFBFacebook.FACEBOOK_API_KEY,
                    HttpUtility.UrlEncode(szFlightURL),
                    HttpUtility.UrlEncode(FlightEntry.Comment.Length > 0 ? FlightEntry.Comment : (FlightEntry.Route.Length > 0 ? FlightEntry.Route : Resources.LocalizedText.MiniFacebookViewFlight)),
                    HttpUtility.UrlEncode(FlightEntry.Route),
                    HttpUtility.UrlEncode(FlightEntry.Comment),
                    HttpUtility.UrlEncode(szFlightURL));
                return new Uri(szURL);
            }
            return null;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}
