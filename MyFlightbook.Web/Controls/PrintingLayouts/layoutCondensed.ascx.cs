﻿using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing.Layouts
{
    public partial class LayoutCondensed : PrintLayoutBase
    {
        #region IPrintingTemplate
        public override void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
        {
            base.BindPages(lst, user, options, showFooter);

            rptPages.DataSource = lst;
            rptPages.DataBind();
        }
        #endregion

        public override bool IsCondensed => true; 

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
    }
}