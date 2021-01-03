using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
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

                switch (ImageType)
                {
                    case ImageFileType.JPEG:
                    case ImageFileType.S3PDF:   // should never happen for a pending image...
                    case ImageFileType.PDF:
                        return String.Format(CultureInfo.InvariantCulture, "~/Public/PendingImg.aspx?i={0}&full=1", HttpUtility.UrlEncode(SessionKey));
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
            PostedFile?.CleanUp();
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
}