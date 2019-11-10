using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
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
            hdnIsEnhanced.Value = value ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
            Container.Attributes["style"] = value ? "border: 1px solid black; -webkit-box-shadow: 2px 2px 2px 0px rgba(0,0,0,0.75); -moz-box-shadow: 2px 2px 2px 0px rgba(0,0,0,0.75); box-shadow: 2px 2px 2px 0px rgba(0,0,0,0.75);" : string.Empty;
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

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnPostback_Click(object sender, EventArgs e)
    {
        ControlClicked(this, e);
    }
}