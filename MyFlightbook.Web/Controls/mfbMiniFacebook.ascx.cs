using System;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2009-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.SocialMedia
{
    public partial class mfbMiniFacebook : System.Web.UI.UserControl
    {
        private LogbookEntry m_le;

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

                    double ratio = (mfbii.WidthThumbnail == 0) ? 1.0 : ((double)mfbii.HeightThumbnail / (double)mfbii.WidthThumbnail);

                    const int nominalDimension = 400;
                    int nominalWidth = (int)((ratio < 1) ? nominalDimension : nominalDimension / ratio);
                    int nominalHeight = (int)((ratio < 1) ? nominalDimension * ratio : nominalDimension);
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
            get { return FlightEntry == null ? null : new Uri(String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://facebook.com/sharer.php?display=page&u={0}", System.Web.HttpUtility.UrlEncode(FlightEntry.SocialMediaItemUri(Branding.CurrentBrand.HostName).ToString()))); }
        }
    }
}