using System;
using System.Globalization;
using System.Web.UI.WebControls;
using MyFlightbook.Printing;

/******************************************************
 * 
 * Copyright (c) 2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
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
            {
                cmbFlightsPerPage.Items.Add(new ListItem(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintViewXPerPage, i), i.ToString(CultureInfo.InvariantCulture)) { Selected = (i == 15) });
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
        ShowPictureOptions(Options);
        NotifyDelegate();
    }
}