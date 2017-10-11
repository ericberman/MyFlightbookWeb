using MyFlightbook.Schedule;
using System;
using System.Collections.ObjectModel;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_ClubControls_TimeZone : System.Web.UI.UserControl
{
    /// <summary>
    /// The default offset to use if no timezone with the specified ID is found
    /// </summary>
    public int DefaultOffset {get; set;}

    /// <summary>
    /// The selected timezoneinfo object
    /// </summary>
    public TimeZoneInfo SelectedTimeZone
    {
        get { return ScheduledEvent.TimeZoneForIdOrOffset(cmbTimezones.SelectedValue);}
        set
        {
            try
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                cmbTimezones.SelectedValue = value.Id;
            }
            catch (ArgumentOutOfRangeException)
            {
                cmbTimezones.SelectedValue = ScheduledEvent.TimeZoneForOffset(TimeSpan.FromMinutes(DefaultOffset)).Id;
            }
        }
    }

    public bool AutoPostBack
    {
        get { return cmbTimezones.AutoPostBack; }
        set { cmbTimezones.AutoPostBack = value; }
    }

    public string SelectedTimeZoneId
    {
        get { return cmbTimezones.SelectedValue; }
        set { cmbTimezones.SelectedValue = value; }
    }

    public string ValidationGroup
    {
        get { return cmbTimezones.ValidationGroup; }
        set { cmbTimezones.ValidationGroup = valRequiredTimezone.ValidationGroup = value; }
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        ReadOnlyCollection<TimeZoneInfo> lstTZ = TimeZoneInfo.GetSystemTimeZones();
        cmbTimezones.DataSource = lstTZ;
        cmbTimezones.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
        }
    }
}