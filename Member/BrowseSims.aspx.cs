using System;
using System.Collections.ObjectModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2011-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Member_BrowseSims : System.Web.UI.Page
{
    private int m_MakeModelID = -1;
    private AircraftInstanceTypes m_acType = AircraftInstanceTypes.RealAircraft; // all sims by default
    private Aircraft m_acLastSelected = null;

    private string keyVSAircraft = "keyVSallSims";

    #region Properties
    /// <summary>
    /// The make/model to restrict to, -1 for all models
    /// </summary>
    public int MakeModelID
    {
        get { return m_MakeModelID; }
        set { m_MakeModelID = value; InvalSimList(); }
    }

    /// <summary>
    /// The type of instance type to restrict to - use RealAircraft for all
    /// </summary>
    public AircraftInstanceTypes SimType
    {
        get { return m_acType; }
        set { m_acType = value; InvalSimList(); }
    }

    /// <summary>
    /// The most recently selected aircraft
    /// </summary>
    public Aircraft SelectedAircraft
    {
        get { return m_acLastSelected; }
        set { m_acLastSelected = value; }
    }

    protected Collection<Aircraft> SimList
    {
        get
        {
            if (ViewState[keyVSAircraft] == null)
                ViewState[keyVSAircraft] = Aircraft.GetSims(MakeModelID, (SimType == AircraftInstanceTypes.RealAircraft), SimType);
            return (Collection<Aircraft>)ViewState[keyVSAircraft];
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.tabAircraft;
        this.Master.Title = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.TitleAircraft, Branding.CurrentBrand.AppName);
        if (!IsPostBack)
        {
            cmbModels.DataSource = SimpleMakeModel.GetAllMakeModels();
            cmbModels.DataBind();
            Refresh();
        }
    }

    protected void Refresh()
    {
        SimType = (AircraftInstanceTypes)(Convert.ToInt32(cmbSimType.SelectedValue, System.Globalization.CultureInfo.InvariantCulture));
        MakeModelID = Convert.ToInt32(cmbModels.SelectedValue, System.Globalization.CultureInfo.InvariantCulture);
        UpdateGrid();
    }

    protected void cmbSimType_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void cmbModels_SelectedIndexChanged(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void InvalSimList()
    {
        ViewState[keyVSAircraft] = null;
    }

    public void UpdateGrid()
    {
        gvSims.DataSource = SimList;
        gvSims.DataBind();
    }

    protected void gvSims_DataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Aircraft ac = (Aircraft)e.Row.DataItem;

            Button btnAdd = (Button)e.Row.FindControl("btnAddThisAircraft");
            Label lblAlreadyPresent = (Label)e.Row.FindControl("lblAlreadyPresent");

            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            Boolean fAlreadyKnown = ua.CheckAircraftForUser(ac);
            lblAlreadyPresent.Visible = fAlreadyKnown;
            btnAdd.Visible = !fAlreadyKnown;
            e.Row.Font.Bold = fAlreadyKnown;
        }
    }

    protected void addAircraft(object sender, GridViewCommandEventArgs e)
    {
        if (e != null && e.CommandName.CompareOrdinalIgnoreCase("AddAircraft") == 0)
        {
            GridViewRow grow = (GridViewRow)((Button)e.CommandSource).NamingContainer;
            int iAc = grow.RowIndex;
            Collection<Aircraft> alSims = SimList;
            SelectedAircraft = (Aircraft)alSims[iAc];
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            ua.FAddAircraftForUser(SelectedAircraft);
            UpdateGrid();
        }
    }
}