using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEndorsement : System.Web.UI.UserControl, IEndorsementListUpdate
{
    protected Endorsement endorsement { get; set; }

    /// <summary>
    /// We render explicitly so that writing to a ZIP - which may have neither page context nor HttpContext.Current - can work.
    /// </summary>
    /// <param name="writer"></param>
    public override void RenderControl(HtmlTextWriter writer)
    {
        RenderHTML(endorsement, writer);
    }

    protected void Page_Load(object sender, EventArgs e) { }

    public void SetEndorsement(Endorsement e)
    {
        endorsement = e ?? throw new ArgumentNullException(nameof(e));
    }

    public void RenderHTML(Endorsement e, HtmlTextWriter tw)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (tw == null)
            throw new ArgumentNullException(nameof(tw));

        tw.AddAttribute("style", "padding: 5px;");
        tw.RenderBeginTag(HtmlTextWriterTag.Div);

        tw.AddAttribute("class", "endorsement");
        tw.RenderBeginTag(HtmlTextWriterTag.Table);
        if (e.IsExternalEndorsement)
        {
            // disclaimer
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold; background-color:darkgray; color:white");
            tw.AddAttribute("colspan", "2");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.InnerWriter.Write(Branding.ReBrand(Resources.SignOff.ExternalEndorsementDisclaimer));
            tw.RenderEndTag(); // td
            tw.RenderEndTag(); // tr
        }

        // Body text
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("colspan", "2");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        if (!e.HasDigitizedSig)
        {
            tw.AddAttribute("style", "float:right; margin: 3px;");
            tw.AddAttribute("title", Resources.SignOff.EndorsementValid);
            tw.AddAttribute("alt", Resources.SignOff.EndorsementValid);
            tw.RenderBeginTag(HtmlTextWriterTag.Div);
            tw.AddAttribute("src", String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/images/sigok.png")));
            tw.RenderBeginTag(HtmlTextWriterTag.Img);
            tw.RenderEndTag();  // img
            tw.RenderEndTag();  // div
        }

        // Text and FAR row
        tw.AddAttribute("style", "font-weight:bold;");
        tw.RenderBeginTag(HtmlTextWriterTag.Div);
        tw.InnerWriter.Write(e.FullTitleAndFar);
        tw.RenderEndTag();  // div

        tw.RenderBeginTag(HtmlTextWriterTag.Hr);
        tw.RenderEndTag();  // hr

        tw.InnerWriter.Write(e.EndorsementText);

        tw.RenderBeginTag(HtmlTextWriterTag.Hr);
        tw.RenderEndTag();  // hr

        tw.RenderEndTag(); // td
        tw.RenderEndTag(); // row

        // Date row
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("style", "font-weight: bold;");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(Resources.SignOff.EditEndorsementDatePrompt);
        tw.RenderEndTag();  // td
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(e.Date.ToShortDateString());
        tw.RenderEndTag();  // td
        tw.RenderEndTag();  // trw

        // if dates are off row
        if (e.CreationDate.Date.CompareTo(e.Date.Date) != 0)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold;");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.InnerWriter.Write(Resources.SignOff.EditEndorsementDateCreatedPrompt);
            tw.RenderEndTag();  // td

            if (e.CreationDate.Date.Subtract(e.Date).Days > 10)
                tw.AddAttribute("style", "font-weight: bold;");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.InnerWriter.Write(e.CreationDate.ToShortDateString());
            tw.InnerWriter.Write(" (UTC)");
            tw.RenderEndTag();  // td

            tw.RenderEndTag();  // tr
        }

        // Student name row
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("style", "font-weight: bold");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(Resources.SignOff.EditEndorsementStudentPrompt);
        tw.RenderEndTag();  // td
        
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(e.StudentDisplayName);
        tw.RenderEndTag();  // td
        tw.RenderEndTag();  // tr

        // CFI Display nam row
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("style", "font-weight: bold");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(Resources.SignOff.EditEndorsementInstructorPrompt);
        tw.RenderEndTag();  // td
        
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(e.CFIDisplayName);
        tw.RenderEndTag();  // td
        tw.RenderEndTag();  // tr

        // CFI Certificate Row
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("style", "font-weight: bold");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(Resources.SignOff.EditEndorsementCFIPrompt);
        tw.RenderEndTag();  // td

        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(e.CFICertificate);
        tw.RenderEndTag();  // td

        tw.RenderEndTag();  // tr

        // CFI Certificate Expiration Row
        tw.RenderBeginTag(HtmlTextWriterTag.Tr);
        tw.AddAttribute("style", "font-weight: bold");
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(Resources.SignOff.EditEndorsementExpirationPrompt);
        tw.RenderEndTag();  // td
        tw.RenderBeginTag(HtmlTextWriterTag.Td);
        tw.InnerWriter.Write(e.CFIExpirationDate.HasValue() ? e.CFIExpirationDate.ToShortDateString() : Resources.SignOff.EndorsementNoDate);
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
}