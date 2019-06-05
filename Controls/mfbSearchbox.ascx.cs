using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSearchbox : System.Web.UI.UserControl
{
    #region Properties
    public string SearchText
    {
        get { return txtSearch.Text; }
        set { txtSearch.Text = value; }
    }

    public string Hint
    {
        get { return TextBoxWatermarkExtender1.WatermarkText; }
        set { TextBoxWatermarkExtender1.WatermarkText = value; }
    }

    /// <summary>
    /// The underlying textbox
    /// </summary>
    public TextBox TextBoxControl { get { return txtSearch; } }

    #endregion

    public event EventHandler<EventArgs> SearchClicked = null;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        if (SearchClicked != null)
            SearchClicked(sender, e);
    }
}