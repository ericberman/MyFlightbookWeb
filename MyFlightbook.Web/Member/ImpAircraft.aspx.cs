using MyFlightbook;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ImpAircraft : System.Web.UI.Page
{
    private const string szvsMatchesKey = "vsMatchesKey";
    private const string szvsCSVRawText = "vsCSVRaw";

    #region Wizard Management
    protected enum ImportStep { stepFile, stepExisting, stepNew };

    protected void wzImportAircraft_NextButtonClick(object sender, WizardNavigationEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.NextStepIndex == (int)ImportStep.stepExisting)
        {
            // We re-parse each time we come to this step to account for edits that were made without postbacks

            // Re-init the CSV text:
            if (fuCSVAircraft.HasFile && fuCSVAircraft.PostedFile.ContentLength > 0)
                RawCSV = new StreamReader(fuCSVAircraft.PostedFile.InputStream).ReadToEnd();

            if (String.IsNullOrEmpty(RawCSV))
            {
                lblUploadErr.Text = fuCSVAircraft.HasFile ? Resources.Aircraft.errImportEmptyFile : Resources.Aircraft.errImportNoFile;
                e.Cancel = true;
                return;
            }

            if (!InitFromCSV(RawCSV))
                e.Cancel = true;
        }
    }

    protected void wzImportAircraft_PreviousButtonClick(object sender, WizardNavigationEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        // Re-init since we're no longer using post-backs.
        if (e.NextStepIndex == (int)ImportStep.stepExisting)
            InitFromCSV(RawCSV);
    }

    protected void wzImportAircraft_ActiveStepChanged(object sender, EventArgs e)
    {
        mvAircraftToImport.Visible = wzImportAircraft.ActiveStepIndex > 0;
        mvAircraftToImport.SetActiveView(wzImportAircraft.ActiveStepIndex == 1 ? vwMatchExisting : vwNoMatch);
    }

    #region WizardStyling
    // Thanks to http://weblogs.asp.net/grantbarrington/archive/2009/08/11/styling-the-asp-net-wizard-control-to-have-the-steps-across-the-top.aspx for how to do this.
    protected void wzImportAircraft_PreRender(object sender, EventArgs e)
    {
        Repeater SideBarList = wzImportAircraft.FindControl("HeaderContainer").FindControl("SideBarList") as Repeater;

        SideBarList.DataSource = wzImportAircraft.WizardSteps;
        SideBarList.DataBind();
    }

    public string GetClassForWizardStep(object wizardStep)
    {
        WizardStep step = wizardStep as WizardStep;

        if (step == null)
            return "";

        int stepIndex = wzImportAircraft.WizardSteps.IndexOf(step);

        if (stepIndex < wzImportAircraft.ActiveStepIndex)
            return "wizStepCompleted";
        else if (stepIndex > wzImportAircraft.ActiveStepIndex)
            return "wizStepFuture";
        else
            return "wizStepInProgress";
    }
    #endregion
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        wzImportAircraft.PreRender += new EventHandler(wzImportAircraft_PreRender);

        this.Master.SelectedTab = tabID.actImportAircraft;
        this.Title = Master.Title = (string)GetLocalResourceObject("PageResource1.Title");

        if (IsPostBack)
        {
            // some of the unmatched aircraft may have been matched asynchronously.
            // Check to see
            if (wzImportAircraft.ActiveStep.ID == stepUnmatched.ID)
            {
                Collection<AircraftImportMatchRow> lstUnMatched = Matches.AllUnmatched;
                Aircraft[] rgac = new UserAircraft(Page.User.Identity.Name).GetAircraftForUser();

                foreach (AircraftImportMatchRow mr in lstUnMatched)
                {
                    foreach (Aircraft ac in rgac)
                        if (String.Compare(ac.TailNumber, mr.TailNumber, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            mr.State = AircraftImportMatchRow.MatchState.JustAdded;
                            break;
                        }
                }
            }
        }
    }

    #region Properties
    private AircraftImportParseContext Matches
    {
        get { return (AircraftImportParseContext) ViewState[szvsMatchesKey]; }
        set { ViewState[szvsMatchesKey] = value; }
    }

    /// <summary>
    /// The raw CSV that was uploaded - so that we can re-parse as needed
    /// </summary>
    protected string RawCSV
    {
        get { return (string)ViewState[szvsCSVRawText]; }
        set { ViewState[szvsCSVRawText] = value; }
    }
    #endregion

    protected void UpdateGrid()
    {
        if (Matches == null || Matches.MatchResults == null)
            return;
        mfbImportAircraftExisting.CandidatesForImport = Matches.AllMatched;
        mfbImportAircraftNew.CandidatesForImport = Matches.AllUnmatched;
    }

    #region Parsing
    protected bool InitFromCSV(string szCSVToParse)
    {
        bool fResult = true;

        AircraftImportParseContext aipc = null;

        try
        {
            // Initial pass - no database hit, just the tailnumbers and models.  We'll populate useraircraft/all aircraft afterwards
            Matches = aipc = new AircraftImportParseContext(szCSVToParse, Page.User.Identity.Name);

            if (aipc.RowsFound && aipc.MatchResults.Count == 0)
                throw new MyFlightbookException(Resources.Aircraft.errImportEmptyFile);

            aipc.ProcessParseResultsForUser(Page.User.Identity.Name);

            UpdateGrid();
            lblCountMatchExisting.Text = aipc.MatchResults.Count(mr => mr.State == AircraftImportMatchRow.MatchState.MatchedExisting).ToString(CultureInfo.InvariantCulture);
            lblCountMatchProfile.Text = aipc.MatchResults.Count(mr => mr.State == AircraftImportMatchRow.MatchState.MatchedInProfile || mr.State == AircraftImportMatchRow.MatchState.JustAdded).ToString(CultureInfo.InvariantCulture);
            lblCountUnmatched.Text = aipc.MatchResults.Count(mr => mr.State == AircraftImportMatchRow.MatchState.UnMatched).ToString(CultureInfo.InvariantCulture);
        }
        catch (MyFlightbookException ex) 
        { 
            lblUploadErr.Text = ex.Message;
            fResult = false;
            if (aipc != null)
                aipc.CleanUpBestMatchAircraft();
        }

        return fResult;
    }
    #endregion

    protected void NewMakeAdded(object sender, EventArgs e)
    {
        if (mfbEditMake1.MakeID != -1)
        {
            // refresh the list of make/models and select the newly added make
            UpdateGrid();

            // reset the add-make panel
            cpeAddMake.Collapsed = true;
            cpeAddMake.ClientState = "True";

            mfbEditMake1.MakeID = -1; // only add with this control - no in-place editing!
        }
    }
    
    #region Import All
    protected void btnImportAllExisting_Click(object sender, EventArgs e)
    {
        Matches.AddAllExistingAircraftForUser(Page.User.Identity.Name);
        wzImportAircraft.ActiveStepIndex++;
    }

    protected void ImportAllNew(object sender, EventArgs e)
    {
        // Fix up any errors.
        Matches.ProcessParseResultsForUser(Page.User.Identity.Name);
        if (Matches.AddAllNewAircraftForUser(Page.User.Identity.Name))
        {
            Response.Redirect("~/Member/Aircraft.aspx");
        }
        else
        {
            lblImportError.Text = Resources.Aircraft.errImportFixErrors;
            UpdateGrid();
        }
    }
    #endregion
}