using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_popmenu : System.Web.UI.UserControl, INamingContainer
{
    [TemplateContainer(typeof(MenuContentTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate MenuContent { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

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