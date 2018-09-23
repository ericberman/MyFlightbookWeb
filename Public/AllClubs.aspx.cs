using MyFlightbook;
using MyFlightbook.Clubs;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_AllClubs : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        bool fAdmin = Page.User.Identity.IsAuthenticated && util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData;
        gvClubs.DataSource = Club.AllClubs(fAdmin);
        gvClubs.DataBind();
    }

    protected void gvClubs_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_ClubControls_ViewClub vc = (Controls_ClubControls_ViewClub)e.Row.FindControl("viewClub1");
            vc.ActiveClub = (Club)e.Row.DataItem;
        }
    }
}