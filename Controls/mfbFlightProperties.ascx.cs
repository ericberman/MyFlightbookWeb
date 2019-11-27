using MyFlightbook;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/// <summary>
/// Note: this control is largely deprecated -- it is really only used now in an admin tool for deleting duplicate properties.
/// </summary>
public partial class Controls_mfbFlightProperties : System.Web.UI.UserControl
{
    private List<CustomFlightProperty> arCfp;
    private CustomPropertyType[] rgCPT;

    const string szVSKeyCFP = "CustomFlightPropertyList";

    private List<CustomFlightProperty> FlightProperties
    {
        get { return arCfp; }
        set { ViewState[szVSKeyCFP] = arCfp = value; gvProperties.DataSource = arCfp; gvProperties.DataBind(); }
    }

    public void SetFlightProperties(IEnumerable<CustomFlightProperty> rgCfp)
    {
        FlightProperties = new List<CustomFlightProperty>(rgCfp);
    }

    /// <summary>
    /// Save a bunch of viewstate if we don't have to keep the control enabled.
    /// </summary>
    public bool Enabled
    {
        get { return !String.IsNullOrEmpty(hdnEnabled.Value); }
        set { hdnEnabled.Value = value ? "1" : string.Empty; }
    }

    protected void InitNewProperty()
    {
        mfbDecEdit.IntValue = 0;
        mfbDecEdit.Value = 0.0M;
        mfbTypeInDate.Date = DateTime.Now;
        mfbDateTime.DateAndTime = DateTime.UtcNow;
        txtString.Text = "";
    }

    private CustomPropertyType SelectedPropertyType(int iSelectedValue)
    {
        foreach (CustomPropertyType cpt in rgCPT)
        {
            if (cpt.PropTypeID == iSelectedValue)
                return cpt;
        }
        return null;
    }

    protected void ShowCorrectEntryForPropertyType()
    {

        mfbDecEdit.Visible = txtString.Visible = mfbDateTime.Visible = mfbTypeInDate.Visible = false;

        int iCfp = Convert.ToInt32(cmbCustomPropertyTypes.SelectedValue, CultureInfo.InvariantCulture);

        btnAddProperty.Visible = (iCfp > 0);

        if (iCfp <= 0)
            return;

        CustomPropertyType cpt = SelectedPropertyType(iCfp);
        if (cpt == null)
            return;

        switch (cpt.Type)
        {
            case CFPPropertyType.cfpInteger:
                mfbDecEdit.Visible = true;
                mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Integer;
                break;
            case CFPPropertyType.cfpDecimal:
                mfbDecEdit.Visible = true;
                mfbDecEdit.EditingMode = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UsesHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal;
                break;
            case CFPPropertyType.cfpCurrency:
                mfbDecEdit.Visible = true;
                mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Currency;
                break;
            case CFPPropertyType.cfpBoolean:
                break;
            case CFPPropertyType.cfpDate:
                mfbTypeInDate.Visible = true;
                break;
            case CFPPropertyType.cfpDateTime:
                mfbDateTime.Visible = true;
                break;
            case CFPPropertyType.cfpString:
                txtString.Visible = true;
                break;
            default:
                break;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Enabled)
            return;

        rgCPT = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Page.User.Identity.Name : ""); // this is cached so we can do it on every call, postback or not

        if (!IsPostBack)
        {
            ListItem li;

            Boolean fFavorites = rgCPT[0].IsFavorite;
            if (fFavorites)
            {
                li = new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PropertyGroupSeparator, Resources.LocalizedText.FlightPropertiesFrequentlyUsed), "-1");
                li.Attributes.Add("style", "font-weight: bold");
                cmbCustomPropertyTypes.Items.Add(li);
            }

            foreach (CustomPropertyType cpt in rgCPT)
            {
                if (fFavorites && !cpt.IsFavorite) // transition to non-favorites
                {
                    fFavorites = false;
                    li = new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PropertyGroupSeparator, Resources.LocalizedText.FlightPropertiesOtherProperties), "-1");
                    li.Attributes.Add("style", "font-weight: bold");
                    cmbCustomPropertyTypes.Items.Add(li);
                }

                li = new ListItem(cpt.Title, cpt.PropTypeID.ToString(CultureInfo.InvariantCulture));

                cmbCustomPropertyTypes.Items.Add(li);
            }

            if (FlightProperties == null)
                FlightProperties = new List<CustomFlightProperty>();

            InitNewProperty();
        }
        else
        {
            if (ViewState[szVSKeyCFP] != null)
                FlightProperties = (List<CustomFlightProperty>)ViewState[szVSKeyCFP];
        }

        ShowCorrectEntryForPropertyType();
    }

    protected void AddProperty(object sender, EventArgs e)
    {
        CustomPropertyType cptCurrent = SelectedPropertyType(Convert.ToInt32(cmbCustomPropertyTypes.SelectedValue, CultureInfo.InvariantCulture));
        if (cptCurrent == null) // should never happen
            return;

        CustomFlightProperty cfpCurrent = null;

        // see if this property already exists.  If so, re-use it.  Otherwise, create a new one.
        foreach (CustomFlightProperty cfp in FlightProperties)
        {
            if (cfp.PropTypeID == cptCurrent.PropTypeID)
            {
                cfpCurrent = cfp;
                break;
            }
        }
        if (cfpCurrent == null)
        {
            cfpCurrent = new CustomFlightProperty(cptCurrent);
            FlightProperties.Add(cfpCurrent);
        }

        switch (cptCurrent.Type)
        {
            case CFPPropertyType.cfpBoolean:
                cfpCurrent.BoolValue = true;
                break;
            case CFPPropertyType.cfpDate:
                cfpCurrent.DateValue = mfbTypeInDate.Date;
                break;
            case CFPPropertyType.cfpDateTime:
                cfpCurrent.DateValue = mfbDateTime.DateAndTime;
                break;
            case CFPPropertyType.cfpDecimal:
            case CFPPropertyType.cfpCurrency:
                cfpCurrent.DecValue = mfbDecEdit.Value;
                break;
            case CFPPropertyType.cfpInteger:
                cfpCurrent.IntValue = mfbDecEdit.IntValue;
                break;
            case CFPPropertyType.cfpString:
                cfpCurrent.TextValue = txtString.Text;
                break;
            default:
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Unknown property type to add: {0}", cptCurrent.Type));
        }

        FlightProperties = arCfp;

        InitNewProperty(); // clear for a new property.
    }

    protected void gvProperties_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        CustomFlightProperty cfp = (CustomFlightProperty)arCfp[e.RowIndex];

        arCfp.RemoveAt(e.RowIndex);

        // remove it from the database (will be a no-op if this has never been saved)
        cfp.DeleteProperty();

        FlightProperties = arCfp;
    }

    protected void PropSelectionChanged(object sender, EventArgs e)
    {
        ShowCorrectEntryForPropertyType();
        InitNewProperty();
    }
}
