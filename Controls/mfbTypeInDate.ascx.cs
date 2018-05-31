using MyFlightbook;
using System;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTypeInDate : System.Web.UI.UserControl
{
    #region Properties.
    private DateTime m_date;

    public enum DefaultDateType { None, Today, Custom }

    /// <summary>
    /// Gets/sets the current date to display
    /// </summary>
    public DateTime Date
    {
        get { ValidateAndUpdateDate(); return m_date; }
        set
        {
            m_date = value;
            if (m_date == DateTime.MinValue)
                txtDate.Text = string.Empty;
            else
                txtDate.Text = Request.IsMobileDeviceOrTablet() && !ForceAjax ? m_date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : m_date.ToShortDateString();
        }
    }

    private const string szVSDefaultDate = "keyVSDefault";
    /// <summary>
    /// Gets/sets the date to use when the field is blank.  If the field is set to this value, the text field will be blanked out.
    /// </summary>
    public DateTime DefaultDate
    {
        get { return ViewState[szVSDefaultDate] == null ? DateTime.Today : (DateTime)ViewState[szVSDefaultDate]; }
        set
        {
            ViewState[szVSDefaultDate] = value;
            TextBoxWatermarkExtender1.WatermarkText = value.HasValue() ? value.ToShortDateString() : CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        }
    }

    /// <summary>
    /// Helper property to declaritively set the default date.
    /// </summary>
    public DefaultDateType DefaultType
    {
        get { return DefaultDate.HasValue() ? (DefaultDate.CompareTo(DateTime.Today) == 0 ? DefaultDateType.Today : DefaultDateType.Custom) : DefaultDateType.None; }
        set
        {
            if (value == DefaultDateType.None)
                DefaultDate = DateTime.MinValue;
            else if (value == DefaultDateType.Today)
                DefaultDate = DateTime.Today;
        }
    }

    /// <summary>
    /// Return the text control of the date box - so that it can be styled, for example.
    /// </summary>
    public TextBox TextControl
    {
        get { return txtDate; }
    }

    /// <summary>
    /// Return the calendar extender control of the date box, so that it can be set via javascript, for example
    /// </summary>
    public AjaxControlToolkit.CalendarExtender CalendarExtenderControl
    {
        get { return CalendarExtender1; }
    }

    /// <summary>
    /// Forces the use of Ajax
    /// </summary>
    public bool ForceAjax { get; set; }

    /// <summary>
    /// Client ID of the text box for the control
    /// </summary>
    public string ClientBoxID
    {
        get { return txtDate.ClientID; }
    }

    public short TabIndex
    {
        get { return txtDate.TabIndex; }
        set { txtDate.TabIndex = value; }
    }

    public Unit Width
    {
        get { return txtDate.Width; }
        set { txtDate.Width = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(TextBoxWatermarkExtender1.WatermarkText))
            DefaultType = DefaultDateType.Today;

        CalendarExtender1.Format = System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern;
        bool fUseHtml5Input = Request.IsMobileDeviceOrTablet() && !ForceAjax;
        txtDate.TextMode = fUseHtml5Input ? TextBoxMode.Date : TextBoxMode.SingleLine;    // use the HTML 5 support only on mobile devices.
        CalendarExtender1.Enabled = !fUseHtml5Input;
    }

    protected Boolean ValidateAndUpdateDate()
    {
        Boolean fResult = true;

        DateTime dt = new DateTime();
        if (txtDate.Text.Length == 0)
            dt = DefaultDate;
        else
            fResult = DateTime.TryParse(txtDate.Text, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dt);

        if (fResult)
            m_date = dt;

        return fResult;
    }

    public void DateIsValid(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        args.IsValid = ValidateAndUpdateDate();
    }

}
