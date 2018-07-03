using MyFlightbook;
using MyFlightbook.Printing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_PrintOptions : System.Web.UI.UserControl
{
    public event EventHandler<PrintingOptionsEventArgs> OptionsChanged = null;

    private DropDownList[] m_optionalColumnsDropdowns;

    private DropDownList[] OptionalColumnDropDowns
    {
        get
        {
            if (m_optionalColumnsDropdowns == null)
                m_optionalColumnsDropdowns = new DropDownList[] { cmbOptionalColumn1, cmbOptionalColumn2, cmbOptionalColumn3, cmbOptionalColumn4 };
            return m_optionalColumnsDropdowns;
        }
    }

    private PrintingOptions m_options = null;
    public PrintingOptions Options
    {
        get
        {
            if (m_options == null)
                m_options = new PrintingOptions();

            m_options.FlightsPerPage = Convert.ToInt32(cmbFlightsPerPage.SelectedValue, CultureInfo.InvariantCulture);
            m_options.IncludeImages = ckIncludeImages.Checked;
            m_options.Layout = (PrintLayoutType) Enum.Parse(typeof(PrintLayoutType), cmbLayout.SelectedValue);
            m_options.PropertySeparator = (PrintingOptions.PropertySeparatorType)Enum.Parse(typeof(PrintingOptions.PropertySeparatorType), rblPropertySeparator.SelectedValue);

            List<int> l = new List<int>();
            foreach (ListItem li in cklProperties.Items)
                if (li.Selected)
                    l.Add(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));
            m_options.ExcludedPropertyIDs = l.ToArray();

            List<OptionalColumn> lst = new List<OptionalColumn>();
            foreach (DropDownList ddl in OptionalColumnDropDowns)
                AddOptionalColumnForValue(ddl.SelectedValue, lst);
            m_options.OptionalColumns = lst.ToArray();

            return m_options;
        }
        set
        {
            if (value == null)
                throw new ArgumentNullException("value");
            m_options = value;
            cmbFlightsPerPage.SelectedValue = m_options.FlightsPerPage.ToString(CultureInfo.InvariantCulture);
            ckIncludeImages.Checked = m_options.IncludeImages;
            cmbLayout.SelectedValue = m_options.Layout.ToString();
            rblPropertySeparator.SelectedValue = m_options.PropertySeparator.ToString();

            List<int> lst = new List<int>(m_options.ExcludedPropertyIDs);
            foreach (ListItem li in cklProperties.Items)
            {
                int id = Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
                li.Selected = lst.Contains(id);
            }

            for (int i = 0; i < OptionalColumnDropDowns.Length; i++)
            {
                OptionalColumnDropDowns[i].SelectedValue = string.Empty;   //just in case
                if (value.OptionalColumns != null && value.OptionalColumns.Length > i)
                {
                    OptionalColumn oc = value.OptionalColumns[i];
                    OptionalColumnDropDowns[i].SelectedValue = oc.ColumnType == OptionalColumnType.CustomProp ? oc.IDPropType.ToString(CultureInfo.InvariantCulture) : oc.ColumnType.ToString();
                }
            }

            AdjustForLayoutCapabilities(m_options);
        }
    }

    protected void AdjustForLayoutCapabilities(PrintingOptions po)
    {
        if (po == null)
            throw new ArgumentNullException("po");
        PrintLayout pl = PrintLayout.LayoutForType(po.Layout);
        pnlIncludeImages.Visible = pl.SupportsImages;
        if (!pl.SupportsImages)
            ckIncludeImages.Checked = m_options.IncludeImages = false;
        pnlOptionalColumns.Visible = pl.SupportsOptionalColumns;
    }

    private void AddOptionalColumnForValue(string value, List<OptionalColumn> lst)
    {
        if (String.IsNullOrEmpty(value))
            return;

        if (lst == null)
            throw new ArgumentNullException("lst");

        // Enum.TryParse works with integers as well as symbolic names, so try it first as an integer (i.e., custom property type)
        int idPropType;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out idPropType))
        {
            lst.Add(new OptionalColumn(idPropType));
            return;
        }

        OptionalColumnType oct;
        if (Enum.TryParse<OptionalColumnType>(value, out oct))
        {
            lst.Add(new OptionalColumn(oct));
            return;
        }

        throw new MyFlightbookException("Attempt to create unknown optional column: " + value);
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            for (int i = 3; i <= 20; i++)
                cmbFlightsPerPage.Items.Add(new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintViewXPerPage, i), i.ToString(CultureInfo.InvariantCulture)) { Selected = (i == 15) });

            MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            List<CustomPropertyType> rgcptUser = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name));
            rgcptUser.RemoveAll(cpt => !cpt.IsFavorite && !pf.BlacklistedProperties.Contains(cpt.PropTypeID));
            List<CustomPropertyType> rgcptUserOptionalColumns = rgcptUser.FindAll(cpt => (cpt.Type == CFPPropertyType.cfpDecimal || cpt.Type == CFPPropertyType.cfpInteger) && !cpt.IsNoSum);
            rgcptUser.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
            cklProperties.DataSource = rgcptUser;
            cklProperties.DataBind();

            List<ListItem> lstOptionalColumnDropdowns = new List<ListItem>()
            {
                new ListItem(Resources.LocalizedText.PrintViewOptionalColumnNone, string.Empty),
                new ListItem(Resources.Makes.IsComplex, OptionalColumnType.Complex.ToString()),
                new ListItem(Resources.Makes.IsRetract, OptionalColumnType.Retract.ToString()),
                new ListItem(Resources.Makes.IsTailwheel, OptionalColumnType.Tailwheel.ToString()),
                new ListItem(Resources.Makes.IsHighPerf, OptionalColumnType.HighPerf.ToString()),
                new ListItem(Resources.Makes.IsTurboprop, OptionalColumnType.TurboProp.ToString()),
                new ListItem(Resources.Makes.IsTurbine, OptionalColumnType.Turbine.ToString()),
                new ListItem(Resources.Makes.IsJet, OptionalColumnType.Jet.ToString()),
            };
            rgcptUserOptionalColumns.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
            foreach (CustomPropertyType cpt in rgcptUserOptionalColumns)
                lstOptionalColumnDropdowns.Add(new ListItem(cpt.Title, cpt.PropTypeID.ToString(CultureInfo.InvariantCulture)));
            foreach (DropDownList ddl in OptionalColumnDropDowns)
            {
                ddl.DataSource = lstOptionalColumnDropdowns;
                ddl.DataBind();
            }
        }
    }

    protected void NotifyDelegate()
    {
        if (OptionsChanged != null)
            OptionsChanged(this, new PrintingOptionsEventArgs(Options));
    }
    
    protected void ckIncludeImages_CheckedChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbFlightsPerPage_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbLayout_SelectedIndexChanged(object sender, EventArgs e)
    {
        AdjustForLayoutCapabilities(Options);
        NotifyDelegate();
    }

    protected void rblPropertySeparator_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cklProperties_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbOptionalColumn1_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbOptionalColumn2_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbOptionalColumn3_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }

    protected void cmbOptionalColumn4_SelectedIndexChanged(object sender, EventArgs e)
    {
        NotifyDelegate();
    }
}