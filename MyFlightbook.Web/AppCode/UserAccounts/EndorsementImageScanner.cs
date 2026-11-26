using MyFlightbook.Image;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    /// <summary>
    /// One endorsement (or document) that the vision model found in an image.  Field names match the tool schema.
    /// </summary>
    public class ScannedEndorsementItem
    {
        [JsonProperty("category")]
        public string CategoryName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("regulation")]
        public string Regulation { get; set; }

        [JsonProperty("date")]
        public string DateRaw { get; set; }

        [JsonProperty("studentName")]
        public string StudentName { get; set; }

        [JsonProperty("instructorName")]
        public string InstructorName { get; set; }

        [JsonProperty("instructorCertificate")]
        public string InstructorCertificate { get; set; }

        [JsonProperty("instructorCertificateExpiration")]
        public string InstructorExpirationRaw { get; set; }

        [JsonProperty("pageNumber")]
        public int? PageNumber { get; set; }

        [JsonProperty("rawText")]
        public string RawText { get; set; }

        [JsonProperty("displayText")]
        public string DisplayText { get; set; }

        [JsonProperty("confidence")]
        public double? Confidence { get; set; }

        #region Normalized accessors
        public EndorsementCategory Category
        {
            get
            {
                return (!String.IsNullOrEmpty(CategoryName) && Enum.TryParse(CategoryName, true, out EndorsementCategory ec) && ec != EndorsementCategory.None && Enum.IsDefined(typeof(EndorsementCategory), ec)) ? ec : EndorsementCategory.Other;
            }
        }

        public DateTime? Date
        {
            get
            {
                DateTime? dt = DateRaw.ParseYMD();
                // Reject obviously misread dates (future, or before powered flight was regulated)
                return (dt.HasValue && dt.Value.Year >= 1926 && dt.Value.Date <= DateTime.Now.Date) ? dt : null;
            }
        }

        public DateTime? InstructorExpiration
        {
            get { return InstructorExpirationRaw.ParseYMD(); }
        }

        /// <summary>
        /// Confidence, 0-100
        /// </summary>
        public int ConfidencePercent
        {
            get { return (int)Math.Round(Math.Max(0.0, Math.Min(1.0, Confidence ?? 0.0)) * 100.0); }
        }
        #endregion
    }

    /// <summary>
    /// The full result from scanning one image.
    /// </summary>
    public class EndorsementScanResult
    {
        [JsonProperty("items")]
        public List<ScannedEndorsementItem> Items { get; set; } = new List<ScannedEndorsementItem>();

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }

    /// <summary>
    /// Sends an endorsement image or PDF to the Anthropic Messages API with a forced tool call, so the response is always an EndorsementScanResult.
    /// Same configuration as FlightDeckScan (localconfig): AnthropicApiKey (required); AnthropicEndorsementModel (optional), falling back
    /// to AnthropicScanModel, falling back to a default.
    /// </summary>
    public static class EndorsementImageScanner
    {
        private const string DefaultModel = "claude-sonnet-4-5-20250929";
        private const string AnthropicVersion = "2023-06-01";
        private const string MessagesEndpoint = "https://api.anthropic.com/v1/messages";
        private const string ToolName = "record_endorsements";

        /// <summary>
        /// Max # of PDF pages to send.
        /// </summary>
        public const int MaxPDFPages = 2;

        /// <summary>
        /// If we can't trim a PDF (e.g., it's encrypted), we'll send the whole thing only if it's smaller than this.
        /// </summary>
        private const int MaxUntrimmedPDFBytes = 3 * 1024 * 1024;

        private const int MaxItemsPerImage = 20;

        /// <summary>
        /// How long to wait for the model.  Note that SharedHttpClient's own default timeout (100 seconds) still applies as an upper bound.
        /// </summary>
        private static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(95);

        private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

        private static string ConfigValue(string szKey)
        {
            try
            {
                return LocalConfig.SettingForKey(szKey);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private static string ModelName
        {
            get
            {
                string sz = ConfigValue("AnthropicEndorsementModel");
                if (String.IsNullOrWhiteSpace(sz))
                    sz = ConfigValue("AnthropicScanModel");
                return String.IsNullOrWhiteSpace(sz) ? DefaultModel : sz;
            }
        }

        private static string ApiKey
        {
            get
            {
                string sz = ConfigValue("AnthropicApiKey");
                return String.IsNullOrWhiteSpace(sz) ? throw new MyFlightbookException("Image reading isn't configured (missing AnthropicApiKey in localconfig).") : sz;
            }
        }

        public static bool IsConfigured
        {
            get { return !String.IsNullOrWhiteSpace(ConfigValue("AnthropicApiKey")); }
        }

        #region Prompt and Schema
        private static string CategoryGuide
        {
            get
            {
                // Name = guidance.  Mirrors the comments on EndorsementCategory; keep in sync if categories are added.
                return @"PresoloKnowledge: 61.87(b) presolo aeronautical knowledge test
PresoloFlight: 61.87(c) presolo flight training
Solo: 61.87(n)/(o)/(p) solo (incl. night, additional 90 days), SFAR 73 2(b)(3)/(4), ""cleared for solo""
SoloLocalAndAirspace: 61.93(b)(1), 61.94, 61.95 (Class B / airport in Class B), 61.101(b)/(d), solo beyond 25nm
SoloCrossCountry: 61.93(c), 61.93(b)(2), 61.101(c) solo cross-country
RecreationalPrivileges: 61.101(g)/(j)
StageCheck: stage/progress checks, phase checks, ""10-hour dual check"", foreign progress checks
KnowledgeTestPrep: 61.35, 61.105(a), 61.65(b), 61.125, home study, 61.183(d)/(f), 61.405(a)(1) - ready for a knowledge (written) test
KnowledgeTestDeficiencies: 61.39(a)(6)(iii) review of deficiencies on a knowledge test
PracticalTestPrep: 61.39(a), 61.107/109, 61.65(c)/(d), 61.127/129, 61.63, 61.157, 61.160, 61.183(g), 61.187, 61.309-313, 61.321, 61.409-419 - ready for a practical test / checkride
Retest: 61.49 retesting after failure
TestResult: 61.189 record of test result, ""PASSED"", knowledge test reports (AKTR), notices of disapproval
CertificationTrainingProgram: 61.153(e) ATP CTP
Complex: 61.31(e)
HighPerformance: 61.31(f)
Tailwheel: 61.31(i)
HighAltitude: 61.31(g) high altitude / pressurized
WithoutRating: 61.31(d)(2) operating without category/class rating
GliderAndTowing: 61.31(j), 61.69 glider towing
NightVisionGoggles: 61.31(k) NVG
LightSport: 61.325, 61.327, 61.329 sport pilot additional privileges
SecondInCommand: 61.55, 61.159(c), 135.99(c) SIC
TypeAndDifferences: SFAR 73 2(a)(3) and 2(b)(1)/(2), type or differences training, EASA variants
Checkout: club, rental and insurance checkouts
OperationalAuthorization: RVSM, PBN, oceanic, DC SFRA, mountain flying, 61.52 ultralight credit
FlightReview: 61.56 flight review (BFR), SFAR 73 2(c)
InstrumentProficiencyCheck: 61.57(d) IPC
ProficiencyCheck: 61.57(g), 61.58, LPC/OPC/PPC, foreign revalidations or proficiency checks
WingsActivity: FAA WINGS (AC 61-91) activity
WingsPhase: FAA WINGS (AC 61-91) phase completed
SafetyCourse: FAASTeam or other safety course certificates
SpinTraining: 61.183(i), 61.405(b)(1)(ii) spin training
InstructorQualification: 61.195, SFAR 73 2(b)(5), instructor qualifications
GroundInstructorRecency: 61.217
Citizenship: 49 CFR 1552.3(h), 1552.15(c) TSA citizenship verification; passports and birth certificates
SecurityAwareness: 49 CFR 1552.23 security awareness training
FlightTrainingSecurityProgram: TSA FTSP approvals
PilotCertificate: pilot certificates and licenses, temporary airman certificates, remote pilot (Part 107)
Medical: medical certificates, BasicMed
LogbookPages: scans of logbook pages
TrainingRecord: ground instruction logs, training records, syllabus records
OtherDocument: FCC permits, simulator letters of authorization, ID cards, other non-endorsement documents
Other: an endorsement that fits none of the above";
            }
        }

        private static string SystemPrompt
        {
            get
            {
                return @"You read scanned or photographed pilot endorsements and aviation documents for an online pilot logbook, and record what is on them.

An image may hold zero, one, or several endorsements (e.g., a logbook page with three instructor sign-offs, or a letter-sized page of endorsements). Report each distinct endorsement as its own item. A non-endorsement document (medical, pilot certificate, logbook pages, test report, course certificate, etc.) is ONE item for the whole document. If the image is neither an endorsement nor a recognizable aviation document (a photo of an airplane, a selfie, a blank page), return an empty items list.

Rules:
- Only report what you can actually read. Never invent names, certificate numbers, dates or regulations. Use null for anything not visible or illegible.
- Handwriting is common; transcribe your best reading, and lower the confidence when unsure.
- Dates must be YYYY-MM-DD. If the year is ambiguous or missing, use null.
- category must be one of the category names listed below, exactly as spelled. Categorize by what the item does, not just by regulation: foreign (non-FAA) items map to the closest category (an EASA revalidation is ProficiencyCheck; ""cleared for solo in circuit"" is Solo).
- regulation is the regulation cited (e.g., ""61.87(b)"", ""61.31(e)"", ""FCL.740""), without ""FAR"" or ""14 CFR"". Null if none is cited.
- title is a short title in the style of ""Pre-solo aeronautical knowledge"" or ""Complex aircraft"" or ""Flight review"" or ""Third class medical"".
- displayText:
  - For an endorsement: the full text of the endorsement, transcribed, in the same style as a typed endorsement (one paragraph, no instructor signature block).
  - For a test result: test name, date and score only.
  - For a medical or BasicMed document: the class and exam date only. NEVER include limitations, conditions, or any health information.
  - For a pilot certificate: certificate type, grade and ratings only. NEVER include the certificate number, address, date of birth or physical description.
  - For a passport, birth certificate or ID: the document type only.
  - For logbook pages: ""Logbook pages"" plus the date range if visible. No transcription.
- rawText: a faithful transcription of the item's text, for search. For medicals, pilot certificates, passports, birth certificates and IDs, rawText MUST be null.
- NEVER include passport numbers, driver's license numbers, birth certificate numbers, or any other government ID number in ANY field, even when it appears in the text of an endorsement (e.g., a TSA citizenship endorsement that cites a passport number). Replace it with ""[number omitted]"" in rawText and omit it from displayText.
- studentName: the name of the person endorsed (or the document holder), if visible.
- instructorName / instructorCertificate / instructorCertificateExpiration: for endorsements, the signing instructor's name, certificate number and expiration as written. instructorCertificate is ONLY the instructor's own pilot/instructor certificate number (often written next to the signature, e.g. ""1234567 CFI""); it is never a number belonging to the student or a document. If the instructor's certificate number isn't visible, use null. For documents, null.
- pageNumber: 1-based page of the PDF the item is on (1 for a single image).
- confidence: 0.0-1.0, your confidence that the item is read AND categorized correctly.

The user's own label for the image is provided as a hint. Labels like ""BasicMed"", ""Solo endorsements"", or ""Flight log as of 2018-12-30"" are very reliable when present; labels like ""IMG_1234.JPG"", ""scan001"", ""1"", or a date or hash carry no meaning and should be ignored.

Categories (name: what belongs there):
" + CategoryGuide;
            }
        }

        private static JObject StringOrNull(string description)
        {
            return new JObject { ["type"] = new JArray("string", "null"), ["description"] = description };
        }

        private static JObject ToolDefinition()
        {
            List<string> lstCategories = new List<string>();
            foreach (EndorsementCategory ec in Enum.GetValues(typeof(EndorsementCategory)))
                if (ec != EndorsementCategory.None)
                    lstCategories.Add(ec.ToString());

            JObject item = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["category"] = new JObject { ["type"] = "string", ["enum"] = new JArray(lstCategories), ["description"] = "The category name" },
                    ["title"] = new JObject { ["type"] = "string", ["description"] = "Short title for the item" },
                    ["regulation"] = StringOrNull("Regulation cited, without FAR/14 CFR prefix"),
                    ["date"] = StringOrNull("Date of the endorsement or document, YYYY-MM-DD"),
                    ["studentName"] = StringOrNull("Name of the person endorsed or the document holder"),
                    ["instructorName"] = StringOrNull("Signing instructor's name"),
                    ["instructorCertificate"] = StringOrNull("Signing instructor's certificate number"),
                    ["instructorCertificateExpiration"] = StringOrNull("Instructor certificate expiration, YYYY-MM-DD"),
                    ["pageNumber"] = new JObject { ["type"] = "integer", ["description"] = "1-based page number" },
                    ["rawText"] = StringOrNull("Faithful transcription; null for medicals, certificates and IDs"),
                    ["displayText"] = new JObject { ["type"] = "string", ["description"] = "Text to display, per the rules for this kind of item" },
                    ["confidence"] = new JObject { ["type"] = "number", ["description"] = "0.0-1.0" }
                },
                ["required"] = new JArray("category", "title", "regulation", "date", "studentName", "instructorName", "instructorCertificate", "instructorCertificateExpiration", "pageNumber", "rawText", "displayText", "confidence")
            };

            return new JObject
            {
                ["name"] = ToolName,
                ["description"] = "Record each endorsement or document found in the image. Use an empty items list if there are none.",
                ["input_schema"] = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["items"] = new JObject { ["type"] = "array", ["items"] = item },
                        ["notes"] = StringOrNull("Optional short note about legibility or anything affecting confidence")
                    },
                    ["required"] = new JArray("items", "notes"),
                ["additionalProperties"] = false
                }
            };
        }
        #endregion

        #region Building the content to send
        /// <summary>
        /// Trims a PDF to its first MaxPDFPages pages.  Returns null if the PDF can't be read (e.g., encrypted or damaged).
        /// </summary>
        private static byte[] TrimPDF(byte[] rgb)
        {
            try
            {
                using (MemoryStream msIn = new MemoryStream(rgb))
                using (PdfDocument docIn = PdfReader.Open(msIn, PdfDocumentOpenMode.Import))
                {
                    if (docIn.PageCount <= MaxPDFPages)
                        return rgb;

                    using (PdfDocument docOut = new PdfDocument())
                    using (MemoryStream msOut = new MemoryStream())
                    {
                        for (int i = 0; i < MaxPDFPages; i++)
                            docOut.AddPage(docIn.Pages[i]);
                        docOut.Save(msOut, false);
                        return msOut.ToArray();
                    }
                }
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves the full file from S3 via a short-lived presigned URL.  We fetch it ourselves (rather than handing the URL to the model)
        /// so that we don't depend on the model's servers being able to reach S3, and so that failures are reported clearly.
        /// </summary>
        private static async Task<byte[]> FetchFromS3(MFBImageInfo mfbii)
        {
            Uri uri = new Uri(mfbii.PresignedUrlFullImageS3(TimeSpan.FromMinutes(5)));
            object result;
            try
            {
                result = await SharedHttpClient.GetResponseForAuthenticatedUri(uri, null, HttpMethod.Get, null, (response) =>
                {
                    if (!response.IsSuccessStatusCode)
                        throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Unable to retrieve {0} from S3 (HTTP {1}).", mfbii.PathFullImage, (int)response.StatusCode));
                    return response.Content.ReadAsByteArrayAsync().Result;
                }, timeout: FetchTimeout).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new MyFlightbookException("Unable to retrieve the file to read: " + ex.Message, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new MyFlightbookException("Timed out retrieving the file to read.", ex);
            }
            return (byte[])result;
        }

        private static async Task<byte[]> FullFileBytes(MFBImageInfo mfbii)
        {
            // Local JPEGs (not yet moved to S3) and local PDFs are read from disk; everything else comes from S3.
            if (mfbii.IsLocal)
                return File.ReadAllBytes(mfbii.PathFullImage.MapAbsoluteFilePath());
            return await FetchFromS3(mfbii).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds the content block (image or document) for the image.  Everything is sent inline (base64); PDFs are trimmed to the first MaxPDFPages pages.
        /// </summary>
        private static async Task<JObject> ContentBlockForImage(MFBImageInfo mfbii)
        {
            switch (mfbii.ImageType)
            {
                case MFBImageInfoBase.ImageFileType.JPEG:
                    return new JObject
                    {
                        ["type"] = "image",
                        ["source"] = new JObject { ["type"] = "base64", ["media_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(await FullFileBytes(mfbii).ConfigureAwait(false)) }
                    };
                case MFBImageInfoBase.ImageFileType.PDF:
                case MFBImageInfoBase.ImageFileType.S3PDF:
                    {
                        byte[] rgb = await FullFileBytes(mfbii).ConfigureAwait(false);

                        byte[] rgbTrimmed = TrimPDF(rgb);
                        if (rgbTrimmed == null)
                        {
                            if (rgb.Length > MaxUntrimmedPDFBytes)
                                throw new MyFlightbookException(Resources.Endorsements.ImageScanPDFUnreadable);
                            rgbTrimmed = rgb;
                        }
                        return new JObject
                        {
                            ["type"] = "document",
                            ["source"] = new JObject { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = Convert.ToBase64String(rgbTrimmed) }
                        };
                    }
                default:
                    throw new InvalidOperationException("Only images and PDFs can be read");
            }
        }
        #endregion

        /// <summary>
        /// The model occasionally returns "items" as a JSON-encoded STRING rather than an array - sometimes with the rest of the object (e.g., "notes")
        /// tacked onto the end of that string, as in "[{...}],\"notes\":\"...\"}".  Recover the array (and notes) in that case.
        /// </summary>
        private static JToken NormalizeToolInput(JToken toolInput)
        {
            if (!(toolInput is JObject jo) || !(jo["items"] is JValue jv) || jv.Type != JTokenType.String)
                return toolInput;

            string szItems = ((string)jv ?? string.Empty).Trim();

            // Case 1: the string is the rest of the object, i.e. the array followed by ,"notes":"..."}
            try
            {
                JObject joRecovered = JObject.Parse("{\"items\":" + szItems + (szItems.EndsWith("}", StringComparison.Ordinal) && !szItems.EndsWith("]}", StringComparison.Ordinal) && !szItems.EndsWith("}]", StringComparison.Ordinal) ? string.Empty : "}"));
                if (joRecovered["items"] is JArray)
                {
                    if (joRecovered["notes"] == null)
                        joRecovered["notes"] = jo["notes"];
                    return joRecovered;
                }
            }
            catch (JsonException) { }

            // Case 2: just read the leading array and ignore whatever follows it.
            try
            {
                using (StringReader sr = new StringReader(szItems))
                using (JsonTextReader reader = new JsonTextReader(sr))
                {
                    if (reader.Read() && reader.TokenType == JsonToken.StartArray)
                        return new JObject { ["items"] = JArray.Load(reader), ["notes"] = jo["notes"] };
                }
            }
            catch (JsonException) { }

            throw new MyFlightbookException(Resources.Endorsements.ImageScanServiceUnavailable);
        }

        /// <summary>
        /// Reads the specified image or PDF and returns what was found.  Throws MyFlightbookException on failure.
        /// </summary>
        /// <param name="mfbii">The image</param>
        /// <param name="fIsOffline">True if this is an instructor's record of endorsements given to (non-member) students</param>
        public static async Task<EndorsementScanResult> ScanAsync(MFBImageInfo mfbii, bool fIsOffline)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));

            JObject contentBlock = await ContentBlockForImage(mfbii).ConfigureAwait(false);

            string szLabel = String.IsNullOrWhiteSpace(mfbii.Comment) ? "(none)" : mfbii.Comment.Trim();
            string szUserText = String.Format(CultureInfo.InvariantCulture, "The user's label for this {0}: \"{1}\"\r\n{2}\r\nToday's date is {3:yyyy-MM-dd}. Record the endorsements and documents you find.",
                mfbii.ImageType == MFBImageInfoBase.ImageFileType.JPEG ? "image" : "document",
                szLabel.Replace("\"", "'"),
                fIsOffline ? "This was uploaded by a flight instructor as a record of endorsements they gave to a student; studentName is the student's name." : "This was uploaded by the pilot who received these endorsements or holds these documents.",
                DateTime.Now);

            JObject requestBody = new JObject
            {
                ["model"] = ModelName,
                ["max_tokens"] = 8192,
                ["system"] = SystemPrompt,
                ["tools"] = new JArray(ToolDefinition()),
                ["tool_choice"] = new JObject { ["type"] = "tool", ["name"] = ToolName },
                ["messages"] = new JArray(
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = new JArray(contentBlock, new JObject { ["type"] = "text", ["text"] = szUserText })
                    })
            };

            Dictionary<string, string> dictHeaders = new Dictionary<string, string>
            {
                { "x-api-key", ApiKey },
                { "anthropic-version", AnthropicVersion }
            };

            string responseBody;
            using (StringContent content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json"))
            {
                try
                {
                    responseBody = (string)await SharedHttpClient.GetResponseForAuthenticatedUri(new Uri(MessagesEndpoint), null, HttpMethod.Post, content, (response) =>
                    {
                        string szBody = response.Content.ReadAsStringAsync().Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            // Surface the API's own error message (e.g., "Unable to download the file") - it's far more useful than the status code alone.
                            string szDetail = null;
                            try { szDetail = (string)JObject.Parse(szBody)["error"]?["message"]; } catch (JsonException) { }
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.Endorsements.ImageScanServiceError, (int)response.StatusCode) + (String.IsNullOrEmpty(szDetail) ? string.Empty : " " + szDetail));
                        }
                        return szBody;
                    }, dictHeaders, timeout: ModelTimeout).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw new MyFlightbookException(Resources.Endorsements.ImageScanServiceUnavailable, ex);
                }
                catch (TaskCanceledException ex)
                {
                    throw new MyFlightbookException(Resources.Endorsements.ImageScanServiceUnavailable, ex);
                }
            }

            JObject parsed;
            try
            {
                parsed = JObject.Parse(responseBody);
            }
            catch (JsonException ex)
            {
                throw new MyFlightbookException(Resources.Endorsements.ImageScanServiceUnavailable, ex);
            }

            JToken toolInput = parsed["content"]?.FirstOrDefault(c => (string)c["type"] == "tool_use")?["input"] ?? throw new MyFlightbookException(Resources.Endorsements.ImageScanServiceUnavailable);

            EndorsementScanResult result = NormalizeToolInput(toolInput).ToObject<EndorsementScanResult>() ?? new EndorsementScanResult();
            result.Items = (result.Items ?? new List<ScannedEndorsementItem>()).Where(i => i != null && !String.IsNullOrWhiteSpace(i.Title)).Take(MaxItemsPerImage).ToList();
            return result;
        }
    }
}
