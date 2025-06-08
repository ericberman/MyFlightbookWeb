using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
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
                if (HttpContext.Current?.Request?.Url?.Host != null && Branding.CurrentBrand?.HostName != null)
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
            get { return UseDebugBucket ? S3BucketNameDebug : Branding.CurrentBrand.AWSBucket; }
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
            return new AmazonS3Client(AWSAccessKey, AWSSecretKey, RegionEndpoint.USEast1);
        }

        public static IAmazonMediaConvert MediaConvertClient()
        {
            return new AmazonMediaConvertClient(AWSAccessKey, AWSSecretKey, RegionEndpoint.USEast1);
        }

        public static Input InputForBucketKey(string szBucket, string szKey)
        {
            return new Input
            {
                FileInput = $"s3://{szBucket}/{szKey}",
                AudioSelectors = new Dictionary<string, AudioSelector>
                {
                    { "Audio Selector 1", new AudioSelector { Offset = 0, DefaultSelection = "DEFAULT", SelectorType = "LANGUAGE_CODE", ProgramSelection = 1, LanguageCode = "ENM" } }
                },
                VideoSelector = new VideoSelector
                {
                    Rotate = "AUTO" // Set to your desired rotation: DEGREE_0, DEGREE_90, DEGREE_180, or DEGREE_270 }
                }
            };
        }

        public static List<Output> DefaultVideoOutputs
        {
            get { return new List<Output>() { new Output { Preset = "MFB-Video-Preset" }, new Output { Preset = "MFB-Video-Thumbnail-Preset" } }; }
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
        private static string WrapAmazonS3Exception(AmazonS3Exception ex)
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
        public static void MoveImageOnS3(string szSrc, string szDst)
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
                    cor.StorageClass = Amazon.S3.S3StorageClass.Standard; // vs. reduced

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
        public static void DeleteImageOnS3(MFBImageInfo mfbii)
        {
            if (mfbii == null)
                return;

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
                            s3.DeleteObject(dor);
                            if (mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4)
                            {
                                // Delete the thumbnail too.
                                string szs3Key = mfbii.S3Key;
                                dor.Key = szs3Key.Replace(Path.GetFileName(szs3Key), MFBImageInfo.ThumbnailPrefixVideo + Path.GetFileNameWithoutExtension(szs3Key) + "00001" + FileExtensions.JPG);
                                s3.DeleteObject(dor);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is AmazonS3Exception) { }
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
                throw new ArgumentNullException(nameof(mfbii));
            if (por == null)
                throw new ArgumentNullException(nameof(por));
            lock (lockObject)
            {
                try
                {
                    using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                    {
                        PutObjectResponse s3r = null;
                        using (por.InputStream = new FileStream(mfbii.PhysicalPathFull, FileMode.Open, FileAccess.Read))
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
                                        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is FileNotFoundException || ex is IOException)
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
            if (mfbii == null)
                return;

            try
            {
                PutObjectRequest por = new PutObjectRequest()
                {
                    BucketName = AWSConfiguration.CurrentS3Bucket,
                    ContentType = mfbii.ImageType == MFBImageInfo.ImageFileType.JPEG ? ContentTypeJPEG : ContentTypePDF,
                    AutoCloseStream = true,
                    Key = mfbii.S3Key,
                    CannedACL = S3CannedACL.PublicRead,
                    StorageClass = Amazon.S3.S3StorageClass.Standard // vs. reduced
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

        #region Video Support
        /// <summary>
        /// Uploads a video for transcoding.  Deletes the source file named in szFileName
        /// </summary>
        /// <param name="szFileName">Filename of the input stream</param>
        /// <param name="szContentType">Content type of the video file</param>
        /// <param name="szBucket">Bucket to use.  Specified as a parameter because CurrentBucket might return the wrong value when called on a background thread</param>
        /// <param name="mfbii">The MFBImageInfo that encapsulates the video</param>
        /// <param name="fDelete">True to delete the file after upload</param>
        public void UploadVideo(string szFileName, string szContentType, string szBucket, MFBImageInfo mfbii, bool fDelete)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));

            using (var mcClient = AWSConfiguration.MediaConvertClient())
            {
                string szGuid = Guid.NewGuid() + MFBImageInfo.szNewS3KeySuffix;
                string szBasePath = mfbii.VirtualPath.StartsWith("/", StringComparison.Ordinal) ? mfbii.VirtualPath.Substring(1) : mfbii.VirtualPath;
                string szBaseFile = String.Format(CultureInfo.InvariantCulture, "{0}{1}", szBasePath, szGuid);

                PutObjectRequest por = new PutObjectRequest()
                {
                    BucketName = szBucket,
                    ContentType = szContentType,
                    AutoCloseStream = true,
                    Key = szBaseFile + FileExtensions.VidInProgress,
                    CannedACL = S3CannedACL.PublicRead,
                    StorageClass = Amazon.S3.S3StorageClass.Standard // vs. reduced
                };

                lock (lockObject)
                {
                    try
                    {
                        using (por.InputStream = new FileStream(szFileName, FileMode.Open, FileAccess.Read))
                        {
                            using (IAmazonS3 s3 = AWSConfiguration.S3Client())
                                s3.PutObject(por);
                            File.Delete(szFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        util.NotifyAdminEvent("Error putting video file", ex.Message, ProfileRoles.maskSiteAdminOnly);
                        throw;
                    }
                }

                var job = mcClient.CreateJob(new CreateJobRequest()
                {
                    Role = LocalConfig.SettingForKey("AWSMediaConvertRoleArn"),
                    UserMetadata = new Dictionary<string, string> { { "Debug", (szBucket.CompareCurrentCultureIgnoreCase(AWSConfiguration.S3BucketNameDebug) == 0).ToString() } },
                    Settings = new JobSettings()
                    {
                       
                        Inputs = new List<Input>  { AWSConfiguration.InputForBucketKey(szBucket, por.Key) },
                        OutputGroups = new List<OutputGroup>
                        {
                            new OutputGroup
                            {
                                OutputGroupSettings = new OutputGroupSettings()
                                {
                                    Type = OutputGroupType.FILE_GROUP_SETTINGS,
                                    FileGroupSettings = new FileGroupSettings() { Destination = $"s3://{szBucket}/{szBasePath}"}
                                },
                                Outputs = AWSConfiguration.DefaultVideoOutputs
                            }
                        }
                    }
                });
                if (job != null)
                    new PendingVideo(szGuid, job.Job.Id, mfbii.Comment, mfbii.Class, mfbii.Key, szBucket).Commit();

                if (fDelete && File.Exists(szFileName))
                    File.Delete(szFileName);
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
            string szBasePath = MFBImageInfo.BasePathFromClass(ic);
            if (szBasePath.StartsWith("/", StringComparison.InvariantCultureIgnoreCase))
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
                        cBytesOnS3 += o.Size ?? 0;
                        lstS3Objects.Add(o);
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated ?? false)
                        request.Marker = response.NextMarker;
                    else
                        request = null;
                } while (request != null);

                onS3EnumDone?.Invoke();

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

                    bool fPDF = (String.Compare(szExt, FileExtensions.PDF, StringComparison.InvariantCultureIgnoreCase) == 0);
                    bool fVid = (String.Compare(szExt, FileExtensions.MP4, StringComparison.InvariantCultureIgnoreCase) == 0);
                    bool fVidThumb = Regex.IsMatch(szExt, FileExtensions.RegExpImageFileExtensions) && szName.StartsWith(MFBImageInfo.ThumbnailPrefixVideo, StringComparison.InvariantCultureIgnoreCase);
                    bool fJpg = (String.Compare(szExt, FileExtensions.JPG, StringComparison.InvariantCultureIgnoreCase) == 0 || String.Compare(szExt, FileExtensions.JPEG, StringComparison.InvariantCultureIgnoreCase) == 0);

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
                    if (!dictDBResults.Remove(szPrimary))
                    {
                        cOrphansFound++;
                        cBytesToFree += o.Size ?? 0;
                        if (onDelete(o.Key, (int)((100 * cOrphansFound) / cOrphansLikely)))
                        {
                            // Make sure that the item 
                            DeleteObjectRequest dor = new DeleteObjectRequest() { BucketName = AWSConfiguration.S3BucketName, Key = o.Key };
                            s3.DeleteObject(dor);
                        }
                    }
                });

                onSummary?.Invoke(cFilesOnS3, cBytesOnS3, cOrphansFound, cBytesToFree);
            }
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
                    if (response.IsTruncated ?? false)
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
                if (mfbii.ImageType == MFBImageInfoBase.ImageFileType.JPEG)
                    mfbii.Update(); // write the meta-data to both thumbnail & full file

                FileInfo fi = new FileInfo(szPathFull);
                cBytes += (Int32)fi.Length;
                MoveImageToS3(true, mfbii);

                return cBytes;
            }

            return -1;
        }

        /// <summary>
        /// Migrates files that are on the server to AWS
        /// </summary>
        /// <param name="cMBytesLimit">Maximum bytes to move (could be exceeded a bit) - i.e., stop moving when you are past this</param>
        /// <param name="cFilesLimit">Maximum files to move</param>
        /// <param name="imageClass">Image class on which to perform the operation.</param>
        /// <returns></returns>
        public static string MigrateImages(int cMBytesLimit, int cFilesLimit, MFBImageInfoBase.ImageClass imageClass)
        {
            int cBytesDone = 0;
            int cFilesDone = 0;

            int cBytesLimit = cMBytesLimit * 1024 * 1024;

            Dictionary<string, MFBImageCollection> images = MFBImageInfo.FromDB(imageClass, true);

            List<string> lstKeys = images.Keys.ToList();
            if (imageClass == MFBImageInfoBase.ImageClass.Aircraft || imageClass == MFBImageInfoBase.ImageClass.Flight)
                lstKeys.Sort((sz1, sz2) => { return Convert.ToInt32(sz2, CultureInfo.InvariantCulture) - Convert.ToInt32(sz1, CultureInfo.InvariantCulture); });
            else
                lstKeys.Sort();

            // ImageDict
            foreach (string szKey in lstKeys)
            {
                if (cBytesDone > cBytesLimit || cFilesDone >= cFilesLimit)
                    break;
                AWSImageManagerAdmin im = new AWSImageManagerAdmin();
                foreach (MFBImageInfo mfbii in images[szKey])
                {
                    int cBytes = im.ADMINMigrateToS3(mfbii);
                    if (cBytes >= 0)  // migration occured
                    {
                        cBytesDone += cBytes;
                        cFilesDone++;
                    }

                    if (cBytesDone > cBytesLimit || cFilesDone >= cFilesLimit)
                        break;
                }
            }

            return String.Format(CultureInfo.CurrentCulture, Resources.Admin.MigrateImagesTemplate, cFilesDone, cBytesDone);
        }
    }
}