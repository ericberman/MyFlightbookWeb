using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbImageList : System.Web.UI.UserControl
{
    private ImageList m_imgList;
    private Controls_mfbEditableImage.GeoLinkType m_LinkType = Controls_mfbEditableImage.GeoLinkType.None;

    public event EventHandler<MFBImageInfoEvent> MakeDefault = null;

    #region properties
    /// <summary>
    /// Should we include docs such as PDFs in addition to images?
    /// </summary>
    public bool IncludeDocs { get; set; }

    public ImageList Images
    {
        get
        {
            if (m_imgList == null)
                m_imgList = new ImageList(ImageClass, Key);
            return m_imgList;
        }
        set { m_imgList = value; }
    }

    public int Columns
    {
        get { return Convert.ToInt32(hdnColumns.Value, CultureInfo.InvariantCulture); }
        set { hdnColumns.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    public Boolean CanEdit
    {
        get { return Convert.ToBoolean(hdnCanEdit.Value, CultureInfo.InvariantCulture); }
        set { hdnCanEdit.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    public bool CanMakeDefault
    {
        get { return Convert.ToBoolean(hdnMakeDefault.Value, CultureInfo.InvariantCulture); }
        set { hdnMakeDefault.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    public int MaxImage
    {
        get { return Convert.ToInt32(hdnMaxImage.Value, CultureInfo.InvariantCulture); }
        set { hdnMaxImage.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    public string AltText
    {
        get { return hdnAltText.Value; }
        set { hdnAltText.Value = value; }
    }

    public string DefaultImage
    {
        get { return hdnDefaultImage.Value; }
        set { hdnDefaultImage.Value = value; }
    }

    /// <summary>
    /// If true, then on postback this will not try to re-initialize from the file.
    /// </summary>
    public bool NoRequery { get; set; }

    public MFBImageInfo.ImageClass ImageClass
    {
        get 
        {
            if (String.IsNullOrEmpty(hdnBasePath.Value))
                return MFBImageInfo.ImageClass.Unknown;
            return (MFBImageInfo.ImageClass) Enum.Parse(typeof(MFBImageInfo.ImageClass), hdnBasePath.Value); 
        }
        set 
        {
            hdnBasePath.Value = value.ToString();
            Images.Class = value;
        }
    }

    public string Key
    {
        get { return hdnKey.Value; }
        set { Images.Key = hdnKey.Value = value; }
    }


    public Controls_mfbEditableImage.GeoLinkType MapLinkType
    {
        get { return m_LinkType; }
        set { m_LinkType = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        Images.Key = Key;  // need to ensure the image list key is set; basepath is likely already set through the property.

        if (IsPostBack)
            Refresh();
    }

    protected bool CanEditImage(MFBImageInfo mfbii)
    {
        return mfbii != null && CanEdit && (mfbii.ImageType == MFBImageInfo.ImageFileType.JPEG || mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4);
    }

    /// <summary>
    /// If you'd like, you can call this from your delete handler to actually delete the specified file.
    /// It assumes that the clicked object was a LinkButton.
    /// </summary>
    /// <param name="sender">The object that was clicked (a LinkButton)</param>
    /// <param name="e">Standard event args for the click</param>
    public void HandleDeleteClick(object sender, MFBImageInfoEvent e)
    {
        // Remove the image from the list; faster than refreshing and works for pending images too.
        if (e == null)
            throw new ArgumentNullException("e");
        Images.RemoveImage(e.Image);
        Refresh(false);
    }

    private List<MFBImageInfo> Trim(List<MFBImageInfo> lst)
    {
        if (MaxImage > 0 && lst.Count > MaxImage)
            lst.RemoveRange(MaxImage, lst.Count - MaxImage);

        return lst;
    }

    /// <summary>
    /// Get a simple HTML table with the images.  NOT JAVASCRIPT SAFE but is HTML encoded.
    /// MUST refresh before calling this
    /// </summary>
    /// <returns>HTML table representation of Images</returns>
    public string AsHTMLTable()
    {
        if (Images == null)
            return string.Empty;

        List<MFBImageInfo> coll = Trim(new List<MFBImageInfo>(Images.ImageArray));
        if (coll == null || coll.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder("<table><tr style=\"vertical-align: top\">");

        int iImage = 0, iColumn = 0, iRow = 0;
        while (iImage < coll.Count)
        {
            if (!String.IsNullOrEmpty(coll[iImage].URLThumbnail))
            {
                sb.Append(String.Format(CultureInfo.InvariantCulture, "<td style=\"text-align:center;\"><img src=\"" + coll[iImage].URLThumbnail + "\" />{0}</td>", coll[iImage].Comment.Length > 0 ? "<br />" + HttpUtility.HtmlEncode(coll[iImage].Comment) : string.Empty));

                // Start a new row, but only if there are more images.
                if (++iColumn == Columns && iImage < coll.Count - 1)
                {
                    iRow++;
                    iColumn = 0;
                    sb.Append("</tr><tr style=\"vertical-align: top\">");
                }
            }
            iImage++;
        }

        sb.Append("</tr></table>");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a table containing thumbnails for each of the pictures found in the path + key used when the imagelist was created
    /// </summary>
    /// <returns># of images rendered</returns>
    public int Refresh(bool fReQuery, bool fIncludeVids = true)
    {
        if (fReQuery)
            Images.Refresh(IncludeDocs, DefaultImage, fIncludeVids);

        List<MFBImageInfo> lst = Trim(new List<MFBImageInfo>(Images.ImageArray));

        // Override make default if no images or only one image, since obviously there's no ambiguity about which should be default in that case.
        if (lst.Count <= 1)
            CanMakeDefault = false;

        rptImg.DataSource = lst;
        rptImg.DataBind();
        pnlImgs.Visible = lst.Count > 0;
        return lst.Count();
    }

    public int Refresh()
    {
        return Refresh(!NoRequery);
    }

    protected void rptImg_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        Controls_mfbEditableImage c = (Controls_mfbEditableImage)e.Item.FindControl("mfbEI");
        MFBImageInfo mfbii = (MFBImageInfo)e.Item.DataItem;
        if (String.IsNullOrEmpty(mfbii.ThumbnailFile))
            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Invalid Image - empty thumbnail file: {0}", mfbii.ToString()));
        c.ID = "img" + e.Item.ItemIndex.ToString(CultureInfo.InvariantCulture);
    }

    protected void mfbEI_ImageMadeDefault(object sender, MFBImageInfoEvent e)
    {
        if (e != null && MakeDefault != null)
            MakeDefault(this, e);
    }
}
