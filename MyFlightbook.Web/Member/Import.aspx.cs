using MyFlightbook;
using MyFlightbook.ImportFlights;
using MyFlightbook.ImportFlights.CloudAhoy;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Import : System.Web.UI.Page
{
    protected bool UseHHMM { get; set;}
    private const string szKeyVSCSVImporter = "viewStateKeyCSVImporter";
    private const string szKeyVSCSVData = "viewStateCSVData";
    private const string szKeyVSPendingOnly = "viewstatePendingOnly";

    #region WizardStyling
    // Thanks to http://weblogs.asp.net/grantbarrington/archive/2009/08/11/styling-the-asp-net-wizard-control-to-have-the-steps-across-the-top.aspx for how to do this.
    protected void wzImportFlights_PreRender(object sender, EventArgs e)
    {
        Repeater SideBarList = wzImportFlights.FindControl("HeaderContainer").FindControl("SideBarList") as Repeater;

        SideBarList.DataSource = wzImportFlights.WizardSteps;
        SideBarList.DataBind();
    }

    public string GetClassForWizardStep(object wizardStep)
    {
        WizardStep step = wizardStep as WizardStep;

        if (step == null)
            return "";

        int stepIndex = wzImportFlights.WizardSteps.IndexOf(step);

        if (stepIndex < wzImportFlights.ActiveStepIndex)
            return "wizStepCompleted";
        else if (stepIndex > wzImportFlights.ActiveStepIndex)
            return "wizStepFuture";
        else
            return "wizStepInProgress";
    }
    #endregion

    protected bool IsPendingOnly
    {
        get { return ViewState[szKeyVSPendingOnly] != null; }
        set { ViewState[szKeyVSPendingOnly] = (value ? value.ToString(CultureInfo.InvariantCulture) : null); }
    }

    /// <summary>
    /// The CSVImporter that is in progress
    /// </summary>
    protected CSVImporter CurrentImporter
    {
        get { return (CSVImporter)ViewState[szKeyVSCSVImporter]; }
        set
        {
            if (value == null)
                ViewState.Remove(szKeyVSCSVImporter);
            else
                ViewState[szKeyVSCSVImporter] = value;
        }
    }

    /// <summary>
    /// The uploaded data
    /// </summary>
    private byte[] CurrentCSVSource
    {
        get { return (byte[])ViewState[szKeyVSCSVData]; }
        set
        {
            if (value == null)
                ViewState.Remove(szKeyVSCSVData);
            else
                ViewState[szKeyVSCSVData] = value;
        }
    }

    private readonly Dictionary<int, string> m_errorContext = new Dictionary<int,string>();
    private Dictionary<int, string> ErrorContext
    {
        get { return m_errorContext; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.lbtImport;

        wzImportFlights.PreRender += new EventHandler(wzImportFlights_PreRender);

        Title = (string)GetLocalResourceObject("PageResource1.Title");
        Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
        UseHHMM = pf.UsesHHMM;

        if (!IsPostBack)
        {
            pnlCloudAhoy.Visible = pf.CloudAhoyToken != null && pf.CloudAhoyToken.AccessToken != null;
            List<FAQItem> lst = new List<FAQItem>(FAQItem.CachedFAQItems);
            FAQItem fi = lst.Find(f => f.idFAQ == 44);
            if (fi != null)
            {
                lblTipsHeader.Text = fi.Question;
                litTipsFAQ.Text = fi.Answer;
            }
        }
    }

    protected void AddTextRow(Control parent, string sz, string szClass = "")
    {
        if (parent == null)
            throw new ArgumentNullException("parent");
        Panel p = new Panel();
        parent.Controls.Add(p);
        Label l = new Label();
        p.Controls.Add(l);
        if (!String.IsNullOrEmpty(szClass))
            p.CssClass = szClass;
        l.Text = sz;
    }

    protected void rptPreview_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookEntry le = (LogbookEntry)e.Item.DataItem;

        PlaceHolder plc = (PlaceHolder)e.Item.FindControl("plcAdditional");
        // throw the less used properties into the final column
        if (le.EngineStart.HasValue())
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Engine Start: {0}", le.EngineStart.UTCDateFormatString()));
        if (le.EngineEnd.HasValue())
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Engine End: {0}", le.EngineEnd.UTCDateFormatString()));
        if (le.FlightStart.HasValue())
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Flight Start: {0}", le.FlightStart.UTCDateFormatString()));
        if (le.FlightEnd.HasValue())
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Flight End: {0}", le.FlightEnd.UTCDateFormatString()));
        if (le.HobbsStart != 0)
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Hobbs Start: {0}", le.HobbsStart));
        if (le.HobbsEnd != 0)
            AddTextRow(plc, String.Format(CultureInfo.CurrentCulture, "Hobbs End: {0}", le.HobbsEnd));

        // Add a concatenation of each property to the row as well.
        foreach (CustomFlightProperty cfp in le.CustomProperties)
            AddTextRow(plc, UseHHMM ? cfp.DisplayStringHHMM : cfp.DisplayString);

        if (!String.IsNullOrEmpty(le.ErrorString))
        {
            int iRow = e.Item.ItemIndex + 1;

            if (ErrorContext.ContainsKey(iRow))
            {
                ((Label)e.Item.FindControl("lblRawRow")).Text = ErrorContext[iRow];
                e.Item.FindControl("rowError").Visible = true;
                ((System.Web.UI.HtmlControls.HtmlTableRow)e.Item.FindControl("rowFlight")).Attributes["class"] = "error";
            }
            else
                e.Item.FindControl("imgNewOrUpdate").Visible = false;
        }

        if (!le.IsNewFlight && CurrentImporter != null && CurrentImporter.OriginalFlightsToModify.ContainsKey(le.FlightID))
        {
            IEnumerable<PropertyDelta> lst = CurrentImporter.OriginalFlightsToModify[le.FlightID].CompareTo(le, UseHHMM);
            if (lst.Count() > 0)
            {
                e.Item.FindControl("pnlDiffs").Visible = true;
                Repeater diffs = (Repeater)e.Item.FindControl("rptDiffs");
                diffs.DataSource = lst;
                diffs.DataBind();
            }
        }
    }

    protected void AddSuccessRow(LogbookEntry le, int iRow)
    {
        if (le == null)
            throw new ArgumentNullException("le");
    }

    protected void AddErrorRow(LogbookEntryBase le, string szContext, int iRow)
    {
        if (le == null)
            throw new ArgumentNullException("le");

        if (IsPendingOnly && le.LastError != LogbookEntryBase.ErrorCode.None)   // ignore errors if the importer is only pending flights and the error is a logbook validation error (no tail, future date, night, etc.)
            return;

        // if we're here, we are *either* not pending only *or* we didn't have a logbookentry validation error (e.g., could be malformed row)
        ErrorContext[iRow] = szContext; // save the context for data bind
        AddTextRow(plcErrorList, String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportRowHasError, iRow, le.ErrorString), "error");
    }

    protected void UploadData()
    {
        if (!fuPreview.HasFile && CurrentCSVSource != null && CurrentCSVSource.Length > 0)
        {
            // re-parse it.
            PreviewData();
            return;
        }

        Stream s = fuPreview.FileContent;
        plcErrorList.Controls.Clear();

        ViewState[szKeyVSCSVImporter] = null;

        if (s.Length > 0)
        {
            byte[] rgb = new byte[s.Length];
            s.Read(rgb, 0, (int) s.Length);
            CurrentCSVSource = rgb;
            PreviewData();
        }
        else
        {
            lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
            SetWizardStep(wsUpload);
        }
    }

    protected void PreviewData()
    {
        lblError.Text = "";

        mvPreviewResults.SetActiveView(vwPreviewResults);   // default to showing results.

        mfbImportAircraft1.CandidatesForImport = new AircraftImportMatchRow[0]; // start fresh every time.

        byte[] rgb = CurrentCSVSource;
        if (rgb == null || rgb.Length == 0)
        {
            lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
            SetWizardStep(wsUpload);
            return;
        }

        // issue #280: some files have \r\r\n as line separators!
        rgb = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(rgb).Replace("\r\r\n", "\r\n"));

        // Validate the file
        ExternalFormatImporter efi = ExternalFormatImporter.GetImporter(rgb);
        if (efi != null)
        {
            rgb = efi.PreProcess(rgb);
            IsPendingOnly = efi.IsPendingOnly;
        }

        MemoryStream ms = new MemoryStream(rgb);
        try
        {
            pnlConverted.Visible = pnlAudit.Visible = false;
            lblAudit.Text = string.Empty;
            using (CSVAnalyzer csvAnalyzer = new CSVAnalyzer(ms))
            {
                ms = null;  // Avoid CA2202
                CSVAnalyzer.CSVStatus result = csvAnalyzer.Status;
                hdnAuditState.Value = result.ToString();

                if (result != CSVAnalyzer.CSVStatus.Broken)
                {
                    string szCSV = null;
                    if (efi == null)    // was already CSV - only update it if it was fixed (vs. broken)
                    {
                        if (result == CSVAnalyzer.CSVStatus.Fixed)
                            szCSV = csvAnalyzer.DataAsCSV;
                    }
                    else  // But if it was converted, ALWAYS update the CSV.
                        szCSV = efi.CSVFromDataTable(csvAnalyzer.Data);

                    if (szCSV != null)
                        CurrentCSVSource = rgb = System.Text.Encoding.UTF8.GetBytes(szCSV);

                    // And show conversion, if it was converted
                    if (efi != null)
                    {
                        lblFileWasConverted.Text = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.importLabelFileWasConverted, efi.Name);
                        pnlConverted.Visible = true;
                    }
                }

                if (result != CSVAnalyzer.CSVStatus.OK)
                    pnlAudit.Visible = true;
                lblAudit.Text = csvAnalyzer.Audit;

                pnlAudit.Visible = pnlConverted.Visible || !String.IsNullOrEmpty(lblAudit.Text);
            }
        }
        finally
        {
            if (ms != null)
                ms.Dispose();
        }

        ErrorContext.Clear();
        CSVImporter csvimporter = CurrentImporter = new CSVImporter() { ModelNameMappings = mfbImportAircraft1.ModelMapping };
        AutoFillOptions afo = ckAutofill.Checked ? new AutoFillOptions(Request.Cookies) { IncludeHeliports = true } : null;
        using (MemoryStream ms2 = new MemoryStream(rgb))
            csvimporter.FInitFromStream(ms2, User.Identity.Name, AddSuccessRow, AddErrorRow, afo);

        if (csvimporter.FlightsToImport == null)
        {
            lblFileRequired.Text = csvimporter.ErrorMessage;
            SetWizardStep(wsUpload);
            return;
        }

        rptPreview.DataSource = csvimporter.FlightsToImport;
        rptPreview.DataBind();
        mvPreview.SetActiveView(csvimporter.FlightsToImport.Count > 0 ? vwPreview : vwNoResults);

        mvMissingAircraft.SetActiveView(vwNoMissingAircraft); // default state.

        if (csvimporter.FlightsToImport.Count > 0)
        {
            if (csvimporter.HasErrors)
            {
                if (!IsPendingOnly)
                    lblError.Text = String.Format(CultureInfo.InvariantCulture, "<p>{0}</p><p>{1}</p>", Resources.LogbookEntry.ImportPreviewNotSuccessful, lblError.Text);

                List<AircraftImportMatchRow> missing = new List<AircraftImportMatchRow>(csvimporter.MissingAircraft);
                if (missing.Count > 0)
                {
                    mfbImportAircraft1.CandidatesForImport = missing;
                    mvMissingAircraft.SetActiveView(vwAddMissingAircraft);
                }

                ((Button)wzImportFlights.FindControl("FinishNavigationTemplateContainerID$btnNewFile")).Visible = true;
            }

            ((AjaxControlToolkit.ConfirmButtonExtender)wzImportFlights.FindControl("FinishNavigationTemplateContainerID$confirmImportWithErrors")).Enabled = csvimporter.HasErrors;
        }
    }

    protected void SetWizardStep(WizardStep ws)
    {
        for (int i = 0; i < wzImportFlights.WizardSteps.Count; i++)
            if (wzImportFlights.WizardSteps[i] == ws)
            {
                wzImportFlights.ActiveStepIndex = i;
                break;
            }
    }

    protected void Import(object sender, WizardNavigationEventArgs e)
    {
        CSVImporter csvimporter = CurrentImporter;

        if (e == null)
            throw new ArgumentNullException("e");

        if (csvimporter == null)
        {
            lblError.Text = Resources.LogbookEntry.ImportNotSuccessful;
            e.Cancel = true;
            SetWizardStep(wsPreview);
            PreviewData();  // rebuild the table.
            return;
        }

        int cFlightsAdded = 0;
        int cFlightsUpdated = 0;
        int cFlightsWithErrors = 0;

        csvimporter.FCommit((le, fIsNew) =>
                {
                    if (String.IsNullOrEmpty(le.ErrorString))
                    {
                        AddTextRow(plcProgress, String.Format(CultureInfo.CurrentCulture, fIsNew ? Resources.LogbookEntry.ImportRowAdded : Resources.LogbookEntry.ImportRowUpdated, le.ToString()), "success");
                        if (fIsNew)
                            cFlightsAdded++;
                        else
                            cFlightsUpdated++;
                    }
                    else
                    {
                        PendingFlight pf = new PendingFlight(le) { User = User.Identity.Name };
                        pf.Commit();
                        lnkPending.Visible = true;
                        AddTextRow(plcProgress, String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportRowAddedPending, le.ToString(), le.ErrorString), "error");
                        cFlightsWithErrors++;
                    }
                }, 
                (le, ex) =>
                {
                    AddTextRow(plcProgress, String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportRowNotAdded, le.ToString(), ex.Message), "error");
                }, 
                true);

        List<string> lstResults = new List<string>();
        if (cFlightsAdded > 0)
            lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsAdded, cFlightsAdded));
        if (cFlightsUpdated > 0)
            lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsUpdated, cFlightsUpdated));
        if (cFlightsWithErrors > 0)
            lstResults.Add(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportFlightsWithErrors, cFlightsWithErrors));
        rptImportResults.DataSource = lstResults;
        rptImportResults.DataBind();
        MyFlightbook.Profile.GetUser(Page.User.Identity.Name).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);
        mvPreviewResults.SetActiveView(vwImportResults);
        wzImportFlights.Visible = false;
        pnlImportSuccessful.Visible = true;
        CurrentImporter = null;
        CurrentCSVSource = null;
    }

    protected void wzImportFlights_ActiveStepChanged(object sender, EventArgs e)
    {
        if (wzImportFlights.ActiveStep == wsMissingAircraft)
            UploadData();
        else if (wzImportFlights.ActiveStep == wsPreview)
            PreviewData();
        else
        {
            CurrentCSVSource = null;
            CurrentImporter = null;
        }

        mvContent.ActiveViewIndex = wzImportFlights.ActiveStepIndex;    // keep bottom half and top half in sync
    }

    protected void lnkDefaultTemplate_Click(object sender, EventArgs e)
    {
        const string szHeaders = @"Date,Tail Number,Approaches,Hold,Landings,FS Night Landings,FS Day Landings,X-Country,Night,IMC,Simulated Instrument,Ground Simulator,Dual Received,CFI,SIC,PIC,Total Flight Time,Route,Comments";

        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", Branding.CurrentBrand.AppName);
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write(szHeaders.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator));
        Response.End();
    }

    protected void btnDownloadConverted_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}{1}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""), hdnAuditState.Value);
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
        Response.Write(System.Text.Encoding.UTF8.GetString(CurrentCSVSource));
        Response.End();
    }

    protected void btnNewFile_Click(object sender, EventArgs e)
    {
        SetWizardStep(wsUpload);
        ((Control)sender).Visible = false;
    }

    protected async void btnImportCloudAhoy_Click(object sender, EventArgs e)
    {
        Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
        CloudAhoyClient client = new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)) { AuthState = pf.CloudAhoyToken };
        DateTime? dtStart = null;
        if (mfbCloudAhoyStartDate.Date.HasValue())
            dtStart = mfbCloudAhoyStartDate.Date;
        DateTime? dtEnd = null;
        if (mfbCloudAhoyEndDate.Date.HasValue())
            dtEnd =  mfbCloudAhoyEndDate.Date;

        try
        {
            IEnumerable<CloudAhoyFlight> rgcaf = await client.GetFlights(User.Identity.Name, dtStart, dtEnd);
            foreach (CloudAhoyFlight caf in rgcaf)
            {
                PendingFlight pendingflight = caf.ToLogbookEntry() as PendingFlight;
                if (pendingflight != null)
                    pendingflight.Commit();
            }
            Response.Redirect("~/Member/ReviewPendingFlights.aspx");
        }
        catch (MyFlightbookException ex)
        {
            // Cloudahoy is sending back HTML
            lblCloudAhoyErr.Text = ex.Message;
            popupCloudAhoy.Show();
        }
        catch (MyFlightbookValidationException ex)
        {
            lblCloudAhoyErr.Text = ex.Message;
            popupCloudAhoy.Show();
        }
    }
}
