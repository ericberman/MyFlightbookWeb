using MyFlightbook;
using MyFlightbook.FlightCurrency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbCustCurrency : System.Web.UI.UserControl
{
    public event EventHandler<CustomCurrencyEventArgs> CurrencyAdded = null;
    public event EventHandler<CustomCurrencyEventArgs> CurrencyUpdated = null;

    #region Properties
    private CustomCurrency m_cc = null;

    /// <summary>
    /// The currency being edited
    /// </summary>
    public CustomCurrency Currency
    {
        get { return m_cc; }
        set
        {
            m_cc = value;
            InitForm();
            ToForm();
        }
    }

    protected CustomCurrencyTimespanType SelectedTimespanType
    {
        get { return (CustomCurrencyTimespanType)Enum.Parse(typeof(CustomCurrencyTimespanType), cmbMonthsDays.SelectedValue); }
        set { cmbMonthsDays.SelectedValue = value.ToString(); }
    }

    private bool m_fNeedsInit = true;
    #endregion

    private void InitForm()
    {
        if (!m_fNeedsInit)
            return;

        m_fNeedsInit = false;

        foreach (CustomCurrencyTimespanType t in Enum.GetValues(typeof(CustomCurrencyTimespanType)))
            cmbMonthsDays.Items.Add(new ListItem(t.DisplayString(), t.ToString()));

        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        IEnumerable<Aircraft> lstAc = ua.GetAircraftForUser();
        lstAircraft.DataSource = lstAc;
        lstAircraft.DataBind();

        List<ListItem> lstCurrencyEvents = new List<ListItem>();
        foreach (var value in Enum.GetValues(typeof(CustomCurrency.CustomCurrencyEventType)))
            lstCurrencyEvents.Add(new ListItem(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CustomCurrency.EventTypeLabel(1, (CustomCurrency.CustomCurrencyEventType)value)), ((int)value).ToString(CultureInfo.CurrentCulture)));

        lstCurrencyEvents.Sort(delegate (ListItem a, ListItem b) { return a.Text.CompareCurrentCultureIgnoreCase(b.Text); });
        foreach (ListItem li in lstCurrencyEvents)
            cmbEventTypes.Items.Add(li);

        lstModels.DataSource = MakeModel.ModelsForAircraft(lstAc);
        lstModels.DataBind();

        IEnumerable<CategoryClass> rgCatClass = CategoryClass.CategoryClasses();
        cmbCatClass.DataSource = rgCatClass;
        cmbCatClass.DataBind();

        List<CustomPropertyType> lstCpt = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name));
        lstCpt.RemoveAll(cpt => !cpt.IsFavorite);
        lstProps.DataSource = lstCpt;
        lstProps.DataBind();

        // Get the categories (as opposed to catclass); this is something of a hack, but it avoids an extra DB call
        Dictionary<string, string> dictCategories = new Dictionary<string, string>();
        foreach (CategoryClass cc in rgCatClass)
            dictCategories[cc.Category] = cc.Category;
        List<string> lst = new List<string>(dictCategories.Keys);
        lst.Sort();
        foreach (string category in lst)
            cmbCategory.Items.Add(new ListItem(category, category));
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            InitForm();
        }
    }
    
    protected void valRangeCheckForTimeFrame_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (SelectedTimespanType.IsAligned())
        {
            args.IsValid = true;
            return;
        }
        int i;
        if (!int.TryParse(txtTimeFrame.Text, out i))
            args.IsValid = false;
        if (i <= 0 || i > 1000)
            args.IsValid = false;
    }

    protected void valRangeCheckEvents_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (decMinEvents.Value <= 0 || decMinEvents.Value > 2000)
            args.IsValid = false;
    }

    protected void valCheckEventSelected_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (Convert.ToInt32(cmbEventTypes.SelectedValue, CultureInfo.InvariantCulture) < 0)
            args.IsValid = false;
    }

    protected void ResetCustomCurrencyForm()
    {
        // clear the form
        txtRuleName.Text = string.Empty;
        cmbEventTypes.SelectedIndex = 0;
        decMinEvents.Value = 1;
        txtTimeFrame.Text = txtAirport.Text = txtContainedText.Text = string.Empty;
        cmbMonthsDays.SelectedIndex = 0;

        foreach (ListItem li in lstAircraft.Items)
            li.Selected = false;
        foreach (ListItem li in lstModels.Items)
            li.Selected = false;
        cmbCatClass.SelectedIndex = 0;
        cmbCategory.SelectedIndex = 0;
        foreach (ListItem li in lstProps.Items)
            li.Selected = false;
        UpdateTimeframeEnabledState();
    }

    protected void UpdateTimeframeEnabledState()
    {
        txtTimeFrame.Enabled = valCheckRequiredTimeframe.Enabled = !SelectedTimespanType.IsAligned();
        if (!txtTimeFrame.Enabled)
            txtTimeFrame.Text = string.Empty;
    }

    protected void cmbMonthsDays_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateTimeframeEnabledState();
    }

    protected void ToForm()
    {
        CustomCurrency cc = Currency;
        hdnCCId.Value = cc.ID.ToString(CultureInfo.InvariantCulture);
        txtRuleName.Text = cc.DisplayName;
        cmbLimitType.SelectedValue = cc.CurrencyLimitType.ToString();
        if (cc.EventType.IsIntegerOnly())
        {
            decMinEvents.EditingMode = Controls_mfbDecimalEdit.EditMode.Integer;
            decMinEvents.IntValue = (int) cc.RequiredEvents;
        }
        else
        {
            decMinEvents.EditingMode = Controls_mfbDecimalEdit.EditMode.Decimal;
            decMinEvents.Value = cc.RequiredEvents;
        }

        cmbEventTypes.SelectedValue = ((int) cc.EventType).ToString(CultureInfo.InvariantCulture);
        SelectedTimespanType = cc.TimespanType;
        txtTimeFrame.Text = cc.TimespanType.IsAligned() ? string.Empty : cc.ExpirationSpan.ToString(CultureInfo.InvariantCulture);

        foreach (ListItem li in lstModels.Items)
            li.Selected = cc.ModelsRestriction.Contains(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));
        foreach (ListItem li in lstAircraft.Items)
            li.Selected = cc.AircraftRestriction.Contains(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));
        cmbCategory.SelectedValue = cc.CategoryRestriction;
        cmbCatClass.SelectedValue = cc.CatClassRestriction.ToString();

        txtAirport.Text = cc.AirportRestriction;
        txtContainedText.Text = cc.TextRestriction;
        if (cc.PropertyRestriction != null)
        {
            HashSet<int> hsPropsChecked = new HashSet<int>(cc.PropertyRestriction);
            foreach (ListItem li in lstProps.Items)
            {
                int propID = Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
                li.Selected = cc.PropertyRestriction.Contains(propID);
                hsPropsChecked.Remove(propID);
            }

            // Add in any properties that were not found above!  (I.e., blacklisted or otherwise not favorite)
            IEnumerable<CustomPropertyType> rgBlackListProps = CustomPropertyType.GetCustomPropertyTypes(hsPropsChecked);
            foreach (CustomPropertyType cpt in rgBlackListProps)
                lstProps.Items.Add(new ListItem(cpt.Title, cpt.PropTypeID.ToString(CultureInfo.InvariantCulture)) { Selected = true });
        }

        btnAddCurrencyRule.Visible = cc.ID <= 0;    // only show the add button if we're doing a new currency.
    }

    protected void FromForm()
    {
        CustomCurrency cc = new CustomCurrency();
        cc.ID = Convert.ToInt32(hdnCCId.Value, CultureInfo.InvariantCulture);
        cc.UserName = Page.User.Identity.Name;
        cc.DisplayName = txtRuleName.Text.Trim();
        CustomCurrency.LimitType lt = CustomCurrency.LimitType.Minimum;
        if (Enum.TryParse<CustomCurrency.LimitType>(cmbLimitType.SelectedValue, out lt))
            cc.CurrencyLimitType = lt;
        cc.RequiredEvents = decMinEvents.Value;
        cc.EventType = (CustomCurrency.CustomCurrencyEventType)Convert.ToInt32(cmbEventTypes.SelectedValue, CultureInfo.InvariantCulture);
        cc.TimespanType = SelectedTimespanType;
        cc.ExpirationSpan = cc.TimespanType.IsAligned() ? 0 : Convert.ToInt32(txtTimeFrame.Text, CultureInfo.CurrentCulture);

        cc.ModelsRestriction = IDsFromList(lstModels);
        cc.AircraftRestriction = IDsFromList(lstAircraft);
        cc.CatClassRestriction = (CategoryClass.CatClassID)Enum.Parse(typeof(CategoryClass.CatClassID), cmbCatClass.SelectedValue);
        cc.CategoryRestriction = cmbCategory.SelectedValue.Trim();

        cc.TextRestriction = txtContainedText.Text;
        cc.AirportRestriction = txtAirport.Text;
        cc.PropertyRestriction = IDsFromList(lstProps);

        m_cc = cc;
    }

    public bool Commit()
    {
        if (!Page.IsValid)
            return false;

        FromForm();

        bool fIsNew = Currency.IsNew;

        if (Currency.FCommit())
        {
            ResetCustomCurrencyForm();

            if (fIsNew && CurrencyAdded != null)
                CurrencyAdded(this, new CustomCurrencyEventArgs(Currency));
            else if (!fIsNew && CurrencyUpdated != null)
                CurrencyUpdated(this, new CustomCurrencyEventArgs(Currency));
        }
        else
            lblErr.Text = Currency.ErrorString;

        return String.IsNullOrEmpty(lblErr.Text);
    }

    protected void btnAddCurrencyRule_Click(object sender, EventArgs e)
    {
        Commit();
    }

    protected IEnumerable<int> IDsFromList(ListControl l)
    {
        if (l == null)
            throw new ArgumentNullException("l");
        List<int> lst = new List<int>();
        foreach (ListItem li in l.Items)
            if (li.Selected)
                lst.Add(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));
        return lst;
    }

    protected void cmbEventTypes_SelectedIndexChanged(object sender, EventArgs e)
    {
        CustomCurrency.CustomCurrencyEventType ccet = (CustomCurrency.CustomCurrencyEventType)Convert.ToInt32(cmbEventTypes.SelectedValue, CultureInfo.InvariantCulture);
        decimal val = decMinEvents.Value;
        if (ccet.IsIntegerOnly()) {
            decMinEvents.EditingMode = Controls_mfbDecimalEdit.EditMode.Integer;
            decMinEvents.IntValue = (int)val;
        }
        else
        {
            decMinEvents.EditingMode = Controls_mfbDecimalEdit.EditMode.Decimal;
            decMinEvents.Value = val;
        }
    }
}