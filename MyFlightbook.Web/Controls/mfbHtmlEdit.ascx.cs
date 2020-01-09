using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbHtmlEdit : System.Web.UI.UserControl
{

    // hack: html sanitizer strips <br> tags; 
    // See http://stackoverflow.com/questions/11398824/how-i-can-prevent-antixss-sanitizer-from-removing-html5-br-tag-from-ajaxcontro for how to fix line breaks in htmlextended text boxes
    public const string szHtmlEditorNewlineHack = "~_!_~"; 

    #region properties
    public TextBox TextControl { get { return txtMain; } }
    public Unit Width
    {
        get { return txtMain.Width; }
        set { txtMain.Width = value; }
    }

    public Unit Height
    {
        get { return txtMain.Height; }
        set { txtMain.Height = value; }
    }

    public int Rows
    {
        get { return txtMain.Rows; }
        set { txtMain.Rows = value; }
    }

    public string FixedHtml
    {
        get { return FixHtml(txtMain.Text); }
    }

    public bool ShowHtmlTab
    {
        get { return TextBox1_HtmlEditorExtender.DisplaySourceTab; }
        set { TextBox1_HtmlEditorExtender.DisplaySourceTab = value; }
    }

    [System.ComponentModel.Bindable(true)]
    public string Text
    {
        // See https://code.google.com/p/chromium/issues/detail?id=395318; htmledit with empty text causes problems on Chrome.
        get { return txtMain.Text.Trim(); }
        set { txtMain.Text = (value == null) ? string.Empty : (String.IsNullOrEmpty(value.Trim()) ? Resources.LocalizedText.ChromeTabHack.Replace("\\t", "\t") : value); }
    }

    public static string FixHtml(string sz)
    {
        if (sz == null)
            throw new ArgumentNullException("sz");
        return sz.Replace(szHtmlEditorNewlineHack, "<br />");
    }

    public static string UnFixEncodedHtml(string sz)
    {
        if (sz == null)
            throw new ArgumentNullException("sz");
        return HttpContext.Current.Server.HtmlDecode(sz.Replace("&lt;br&gt;", szHtmlEditorNewlineHack));
    }

    public static string UnFixHtml(string sz)
    {
        if (sz == null)
            throw new ArgumentNullException("sz");
        return sz.Replace("<br />", szHtmlEditorNewlineHack);
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        // Hack to make the html editing work on IE11.  Lame!
        System.Web.UI.HtmlControls.HtmlMeta m = new System.Web.UI.HtmlControls.HtmlMeta();
        Page.Header.Controls.Add(m);
        m.HttpEquiv = "X-UA-Compatible";
        m.Content = "IE=10,chrome=1";

        if (IsPostBack)
        {
            Text = UnFixEncodedHtml(txtMain.Text);
        }
    }

    protected void TextBox1_HtmlEditorExtender_PreRender(object sender, EventArgs e)
    {
        Text = FixHtml(txtMain.Text);
    }
}