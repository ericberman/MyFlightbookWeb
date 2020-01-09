using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_MiniLogbook : System.Web.UI.Page
{
    protected override void OnError(EventArgs e)
    {
        Exception ex = Server.GetLastError();

        if (ex.GetType() == typeof(HttpRequestValidationException))
        {
            Context.ClearError();
            Response.Redirect("~/SecurityError.aspx");
            Response.End();
        }
        else
            base.OnError(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

        if (!IsPostBack)
        {
            this.Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFirstName);

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
