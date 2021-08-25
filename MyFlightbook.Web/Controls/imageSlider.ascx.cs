using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2017-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public partial class imageSlider : UserControl
    {
        #region properties
        protected string SliderClientID
        {
            get { return ClientID + "bxslider"; }
        }

        // Gets/sets the images to display.
        public IEnumerable<MFBImageInfo> Images
        {
            get { return (IEnumerable<MFBImageInfo>)rptImages.DataSource; }
            set
            {
                rptImages.DataSource = value;
                rptImages.DataBind();
                pnlSlider.Visible = value != null && value.Any();
            }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.Header.Controls.Add(new LiteralControl("<link rel=\"stylesheet\" type=\"text/css\" href=\"https://cdn.jsdelivr.net/bxslider/4.2.12/jquery.bxslider.css\")"));
        }
    }
}