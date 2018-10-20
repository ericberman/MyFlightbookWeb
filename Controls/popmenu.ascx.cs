using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_popmenu : System.Web.UI.UserControl, INamingContainer
{
    [TemplateContainer(typeof(MenuContentTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate MenuContent { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public int OffsetX
    {
        get { return PopupControlExtender1.OffsetX; }
        set { PopupControlExtender1.OffsetX = value; }
    }

    public int OffsetY
    {
        get { return PopupControlExtender1.OffsetY; }
        set { PopupControlExtender1.OffsetY = value; }
    }

    public PlaceHolder Container { get { return plcMenuContent; } }

    protected override void OnInit(EventArgs e)
    {
        if (MenuContent != null)
            MenuContent.InstantiateIn(plcMenuContent);
        base.OnInit(e);
    }

    protected class MenuContentTemplate : Control, INamingContainer
    {
        public MenuContentTemplate()
        {
        }
    }
}