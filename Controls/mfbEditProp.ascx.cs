using System;
using System.Web.UI;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2013-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditProp : System.Web.UI.UserControl
{
    private CustomFlightProperty m_fp = null;

    #region properties
    /// <summary>
    /// The flight property being edited
    /// </summary>
    public CustomFlightProperty FlightProperty
    {
        get
        {
            FromForm();
            return m_fp;
        }
        set 
        { 
            m_fp = value; 
            ToForm(); 
        }
    }

    /// <summary>
    /// The ClientID of the source control for cross-filling.
    /// </summary>
    public string CrossFillSourceClientID { get; set; }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void FromForm()
    {
        CustomFlightProperty fp = m_fp;
        switch (fp.PropertyType.Type)
        {
            case CFPPropertyType.cfpBoolean:
                fp.BoolValue = ckValue.Checked;
                break;
            case CFPPropertyType.cfpInteger:
                fp.IntValue = mfbDecEdit.IntValue;
                break;
            case CFPPropertyType.cfpDecimal:
            case CFPPropertyType.cfpCurrency:
                fp.DecValue = mfbDecEdit.Value;
                break;
            case CFPPropertyType.cfpDate:
                fp.DateValue = mfbTypeInDate.Date;
                break;
            case CFPPropertyType.cfpDateTime:
                fp.DateValue = mfbDateTime.DateAndTime;
                break;
            case CFPPropertyType.cfpString:
                fp.TextValue = txtString.Text;
                break;
            default:
                throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Unknown property type: {0}", fp.PropertyType.Type));
        }
    }

    protected void ToForm()
    {
        CustomFlightProperty fp = m_fp;

        lblPropName.Text = fp.PropertyType.Title;
        mfbTooltip.Visible = !String.IsNullOrEmpty(mfbTooltip.BodyContent = fp.PropertyType.Description);
        switch (fp.PropertyType.Type)
        {
            case CFPPropertyType.cfpBoolean:
                {
                    lblPropName.AssociatedControlID = ckValue.ID;
                    ckValue.Checked = fp.BoolValue;
                    mvProp.SetActiveView(vwBool);
                }
                break;
            case CFPPropertyType.cfpInteger:
                mfbDecEdit.IntValue = fp.IntValue;
                mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Integer;
                mvProp.SetActiveView(vwDecimal);
                break;
            case CFPPropertyType.cfpDecimal:
                // Set the cross-fill source before setting the editing mode.
                if (!fp.PropertyType.IsBasicDecimal)
                    mfbDecEdit.CrossFillSourceClientID = CrossFillSourceClientID;
                mfbDecEdit.EditingMode = (!fp.PropertyType.IsBasicDecimal && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal);
                mfbDecEdit.Value = fp.DecValue;
                mvProp.SetActiveView(vwDecimal);
                break;
            case CFPPropertyType.cfpCurrency:
                mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Currency;
                mfbDecEdit.Value = fp.DecValue;
                mvProp.SetActiveView(vwDecimal);
                break;
            case CFPPropertyType.cfpDate:
                mfbTypeInDate.Date = fp.DateValue;
                mvProp.SetActiveView(vwDate);
                break;
            case CFPPropertyType.cfpDateTime:
                mfbDateTime.DateAndTime = fp.DateValue;
                mvProp.SetActiveView(vwDateTime);
                break;
            case CFPPropertyType.cfpString:
                txtString.Text = fp.TextValue;
                mvProp.SetActiveView(vwText);
                autocompleteStringProp.ContextKey = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0};{1}", Page.User.Identity.Name, fp.PropTypeID.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Unknown property type: {0}", fp.PropertyType.Type));
        }
    }
}