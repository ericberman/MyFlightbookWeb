using System;
using System.Web;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public class AdminPage : System.Web.UI.Page
    {
        protected void CheckAdmin(bool fAllowed)
        {
            if (!fAllowed)
            {
                util.NotifyAdminEvent("Attempt to view admin page", String.Format(CultureInfo.CurrentCulture, "User {0} tried to hit the admin page.", Page.User.Identity.Name), ProfileRoles.maskSiteAdminOnly);
                Response.Redirect("~/HTTP403.htm");
            }
        }

        /// <summary>
        /// Removes everything from the runtime cache.
        /// </summary>
        protected static void FlushCache()
        {
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
                HttpRuntime.Cache.Remove((string)entry.Key);
            GC.Collect();
        }
    }
}