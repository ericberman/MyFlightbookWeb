using AjaxControlToolkit;
using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_FAQ : System.Web.UI.Page
{
    protected int SelectedFAQItem { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;

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

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        IEnumerable<FAQGroup> results = null;
        if (String.IsNullOrWhiteSpace(mfbSearchbox.SearchText))
            results = FAQGroup.CategorizedFAQs;
        else
        {
            results = FAQGroup.CategorizedFAQItemsContainingWords(mfbSearchbox.SearchText);
            if (results.Count() == 0)
            {
                lblErr.Text = Resources.LocalizedText.FAQSearchNoResults;
                results = FAQGroup.CategorizedFAQs;
            }
        }

        rptFAQGroup.DataSource = results;
        rptFAQGroup.DataBind();
    }
}