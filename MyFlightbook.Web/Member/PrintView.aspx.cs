using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class PrintViewBase : Page
    {
        #region Properties
        protected Profile CurrentUser { get; set; }

        protected string CurrentUserDOB
        {
            get
            {
                DateTime? dob = CurrentUser?.DateOfBirth;
                return (dob == null || !dob.HasValue || !dob.Value.HasValue()) ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Profile.dateOfBirthFormat, dob.Value);
            }
        }

        protected bool OverrideRender { get; set; }

        protected bool SuppressFooter { get; set; }

        protected PrintingOptions PrintOptions { get; set; }

        private const string szSessKeyPDFError = "pdfRenderError";

        protected string PDFError
        {
            get { return ((string)Session[szSessKeyPDFError]) ?? string.Empty; }
            set { Session[szSessKeyPDFError] = value; }
        }
        #endregion

        #region Linking Utilities
        protected string PermaLink(PrintingOptions po, FlightQuery fq)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            string szStudent = util.GetStringParam(Request, "u");
            return String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}",
                Request.Url.Scheme,
                Request.Url.Host,
                VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/PrintView.aspx?po={0}&fq={1}{2}",
                HttpUtility.UrlEncode(Convert.ToBase64String(JsonConvert.SerializeObject(po, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }).Compress())),
                HttpUtility.UrlEncode(fq.ToBase64CompressedJSONString()),
                String.IsNullOrEmpty(szStudent) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "&u={0}", szStudent))
                ));
        }

        protected string ReturnLink(FlightQuery fq)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            return String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", HttpUtility.UrlEncode(fq.ToBase64CompressedJSONString()));
        }
        #endregion

        protected void InitPrintingOptionsFromCompressedJSON(string sz)
        {
            PrintOptions = String.IsNullOrEmpty(sz)
                ? new PrintingOptions()
                : JsonConvert.DeserializeObject<PrintingOptions>(Convert.FromBase64String(sz).Uncompress());
        }


        protected override void Render(HtmlTextWriter writer)
        {
            if (OverrideRender)
            {
                PDFRenderer.RenderFile(
                    PrintOptions.PDFSettings,
                    (htwOut) => { base.Render(htwOut); },
                    (szErr) => // onError
                    {
                        util.NotifyAdminEvent("Error saving PDF", String.Format(CultureInfo.CurrentCulture, "{0}\r\nUser: {1}\r\nOptions:\r\n{2}\r\n",
                            szErr,
                            CurrentUser.UserName,
                            JsonConvert.SerializeObject(PrintOptions)
                            ), ProfileRoles.maskSiteAdminOnly);

                        // put the error into the session
                        PDFError = szErr;
                        Response.Redirect(Request.Url.PathAndQuery);
                    },
                    (szOutputPDF) => // onSuccess
                    {
                        try
                        {
                            Page.Response.Clear();
                            Page.Response.ContentType = "application/pdf";
                            Response.AddHeader("content-disposition", String.Format(CultureInfo.CurrentCulture, @"attachment;filename=""{0}.pdf""", CurrentUser.UserFullName));
                            Response.WriteFile(szOutputPDF);
                            Page.Response.Flush();

                            // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                            Page.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                            HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                        }
                        catch (HttpUnhandledException) { }  // sometimes the remote host has closed the connection - allow cleanup to proceed.
                        catch (HttpException) { }
                    });
            }
            else
                base.Render(writer);
        }

        protected string GetUserName(out string szError)
        {
            szError = string.Empty;
            string szUser = Page.User.Identity.Name;    // default value
            string szStudent = util.GetStringParam(Request, "u");
            if (!String.IsNullOrEmpty(szStudent))
            {
                // See if the current user is authorized to view print view of the specified user
                CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
                InstructorStudent student = CFIStudentMap.GetInstructorStudent(sm.Students, szStudent);
                if (student == null)
                    szError = Resources.SignOff.ViewStudentNoSuchStudent;
                else if (!student.CanViewLogbook)
                    szError = Resources.SignOff.ViewStudentLogbookUnauthorized;
                else
                    szUser = szStudent;     // all good - can view.
            }
            return szUser;
        }
    }

    public partial class PrintView : PrintViewBase
    {
        #region properties
        protected IPrintingTemplate ActiveTemplate
        {
            get
            {
                View v = mvLayouts.Views[mvLayouts.ActiveViewIndex];
                foreach (Control c in v.Controls)
                {
                    if (c is IPrintingTemplate pt)
                        return pt;
                }
                return null;
            }
        }
        #endregion

        private void InitializeRestriction()
        {
            string szFQParam = util.GetStringParam(Request, "fq");
            if (!String.IsNullOrEmpty(szFQParam))
            {
                FlightQuery fq = FlightQuery.FromBase64CompressedJSON(szFQParam);

                if (fq.UserName.CompareCurrentCultureIgnoreCase(CurrentUser.UserName) != 0)
                    return; // do nothing if this isn't for the current user

                mfbSearchForm1.Restriction = fq;
                Master.HasFooter = Master.HasHeader = false;
                if (!mfbSearchForm1.Restriction.IsDefault)
                    TabContainer1.ActiveTab = tpFilter;
                lnkReturnToFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", szFQParam);
            }
        }

        private void HandlePDFErr()
        {
            if (!String.IsNullOrEmpty(PDFError)) {
                lblErr.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PDFGenerationFailed, PDFError));
                PDFError = null;
            }
        }

        private PDFOptions pdfOptions
        {
            get
            {
                return new PDFOptions()
                {
                    PaperSize = (PDFOptions.PageSize)Enum.Parse(typeof(PDFOptions.PageSize), cmbPageSize.SelectedValue),
                    PageHeight = decCustHeight.IntValue,
                    PageWidth = decCustWidth.IntValue,
                    Orientation = rbLandscape.Checked ? PDFOptions.PageOrientation.Landscape : PDFOptions.PageOrientation.Portrait,
                    FooterUri = VirtualPathUtility.ToAbsolute("~/Public/PrintFooter.aspx/" + PDFOptions.PathEncodeOptions(ckIncludeCoverSheet.Checked, ckPrintTotalPages.Checked)).ToAbsoluteURL(Request),
                    LeftMargin = decLeftMargin.IntValue,
                    RightMargin = decRightMargin.IntValue,
                    TopMargin = decTopMargin.IntValue,
                    BottomMargin = decBottomMargin.IntValue
                };
            }
            set
            {
                cmbPageSize.SelectedValue = value.PaperSize.ToString();
                decCustHeight.IntValue = value.PageHeight;
                decCustWidth.IntValue = value.PageWidth;
                rbLandscape.Checked = value.Orientation == PDFOptions.PageOrientation.Landscape;
                rbPortrait.Checked = value.Orientation == PDFOptions.PageOrientation.Portrait;
                decLeftMargin.IntValue = value.LeftMargin ?? decLeftMargin.DefaultValueInt;
                decRightMargin.IntValue = value.RightMargin ?? decRightMargin.DefaultValueInt;
                decTopMargin.IntValue = value.TopMargin ?? decTopMargin.DefaultValueInt;
                decBottomMargin.IntValue = value.BottomMargin ?? decBottomMargin.DefaultValueInt;
            }
        }

        private PrintingSections printingSections
        {
            get
            {
                return new PrintingSections()
                {
                    Endorsements = ckEndorsements.Checked ? (ckIncludeEndorsementImages.Checked ? PrintingSections.EndorsementsLevel.DigitalAndPhotos : PrintingSections.EndorsementsLevel.DigitalOnly) : PrintingSections.EndorsementsLevel.None,
                    IncludeFlights = ckFlights.Checked,
                    IncludeCoverPage = ckIncludeCoverSheet.Checked,
                    IncludeTotals = ckTotals.Checked
                };
            }
            set
            {
                ckEndorsements.Checked = value.Endorsements != PrintingSections.EndorsementsLevel.None;
                ckIncludeEndorsementImages.Checked = value.Endorsements == PrintingSections.EndorsementsLevel.DigitalAndPhotos;
                ckFlights.Checked = value.IncludeFlights;
                ckIncludeCoverSheet.Checked = value.IncludeCoverPage;
                ckTotals.Checked = value.IncludeTotals;
            }
        }


        private void InitializePrintOptions()
        {
            InitPrintingOptionsFromCompressedJSON(util.GetStringParam(Request, "po"));
            PrintingOptions po = PrintOptions;
            PrintOptions1.Options = po;
            pdfOptions = po.PDFSettings;
            printingSections = po.Sections;
        }

        private void InitializeTitleAndQueryDescriptor()
        {
            Master.Title = lblUserName.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, CurrentUser.UserFullName);
            mfbQueryDescriptor.DataSource = mfbTotalSummary1.CustomRestriction = mfbSearchForm1.Restriction;
            mfbQueryDescriptor.DataBind();
            mfbTotalSummary1.DataBind();
        }

        private void InitializeEndorsements()
        {
            // Set up endorsements
            mfbEndorsementList.Student = CurrentUser.UserName;
            mfbEndorsementList.RefreshEndorsements();
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, CurrentUser.UserName);
            il.Refresh(fIncludeDocs: false, fIncludeVids: false);
            rptImages.DataSource = il.ImageArray;
            rptImages.DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // wire up the logbook to the current user
            Master.SelectedTab = tabID.lbtPrintView;

            string szUser = GetUserName(out string szError);
            lblErr.Text = szError;

            CurrentUser = Profile.GetUser(szUser);

            mfbSearchForm1.Username = mfbTotalSummary1.Username = PrintOptions1.UserName = CurrentUser.UserName;

            pnlResults.Visible = false; // since we aren't saving viewstate, bind to nothing  (e.g., in case we're editing a flight query)

            Master.SuppressTopNavPrint = true;
            string szPath = Request.Url.PathAndQuery.Substring(0, Request.Url.PathAndQuery.LastIndexOf("/", StringComparison.Ordinal) + 1);
            string szURL = szPath.ToAbsoluteURL(Request.Url.Scheme, Request.Url.Host, Request.Url.Port).ToString();
            Master.Page.Header.Controls.Add(new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<base href=\"{0}\" />", szURL)));

            cmbPageSize.Attributes["onchange"] = "pageSizeChanged(this)";

            if (!IsPostBack)
            {
                InitializeRestriction();

                HandlePDFErr();

                InitializePrintOptions();

                InitializeTitleAndQueryDescriptor();

                InitializeEndorsements();

                RefreshLogbookData();

                imgLogo.ImageUrl = Branding.CurrentBrand.LogoHRef;
            }

            rowCustomPage.Style["display"] = (PDFOptions.PageSize)Enum.Parse(typeof(PDFOptions.PageSize), cmbPageSize.SelectedValue) == PDFOptions.PageSize.Custom ? "block" : "none";
        }

        protected void btnChangeQuery_Click(object sender, EventArgs e)
        {
            mvSearch.SetActiveView(vwSearchForm);
        }

        protected void mfbSearchForm1_Reset(object sender, FlightQueryEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            mfbSearchForm1.Restriction = e.Query;
            mvSearch.SetActiveView(vwDescriptor);
            lnkReturnToFlights.NavigateUrl = "~/Member/LogbookNew.aspx";
        }

        protected void FilterResults(object sender, FlightQueryEventArgs e)
        {
            mfbQueryDescriptor.DataSource = mfbTotalSummary1.CustomRestriction = mfbSearchForm1.Restriction;
            mfbQueryDescriptor.DataBind();
            RefreshLogbookData();
            mfbTotalSummary1.DataBind();
            mvSearch.SetActiveView(vwDescriptor);
            lnkReturnToFlights.NavigateUrl = ReturnLink(mfbSearchForm1.Restriction);
        }

        protected void RefreshLogbookData()
        {
            mvLayouts.ActiveViewIndex = (int)PrintOptions1.Options.Layout;
            pnlResults.Visible = true;
            lblCoverDate.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintViewCoverSheetDateTemplate, DateTime.Now);

            PrintingOptions po = PrintOptions1.Options;

            // Set any global font appropriately.
            pnlResults.Style["font-size"] = po.Size == PrintingOptions.FontSize.Normal ? "9pt" : String.Format(CultureInfo.InvariantCulture, po.Size == PrintingOptions.FontSize.Small ? "7pt" : "11pt");

            // Make sure that any PDF and section information is up-to-date, and that the PrintOptions property is set up correctly
            po.Sections = printingSections;
            po.PDFSettings = pdfOptions;
            PrintOptions = po;

            // Make sure copy link is up-to-date
            lnkPermalink.NavigateUrl = PermaLink(po, mfbSearchForm1.Restriction);

            mvLayouts.Visible = po.Sections.IncludeFlights;
            pnlEndorsements.Visible = po.Sections.Endorsements != PrintingSections.EndorsementsLevel.None;
            rptImages.Visible = po.Sections.Endorsements == PrintingSections.EndorsementsLevel.DigitalAndPhotos;
            pnlTotals.Visible = po.Sections.IncludeTotals;
            pnlCover.Visible = po.Sections.IncludeCoverPage;

            IList<LogbookEntryDisplay> lstFlights = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntry.QueryCommand(mfbSearchForm1.Restriction, fAsc: true), CurrentUser.UserName, string.Empty, SortDirection.Ascending, CurrentUser.UsesHHMM, CurrentUser.UsesUTCDateOfFlight);
            PrintLayout pl = LogbookPrintedPage.LayoutLogbook(CurrentUser, lstFlights, ActiveTemplate, po, SuppressFooter);
            Master.PrintingCSS = pl.CSSPath.ToAbsoluteURL(Request).ToString();
        }

        protected void PrintOptions1_OptionsChanged(object sender, PrintingOptionsEventArgs e)
        {
            RefreshLogbookData();
        }

        protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
            mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);
            FilterResults(sender, new FlightQueryEventArgs(mfbSearchForm1.Restriction));
            lnkReturnToFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", HttpUtility.UrlEncode(mfbSearchForm1.Restriction.ToBase64CompressedJSONString()));
        }

        protected void lnkDownloadPDF_Click(object sender, EventArgs e)
        {
            Page.Validate();
            if (Page.IsValid)
            {
                SuppressFooter = true;
                OverrideRender = true;
            }
            RefreshLogbookData();
        }

        protected void IncludeParametersChanged(object sender, EventArgs e)
        {
            RefreshLogbookData();
        }

        protected void valCustWidth_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if ((PDFOptions.PageSize)Enum.Parse(typeof(PDFOptions.PageSize), cmbPageSize.SelectedValue) == PDFOptions.PageSize.Custom &&
                (decCustWidth.IntValue < 50 || decCustWidth.IntValue > 1000 || decCustHeight.IntValue < 50 || decCustHeight.IntValue > 1000))
                args.IsValid = false;
        }
    }
}