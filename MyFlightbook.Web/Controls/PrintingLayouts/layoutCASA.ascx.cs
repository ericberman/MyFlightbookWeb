using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing.Layouts
{
    public partial class LayoutCASA : PrintLayoutBase
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

        protected string CrewNames(LogbookEntryDisplay led)
        {
            if (led == null)
                throw new ArgumentNullException(nameof(led));

            List<string> lst = new List<string>(new string[] {
                led.SICName,
                led.StudentName,
                led.CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropCrew1),
                led.CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropCrew2),
                led.CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropCrew3),
                led.CustomProperties.StringValueForProperty(CustomPropertyType.KnownProperties.IDPropAdditionalCrew)
            });
            lst.RemoveAll(s => String.IsNullOrWhiteSpace(s));

            return String.Join(", ", lst);
        }

        protected void rptPages_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookPrintedPage lep = (LogbookPrintedPage)e.Item.DataItem;
            StripRedundantOrExcludedProperties(new int[] { (int)CustomPropertyType.KnownProperties.IDPropStudentName, 
                (int)CustomPropertyType.KnownProperties.IDPropNameOfPIC, 
                (int) CustomPropertyType.KnownProperties.IDPropCrew1,
                (int) CustomPropertyType.KnownProperties.IDPropCrew2,
                (int) CustomPropertyType.KnownProperties.IDPropCrew3,
                (int) CustomPropertyType.KnownProperties.IDPropAdditionalCrew,
                (int)CustomPropertyType.KnownProperties.IDPropNightTakeoff }, lep.Flights);

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

        protected void rptFlight_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookEntryDisplay led = (LogbookEntryDisplay)e.Item.DataItem;
            Controls_mfbImageList mfbil = (Controls_mfbImageList)e.Item.FindControl("mfbilFlights");
            mfbil.Key = led.FlightID.ToString(CultureInfo.InvariantCulture);
            mfbil.Refresh(true, false);

            Controls_mfbSignature sig = (Controls_mfbSignature)e.Item.FindControl("mfbSignature");
            sig.Flight = led;
        }
    }
}