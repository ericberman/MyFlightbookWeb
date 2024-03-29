﻿using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing.Layouts
{
    public partial class LayoutCondensed : PrintLayoutBase, ICondenseFlights
    {
        #region IPrintingTemplate
        public override void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
        {
            base.BindPages(lst, user, options, showFooter);

            rptPages.DataSource = lst;
            rptPages.DataBind();
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e) { }

        protected void rptPages_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookPrintedPage lep = (LogbookPrintedPage)e.Item.DataItem;
            StripRedundantOrExcludedProperties(null, lep.Flights);

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
}