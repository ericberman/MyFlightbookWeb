using MyFlightbook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
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
            throw new ArgumentException("Missing tail in AddNewAircraft");

        string szCurrentUser = HttpContext.Current.User.Identity.Name;

        Aircraft ac = new Aircraft() { TailNumber = szTail, ModelID = idModel, InstanceTypeID = instanceType };

        // Issue #296: allow sims to come through without a sim prefix; we can fix it at AddNewAircraft time.
        AircraftInstance aic = Array.Find(AircraftInstance.GetInstanceTypes(), it => it.InstanceTypeInt == instanceType);
        string szSpecifiedTail = szTail;
        bool fIsNamedSim = !aic.IsRealAircraft && !szTail.ToUpper(CultureInfo.CurrentCulture).StartsWith(CountryCodePrefix.SimCountry.Prefix.ToUpper(CultureInfo.CurrentCulture));
        if (fIsNamedSim)
            ac.TailNumber = CountryCodePrefix.SimCountry.Prefix;

        if (ac.FixTailAndValidate())
        {
            ac.CommitForUser(szCurrentUser);

            UserAircraft ua = new UserAircraft(szCurrentUser);
            if (fIsNamedSim)
                ac.PrivateNotes = String.Format(CultureInfo.InvariantCulture, "{0} #ALT{1}#", ac.PrivateNotes ?? string.Empty, szSpecifiedTail);

            ua.FAddAircraftForUser(ac);
            ua.InvalidateCache();
        }
        else
            throw new MyFlightbookValidationException(ac.ErrorString);
    }

    [WebMethod(EnableSession = true)]
    public static string ValidateAircraft(string szTail, int idModel, int instanceType)
    {
        if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("Unauthenticated call to ValidateAircraft");

        if (string.IsNullOrEmpty(szTail))
            throw new ArgumentException("Empty tail in ValidateAircraft");

        // Issue #296: allow sims to come through without a sim prefix; we can fix it at AddNewAircraft time.
        AircraftInstance aic = Array.Find(AircraftInstance.GetInstanceTypes(), it => it.InstanceTypeInt == instanceType);
        if (!aic.IsRealAircraft && !szTail.ToUpper(CultureInfo.CurrentCulture).StartsWith(CountryCodePrefix.SimCountry.Prefix.ToUpper(CultureInfo.CurrentCulture)))
            szTail = CountryCodePrefix.SimCountry.Prefix;

        Aircraft ac = new Aircraft() { TailNumber = szTail, ModelID = idModel, InstanceTypeID = instanceType };

        ac.FixTailAndValidate();
        return ac.ErrorString;
    }

    [WebMethod(EnableSession = true)]
    public static string[] SuggestFullModelsWithTargets(string prefixText, int count, string contextKey)
    {
        if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
            throw new MyFlightbookException("Unauthenticated call to add new aircraft");

        if (String.IsNullOrEmpty(prefixText))
            return new string[0];

        Dictionary<string, object> dictContextIn = contextKey == null ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(contextKey);

        ModelQuery modelQuery = new ModelQuery() { FullText = prefixText.Replace("-", "*"), PreferModelNameMatch = true, Skip = 0, Limit = count };
        List<string> lst = new List<string>();
        foreach (MakeModel mm in MakeModel.MatchingMakes(modelQuery))
        {
            Dictionary<string, object> d = new Dictionary<string, object>(dictContextIn);
            string modelID = mm.MakeModelID.ToString(CultureInfo.InvariantCulture);
            string modelDisplay = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithDash, mm.ManufacturerDisplay, mm.ModelDisplayName);
            d["modelID"] = modelID;
            d["modelDisplay"] = modelDisplay;
            lst.Add(AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(modelDisplay, JsonConvert.SerializeObject(d)));
        }
        return lst.ToArray();
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}