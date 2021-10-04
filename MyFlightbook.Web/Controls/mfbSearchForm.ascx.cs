using MyFlightbook;
using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSearchForm : UserControl
{
    private FlightQuery m_fq;
    private string m_szUser;

    public const string FullStopAirportAnchor = "!";

    private readonly static Regex regAirports = new Regex(String.Format(CultureInfo.InvariantCulture, "{0}?{1}?[a-zA-Z0-9]+{0}?", FullStopAirportAnchor, airport.ForceNavaidPrefix), RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #region properties
    /// <summary>
    /// The restriction clause that defines the selected flights; updates from the form if necessary (if not already in memory)
    /// </summary>
    public FlightQuery Restriction
    {
        get { return m_fq ?? GetFlightQuery(); }
        set
        {
            ClearForm();
            m_fq = value;
            RefreshFormForQuery();
        }
    }

    const string szKeyVSTypes = "vsKeyTypes";
    protected HashSet<string> TypeNames
    {
        get { return (HashSet<string>) ViewState[szKeyVSTypes] ?? new HashSet<string>(); }
        private set { ViewState[szKeyVSTypes] = value; }
    }

    public string Username 
    {
        get { return String.IsNullOrEmpty(m_szUser) ? Page.User.Identity.Name : m_szUser; }
        set
        {
            m_szUser = value;
            popCannedQueries.Visible = (string.CompareOrdinal(Page.User.Identity.Name, m_szUser) == 0);
        }
    }
    #endregion

    /// <summary>
    /// Event raised when the user submits.
    /// </summary>
    public event EventHandler<FlightQueryEventArgs> QuerySubmitted;

    /// <summary>
    /// Event raised when user clears the query
    /// </summary>
    public event System.EventHandler<FlightQueryEventArgs> Reset;

    /// <summary>
    /// Set the initial expanded/collapsed state for the object.  True = collapsed.  Dates and text are always expanded.
    /// </summary>
    public Boolean InitialCollapseState
    {
        get { return cpeCatClass.Collapsed && cpeAircraftChars.Collapsed && cpeAirplanes.Collapsed && cpeAirports.Collapsed && cpeMakes.Collapsed && cpeFlightCharacteristics.Collapsed; }
        set
        {
            cpeCatClass.Collapsed = cpeAircraftChars.Collapsed = cpeAirplanes.Collapsed = cpeAirports.Collapsed = cpeMakes.Collapsed = cpeFlightCharacteristics.Collapsed = value;
            cpeCatClass.ClientState = cpeAircraftChars.ClientState = cpeAirplanes.ClientState = cpeAirports.ClientState = cpeMakes.ClientState = cpeFlightCharacteristics.ClientState = value.ToString(CultureInfo.InvariantCulture);

            if (value)
            {
                pnlCatClass.Height = pnlAircraftType.Height = pnlAirplanes.Height = pnlAirports.Height = pnlMakes.Height = pnlFlightCharacteristics.Height = 0;
            }
        }
    }

    public void ClearForm()
    {
        // clear the dates for all of time...
        mfbTIDateFrom.Date = mfbTIDateFrom.DefaultDate;
        mfbTIDateTo.Date = mfbTIDateTo.DefaultDate;
        UncheckDates();
        rbAllTime.Checked = true;

        // Clear the text field and airport fields
        txtAirports.Text = txtRestrict.Text = string.Empty;

        rblFlightDistance.SelectedIndex = 0;

        // clear the aircraft
        foreach (ListItem li in cklAircraft.Items)
            li.Selected = false;

        // clear the makes
        foreach (ListItem li in cklMakes.Items)
            li.Selected = false;
        txtModelNameText.Text = string.Empty;

        // clear the types. 
        ckComplex.Checked = ckCowl.Checked = ckHighPerf.Checked = ckProp.Checked = ckRetract.Checked = ckGlass.Checked = ckTAA.Checked = ckTailwheel.Checked = ckMotorGlider.Checked = ckMultiEngineHeli.Checked = false;

        // And clear the "contains times
        ckFSLanding.Checked = ckNightLandings.Checked = ckApproaches.Checked = ckHolds.Checked =
            ckXC.Checked = ckSimIMC.Checked = ckIMC.Checked = ckNight.Checked =
            ckDual.Checked = ckCFI.Checked = ckSIC.Checked = ckPIC.Checked = 
            ckAnyInstrument.Checked = ckAnyLandings.Checked = ckGroundSim.Checked = ckTotal.Checked =
            ckPublic.Checked = ckIsSigned.Checked = ckHasTelemetry.Checked = ckHasImages.Checked = false;
        cmbFlightCharsConjunction.SelectedValue = GroupConjunction.All.ToString();

        rbEngineAny.Checked = true;
        rbEngineJet.Checked = rbEnginePiston.Checked = rbEngineTurbine.Checked = rbEngineTurboprop.Checked = false;
        rbInstanceAny.Checked = true;
        rbInstanceReal.Checked = rbInstanceTrainingDevices.Checked = false;

        // clear the catclasses
        foreach (ListItem li in cklCatClass.Items)
            li.Selected = false;

        // Clear the flight properties
        foreach (ListItem li in cklCustomProps.Items)
            li.Selected = false;
        cmbPropertiesConjunction.SelectedValue = GroupConjunction.Any.ToString();

        // reset any cached types
        TypeNames.Clear();

        txtQueryName.Text = string.Empty;

        // Force the next "get" of the restriction to regenerate
        m_fq = null;
    }

    #region Refresh Form for Query helper functions
    private void UncheckDates()
    {
        rbAllTime.Checked = rbYTD.Checked = rbPrevYear.Checked = rbThisMonth.Checked = rbPrevMonth.Checked = rbCustom.Checked = rbTrailing30.Checked = rbTrailing90.Checked = rbTrailing6Months.Checked = rbTrailing12.Checked = false;
    }

    private void DateToForm()
    {
        UncheckDates();
        switch (m_fq.DateRange)
        {
            case FlightQuery.DateRanges.AllTime:
                rbAllTime.Checked = true;
                break;
            case FlightQuery.DateRanges.Custom:
                rbCustom.Checked = true;
                mfbTIDateFrom.Date = m_fq.DateMin;
                mfbTIDateTo.Date = m_fq.DateMax;
                break;
            case FlightQuery.DateRanges.PrevMonth:
                rbPrevMonth.Checked = true;
                break;
            case FlightQuery.DateRanges.PrevYear:
                rbPrevYear.Checked = true;
                break;
            case FlightQuery.DateRanges.Tailing6Months:
                rbTrailing6Months.Checked = true;
                break;
            case FlightQuery.DateRanges.ThisMonth:
                rbThisMonth.Checked = true;
                break;
            case FlightQuery.DateRanges.Trailing12Months:
                rbTrailing12.Checked = true;
                break;
            case FlightQuery.DateRanges.Trailing30:
                rbTrailing30.Checked = true;
                break;
            case FlightQuery.DateRanges.Trailing90:
                rbTrailing90.Checked = true;
                break;
            case FlightQuery.DateRanges.YTD:
                rbYTD.Checked = true;
                break;
        }
    }

    private void AirportsToForm()
    {
        txtAirports.Text = String.Join(Resources.LocalizedText.LocalizedSpace, m_fq.AirportList);

        rblFlightDistance.SelectedValue = ((int) m_fq.Distance).ToString(CultureInfo.InvariantCulture);

        cpeAirports.Collapsed = m_fq.AirportList.Count == 0 && m_fq.Distance == FlightQuery.FlightDistance.AllFlights;
        cpeAirports.ClientState = cpeAirports.Collapsed.ToString(CultureInfo.InvariantCulture);
    }

    private void FlightCharacteristicsToForm()
    {
        ckFSLanding.Checked = m_fq.HasFullStopLandings;
        ckNightLandings.Checked = m_fq.HasNightLandings;
        ckAnyLandings.Checked = m_fq.HasLandings;
        ckApproaches.Checked = m_fq.HasApproaches;
        ckHolds.Checked = m_fq.HasHolds;
        ckHasTelemetry.Checked = m_fq.HasTelemetry;
        ckHasImages.Checked = m_fq.HasImages;

        ckXC.Checked = m_fq.HasXC;
        ckSimIMC.Checked = m_fq.HasSimIMCTime;
        ckIMC.Checked = m_fq.HasIMC;
        ckAnyInstrument.Checked = m_fq.HasAnyInstrument;
        ckNight.Checked = m_fq.HasNight;
        ckPublic.Checked = m_fq.IsPublic;
        ckGroundSim.Checked = m_fq.HasGroundSim;

        ckDual.Checked = m_fq.HasDual;
        ckCFI.Checked = m_fq.HasCFI;
        ckSIC.Checked = m_fq.HasSIC;
        ckPIC.Checked = m_fq.HasPIC;
        ckTotal.Checked = m_fq.HasTotalTime;
        ckIsSigned.Checked = m_fq.IsSigned;

        cpeFlightCharacteristics.Collapsed = !(ckFSLanding.Checked || ckNight.Checked || ckApproaches.Checked || ckHolds.Checked || ckHasTelemetry.Checked || ckHasImages.Checked ||
            ckXC.Checked || ckSimIMC.Checked || ckIMC.Checked || ckNight.Checked || ckPublic.Checked ||
            ckAnyLandings.Checked || ckAnyInstrument.Checked || ckGroundSim.Checked || ckTotal.Checked ||
            ckDual.Checked || ckCFI.Checked || ckSIC.Checked || ckPIC.Checked || ckIsSigned.Checked);
        cpeFlightCharacteristics.ClientState = cpeFlightCharacteristics.Collapsed.ToString(CultureInfo.InvariantCulture);

        cmbFlightCharsConjunction.SelectedValue = m_fq.FlightCharacteristicsConjunction.ToString();
    }

    private void AircraftToForm()
    {
        List<Aircraft> lst = new List<Aircraft>(m_fq.AircraftList);
        foreach (ListItem li in cklAircraft.Items)
            if (li.Selected = lst.Exists(ac => ac.AircraftID == Convert.ToInt32(li.Value, CultureInfo.InvariantCulture)))
                cpeAirplanes.Collapsed = false;
        cpeAirplanes.ClientState = cpeAirplanes.Collapsed.ToString(CultureInfo.InvariantCulture);
    }

    private void ModelsToForm()
    {
        List<MakeModel> lst = new List<MakeModel>(m_fq.MakeList);
        foreach (ListItem li in cklMakes.Items)
            if (li.Selected = lst.Exists(mm => mm.MakeModelID == Convert.ToInt32(li.Value, CultureInfo.InvariantCulture)))
                cpeMakes.Collapsed = false;
        cpeMakes.ClientState = cpeMakes.Collapsed.ToString(CultureInfo.InvariantCulture);
        txtModelNameText.Text = m_fq.ModelName;
    }

    private void CategoryClassesToForm()
    {
        List<CategoryClass> lst = new List<CategoryClass>(m_fq.CatClasses);

        foreach (ListItem li in cklCatClass.Items)
            if (li.Selected = lst.Exists(cc => cc.IdCatClass == (CategoryClass.CatClassID)Convert.ToInt32(li.Value, CultureInfo.InvariantCulture)))
                cpeCatClass.Collapsed = false;
        cpeCatClass.ClientState = cpeCatClass.Collapsed.ToString(CultureInfo.InvariantCulture);
    }

    private void AircraftCharacteristicsToForm()
    {
        ckTailwheel.Checked = m_fq.IsTailwheel;
        ckHighPerf.Checked = m_fq.IsHighPerformance;
        ckGlass.Checked = m_fq.IsGlass;
        ckTAA.Checked = m_fq.IsTechnicallyAdvanced;
        ckMotorGlider.Checked = m_fq.IsMotorglider;
        ckMultiEngineHeli.Checked = m_fq.IsMultiEngineHeli;
        ckComplex.Checked = m_fq.IsComplex;
        ckRetract.Checked = m_fq.IsRetract;
        ckProp.Checked = m_fq.IsConstantSpeedProp;
        ckCowl.Checked = m_fq.HasFlaps;
        switch (m_fq.EngineType)
        {
            case FlightQuery.EngineTypeRestriction.AllEngines:
                rbEngineAny.Checked = true;
                break;
            case FlightQuery.EngineTypeRestriction.AnyTurbine:
                rbEngineTurbine.Checked = true;
                break;
            case FlightQuery.EngineTypeRestriction.Electric:
                rbEngineElectric.Checked = true;
                break;
            case FlightQuery.EngineTypeRestriction.Jet:
                rbEngineJet.Checked = true;
                break;
            case FlightQuery.EngineTypeRestriction.Piston:
                rbEnginePiston.Checked = true;
                break;
            case FlightQuery.EngineTypeRestriction.Turboprop:
                rbEngineTurboprop.Checked = true;
                break;
        }
        switch (m_fq.AircraftInstanceTypes)
        {
            case FlightQuery.AircraftInstanceRestriction.AllAircraft:
                rbInstanceAny.Checked = true;
                break;
            case FlightQuery.AircraftInstanceRestriction.RealOnly:
                rbInstanceReal.Checked = true;
                break;
            case FlightQuery.AircraftInstanceRestriction.TrainingOnly:
                rbInstanceTrainingDevices.Checked = true;
                break;
        }

        cpeAircraftChars.Collapsed = !(ckTailwheel.Checked || ckHighPerf.Checked || ckGlass.Checked || ckTAA.Checked || ckMotorGlider.Checked || ckMultiEngineHeli.Checked ||
            ckComplex.Checked || ckRetract.Checked || ckProp.Checked || ckCowl.Checked ||
            m_fq.EngineType != FlightQuery.EngineTypeRestriction.AllEngines || m_fq.AircraftInstanceTypes != FlightQuery.AircraftInstanceRestriction.AllAircraft);
        cpeAircraftChars.ClientState = cpeAircraftChars.Collapsed.ToString(CultureInfo.InvariantCulture);
    }

    private void CustomPropertiesToForm()
    {
        List<CustomPropertyType> lst = new List<CustomPropertyType>(m_fq.PropertyTypes);
        foreach (ListItem li in cklCustomProps.Items)
            li.Selected = lst.Exists(cpt => cpt.PropTypeID == Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));

        cmbPropertiesConjunction.SelectedValue = m_fq.PropertiesConjunction.ToString();
    }
    #endregion

    protected void RefreshFormForQuery()
    {
        SetUpForUser(); // in case it's not yet set up.
        InitialCollapseState = true;

        if (m_fq == null)
            return;

        txtRestrict.Text = m_fq.GeneralText;

        DateToForm();
        FlightCharacteristicsToForm();
        AirportsToForm();
        AircraftToForm();
        ModelsToForm();
        CategoryClassesToForm();
        AircraftCharacteristicsToForm();
        CustomPropertiesToForm();

        // TypeNames aren't shown to the user, but we want to preserve them
        TypeNames = m_fq.TypeNames;
    }

    /// <summary>
    /// Initializes a simple free-form text search.
    /// </summary>
    public string SimpleSearchText
    {
        get { return txtRestrict.Text; }
        set
        {
            txtRestrict.Text = value;
            GetFlightQuery();
        }
    }

    /// <summary>
    /// Initialize an airport search
    /// </summary>
    public string AirportSearch
    {
        get { return txtAirports.Text; }
        set 
        {
            txtAirports.Text = value ?? throw new ArgumentNullException(nameof(value));
            cpeAirports.Collapsed = (value.Length == 0);
            cpeAirports.ClientState = cpeAirports.Collapsed.ToString(CultureInfo.InvariantCulture);
            GetFlightQuery(); 
        }
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        // we set these here in Page_Init so that there is a value (the query can be read before Page_Load gets called!)
        // and the value does not depend on the correct locale being selected (which has not yet happened when this is called)
        mfbTIDateFrom.Date = mfbTIDateFrom.DefaultDate = DateTime.MinValue;
        mfbTIDateTo.Date = mfbTIDateTo.DefaultDate = DateTime.MinValue;
        UncheckDates();
        rbAllTime.Checked = true;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(Username))
            Username = Page.User.Identity.Name;

        if (!IsPostBack)
        {
            SetUpForUser();
            UpdateSavedQueries();

            mfbTIDateFrom.TextControl.Attributes["onfocus"] = mfbTIDateTo.TextControl.Attributes["onfocus"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').checked = true;", rbCustom.ClientID);
        }

        SetUpDateJScript();
    }

    private bool fHasBeenSetUp; // have we already been initialized for the user?

    protected void SetUpPropertiesForUser(bool fIncludeAll)
    {
        CustomPropertyType[] rgCpt = CustomPropertyType.GetCustomPropertyTypes(Username);
        List<CustomPropertyType> al = new List<CustomPropertyType>(rgCpt);
        int cAllProps = al.Count;

        HashSet<int> hsBlackList = fIncludeAll ? new HashSet<int>(Profile.GetUser(Username).BlocklistedProperties) : new HashSet<int>();

        // Props to include are favorites OR it's in the query OR (Include ALL and in blacklist)
        // So remove anything that is NOT favorite AND NOT in the query AND NOT (include all and in blacklist)
        al.RemoveAll(cpt => !cpt.IsFavorite && !(m_fq != null && m_fq.PropertyTypes.Contains(cpt)) && !(fIncludeAll && hsBlackList.Contains(cpt.PropTypeID)));

        lnkShowAllProps.Visible = !fIncludeAll && cAllProps > al.Count;

        if (al.Count == 0)
            pnlCustomProps.Visible = false;
        else
        {
            cklCustomProps.DataSource = al;
            cklCustomProps.DataBind();
        }
    }

    protected void SetUpForUser()
    {
        if (fHasBeenSetUp)
            return;

        if (String.IsNullOrEmpty(Username))
            Username = Page.User.Identity.Name;

        UserAircraft ua = new UserAircraft(Username);
        Aircraft[] rgac = ua.GetAircraftForUser();
        Aircraft[] rgacActive = Array.FindAll(rgac, aircraft => !aircraft.HideFromSelection);

        // Hide inactive aircraft unless 
        // (a) all aircraft are active, or 
        // (b) the current query references inactive aircraft
        // (c) pnlshowAllAircraft is invisible (indicating that it has been clicked)
        bool fShowAll = !pnlShowAllAircraft.Visible || rgacActive.Length == rgac.Length || Restriction.AircraftList.FirstOrDefault(ac => ac.HideFromSelection) != null;
        if (fShowAll)
            pnlShowAllAircraft.Visible = false;
        cklAircraft.DataSource = fShowAll ? rgac : rgacActive;
        cklAircraft.DataBind();
        ckAllAircraft.Visible = cklAircraft.Items.Count > 4;
        
        cklMakes.DataSource = MakeModel.ModelsForAircraft(rgac);
        cklMakes.DataBind();
        ckAllMakes.Visible = cklMakes.Items.Count > 4;

        cklCatClass.DataSource = CategoryClass.CategoryClasses();
        cklCatClass.DataBind();

        SetUpPropertiesForUser(false);

        fHasBeenSetUp = true;
    }

    protected void UpdateSavedQueries()
    {
        gvSavedQueries.DataSource = CannedQuery.QueriesForUser(Page.User.Identity.Name);
        gvSavedQueries.DataBind();
    }

    protected void SetUpDateJScript()
    {
        string szDates = String.Format(CultureInfo.InvariantCulture, @"
function setDates(isCustom)
{{
    var objFromDate = document.getElementById('{0}');
    var objToDate = document.getElementById('{1}');

    if (!isCustom)
    {{
            objFromDate.value = '';
            objToDate.value = '';
    }}
    return false;
}}",
                mfbTIDateFrom.ClientBoxID,
                mfbTIDateTo.ClientBoxID);

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "setDates", szDates, true);

        rbAllTime.Attributes["onclick"] = rbYTD.Attributes["onclick"] = rbPrevYear.Attributes["onclick"] =
            rbThisMonth.Attributes["onclick"] = rbPrevMonth.Attributes["onclick"] = rbTrailing30.Attributes["onclick"] =
            rbTrailing90.Attributes["onclick"] = rbTrailing6Months.Attributes["onclick"] = rbTrailing12.Attributes["onclick"] =
            "javascript:setDates(false);";

        rbCustom.Attributes["onclick"] = "javascript:setDates(true);";
    }

    #region update query from form
    /// <summary>
    /// Initializes the date parts of the flight query from the form
    /// </summary>
    private void QueryDateFromForm()
    {
        m_fq.DateMin = mfbTIDateFrom.Date;
        m_fq.DateMax = mfbTIDateTo.Date;

        // validate any dates
        bool fHasMinDate = (m_fq.DateMin.CompareTo(mfbTIDateFrom.DefaultDate) != 0);
        bool fHasMaxDate = (m_fq.DateMax.CompareTo(mfbTIDateTo.DefaultDate) != 0);

        // Custom selected but no date - call it "All Time" and fall through.
        if (rbCustom.Checked && !fHasMinDate && !fHasMaxDate)
        {
            rbAllTime.Checked = true;
            rbCustom.Checked = false;
        }

        // Or, custom not selected but dates provided - switch to custom IF
        // either date is specified or two dates are specified in ascending order.
        // else default back to all time.
        if (rbCustom.Checked || fHasMaxDate || fHasMinDate)  // Custom if the radio button is checked OR if either date was specified
        {
            // It's OK for custom if either only one date is specified, or if both dates are specified and max > min
            rbCustom.Checked = !fHasMaxDate || !fHasMinDate || (m_fq.DateMax.CompareTo(m_fq.DateMin) >= 0);
            if (rbCustom.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.Custom;
            else
            {
                rbAllTime.Checked = true;
                mfbTIDateFrom.Date = mfbTIDateFrom.DefaultDate;
                mfbTIDateTo.Date = mfbTIDateTo.DefaultDate;
            }
        }
        else
        {
            if (rbAllTime.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.AllTime;
            else if (rbTrailing12.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.Trailing12Months;
            else if (rbYTD.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.YTD;
            else if (rbPrevYear.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.PrevYear;
            else if (rbTrailing6Months.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.Tailing6Months;
            else if (rbThisMonth.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.ThisMonth;
            else if (rbPrevMonth.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.PrevMonth;
            else if (rbTrailing30.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.Trailing30;
            else if (rbTrailing90.Checked)
                m_fq.DateRange = FlightQuery.DateRanges.Trailing90;
        }
    }

    /// <summary>
    /// Updates the flight query from the aircraft checkbox
    /// </summary>
    private void AircraftFromForm()
    {
        // Airplanes:
        if (cklAircraft.SelectedIndex >= 0)
        {
            UserAircraft ua = new UserAircraft(Username);
            Aircraft[] rgAircraft = ua.GetAircraftForUser();

            m_fq.AircraftList.Clear();
            foreach (ListItem li in cklAircraft.Items)
                if (li.Selected)
                {
                    // ac can be null if it's been deleted between the form being populated and this method being called.  Ignore it in that case.
                    Aircraft acQuery = Array.Find(rgAircraft, ac => ac.AircraftID == Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));
                    if (acQuery != null)
                        m_fq.AircraftList.Add(acQuery);
                }
        }
    }

    /// <summary>
    /// Updates the flight query with the specified models
    /// </summary>
    private void ModelsFromForm()
    {
        if (cklMakes.SelectedIndex >= 0)
        {
            m_fq.MakeList.Clear();

            foreach (ListItem li in cklMakes.Items)
                if (li.Selected)
                    m_fq.MakeList.Add(MakeModel.GetModel(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture)));
        }
        m_fq.ModelName = txtModelNameText.Text;
    }

    /// <summary>
    /// Updates the flight query with the aircraft characteristics.
    /// </summary>
    private void AircraftCharacteristicsFromForm()
    {
        m_fq.IsTailwheel = ckTailwheel.Checked;
        m_fq.IsHighPerformance = ckHighPerf.Checked;
        m_fq.IsGlass = ckGlass.Checked;
        m_fq.IsTechnicallyAdvanced = ckTAA.Checked;
        m_fq.IsComplex = ckComplex.Checked;
        m_fq.HasFlaps = ckCowl.Checked;
        m_fq.IsConstantSpeedProp = ckProp.Checked;
        m_fq.IsRetract = ckRetract.Checked;
        m_fq.IsMotorglider = ckMotorGlider.Checked;
        m_fq.IsMultiEngineHeli = ckMultiEngineHeli.Checked;

        m_fq.EngineType = rbEnginePiston.Checked
            ? FlightQuery.EngineTypeRestriction.Piston
            : rbEngineTurboprop.Checked
            ? FlightQuery.EngineTypeRestriction.Turboprop
            : rbEngineJet.Checked
            ? FlightQuery.EngineTypeRestriction.Jet
            : rbEngineTurbine.Checked
            ? FlightQuery.EngineTypeRestriction.AnyTurbine
            : rbEngineElectric.Checked ? FlightQuery.EngineTypeRestriction.Electric : FlightQuery.EngineTypeRestriction.AllEngines;

        m_fq.AircraftInstanceTypes = rbInstanceReal.Checked
            ? FlightQuery.AircraftInstanceRestriction.RealOnly
            : rbInstanceTrainingDevices.Checked
            ? FlightQuery.AircraftInstanceRestriction.TrainingOnly
            : FlightQuery.AircraftInstanceRestriction.AllAircraft;
    }

    /// <summary>
    /// Updates the flgiht query with the flight characteristics
    /// </summary>
    private void FlightCharacteristicsFromForm()
    {
        m_fq.HasApproaches = ckApproaches.Checked;
        m_fq.HasCFI = ckCFI.Checked;
        m_fq.HasDual = ckDual.Checked;
        m_fq.HasFullStopLandings = ckFSLanding.Checked;
        m_fq.HasGroundSim = ckGroundSim.Checked;
        m_fq.HasHolds = ckHolds.Checked;
        m_fq.HasIMC = ckIMC.Checked;
        m_fq.HasAnyInstrument = ckAnyInstrument.Checked;
        m_fq.HasNight = ckNight.Checked;
        m_fq.HasNightLandings = ckNightLandings.Checked;
        m_fq.HasLandings = ckAnyLandings.Checked;
        m_fq.HasPIC = ckPIC.Checked;
        m_fq.HasSIC = ckSIC.Checked;
        m_fq.HasTotalTime = ckTotal.Checked;
        m_fq.HasSimIMCTime = ckSimIMC.Checked;
        m_fq.HasXC = ckXC.Checked;
        m_fq.IsPublic = ckPublic.Checked;
        m_fq.IsSigned = ckIsSigned.Checked;
        m_fq.HasTelemetry = ckHasTelemetry.Checked;
        m_fq.HasImages = ckHasImages.Checked;
        m_fq.FlightCharacteristicsConjunction = (GroupConjunction)Enum.Parse(typeof(GroupConjunction), cmbFlightCharsConjunction.SelectedValue);
    }

    /// <summary>
    /// Updates the flight query with the category/class requested
    /// </summary>
    private void CatClassFromForm()
    {
        if (cklCatClass.SelectedIndex >= 0)
        {
            List<CategoryClass> rgcc = new List<CategoryClass>(CategoryClass.CategoryClasses());
            m_fq.CatClasses.Clear();
            foreach (ListItem li in cklCatClass.Items)
            {
                int idCc = Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
                if (li.Selected)
                    m_fq.CatClasses.Add(rgcc.Find(cc => (int)cc.IdCatClass == idCc));
            }
        }
    }

    /// <summary>
    /// Updates the flight query with the requested flight properties
    /// </summary>
    private void CustomPropsFromForm()
    {
        if (cklCustomProps.SelectedIndex >= 0)
        {
            m_fq.PropertyTypes.Clear();
            CustomPropertyType[] rgcptAll = CustomPropertyType.GetCustomPropertyTypes();

            foreach (ListItem li in cklCustomProps.Items)
                if (li.Selected)
                    m_fq.PropertyTypes.Add(Array.Find<CustomPropertyType>(rgcptAll, cpt => cpt.PropTypeID == Convert.ToInt32(li.Value, CultureInfo.InvariantCulture)));

            m_fq.PropertiesConjunction = (GroupConjunction)Enum.Parse(typeof(GroupConjunction), cmbPropertiesConjunction.SelectedValue);
        }
    }
    #endregion

    private FlightQuery GetFlightQuery()
    {
        if (String.IsNullOrEmpty(Username))
            Username = Page.User.Identity.Name;

        m_fq = new CannedQuery(Username) { DateRange = FlightQuery.DateRanges.AllTime, QueryName = txtQueryName.Text };

        // General text
        m_fq.GeneralText = txtRestrict.Text;

        // Airports:
        if (txtAirports.Text.Length > 0)
        {
            m_fq.AirportList.Clear();
            MatchCollection mc = regAirports.Matches(txtAirports.Text);
            foreach (Match m in mc)
                if (!String.IsNullOrEmpty(m.Value))
                    m_fq.AirportList.Add(m.Value);
        }

        m_fq.Distance = (FlightQuery.FlightDistance)Convert.ToInt32(rblFlightDistance.SelectedValue, CultureInfo.InvariantCulture);

        // Remaining sections
        QueryDateFromForm();
        AircraftFromForm();
        ModelsFromForm();
        AircraftCharacteristicsFromForm();
        FlightCharacteristicsFromForm();
        CatClassFromForm();
        CustomPropsFromForm();

        // Typenames aren't exposed to the user; reset them here.
        m_fq.TypeNames.Clear();
        foreach (string sz in TypeNames)
            m_fq.TypeNames.Add(sz);

        // Save it, if it was given a name, before refreshing it, since refresh can be destructive (e.g., can change trailing:2D to actual dates.
        // We'll clear the queryname field at the same time to prevent doing this multiple times.
        if (m_fq is CannedQuery cq && !String.IsNullOrEmpty(cq.QueryName))
        {
            cq.Commit();
            UpdateSavedQueries();
            txtQueryName.Text = string.Empty;
        }


        m_fq.Refresh();

        return m_fq;
    }

    protected void DoSearch()
    {
        GetFlightQuery();

        QuerySubmitted?.Invoke(this, new FlightQueryEventArgs(m_fq));
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        DoSearch();
    }

    protected void btnReset_Click(object sender, EventArgs e)
    {
        ClearForm();

        // Page should be valid at this point; no need to verify validity.
        Reset?.Invoke(sender, new FlightQueryEventArgs(Restriction));
    }

    protected void gvSavedQueries_RowCommand(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (!int.TryParse(e.CommandArgument.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int idRow))
            return;

        CannedQuery cq = CannedQuery.QueriesForUser(Page.User.Identity.Name).ElementAt(idRow);

        if (e.CommandName.CompareOrdinal("_Delete") == 0)
        {
            cq.Delete();
            UpdateSavedQueries();
        }
        else if (e.CommandName.CompareOrdinal("_Load") == 0)
        {
            Restriction = cq;
            RefreshFormForQuery();
            txtQueryName.Text = string.Empty;
            DoSearch();
        }
    }

    protected void lnkShowAllAircraft_Click(object sender, EventArgs e)
    {
        m_fq = GetFlightQuery();
        pnlShowAllAircraft.Visible = false;
        RefreshFormForQuery();
    }

    protected void ckAllAircraft_CheckedChanged(object sender, EventArgs e)
    {
        foreach (ListItem li in cklAircraft.Items)
            li.Selected = ckAllAircraft.Checked;
    }

    protected void ckAllMakes_CheckedChanged(object sender, EventArgs e)
    {
        foreach (ListItem li in cklMakes.Items)
            li.Selected = ckAllMakes.Checked;
    }

    protected void ckAllCatClass_CheckedChanged(object sender, EventArgs e)
    {
        foreach (ListItem li in cklCatClass.Items)
            li.Selected = ckAllCatClass.Checked;
    }

    protected void lnkShowAllProps_Click(object sender, EventArgs e)
    {
        m_fq = GetFlightQuery();
        lnkShowAllProps.Visible = false;
        SetUpPropertiesForUser(true);
        CustomPropertiesToForm();
    }
}
