using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
************************************************/

public partial class Controls_mfbTooltip : System.Web.UI.UserControl
{
    #region Properties
    /// <summary>
    /// The content of the tooltip (can be HTML)
    /// </summary>
    public string BodyContent
    {
        get { return litPop.Text; }
        set { litPop.Text = value; }
    }

    /// <summary>
    /// The ID of a label control whose text value has the required text
    /// </summary>
    /// <returns></returns>
    public string BodySource { get; set; }

    /// <summary>
    /// A placeholder control, if more customized content is desired.
    /// </summary>
    public PlaceHolder BodyPlaceholder
    {
        get { return plcCustom; }
    }

    /// <summary>
    /// The text over which to hover for the tooltip ("[?]" by default)
    /// </summary>
    public string HoverText
    {
        get { return lblTip.Text; }
        set { lblTip.Text = value; }
    }

    public string HoverControl
    {
        get { return hmeHover.TargetControlID; }
        set
        {
            if (String.IsNullOrEmpty(value))
            {
                hmeHover.TargetControlID = lblTip.ID;
                lblTip.Visible = true;
            }
            else
            {
                hmeHover.TargetControlID = value;
                lblTip.Visible = false;
            }
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (!String.IsNullOrEmpty(BodySource))
                BodyContent = ((Label)this.NamingContainer.FindControl(BodySource)).Text;
        }
    }
}
