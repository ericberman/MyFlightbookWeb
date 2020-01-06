using MyFlightbook;
using MyFlightbook.Printing;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_PrintingLayouts_LayoutSACAA : System.Web.UI.UserControl, IPrintingTemplate
{
    protected MyFlightbook.Profile CurrentUser { get; set; }

    protected bool ShowFooter { get; set; }

    protected string PropSeparator { get; set; }

    protected PrintingOptions Options { get; set; }

    #region IPrintingTemplate
    public void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        ShowFooter = showFooter;
        CurrentUser = user;
        Options = options;
        PropSeparator = options.PropertySeparatorText;
        rptPages.DataSource = lst;
        rptPages.DataBind();
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e) { CurrentUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name); }

    protected void rptPages_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookPrintedPage lep = (LogbookPrintedPage)e.Item.DataItem;

        HashSet<int> hsRedundantProps = new HashSet<int>() { (int)CustomPropertyType.KnownProperties.IDPropNameOfPIC };
        hsRedundantProps.UnionWith(Options.ExcludedPropertyIDs);
        foreach (LogbookEntryDisplay led in lep.Flights)
        {
            List<CustomFlightProperty> lstProps = new List<CustomFlightProperty>(led.CustomProperties);
            lstProps.RemoveAll(cfp => hsRedundantProps.Contains(cfp.PropTypeID));
            led.CustPropertyDisplay = CustomFlightProperty.PropListDisplay(lstProps, CurrentUser.UsesHHMM, PropSeparator);
        }

        Repeater rpt = (Repeater)e.Item.FindControl("rptFlight");
        rpt.DataSource = lep.Flights;
        rpt.DataBind();

        rpt = (Repeater)e.Item.FindControl("rptSubtotalCollections");
        rpt.DataSource = lep.Subtotals;
        rpt.DataBind();
    }

    protected void rptSubtotalCollections_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookPrintedPageSubtotalsCollection sc = (LogbookPrintedPageSubtotalsCollection)e.Item.DataItem;
        Repeater rpt = (Repeater)e.Item.FindControl("rptSubtotals");
        rpt.DataSource = sc.Subtotals;
        rpt.DataBind();
    }

    protected void rptFlight_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Controls_mfbSignature sig = (Controls_mfbSignature)e.Item.FindControl("mfbSignature");
        sig.Flight = (LogbookEntryDisplay)e.Item.DataItem;
    }
}