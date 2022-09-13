using MyFlightbook.Geography;
using MyFlightbook.Payments;
using OAuthAuthorizationServer.Services;
using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public partial class UploadPicture : UploadImagePage
    {
        protected override MFBImageInfo UploadForUser(string szUser, HttpPostedFile pf, string szComment)
        {
            int idFlight = Convert.ToInt32(Request.Form["idFlight"], CultureInfo.InvariantCulture);
            if (idFlight <= 0)
                throw new MyFlightbookException(Resources.WebService.errInvalidFlight);

            LogbookEntry le = new LogbookEntry
            {
                FlightID = idFlight
            };
            if (!le.FLoadFromDB(le.FlightID, szUser, LogbookEntryCore.LoadTelemetryOption.None))
                throw new MyFlightbookException(Resources.WebService.errFlightDoesntExist);
            if (le.User != szUser)
                throw new MyFlightbookException(Resources.WebService.errFlightNotYours);

            // Check if authorized for videos
            if (MFBImageInfo.ImageTypeFromFile(pf) == MFBImageInfoBase.ImageFileType.S3VideoMP4 && !EarnedGratuity.UserQualifies(szUser, Gratuity.GratuityTypes.Videos))
                throw new MyFlightbookException(Branding.ReBrand(Resources.LocalizedText.errNotAuthorizedVideos));

            LatLong ll = null;
            string szLat = Request.Form["txtLat"];
            string szLon = Request.Form["txtLon"];
            if (!String.IsNullOrEmpty(szLat) && !String.IsNullOrEmpty(szLon))
                ll = LatLong.TryParse(szLat, szLon, CultureInfo.InvariantCulture);

            return new MFBImageInfo(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture), pf, szComment, ll);
        }
    }
}