using MyFlightbook.Encryptors;
using System;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class Export : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length > 0)
                    util.SetCulture(HttpContext.Current.Request.UserLanguages[0]);
                else
                {
                    string szRequestedLocale = util.GetStringParam(Request, "loc");
                    if (!String.IsNullOrEmpty(szRequestedLocale))
                        util.SetCulture(szRequestedLocale);
                }

                string szAuth = util.GetStringParam(Request, "auth");
                bool useCSV = util.GetIntParam(Request, "csv", 0) != 0;
                string szUser = util.GetStringParam(Request, "user");
                string szOrder = util.GetStringParam(Request, "Cols");

                string szIPThis = System.Net.Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
                bool isLocal = (String.Compare(Request.UserHostAddress, szIPThis, StringComparison.OrdinalIgnoreCase) == 0);

                if (szUser.Length == 0)
                    return;

                // return csv.  On any error, fall through and return an HTML table.
                if (useCSV && !String.IsNullOrEmpty(szAuth) && (isLocal || Request.IsLocal))
                {
                    AdminAuthEncryptor enc = new AdminAuthEncryptor();
                    string szDate = enc.Decrypt(szAuth);
                    DateTime dt = DateTime.Parse(szDate, CultureInfo.InvariantCulture);
                    double elapsedSeconds = DateTime.Now.Subtract(dt).TotalSeconds;
                    if (elapsedSeconds < 0 || elapsedSeconds > 10)
                        throw new MyFlightbookException("Unauthorized attempt to view export data");

                    if (!String.IsNullOrEmpty(szUser))
                    {
                        mfbDownload1.User = szUser;
                        mfbDownload1.OrderString = szOrder;
                        DownloadCSVForUser();
                        return;
                    }
                }

                string szPass = util.GetStringParam(Request, "pass");

                if (szPass.Length == 0)
                    return;

                if (szUser.Contains("@"))
                    szUser = Membership.GetUserNameByEmail(szUser);

                if (UserEntity.ValidateUser(szUser, szPass).Length > 0)
                {
                    mfbDownload1.User = szUser;
                    mfbDownload1.OrderString = szOrder;
                    if (useCSV)
                        DownloadCSVForUser();
                    else
                        mfbDownload1.UpdateData();
                }
                else
                    return;
            }
        }

        public void DownloadCSVForUser()
        {
            mfbDownload1.UpdateData();
            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            mfbDownload1.ToStream(Response.OutputStream);
            Response.End();
        }
    }
}