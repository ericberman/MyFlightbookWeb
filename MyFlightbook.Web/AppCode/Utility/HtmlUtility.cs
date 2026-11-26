using MyFlightbook.Image;
using MyFlightbook.Instruction;
using System;
using System.Globalization;
using System.IO;
using System.Web;
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

            tw.AddAttribute("class", "dib");
            tw.AddStyleAttribute(HtmlTextWriterStyle.Padding, "3px");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            // Link to the full image via ViewPic (signed, for non-public images), absolute since this is typically viewed from a local file.
            tw.AddAttribute(HtmlTextWriterAttribute.Href, String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, mfbii.URLFullImage));
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

        /// <summary>
        /// Renders the endorsement to HTML.  We render explicitly so that writing to a ZIP - which may have neither page context nor HttpContext.Current - can work.
        /// </summary>
        /// <param name="tw"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RenderHTML(this Endorsement e, HtmlTextWriter tw)
        {
            if (tw == null)
                throw new ArgumentNullException(nameof(tw));
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.IsImageEndorsement)
            {
                RenderImageEndorsementHTML(e, tw);
                return;
            }

            tw.AddAttribute("class", "pHem");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute("class", "endorsement");
            tw.RenderBeginTag(HtmlTextWriterTag.Table);
            if (e.IsExternalEndorsement)
            {
                // disclaimer
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("class", "endorsementHeaderDisclaimer");
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(Branding.ReBrand(Resources.SignOff.ExternalEndorsementDisclaimer));
                tw.RenderEndTag(); // td
                tw.RenderEndTag(); // tr
            }

            if (e.DateDeleted != null)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("class", "endorsementHeaderDeleted");
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EndorsementDeleted, e.DateDeleted.Value));
                tw.RenderEndTag();  // td
                tw.RenderEndTag();  // tr
            }

            // Body text
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("colspan", "2");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            if (!e.IsAdHocEndorsement)
            {
                tw.AddAttribute("class", "flR mHem");
                tw.AddAttribute("title", Resources.SignOff.EndorsementValid);
                tw.AddAttribute("alt", Resources.SignOff.EndorsementValid);
                tw.RenderBeginTag(HtmlTextWriterTag.Div);
                tw.AddAttribute("src", "~/images/sigok.png".ToAbsoluteBrandedUri().ToString());
                tw.RenderBeginTag(HtmlTextWriterTag.Img);
                tw.RenderEndTag();  // img
                tw.RenderEndTag();  // div
            }

            // Text and FAR row
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);
            tw.Write(HttpUtility.HtmlEncode(e.FullTitleAndFar));
            tw.RenderEndTag();  // div

            tw.RenderBeginTag(HtmlTextWriterTag.Hr);
            tw.RenderEndTag();  // hr

            tw.Write(HttpUtility.HtmlEncode(e.EndorsementText));

            tw.RenderBeginTag(HtmlTextWriterTag.Hr);
            tw.RenderEndTag();  // hr

            tw.RenderEndTag(); // td
            tw.RenderEndTag(); // row

            // Date row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementDatePrompt);
            tw.RenderEndTag();  // td
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(e.Date.ToShortDateString());
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // trw

            // if dates are off row
            if (e.CreationDate.Date.CompareTo(e.Date.Date) != 0)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("class", "b");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(Resources.SignOff.EditEndorsementDateCreatedPrompt);
                tw.RenderEndTag();  // td

                if (e.CreationDate.Date.Subtract(e.Date).Days > 10)
                    tw.AddAttribute("class", "b");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(e.CreationDate.ToShortDateString());
                tw.Write(" (UTC)");
                tw.RenderEndTag();  // td

                tw.RenderEndTag();  // tr
            }

            // Student name row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementStudentPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(e.StudentDisplayName));
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // CFI Display nam row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementInstructorPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(e.CFIDisplayName));
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // CFI Certificate Row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementCFIPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(e.CFICertificate));
            tw.RenderEndTag();  // td

            tw.RenderEndTag();  // tr

            // CFI Certificate Expiration Row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementExpirationPrompt);
            tw.RenderEndTag();  // td
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(e.CFIExpirationDate.HasValue() ? e.CFIExpirationDate.ToShortDateString() : Resources.SignOff.EndorsementNoDate);
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // Digitized Signature row
            if (e.HasDigitizedSig)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.AddAttribute("src", e.DigitizedSigLink);
                tw.RenderBeginTag(HtmlTextWriterTag.Img);
                tw.RenderEndTag();  // td
                tw.RenderEndTag();  // img
                tw.RenderEndTag();  // tr
            }

            tw.RenderEndTag();  // table

            tw.RenderEndTag();  // div
        }

        private static void RenderLabeledRow(HtmlTextWriter tw, string szLabel, string szValue)
        {
            if (String.IsNullOrWhiteSpace(szValue))
                return;
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(szLabel);
            tw.RenderEndTag();  // td
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.WriteEncodedText(szValue);
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr
        }

        private static string AbsoluteBrandedUrl(string szUrl)
        {
            return String.IsNullOrEmpty(szUrl) || szUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? szUrl : String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, szUrl);
        }

        /// <summary>
        /// Renders an image endorsement: a summary of an endorsement or document that exists elsewhere, along with a link to the scan.
        /// Only shows instructor/student rows that have values, and NEVER shows the "valid signature" badge.
        /// </summary>
        private static void RenderImageEndorsementHTML(Endorsement e, HtmlTextWriter tw)
        {
            MFBImageInfo mfbii = e.UnderlyingImage;
            bool fIsPDF = mfbii != null && (mfbii.ImageType == ImageFileType.PDF || mfbii.ImageType == ImageFileType.S3PDF);
            bool fFullImage = mfbii != null && !fIsPDF;

            // Endorsements that show a full image get the endorsementImageContainer class, which lets them grow past the usual 600px
            // (to fit the image, up to 70% of the list's width - see stylesheet.css); the table fills the container and the image scales within it.
            // PDFs (summary + icon) and image-less ones size like any other endorsement.
            tw.AddAttribute("class", $"pHem {(fFullImage ? "endorsementImageContainer" : string.Empty)}");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute("class", "endorsement");
            if (fFullImage)
                tw.AddAttribute("style", "width: 100%");
            tw.RenderBeginTag(HtmlTextWriterTag.Table);

            // disclaimer
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("class", "endorsementHeaderDisclaimer");
            tw.AddAttribute("colspan", "2");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(Branding.ReBrand(Resources.Endorsements.ImageEndorsementDisclaimer)));
            if (e.NeedsReview)
            {
                tw.Write(" ");
                tw.AddAttribute("class", "endorsementHeaderReview");
                tw.AddAttribute("title", Resources.Endorsements.ImageEndorsementNeedsReviewTip);
                tw.RenderBeginTag(HtmlTextWriterTag.Span);
                tw.Write(HttpUtility.HtmlEncode(Resources.Endorsements.ImageEndorsementNeedsReview));
                tw.RenderEndTag();  // span
            }
            tw.RenderEndTag(); // td
            tw.RenderEndTag(); // tr

            // Title/FAR, then body
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("colspan", "2");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.AddAttribute("class", "b");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);
            tw.Write(HttpUtility.HtmlEncode(e.FullTitleAndFar));
            tw.RenderEndTag();  // div
            if (!String.IsNullOrWhiteSpace(e.EndorsementText))
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Hr);
                tw.RenderEndTag();  // hr
                tw.AddAttribute("class", "wsprel");
                tw.RenderBeginTag(HtmlTextWriterTag.Div);
                tw.Write(HttpUtility.HtmlEncode(e.EndorsementText));
                tw.RenderEndTag();  // div
            }
            tw.RenderBeginTag(HtmlTextWriterTag.Hr);
            tw.RenderEndTag();  // hr
            tw.RenderEndTag(); // td
            tw.RenderEndTag(); // tr

            RenderLabeledRow(tw, Resources.SignOff.EditEndorsementDatePrompt, e.Date.ToShortDateString());
            if (e.IsExternalEndorsement)
                RenderLabeledRow(tw, Resources.SignOff.EditEndorsementStudentPrompt, e.StudentName);
            RenderLabeledRow(tw, Resources.SignOff.EditEndorsementInstructorPrompt, e.IsExternalEndorsement ? e.CFIDisplayName : e.CFICachedName);
            RenderLabeledRow(tw, Resources.SignOff.EditEndorsementCFIPrompt, e.CFICertificate);
            if (e.CFIExpirationDate.HasValue())
                RenderLabeledRow(tw, Resources.SignOff.EditEndorsementExpirationPrompt, e.CFIExpirationDate.ToShortDateString());

            // The scan itself
            if (mfbii != null)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.AddAttribute(HtmlTextWriterAttribute.Href, AbsoluteBrandedUrl(mfbii.URLFullImage));
                tw.AddAttribute(HtmlTextWriterAttribute.Target, "_blank");
                tw.RenderBeginTag(HtmlTextWriterTag.A);
                // JPEGs: the full image (the absorbed image IS the endorsement, so print it like one).  PDFs: just the PDF icon, linking to the file.
                tw.AddAttribute(HtmlTextWriterAttribute.Src, AbsoluteBrandedUrl(fIsPDF ? mfbii.URLThumbnail : mfbii.URLFullImage));
                tw.AddAttribute(HtmlTextWriterAttribute.Alt, Resources.Endorsements.ImageEndorsementViewOriginal);
                tw.AddAttribute(HtmlTextWriterAttribute.Title, Resources.Endorsements.ImageEndorsementViewOriginal);
                if (!fIsPDF)
                    tw.AddAttribute("loading", "lazy");
                tw.AddAttribute("class", fIsPDF ? "endorsementImageBodyPDF" : "endorsementImageBodyJPG");
                tw.RenderBeginTag(HtmlTextWriterTag.Img);
                tw.RenderEndTag();  // img
                tw.RenderEndTag();  // a
                if (fIsPDF && e.PageNumber > 0)
                {
                    tw.Write(" ");
                    tw.WriteEncodedText(String.Format(CultureInfo.CurrentCulture, Resources.Endorsements.ImageEndorsementPage, e.PageNumber));
                }
                tw.RenderEndTag(); // td
                tw.RenderEndTag(); // tr
            }

            tw.RenderEndTag();  // table
            tw.RenderEndTag();  // div
        }

        public static string RenderHTML(this Endorsement e)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    e.RenderHTML(tw);
                    return sw.ToString();
                }
            }
        }
    }
}