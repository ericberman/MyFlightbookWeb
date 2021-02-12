using System;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class MiniLogbook : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                this.Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, System.Web.HttpUtility.HtmlEncode(Profile.GetUser(User.Identity.Name).PreferredGreeting));

                mfbEditFlight1.SetUpNewOrEdit(-1);

                this.Master.SetMobile(true);
            }
        }

        protected void FlightUpdated(object sender, EventArgs e)
        {
            pnlSuccess.Visible = true;
            mfbEditFlight1.SetUpNewOrEdit(-1); // and clear the form for the next one.
        }
    }
}