using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_PrintView : System.Web.UI.Page
{
    #region properties
    protected MyFlightbook.Profile CurrentUser { get; set; }

    protected IPrintingTemplate ActiveTemplate
    {
        get
        {
            foreach (Control c in mvLayouts.GetActiveView().Controls)
            {
                IPrintingTemplate pt = c as IPrintingTemplate;
                if (pt != null)
                    return pt;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the current page options as chosen by the user
    /// </summary>
    protected PDFOptions PageOptions
    {
        get
        {
            return new PDFOptions()
            {
                PaperSize = (PDFOptions.PageSize)Enum.Parse(typeof(PDFOptions.PageSize), cmbPageSize.SelectedValue),
                Orientation = rbLandscape.Checked ? PDFOptions.PageOrientation.Landscape : PDFOptions.PageOrientation.Portrait,
                FooterLeft = Resources.LogbookEntry.LogbookCertification,
                FooterRight = PDFOptions.FooterPageCountArg,
                LeftMargin = decLeftMargin.IntValue,
                RightMargin = decRightMargin.IntValue,
                TopMargin = decTopMargin.IntValue,
                BottomMargin = decBottomMargin.IntValue
            };
        }
    }

    protected bool OverrideRender { get; set; }

    protected bool SuppressFooter { get; set; }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        // wire up the logbook to the current user
        mfbSearchForm1.Username = Page.User.Identity.Name;

        Master.SelectedTab = tabID.lbtPrintView;

        CurrentUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);

        pnlResults.Visible = false; // since we aren't saving viewstate, bind to nothing  (e.g., in case we're editing a flight query)

        Master.SuppressTopNavPrint = true;
        string szPath = Request.Url.PathAndQuery.Substring(0, Request.Url.PathAndQuery.LastIndexOf("/", StringComparison.Ordinal) + 1);
        string szURL = String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host, szPath);
        Master.Page.Header.Controls.Add(new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<base href=\"{0}\" />", szURL)));

        if (!IsPostBack)
        {
            string szFQParam = util.GetStringParam(Request, "fq");
            if (!String.IsNullOrEmpty(szFQParam))
            {
                mfbSearchForm1.Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
                Master.HasFooter = Master.HasHeader = false;
                if (!mfbSearchForm1.Restriction.IsDefault)
                    TabContainer1.ActiveTab = tpFilter;
                lnkReturnToFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", szFQParam);
            }

            if (util.GetIntParam(Request, "pdfErr", 0) != 0)
                lblErr.Text = Resources.LocalizedText.PDFGenerationFailed;

            string szOptionsParam = util.GetStringParam(Request, "po");
            if (!String.IsNullOrEmpty(szOptionsParam))
                PrintOptions1.Options = JsonConvert.DeserializeObject<PrintingOptions>(Convert.FromBase64String(szOptionsParam).Uncompress());

            Master.Title = lblUserName.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
            mfbQueryDescriptor.DataSource = mfbTotalSummary1.CustomRestriction = mfbSearchForm1.Restriction;
            mfbQueryDescriptor.DataBind();
            mfbTotalSummary1.DataBind();

            // Set up endorsements
            mfbEndorsementList.Student = CurrentUser.UserName;
            int cEndorsements = mfbEndorsementList.RefreshEndorsements();
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, CurrentUser.UserName);
            il.Refresh(fIncludeDocs: false, fIncludeVids: false);
            cEndorsements += il.ImageArray.Count();
            rptImages.DataSource = il.ImageArray;
            rptImages.DataBind();
            ckEndorsements.Checked = ckIncludeEndorsementImages.Checked = pnlEndorsements.Visible = (cEndorsements > 0);

            RefreshLogbookData();
        }
    }

    protected void btnChangeQuery_Click(object sender, EventArgs e)
    {
        mvSearch.SetActiveView(vwSearchForm);
    }

    protected void mfbSearchForm1_Reset(object sender, FlightQueryEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
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
        MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        PrintingOptions printingOptions = PrintOptions1.Options;
        mvLayouts.ActiveViewIndex = (int) printingOptions.Layout;
        List<LogbookEntryDisplay> lstFlights = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntry.QueryCommand(mfbSearchForm1.Restriction, fAsc: true), pf.UserName, string.Empty, SortDirection.Ascending, pf.UsesHHMM, pf.UsesUTCDateOfFlight);

        IPrintingTemplate pt = ActiveTemplate;

        PrintLayout pl = PrintLayout.LayoutForType(printingOptions.Layout, CurrentUser);
        bool fCanIncludeImages = pl.SupportsImages;

        // Exclude both excluded properties and properties that have been moved to their own columns
        HashSet<int> lstPropsToExclude = new HashSet<int>(printingOptions.ExcludedPropertyIDs);
        HashSet<int> lstPropsInOwnColumns = new HashSet<int>();
        foreach (OptionalColumn oc in printingOptions.OptionalColumns)
        {
            if (oc.ColumnType == OptionalColumnType.CustomProp)
                lstPropsInOwnColumns.Add(oc.IDPropType);
        }
        string szPropSeparator = printingOptions.PropertySeparatorText;

        // set up properties per flight, and compute rough lineheight
        foreach (LogbookEntryDisplay led in lstFlights)
        {
            // Fix up properties according to the printing options
            List<CustomFlightProperty> lstProps = new List<CustomFlightProperty>(led.CustomProperties);

            // And fix up model as well.
            switch (printingOptions.DisplayMode) {
                case PrintingOptions.ModelDisplayMode.Full:
                    break;
                case PrintingOptions.ModelDisplayMode.Short:
                    led.ModelDisplay = led.ShortModelName;
                    break;
                case PrintingOptions.ModelDisplayMode.ICAO:
                    led.ModelDisplay = led.FamilyName;
                    break;
            }


            // Remove from the total property set all explicitly excluded properties...
            lstProps.RemoveAll(cfp => lstPropsToExclude.Contains(cfp.PropTypeID));

            // ...and then additionally exclude from the display any that's in its own column to avoid redundancy.
            lstProps.RemoveAll(cfp => lstPropsInOwnColumns.Contains(cfp.PropTypeID));
            led.CustPropertyDisplay = CustomFlightProperty.PropListDisplay(lstProps, pf.UsesHHMM, szPropSeparator);

            if (printingOptions.IncludeImages)
                led.PopulateImages(true);

            if (!printingOptions.IncludeSignatures)
                led.CFISignatureState = LogbookEntryBase.SignatureState.None;
            led.RowHeight = pl.RowHeight(led);
        }

        Master.PrintingCSS = pl.CSSPath.ToAbsoluteURL(Request).ToString();

        pt.BindPages(LogbookPrintedPage.Paginate(lstFlights, printingOptions), CurrentUser, printingOptions, !SuppressFooter);

        pnlResults.Visible = true;
    }

    protected void PrintOptions1_OptionsChanged(object sender, PrintingOptionsEventArgs e)
    {
        RefreshLogbookData();
    }

    protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm1.Restriction = mfbSearchForm1.Restriction.ClearRestriction(fic.FilterItem);
        FilterResults(sender, new FlightQueryEventArgs(mfbSearchForm1.Restriction));
        lnkReturnToFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx?fq={0}", HttpUtility.UrlEncode(mfbSearchForm1.Restriction.ToBase64CompressedJSONString()));
    }

    protected void lnkDownloadPDF_Click(object sender, EventArgs e)
    {
        SuppressFooter = true;
        OverrideRender = true;
        RefreshLogbookData();
    }

    protected override void Render(HtmlTextWriter writer)
    {
        if (OverrideRender)
        {
            StringBuilder sbOut = new StringBuilder();

            using (StringWriter swOut = new StringWriter(sbOut, CultureInfo.InvariantCulture))
            {
                UTF8Encoding enc = new UTF8Encoding(true);
                swOut.Write(Encoding.UTF8.GetString(enc.GetPreamble()));
                HtmlTextWriter htwOut = new HtmlTextWriter(swOut);
                base.Render(htwOut);
                string sOut = sbOut.ToString();

                string szTempPath = Path.GetTempPath();
                string szFileRoot = Guid.NewGuid().ToString();
                string szOutputHtm = szTempPath + szFileRoot + ".htm";
                string szOutputPDF = szTempPath + szFileRoot + ".pdf";

                File.WriteAllText(szOutputHtm, sbOut.ToString());

                const string szWkTextApp = "c:\\program files\\wkhtmltopdf\\bin\\wkhtmltopdf.exe";  // TODO: this needs to be in a config file
                string szArgs = PageOptions.WKHtmlToPDFArguments(szOutputHtm, szOutputPDF);

                Process p = null;
                using (p = new Process())
                {
                    try
                    {
                        p.StartInfo = new ProcessStartInfo(szWkTextApp, szArgs) { UseShellExecute = true }; // for some reason, wkhtmltopdf runs better in shell exec than without.

                        p.Start();

                        bool fResult = p.WaitForExit(120000);   // wait up to 2 minutes

                        if (!fResult || !File.Exists(szOutputPDF))
                        {
                                util.NotifyAdminEvent("Error saving PDF", String.Format(CultureInfo.CurrentCulture, "User: {0}\r\nOptions:\r\n{1}\r\n\r\nQuery:{2}\r\n",
                                    CurrentUser.UserName,
                                    JsonConvert.SerializeObject(PageOptions),
                                    JsonConvert.SerializeObject(PrintOptions1.Options)), ProfileRoles.maskSiteAdminOnly);

                                Response.Redirect(Request.Url.PathAndQuery + (Request.Url.PathAndQuery.Contains("?") ? "&" : "?") + "pdfErr=1");
                        }
                        else
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
                    }
                    catch (MyFlightbookException) { throw; }
                    finally
                    {
                        File.Delete(szOutputHtm);
                        File.Delete(szOutputPDF);

                        if (p != null && !p.HasExited)
                            p.Kill();
                    }
                }
            }
        }
        else
            base.Render(writer);
    }

    protected void IncludeParamtersChanged(object sender, EventArgs e)
    {
        mvLayouts.Visible = ckFlights.Checked;
        pnlEndorsements.Visible = ckEndorsements.Checked;
        if (!ckEndorsements.Checked)
            ckIncludeEndorsementImages.Checked = false;

        rptImages.Visible = ckIncludeEndorsementImages.Checked;
        pnlTotals.Visible = ckTotals.Checked;
        RefreshLogbookData();
    }
}
