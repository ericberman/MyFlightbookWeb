using MyFlightbook.Web.Sharing;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Public
{
    public partial class ViewSharedLogbook : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string guid = util.GetStringParam(Request, "g");
                ShareKey sk = ShareKey.ShareKeyWithID(guid);
                if (sk == null)
                    Response.Redirect("/SecurityError.aspx");

                Profile pf = Profile.GetUser(sk.Username);

                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, pf.UserFullName);

                // Indicate success.
                sk.FUpdateAccess();

                if (sk.CanViewCurrency)
                {
                    mfbCurrency.Visible = true;
                    mfbCurrency.UserName = sk.Username;
                    mfbCurrency.RefreshCurrencyTable();
                }
                if (sk.CanViewTotals)
                {
                    mfbTotalSummary.Visible = true;
                    mfbTotalSummary.Username = sk.Username;
                    mfbTotalSummary.CustomRestriction = new FlightQuery(sk.Username);   // will call bind
                }
                if (sk.CanViewFlights)
                {
                    mfbLogbook.Visible = true;
                    mfbLogbook.User = sk.Username;
                    mfbLogbook.RefreshData();
                }
            }
        }
    }
}