using MyFlightbook.CloudStorage;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// MFBImageInfo that has NOT YET been persisted
    /// TODO: These are currently tied to HttpSessionStateBase, which is passed in (good) but not the right model - should instead have a collection in the session and pass the collection in/out, and interact with that.
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
        private string SessionKey { get; set; }

        public override string URLThumbnail
        {
            get
            {
                if (!IsValid)
                    return string.Empty;

                if (ImageType == ImageFileType.JPEG)
                    return String.Format(CultureInfo.InvariantCulture, "~/mvc/Image/PendingImage/{0}", HttpUtility.UrlEncode(SessionKey)).ToAbsolute();
                else if (ImageType == ImageFileType.S3VideoMP4)
                    return "~/images/pendingvideo.png".ToAbsolute();
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

                switch (ImageType)
                {
                    case ImageFileType.JPEG:
                    case ImageFileType.S3PDF:   // should never happen for a pending image...
                    case ImageFileType.PDF:
                        return String.Format(CultureInfo.InvariantCulture, "~/mvc/Image/PendingImage/{0}?full=1", HttpUtility.UrlEncode(SessionKey)).ToAbsolute();
                    case ImageFileType.S3VideoMP4:
                        return string.Empty;    // nothing to click on, at least not yet.
                    default:
                        return base.URLFullImage;
                }
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
            Init();
            PostedFile = mfbpf ?? throw new ArgumentNullException(nameof(mfbpf));
            ImageType = ImageTypeFromFile(mfbpf);
            ThumbnailFile = ThumbnailPrefix + mfbpf.FileID;
            SessionKey = szSessKey;
            if (ImageTypeFromFile(mfbpf) == ImageFileType.PDF)
                Comment = mfbpf.FileName;
        }
        #endregion

        #region Caching of in-memory objects
        public static IEnumerable<MFBPendingImage> PendingImagesInSession(HttpSessionStateBase session = null)
        {
            if (session == null)
            {
                if (HttpContext.Current?.Session == null)
                    return Array.Empty<MFBPendingImage>();
                session = new HttpSessionStateWrapper(HttpContext.Current.Session);
            }

            List<MFBPendingImage> lst = new List<MFBPendingImage>();
            foreach (string szSessKey in session.Keys)
            {
                    MFBPendingImage pi = session[szSessKey] as MFBPendingImage;
                    if (pi != null)
                        lst.Add(pi);
            }

            return lst;
        }
        #endregion

        /// <summary>
        /// Persists the pending image, returning the resulting MFBImageInfo object.
        /// </summary>
        /// <returns>The newly created MFBImageInfo object</returns>
        public MFBImageInfo Commit(ImageClass ic, string szKey)
        {
            return new MFBImageInfo(ic, szKey, PostedFile, Comment, Location);
        }

        /// <summary>
        /// Doesn't really delete (since nothing has been persisted), but removes the object from memory and invalidates it.
        /// </summary>
        public override void DeleteImage()
        {
            PostedFile?.CleanUp();
            PostedFile = null;
            ThumbnailFile = string.Empty;
            if (SessionKey != null && HttpContext.Current?.Session?[SessionKey] != null)
                HttpContext.Current.Session.Remove(SessionKey);
        }

        public override void UpdateAnnotation(string szText)
        {
            Comment = szText;
        }

        /// <summary>
        /// Pull in an image from google photos, usin the user's accesstoken, and optionally geo-tagging it
        /// </summary>
        /// <param name="flightData">Optional data for the flight for geotagging</param>
        /// <param name="accessToken">Required - the user's access token</param>
        /// <param name="item">The item to import.</param>
        /// <param name="key">A unique key for the image</param>
        /// <returns>The imported image as a pending image</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<MFBPendingImage> FromGooglePhoto(string flightData, string accessToken, GPickerMediaItem item, string key)
        {
            MFBPostedFile pf = await item?.ImportImage(accessToken) ?? throw new InvalidOperationException("Unable to import image");
            MFBPendingImage pi = new MFBPendingImage(pf, key);

            // Geo tag, if  possible
            if (item.createTime.HasValue && !String.IsNullOrEmpty(flightData))
            {
                using (Telemetry.FlightData fd = new Telemetry.FlightData())
                {
                    if (fd.ParseFlightData(flightData) && fd.HasDateTime && fd.HasLatLongInfo)
                        pi.Location = Position.Interpolate(item.createTime.Value, fd.GetTrajectory());
                }
            }

            return pi;
        }

        /// <summary>
        /// Process the result from the google photo picker, producing pending images
        /// </summary>
        /// <param name="szJSONResponse">A JSON string representing a GPickerMediaResponse</param>
        /// <param name="flightData">Any flight data (for geotagging)</param>
        /// <param name="userName">user for whom this is being called</param>
        /// <param name="onProcess">An action called on each pending image; commit it or stash it away somewhere as needed</param>
        /// <returns>true if no errors were encountered</returns>
        public static async Task<bool> ProcessSelectedPhotosResult(string userName, string szJSONResponse, string flightData, Action<MFBPendingImage, string> onProcess)
        {
            if (String.IsNullOrEmpty(szJSONResponse))
                return false;
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            if (onProcess == null)
                throw new ArgumentNullException(nameof(onProcess));

            Profile pf = Profile.GetUser(userName);
            string accessToken = new GooglePhoto(pf).AuthState.AccessToken;
            if (String.IsNullOrEmpty(accessToken))
                throw new UnauthorizedAccessException("Attempt to fetch Google photo without an access token!");

            GPickerMediaResponse gmr = JsonConvert.DeserializeObject<GPickerMediaResponse>(szJSONResponse);

            int i = 0;
            foreach (GPickerMediaItem item in gmr.mediaItems)
            {
                string szKey = String.Format(CultureInfo.InvariantCulture, "googlePhoto_{0}_{1}", i++, DateTime.Now.Ticks);
                MFBPendingImage pi = await FromGooglePhoto(flightData, accessToken, item, szKey);
                onProcess(pi, szKey);
            }

            return true;
        }

        public static string ProcessUploadedFile(IPostedImageFile file, ImageClass ic, Func<ImageFileType, bool> checkType, Action<MFBPendingImage, string> onProcess)
        {
            if (onProcess == null)
                throw new ArgumentNullException(nameof(onProcess));
            if (checkType == null)
                throw new ArgumentNullException(nameof(checkType));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            MFBPostedFile pf = new MFBPostedFile(file);
            string szID = String.Format(CultureInfo.InvariantCulture, "{0}-pendingImage-{1}-{2}", ic.ToString(), (pf.FileName ?? string.Empty).Replace(".", "_"), pf.GetHashCode());
            MFBPendingImage pi = new MFBPendingImage(pf, szID);

            if (!checkType(ImageTypeFromFile(pf)))
                return string.Empty;

            onProcess(pi, szID);

            return pi.URLThumbnail.ToAbsolute();
        }
    }
}