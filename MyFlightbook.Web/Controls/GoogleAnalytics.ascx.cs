using System;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_GoogleAnalytics : System.Web.UI.UserControl
{

    public string RedirHref {get; set;}

    protected string RedirJScript
    {
        get
        {
            return String.IsNullOrEmpty(RedirHref) ? string.Empty : ", { 'hitCallback': function() { window.location = \"" + RedirHref + "\";}}";
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}