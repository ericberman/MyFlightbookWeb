using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTimeZone : System.Web.UI.UserControl
{
    public int TimeZoneOffset
    {
        get { return hdnTZOffset.Value.SafeParseInt(); }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}