using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbMultiFileUpload : System.Web.UI.UserControl
{
    private const int MaxFiles = 5;

    private Controls_mfbFileUpload[] rgmfbFu;
    private bool m_fHasProcessed = false;   // don't process twice.

    // Note: when adjusting this list, check against https://github.com/DevExpress/AjaxControlToolkit/wiki/AjaxFileUpload-setup to see if we need to edit web.config.
    private const string szFileTypesImages = "jpg,jpeg,jpe,png,heic";
    private const string szFileTypesPdf = "pdf";
    private const string szFileTypesVideos = "avi,wmv,mp4,mov,m4v,m2p,mpeg,mpg,hdmov,flv,avchd,mpeg4,m2t,h264";

    public enum UploadMode {Legacy, Ajax};

    #region properties
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public IEnumerable<Controls_mfbFileUpload> FileUploadControls
    {
        get { return rgmfbFu; }
    }

    /// <summary>
    /// Called when upload is complete on a single file 
    /// </summary>
    public event EventHandler UploadComplete = null;

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
        get { return (UploadMode) mvFileUpload.ActiveViewIndex; }
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

    protected string FileObjectSessionKey(string id)
    {
        return String.Format(CultureInfo.InvariantCulture, "ajaxFileUploadObject-{0}", id);
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
            throw new ArgumentNullException("e");
        if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            return;

        string szKey = FileObjectSessionKey(e.FileId);
        PendingIDs.Add(szKey);
        Session[szKey] = new MFBPendingImage(new MFBPostedFile(e.FileName, e.ContentType, e.FileSize, e.GetContents(), e.FileId), szKey);
        e.DeleteTemporaryData();

        RefreshPreviewList();

        if (Mode == UploadMode.Legacy)
            Mode = UploadMode.Ajax;

        if (UploadComplete != null)
            UploadComplete(this, e);
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

    /// <summary>
    /// Loads the uploaded images into the specified virtual path, resets each upload control in turn
    /// </summary>
    public void ProcessUploadedImages()
    {
        MFBImageInfo mfbii;

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
                if (rgmfbFu == null)
                    throw new MyFlightbookValidationException("rgmfbu is null in mfbMultiFileUpload; shouldn't be.");
                foreach (Controls_mfbFileUpload fu in rgmfbFu)
                {
                    if (fu.HasFile)
                    {
                        // skip anything that isn't an image if we're not supposed to include non-image docs.
                        if (!ValidateFileType(MFBImageInfo.ImageTypeFromFile(fu.PostedFile)))
                            continue;

                        mfbii = new MFBImageInfo(Class, ImageKey, fu.PostedFile, fu.Comment, null);
                    }
                    // clear the comment field now that it is uploaded.
                    fu.Comment = "";
                }
                break;
            case UploadMode.Ajax:
                string[] rgIDs = PendingIDs.ToArray();  // make a copy of the PendingIDs, since we're going to be removing from the Pending list as we go.

                foreach (string szID in rgIDs)
                {
                    MFBPendingImage mfbpi = (MFBPendingImage) Session[szID];
                    if (mfbpi == null || mfbpi.PostedFile == null)
                        continue;

                    // skip anything that isn't an image if we're not supposed to include non-image docs.
                    if (!ValidateFileType(MFBImageInfo.ImageTypeFromFile(mfbpi.PostedFile)))
                        continue;

                    mfbii = mfbpi.Commit(Class, ImageKey);
                    Session[FileObjectSessionKey(szID)] = null;     // free up some memory and prevent duplicate processing.
                    PendingIDs.Remove(szID);
                }
                RefreshPreviewList();
                break;
        }
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        rgmfbFu = new Controls_mfbFileUpload[MaxFiles];

        for (int i = 0; i < MaxFiles; i++)
        {
            rgmfbFu[i] = (Controls_mfbFileUpload) LoadControl("~/Controls/mfbFileUpload.ascx");
            rgmfbFu[i].ID = "mfbFu" + i.ToString(CultureInfo.InvariantCulture);

            // Hide all but the first one
            if (i > 0)
                rgmfbFu[i].Display = "none";

            // And wire them up
            PlaceHolder1.Controls.Add(rgmfbFu[i]);
        }

        // Now iterate through the items again to wire up the "add another" links; we do this after doing above so that all of the ClientIDs can be correctly wired
        for (int i = 0; i < MaxFiles - 1; i++)
        {
            rgmfbFu[i].AddAnotherLink.Attributes["onclick"] = String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:return ShowPanel('{0}', this);", rgmfbFu[i + 1].DisplayID);
        }

        // hide the last link
        rgmfbFu[MaxFiles - 1].AddAnotherVisible = false;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        const string szJSShowPanel = @"function ShowPanel(id, sender) {
document.getElementById(id).style.display='block';
sender.style.display='none';
return false;
}";
        Page.ClientScript.RegisterClientScriptBlock(GetType(), "displayfileupload", szJSShowPanel, true);

        RefreshPreviewList();

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
}
