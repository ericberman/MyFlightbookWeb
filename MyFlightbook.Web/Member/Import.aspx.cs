using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    public partial class ImportPage : Web.WizardPage.MFBWizardPage
    {
        protected bool UseHHMM { get; set; }
        private const string szKeyVSCSVImporter = "viewStateKeyCSVImporter";
        private const string szKeyVSCSVData = "viewStateCSVData";
        private const string szKeyVSPendingOnly = "viewstatePendingOnly";

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

        protected string IDNext { get { return wzImportFlights.FindControl("StepNavigationTemplateContainerID$btnNext").ClientID; } }

        protected string IDPrev { get { return wzImportFlights.FindControl("StepNavigationTemplateContainerID$btnPrev").ClientID; } }

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

        private readonly Dictionary<int, string> m_errorContext = new Dictionary<int, string>();
        private Dictionary<int, string> ErrorContext
        {
            get { return m_errorContext; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.lbtImport;
            InitWizard(wzImportFlights);

            Title = Resources.LogbookEntry.ImportFlightsPageTitle;
            Profile pf = Profile.GetUser(User.Identity.Name);
            UseHHMM = pf.UsesHHMM;
            

            if (!IsPostBack)
            {
                lblAutofillPreferred.Text = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ImportWizardAutofillTryPreferred, pf.PreferredTimeZone.DisplayName).Linkify();
            }
        }

        protected static void AddTextRow(Control parent, string sz, string szClass = "")
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            Panel p = new Panel();
            parent.Controls.Add(p);
            Label l = new Label();
            p.Controls.Add(l);
            if (!String.IsNullOrEmpty(szClass))
                p.CssClass = szClass;
            l.Text = HttpUtility.HtmlEncode(sz);
        }

        protected void rptPreview_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookEntryCore le = (LogbookEntryCore)e.Item.DataItem;

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

                if (ErrorContext.TryGetValue(iRow, out string errVal))
                {
                    ((Label)e.Item.FindControl("lblRawRow")).Text = errVal;
                    e.Item.FindControl("rowError").Visible = true;
                    ((System.Web.UI.HtmlControls.HtmlTableRow)e.Item.FindControl("rowFlight")).Attributes["class"] = "error";
                }
                else
                    e.Item.FindControl("imgNewOrUpdate").Visible = false;
            }

            if (!le.IsNewFlight && CurrentImporter != null && CurrentImporter.OriginalFlightsToModify.TryGetValue(le.FlightID, out LogbookEntry value))
            {
                List<PropertyDelta> lst = new List<PropertyDelta>(value.CompareTo(le, UseHHMM));
                if (lst.Count > 0)
                {
                    e.Item.FindControl("pnlDiffs").Visible = true;
                    Repeater diffs = (Repeater)e.Item.FindControl("rptDiffs");
                    diffs.DataSource = lst;
                    diffs.DataBind();
                }
            }
        }

        protected static void AddSuccessRow(LogbookEntryCore le, int iRow) { }

        protected void AddErrorRow(LogbookEntryCore le, string szContext, int iRow)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            if (IsPendingOnly && le.LastError != LogbookEntryCore.ErrorCode.None)   // ignore errors if the importer is only pending flights and the error is a logbook validation error (no tail, future date, night, etc.)
                return;

            // if we're here, we are *either* not pending only *or* we didn't have a logbookentry validation error (e.g., could be malformed row)
            ErrorContext[iRow] = szContext; // save the context for data bind
            AddTextRow(plcErrorList, String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportRowHasError, iRow, le.ErrorString), "error");
        }

        protected void UploadData()
        {
            plcErrorList.Controls.Clear();

            ViewState[szKeyVSCSVImporter] = null;

            if (CurrentCSVSource != null && CurrentCSVSource.Length > 0)
            {
                // re-parse it.
                PreviewData();
            }
            else
            {
                lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
                SetWizardStep(wsUpload);
            }
        }

        const string szSessFile = "mfbImportFile";

        protected void AjaxFileUpload1_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            {
                lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
                SetWizardStep(wsUpload);
                return;
            }

            Session[szSessFile] = e.GetContents();

            e.DeleteTemporaryData();

            // Now we wait for the force refresh
        }

        protected void btnForceRefresh_Click(object sender, EventArgs e)
        {
            if (Session[szSessFile] != null)
            {
                CurrentImporter = null;
                CurrentCSVSource = (byte[])Session[szSessFile];
                Session[szSessFile] = null;

                SetWizardStep(wsMissingAircraft);
            }
            else
            {
                lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
                SetWizardStep(wsUpload);
            }
        }


        protected void PreviewData()
        {
            lblError.Text = string.Empty;

            mvPreviewResults.SetActiveView(vwPreviewResults);   // default to showing results.

            mfbImportAircraft1.CandidatesForImport = Array.Empty<AircraftImportMatchRow>(); // start fresh every time.

            byte[] rgb = CurrentCSVSource;
            if (rgb == null || rgb.Length == 0)
            {
                lblFileRequired.Text = Resources.LogbookEntry.errImportInvalidCSVFile;
                SetWizardStep(wsUpload);
                return;
            }

            int cOriginalSize = rgb.Length;

            pnlConverted.Visible = pnlAudit.Visible = false;

            ExternalFormatConvertResults results = ExternalFormatConvertResults.ConvertToCSV(rgb);
            lblAudit.Text = results.AuditResult;
            hdnAuditState.Value = results.ResultString;
            CurrentCSVSource = results.GetConvertedBytes();

            int cConvertedSize = CurrentCSVSource.Length;

            IsPendingOnly = IsPendingOnly || results.IsPendingOnly; // results can change between first preview and import, so if it's true anywhere along the way, preserve that.

            if (!String.IsNullOrEmpty(results.ConvertedName))
            {
                lblFileWasConverted.Text = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.importLabelFileWasConverted, results.ConvertedName);
                pnlConverted.Visible = true;
            }

            pnlAudit.Visible = (results.IsFixedOrBroken);
            if (results.IsBroken)
            {
                lblAudit.CssClass = "error";
                ExpandoAudit.ExpandoControl.Collapsed = false;
                ExpandoAudit.ExpandoControl.ClientState = "false";
            }
            else
            {
                lblAudit.CssClass = string.Empty;
                ExpandoAudit.ExpandoControl.Collapsed = true;
                ExpandoAudit.ExpandoControl.ClientState = "true";
            }

            pnlAudit.Visible = pnlConverted.Visible || !String.IsNullOrEmpty(lblAudit.Text);

            ErrorContext.Clear();
            CSVImporter csvimporter = CurrentImporter = new CSVImporter(mfbImportAircraft1.ModelMapping);

            // Issue #1172 - support for local times.

            // make a copy of the autofill options so that we don't muck up user's settings in memory
            AutoFillOptions afo = rbAutofillNone.Checked ? null : new AutoFillOptions(AutoFillOptions.DefaultOptionsForUser(User.Identity.Name));
            if (afo != null)
            {
                afo.IncludeHeliports = true;
                afo.TimeConversion = (rbAutofillUtc.Checked ? AutoFillOptions.TimeConversionCriteria.None : (rbAutofillLocal.Checked ? AutoFillOptions.TimeConversionCriteria.Local : AutoFillOptions.TimeConversionCriteria.Preferred));
                if (afo.TimeConversion != AutoFillOptions.TimeConversionCriteria.None)
                    afo.AutoFillTotal = AutoFillOptions.AutoFillTotalOption.EngineTime; // we will rewrite engine times, but not block times.
                if (afo.TimeConversion == AutoFillOptions.TimeConversionCriteria.Preferred)
                    afo.PreferredTimeZone = Profile.GetUser(User.Identity.Name).PreferredTimeZone;
            }
            csvimporter.InitWithBytes(CurrentCSVSource, User.Identity.Name, AddSuccessRow, AddErrorRow, afo);

            if (csvimporter.FlightsToImport == null || !String.IsNullOrEmpty(csvimporter.ErrorMessage))
            {
                lblFileRequired.Text = csvimporter.ErrorMessage;
                SetWizardStep(wsUpload);
                return;
            }

            rptPreview.DataSource = csvimporter.FlightsToImport;
            rptPreview.DataBind();
            mvPreview.SetActiveView(csvimporter.FlightsToImport.Count > 0 ? vwPreview : vwNoResults);

            if (wzImportFlights.ActiveStep == wsMissingAircraft)
                EventRecorder.LogCall("Import Preview - User: {user}, Upload size {cbin}, converted size {cbconvert}, flights found: {flightcount}", User.Identity.Name, cOriginalSize, cConvertedSize, csvimporter.FlightsToImport.Count);

            mvMissingAircraft.SetActiveView(vwNoMissingAircraft); // default state.

            if (csvimporter.FlightsToImport.Count > 0)
            {
                if (csvimporter.HasErrors)
                {
                    if (!IsPendingOnly)
                        lblError.Text = Resources.LogbookEntry.ImportPreviewNotSuccessful;

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

        protected string ImportButtonClientID
        {
            get
            {
                return wzImportFlights.FindControl("FinishNavigationTemplateContainerID").FindControl("btnImport").ClientID.Trim();
            }
        }

        protected void Import(object sender, WizardNavigationEventArgs e)
        {
            CSVImporter csvimporter = CurrentImporter;

            if (e == null)
                throw new ArgumentNullException(nameof(e));

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
                            reviewPending.Visible = true;
                            if (!IsPendingOnly)
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
            Profile.GetUser(Page.User.Identity.Name).SetAchievementStatus();
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
            pnl3rdPartyServices.Visible = wzImportFlights.ActiveStep == wsCreateFile;
        }

        protected void lnkDefaultTemplate_Click(object sender, EventArgs e)
        {
            const string szHeaders = @"Date,Tail Number,Model,Approaches,Hold,Landings,FS Night Landings,FS Day Landings,X-Country,Night,IMC,Simulated Instrument,Ground Simulator,Dual Received,CFI,SIC,PIC,Total Flight Time,Route,Comments";

            Response.DownloadToFile(szHeaders.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator), "text/csv", Branding.CurrentBrand.AppName, "csv");
        }

        protected void btnDownloadConverted_Click(object sender, EventArgs e)
        {
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            Response.DownloadToFile(CurrentCSVSource,
                "text/csv",
                String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}{3}", Branding.CurrentBrand.AppName, Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), hdnAuditState.Value).Replace(" ", "-"),
                "csv");
        }

        protected void btnNewFile_Click(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            SetWizardStep(wsUpload);
            ((Control)sender).Visible = false;
        }
    }
}