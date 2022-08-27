using MyFlightbook.CSV;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public static class MFBConstants
    {
        public const string keyLite = "Lite";   // Show a lightweight version of the site?
        public const string keyClassic = "Classic"; // persistant cookie - show a full version of the site even on mobile devices?
        public const string keyIsImpersonating = "IsImpersonating";
        public const string keyOriginalID = "OriginalID";
        public const string keyNewUser = "IsNewUser";
        public const string keyEncryptMyFlights = "MyFlightsKey";
        public const string keyCookiePrivacy = "cookiesAccepted";
        public const string keyTFASettings = "prefTFASettings"; // any 2-factor authentication settings.
        public const string keyDecimalSettings = "prefDecimalDisplay";  // adaptive, single, or double digit precision
        public const string keyMedicalNotes = "prefMedicalNotes";   // any notes on your medical
        public const string keyCoreFieldsPermutation = "prefCoreFields";    // permutation of the core fields
        public const string keyWindowAircraftMaintenance = "prefMaintenanceWindow"; // default window for showing/hiding aircraft maintenance
        public const int DefaultMaintenanceWindow = 90;
        public const string keyPrefFlatHierarchy = "UsesFlatCloudStorageFileHierarchy";    // indicates that cloud storage should be done in a flat hierarchy rather than by month.
        private const int StyleSheetVer = 35;

        public static string BaseStylesheet
        {
            get { return String.Format(CultureInfo.InvariantCulture, "~/Public/stylesheet.css?v={0}", StyleSheetVer); }
        }
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
            IsShunted = (ConfigurationManager.AppSettings[keyShuntState].CompareOrdinalIgnoreCase("Shunted") == 0);
        }

        /// <summary>
        /// The branded message to display when shunted
        /// </summary>
        public static string ShuntMessage
        {
            get { return Branding.ReBrand(ConfigurationManager.AppSettings[keyShuntMsg], Branding.CurrentBrand); }
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

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    /// <summary>
    /// Encapsulates the shared httpclient
    /// </summary>
    public static class SharedHttpClient
    {
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// GetResponseForAuthenticatedUri - assumes a POST
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, HttpMethod.Post, null, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="content">HttpContent to post (REQUIRES POST/PUT)</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpContent content, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, HttpMethod.Post, content, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="method">The http method to use (GET or POST)</param>
        public static async Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpMethod method, Func<HttpResponseMessage, object> OnResult)
        {
            return await GetResponseForAuthenticatedUri(uri, szAuth, method, null, OnResult);
        }

        /// <summary>
        /// GetResponseForAuthenticatedUri
        /// </summary>
        /// <param name="uri">The target URI</param>
        /// <param name="szAuth">Bearer token</param>
        /// <param name="OnResult">Action called on result</param>
        /// <param name="content">HttpContent to post (REQUIRES POST/PUT)</param>
        /// <param name="method">The http method to use (GET or POST)</param>
        /// <param name="dictHeaders">Any additional request headers</param>
        public async static Task<object> GetResponseForAuthenticatedUri(Uri uri, string szAuth, HttpMethod method, HttpContent content, Func<HttpResponseMessage, object> OnResult, IDictionary<string, string> dictHeaders = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (OnResult == null)
                throw new ArgumentNullException(nameof(OnResult));
            if (content != null && method == HttpMethod.Get)
                throw new InvalidOperationException("Cannot do http GET with content passed.");

            object result = null;
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(method, uri))
            {
                if (!String.IsNullOrEmpty(szAuth))
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", szAuth);

                if (dictHeaders != null)
                {
                    foreach (string szKey in dictHeaders.Keys)
                        requestMessage.Headers.Add(szKey, dictHeaders[szKey]);
                }

                if (content != null)
                    requestMessage.Content = content;

                using (HttpResponseMessage response = await _client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    result = OnResult(response);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Utility Class - contains a few commonly used/needed functions
    /// </summary>
    public static class util
    {
        private const string sessCultureKey = "currCulture";

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
                    CultureInfo ciRequested = new CultureInfo(szCulture);
                    // Some locale's like Slovak have multi-character date separators, and this kills masked edit extender.
                    // hack workaround is to just use the first character of the date separator.
                    if (ciRequested.DateTimeFormat.DateSeparator.Length > 1)
                        ciRequested.DateTimeFormat.DateSeparator = ciRequested.DateTimeFormat.DateSeparator.Substring(0, 1);
                    System.Threading.Thread.CurrentThread.CurrentCulture = ciRequested;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = ciRequested;
                }
                catch (Exception ex) when (ex is CultureNotFoundException)
                {
                    try
                    {
                        // iPhone sends fucked up strings like "en-GB-GB" for UK-English.  So try removing the middle string
                        string[] rgsz = szCulture.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (rgsz.Length == 3)
                            SetCulture(String.Format(CultureInfo.InvariantCulture, "{0}-{1}", rgsz[0], rgsz[2]));
                    }
                    catch (Exception ex2) when (ex2 is CultureNotFoundException || ex2 is ArgumentNullException) { }
                }

                // Culture isn't passed up to Ajax calls, so store it in the session in case any ajax call needs it
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                    HttpContext.Current.Session[sessCultureKey] = System.Threading.Thread.CurrentThread.CurrentCulture;
            }
        }

        /// <summary>
        /// Return the culture that is squirreled away in the session.  Generally unnecessary as CultureInfo.CurrentCulture works, but in Ajax calls, that doesn't get passed up.
        /// CAN RETURN NULL - in which case go ahead and use CultureInfo.CurrentCulture
        /// </summary>
        public static CultureInfo SessionCulture
        {
            get { return HttpContext.Current != null && HttpContext.Current.Session != null ? (CultureInfo)HttpContext.Current.Session[sessCultureKey] : null; }
        }

        /// <summary>
        /// Switches to/from mobile state (overriding default detection) by setting the appropriate session variables.
        /// </summary>
        /// <param name="fMobile">True for the mobile state</param>
        public static void SetMobile(Boolean fMobile)
        {
            if (fMobile)
            {
                HttpContext.Current.Response.Cookies[MFBConstants.keyClassic].Value = null; // let autodetect do its thing next time...
                HttpContext.Current.Request.Cookies[MFBConstants.keyClassic].Value = null;
                HttpContext.Current.Session[MFBConstants.keyLite] = Boolean.TrueString; // ...but keep it lite for the session
            }
            else
            {
                HttpContext.Current.Response.Cookies[MFBConstants.keyClassic].Value = "yes"; // override autodetect
                HttpContext.Current.Request.Cookies[MFBConstants.keyClassic].Value = "yes";
                HttpContext.Current.Session[MFBConstants.keyLite] = null; // and hence there should be no need for a session variable.
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
                throw new ArgumentNullException(nameof(ctlRoot));
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
        /// HttpRequestBase variant of GetStringParam
        /// </summary>
        /// <param name="req"></param>
        /// <param name="szKey"></param>
        /// <returns></returns>
        static public string GetStringParam(HttpRequestBase req, string szKey)
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
                if (int.TryParse(req[szKey], NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    return i;

                return defaultValue;
            }
        }
                
        /// <summary>
        /// HttpRequestBase variant of GetIntParam
        /// </summary>
        /// <param name="req"></param>
        /// <param name="szKey"></param>
        /// <returns></returns>
        static public int GetIntParam(HttpRequestBase req, string szKey, int defaultValue)
        {
            if (req == null || req[szKey] == null)
                return defaultValue;
            else
            {
                if (int.TryParse(req[szKey], NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    return i;

                return defaultValue;
            }
        }

        /// <summary>
        /// Creates a CSV string from the contents of a gridveiw
        /// </summary>
        /// <param name="gv">The databound gridview</param>
        /// <returns>A CSV string representing the data</returns>
        public static void ToCSV(this GridView gv, Stream s)
        {
            if (gv == null)
                throw new ArgumentNullException(nameof(gv));
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            using (DataTable dt = new DataTable())
            {
                dt.Locale = CultureInfo.CurrentCulture;
                if (gv.Rows.Count == 0 || gv.HeaderRow == null || gv.HeaderRow.Cells == null || gv.HeaderRow.Cells.Count == 0)
                    return;

                // add the header columns from the gridview
                foreach (TableCell tc in gv.HeaderRow.Cells)
                    dt.Columns.Add(new DataColumn(tc.Text));

                foreach (GridViewRow gvr in gv.Rows)
                {
                    DataRow dr = dt.NewRow();

                    for (int j = 0; j < gvr.Cells.Count; j++)
                    {
                        string szCell = gvr.Cells[j].Text;
                        if (szCell.Length == 0 && gvr.Cells[j].Controls.Count > 0)
                        {
                            Control c;
                            // Look for a label or a hyperlink
                            if (gvr.Cells[j].Controls.Count > 1)
                            {
                                c = gvr.Cells[j].Controls[1];
                                if (c is Label l)
                                    szCell = l.Text;
                            }
                            else
                            {
                                c = gvr.Cells[j].Controls[0];
                                if (c is HyperLink h)
                                    szCell = h.Text;
                                else if (c is Label l)
                                    szCell = l.Text;
                            }
                        }
                        szCell = HttpUtility.HtmlDecode(szCell).Trim().Replace('\uFFFD', ' ');

                        dr[j] = szCell;
                    }

                    dt.Rows.Add(dr);
                }

                if (dt != null)
                {
                    using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8, 1024, true /* leave the stream open */))
                    {
                        CsvWriter.WriteToStream(sw, dt, true, true);
                    }
                }
            }
        }

        /// <summary>
        /// Given a set of objects, slices them up into smaller lists grouping by the values in the specified property name.
        /// </summary>
        /// <param name="lst">The set of objects to slice</param>
        /// <param name="PropertyName">The name of the property - obviously must exist</param>
        /// <returns>An IDictionary grouped by names of the properties</returns>
        public static IDictionary<string, IEnumerable<T>> GroupByProperty<T>(IEnumerable<T> lst, string PropertyName)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            if (PropertyName == null)
                throw new ArgumentNullException(nameof(PropertyName));

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
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            using (SmtpClient smtpClient = new SmtpClient())
            {
                if (!smtpClient.Host.Contains("local"))
                    smtpClient.EnableSsl = true;
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

                if (msg.IsBodyHtml && msg.AlternateViews.Count == 0)
                {
                    string szHTML = msg.Body;
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(msg.Body);
                    StringBuilder sb = new StringBuilder();
                    foreach (HtmlAgilityPack.HtmlNode node in doc.DocumentNode.SelectNodes("//text()"))
                    {
                        string szLine = node.InnerText.Trim();
                        if (!String.IsNullOrEmpty(szLine))
                            sb.AppendLine(node.InnerText.Trim());

                        if (node.ParentNode.OriginalName.CompareCurrentCultureIgnoreCase("a") == 0 && node.ParentNode.Attributes["href"] != null)
                            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}{2}", Resources.LocalizedText.LocalizedSpace, node.ParentNode.Attributes["href"].Value, Resources.LocalizedText.LocalizedSpace);
                    }

                    msg.Body = sb.ToString();
                    msg.IsBodyHtml = false; // we're now sending plain text with an html alternate
                    msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(szHTML, new System.Net.Mime.ContentType("text/html")));
                }

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
                throw new ArgumentNullException(nameof(msg));
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
            if (brand == null)
                brand = Branding.CurrentBrand;

            if (szMessage == null)
                throw new ArgumentNullException(nameof(szMessage));

            new System.Threading.Thread(() =>
            {
                try
                {
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
                catch (ArgumentNullException) { }
                catch (InvalidOperationException) { }
                catch (Exception ex) when (ex is SmtpException) { }
            }).Start();
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
        /// <param name="ex">The exception</param>
        static public void NotifyAdminException(string szInfo, Exception ex)
        {
            if (ex == null)
                return;

            string szErr = ex.PrettyPrint(szInfo);

            // Reduce viewstate spam if it's just a viewstate error.
            if (szErr.Contains("Failed to load viewstate") || szErr.Contains("Invalid viewstate"))
                return;

            NotifyAdminEvent("Error on the myflightbook site", szErr, ProfileRoles.maskSiteAdminOnly);
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
                throw new ArgumentNullException(nameof(dr));
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
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is NullReferenceException)
            {
                return defObj;
            }
        }

        static public string ReadNullableString(MySqlDataReader dr, string key)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            object o = dr[key];
            if (o == null || o == System.DBNull.Value)
                return string.Empty;
            else
                return o.ToString();
        }
        #endregion

        public const string keyColumn = "ColumnKey";

        /// <summary>
        /// Retrieves completions for a specified prefix, using the given database query.  MUST have a parameter named "?prefix" and MUST have a result column called "ColumnKey" (=util.keyColumn)
        /// </summary>
        /// <param name="szQ">The database query</param>
        /// <param name="prefixText">The prefix</param>
        /// <returns>An array of results with the specified prefix</returns>
        public static string[] GetKeysFromDB(string szQ, string prefixText)
        {
            if (String.IsNullOrEmpty(szQ) || prefixText == null)
                return Array.Empty<string>();

            List<string> al = new List<string>();
            DBHelper dbo = new DBHelper(szQ);

            if (dbo.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("prefix", prefixText); },
                (dr) => { al.Add(dr[keyColumn].ToString()); }))
                return al.ToArray();
            return Array.Empty<string>();
        }
    }

    // Hack from http://stackoverflow.com/questions/976524/issues-rendering-usercontrol-using-server-execute-in-an-asmx-web-service
    // This lets us load up a control without having an actual page environment and get its HTML
    // Important for infinite scroll, which must produce HTML without a page context.
    public class FormlessPage : Page
    {
        public override void VerifyRenderingInServerForm(Control control)
        {
        }

        public static string RenderControlsToHTML(Action<FormlessPage> addControlsToPage, Action<HtmlTextWriter> renderToStream)
        {
            if (addControlsToPage == null)
                throw new ArgumentNullException(nameof(addControlsToPage));
            if (renderToStream == null)
                throw new ArgumentNullException(nameof(renderToStream));

            // We have no Page, so things like Page_Load don't get called.
            // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
            using (FormlessPage p = new FormlessPage())
            {
                p.Controls.Add(new System.Web.UI.HtmlControls.HtmlForm());
                using (StringWriter sw1 = new StringWriter(CultureInfo.CurrentCulture))
                    HttpContext.Current.Server.Execute(p, sw1, false);

                // Add the controls to the page
                addControlsToPage(p);

                // Now, write it out.
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb, CultureInfo.CurrentCulture))
                {
                    using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                    {
                        try
                        {
                            renderToStream(htmlTW);
                            return sb.ToString();
                        }
                        catch (ArgumentException ex) when (ex is ArgumentOutOfRangeException) { } // don't write bogus or incomplete HTML
                    }
                }
            }

            return string.Empty;
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
        void ToStream(string szUser, Stream s);
    }
}