using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Static class to encapsulate authorization to modify images.
    /// Want to keep MFBImageInfo (relatively) clean w.r.t. MyFlightbook classes; this encapsulates the semantic
    /// knowledge needed for services 
    /// </summary>
    public static class ImageAuthorization
    {
        public enum ImageAction { View, Delete, Annotate }

        /// <summary>
        /// Determines if the specified user is authorized to modify/delete an image
        /// </summary>
        /// <param name="mfbii">The image</param>
        /// <param name="szuser">The user</param>
        /// <param name="requestedAction">The action that is requested.  We restrict deletion of aircraft images but anybody can update annotation</param>
        /// <param name="fDeleteConfirmed">True if the deletion has already been confirmed by the user and thus we can delete even if non-admin and multiple people using the image</param>
        /// <exception cref="UnauthorizedAccessException">Throws UnauthorizedAccessException if user isn't authorized </exception>
        /// <exception cref="ArgumentNullException"></exception>"
        public static void ValidateAuth(MFBImageInfo mfbii, string szuser, ImageAction requestedAction, bool fDeleteConfirmed = false)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));

            if (String.IsNullOrEmpty(szuser))
                throw new UnauthorizedAccessException();

            switch (mfbii.Class)
            {
                case MFBImageInfoBase.ImageClass.Aircraft:
                    if (requestedAction == ImageAction.View)    // all aircraft images are public
                        return;

                    // Check that the user actually has this aircraft in their account
                    UserAircraft ua = new UserAircraft(szuser);
                    Aircraft ac = new Aircraft(Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture));
                    if (!ua.CheckAircraftForUser(ac))
                        throw new UnauthorizedAccessException();

                    // Further restrict deletion of images if (a) aircraft is shared, or (b) is anonymous.  If it's just you, you're fine.
                    // BUT if the user has already confirmed deletion (i.e., from the website, which enforces deletion), then allow it.
                    if (requestedAction == ImageAction.Delete && !fDeleteConfirmed)
                    {
                        if (ac.IsAnonymous || new AircraftStats(szuser, ac.AircraftID).Users > 1)
                            throw new InvalidOperationException(Resources.Aircraft.errDontDeleteImageAnonymous);
                    }
                    break;
                case MFBImageInfoBase.ImageClass.BasicMed:
                    {
                        // Basicmed are inherently private.
                        if (int.TryParse(mfbii.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idBME))
                        {
                            List<MyFlightbook.BasicmedTools.BasicMedEvent> lst = new List<BasicmedTools.BasicMedEvent>(BasicmedTools.BasicMedEvent.EventsForUser(szuser));
                            if (!lst.Exists(bme => bme.ID == idBME))
                                throw new UnauthorizedAccessException();
                        }
                    }
                    break;
                case MFBImageInfoBase.ImageClass.Endorsement:
                case MFBImageInfoBase.ImageClass.OfflineEndorsement:
                    if (requestedAction == ImageAction.View && mfbii.Class == MFBImageInfoBase.ImageClass.Endorsement)  // Endorsements may be viewable by an instructor, but offline endorsements are not
                        return;
                    if (szuser.CompareCurrentCultureIgnoreCase(mfbii.Key) != 0)
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfoBase.ImageClass.Flight:
                    // flight *images* are inherently public due to shared flights or share flights URL, where the user could be entirely unauthenticated.
                    if (requestedAction == ImageAction.View)    // all aircraft images are public
                        return;
                    if (!new LogbookEntry().FLoadFromDB(Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture), szuser))
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfoBase.ImageClass.Unknown:
                default:
                    throw new UnauthorizedAccessException();
            }
        }
    }
}