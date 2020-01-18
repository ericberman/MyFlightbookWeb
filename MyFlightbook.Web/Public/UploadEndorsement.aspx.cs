using MyFlightbook.Image;
using OAuthAuthorizationServer.Services;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_UploadEndorsement : UploadImagePage
{
    protected override MFBImageInfo UploadForUser(string szUser, HttpPostedFile pf, string szComment)
    {
        mfbImageEndorsement.Key = szUser;
        return new MFBImageInfo(MFBImageInfo.ImageClass.Endorsement, mfbImageEndorsement.Key, pf, szComment, null);
    }
}
