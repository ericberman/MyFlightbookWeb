﻿using MyFlightbook.Payments;
using OAuthAuthorizationServer.Services;
using System;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public partial class UploadAirplanePicture : UploadImagePage
    {
        protected override MFBImageInfo UploadForUser(string szUser, HttpPostedFile pf, string szComment)
        {
            string szTail = Request.Form["txtAircraft"];
            int idAircraft = Aircraft.idAircraftUnknown;
            bool fUseID = util.GetIntParam(Request, "id", 0) != 0;
            if (String.IsNullOrEmpty(szTail))
                throw new MyFlightbookException(Resources.WebService.errBadTailNumber);
            if (fUseID)
            {
                if (!int.TryParse(szTail, out idAircraft) || idAircraft == Aircraft.idAircraftUnknown)
                    throw new MyFlightbookException(Resources.WebService.errBadTailNumber);
            }
            else if (szTail.Length > Aircraft.maxTailLength || szTail.Length < 3)
                throw new MyFlightbookException(Resources.WebService.errBadTailNumber);

            // Check if authorized for videos
            if (MFBImageInfo.ImageTypeFromFile(pf) == MFBImageInfoBase.ImageFileType.S3VideoMP4 && !EarnedGratuity.UserQualifies(szUser, Gratuity.GratuityTypes.Videos))
                throw new MyFlightbookException(Branding.ReBrand(Resources.LocalizedText.errNotAuthorizedVideos));

            UserAircraft ua = new UserAircraft(szUser);
            ua.InvalidateCache();   // in case the aircraft was added but cache is not refreshed.

            Aircraft ac = null;
            if (fUseID)
            {
                ac = new Aircraft(idAircraft);
            }
            else
            {
                string szTailNormal = Aircraft.NormalizeTail(szTail);

                // Look for the aircraft in the list of the user's aircraft (that way you get the right version if it's a multi-version aircraft and no ID was specified
                // Hack for backwards compatibility with mobile apps and anonymous aircraft
                // Look to see if the tailnumber matches the anonymous tail 
                ac = ua.Find(uac =>
                    (String.Compare(Aircraft.NormalizeTail(szTailNormal), Aircraft.NormalizeTail(uac.TailNumber), StringComparison.CurrentCultureIgnoreCase) == 0 ||
                     String.Compare(szTail, uac.HackDisplayTailnumber, StringComparison.CurrentCultureIgnoreCase) == 0));
            }

            if (ac == null || !ua.CheckAircraftForUser(ac))
                throw new MyFlightbookException(Resources.WebService.errNotYourAirplane);

            return new MFBImageInfo(MFBImageInfoBase.ImageClass.Aircraft, ac.AircraftID.ToString(System.Globalization.CultureInfo.InvariantCulture), pf, szComment, null);
        }
    }
}