using MyFlightbook;
using MyFlightbook.Printing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_PrintingLayouts_layoutCondensed : System.Web.UI.UserControl, IPrintingTemplate, ICondenseFlights
{
    public MyFlightbook.Profile CurrentUser { get; set; }

    protected bool ShowFooter { get; set; }

    public bool IncludeImages { get; set; }

    protected Collection<OptionalColumn> OptionalColumns { get; private set; }

    protected Boolean ShowOptionalColumn(int index)
    {
        return OptionalColumns != null && index >= 0 && index < OptionalColumns.Count;
    }

    protected string OptionalColumnName(int index)
    {
        return ShowOptionalColumn(index) ? OptionalColumns[index].Title : string.Empty;
    }

    protected string OtherCatClassValue(LogbookEntryDisplay led)
    {
        return (led != null && led.EffectiveCatClass != (int)CategoryClass.CatClassID.ASEL && led.EffectiveCatClass != (int)CategoryClass.CatClassID.AMEL && OptionalColumn.ShowOtherCatClass(OptionalColumns, (CategoryClass.CatClassID)led.EffectiveCatClass)) ?
            String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}: {1}", led.CategoryClassNoType, led.TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM)) :
            string.Empty;
    }

    #region IPrintingTemplate
    public void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        ShowFooter = showFooter;
        CurrentUser = user;
        OptionalColumns = options.OptionalColumns;
        IncludeImages = options.IncludeImages;

        rptPages.DataSource = lst;
        rptPages.DataBind();
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e) { CurrentUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name); }

    protected void rptPages_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        LogbookPrintedPage lep = (LogbookPrintedPage)e.Item.DataItem;

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
            throw new ArgumentNullException(nameof(e));

        LogbookPrintedPageSubtotalsCollection sc = (LogbookPrintedPageSubtotalsCollection)e.Item.DataItem;
        Repeater rpt = (Repeater)e.Item.FindControl("rptSubtotals");
        rpt.DataSource = sc.Subtotals;
        rpt.DataBind();
    }

    public IList<LogbookEntryDisplay> CondenseFlights(IEnumerable<LogbookEntryDisplay> lstIn)
    {
        List<LogbookEntryDisplay> lstOut = new List<LogbookEntryDisplay>();

        if (lstIn == null)
            throw new ArgumentNullException(nameof(lstIn));

        LogbookEntryDisplay ledCurrent = null;
        foreach (LogbookEntryDisplay ledSrc in lstIn)
        {
            if (ledCurrent == null)
            {
                ledCurrent = ledSrc;
                ledSrc.FlightCount = 1;
            }
            else if (ledSrc.Date.Date.CompareTo(ledCurrent.Date.Date) != 0 || ledSrc.CatClassDisplay.CompareCurrentCultureIgnoreCase(ledCurrent.CatClassDisplay) != 0)
            {
                lstOut.Add(ledCurrent);
                ledCurrent = ledSrc;
                ledSrc.FlightCount = 1;
            }
            else
            {
                ledCurrent.AddFrom(ledSrc);
                List<string> lst = new List<string>() { ledCurrent.Route, ledSrc.Route };
                lst.RemoveAll(sz => String.IsNullOrWhiteSpace(sz));
                ledCurrent.Route = String.Join(" / ", lst);
                lst = new List<string>() { ledCurrent.Comment, ledSrc.Comment };
                lst.RemoveAll(sz => String.IsNullOrWhiteSpace(sz));
                ledCurrent.Comment = String.Join(" / ", lst);
                ledSrc.FlightCount++;
            }
        }
        if (ledCurrent != null)
            lstOut.Add(ledCurrent);

        return lstOut;
    }
}