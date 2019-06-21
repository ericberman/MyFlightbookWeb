using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbDeadlines : UserControl
{
    private const string szDeadlinesKey = "viewstateKeyDeadlines";

    public event EventHandler<DeadlineEventArgs> DeadlineUpdated = null;
    public event EventHandler<DeadlineEventArgs> DeadlineAdded = null;
    public event EventHandler<DeadlineEventArgs> DeadlineDeleted = null;

    #region Properties
    private List<DeadlineCurrency> UserDeadlines
    {
        get
        {
            if (ViewState[szDeadlinesKey] == null)
                ViewState[szDeadlinesKey] = new List<DeadlineCurrency>(DeadlineCurrency.DeadlinesForUser(UserName, AircraftID, true));  // if an aircraft is specified, will pull in shared deadlines too.
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

    /// <summary>
    /// True if any new deadline should be shared (i.e., owned by an aircraft not a user)
    /// </summary>
    public bool CreateShared
    {
        get { return String.IsNullOrEmpty(hdnCreateShared.Value) ? false : Convert.ToBoolean(hdnCreateShared.Value, CultureInfo.InvariantCulture); }
        set
        {
            hdnCreateShared.Value = value.ToString(CultureInfo.InvariantCulture);
            pnlAircraftLabel.Visible = cmbDeadlineAircraft.Visible = !value;
            if (value)
                ckDeadlineUseHours.Visible = true;
        }
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
        if (!IsPostBack)
        {
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            List<Aircraft> lstDeadlineAircraft = new List<Aircraft>(ua.GetAircraftForUser());
            lstDeadlineAircraft.RemoveAll(ac => ac.InstanceType != AircraftInstanceTypes.RealAircraft);
            lstDeadlineAircraft.RemoveAll(ac => ac.HideFromSelection);
            cmbDeadlineAircraft.DataSource = lstDeadlineAircraft;
            cmbDeadlineAircraft.DataBind();
            decRegenInterval.EditBox.Attributes["onfocus"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').checked = true;", rbRegenInterval.ClientID);
        }
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
                if (DeadlineDeleted != null)
                    DeadlineDeleted(this, new DeadlineEventArgs(dc, null));
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

            if (DeadlineUpdated != null)
                DeadlineUpdated(this, new DeadlineEventArgs(dcOriginal, dc));
        }
    }

    #region New Deadlines
    protected void SetNewDeadlineMode(bool fHours)
    {
        mvDeadlineDue.SetActiveView(fHours ? vwDeadlineDueHours : vwDeadlineDueDate);
        mvRegenInterval.SetActiveView(fHours ? vwDeadlineHours : vwDeadlineCalendarRange);
        if (fHours)
            mfbDeadlineDate.Date = DateTime.MinValue;
        else
            decDueHours.Value = 0.0M;
    }

    protected void ckDeadlineUseHours_CheckedChanged(object sender, EventArgs e)
    {
        SetNewDeadlineMode(ckDeadlineUseHours.Checked);
    }

    protected void cmbDeadlineAircraft_SelectedIndexChanged(object sender, EventArgs e)
    {
        ckDeadlineUseHours.Visible = !String.IsNullOrEmpty(cmbDeadlineAircraft.SelectedValue);
        if (!ckDeadlineUseHours.Visible)
            ckDeadlineUseHours.Checked = false;
        SetNewDeadlineMode(ckDeadlineUseHours.Checked);
    }

    protected void btnAddDeadline_Click(object sender, EventArgs e)
    {
        int regenspan;
        DeadlineCurrency.RegenUnit ru = rbRegenManual.Checked ? DeadlineCurrency.RegenUnit.None : (ckDeadlineUseHours.Checked ? DeadlineCurrency.RegenUnit.Hours : (DeadlineCurrency.RegenUnit)Enum.Parse(typeof(DeadlineCurrency.RegenUnit), cmbRegenRange.SelectedValue));
        switch (ru)
        {
            default:
            case DeadlineCurrency.RegenUnit.None:
                regenspan = 0;
                break;
            case DeadlineCurrency.RegenUnit.Days:
            case DeadlineCurrency.RegenUnit.CalendarMonths:
            case DeadlineCurrency.RegenUnit.Hours:
                regenspan = decRegenInterval.IntValue;
                break;
        }

        decimal aircraftHours = decDueHours.Value;
        int idAircraft = 0;
        if (CreateShared)
            idAircraft = AircraftID;
        else
        {
            if (!String.IsNullOrEmpty(cmbDeadlineAircraft.SelectedValue))
                idAircraft = Convert.ToInt32(cmbDeadlineAircraft.SelectedValue, CultureInfo.InvariantCulture);
        }

        DeadlineCurrency dc = new DeadlineCurrency(CreateShared ? null : UserName, txtDeadlineName.Text, mfbDeadlineDate.Date, regenspan, ru, idAircraft, aircraftHours);
        if (dc.IsValid() && dc.FCommit())
        {
            ForceRefresh();
            ResetDeadlineForm();
            Refresh();
            if (DeadlineAdded != null)
                DeadlineAdded(this, new DeadlineEventArgs(null, dc));
        }
        else
            lblErrDeadline.Text = dc.ErrorString;
    }

    protected void ResetDeadlineForm()
    {
        ckDeadlineUseHours.Checked = false;
        AircraftID = AircraftID;    // will cause cmbDeadline to hide/show as needed.
        cmbDeadlineAircraft.SelectedValue = (AircraftID > 0) ? AircraftID.ToString(CultureInfo.InvariantCulture) : string.Empty;
        SetNewDeadlineMode(false);
        txtDeadlineName.Text = string.Empty;
        mfbDeadlineDate.Date = DateTime.MinValue;
        decDueHours.Value = decRegenInterval.IntValue = 0;
        ckDeadlineUseHours.Visible = false;
        rbRegenManual.Checked = true;
        cpeDeadlines.ClientState = "true";
    }
    #endregion
}