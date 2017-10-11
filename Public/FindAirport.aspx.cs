using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Airports;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_FindAirport : System.Web.UI.Page
{
    const string szVSResults = "keyViewStateResults";
    AirportList m_alResults = new AirportList("");

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.mptFindAirport;
        lblPageHeader.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Airports.MapRouteHeader, Branding.CurrentBrand.AppName);

        MfbGoogleMapManager1.ShowRoute = false;
        MfbGoogleMapManager1.Map.AllowDupeMarkers = true;

        if (!IsPostBack)
            doSearch();
        else
        {
            if (ViewState[szVSResults] != null)
            {
                m_alResults = (AirportList)ViewState[szVSResults];
                doSearch();
            }
        }
    }

    protected void doSearch()
    {
        airport[] rgap = m_alResults.GetAirportList();
        gvResults.DataSource = rgap;
        gvResults.DataBind();
        MfbGoogleMapManager1.Map.SetAirportList(m_alResults);
        lnkZoomOut.NavigateUrl = MfbGoogleMapManager1.ZoomToFitScript;
        lnkZoomOut.Visible = (rgap.Length > 0);
    }

    protected void btnFind_Click(object sender, EventArgs e)
    {
        gvResults.Visible = true;
        m_alResults = new AirportList();
        m_alResults.InitFromSearch(txtSearch.Text);
        ViewState[szVSResults] = m_alResults;

        doSearch();
    }

    protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        GridView gv = (GridView)sender;
        gv.PageIndex = e.NewPageIndex;
        gv.DataBind();
    }


}