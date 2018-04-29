using System;
using System.Globalization;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using MyFlightbook;
using MyFlightbook.Printing;

/******************************************************
 * 
 * Copyright (c) 2016-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_PrintOptions : System.Web.UI.UserControl
{
    public event EventHandler<PrintingOptionsEventArgs> OptionsChanged = null;

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

            ShowPictureOptions(m_options);
        }
    }

    protected void ShowPictureOptions(PrintingOptions po)
    {
        if (po == null)
            throw new ArgumentNullException("po");
        PrintLayout pl = PrintLayout.LayoutForType(po.Layout);
        pnlIncludeImages.Visible = pl.SupportsImages;
        if (!pl.SupportsImages)
            ckIncludeImages.Checked = m_options.IncludeImages = false;
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
            rgcptUser.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
            cklProperties.DataSource = rgcptUser;
            cklProperties.DataBind();
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
        ShowPictureOptions(Options);
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
}