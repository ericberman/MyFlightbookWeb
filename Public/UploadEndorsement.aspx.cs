using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.Payments;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_UploadEndorsement : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (String.Compare(Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) == 0)
            return;

        String szErr = "OK";

        using (MFBWebService ws = new MFBWebService())
        {
            try
            {
                if (ShuntState.IsShunted)
                    throw new MyFlightbookException(ShuntState.ShuntMessage);

                string szAuth = Request.Form["txtAuthToken"];
                HttpPostedFile pf = imgPicture.PostedFile;
                string szComment = Request.Form["txtComment"];

                if (szComment == null)
                    szComment = string.Empty;

                string szUser = ws.GetEncryptedUser(szAuth);
                if (szUser.Length == 0)
                    throw new MyFlightbookException(Resources.WebService.errBadAuth);

                if (pf == null || pf.ContentLength == 0)
                    throw new MyFlightbookException(Resources.WebService.errNoImageProvided);

                mfbImageEndorsement.Key = szUser;
                MFBImageInfo mfbii = new MFBImageInfo(MFBImageInfo.ImageClass.Endorsement, mfbImageEndorsement.Key, pf, szComment, null);

                // Pseudo-idempotency check: see if the just-added image is a dupe, delete it if so.
                mfbii.IdempotencyCheck();
            }
            catch (MyFlightbookException ex)
            {
                szErr = ex.Message;
            }
        }

        Response.Clear();
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Write(szErr);
    }
}
