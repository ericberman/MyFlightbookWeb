using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing
{
    /// <summary>
    /// Interface supported by any of the printing templates
    /// </summary>
    public interface IPrintingTemplate
    {
        /// <summary>
        /// Binds the template to the set of printed pages
        /// </summary>
        /// <param name="lst">The enumerable set of printed pages</param>
        /// <param name="user">The user for whom the pages are printed</param>
        /// <param name="showFooter">True to display the certification/page numbers in the footer.</param>
        /// <param name="options">The options used for printing</param>
        void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true);
    }

    public enum PrintLayoutType { Native, Portrait, EASA, USA, Canada, SACAA, CASA, NZ, Glider}

    #region Printing Layout implementations
    public abstract class PrintLayout
    {
        public MyFlightbook.Profile CurrentUser { get; set; }

        /// <summary>
        /// Measures the specified logbookentry and determines if it needs to take up more than one flight row.  
        /// </summary>
        /// <param name="le">The logbook entry to measure</param>
        /// <returns>The # of entries that this should span; default is 1.</returns>
        public abstract int RowHeight(LogbookEntryDisplay le);

        /// <summary>
        /// Does this template support images?
        /// </summary>
        public abstract bool SupportsImages { get; }

        public abstract bool SupportsOptionalColumns { get; }

        /// <summary>
        /// The path to the stylesheet to use for the particular layout
        /// </summary>
        public abstract string CSSPath { get; }

        /// <summary>
        /// Get the layout for the specified type
        /// </summary>
        /// <param name="plt">The type of layout</param>
        /// <returns>The layout object</returns>
        public static PrintLayout LayoutForType(PrintLayoutType plt, MyFlightbook.Profile pf = null)
        {
            switch (plt)
            {
                case PrintLayoutType.Native:
                    return new PrintLayoutNative() { CurrentUser = pf };
                case PrintLayoutType.Portrait:
                    return new PrintLayoutPortrait() { CurrentUser = pf };
                case PrintLayoutType.EASA:
                    return new PrintLayoutEASA() { CurrentUser = pf };
                case PrintLayoutType.USA:
                    return new PrintLayoutUSA() { CurrentUser = pf };
                case PrintLayoutType.Canada:
                    return new PrintLayoutCanada() { CurrentUser = pf };
                case PrintLayoutType.SACAA:
                    return new PrintLayoutSACAA() { CurrentUser = pf };
                case PrintLayoutType.NZ:
                    return new PrintLayoutNZ() { CurrentUser = pf };
                case PrintLayoutType.Glider:
                    return new PrintLayoutGlider() { CurrentUser = pf };
                case PrintLayoutType.CASA:
                    return new PrintLayoutCASA() { CurrentUser = pf };
                default:
                    throw new ArgumentOutOfRangeException(nameof(plt));
            }
        }
    }

    #region concrete layout classes
    public class PrintLayoutNative : PrintLayout
    {
        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            // Signature can be about 3 lines tall
            int sigHeight = le.CFISignatureState == LogbookEntry.SignatureState.None ? 0 : (le.HasDigitizedSig ? 2 : 1);
            int imgHeight = le.FlightImages != null && le.FlightImages.Count > 0 ? 3 : 0;

            // see how many rows of times we have - IF the user shows them
            int times = 0;

            if (CurrentUser != null && CurrentUser.DisplayTimesByDefault)
            {
                times = String.IsNullOrEmpty(le.EngineTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.FlightTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.HobbsDisplay) ? 0 : 1;

                // if there are 1 or 2 rows of times, add 1 to rowheight.  If 3, add 2.
                times = (times + 1) / 2;
            }

            return Math.Max(1 + imgHeight + sigHeight + times, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override string CSSPath { get { return "~/Public/CSS/printNative.css"; } }
    }

    public class PrintLayoutPortrait : PrintLayout
    {
        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            // Signature can be about 3 lines tall
            int sigHeight = le.CFISignatureState == LogbookEntry.SignatureState.None ? 0 : (le.HasDigitizedSig ? 2 : 1);
            int imgHeight = le.FlightImages != null && le.FlightImages.Count > 0 ? 3 : 0;

            // see how many rows of times we have - IF the user shows them
            int times = 0;

            if (CurrentUser != null && CurrentUser.DisplayTimesByDefault)
            {
                times = String.IsNullOrEmpty(le.EngineTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.FlightTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.HobbsDisplay) ? 0 : 1;

                // if there are 1 or 2 rows of times, add 1 to rowheight.  If 3, add 2.
                times = (times + 1) / 2;
            }

            return Math.Max(1 + imgHeight + sigHeight + times, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override string CSSPath { get { return "~/Public/CSS/printPortrait.css"; } }
    }

    public class PrintLayoutGlider : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override string CSSPath { get { return "~/Public/CSS/printGlider.css"; } }
    }

    public class PrintLayoutEASA : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override string CSSPath { get { return "~/Public/CSS/printEASA.css"; } }
    }

    public class PrintLayoutCASA : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override string CSSPath { get { return "~/Public/CSS/printCASA.css"; } }
    }

    public class PrintLayoutSACAA : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override string CSSPath { get { return "~/Public/CSS/printSACAA.css"; } }
    }

    public class PrintLayoutNZ : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override string CSSPath { get { return "~/Public/CSS/printNZ.css"; } }
    }

    public class PrintLayoutCanada : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override string CSSPath { get { return "~/Public/CSS/printCanada.css"; } }
    }

    public class PrintLayoutUSA : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~120chars/line
            int linesOfText = (int) Math.Ceiling(le.RedactedComment.Length / 120.0) + (int) Math.Ceiling(le.CustPropertyDisplay.Length / 120.0);
            int routeLine = le.Airports.Count() > 2 ? 1 : 0;
            return Math.Max(1, (linesOfText + routeLine + 1) / 2);
        }

        public override string CSSPath { get { return "~/Public/CSS/printUSA.css"; } }
    }
    #endregion

    #endregion

    /// <summary>
    /// Encapsulates a set of options for printing.
    /// </summary>
    [Serializable]
    public class PrintingOptions
    {

        public enum PropertySeparatorType { Space, Comma, Semicolon, Newline }

        public enum ModelDisplayMode { Full, Short, ICAO }

        #region properties
        /// <summary>
        /// Number of flights to print; less than or = 0 for continuous
        /// </summary>
        public int FlightsPerPage { get; set; } = 15;

        /// <summary>
        /// Include images when printing?
        /// </summary>
        public bool IncludeImages { get; set; } = false;

        /// <summary>
        /// Include signatures when printing?
        /// </summary>
        public bool IncludeSignatures { get; set; } = true;

        /// <summary>
        /// Start a new page when encountering a month boundary (non-continuous only)
        /// </summary>
        public bool BreakAtMonthBoundary { get; set; } = false;

        /// <summary>
        /// Layout to use
        /// </summary>
        public PrintLayoutType Layout { get; set; } = PrintLayoutType.Native;

        /// <summary>
        /// Properties to exclude from printing
        /// </summary>
        public Collection<int> ExcludedPropertyIDs { get; private set; } = new Collection<int>();

        /// <summary>
        /// How do we want to display model names?
        /// </summary>
        public ModelDisplayMode DisplayMode { get; set; } = ModelDisplayMode.Full;

        /// <summary>
        /// Character to use to separate properties in print layout
        /// </summary>
        public PropertySeparatorType PropertySeparator { get; set; } = PropertySeparatorType.Space;

        /// <summary>
        /// If true, pull forward totals from previous pages (conserves space on the page); just shows this page and running total.  True by defalt.
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludePullForwardTotals { get; set; } = true;

        /// <summary>
        /// Determines if a cover sheet should be shown.
        /// </summary>
        public bool IncludeCoverSheet { get; set; }

        /// <summary>
        /// If true, stripes subtotals by category/class
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool StripeSubtotalsByCategoryClass { get; set; } = true;

        /// <summary>
        /// Returns the text to use to separate properties (based on PropertySeparator)
        /// </summary>
        public string PropertySeparatorText
        {
            get
            {
                switch (PropertySeparator)
                {
                    default:
                    case PropertySeparatorType.Space:
                        return Resources.LocalizedText.LocalizedSpace;
                    case PropertySeparatorType.Semicolon:
                        return "; ";
                    case PropertySeparatorType.Comma:
                        return ", ";
                    case PropertySeparatorType.Newline:
                        return Environment.NewLine;
                }
            }
        }

        public Collection<OptionalColumn> OptionalColumns { get; private set; } = new Collection<OptionalColumn>();
        #endregion

        public PrintingOptions() { }
    }

    /// <summary>
    /// Describes options for saving to PDF
    /// </summary>
    [Serializable]
    public class PDFOptions
    {
        public enum PageOrientation { Landscape, Portrait };
        public enum PageSize { Letter, Legal, Tabloid, Executive, A1, A2, A3, A4, A5, B1, B2, B3, B4, B5, Custom};

        #region properties
        /// <summary>
        /// Orientation in which to print
        /// </summary>
        public PageOrientation Orientation { get; set; }

        /// <summary>
        /// Size of paper on which to print
        /// </summary>
        public PageSize PaperSize { get; set; }

        /// <summary>
        /// If PageSize is custom, height of page in mm
        /// </summary>
        public int PageHeight { get; set; }

        /// <summary>
        /// If PageWidth is custom, width of page in mm.
        /// </summary>
        public int PageWidth { get; set; }

        #region Optional margins
        public int? TopMargin { get; set; }
        public int? BottomMargin { get; set; }
        public int? LeftMargin { get; set; }
        public int? RightMargin { get; set; }
        #endregion

        /// <summary>
        /// Text to display in lower left corner of each page
        /// </summary>
        public string FooterLeft { get; set; }

        /// <summary>
        /// Text to display in lower right corner of each page
        /// </summary>
        public string FooterRight { get; set; }

        static public string FooterPageCountArg
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, "[page]" /* , "[topage]" */); }
        }
        #endregion

        public PDFOptions()
        {
            Orientation = PageOrientation.Landscape;
            PaperSize = PageSize.Letter;
            FooterLeft = FooterRight = string.Empty;
        }

        /// <summary>
        /// Generates the arguments string to pass to WKHtmlToPDF
        /// </summary>
        /// <param name="inputFile">The name of the source file (html)</param>
        /// <param name="outputFile">The name of the output PDF file</param>
        /// <returns>A string that can be passed as arguments, reflecting the specified options.</returns>
        public string WKHtmlToPDFArguments(string inputFile, string outputFile)
        {
            if (inputFile == null)
                throw new ArgumentNullException(nameof(inputFile));
            if (outputFile == null)
                throw new ArgumentNullException(nameof(outputFile));
            return String.Format(CultureInfo.InvariantCulture, "--orientation {0} --print-media-type --disable-javascript --footer-font-size 8 {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                Orientation.ToString(),
                PaperSize == PageSize.Custom ? String.Format(CultureInfo.InvariantCulture, "--page-height {0} --page-width {1}", PageHeight, PageWidth) : String.Format(CultureInfo.InvariantCulture, "-s {0}", PaperSize.ToString()),
                TopMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-top {0}", TopMargin.Value) : string.Empty,
                BottomMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-bottom {0}", BottomMargin.Value) : string.Empty,
                LeftMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-left {0}", LeftMargin.Value) : string.Empty,
                RightMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-right {0}", RightMargin.Value) : string.Empty,
                String.IsNullOrEmpty(FooterLeft) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-left \"{0}\"", FooterLeft),
                String.IsNullOrEmpty(FooterRight) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-right \"{0}\"", FooterRight),
                inputFile,
                outputFile);
        }
    }

    /// <summary>
    /// Renders the specified HTML to a PDF file on disk.
    /// </summary>
    public static class PDFRenderer
    {
        /// <summary>
        /// Renders the specified HTML to a PDF file on the disk.  FILE IS DELETED AT END OF THE CALL, SO DO WHAT YOU NEED IN THE ONSUCCESS CALL
        /// </summary>
        /// <param name="szHTML">The HTML to render</param>
        /// <param name="pdfOptions">The options controlling the rendering</param>
        /// <param name="onError">Action (no parameters) called if things fail.  Unfortunately, we can't report useful errors</param>
        /// <param name="onSuccess">Action called on success; single parameter is the filename of the PDF</param>
        static public void RenderFile(string szHTML, PDFOptions pdfOptions, Action onError, Action<string> onSuccess)
        {
            if (szHTML == null)
                throw new ArgumentNullException(nameof(szHTML));
            if (pdfOptions == null)
                throw new ArgumentNullException(nameof(pdfOptions));

            string szTempPath = Path.GetTempPath();
            string szFileRoot = Guid.NewGuid().ToString();
            string szOutputHtm = szTempPath + szFileRoot + ".htm";
            string szOutputPDF = szTempPath + szFileRoot + ".pdf";

            string szArgs = pdfOptions.WKHtmlToPDFArguments(szOutputHtm, szOutputPDF);

            File.WriteAllText(szOutputHtm, szHTML);

            const string szWkTextApp = "c:\\program files\\wkhtmltopdf\\bin\\wkhtmltopdf.exe";  // TODO: this needs to be in a config file

            using (Process p = new Process())
            {
                try
                {
                    p.StartInfo = new ProcessStartInfo(szWkTextApp, szArgs) { UseShellExecute = true }; // for some reason, wkhtmltopdf runs better in shell exec than without.

                    p.Start();

                    bool fResult = p.WaitForExit(120000);   // wait up to 2 minutes

                    if (!fResult || !File.Exists(szOutputPDF))
                        onError?.Invoke();
                    else
                        onSuccess?.Invoke(szOutputPDF);
                }
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

    public class PrintingOptionsEventArgs : EventArgs
    {
        public PrintingOptions Options { get; set; }

        public PrintingOptionsEventArgs(PrintingOptions options) : base()
        {
            Options = options;
        }

        public PrintingOptionsEventArgs() : base()
        {
            Options = new PrintingOptions();
        }
    }

    /// <summary>
    /// Represents a collection of subtotals
    /// </summary>
    public class LogbookPrintedPageSubtotalsCollection
    {
        private readonly List<LogbookEntryDisplay> m_list;

        #region properties
        /// <summary>
        /// Title to display for the group
        /// </summary>
        public string GroupTitle { get; set; }

        /// <summary>
        /// Group Type - mostly for sorting
        /// </summary>
        public LogbookEntryDisplay.LogbookRowType GroupType { get; set; }

        /// <summary>
        /// Sorted list of subtotals for the group
        /// </summary>
        public IEnumerable<LogbookEntryDisplay> Subtotals { get { return m_list; } }

        /// <summary>
        /// Number of subtotals for this group
        /// </summary>
        public int SubtotalCount { get { return m_list.Count; } }
        #endregion

        #region Constructors
        public LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType groupType, string title, IEnumerable<LogbookEntryDisplay> lst)
        {
            GroupType = groupType;
            GroupTitle = title;
            m_list = new List<LogbookEntryDisplay>(lst);
            m_list.Sort((le1, le2) => { return le1.EffectiveCatClass.CompareTo(le2.EffectiveCatClass); });
        }
        #endregion
    }

    /// <summary>
    /// Represents a set of flights and subtotals for a printed page
    /// </summary>
    public class LogbookPrintedPage
    {
        #region properties
        /// <summary>
        /// The flights for the page
        /// </summary>
        public IEnumerable<LogbookEntryDisplay> Flights { get; set; }

        /// <summary>
        /// Page number for this printed page
        /// </summary>
        public int PageNum { get; set; }

        /// <summary>
        /// Total # of pages to print
        /// </summary>
        public int TotalPages { get; set; }

        public IEnumerable<LogbookPrintedPageSubtotalsCollection> Subtotals
        {
            get
            {
                List<LogbookPrintedPageSubtotalsCollection> lst = new List<LogbookPrintedPageSubtotalsCollection>();
                if (TotalsThisPage.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.PageTotal, Resources.LogbookEntry.PrintTotalsThisPage, SortedSubtotals(TotalsThisPage)));
                if (TotalsPreviousPages.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.PreviousTotal, Resources.LogbookEntry.PrintTotalsPreviousPage, SortedSubtotals(TotalsPreviousPages)));
                if (RunningTotals.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.RunningTotal, Resources.LogbookEntry.PrintTotalsRunning, SortedSubtotals(RunningTotals)));

                return lst;
            }
        }

        #region internal properties
        /// <summary>
        /// Totals from this page, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> TotalsThisPage { get; private set; }

        /// <summary>
        /// Totals from previous pages, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> TotalsPreviousPages { get; private set; }

        /// <summary>
        /// Running totals, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> RunningTotals { get; private set; }
        #endregion
        #endregion

        #region Constructors
        public LogbookPrintedPage()
        {
            Flights = new List<LogbookEntryDisplay>();
            TotalsThisPage = new Dictionary<string, LogbookEntryDisplay>();
            TotalsPreviousPages = new Dictionary<string, LogbookEntryDisplay>();
            RunningTotals = new Dictionary<string, LogbookEntryDisplay>();
        }
        #endregion

        #region Subtotals collections
        private static List<LogbookEntryDisplay> SortedSubtotals(IDictionary<string, LogbookEntryDisplay> d)
        {
            List<LogbookEntryDisplay> lstResult = new List<LogbookEntryDisplay>();
            foreach (string key in d.Keys)
                lstResult.Add(d[key]);
            lstResult.Sort((led1, led2) => { return led1.EffectiveCatClass.CompareTo(led2.EffectiveCatClass); });
            return lstResult;
        }
        #endregion

        /// <summary>
        /// Inserts subtotals into an enumerable set of flights, returning an enumerable set of LogbookPrintedPages.
        /// </summary>
        /// <param name="lstIn">The input set of flights.  Should be ALL RowType=Flight and should have rowheight property set</param>
        /// <param name="po">Options that guide pagination</param>
        /// <returns>A new enumerable with per-page subtotals and (optional) running totals</returns>
        public static IEnumerable<LogbookPrintedPage> Paginate(IEnumerable<LogbookEntryDisplay> lstIn, PrintingOptions po)
        {
            if (lstIn == null)
                throw new ArgumentNullException(nameof(lstIn));
            if (po == null)
                throw new ArgumentNullException(nameof(po));
            int cIn = lstIn.Count();
            if (cIn == 0)
                return Array.Empty<LogbookPrintedPage>();

            // For speed, cache the names of each category/class
            Dictionary<int, string> dictCatClasses = new Dictionary<int, string>();
            foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                dictCatClasses.Add(cc.IDCatClassAsInt, cc.CatClass);

            List<LogbookPrintedPage> lstOut = new List<LogbookPrintedPage>();

            Dictionary<string, LogbookEntryDisplay> dictPageTotals = null, dictPreviousTotals, dictRunningTotals = new Dictionary<string, LogbookEntryDisplay>();
            List<LogbookEntryDisplay> lstFlightsThisPage = null;
            LogbookPrintedPage currentPage = null;

            int flightIndexOnPage = 0;
            int index = 0;
            int pageNum = 0;

            DateTime? dtLastEntry = null;

            foreach (LogbookEntryDisplay led in lstIn)
            {
                // force a page break if a new month is starting IF the option to do so has been set
                if (po.FlightsPerPage > 0 && po.BreakAtMonthBoundary && dtLastEntry != null && dtLastEntry.HasValue && (led.Date.Month != dtLastEntry.Value.Month || led.Date.Year != dtLastEntry.Value.Year))
                    flightIndexOnPage = po.FlightsPerPage;

                dtLastEntry = led.Date;
                led.SetOptionalColumns(po.OptionalColumns);
                if ((po.FlightsPerPage > 0 && flightIndexOnPage >= po.FlightsPerPage) || currentPage == null)   // need to start a new page.
                {
                    flightIndexOnPage = 0;  // reset
                    dictPageTotals = new Dictionary<string, LogbookEntryDisplay>();
                    // COPY the running totals to the new previous totals, since AddFrom modifies the object, 
                    dictPreviousTotals = new Dictionary<string, LogbookEntryDisplay>();
                    Dictionary<string, LogbookEntryDisplay> dictNewRunningTotals = new Dictionary<string, LogbookEntryDisplay>();
                    foreach (string szKeySrc in dictRunningTotals.Keys)
                    {
                        LogbookEntryDisplay ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.RowType = LogbookEntryDisplay.LogbookRowType.PreviousTotal;
                        dictPreviousTotals[szKeySrc] = ledRunningNew;
                        ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.RowType = LogbookEntryDisplay.LogbookRowType.RunningTotal;
                        dictNewRunningTotals[szKeySrc] = ledRunningNew;
                    }
                    dictRunningTotals = dictNewRunningTotals;  // set up for the new page to pick up where the last one left off...
                    lstFlightsThisPage = new List<LogbookEntryDisplay>();
                    currentPage = new LogbookPrintedPage() { RunningTotals = dictRunningTotals, TotalsPreviousPages = dictPreviousTotals, TotalsThisPage = dictPageTotals, Flights = lstFlightsThisPage, PageNum = ++pageNum };

                    lstOut.Add(currentPage);
                }

                flightIndexOnPage += led.RowHeight;

                string szCatClassKey = dictCatClasses[led.EffectiveCatClass];   // should never not be present!!

                led.Index = ++index;

                // Add the flight to the page
                lstFlightsThisPage.Add(led);

                // And add the flight to the page catclass totals and running catclass totals
                if (po.FlightsPerPage > 0)
                {
                    if (!dictPageTotals.ContainsKey(szCatClassKey))
                        dictPageTotals[szCatClassKey] = new LogbookEntryDisplay(po.OptionalColumns) { RowType = LogbookEntryDisplay.LogbookRowType.PageTotal, CatClassDisplay = szCatClassKey  };
                    dictPageTotals[szCatClassKey].AddFrom(led);
                }
                if (!dictRunningTotals.ContainsKey(szCatClassKey))
                    dictRunningTotals[szCatClassKey] = new LogbookEntryDisplay(po.OptionalColumns) { RowType = LogbookEntryDisplay.LogbookRowType.RunningTotal, CatClassDisplay = szCatClassKey };
                dictRunningTotals[szCatClassKey].AddFrom(led);
            }

            // Assign page number, and index totals
            foreach (LogbookPrintedPage lpp in lstOut)
            {
                // And add unstriped totals as needed
                ConsolidateTotals(lpp.TotalsThisPage, LogbookEntryDisplay.LogbookRowType.PageTotal, po.OptionalColumns);
                ConsolidateTotals(lpp.TotalsPreviousPages, LogbookEntryDisplay.LogbookRowType.PreviousTotal, po.OptionalColumns);
                ConsolidateTotals(lpp.RunningTotals, LogbookEntryDisplay.LogbookRowType.RunningTotal, po.OptionalColumns);

                lpp.TotalPages = pageNum;
                int iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.TotalsThisPage.Values)
                    lep.Index = iTotal++;
                iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.TotalsPreviousPages.Values)
                    lep.Index = iTotal++;
                iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.RunningTotals.Values)
                    lep.Index = iTotal++;

                if (!po.StripeSubtotalsByCategoryClass)
                {
                    RemoveStripedSubtotals(lpp.TotalsThisPage);
                    RemoveStripedSubtotals(lpp.TotalsPreviousPages);
                    RemoveStripedSubtotals(lpp.RunningTotals);
                }

                if (!po.IncludePullForwardTotals)
                    lpp.TotalsPreviousPages.Clear();
            }

            return lstOut;
        }

        private static void ConsolidateTotals(IDictionary<string, LogbookEntryDisplay> d, LogbookEntryDisplay.LogbookRowType rowType, Collection<OptionalColumn> optionalColumns)
        {
            if (d == null || d.Count <= 1)
                return;

            LogbookEntryDisplay ledAll = new LogbookEntryDisplay(optionalColumns) { RowType = rowType };
            foreach (LogbookEntryDisplay led in d.Values)
                ledAll.AddFrom(led);
            ledAll.CatClassDisplay = Resources.LogbookEntry.PrintTotalsAllCatClass;
            d[Resources.LogbookEntry.PrintTotalsAllCatClass] = ledAll;
        }

        /// <summary>
        /// If we have a subtotal group that is striped (e.g., "ASEL", "AMEL", "[All]"), removes everything but the "[All]" group
        /// </summary>
        /// <param name="d"></param>
        private static void RemoveStripedSubtotals(IDictionary<string, LogbookEntryDisplay> d)
        {
            if (d == null || d.Count <= 1 || !d.ContainsKey(Resources.LogbookEntry.PrintTotalsAllCatClass))
                return;

            LogbookEntryDisplay led = d[Resources.LogbookEntry.PrintTotalsAllCatClass];
            d.Clear();
            d[Resources.LogbookEntry.PrintTotalsAllCatClass] = led;
        }

        public static PrintLayout LayoutLogbook(Profile pf, IList<LogbookEntryDisplay> lstFlights, IPrintingTemplate pt, PrintingOptions printingOptions, bool fSuppressFooter)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            if (pt == null)
                throw new ArgumentNullException(nameof(pt));
            if (printingOptions == null)
                throw new ArgumentNullException(nameof(printingOptions));
            if (lstFlights == null)
                throw new ArgumentNullException(nameof(lstFlights));

            PrintLayout pl = PrintLayout.LayoutForType(printingOptions.Layout, pf);

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
                switch (printingOptions.DisplayMode)
                {
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

            pt.BindPages(LogbookPrintedPage.Paginate(lstFlights, printingOptions), pf, printingOptions, !fSuppressFooter);

            return pl;
        }
    }
}