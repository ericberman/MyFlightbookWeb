using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2008-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_CurrencyDisclaimer : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;
        Master.Layout = MasterPage.LayoutMode.Accordion;
    }
}
