﻿using System;
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
                { "FLIGHTS", "~/mvc/flights"},
                { "MINIFLIGHTS", "~/mvc/flights/minirecents"},
                { "PROFILE", "~/mvc/prefs"},
                { "DONATE", "~/mvc/Donate"},
                { "ENDORSE", "~/mvc/Training/Endorsements"},
                { "PROGRESS", "~/mvc/Training/RatingsProgress" },
                { "BADGES", "~/mvc/Training/Achievements" },
                { "STUDENTS", "~/mvc/training/students" },
                { "STUDENTSFIXED", "~/mvc/training/students"},
                { "INSTRUCTORS", "~/mvc/training/instructors" },
                { "INSTRUCTORSFIXED", "~/mvc/training/instructors" },
                { "8710", "~/mvc/training/reports/8710"},
                { "MODELROLLUP", "~/mvc/training/reports/Model"},
                { "TIMEROLLUP", "~/mvc/training/reports/Time" },
                { "AIRCRAFTEDIT", "~/Member/EditAircraft.aspx"},
                { "AIRCRAFTSCHEDULE", "~/mvc/club/ACSchedule"},
                { "FAQ", "~/mvc/faq"},
                { "REQSIGS", "~/mvc/Training/RequestSigs"},
                { "FLIGHTREVIEW", "~/mvc/prefs/pilotinfo"},
                { "CERTIFICATES", "~/mvc/prefs/pilotinfo"},
                { "MEDICAL", "~/mvc/prefs/pilotinfo"},
                { "DEADLINE", "~/mvc/prefs"},
                { "CUSTOMCURRENCY", "~/mvc/prefs"},
                { "ACCOUNT", "~/mvc/prefs/account"},
                { "BIGREDBUTTONS", "~/mvc/prefs/account"},
                { "CONTACT", "~/mvc/pub/contact"},
                { "SIGNENTRY", "~/mvc/flightedit/SignMobile"}
        };

        private readonly static Dictionary<string, string> dictAdditionalParams = new Dictionary<string, string>()
        {
            { "PROFILE", "nolocalprefs=yes" },
            { "FLIGHTREVIEW", "pane=flightreviews" },
            { "CERTIFICATES", "pane=certs" },
            { "MEDICAL",  "pane=medical" },
            { "DEADLINE","pane=deadlines" },
            { "CUSTOMCURRENCY", "pane=custcurrency" },
            { "BIGREDBUTTONS", "pane=redbuttons" }
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

            // Issue #1204: Apple isn't properly url-encoding a "+" sign as %2B, so it comes through as a space.  But it's perfectly legal in an email address.
            string szUserName = Membership.GetUserNameByEmail(szUser);
            if (String.IsNullOrEmpty(szUserName) && szUser.Contains(" "))
                szUserName = Membership.GetUserNameByEmail(szUser.Replace(' ', '+'));
            szUser = szUserName;

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

            // Issue #1223 - if night mode isn't set, explicitly turn it off
            if (Request["night"] == null)
                lstParams.Add("night=no");

            if (lstParams.Contains("naked=1"))
                Session["IsNaked"] = true;

            Response.Redirect(szDest.Length == 0 ? szDestErr : String.Format(CultureInfo.InvariantCulture, "{0}?{1}", ResolveUrl(szDest), String.Join("&", lstParams)));
        }
    }
}