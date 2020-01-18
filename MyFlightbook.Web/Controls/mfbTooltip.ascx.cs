using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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

    public string TipCss
    {
        get { return lblTip.CssClass; }
        set { lblTip.CssClass = value; }
    }

    public string TipStyle
    {
        get { return lblTip.Attributes["style"]; }
        set { lblTip.Attributes["style"] = value; }
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

    [TemplateContainer(typeof(TooltipContentTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate TooltipBody { get; set; }
    #endregion

    protected class TooltipContentTemplate : Control, INamingContainer
    {
        public TooltipContentTemplate() { }
    }


    protected override void OnInit(EventArgs e)
    {
        if (TooltipBody != null)
            TooltipBody.InstantiateIn(plcCustom);
        base.OnInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (!String.IsNullOrEmpty(BodySource))
                BodyContent = ((Label)this.NamingContainer.FindControl(BodySource)).Text;
        }
    }
}
