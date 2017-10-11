using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbGoogleAdSense : System.Web.UI.UserControl
{
    public enum GoogleAdLayoutStyle {adStyleHorizontal, adStyleVertical};

    /// <summary>
    /// Which style to display - horizontal or vertical
    /// </summary>
    public GoogleAdLayoutStyle LayoutStyle
    {
        get { return (GoogleAdLayoutStyle) mvGoogleAd.ActiveViewIndex; }
        set { mvGoogleAd.ActiveViewIndex = (int)value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}