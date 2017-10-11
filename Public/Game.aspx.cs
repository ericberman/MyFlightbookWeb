using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using MyFlightbook;
using MyFlightbook.Airports;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Game_Game : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.mptQuiz;

        if (!IsPostBack)
        {
            lblAirportGameHeader.Text = this.Master.Title = Resources.Airports.airportGameTitle;
            if (!String.IsNullOrEmpty(Request.QueryString["SkipIntro"]))
            {
                MfbAirportIDGame1.InitQuiz();
                MfbAirportIDGame1.StartQuiz(false);
            }
        }
    }
}
