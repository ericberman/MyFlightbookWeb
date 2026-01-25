using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web.UI;
using static MyFlightbook.Image.MFBImageInfoBase;


/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.HtmlUtility
{
    public static class HtmlUtility
    {
        public static void ToHtml(this MFBImageInfo mfbii, HtmlTextWriter tw, string szThumbFolder)
        {
            if (tw == null)
                throw new ArgumentNullException(nameof(tw));
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));

            if (szThumbFolder == null)
                szThumbFolder = "thumbs/";

            if (!szThumbFolder.EndsWith("/", StringComparison.Ordinal))
                szThumbFolder += "/";

            tw.AddStyleAttribute(HtmlTextWriterStyle.Display, "inline-block");
            tw.AddStyleAttribute(HtmlTextWriterStyle.Padding, "3px");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute(HtmlTextWriterAttribute.Href, mfbii.ResolveFullImage());
            tw.RenderBeginTag(HtmlTextWriterTag.A);

            tw.AddAttribute(HtmlTextWriterAttribute.Src, (mfbii.ImageType == ImageFileType.PDF || mfbii.ImageType == ImageFileType.S3PDF) ? String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, "~/images/pdficon_large.png".ToAbsolute()) : szThumbFolder + mfbii.ThumbnailFile);
            tw.RenderBeginTag(HtmlTextWriterTag.Img);
            tw.RenderEndTag();  // img
            tw.RenderEndTag();  // a

            if (!String.IsNullOrEmpty(mfbii.Comment))
            {
                tw.WriteBreak();
                tw.RenderBeginTag(HtmlTextWriterTag.P);
                tw.WriteEncodedText(mfbii.Comment);
                tw.RenderEndTag();  // p
            }

            tw.RenderEndTag();  // div
        }
    }
}