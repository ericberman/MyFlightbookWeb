using Amazon;
using Amazon.ElasticTranscoder.Model;
using Amazon.S3;
using Amazon.S3.Model;
using AWSNotifications;
using gma.Drawing.ImageInfo;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using ImageMagick;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;


/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Default extensions for common formats, regular expression for determining image vs. video files.
    /// </summary>
    public static class FileExtensions
    {
        public const string JPG = ".jpg";
        public const string JPEG = ".jpeg";
        public const string PDF = ".pdf";
        public const string S3PDF = ".s3pdf";
        public const string MP4 = ".mp4";
        public const string VidInProgress = ".vid";
        public const string RegExpVideoFileExtensions = "(avi|wmv|mp4|mov|m4v|m2p|mpeg|mpg|hdmov|flv|avchd|mpeg4|m2t|h264)$";
        public const string RegExpImageFileExtensions = "(jpg|jpeg|jpe|gif|png|heic)$";
    }

    /// <summary>
    /// MFBImageInfo wraps an image or PDF file.  It has the following characteristics:
    ///  - A thumbnail file and a full-image file.  (For PDFs, this is one and the same)
    ///  - An optional comment
    ///  - An optional latitude/longitude (geotag).
    ///  
    /// The object is defined by a combination of a virtual path (where it is relative to
    /// the root of the application) and a thumbnail filename; the full-filename is derived from
    /// that.  You can get the URL to the thumbnail and to the full filename.
    /// </summary>
    [Serializable]
    public class MFBImageInfo : IComparable
    {
        /// <summary>
        /// What type of image is this?  JPEG or PDF
        /// These can be in database so NEVER MODIFY their values
        /// </summary>
        public enum ImageFileType
        {
            JPEG,           // Image (Jpeg)
            PDF,            // PDF file (Local)
            S3PDF,          // PDF file (on S3)
            S3VideoMP4,     // Video (on S3) - MP4 format
            Unknown = 100   // Placeholder for unknown
        };

        /// <summary>
        /// What class of image is this (Flight, Aircraft, Endorsement, etc.)
        /// </summary>
        public enum ImageClass { Flight, Aircraft, Endorsement, BasicMed, Unknown };

        public const string ThumbnailPrefix = "t_";
        public const string ThumbnailPrefixVideo = "v_";
        internal const string szNewS3KeySuffix = "_";

        public const int ThumbnailWidth = 150;
        public const int ThumbnailHeight = 150;
        public const int MaxImgHeight = 1600;
        public const int MaxImgWidth = 1600;

        private readonly System.Object lockObject = new System.Object();
        private readonly static System.Object videoLockObject = new System.Object();
        private readonly static System.Object idempotencyLock = new System.Object();

        #region Properties
        /// <summary>
        /// Deprecated - do not use, but don't delete for backwards compat
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Deprecated - do not use, but don't delete for backwards compat
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Width of the thumbnail
        /// </summary>
        public int WidthThumbnail { get; set; }

        /// <summary>
        /// Height of the thumbnail
        /// </summary>
        public int HeightThumbnail { get; set; }     // Height of the thumbnail

        /// <summary>
        /// The type of the image
        /// </summary>
        public ImageFileType ImageType { get; set; }

        /// <summary>
        /// Comment for the image
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Get/Set the virtual path for the image.  Just the path (directory)
        /// </summary>
        public string VirtualPath
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0}{1}/", BasePathFromClass(Class), Key); }
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;

                // Hack for backwards compatibility with mobile devices still using virtual path vs. imageclass.
                // Try to parse this into a virtual path and a key
                Regex r = new Regex("(.*/)([^/]+)/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                Match m = r.Match(value);
                if (m.Groups.Count > 2)
                {
                    this.Class = ClassFromVirtPath(m.Groups[1].Value);
                    this.Key = m.Groups[2].Value;
                }
            }
        }

        /// <summary>
        /// Key (i.e., virtual path without the base path).
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string Key { get; protected set; }

        /// <summary>
        /// The image class.  Must be explicitly set
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ImageClass Class { get; protected set; }

        /// <summary>
        /// Get/set the name of the thumbnail file.  No path, just the name of the file.
        /// </summary>
        public string ThumbnailFile { get; set; }

        /// <summary>
        /// Get the filename of the full-size file.  No path, just the name of the file.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string FullImageFile
        {
            get
            {
                try
                {
                    switch (ImageType)
                    {
                        case ImageFileType.JPEG:
                            return ThumbnailFile.Substring(ThumbnailPrefix.Length);
                        case ImageFileType.S3PDF:
                            return ThumbnailFile.Replace(FileExtensions.S3PDF, FileExtensions.PDF);
                        case ImageFileType.S3VideoMP4:
                            return Path.GetFileNameWithoutExtension(ThumbnailFile.Substring(ThumbnailPrefixVideo.Length)) + FileExtensions.MP4;
                        case ImageFileType.PDF:
                        default:
                            return ThumbnailFile;
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new System.InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Bad index in FullImageFile - ImageType={0}, Thumbnail={1}\r\n{2}", ImageType.ToString(), ThumbnailFile ?? string.Empty, ex.Message));
                }
            }
        }

        /// <summary>
        /// The GPS location for the image.  If you change this value, it won't be saved.
        /// </summary>
        public LatLong Location { get; set; }

        /// <summary>
        /// Return a URL to the full image file.  Read-only, but has a setter for web service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public virtual string URLFullImage
        {
            get
            {
                switch (ImageType)
                {
                    default:
                    case ImageFileType.JPEG:
                    case ImageFileType.S3VideoMP4:
                    case ImageFileType.S3PDF:
                        return VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/public/ViewPic.aspx?r={0}&k={1}&t={2}", HttpUtility.UrlEncode(Class.ToString()), HttpUtility.UrlEncode(Key), HttpUtility.UrlEncode(ThumbnailFile)));
                    case ImageFileType.PDF:
                        return VirtualPathUtility.ToAbsolute(PathThumbnail);
                }
            }
            set { }
        }

        /// <summary>
        /// Return a URL to the thumbnail file.  Read-only, but has a setter for web service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public virtual string URLThumbnail
        {
            get
            {
                switch (ImageType)
                {
                    default:
                    case ImageFileType.JPEG:
                        return VirtualPath + ThumbnailFile;
                    case ImageFileType.S3PDF:
                    case ImageFileType.PDF:
                        // Icon used per text of http://www.adobe.com/misc/linking.html
                        return VirtualPathUtility.ToAbsolute("~/Images/pdficon_large.png");
                }
            }
            set { }
        }

        /// <summary>
        /// Returns a URI to the full-sized video thumbnail that's up on S3
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Uri UriS3VideoThumbnail
        {
            get
            {
                switch (ImageType)
                {
                    case ImageFileType.S3VideoMP4:
                        return new Uri(AWSConfiguration.AmazonS3Prefix + VirtualPath.Substring(1) + ThumbnailFile.Replace("_.jpg", "_00001.jpg"));
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Returns a virtual path to the full image.  This may not be usable as a URL to the full image as the file may not actually exist here.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string PathFullImage
        {
            get { return VirtualPath + FullImageFile; }
        }

        /// <summary>
        /// Returns a virtual path to the thumbnail
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string PathThumbnail
        {
            get { return VirtualPath + ThumbnailFile; }
        }

        /// <summary>
        /// The key to use on S3 - has the leading slash if image is in legacy location
        /// </summary>
        internal string S3Key
        {
            get
            {
                string szPath = PathFullImage;
                return !szPath.StartsWith("/", StringComparison.OrdinalIgnoreCase) ? szPath : szPath.Substring(1);
            }
        }

        /// <summary>
        /// Returns a path to the expected location of the full image on S3.  This may not be usable as a URL to the full image as the file may not exist there.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string PathFullImageS3
        {
            get { return AWSConfiguration.AmazonS3Prefix + S3Key; }
        }

        /// <summary>
        /// Gets a mapped path (i.e., c:\...) for the local thumbnail file
        /// </summary>
        internal string PhysicalPathThumbnail
        {
            get { return HostingEnvironment.MapPath(PathThumbnail); }
        }

        /// <summary>
        /// Gets a mapped path (i.e., c:\...) from the local full file.
        /// </summary>
        internal string PhysicalPathFull
        {
            get { return HostingEnvironment.MapPath(PathFullImage); }
        }

        /// <summary>
        /// Returns the mapped physical path for the virtual directory
        /// </summary>
        private string PhysicalPath
        {
            get { return HostingEnvironment.MapPath(VirtualPath); }
        }

        /// <summary>
        /// True if the full file is local.  Note that this is always the case for PDF files.  S3PDF is always non-local, images are local if the physical path exists.
        /// </summary>
        public bool IsLocal
        {
            get { return (this.ImageType == ImageFileType.PDF || (this.ImageType == ImageFileType.JPEG && File.Exists(PhysicalPathFull))); }
        }

        /// <summary>
        /// Returns a unique key for this image for caching
        /// </summary>
        private string CacheKey
        {
            get { return PrimaryKey; }
        }
        #endregion

        #region Static Utility Functions
        /// <summary>
        /// Returns the ratio by which to scale a picture proportionally to fit into particular dimensions
        /// </summary>
        /// <param name="maxHeight">Maximum allowed height</param>
        /// <param name="maxWidth">Maximum allowed width</param>
        /// <param name="Height">Current height</param>
        /// <param name="Width">Current width</param>
        /// <returns>The ratio that, when multiplied by the current height/width, yeilds dimensions that fit into the specified dimensions</returns>
        public static float ResizeRatio(int maxHeight, int maxWidth, int Height, int Width)
        {
            float ratioX = ((float)maxWidth / (float)Width);
            float ratioY = ((float)maxHeight / (float)Height);
            float minRatio = 1.0F;
            if (ratioX < 1.0 || ratioY < 1.0)
            {
                minRatio = (ratioX < ratioY) ? ratioX : ratioY;
            }
            return minRatio;
        }

        /// <summary>
        /// Returns the imagefiletype for the given filename
        /// </summary>
        /// <param name="szName">The filename</param>
        /// <returns>ImageFileType.  Defaults to JPEG if unknown type</returns>
        public static ImageFileType ImageTypeFromName(string szName)
        {
            if (szName == null)
                throw new ArgumentNullException("szName");
            string szExt = Path.GetExtension(szName);
            if (szExt.CompareOrdinalIgnoreCase(FileExtensions.PDF) == 0)
                return ImageFileType.PDF;
            else if (szExt.CompareOrdinalIgnoreCase(FileExtensions.S3PDF) == 0)
                return ImageFileType.S3PDF;
            else if (Regex.IsMatch(szExt, FileExtensions.RegExpImageFileExtensions, RegexOptions.IgnoreCase))
                return szName.StartsWith(ThumbnailPrefixVideo, StringComparison.OrdinalIgnoreCase) ? ImageFileType.S3VideoMP4 : ImageFileType.JPEG;
            else if (Regex.IsMatch(szExt, FileExtensions.RegExpVideoFileExtensions, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))
                return ImageFileType.S3VideoMP4;
            else
                return ImageFileType.Unknown;
        }

        /// <summary>
        /// Returns the best-guess file type from a posted file.  Uses the contenttype, if possible, to identify images; else uses the filename extension
        /// </summary>
        /// <param name="pf">The httpposted file.</param>
        /// <returns>Best guess image type</returns>
        public static ImageFileType ImageTypeFromFile(MFBPostedFile pf)
        {
            if (pf == null)
                throw new ArgumentNullException("pf");
            if (Regex.IsMatch(pf.ContentType, "image/\\S+"))
                return ImageFileType.JPEG;
            else if (Regex.IsMatch(pf.ContentType, "video/\\S+"))
                return ImageFileType.S3VideoMP4;
            else
                return ImageTypeFromName(pf.FileName);
        }

        public static ImageFileType ImageTypeFromFile(HttpPostedFile pf)
        {
            return ImageTypeFromFile(new MFBPostedFile(pf));
        }

        /// <summary>
        /// Sanity check on the image type - adjusts imagetype based on heuristics.  Important for webservice, where imagetype might not be correctly set.
        /// </summary>
        public void FixImageType()
        {
            if (ThumbnailFile.EndsWith(FileExtensions.PDF, StringComparison.OrdinalIgnoreCase))
                ImageType = ImageFileType.PDF;
            else if (ThumbnailFile.EndsWith(FileExtensions.S3PDF, StringComparison.OrdinalIgnoreCase))
                ImageType = ImageFileType.S3PDF;
            else if (ImageType == ImageFileType.JPEG && ThumbnailFile.StartsWith(ThumbnailPrefixVideo, StringComparison.OrdinalIgnoreCase))
                ImageType = ImageFileType.S3VideoMP4;
        }

        /// <summary>
        /// Return the base path (virtual) for a particular class of images
        /// </summary>
        /// <param name="ic">The ImageClass</param>
        /// <returns>Virtual directory, null if not found.</returns>
        public static string BasePathFromClass(ImageClass ic)
        {
            switch (ic)
            {
                default:
                case ImageClass.Unknown:
                    return null;
                case ImageClass.Aircraft:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["AircraftPixDir"].ToString());
                case ImageClass.Endorsement:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["EndorsementsPixDir"].ToString());
                case ImageClass.Flight:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["FlightsPixDir"].ToString());
                case ImageClass.BasicMed:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["BasicMedDir"].ToString());
            }
        }

        /// <summary>
        /// Tries to extract the imageclass from a virtual path - required for legacy support
        /// </summary>
        /// <param name="szVirtPath">The virtual path</param>
        /// <returns>The best-guess image class</returns>
        public static ImageClass ClassFromVirtPath(string szVirtPath)
        {
            if (String.IsNullOrEmpty(szVirtPath))
                return ImageClass.Unknown;

            if (szVirtPath.EndsWith("//", StringComparison.OrdinalIgnoreCase) && szVirtPath.Length > 2)
                szVirtPath = szVirtPath.Substring(0, szVirtPath.Length - 1);
            szVirtPath = szVirtPath.ToUpperInvariant();

            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["AircraftPixDir"].ToString()), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Aircraft;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["FlightsPixDir"].ToString()), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Flight;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["EndorsementsPixDir"].ToString()), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Endorsement;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["BasicMedDir"].ToString()), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.BasicMed;

            // No exact match - try looking for other clues
            if (szVirtPath.Contains("AIRCRAFT"))
                return ImageClass.Aircraft;
            if (szVirtPath.Contains("FLIGHTS"))
                return ImageClass.Flight;
            if (szVirtPath.Contains("ENDORSEMENTS"))
                return ImageClass.Endorsement;

            return ImageClass.Unknown;

        }
        #endregion

        /// <summary>
        /// Initialize the MFBImageInfo object from its associated file.
        /// </summary>
        /// <returns>The updated object</returns>
        private void InitFromFile()
        {
            ImageType = ImageTypeFromName(ThumbnailFile);
            Info inf = null;

            try
            {
                switch (ImageType)
                {
                    case ImageFileType.S3VideoMP4:
                    case ImageFileType.JPEG:
                        // Get the thumbnail dimensions
                        inf = new Info(PhysicalPathThumbnail);
                        WidthThumbnail = inf.Image.Width;
                        HeightThumbnail = inf.Image.Height;
                        Comment = inf.ImageDescription;

                        if (ImageType == ImageFileType.JPEG)
                        {
                            // If it's local, the thumbnail may not have the exif metadata we need, so switch out the inf for the full file; 
                            // if the full image is on S3, then we can just continue using the existing thumbnail inf. 
                            if (IsLocal)
                            {
                                inf.Image.Dispose();
                                inf = new Info(PhysicalPathFull);
                            }

                            if (inf.HasGeotag)
                            {
                                Location = new LatLong(inf.Latitude, inf.Longitude);
                                if (!Location.IsValid)
                                    Location = null;
                            }
                        }
                        break;
                    case ImageFileType.PDF:
                    case ImageFileType.S3PDF:
                        WidthThumbnail = HeightThumbnail = 100;
                        Comment = FullImageFile;
                        if (ImageType == ImageFileType.S3PDF)
                        {
                            using (StreamReader sr = File.OpenText(PhysicalPathThumbnail))
                            {
                                Comment = sr.ReadToEnd();
                            }
                            if (String.IsNullOrEmpty(Comment))
                                Comment = ThumbnailFile.Replace(FileExtensions.S3PDF, String.Empty);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (IOException ex)
            {
                MyFlightbookException.NotifyAdminException(ex);
            }
            finally
            {
                if (inf != null)
                    inf.Image.Dispose();
            }
        }

        #region IComparable
        /// <summary>
        /// Sorts MFBImageInfo objects.  Puts images before all other documents, subsorts by filename (== timestamp for images)
        /// </summary>
        /// <param name="o">Object being compared to.</param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            MFBImageInfo mfbii = (MFBImageInfo)obj;

            if (this.ImageType == mfbii.ImageType)
                return this.FullImageFile.CompareOrdinalIgnoreCase(mfbii.FullImageFile);
            else
                return this.ImageType == ImageFileType.JPEG ? -1 : 1;
        }
        #endregion

        /// <summary>
        /// Resolves a link to the full image usable by a browser (assumes Key/Class/Thumb are all set)
        /// </summary>
        /// <returns></returns>
        public string ResolveFullImage()
        {
            return (IsLocal) ? PathFullImage : PathFullImageS3;
        }

        /// <summary>
        /// Updates the EXIF annotation for a given image file.  Ultimately rewrites the file.
        /// </summary>
        /// <param name="szFile">The physical filename (mapped to filesystem!) of the file to edit</param>
        private void UpdateImageFile(string szFile)
        {
            string szTempFile = szFile.Substring(0, szFile.Length - 4) + " (temp)" + FileExtensions.JPG;
            Info inf = null;
            try
            {
                inf = new Info(szFile) { ImageDescription = Comment };

                if (Location != null && Location.IsValid)
                {
                    inf.Latitude = Location.Latitude;
                    inf.Longitude = Location.Longitude;
                }

                inf.Image.Save(szTempFile, ImageFormat.Jpeg);
                inf.Image.Dispose();

                File.Delete(szFile);
                File.Copy(szTempFile, szFile);
                File.Delete(szTempFile);
            }
            catch (IOException ex)
            {
                MyFlightbookException.NotifyAdminException(ex);
                throw;
            }
            catch (Exception ex)
            {
                util.NotifyAdminEvent("Error editing image", 
                    String.Format(CultureInfo.InvariantCulture, "{0}\r\n\r\nLat/Lon:{1}\r\n\r\n{2}\r\n\r\n{3}", 
                        ex.Message,
                        Location == null ? "n/a" : Location.ToDegMinSecString(),
                        this.ToString(),
                        ex.StackTrace), 
                    ProfileRoles.maskSiteAdminOnly);
                throw;
            }
            finally
            {
                if (inf != null && inf.Image != null)
                    inf.Image.Dispose();
            }
        }

        internal void Update()
        {
            UpdateImageFile(PhysicalPathThumbnail);
            if (IsLocal)
                UpdateImageFile(PhysicalPathFull);

            UnCache();
            ToDB();
        }

        public void UnCache()
        {
            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache.Remove(CacheKey);
        }

        /// <summary>
        /// Re-saves the image contained in the given file and updates the image description with the supplied text.
        /// </summary>
        /// <param name="szText">Text of the annotation</param>
        public virtual void UpdateAnnotation(string szText)
        {
            Comment = szText;
            Update();
        }

        /// <summary>
        /// Deletes this image file
        /// </summary>
        public virtual void DeleteImage()
        {
            lock (lockObject)   // Don't delete the object if it is still being migrated to S3 (which can happen asynchronously)
            {
                try
                {
                    // Delete the thumbnail, if it exists
                    string szThumb = PhysicalPathThumbnail;
                    if (szThumb.Length > 0 && File.Exists(szThumb))
                        File.Delete(szThumb);

                    if (!String.IsNullOrEmpty(PathFullImage))
                    {
                        string szLocalPath = PhysicalPathFull;
                        if (IsLocal)
                            File.Delete(szLocalPath);
                        else
                            new AWSS3ImageManager().DeleteImageOnS3(this);
                    }
                }
                catch (Exception ex)
                {
                    util.NotifyAdminEvent("Exception Deleting image", String.Format(CultureInfo.InvariantCulture, "{0}: {1}\r\n\r\n{2}\r\n\r\n at {3}", this.PhysicalPathThumbnail, ex.Message, ex.StackTrace, Environment.StackTrace), ProfileRoles.maskSiteAdminOnly);
                    throw;
                }
                finally
                {
                    UnCache();

                    // ALWAYS delete from the DB to avoid orphans; this is no-op if it doesn't exist.
                    DeleteFromDB();
                }
            }
        }

        /// <summary>
        /// Move the image from one key to another
        /// </summary>
        /// <param name="szPathDest">The new key</param>
        public void MoveImage(string szKeyNew)
        {
            string s3KeyOld = S3Key;
            string szVirtPathOld = VirtualPath;
            bool isLocalOld = IsLocal;

            DeleteFromDB();

            Key = szKeyNew;

            string szDirSrc = HostingEnvironment.MapPath(szVirtPathOld);
            string szDirDest = HostingEnvironment.MapPath(VirtualPath);

            DirectoryInfo diSrc = new DirectoryInfo(szDirSrc);
            DirectoryInfo diDest = new DirectoryInfo(szDirDest);
            if (!diDest.Exists)
                diDest.Create();

            FileInfo[] rgfi;

            // Move the full image before the thumbnail
            if (isLocalOld)
            {
                rgfi = diSrc.GetFiles(FullImageFile);
                if (rgfi != null && rgfi.Length > 0)
                    rgfi[0].MoveTo(szDirDest + FullImageFile);
            }
            else
                new AWSS3ImageManager().MoveImageOnS3(s3KeyOld, S3Key);

            rgfi = diSrc.GetFiles(ThumbnailFile);
            if (rgfi != null && rgfi.Length > 0)
                rgfi[0].MoveTo(szDirDest + ThumbnailFile);

            ToDB();
        }

        #region pseudo-Idempotency
        /// <summary>
        /// Determines if two files are byte-for-byte the same.
        /// Thanks for the hash pointer to http://www.daveoncsharp.com/2009/07/file-comparison-in-csharp-part-3/
        /// </summary>
        /// <param name="fi1">The first file spec</param>
        /// <param name="fi2">The 2nd file spec</param>
        /// <returns>true if they are the same</returns>
        public static bool CompareImageFiles(FileInfo fi1, FileInfo fi2)
        {
            if (fi1 == null || fi2 == null)
                return false;

            // quick check - if lengths don't match
            if (fi1.Length != fi2.Length)
                return false;

            // Declare byte arrays to store our file hashes
            byte[] fileHash1;
            byte[] fileHash2;

            // Create an instance of System.Security.Cryptography.HashAlgorithm
            using (HashAlgorithm hash = HashAlgorithm.Create())
            {
                // Open a System.IO.FileStream for each file.
                // Note: With the 'using' keyword the streams 
                // are closed automatically.
                using (FileStream fileStream1 = fi1.OpenRead(),
                                    fileStream2 = fi2.OpenRead())
                {
                    // Compute file hashes
                    fileHash1 = hash.ComputeHash(fileStream1);
                    fileHash2 = hash.ComputeHash(fileStream2);
                }
            }

            return BitConverter.ToString(fileHash1) == BitConverter.ToString(fileHash2);
        }

        /// <summary>
        /// Deletes this image if it is a dupe of another file in the folder.
        /// RUNS ASYNC
        /// </summary>
        public void IdempotencyCheck()
        {
            DirectoryInfo dir = new DirectoryInfo(PhysicalPath);
            if (dir != null && dir.Exists)
            {
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(1000); // wait a second for any pending operations to finish.
                    lock (idempotencyLock)  // Only allow one object to do an idempotency check at a time.
                    {
                        FileInfo[] rgfiThis = dir.GetFiles(ThumbnailFile);
                        FileInfo[] rgfi = dir.GetFiles();

                        // rgfiThis should have exactly one file - this one
                        if (rgfiThis == null || rgfiThis.Length != 1)
                            return;

                        FileInfo fiThis = rgfiThis[0];

                        foreach (FileInfo fi in rgfi)
                        {
                            // skip full-image files, skip this file
                            if (String.Compare(fi.Name, ThumbnailFile) == 0)
                                continue;
                            if (this.ImageType == ImageFileType.JPEG && !fi.Name.StartsWith(ThumbnailPrefix))
                                continue;

                            // test to see if the bytes are the same
                            if (CompareImageFiles(fi, fiThis))
                            {
                                DeleteImage();
                                return; // we're done - no need to compare against more images.
                            }
                        }
                    }
                })).Start();
            }
        }
        #endregion

        #region DB Support
        /// <summary>
        /// Are we using the DB for images?
        /// </summary>
        /// <returns>True if we are using the DB</returns>
        private static bool UseDB()
        {
            return (String.Compare(ConfigurationManager.AppSettings["UseImageDB"].ToString(), "yes", StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Remove the image from the DB
        /// </summary>
        public void DeleteFromDB()
        {
            string szDelete = @"DELETE FROM images WHERE VirtPathID=?virtPathID AND ImageKey=?imagekey AND ThumbFilename=?thumbFile";
            DBHelper dbh = new DBHelper(szDelete);
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("virtPathID", (int)this.Class);
                    comm.Parameters.AddWithValue("imagekey", Key);
                    comm.Parameters.AddWithValue("thumbFile", ThumbnailFile);
                });
        }

        /// <summary>
        /// Updates (inserting, if necessary) the DB entry for this image.  No-op if UseDB is false.
        /// </summary>
        public void ToDB()
        {
            if (!UseDB())
                return;

            if (this.Class == ImageClass.Unknown)
                throw new MyFlightbookException("Attempting to save an image with unknown imageclass!");

            if (String.IsNullOrEmpty(ThumbnailFile))
                throw new MyFlightbookException("Attempting to save an image with no thumbnail!");

            string szUpdate = @"REPLACE INTO images
                        SET
                        VirtPathID=?virtPathID,
                        ImageKey=?imagekey,
                        ThumbFilename=?thumbFile,
                        ThumbWidth=?thumbWidth,
                        ThumbHeight=?thumbHeight,
                        imageType=?imageType,
                        Comment=?comment,
                        Latitude=?latitude,
                        Longitude=?longitude,
                        IsLocal=?islocal
                        ";

            DBHelper dbh = new DBHelper(szUpdate);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("virtPathID", (int)this.Class);
                comm.Parameters.AddWithValue("imagekey", Key);
                comm.Parameters.AddWithValue("thumbFile", ThumbnailFile);
                comm.Parameters.AddWithValue("imageType", ImageType);
                comm.Parameters.AddWithValue("comment", Comment.Length > 255 ? Comment.Substring(0, 254) : Comment);
                comm.Parameters.AddWithValue("thumbWidth", WidthThumbnail);
                comm.Parameters.AddWithValue("thumbHeight", HeightThumbnail);
                comm.Parameters.AddWithValue("islocal", IsLocal);

                if (Location == null || !Location.IsValid)
                {
                    comm.Parameters.AddWithValue("latitude", 0);
                    comm.Parameters.AddWithValue("longitude", 0);
                }
                else
                {
                    comm.Parameters.AddWithValue("latitude", Location.Latitude);
                    comm.Parameters.AddWithValue("longitude", Location.Longitude);
                }
            });
        }

        /// <summary>
        /// Update the local/S3 flag on the entry in the images table
        /// </summary>
        /// <param name="isLocal">true for local, false for remote (on S3)</param>
        internal void UpdateDBLocation(bool isLocal)
        {
            string szUpdate = @"UPDATE images
                        SET IsLocal=?islocal
                        WHERE VirtPathID=?virtPathID AND ImageKey=?imagekey AND ThumbFilename=?thumbFile";

            DBHelper dbh = new DBHelper(szUpdate);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("virtPathID", (int)this.Class);
                comm.Parameters.AddWithValue("imagekey", Key);
                comm.Parameters.AddWithValue("thumbFile", ThumbnailFile);
                comm.Parameters.AddWithValue("islocal", isLocal);
            });
        }

        /// <summary>
        /// Updates the thumbnail file in the db and on the disk
        /// </summary>
        /// <param name="szNewThumbnailName"></param>
        internal void RenameLocalFile(string szNewThumbnailName)
        {
            string szUpdate = @"UPDATE images SET ThumbFilename=?newThumbFile, imageType=?imageType WHERE VirtPathID=?virtPathID AND ImageKey=?imagekey AND ThumbFilename=?oldThumbFile";
            string szCurThumb = ThumbnailFile;
            DBHelper dbh = new DBHelper(szUpdate);
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("imageType", ImageType);
                    comm.Parameters.AddWithValue("virtPathID", (int)this.Class);
                    comm.Parameters.AddWithValue("imagekey", Key);
                    comm.Parameters.AddWithValue("oldThumbFile", szCurThumb);
                    comm.Parameters.AddWithValue("newThumbFile", szNewThumbnailName);
                });
            string szPathOrig = PhysicalPathThumbnail;
            ThumbnailFile = szNewThumbnailName;
            File.Move(szPathOrig, PhysicalPathThumbnail);
        }

        /// <summary>
        /// Initializes the image from a datareader
        /// </summary>
        /// <param name="dr">The datareader</param>
        /// <returns>The NEWLY ALLOCATED image</returns>
        static public MFBImageInfo ImageFromDBRow(MySql.Data.MySqlClient.MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");

            MFBImageInfo.ImageClass ic = (MFBImageInfo.ImageClass)Convert.ToInt32(dr["VirtPathID"], CultureInfo.InvariantCulture);
            string szKey = (string)dr["ImageKey"];

            MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, (string)dr["ThumbFilename"]);
            try
            {
                double lat = Convert.ToDouble(dr["Latitude"], CultureInfo.InvariantCulture);
                double lon = Convert.ToDouble(dr["Longitude"], CultureInfo.InvariantCulture);
                if (lat != 0 && lon != 0)
                    mfbii.Location = new LatLong(lat, lon);
            }
            catch (FormatException) { }
            mfbii.Comment = (string)dr["Comment"];
            mfbii.WidthThumbnail = Convert.ToInt32(dr["ThumbWidth"], CultureInfo.InvariantCulture);
            mfbii.HeightThumbnail = Convert.ToInt32(dr["ThumbHeight"], CultureInfo.InvariantCulture);
            mfbii.ImageType = (MFBImageInfo.ImageFileType)Convert.ToInt32(dr["ImageType"], CultureInfo.InvariantCulture);
            return mfbii;
        }

        /// <summary>
        /// Primary key for the object - useful as a hash tag
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string PrimaryKey
        {
            get { return MFBImageInfo.PrimaryKeyForValues(this.Class, this.Key, this.ThumbnailFile); }
        }

        /// <summary>
        /// And a utility function to create a hash tag (used for sync w/S3.)
        /// </summary>
        /// <param name="ic"></param>
        /// <param name="szKey"></param>
        /// <param name="szThumbnail"></param>
        /// <returns></returns>
        internal static string PrimaryKeyForValues(ImageClass ic, string szKey, string szThumbnail)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", ic, szKey, szThumbnail);
        }

        /// <summary>
        /// Returns a dictionary of all of the images of a given class, key'd by their primary key (string)
        /// </summary>
        /// <param name="ic">The image class</param>
        /// <returns>The dictionary </returns>
        public static Dictionary<string, MFBImageInfo> AllImagesForClass(MFBImageInfo.ImageClass ic)
        {
            List<MFBImageInfo> lst = FromDB(ic, String.Empty);
            Dictionary<string, MFBImageInfo> d = new Dictionary<string, MFBImageInfo>();
            foreach (MFBImageInfo mfbii in lst)
                d[mfbii.PrimaryKey] = mfbii;
            return d;
        }

        /// <summary>
        /// Returns a dictionary of image lists from the DB matching the specified class.
        /// </summary>
        /// <param name="count">The maximum number of images sets to return.  Note that this is not the number of IMAGES but the number of SETS.</param>
        /// <param name="offset">The offset to the first set of images to return.</param>
        /// <param name="ic">The image class</param>
        /// <param name="lstKeys">The sorted list of keys</param>
        /// <returns>Matching results.  Index by key, each key returns a list of images</returns>
        static public Dictionary<string, List<MFBImageInfo>> FromDB(MFBImageInfo.ImageClass ic, int offset, int count, out List<string> lstKeys)
        {
            if (offset < 0 || count <= 0)
            {
                lstKeys = null;
                return FromDB(ic);
            }

            // This one gets tricky because we don't want count images, we want count keys.
            string szSort;
            string sz2ndSort;
            switch (ic)
            {
                case ImageClass.Aircraft:
                case ImageClass.Flight:
                    szSort = " ORDER BY (ImageKey * 1) DESC ";
                    sz2ndSort = " ORDER BY ImageKey DESC ";
                    break;
                default:
                    szSort = " ORDER BY ImageKey ASC ";
                    sz2ndSort = " ORDER BY ImageKey ASC ";
                    break;
            }

            Dictionary<string, List<MFBImageInfo>> dictResults = new Dictionary<string, List<MFBImageInfo>>();

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT i1.* FROM images i1 INNER JOIN (SELECT DISTINCT ImageKey FROM images WHERE virtPathID={0} {1} LIMIT {2},{3}) AS ik ON i1.ImageKey=ik.ImageKey AND i1.virtPathID={0} {4}", (int)ic, szSort, offset, count, sz2ndSort));

            string szKeyCurrent = string.Empty;
            List<string> keyList = new List<string>();
            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    MFBImageInfo mfbii = ImageFromDBRow(dr);

                    List<MFBImageInfo> lst;

                    if (dictResults.ContainsKey(mfbii.Key))
                        lst = dictResults[mfbii.Key];
                    else
                        dictResults[mfbii.Key] = lst = new List<MFBImageInfo>();
                    lst.Add(mfbii);

                    if (String.Compare(szKeyCurrent, mfbii.Key) != 0 && keyList != null)
                        keyList.Add(szKeyCurrent = mfbii.Key);
                });

            lstKeys = keyList;
            return dictResults;
        }

        /// <summary>
        /// Returns a dictionary of image lists from the DB matching the specified class; dictioanry key = image key.  This returns ALL images in the class
        /// </summary>
        /// <param name="ic">The image class</param>
        /// <returns>Matching results.  Index by key, each key returns a list of images</returns>
        static public Dictionary<string, List<MFBImageInfo>> FromDB(MFBImageInfo.ImageClass ic)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM images WHERE virtPathID={0}", (int)ic));

            Dictionary<string, List<MFBImageInfo>> dictResults = new Dictionary<string, List<MFBImageInfo>>();

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    MFBImageInfo mfbii = ImageFromDBRow(dr);
                    List<MFBImageInfo> lst;

                    if (dictResults.ContainsKey(mfbii.Key))
                        lst = dictResults[mfbii.Key];
                    else
                    {
                        lst = new List<MFBImageInfo>();
                        dictResults[mfbii.Key] = lst;
                    }
                    lst.Add(mfbii);
                });

            return dictResults;
        }

        /// <summary>
        /// Returns a dictionary of images from the DB matching the specified class and keys - integer only (i.e., for aircraft/flights).
        /// </summary>
        /// <param name="ic">The image class</param>
        /// <param name="lstKeys">List of integer keys</param>
        /// <returns>Matching results.  Index by key, each key returns a list of images</returns>
        static public Dictionary<int, List<MFBImageInfo>> FromDB(MFBImageInfo.ImageClass ic, List<int> lstKeys)
        {
            List<string> lstKeyStrings = new List<string>();
            foreach (int key in lstKeys)
                lstKeyStrings.Add(key.ToString());

            string szIDs = String.Join("', '", lstKeyStrings.ToArray());
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM images WHERE virtPathID={0} AND ImageKey IN ('{1}')", (int)ic, szIDs));

            Dictionary<int, List<MFBImageInfo>> dictResults = new Dictionary<int, List<MFBImageInfo>>();

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    MFBImageInfo mfbii = ImageFromDBRow(dr);
                    int iKey = Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture);
                    List<MFBImageInfo> lst;

                    if (dictResults.ContainsKey(iKey))
                        lst = dictResults[iKey];
                    else
                    {
                        lst = new List<MFBImageInfo>();
                        dictResults[iKey] = lst;
                    }
                    lst.Add(mfbii);
                });

            return dictResults;
        }

        /// <summary>
        /// Returns the images that match a specified key and imageclass
        /// </summary>
        /// <param name="ic">The imageclass</param>
        /// <param name="szKey">The key to match</param>
        /// <returns>Matching results from the DB</returns>
        static public List<MFBImageInfo> FromDB(MFBImageInfo.ImageClass ic, string szKey)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, String.IsNullOrEmpty(szKey) ?
                "SELECT * FROM images WHERE virtPathID={0}" :
                "SELECT * FROM images WHERE virtPathID={0} AND ImageKey LIKE ?szkey", (int)ic));

            List<MFBImageInfo> lstResult = new List<MFBImageInfo>();

            dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("szkey", szKey);
                },
                (dr) =>
                {
                    lstResult.Add(ImageFromDBRow(dr));
                });

            return lstResult;
        }

        /// <summary>
        /// Initializes from a simple serialized form.  Each image is separated by \r\n, and each field by \t.
        /// Order of tabbed data is:
        ///  - Thumbnail file
        ///  - Image Type
        ///  - Latitude
        ///  - Longitude
        ///  - Local (0/1)
        ///  - ThumbWidth
        ///  - ThumbHeight
        ///  - Comment
        ///  
        /// The key/imageclass is inferred from this ImageList
        /// </summary>
        /// <param name="szSerialization">The serialized objects</param>
        /*
        public static List<MFBImageInfo> Deserialize(ImageClass ic, string szKey, string szSerialization)
        {
            if (szKey == null)
                throw new ArgumentNullException("szKey");
            if (szSerialization == null)
                throw new ArgumentNullException("szSerialization");
            List<MFBImageInfo> lst = new List<MFBImageInfo>();

            string[] rgRows = szSerialization.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string szRow in rgRows)
            {
                string[] szCols = szRow.Split(new char[] { '\t' }, StringSplitOptions.None);
                string szThumb = szCols[0];
                MFBImageInfo.ImageFileType imageType = (MFBImageInfo.ImageFileType)Convert.ToInt32(szCols[1], CultureInfo.InvariantCulture);
                double lat = Convert.ToDouble(szCols[2], CultureInfo.InvariantCulture);
                double lon = Convert.ToDouble(szCols[3], CultureInfo.InvariantCulture);
                int thWidth = Convert.ToInt32(szCols[4], CultureInfo.InvariantCulture);
                int thHeight = Convert.ToInt32(szCols[5], CultureInfo.InvariantCulture);
                string szComment = szCols[7];

                MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, szThumb);
                if (lat != 0 && lon != 0)
                    mfbii.Location = new LatLong(lat, lon);
                mfbii.Comment = szComment;
                mfbii.ImageType = imageType;
                mfbii.WidthThumbnail = thWidth;
                mfbii.HeightThumbnail = thHeight;
                lst.Add(mfbii);
            }

            return lst;
        }
         * */
        #endregion

        #region Constructors
        public MFBImageInfo() : this(ImageClass.Unknown, string.Empty) { }

        private MFBImageInfo(ImageClass ic, string szKey)
        {
            Comment = VirtualPath = ThumbnailFile = String.Empty;
            Class = ic;
            Key = szKey;
        }

        /// <summary>
        /// Creates an MFBImageInfo object for the specified file
        /// </summary>
        /// <param name="szThumbnail">The filename of the *thumbnail* for the file</param>
        public MFBImageInfo(ImageClass ic, string szKey, string szThumbnail) : this(ic, szKey)
        {
            if (szThumbnail == null)
                throw new ArgumentNullException("szThumbnail");
            ThumbnailFile = szThumbnail;
            if (szThumbnail.StartsWith(ThumbnailPrefixVideo, StringComparison.OrdinalIgnoreCase))
                ImageType = ImageFileType.S3VideoMP4;
            else if (Path.GetExtension(szThumbnail).CompareCurrentCultureIgnoreCase(FileExtensions.PDF) == 0)
                ImageType = ImageFileType.PDF;
            else if (Path.GetExtension(szThumbnail).CompareCurrentCultureIgnoreCase(FileExtensions.S3PDF) == 0)
                ImageType = ImageFileType.S3PDF;
        }

        public MFBImageInfo(ImageClass ic, string szKey, HttpPostedFile myFile, string szComment, LatLong ll) : this(ic, szKey)
        {
            InitWithFile(new MFBPostedFile(myFile), szComment, ll);
        }

        /// <summary>
        /// Uploads a picture into the specified directory/subdirectory ("key").
        /// </summary>
        /// <param name="myFile">The uploaded file object</param>
        /// <param name="szVirtPath">The virtual path to use for this image</param>
        /// <param name="szComment">Any comment that the user may have typed</param>
        public MFBImageInfo(ImageClass ic, string szKey, MFBPostedFile myFile, string szComment, LatLong ll) : this(ic, szKey)
        {
            InitWithFile(myFile, szComment, ll);
        }

        /// <summary>
        /// Creates a video object from a completed Amazon elastic transponder notification
        /// </summary>
        /// <param name="notification">Notification of completion from Amazon Elastic Transponder - MUST have a corresponding PendingVideo</param>
        public MFBImageInfo(SNSNotification notification) : this()
        {
            if (notification == null)
                throw new ArgumentNullException("notification");
            AWSETSStateMessage etsNotification = JsonConvert.DeserializeObject<AWSETSStateMessage>(notification.Message);

            lock (videoLockObject)   // avoid re-entrancy due to possible timeouts.
            {
                PendingVideo pv = new PendingVideo(etsNotification.JobId);

                switch (etsNotification.JobState)
                {
                    default:
                    case AWSETSStateMessage.ETSStates.PROGRESSING:
                        break;
                    case AWSETSStateMessage.ETSStates.WARNING:
                    case AWSETSStateMessage.ETSStates.ERROR:
                        util.NotifyAdminEvent("Error from Elastic Transcoder", JsonConvert.SerializeObject(notification), ProfileRoles.maskSiteAdminOnly);
                        break;
                    case AWSETSStateMessage.ETSStates.COMPLETED:
                        if (!String.IsNullOrEmpty(pv.JobID))
                        {
                            Class = pv.Class;
                            Key = pv.Key;

                            DirectoryInfo di = new DirectoryInfo(PhysicalPath);
                            if (!di.Exists)
                                di.Create();

                            Comment = pv.Comment;
                            ImageType = ImageFileType.S3VideoMP4;
                            ThumbnailFile = pv.ThumbnailFileName;

                            pv.InitThumbnail(VirtualPath, PhysicalPathThumbnail);
                            WidthThumbnail = pv.ThumbWidth;
                            HeightThumbnail = pv.ThumbHeight;

                            // Then add it to the database
                            ToDB();

                            // finally, delete the pending job
                            pv.Delete();
                        }
                        break;
                }
            }
        }
        #endregion

        /// <summary>
        /// Returns an MFBImageInfo object for the specified virtual path/thumbnail, creating it if needed or loading from the cache
        /// </summary>
        /// <param name="szVirtPath">The virtual path</param>
        /// <param name="szThumbnail">The thumbnail file</param>
        public static MFBImageInfo LoadMFBImageInfo(ImageClass ic, string szKey, string szThumbnail)
        {
            MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, szThumbnail);
            if (HttpRuntime.Cache != null && HttpRuntime.Cache[mfbii.CacheKey] != null)
                return (MFBImageInfo)HttpRuntime.Cache[mfbii.CacheKey];
            else
            {
                mfbii.InitFromFile();
                if (HttpRuntime.Cache != null)
                    HttpRuntime.Cache[mfbii.CacheKey] = mfbii;
                return mfbii;
            }
        }

        /// <summary>
        /// Creates an INF object from the specified image, rotating as needed.
        /// </summary>
        /// <param name="image">The source image</param>
        /// <returns>The Info object</returns>
        public static Info InfoFromImage(System.Drawing.Image image)
        {
            Info inf = new Info(image);
            try
            {
                switch (inf.Orientation)
                {
                    case gma.Drawing.ImageInfo.Orientation.TopLeft: // normal
                        break;
                    case gma.Drawing.ImageInfo.Orientation.TopRight: // Mirror left/right
                        image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.BottomLeft: // 180 rotation
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.BottomRight: // Mirror up/down
                        image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.LeftTop:
                        image.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.RightTop:
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.RightBottom:
                        image.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case gma.Drawing.ImageInfo.Orientation.LeftBottom:
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }

                // Now that the image has been rotated, we do NOT want to flag it as rotated any more.
                inf.Image.RemovePropertyItem((int)PropertyTagId.Orientation);
            }
            catch (InvalidCastException) { }
            catch (ArgumentException) { }
            finally
            {
                // nothing to do...move on if property were was not found.
            }

            return inf;
        }

        /// <summary>
        /// Returns a scaled bitmap from the image, constrained to the specified width/height (proportional)
        /// </summary>
        /// <param name="img">The source image</param>
        /// <param name="maxHeight">Maximum resulting height</param>
        /// <param name="maxWidth">Maximum resulting width</param>
        /// <returns></returns>
        public static Bitmap BitmapFromImage(System.Drawing.Image img, int maxHeight, int maxWidth)
        {
            if (img == null)
                throw new ArgumentNullException("img");
            int originalWidth = img.Width;
            int originalHeight = img.Height;
            float resizeRatio = MFBImageInfo.ResizeRatio(maxHeight, maxWidth, originalHeight, originalWidth);
            int Width = (int)(resizeRatio * (float)originalWidth);
            int Height = (int)(resizeRatio * (float)originalHeight);
            return new Bitmap(img, Width, Height);
        }

        /// <summary>
        /// Returns an Image from the specified stream.  Does NOT take ownership of the stream, and the drawing object MUST be disposed!!!
        /// This will convert HEIC to JPG format, but if it does so, it will put the result in a file; for some reason, System.Drawing.Image.FromStream
        /// doesn't pick up EXIF data, but FromFile does.
        /// </summary>
        /// <param name="s">Input stream</param>
        /// <param name="szTemp">The temp file created if the file had to be converted; must be deleted after image is disposed if not null</param>
        /// <returns>A system.Drawing.Image object</returns>
        public static System.Drawing.Image DrawingCompatibleImageFromStream(Stream s, out string szTemp)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            szTemp = null;
            try
            {
                return System.Drawing.Image.FromStream(s);
            }
            catch (ArgumentException)
            {
                szTemp = Path.GetTempFileName();
                try
                {
                    using (MagickImage image = new MagickImage(s))
                        image.Write(szTemp, MagickFormat.Jpg);

                    return System.Drawing.Image.FromFile(szTemp);
                }
                catch (MagickException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Variant of DrawingcompatibleImageBytesFromStream that does NOT handle conversion from HEIC, so UNSAFE to call (leaves temporary file turds) if conversion is needed
        /// </summary>
        /// <param name="s">The stream</param>
        /// <returns>A system.drawing.image object</returns>
        public static System.Drawing.Image DrawingCompatibleImageFromStream(Stream s)
        {
            string szTempFile;
            System.Drawing.Image result = DrawingCompatibleImageFromStream(s, out szTempFile);
            if (szTempFile != null || File.Exists(szTempFile))
                throw new InvalidOperationException("DrawingCompatibleImageBytesFromStream called on an image that generated a temp file, but without the temp file being cleaned up.");
            return result;
        }

        /// <summary>
        /// Returns a byte array representing JPG using MagickImage - LOSES EXIF DATA and can be slow.
        /// </summary>
        /// <param name="s">The input stream</param>
        /// <returns></returns>
        public static byte[] ConvertStreamToJPG(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            byte[] result = null;
            using (MagickImage image = new MagickImage(s))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Write(ms, MagickFormat.Jpg);
                    result = ms.ToArray();
                }
            }
            return result;
        }

        private void InitWithFile(MFBPostedFile myFile, string szComment, LatLong ll)
        {
            if (myFile == null || myFile.ContentLength == 0 || String.IsNullOrEmpty(Key) || Class == ImageClass.Unknown || VirtualPath.Length <= 1)
                return;

            // Trust image/ content type over filename; otherwise, use filename.
            if ((this.ImageType = ImageTypeFromFile(myFile)) == ImageFileType.Unknown)
                return;

            Comment = szComment.LimitTo(254);

            // see if the subdirectory for this exists
            DirectoryInfo di = new DirectoryInfo(PhysicalPath);
            if (!di.Exists)
                di.Create();

            switch (this.ImageType)
            {
                case ImageFileType.S3VideoMP4:
                    // Copy the file to a temp file so that we can return quickly and don't have to hold open the input stream
                    {
                        string szTemp = Path.GetTempFileName();
                        if (myFile.PostedFile != null)
                            myFile.PostedFile.SaveAs(szTemp);
                        else if (myFile.InputStream != null)
                        {
                            using (var fileStream = File.Create(szTemp))
                            {
                                myFile.InputStream.Seek(0, SeekOrigin.Begin);
                                myFile.InputStream.CopyTo(fileStream);
                            }
                        }
                        else
                            return;
                        string szBucket = AWSConfiguration.CurrentS3Bucket;  // bind this now - in a separate thread (below) it defaults to main, not debug.
                        string szPipelineID = LocalConfig.SettingForKey(AWSConfiguration.UseDebugBucket ? "ETSPipelineIDDebug" : "ETSPipelineID");  // bind this as well, same reason
                        new Thread(new ThreadStart(() => { new AWSS3ImageManager().UploadVideo(szTemp, myFile.ContentType, szBucket, szPipelineID, this); })).Start();
                    }
                    break;
                case ImageFileType.JPEG:
                    {
                        string szTempFile = null;
                        try
                        {
                            // Create a file with a unique name - use current time for uniqueness
                            DateTime dt = DateTime.Now;
                            string szFileName = String.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}{3}", dt.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture), myFile.ContentLength.ToString(), szNewS3KeySuffix, FileExtensions.JPG);
                            ThumbnailFile = MFBImageInfo.ThumbnailPrefix + szFileName;

                            // Resize the image if needed, and then create a thumbnail of it and write that too:
                            using (System.Drawing.Image image = DrawingCompatibleImageFromStream(myFile.InputStream, out szTempFile))
                            {
                                // rotate the image, if necessary
                                Info inf = InfoFromImage(image);

                                // update the location
                                try
                                {
                                    // save the comment
                                    inf.ImageDescription = szComment;

                                    if (ll == null) // no geotag is specified - initialize location from the underlying image.
                                    {
                                        if (inf.HasGeotag)
                                            Location = new LatLong(inf.Latitude, inf.Longitude);
                                        if (!Location.IsValid)
                                            Location = null;
                                    }
                                    else
                                    {
                                        // A specific geotag is provided: geotag the image.
                                        Location = ll;
                                        inf.Latitude = ll.Latitude;
                                        inf.Longitude = ll.Longitude;
                                    }
                                }
                                catch { }

                                if (Location != null && !Location.IsValid)
                                    Location = null;

                                // save the full-sized image
                                Bitmap bmp = BitmapFromImage(inf.Image, MaxImgHeight, MaxImgWidth);
                                Width = bmp.Width;
                                Height = bmp.Height;

                                using (bmp)
                                {
                                    // get all properties of the original image and copy them to the new image.  This should include the annotation (above)
                                    foreach (PropertyItem pi in inf.Image.PropertyItems)
                                        bmp.SetPropertyItem(pi);

                                    string szPathFullImage = PhysicalPathFull;
                                    bmp.Save(szPathFullImage, ImageFormat.Jpeg);
                                }

                                bmp = BitmapFromImage(inf.Image, ThumbnailHeight, ThumbnailWidth);
                                WidthThumbnail = bmp.Width;
                                HeightThumbnail = bmp.Height;
                                using (bmp)
                                {
                                    // copy the properties here too.
                                    foreach (PropertyItem pi in inf.Image.PropertyItems)
                                        bmp.SetPropertyItem(pi);
                                    bmp.Save(PhysicalPathThumbnail, ImageFormat.Jpeg);
                                }

                                // if we got here, everything is hunky-dory.  Cache it!
                                if (HttpRuntime.Cache != null)
                                    HttpRuntime.Cache[CacheKey] = this;

                                inf.Image.Dispose();

                                // Save it in the DB - we do this BEFORE moving to S3 to avoid a race condition
                                // that could arise when MoveImageToS3 attempts to update the record to show that it is no longer local.
                                // We need the record to be present so that the update works.
                                ToDB();

                                // Move this up to S3.  Note that if we're debugging, it will go into the debug bucket.
                                if (AWSConfiguration.UseS3)
                                    new AWSS3ImageManager().MoveImageToS3(false, this);
                            }
                        }
                        catch
                        {
                            // nothing to do here; fail silently.
                        }
                        finally
                        {
                            // clean up a temp file, if one was created; can only do this AFTER the image that used it has been disposed (above).
                            if (!String.IsNullOrEmpty(szTempFile) && File.Exists(szTempFile))
                                File.Delete(szTempFile);
                        }
                    }
                    break;
                case ImageFileType.PDF:
                    // just save the file as-is; we virtualize the thumbanil.
                    string szFilename = Path.GetFileName(myFile.FileName);
                    if (Comment.Length > 0)
                    {
                        string szCleaned = Regex.Replace(Comment, @"[^a-zA-Z0-9]", string.Empty);
                        Regex r = new Regex("[a-zA-Z0-9]+");
                        if (szCleaned.Length > 0 && r.IsMatch(szCleaned))
                            szFilename = szCleaned + FileExtensions.PDF;
                    }
                    else
                        Comment = Path.GetFileNameWithoutExtension(szFilename);

                    szFilename = Path.GetFileNameWithoutExtension(szFilename) + szNewS3KeySuffix + FileExtensions.PDF;

                    ThumbnailFile = szFilename;
                    WidthThumbnail = HeightThumbnail = 100;
                    string szFullPhysicalPath = HostingEnvironment.MapPath(VirtualPath + szFilename);

                    if (myFile.PostedFile != null)
                        myFile.PostedFile.SaveAs(szFullPhysicalPath);
                    else if (myFile.ContentData != null)
                        File.WriteAllBytes(szFullPhysicalPath, myFile.ContentData);

                    // Save it in the DB - do this BEFORE moving to S3 to avoid a race condition (see above).
                    ToDB();

                    // We do this SYNCHRONOUSLY because the name of the thumbnail will change in the process.
                    if (AWSConfiguration.UseS3)
                        new AWSS3ImageManager().MoveImageToS3(true, this);
                    break;
                case ImageFileType.S3PDF:
                    throw new MyFlightbookException("Cannot upload an S3PDF file; this should never happen.  S3PDF files are created from uploaded PDF files");
                default:
                    break;
            }
        }

        #region Misc
        /// <summary>
        /// Renders the image to html, using a thumbnail and linking to the full image
        /// </summary>
        /// <param name="tw">HtmlTextWriter</param>
        /// <param name="szThumbFolder">The thumbnail folder, "thumbs/" if null</param>
        public void ToHtml(HtmlTextWriter tw, string szThumbFolder)
        {
            if (tw == null)
                throw new ArgumentNullException("tw");

            if (szThumbFolder == null)
                szThumbFolder = "thumbs/";

            if (!szThumbFolder.EndsWith("/", StringComparison.Ordinal))
                szThumbFolder += "/";

            tw.AddStyleAttribute(HtmlTextWriterStyle.Display, "inline-block");
            tw.AddStyleAttribute(HtmlTextWriterStyle.Padding, "3px");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute(HtmlTextWriterAttribute.Href, ResolveFullImage());
            tw.RenderBeginTag(HtmlTextWriterTag.A);

            tw.AddAttribute(HtmlTextWriterAttribute.Src, (ImageType == MFBImageInfo.ImageFileType.PDF || ImageType == MFBImageInfo.ImageFileType.S3PDF) ? String.Format(CultureInfo.InvariantCulture, "http://{0}/logbook/images/pdficon_large.png", Branding.CurrentBrand.HostName) : szThumbFolder + ThumbnailFile);
            tw.RenderBeginTag(HtmlTextWriterTag.Img);
            tw.RenderEndTag();  // img
            tw.RenderEndTag();  // a

            if (!String.IsNullOrEmpty(Comment))
            {
                tw.WriteBreak();
                tw.RenderBeginTag(HtmlTextWriterTag.P);
                tw.Write(HttpUtility.HtmlEncode(Comment));
                tw.RenderEndTag();  // p
            }

            tw.RenderEndTag();  // div
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Class={0}, Key={1}, Thumb={2}", Class.ToString(), Key, ThumbnailFile);
        }
        #endregion

        #region Admin tools
        /// <summary>
        /// Note - this requires that the images be in sync
        /// </summary>
        /// <param name="ic">The image class</param>
        /// <returns>A log of changes</returns>
        public static string ADMINDeleteOrphans(ImageClass ic)
        {
            if (ic == ImageClass.Unknown)
                return "Unknown class";

            string szQ = string.Empty;
            switch (ic)
            {
                case ImageClass.Aircraft:
                    szQ = "SELECT DISTINCT(ImageKey) AS idPic FROM images i LEFT JOIN aircraft ac ON i.imagekey=ac.idaircraft WHERE i.VirtPathID=1 AND ac.idaircraft IS NULL;";
                    break;
                case ImageClass.Endorsement:
                    szQ = "SELECT DISTINCT(ImageKey) AS idPic FROM images i LEFT JOIN users u ON i.imagekey=u.username WHERE i.VirtPathID=2 AND u.username IS NULL;";
                    break;
                case ImageClass.Flight:
                    szQ = "SELECT DISTINCT(ImageKey) AS idPic FROM images i LEFT JOIN flights f ON i.imagekey=f.idflight WHERE i.VirtPathID=0 AND f.idflight IS NULL";
                    break;
                case ImageClass.BasicMed:
                    szQ = "SELECT DISTINCT(ImageKey) AS idPic FROM images i LEFT JOIN basicmedevent bme ON i.imagekey=bme.idBasicMedEvent WHERE i.VirtPathID=3 AND bme.idBasicMedEvent IS NULL";
                    break;
            }

            // now find the relevant flights and delete the orphaned images
            StringBuilder sbLog = new StringBuilder();
            string szPixDir = BasePathFromClass(ic);
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    string szIDOrphan = dr["idPic"].ToString();
                    DirectoryInfo dirOrphan = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(String.Format("{0}\\{1}", szPixDir, szIDOrphan)));

                    ImageList il = new ImageList(ic, szIDOrphan.ToString());
                    il.Refresh();
                    foreach (MFBImageInfo mfbii in il.ImageArray)
                    {
                        sbLog.AppendFormat("Deleting: {0} and {1}\r\n<br />", System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail), mfbii.IsLocal ? System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathFullImage) : mfbii.PathFullImageS3);
                        mfbii.DeleteImage();
                    }

                // now delete the containing directory so it doesn't appear again.
                dirOrphan.Delete(true);
                });

            sbLog.Append(dbh.LastError);

            return sbLog.ToString();
        }
        #endregion
    }

    #region Amazon S3 Support
    /// <summary>
    /// Encapsulates configuration information about how MyFlightbook uses Amazon AWS S3
    /// </summary>
    public static class AWSConfiguration
    {
        public const string S3BucketName = "mfbimages";
        public const string S3BucketNameDebug = "mfbdebug";

        #region Properties
        public static bool UseS3 { get { return String.Compare(LocalConfig.SettingForKey("UseAWSS3"), "yes", StringComparison.OrdinalIgnoreCase) == 0; } }

        private static string AWSAccessKey { get { return LocalConfig.SettingForKey("AWSAccessKey"); } }

        private static string AWSSecretKey { get { return LocalConfig.SettingForKey("AWSSecretKey"); } }

        /// <summary>
        /// Should we use the debug bucket?  We do this if the current host name doesn't equal the branding host name.
        /// </summary>
        public static bool UseDebugBucket
        {
            get
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Url != null && HttpContext.Current.Request.Url.Host != null &&
                    Branding.CurrentBrand != null && Branding.CurrentBrand.HostName != null)
                    return !Branding.CurrentBrand.MatchesHost(HttpContext.Current.Request.Url.Host);
                else
                    return false;
            }
        }

        /// <summary>
        /// The name of the current S3 bucket to use.
        /// </summary>
        public static string CurrentS3Bucket
        {
            get { return UseDebugBucket ? S3BucketNameDebug : S3BucketName; }
        }

        /// <summary>
        /// Gets the URL prefix for the physical image.
        /// </summary>
        public static string AmazonS3Prefix
        {
            get { return String.Format(CultureInfo.InvariantCulture, "https://s3.amazonaws.com/{0}/", CurrentS3Bucket); }
        }
        #endregion

        /// <summary>
        /// returns a new Amazon S3 client
        /// </summary>
        /// <returns>The S3 client</returns>
        public static IAmazonS3 S3Client()
        {
           AWSConfigs.S3Config.UseSignatureVersion4 = true;
            return AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey, RegionEndpoint.USEast1);
        }

        public static Amazon.ElasticTranscoder.IAmazonElasticTranscoder ElasticTranscoderClient()
        {
            AWSConfigs.S3Config.UseSignatureVersion4 = true;
            return AWSClientFactory.CreateAmazonElasticTranscoderClient(AWSAccessKey, AWSSecretKey, RegionEndpoint.USEast1);
        }
    }

    /// <summary>
    /// Manages movement of images/videos to/from the S3 server.
    /// </summary>
    public class AWSS3ImageManager
    {
        private const string ContentTypeJPEG = "image/jpeg";
        private const string ContentTypePDF = "application/pdf";
        private readonly object lockObject = new object();

        #region Constructors
        public AWSS3ImageManager() { }
        #endregion

        /// <summary>
        /// Serialize an AmazonS3 exception, including the data dictionary
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns>A serialized exception string.</returns>
        private string WrapAmazonS3Exception(AmazonS3Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            if (ex.Data != null)
            {
                foreach (object key in ex.Data.Keys)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} : {1}\r\n", key.ToString(), ex.Data[key].ToString());
            }
            return String.Format(CultureInfo.InvariantCulture, "Error moving file on S3: Message:{0}\r\nHelp:\r\n{1}\r\nData:\r\n{2}", ex.Message, ex.HelpLink ?? string.Empty, sb.ToString());
        }

        /// <summary>
        /// Moves an image (well, any object, really) from one key to another
        /// </summary>
        /// <param name="szSrc">Source path (key)</param>
        /// <param name="szDst">Destination path (key)</param>
        public void MoveImageOnS3(string szSrc, string szDst)
        {
            try
            {
                using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                {
                    CopyObjectRequest cor = new CopyObjectRequest();
                    DeleteObjectRequest dor = new DeleteObjectRequest();
                    cor.SourceBucket = cor.DestinationBucket = dor.BucketName = AWSConfiguration.CurrentS3Bucket;
                    cor.DestinationKey = szDst;
                    cor.SourceKey = dor.Key = szSrc;
                    cor.CannedACL = S3CannedACL.PublicRead;
                    cor.StorageClass = S3StorageClass.Standard; // vs. reduced

                    s3.CopyObject(cor);
                    s3.DeleteObject(dor);
                }
            }
            catch (AmazonS3Exception ex)
            {
                util.NotifyAdminEvent("Error moving image on S3", String.Format(CultureInfo.InvariantCulture, "Error moving from key\r\n{0}to\r\n{1}\r\n\r\n{2}", szSrc, szDst, WrapAmazonS3Exception(ex)), ProfileRoles.maskSiteAdminOnly);
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error moving file on S3: Request address:{0}, Message:{1}", WrapAmazonS3Exception(ex), ex.Message));
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Unknown error moving image on S3: " + ex.Message);
            }
        }

        /// <summary>
        /// Deletes the image from S3.  The actual operation happens asynchronously; the result is not captured.
        /// </summary>
        /// <param name="mfbii">The image to delete</param>
        public void DeleteImageOnS3(MFBImageInfo mfbii)
        {
            try
            {
                DeleteObjectRequest dor = new DeleteObjectRequest()
                {
                    BucketName = AWSConfiguration.CurrentS3Bucket,
                    Key = mfbii.S3Key
                };

                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                        {
                            DeleteObjectResponse doresp = s3.DeleteObject(dor);
                            if (mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4)
                            {
                                // Delete the thumbnail too.
                                string szs3Key = mfbii.S3Key;
                                dor.Key = szs3Key.Replace(Path.GetFileName(szs3Key), MFBImageInfo.ThumbnailPrefixVideo + Path.GetFileNameWithoutExtension(szs3Key) + "00001" + FileExtensions.JPG);
                                doresp = s3.DeleteObject(dor);
                            }
                        }
                    }
                    catch (AmazonS3Exception ex) { string sz = ex.Message; }
                }
                )).Start();
            }
            catch (AmazonS3Exception ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error deleting file on S3: {0}", WrapAmazonS3Exception(ex)), ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Unknown error deleting image on S3: " + ex.Message);
            }
        }

        /// <summary>
        /// Moves the image to Amazon
        /// </summary>
        /// <param name="por"></param>
        /// <param name="mfbii">The object to move.</param>
        public void MoveByRequest(PutObjectRequest por, MFBImageInfo mfbii)
        {
            if (mfbii == null)
                throw new ArgumentNullException("mfbii");
            if (por == null)
                throw new ArgumentNullException("por");
            lock (lockObject)
            {
                try
                {
                    using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                    {
                        PutObjectResponse s3r = null;
                        using (por.InputStream)
                        {
                            s3r = s3.PutObject(por);
                        }
                        if (s3r != null)
                        {
                            switch (mfbii.ImageType)
                            {
                                case MFBImageInfo.ImageFileType.JPEG:
                                    File.Delete(mfbii.PhysicalPathFull);
                                    break;
                                case MFBImageInfo.ImageFileType.PDF:
                                    {
                                        try
                                        {
                                            string szOldPhysicalPath = mfbii.PhysicalPathFull;
                                            if (String.IsNullOrEmpty(mfbii.Comment))
                                                mfbii.Comment = mfbii.ThumbnailFile;
                                            mfbii.ImageType = MFBImageInfo.ImageFileType.S3PDF;
                                            mfbii.RenameLocalFile(mfbii.ThumbnailFile.Replace(FileExtensions.PDF, FileExtensions.S3PDF));

                                            // Write the comment to the resulting file.
                                            using (FileStream fs = File.OpenWrite(mfbii.PhysicalPathThumbnail))
                                            {
                                                fs.SetLength(0);
                                                byte[] rgBytes = Encoding.UTF8.GetBytes(mfbii.Comment.ToCharArray());
                                                fs.Write(rgBytes, 0, rgBytes.Length);
                                            }
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            mfbii.ImageType = MFBImageInfo.ImageFileType.PDF;
                                        }
                                        catch (FileNotFoundException)
                                        {
                                            mfbii.ImageType = MFBImageInfo.ImageFileType.PDF;
                                        }
                                        catch (IOException)
                                        {
                                            mfbii.ImageType = MFBImageInfo.ImageFileType.PDF;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            // ALWAYS update the db
                            mfbii.UpdateDBLocation(false);
                        }
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    throw new MyFlightbookException(WrapAmazonS3Exception(ex), ex);
                }
            }
        }

        /// <summary>
        /// Moves the full-size image to the current bucket.  Actual move is done on a background thread, and the local file is deleted IF the operation is successful.
        /// <param name="fSynchronous">true if the call should be made synchronously; otherwise, the call returns immediately and the move is done on a background thread</param>
        /// <param name="mfbii">The image to move</param>
        /// </summary>
        public void MoveImageToS3(bool fSynchronous, MFBImageInfo mfbii)
        {
            try
            {
                PutObjectRequest por = new PutObjectRequest()
                {
                    BucketName = AWSConfiguration.CurrentS3Bucket,
                    ContentType = mfbii.ImageType == MFBImageInfo.ImageFileType.JPEG ? ContentTypeJPEG : ContentTypePDF,
                    AutoCloseStream = true,
                    InputStream = new FileStream(mfbii.PhysicalPathFull, FileMode.Open, FileAccess.Read),
                    Key = mfbii.S3Key,
                    CannedACL = S3CannedACL.PublicRead,
                    StorageClass = S3StorageClass.Standard // vs. reduced
                };

                if (fSynchronous)
                    MoveByRequest(por, mfbii);
                else
                    new Thread(new ThreadStart(() => { MoveByRequest(por, mfbii); })).Start();
            }
            catch (AmazonS3Exception ex)
            {
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error moving file to S3: {0}", WrapAmazonS3Exception(ex)), ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException("Unknown error moving image to S3: " + ex.Message);
            }
        }
        #endregion

        #region Video Support
        /// <summary>
        /// Uploads a video for transcoding
        /// </summary>
        /// <param name="szFileName">Filename of the input stream</param>
        /// <param name="szContentType">Content type of the video file</param>
        /// <param name="szBucket">Bucket to use.  Specified as a parameter because CurrentBucket might return the wrong value when called on a background thread</param>
        /// <param name="szPipelineID">PipelineID from amazon</param>
        /// <param name="mfbii">The MFBImageInfo that encapsulates the video</param>
        public void UploadVideo(string szFileName, string szContentType, string szBucket, string szPipelineID, MFBImageInfo mfbii)
        {
            if (mfbii == null)
                throw new ArgumentNullException("mfbii");

            using (var etsClient = AWSConfiguration.ElasticTranscoderClient())
            {
                string szGuid = Guid.NewGuid() + MFBImageInfo.szNewS3KeySuffix;
                string szBasePath = mfbii.VirtualPath.StartsWith("/", StringComparison.Ordinal) ? mfbii.VirtualPath.Substring(1) : mfbii.VirtualPath;
                string szBaseFile = String.Format(CultureInfo.InvariantCulture, "{0}{1}", szBasePath, szGuid);

                PutObjectRequest por = new PutObjectRequest()
                {
                    BucketName = szBucket,
                    ContentType = szContentType,
                    AutoCloseStream = true,
                    InputStream = new FileStream(szFileName, FileMode.Open, FileAccess.Read),
                    Key = szBaseFile + FileExtensions.VidInProgress,
                    CannedACL = S3CannedACL.PublicRead,
                    StorageClass = S3StorageClass.Standard // vs. reduced
                };

                PutObjectResponse s3r = null;
                lock (lockObject)
                {
                    try
                    {
                        using (por.InputStream)
                        {
                            using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                                s3r = s3.PutObject(por);
                            File.Delete(szFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        util.NotifyAdminEvent("Error putting video file", ex.Message, ProfileRoles.maskSiteAdminOnly);
                        throw;
                    }
                }

                var job = etsClient.CreateJob(new CreateJobRequest()
                {
                    PipelineId = szPipelineID,
                    Input = new JobInput()
                    {
                        AspectRatio = "auto",
                        Container = "auto",
                        FrameRate = "auto",
                        Interlaced = "auto",
                        Resolution = "auto",
                        Key = por.Key,
                    },
                    Output = new CreateJobOutput()
                    {
                        ThumbnailPattern = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}", szBasePath, MFBImageInfo.ThumbnailPrefixVideo, szGuid, "{count}"),
                        Rotate = "auto",
                        // Generic 720p: Go to http://docs.aws.amazon.com/elastictranscoder/latest/developerguide/create-job.html#PresetId to see a list of some
                        // of the support presets or call the ListPresets operation to get the full list of available presets
                        // PresetId = "1351620000000-000010",
                        PresetId = "1423799228749-hsj7ba",
                        Key = szBaseFile + FileExtensions.MP4
                    }
                });
                if (job != null)
                    new PendingVideo(szGuid, job.Job.Id, mfbii.Comment, mfbii.Class, mfbii.Key, szBucket).Commit();
            }
        }
        #endregion
    }

    /// <summary>
    /// Utility Admin functions for AWS S3 images
    /// </summary>
    public class AWSImageManagerAdmin : AWSS3ImageManager
    {
        /// <summary>
        /// ADMIN ONLY - Remove images from S3 LIVE BUCKET that are orphaned (no reference from live site)
        /// </summary>
        /// <param name="ic"></param>
        /// <param name="handleS3Object"></param>
        public static void ADMINDeleteS3Orphans(MFBImageInfo.ImageClass ic, Action<long, long, long, long> onSummary, Action onS3EnumDone, Func<string, int, bool> onDelete)
        {
            try
            {
                string szBasePath = MFBImageInfo.BasePathFromClass(ic);
                if (szBasePath.StartsWith("/"))
                    szBasePath = szBasePath.Substring(1);

                Regex r = new Regex(String.Format(CultureInfo.InvariantCulture, "{0}(.*)/(.*)(\\.jpg|\\.jpeg|\\.pdf|\\.s3pdf|\\.mp4)", szBasePath), RegexOptions.IgnoreCase);

                long cBytesToFree = 0;
                long cBytesOnS3 = 0;
                long cFilesOnS3 = 0;
                long cOrphansFound = 0;

                using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                {
                    ListObjectsRequest request = new ListObjectsRequest() { BucketName = AWSConfiguration.S3BucketName, Prefix = szBasePath };

                    /*
                     * Need to get the files from S3 FIRST, otherwise there is a race condition
                     * I.e., if we get files from the DB, then get files from S3, a file could be added to the db AFTER our query
                     * but BEFORE we retrieve it from the S3 listing, and we will thus treat it as an orphan and delete it.
                     * But since the file is put into the DB before being moved to S3, if we get all the S3 files and THEN
                     * get the DB references, we will always have a subset of the valid S3 files, which prevents a false positive 
                     * orphan identification.
                     */

                    List<S3Object> lstS3Objects = new List<S3Object>();
                    // Get the list of S3 objects
                    do
                    {
                        ListObjectsResponse response = s3.ListObjects(request);

                        cFilesOnS3 += response.S3Objects.Count;
                        foreach (S3Object o in response.S3Objects)
                        {
                            cBytesOnS3 += o.Size;
                            lstS3Objects.Add(o);
                        }

                        // If response is truncated, set the marker to get the next 
                        // set of keys.
                        if (response.IsTruncated)
                            request.Marker = response.NextMarker;
                        else
                            request = null;
                    } while (request != null);

                    onS3EnumDone();

                    // Now get all of the images in the class and do orphan detection
                    Dictionary<string, MFBImageInfo> dictDBResults = MFBImageInfo.AllImagesForClass(ic);

                    long cOrphansLikely = Math.Max(lstS3Objects.Count - dictDBResults.Keys.Count, 1);
                    Regex rGuid = new Regex(String.Format(CultureInfo.InvariantCulture, "^({0})?([^_]*).*", MFBImageInfo.ThumbnailPrefixVideo), RegexOptions.Compiled);  // for use below in extracting GUIDs from video thumbnail and video file names.
                    lstS3Objects.ForEach((o) =>
                    {
                        Match m = r.Match(o.Key);
                        if (m.Groups.Count < 3)
                            return;

                        string szKey = m.Groups[1].Value;
                        string szName = m.Groups[2].Value;
                        string szExt = m.Groups[3].Value;

                        bool fPDF = (String.Compare(szExt, FileExtensions.PDF, true) == 0);
                        bool fVid = (String.Compare(szExt, FileExtensions.MP4, true) == 0);
                        bool fVidThumb = Regex.IsMatch(szExt, FileExtensions.RegExpImageFileExtensions) && szName.StartsWith(MFBImageInfo.ThumbnailPrefixVideo);
                        bool fJpg = (String.Compare(szExt, FileExtensions.JPG, true) == 0 || String.Compare(szExt, FileExtensions.JPEG, true) == 0);

                        string szThumb = string.Empty;
                        if (fPDF)
                            szThumb = szName + FileExtensions.S3PDF;
                        else if (fVid || fVidThumb)
                        {
                            // This is a bit of a hack, but we have two files on the server for a video, neither of which precisely matches what's in the database.
                            // The video file is {guid}.mp4 or {guid}_.mp4, the thumbnail on S3 is v_{guid}_0001.jpg.
                            // So we grab the GUID and see if we have a database entry matching that guid.
                            Match mGuid = rGuid.Match(szName);
                            szThumb = (mGuid.Groups.Count >= 3) ? mGuid.Groups[2].Value : szName + szExt;
                            string szMatchKey = dictDBResults.Keys.FirstOrDefault(skey => skey.Contains(szThumb));
                            if (!String.IsNullOrEmpty(szMatchKey))  // leave it in the dictionary - don't remove it - because we may yet hit the Mp4 or the JPG.
                                return;
                        }
                        else if (fJpg)
                            szThumb = MFBImageInfo.ThumbnailPrefix + szName + szExt;

                        string szPrimary = MFBImageInfo.PrimaryKeyForValues(ic, szKey, szThumb);

                        // if it is found, super - remove it from the dictionary (for performance) and return
                        if (dictDBResults.ContainsKey(szPrimary))
                            dictDBResults.Remove(szPrimary);
                        else
                        {
                            cOrphansFound++;
                            cBytesToFree += o.Size;
                            if (onDelete(o.Key, (int)((100 * cOrphansFound) / cOrphansLikely)))
                            {
                                // Make sure that the item 
                                DeleteObjectRequest dor = new DeleteObjectRequest() { BucketName = AWSConfiguration.S3BucketName, Key = o.Key };
                                DeleteObjectResponse delr = s3.DeleteObject(dor);
                            }
                        }
                    });

                    onSummary(cFilesOnS3, cBytesOnS3, cOrphansFound, cBytesToFree);
                }
            }
            catch { }
        }

        /// <summary>
        /// ADMIN ONLY - Removes images from the debug bucket
        /// </summary>
        public static void ADMINCleanUpDebug()
        {
            using (IAmazonS3 s3 = AWSConfiguration.S3Client())
            {
                ListObjectsRequest request = new ListObjectsRequest() { BucketName = AWSConfiguration.S3BucketNameDebug };

                do
                {
                    ListObjectsResponse response = s3.ListObjects(request);

                    foreach (S3Object o in response.S3Objects)
                    {
                        DeleteObjectRequest dor = new DeleteObjectRequest() { BucketName = AWSConfiguration.S3BucketNameDebug, Key = o.Key };
                        s3.DeleteObject(dor);
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);
            }
        }

        /// <summary>
        /// ADMIN Migrates the full image file to S3, returns the number of bytes moved.
        /// </summary>
        /// <param name="mfbii">The image to move</param>
        /// <returns>The # of bytes moved, if any, -1 for failure</returns>
        public int ADMINMigrateToS3(MFBImageInfo mfbii)
        {
            int cBytes = 0;

            if (mfbii == null || !AWSConfiguration.UseS3)
                return -1;

            string szPathFull = mfbii.PhysicalPathFull;
            if (mfbii.IsLocal)
            {
                if (mfbii.ImageType == MFBImageInfo.ImageFileType.JPEG)
                    mfbii.Update(); // write the meta-data to both thumbnail & full file

                FileInfo fi = new FileInfo(szPathFull);
                cBytes += (Int32)fi.Length;
                MoveImageToS3(true, mfbii);

                return cBytes;
            }

            return -1;
        }
    }


    #region Pending videos and images
    /// <summary>
    /// A video that has been submitted to Amazon for transcoding and is awaiting a response
    /// </summary>
    public class PendingVideo
    {
        #region Properties
        /// <summary>
        /// The GUID that is the basis for the filename
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// The AWS-assigned jobID
        /// </summary>
        public string JobID { get; set; }

        /// <summary>
        /// The user-provided comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Date/time of submission
        /// </summary>
        public DateTime SubmissionTime { get; set; }

        /// <summary>
        /// The class of the pending video
        /// </summary>
        public MFBImageInfo.ImageClass Class { get; set; }

        /// <summary>
        /// The key for the pending video
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Which bucket is this pending video in?
        /// </summary>
        public string Bucket { get; set; }
        #endregion

        #region Constructors
        public PendingVideo(string guid, string jobID, string comment, MFBImageInfo.ImageClass ic, string key, string bucket)
        {
            GUID = guid;
            JobID = jobID;
            Comment = comment;
            Class = ic;
            Key = key;
            Bucket = bucket;
            SubmissionTime = DateTime.Now;
        }

        public PendingVideo(string jobID)
        {
            JobID = Comment = GUID = string.Empty;
            DBHelper dbh = new DBHelper("SELECT *  FROM pendingvideos WHERE jobID=?j");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("j", jobID); },
                (dr) =>
                {
                    GUID = (string)dr["guid"];
                    JobID = (string)dr["jobID"];
                    Comment = (string)dr["Comment"];
                    Class = (MFBImageInfo.ImageClass)Convert.ToInt32(dr["virtPathID"], CultureInfo.InvariantCulture);
                    Key = (string)dr["imagekey"];
                    Bucket = (string)dr["Bucket"];
                    SubmissionTime = DateTime.SpecifyKind(Convert.ToDateTime(dr["Submitted"]), DateTimeKind.Utc);
                });
        }
        #endregion

        public void Commit()
        {
            DBHelper dbh = new DBHelper("REPLACE INTO pendingvideos SET jobID=?j, guid=?g, Comment=?c, imagekey=?k, virtPathID=?v, Bucket=?b, Submitted=UTC_TIMESTAMP()");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("j", JobID);
                comm.Parameters.AddWithValue("g", GUID);
                comm.Parameters.AddWithValue("c", Comment);
                comm.Parameters.AddWithValue("k", Key);
                comm.Parameters.AddWithValue("v", (int)Class);
                comm.Parameters.AddWithValue("b", Bucket);
            });
        }

        public void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM pendingvideos WHERE jobID=?j");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("j", JobID); });
        }

        /// <summary>
        /// Filename for the thumbnail file
        /// </summary>
        public string ThumbnailFileName
        {
            get { return String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", MFBImageInfo.ThumbnailPrefixVideo, GUID, FileExtensions.JPG); }
        }

        /// <summary>
        /// Width of the thumbnail
        /// </summary>
        public int ThumbWidth { get; set; }

        /// <summary>
        /// Height of the thumbnail
        /// </summary>
        public int ThumbHeight { get; set; }

        /// <summary>
        /// Creates the thumbnail from a completed video process.  Sets the width/height in the process.
        /// </summary>
        /// <param name="szBasePath">The base path for the video object</param>
        /// <param name="szPhysicalPath">The filename to use for the resulting thumbnail image</param>
        public void InitThumbnail(string szBasePath, string szPhysicalPath)
        {
            if (szBasePath == null)
                throw new ArgumentNullException("szBasePath");

            string szThumbFile = String.Format(CultureInfo.InvariantCulture, "{0}{1}00001{2}", MFBImageInfo.ThumbnailPrefixVideo, GUID, FileExtensions.JPG);

            if (szBasePath.StartsWith("/", StringComparison.Ordinal))
                szBasePath = szBasePath.Substring(1);
            string srcFile = szBasePath + szThumbFile;
            // Copy the thumbnail over
            using (IAmazonS3 s3 = AWSConfiguration.S3Client())
            {
                GetObjectResponse gor = s3.GetObject(new GetObjectRequest() { BucketName = Bucket, Key = srcFile });
                if (gor != null && gor.ResponseStream != null)
                {
                    using (gor.ResponseStream)
                    {
                        using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(gor.ResponseStream))
                        {
                            Info inf = MFBImageInfo.InfoFromImage(image);

                            // save the thumbnail locally.
                            using (inf.Image)
                            {

                                inf.ImageDescription = Comment;

                                Bitmap bmp = MFBImageInfo.BitmapFromImage(inf.Image, MFBImageInfo.ThumbnailHeight, MFBImageInfo.ThumbnailWidth);
                                ThumbWidth = bmp.Width;
                                ThumbHeight = bmp.Height;

                                using (bmp)
                                {
                                    // get all properties of the original image and copy them to the new image.  This should include the annotation (above)
                                    foreach (PropertyItem pi in inf.Image.PropertyItems)
                                        bmp.SetPropertyItem(pi);

                                    bmp.Save(szPhysicalPath, ImageFormat.Jpeg);
                                }
                            }
                        }
                    }
                }

                // clean up the folder on S3 - anything that has the GUID but not .mp4 in it or the thumbnail in it.  (Save space!)  i.e., delete excess thumbnails and the source video file.
                List<S3Object> lstS3Objects = new List<S3Object>();
                ListObjectsRequest loRequest = new ListObjectsRequest() { BucketName = Bucket, Prefix = szBasePath };
                // Get the list of S3 objects
                do
                {
                    ListObjectsResponse response = s3.ListObjects(loRequest);
                    foreach (S3Object o in response.S3Objects)
                    {
                        if (o.Key.Contains(GUID) && !o.Key.Contains(FileExtensions.MP4) && !o.Key.Contains(szThumbFile))
                            lstS3Objects.Add(o);
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                        loRequest.Marker = response.NextMarker;
                    else
                        loRequest = null;
                } while (loRequest != null);

                lstS3Objects.ForEach((o) =>
                {
                    DeleteObjectResponse delr = s3.DeleteObject(new DeleteObjectRequest() { BucketName = Bucket, Key = o.Key });
                });
            }
        }
    }

    /// <summary>
    /// MFBImageInfo that has NOT YET been persisted
    /// </summary>
    [Serializable]
    public class MFBPendingImage : MFBImageInfo
    {
        #region properties
        /// <summary>
        /// The MFBPostedFile containing the pending file to upload
        /// </summary>
        public MFBPostedFile PostedFile { get; set; }

        /// <summary>
        /// Is this valid?  (I.e., has the posted file been deleted)
        /// </summary>
        public bool IsValid
        {
            get { return PostedFile != null; }
        }

        /// <summary>
        /// The key by which to access this object in the session
        /// </summary>
        public string SessionKey { get; set; }

        public override string URLThumbnail
        {
            get
            {
                if (!IsValid)
                    return string.Empty;

                if (ImageType == ImageFileType.JPEG)
                    return String.Format(CultureInfo.InvariantCulture, "~/Public/PendingImg.aspx?i={0}", HttpUtility.UrlEncode(SessionKey));
                else if (ImageType == ImageFileType.S3VideoMP4)
                    return "~/images/pendingvideo.png";
                else
                    return base.URLThumbnail;
            }
            set
            {
                base.URLThumbnail = value;
            }
        }

        public override string URLFullImage
        {
            get
            {
                if (!IsValid)
                    return string.Empty;

                if (ImageType == ImageFileType.JPEG)
                    return String.Format(CultureInfo.InvariantCulture, "~/Public/PendingImg.aspx?i={0}&full=1", HttpUtility.UrlEncode(SessionKey));
                else if (ImageType == ImageFileType.S3VideoMP4)
                    return string.Empty;    // nothing to click on, at least not yet.

                return base.URLFullImage;
            }
            set
            {
                base.URLFullImage = value;
            }
        }
        #endregion

        #region object creation
        protected void Init()
        {
            PostedFile = null;
        }

        public MFBPendingImage()
            : base()
        {
            Init();
        }

        public MFBPendingImage(MFBPostedFile mfbpf, string szSessKey)
            : base()
        {
            if (mfbpf == null)
                throw new ArgumentNullException("mfbpf");
            Init();
            PostedFile = mfbpf;
            ImageType = ImageTypeFromFile(mfbpf);
            ThumbnailFile = MFBImageInfo.ThumbnailPrefix + mfbpf.FileID;
            SessionKey = szSessKey;
        }
        #endregion

        /// <summary>
        /// Persists the pending image, returning the resulting MFBImageInfo object.
        /// </summary>
        /// <returns>The newly created MFBImageInfo object</returns>
        public MFBImageInfo Commit(MFBImageInfo.ImageClass ic, string szKey)
        {
            return new MFBImageInfo(ic, szKey, PostedFile, Comment, Location);
        }

        /// <summary>
        /// Doesn't really delete (since nothing has been persisted), but removes the object from memory and invalidates it.
        /// </summary>
        public override void DeleteImage()
        {
            PostedFile = null;
            ThumbnailFile = string.Empty;
            if (SessionKey != null && HttpContext.Current != null && HttpContext.Current.Session != null)
                HttpContext.Current.Session[SessionKey] = null;
        }

        public override void UpdateAnnotation(string szText)
        {
            Comment = szText;
        }
    }
    #endregion

    /// <summary>
    /// Pseudo HTTPPostedFile, since I can't create those.
    /// </summary>
    [Serializable]
    public class MFBPostedFile
    {
        #region Constructors
        public MFBPostedFile()
        {
        }

        public MFBPostedFile(HttpPostedFile pf) : this()
        {
            if (pf == null)
                throw new ArgumentNullException("pf");
            FileName = pf.FileName;
            ContentType = pf.ContentType;
            ContentLength = pf.ContentLength;
            ContentData = null;
            PostedFile = pf;
        }

        public MFBPostedFile(string szFile, string szContentType, int cBytes, byte[] rgBytes, string szID) : this()
        {
            FileName = szFile;
            ContentType = szContentType;
            ContentLength = cBytes;
            ContentData = rgBytes;
            FileID = szID;
            PostedFile = null;
        }
        #endregion

        #region properties
        /// <summary>
        /// File name for the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// MIME content type for the file
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The length of the file
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// The bytes of the posted file.
        /// </summary>
        public byte[] ContentData { get; set; }

        /// <summary>
        /// The unique ID provided by the AJAX file upload control
        /// </summary>
        public string FileID { get; set; }

        private byte[] m_ThumbBytes = null;

        /// <summary>
        /// Returns the bytes of the posted file, converted if needed from HEIC.
        /// </summary>
        public byte[] CompatibleContentData()
        {
            if (ContentData == null)
                return null;

            using (MemoryStream ms = new MemoryStream(ContentData))
            {
                // Check for HEIC
                try
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms))
                    {
                        // If we got here then the content is Drawing Compatible - i.e., not HEIC; just return contentdata
                        return ContentData;
                    }
                }
                catch (ArgumentException)
                {
                    return MFBImageInfo.ConvertStreamToJPG(ms);
                }
            }
        }
        
        /// <summary>
        /// The stream of the thumbnail file - computed once and cached in ThumbnailData
        /// </summary>
        public byte[] ThumbnailBytes()
        {
            if (m_ThumbBytes != null)
                return m_ThumbBytes;

            Stream s = InputStream;
            if (s != null)
            {
                string szTempFile = null;
                try
                {
                    using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(s, out szTempFile))
                    {
                        Info inf = MFBImageInfo.InfoFromImage(image);
                        using (Bitmap bmp = MFBImageInfo.BitmapFromImage(inf.Image, MFBImageInfo.ThumbnailHeight, MFBImageInfo.ThumbnailWidth))
                        {
                            using (MemoryStream sOut = new MemoryStream())
                            {
                                bmp.Save(sOut, ImageFormat.Jpeg);
                                m_ThumbBytes = sOut.ToArray();
                            }
                        }
                    }
                }
                finally
                {
                    if (!String.IsNullOrEmpty(szTempFile) && File.Exists(szTempFile))
                        File.Delete(szTempFile);
                }
            }
            return m_ThumbBytes;
        }

        /// <summary>
        /// The HTTPPostedFile from which this was created, if any
        /// </summary>
        public HttpPostedFile PostedFile { get; set; }

        public Stream InputStream
        {
            get
            {
                if (PostedFile != null)
                    return PostedFile.InputStream;
                else if (ContentData != null)
                    return new MemoryStream(ContentData);
                else
                    return null;
            }
        }
        #endregion
    }

    public class MFBImageInfoEvent : EventArgs
    {
        public MFBImageInfo Image { get; set; }

        public MFBImageInfoEvent(MFBImageInfo img = null) : base()
        {
            Image = img;
        }
    }

    /// <summary>
    /// Encapsulates utilities for scribbled PNG signatures, including presentation as a data: URL
    /// </summary>
    public static class ScribbleImage
    {
        private const string DataURLPrefix = "data:image/png;base64,";

        /// <summary>
        /// Returns a data:... URL for the specified byte array
        /// </summary>
        /// <param name="rgb">The byte array</param>
        /// <returns>A url which can be used in-line for images</returns>
        public static string DataLinkForByteArray(byte[] rgb)
        {
            if (rgb == null)
                return string.Empty;

            return DataURLPrefix + Convert.ToBase64String(rgb);
        }

        /// <summary>
        /// Validates that the specified data URL is well formed and contains data
        /// </summary>
        /// <param name="szLink"></param>
        /// <returns></returns>
        public static bool IsValidDataURL(string szLink)
        {
            if (String.IsNullOrWhiteSpace(szLink))
                return false;

            if (!szLink.StartsWith(DataURLPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            return szLink.Length > DataURLPrefix.Length;
        }

        /// <summary>
        /// Retrieves the bytes from a data: URL
        /// </summary>
        /// <param name="szLink">The data: URL</param>
        /// <returns>The bytes of the PNG.</returns>
        public static byte[] FromDataLinkURL(string szLink)
        {
            if (szLink == null)
                return new byte[0];

            string szSigB64 = szLink.Substring(ScribbleImage.DataURLPrefix.Length);
            byte[] rgbSignature = Convert.FromBase64CharArray(szSigB64.ToCharArray(), 0, szSigB64.Length);

            if (rgbSignature.Length > 10000) // this may not be compressed (e.g., from Android) - compress it.
            {
                using (Stream st = new MemoryStream(rgbSignature))
                {
                    using (Stream stDst = new MemoryStream())
                    {
                        // This is a PNG, so no need to handle temp files/conversion.
                        using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(st))
                        {
                            image.Save(stDst, System.Drawing.Imaging.ImageFormat.Png);
                            rgbSignature = new byte[stDst.Length];
                            stDst.Position = 0;
                            stDst.Read(rgbSignature, 0, (int)stDst.Length);
                        }
                    }
                }
            }

            return rgbSignature;

        }
    }
}