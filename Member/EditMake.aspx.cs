using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Net.Mail;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class EditMake : System.Web.UI.Page
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

    protected bool FUserHasFlownModel()
    {
        Aircraft[] rgac = (new UserAircraft(User.Identity.Name)).GetAircraftForUser();

        if (rgac == null)
            return false;

        int idModel = mfbEditMake1.MakeID;

        foreach (Aircraft ac in rgac)
            if (ac.ModelID == idModel)
                return true;

        return false;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.actMakes;
        if (!IsPostBack)
        {
            mfbEditMake1.MakeID = util.GetIntParam(Request, "id", -1);

            if (mfbEditMake1.MakeID > 0)
            {
                lblEditModel.Text = String.Format(CultureInfo.CurrentCulture, Resources.Makes.editMakeHeader, mfbEditMake1.Model.DisplayName);

                if (FUserHasFlownModel())
                {
                    pnlStats.Visible = true;
                    lblMake.Text = String.Format(CultureInfo.CurrentCulture, Resources.Makes.makeStatsHeader, mfbEditMake1.Model.DisplayName);
                    FlightQuery fq = new FlightQuery(Page.User.Identity.Name);
                    fq.MakeList = new MakeModel[] { mfbEditMake1.Model };
                    // TODO: create mfblogbook/mfbtotalssummary here, leaving them undefined otherwise?
                    mfbTotalSummary1.CustomRestriction = fq;
                    mfbLogbook1.Restriction = fq;
                }

                if (Page.User.Identity.IsAuthenticated && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData)
                {
                    lnkViewAircraft.Visible = true;
                    lnkViewAircraft.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/Aircraft.aspx?a=1&m={0}", mfbEditMake1.MakeID);
                }
            }
            else
                lblEditModel.Text = Resources.Makes.newMakeHeader;
        }
    }

    protected void MakeUpdated(object sender, EventArgs e)
    {
        Response.Redirect("makes.aspx");
    }
}
