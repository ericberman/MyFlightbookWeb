using MyFlightbook.CSV;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class MFBConstants
    {
        public const string keyLite = "Lite";   // Show a lightweight version of the site?
        public const string keyClassic = "Classic"; // persistant cookie - show a full version of the site even on mobile devices?
        public const string keyLastTail = "LastTail"; // the id of the aircraft for the most recently entered flight.
        public const string keyIsImpersonating = "IsImpersonating";
        public const string keyOriginalID = "OriginalID";
        public const string keyNewUser = "IsNewUser";
        public const string keyEncryptMyFlights = "MyFlightsKey";
        public const string keyCookiePrivacy = "cookiesAccepted";
    }

    public static class ShuntState
    {
        private const string keyIsShunted = "IsShunted";
        private const string keyShuntMsg = "ShuntMessage";
        private const string keyShuntState = "ShuntState";

        /// <summary>
        /// True if site is shunted
        /// </summary>
        public static bool IsShunted
        {
            get { return (bool)HttpContext.Current.Application[keyIsShunted]; }
            set { HttpContext.Current.Application[keyIsShunted] = value; }
        }

        /// <summary>
        /// Caches the current shunt state; should only be called on appliction start.
        /// </summary>
        public static void Init()
        {
            IsShunted = (ConfigurationManager.AppSettings[keyShuntState].ToString().CompareOrdinalIgnoreCase("Shunted") == 0);
        }

        /// <summary>
        /// The branded message to display when shunted
        /// </summary>
        public static string ShuntMessage
        {
            get { return Branding.ReBrand(ConfigurationManager.AppSettings[keyShuntMsg].ToString(), Branding.CurrentBrand); }
        }
    }

    /// <summary>
    /// Provides text with an optionally linked string.
    /// </summary>
    [Serializable]
    public class LinkedString
    {
        public string Value { get; set; }
        public string Link { get; set; }

        public LinkedString() { } 

        public LinkedString(string szValue, string szLink)
        {
            Value = szValue;
            Link = szLink;
        }

        public LinkedString(string szValue)
        {
            Value = szValue;
            Link = null;
        }
    }

    /// <summary>
    /// Utility Class - contains a few commonly used/needed functions
    /// </summary>
    public static class util
    {
        /// <summary>
        /// Set the culture for the duration of the request.
        /// </summary>
        /// <param name="szCulture">The user language string (e.g., "en-us")</param>
        public static void SetCulture(string szCulture)
        {
            if (!String.IsNullOrEmpty(szCulture))
            {
                if (szCulture.Length < 3)
                    szCulture = szCulture + "-" + szCulture.ToUpperInvariant();

                try
                {
                    System.Globalization.CultureInfo ciRequested = new System.Globalization.CultureInfo(szCulture);
                    // Some locale's like Slovak have multi-character date separators, and this kills masked edit extender.
                    // hack workaround is to just use the first character of the date separator.
                    if (ciRequested.DateTimeFormat.DateSeparator.Length > 1)
                        ciRequested.DateTimeFormat.DateSeparator = ciRequested.DateTimeFormat.DateSeparator.Substring(0, 1);
                    System.Threading.Thread.CurrentThread.CurrentCulture = ciRequested;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = ciRequested;
                }
                catch (CultureNotFoundException)
                {
                    try
                    {
                        // iPhone sends fucked up strings like "en-GB-GB" for UK-English.  So try removing the middle string
                        string[] rgsz = szCulture.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rgsz.Length == 3)
                            SetCulture(String.Format(CultureInfo.InvariantCulture, "{0}-{1}", rgsz[0], rgsz[2]));
                    }
                    catch (CultureNotFoundException) { }
                    catch (ArgumentNullException) { }
                }
            }
        }

        /// <summary>
        /// Deep copy one object to another using reflection
        /// </summary>
        /// <param name="objSrc">The source object</param>
        /// <param name="objDest">The destination object</param>
        static public void CopyObject(object objSrc, object objDest)
        {
            if (objSrc == null)
                throw new MyFlightbookException("Attempt to copy from null object");
            if (objDest == null)
                throw new MyFlightbookException("Attempt to copy to null object");

            //	Get the type of each object
            Type sourceType = objSrc.GetType();
            Type targetType = objDest.GetType();

            //	Loop through the source properties
            foreach (PropertyInfo p in sourceType.GetProperties())
            {
                //	Get the matching property in the destination object
                PropertyInfo targetObj = targetType.GetProperty(p.Name);
                //	If there is none, skip
                if (targetObj == null)
                    continue;

                //	Set the value in the destination
                if (targetObj.CanWrite)
                    targetObj.SetValue(objDest, p.GetValue(objSrc, null), null);
            }
        }

        /// <summary>
        /// Since iPhone doesn't seem to properly HTML encode objects (or too aggressively does so?), we need to decode objects.
        /// This uses introspection to un-escape the TOP-LEVEL string properties of an object.
        /// </summary>
        /// <param name="o"></param>
        public static void UnescapeObject(object o)
        {
            if (o == null)
                return;

            Type objType = o.GetType();
            foreach (PropertyInfo p in objType.GetProperties())
            {
                if (p.CanRead && p.CanWrite && (p.PropertyType == typeof(string) || p.PropertyType == typeof(String)))
                {
                    object propVal = p.GetValue(o, null);
                    if (propVal != null)
                        p.SetValue(o, ((string)propVal).UnescapeHTML(), null);
                }
            }
        }

        /// <summary>
        /// Recursively set the validation group for a control
        /// </summary>
        /// <param name="ctlRoot">The root control</param>
        /// <param name="szGroup">The desired group</param>
        static public void SetValidationGroup(Control ctlRoot, string szGroup)
        {
            if (ctlRoot == null)
                throw new ArgumentNullException("ctlRoot");
            foreach (Control ctl in ctlRoot.Controls)
            {
                if (ctl.GetType().BaseType == typeof(BaseValidator))
                    ((BaseValidator)ctl).ValidationGroup = szGroup;
                if (ctl.HasControls())
                    SetValidationGroup(ctl, szGroup);
            }
        }

        /// <summary>
        /// Returns a string parameter, even if none was passed
        /// </summary>
        /// <param name="req">The httprequest object</param>
        /// <param name="szKey">The desired parameter</param>
        /// <returns>The string parameter, or ""</returns>
        static public string GetStringParam(HttpRequest req, string szKey)
        {
            if (req == null || req[szKey] == null)
                return string.Empty;
            else
                return req[szKey];
        }

        /// <summary>
        /// Returns an integer parameter, even if none was passed
        /// </summary>
        /// <param name="req">The httprequest object</param>
        /// <param name="szKey">The desired paramter</param>
        /// <param name="defaultValue">The default value if the parameter is null</param>
        /// <returns>The value that was passed or else the default value</returns>
        static public int GetIntParam(HttpRequest req, string szKey, int defaultValue)
        {
            if (req == null || req[szKey] == null)
                return defaultValue;
            else
            {
                int i;
                if (int.TryParse(req[szKey], NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
                    return i;

                return defaultValue;
            }
        }

        /// <summary>
        /// Creates a CSV string from the contents of a gridveiw
        /// </summary>
        /// <param name="gv">The databound gridview</param>
        /// <returns>A CSV string representing the data</returns>
        public static string CSVFromData(this GridView gv)
        {
            if (gv == null)
                throw new ArgumentNullException("gv");
            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;
                if (gv.Rows.Count == 0 || gv.HeaderRow == null || gv.HeaderRow.Cells == null || gv.HeaderRow.Cells.Count == 0)
                    return string.Empty;

                // add the header columns from the gridview
                foreach (TableCell tc in gv.HeaderRow.Cells)
                    dt.Columns.Add(new DataColumn(tc.Text));

                foreach (GridViewRow gvr in gv.Rows)
                {
                    DataRow dr = dt.NewRow();

                    for (int j = 0; j < gvr.Cells.Count; j++)
                    {
                        string szCell = gvr.Cells[j].Text;
                        if (szCell.Length == 0 && gvr.Cells[j].Controls.Count > 1)
                        {
                            Control c = gvr.Cells[j].Controls[1];
                            if (c.GetType() == typeof(Label))
                                szCell = ((Label)c).Text;
                        }
                        szCell = HttpUtility.HtmlDecode(szCell).Trim().Replace('\uFFFD', ' ');

                        dr[j] = szCell;
                    }

                    dt.Rows.Add(dr);
                }

                if (dt != null)
                    return CsvWriter.WriteToString(dt, true, true);
            }

            return string.Empty;
        }

        /// <summary>
        /// Given a set of objects, slices them up into smaller lists grouping by the values in the specified property name.
        /// </summary>
        /// <param name="lst">The set of objects to slice</param>
        /// <param name="PropertyName">The name of the property - obviously must exist</param>
        /// <returns>An IDictionary grouped by names of the properties</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static IDictionary<string, IEnumerable<T>> GroupByProperty<T>(IEnumerable<T> lst, string PropertyName)
        {
            if (lst == null)
                throw new ArgumentNullException("lst");
            if (PropertyName == null)
                throw new ArgumentNullException("PropertyName");

            Dictionary<string, IEnumerable<T>> d = new Dictionary<string, IEnumerable<T>>();

            if (String.IsNullOrEmpty(PropertyName))
            {
                d.Add(PropertyName, new List<T>(lst));
                return d;
            }

            foreach (T o in lst)
            {
                string key = o.GetType().GetProperty(PropertyName).GetValue(o, null).ToString();
                if (!d.ContainsKey(key))
                    d[key] = new List<T>();
                List<T> l = (List<T>)d[key];
                l.Add(o);
            }

            return d;
        }

        #region Admin Email
        /// <summary>
        /// Sends a message, setting enableSSL appropriately
        /// </summary>
        /// <param name="msg"></param>
        static public void SendMessage(MailMessage msg)
        {
            using (SmtpClient smtpClient = new SmtpClient())
            {
                if (!smtpClient.Host.Contains("local"))
                    smtpClient.EnableSsl = true;
                smtpClient.Send(msg);
            }
        }

        /// <summary>
        /// Add admins to the message
        /// </summary>
        /// <param name="msg">The message</param>
        /// <param name="fTo">True to put the admins on the "To" line, false to Bcc them</param>
        /// <param name="RoleMask">The admins that should receive the message</param>
        static public void AddAdminsToMessage(MailMessage msg, bool fTo, uint RoleMask)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            IEnumerable<ProfileBase> lst = ProfileRoles.GetNonUsers();
            foreach (ProfileBase pf in lst)
            {
                if ((RoleMask & (uint)pf.Role) != 0)
                {
                    MailAddress ma = new MailAddress(pf.Email, pf.UserFullName);
                    if (fTo)
                        msg.To.Add(ma);
                    else
                        msg.Bcc.Add(ma);
                }
            }
        }

        /// <summary>
        /// Notify the user of an event, optionally Bcc admins.
        /// </summary>
        /// <param name="szSubject">The subject to send</param>
        /// <param name="szMessage">The message to send  This will be rebranded per Branding.ReBrand().</param>
        /// <param name="maUser">The address of the recipient</param>
        /// <param name="fCcAdmins">True if you want Admins CC'd; false if not</param>
        /// <param name="fIsHTML">True if the content of the message is HTML</param>
        /// <param name="brand">The branding to use</param>
        /// <param name="RoleMask">The roles to whom this should go (if admin)</param>
        static private void NotifyUser(string szSubject, string szMessage, MailAddress maUser, bool fCcAdmins, bool fIsHTML, Brand brand, uint RoleMask)
        {
            try
            {
                if (brand == null)
                    brand = Branding.CurrentBrand;

                using (MailMessage msg = new MailMessage())
                {
                    msg.Subject = szSubject;
                    msg.Body = Branding.ReBrand(szMessage, brand);
                    msg.IsBodyHtml = fIsHTML || szMessage.Contains("<!DOCTYPE");

                    msg.From = new MailAddress(brand.EmailAddress, brand.AppName);

                    if (maUser != null)
                        msg.To.Add(maUser);

                    if (fCcAdmins)
                        AddAdminsToMessage(msg, (maUser == null), RoleMask);

                    SendMessage(msg);
                }
            }
            catch (ArgumentNullException ex)
            {
                MyFlightbookException mfbEx = new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Null Argument in NotifyUser: {0}", ex.ParamName), ex);
                MyFlightbookException.NotifyAdminException(mfbEx);
            }
            catch (InvalidOperationException ex)
            {
                MyFlightbookException mfbEx = new MyFlightbookException("Invalid Operation in NotifyUser", ex);
                MyFlightbookException.NotifyAdminException(mfbEx);
            }
            catch (System.Net.Mail.SmtpException)
            {
                // Don't re-throw or do anything that would cause new mail to be sent because that could loop.  Just fail silently and eat this.
            }
        }

        /// <summary>
        /// Notify the user of an event, optionally Bcc admins.  Uses the current brand
        /// </summary>
        /// <param name="szSubject">The subject to send</param>
        /// <param name="szMessage">The message to send  This will be rebranded per Branding.ReBrand().</param>
        /// <param name="maUser">The address of the recipient</param>
        /// <param name="fCcAdmins">True if you want Admins CC'd; false if not</param>
        /// <param name="fIsHTML">True if the content of the message is HTML</param>
        static public void NotifyUser(string szSubject, string szMessage, MailAddress maUser, bool fCcAdmins, bool fIsHTML)
        {
            NotifyUser(szSubject, szMessage, maUser, fCcAdmins, fIsHTML, Branding.CurrentBrand, fCcAdmins ? ProfileRoles.maskCanSupport : 0);
        }

        /// <summary>
        /// Notify the admin of an event.
        /// </summary>
        /// <param name="szSubject">The subject to send</param>
        /// <param name="szMessage">The message to send</param>
        /// <param name="RoleMask">The roles to whom the notification should go</param>
        static public void NotifyAdminEvent(string szSubject, string szMessage, uint RoleMask)
        {
            NotifyUser(szSubject, szMessage, null, true, false, Branding.CurrentBrand, RoleMask);
        }

        /// <summary>
        /// Notify the site admin of an exception
        /// </summary>
        /// <param name="szInfo">Additional data</param>
        /// <param name="myError">The exception</param>
        static public void NotifyAdminException(string szInfo, Exception myError)
        {
            StringBuilder ErrorMessage = new StringBuilder(szInfo + "\r\n\r\n");
            while (myError != null)
            {
                ErrorMessage.Append("Message\r\n" + myError.Message + "\r\n\r\n");
                ErrorMessage.Append("Source\r\n" + myError.Source + "\r\n\r\n");
                if (myError.TargetSite != null)
                    ErrorMessage.Append("Target site\r\n" + myError.TargetSite.ToString() + "\r\n\r\n");
                ErrorMessage.Append("Stack trace\r\n" + myError.StackTrace + "\r\n\r\n");
                ErrorMessage.Append("Overall Data:\r\n" + myError.ToString() + "\r\n\r\n");
                if (myError.Data != null && myError.Data.Keys != null)
                {
                    foreach (string key in myError.Data.Keys)
                    {
                        if (myError.Data[key] != null)
                            ErrorMessage.AppendFormat(CultureInfo.CurrentCulture, "Data key {0}: {1}", key, myError.Data[key].ToString());
                    }
                }

                ErrorMessage.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Occured at: {0} (UTC)", DateTime.Now.ToUniversalTime().ToString("G", System.Globalization.CultureInfo.InvariantCulture)) + "\r\n\r\n");
                ErrorMessage.Append("Moving to next exception down...\r\n\r\n");

                // Assign the next InnerException
                // to drill down to the lowest level exception
                myError = myError.InnerException;
            }

            string szError = ErrorMessage.ToString();
            if (!szError.Contains("Invalid viewstate"))
                util.NotifyAdminEvent("Error on the myflightbook site" + (szError.Contains("Invalid viewstate") ? " (Viewstate)" : string.Empty), szError, ProfileRoles.maskSiteAdminOnly);
        }
        #endregion

        #region Read nullable MySql fields - should be extension?
        /// <summary>
        /// Reads the specified field from the datareader row, returning the default object in case of an exception.
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="key"></param>
        /// <param name="defObj"></param>
        /// <returns></returns>
        static public object ReadNullableField(MySqlDataReader dr, string key, object defObj)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            if (string.IsNullOrEmpty(key))
                return defObj;
            try
            {
                object o = dr[key];
                if (o == null || o == System.DBNull.Value || o.ToString().Length == 0)
                    return defObj;
                else
                    return o;
            }
            catch (IndexOutOfRangeException)
            {
                return defObj;
            }
            catch (NullReferenceException)
            {
                return defObj;
            }
        }

        static public string ReadNullableString(MySqlDataReader dr, string key)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            object o = dr[key];
            if (o == null || o == System.DBNull.Value)
                return string.Empty;
            else
                return o.ToString();
        }
        #endregion
    }

    // Hack from http://stackoverflow.com/questions/976524/issues-rendering-usercontrol-using-server-execute-in-an-asmx-web-service
    // This lets us load up a control without having an actual page environment and get its HTML
    // Important for infinite scroll, which must produce HTML without a page context.
    public class FormlessPage : Page
    {
        public override void VerifyRenderingInServerForm(Control control)
        {
        }
    }

    /// <summary>
    /// Retrieves data from a control that is rendered on a formless page; format of the data is defined by the control (could be text, html, csv, xml, etc.) - just returns raw bytes.
    /// </summary>
    public interface IDownloadableAsData
    {
        /// <summary>
        /// Refresh however you need to refresh for the given user.
        /// </summary>
        /// <param name="szUser"></param>
        byte[] RawData(string szUser);
    }
}