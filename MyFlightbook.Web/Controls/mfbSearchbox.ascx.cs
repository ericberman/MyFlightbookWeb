using MyFlightbook;
using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2022 MyFlightbook LLC
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
        get { return txtSearch.GetPlaceholder(); }
        set { txtSearch.SetPlaceholder(value); }
    }

    /// <summary>
    /// The underlying textbox
    /// </summary>
    public TextBox TextBoxControl { get { return txtSearch; } }

    #endregion

    public event EventHandler<EventArgs> SearchClicked;

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        SearchClicked?.Invoke(sender, e);
    }
}