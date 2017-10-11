using System;
using System.Web.UI.WebControls;
using AjaxControlToolkit;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2012-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_FAQ : System.Web.UI.Page
{
    protected int SelectedFAQItem { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;
        Master.Layout = MasterPage.LayoutMode.Accordion;

        if (!IsPostBack)
        {
            SelectedFAQItem = util.GetIntParam(Request, "q", 0);
            rptFAQGroup.DataSource = FAQGroup.CategorizedFAQs;
            rptFAQGroup.DataBind();
        }
    }

    protected void rptFAQGroup_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        Accordion acc = (Accordion)e.Item.FindControl("accFAQGroup");
        FAQGroup fg = (FAQGroup) e.Item.DataItem;
        acc.DataSource = fg.Items;
        acc.DataBind();

        // Auto-expand if it contains the selected FAQ item
        int i = 0;
        foreach (FAQItem fi in fg.Items)
        {
            if (fi.idFAQ == SelectedFAQItem)
                acc.SelectedIndex = i;
            i++;
        }
    }
}