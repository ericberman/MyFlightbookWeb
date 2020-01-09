using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbVideoEntry : UserControl
{
    private const string szKeyVideo = "vsKeyVideoToEmbed";

    #region properties
    /// <summary>
    /// The videos to display
    /// </summary>
    public Collection<VideoRef> Videos 
    {
        get
        {
            Collection<VideoRef> l = (Collection<VideoRef>)ViewState[szKeyVideo];
            if (l == null)
                ViewState[szKeyVideo] = l = new Collection<VideoRef>();
            return l;
        }
    }

    /// <summary>
    /// Only some people are allowed to add new videos
    /// </summary>
    public bool CanAddVideos
    {
        get { return String.IsNullOrEmpty(hdnCanEdit.Value) ? false : Convert.ToBoolean(hdnCanEdit.Value, CultureInfo.InvariantCulture); }
        set 
        { 
            hdnCanEdit.Value = value.ToString(CultureInfo.InvariantCulture);
            pnlNewVideo.Visible = value;
            pnlVideoUpload.Visible = value;
        }
    }

    /// <summary>
    /// Typically the owner of the video can delete
    /// </summary>
    public bool CanDelete
    {
        get { return String.IsNullOrEmpty(hdnCanDelete.Value) ? false : Convert.ToBoolean(hdnCanDelete.Value, CultureInfo.InvariantCulture); }
        set { hdnCanDelete.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// The id for the videos
    /// </summary>
    public int FlightID
    {
        get { return String.IsNullOrEmpty(hdnDefaultFlightID.Value) ? LogbookEntry.idFlightNew : Convert.ToInt32(hdnDefaultFlightID.Value, CultureInfo.InvariantCulture); }
        set { hdnDefaultFlightID.Value = value.ToString(CultureInfo.InvariantCulture); }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void Refresh()
    {
        gvVideos.DataSource = Videos;
        gvVideos.DataBind();
    }

    protected void btnAdd_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtVideoToEmbed.Text))
        {
            VideoRef v = new VideoRef(FlightID, txtVideoToEmbed.Text, txtComment.Text);

            if (!v.IsValid)
            {
                pnlError.Visible = true;
                lblErr.Text = v.ErrorString;
            }
            else
            {
                Videos.Add(v);
                txtVideoToEmbed.Text = string.Empty;
                Refresh();
            }
        }
    }

    protected void gvVideos_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            GridViewRow grow = (GridViewRow)((LinkButton) e.CommandSource).NamingContainer;
            int iVideo = grow.RowIndex;
            VideoRef v = Videos[iVideo];
            Videos.RemoveAt(iVideo);
            v.Delete();
            Refresh();
        }
    }
    protected void gvVideos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            LinkButton l = (LinkButton)e.Row.FindControl("lnkDelete");
            l.Visible = CanDelete;
            Literal lit = (Literal)e.Row.FindControl("litVideo");
            lit.Text = Videos[e.Row.RowIndex].EmbedHTML();
        }
    }
}