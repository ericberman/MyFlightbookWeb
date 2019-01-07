using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditAppt : System.Web.UI.UserControl
{
    /// <summary>
    /// Text to use by default for new appointments (typically owner's name)
    /// </summary>
    public string DefaultTitle
    {
        get { return hdnDefaultTitle.Value; }
        set { hdnDefaultTitle.Value = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Popuplate time-of-day combo-box in 15-minute increments
            for (int i = 0; i < 24 * 4; i++)
            {
                DateTime d = new DateTime(2000, 1, 1, i / 4, 15 * (i % 4), 0);
                ListItem li = new ListItem(d.ToShortTimeString(), (i * 15).ToString(System.Globalization.CultureInfo.InvariantCulture));
                cmbHourStart.Items.Add(li);
                cmbHourEnd.Items.Add(li);
            }
        }
    }


    /*
     * The code below ought to work, but doesn't properly, so we're doing a hack with the drop-down date on mobile devices.
     * Two bugs I've seen with this code:
     * a) on iOS, once you use the html5 date control, focus goes to the page behind the modal dialog box, and you need to then scroll the backing page before you can do anything else in the modal popup - lame!
     * b) on Android, the date picker pops up but leaves an empty text box where the date should be!
     * 
     * So instead, we'll say "forceajax" on the date controls.  
     * 
        function setApptDate(dt, idDateExtender, idDate, idHour) {
            var fUseHtml5Date = <% =(Request.IsMobileDeviceOrTablet()) ? "true" : "false" %>;
            var d = new Date(dt.getYear(), dt.getMonth(), dt.getDay(), 0, 0, 0, 0);
            if (fUseHtml5Date)
                document.getElementById(idDate).value = dt.toString('yyyy-MM-dd');
            else
                $find(idDateExtender).set_selectedDate(d);
            var h = parseInt(dt.getHours());
            var m = parseInt(dt.getMinutes() / 15) * 15;
            document.getElementById(idHour).value = (h * 60 + m).toString();
        }

        function getApptDate(idDateExtender, idDate, idHour) {
            var fUseHtml5Date = <% =(Request.IsMobileDeviceOrTablet()) ? "true" : "false" %>;
            var dLocal = (fUseHtml5Date) ? new Date(document.getElementById(idDate).value) : $find(idDateExtender)._selectedDate;
            
            var minutesIntoDay = parseInt(document.getElementById(idHour).value);

            var h = parseInt(minutesIntoDay / 60);
            var m = parseInt(minutesIntoDay % 60);
            var d = new Date(dLocal.getUTCFullYear(), dLocal.getUTCMonth(), dLocal.getUTCDate(), h, m, 0, 0);
            dt = new DayPilot.Date(d, true);
            return dt;
        }

        function setStartDate(dt) { setApptDate(dt, '<% =dateStart.CalendarExtenderControl.ClientID %>', '<% =dateStart.TextControl.ClientID %>', '<% =cmbHourStart.ClientID %>'); }
        function getStartDate() { return getApptDate('<% =dateStart.CalendarExtenderControl.ClientID %>', '<% =dateStart.TextControl.ClientID %>', '<% =cmbHourStart.ClientID %>'); }
        function setEndDate(dt) { setApptDate(dt, '<% =dateEnd.CalendarExtenderControl.ClientID %>', '<% =dateEnd.TextControl.ClientID %>', '<% =cmbHourEnd.ClientID %>'); }
        function getEndDate() { return getApptDate('<% =dateEnd.CalendarExtenderControl.ClientID %>', '<% =dateEnd.TextControl.ClientID %>', '<% =cmbHourEnd.ClientID %>'); }
        
     * * */
}