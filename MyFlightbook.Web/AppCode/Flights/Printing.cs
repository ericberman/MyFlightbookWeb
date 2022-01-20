using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2022 MyFlightbook LLC
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

    public interface ICondenseFlights
    {
        IList<LogbookEntryDisplay> CondenseFlights(IEnumerable<LogbookEntryDisplay> lstIn);
    }

    public enum PrintLayoutType { Native, Portrait, EASA, USA, Canada, SACAA, CASA, NZ, Glider, Condensed, PCAA, UASCivi }

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
        /// Perform any necessary initialization that is specific to this layout.
        /// </summary>
        /// <param name="po">The requested printing options</param>
        public virtual void Init(PrintingOptions po) {  }

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
                case PrintLayoutType.Condensed:
                    return new PrintLayoutCondensed() { CurrentUser = pf };
                case PrintLayoutType.PCAA:
                    return new PrintLayoutPCAA() { CurrentUser = pf };
                case PrintLayoutType.UASCivi:
                    return new PrintLayoutUASCivi() { CurrentUser = pf };
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

        public override string CSSPath { get { return "~/Public/CSS/printNative.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printPortrait.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printGlider.css?v=3"; } }
    }

    public class PrintLayoutEASA : PrintLayout
    {
        public override void Init(PrintingOptions po)
        {
            if (po == null)
                throw new ArgumentNullException(nameof(po));

            // Issue #810 - show multi-pilot time IF the user has ever used multi-pilot time.
            // Even though we don't support optional columns for EASA layout, we use an optional column for this.
            IEnumerable<CustomPropertyType> rgcpt = CustomPropertyType.GetCustomPropertyTypes(CurrentUser.UserName);
            foreach (CustomPropertyType cpt in rgcpt)
                if (cpt.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropMultiPilotTime && cpt.IsFavorite)
                {
                    po.OptionalColumns.Add(new OptionalColumn(cpt.PropTypeID));
                    break;
                }
        }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.RedactedComment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override string CSSPath { get { return "~/Public/CSS/printEASA.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printCASA.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printSACAA.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printNZ.css?v=3"; } }
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

        public override string CSSPath { get { return "~/Public/CSS/printCanada.css?v=3"; } }
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
            int linesOfText = (int)Math.Ceiling(le.RedactedComment.Length / 120.0) + (int)Math.Ceiling(le.CustPropertyDisplay.Length / 120.0);
            int routeLine = le.Airports.Count() > 2 ? 1 : 0;
            return Math.Max(1, (linesOfText + routeLine + 1) / 2);
        }

        public override string CSSPath { get { return "~/Public/CSS/printUSA.css?v=3"; } }
    }

    public class PrintLayoutCondensed : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le) { return 1; }

        public override string CSSPath { get { return "~/Public/CSS/printCondensed.css?v=3"; } }
    }

    public class PrintLayoutPCAA : PrintLayout
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

        public override string CSSPath { get { return "~/Public/CSS/printPCAA.css?v=3"; } }
    }

    public class PrintLayoutUASCivi : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            // Very rough computation: look at customproperties + comments, shoot for ~120chars/line
            int linesOfText = (int)Math.Ceiling(le.RedactedComment.Length / 120.0) + (int)Math.Ceiling(le.CustPropertyDisplay.Length / 120.0);
            int routeLine = le.Airports.Count() > 2 ? 1 : 0;
            return Math.Max(1, (linesOfText + routeLine + 1) / 2);
        }

        public override string CSSPath { get { return "~/Public/CSS/printUAS-civi.css?v=2"; } }
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

        public enum FontSize { Small, Normal, Large }

        #region properties
        /// <summary>
        /// Number of flights to print; less than or = 0 for continuous
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
        public int FlightsPerPage { get; set; } = 0;

        /// <summary>
        /// Include images when printing?
        /// </summary>
        public bool IncludeImages { get; set; }

        /// <summary>
        /// Include signatures when printing?
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeSignatures { get; set; } = true;

        /// <summary>
        /// Start a new page when encountering a month boundary (non-continuous only)
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool BreakAtMonthBoundary { get; set; }

        /// <summary>
        /// Start a new page when encountering a year boundary (non-continuous only)
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool BreakAtYearBoundary { get; set; }

        /// <summary>
        /// Use flight coloring in the print view?
        /// </summary>
        public bool UseFlightColoring { get; set; }

        /// <summary>
        /// Layout to use
        /// </summary>
        [System.ComponentModel.DefaultValue(PrintLayoutType.Native)]
        public PrintLayoutType Layout { get; set; } = PrintLayoutType.Native;

        /// <summary>
        /// Properties to exclude from printing
        /// </summary>
        public Collection<int> ExcludedPropertyIDs { get; private set; } = new Collection<int>();

        /// <summary>
        /// How do we want to display model names?
        /// </summary>
        [System.ComponentModel.DefaultValue(ModelDisplayMode.Full)]
        public ModelDisplayMode DisplayMode { get; set; } = ModelDisplayMode.Full;

        /// <summary>
        /// Character to use to separate properties in print layout
        /// </summary>
        [System.ComponentModel.DefaultValue(PropertySeparatorType.Space)] 
        public PropertySeparatorType PropertySeparator { get; set; } = PropertySeparatorType.Space;

        /// <summary>
        /// If true, pull forward totals from previous pages (conserves space on the page); just shows this page and running total.  True by defalt.
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludePullForwardTotals { get; set; } = true;

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

        /// <summary>
        /// Default font size
        /// </summary>
        [System.ComponentModel.DefaultValue(FontSize.Normal)]
        public FontSize Size { get; set; } = FontSize.Normal;

        /// <summary>
        /// Which sections to print.
        /// </summary>
        public PrintingSections Sections { get; set; } = new PrintingSections();

        /// <summary>
        /// For PDF download, what settings to offer.
        /// </summary>
        public PDFOptions PDFSettings { get; set; } = new PDFOptions();
        #endregion

        public PrintingOptions() { }

        #region Permalinks
        /// <summary>
        /// Generates a permanent link to a print view with the specified query and print options.
        /// </summary>
        /// <param name="fq">Flight Query (search)</param>
        /// <param name="po">Print options</param>
        /// <param name="hostName">Host.  Uses current brand if not specified</param>
        /// <param name="scheme">Scheme.  Assumes https if not specified</param>
        /// <param name="addParams">lambda to allow addition of more parameters, if desired.</param>
        /// <returns>Uri to the permalink.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        static public Uri PermaLink(FlightQuery fq, PrintingOptions po, string hostName = null, string scheme = null, Action<NameValueCollection> addParams = null)
        {
            if (fq == null)
                throw new ArgumentNullException(nameof(fq));
            if (po == null)
                throw new ArgumentNullException(nameof(po));
            if (String.IsNullOrEmpty(hostName))
                hostName = Branding.CurrentBrand.HostName;
            if (String.IsNullOrEmpty(scheme))
                scheme = "https";


            UriBuilder uriBuilder = new UriBuilder("~/Member/PrintView.aspx".ToAbsoluteURL(scheme, hostName));
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["po"] = Convert.ToBase64String(JsonConvert.SerializeObject(po, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }).Compress());
            nvc["fq"] = fq.ToBase64CompressedJSONString();
            addParams?.Invoke(nvc);
            uriBuilder.Query = nvc.ToString();
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Parses the specified HREF permalink and updates it for the specified print options.  Calls PermaLink
        /// </summary>
        /// <param name="szHRef">An existing HREF (presumably generated by calling PermaLink)</param>
        /// <param name="po">Desired print options</param>
        /// <returns>Uri to the permalink</returns>
        static public Uri UpdatedPermaLink(string szHRef, PrintingOptions po)
        {
            if (szHRef == null)
                throw new ArgumentNullException(nameof(szHRef));

            UriBuilder ub = new UriBuilder(new Uri(szHRef));
            NameValueCollection nvc = HttpUtility.ParseQueryString(ub.Query);
            FlightQuery fq = (nvc["fq"] == null) ? new FlightQuery() : FlightQuery.FromBase64CompressedJSON(nvc["fq"]);
            return PermaLink(fq, po, ub.Host, ub.Scheme, (nvc2) =>
            {
                // add any additional parameters that may have been added.
                foreach (string szKey in nvc.Keys)
                {
                    if (nvc2[szKey] == null)
                        nvc2[szKey] = nvc[szKey];
                }
            });
        }
        #endregion
    }

    /// <summary>
    /// Class to encapsulate what sections to include in the printout
    /// </summary>
    [Serializable]
    public class PrintingSections
    {
        public enum EndorsementsLevel { None, DigitalOnly, DigitalAndPhotos }

        #region Properties
        /// <summary>
        /// True to include a cover page
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeCoverPage { get; set; } = true;

        /// <summary>
        /// True to include totals
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeTotals { get; set; } = true;

        /// <summary>
        /// True to include flights
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool IncludeFlights { get; set; } = true;

        /// <summary>
        /// Whether to include digital images.
        /// </summary>
        [System.ComponentModel.DefaultValue(EndorsementsLevel.DigitalAndPhotos)]
        public EndorsementsLevel Endorsements { get; set; } = EndorsementsLevel.DigitalAndPhotos;
        #endregion

        public PrintingSections() { }
    }

    /// <summary>
    /// Describes options for saving to PDF
    /// </summary>
    [Serializable]
    public class PDFOptions
    {
        public enum PageOrientation { Landscape, Portrait };
        public enum PageSize { Letter, Legal, Tabloid, Executive, A1, A2, A3, A4, A5, B1, B2, B3, B4, B5, Custom };

        #region properties
        /// <summary>
        /// Orientation in which to print
        /// </summary>
        [System.ComponentModel.DefaultValue(PageOrientation.Landscape)]
        public PageOrientation Orientation { get; set; } = PageOrientation.Landscape;

        /// <summary>
        /// Size of paper on which to print
        /// </summary>
        [System.ComponentModel.DefaultValue(PageSize.Letter)]
        public PageSize PaperSize { get; set; } = PageSize.Letter;

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
        [System.ComponentModel.DefaultValue("")]
        public string FooterLeft { get; set; } = string.Empty;

        /// <summary>
        /// Text to display in lower right corner of each page
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string FooterRight { get; set; } = string.Empty;

        /// <summary>
        /// Uri for footer.  OVERRIDES FOOTER LEFT / FOOTER RIGHT
        /// </summary>
        [JsonIgnore]
        public Uri FooterUri { get; set; }

        static public string FooterPageCountArg
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, "[page]" /* , "[topage]" */); }
        }
        #endregion

        #region Options encoded to pass to the footer
        private const UInt32 flagCoverPage = 0x00000001;
        private const UInt32 flagTotalPages = 0x00000002;

        public static string PathEncodeOptions(bool fCover, bool fTotal)
        {
            UInt32 flags = ((fCover ? 0xffffffff : 0) & flagCoverPage) | ((fTotal ? 0xffffffff : 0) & flagTotalPages);
            return flags.ToString(CultureInfo.InvariantCulture);
        }

        public static bool CoverFromEncodedOptions(string sz)
        {
            if (sz == null)
                throw new ArgumentNullException(nameof(sz));

            if (UInt32.TryParse(sz, NumberStyles.Integer, CultureInfo.InvariantCulture, out UInt32 flags))
                return (flags & flagCoverPage) != 0;
            else
                return false;
        }

        public static bool TotalPagesFromEncodedOptions(string sz)
        {
            if (sz == null)
                throw new ArgumentNullException(nameof(sz));

            if (UInt32.TryParse(sz, NumberStyles.Integer, CultureInfo.InvariantCulture, out UInt32 flags))
                return (flags & flagTotalPages) != 0;
            else
                return false;
        }

        #endregion

        public PDFOptions() { }

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
            return String.Format(CultureInfo.InvariantCulture, "--orientation {0} --print-media-type --disable-javascript --enable-local-file-access --quiet --footer-font-size 8 {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                Orientation.ToString(),
                PaperSize == PageSize.Custom ? String.Format(CultureInfo.InvariantCulture, "--page-height {0} --page-width {1}", PageHeight, PageWidth) : String.Format(CultureInfo.InvariantCulture, "-s {0}", PaperSize.ToString()),
                TopMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-top {0}", TopMargin.Value) : string.Empty,
                BottomMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-bottom {0}", BottomMargin.Value) : string.Empty,
                LeftMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-left {0}", LeftMargin.Value) : string.Empty,
                RightMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-right {0}", RightMargin.Value) : string.Empty,
                String.IsNullOrEmpty(FooterLeft) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-left \"{0}\"", FooterLeft),
                String.IsNullOrEmpty(FooterRight) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-right \"{0}\"", FooterRight),
                FooterUri == null ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-html {0}", FooterUri.ToString()),
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
        /// <param name="pdfOptions">The options controlling the rendering</param>
        /// <param name="render">Action to render the html to a writer</param>
        /// <param name="onError">Action (no parameters) called if things fail.  Unfortunately, we can't report useful errors</param>
        /// <param name="onSuccess">Action called on success; single parameter is the filename of the PDF</param>
        static public void RenderFile(PDFOptions pdfOptions, Action<HtmlTextWriter> render, Action<string> onError, Action<string> onSuccess)
        {
            if (pdfOptions == null)
                throw new ArgumentNullException(nameof(pdfOptions));
            if (render == null)
                throw new ArgumentNullException(nameof(render));

            string szTempPath = Path.GetTempPath();
            string szFileRoot = Guid.NewGuid().ToString();
            string szOutputHtm = szTempPath + szFileRoot + ".htm";
            string szOutputPDF = szTempPath + szFileRoot + ".pdf";

            string szArgs = pdfOptions.WKHtmlToPDFArguments(szOutputHtm, szOutputPDF);

            const string szWkTextApp = "c:\\program files\\wkhtmltopdf\\bin\\wkhtmltopdf.exe";  // TODO: this needs to be in a config file

            using (Process p = new Process())
            {
                try
                {
                    // Render the html directly into our temporary file.
                    using (StreamWriter sw = File.CreateText(szOutputHtm))
                    {
                        using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                        {
                            render(htw);
                        }
                    }

                    p.StartInfo = new ProcessStartInfo(szWkTextApp, szArgs) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false, RedirectStandardError = true };

                    p.Start();

                    string szErr = p.StandardError.ReadToEnd(); // read BEFORE waiting for exit to avoid deadlock - see https://www.generacodice.com/en/articolo/39973/ProcessStartInfo-hanging-on-%E2%80%9CWaitForExit%E2%80%9D-Whya=r

                    bool fResult = p.WaitForExit(120000);   // wait up to 2 minutes

                    if (!fResult || !File.Exists(szOutputPDF))
                        onError?.Invoke(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PDFGenerationFailed, szErr));
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

            GC.Collect();   // We could have used a bunch of memory.
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

        private static void CheckPaginateArgs(IEnumerable<LogbookEntryDisplay> lstIn, PrintingOptions po)
        {
            if (lstIn == null)
                throw new ArgumentNullException(nameof(lstIn));
            if (po == null)
                throw new ArgumentNullException(nameof(po));
        }

        /// <summary>
        /// Inserts subtotals into an enumerable set of flights, returning an enumerable set of LogbookPrintedPages.
        /// </summary>
        /// <param name="lstIn">The input set of flights.  Should be ALL RowType=Flight and should have rowheight property set</param>
        /// <param name="po">Options that guide pagination</param>
        /// <returns>A new enumerable with per-page subtotals and (optional) running totals</returns>
        public static IEnumerable<LogbookPrintedPage> Paginate(IEnumerable<LogbookEntryDisplay> lstIn, PrintingOptions po)
        {
            CheckPaginateArgs(lstIn, po);   // will throw an exception

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
                if (dtLastEntry != null && dtLastEntry.HasValue &&
                    ((po.BreakAtMonthBoundary &&  (led.Date.Month != dtLastEntry.Value.Month || led.Date.Year != dtLastEntry.Value.Year)) ||
                    (po.BreakAtYearBoundary && led.Date.Year != dtLastEntry.Value.Year)))
                    flightIndexOnPage = po.FlightsPerPage;

                dtLastEntry = led.Date;
                led.SetOptionalColumns(po.OptionalColumns);
                if (((po.FlightsPerPage > 0 && flightIndexOnPage >= po.FlightsPerPage) || flightIndexOnPage < 0) || currentPage == null)   // need to start a new page.
                {
                    flightIndexOnPage = 0;  // reset
                    dictPageTotals = new Dictionary<string, LogbookEntryDisplay>();
                    // COPY the running totals to the new previous totals, since AddFrom modifies the object, 
                    dictPreviousTotals = new Dictionary<string, LogbookEntryDisplay>();
                    Dictionary<string, LogbookEntryDisplay> dictNewRunningTotals = new Dictionary<string, LogbookEntryDisplay>();
                    foreach (string szKeySrc in dictRunningTotals.Keys)
                    {
                        LogbookEntryDisplay ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.InitializeCatClassTotalsFrom(dictRunningTotals[szKeySrc]);    // the catclass totals are NOT exposed (not serialized), so initialize them.
                        ledRunningNew.RowType = LogbookEntryDisplay.LogbookRowType.PreviousTotal;
                        dictPreviousTotals[szKeySrc] = ledRunningNew;
                        ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.InitializeCatClassTotalsFrom(dictRunningTotals[szKeySrc]);
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
                if (po.FlightsPerPage > 0 || po.BreakAtMonthBoundary || po.BreakAtYearBoundary)
                {
                    if (!dictPageTotals.ContainsKey(szCatClassKey))
                        dictPageTotals[szCatClassKey] = new LogbookEntryDisplay(po.OptionalColumns) { RowType = LogbookEntryDisplay.LogbookRowType.PageTotal, CatClassDisplay = szCatClassKey };
                    dictPageTotals[szCatClassKey].AddFrom(led);
                }
                if (!dictRunningTotals.ContainsKey(szCatClassKey))
                    dictRunningTotals[szCatClassKey] = new LogbookEntryDisplay(po.OptionalColumns) { RowType = LogbookEntryDisplay.LogbookRowType.RunningTotal, CatClassDisplay = szCatClassKey };
                dictRunningTotals[szCatClassKey].AddFrom(led);
            }

            // Assign page number, and index totals
            AddPageNumbersIndexTotals(po, lstOut, pageNum);

            return lstOut;
        }

        private static void AddPageNumbersIndexTotals(PrintingOptions po, IEnumerable<LogbookPrintedPage> lstOut, int pageNum)
        {
            if (po == null)
                throw new ArgumentNullException(nameof(po));
            if (lstOut == null)
                throw new ArgumentNullException(nameof(lstOut));

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
            pl.Init(printingOptions);  // do any layout-specific initialization.

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

            // Condense the flights, if the template supports condensing
            if (pt is ICondenseFlights cf)
                lstFlights = cf.CondenseFlights(lstFlights);

            pt.BindPages(LogbookPrintedPage.Paginate(lstFlights, printingOptions), pf, printingOptions, !fSuppressFooter);

            return pl;
        }
    }

    /// <summary>
    /// Base class for actual print layouts
    /// </summary>
    public class PrintLayoutBase : UserControl, IPrintingTemplate
    {
        #region Properties
        public MyFlightbook.Profile CurrentUser { get; private set; }

        protected PrintingOptions Options { get; private set; }

        protected bool ShowFooter { get; private set; }

        public bool IncludeImages { get; private set; }

        protected IEnumerable<CannedQuery> QueriesToColor { get; private set; }

        protected Collection<OptionalColumn> OptionalColumns { get; private set; }

        protected string PropSeparator { get; private set; }

        #endregion

        #region common utilities.
        protected Boolean ShowOptionalColumn(int index)
        {
            return OptionalColumns != null && index >= 0 && index < OptionalColumns.Count;
        }

        protected string OptionalColumnName(int index)
        {
            return ShowOptionalColumn(index) ? OptionalColumns[index].Title : string.Empty;
        }

        protected string OtherCatClassValue(LogbookEntryDisplay led)
        {
            return (led != null && led.EffectiveCatClass != (int)CategoryClass.CatClassID.ASEL && led.EffectiveCatClass != (int)CategoryClass.CatClassID.AMEL && OptionalColumn.ShowOtherCatClass(OptionalColumns, (CategoryClass.CatClassID)led.EffectiveCatClass)) ?
                String.Format(CultureInfo.CurrentCulture, "{0}: {1}", led.CategoryClassNoType, led.TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM)) :
                string.Empty;
        }

        protected static string FormatTakeoffs(int i)
        {
            return (i == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, "{0}T", i);
        }

        protected static string FormatLandings(int i)
        {
            return (i == 0) ? string.Empty : String.Format(CultureInfo.CurrentCulture, "{0}L", i);
        }

        /// <summary>
        /// Removes any properties from the property display of flights that are redundant with the rest of the print layout (as provided in rgProps)
        /// </summary>
        /// <param name="rgProps">The set of properties to be excluded</param>
        /// <param name="rgle">The set of flights to modify</param>
        protected void StripRedundantOrExcludedProperties(IEnumerable<int> rgProps, IEnumerable<LogbookEntryDisplay> rgle)
        {
            if (rgle == null)
                return;

            HashSet<int> hsRedundantProps = rgProps == null ? new HashSet<int>() : new HashSet<int>(rgProps);
            hsRedundantProps.UnionWith(Options.ExcludedPropertyIDs);
            foreach (LogbookEntryDisplay led in rgle)
            {
                List<CustomFlightProperty> lstProps = new List<CustomFlightProperty>(led.CustomProperties);
                lstProps.RemoveAll(cfp => hsRedundantProps.Contains(cfp.PropTypeID) || (led.IsFSTD && cfp.PropTypeID == (int) CustomPropertyType.KnownProperties.IDPropSimRegistration));
                led.CustPropertyDisplay = CustomFlightProperty.PropListDisplay(lstProps, CurrentUser.UsesHHMM, PropSeparator);
            }
        }
        #endregion

        #region Common utilities
        /// <summary>
        /// Return the direct-style flight coloring for a logbookentrydisplay
        /// </summary>
        /// <returns></returns>
        protected string ColorForFlight(object o)
        {
            if (o == null || !Options.UseFlightColoring || (!(o is LogbookEntryDisplay led)))
                return string.Empty;

            if (QueriesToColor == null)
                QueriesToColor = FlightColor.QueriesToColor(led.User);

            System.Drawing.Color c = System.Drawing.Color.Empty;
            foreach (CannedQuery cq in QueriesToColor)
            {
                if (cq.MatchesFlight(led))
                {
                    c = FlightColor.TryParseColor(cq.ColorString);
                    break;
                }
            }
            return c == System.Drawing.Color.Empty ? string.Empty : String.Format(CultureInfo.InvariantCulture, "style=\"background-color: {0};\" ", System.Drawing.ColorTranslator.ToHtml(c));
        }
        #endregion

        #region IPrintingTemplate
        public virtual void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, PrintingOptions options, bool showFooter = true)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            ShowFooter = showFooter;
            CurrentUser = user;
            OptionalColumns = options.OptionalColumns;
            IncludeImages = options.IncludeImages;
            Options = options;
            PropSeparator = options.PropertySeparatorText;

        }
        #endregion
    }
}