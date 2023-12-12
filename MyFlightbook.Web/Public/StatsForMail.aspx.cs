using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2012-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class StatsForMail : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // This page is public, so that it doesn't require any authentication, making it easy to set up a scheduled task.
                // SO, we ONLY allow it from the local machine.
                if (Request.IsLocal)
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        string szURL = "~/mvc/Admin/Stats?fForEmail=true&naked=1".ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host, Request.Url.Port).ToString();
                        byte[] rgdata = wc.DownloadData(szURL);
                        util.NotifyAdminEvent(String.Format(CultureInfo.CurrentCulture, "{0} site stats as of {1} {2}", Request.Url.Host, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()), System.Text.UTF8Encoding.UTF8.GetString(rgdata).Trim(), ProfileRoles.maskCanReport);
                        Response.Write("Success");
                        Response.Flush();
                        Response.End();
                    }
                }
                else
                    throw new UnauthorizedAccessException(String.Format(CultureInfo.CurrentCulture, "Attempt to hit StatsForMail from other than local machine.  Source: {0}", Request.UserHostAddress));
            }
        }
    }
}