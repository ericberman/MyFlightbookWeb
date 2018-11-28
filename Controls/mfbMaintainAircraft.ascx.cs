using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbMaintainAircraft : System.Web.UI.UserControl
{
    private string szKeyVSMaint = "viewstateMaintenance";

    #region properties
    /// <summary>
    /// The id of the aircraft for which this maintenance record is associated
    /// </summary>
    public int AircraftID
    {
        get { return Convert.ToInt32(hdnIDAircraft.Value, CultureInfo.InvariantCulture); }
        set
        {
            hdnIDAircraft.Value = value.ToString(CultureInfo.InvariantCulture);
            mfbDeadlines1.AircraftID = value;
        }
    }

    /// <summary>
    /// The maintenancerecord
    /// </summary>
    public MaintenanceRecord Maintenance
    {
        get { return (MaintenanceRecord) ViewState[szKeyVSMaint]; }
        set { ViewState[szKeyVSMaint] = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            InitForm();
    }

    /// <summary>
    /// Initialize the form from the loaded aircraft
    /// </summary>
    public void InitForm()
    {
        if (Maintenance != null)
        {
            mfbLastAltimeter.Date = Maintenance.LastAltimeter;
            mfbLastAnnual.Date = Maintenance.LastAnnual;
            mfbLastELT.Date = Maintenance.LastELT;
            mfbLastPitotStatic.Date = Maintenance.LastStatic;
            mfbLastTransponder.Date = Maintenance.LastTransponder;
            mfbLastVOR.Date = Maintenance.LastVOR;
            mfbLastEngine.Value = Maintenance.LastNewEngine;
            mfbLast100.Value = Maintenance.Last100;
            mfbLastOil.Value = Maintenance.LastOilChange;
            mfbRenewalDue.Date = Maintenance.RegistrationExpiration;

            lblNext100.Text = (Maintenance.Next100 == 0.0M) ? string.Empty : Maintenance.Next100.ToString("0.0", CultureInfo.InvariantCulture);

            SetTextForDate(lblNextAltimeter, Maintenance.NextAltimeter);
            SetTextForDate(lblNextVOR, Maintenance.NextVOR);
            SetTextForDate(lblNextAnnual, Maintenance.NextAnnual);
            SetTextForDate(lblNextELT, Maintenance.NextELT);
            SetTextForDate(lblNextPitot, Maintenance.NextStatic);
            SetTextForDate(lblNextTransponder, Maintenance.NextTransponder);

            lblNextOil.Text = (Maintenance.LastOilChange > 0.0M) ? String.Format(CultureInfo.InvariantCulture, (string)GetLocalResourceObject("lblNextOilResource1.Text"), Maintenance.LastOilChange + 25, Maintenance.LastOilChange + 50, Maintenance.LastOilChange + 100) : String.Empty;

            UpdateMaintHistory();
        }

        // See if any deadlines are associated with this aircraft
        mfbDeadlines1.UserName = Page.User.Identity.Name;
        mfbDeadlines1.AircraftID = AircraftID;
        mfbDeadlines1.ForceRefresh();
    }

    private void SetTextForDate(Label lbl, DateTime dt)
    {
        if (dt.CompareTo(DateTime.MinValue) == 0)
        {
            lbl.Text = "";
            return;
        }

        lbl.Text = dt.ToShortDateString();
        if (dt.CompareTo(DateTime.Now) < 0)
            lbl.CssClass = "currencyexpired";
        else if (dt.AddDays(-31).CompareTo(DateTime.Now) < 0)
            lbl.CssClass = "currencynearlydue";
    }

    private void UpdateMaintHistory()
    {
        MaintenanceLog[] rgml = MaintenanceLog.ChangesByAircraftID(this.AircraftID);
        gvMaintLog.DataSource = rgml;
        gvMaintLog.DataBind();
    }

    /// <summary>
    /// Initialize MaintenanceRecord object from form
    /// </summary>
    public MaintenanceRecord MaintenanceForAircraft()
    {
        MaintenanceRecord mr = new MaintenanceRecord();
        mr.LastAltimeter = mfbLastAltimeter.Date;
        mr.LastAnnual = mfbLastAnnual.Date;
        mr.LastELT = mfbLastELT.Date;
        mr.LastStatic = mfbLastPitotStatic.Date;
        mr.LastTransponder = mfbLastTransponder.Date;
        mr.LastVOR = mfbLastVOR.Date;
        mr.LastNewEngine = mfbLastEngine.Value;
        mr.Last100 = mfbLast100.Value;
        mr.LastOilChange = mfbLastOil.Value;
        mr.RegistrationExpiration = mfbRenewalDue.Date;
        return mr;
    }

    protected void gvMaintLog_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        gvMaintLog.PageIndex = e.NewPageIndex;
        UpdateMaintHistory();
    }

    protected void mfbDeadlines1_DeadlineUpdated(object sender, DeadlineEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.OriginalDeadline == null || e.NewDeadline == null)
            return;

        string szDiff = e.NewDeadline.DifferenceDescription(e.OriginalDeadline);
        if (!String.IsNullOrEmpty(szDiff) && e.NewDeadline.IsSharedAircraftDeadline)
        {
            MaintenanceLog ml = new MaintenanceLog() { AircraftID = e.NewDeadline.AircraftID, ChangeDate = DateTime.Now, User = Page.User.Identity.Name, Description = szDiff, Comment = string.Empty };
            ml.FAddToLog();

            UpdateMaintHistory();
        }
    }

    protected void mfbDeadlines1_DeadlineAdded(object sender, DeadlineEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.NewDeadline.IsSharedAircraftDeadline)
        {
            MaintenanceLog ml = new MaintenanceLog() { AircraftID = e.NewDeadline.AircraftID, ChangeDate = DateTime.Now, User = Page.User.Identity.Name, Description = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineCreated, e.NewDeadline.DisplayName), Comment = string.Empty };
            ml.FAddToLog();

            UpdateMaintHistory();
        }
    }

    protected void mfbDeadlines1_DeadlineDeleted(object sender, DeadlineEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.OriginalDeadline == null)
            return;

        if (e.OriginalDeadline.IsSharedAircraftDeadline)
        {
            MaintenanceLog ml = new MaintenanceLog() { AircraftID = e.OriginalDeadline.AircraftID, ChangeDate = DateTime.Now, User = Page.User.Identity.Name, Description = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineDeleted, e.OriginalDeadline.DisplayName), Comment = string.Empty };
            ml.FAddToLog();

            UpdateMaintHistory();
        }
    }
}
