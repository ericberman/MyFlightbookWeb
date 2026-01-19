using MyFlightbook.Image;
using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

/******************************************************
 * 
 * Copyright (c) 2024-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrintController : FlightControllerBase
    {
        #region Web Services
        /// <summary>
        /// 
        /// </summary>
        /// <param name="szExistingHRef"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult PrintLink(string szExistingHRef, PrintingSections ps)
        {
            return SafeOp(() =>
            {
                return Json((szExistingHRef == null || ps == null) ? szExistingHRef ?? string.Empty : PrintingOptions.UpdatedPermaLink(szExistingHRef, new PrintingOptions() { Sections = ps }).ToString());
            });
        }

        #endregion

        #region Child Views
        /// <summary>
        /// Generates the print options subsection for which sections to include
        /// </summary>
        /// <param name="fq">The active query</param>
        /// <param name="po">Any existing print options; if null, a default set will be used</param>
        /// <param name="paramList">Additional parameters to include in a link (e.g., user name)</param>
        /// <param name="includeFlightsSection">Whether or not to offer flights as a toggle option</param>
        /// <param name="onChange">Name of a javascript function to call on any change.  The function receives one argument, which contains an object containing the selected sections.  If null or empty, then a local link to a printview will be provided and updated.</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult PrintPrefsSections(FlightQuery fq, PrintingOptions po, bool includeFlightsSection, NameValueCollection paramList = null, string onChange = null)
        {
            ViewBag.paramList = paramList ?? HttpUtility.ParseQueryString(string.Empty);
            ViewBag.query = fq;
            ViewBag.includeFlightsSection = includeFlightsSection;
            ViewBag.onChange = onChange;
            ViewBag.po = po ?? new PrintingOptions() { Sections = new PrintingSections() { Endorsements = PrintingSections.EndorsementsLevel.DigitalAndPhotos, IncludeCoverPage = true, IncludeFlights = true, IncludeTotals = true } };
            return PartialView("_printPrefsSections");
        }

        [ChildActionOnly]
        public ActionResult PrintPrefsOptions(string userName, PrintingOptions po, PrintLayout pl, string onChange)
        {
            ViewBag.po = po;
            ViewBag.userName = userName;
            ViewBag.onChange = onChange;
            ViewBag.pl = pl;
            return PartialView("_printPrefsOptions");
        }

        [ChildActionOnly]
        public ActionResult PageFooter(Profile pf, int pageNum, int totalPages, bool showFoot, string layoutNotesPartial = null)
        {
            ViewBag.pf = pf;
            ViewBag.pageNum = pageNum;
            ViewBag.totalPages = totalPages;
            ViewBag.layoutNotesPartial = layoutNotesPartial;
            ViewBag.showFoot = showFoot;
            return PartialView("_printFooter");
        }

        [ChildActionOnly]
        public ActionResult FlightPages(PrintLayout pl, Profile pf, FlightQuery fq, PrintingOptions po, bool suppressFooter)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            if (po == null)
                throw new ArgumentNullException(nameof(po));
            if (pl == null)
                throw new ArgumentNullException(nameof(pl));
            FlightResultManager frm = FlightResultManager.FlightResultManagerForUser(pf.UserName);
            FlightResult fr = frm.ResultsForQuery(fq);
            IList<LogbookEntryDisplay> lstFlights = fr.FlightsInRange(fr.GetResultRange(0, FlightRangeType.First, "Date", SortDirection.Ascending)) as IList<LogbookEntryDisplay>;
            ViewBag.pl = LogbookPrintedPage.LayoutLogbook(pf, lstFlights, pl, po, suppressFooter);

            if (pl.IsCondensed)
                frm.Invalidate();   // issue #1314 - LayoutLogbook condensed data, which modified the source enumerable; invalidate all results, then, so that nobody else picks up the modified data (e.g., if we switch layout, or go back to a regular view, or modify other print options.)

            return PartialView(po.Layout.ToString());
        }
        #endregion

        #region Helper utilities
        private PrintingSections SectionsFromForm()
        {
            return new PrintingSections()
            {
                IncludeCoverPage = Request["poSectCover"] != null,
                IncludeFlights = Request["poSectFlights"] != null,
                IncludeTotals = Request["poSectTotals"] != null,
                IncludeDOB = Request["poSectDOB"] != null,
                IncludeAddress = Request["poSectAddress"] != null,
                IncludeHeadshot = Request["poHeadshot"] != null,
                CompactTotals = Request["poSectTotalsCompact"] != null,
                Endorsements = Request["poSectEndorsements"] != null ? (Request["poSectEndorsementsJPG"] != null ? PrintingSections.EndorsementsLevel.DigitalAndPhotos : PrintingSections.EndorsementsLevel.DigitalOnly) : PrintingSections.EndorsementsLevel.None
            };
        }

        private PrintingOptions OptionsFromForm(PDFOptions pdfOptions, PrintingSections ps)
        {
            return new PrintingOptions()
            {
                PDFSettings = pdfOptions,
                Sections = ps,
                Layout = Enum.TryParse(Request["poLayout"], out PrintLayoutType plt) ? plt : PrintLayoutType.Native,
                IncludeImages = Request["poImages"] != null,
                IncludeSignatures = Request["poSigs"] != null,
                UseFlightColoring = Request["poColors"] != null,
                Size = Enum.TryParse(Request["poFontSize"], true, out PrintingOptions.FontSize fontSize) ? fontSize : PrintingOptions.FontSize.Normal,
                FlightsPerPage = int.TryParse(Request["poFlightsPerPage"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fpp) ? fpp : 0,
                IncludeColumnTotals = Request["poIncludeColumn"] != null,
                IncludePullForwardTotals = Request["poIncludePreviousPage"] != null,
                SubtotalStripeRules = Enum.TryParse(Request["poSubtotalsRules"], true, out PrintingOptions.SubtotalStripe stripeRules) ? stripeRules : PrintingOptions.SubtotalStripe.CatClass,
                BreakAtMonthBoundary = Request["poForcedBreak"].CompareCurrentCultureIgnoreCase("Month") == 0,
                BreakAtYearBoundary = Request["poForcedBreak"].CompareCurrentCultureIgnoreCase("Year") == 0,
                DisplayMode = Enum.TryParse(Request["poModelDisplay"], true, out PrintingOptions.ModelDisplayMode displayMode) ? displayMode : PrintingOptions.ModelDisplayMode.Full,
                PropertySeparator = Enum.TryParse(Request["poPropSeparator"], true, out PrintingOptions.PropertySeparatorType pst) ? pst : PrintingOptions.PropertySeparatorType.Space,
                FlightStartDate = DateTime.TryParse(Request["poPrintFrom"] ?? string.Empty, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtFlightStart) ? (DateTime?)dtFlightStart : null,
                StartingPageNumberOffset = Math.Max((int.TryParse(Request["poPrintFromPage"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int startPage) ? startPage : 1) - 1, 0)
            };
        }

        private PDFOptions PDFOptionsFromForm()
        {
            const int defMargin = 10;
            return new PDFOptions()
            {
                PaperSize = Enum.TryParse(Request["pdfPageSize"], true, out PDFOptions.PageSize pagesize) ? pagesize : PDFOptions.PageSize.Letter,
                BottomMargin = int.TryParse(Request["pdfMarginBottom"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bottomMargin) ? bottomMargin : defMargin,
                LeftMargin = int.TryParse(Request["pdfMarginLeft"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int leftMargin) ? leftMargin : defMargin,
                RightMargin = int.TryParse(Request["pdfMarginRight"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rightMargin) ? rightMargin : defMargin,
                TopMargin = int.TryParse(Request["pdfMarginTop"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int topMargin) ? topMargin : defMargin,
                Orientation = Enum.TryParse(Request["pdfOrientation"], true, out PDFOptions.PageOrientation orientation) ? orientation : PDFOptions.PageOrientation.Landscape,
                PageHeight = int.TryParse(Request["pdfCustHeight"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int custHeight) ? custHeight : 0,
                PageWidth = int.TryParse(Request["pdfCustWidth"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int custWidth) ? custWidth : 0,
                IncludeTotalPageCount = Request["pdfIncludePageCount"] != null
            };
        }

        private PrintingOptions PrintingOptionsFromForm()
        {
            PrintingSections ps = SectionsFromForm();

            PDFOptions pdfOptions = PDFOptionsFromForm();

            PrintingOptions po = OptionsFromForm(pdfOptions, ps);

            po.OptionalColumns.Clear();
            foreach (string szColumn in (Request["poOptColumn"] ?? string.Empty).SplitCommas())
            {
                // Enum.TryParse works with integers as well as symbolic names, so try it first as an integer (i.e., custom property type)
                if (int.TryParse(szColumn, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idPropType))
                    po.OptionalColumns.Add(new OptionalColumn(idPropType));
                else if (Enum.TryParse<OptionalColumnType>(szColumn, out OptionalColumnType oct))
                    po.OptionalColumns.Add(new OptionalColumn(oct));
            }

            // get the requested page breaks
            po.PageBreaks = new HashSet<int>((Request["pageBreaks"] ?? string.Empty).ToInts());

            po.ExcludedPropertyIDs.Clear();
            foreach (int propID in (Request["poExcludedProp"] ?? string.Empty).ToInts())
                po.ExcludedPropertyIDs.Add(propID);

            return po;
        }
        #endregion

        [Authorize]
        [HttpPost]
        public ActionResult Refresh(bool fPropDeleteClicked = false, string propToDelete = null)
        {
            string student = Request["u"];
            string user = User.Identity.Name;
            if (!string.IsNullOrEmpty(student))
            {
                CheckCanViewFlights(student, User.Identity.Name);
                user = student;
            }
            string szJsonFq = Request["fqJSON"];
            FlightQuery fq = String.IsNullOrEmpty(szJsonFq) ? new FlightQuery(user) : FlightQuery.FromJSON(szJsonFq);
            if (fq.UserName.CompareOrdinal(user) != 0)
                throw new UnauthorizedAccessException();

            if (fPropDeleteClicked)
                fq.ClearRestriction(propToDelete ?? string.Empty);

            PrintingOptions po = PrintingOptionsFromForm();

            RouteValueDictionary routeVals = new RouteValueDictionary
            {
                ["po"] = JsonConvert.SerializeObject(po, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }).ToSafeParameter()
            };
            if (!fq.IsDefault)
            routeVals["fq"] = fq.ToBase64CompressedJSONString();
            if (user.CompareCurrentCultureIgnoreCase(User.Identity.Name) != 0)
                routeVals["u"] = user;
            if ((Request["refreshPrintView"] ?? string.Empty).CompareCurrentCultureIgnoreCase("pdf") == 0)
                routeVals["pdf"] = true;
            if (int.TryParse(Request["t"] ?? string.Empty, NumberStyles.None, CultureInfo.InvariantCulture, out int t))
                routeVals["tabIndex"] = t;

            // Digital signing should not be preserved in the URL - put it in session
            Session["rbSigningType"] = Request["rbSigningType"] ?? string.Empty;
            string base64Data = Request["hdnSigData"];
            Session["attestationSignature"] = String.IsNullOrEmpty(base64Data) ? null : ScribbleImage.FromDataLinkURL(base64Data);

            return RedirectToAction("Index", routeVals);
        }

        private void RenderPDF(PrintingOptions printingOptions, string viewName, string szUser)
        {
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            bool fDigitalAttestation = ((string) Session["rbSigningType"] ?? string.Empty).CompareCurrentCultureIgnoreCase("digital") == 0;
            byte[] sigData = (byte[]) Session["attestationSignature"] ?? Array.Empty<byte>();
            
            Session["rbSigningType"] = null;
            Session["attestationSignature"] = null;
                        
            PDFFooterOptions footerOptions = new PDFFooterOptions()
            {
                fCover = printingOptions.Sections.IncludeCoverPage,
                fTotalPages = printingOptions.PDFSettings.IncludeTotalPageCount,
                fTrackChanges = pf.PreferenceExists(MFBConstants.keyTrackOriginal),
                fDigitalSignature = fDigitalAttestation,
                StartPageOffset = printingOptions.StartingPageNumberOffset,
                UserName = String.IsNullOrEmpty(pf.FirstName + pf.LastName) ? string.Empty : String.Format(CultureInfo.CurrentCulture, "[{0}]", pf.UserFullName)
            };
            printingOptions.PDFSettings.FooterUri = VirtualPathUtility.ToAbsolute("~/mvc/Internal/PrintFooter/" + footerOptions.ToEncodedPathSegment()).ToAbsoluteURL(Request);
            string szFileName = $"{RegexUtility.UnSafeFileChars.Replace($"{MyFlightbook.Profile.GetUser(szUser).UserFullName} {Resources.LocalizedText.PrintViewCoverSheetNameTemplate}", "-")}.pdf";

            // Dictionary to map file names to human friendly names
            Dictionary<string, string> dictFiles = new Dictionary<string, string>();
            string szFileToDownload = string.Empty;

            try
            {
                PDFRenderer.RenderFile(
                printingOptions.PDFSettings,
                (htwOut) =>
                {
                    IView view = ViewEngines.Engines.FindView(ControllerContext, viewName, null).View;
                    ControllerContext.Controller.ViewData.Model = null;
                    var viewContext = new ViewContext(ControllerContext, view, ControllerContext.Controller.ViewData, ControllerContext.Controller.TempData, htwOut);
                    view.Render(viewContext, htwOut);
                },
                (szErr) => // onError
                {
                    util.NotifyAdminEvent("Error saving PDF", $"{szErr}\r\nUser: {szUser}", ProfileRoles.maskSiteAdminOnly);
                },
                (szOutputPDF) => // onSuccess
                {
                    szFileToDownload = szOutputPDF;
                    dictFiles[szOutputPDF] = szFileName;

                    if (fDigitalAttestation)
                    {
                        // Create a digital attestation page: include the hash of the PDF we just created, and do it portrait with no footer
                        // Compute the hash
                        string szHash = PDFRenderer.ComputeSHAForFile(szOutputPDF);
                        printingOptions.PDFSettings.FooterUri = null; // no footer on attestation page
                        printingOptions.PDFSettings.Orientation = PDFOptions.PageOrientation.Portrait;

                        // Now create the attestation page
                        PDFRenderer.RenderFile(printingOptions.PDFSettings,
                                    (htwOut) =>
                                    {
                                        IView view = ViewEngines.Engines.FindView(ControllerContext, "_printAttest", null).View;
                                        // Populate ViewBag (ViewBag is just ViewData)
                                        var viewData = ControllerContext.Controller.ViewData;
                                        viewData["pf"] = pf;
                                        viewData["scribble"] = sigData;
                                        viewData["fileName"] = szFileName;
                                        viewData["hashValue"] = szHash;

                                        var viewContext = new ViewContext(ControllerContext, view, viewData, ControllerContext.Controller.TempData, htwOut);
                                        view.Render(viewContext, htwOut);
                                    },
                                    (szErr) => // onError
                                    {
                                        util.NotifyAdminEvent("Error saving PDF", $"{szErr}\r\nUser: {szUser}", ProfileRoles.maskSiteAdminOnly);
                                    },
                                    (szAttestationPDF) => // onSuccess
                                    {
                                        dictFiles[szAttestationPDF] = szFileName.Replace(".pdf", $"{Resources.LocalizedText.AttestationFileNameSuffix}.pdf"); // Add the attestation page
                                        szFileToDownload = PDFRenderer.CreateZipForFiles(dictFiles);  // and zip it up to return that.

                                        // Now change the output file name to indicate it's a zip and add the zip file to the dictionary for cleanup
                                        szFileName = szFileName.Replace(".pdf", ".zip");
                                        dictFiles[szFileToDownload] = szFileName;
                                    });
                    }
                });

                // hack, ideally we want to return a File actionresult, but the file will be deleted by the time we get there.
                bool fIsPDF = szFileToDownload.EndsWith(".pdf");
                Response.Clear();
                Response.ContentType = fIsPDF ? "application/pdf" : "application/zip";
                Response.AddHeader("content-disposition", $"attachment;filename=\"{szFileName}\"");
                Response.WriteFile(szFileToDownload);
                Response.Flush();
                Response.End();
            }
            catch (HttpUnhandledException) { }  // sometimes the remote host has closed the connection - allow cleanup to proceed.
            catch (HttpException) { }
            finally
            {
                foreach (string szFile in dictFiles.Keys)
                {
                    if (System.IO.File.Exists(szFile))
                        System.IO.File.Delete(szFile);
                }
            }
        }

        // GET: mvc/Print
        [Authorize]
        [HttpGet]
        public ActionResult Index(string u = null, string po = null, string fq = null, bool pdf = false, int tabIndex = 1)
        {
            string szUser = User.Identity.Name;    // default value
            PrintingOptions printingOptions = string.IsNullOrEmpty(po) ? new PrintingOptions() : JsonConvert.DeserializeObject<PrintingOptions>(po.FromSafeParameter());
            FlightQuery query = String.IsNullOrEmpty(fq) ? null : FlightQuery.FromBase64CompressedJSON(fq);
            try
            {
                if (!String.IsNullOrEmpty(u))
                {
                    CheckCanViewFlights(u, User.Identity.Name);
                    // if we are here, then it's legit.
                    szUser = u;
                }
                if (query == null)
                    query = new FlightQuery(szUser);
                else if (szUser.CompareCurrentCultureIgnoreCase(query.UserName) != 0)
                    throw new UnauthorizedAccessException();
            }
            catch (UnauthorizedAccessException e)
            {
                ViewBag.error = e.Message;
                query = new FlightQuery(User.Identity.Name);
                printingOptions.Sections.IncludeCoverPage = printingOptions.Sections.IncludeFlights = printingOptions.Sections.IncludeTotals = false;
                printingOptions.Sections.Endorsements = PrintingSections.EndorsementsLevel.None;
            }

            query.Refresh();
            ViewBag.fq = query;
            ViewBag.po = printingOptions;
            ViewBag.pdfOptions = new PDFOptions();
            ViewBag.suppressFooter = pdf;
            ViewBag.prefTab = tabIndex;
            ViewBag.isPDF = pdf;

            const string viewName = "__printView";

            if (pdf)
            {
                RenderPDF(printingOptions, viewName, szUser);
                return new EmptyResult();
            }

            return View(viewName);
        }
    }
}