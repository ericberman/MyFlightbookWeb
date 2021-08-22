using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2021 MyFlightbook LLC
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
        if (Request.UserAgent.ToUpperInvariant().Contains("IPHONE"))
            txtApptTitle.Style["font-size"] = dateEnd.TextControl.Style["font-size"] = dateStart.TextControl.Style["font-size"] = "16px";   // prevent autozoom on iPhone

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
}