using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class EditMake : System.Web.UI.Page
{
    protected IEnumerable<Aircraft> AircraftWithModel(int idModel)
    {
        Aircraft[] rgac = (new UserAircraft(User.Identity.Name)).GetAircraftForUser();

        if (rgac == null)
            return Array.Empty<Aircraft>();

        return Array.FindAll<Aircraft>(rgac, ac => ac.ModelID == idModel);
    }

    const string szVSStats = "szvsModelStats";

    protected MakeModelStats Stats
    {
        get
        {
            if (ViewState[szVSStats] == null)
                ViewState[szVSStats] = MakeModel.GetModel(mfbEditMake1.MakeID).StatsForUser(User.Identity.Name);
            return (MakeModelStats)ViewState[szVSStats];
        }
    }

    const string szVSSampleImages = "szvsSampleImages";
    protected IEnumerable<MFBImageInfo> SampleImages
    {
        get
        {
            if (ViewState[szVSSampleImages] == null)
                ViewState[szVSSampleImages] = mfbEditMake1.Model.SampleImages(16);
            return (IEnumerable<MFBImageInfo>) ViewState[szVSSampleImages];
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Master.SelectedTab = tabID.actMakes;

        int idModel = util.GetIntParam(Request, "id", -1);
        IEnumerable<Aircraft> rgac = AircraftWithModel(idModel);
        AircraftList1.AircraftSource = rgac;    // do this on every page load for images, etc.  It doesn't keep viewstate

        if (!IsPostBack)
        {
            mfbEditMake1.MakeID = idModel;

            if (mfbEditMake1.MakeID > 0)
            {
                // Default to read-only
                mvMake.SetActiveView(vwView);

                lblEditModel.Text = mfbEditMake1.Model.DisplayName;

                List<LinkedString> lstAttribs = new List<LinkedString>();
                MakeModel mm = mfbEditMake1.Model;
                lblMakeModel.Text = mm.DisplayName;
                if (mm != null)
                {
                    if (!String.IsNullOrEmpty(mm.FamilyName))
                        lstAttribs.Add(new LinkedString(ModelQuery.ICAOPrefix + mm.FamilyName));
                    foreach (string sz in mm.AttributeList())
                        lstAttribs.Add(new LinkedString(sz));
                }

                if (rgac.Any())
                {
                    lstAttribs.Add(new LinkedString(String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsAircraftCount, rgac.Count())));

                    lnkViewTotals.Visible = true;
                    FlightQuery fq = new FlightQuery(Page.User.Identity.Name);
                    fq.MakeList.Add(mfbEditMake1.Model);
                    MakeModelStats stats = Stats;
                    string szStatsLabel = String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsFlightsCount, stats.NumFlights, stats.EarliestFlight.HasValue && stats.LatestFlight.HasValue ?
                        String.Format(CultureInfo.CurrentCulture, Resources.Makes.MakeStatsFlightsDateRange, stats.EarliestFlight.Value, stats.LatestFlight.Value) : string.Empty);
                    lnkViewTotals.Text = szStatsLabel;
                    lnkViewTotals.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?ft=Totals&fq={0}", HttpUtility.UrlEncode(Convert.ToBase64String(fq.ToJSONString().Compress())));
                    lstAttribs.Add(new LinkedString(szStatsLabel, lnkViewTotals.NavigateUrl));
                }

                if (Page.User.Identity.IsAuthenticated && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData)
                {
                    lnkViewAircraft.Visible = true;
                    lnkViewAircraft.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/Aircraft.aspx?a=1&m={0}", mfbEditMake1.MakeID);
                    lstAttribs.Add(new LinkedString(lnkViewAircraft.Text, lnkViewAircraft.NavigateUrl));
                }

                rptAttributes.DataSource = lstAttribs;
                rptAttributes.DataBind();
            }
            else
            {
                mvMake.SetActiveView(vwEdit);
                mvInstances.Visible = false;
                lblEditModel.Text = Resources.Makes.newMakeHeader;
            }
        }

        mvInstances.SetActiveView(!rgac.Any() ? vwSampleImages : vwAircraft);

        mfbImageList.Images = new ImageList(SampleImages.ToArray());
        mfbImageList.Refresh(false);
    }

    protected void MakeUpdated(object sender, EventArgs e)
    {
        Response.Redirect("makes.aspx");
    }

    protected void imgEditAircraftModel_Click(object sender, ImageClickEventArgs e)
    {
        // Hack Need to reset the make, but this wasn't shown before, so it won't preserve it due to optimizations around the manufacturer list.
        mfbEditMake1.MakeID = mfbEditMake1.MakeID;
        lblEditModel.Text = String.Format(CultureInfo.CurrentCulture, Resources.Makes.editMakeHeader, mfbEditMake1.Model.DisplayName);
        mvMake.SetActiveView(vwEdit);
        mvInstances.Visible = false;
    }

    protected void AircraftList1_AircraftDeleted(object sender, System.Web.UI.WebControls.CommandEventArgs e)
    {
        AircraftList1.AircraftSource = AircraftWithModel(mfbEditMake1.MakeID);
    }

    protected void AircraftList1_FavoriteChanged(object sender, EventArgs e)
    {
        AircraftList1.AircraftSource = AircraftWithModel(mfbEditMake1.MakeID);
    }
}
