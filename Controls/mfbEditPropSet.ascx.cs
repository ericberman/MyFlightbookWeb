using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2013-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditPropSet : System.Web.UI.UserControl
{
    private const string vsActiveProps = "vsActiveProps";
    private const string vsPropVals = "vsPropVals";

    #region properties
    private List<CustomFlightProperty> m_cfpActive = null;
    /// <summary>
    /// These are the properties that are instantiated to edit
    /// </summary>
    private List<CustomFlightProperty> ActiveProperties
    {
        get
        {
            if (m_cfpActive == null)
                m_cfpActive = new List<CustomFlightProperty>();
            return m_cfpActive;
        }
    }

    // Note: On postbacks initiated within EditFlight, any properties that were added to the list but not given values are dropped.  
    // This is because the editflight form is distilling the list before initiating the postback.
    /// <summary>
    /// A set of the property types being actively edited.
    /// </summary>
    private List<int> ActivePropTypes
    {
        get
        {
            if (ViewState[vsActiveProps] == null)
                ViewState[vsActiveProps] = new List<int>();
            return (List<int>)ViewState[vsActiveProps];
        }
    }

    /// <summary>
    /// Maps existing properties to IDs based on the proptypeid.
    /// </summary>
    private Dictionary<int, int> ExistingPropIDs
    {
        get
        {
            if (ViewState[vsPropVals] == null)
                ViewState[vsPropVals] = new Dictionary<int, int>();
            return (Dictionary<int, int>)ViewState[vsPropVals];
        }
    }

    /// <summary>
    /// The properties that we want to ensure we have.  E.g., from a flight.
    /// </summary>
    private List<CustomFlightProperty> Properties { get; set; }

    private List<CustomFlightProperty> m_propertiesFromPropSet = null;

    /// <summary>
    /// Gets/sets a list of properties to edit.
    /// When getting, this returns ALL ACTIVE properties; it needs to be distilled:
    ///  - Delete any property that has a default value
    ///  - Update or add the remainders.
    /// </summary>
    private List<CustomFlightProperty> PropertiesFromPropSet
    {
        get
        {
            if (m_propertiesFromPropSet == null)
            {
                m_propertiesFromPropSet = new List<CustomFlightProperty>();
                foreach (Controls_mfbEditProp ep in plcHolderProps.Controls)
                    m_propertiesFromPropSet.Add(ep.FlightProperty);
            }
            return m_propertiesFromPropSet;
        }
    }

    public void SetFlightProperties(IEnumerable<CustomFlightProperty> rgcfp)
    {
        if (rgcfp == null)
            return;
        Properties = new List<CustomFlightProperty>(rgcfp);
        foreach (CustomFlightProperty cfp in rgcfp)
            if (!cfp.IsDefaultValue && cfp.IsExisting)
                ExistingPropIDs[cfp.PropTypeID] = cfp.PropID;
        SegregateProperties();              // add the new property to the list
        PopulateControls();                 // And re-populate.
    }

    /// <summary>
    /// Returns a DISTILLED set of properties.  Specifically, every property that is a default value is removed (and, if it is an existing property, it is deleted as well).
    /// </summary>
    public IEnumerable<CustomFlightProperty> DistilledList
    {
        get
        {
            List<CustomFlightProperty> lst = PropertiesFromPropSet;
            bool fPropsDeleted = false;
            lst.ForEach((cfp) =>
            {
                if (cfp.IsDefaultValue && cfp.IsExisting)
                {
                    cfp.DeleteProperty();
                    if (ExistingPropIDs.ContainsKey(cfp.PropTypeID))
                        ExistingPropIDs.Remove(cfp.PropTypeID);
                    fPropsDeleted = true;
                }
            });

            if (fPropsDeleted)
                MyFlightbook.Profile.GetUser(Page.User.Identity.Name).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);

            lst.RemoveAll(cfp => cfp.IsDefaultValue);
            return lst;
        }
    }

    const string keyViewStateXFill = "vsXF";
    /// <summary>
    /// The ClientID of the source control for cross-filling.
    /// </summary>
    public string CrossFillSourceClientID
    {
        get { return (string)ViewState[keyViewStateXFill]; }
        set { ViewState[keyViewStateXFill] = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        RecreateControls(); // set them up for postback or initial
        // Javascript below adapted from http://www.aspsnippets.com/Articles/Filter-and-Search-ASP.Net-DropDownList-items-using-JavaScript.aspx
        string szFilterScript = @"
        var ddlText, ddlValue, ddl, lblMesg;

        function CacheItems() {
            ddlText = new Array();
            ddlValue = new Array();
            ddl = document.getElementById(""" + cmbPropsToAdd.ClientID + @""");
            lblMesg = document.getElementById(""" + lblFilterMessage.ClientID + @""");
            for (var i = 0; i < ddl.options.length; i++)
            {
                ddlText[ddlText.length] = ddl.options[i].text;
                ddlValue[ddlValue.length] = ddl.options[i].value;
            }
        }
        window.onload = CacheItems;

        function FilterItems(value) {
            ddl.options.length = 0;
            value = value.toUpperCase();
            for (var i = 0; i < ddlText.length; i++)
            {
                if (i == 0 || ddlText[i].toUpperCase().indexOf(value) != -1)
                    AddItem(ddlText[i], ddlValue[i]);
            }

            lblMesg.innerHTML = ddl.options.length - 1  + """ + Resources.LogbookEntry.PropertiesFound + @""";
        }

        function AddItem(text, value) {
            var opt = document.createElement(""option"");
            opt.text = text;
            opt.value = value;
            ddl.options.add(opt);
        }";

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "propFilter", szFilterScript, true);
    }

    protected void SegregateProperties()
    {
        List<CustomPropertyType> lstRemainingProps = new List<CustomPropertyType>();

        ActiveProperties.Clear();
        ActivePropTypes.Clear();

        // this is cached so we can do it on every call, postback or not
        CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Page.User.Identity.Name : string.Empty);

        foreach (CustomPropertyType cpt in rgCptAll)
        {
            CustomFlightProperty fp = Properties.Find(cfp => cfp.PropTypeID == cpt.PropTypeID);
            if (fp == null && cpt.IsFavorite)
                fp = new CustomFlightProperty(cpt);
            if (fp == null)
                lstRemainingProps.Add(cpt);
            else
            {
                ActiveProperties.Add(fp);
                ActivePropTypes.Add(fp.PropTypeID);
            }
        }

        ActiveProperties.Sort((cfp1, cfp2) => { return cfp1.PropertyType.Title.CompareCurrentCultureIgnoreCase(cfp2.PropertyType.Title); });

        ListItem li = cmbPropsToAdd.Items[0];
        cmbPropsToAdd.Items.Clear();
        cmbPropsToAdd.Items.Add(li);
        cmbPropsToAdd.SelectedValue = string.Empty;    // reset the selection
        cmbPropsToAdd.DataSource = lstRemainingProps;
        cmbPropsToAdd.DataBind();
    }

    private string IDForPropType(CustomPropertyType cpt)
    {
        return String.Format(System.Globalization.CultureInfo.InvariantCulture, "editProp{0}", cpt.PropTypeID);
    }

    private void InsertEditProp(CustomFlightProperty cfp)
    {
        Controls_mfbEditProp ep = (Controls_mfbEditProp)LoadControl("~/Controls/mfbEditProp.ascx");
        // Add it to the placeholder so that the client ID works, then set the client ID before setting the property so that it picks up cross-fill
        plcHolderProps.Controls.Add(ep);
        ep.CrossFillSourceClientID = CrossFillSourceClientID;
        ep.ID = IDForPropType(cfp.PropertyType);
        ep.FlightProperty = cfp;

        pnlProps.CssClass = (plcHolderProps.Controls.Count > 10) ? "propItemContainerOverflow" : "propItemContainer";
    }

    /// <summary>
    /// Recreates the combobox and propedit controls so that they exist for postback events.
    /// </summary>
    protected void RecreateControls()
    {
        if (plcHolderProps.Controls.Count > 0)  // don't do this if we've already set them up.
            return;

        List<CustomPropertyType> lstAll = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());
        foreach (int idPropType in ActivePropTypes)
        {
            CustomPropertyType cpt = lstAll.Find(c => c.PropTypeID == idPropType);
            CustomFlightProperty cfp = new CustomFlightProperty(cpt);
            if (ExistingPropIDs.ContainsKey(idPropType))
                cfp.PropID = ExistingPropIDs[idPropType];
            InsertEditProp(cfp);
            lstAll.Remove(cpt); // since it's here, make sure it isn't in the list of available types; we'll bind this below.

        }

        // recreate combo box - huge viewstate, so we just recreate it each time.
        // This loses the selected value, so we have to grab that directly from the form.
        cmbPropsToAdd.DataSource = lstAll;
        cmbPropsToAdd.DataBind();
        if (IsPostBack)
            cmbPropsToAdd.SelectedValue = Request.Form[cmbPropsToAdd.UniqueID];
    }

    protected void PopulateControls()
    {
        plcHolderProps.Controls.Clear();
        foreach (CustomFlightProperty fp in ActiveProperties)
            InsertEditProp(fp);
    }

    protected void cmbPropsToAdd_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(cmbPropsToAdd.SelectedValue))
            return;
        int idPropType = Convert.ToInt32(cmbPropsToAdd.SelectedValue);
        CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Page.User.Identity.Name : string.Empty);
        CustomPropertyType cpt = rgCptAll.First((cpt2) => {return cpt2.PropTypeID == idPropType;});

        if (cpt == null)
            throw new MyFlightbookException(String.Format("Custom property type with id {0} not found!", idPropType));

        Properties = PropertiesFromPropSet;             // Pick up any changes from the existing child controls, to preserve across postback
        Properties.Add(new CustomFlightProperty(cpt));
        SegregateProperties();                          // add the new property to the list
        PopulateControls();                             // And re-populate.
        txtFilter.Text = string.Empty;
        CollapsiblePanelExtender1.Collapsed = true;
    }
}