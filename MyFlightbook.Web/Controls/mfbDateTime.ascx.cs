using MyFlightbook;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
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

    protected TimeZoneInfo DefaultTimeZone { get; set; }

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

    private readonly static Regex rTimeOnly = new Regex("^\\w*\\d\\d?\\:\\d\\d\\w*$", RegexOptions.Compiled);
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
            {
                txtDateTime.Text = string.Empty;
                return DateTime.MinValue;
            }
            else
            {
                switch (dt.Kind)
                {
                    default:
                    case DateTimeKind.Unspecified:
                        return TimeZoneInfo.ConvertTimeToUtc(dt, DefaultTimeZone);
                    case DateTimeKind.Utc:
                        return dt;
                    case DateTimeKind.Local:
                        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), DefaultTimeZone);
                }
            }
        }
        set 
        {
            if (null == value || value.CompareTo(DateTime.MinValue) == 0)
                txtDateTime.Text = "";
            else
                txtDateTime.Text = TimeZoneInfo.ConvertTimeFromUtc(value, DefaultTimeZone).UTCDateFormatString(); 
        }
    }
    #endregion

    protected Boolean DateIsValid()
    {
        DateTime dt;
        return DateTime.TryParse(txtDateTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out dt);
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        DefaultTimeZone = (Page.User.Identity.IsAuthenticated) ? MyFlightbook.Profile.GetUser(Page.User.Identity.Name).PreferredTimeZone : TimeZoneInfo.Utc;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        TextBoxWatermarkExtender1.WatermarkText = Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern + " HH:mm";
        Page.ClientScript.RegisterClientScriptBlock(GetType(), "FillNow", String.Format(CultureInfo.InvariantCulture, @"
        function setNowUTCWithOffset(wme) {{
            var params = new Object();
            var d = JSON.stringify(params);
            $.ajax(
            {{
                url: '{0}',
                type: ""POST"", data: d, dataType: ""json"", contentType: ""application/json"",
                error: function(xhr, status, error) {{ }},
                complete: function(response) {{ }},
                success: function(response) {{ $find(wme).set_text(response.d); }}
            }});
        }}", ResolveClientUrl("~/Member/LogbookNew.aspx/NowInUTC")), true);
    }

    protected void CustomValidator1_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args != null)
            args.IsValid = DateIsValid();
    }
}
