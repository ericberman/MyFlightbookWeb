using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Clubs : System.Web.UI.Page
{
    public enum AuthState { Unauthenticated, Unauthorized, Authorized }

    [System.Web.Services.WebMethod(EnableSession=true)]
    public static string PopulateClub(int idClub) 
    {
        StringBuilder sb = new StringBuilder();

        try
        {
            // We have no Page, so things like Page_Load don't get called.
            // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
            using (Page p = new FormlessPage())
            {
                p.Controls.Add(new HtmlForm());
                using (StringWriter sw1 = new StringWriter(CultureInfo.InvariantCulture))
                    HttpContext.Current.Server.Execute(p, sw1, false);

                HttpRequest r = HttpContext.Current.Request;
                if (!r.IsLocal && !r.UrlReferrer.Host.EndsWith(Branding.CurrentBrand.HostName, StringComparison.OrdinalIgnoreCase))
                    throw new MyFlightbookException("Unauthorized attempt to populate club! {0}, {1}");

                Club c = Club.ClubWithID(idClub);
                if (c == null)
                    return string.Empty;

                Controls_ClubControls_ViewClub vc = (Controls_ClubControls_ViewClub)p.LoadControl("~/Controls/ClubControls/ViewClub.ascx");
                vc.LinkToDetails = true;
                vc.ActiveClub = c;
                p.Form.Controls.Add(vc);

                // Now, write it out.
                StringWriter sw = null;
                try
                {
                    sw = new StringWriter(sb, CultureInfo.InvariantCulture);
                    using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                    {
                        sw = null;
                        vc.RenderControl(htmlTW);
                    }
                }
                finally
                {
                    if (sw != null)
                        sw.Dispose();
                }
            }
        }
        catch (MyFlightbookException ex)
        {
            sb.Append(ex.Message);
        }

        return sb.ToString();
    }

    protected const string szVSKeyClubs = "vsKeyClubsToView";
    private List<Club> Clubs
    {
        get
        {
            if (ViewState[szVSKeyClubs] == null)
                ViewState[szVSKeyClubs] = new List<Club>();
            return (List<Club>)ViewState[szVSKeyClubs];
        }
    }

    protected const string szVSUserSate = "vsUserState";
    protected AuthState UserState 
    {
        get { return (AuthState) ViewState[szVSUserSate]; }
        set { ViewState[szVSUserSate] = value; }
    }

    protected string FixUpDonationAmount()
    {
        return String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Club.CreateClubRestriction), Gratuity.GratuityFromType(Gratuity.GratuityTypes.CreateClub).Threshold);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.actMyClubs;

        if (!IsPostBack)
        {
            expandoCreateClub.ExpandoLabel.Font.Bold = true;
            UserState = Page.User.Identity.IsAuthenticated ? (EarnedGrauity.UserQualifies(Page.User.Identity.Name, Gratuity.GratuityTypes.CreateClub) ? AuthState.Authorized : AuthState.Unauthorized) : AuthState.Unauthenticated;

            vcNew.ActiveClub = new Club();

            switch (UserState) 
            {
                case AuthState.Unauthenticated:
                    pnlCreateClub.Visible = pnlYourClubs.Visible = false;
                    lblTrialStatus.Text = Branding.ReBrand(Resources.Club.MustBeMember);
                    break;
                case AuthState.Unauthorized:
                    lblTrialStatus.Text = Branding.ReBrand(Resources.Club.ClubCreateTrial);
                    vcNew.ActiveClub.Status = Club.ClubStatus.Promotional;
                    break;
                case AuthState.Authorized:
                    lblTrialStatus.Text = Branding.ReBrand(Resources.Club.ClubCreateNoTrial);
                    vcNew.ActiveClub.Status = Club.ClubStatus.OK;
                    break;
            }

            Refresh();
        }
    }

    protected void Refresh()
    {
        Clubs.Clear();
        Clubs.AddRange(UserState == AuthState.Unauthenticated ? new List<Club>() : Club.AllClubsForUser(Page.User.Identity.Name));

        // if only one club, go directly to it
        if (Clubs.Count == 1 && util.GetIntParam(Request, "noredir", 0) == 0)
            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", Clubs[0].ID));
        else
        {
            gvClubs.DataSource = Clubs;
            gvClubs.DataBind();
        }
    }

    protected void gvClubs_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_ClubControls_ViewClub vc = (Controls_ClubControls_ViewClub)e.Row.FindControl("viewClub1");
            vc.ActiveClub = (Club)e.Row.DataItem;
        }
    }

    protected void vcNew_ClubChanged(object sender, ClubChangedEventsArgs e)
    {
        if (e == null || e.EventClub == null)
            throw new ArgumentNullException("e");
        Club.ClearCachedClub(e.EventClub.ID);   // newly created - cache actually doesn't have things like the airport code, so force a reload when we redirect.
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", e.EventClub.ID));
    }

    #region FindClubs
    private const string szKeyVSSearchResults = "clubSearchResults";
    private List<Club> SearchResults 
    {
        get
        {
            List<Club> lst = (List<Club>)ViewState[szKeyVSSearchResults];
            if (lst == null)
                ViewState[szKeyVSSearchResults] = lst = new List<Club>();
            return lst;
        }
    }

    protected void DisplaySearchResults()
    {
        mfbGoogleMapManager2.Map.Clubs = SearchResults;
        mfbGoogleMapManager2.Map.ClubClickHandler = "displayClubDetails";
        pnlSearchResults.Visible = true;
    }

    protected void btnFindClubs_Click(object sender, EventArgs e)
    {
        txtHomeAirport.Text = txtHomeAirport.Text.ToUpper(CultureInfo.CurrentCulture).Trim();

        SearchResults.Clear();

        bool fAdmin = Page.User.Identity.IsAuthenticated && util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData;

        if (String.IsNullOrEmpty(txtHomeAirport.Text))
            SearchResults.AddRange(Club.AllClubs(fAdmin));
        else
        {
            AirportList al = new AirportList(txtHomeAirport.Text);
            List<airport> lst = new List<airport>(al.GetAirportList());
            lst.RemoveAll(ap => !ap.IsPort);

            if (lst.Count == 0)
            {
                lblErr.Text = Resources.Club.errHomeAirportNotFound;
                return;
            }

            mfbGoogleMapManager2.Map.SetAirportList(al);
            SearchResults.AddRange(Club.ClubsNearAirport(hdnMatchingHomeAirport.Value = lst[0].Code, fAdmin));
        }

        DisplaySearchResults();
    }
    #endregion
}