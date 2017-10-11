using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_GoogleAnalytics : System.Web.UI.UserControl
{

    public string RedirURL {get; set;}

    protected string RedirJScript
    {
        get
        {
            return String.IsNullOrEmpty(RedirURL) ? string.Empty : ", { 'hitCallback': function() { window.location = \"" + RedirURL + "\";}}";
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}