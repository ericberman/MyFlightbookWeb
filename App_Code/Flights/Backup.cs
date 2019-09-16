using Ionic.Zip;
using MyFlightbook.Basicmed;
using MyFlightbook.CloudStorage;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
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
        private Profile User { get; set; }

        public LogbookBackup(Profile user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            User = user;
        }

        private string WriteTelemetryStringToArchive(ZipFile z, string szTelemetry, int idFlight)
        {
            DataSourceType dst = DataSourceType.BestGuessTypeFromText(szTelemetry);
            string szFile = String.Format(CultureInfo.InvariantCulture, "Telemetry\\{0}.{1}", idFlight, dst.DefaultExtension);
            z.AddEntry(szFile, szTelemetry);
            return szFile;
        }

        private void WriteVideo(HtmlTextWriter tw, VideoRef v)
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

        private void WriteFlightInfo(HtmlTextWriter tw, ZipFile zip, LogbookEntry le)
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
                    // Add the image to the zip
                    string imgPath = System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail);
                    if (File.Exists(imgPath))
                    {
                        // No need to add an S3PDF to the zip file; it's just a placeholder file on the disk, and it can 
                        // potentially have a duplicate name with another PDF as well.
                        if (mfbii.ImageType != MFBImageInfo.ImageFileType.S3PDF)
                            zip.AddFile(imgPath, szThumbFolderFlights);
                    }
                    else
                        mfbii.DeleteFromDB();   // clean up an orphan.
                }
            }

            if (le.Videos.Count() > 0)
            {
                foreach (VideoRef v in le.Videos)
                    WriteVideo(tw, v);
            }
        }

        /// <summary>
        /// Creates/returns a memory stream containing a zip of a) an HTML file of images, and b) the thumbnails of the images, linked to Amazon.
        /// THE STREAM MUST BE CLOSED BY THE CALLER!
        /// </summary>
        /// <param name="activeBrand">The brand to use - null for current brand</param>
        /// <returns>A memory stream of flight images followed by any profile images</returns>
        public MemoryStream ZipOfImagesForUser(Brand activeBrand)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            MemoryStream ms = new MemoryStream();

            using (ZipFile zip = new ZipFile())
            {
                StringWriter sw = new StringWriter();
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    tw.RenderBeginTag(HtmlTextWriterTag.Html);
                    tw.RenderBeginTag(HtmlTextWriterTag.Head);
                    tw.AddAttribute("href", Branding.ReBrand("http://%APP_URL%%APP_ROOT%/public/stylesheet.css", activeBrand));
                    tw.AddAttribute("rel", "stylesheet");
                    tw.AddAttribute("type", "text/css");
                    tw.RenderBeginTag(HtmlTextWriterTag.Link);
                    tw.RenderEndTag();   // Link
                    tw.RenderBeginTag(HtmlTextWriterTag.Title);
                    tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupTitle, User.UserFullName)));
                    tw.RenderEndTag();   // Head
                    tw.RenderBeginTag(HtmlTextWriterTag.Body);

                    // Write out profile images
                    tw.RenderBeginTag(HtmlTextWriterTag.H1);
                    tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupEndorsementsHeader, User.UserFullName)));
                    tw.RenderEndTag();  // h1

                    ImageList il = new ImageList(MFBImageInfo.ImageClass.Endorsement, User.UserName);
                    il.Refresh(true);
                    foreach (MFBImageInfo mfbii in il.ImageArray)
                    {
                        zip.AddFile(System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail), szThumbFolderEndorsements);
                        mfbii.ToHtml(tw, szThumbFolderEndorsements);
                        mfbii.UnCache();
                    }

                    // Write out any digital endorsements too
                    IEnumerable<Endorsement> rgEndorsements = Endorsement.EndorsementsForUser(User.UserName, null);
                    if (rgEndorsements.Count() > 0)
                    {
                        using (Page p = new FormlessPage())
                        {
                            p.Controls.Add(new HtmlForm());
                            IEndorsementListUpdate el = (IEndorsementListUpdate)p.LoadControl("~/Controls/mfbEndorsement.ascx");
                            foreach (Endorsement en in rgEndorsements)
                            {
                                el.SetEndorsement(en);
                                try { ((UserControl)el).RenderControl(tw); }
                                catch { }  // don't write bogus or incomplete HTML
                            }
                        }
                    }

                    // And any BasicMed stuff
                    IEnumerable<BasicMedEvent> lstBMed = BasicMedEvent.EventsForUser(User.UserName);
                    foreach (BasicMedEvent bme in lstBMed)
                    {
                        string szZipFolder = String.Format(CultureInfo.InvariantCulture, "{0}-{1}", bme.ImageKey, szThumbFolderBasicMed);
                        ImageList ilBasicMed = new ImageList(MFBImageInfo.ImageClass.BasicMed, bme.ImageKey);
                        ilBasicMed.Refresh(true);
                        foreach (MFBImageInfo mfbii in ilBasicMed.ImageArray)
                        {
                            zip.AddFile(System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail), szZipFolder);
                            mfbii.ToHtml(tw, szZipFolder);
                            mfbii.UnCache();
                        }
                    }

                    // Write out flight images
                    tw.RenderBeginTag(HtmlTextWriterTag.H1);
                    tw.Write(HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ImagesBackupFlightsHeader, User.UserFullName)));
                    tw.RenderEndTag();  // H1

                    // We'll get images from the DB rather than slamming the disk
                    // this is a bit of a hack, but limits our queries
                    const string szQ = @"SELECT f.idflight, img.*
            FROM Images img INNER JOIN flights f ON f.idflight=img.ImageKey
            WHERE f.username=?user AND img.VirtPathID=0
            ORDER BY f.Date desc, f.idFlight desc";
                    DBHelper dbhImages = new DBHelper(szQ);
                    Dictionary<int, List<MFBImageInfo>> dImages = new Dictionary<int, List<MFBImageInfo>>();
                    dbhImages.ReadRows((comm) => { comm.Parameters.AddWithValue("user", User.UserName); },
                        (dr) =>
                        {
                            int idFlight = Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture);
                            List<MFBImageInfo> lstMFBii;
                            if (dImages.ContainsKey(idFlight))
                                lstMFBii = dImages[idFlight];
                            else
                                dImages[idFlight] = lstMFBii = new List<MFBImageInfo>();
                            lstMFBii.Add(MFBImageInfo.ImageFromDBRow(dr));
                        });

                    // Get all of the user's flights, including telemetry
                    const int PageSize = 200;   // get 200 flights at a time.
                    int offset = 0;
                    int iRow = 0;
                    bool fCouldBeMore = true;

                    while (fCouldBeMore)
                    {
                        FlightQuery fq = new FlightQuery(User.UserName);
                        DBHelper dbhFlights = new DBHelper(LogbookEntry.QueryCommand(fq, offset, PageSize, true, LogbookEntry.LoadTelemetryOption.LoadAll));
                        dbhFlights.ReadRows((comm) => { },
                            (dr) =>
                            {
                                LogbookEntry le = new LogbookEntry(dr, User.UserName, LogbookEntry.LoadTelemetryOption.LoadAll);
                                le.FlightImages = (dImages.ContainsKey(le.FlightID)) ? dImages[le.FlightID].ToArray() : new MFBImageInfo[0];

                            // skip any flights here that don't have images, videos, or telemetry
                            if (le.FlightImages.Length > 0 || le.Videos.Count() > 0 || le.HasFlightData)
                                    WriteFlightInfo(tw, zip, le);
                                iRow++;
                            });
                        if (fCouldBeMore = (iRow == offset + PageSize))
                            offset += PageSize;
                    }

                    tw.RenderEndTag();  // Body
                    tw.RenderEndTag();  // Html
                }

                zip.AddEntry(Branding.ReBrand(String.Format(CultureInfo.InvariantCulture, "{0}.htm", Resources.LocalizedText.ImagesBackupFilename), activeBrand), sw.ToString());
                zip.Save(ms);
            }

            return ms;
        }

        public byte[] LogbookDataForBackup()
        {
            using (Page p = new FormlessPage())
            {
                p.Controls.Add(new HtmlForm());
                IDownloadableAsData ifr = (IDownloadableAsData)p.LoadControl("~/Controls/mfbDownload.ascx");
                return ifr.RawData(User.UserName);
            }
        }

        /// <summary>
        /// The name for the backup file
        /// </summary>
        public string BackupFilename(Brand activeBrand)
        {
            if (activeBrand == null)
                throw new ArgumentNullException("activeBrand");
            string szBaseName = String.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}", activeBrand.AppName, User.UserFullName, User.OverwriteCloudBackup ? string.Empty : String.Format(CultureInfo.InvariantCulture, "-{0}", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Replace(" ", "-");
            return String.Format(CultureInfo.InvariantCulture, "{0}.csv", System.Text.RegularExpressions.Regex.Replace(szBaseName, "[^0-9a-zA-Z-]", ""));
        }

        /// <summary>
        /// The name for the images file
        /// </summary>
        public string BackupImagesFilename(Brand activeBrand, bool fDateStamp = false)
        {
            if (activeBrand == null)
                throw new ArgumentNullException("activeBrand");
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

            using (MemoryStream ms = ZipOfImagesForUser(activeBrand))
            {
                Dropbox.Api.Files.FileMetadata result = await MFBDropbox.PutFile(User.DropboxAccessToken, ms, BackupImagesFilename(activeBrand));
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

            return await MFBDropbox.PutFile(User.DropboxAccessToken, BackupFilename(activeBrand), LogbookDataForBackup());
        }
        #endregion

        #region OneDrive
        /// <summary>
        /// Saves a zip of the user's images to OneDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="Microsoft.OneDrive.Sdk.OneDriveException"></exception>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        /// <param name="od">The OneDrive object to use (one will be initialized, if necessary)</param>
        public async Task<Microsoft.OneDrive.Sdk.Item> BackupImagesToOneDrive(OneDrive od = null, Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (User.OneDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredOneDrive);

            if (od == null)
                od = new OneDrive(User.OneDriveAccessToken);

            using (MemoryStream ms = ZipOfImagesForUser(activeBrand))
            {
                return await od.PutFile(ms, BackupImagesFilename(activeBrand));
            }
        }

        /// <summary>
        /// Saves a the user's logbook to OneDrive, if configured.
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        /// <exception cref="Microsoft.OneDrive.Sdk.OneDriveException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <param name="od">The OneDrive object to use (one will be initialized, if necessary)</param>
        /// <param name="activeBrand">The brand to use.  Current brand is used if null.</param>
        public async Task<Microsoft.OneDrive.Sdk.Item> BackupToOneDrive(OneDrive od = null, Brand activeBrand = null)
        {
            if (User.OneDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredOneDrive);

            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (od == null)
                od = new OneDrive(User.OneDriveAccessToken);

            return await od.PutFile(BackupFilename(activeBrand), LogbookDataForBackup());
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
        public async Task<IReadOnlyDictionary<string, string>> BackupImagesToGoogleDrive(GoogleDrive gd, Brand activeBrand = null)
        {
            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            if (gd == null)
                throw new ArgumentNullException("gd");

            if (User.GoogleDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredGoogleDrive);

            using (MemoryStream ms = ZipOfImagesForUser(activeBrand))
            {
                return await gd.PutFile(ms, BackupImagesFilename(activeBrand, true), "application/zip");
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
        public async Task<IReadOnlyDictionary<string, string>> BackupToGoogleDrive(GoogleDrive gd, Brand activeBrand = null)
        {
            if (gd == null)
                throw new ArgumentNullException("gd");

            if (User.GoogleDriveAccessToken == null)
                throw new MyFlightbookException(Resources.Profile.errNotConfiguredGoogleDrive);

            if (activeBrand == null)
                activeBrand = Branding.CurrentBrand;

            return await gd.PutFile(BackupFilename(activeBrand), LogbookDataForBackup(), "text/csv");
        }
        #endregion
        #endregion
    }
}