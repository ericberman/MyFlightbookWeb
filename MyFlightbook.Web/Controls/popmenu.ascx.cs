using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_popmenu : UserControl, INamingContainer
{
    [TemplateContainer(typeof(MenuContentTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate MenuContent { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public int OffsetX
    {
        get { return (int) Unit.Parse(pnlMenuContent.Style["margin-left"], CultureInfo.InvariantCulture).Value; }
        set { pnlMenuContent.Style["margin-left"] = Unit.Pixel(value).ToString(CultureInfo.InvariantCulture); }
    }

    public int OffsetY
    {
        get { return (int)Unit.Parse(pnlMenuContent.Style["margin-top"], CultureInfo.InvariantCulture).Value; }
        set { pnlMenuContent.Style["margin-top"] = Unit.Pixel(value).ToString(CultureInfo.InvariantCulture); }
    }

    public PlaceHolder Container { get { return plcMenuContent; } }

    protected override void OnInit(EventArgs e)
    {
        if (MenuContent != null)
            MenuContent.InstantiateIn(plcMenuContent);
        base.OnInit(e);
    }
}

public class MenuContentTemplate : Control, INamingContainer
{
    public MenuContentTemplate()
    {
    }
}