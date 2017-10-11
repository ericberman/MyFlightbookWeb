using MyFlightbook;
using MyFlightbook.FlightStats;
using MyFlightbook.Image;
using MyFlightbook.SocialMedia;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Home : System.Web.UI.Page
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
        Master.SelectedTab = tabID.tabHome;
        Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitleWithDescription, Branding.CurrentBrand.AppName);
        Master.Layout = MasterPage.LayoutMode.Accordion;

        string s = util.GetStringParam(Request, "m");
        if (s.Length > 0)
        {
            this.Master.SetMobile((string.Compare(s, "no", StringComparison.OrdinalIgnoreCase) != 0));
        }

        FlightStats fs = FlightStats.GetFlightStats();

        if (!IsPostBack)
        {
            litAppDesc.Text = Branding.ReBrand(Resources.Profile.AppDescription);
            locRecentFlightsHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageRecentFlightsHeader, Branding.CurrentBrand.AppName);

            mvWelcome.SetActiveView(User.Identity.IsAuthenticated ? vwWelcomeBack : vwWelcomeNewUser);

            // turn off maps for iPad since it seems to crash
            if (String.IsNullOrEmpty(Request.UserAgent) || (Request.UserAgent.Contains("iPad") && Request.UserAgent.Contains("Safari")))
                lblSomeShown.Visible = mfbGoogleMapManagerRecentFlights.Visible = false;

            if (User.Identity.IsAuthenticated)
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DefaultPageWelcomeBack, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFirstName);
                MfbCurrency1.UserName = User.Identity.Name;
            }
            else
            {
                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.WelcomeTitle, Branding.CurrentBrand.AppName);
                HtmlForm mainform = (HtmlForm)Master.FindControl("form1");
                if (mainform != null && mfbSignIn1.DefButtonUniqueID.Length > 0)
                    mainform.DefaultButton = mfbSignIn1.DefButtonUniqueID;
            }

            mfbEditFlight1.SetUpNewOrEdit(-1);

            lblRecentFlightsStats.Text = fs.ToString();
        }

        // redirect to a mobile view if this is from a mobile device UNLESS cookies suggest to do otherwise.
        if (this.Master.IsMobileSession())
        {
            if ((Request.Cookies[MFBConstants.keyClassic] != null && String.Compare(Request.Cookies[MFBConstants.keyClassic].Value, "yes", StringComparison.OrdinalIgnoreCase) == 0))
                lnkViewMobile.Visible = true;
            else
                Response.Redirect("DefaultMini.aspx");
        }

        // need to do this on every callback so that pictures are displayed.
        mfbGoogleMapManagerRecentFlights.Map.Airports = fs.RecentRoutes;

        gvRecentFlights.DataSource = fs.RecentPublicFlights;
        gvRecentFlights.DataBind();
    }

    public void gvRecentFlights_AddPictures(Object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            string szTail = DataBinder.Eval(e.Row.DataItem, "TailNumDisplay").ToString();
            string szTailID = DataBinder.Eval(e.Row.DataItem, "AircraftID").ToString();
            string szID = DataBinder.Eval(e.Row.DataItem, "FlightID").ToString();

            Controls_mfbImageList mfbIl = (Controls_mfbImageList)LoadControl("~/Controls/mfbImageList.ascx");

            PlaceHolder plcAircraft = (PlaceHolder)e.Row.FindControl("plcAircraft");
            PlaceHolder plcFlightPix = (PlaceHolder)e.Row.FindControl("plcFlightPix");

            mfbIl.Key = szTailID;
            mfbIl.ImageClass = MFBImageInfo.ImageClass.Aircraft;
            mfbIl.AltText = szTail;
            mfbIl.CanEdit = false;
            mfbIl.Columns = 1;
            mfbIl.MaxImage = 1;
            mfbIl.Refresh();

            plcAircraft.Controls.Add(mfbIl);

            Controls_mfbImageList mfbIlFlight = (Controls_mfbImageList) LoadControl("~/Controls/mfbImageList.ascx");
            mfbIlFlight.ImageClass = MFBImageInfo.ImageClass.Flight;
            mfbIlFlight.Key = szID;
            mfbIlFlight.AltText = "";
            mfbIlFlight.Columns = 4;
            mfbIlFlight.MaxImage = -1;
            mfbIlFlight.CanEdit = false;
            plcFlightPix.Controls.Add(mfbIlFlight);
            mfbIlFlight.Refresh();

            Controls_fbComment fbComment = (Controls_fbComment)e.Row.FindControl("fbComment1");
            fbComment.URI = Branding.PublicFlightURL(Convert.ToInt32(szID, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    protected void lnkViewMobile_Click(object sender, EventArgs e)
    {
        Response.Cookies[MFBConstants.keyClassic].Value = null;
        Response.Redirect("DefaultMini.aspx");
    }

    protected void EnteredFlight(object sender, EventArgs e)
    {
        Response.Redirect(SocialNetworkAuthorization.PopRedirect(SocialNetworkAuthorization.DefaultRedirPage));
    }

    protected void OnPageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (sender == null)
            throw new ArgumentNullException("sender");
        GridView gv = (GridView)sender;
        gv.PageIndex = e.NewPageIndex;
        gv.DataBind();
    }
}
