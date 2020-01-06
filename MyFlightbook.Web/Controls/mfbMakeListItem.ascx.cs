using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbMakeListItem : System.Web.UI.UserControl
{
    private MakeModel m_model = null;

    #region Properties
    public ModelQuery.ModelSortMode SortMode { get; set; }

    public bool SuppressImages { get; set; }

    public HyperLink ModelLink
    {
        get
        {
            return (HyperLink)modelView.FindControl("lnkEditMake");
        }
    }

    public MakeModel Model 
    {
        get { return m_model; }

        set
        {
            m_model = value;
            modelView.DataSource = new MakeModel[] {value};
            modelView.DataBind();
            RefreshForm();
        }
    }
    #endregion

    protected void RefreshForm()
    {
        // now add any sample aircraft pictures
        if (SuppressImages)
            return;

        int[] rgAircraft = Model.SampleAircraft();

        Image img = (Image)modelView.FindControl("imgThumbSample");
        img.ImageUrl = ResolveUrl("~/images/noimage.png");  // default value.  Set it here instead of ascx because infinite scroll loses the location context.
        foreach (int acID in rgAircraft)
        {
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Aircraft, acID.ToString(CultureInfo.InvariantCulture));
            il.Refresh();
            if (il.ImageArray.Count > 0)
            {
                MFBImageInfo mfbii = il.ImageArray[0];
                ((HyperLink)modelView.FindControl("lnkImage")).NavigateUrl = mfbii.URLFullImage;
                img.ImageUrl = mfbii.URLThumbnail;
                img.AlternateText = img.ToolTip = string.Empty;
                break;
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}