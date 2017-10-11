using System;
using System.Globalization;
using System.Web;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class _Default : System.Web.UI.Page
{

    private const string szParamIDFlight = "idFlight";
    private const string szParamSearch = "s";

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
        // wire up the logbook to the current user
        MfbLogbook1.User = User.Identity.Name;

        this.Master.SelectedTab = tabID.lbtAddNew;
        this.Master.SuppressTopNavPrint = true;
        string szTitle = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);

        ModalPopupExtender1.OnCancelScript = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').style.display = 'none';", pnlWelcomeNewUser.ClientID);

        if (Request.Cookies[MFBConstants.keyNewUser] != null && !String.IsNullOrEmpty(Request.Cookies[MFBConstants.keyNewUser].Value) || util.GetStringParam(Request, "sw").Length > 0 || Request.PathInfo.Contains("/sw"))
        {
            Response.Cookies[MFBConstants.keyNewUser].Expires = DateTime.Now.AddDays(-1);
            ModalPopupExtender1.Show();
        }

        if (!IsPostBack)
        {
            string szSearch = util.GetStringParam(Request, szParamSearch);
            if (szSearch.Length > 0)
            {
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/FindFlights.aspx?s={0}", HttpUtility.UrlEncode(szSearch)), true);
                return;
            }

            int idFlight = util.GetIntParam(Request, szParamIDFlight, LogbookEntry.idFlightNew);

            // Redirect to the non-querystring based page so that Ajax file upload works
            if (idFlight != LogbookEntry.idFlightNew)
            {
                string szNew = Request.Url.PathAndQuery.Replace(".aspx", String.Format(CultureInfo.InvariantCulture, ".aspx/{0}", idFlight)).Replace(String.Format(CultureInfo.InvariantCulture, "{0}={1}", szParamIDFlight, idFlight), string.Empty).Replace("?&", "?");
                Response.Redirect(szNew, true);
                return;
            }

            if (Request.PathInfo.Length > 0)
            {
                try { idFlight = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture); }
                catch (FormatException) { }
            }

            SetUpNewOrEdit(idFlight);

            lblUserName.Text = Master.Title = szTitle;
        }
    }

    protected void FlightUpdated(object sender, EventArgs e)
    {
        // if we had been editing a flight do a redirect so we have a clean URL
        // OR if there are pending redirects, do them.
        // Otherwise, just clean the page.
        if (Request[szParamIDFlight] != null || SocialNetworkAuthorization.RedirectList.Count > 0)
            Response.Redirect(SocialNetworkAuthorization.PopRedirect(Master.IsMobileSession() ? SocialNetworkAuthorization.DefaultRedirPageMini : "~/Member/LogbookNew.aspx"));
        else
        {
            Response.Redirect("~/Member/Logbook.aspx", true);
        }
    }

    protected void SetUpNewOrEdit(int idFlight)
    {
        mfbEF1.SetUpNewOrEdit(idFlight);

        MfbLogbook1.Visible = (idFlight == LogbookEntry.idFlightNew);  // hide the list of flights if editing an existing flight - avoids confusion.
    }
}
