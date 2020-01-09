using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Shunt : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        lblShuntMsg.Text = Branding.ReBrand((string)ConfigurationManager.AppSettings["ShuntMessage"]);
    }
}