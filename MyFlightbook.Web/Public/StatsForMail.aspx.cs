using MyFlightbook;
using MyFlightbook.Encryptors;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2012-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_StatsForMail : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            cssRef.Href = "~/Public/Stylesheet.css?v=19".ToAbsoluteURL(Request.Url.Scheme, Branding.CurrentBrand.HostName, Request.Url.Port).ToString();
            baseRef.Attributes["href"] = "~/Public/".ToAbsoluteURL(Request.Url.Scheme, Branding.CurrentBrand.HostName, Request.Url.Port).ToString();
            string szAuthKey = util.GetStringParam(Request, "k");

            // This page is public, so that it doesn't require any authentication, making it easy to set up a scheduled task.
            // SO, we do the following:
            // If you request the page from ANOTHER machine, we return an error
            // If you request it from THIS machine, then we perform a very simple authentication (pass an encrypted datetime) to ourselves.
            // If we receive this request with a valid encrypted key, we return the stats.
            if (String.IsNullOrEmpty(szAuthKey))
            {
                // see if this is coming from the local machine
                string szIPThis = System.Net.Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
                if (String.Compare(Request.UserHostAddress, szIPThis, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    // request came from this machine - make a request to ourselves and send it out in email
                    AdminAuthEncryptor enc = new AdminAuthEncryptor();
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        string szURL = String.Format(CultureInfo.InvariantCulture, "{0}?k={1}", "~/public/StatsForMail.aspx".ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host, Request.Url.Port), HttpUtility.UrlEncode(enc.Encrypt(DateTime.Now.ToString("s", CultureInfo.InvariantCulture))));
                        byte[] rgdata = wc.DownloadData(szURL);
                        util.NotifyAdminEvent(String.Format(CultureInfo.CurrentCulture, "{0} site stats as of {1} {2}", Request.Url.Host, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()), System.Text.UTF8Encoding.UTF8.GetString(rgdata).Trim(), ProfileRoles.maskCanReport);
                        lblSuccess.Visible = true;
                    }
                }
                else
                {
                    lblErr.Visible = true;
                }
            }
            else
            {
                AdminAuthEncryptor enc = new AdminAuthEncryptor();
                string szDate = enc.Decrypt(szAuthKey);
                DateTime dt = DateTime.Parse(szDate, CultureInfo.InvariantCulture);
                double elapsedSeconds = DateTime.Now.Subtract(dt).TotalSeconds;
                if (elapsedSeconds < 0 || elapsedSeconds > 60)
                    throw new MyFlightbookException("Unauthorized attempt to view stats for mail");

                // If we're here, then the auth was successfully sent - show the admin panel!
                Page.Title = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SiteStatsTemplate, Request.Url.Host);
                adminStats1.Visible = true;
            }
        }
    }
}