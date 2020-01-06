using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_imageSlider : System.Web.UI.UserControl
{
    #region properties
    protected string SliderClientID
    {
        get { return ClientID + "bxslider"; }
    }

    // Gets/sets the images to display.
    public IEnumerable<MFBImageInfo> Images
    {
        get { return (IEnumerable<MFBImageInfo>) rptImages.DataSource; }
        set
        {
            rptImages.DataSource = value;
            rptImages.DataBind();
            pnlSlider.Visible = value != null && value.Count() > 0;
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("GoogleJQ", "https://ajax.googleapis.com/ajax/libs/jquery/1.11.3/jquery.min.js");
        Page.ClientScript.RegisterClientScriptInclude("BXSliderVideo", ResolveClientUrl("~/Public/bxslider/jquery.fitvids.js"));
        Page.ClientScript.RegisterClientScriptInclude("BXSlider", ResolveClientUrl("~/Public/bxslider/jquery.bxslider.min.js"));
        Page.Header.Controls.Add(new LiteralControl("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + ResolveUrl("~/Public/bxslider/jquery.bxslider.min.css") + "\" />"));
    }
}