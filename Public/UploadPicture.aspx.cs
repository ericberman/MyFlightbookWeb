using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Globalization;
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
using MyFlightbook.Payments;
using MyFlightbook.Image;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_UploadPicture : System.Web.UI.Page
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
                int idFlight = Convert.ToInt32(Request.Form["idFlight"], CultureInfo.InvariantCulture);
                HttpPostedFile pf = imgPicture.PostedFile;
                string szComment = Request.Form["txtComment"];

                if (szComment == null)
                    szComment = "";

                string szUser = ws.GetEncryptedUser(szAuth);
                if (szUser.Length == 0)
                    throw new MyFlightbookException(Resources.WebService.errBadAuth);
                if (idFlight <= 0)
                    throw new MyFlightbookException(Resources.WebService.errInvalidFlight);

                LogbookEntry le = new LogbookEntry();
                le.FlightID = idFlight;
                if (!le.FLoadFromDB(le.FlightID, szUser, LogbookEntry.LoadTelemetryOption.None))
                    throw new MyFlightbookException(Resources.WebService.errFlightDoesntExist);
                if (le.User != szUser)
                    throw new MyFlightbookException(Resources.WebService.errFlightNotYours);

                if (pf == null || pf.ContentLength == 0)
                    throw new MyFlightbookException(Resources.WebService.errNoImageProvided);

                // Check if authorized for videos
                if (MFBImageInfo.ImageTypeFromFile(pf) == MFBImageInfo.ImageFileType.S3VideoMP4 && !EarnedGrauity.UserQualifies(szUser, Gratuity.GratuityTypes.Videos))
                    throw new MyFlightbookException(Branding.ReBrand(Resources.LocalizedText.errNotAuthorizedVideos));

                LatLong ll = null;
                string szLat = Request.Form["txtLat"];
                string szLon = Request.Form["txtLon"];
                if (!String.IsNullOrEmpty(szLat) && !String.IsNullOrEmpty(szLon))
                {
                    ll = LatLong.TryParse(szLat, szLon, CultureInfo.InvariantCulture);
                }

                mfbImageFlight.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
                MFBImageInfo mfbii = new MFBImageInfo(MFBImageInfo.ImageClass.Flight, mfbImageFlight.Key, pf, szComment, ll);

                // Pseudo-idempotency check: see if the just-added image is a dupe, delete it if so.
                mfbii.IdempotencyCheck();
            }
            catch (MyFlightbookException ex)
            {
                szErr = ex.Message;
            }

            Response.Clear();
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Write(szErr);
        }
    }
}
