using MyFlightbook.BasicmedTools;
using MyFlightbook.CloudStorage;
using MyFlightbook.CSV;
using MyFlightbook.Currency;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Provides services for backing up data for a given user
    /// </summary>
    public abstract class LogbookBackupBase
    {
        #region Properties
        protected Profile User { get; set; }

        public bool IncludeImages { get; set; } = true;
        #endregion

        protected LogbookBackupBase(Profile user, bool fIncludeIMages = true)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            IncludeImages = fIncludeIMages;
        }

        #region ZipFileHelpers
        private static string WriteTelemetryStringToArchive(ZipArchive z, string szTelemetry, int idFlight)
        {
            DataSourceType dst = DataSourceType.BestGuessTypeFromText(szTelemetry);
            string szFile = String.Format(CultureInfo.InvariantCulture, "{0}/{1}.{2}", szThumbTelemetry, idFlight, dst.DefaultExtension);
            ZipArchiveEntry ze = z.CreateEntry(szFile);
            using (StreamWriter writer = new StreamWriter(ze.Open()))
                writer.Write(szTelemetry);
            return szFile;
        }

        private static void WriteVideo(HtmlTextWriter tw, VideoRef v)
        {
            tw.Write(v.EmbedHTML());
            tw.RenderBeginTag(HtmlTextWriterTag.P);
            tw.Write(String.Format(CultureInfo.InvariantCulture, "<a href=\"{0}\"{1}</a></p>", v.VideoReference, v.DisplayString));
            tw.Write(v.Comment);
            tw.RenderEndTag();  // P
        }

        const string szThumbFolderEndorsements = "thumbsendorsement";
        const string szThumbFolderBasicMed = "thumbsbasicmed";
        const string szThumbFolderFlights = "thumbsFlights";
        const string szThumbTelemetry = "Telemetry";

        protected static IDictionary<string, object> PilotInfo(Profile pf)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            return new Dictionary<string, object>
            {
                ["Given Name"] = String.IsNullOrEmpty(pf.UserFirstName) ? string.Empty : pf.UserFirstName,
                ["Family Name"] = String.IsNullOrEmpty(pf.UserLastName) ? string.Empty : pf.UserLastName,
                ["Email"] = String.IsNullOrEmpty(pf.Email) ? string.Empty : pf.Email,
                ["Address"] = String.IsNullOrEmpty(pf.Address) ? string.Empty : pf.Address,
                ["CFICertificate"] = String.IsNullOrEmpty(pf.CFIDisplay) ? string.Empty : pf.CFIDisplay,
                ["Certificate"] = pf.License ?? string.Empty,
                ["Last Medical"] = pf.LastMedical.HasValue() ? pf.LastMedical.YMDString() : string.Empty,
                ["Kind of Medical"] = pf.LastMedical.HasValue() ? new ProfileCurrency(pf).MedicalDescription : string.Empty,
                ["English Proficiency"] = pf.EnglishProficiencyExpiration.HasValue() ? pf.EnglishProficiencyExpiration.YMDString() : string.Empty,
                ["Flight Reviews"] = ProfileEvent.AsPublicList(ProfileEvent.GetBFREvents(pf.UserName, new ProfileCurrency(pf).LastBFREvent)),
                ["IPCs"] = ProfileEvent.AsPublicList(ProfileEvent.GetIPCEvents(pf.UserName)),
                ["Ratings"] = new Achievements.UserRatings(pf.UserName).AsKeyValuePairs()
            };
        }

        public static string PilotInfoAsJSon(Profile pf)
        {
            return JsonConvert.SerializeObject(PilotInfo(pf), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
        }

        private static void WriteKeyValue(HtmlTextWriter tw, string szKey, string szValue, bool fBoldKey)
        {
            if (String.IsNullOrEmpty(szValue))
                return;

            tw.Write(String.Format(CultureInfo.InvariantCulture, "<div {0}>{1}: {2}</div>", fBoldKey ? "style=\"font-weight:bold\"" : string.Empty, szKey, szValue));
        }

        private static void WriteProfileEvent(HtmlTextWriter tw, IDictionary<string, object> d)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.Li);
            tw.Write(String.Format(CultureInfo.InvariantCulture, "<span style=\"font-weight: bold\">{0}</span>: {1} ({2})", d["Date"], d["Property Type"], String.Format(CultureInfo.CurrentCulture, "{0}, {1}, {2}", d["Model"], d["Category"], d["Type"]).Trim()));
            tw.RenderEndTag();
        }

        private static void WritePilotInformation(HtmlTextWriter tw, Profile pf)
        {
            if (tw == null)
                throw new ArgumentNullException(nameof(tw));
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            IDictionary<string, object> d = PilotInfo(pf);

            if (d.Count == 0)
                return;

            tw.RenderBeginTag(HtmlTextWriterTag.H1);
            tw.Write(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupPilotInfoHeader, pf.UserFullName));
            tw.RenderEndTag();

            // Write out the top line strings, ignoring empty values
            foreach (string szkey in d.Keys)
            {
                if (d[szkey] is string szValue)
                    WriteKeyValue(tw, szkey, szValue, true);
            }

            IEnumerable<IDictionary<string, object>> flightreviews = (IEnumerable<IDictionary<string, object>>)d["Flight Reviews"];
            if (flightreviews.Any())
            {
                tw.RenderBeginTag(HtmlTextWriterTag.H2);
                tw.Write(Resources.Preferences.PilotInfoBFRs);
                tw.RenderEndTag();

                tw.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach (var review in flightreviews)
                    WriteProfileEvent(tw, review);
                tw.RenderEndTag();
            }

            IEnumerable<IDictionary<string, object>> ipcs = (IEnumerable<IDictionary<string, object>>)d["IPCs"];
            if (ipcs.Any())
            {
                tw.RenderBeginTag(HtmlTextWriterTag.H2);
                tw.Write(Resources.Preferences.PilotInfoIPCHeader);
                tw.RenderEndTag();

                tw.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach (var ipc in ipcs)
                    WriteProfileEvent(tw, ipc);
                tw.RenderEndTag();
            }

            // Checkrides and ratings
            IDictionary<string, object> ratings = (IDictionary<string, object>)d["Ratings"];
            IEnumerable<IDictionary<string, object>> certificates = (IEnumerable<IDictionary<string, object>>)ratings["Certificates"];

            if (certificates.Any())
            {
                tw.RenderBeginTag(HtmlTextWriterTag.H2);
                tw.Write(Resources.Preferences.PilotInfoRatings);
                tw.RenderEndTag();

                foreach (var rating in certificates)
                {
                    tw.RenderBeginTag(HtmlTextWriterTag.P);
                    tw.Write(rating["Certificate Name"]);
                    tw.RenderEndTag();

                    tw.RenderBeginTag(HtmlTextWriterTag.Ul);
                    IEnumerable<string> rgPrivs = (IEnumerable<string>)rating["Privileges"];
                    foreach (string sz in rgPrivs)
                    {
                        tw.RenderBeginTag(HtmlTextWriterTag.Li);
                        tw.Write(sz);
                        tw.RenderEndTag();
                    }
                    tw.RenderEndTag();
                }
            }
        }

        private static void AddThumbnailToZip(MFBImageInfo mfbii, ZipArchive zip, string szFolder)
        {
            if (mfbii is null)
                throw new ArgumentNullException(nameof(mfbii));
            if (zip is null)
                throw new ArgumentNullException(nameof(zip));
            if (szFolder is null)
                throw new ArgumentNullException(nameof(szFolder));

            string imgPath = System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail);
            if (!File.Exists(imgPath))
            {
                mfbii.DeleteFromDB();   // clean up an orphan
                return;
            }

            // No need to add an S3PDF to the zip file; it's just a placeholder file on the disk, and it can 
            // potentially have a duplicate name with another PDF as well.
            if (mfbii.ImageType != MFBImageInfo.ImageFileType.S3PDF)
                zip.CreateEntryFromFile(imgPath, szFolder + "/" + mfbii.ThumbnailFile);
        }

        private static void WriteFlightInfo(HtmlTextWriter tw, ZipArchive zip, LogbookEntry le)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.H2);
            tw.Write(String.Format(CultureInfo.CurrentCulture, "{0} - {1}", HttpUtility.HtmlEncode(le.Date.ToShortDateString()), HttpUtility.HtmlEncode(le.TailNumDisplay)));
            tw.RenderEndTag();

            if (!String.IsNullOrEmpty(le.Route))
                tw.Write(String.Format(CultureInfo.InvariantCulture, "<p>{0}</p>", HttpUtility.HtmlEncode(le.Route)));
            if (!String.IsNullOrEmpty(le.Comment))
                tw.Write(String.Format(CultureInfo.InvariantCulture, "<p>{0}</p>", HttpUtility.HtmlEncode(le.Comment)));

            if (!String.IsNullOrEmpty(le.FlightData))
            {
                string szFile = WriteTelemetryStringToArchive(zip, le.FlightData, le.FlightID);
                tw.Write(String.Format(CultureInfo.InvariantCulture, "<p><a href=\"{0}\">{1}</a></p>", szFile, Resources.LocalizedText.TelemetryBackupLink));
            }

            if (le.FlightImages != null)
            {
                foreach (MFBImageInfo mfbii in le.FlightImages)
                {
                    mfbii.ToHtml(tw, szThumbFolderFlights);
                    AddThumbnailToZip(mfbii, zip, szThumbFolderFlights);
                }
            }

            if (le.Videos.Count != 0)
            {
                foreach (VideoRef v in le.Videos)
                    WriteVideo(tw, v);
            }
        }

        private static void WriteHtmlHeaders(HtmlTextWriter tw, string szStylesheetRef, string szUserFullName)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.Html);
            tw.RenderBeginTag(HtmlTextWriterTag.Head);
            tw.AddAttribute("href", szStylesheetRef);
            tw.AddAttribute("rel", "stylesheet");
            tw.AddAttribute("type", "text/css");
            tw.RenderBeginTag(HtmlTextWriterTag.Link);
            tw.RenderEndTag();   // Link
            tw.RenderBeginTag(HtmlTextWriterTag.Title);
            tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupTitle, szUserFullName)));
            tw.RenderEndTag();   // Head
            tw.RenderBeginTag(HtmlTextWriterTag.Body);
        }

        private static void WriteProfileImages(HtmlTextWriter tw, string szUserFullName, string szUser, ZipArchive zip)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.H1);
            tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupEndorsementsHeader, szUserFullName)));
            tw.RenderEndTag();  // h1

            ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, szUser);
            il.Refresh(true);
            foreach (MFBImageInfo mfbii in il.ImageArray)
            {
                mfbii.ToHtml(tw, szThumbFolderEndorsements);
                AddThumbnailToZip(mfbii, zip, szThumbFolderEndorsements);
                mfbii.UnCache();
            }
        }

        private static void WriteDigitalEndorsements(HtmlTextWriter tw, string szUser)
        {
            IEnumerable<Endorsement> rgEndorsements = Endorsement.EndorsementsForUser(szUser, null);
            foreach (Endorsement en in rgEndorsements)
            {
                try { en.RenderHTML(tw); }
                catch (Exception ex) when (!(ex is OutOfMemoryException)) { }  // don't write bogus or incomplete HTML
            }
        }

        private static void WriteBasicMedForUser(HtmlTextWriter tw, string szUser, ZipArchive zip)
        {
            IEnumerable<BasicMedEvent> lstBMed = BasicMedEvent.EventsForUser(szUser);
            if (!lstBMed.Any())
                return;

            tw.Write(String.Format(CultureInfo.CurrentCulture, "<p>{0}</p>", Resources.Profile.BasicMedHeader));
            tw.RenderBeginTag(HtmlTextWriterTag.Ul);
            foreach (BasicMedEvent bme in lstBMed)
                tw.Write(String.Format(CultureInfo.InvariantCulture, "<li>{0} - {1} {2}</li>", bme.EventDate.YMDString(), bme.EventTypeDescription, bme.Description));
            tw.RenderEndTag(); // Ul

            foreach (BasicMedEvent bme in lstBMed)
            {
                string szZipFolder = String.Format(CultureInfo.InvariantCulture, "{0}-{1}", bme.ImageKey, szThumbFolderBasicMed);
                ImageList ilBasicMed = new ImageList(MFBImageInfo.ImageClass.BasicMed, bme.ImageKey);
                ilBasicMed.Refresh(true);
                foreach (MFBImageInfo mfbii in ilBasicMed.ImageArray)
                {
                    mfbii.ToHtml(tw, szZipFolder);
                    AddThumbnailToZip(mfbii, zip, szZipFolder);
                    mfbii.UnCache();
                }
            }
        }

        private void WriteFlightImagesForUser(HtmlTextWriter tw, string szUserFullName, string szUser, ZipArchive zip)
        {
            tw.RenderBeginTag(HtmlTextWriterTag.H1);
            tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupFlightsHeader, szUserFullName)));
            tw.RenderEndTag();  // H1

            // We'll get images from the DB rather than slamming the disk
            // this is a bit of a hack, but limits our queries
            Dictionary<int, List<MFBImageInfo>> dImages = new Dictionary<int, List<MFBImageInfo>>();
            if (IncludeImages)
            {
                const string szQ = @"SELECT f.idflight, img.*
            FROM Images img INNER JOIN flights f ON CAST(f.idflight AS CHAR(15) CHARACTER SET utf8mb4) COLLATE utf8mb4_unicode_ci=img.ImageKey
            WHERE f.username=?user AND img.VirtPathID=0
            ORDER BY f.Date DESC, f.idFlight DESC";
                DBHelper dbhImages = new DBHelper(szQ);
                dbhImages.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                    (dr) =>
                    {
                        int idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture);
                        List<MFBImageInfo> lstMFBii;
                        if (dImages.TryGetValue(idFlight, out List<MFBImageInfo> value))
                            lstMFBii = value;
                        else
                            dImages[idFlight] = lstMFBii = new List<MFBImageInfo>();
                        lstMFBii.Add(MFBImageInfo.ImageFromDBRow(dr));
                    });
            }

            // Get all of the user's flights, including telemetry
            const int PageSize = 200;   // get 200 flights at a time.
            int offset = 0;
            int iRow = 0;
            bool fCouldBeMore = true;

            while (fCouldBeMore)
            {
                FlightQuery fq = new FlightQuery(szUser);
                DBHelper dbhFlights = new DBHelper(LogbookEntry.QueryCommand(fq, offset, PageSize, true, LogbookEntry.LoadTelemetryOption.LoadAll));
                dbhFlights.ReadRows((comm) => { },
                    (dr) =>
                    {
                        LogbookEntry le = new LogbookEntry(dr, szUser, LogbookEntry.LoadTelemetryOption.LoadAll);
                        le.FlightImages.Clear();
                        IEnumerable<MFBImageInfo> rgmfbii = (dImages.TryGetValue(le.FlightID, out List<MFBImageInfo> value)) ? value : new List<MFBImageInfo>();
                        foreach (MFBImageInfo mfbii in rgmfbii)
                            le.FlightImages.Add(mfbii);

                        // skip any flights here that don't have images, videos, or telemetry
                        if (le.FlightImages.Count > 0 || le.Videos.Count != 0 || le.HasFlightData)
                            WriteFlightInfo(tw, zip, le);
                        iRow++;
                    });
                if (fCouldBeMore = (iRow == offset + PageSize))
                    offset += PageSize;
            }
        }
        #endregion

        /// <summary>
        /// Creates/returns a memory stream containing a zip of a) an HTML file of images, and b) the thumbnails of the images, linked to Amazon.
        /// </summary>
        /// <param name="activeBrand">The brand to use - null for current brand</param>
        /// <param name="ms">The stream to which to write</param>
        public void WriteZipOfImagesToStream(Stream ms, Brand activeBrand, Action<ZipArchive> finalize = null)
        {
            if (ms == null)
                throw new ArgumentNullException(nameof(ms));

            using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
                {
                    using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                    {
                        string szUserFullName = User.UserFullName;

                        // Write header tags.  This leaves an open body tag and an open html tag.
                        WriteHtmlHeaders(tw, Branding.ReBrand("https://%APP_URL%%APP_ROOT%/public/stylesheet.css", activeBrand ?? Branding.CurrentBrand), szUserFullName);

                        // Write out pilot information
                        WritePilotInformation(tw, User);

                        // Write out profile images
                        WriteProfileImages(tw, szUserFullName, User.UserName, zip);

                        // Write out any digital endorsements too
                        WriteDigitalEndorsements(tw, User.UserName);

                        // And any BasicMed stuff
                        WriteBasicMedForUser(tw, User.UserName, zip);

                        // Write out flight images and Telemetry
                        WriteFlightImagesForUser(tw, szUserFullName, User.UserName, zip);

                        tw.RenderEndTag();  // Body
                        tw.RenderEndTag();  // Html
                    }

                    ZipArchiveEntry zipArchiveEntry = zip.CreateEntry(Branding.ReBrand(String.Format(CultureInfo.InvariantCulture, "{0}.htm", Resources.LocalizedText.ImagesBackupFilename), activeBrand));

                    using (StreamWriter swHtm = new StreamWriter(zipArchiveEntry.Open()))
                    {
                        swHtm.Write(sw.ToString());
                    }
                }

                // Allow caller to add any additional items.
                finalize?.Invoke(zip);
            }
        }

        public byte[] WriteZipOfImagesToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteZipOfImagesToStream(ms, Branding.CurrentBrand);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// The name for the backup file
        /// </summary>
        public string BackupFilename(Brand activeBrand = null)
        {
            activeBrand = activeBrand ?? Branding.CurrentBrand;
            string szBaseName = String.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}", activeBrand.AppName, User.UserFullName, User.OverwriteCloudBackup ? string.Empty : String.Format(CultureInfo.InvariantCulture, "-{0}", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Replace(" ", "-");
            return String.Format(CultureInfo.InvariantCulture, "{0}.csv", RegexUtility.UnSafeFileChars.Replace(szBaseName, string.Empty));
        }

        /// <summary>
        /// Writes a logbook's CSV to a data table and calls the requested action
        /// <paramref name="action">The action to call</paramref>
        /// </summary>
        public void WriteLogbookCSVToDataTable(Action<DataTable> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            UserAircraft ua = new UserAircraft(User.UserName);
            // See whether or not to show catclassoverride column

            bool fShowAltCatClass = false;
            IEnumerable<LogbookEntryDisplay> rgle = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryDisplay.QueryCommand(new FlightQuery(User.UserName)), User.UserName, "Date", SortDirection.Descending, false, false);
            foreach (LogbookEntryDisplay le in rgle)
                fShowAltCatClass |= le.IsOverridden;

            // Generate the set of properties used by the user
            HashSet<CustomPropertyType> hscpt = new HashSet<CustomPropertyType>();
            foreach (LogbookEntryBase le in rgle)
            {
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    if (!hscpt.Contains(cfp.PropertyType))
                        hscpt.Add(cfp.PropertyType);
                }
            }
            // Now sort that alphabetically
            List<CustomPropertyType> lst = new List<CustomPropertyType>(hscpt);
            lst.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });

            DecimalFormat df = User.PreferenceExists(MFBConstants.keyDecimalSettings) ? User.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings) : DecimalFormat.Adaptive;

            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                if (HttpContext.Current?.Session != null)
                    HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = DecimalFormat.Adaptive;

                #region Add Headers
                // add the header columns 
                dt.Columns.Add(new DataColumn("Date", typeof(string)));
                dt.Columns.Add(new DataColumn("Flight ID", typeof(string)));
                dt.Columns.Add(new DataColumn("Model", typeof(string)));
                dt.Columns.Add(new DataColumn("ICAO Model", typeof(string)));
                dt.Columns.Add(new DataColumn("Tail Number", typeof(string)));
                dt.Columns.Add(new DataColumn("Display Tail", typeof(string)));
                dt.Columns.Add(new DataColumn("Aircraft ID", typeof(string)));
                dt.Columns.Add(new DataColumn("Category/Class", typeof(string)));
                if (fShowAltCatClass)
                    dt.Columns.Add(new DataColumn("Alternate Cat/Class", typeof(string)));
                dt.Columns.Add(new DataColumn("Approaches", typeof(string)));
                dt.Columns.Add(new DataColumn("Hold", typeof(string)));
                dt.Columns.Add(new DataColumn("Landings", typeof(string)));
                dt.Columns.Add(new DataColumn("FS Night Landings", typeof(string)));
                dt.Columns.Add(new DataColumn("FS Day Landings", typeof(string)));
                dt.Columns.Add(new DataColumn("X-Country", typeof(string)));
                dt.Columns.Add(new DataColumn("Night", typeof(string)));
                dt.Columns.Add(new DataColumn("IMC", typeof(string)));
                dt.Columns.Add(new DataColumn("Simulated Instrument", typeof(string)));
                dt.Columns.Add(new DataColumn("Ground Simulator", typeof(string)));
                dt.Columns.Add(new DataColumn("Dual Received", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI", typeof(string)));
                dt.Columns.Add(new DataColumn("SIC", typeof(string)));
                dt.Columns.Add(new DataColumn("PIC", typeof(string)));
                dt.Columns.Add(new DataColumn("Total Flight Time", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Time (HH:MM)", typeof(string)));
                dt.Columns.Add(new DataColumn("SIC Time (HH:MM)", typeof(string)));
                dt.Columns.Add(new DataColumn("PIC (HH:MM)", typeof(string)));
                dt.Columns.Add(new DataColumn("Total Flight Time (HH:MM)", typeof(string)));
                dt.Columns.Add(new DataColumn("Route", typeof(string)));
                dt.Columns.Add(new DataColumn("Flight Properties", typeof(string)));
                dt.Columns.Add(new DataColumn("Comments", typeof(string)));
                dt.Columns.Add(new DataColumn("Hobbs Start", typeof(string)));
                dt.Columns.Add(new DataColumn("Hobbs End", typeof(string)));
                dt.Columns.Add(new DataColumn("Engine Start", typeof(string)));
                dt.Columns.Add(new DataColumn("Engine End", typeof(string)));
                dt.Columns.Add(new DataColumn("Engine Time", typeof(string)));
                dt.Columns.Add(new DataColumn("Flight Start", typeof(string)));
                dt.Columns.Add(new DataColumn("Flight End", typeof(string)));
                dt.Columns.Add(new DataColumn("Flying Time", typeof(string)));
                dt.Columns.Add(new DataColumn("Complex", typeof(string)));
                dt.Columns.Add(new DataColumn("Controllable pitch prop", typeof(string)));
                dt.Columns.Add(new DataColumn("Flaps", typeof(string)));
                dt.Columns.Add(new DataColumn("Retract", typeof(string)));
                dt.Columns.Add(new DataColumn("Tailwheel", typeof(string)));
                dt.Columns.Add(new DataColumn("High Performance", typeof(string)));
                dt.Columns.Add(new DataColumn("Turbine", typeof(string)));
                dt.Columns.Add(new DataColumn("TAA", typeof(string)));
                dt.Columns.Add(new DataColumn("Signature State", typeof(string)));
                dt.Columns.Add(new DataColumn("Date of Signature", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Comment", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Certificate", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Name", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Email", typeof(string)));
                dt.Columns.Add(new DataColumn("CFI Expiration", typeof(string)));
                dt.Columns.Add(new DataColumn("Public", typeof(string)));
                #endregion

                foreach (CustomPropertyType cpt in lst)
                    dt.Columns.Add(new DataColumn(cpt.Title, typeof(string)));

                foreach (LogbookEntryDisplay le in rgle)
                {
                    Aircraft ac = ua[le.AircraftID];
                    MakeModel mm = MakeModel.GetModel(ac.ModelID);
                    DataRow dr = dt.NewRow();
                    #region Add the details of the row
                    int i = 0;
                    dr[i++] = le.Date.YMDString();
                    dr[i++] = le.FlightID.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.ModelDisplay;
                    dr[i++] = le.FamilyName;
                    dr[i++] = ac.TailNumber;
                    dr[i++] = ac.DisplayTailnumber;
                    dr[i++] = ac.AircraftID.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.CatClassDisplay;
                    if (fShowAltCatClass)
                        dr[i++] = le.IsOverridden ? le.CatClassOverride.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    dr[i++] = le.Approaches.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.fHoldingProcedures.FormatBooleanInt();
                    dr[i++] = le.Landings.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.NightLandings.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.FullStopLandings.ToString(CultureInfo.InvariantCulture);
                    dr[i++] = le.CrossCountry.FormatDecimal(false);
                    dr[i++] = le.Nighttime.FormatDecimal(false);
                    dr[i++] = le.IMC.FormatDecimal(false);
                    dr[i++] = le.SimulatedIFR.FormatDecimal(false);
                    dr[i++] = le.GroundSim.FormatDecimal(false);
                    dr[i++] = le.Dual.FormatDecimal(false);
                    dr[i++] = le.CFI.FormatDecimal(false);
                    dr[i++] = le.SIC.FormatDecimal(false);
                    dr[i++] = le.PIC.FormatDecimal(false);
                    dr[i++] = le.TotalFlightTime.FormatDecimal(false);
                    dr[i++] = le.CFI.FormatDecimal(true);
                    dr[i++] = le.SIC.FormatDecimal(true);
                    dr[i++] = le.PIC.FormatDecimal(true);
                    dr[i++] = le.TotalFlightTime.FormatDecimal(true);
                    dr[i++] = le.Route.Trim();
                    dr[i++] = CustomFlightProperty.PropListDisplay(le.CustomProperties, fShowAltCatClass, "; ");
                    dr[i++] = le.Comment.Trim();
                    dr[i++] = le.HobbsStart.ToString(CultureInfo.CurrentCulture);
                    dr[i++] = le.HobbsEnd.ToString(CultureInfo.CurrentCulture);
                    dr[i++] = le.EngineStart.FormatDateZulu();
                    dr[i++] = le.EngineEnd.FormatDateZulu();
                    dr[i++] = LogbookEntryDisplay.FormatTimeSpan(le.EngineStart, le.EngineEnd);
                    dr[i++] = le.FlightStart.FormatDateZulu();
                    dr[i++] = le.FlightEnd.FormatDateZulu();
                    dr[i++] = LogbookEntryDisplay.FormatTimeSpan(le.FlightStart, le.FlightEnd);
                    dr[i++] = mm.IsComplex.FormatBooleanInt();
                    dr[i++] = mm.IsConstantProp.FormatBooleanInt();
                    dr[i++] = mm.HasFlaps.FormatBooleanInt();
                    dr[i++] = mm.IsRetract.FormatBooleanInt();
                    dr[i++] = mm.IsTailWheel.FormatBooleanInt();
                    dr[i++] = mm.IsHighPerf.FormatBooleanInt();
                    dr[i++] = mm.EngineType == MakeModel.TurbineLevel.Piston || mm.EngineType == MakeModel.TurbineLevel.Electric ? string.Empty : 1.FormatBooleanInt();
                    dr[i++] = (mm.AvionicsTechnology == MakeModel.AvionicsTechnologyType.TAA || (ac != null && (ac.AvionicsTechnologyUpgrade == MakeModel.AvionicsTechnologyType.TAA && (!ac.GlassUpgradeDate.HasValue || le.Date.CompareTo(ac.GlassUpgradeDate.Value) >= 0)))).FormatBooleanInt();
                    dr[i++] = le.CFISignatureState.FormatSignatureState();
                    dr[i++] = le.CFISignatureDate.FormatOptionalInvariantDate();
                    dr[i++] = le.CFIComments;
                    dr[i++] = le.CFICertificate;
                    dr[i++] = le.CFIName;
                    dr[i++] = le.CFIEmail;
                    dr[i++] = le.CFIExpiration.FormatOptionalInvariantDate();
                    dr[i++] = le.fIsPublic.FormatBooleanInt();

                    // Whew!  Now add the properties that exists, for each column
                    foreach (CustomPropertyType cpt in lst)
                        dr[i++] = le.CustomProperties[cpt.PropTypeID]?.ValueString;
                    #endregion

                    dt.Rows.Add(dr);
                }

                if (HttpContext.Current?.Session != null)
                    HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = df;

                action(dt);
            }
        }

        /// <summary>
        /// Writes a logbook's CSV to the specified stream
        /// </summary>
        /// <param name="s">The stream to write to</param>
        public void WriteLogbookCSVToStream(Stream s)
        {
            using (TextWriter tw = new StreamWriter(s, Encoding.UTF8, 1024, true /* leave the underlying stream open */))
            {
                WriteLogbookCSVToDataTable((dt) =>
                {
                    CsvWriter.WriteToStream(tw, dt, true, true);
                });
            }
        }

        /// <summary>
        /// Writes a logbook's CSV to the specified stream
        /// </summary>
        public byte[] WriteLogbookCSVToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteLogbookCSVToStream(ms);
                return ms.ToArray();
            }
        }

        public string WriteLogbookCSVToHtml()
        {
            using (StringWriter sw = new StringWriter(CultureInfo.CurrentCulture))
            {
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    WriteLogbookCSVToDataTable((dt) =>
                    {
                        tw.AddAttribute("cellpadding", "3");
                        tw.AddAttribute("cellspacing", "0");
                        tw.AddAttribute("rules", "all");
                        tw.AddAttribute("border", "1");
                        tw.AddAttribute("style", "font-size: 8pt; border-collapse: collapse;");
                        tw.RenderBeginTag(HtmlTextWriterTag.Table);

                        tw.RenderBeginTag(HtmlTextWriterTag.Thead);
                        tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                        foreach (DataColumn dc in dt.Columns)
                        {
                            tw.AddAttribute("class", "PaddedCell");
                            tw.RenderBeginTag(HtmlTextWriterTag.Th);
                            tw.WriteEncodedText(dc.ColumnName);
                            tw.RenderEndTag();
                        }

                        tw.RenderEndTag();  // tr
                        tw.RenderEndTag();  // thead

                        tw.RenderBeginTag(HtmlTextWriterTag.Tbody);

                        foreach (DataRow dr in dt.Rows)
                        {
                            tw.RenderBeginTag(HtmlTextWriterTag.Tr);

                            foreach (DataColumn dc in dt.Columns)
                            {
                                tw.AddAttribute("class", "PaddedCell");
                                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                                tw.WriteEncodedText(dr[dc.ColumnName].ToString());
                                tw.RenderEndTag();
                            }

                            tw.RenderEndTag();  // tr
                        }

                        tw.RenderEndTag();  // tbody
                        tw.RenderEndTag();  // table
                    });
                    return sw.ToString();
                }
            }
        }

        /// <summary>
        /// The name for the images file
        /// </summary>
        public static string BackupImagesFilename(Brand activeBrand = null, bool fDateStamp = false)
        {
            return Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, "%APP_NAME% Images{0}.zip", fDateStamp ? " " + DateTime.Now.YMDString() : string.Empty), activeBrand ?? Branding.CurrentBrand);
        }
    }

    public class LogbookBackup : LogbookBackupBase
    {
        public LogbookBackup(Profile user, bool fIncludeIMages = true) : base(user, fIncludeIMages) { }

        public LogbookBackup(string user, bool fIncludeIMages = true) : this(Profile.GetUser(user), fIncludeIMages) { }

        #region cloud storage instances
        #region Dropbox
        /// <summary>
        /// Saves a zip of the user's images to Dropbox, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="Dropbox.Api.AuthException"></exception>
        /// <exception cref="Dropbox.Api.BadInputException"></exception>
        /// <exception cref="Dropbox.Api.HttpException"></exception>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        public async Task<Dropbox.Api.Files.FileMetadata> BackupImagesToDropbox(Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (String.IsNullOrEmpty(User.DropboxAccessToken))
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredDropBox);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteZipOfImagesToStream(fs, activeBrand);
                Dropbox.Api.Files.FileMetadata result = await new MFBDropbox(User).PutFile(BackupImagesFilename(activeBrand), fs).ConfigureAwait(true);
                return result;
            }
        }

        /// <summary>
        /// Saves a the user's logbook to Dropbox, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="Dropbox.Api.AuthException"></exception>
        /// <exception cref="Dropbox.Api.BadInputException"></exception>
        /// <exception cref="Dropbox.Api.HttpException"></exception>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        public async Task<Dropbox.Api.Files.FileMetadata> BackupToDropbox(Brand activeBrand = null)
        {
            if (String.IsNullOrEmpty(User.DropboxAccessToken))
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredDropBox);

            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteLogbookCSVToStream(fs);
                Dropbox.Api.Files.FileMetadata result = await new MFBDropbox(User).PutFile(BackupFilename(activeBrand), fs).ConfigureAwait(true);
                return result;
            }
        }

        /// <summary>
        /// Backs up to dropbox, returning any error.  Empty error = success
        /// </summary>
        /// <param name="fIncludeImages"></param>
        /// <param name="activeBrand"></param>
        /// <returns></returns>
        public async Task<string> BackupDropbox(bool fIncludeImages, Brand activeBrand = null)
        {
            try
            {
                if (await new MFBDropbox(User).ValidateDropboxToken().ConfigureAwait(false) == MFBDropbox.TokenStatus.None)
                    return Resources.Profile.errNotConfiguredDropBox;

                Dropbox.Api.Files.FileMetadata result = await BackupToDropbox().ConfigureAwait(false);

                if (fIncludeImages)
                    result = await BackupImagesToDropbox(activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                return string.Empty;
            }
            catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
            {
                return (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath != null && ex.ErrorResponse.AsPath.Value.Reason.IsInsufficientSpace) ? Resources.LocalizedText.DropboxErrorOutOfSpace : ex.Message;
            }
            catch (Exception ex) when (ex is Dropbox.Api.ApiException<Dropbox.Api.Auth.TokenFromOAuth1Error> || ex is Dropbox.Api.AuthException || ex is Dropbox.Api.BadInputException || ex is Dropbox.Api.HttpException || ex is UnauthorizedAccessException || ex is MyFlightbookException)
            {
                return ex.Message;
            }
        }
        #endregion

        #region Box
        public async Task<string> BackupToBox(Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (!User.PreferenceExists(BoxDrive.PrefKeyBoxAuthToken))
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredBox);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteLogbookCSVToStream(fs);
                return (string)await new BoxDrive(User).PutFile(BackupFilename(activeBrand), fs).ConfigureAwait(true);
            }
        }

        public async Task<string> BackupImagesToBox(Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (!User.PreferenceExists(BoxDrive.PrefKeyBoxAuthToken))
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredBox);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteZipOfImagesToStream(fs, activeBrand);
                return (string)await new BoxDrive(User).PutFile(BackupImagesFilename(activeBrand), fs).ConfigureAwait(true);
            }
        }
        public async Task<string> BackupBox(bool fIncludeImages, Brand activeBrand = null)
        {
            try
            {
                BoxDrive bd = new BoxDrive(User);

                string result = await BackupToBox(activeBrand).ConfigureAwait(false);

                if (fIncludeImages)
                    result = await BackupImagesToBox(activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                return result;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion

        #region OneDrive
        /// <summary>
        /// Saves a zip of the user's images to OneDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="OneDriveMFBException"></exception>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        /// <param name="od">The OneDrive object to use (one will be initialized, if necessary)</param>
        public async Task<bool> BackupImagesToOneDrive(OneDrive od = null, Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (User.OneDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredOneDrive);

            if (od == null)
                od = new OneDrive(User);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteZipOfImagesToStream(fs, activeBrand);
                bool result = await od.PutFileDirect(BackupImagesFilename(activeBrand), fs, "application/zip").ConfigureAwait(true);
                return result;
            }
        }

        /// <summary>
        /// Saves a the user's logbook to OneDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="OneDriveMFBException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <param name="od">The OneDrive object to use (one will be initialized, if necessary)</param>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        public async Task<bool> BackupToOneDrive(OneDrive od = null, Brand activeBrand = null)
        {
            if (User.OneDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredOneDrive);

            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (od == null)
                od = new OneDrive(User);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteLogbookCSVToStream(fs);
                return await od.PutFileDirect(BackupFilename(activeBrand), fs, "text/csv").ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Backs up to one drive, returning any error.  Empty error = success
        /// </summary>
        /// <param name="fIncludeImages"></param>
        /// <param name="activeBrand"></param>
        /// <returns></returns>
        public async Task<string> BackupOneDrive(bool fIncludeImages, Brand activeBrand = null)
        {
            if (String.IsNullOrEmpty(User.OneDriveAccessToken?.RefreshToken))
                return Resources.Profile.errNotConfiguredOneDrive;
            try
            {
                OneDrive od = new OneDrive(User);
                await BackupToOneDrive(od, activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                if (fIncludeImages)
                    await BackupImagesToOneDrive(od, activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                // if we are here we were successful, so save the updated refresh token
                User.OneDriveAccessToken = od.AuthState;
                User.FCommit();

                return string.Empty;
            }
            catch (Exception ex) when (ex is OneDriveMFBException || ex is MyFlightbookException || !(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion

        #region GoogleDrive
        /// <summary>
        /// Saves a zip of the user's images to GoogleDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        /// <param name="gd">The GoogleDrive object to use - Must be initialized and refreshed!</param>
        /// <returns>True for success</returns>
        public async Task<GoogleDriveResultDictionary> BackupImagesToGoogleDrive(GoogleDrive gd, Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (gd == null)
                throw new ArgumentNullException(nameof(gd));

            if (User.GoogleDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredGoogleDrive);

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteZipOfImagesToStream(fs, activeBrand);
                GoogleDriveResultDictionary result = await gd.PutFile(BackupImagesFilename(activeBrand, true), fs, "application/zip").ConfigureAwait(true);
                return result;
            }
        }

        /// <summary>
        /// Saves a the user's logbook to GoogleDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <param name="gd">The GoogleDrive object to use - Must be initialized and refreshed!</param>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        /// <returns>True for success</returns>
        public async Task<GoogleDriveResultDictionary> BackupToGoogleDrive(GoogleDrive gd, Brand activeBrand = null)
        {
            if (gd == null)
                throw new ArgumentNullException(nameof(gd));

            if (User.GoogleDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredGoogleDrive);

            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                WriteLogbookCSVToStream(fs);
                return await gd.PutFile(BackupFilename(activeBrand), fs, "text/csv").ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Backs up to GoogleDrive, returning any error.  Empty string = success
        /// </summary>
        /// <param name="fIncludeImages"></param>
        /// <param name="activeBrand"></param>
        /// <returns></returns>
        public async Task<string> BackupGoogleDrive(bool fIncludeImages, Brand activeBrand = null)
        {
            if (String.IsNullOrEmpty(User.GoogleDriveAccessToken?.RefreshToken))
                return Resources.Profile.errNotConfiguredGoogleDrive;

            try
            {
                GoogleDrive gd = new GoogleDrive(User);

                await BackupToGoogleDrive(gd, activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                if (fIncludeImages)
                    await BackupImagesToGoogleDrive(gd, activeBrand ?? Branding.CurrentBrand).ConfigureAwait(false);

                // if we are here we were successful, so save the updated refresh token
                User.GoogleDriveAccessToken = gd.AuthState;
                User.FCommit();

                return string.Empty;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return ex.Message;
            }
        }
        #endregion
        #endregion

        public async Task<string> BackupToCloudService(StorageID sid, bool fIncludeImages)
        {
            string szResult = string.Empty;
            switch (sid)
            {
                case StorageID.Dropbox:
                    szResult = await BackupDropbox(fIncludeImages);
                    break;
                case StorageID.OneDrive:
                    szResult = await BackupOneDrive(fIncludeImages);
                    break;
                case StorageID.GoogleDrive:
                    szResult = await BackupGoogleDrive(fIncludeImages);
                    break;
                case StorageID.Box:
                    szResult = await BackupBox(fIncludeImages);
                    break;
                default:
                    throw new InvalidOperationException("Unknown cloud service: " + sid.ToString());
            }
            return szResult;
        }
    }
}
