using MyFlightbook.CloudStorage;
using MyFlightbook.Geography;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls.ImageControls
{
    public partial class mfbMultiFileUpload : UserControl
    {
        private bool m_fHasProcessed;   // don't process twice.

        // Note: when adjusting this list, check against https://github.com/DevExpress/AjaxControlToolkit/wiki/AjaxFileUpload-setup to see if we need to edit web.config.
        private const string szFileTypesImages = "jpg,jpeg,jpe,png,heic";
        private const string szFileTypesPdf = "pdf";
        private const string szFileTypesVideos = "avi,wmv,mp4,mov,m4v,m2p,mpeg,mpg,hdmov,flv,avchd,mpeg4,m2t,h264,mp3,wav";

        private const string keyVSGPhotos = "VSGPhotosResult";
        private const string keyVSLastPhotosDate = "VSPhotosDate";

        public enum UploadMode { Legacy, Ajax };

        #region properties
        /// <summary>
        /// DO NOT USE
        /// </summary>
        public IEnumerable<mfbFileUpload> FileUploadControls
        {
            get { return new mfbFileUpload[] { mfbFu1, mfbFu2, mfbFu3, mfbFu4 }; }
        }

        /// <summary>
        /// Called when upload is complete on a single file 
        /// </summary>
        public event EventHandler UploadComplete;

        /// <summary>
        /// Called before fetching images from GooglePhotos - allows setting of things like date to fetch
        /// </summary>
        public event EventHandler FetchingGooglePhotos;

        /// <summary>
        /// Called before importing an image from GooglePhotos - allows for geotagging, if possible.
        /// </summary>
        public event EventHandler<PositionEventArgs> ImportingGooglePhoto;

        #region Google Photos import
        private GoogleMediaResponse RetrievedGooglePhotos
        {
            get
            {
                GoogleMediaResponse gmr = (GoogleMediaResponse)ViewState[keyVSGPhotos];
                if (gmr == null)
                    ViewState[keyVSGPhotos] = gmr = new GoogleMediaResponse();
                return gmr;
            }
            set { ViewState[keyVSGPhotos] = value; }
        }

        private DateTime? RetrievedGooglePhotosDate
        {
            get { return (DateTime?)ViewState[keyVSLastPhotosDate]; }
            set { ViewState[keyVSLastPhotosDate] = value; }
        }

        public DateTime GooglePhotosDateToRetrieve { get; set; }

        public bool AllowGoogleImport
        {
            get { return imgPullGoogle.Visible; }
            set { imgPullGoogle.Visible = value; }
        }
        #endregion

        /// <summary>
        /// Set to true to force the whole page to refresh (via postback) on upload
        /// </summary>
        public bool RefreshOnUpload { get; set; }

        /// <summary>
        /// Specifies the ID of the control for a postback when all files have been updated
        /// </summary>
        protected string RefreshButtonID { get; set; }

        /// <summary>
        /// Use Legacy mode (a series of file-upload controls) or the Ajax file uploader?
        /// </summary>
        public UploadMode Mode
        {
            get { return (UploadMode)mvFileUpload.ActiveViewIndex; }
            set { mvFileUpload.ActiveViewIndex = (int)value; }
        }

        private void UpdateFileTypes()
        {
            StringBuilder sb = new StringBuilder(szFileTypesImages);
            if (IncludeDocs)
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, ",{0}", szFileTypesPdf);
            if (IncludeVideos)
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, ",{0}", szFileTypesVideos);
            AjaxFileUpload1.AllowedFileTypes = sb.ToString();
        }

        private const string szVSIncludeDocs = "vsIncludeDocs";
        private const string szVSIncludeVids = "vsIncludeVids";

        /// <summary>
        /// Set to include PDF files
        /// </summary>
        public bool IncludeDocs
        {
            get
            {
                if (ViewState[szVSIncludeDocs] != null)
                    return (bool)ViewState[szVSIncludeDocs];
                return false;
            }
            set { ViewState[szVSIncludeDocs] = value; UpdateFileTypes(); }
        }

        public bool IncludeVideos
        {
            get
            {
                if (ViewState[szVSIncludeVids] != null)
                    return (bool)ViewState[szVSIncludeVids];
                return false;
            }
            set { ViewState[szVSIncludeVids] = value; UpdateFileTypes(); }
        }

        /// <summary>
        /// The allowed file types
        /// </summary>
        public string FileTypes
        {
            get { return AjaxFileUpload1.AllowedFileTypes; }
            set { AjaxFileUpload1.AllowedFileTypes = value; }
        }

        private const string sessKeyPendingIDs = "pendingIDs";

        /// <summary>
        /// The IDs of the images that are awaiting upload
        /// </summary>
        private List<string> PendingIDs
        {
            get
            {
                List<string> lst = (List<string>)Session[sessKeyPendingIDs];
                if (lst == null)
                    Session[sessKeyPendingIDs] = lst = new List<string>();
                return lst;
            }
        }
        #endregion

        protected static string FileObjectSessionKey(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            return String.Format(CultureInfo.InvariantCulture, "ajaxFileUploadObject-{0}", id.Replace(".", "_"));
        }

        public string ImageKey
        {
            get { return mfbImageListPending.Key; }
            set { mfbImageListPending.Key = value; }
        }

        public MFBImageInfo.ImageClass Class
        {
            get { return mfbImageListPending.ImageClass; }
            set { mfbImageListPending.ImageClass = value; }
        }

        protected void AjaxFileUpload1_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
                return;

            string szKey = FileObjectSessionKey(e.FileId);
            PendingIDs.Add(szKey);

            Session[szKey] = new MFBPendingImage(new MFBPostedFile(e), szKey);
            e.DeleteTemporaryData();

            RefreshPreviewList();

            if (Mode == UploadMode.Legacy)
                Mode = UploadMode.Ajax;

            UploadComplete?.Invoke(this, e);
        }

        protected MFBPendingImage AddPostedFile(MFBPostedFile pf, LatLong ll)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            string szKey = FileObjectSessionKey(pf.FileID);
            PendingIDs.Add(szKey);
            MFBPendingImage result = new MFBPendingImage(pf, szKey);
            if (ll != null)
                result.Location = ll;
            Session[szKey] = result;
            UploadComplete?.Invoke(this, new EventArgs());
            RefreshPreviewList();
            return result;
        }

        protected void RefreshPreviewList()
        {
            // Refresh the image list
            List<MFBPendingImage> lst = new List<MFBPendingImage>();
            string[] rgIDs = PendingIDs.ToArray();  // get a copy since we may modify pendingIDs
            foreach (string szID in rgIDs)
            {
                MFBPendingImage mfbpi = (MFBPendingImage)Session[szID];
                if (mfbpi != null && mfbpi.IsValid)
                    lst.Add((MFBPendingImage)Session[szID]);
                else
                {
                    Session[szID] = null;
                    PendingIDs.Remove(szID);
                }
            }
            ImageList imglist = new ImageList(lst.ToArray());
            mfbImageListPending.Images = imglist;
            mfbImageListPending.Refresh(false);
        }

        protected void AjaxFileUpload1_UploadCompleteAll(object sender, AjaxControlToolkit.AjaxFileUploadCompleteAllEventArgs e)
        {
        }

        /// <summary>
        /// Checks the type of file that is being uploaded, returns OK if it's allowed
        /// </summary>
        /// <param name="ic">The file type</param>
        /// <returns>True if it's OK</returns>
        private bool ValidateFileType(MFBImageInfo.ImageFileType ic)
        {
            switch (ic)
            {
                default:
                case MFBImageInfo.ImageFileType.Unknown:
                    return false;
                case MFBImageInfo.ImageFileType.JPEG:   // Image is always OK.
                    return true;
                case MFBImageInfo.ImageFileType.PDF:
                    return IncludeDocs;
                case MFBImageInfo.ImageFileType.S3VideoMP4:
                    return IncludeVideos;
            }
        }

        private void ProcessSingleNonAjaxUpload(mfbFileUpload fu)
        {
            if (fu.HasFile)
            {
                // skip anything that isn't an image if we're not supposed to include non-image docs.
                if (!ValidateFileType(MFBImageInfo.ImageTypeFromFile(fu.PostedFile)))
                    return;

                // Simple creation of the MFBImageInfo object will create the persisted object.
                MFBImageInfo _ = new MFBImageInfo(Class, ImageKey, fu.PostedFile, fu.Comment, null);
            }
            // clear the comment field now that it is uploaded.
            fu.Comment = string.Empty;
        }

        /// <summary>
        /// Loads the uploaded images into the specified virtual path, resets each upload control in turn
        /// </summary>
        public void ProcessUploadedImages()
        {
            if (String.IsNullOrEmpty(ImageKey))
                throw new MyFlightbookException("No Image Key specified in ProcessUploadedImages");
            if (Class == MFBImageInfo.ImageClass.Unknown)
                throw new MyFlightbookException("Unknown image class in ProcessUploadedImages");

            switch (Mode)
            {
                case UploadMode.Legacy:
                    if (m_fHasProcessed)
                        return;
                    m_fHasProcessed = true;
                    foreach (mfbFileUpload fu in FileUploadControls)
                        ProcessSingleNonAjaxUpload(fu);
                    break;
                case UploadMode.Ajax:
                    string[] rgIDs = PendingIDs.ToArray();  // make a copy of the PendingIDs, since we're going to be removing from the Pending list as we go.

                    foreach (string szID in rgIDs)
                    {
                        MFBPendingImage mfbpi = (MFBPendingImage)Session[szID];
                        if (mfbpi == null || mfbpi.PostedFile == null)
                            continue;

                        // skip anything that isn't an image if we're not supposed to include non-image docs.
                        if (ValidateFileType(MFBImageInfo.ImageTypeFromFile(mfbpi.PostedFile)))
                            mfbpi.Commit(Class, ImageKey);

                        // Regardless, clean up the temp file and 
                        Session.Remove(szID);     // free up some memory and prevent duplicate processing.
                        PendingIDs.Remove(szID);
                    }
                    RefreshPreviewList();
                    GC.Collect();    // could be a lot of memory used and/or temp files from the images, so clean them up.
                    break;
            }
        }

        protected void ProcessClipboardImage()
        {
            // Regardless of mode, see if there's a clipboard image
            if (!String.IsNullOrEmpty(hImg.Value))
            {
                try
                {
                    MFBPostedFile pf = new MFBPostedFile(hImg.Value);
                    if (ValidateFileType(MFBImageInfo.ImageTypeFromFile(pf)))
                    {
                        // create a pending file.
                        string szKey = FileObjectSessionKey(pf.FileName);
                        PendingIDs.Add(szKey);
                        Session[szKey] = new MFBPendingImage(pf, szKey);
                        RefreshPreviewList();
                        UploadComplete?.Invoke(this, new EventArgs());
                    }
                }
                catch (FormatException) { } // if it's bad data, just don't do anything.
                hImg.Value = string.Empty;  // don't send it back down the wire!!!
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            mfbFileUpload[] rgControls = FileUploadControls.ToArray();

            for (int i = 0; i < rgControls.Length - 1; i++)
                rgControls[i].OnAddAnotherClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:return ShowPanel('{0}', this);", rgControls[i + 1].DisplayID);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            RefreshPreviewList();

            ProcessClipboardImage();

            if (RefreshOnUpload)
            {
                pnlRefresh.Visible = true;
                AjaxFileUpload1.OnClientUploadCompleteAll = "ajaxFileUploadAttachments_UploadComplete";
            }
            UpdateFileTypes();
        }

        protected void btnForceRefresh_Click(object sender, EventArgs e)
        {
        }

        protected void lnkBtnForceLegacy_Click(object sender, EventArgs e)
        {
            Mode = UploadMode.Legacy;
        }

        #region Processing Google Photos

        protected void AppendMoreGoogleImages()
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);

            // Get an update
            FetchingGooglePhotos?.Invoke(this, new EventArgs());

            if (!GooglePhotosDateToRetrieve.HasValue())
                return; // nothing to do.

            DateTime? lastDate = RetrievedGooglePhotosDate;

            // flush results if the requested date has changed because the "more results" link will break otherwise.
            if (lastDate != null && lastDate.HasValue && lastDate.Value.CompareTo(GooglePhotosDateToRetrieve) != 0)
                RetrievedGooglePhotos = new GoogleMediaResponse();

            RetrievedGooglePhotosDate = GooglePhotosDateToRetrieve;

            GoogleMediaResponse result = RetrievedGooglePhotos = GooglePhoto.AppendImagesForDate(pf, GooglePhotosDateToRetrieve, IncludeVideos, RetrievedGooglePhotos).Result;

            lnkMoreGPhotos.Visible = false;
            if (result == null || !result.mediaItems.Any())
            {
                lblGPhotoResult.Text = Resources.LocalizedText.GooglePhotosNoneFound;
                result = new GoogleMediaResponse();
            }

            rptGPhotos.DataSource = result.mediaItems;
            rptGPhotos.DataBind();
            lnkMoreGPhotos.Visible = !String.IsNullOrEmpty(result.nextPageToken);
            pnlGPResult.Visible = RetrievedGooglePhotos.mediaItems.Any();
        }

        protected void imgPullGoogle_Click(object sender, EventArgs e)
        {
            RetrievedGooglePhotos = new GoogleMediaResponse();
            AppendMoreGoogleImages();
        }

        protected void lnkMoreGPhotos_Click(object sender, EventArgs e)
        {
            AppendMoreGoogleImages();
        }

        protected void imgAdd_Command(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            // Find the appropriate image
            List<GoogleMediaItem> items = new List<GoogleMediaItem>(RetrievedGooglePhotos.mediaItems);
            GoogleMediaItem clickedItem = items.Find(i => i.productUrl.CompareOrdinal((string)e.CommandArgument) == 0);

            if (clickedItem == null)
                throw new ArgumentOutOfRangeException("Can't find item with id " + e.CommandArgument);

            PositionEventArgs pea = new PositionEventArgs(null, clickedItem.mediaMetadata.CreationTime);
            ImportingGooglePhoto?.Invoke(sender, pea);

            MFBPostedFile pf = RetrievedGooglePhotos.ImportImage(e.CommandArgument.ToString());

            if (pf == null)
                return;

            MFBPendingImage pi = AddPostedFile(pf, pea.ExpectedPosition);

            rptGPhotos.DataSource = RetrievedGooglePhotos.mediaItems;
            rptGPhotos.DataBind();

            pnlGPResult.Visible = RetrievedGooglePhotos.mediaItems.Any();
        }
        #endregion
    }
}