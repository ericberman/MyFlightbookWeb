using MyFlightbook;
using MyFlightbook.Telemetry;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class BulkImportFromTelemetry : System.Web.UI.Page
{
    #region WizardStyling
    // Thanks to http://weblogs.asp.net/grantbarrington/archive/2009/08/11/styling-the-asp-net-wizard-control-to-have-the-steps-across-the-top.aspx for how to do this.
    protected void wzFlightsFromTelemetry_PreRender(object sender, EventArgs e)
    { 
        Repeater SideBarList = wzFlightsFromTelemetry.FindControl("HeaderContainer").FindControl("SideBarList") as Repeater;

        SideBarList.DataSource = wzFlightsFromTelemetry.WizardSteps;
        SideBarList.DataBind();
    }

    public string GetClassForWizardStep(object wizardStep)
    {
        if (!(wizardStep is WizardStep step))
            return string.Empty;

        int stepIndex = wzFlightsFromTelemetry.WizardSteps.IndexOf(step);

        if (stepIndex < wzFlightsFromTelemetry.ActiveStepIndex)
            return "wizStepCompleted";
        else if (stepIndex > wzFlightsFromTelemetry.ActiveStepIndex)
            return "wizStepFuture";
        else
            return "wizStepInProgress";
    }
    #endregion

    private string SessionKeyBase { get { return Page.User.Identity.Name + "bulkImport"; } }

    private string SessionKeyTZ { get { return SessionKeyBase + "TZ"; } }

    private string SessionKeyOpt { get { return SessionKeyBase + "Opt"; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        wzFlightsFromTelemetry.PreRender += new EventHandler(wzFlightsFromTelemetry_PreRender);

        if (IsPostBack)
        {
            Session[SessionKeyTZ] = TimeZone.SelectedTimeZone;
            Session[SessionKeyOpt] = AutofillOptionsChooser.Options;
        }
        else
        {
            Session[SessionKeyOpt] = new AutoFillOptions(Request.Cookies);
        }
    }

    protected void afuUpload_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            return;

        PendingFlight pf = new PendingFlight() { User = Page.User.Identity.Name };
        pf.FlightData = System.Text.Encoding.UTF8.GetString(e.GetContents());
        using (FlightData fd = new FlightData())
        {
            fd.AutoFill(pf, (AutoFillOptions) Session[SessionKeyOpt]);

            pf.FlightData = string.Empty;
            LogbookEntry leEmpty = new PendingFlight() { User = Page.User.Identity.Name, FlightData = string.Empty };
            if (!pf.IsEqualTo(leEmpty))
            {
                pf.TailNumDisplay = fd.TailNumber ?? string.Empty;
                // TODO: Save the flight data?  Perhaps de-sampled for size?
                pf.Commit();
            }
        }
    }

    protected void wzFlightsFromTelemetry_FinishButtonClick(object sender, WizardNavigationEventArgs e)
    {
        Response.Redirect("~/Member/ReviewPendingFlights.aspx");
    }
}