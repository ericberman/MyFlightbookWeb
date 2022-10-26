using MyFlightbook.Printing;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing
{
    public partial class PrintFooter : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            int page = util.GetIntParam(Request, "page", 1);
            int pageCount = util.GetIntParam(Request, "topage", 1);

            string szOptions = Request.PathInfo.Length > 1 ? Request.PathInfo.Substring(1) : string.Empty;
            bool fHasCover = PDFOptions.CoverFromEncodedOptions(szOptions);
            bool fHasTotal = PDFOptions.TotalPagesFromEncodedOptions(szOptions);

            // If we have a cover page, start numbering on the page AFTER the cover.
            if (fHasCover)
            {
                page--;
                pageCount--;
            }

            lblPage.Text = fHasTotal ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCountWithTotals, page, pageCount) : String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, page);
            lblShowModified.Visible = PDFOptions.ShowChangeTrack(szOptions);
            tblFooter.Visible = page > 0; // don't show the footer on the cover page.
        }
    }
}