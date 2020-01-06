using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditableImage : System.Web.UI.UserControl
{
    private MFBImageInfo mfbii = null;
    private string m_szAltTextDefault = "";
    private GeoLinkType m_zoomLinkType = GeoLinkType.None;

    public enum GeoLinkType { None, ZoomOnLocalMap, ZoomOnGoogleMaps };

    public event System.EventHandler<MFBImageInfoEvent> ImageDeleted = null;
    public event System.EventHandler<MFBImageInfoEvent> ImageMadeDefault = null;

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
        set { divDel.Visible = lnkDelete.Visible = value; }
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
        img.AlternateText = (AltText.Length == 0) ? mfbii.Comment : AltText;
        img.Attributes["title"] = (AltText.Length == 0) ? mfbii.Comment : AltText; // for firefox compat
        lnkFullPicture.NavigateUrl = mfbii.URLFullImage;
        txtComments.Text = mfbii.Comment;
        lblComments.Text = mfbii.Comment.EscapeHTML();

        if (mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4 && !(mfbii is MFBPendingImage))
        {
            litVideoOpenTag.Text = String.Format(CultureInfo.InvariantCulture, "<video width=\"320\" height=\"240\" controls><source src=\"{0}\"  type=\"video/mp4\">", mfbii.ResolveFullImage());
            litVideoCloseTag.Text = "</video>";
            pnlStatic.Style["max-width"] = "320px";
        }
        else if (mfbii.WidthThumbnail > 0)
        {
            pnlStatic.Style["max-width"] = img.Width.ToString();
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
                    lnkZoom.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://maps.google.com?q={0},{1}", mfbii.Location.LatitudeString, mfbii.Location.LongitudeString);
                    lnkZoom.Target = "_blank";
                    break;
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        string szEditComment = @"
function EditComment(idStatic, idEdit) {
    document.getElementById(idStatic).style.display = 'none';
    document.getElementById(idEdit).style.display = 'block';
}";

        Page.ClientScript.RegisterClientScriptBlock(GetType(), "EditComment", szEditComment, true);

        pnlEdit.Visible = CanEdit;  // don't bother with any of the editing panel if you can't edit...

        if (IsPostBack)
        {
            // Get what the user typed and compare it to the static comments.  If they differ, something has been updated
            if (Request.Form[txtComments.UniqueID] != null)
            {
                txtComments.Text = Request.Form[txtComments.UniqueID];
                if (txtComments.Text != mfbii.Comment)
                    UpdateComments();
            }
        }

        lnkAnnotate.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:EditComment('{0}', '{1}');", pnlStatic.ClientID, pnlEdit.ClientID);

        if (mfbii != null)
            divActions.Visible = lnkAnnotate.Visible || lnkDelete.Visible || lnkMakeDefault.Visible || lnkZoom.Visible;
    }

    /// <summary>
    /// Updates the comments on the file to match the text box.
    /// </summary>
    protected void UpdateComments()
    {
        mfbii.UpdateAnnotation(HttpUtility.HtmlDecode(txtComments.Text));

        lblComments.Text = mfbii.Comment.EscapeHTML();
    }

    protected void btnUpdateComments_Click(object sender, EventArgs e)
    {
        // No need to call Update Comments; this will happen automatically for any box where a difference is found between 
        // the edit box and the existing label.
        // So this function really just serves to cause a post-back event.
    }

    #region Events
    protected void lnkMakeDefault_Click(object sender, ImageClickEventArgs e)
    {
        if (mfbii != null && ImageMadeDefault != null)
            ImageMadeDefault(this, new MFBImageInfoEvent(mfbii));
    }

    protected void DeleteImage(object sender, EventArgs e)
    {
        if (mfbii != null)
        {
            // Notify of the deletion before actually deleting it...
            if (ImageDeleted != null)
                ImageDeleted(sender, new MFBImageInfoEvent(mfbii));
            // ...then delete it
            mfbii.DeleteImage();
        }
    }
    #endregion
}
