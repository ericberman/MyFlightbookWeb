using MyFlightbook.Clubs;
using System;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    public partial class ViewUser : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string szUser = (Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/", StringComparison.OrdinalIgnoreCase)) ? Request.PathInfo.Substring(1) : string.Empty;

            const string szDefault = "~/Public/tabimages/ProfileTab.png";
            string szReturn = szDefault;
            if (!String.IsNullOrEmpty(szUser) && Page.User.Identity.IsAuthenticated)
            {
                // Two scenarios where we're allowed to see the image:
                // (a) we're the user or 
                // (b) we are both in the same club.
                // ClubMember.ShareClub returns true for both of these.
                if (ClubMember.CheckUsersShareClub(Page.User.Identity.Name, szUser))
                {
                    Profile pf = Profile.GetUser(szUser);
                    if (pf.HasHeadShot)
                    {
                        Response.Clear();
                        Response.ContentType = "image/jpeg";
                        Response.Cache.SetExpires(DateTime.UtcNow.AddDays(14));
#pragma warning disable CA3002 // Review code for XSS vulnerabilities - this came from the database, it's not arbitrary.
                        Response.BinaryWrite(pf.HeadShot.ToArray());
#pragma warning restore CA3002 // Review code for XSS vulnerabilities
                        Response.Flush();
                        Response.End();
                    }
                }
            }

            Response.Redirect(szReturn);
        }
    }
}