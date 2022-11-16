using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
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
    /// The text over which to hover for the tooltip ("[?]" by default)
    /// </summary>
    public string HoverText
    {
        get { return lblTip.Text; }
        set { lblTip.Text = value; }
    }

    private string m_hoverControlID = string.Empty;
    /// <summary>
    /// Sets the the hover control by ASP.NET ID, but returns it in JQueriable format.
    /// </summary>
    public string HoverControlID 
    { 
        get { return m_hoverControlID; } 
        set 
        {
            m_hoverControlID = value;
            lblTip.Visible = String.IsNullOrEmpty(value);
        }
    }

    public string HoverControlSelector
    {
        get { return "#" + (String.IsNullOrEmpty(HoverControlID) ? lblTip.ClientID : this.NamingContainer.FindControl(HoverControlID).ClientID); }
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
