using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

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
                    return String.Format(CultureInfo.InvariantCulture, "~/mvc/Image/PendingImage/{0}", Uri.EscapeDataString(SessionKey)).ToAbsolute();
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
                        return String.Format(CultureInfo.InvariantCulture, "~/mvc/Image/PendingImage/{0}?full=1", Uri.EscapeDataString(SessionKey)).ToAbsolute();
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
        public static IEnumerable<MFBPendingImage> PendingImagesInSession()
        {
            List<MFBPendingImage> lst = new List<MFBPendingImage>();
            foreach (string szSessKey in util.RequestContext.SessionKeys)
            {
                MFBPendingImage pi = util.RequestContext.GetSessionValue(szSessKey) as MFBPendingImage;
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
        public async Task<MFBImageInfo> Commit(ImageClass ic, string szKey)
        {
            return await new MFBImageInfo(ic, szKey).InitWithFile(PostedFile, Comment, Location);
        }

        /// <summary>
        /// Doesn't really delete (since nothing has been persisted), but removes the object from memory and invalidates it.
        /// </summary>
        public override void DeleteImage()
        {
            PostedFile?.CleanUp();
            PostedFile = null;
            ThumbnailFile = string.Empty;
            util.RequestContext.SetSessionValue(SessionKey, null);
        }

        public override void UpdateAnnotation(string szText)
        {
            Comment = szText;
        }
                
        /// <summary>
        /// Processes the uploaded file for the specified imageclass and key.  Commits if the key is non-empty
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="ic">Image class</param>
        /// <param name="szKey">The key; if null or empty, no commit will be attempted</param>
        /// <param name="checkType">Lambda to verify that the type is allowed (e.g., videos or PDFs)</param>
        /// <param name="onNoSave">If the file is NOT saved, pending image back to the caller (e.g., to store in a session or cache or database)</param>
        /// <returns>The absolute URL thumbnail to the resulting image</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<string> ProcessUploadedFile(IPostedImageFile file, ImageClass ic, string szKey, Func<ImageFileType, bool> checkType, Action<MFBPendingImage, string> onNoSave)
        {
            if (onNoSave == null)
                throw new ArgumentNullException(nameof(onNoSave));
            if (checkType == null)
                throw new ArgumentNullException(nameof(checkType));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            MFBPostedFile pf = new MFBPostedFile(file);
            string szID = String.Format(CultureInfo.InvariantCulture, "{0}-pendingImage-{1}-{2}", ic.ToString(), (pf.FileName ?? string.Empty).Replace(".", "_"), pf.GetHashCode());
            MFBPendingImage pi = new MFBPendingImage(pf, szID);

            if (!checkType(ImageTypeFromFile(pf)))
                return string.Empty;

            if (!String.IsNullOrEmpty(szKey))
                _ = await pi.Commit(ic, szKey.ToString(CultureInfo.InvariantCulture));
            else
                onNoSave(pi, szID);

            return pi.URLThumbnail.ToAbsolute();
        }
    }
}