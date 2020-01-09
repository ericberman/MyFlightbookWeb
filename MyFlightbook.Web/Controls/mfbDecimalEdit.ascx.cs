using System;
using System.Globalization;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbDecimalEdit : System.Web.UI.UserControl
{
    public enum EditMode { Integer, Decimal, Currency, HHMMFormat };

    private const string WaterMarkInteger = "0";
    private const string WaterMarkDecimal = "0.0";
    private const string WaterMarkCurrency = "0.00";

    private const string regDecimalTemplate = "([0-9])*([{0}]([0-9])*)?";
    private const string regInteger = "([0-9])*";

    private Decimal m_defValueDec = 0.0M;
    private int m_defValueInt = 0;
    private EditMode m_EditMode = EditMode.Decimal;

    private const string keyViewStateEditMode = "EM";

    /// <summary>
    /// The default decimal value (0.0 if unspecified)
    /// </summary>
    public Decimal DefaultValueDecimal
    {
        get { return m_defValueDec; }
        set { m_defValueDec = value; }
    }

    public TextBox EditBox
    {
        get { return txtDecimal; }
    }

    public AjaxControlToolkit.TextBoxWatermarkExtender EditBoxWE
    {
        get { return txtDecimal_TextBoxWatermarkExtender; }
    }

    /// <summary>
    /// To enable corss-fill, provide the clientID of the source edit box.
    /// </summary>
    public string CrossFillSourceClientID { get; set; }

    private readonly System.Text.RegularExpressions.Regex regexIOSSafariHack = new System.Text.RegularExpressions.Regex("(ipad|iphone)", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private bool IsIOSSafari { get { return Request != null && Request.UserAgent != null && regexIOSSafariHack.IsMatch(Request.UserAgent); } }

    /// <summary>
    /// What is the editing mode for this control?  Decimal, integer, or HHMM?
    /// </summary>
    public EditMode EditingMode
    {
        get { return m_EditMode; }
        set 
        {
            // by default, enable validation for a number, not HHMM
            FilteredTextBoxExtender.Enabled = valNumber.Enabled = true;
            valHHMMFormat.Enabled = false;

            switch (value)
            {
                case EditMode.Currency:
                case EditMode.Decimal:
                    {
                        if (Request.IsMobileDeviceOrTablet())
                        {
                            txtDecimal.TextMode = TextBoxMode.Number;
                            txtDecimal.Attributes["step"] = "0.01";
                        }
                        else
                        {
                            txtDecimal.TextMode = TextBoxMode.SingleLine;
                            txtDecimal.Attributes.Remove("step");
                        }
                        string szDecChar = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                        txtDecimal_TextBoxWatermarkExtender.WatermarkText = ((value == EditMode.Currency) ? WaterMarkCurrency : WaterMarkDecimal).Replace(".", szDecChar);
                        FilteredTextBoxExtender.FilterType = AjaxControlToolkit.FilterTypes.Custom;

                        if (IsIOSSafari)
                            szDecChar = ".,";

                        FilteredTextBoxExtender.ValidChars = "0123456789" + szDecChar;
                        valNumber.ValidationExpression = String.Format(System.Globalization.CultureInfo.InvariantCulture, regDecimalTemplate, szDecChar);
                    }
                    break;

                case EditMode.HHMMFormat:
                    string szTimeSep = System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.TimeSeparator;

                    txtDecimal.TextMode = TextBoxMode.SingleLine;
                    txtDecimal_TextBoxWatermarkExtender.WatermarkText = String.Format(CultureInfo.InvariantCulture, "hh{0}mm", szTimeSep);
                    FilteredTextBoxExtender.FilterType = AjaxControlToolkit.FilterTypes.Custom;
                    FilteredTextBoxExtender.ValidChars = "0123456789" + szTimeSep;
                    FilteredTextBoxExtender.Enabled = true;
                    valNumber.Enabled = false;
                    valHHMMFormat.Enabled = true;

                    valHHMMFormat.ErrorMessage = valHHMMFormat.ErrorMessage.Replace(":", szTimeSep);
                    valHHMMFormat.ValidationExpression = valHHMMFormat.ValidationExpression.Replace(":", szTimeSep);
                    break;

                case EditMode.Integer:
                    if (Request.IsMobileDeviceOrTablet())
                    {
                        txtDecimal.TextMode = TextBoxMode.Number;
                        txtDecimal.Attributes["step"] = "1";
                    }
                    else
                    {
                        txtDecimal.TextMode = TextBoxMode.SingleLine;
                        txtDecimal.Attributes.Remove("step");
                    }
                    txtDecimal_TextBoxWatermarkExtender.WatermarkText = WaterMarkInteger;
                    FilteredTextBoxExtender.FilterType = AjaxControlToolkit.FilterTypes.Numbers;

                    valNumber.ValidationExpression = regInteger;
                    break;
            }

            if (!String.IsNullOrEmpty(CrossFillSourceClientID))
            {
                imgXFill.Visible = true;
                imgXFill.Attributes["onclick"] = String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:$find('{0}').set_text(document.getElementById('{1}').value);", EditBoxWE.ClientID, CrossFillSourceClientID);
            }

            ViewState[keyViewStateEditMode] = m_EditMode = value;
        }
    }

    /// <summary>
    /// The default integer value, if in integer-only mode (0 if unspecified)
    /// </summary>
    public int DefaultValueInt
    {
        get { return m_defValueInt; }
        set { m_defValueInt = value; }
    }


    /// <summary>
    /// Specifies whether or not the field is a required field
    /// </summary>
    public Boolean IsRequired
    {
        get { return valRequired.Enabled; }
        set { valRequired.Enabled = value; }
    }

    /// <summary>
    /// The text to display if the field is required and not filled in.
    /// </summary>
    public string RequiredText
    {
        get { return valRequired.ErrorMessage; }
        set { valRequired.ErrorMessage = value; }
    }

    /// <summary>
    /// The decimal value for the control
    /// </summary>
    public Decimal Value
    {
        get 
        {
            if (m_EditMode == EditMode.Decimal || m_EditMode == EditMode.Integer || m_EditMode == EditMode.Currency)
            {
                // HACK for iOS, which always reports the resulting number in US format.
                // I.e., if you're in Italy, and type in "3,14", the resulting text is "3.14", but I parse that in Italian culture!!
                if (m_EditMode != EditMode.Integer && System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator.CompareOrdinal(".") != 0 && IsIOSSafari)
                {
                    Decimal d;
                    if (Decimal.TryParse(txtDecimal.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                        return d;
                    return 0.0M;
                }
                return txtDecimal.Text.SafeParseDecimal(DefaultValueDecimal);
            }
            else
                return txtDecimal.Text.DecimalFromHHMM();
        }
        set 
        {
            if (m_EditMode == EditMode.Decimal || m_EditMode == EditMode.Integer || m_EditMode == EditMode.Currency)
            {
                // return empty string if we equal the default value
                // Otherwise, return at least one significant digit to the right of the decimal point, but
                // no more if there is only one significant digit.
                System.Globalization.CultureInfo ci = (m_EditMode == EditMode.Integer || !IsIOSSafari) ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;
                txtDecimal.Text = (value == DefaultValueDecimal) ? string.Empty : value.ToString(m_EditMode == EditMode.Currency ? "C" : "#.0##", ci);
            }
            else
                txtDecimal.Text = (value == DefaultValueDecimal) ? string.Empty : value.ToHHMM();
        }
    }

    /// <summary>
    /// The integer value for the control, if in integer-only mode
    /// </summary>
    public int IntValue
    {
        get { return txtDecimal.Text.SafeParseInt(DefaultValueInt); }
        set { txtDecimal.Text = (value == DefaultValueInt) ? string.Empty : value.ToString(CultureInfo.InvariantCulture); }
    }

    public Unit Width
    {
        get { return txtDecimal.Width; }
        set { txtDecimal.Width = value; }
    }

    public short TabIndex
    {
        get { return txtDecimal.TabIndex; }
        set { txtDecimal.TabIndex = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        object em = ViewState[keyViewStateEditMode];

        if (em != null)
            EditingMode = (EditMode)em;
        else
            EditingMode = EditingMode; // force the watermark/filter to update appropriately.
    }
}
