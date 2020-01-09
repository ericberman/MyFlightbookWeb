using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Text;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFileUpload : System.Web.UI.UserControl
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

    /// <summary>
    /// The "add another image" link control.
    /// </summary>
    public HyperLink AddAnotherLink
    {
        get {return lnkAddAnother;}
    }

    /// <summary>
    /// Is the AddAnother link visible?
    /// </summary>
    public bool AddAnotherVisible
    {
        get { return pnlAddAnother.Visible; }
        set { pnlAddAnother.Visible = value; }
    }

    /// <summary>
    /// CSS-based display property
    /// </summary>
    public string Display
    {
        get { return Panel1.Style["display"]; }
        set { Panel1.Style["display"] = value; }
    }

    /// <summary>
    /// The clientID for the panel (to show/hide this)
    /// </summary>
    public string DisplayID
    {
        get { return Panel1.ClientID; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack)
        {
        }
    }
}
