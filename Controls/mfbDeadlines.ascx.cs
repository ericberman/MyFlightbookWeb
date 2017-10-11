using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbDeadlines : System.Web.UI.UserControl
{
    private const string szDeadlinesKey = "viewstateKeyDeadlines";
    private const string szAircraftKey = "viewstateKeyAircraft";

    public event EventHandler<DeadlineEventArgs> DeadlineUpdated = null;

    #region Properties
    private List<DeadlineCurrency> UserDeadlines
    {
        get
        {
            if (ViewState[szDeadlinesKey] == null)
                ViewState[szDeadlinesKey] = new List<DeadlineCurrency>(DeadlineCurrency.DeadlinesForUser(Page.User.Identity.Name, AircraftID));
            return (List<DeadlineCurrency>)ViewState[szDeadlinesKey];
        }
        set { ViewState[szDeadlinesKey] = value; }
    }

    public int DeadlineCount
    {
        get { return UserDeadlines.Count; }
    }

    public int AircraftID
    {
        get { return String.IsNullOrEmpty(hdnAircraft.Value) ? Aircraft.idAircraftUnknown : Convert.ToInt32(hdnAircraft.Value, CultureInfo.InvariantCulture); }
        set { hdnAircraft.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    public string UserName
    {
        get { return hdnUser.Value; }
        set { hdnUser.Value = value; }
    }
    #endregion

    /// <summary>
    /// Rebinds the list, always hitting the database.
    /// </summary>
    public void ForceRefresh()
    {
        UserDeadlines = null;
        Refresh();
    }

    /// <summary>
    /// Rebinds the list, using potentially cached results
    /// </summary>
    public void Refresh()
    {
        gvDeadlines.DataSource = UserDeadlines;
        gvDeadlines.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void gvDeadlines_RowCommand(object sender, CommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            DeadlineCurrency dc = UserDeadlines.Find(d => id == d.ID);
            if (dc != null)
            {
                dc.FDelete();
                ForceRefresh();
            }
        }
    }

    protected void gvDeadlines_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        gvDeadlines.EditIndex = e.NewEditIndex;
        Refresh();
    }

    protected void gvDeadlines_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        gvDeadlines.EditIndex = -1;
        Refresh();
    }

    protected void gvDeadlines_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                Controls_mfbTypeInDate typeindate = (Controls_mfbTypeInDate)e.Row.FindControl("mfbUpdateDeadlineDate");
                Controls_mfbDecimalEdit decEdit = (Controls_mfbDecimalEdit)e.Row.FindControl("decNewHours");
                DeadlineCurrency dc = UserDeadlines[e.Row.RowIndex];
                if (dc.UsesHours)
                    decEdit.Value = dc.AircraftHours;
                else
                    typeindate.Date = typeindate.DefaultDate = dc.Expiration.LaterDate(DateTime.Now);
            }
        }
    }

    protected void gvDeadlines_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        DeadlineCurrency dc = UserDeadlines[e.RowIndex];
        DeadlineCurrency dcOriginal = new DeadlineCurrency();
        util.CopyObject(dc, dcOriginal);
        Controls_mfbTypeInDate typeinNew = (Controls_mfbTypeInDate)gvDeadlines.Rows[e.RowIndex].FindControl("mfbUpdateDeadlineDate");
        Controls_mfbDecimalEdit typeinNewHours = (Controls_mfbDecimalEdit)gvDeadlines.Rows[e.RowIndex].FindControl("decNewHours");

        if (dc.AircraftHours > 0)
            dc.AircraftHours = dc.NewHoursBasedOnHours(typeinNewHours.Value);
        else
            dc.Expiration = dc.NewDueDateBasedOnDate(typeinNew.Date);

        if (dc.IsValid() && dc.FCommit())
        {
            gvDeadlines.EditIndex = -1;
            ForceRefresh();

            if (dc.AircraftID > 0)
            {
                string szDiff = dc.DifferenceDescription(dcOriginal);
                if (!String.IsNullOrEmpty(szDiff))
                {
                    MaintenanceLog ml = new MaintenanceLog() { AircraftID = dc.AircraftID, ChangeDate = DateTime.Now, User = UserName, Description = szDiff, Comment = string.Empty };
                    ml.FAddToLog();
                }
            }

            if (DeadlineUpdated != null)
                DeadlineUpdated(this, new DeadlineEventArgs(dcOriginal, dc));
        }
    }
}