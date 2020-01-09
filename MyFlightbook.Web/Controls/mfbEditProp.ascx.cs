using MyFlightbook;
using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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

    protected void SetUpCrossFillTach()
    {
        imgXFillTach.Visible = true;

        string szXFillScript = String.Format(CultureInfo.InvariantCulture, @"
    addLoadEvent(function () {{
        document.getElementById('{0}').style.display = (currentlySelectedAircraft) ? ""inline-block"" : ""none"";
    }});

    function onTachAutofill()
    {{
        if (!currentlySelectedAircraft)
            return;

        var id = currentlySelectedAircraft();

        if (id === null || id === '')
            return;

        var params = new Object();
        params.idAircraft = id;
        var d = JSON.stringify(params);
        $.ajax(
        {{
            url: '{1}',
            type: ""POST"", data: d, dataType: ""json"", contentType: ""application/json"",
            error: function(xhr, status, error) {{
                window.alert(xhr.responseJSON.Message);
                if (onError !== null)
                    onError();
            }},
            complete: function(response) {{ }},
            success: function(response) {{
                $find('{2}').set_text(response.d);
            }}
        }});
    }}",
        imgXFillTach.ClientID,
        ResolveUrl("~/Member/LogbookNew.aspx/HighWaterMarkTachForAircraft"),
        mfbDecEdit.EditBoxWE.ClientID
    );

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "CrossFillHobbs", szXFillScript, true);
    }

    protected void ToForm()
    {
        CustomFlightProperty fp = m_fp;

        TimeZoneInfo tz = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).PreferredTimeZone;
        lblPropName.Text = (fp.PropertyType.Type == CFPPropertyType.cfpDateTime) ? fp.PropertyType.Title.IndicateUTCOrCustomTimeZone(tz) : fp.PropertyType.Title;
        lblPropName.ToolTip = (fp.PropertyType.Type == CFPPropertyType.cfpDateTime && tz.Id.CompareCurrentCultureIgnoreCase(TimeZoneInfo.Utc.Id) != 0) ? tz.DisplayName : string.Empty;
        mfbTooltip.Visible = !String.IsNullOrEmpty(mfbTooltip.BodyContent = fp.PropertyType.Type == CFPPropertyType.cfpDateTime ? fp.PropertyType.Description.Replace("(UTC)", tz.DisplayName) : fp.PropertyType.Description);
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
                mvProp.SetActiveView(vwDecimal);    // need to do this before setting the cross-fill image to visible
                // Set the cross-fill source before setting the editing mode.
                if (!fp.PropertyType.IsBasicDecimal)
                    mfbDecEdit.CrossFillSourceClientID = CrossFillSourceClientID;
                if (fp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachStart)
                    SetUpCrossFillTach();
                mfbDecEdit.EditingMode = (!fp.PropertyType.IsBasicDecimal && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal);
                mfbDecEdit.Value = fp.DecValue;
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