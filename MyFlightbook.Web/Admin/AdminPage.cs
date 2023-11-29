using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020-2023 MyFlightbook LLC
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
    }
}