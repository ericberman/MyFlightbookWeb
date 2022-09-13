using OAuthAuthorizationServer.Services;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public partial class UploadEndorsement : UploadImagePage
    {
        protected override MFBImageInfo UploadForUser(string szUser, HttpPostedFile pf, string szComment)
        {
            return new MFBImageInfo(MFBImageInfoBase.ImageClass.Endorsement, szUser, pf, szComment, null);
        }
    }
}