using Amazon.S3;
using Amazon.S3.Model;
using AWSNotifications;
using gma.Drawing.ImageInfo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Web.Hosting;

/******************************************************
 * 
 * Copyright (c) 2008-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// A video that has been submitted to Amazon for transcoding and is awaiting a response
    /// </summary>
    public class PendingVideo
    {
        #region Properties
        /// <summary>
        /// The GUID that is the basis for the filename
        /// </summary>
        public string GUID { get; private set; }

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
                    SubmissionTime = DateTime.SpecifyKind(Convert.ToDateTime(dr["Submitted"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
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
                throw new ArgumentNullException(nameof(szBasePath));

            if (szBasePath.StartsWith("/", StringComparison.Ordinal))
                szBasePath = szBasePath.Substring(1);

            // Copy the thumbnail over
            using (IAmazonS3 s3 = AWSConfiguration.S3Client())
            {
                // Get everything that's in the folder with this guid
                List<S3Object> lstS3Objects = new List<S3Object>();
                ListObjectsRequest loRequest = new ListObjectsRequest() { BucketName = Bucket, Prefix = szBasePath };
                S3Object thumbnail = null;
                S3Object originalVideo = null;
                S3Object mp4Video = null;

                // Get the list of S3 objects that contain the guid
                // This should be 3 objects:
                //  * the original uploade .vid file
                //  * a .jpg thumbnail file
                //  * an .mp4 video.
                do
                {
                    ListObjectsResponse response = s3.ListObjects(loRequest);
                    foreach (S3Object o in response.S3Objects)
                    {
                        if (o.Key.Contains(GUID))
                        {
                            lstS3Objects.Add(o);
                            switch (Path.GetExtension(o.Key).ToLowerInvariant())
                            {
                                case FileExtensions.JPEG:
                                case FileExtensions.JPG:
                                    thumbnail = o;
                                    break;
                                case FileExtensions.VidInProgress:
                                    originalVideo = o;
                                    break;
                                case FileExtensions.MP4:
                                    mp4Video = o;
                                    break;
                            }
                        }
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                        loRequest.Marker = response.NextMarker;
                    else
                        loRequest = null;
                } while (loRequest != null);

                if (thumbnail == null)
                    throw new InvalidOperationException($"Thumbnail not found for pending video {GUID} in bucket {Bucket}");

                try
                {
                    using (GetObjectResponse gor = s3.GetObject(new GetObjectRequest() { BucketName = Bucket, Key = thumbnail.Key }))
                    {
                        if (gor?.ResponseStream != null)
                        {
#pragma warning disable IDISP007 // Don't dispose injected
                            // Amazon documents EXPLICITLY say we should wrap in a using block.  See https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/P_Amazon_S3_Model_S3Response_ResponseStream.htm
                            using (gor.ResponseStream)
#pragma warning restore IDISP007 // Don't dispose injected.
                            {
                                using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(gor.ResponseStream))
                                {
                                    Info inf = MFBImageInfo.InfoFromImage(image);
                                    // save the thumbnail locally.
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

                    // success?  Rename the thumbnail file to match the old elastic transcoder naming and delete the source .vid file
                    string thumbFileName = Path.GetFileName(thumbnail.Key);
                    string thumbFileNew = thumbnail.Key.Replace(thumbFileName, MFBImageInfoBase.ThumbnailPrefixVideo + GUID + "00001" + FileExtensions.JPG);
                    CopyObjectRequest coreq = new CopyObjectRequest() { SourceBucket = Bucket, SourceKey = thumbnail.Key, DestinationBucket = Bucket, DestinationKey = thumbFileNew };
                    CopyObjectResponse cor = s3.CopyObject(coreq);
                    if (cor.ETag.CompareCurrentCultureIgnoreCase(thumbnail.ETag) == 0)
                        s3.DeleteObject(new DeleteObjectRequest() { BucketName = Bucket, Key = thumbnail.Key });

                    if (originalVideo != null)
                        s3.DeleteObject(new DeleteObjectRequest() { BucketName = Bucket, Key = originalVideo.Key });

                    // Now set the ACLs so that it's all public
                    s3.PutACL(new PutACLRequest() { BucketName = Bucket, Key = thumbFileNew, CannedACL = S3CannedACL.PublicRead });
                    s3.PutACL(new PutACLRequest() { BucketName = Bucket, Key = mp4Video.Key, CannedACL = S3CannedACL.PublicRead });
                }
                catch (AmazonS3Exception)
                {
                    // Thumbnail was not found - audio file perhaps?  Use the generic audio file.
                    System.IO.File.Copy(HostingEnvironment.MapPath("~/images/audio.png"), szPhysicalPath);
                }
            }
        }

        public static IEnumerable<int> ProcessPendingVideos(out string szSummary)
        {
            List<SNSNotification> lstPending = new List<SNSNotification>();
            List<int> lstFlights = new List<int>();

            // Get all pending videos that are more than an hour old and create synthetic SNS notifications for them.
            DBHelper dbh = new DBHelper("SELECT * FROM pendingvideos pv WHERE submitted < DATE_ADD(Now(), INTERVAL -1 HOUR)");
            dbh.ReadRows((comm) => { },
            (dr) =>
            {
                SNSNotification sns = new SNSNotification() { Message = JsonConvert.SerializeObject(new { detail = new { jobId = (string)dr["jobID"], status = "COMPLETE" } }) };
                lstPending.Add(sns);
                lstFlights.Add(Convert.ToInt32(dr["imagekey"], CultureInfo.InvariantCulture));
            });

            int cPending = lstPending.Count;

            // Now, go through them and create each one.  Should clean up as part of the process.
            // simply creating the object will do all that is necessary.
            foreach (SNSNotification sns in lstPending)
                _ = new MFBImageInfo(sns);

            int cRemaining = 0;
            dbh.CommandText = "SELECT count(*) AS numRemaining FROM pendingvideos pv WHERE submitted < DATE_ADD(Now(), INTERVAL -1 HOUR)";
            dbh.ReadRow((comm) => { }, (dr) => { cRemaining = Convert.ToInt32(dr["numRemaining"], CultureInfo.InvariantCulture); });

            szSummary = String.Format(CultureInfo.CurrentCulture, "Found {0} videos orphaned, {1} now remain", cPending, cRemaining);
            return lstFlights;
        }
    }
}