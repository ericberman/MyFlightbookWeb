using MyFlightbook;
using System;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2016-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ImpAircraftService : System.Web.UI.Page
{
    #region WebMethods
    [WebMethod(EnableSession = true)]
    public static void AddExistingAircraft(int aircraftID)
    {
        if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("Unauthenticated call to add existing aircraft");

        string szCurrentUser = HttpContext.Current.User.Identity.Name;

        UserAircraft ua = new UserAircraft(szCurrentUser);
        Aircraft ac = new Aircraft(aircraftID);

        if (ac.AircraftID == Aircraft.idAircraftUnknown)
            return;

        ua.FAddAircraftForUser(ac);
    }

    [WebMethod(EnableSession = true)]
    public static void AddNewAircraft(string szTail, int idModel, int instanceType)
    {
        if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("Unauthenticated call to add new aircraft");

        if (string.IsNullOrEmpty(szTail))
            throw new ArgumentException("Invalid tail in AddNewAircraft: " + szTail);

        string szCurrentUser = HttpContext.Current.User.Identity.Name;

        Aircraft ac = new Aircraft();
        ac.TailNumber = szTail;
        ac.ModelID = idModel;
        ac.InstanceTypeID = instanceType;
        ac.CommitForUser(szCurrentUser);

        new UserAircraft(szCurrentUser).FAddAircraftForUser(ac);
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}