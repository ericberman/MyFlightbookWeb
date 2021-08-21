using System;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls.ImageControls
{
    public partial class mfbFileUpload : System.Web.UI.UserControl
    {
        /// <summary>
        /// Indicates whether a file is present
        /// </summary>
        public Boolean HasFile
        {
            get { return FileUpload1.HasFile; }
        }

        /// <summary>
        /// The uploaded file
        /// </summary>
        public HttpPostedFile PostedFile
        {
            get { return FileUpload1.PostedFile; }
        }

        /// <summary>
        /// Any comment that the user has typed
        /// </summary>
        public string Comment
        {
            get { return txtComment.Text; }
            set { txtComment.Text = value; }
        }

        public string OnAddAnotherClientClick
        {
            get { return lnkAddAnother.Attributes["onclick"]; }
            set
            {
                lnkAddAnother.Attributes["onclick"] = value;
                pnlAddAnother.Visible = !String.IsNullOrEmpty(value);
            }
        }

        public bool ClientVisible
        {
            get { return Panel1.Style["display"].CompareOrdinal("block") == 0; }
            set { Panel1.Style["display"] = value ? "block" : "none"; }
        }

        /// <summary>
        /// The clientID for the panel (to show/hide this from script)
        /// </summary>
        public string DisplayID
        {
            get { return Panel1.ClientID; }
        }
    }
}