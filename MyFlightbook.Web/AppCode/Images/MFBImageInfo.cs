using AWSNotifications;
using gma.Drawing.ImageInfo;
using ImageMagick;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2021 MyFlightbook LLC
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
        public const string RegExpVideoFileExtensions = "(avi|wmv|mp4|mov|m4v|m2p|mpeg|mpg|hdmov|flv|avchd|mpeg4|m2t|h264|mp3|wav)$";
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
    /// 
    /// This is the base class for MFBImageInfo
    /// </summary>
    [Serializable]
    public abstract class MFBImageInfoBase : IComparable
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
        public enum ImageClass { Flight, Aircraft, Endorsement, BasicMed, OfflineEndorsement, Unknown };

        public const string ThumbnailPrefix = "t_";
        public const string ThumbnailPrefixVideo = "v_";

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
        protected string PhysicalPath
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
        protected string CacheKey
        {
            get { return PrimaryKey; }
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
        #endregion

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
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["AircraftPixDir"]);
                case ImageClass.Endorsement:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["EndorsementsPixDir"]);
                case ImageClass.OfflineEndorsement:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["OfflineEndorsementsPixDir"]);
                case ImageClass.Flight:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["FlightsPixDir"]);
                case ImageClass.BasicMed:
                    return VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["BasicMedDir"]);
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

            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["AircraftPixDir"]), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Aircraft;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["FlightsPixDir"]), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Flight;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["EndorsementsPixDir"]), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.Endorsement;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["OfflineEndorsementsPixDir"]), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.OfflineEndorsement;
            if (String.Compare(szVirtPath, VirtualPathUtility.ToAbsolute(ConfigurationManager.AppSettings["BasicMedDir"]), StringComparison.OrdinalIgnoreCase) == 0)
                return ImageClass.BasicMed;

            // No exact match - try looking for other clues
            if (szVirtPath.Contains("AIRCRAFT"))
                return ImageClass.Aircraft;
            else if (szVirtPath.Contains("FLIGHTS"))
                return ImageClass.Flight;
            else if (szVirtPath.Contains("ENDORSEMENTS"))
                return ImageClass.Endorsement;

            return ImageClass.Unknown;

        }

        #region IComparable
        private string SortKey
        {
            get { return String.IsNullOrWhiteSpace(Comment) ? FullImageFile : Comment; }
        }

        /// <summary>
        /// Sorts MFBImageInfoBase objects.  Puts images before all other documents, subsorts by comment or filename (== timestamp for images)
        /// </summary>
        /// <param name="o">Object being compared to.</param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            MFBImageInfoBase mfbii = obj as MFBImageInfoBase;

            if (this.ImageType == mfbii.ImageType)
                return this.SortKey.CompareOrdinalIgnoreCase(mfbii.SortKey);
            else
                return this.ImageType == ImageFileType.JPEG ? -1 : 1;
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null || !(obj is MFBImageInfoBase))
            {
                return false;
            }

            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -750829556;
                hashCode = hashCode * -1521134295 + ImageType.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Comment);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VirtualPath);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Key);
                hashCode = hashCode * -1521134295 + Class.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ThumbnailFile);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FullImageFile);
                hashCode = hashCode * -1521134295 + EqualityComparer<LatLong>.Default.GetHashCode(Location);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(URLFullImage);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(URLThumbnail);
                hashCode = hashCode * -1521134295 + EqualityComparer<Uri>.Default.GetHashCode(UriS3VideoThumbnail);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathFullImage);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathThumbnail);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S3Key);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathFullImageS3);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PhysicalPathThumbnail);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PhysicalPathFull);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PhysicalPath);
                hashCode = hashCode * -1521134295 + IsLocal.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CacheKey);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PrimaryKey);
                return hashCode;
            }
        }

        public static bool operator ==(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return EqualityComparer<MFBImageInfoBase>.Default.Equals(left, right);
        }

        public static bool operator !=(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return !(left == right);
        }

        public static bool operator <(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(MFBImageInfoBase left, MFBImageInfoBase right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
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
    public class MFBImageInfo : MFBImageInfoBase
    {
        internal const string szNewS3KeySuffix = "_";

        public const int ThumbnailWidth = 150;
        public const int ThumbnailHeight = 150;
        public const int MaxImgHeight = 1600;
        public const int MaxImgWidth = 1600;

        private readonly System.Object lockObject = new System.Object();
        private readonly static System.Object videoLockObject = new System.Object();
        private readonly static System.Object idempotencyLock = new System.Object();

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
                throw new ArgumentNullException(nameof(szName));
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
                throw new ArgumentNullException(nameof(pf));
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
                            AWSS3ImageManager.DeleteImageOnS3(this);
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
                AWSS3ImageManager.MoveImageOnS3(s3KeyOld, S3Key);

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
                            if (String.Compare(fi.Name, ThumbnailFile, StringComparison.CurrentCultureIgnoreCase) == 0)
                                continue;
                            if (this.ImageType == ImageFileType.JPEG && !fi.Name.StartsWith(ThumbnailPrefix, StringComparison.CurrentCultureIgnoreCase))
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
            return (String.Compare(ConfigurationManager.AppSettings["UseImageDB"], "yes", StringComparison.OrdinalIgnoreCase) == 0);
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
                throw new ArgumentNullException(nameof(dr));

            MFBImageInfo.ImageClass ic = (MFBImageInfo.ImageClass)Convert.ToInt32(dr["VirtPathID"], CultureInfo.InvariantCulture);
            string szKey = (string)dr["ImageKey"];

            MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, (string)dr["ThumbFilename"]);
            double lat = (double)util.ReadNullableField(dr, "Latitude", 0.0);
            double lon = (double)util.ReadNullableField(dr, "Longitude", 0.0); 
            if (lat != 0 && lon != 0)
                mfbii.Location = new LatLong(lat, lon);
            mfbii.Comment = (string)dr["Comment"];
            mfbii.WidthThumbnail = Convert.ToInt32(dr["ThumbWidth"], CultureInfo.InvariantCulture);
            mfbii.HeightThumbnail = Convert.ToInt32(dr["ThumbHeight"], CultureInfo.InvariantCulture);
            mfbii.ImageType = (MFBImageInfo.ImageFileType)Convert.ToInt32(dr["ImageType"], CultureInfo.InvariantCulture);
            return mfbii;
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
        static public Dictionary<string, MFBImageCollection> FromDB(MFBImageInfo.ImageClass ic, int offset, int count, out List<string> lstKeys)
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

            Dictionary<string, MFBImageCollection> dictResults = new Dictionary<string, MFBImageCollection>();

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT i1.* FROM images i1 INNER JOIN (SELECT DISTINCT ImageKey FROM images WHERE virtPathID={0} {1} LIMIT {2},{3}) AS ik ON i1.ImageKey=ik.ImageKey AND i1.virtPathID={0} {4}", (int)ic, szSort, offset, count, sz2ndSort));

            string szKeyCurrent = string.Empty;
            List<string> keyList = new List<string>();
            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    MFBImageInfo mfbii = ImageFromDBRow(dr);

                    MFBImageCollection lst;

                    if (dictResults.ContainsKey(mfbii.Key))
                        lst = dictResults[mfbii.Key];
                    else
                        dictResults[mfbii.Key] = lst = new MFBImageCollection();
                    lst.Add(mfbii);

                    if (String.Compare(szKeyCurrent, mfbii.Key, StringComparison.InvariantCultureIgnoreCase) != 0 && keyList != null)
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
        static public Dictionary<string, MFBImageCollection> FromDB(MFBImageInfo.ImageClass ic)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM images WHERE virtPathID={0}", (int)ic));

            Dictionary<string, MFBImageCollection> dictResults = new Dictionary<string, MFBImageCollection>();

            dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    MFBImageInfo mfbii = ImageFromDBRow(dr);
                    MFBImageCollection lst;

                    if (dictResults.ContainsKey(mfbii.Key))
                        lst = dictResults[mfbii.Key];
                    else
                    {
                        lst = new MFBImageCollection();
                        dictResults[mfbii.Key] = lst;
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
            ThumbnailFile = szThumbnail ?? throw new ArgumentNullException(nameof(szThumbnail));
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
                throw new ArgumentNullException(nameof(notification));
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
            if (image == null)
                throw new ArgumentNullException(nameof(image));

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
            catch (Exception ex) when (ex is InvalidCastException || ex is ArgumentException)
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
                throw new ArgumentNullException(nameof(img));
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
                throw new ArgumentNullException(nameof(s));

            szTemp = null;
            MagickImage image = null;
            try
            {
                return System.Drawing.Image.FromStream(s);
            }
            catch (Exception ex) when (ex is ArgumentException)
            {
                szTemp = Path.GetTempFileName();
                try
                {
                    image = new MagickImage(s);
                    image.Write(szTemp, MagickFormat.Jpg);

                    return System.Drawing.Image.FromFile(szTemp);
                }
                catch (Exception ex2) when (ex2 is MagickException)
                {
                    return null;
                }
            }
            finally
            {
                if (image != null)
                    image.Dispose();
            }
        }

        /// <summary>
        /// Variant of DrawingcompatibleImageBytesFromStream that does NOT handle conversion from HEIC, so UNSAFE to call (leaves temporary file turds) if conversion is needed
        /// </summary>
        /// <param name="s">The stream</param>
        /// <returns>A system.drawing.image object</returns>
        public static System.Drawing.Image DrawingCompatibleImageFromStream(Stream s)
        {
            System.Drawing.Image result = DrawingCompatibleImageFromStream(s, out string szTempFile);
            return szTempFile != null || File.Exists(szTempFile)
                ? throw new InvalidOperationException("DrawingCompatibleImageBytesFromStream called on an image that generated a temp file, but without the temp file being cleaned up.")
                : result;
        }

        /// <summary>
        /// Returns a byte array representing JPG using MagickImage - LOSES EXIF DATA and can be slow.
        /// </summary>
        /// <param name="s">The input stream</param>
        /// <returns></returns>
        public static byte[] ConvertStreamToJPG(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

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

        public static byte[] ScaledImage(Stream sIn, int width, int height)
        {
            if (sIn == null)
                throw new ArgumentNullException(nameof(sIn));

            if (sIn.Length == 0)
                return Array.Empty<byte>();

            using (MagickImage image = new MagickImage(sIn))
            {
                // first, fix any orientation issues.
                OrientationType ot = image.Orientation;
                switch (ot)
                {
                    case OrientationType.TopLeft:
                        break;
                    case OrientationType.TopRight:
                        image.Flop();
                        break;
                    case OrientationType.BottomRight:
                        image.Rotate(180);
                        break;
                    case OrientationType.BottomLeft:
                        image.Flop();
                        image.Rotate(180);
                        break;
                    case OrientationType.LeftTop:
                        image.Flop();
                        image.Rotate(-90);
                        break;
                    case OrientationType.RightTop:
                        image.Rotate(90);
                        break;
                    case OrientationType.RightBottom:
                        image.Flop();
                        image.Rotate(90);
                        break;
                    case OrientationType.LeftBotom:
                        image.Rotate(-90);
                        break;
                    default:
                        break;
                }
                image.Orientation = OrientationType.TopLeft;

                // Make it square and then resize.  Cut off from half of longer dimension on each end.
                int curHeight = image.Height;
                int curWidth = image.Width;
                
                if (curWidth > curHeight)
                {
                    int dWidth = (curWidth - curHeight) / 2;
                    image.ChopHorizontal(0, dWidth);
                    image.ChopHorizontal(image.Width - dWidth, dWidth);
                }
                else if (curHeight > curWidth)
                {
                    int dHeight = (curHeight - curWidth) / 2;
                    image.ChopVertical(0, dHeight);
                    image.ChopVertical(image.Height - dHeight, dHeight);
                }

                // Now it is square - resize it, maintaining aspect ratio
                image.Resize(new MagickGeometry(width, height));

                // below is from https://github.com/dlemstra/Magick.NET/issues/57 and puts the image into a circle.  We will keep it square and use CSS for the circle
                /*
                using (MagickImage clone = image.Clone() as MagickImage)
                {
                    // -distort DePolar 0
                    clone.Distort(DistortMethod.DePolar, 0);

                    // -virtual-pixel HorizontalTile
                    clone.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;

                    // -background None
                    clone.BackgroundColor = MagickColors.None;

                    // -distort Polar 0
                    clone.Distort(DistortMethod.Polar, 0);

                    // -compose Dst_In -composite
                    image.Composite(clone, CompositeOperator.DstIn);

                    // -trime
                    image.Trim();

                    // +repage
                    image.RePage();
                }
                */

                using (MemoryStream msOut = new MemoryStream())
                {
                    image.Write(msOut, MagickFormat.Jpeg);
                    return msOut.GetBuffer();
                }
            }
        }

        #region InitfileHelpers
        private void InitFileS3Mp4(MFBPostedFile myFile)
        {
            if (String.IsNullOrEmpty(myFile.TempFileName) || !File.Exists(myFile.TempFileName))
                return;

            string szBucket = AWSConfiguration.CurrentS3Bucket;  // bind this now - in a separate thread (below) it defaults to main, not debug.
            string szPipelineID = LocalConfig.SettingForKey(AWSConfiguration.UseDebugBucket ? "ETSPipelineIDDebug" : Branding.CurrentBrand.AWSETSPipelineConfigKey);  // bind this as well, same reason
            new Thread(new ThreadStart(() => { new AWSS3ImageManager().UploadVideo(myFile.TempFileName, myFile.ContentType, szBucket, szPipelineID, this); })).Start();
        }

        private void InitFileJPG(MFBPostedFile myFile, string szComment, LatLong ll)
        {
            string szTempFile = null;
            try
            {
                // Create a file with a unique name - use current time for uniqueness
                DateTime dt = DateTime.Now;
                string szFileName = String.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}{3}", dt.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture), myFile.ContentLength.ToString(CultureInfo.InvariantCulture), szNewS3KeySuffix, FileExtensions.JPG);
                ThumbnailFile = MFBImageInfo.ThumbnailPrefix + szFileName;

                using (Stream s = myFile.GetInputStream())
                {
                    // Resize the image if needed, and then create a thumbnail of it and write that too:
                    using (System.Drawing.Image image = DrawingCompatibleImageFromStream(s, out szTempFile))
                    {
                        // rotate the image, if necessary
                        Info inf = InfoFromImage(image);

                        // save the comment
                        inf.ImageDescription = szComment;

                        // update the location
                        if (ll == null) // no geotag is specified - initialize location from the underlying image.
                        {
                            if (inf.HasGeotag)
                                Location = new LatLong(inf.Latitude, inf.Longitude);
                        }
                        else
                        {
                            // A specific geotag is provided: geotag the image.
                            Location = ll;
                            inf.Latitude = ll.Latitude;
                            inf.Longitude = ll.Longitude;
                        }

                        if (Location != null && !Location.IsValid)
                            Location = null;

                        // save the full-sized image
                        Bitmap bmp;

                        using (bmp = BitmapFromImage(inf.Image, MaxImgHeight, MaxImgWidth))
                        {
                            Width = bmp.Width;
                            Height = bmp.Height;

                            // get all properties of the original image and copy them to the new image.  This should include the annotation (above)
                            foreach (PropertyItem pi in inf.Image.PropertyItems)
                                bmp.SetPropertyItem(pi);

                            string szPathFullImage = PhysicalPathFull;
                            bmp.Save(szPathFullImage, ImageFormat.Jpeg);
                        }

                        using (bmp = BitmapFromImage(inf.Image, ThumbnailHeight, ThumbnailWidth))
                        {
                            WidthThumbnail = bmp.Width;
                            HeightThumbnail = bmp.Height;

                            // copy the properties here too.
                            foreach (PropertyItem pi in inf.Image.PropertyItems)
                                bmp.SetPropertyItem(pi);
                            bmp.Save(PhysicalPathThumbnail, ImageFormat.Jpeg);
                        }

                        // if we got here, everything is hunky-dory.  Cache it!
                        if (HttpRuntime.Cache != null)
                            HttpRuntime.Cache[CacheKey] = this;

                        // Save it in the DB - we do this BEFORE moving to S3 to avoid a race condition
                        // that could arise when MoveImageToS3 attempts to update the record to show that it is no longer local.
                        // We need the record to be present so that the update works.
                        ToDB();

                        // Move this up to S3.  Note that if we're debugging, it will go into the debug bucket.
                        if (AWSConfiguration.UseS3)
                            new AWSS3ImageManager().MoveImageToS3(false, this);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) { } // nothing to do here; fail silently.
            finally
            {
                // clean up a temp file, if one was created; can only do this AFTER the image that used it has been disposed (above).
                if (!String.IsNullOrEmpty(szTempFile) && File.Exists(szTempFile))
                    File.Delete(szTempFile);

                myFile.CleanUp();
            }
        }

        private void InitFilePDF(MFBPostedFile myFile)
        {
            // just save the file as-is; we virtualize the thumbanil.
            string szBase = Regex.Replace(Path.GetFileNameWithoutExtension(myFile.FileName), @"[^a-zA-Z0-9]", string.Empty);
            if (String.IsNullOrWhiteSpace(szBase))
                szBase = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            
            if (Comment.Length > 0)
            {
                string szCleaned = Regex.Replace(Comment, @"[^a-zA-Z0-9]", string.Empty);
                if (!string.IsNullOrWhiteSpace(szCleaned))
                    szBase = szCleaned;
            }
            else
                Comment = szBase;

            string szFilename = szBase + szNewS3KeySuffix + FileExtensions.PDF;

            ThumbnailFile = szFilename;
            WidthThumbnail = HeightThumbnail = 100;
            string szFullPhysicalPath = HostingEnvironment.MapPath(VirtualPath + szFilename);

            using (FileStream fsDst = File.OpenWrite(szFullPhysicalPath))
            {
                using (Stream src = myFile.GetInputStream())
                {
                    src.Seek(0, SeekOrigin.Begin);
                    src.CopyTo(fsDst);
                }
            }
            myFile.CleanUp();

            // Save it in the DB - do this BEFORE moving to S3 to avoid a race condition (see above).
            ToDB();

            // We do this SYNCHRONOUSLY because the name of the thumbnail will change in the process.
            if (AWSConfiguration.UseS3)
                new AWSS3ImageManager().MoveImageToS3(true, this);
        }
        #endregion

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
                    InitFileS3Mp4(myFile);
                    break;
                case ImageFileType.JPEG:
                    InitFileJPG(myFile, szComment, ll);
                    break;
                case ImageFileType.PDF:
                    InitFilePDF(myFile);
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
                throw new ArgumentNullException(nameof(tw));

            if (szThumbFolder == null)
                szThumbFolder = "thumbs/";

            if (!szThumbFolder.EndsWith("/", StringComparison.Ordinal))
                szThumbFolder += "/";

            tw.AddStyleAttribute(HtmlTextWriterStyle.Display, "inline-block");
            tw.AddStyleAttribute(HtmlTextWriterStyle.Padding, "3px");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute(HtmlTextWriterAttribute.Href, ResolveFullImage());
            tw.RenderBeginTag(HtmlTextWriterTag.A);

            tw.AddAttribute(HtmlTextWriterAttribute.Src, (ImageType == MFBImageInfo.ImageFileType.PDF || ImageType == MFBImageInfo.ImageFileType.S3PDF) ? String.Format(CultureInfo.InvariantCulture, "http://{0}{1}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/images/pdficon_large.png")) : szThumbFolder + ThumbnailFile);
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
                case ImageClass.OfflineEndorsement:
                    szQ = "SELECT DISTINCT(ImageKey) AS idPic FROM images i LEFT JOIN users u ON i.imagekey=u.username WHERE i.VirtPathID=4 AND u.username IS NULL;";
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
                    DirectoryInfo dirOrphan = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", szPixDir, szIDOrphan)));

                    ImageList il = new ImageList(ic, szIDOrphan);
                    il.Refresh();
                    foreach (MFBImageInfo mfbii in il.ImageArray)
                    {
                        sbLog.AppendFormat(CultureInfo.CurrentCulture, "Deleting: {0} and {1}\r\n<br />", System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathThumbnail), mfbii.IsLocal ? System.Web.Hosting.HostingEnvironment.MapPath(mfbii.PathFullImage) : mfbii.PathFullImageS3);
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

    public class MFBImageInfoEventArgs : EventArgs
    {
        public MFBImageInfo Image { get; set; }

        public MFBImageInfoEventArgs(MFBImageInfo img = null) : base()
        {
            Image = img;
        }
    }

    [Serializable]
    public class MFBImageCollection : List<MFBImageInfo>
    {

    }
}