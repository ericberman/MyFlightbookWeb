using System;

/******************************************************
 * 
 * Copyright (c) 2014-2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_CFISigs : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = MyFlightbook.tabID.tabUnknown;
        Master.Layout = MasterPage.LayoutMode.Accordion;
    }
}