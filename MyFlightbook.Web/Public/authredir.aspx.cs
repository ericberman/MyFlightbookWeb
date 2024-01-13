using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Security;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.PublicPages
{
    public partial class authredir : Page
    {
        private readonly static Dictionary<string, string> dictRedir = new Dictionary<string, string>()
        {
                { "FLIGHTS", "~/Member/LogbookNew.aspx"},
                { "MINIFLIGHTS", "~/Member/MiniRecents.aspx"},
                { "PROFILE", "~/Member/EditProfile.aspx/pftPrefs"},
                { "DONATE", "~/mvc/Donate"},
                { "ENDORSE", "~/Member/Training.aspx/instEndorsements"},
                { "PROGRESS", "~/mvc/Training/RatingsProgress" },
                { "BADGES", "~/mvc/Training/Achievements" },
                { "STUDENTS", "~/Member/Training.aspx/instStudents"},
                { "STUDENTSFIXED", "~/Member/Training.aspx/instStudents"},
                { "INSTRUCTORS", "~/Member/Training.aspx/instInstructors"},
                { "INSTRUCTORSFIXED", "~/Member/Training.aspx/instInstructors"},
                { "8710", "~/Member/8710Form.aspx"},
                { "MODELROLLUP", "~/Member/8710Form.aspx/Model"},
                { "TIMEROLLUP", "~/Member/8710Form.aspx/Time"},
                { "AIRCRAFTEDIT", "~/Member/EditAircraft.aspx"},
                { "AIRCRAFTSCHEDULE", "~/mvc/club/ACSchedule"},
                { "FAQ", "~/mvc/faq"},
                { "REQSIGS", "~/Member/RequestSigs.aspx"},
                { "FLIGHTREVIEW", "~/Member/EditProfile.aspx/pftPilotInfo"},
                { "CERTIFICATES", "~/Member/EditProfile.aspx/pftPilotInfo"},
                { "MEDICAL", "~/Member/EditProfile.aspx/pftPilotInfo"},
                { "DEADLINE", "~/Member/EditProfile.aspx/pftPrefs"},
                { "CUSTOMCURRENCY", "~/Member/EditProfile.aspx/pftPrefs"},
                { "ACCOUNT", "~/Member/EditProfile.aspx/pftAccount"},
                { "BIGREDBUTTONS", "~/Member/EditProfile.aspx/pftBigRedButtons"},
                { "CONTACT", "~/public/ContactMe.aspx"},
                { "SIGNENTRY", "~/public/SignEntry.aspx"}
        };

        private readonly static Dictionary<string, string> dictAdditionalParams = new Dictionary<string, string>()
        {
            { "PROFILE", "nolocalprefs=yes" },
            { "FLIGHTREVIEW", "pane=flightreview" },
            { "CERTIFICATES", "pane=certificates" },
            { "MEDICAL",  "pane=medical" },
            { "DEADLINE","pane=deadlines" },
            { "CUSTOMCURRENCY", "pane=custcurrency" }
        };

        private static string RedirForDest(string szDest, List<string> lstParams)
        {
            string dest = szDest.ToUpperInvariant();
            if (dictRedir.TryGetValue(dest, out string redir))
            {
                if (dictAdditionalParams.TryGetValue(dest, out string szParams))
                    lstParams.Add(szParams);
                return redir;
            }
            return string.Empty;
        }

        private static readonly char[] adminSeparator = new char[] { ':' };

        protected void Page_Load(object sender, EventArgs e)
        {
            const string szDestErr = "~/Default.aspx";

            string szUser = util.GetStringParam(Request, "u");
            string szPass = util.GetStringParam(Request, "p");
            string szDest = util.GetStringParam(Request, "d");

            if (!MFBWebService.CheckSecurity(Request) ||
                String.IsNullOrEmpty(szUser) ||
                String.IsNullOrEmpty(szPass) ||
                String.IsNullOrEmpty(szDest))
                Response.Redirect(szDestErr);

            // look for admin emulation in the form of 
            string[] rgUsers = szUser.Split(adminSeparator, StringSplitOptions.RemoveEmptyEntries);
            string szEmulate = string.Empty;
            if (rgUsers != null && rgUsers.Length == 2)
            {
                szEmulate = rgUsers[0];
                szUser = rgUsers[1];
            }

            szUser = Membership.GetUserNameByEmail(szUser);

            if (Membership.ValidateUser(szUser, szPass))
            {
                if (!String.IsNullOrEmpty(szEmulate))   // emulation requested - validate that the authenticated user is actually authorized!!!
                {
                    Profile pf = Profile.GetUser(szUser);
                    if (pf.CanSupport || pf.CanManageData)
                    {
                        // see if the emulated user actually exists
                        pf = MyFlightbook.Profile.GetUser(szEmulate);
                        if (!pf.IsValid())
                            throw new MyFlightbookException("No such user: " + szEmulate);
                        szUser = szEmulate;
                    }
                    else
                        throw new UnauthorizedAccessException();
                }

                FormsAuthentication.SetAuthCookie(szUser, false);
            }

            List<string> lstParams = new List<string>();

            // BUGBUG: I got students/instructors reversed in iPhone.
            if (Request.UserAgent.Contains("iPhone") || Request.UserAgent.Contains("iPad"))
            {
                if (String.Compare(szDest, "students", StringComparison.CurrentCultureIgnoreCase) == 0)
                    szDest = "instructors";
                else if (String.Compare(szDest, "instructors", StringComparison.CurrentCultureIgnoreCase) == 0)
                    szDest = "students";
            }

            szDest = RedirForDest(szDest, lstParams);

            // this is something of a hack, but pass on any additional parameters
            foreach (string szKey in Request.QueryString.Keys)
                if (szKey != "u" && szKey != "p" && szKey != "d")
                    lstParams.Add(String.Format(CultureInfo.InvariantCulture, "{0}={1}", szKey, Request.Params[szKey]));

            if (lstParams.Contains("naked=1"))
                Session["IsNaked"] = true;

            Response.Redirect(szDest.Length == 0 ? szDestErr : String.Format(CultureInfo.InvariantCulture, "{0}?{1}", ResolveUrl(szDest), String.Join("&", lstParams)));
        }
    }
}