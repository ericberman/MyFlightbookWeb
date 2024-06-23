using MyFlightbook.Instruction;
using MyFlightbook.Web.Sharing;
using System;
using System.Web.Security;

/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class FlightControllerBase : AdminControllerBase
    {
        #region Check if you are authorized for a given operation
        /// <summary>
        /// Determines if the viewing user has access to view flight data for the specified target user.
        /// If unauthorized for any reason, an exception is thrown
        /// 4 allowed conditions:
        ///  a) You have a valid sharekey
        ///  b) It's your flight
        ///  c) you're an admin AND "a=1" is in the request
        ///  d) You're an instructor for the student AND you can view the logbook.
        /// </summary>
        /// <param name="targetUser">User whose data is being accessed</param>
        /// <param name="viewingUser">Viewing user (should be User.identity.name, but null is a shortcut for that)</param>
        /// <param name="sk">An optional sharekey that may provide access</param>
        /// <returns>The student profile, if appropriate, or null.  Null doesn't mean unauthorized, it just means it's not a student relationship!</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected InstructorStudent CheckCanViewFlights(string targetUser, string viewingUser, ShareKey sk = null)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));

            // three allowed conditions:
            // a) Viewing user (Authenticated or not!) has a valid sharekey for the user and can view THAT USER's flights.  This is the only unauthenticated access allowed
            if ((sk?.CanViewFlights ?? false) && (sk?.Username ?? string.Empty).CompareOrdinal(targetUser) == 0)
                return null;

            if (User.Identity.IsAuthenticated)
            {
                // Viewing user should ALWAYS be the authenticated user; null just means "use the logged user
                viewingUser = viewingUser ?? User.Identity.Name;

                if (viewingUser.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("Supplied viewing user is different from the authenticated user - that should never happen!");

                // a) Authenticated, viewing user is target user
                if (targetUser.CompareOrdinal(viewingUser) == 0)
                    return null;

                // b) Admin acting as such
                Profile pfviewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pfviewer.CanSupport && util.GetIntParam(Request, "a", 0) == 1)
                    return null;

                // c) Authenticated, viewing user is an instructor of target user and user has given permission to view logbook
                InstructorStudent csm = CFIStudentMap.GetInstructorStudent(new CFIStudentMap(viewingUser).Students, targetUser);
                if (csm?.CanViewLogbook ?? false)
                    return csm;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);
        }

        /// <summary>
        /// Determine if the current user can SAVE a flight for the specified target user.
        /// 3 allowed conditions:
        ///  a) It's your flight
        ///  b) you're an admin AND "a=1" is in the request
        ///  c) You are an instructor with "CanAddLogbook" privileges
        ///  Note: does NOT check if instructor is UPDATING a flight, simply for writing access.
        /// </summary>
        /// <returns>The student profile, if appropriate, or null.  Null doesn't mean unauthorized, it just means it's not a student relationship!</returns>
        /// <param name="targetUser">Name of the target use</param>
        /// <exception cref="UnauthorizedAccessException"></exception>

        protected InstructorStudent CheckCanSaveFlight(string targetUser)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));

            // Admin can save flights IF "a=1" is in the request
            if (User.Identity.IsAuthenticated)
            {
                if (targetUser.CompareOrdinal(User.Identity.Name) == 0)
                    return null; // all good!

                Profile pfviewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pfviewer.CanSupport && util.GetIntParam(Request, "a", 0) == 1)
                    return null;

                InstructorStudent instructorStudent = CheckCanViewFlights(targetUser, User.Identity.Name);
                if (instructorStudent?.CanAddLogbook ?? false)
                    return instructorStudent;
            }

            // Otherwise, we're unauthenticated
            throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToSaveToLogbook);
        }

        protected static void ValidateUser(string user, string pass, out string fixedUser)
        {
            if (String.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);

            if (user.Contains("@"))
                user = Membership.GetUserNameByEmail(user);

            if (UserEntity.ValidateUser(user, pass).Length == 0)
                throw new UnauthorizedAccessException(Resources.LogbookEntry.errNotAuthorizedToViewLogbook);

            fixedUser = user;
        }
        #endregion
    }
}