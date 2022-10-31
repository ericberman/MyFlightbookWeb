using MyFlightbook;
using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbAccordionProxyControl : System.Web.UI.UserControl
{
    public event EventHandler ControlClicked;

    #region properties
    public string LabelText
    {
        get { return lblLabel.Text; }
        set { lblLabel.Text = value; }
    }

    public bool IsEnhanced
    {
        get { return !string.IsNullOrEmpty(hdnIsEnhanced.Value); }
        set
        {
            const string enhancedCSS = "accordionEnhanced";

            hdnIsEnhanced.Value = value ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            Container.CssClass = Container.CssClass.ReplaceCSSClasses(new string[] { enhancedCSS }, value ? enhancedCSS : string.Empty, true);
        }
    }

    /// <summary>
    /// True to force a postback when clicked for the first time rather than semply opening a pane.
    /// </summary>
    public bool LazyLoad
    {
        get { return btnPostback.Visible; }
        set { btnPostback.Visible = value; }
    }

    public string PostbackID { get { return btnPostback.UniqueID; } }

    public Label LabelControl
    {
        get { return lblLabel; }
    }

    public Panel Container
    {
        get { return pnlContainer; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e) {  }

    protected void btnPostback_Click(object sender, EventArgs e)
    {
        ControlClicked?.Invoke(this, e);
    }
}