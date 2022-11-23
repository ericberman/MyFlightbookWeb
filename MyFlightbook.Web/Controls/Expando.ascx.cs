using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_Expando : UserControl, INamingContainer
{
    [TemplateContainer(typeof(ExpandoHeaderTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate Header { get; set; }

    [TemplateContainer(typeof(ExpandoBodyTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate Body { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public AjaxControlToolkit.CollapsiblePanelExtender ExpandoControl
    {
        get { return CollapsiblePanelExtender1; }
    }

    public Label ExpandoLabel
    {
        get { return lblShowHide; }
    }

    public PlaceHolder HeaderContainer
    {
        get { return plcExpandoHeader; }
    }

    public string HeaderCss
    {
        get { return pnlHeader.CssClass; }
        set { pnlHeader.CssClass = value; }
    }

    public PlaceHolder BodyContainer
    {
        get { return plcExpandoBody; }
    }

    protected override void OnInit(EventArgs e)
    {
        Header?.InstantiateIn(plcExpandoHeader);
        Body?.InstantiateIn(plcExpandoBody);
        base.OnInit(e);
    }

    protected class ExpandoHeaderTemplate : Control, INamingContainer
    {
        public ExpandoHeaderTemplate()
        {
        }
    }

    protected class ExpandoBodyTemplate : Control, INamingContainer
    {
        public ExpandoBodyTemplate()
        {
        }
    }
}