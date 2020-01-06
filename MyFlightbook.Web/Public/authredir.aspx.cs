using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Security;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_authredir : System.Web.UI.Page
{
    private string RedirForDest(string szDest, List<string> lstParams)
    {
        switch (szDest.ToUpperInvariant())
        {
            case "FLIGHTS":
                return "~/Member/LogbookNew.aspx";
            case "MINIFLIGHTS":
                return "~/Member/MiniRecents.aspx";
            case "PROFILE":
                lstParams.Add("nolocalprefs=yes");
                return "~/Member/EditProfile.aspx/pftPrefs";
            case "DONATE":
                return "~/Member/EditProfile.aspx/pftDonate";
            case "ENDORSE":
                return "~/Member/Training.aspx/instEndorsements";
            case "PROGRESS":
                return "~/Member/RatingProgress.aspx";
            case "BADGES":
                return "~/Member/Achievements.aspx";
            case "STUDENTS":
            case "STUDENTSFIXED":
                return "~/Member/Training.aspx/instStudents";
            case "INSTRUCTORS":
            case "INSTRUCTORSFIXED":
                return "~/Member/Training.aspx/instInstructors";
            case "8710":
                return "~/Member/8710Form.aspx";
            case "AIRCRAFTEDIT":
                return "~/Member/EditAircraft.aspx";
            case "AIRCRAFTSCHEDULE":
                return "~/Member/ACSchedule.aspx";
            case "FAQ":
                return "~/Public/FAQ.aspx";
            case "REQSIGS":
                return "~/Member/RequestSigs.aspx";
            case "FLIGHTREVIEW":
                lstParams.Add("pane=flightreview");
                return "~/Member/EditProfile.aspx/pftPilotInfo";
            case "CERTIFICATES":
                lstParams.Add("pane=certificates");
                return "~/Member/EditProfile.aspx/pftPilotInfo";
            case "MEDICAL":
                lstParams.Add("pane=medical");
                return "~/Member/EditProfile.aspx/pftPilotInfo";
            case "DEADLINE":
                lstParams.Add("pane=deadlines");
                return "~/Member/EditProfile.aspx/pftPrefs";
            case "CUSTOMCURRENCY":
                lstParams.Add("pane=custcurrency");
                return "~/Member/EditProfile.aspx/pftPrefs";
            default:
                return string.Empty;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        string szDestErr = "~/Default.aspx";

        string szUser = util.GetStringParam(Request, "u");
        string szPass = util.GetStringParam(Request, "p");
        string szDest = util.GetStringParam(Request, "d");

        if (!MFBWebService.CheckSecurity(Request) ||
            String.IsNullOrEmpty(szUser) || 
            String.IsNullOrEmpty(szPass) || 
            String.IsNullOrEmpty(szDest))
            Response.Redirect(szDestErr);

        // look for admin emulation in the form of 
        string[] rgUsers = szUser.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
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
                Profile pf = MyFlightbook.Profile.GetUser(szUser);
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

        if (szDest.Length == 0)
            Response.Redirect(szDestErr);
        else
        {
            string szUrlRedir = String.Format(CultureInfo.InvariantCulture, "javascript:window.top.location='{0}?{1}'", ResolveUrl(szDest), String.Join("&", lstParams.ToArray()));
            Page.ClientScript.RegisterStartupScript(this.GetType(), "StartupRedir", szUrlRedir, true);
        }
    }
}