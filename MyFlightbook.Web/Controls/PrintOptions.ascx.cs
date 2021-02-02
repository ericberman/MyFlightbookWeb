using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing
{
    public partial class PrintOptions : System.Web.UI.UserControl
    {
        public event EventHandler<PrintingOptionsEventArgs> OptionsChanged;

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

        const string szVSUser = "szvsActiveUser";
        public string UserName
        {
            get
            {
                string szUser = (string)ViewState[szVSUser];
                return String.IsNullOrEmpty(szUser) ? Page.User.Identity.Name : szUser;
            }
            set
            {
                string priorUser = ((string)ViewState[szVSUser]) ?? string.Empty;
                ViewState[szVSUser] = value;
                if (String.Compare(priorUser, value ?? string.Empty, StringComparison.OrdinalIgnoreCase) != 0)
                    SetPropsForUser();
            }
        }

        private PrintingOptions m_options;
        public PrintingOptions Options
        {
            get
            {
                if (m_options == null)
                    m_options = new PrintingOptions();

                m_options.FlightsPerPage = Convert.ToInt32(cmbFlightsPerPage.SelectedValue, CultureInfo.InvariantCulture);
                m_options.IncludeImages = ckIncludeImages.Checked;
                m_options.IncludeSignatures = ckIncludeSignatures.Checked;
                m_options.UseFlightColoring = ckFlightColoring.Checked;
                m_options.BreakAtMonthBoundary = rblForceBreak.SelectedValue.CompareCurrentCultureIgnoreCase("1") == 0;
                m_options.BreakAtYearBoundary = rblForceBreak.SelectedValue.CompareCurrentCultureIgnoreCase("2") == 0;
                m_options.Layout = (PrintLayoutType)Enum.Parse(typeof(PrintLayoutType), cmbLayout.SelectedValue);
                m_options.PropertySeparator = (PrintingOptions.PropertySeparatorType)Enum.Parse(typeof(PrintingOptions.PropertySeparatorType), rblPropertySeparator.SelectedValue);

                m_options.ExcludedPropertyIDs.Clear();
                foreach (ListItem li in cklProperties.Items)
                    if (li.Selected)
                        m_options.ExcludedPropertyIDs.Add(Convert.ToInt32(li.Value, CultureInfo.InvariantCulture));

                Collection<OptionalColumn> lst = new Collection<OptionalColumn>();
                foreach (DropDownList ddl in OptionalColumnDropDowns)
                    AddOptionalColumnForValue(ddl.SelectedValue, lst);
                m_options.OptionalColumns.Clear();
                foreach (OptionalColumn oc in lst)
                    m_options.OptionalColumns.Add(oc);

                m_options.IncludePullForwardTotals = ckPullForwardTotals.Checked;
                m_options.StripeSubtotalsByCategoryClass = ckSeparateCategoryClass.Checked;

                m_options.DisplayMode = (PrintingOptions.ModelDisplayMode)rblModelDisplay.SelectedIndex;

                return m_options;
            }
            set
            {
                m_options = value ?? throw new ArgumentNullException(nameof(value));
                cmbFlightsPerPage.SelectedValue = m_options.FlightsPerPage.ToString(CultureInfo.InvariantCulture);
                ckIncludeImages.Checked = m_options.IncludeImages;
                ckIncludeSignatures.Checked = m_options.IncludeSignatures;
                ckFlightColoring.Checked = m_options.UseFlightColoring;
                if (m_options.BreakAtMonthBoundary)
                    rblForceBreak.SelectedValue = "1";
                else if (m_options.BreakAtYearBoundary)
                    rblForceBreak.SelectedValue = "2";
                else
                    rblForceBreak.SelectedValue = "0";
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
                    if (value.OptionalColumns != null && value.OptionalColumns.Count > i)
                    {
                        OptionalColumn oc = value.OptionalColumns[i];
                        OptionalColumnDropDowns[i].SelectedValue = oc.ColumnType == OptionalColumnType.CustomProp ? oc.IDPropType.ToString(CultureInfo.InvariantCulture) : oc.ColumnType.ToString();
                    }
                }

                ckPullForwardTotals.Checked = value.IncludePullForwardTotals;
                rblForceBreak.Visible = ckPullForwardTotals.Visible = m_options.FlightsPerPage > 0;
                ckSeparateCategoryClass.Checked = m_options.StripeSubtotalsByCategoryClass;

                rblModelDisplay.SelectedIndex = (int)value.DisplayMode;

                AdjustForLayoutCapabilities(m_options);
            }
        }

        protected void AdjustForLayoutCapabilities(PrintingOptions po)
        {
            if (po == null)
                throw new ArgumentNullException(nameof(po));
            PrintLayout pl = PrintLayout.LayoutForType(po.Layout);
            pnlIncludeImages.Visible = pl.SupportsImages;
            if (!pl.SupportsImages)
                ckIncludeImages.Checked = m_options.IncludeImages = false;
            pnlOptionalColumns.Visible = pl.SupportsOptionalColumns;
        }

        private static void AddOptionalColumnForValue(string value, Collection<OptionalColumn> lst)
        {
            if (String.IsNullOrEmpty(value))
                return;

            if (lst == null)
                throw new ArgumentNullException(nameof(lst));

            // Enum.TryParse works with integers as well as symbolic names, so try it first as an integer (i.e., custom property type)
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idPropType))
            {
                lst.Add(new OptionalColumn(idPropType));
                return;
            }

            if (Enum.TryParse<OptionalColumnType>(value, out OptionalColumnType oct))
            {
                lst.Add(new OptionalColumn(oct));
                return;
            }

            throw new MyFlightbookException("Attempt to create unknown optional column: " + value);
        }

        protected void SetPropsForUser()
        {
            MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(UserName);
            List<CustomPropertyType> rgcptUser = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(pf.UserName));
            rgcptUser.RemoveAll(cpt => !cpt.IsFavorite && !pf.BlocklistedProperties.Contains(cpt.PropTypeID));
            List<CustomPropertyType> rgcptUserOptionalColumns = rgcptUser.FindAll(cpt => (cpt.Type == CFPPropertyType.cfpDecimal || cpt.Type == CFPPropertyType.cfpInteger) && !cpt.IsNoSum);
            rgcptUser.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
            cklProperties.DataSource = rgcptUser;
            cklProperties.DataBind();
            ckCheckAll.Visible = rgcptUser.Count > 4;
            expPropertiesToExclude.Visible = rgcptUser.Count > 0;

            // By default, exclude "Additional flight remarks"
            foreach (ListItem li in cklProperties.Items)
                if (Convert.ToInt32(li.Value, CultureInfo.InvariantCulture) == (int)CustomPropertyType.KnownProperties.IDPropAdditionalFlightRemarks)
                    li.Selected = true;

            List<ListItem> lstOptionalColumnDropdowns = new List<ListItem>()
            {
                new ListItem(Resources.LocalizedText.PrintViewOptionalColumnNone, string.Empty),
                new ListItem(Resources.Makes.IsComplex, OptionalColumnType.Complex.ToString()),
                new ListItem(Resources.Makes.IsRetract, OptionalColumnType.Retract.ToString()),
                new ListItem(Resources.Makes.IsTailwheel, OptionalColumnType.Tailwheel.ToString()),
                new ListItem(Resources.Makes.IsHighPerf, OptionalColumnType.HighPerf.ToString()),
                new ListItem(Resources.Makes.IsTAA, OptionalColumnType.TAA.ToString()),
                new ListItem(Resources.Makes.IsTurboprop, OptionalColumnType.TurboProp.ToString()),
                new ListItem(Resources.Makes.IsTurbine, OptionalColumnType.Turbine.ToString()),
                new ListItem(Resources.Makes.IsJet, OptionalColumnType.Jet.ToString()),
                new ListItem(Resources.LocalizedText.DropDownListSeparator, string.Empty),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.ATD), OptionalColumnType.ATD.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.FTD), OptionalColumnType.FTD.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.FFS), OptionalColumnType.FFS.ToString()),
                new ListItem(Resources.LocalizedText.DropDownListSeparator, string.Empty),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.ASEL), OptionalColumnType.ASEL.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.AMEL), OptionalColumnType.AMEL.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.ASES), OptionalColumnType.ASES.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.AMES), OptionalColumnType.AMES.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.Helicopter), OptionalColumnType.Helicopter.ToString()),
                new ListItem(OptionalColumn.TitleForType(OptionalColumnType.Glider), OptionalColumnType.Glider.ToString()),
            };
            if (rgcptUserOptionalColumns.Count > 0)
                lstOptionalColumnDropdowns.Add(new ListItem(Resources.LocalizedText.DropDownListSeparator, string.Empty));
            rgcptUserOptionalColumns.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
            foreach (CustomPropertyType cpt in rgcptUserOptionalColumns)
                lstOptionalColumnDropdowns.Add(new ListItem(cpt.Title, cpt.PropTypeID.ToString(CultureInfo.InvariantCulture)));

            foreach (DropDownList ddl in OptionalColumnDropDowns)
            {
                ddl.DataSource = lstOptionalColumnDropdowns;
                ddl.DataBind();
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                for (int i = 3; i <= 40; i++)
                    cmbFlightsPerPage.Items.Add(new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintViewXPerPage, i), i.ToString(CultureInfo.InvariantCulture)) { Selected = (i == 15) });

                SetPropsForUser();
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // need to disable the separators in the optional column dropdowns on each postback since ASP.NET doesn't preserve that state.
            foreach (DropDownList ddl in OptionalColumnDropDowns)
                foreach (ListItem li in ddl.Items)
                    if (String.IsNullOrEmpty(li.Value) && li.Text.CompareCurrentCulture(Resources.LocalizedText.DropDownListSeparator) == 0)
                        li.Attributes.Add("disabled", "disabled");
        }

        protected void NotifyDelegate()
        {
            pnlForceBreak.Visible = ckPullForwardTotals.Visible = cmbFlightsPerPage.SelectedIndex > 0;
            if (cmbFlightsPerPage.SelectedIndex == 0)
                rblForceBreak.SelectedValue = "0";

            OptionsChanged?.Invoke(this, new PrintingOptionsEventArgs(Options));
        }

        protected void ckIncludeImages_CheckedChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }

        protected void ckIncludeSignatures_CheckedChanged(object sender, EventArgs e)
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

        protected void ckPullForwardTotals_CheckedChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }

        protected void ckSeparateCategoryClass_CheckedChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }

        protected void rblModelDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }

        protected void ckCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (ListItem ck in cklProperties.Items)
                ck.Selected = ckCheckAll.Checked;
            NotifyDelegate();
        }

        protected void ckFlightColoring_CheckedChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }

        protected void rblForceBreak_SelectedIndexChanged(object sender, EventArgs e)
        {
            NotifyDelegate();
        }
    }
}