using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.PublicPages
{
    public partial class PrintFooter : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string szPage = util.GetStringParam(Request, "page");
            int page = Convert.ToInt32(szPage, CultureInfo.InvariantCulture);
            bool fHasCover = Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/Cover", StringComparison.OrdinalIgnoreCase);

            // If we have a cover page, start numbering on the page AFTER the cover.
            if (fHasCover)
                page--;

            lblPage.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, HttpUtility.HtmlEncode(page));
            tblFooter.Visible = page > 0; // don't show the footer on the cover page.
        }
    }
}