using MyFlightbook;
using MyFlightbook.Printing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_PrintingLayouts_layoutCanada : System.Web.UI.UserControl, IPrintingTemplate
{
    public MyFlightbook.Profile CurrentUser { get; set; }

    public bool IncludeImages { get; set; }

    protected bool ShowFooter { get; set; }

    protected OptionalColumn[] OptionalColumns { get; set; }

    protected PrintingOptions Options { get; set; }

    protected Boolean ShowOptionalColumn(int index)
    {
        return OptionalColumns != null && index >= 0 && index < OptionalColumns.Length;
    }

    protected string OptionalColumnName(int index)
    {
        return ShowOptionalColumn(index) ? OptionalColumns[index].Title : string.Empty;
    }

    protected string PropSeparator { get; set; }

    #region IPrintingTemplate
    public void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        ShowFooter = showFooter;
        IncludeImages = options.IncludeImages;
        CurrentUser = user;
        Options = options;
        OptionalColumns = options.OptionalColumns;
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

        HashSet<int> hsRedundantProps = new HashSet<int>() { (int)CustomPropertyType.KnownProperties.IDPropStudentName, (int)CustomPropertyType.KnownProperties.IDPropNameOfPIC, (int)CustomPropertyType.KnownProperties.IDPropNightTakeoff };
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

    protected void rptFlight_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookEntryDisplay led = (LogbookEntryDisplay)e.Item.DataItem;
        Controls_mfbImageList mfbil = (Controls_mfbImageList)e.Item.FindControl("mfbilFlights");
        mfbil.Key = led.FlightID.ToString(CultureInfo.InvariantCulture);
        mfbil.Refresh(true, false);
        Controls_mfbSignature sig = (Controls_mfbSignature)e.Item.FindControl("mfbSignature");
        sig.Flight = led;
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
}