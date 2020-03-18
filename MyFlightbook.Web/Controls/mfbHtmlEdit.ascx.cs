using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbHtmlEdit : System.Web.UI.UserControl
{
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

    public bool ShowHtmlTab
    {
        get { return TextBox1_HtmlEditorExtender.DisplaySourceTab; }
        set { TextBox1_HtmlEditorExtender.DisplaySourceTab = value; }
    }

    [System.ComponentModel.Bindable(true)]
    public string Text
    {
        get { return txtMain.Text.Trim(); }
        set { txtMain.Text = (value == null) ? string.Empty : (String.IsNullOrEmpty(value.Trim()) ? Resources.LocalizedText.ChromeTabHack.Replace("\\t", "\t") : value); }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        // Hack to make the html editing work on IE11.  Lame!
        System.Web.UI.HtmlControls.HtmlMeta m = new System.Web.UI.HtmlControls.HtmlMeta();
        Page.Header.Controls.Add(m);
        m.HttpEquiv = "X-UA-Compatible";
        m.Content = "IE=10,chrome=1";
    }
}