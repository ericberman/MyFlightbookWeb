using System;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Xml;
using MyFlightbook;
using MyFlightbook.Encryptors;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_RSSCurrency : System.Web.UI.Page
{
    protected bool IsTotals { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        string szUser = "";
        string szDebug = "";

        if (Request.Params["uid"] != null)
        {
            string szUid = Request.Params["uid"].ToString().Replace(" ", "+");
            SharedDataEncryptor ec = new SharedDataEncryptor("mfb");
            szUser = ec.Decrypt(szUid);
            szDebug += "original uid=" + Request.Params["uid"].ToString() + " fixed szUid=" + szUid + " and szUser=" + szUser + " and timestamp = " + DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString();
        }
        else if (User.Identity.IsAuthenticated)
        {
            szUser = User.Identity.Name;
            szDebug += "Using cached credentials...";
        }

        IsTotals = (util.GetIntParam(Request, "t", 0) != 0);
        mvData.SetActiveView(IsTotals ? vwTotals : vwCurrency);

        string szDescription;
        if (szUser.Length > 0)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb, System.Globalization.CultureInfo.CurrentCulture))
            {
                HtmlTextWriter htmlTW = new HtmlTextWriter(sw);
                if (IsTotals)
                {
                    mfbTotalSummary.Username = szUser;
                    mfbTotalSummary.CustomRestriction = new FlightQuery(szUser);
                    mfbTotalSummary.RenderControl(htmlTW);
                }
                else
                {
                    mfbCurrency1.UserName = szUser;
                    mfbCurrency1.RefreshCurrencyTable();
                    mfbCurrency1.RenderControl(htmlTW);
                }
            }
            szDescription = sb.ToString();

            if (!String.IsNullOrEmpty(Request.QueryString["HTML"]))
                return;
        }
        else
        {
            szDescription = Resources.Currency.RSSNoUser;
        }

        using (XmlTextWriter xmltw = new XmlTextWriter(Response.OutputStream, System.Text.Encoding.UTF8))
        {
            Profile pf = MyFlightbook.Profile.GetUser(szUser);

            szDescription += "\r\n<!-- " + szDebug + "-->";

            if (!String.IsNullOrEmpty(Request.QueryString["Google"]))
                WrapGoogleRSS(xmltw, pf.UserFullName, szDescription);
            else
                WrapGenericRSS(xmltw, pf.UserFullName, szDescription);

            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.ContentType = "text/xml";
            xmltw.Flush();
        }
        Response.End();
    }

    public override void VerifyRenderingInServerForm(Control control)
    {
        //base.VerifyRenderingInServerForm(control);
    }

    private void WrapGenericRSS(XmlTextWriter xmltw, string szUser, string szBody)
    {
        xmltw.WriteStartElement("rss");
        xmltw.WriteAttributeString("version", "2.0");
        xmltw.WriteStartElement("channel");
        xmltw.WriteElementString("title", String.Format(System.Globalization.CultureInfo.CurrentCulture, IsTotals ? Resources.Currency.RSSTitleTotals : Resources.Currency.RSSTitle, Branding.CurrentBrand.AppName));
        xmltw.WriteElementString("link", String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://{0}", Request.Url.Host));
        xmltw.WriteElementString("ttl", "2");

        xmltw.WriteStartElement("item");
        xmltw.WriteElementString("title", String.Format(System.Globalization.CultureInfo.CurrentCulture, IsTotals ? Resources.Currency.RSSHeaderTotals : Resources.Currency.RSSHeader, szUser, DateTime.Now.ToShortDateString()));
        xmltw.WriteStartElement("description");
        xmltw.WriteCData(szBody);
        xmltw.WriteEndElement(); // Description.

        xmltw.WriteEndElement(); // item

        xmltw.WriteEndElement(); // channel
        xmltw.WriteEndElement(); // rss
    }

    private void WrapGoogleRSS(XmlTextWriter xmltw, string szUser, string szBody)
    {
        xmltw.WriteStartDocument();
        xmltw.WriteStartElement("module");
        xmltw.WriteStartElement("ModulePrefs");
        xmltw.WriteAttributeString("title", String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Currency.RSSHeader, szUser, DateTime.Now.ToShortDateString()));
        xmltw.WriteAttributeString("title_url", String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://{0}", Request.Url.Host));
        xmltw.WriteStartElement("Require");
        xmltw.WriteAttributeString("feature", "dynamic-height");
        xmltw.WriteEndElement(); // Require
        xmltw.WriteEndElement(); // moduleprefs
        xmltw.WriteStartElement("Content");
        xmltw.WriteAttributeString("type", "html");
        xmltw.WriteCData(szBody + "<script type=\"text/javascript\">gadgets.window.adjustHeight();</script>");
        xmltw.WriteEndElement(); // content
        xmltw.WriteEndElement(); // module
    }

}
