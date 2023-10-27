using System;

/******************************************************
 * 
 * Copyright (c) 2017-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_METAR : System.Web.UI.UserControl
{
    #region Properties
    public string Route
    {
        get { return hdnAirports.Value; }
        set { hdnAirports.Value = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}