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
                string szIPThis = System.Net.Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
                if (Request.IsLocal && String.Compare(Request.UserHostAddress, szIPThis, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        string szURL = "~/mvc/Admin/Stats?fForEmail=true&naked=1".ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host, Request.Url.Port).ToString();
                        byte[] rgdata = wc.DownloadData(szURL);
                        util.NotifyAdminEvent(String.Format(CultureInfo.CurrentCulture, "{0} site stats as of {1} {2}", Request.Url.Host, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()), System.Text.UTF8Encoding.UTF8.GetString(rgdata).Trim(), ProfileRoles.maskCanReport);
                    }
                }
            }
        }
    }
}