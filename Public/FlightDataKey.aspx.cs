using MyFlightbook.Telemetry;
using System;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_FlightDataKey : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.Title = Resources.FlightData.FlightDataTitle;
        Master.Layout = MasterPage.LayoutMode.Accordion;
        gvKnownColumns.DataSource = KnownColumn.KnownColumns;
        gvKnownColumns.DataBind();
    }
}
