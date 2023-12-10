using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditableImage : UserControl
{
    private MFBImageInfo mfbii;
    private string m_szAltTextDefault = "";
    private GeoLinkType m_zoomLinkType = GeoLinkType.None;

    public event EventHandler<MFBImageInfoEventArgs> ImageDeleted;
    public event EventHandler<MFBImageInfoEventArgs> ImageMadeDefault;
    public event EventHandler<MFBImageInfoEventArgs> ImageModified;

    /// <summary>
    /// Gets/sets the image which can be edited.
    /// </summary>
    public MFBImageInfo MFBImageInfo
    {
        get { return mfbii; }
        set { mfbii = value; InitFromImage(); }
    }

    /// <summary>
    /// Default alternative text to use when the image has none.
    /// </summary>
    public string AltText
    {
        get { return m_szAltTextDefault; }
        set { m_szAltTextDefault = value; }
    }

    /// <summary>
    /// Can the image be deleted?
    /// </summary>
    public bool CanDelete
    {
        get { return lnkDelete.Visible; }
        set
        { 
            divDel.Visible = lnkDelete.Visible = value;
            cbeDelete.Enabled = !String.IsNullOrEmpty(cbeDelete.ConfirmText);
        }
    }

    public string ConfirmText
    {
        get { return cbeDelete.ConfirmText; }
        set
        { 
            cbeDelete.ConfirmText = value;
            cbeDelete.Enabled = !String.IsNullOrEmpty(value);
        }
    }

    /// <summary>
    /// Can the image have its comment edited?
    /// </summary>
    public bool CanEdit
    {
        get { return lnkAnnotate.Visible; }
        set { divAnnot.Visible = lnkAnnotate.Visible = value; }
    }

    /// <summary>
    /// Can you make this image the default in a group of images?
    /// </summary>
    public bool CanMakeDefault
    {
        get { return lnkMakeDefault.Visible; }
        set { divDefault.Visible = lnkMakeDefault.Visible = value; }
    }

    private bool _isDefault;
    /// <summary>
    /// Indicates that this one is the default image for this user.
    /// </summary>
    public bool IsDefault
    {
        get { return _isDefault; }
        set
        {
            _isDefault = value;
            lnkMakeDefault.ImageUrl = value ? "~/images/favoritefilledsm.png" : "~/images/favoritesm.png";
        }
    }

    /// <summary>
    /// Show a link to zoom in on the image?
    /// </summary>
    public GeoLinkType ZoomLinkType
    {
        get { return m_zoomLinkType; }
        set { m_zoomLinkType = value; }
    }

    protected void InitFromImage()
    {
        img.ImageUrl = mfbii.URLThumbnail;
        img.Width = (mfbii.WidthThumbnail == 0) ? Unit.Empty : mfbii.WidthThumbnail;
        img.Height = (mfbii.HeightThumbnail == 0) ? Unit.Empty : mfbii.HeightThumbnail;
        string szTitle = String.IsNullOrEmpty(AltText) ? mfbii.Comment : AltText;
        img.AlternateText = szTitle;
        img.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, mfbii.ImageType == MFBImageInfoBase.ImageFileType.PDF || mfbii.ImageType == MFBImageInfoBase.ImageFileType.S3PDF ? "javascript:window.location = '{0}';" : "javascript:viewMFBImg('{0}');", ResolveClientUrl(mfbii.URLFullImage));
        if (!String.IsNullOrEmpty(szTitle))
            img.Attributes["title"] = szTitle; // for firefox compat
        txtComments.Text = mfbii.Comment;
        lblComments.Text = mfbii.Comment.EscapeHTML();

        if (mfbii.ImageType == MFBImageInfoBase.ImageFileType.S3VideoMP4 && !(mfbii is MFBPendingImage))
        {
            litVideoOpenTag.Text = String.Format(CultureInfo.InvariantCulture, "<video width=\"320\" height=\"240\" controls><source src=\"{0}\"  type=\"video/mp4\">", mfbii.ResolveFullImage());
            litVideoCloseTag.Text = "</video>";
            pnlStatic.Style["max-width"] = "320px";
        }
        else if (mfbii.WidthThumbnail > 0)
        {
            pnlStatic.Style["max-width"] = img.Width.ToString(CultureInfo.InvariantCulture);
        }

        if (mfbii.Location != null)
        {
            switch (ZoomLinkType)
            {
                case GeoLinkType.None:
                    break;
                case GeoLinkType.ZoomOnLocalMap:
                    divZoom.Visible = lnkZoom.Visible = true;
                    lnkZoom.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:getGMap().setCenter(new google.maps.LatLng({0}, {1}));getGMap().setZoom(12);", mfbii.Location.Latitude, mfbii.Location.Longitude);
                    break;
                case GeoLinkType.ZoomOnGoogleMaps:
                    divZoom.Visible = lnkZoom.Visible = true;
                    lnkZoom.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "https://maps.google.com?q={0},{1}", mfbii.Location.LatitudeString, mfbii.Location.LongitudeString);
                    lnkZoom.Target = "_blank";
                    break;
            }
        }
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
        // We do this late in the cycle because our ID may have changed between databinding and now.
        lnkAnnotate.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:EditComment('{0}', '{1}');", pnlStatic.ClientID, pnlEdit.ClientID);
        pnlEdit.Visible = CanEdit;  // don't bother with any of the editing panel if you can't edit...
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        string szEditComment = @"
function EditComment(idStatic, idEdit) {
    document.getElementById(idStatic).style.display = 'none';
    document.getElementById(idEdit).style.display = 'block';
}";

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "EditComment", szEditComment, true);

        if (mfbii != null)
            divActions.Visible = lnkAnnotate.Visible || lnkDelete.Visible || lnkMakeDefault.Visible || lnkZoom.Visible;
    }

    /// <summary>
    /// Updates the comments on the file to match the text box.
    /// </summary>
    protected void btnUpdateComments_Click(object sender, EventArgs e)
    {
        if (txtComments.Text != mfbii.Comment)
        {
            mfbii.UpdateAnnotation(HttpUtility.HtmlDecode(txtComments.Text));
            lblComments.Text = mfbii.Comment.EscapeHTML();
            ImageModified?.Invoke(sender, new MFBImageInfoEventArgs(mfbii));
        }
    }

    #region Events
    protected void lnkMakeDefault_Click(object sender, ImageClickEventArgs e)
    {
        if (mfbii != null && ImageMadeDefault != null)
            ImageMadeDefault(this, new MFBImageInfoEventArgs(mfbii));
    }

    protected void DeleteImage(object sender, EventArgs e)
    {
        if (mfbii != null)
        {
            // Notify of the deletion before actually deleting it...
            ImageDeleted?.Invoke(sender, new MFBImageInfoEventArgs(mfbii));
            // ...then delete it
            mfbii.DeleteImage();
        }
    }
    #endregion
}
