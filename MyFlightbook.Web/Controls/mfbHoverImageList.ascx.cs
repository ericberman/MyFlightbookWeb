﻿using System;
using System.Globalization;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Image;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbHoverImageList : System.Web.UI.UserControl
{
    #region Properties
    /// <summary>
    /// Max width for the image
    /// </summary>
    public Unit MaxWidth
    {
        get { return Unit.Parse(imgThumb.Style["max-width"], CultureInfo.InvariantCulture); }
        set { imgThumb.Style["max-width"] = value.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// Default image when none is specified
    /// </summary>
    public string DefaultImagePath
    {
        get { return imgThumb.ImageUrl; }
        set { imgThumb.ImageUrl = value; }
    }

    /// <summary>
    /// Key for the image list
    /// </summary>
    public string ImageListKey
    {
        get { return mfbil.Key; }
        set { mfbil.Key = value; }
    }

    /// <summary>
    /// Default Image for the image list
    /// </summary>
    public string ImageListDefaultImage
    {
        get { return mfbil.DefaultImage; }
        set { mfbil.DefaultImage = value; }
    }

    /// <summary>
    /// URL to navigate to if the "no image" image is shown.
    /// </summary>
    public string ImageListDefaultLink { get; set; }

    /// <summary>
    /// Alt text for the image list.
    /// </summary>
    public string ImageListAltText
    {
        get { return mfbil.AltText; }
        set { mfbil.AltText = value; }
    }

    /// <summary>
    /// Class for the images (required for refresh
    /// </summary>
    public MFBImageInfo.ImageClass ImageClass
    {
        get { return mfbil.ImageClass; }
        set { mfbil.ImageClass = value; }
    }

    /// <summary>
    /// Opacity for the image
    /// </summary>
    public string CssClass
    {
        get { return imgThumb.CssClass; }
        set { imgThumb.CssClass = value; }
    }

    /// <summary>
    /// For efficiency, specifies whether or not to bind
    /// </summary>
    public bool SuppressRefresh { get; set; }
    #endregion

    public void Refresh(ImageList imgList = null)
    {
        if (!SuppressRefresh)
        {
            if (imgList == null)
                mfbil.Refresh();
            else
            {
                mfbil.Images = imgList;
                mfbil.Refresh(false);
            }

            if (mfbil.Images.ImageArray.Count == 0)
                imgThumb.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:window.location='{0}';", ImageListDefaultLink.ToAbsoluteURL(Request));
            else if (mfbil.Images.ImageArray.Count > 0)
            {
                MFBImageInfo mfbii = mfbil.Images.ImageArray[0];
                imgThumb.ImageUrl = mfbii.URLThumbnail;
                imgThumb.AlternateText = imgThumb.ToolTip = string.Empty;
                imgThumb.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, mfbii.ImageType == MFBImageInfoBase.ImageFileType.PDF || mfbii.ImageType == MFBImageInfoBase.ImageFileType.S3PDF ? "javascript:window.location = '{0}';" : "javascript:viewMFBImg('{0}');", ResolveClientUrl(mfbii.URLFullImage));
            }
            hmImages.Enabled = mfbil.Images.ImageArray.Count > 1;    // popup only if one or more images found
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}