using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Text.RegularExpressions;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class SearchTotals : System.Web.UI.Page
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
        this.Master.SelectedTab = tabID.tabUnknown;
        Title = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);

        if (!IsPostBack)
        {
            UpdateTotalsAndLogbook();
        }
    }


    protected void UpdateTotalsAndLogbook()
    {
        MfbLogbook1.User = User.Identity.Name;
        MfbLogbook1.Restriction = mfbSearchAndTotals1.Restriction;
        MfbLogbook1.RefreshData();
    }

    protected void UpdateGridVisibility(Boolean fShowGrid)
    {
        MfbLogbook1.Visible = lnkHideFlights.Visible = fShowGrid;
        lnkShowFlights.Visible = !fShowGrid;
    }

    protected void lnkShowFlights_Click(object sender, EventArgs e)
    {
        UpdateGridVisibility(true);
    }

    protected void lnkHideFlights_Click(object sender, EventArgs e)
    {
        UpdateGridVisibility(false);
    }

    protected void UpdateQueryResults(object sender, EventArgs e)
    {
        UpdateTotalsAndLogbook();
    }
}
