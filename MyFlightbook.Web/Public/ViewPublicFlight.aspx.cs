using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class ViewPublicFlight : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // This page is obsolete - handled in MVC now.  But slightly tricky to preserve the right things, since we supported both
            // ViewPublicFlight.aspx/####?xxxx and ViewPublicFlight.aspx?id=###&xxxx

            // Try getting the id of the flight
            int id = util.GetIntParam(Request, "id", LogbookEntryCore.idFlightNone);

            if (id != -1 || (!String.IsNullOrEmpty(Request.PathInfo) && Int32.TryParse(Request.PathInfo.Substring(1), out id)))
            {
                NameValueCollection nvc = Request.QueryString;
                // Remove the id from the querystring because we're using a rest URL now.
                NameValueCollection nvcNew = HttpUtility.ParseQueryString(string.Empty);
                foreach (string key in nvc.Keys)
                {
                    if (key.CompareCurrentCultureIgnoreCase("id") != 0)
                        nvcNew[key] = nvc[key];
                }

                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/Pub/ViewFlight/{0}?{1}", id, nvcNew.ToString()));
            }
            else
                Response.Redirect("~/mvc/pub");
        }
   }
}