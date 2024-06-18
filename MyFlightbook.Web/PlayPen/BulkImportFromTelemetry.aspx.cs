using MyFlightbook;
using MyFlightbook.Telemetry;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class BulkImportFromTelemetry : MyFlightbook.Web.WizardPage.MFBWizardPage
{
    private string SessionKeyBase { get { return Page.User.Identity.Name + "bulkImport"; } }

    private string SessionKeyTZ { get { return SessionKeyBase + "TZ"; } }

    private string SessionKeyOpt { get { return SessionKeyBase + "Opt"; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.User.Identity.IsAuthenticated)
            throw new UnauthorizedAccessException("You must be signed in to view this page.");

        InitWizard(wzFlightsFromTelemetry);
        if (IsPostBack)
        {
            Session[SessionKeyTZ] = TimeZone.SelectedTimeZone;
            Session[SessionKeyOpt] = AutofillOptionsChooser.Options;
        }
        else
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            if (pf.PreferredTimeZone != null && Session[SessionKeyTZ] == null)
                Session[SessionKeyTZ] = pf.PreferredTimeZone;

            if (Session[SessionKeyTZ] != null)
                TimeZone.SelectedTimeZone = (TimeZoneInfo)Session[SessionKeyTZ];

            AutoFillOptions afo = AutoFillOptions.DefaultOptionsForUser(Page.User.Identity.Name);
            afo.SaveForUser(Page.User.Identity.Name);
            Session[SessionKeyOpt] = afo;
        }
    }

    protected void afuUpload_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            return;

        PendingFlight pf = new PendingFlight() { User = Page.User.Identity.Name };
        string szOriginal = pf.FlightData = System.Text.Encoding.UTF8.GetString(e.GetContents());
        using (FlightData fd = new FlightData())
        {
            fd.AutoFill(pf, (AutoFillOptions) Session[SessionKeyOpt]);

            pf.FlightData = string.Empty;
            LogbookEntry leEmpty = new PendingFlight() { User = Page.User.Identity.Name, FlightData = string.Empty };
            if (!pf.IsEqualTo(leEmpty))
            {
                pf.TailNumDisplay = fd.TailNumber ?? string.Empty;
                pf.FlightData = szOriginal;
                pf.Commit();
            }
        }
    }

    protected void wzFlightsFromTelemetry_FinishButtonClick(object sender, WizardNavigationEventArgs e)
    {
        Response.Redirect("~/mvc/flightedit/pending");
    }
}