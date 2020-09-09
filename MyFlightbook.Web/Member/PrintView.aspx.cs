using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_PrintViewBase : Page
{
    protected Profile CurrentUser { get; set; }

    protected bool OverrideRender { get; set; }

    protected bool SuppressFooter { get; set; }

    protected PDFOptions PageOptions { get; set; }

    protected PrintingOptions PrintOptions { get; set; }

    protected override void Render(HtmlTextWriter writer)
    {
        if (OverrideRender)
        {
            StringBuilder sbOut = new StringBuilder();

            using (StringWriter swOut = new StringWriter(sbOut, CultureInfo.InvariantCulture))
            {
                using (HtmlTextWriter htwOut = new HtmlTextWriter(swOut))
                {
                    base.Render(htwOut);
                }
            }

            PDFRenderer.RenderFile(sbOut.ToString(),
                PageOptions,
                () => // onError
                {
                    util.NotifyAdminEvent("Error saving PDF", String.Format(CultureInfo.CurrentCulture, "User: {0}\r\nOptions:\r\n{1}\r\n\r\nQuery:{2}\r\n",
                        CurrentUser.UserName,
                        JsonConvert.SerializeObject(PageOptions),
                        JsonConvert.SerializeObject(PrintOptions)), ProfileRoles.maskSiteAdminOnly);

                    Response.Redirect(Request.Url.PathAndQuery + (Request.Url.PathAndQuery.Contains("?") ? "&" : "?") + "pdfErr=1");
                },
                (szOutputPDF) => // onSuccess
                {
                    Page.Response.Clear();
                    Page.Response.ContentType = "application/pdf";
                    Response.AddHeader("content-disposition", String.Format(CultureInfo.CurrentCulture, @"attachment;filename=""{0}.pdf""", CurrentUser.UserFullName));
                    Response.WriteFile(szOutputPDF);
                    Page.Response.Flush();

                    // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                    Page.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
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

public partial class Member_PrintView : Member_PrintViewBase
{
    #region properties
    protected IPrintingTemplate ActiveTemplate
    {
        get
        {
            foreach (Control c in mvLayouts.GetActiveView().Controls)
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
        if (util.GetIntParam(Request, "pdfErr", 0) != 0)
            lblErr.Text = Resources.LocalizedText.PDFGenerationFailed;
    }

    private void InitializePrintOptions()
    {
        string szOptionsParam = util.GetStringParam(Request, "po");
        if (!String.IsNullOrEmpty(szOptionsParam))
            PrintOptions1.Options = JsonConvert.DeserializeObject<PrintingOptions>(Convert.FromBase64String(szOptionsParam).Uncompress());
    }

    private void InitializeTitleAndQueryDescriptor()
    {
        Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, CurrentUser.UserFullName);
        mfbQueryDescriptor.DataSource = mfbTotalSummary1.CustomRestriction = mfbSearchForm1.Restriction;
        mfbQueryDescriptor.DataBind();
        mfbTotalSummary1.DataBind();
    }

    private void InitializeEndorsements()
    {
        // Set up endorsements
        mfbEndorsementList.Student = CurrentUser.UserName;
        int cEndorsements = mfbEndorsementList.RefreshEndorsements();
        ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, CurrentUser.UserName);
        il.Refresh(fIncludeDocs: false, fIncludeVids: false);
        cEndorsements += il.ImageArray.Count;
        rptImages.DataSource = il.ImageArray;
        rptImages.DataBind();
        ckEndorsements.Checked = ckIncludeEndorsementImages.Checked = pnlEndorsements.Visible = (cEndorsements > 0);
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
        lnkReturnToFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", HttpUtility.UrlEncode(mfbSearchForm1.Restriction.ToBase64CompressedJSONString()));
    }

    protected void RefreshLogbookData()
    {
        mvLayouts.ActiveViewIndex = (int)PrintOptions1.Options.Layout;
        pnlResults.Visible = true;
        lblCoverDate.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintViewCoverSheetDateTemplate, DateTime.Now);

        IList<LogbookEntryDisplay> lstFlights = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntry.QueryCommand(mfbSearchForm1.Restriction, fAsc: true), CurrentUser.UserName, string.Empty, SortDirection.Ascending, CurrentUser.UsesHHMM, CurrentUser.UsesUTCDateOfFlight);
        PrintLayout pl = LogbookPrintedPage.LayoutLogbook(CurrentUser, lstFlights, ActiveTemplate, PrintOptions1.Options, SuppressFooter);
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
            PageOptions = new PDFOptions()
            {
                PaperSize = (PDFOptions.PageSize)Enum.Parse(typeof(PDFOptions.PageSize), cmbPageSize.SelectedValue),
                PageHeight = decCustHeight.IntValue,
                PageWidth = decCustWidth.IntValue,
                Orientation = rbLandscape.Checked ? PDFOptions.PageOrientation.Landscape : PDFOptions.PageOrientation.Portrait,
                FooterUri = (VirtualPathUtility.ToAbsolute("~/Public/PrintFooter.aspx") + (ckIncludeCoverSheet.Checked ? "/Cover" : string.Empty)).ToAbsoluteURL(Request),
                // FooterLeft = Resources.LogbookEntry.LogbookCertification,
                // FooterRight = PDFOptions.FooterPageCountArg,
                LeftMargin = decLeftMargin.IntValue,
                RightMargin = decRightMargin.IntValue,
                TopMargin = decTopMargin.IntValue,
                BottomMargin = decBottomMargin.IntValue
            };
            PrintOptions = PrintOptions1.Options;
        }
        RefreshLogbookData();
    }

    protected void IncludeParametersChanged(object sender, EventArgs e)
    {
        mvLayouts.Visible = ckFlights.Checked;
        pnlEndorsements.Visible = ckEndorsements.Checked;
        if (!ckEndorsements.Checked)
            ckIncludeEndorsementImages.Checked = false;

        rptImages.Visible = ckIncludeEndorsementImages.Checked;
        pnlTotals.Visible = ckTotals.Checked;
        pnlCover.Visible = ckIncludeCoverSheet.Checked;
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
