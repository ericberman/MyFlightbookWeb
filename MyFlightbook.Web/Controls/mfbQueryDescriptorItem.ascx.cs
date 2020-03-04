using MyFlightbook;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbQueryDescriptorItem : System.Web.UI.UserControl
{
    #region properties
    public string Title
    {
        get { return lblTitle.Text; }
        set { lblTitle.Text = value; }
    }

    public string Description
    {
        get { return lblDescriptor.Text; }
        set
        {
            lblDescriptor.Text = value; 
            lblColon.Visible = !String.IsNullOrEmpty(value);
        }
    }

    public string PropName
    {
        get { return hdnPropName.Value; }
        set { hdnPropName.Value = value; }
    }

    public event EventHandler<FilterItemClicked> DeleteItemClicked = null;
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnDelete_Click(object sender, ImageClickEventArgs e)
    {
        FilterItemClicked fic = new FilterItemClicked(new QueryFilterItem(Title, Description, PropName));
        if (DeleteItemClicked != null)
            DeleteItemClicked(this, fic);
    }
}