using MyFlightbook;
using MyFlightbook.Image;
using System;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_PendingImg : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Clear();
        string szKey = util.GetStringParam(Request, "i");
        MFBPendingImage mfbpb;
        if (String.IsNullOrEmpty(szKey) || ((mfbpb = (MFBPendingImage)Session[szKey]) == null))
        {
            Response.Redirect("~/Images/x.gif", true);
            return;
        }

        bool fShowFull = util.GetIntParam(Request, "full", 0) != 0;

        Response.ContentType = "image/jpeg";
        Response.BinaryWrite(fShowFull ? mfbpb.PostedFile.CompatibleContentData() : mfbpb.PostedFile.ThumbnailBytes());
        Response.End();
    }
}