using MyFlightbook.BasicmedTools;
using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

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
    public class LogbookBackup
    {
        #region Properties
        private Profile User { get; set; }

        public bool IncludeImages { get; set; } = true;
        #endregion

        public LogbookBackup(Profile user, bool fIncludeIMages = true)
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

            IEnumerable<IDictionary<string, object>> flightreviews = (IEnumerable<IDictionary<string, object>>) d["Flight Reviews"];
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
            Dictionary<int, Collection<MFBImageInfo>> dImages = new Dictionary<int, Collection<MFBImageInfo>>();
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
                        Collection<MFBImageInfo> lstMFBii;
                        if (dImages.TryGetValue(idFlight, out Collection<MFBImageInfo> value))
                            lstMFBii = value;
                        else
                            dImages[idFlight] = lstMFBii = new Collection<MFBImageInfo>();
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
                        IEnumerable<MFBImageInfo> rgmfbii = (dImages.TryGetValue(le.FlightID, out Collection<MFBImageInfo> value)) ? value : new Collection<MFBImageInfo>();
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
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
                {
                    using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                    {
                        string szUserFullName = User.UserFullName;

                        // Write header tags.  This leaves an open body tag and an open html tag.
                        WriteHtmlHeaders(tw, Branding.ReBrand("https://%APP_URL%%APP_ROOT%/public/stylesheet.css", activeBrand), szUserFullName);

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

        public void LogbookDataForBackup(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            using (Page p = new FormlessPage())
            {
                p.Controls.Add(new HtmlForm());
                using (Control c = p.LoadControl("~/Controls/mfbDownload.ascx"))
                {
                    ((IDownloadableAsData) c).ToStream(User.UserName, s);
                }
            }
        }

        /// <summary>
        /// The name for the backup file
        /// </summary>
        public string BackupFilename(Brand activeBrand)
        {
            if (activeBrand == null)
                throw new ArgumentNullException(nameof(activeBrand));
            string szBaseName = String.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}", activeBrand.AppName, User.UserFullName, User.OverwriteCloudBackup ? string.Empty : String.Format(CultureInfo.InvariantCulture, "-{0}", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Replace(" ", "-");
            return String.Format(CultureInfo.InvariantCulture, "{0}.csv", RegexUtility.UnSafeFileChars.Replace(szBaseName, string.Empty));
        }

        /// <summary>
        /// The name for the images file
        /// </summary>
        public static string BackupImagesFilename(Brand activeBrand, bool fDateStamp = false)
        {
            if (activeBrand == null)
                throw new ArgumentNullException(nameof(activeBrand));
            return Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, "%APP_NAME% Images{0}.zip", fDateStamp ? " " + DateTime.Now.YMDString() : string.Empty), activeBrand);
        }

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
                LogbookDataForBackup(fs);
                Dropbox.Api.Files.FileMetadata result = await new MFBDropbox(User).PutFile(BackupFilename(activeBrand), fs).ConfigureAwait(true);
                return result;
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
                    LogbookDataForBackup(fs);
                    return await od.PutFileDirect(BackupFilename(activeBrand), fs, "text/csv").ConfigureAwait(true);
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
                LogbookDataForBackup(fs);
                return await gd.PutFile(BackupFilename(activeBrand), fs, "text/csv").ConfigureAwait(true);
            }
        }
        #endregion
        #endregion
    }
}