using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing.Layouts
{
    public partial class layoutAirline : PrintLayoutBase, IPrintingTemplate
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
            StripRedundantOrExcludedProperties(new int[] { 
                (int)CustomPropertyType.KnownProperties.IDPropNameOfPIC, 
                (int) CustomPropertyType.KnownProperties.IDPropCaptainName,
                (int) CustomPropertyType.KnownProperties.IDPropFlightNumber, 
                (int) CustomPropertyType.KnownProperties.IDPropMultiPilotTime, 
                (int) CustomPropertyType.KnownProperties.IDPropSolo, 
                (int) CustomPropertyType.KnownProperties.IDPropPICUS,
                (int) CustomPropertyType.KnownProperties.IDBlockOut,
                (int) CustomPropertyType.KnownProperties.IDBlockIn,
                (int) CustomPropertyType.KnownProperties.IDPropTakeoffAny,
                (int) CustomPropertyType.KnownProperties.IDPropNightTakeoff,
                (int) CustomPropertyType.KnownProperties.IDPropFlightEngineerTime,
                (int) CustomPropertyType.KnownProperties.IDPropReliefPilotTime }, 
                lep.Flights);

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
            bool fHHMM = CurrentUser.UsesHHMM;
            foreach (LogbookEntryDisplay led in sc.Subtotals)
                led.UseHHMM = fHHMM;
            Repeater rpt = (Repeater)e.Item.FindControl("rptSubtotals");
            rpt.DataSource = sc.Subtotals;
            rpt.DataBind();
        }

        protected void rptFlight_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            Controls_mfbSignature sig = (Controls_mfbSignature)e.Item.FindControl("mfbSignature");
            sig.Flight = (LogbookEntryDisplay)e.Item.DataItem;
        }
    }
}