using MyFlightbook.Instruction;
using MyFlightbook.Web.Sharing;
using System;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI.WebControls;

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

                if (viewingUser.CompareOrdinalIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("Supplied viewing user is different from the authenticated user - that should never happen!");

                // a) Authenticated, viewing user is target user
                if (targetUser.CompareOrdinalIgnoreCase(viewingUser) == 0)
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
        ///  c) You are an instructor with "CanAddLogbook" privileges for a new flight, or with a pending signature request for an existing flight.
        /// </summary>
        /// <returns>The student profile, if appropriate, or null.  Null doesn't mean unauthorized, it just means it's not a student relationship!</returns>
        /// <param name="targetUser">Name of the target use</param>
        /// <param name="le">The logbook entry to edit/save</param>
        /// <exception cref="UnauthorizedAccessException"></exception>

        protected InstructorStudent CheckCanSaveFlight(string targetUser, LogbookEntry le)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new ArgumentNullException(nameof(targetUser));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            // Admin can save flights IF "a=1" is in the request
            if (User.Identity.IsAuthenticated)
            {
                if (targetUser.CompareOrdinalIgnoreCase(User.Identity.Name) == 0)
                    return null; // all good!

                Profile pfviewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pfviewer.CanSupport && util.GetIntParam(Request, "a", 0) == 1)
                    return null;

                // Instructor's can only:
                // a) ADD a NEW flight (pending or regular) IF they have "can add" privileges OR
                // b) EDIT an EXISTING flight (not pending, obviously) that is not currently validly signed AND which is awaiting a signature from THIS instructor
                InstructorStudent instructorStudent = CheckCanViewFlights(targetUser, User.Identity.Name);
                if (le.IsNewFlight && (instructorStudent?.CanAddLogbook ?? false))
                    return instructorStudent;
                if (!le.IsNewFlight && le.CanEditThisFlight(User.Identity.Name, true))
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

        #region Context management
        /// <summary>
        /// Builds the and returns the query string for any status parameters that we may want to preserve,
        /// such as the active query, the active sort, and the page number that we came from
        /// </summary>
        /// <param name="fq"></param>
        /// <param name="fr"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        protected string GetContextParams(FlightQuery fq, FlightResult fr, FlightResultRange range)
        {
            // Add parameters to the edit link to preserve context on return
            var dictParams = HttpUtility.ParseQueryString(Request.Url.Query);

            // Issue #458: clone and reverse are getting duplicated and the & is getting url encoded, so even edits look like clones
            dictParams.Remove("Clone");
            dictParams.Remove("Reverse");
            dictParams.Remove("src");
            dictParams.Remove("chk");
            dictParams.Remove("a");

            // clear out any others that may be defaulted
            dictParams.Remove("fq");
            dictParams.Remove("se");
            dictParams.Remove("so");
            dictParams.Remove("pg");

            // and add back from the 4 above as needed
            if (fq != null && !fq.IsDefault)
                dictParams["fq"] = fq.ToBase64CompressedJSONString();
            if (fr != null)
            {
                if (fr.CurrentSortKey.CompareCurrentCultureIgnoreCase(LogbookEntry.DefaultSortKey) != 0)
                    dictParams["se"] = fr.CurrentSortKey;
                if (fr.CurrentSortDir != LogbookEntry.DefaultSortDir)
                    dictParams["so"] = fr.CurrentSortDir.ToString();
            }
            if ((range?.PageNum ?? 0) != 0)
                dictParams["pg"] = range.PageNum.ToString(CultureInfo.InvariantCulture);
            return dictParams.ToString();
        }

        /// <summary>
        /// Sets up - as appropriate, the next/previous flight for the specified flight with the specified query.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="idFlight"></param>
        /// <returns></returns>
        protected string SetUpNextPrevious(FlightQuery q, int idFlight, Profile pf)
        {
            FlightResult fr = FlightResultManager.FlightResultManagerForUser(User.Identity.Name).ResultsForQuery(q);
            string sortExpr = Request["se"] ?? fr.CurrentSortKey;
            SortDirection sortDir = Enum.TryParse(Request["so"], out SortDirection sd) ? sd : fr.CurrentSortDir;

            int flightsPerPage = FlightsPerPageForUser(pf);
            string szParam = GetContextParams(q, fr, fr.GetResultRange(flightsPerPage, int.TryParse(Request["pg"], out int page) ? FlightRangeType.Page : FlightRangeType.First, sortExpr, sortDir, page));
            if (!String.IsNullOrEmpty(szParam))
                szParam = "?" + szParam;

            // Find any next/previous values
            // "idflightplus1" is the *previous* flight for a descending date search...
            int _ = fr.IndexOfFlightID(idFlight, out int idFlightPlus1, out int idFlightMinus1);

            if (idFlightMinus1 > 0)
                ViewBag.nextFlightHref = String.Format(CultureInfo.InvariantCulture, "~/mvc/FlightEdit/flight/{0}", idFlightMinus1.ToString(CultureInfo.InvariantCulture)).ToAbsolute() + szParam;
            if (idFlightPlus1 > 0)
                ViewBag.prevFlightHref = String.Format(CultureInfo.InvariantCulture, "~/mvc/FlightEdit/flight/{0}", idFlightPlus1.ToString(CultureInfo.InvariantCulture)).ToAbsolute() + szParam;

            return szParam;
        }

        protected static int FlightsPerPageForUser(Profile pf)
        {
            return pf?.GetPreferenceForKey(MFBConstants.keyPrefFlightsPerPage, MFBConstants.DefaultFlightsPerPage) ?? MFBConstants.DefaultFlightsPerPage;
        }
        #endregion
    }
}