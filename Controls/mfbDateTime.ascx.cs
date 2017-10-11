using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbDateTime : System.Web.UI.UserControl
{
    #region properties
    public string Text
    {
        get { return txtDateTime.Text; }
    }

    public short TabIndex
    {
        get { return txtDateTime.TabIndex; }
        set { txtDateTime.TabIndex = value; }
    }

    private const string szKeyVSDefaultDate = "VSDefaultDateTimedate";

    /// <summary>
    /// Default date to use if a date is not specified (just a time)
    /// </summary>
    public DateTime DefaultDate 
    { 
        get
        {
            DateTime dt = DateTime.Now;

            if (ViewState[szKeyVSDefaultDate] != null)
                dt = (DateTime) ViewState[szKeyVSDefaultDate];
            return dt;
        }
        set
        {
            ViewState[szKeyVSDefaultDate] = value;
        }
    }

    private static Regex rTimeOnly = new Regex("^\\w*\\d\\d?\\:\\d\\d\\w*$", RegexOptions.Compiled);
    public DateTime DateAndTime
    {
        get {
            DateTime dt = txtDateTime.Text.SafeParseDate(DateTime.MinValue);
            // check for time-only - if so, set to the default date.
            if (rTimeOnly.IsMatch(txtDateTime.Text))
            {
                DateTime dtDef = DefaultDate;
                dt = new DateTime(dtDef.Year, dtDef.Month, dtDef.Day, dt.Hour, dt.Minute, 0, dtDef.Kind);
            }
            if (!dt.HasValue())
                txtDateTime.Text = string.Empty;
            return dt;
        }
        set 
        {
            if (null == value || value.CompareTo(DateTime.MinValue) == 0)
                txtDateTime.Text = "";
            else
                txtDateTime.Text = value.UTCDateFormatString(); 
        }
    }
    #endregion

    protected Boolean DateIsValid()
    {
        DateTime dt;
        return DateTime.TryParse(txtDateTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out dt);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        TextBoxWatermarkExtender1.WatermarkText = Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern + " HH:mm";
    }

    protected void CustomValidator1_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args != null)
            args.IsValid = DateIsValid();
    }
}
