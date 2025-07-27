using MyFlightbook.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrintController : FlightControllerBase
    {
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

            return RedirectToAction("Index", routeVals);
        }

        private void RenderPDF(PrintingOptions printingOptions, string viewName, string szUser)
        {
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            PDFFooterOptions footerOptions = new PDFFooterOptions()
            {
                fCover = printingOptions.Sections.IncludeCoverPage,
                fTotalPages = printingOptions.PDFSettings.IncludeTotalPageCount,
                fTrackChanges = pf.PreferenceExists(MFBConstants.keyTrackOriginal),
                StartPageOffset = printingOptions.StartingPageNumberOffset,
                UserName = String.IsNullOrEmpty(pf.FirstName + pf.LastName) ? string.Empty : String.Format(CultureInfo.CurrentCulture, "[{0}]", pf.UserFullName)
            };
            printingOptions.PDFSettings.FooterUri = VirtualPathUtility.ToAbsolute("~/mvc/Internal/PrintFooter/" + footerOptions.ToEncodedPathSegment()).ToAbsoluteURL(Request);

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
                    util.NotifyAdminEvent("Error saving PDF", String.Format(CultureInfo.CurrentCulture, "{0}\r\nUser: {1}\r\nOptions:\r\n{2}\r\n",
                        szErr, szUser, JsonConvert.SerializeObject(printingOptions)), ProfileRoles.maskSiteAdminOnly);

                    // put the error into the session
                    ViewBag.error = szErr;
                },
                (szOutputPDF) => // onSuccess
                {
                    try
                    {
                        // hack, ideally we want to return a File actionresult, but the file will be deleted by the time we get there.
                        Response.Clear();
                        Response.ContentType = "application/pdf";
                        Response.AddHeader("content-disposition", String.Format(CultureInfo.CurrentCulture, @"attachment;filename=""{0}.pdf""", MyFlightbook.Profile.GetUser(szUser).UserFullName));
                        Response.WriteFile(szOutputPDF);
                        Response.Flush();

                        // See http://stackoverflow.com/questions/20988445/how-to-avoid-response-end-thread-was-being-aborted-exception-during-the-exce for the reason for the next two lines.
                        Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                        System.Web.HttpContext.Current.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
                    }
                    catch (HttpUnhandledException) { }  // sometimes the remote host has closed the connection - allow cleanup to proceed.
                    catch (HttpException) { }
                });
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
                RenderPDF(printingOptions, viewName, szUser);

            return View(viewName);
        }
    }
}