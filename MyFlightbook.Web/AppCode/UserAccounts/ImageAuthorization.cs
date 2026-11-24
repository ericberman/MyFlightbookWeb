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

        /*
         * Class	        View	                            Annotate	        Delete
        Aircraft	        anyone                              has aircraft	    has aircraft, and (sole user and not anonymous, or confirmed)
        Flight	            owner, public, instructor, support	owner	            owner
        Endorsement	        owner, instructor	                owner	            owner
        OfflineEndorsement	owner (instructor)	                owner	            owner
        BasicMed	        owner	                            owner	            owner
         */
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

            // Viewing is a function of class+key, not of the specific image
            if (requestedAction == ImageAction.View)
            {
                ValidateViewAuth(mfbii.Class, mfbii.Key, szuser);
                return;
            }

            switch (mfbii.Class)
            {
                case MFBImageInfoBase.ImageClass.Aircraft:
                    {
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
                    }
                    break;
                case MFBImageInfoBase.ImageClass.BasicMed:
                    // Basicmed are inherently private.
                    if (!IsUsersBasicMedEvent(mfbii.Key, szuser))
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfoBase.ImageClass.Endorsement:
                case MFBImageInfoBase.ImageClass.OfflineEndorsement:
                    // Only the owner (the key) may modify endorsement images
                    if (szuser.CompareCurrentCultureIgnoreCase(mfbii.Key) != 0)
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfoBase.ImageClass.Flight:
                    if (!new LogbookEntry().FLoadFromDB(Convert.ToInt32(mfbii.Key, CultureInfo.InvariantCulture), szuser))
                        throw new UnauthorizedAccessException();
                    break;
                case MFBImageInfoBase.ImageClass.Unknown:
                default:
                    throw new UnauthorizedAccessException();
            }
        }

        /// <summary>
        /// Determines if the specified (authenticated) user may view the images for the given class and key - e.g., all of the images for a flight.
        /// View authorization depends only on the class and key, not on the individual image, so a caller displaying the images
        /// for a single class/key needs to call this only once.
        /// NOTE: this governs what we will put on a page; it does NOT gate the serving of the underlying files.
        /// Public flights and share links are authorized by the pages that display them (PubController.ViewFlight, CheckCanViewFlights with a sharekey),
        /// since those viewers may be unauthenticated.
        /// </summary>
        /// <param name="ic">The class of image</param>
        /// <param name="key">The key for the image (flight ID, aircraft ID, username, or basicmed event ID, depending on class)</param>
        /// <param name="szuser">The authenticated viewing user</param>
        /// <exception cref="UnauthorizedAccessException">Throws UnauthorizedAccessException if user isn't authorized </exception>
        public static void ValidateViewAuth(MFBImageInfoBase.ImageClass ic, string key, string szuser)
        {
            if (String.IsNullOrEmpty(szuser) || String.IsNullOrEmpty(key))
                throw new UnauthorizedAccessException();

            switch (ic)
            {
                case MFBImageInfoBase.ImageClass.Aircraft:
                    // all aircraft images are public
                    return;
                case MFBImageInfoBase.ImageClass.BasicMed:
                    // Visible only to the owner.  The key is the ID of the basicmed event, not the user, so verify that the event belongs to the user.
                    if (IsUsersBasicMedEvent(key, szuser))
                        return;
                    break;
                case MFBImageInfoBase.ImageClass.Endorsement:
                    // Visible to the owner (the key) or to any instructor of the owner
                    if (szuser.CompareCurrentCultureIgnoreCase(key) == 0 || new Instruction.CFIStudentMap() { User = szuser }.IsInstructorOf(key))
                        return;
                    break;
                case MFBImageInfoBase.ImageClass.OfflineEndorsement:
                    // Records kept by an instructor (the key) of endorsements given outside of the system; visible only to that instructor
                    if (szuser.CompareCurrentCultureIgnoreCase(key) == 0)
                        return;
                    break;
                case MFBImageInfoBase.ImageClass.Flight:
                    {
                        if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idFlight) || idFlight <= 0)
                            break;

                        // Force the load so that we can see who owns the flight
                        LogbookEntry le = new LogbookEntry();
                        if (!le.FLoadFromDB(idFlight, szuser, LogbookEntryCore.LoadTelemetryOption.None, true) || String.IsNullOrEmpty(le.User))
                            break;

                        // a) It's your flight, or b) it's a public flight
                        if (le.User.CompareCurrentCultureIgnoreCase(szuser) == 0 || le.fIsPublic)
                            return;

                        // c) You're an instructor of the owner and can view (or add to) their logbook
                        Instruction.CFIStudentMap sm = new Instruction.CFIStudentMap() { User = szuser };
                        if (sm.IsInstructorOf(le.User) && (Instruction.CFIStudentMap.GetInstructorStudent(sm.Students, le.User)?.CanViewLogbook ?? false))
                            return;

                        // d) You're support
                        if (MyFlightbook.Profile.GetUser(szuser).CanSupport)
                            return;
                    }
                    break;
                case MFBImageInfoBase.ImageClass.Unknown:
                default:
                    break;
            }

            throw new UnauthorizedAccessException();
        }

        private static bool IsUsersBasicMedEvent(string key, string szuser)
        {
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idBME))
                return false;
            List<BasicmedTools.BasicMedEvent> lst = new List<BasicmedTools.BasicMedEvent>(BasicmedTools.BasicMedEvent.EventsForUser(szuser));
            return lst.Exists(bme => bme.ID == idBME);
        }
    }
}