using MyFlightbook;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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
                    lnkViewTotals.Visible = true;
                    FlightQuery fq = new FlightQuery(Page.User.Identity.Name) { MakeList = new MakeModel[] { mfbEditMake1.Model } };
                    lnkViewTotals.Text = String.Format(CultureInfo.CurrentCulture, Resources.Makes.makeStatsHeader, mfbEditMake1.Model.DisplayName);
                    lnkViewTotals.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(Convert.ToBase64String(fq.ToJSONString().Compress())));
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
