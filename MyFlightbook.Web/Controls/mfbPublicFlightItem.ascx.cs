using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbPublicFlightItem : System.Web.UI.UserControl
{
    private LogbookEntry m_le;

    public LogbookEntry Entry
    {
        get { return m_le; }
        set
        {
            m_le = value ?? throw new ArgumentNullException(nameof(value));
            lnkFlight.NavigateUrl = VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/public/ViewPublicFlight.aspx/{0}", value.FlightID));
            lblDate.Text = value.Date.ToShortDateString();
            lblDetails.Text = (value.TotalFlightTime > 0) ? String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PublicFlightDuration, value.TotalFlightTime) : string.Empty;

            lblroute.Text = HttpUtility.HtmlEncode(value.Route);
            lblComments.Text = HttpUtility.HtmlEncode(value.Comment);
            lblTail.Text = value.TailNumDisplay;

            // We don't use an mfbImageList control because that requires a scriptmanager.
            ImageList il = new ImageList(MFBImageInfo.ImageClass.Aircraft, value.AircraftID.ToString(CultureInfo.InvariantCulture));
            il.Refresh();
            if (il.ImageArray.Count == 0)
                lnkFullAc.Visible = false;
            else
            {
                lnkFullAc.Visible = true;
                imgAc.ImageUrl = il.ImageArray[0].URLThumbnail;
                lnkFullAc.NavigateUrl = il.ImageArray[0].URLFullImage;
            }

            lblModel.Text = value.ModelDisplay;
            lblCatClass.Text = value.CatClassDisplay;

            il = new ImageList(MFBImageInfo.ImageClass.Flight, value.FlightID.ToString(CultureInfo.InvariantCulture));
            il.Refresh();
            rptFlightImages.DataSource = il.ImageArray;
            rptFlightImages.DataBind();
        }
    }

    protected void Page_Load(object sender, EventArgs e) { }

    protected void rptFlightImages_ItemDataBound(object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        MFBImageInfo mfbii = e.Item.DataItem as MFBImageInfo;
        Panel p = (Panel) e.Item.FindControl("pnlStatic");
        if (mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4 && !(mfbii is MFBPendingImage))
        {
            ((Literal) e.Item.FindControl("litVideoOpenTag")).Text = String.Format(CultureInfo.InvariantCulture, "<video width=\"320\" height=\"240\" controls><source src=\"{0}\"  type=\"video/mp4\">", mfbii.ResolveFullImage());
            ((Literal) e.Item.FindControl("litVideoCloseTag")).Text = "</video>";
            p.Style["max-width"] = "320px";
        }
        else if (mfbii.WidthThumbnail > 0)
        {
            p.Style["max-width"] = mfbii.WidthThumbnail.ToString(CultureInfo.InvariantCulture);
        }
    }
}