﻿using MyFlightbook.Clubs;
using MyFlightbook.Currency;
using MyFlightbook.Printing;
using MyFlightbook.Schedule;
using MyFlightbook.Subscriptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.ServiceModel;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2022-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Ajax
{
    /// <summary>
    /// Provides AUTHENTICATED AJAX support for the Website.  NOT FOR EXTERNAL CONSUMPTION!!!  These APIs may change at any point.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    public class MyFlightbookAjax : WebService
    {
        private static void CheckAuth()
        {
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException();
        }

        #region Flight Methods
        /// <summary>
        /// Gets a flight by id for the current user.
        /// </summary>
        /// <param name="idFlight"></param>
        /// <param name="fIncludeImages"></param>
        /// <param name="fIncludeTelemetry"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [WebMethod(EnableSession = true)]
        public LogbookEntryDisplay GetFlight(int idFlight, bool fIncludeImages = false, bool fIncludeTelemetry = false)
        {
            CheckAuth();

            LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, HttpContext.Current.User.Identity.Name, fIncludeTelemetry ? LogbookEntryCore.LoadTelemetryOption.LoadAll : LogbookEntryCore.LoadTelemetryOption.None);
            if (led.FlightID != idFlight)
                throw new UnauthorizedAccessException();

            if (fIncludeImages)
                led.PopulateImages();

            return led;
        }

        [WebMethod(EnableSession = true)]
        public void sendFlight(int idFlight, string szTargetEmail, string szMessage, string szSendPageTarget)
        {
            CheckAuth();

            if (String.IsNullOrWhiteSpace(szTargetEmail))
                throw new ArgumentException(Resources.LocalizedText.ValidationEmailRequired);

            if (!RegexUtility.Email.IsMatch(szTargetEmail))
                throw new ArgumentException(Resources.LocalizedText.ValidationEmailFormat);

            string szUser = HttpContext.Current.User.Identity.Name;
            LogbookEntry le = new LogbookEntry(Convert.ToInt32(idFlight, CultureInfo.InvariantCulture), szUser);
            Profile pfSender = Profile.GetUser(szUser);

            using (MailMessage msg = new MailMessage())
            {
                msg.Body = Branding.ReBrand(Resources.LogbookEntry.SendFlightBody.Replace("<% Sender %>", HttpUtility.HtmlEncode(pfSender.UserFullName))
                    .Replace("<% Message %>", HttpUtility.HtmlEncode(szMessage))
                    .Replace("<% Date %>", le.Date.ToShortDateString())
                    .Replace("<% Aircraft %>", HttpUtility.HtmlEncode(le.TailNumDisplay))
                    .Replace("<% Route %>", HttpUtility.HtmlEncode(le.Route))
                    .Replace("<% Comments %>", HttpUtility.HtmlEncode(le.Comment))
                    .Replace("<% Time %>", le.TotalFlightTime.FormatDecimal(pfSender.UsesHHMM))
                    .Replace("<% FlightLink %>", le.SendFlightUri(Branding.CurrentBrand.HostName, szSendPageTarget).ToString()));

                msg.Subject = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.SendFlightSubject, pfSender.UserFullName);
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pfSender.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pfSender.Email));
                msg.To.Add(new MailAddress(szTargetEmail));
                msg.IsBodyHtml = true;
                util.SendMessage(msg);
            }
        }
        #endregion

        #region Aircraft Methods
        /// <summary>
        /// Autocompletion for modelnames
        /// </summary>
        /// <param name="prefixText"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [WebMethod(EnableSession = true)]
        public string[] SuggestFullModels(string prefixText, int count)
        {
            CheckAuth();
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();

            ModelQuery modelQuery = new ModelQuery() { FullText = prefixText.Replace("-", "*"), PreferModelNameMatch = true, Skip = 0, Limit = count };
            List<string> lst = new List<string>();
            foreach (MakeModel mm in MakeModel.MatchingMakes(modelQuery))
                lst.Add(AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithDash, mm.ManufacturerDisplay, mm.ModelDisplayName), mm.MakeModelID.ToString(CultureInfo.InvariantCulture)));

            return lst.ToArray();
        }

        /// <summary>
        /// Autocompletion for aircraft tails - TODO: this will be obsolete after MVC Migration.
        /// </summary>
        /// <param name="prefixText"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [WebMethod(EnableSession = true)]
        public string[] SuggestAircraft(string prefixText, int count)
        {
            CheckAuth();
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            IEnumerable<Aircraft> lstAircraft = Aircraft.AircraftWithPrefix(prefixText, count);
            List<string> lst = new List<string>();
            foreach (Aircraft ac in lstAircraft)
                lst.Add(AjaxControlToolkit.AutoCompleteExtender.CreateAutoCompleteItem(String.Format(CultureInfo.CurrentCulture, "{0} - {1}", ac.TailNumber, ac.ModelDescription), ac.AircraftID.ToString(CultureInfo.InvariantCulture)));
            return lst.ToArray();
        }

        /// <summary>
        /// Return the high-water mark for an aircraft's tach or hobbs
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns></returns>
        /// <exception cref="MyFlightbookException"></exception>
        [WebMethod(EnableSession = true)]
        public string GetHighWaterMarks(int idAircraft)
        {
            CheckAuth();
            if (String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new MyFlightbookException("Unauthenticated call to GetHighWaterMarks");

            if (idAircraft <= 0)
                return String.Empty;

            decimal hwHobbs = AircraftUtility.HighWaterMarkHobbsForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name);
            decimal hwTach = AircraftUtility.HighWaterMarkTachForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name);

            if (hwTach == 0)
                return hwHobbs == 0 ? String.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkHobbsOnly, hwHobbs);
            else if (hwHobbs == 0)
                return String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachOnly, hwTach);
            else
                return String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachAndHobbs, hwTach, hwHobbs);
        }

        #region user-specific aircraft calls (roles, templates, active/inactive)
        /// <summary>
        /// Calls CheckAuth but also verifies that the specified idAircraft is in the user's aircraft list, returning the aircraft itself and the corresponding useraircraft
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <param name="ua"></param>
        /// <returns></returns>
        /// <exception cref="MyFlightbookException"></exception>
        private static Aircraft CheckValidUserAircraft(int idAircraft, out UserAircraft ua)
        {
            CheckAuth();

            if (idAircraft <= 0)
                throw new MyFlightbookException("Invalid aircraft ID");

            ua = new UserAircraft(HttpContext.Current.User.Identity.Name);
            Aircraft ac = ua[idAircraft];
            if (ac == null || ac.AircraftID == Aircraft.idAircraftUnknown)
                throw new MyFlightbookException("This is not your aircraft");
            return ac;
        }

        /// <summary>
        /// Toggles the active state of a given aircraft.
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="fIsActive">Active or inactive</param>
        [WebMethod(EnableSession = true)]
        public void SetActive(int idAircraft, bool fIsActive)
        {
            Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);

            ac.HideFromSelection = !fIsActive;
            ua.FAddAircraftForUser(ac);
        }

        /// <summary>
        /// Sets the role for flights in the given aircraft
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="fAddPICName">True to copy the pic name when autofilling PIC</param>
        /// <param name="Role">The role to assign</param>
        [WebMethod(EnableSession = true)]
        public void SetRole(int idAircraft, string Role, bool fAddPICName)
        {
            Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);

            if (!Enum.TryParse(Role, true, out Aircraft.PilotRole role))
                throw new MyFlightbookException("Invalid role - " + Role);
            ac.RoleForPilot = role;
            ac.CopyPICNameWithCrossfill = role == Aircraft.PilotRole.PIC && fAddPICName;
            ua.FAddAircraftForUser(ac);
        }

        /// <summary>
        /// Sets the role for flights in the given aircraft
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="idTemplate">The ID of the template to add or remove.</param>
        /// <param name="fAdd">True to add the template, false to remove it</param>
        [WebMethod(EnableSession = true)]
        public void AddRemoveTemplate(int idAircraft, int idTemplate, bool fAdd)
        {
            Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);

            if (fAdd)
                ac.DefaultTemplates.Add(idTemplate);
            else
                ac.DefaultTemplates.Remove(idTemplate);
            ua.FAddAircraftForUser(ac);
        }
        #endregion
        #endregion

        #region LogbookNew stuff
        /// <summary>
        /// Returns the high-watermark starting hobbs for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [WebMethod(EnableSession = true)]
        public string HighWaterMarkHobbsForAircraft(int idAircraft)
        {
            CheckAuth();

            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            return AircraftUtility.HighWaterMarkHobbsForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns the high-watermark starting hobbs for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [WebMethod(EnableSession = true)]
        public string HighWaterMarkTachForAircraft(int idAircraft)
        {
            CheckAuth();

            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            return AircraftUtility.HighWaterMarkTachForUserInAircraft(idAircraft, HttpContext.Current.User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns the high-watermark flight meter for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [WebMethod(EnableSession = true)]
        public string HighWaterMarkFlightMeter(int idAircraft)
        {
            CheckAuth();

            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            return AircraftUtility.HighWaterMarkFlightMeter(idAircraft, HttpContext.Current.User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns the current time formatted in UTC or specified time-zone
        /// </summary>
        /// <returns>Now in the specified locale, adjusted for the timezone.</returns>
        [WebMethod(EnableSession = true)]
        public string NowInUTC()
        {
            CheckAuth();

            // For now, always return true UTC
            if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length > 0)
                util.SetCulture(HttpContext.Current.Request.UserLanguages[0]);

            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            return DateTime.UtcNow.FormattedNowInUtc(Profile.GetUser(HttpContext.Current.User.Identity.Name).PreferredTimeZone);
        }

        private readonly static char[] trainingPrefixSeparators = new char[] { '-', '[' };

        [WebMethod(EnableSession = true)]
        public string[] SuggestTraining(string prefixText, int count)
        {
            CheckAuth();

            const string szCacheKey = "keyTrainingAutocomplete";

            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();

            // Don't do anything if the cache is null - bad things afoot!
            if (HttpRuntime.Cache == null)
                return Array.Empty<string>();

            List<string> lst = (List<string>)HttpRuntime.Cache[szCacheKey];
            if (lst == null)
            {
                lst = new List<string>();
                DBHelper dbh = new DBHelper("SELECT * FROM trainingitems");
                dbh.ReadRows((comm) => { }, (dr) => { lst.Add(String.Format(CultureInfo.CurrentCulture, "[{0}]", dr["task"])); });
                HttpRuntime.Cache.Add(szCacheKey, lst, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0, 0), System.Web.Caching.CacheItemPriority.BelowNormal, null);
            }

            string[] rgszTerms = prefixText.ToUpper(CultureInfo.CurrentCulture).Split(trainingPrefixSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (rgszTerms.Length == 0)
                return Array.Empty<string>();

            List<string> lstResult = lst.FindAll((sz) =>
            {
                foreach (string szTerm in rgszTerms)
                    if (!sz.ToUpper(CultureInfo.CurrentCulture).Contains(szTerm))
                        return false;
                return true;
            });

            if (lstResult.Count > count)
                lstResult.RemoveRange(count, lstResult.Count - count);

            return lstResult.ToArray();
        }


        [WebMethod(EnableSession = true)]
        public string PrintLink(string szExisting, PrintingSections ps)
        {
            CheckAuth();

            // check for invalid data
            if (szExisting == null || ps == null)
                return szExisting;

            return PrintingOptions.UpdatedPermaLink(szExisting, new PrintingOptions() { Sections = ps }).ToString();
        }

        [WebMethod(EnableSession = true)]
        public string TaxiTime(string fsStart, string fsEnd, string szTotal)
        {
            CheckAuth();

            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            bool fUseHHMM = Profile.GetUser(HttpContext.Current.User.Identity.Name).UsesHHMM;

            DateTime dtFStart = fsStart.SafeParseDate(DateTime.MinValue);
            DateTime dtFEnd = fsEnd.SafeParseDate(DateTime.MinValue);
            decimal totalTime = fUseHHMM ? szTotal.DecimalFromHHMM() : decimal.Parse(szTotal, NumberStyles.Any, CultureInfo.CurrentCulture);

            decimal elapsedFlight = (dtFEnd.HasValue() && dtFStart.HasValue() && dtFEnd.CompareTo(dtFStart) > 0) ? (decimal)dtFEnd.Subtract(dtFStart).TotalHours : 0;

            decimal taxi = Math.Max(totalTime - elapsedFlight, 0);
            return taxi.FormatDecimal(fUseHHMM);
        }

        [WebMethod(EnableSession = true)]
        public string AirborneTime(string fsStart, string fsEnd)
        {
            CheckAuth();
            System.Threading.Thread.CurrentThread.CurrentCulture = util.SessionCulture ?? CultureInfo.CurrentCulture;

            bool fUseHHMM = Profile.GetUser(HttpContext.Current.User.Identity.Name).UsesHHMM;

            DateTime dtFStart = fsStart.SafeParseDate(DateTime.MinValue);
            DateTime dtFEnd = fsEnd.SafeParseDate(DateTime.MinValue);

            decimal elapsedFlight = (dtFEnd.HasValue() && dtFStart.HasValue() && dtFEnd.CompareTo(dtFStart) > 0) ? (decimal)dtFEnd.Subtract(dtFStart).TotalHours : 0;

            return Math.Max(elapsedFlight, 0).FormatDecimal(fUseHHMM);
        }
        #endregion

        #region Preferences
        /// <summary>
        /// Sets the color for a specified query; null or empty color string to remove it.
        /// </summary>
        /// <param name="queryName"></param>
        /// <param name="color"></param>
        [WebMethod(EnableSession = true)]
        public void SetColorForQuery(string queryName, string color)
        {
            CheckAuth();

            // Get the query
            List<CannedQuery> lst = new List<CannedQuery>(CannedQuery.QueriesForUser(User.Identity.Name));
            CannedQuery cq = lst.Find(q => q.QueryName.CompareCurrentCultureIgnoreCase(queryName) == 0) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Unknown query '{0}'", queryName));
            cq.ColorString = String.IsNullOrWhiteSpace(color) ? null : color.Replace("#", string.Empty);
            cq.Commit();
        }

        [WebMethod(EnableSession = true)]
        public void SetMapColors(string routeColor, string pathColor)
        {
            CheckAuth();

            // Verify that both colors are valid colors
            if (!String.IsNullOrWhiteSpace(routeColor) && !RegexUtility.HexColor.IsMatch(routeColor))
                throw new ArgumentOutOfRangeException("Invalid route color: " + routeColor);
            if (!String.IsNullOrWhiteSpace(pathColor) && !RegexUtility.HexColor.IsMatch(pathColor))
                throw new ArgumentOutOfRangeException("Invalid path color: " + pathColor);
            Profile pf = Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(MFBConstants.keyRouteColor, routeColor, String.IsNullOrWhiteSpace(routeColor));
            pf.SetPreferenceForKey(MFBConstants.keyPathColor, pathColor, String.IsNullOrWhiteSpace(pathColor));
        }

        #region Preference Helper Functions 
        private static void SetDecimalPref(Profile pf, string prefValue)
        {
            if (prefValue.CompareCurrentCultureIgnoreCase("HHMM") == 0)
            {
                pf.UsesHHMM = true;
                pf.SetPreferenceForKey(MFBConstants.keyDecimalSettings, null, true);
                HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = null;
            }
            else if (Enum.TryParse<DecimalFormat>(prefValue, true, out DecimalFormat df))
            {
                pf.UsesHHMM = false;
                pf.SetPreferenceForKey(MFBConstants.keyDecimalSettings, df, df == DecimalFormat.Adaptive);
                HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = df;
            }
            else
                throw new ArgumentException("Unknown decimal value preference: " + prefValue);
        }

        private static void SetRoundingPref(Profile pf, string prefValue)
        {
            if (int.TryParse(prefValue, out int n) && n >= 0)
            {
                pf.SetPreferenceForKey(MFBConstants.keyMathRoundingUnits, n, n == 60);
                HttpContext.Current.Session[MFBConstants.keyMathRoundingUnits] = n;
            }
            else
                throw new ArgumentException("invalid math rounding value " + prefValue);
        }

        private static void SetDisplayFields(Profile pf, string prefValue)
        {
            int[] rgPerms = (String.IsNullOrEmpty(prefValue)) ? Array.Empty<int>() : JsonConvert.DeserializeObject<int[]>(prefValue);

            // check for default array - 10 items, in order.
            const int defSize = 10;  // hack - assuming it's 10.
            bool fIsDefault = rgPerms.Length == defSize;
            int i = 0;
            while (fIsDefault && i < rgPerms.Length && rgPerms[i] == i)
                i++;

            if (fIsDefault && i == defSize)
                rgPerms = Array.Empty<int>();

            pf.SetPreferenceForKey(MFBConstants.keyCoreFieldsPermutation, rgPerms, rgPerms == null || rgPerms.Length == 0);
        }

        private static void SetTotalsMode(Profile pf, string prefValue)
        {
            pf.TotalsGroupingMode = Enum.TryParse(prefValue, true, out TotalsGrouping tg)
                ? tg
                : throw new ArgumentException("invalid totals grouping mode " + prefValue);
        }

        private static void SetCurrencyJurisdiction(Profile pf, string prefValue)
        {
            pf.CurrencyJurisdiction = Enum.TryParse(prefValue, true, out CurrencyJurisdiction cj)
                ? cj
                : throw new ArgumentException("invalid currency jurisdiction " + prefValue);
        }

        private static void SetCurrencyExpiration(Profile pf, string prefValue)
        {
            pf.CurrencyExpiration = Enum.TryParse(prefValue, true, out CurrencyExpiration.Expiration ce)
                ? ce
                : throw new ArgumentException("invalid currency expiration " + prefValue);
        }

        private static void SetMaintenanceWindow(Profile pf, string prefValue)
        {
            int maintWindow = Convert.ToInt32(prefValue, CultureInfo.InvariantCulture);
            pf.SetPreferenceForKey(MFBConstants.keyWindowAircraftMaintenance, maintWindow, maintWindow == MFBConstants.DefaultMaintenanceWindow);
        }

        private static readonly Dictionary<string, Action<Profile, string, bool>> _setPrefMap = new Dictionary<string, Action<Profile, string, bool>>()
        {
            { "DECIMAL" , (pf, prefValue, fIsSet) => { SetDecimalPref(pf, prefValue); } },
            { "ROUNDING" , (pf, prefValue, fIsSet) => { SetRoundingPref(pf, prefValue); } },
            { "TIMEZONE" , (pf, prefValue, fIsSet) => { pf.PreferredTimeZoneID = prefValue; } },
            { "DATEOFFLIGHTTZ" , (pf, prefValue, fIsSet) => { pf.UsesUTCDateOfFlight = prefValue.CompareCurrentCultureIgnoreCase("UTC") == 0; } },
            { "USECFI" , (pf, prefValue, fIsSet) => { pf.IsInstructor = fIsSet; } },
            { "USESIC" , (pf, prefValue, fIsSet) => { pf.TracksSecondInCommandTime = fIsSet; } },
            { "TRACKTIMES" , (pf, prefValue, fIsSet) => { pf.DisplayTimesByDefault = fIsSet; } },
            { "TRACKORIGINAL" , (pf, prefValue, fIsSet) => { pf.SetPreferenceForKey(MFBConstants.keyTrackOriginal, fIsSet, !fIsSet); } },
            { "FIELDDISPLAY" , (pf, prefValue, fIsSet) => { SetDisplayFields(pf, prefValue); } },
            { "EMAILCURRENCYWEEKLY" , (pf, prefValue, fIsSet) => { pf.Subscriptions = new EmailSubscriptionManager(pf.Subscriptions).SetSubscription(SubscriptionType.Currency, fIsSet); } },
            { "EMAILCURRENCYEXPIRING" , (pf, prefValue, fIsSet) => { pf.Subscriptions = new EmailSubscriptionManager(pf.Subscriptions).SetSubscription(SubscriptionType.Expiration, fIsSet); } },
            { "EMAILTOTALSWEEKLY" , (pf, prefValue, fIsSet) => { pf.Subscriptions = new EmailSubscriptionManager(pf.Subscriptions).SetSubscription(SubscriptionType.Totals, fIsSet); } },
            { "EMAILTOTALSMONTHLY" , (pf, prefValue, fIsSet) => { pf.Subscriptions = new EmailSubscriptionManager(pf.Subscriptions).SetSubscription(SubscriptionType.MonthlyTotals, fIsSet); } },
            { "TOTALSMODE" , (pf, prefValue, fIsSet) => { SetTotalsMode(pf, prefValue); } },
            { "TOTALSINCLUDEMF" , (pf, prefValue, fIsSet) => { pf.SuppressModelFeatureTotals = !fIsSet; } },
            { "CURRENCYJURISDICTION" , (pf, prefValue, fIsSet) => { SetCurrencyJurisdiction(pf, prefValue); } },
            { "CURRENCYEXPIRATION" , (pf, prefValue, fIsSet) => { SetCurrencyExpiration(pf, prefValue); } },
            { "USEARMYCURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesArmyCurrency = fIsSet; } },
            { "USE117DUTYTIME" , (pf, prefValue, fIsSet) => { pf.UsesFAR117DutyTime = fIsSet; } },
            { "USE117DUTYALLFLIGHTS" , (pf, prefValue, fIsSet) => { pf.UsesFAR117DutyTimeAllFlights = fIsSet; } },
            { "USE1252CURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesFAR1252xxCurrency = fIsSet; } },
            { "USE135DUTYTIME" , (pf, prefValue, fIsSet) => { pf.UsesFAR135DutyTime = fIsSet; } },
            { "USEFAR13529XCURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesFAR13529xCurrency = fIsSet; } },
            { "USEFAR13526XCURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesFAR13526xCurrency = fIsSet; } },
            { "USEFAR61217CURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesFAR61217Currency = fIsSet; } },
            { "ALLOWNIGHTTOUCHANDGO" , (pf, prefValue, fIsSet) => { pf.AllowNightTouchAndGoes = fIsSet; } },
            { "ONLYDAYLANDINGSFORDAYCURRENCY" , (pf, prefValue, fIsSet) => { pf.OnlyDayLandingsForDayCurrency = fIsSet; } },
            { "USEPERMODELCURRENCY" , (pf, prefValue, fIsSet) => { pf.UsesPerModelCurrency = fIsSet; } },
            { "MAINTWINDOW", (pf, prefValue, fIsSet) => { SetMaintenanceWindow(pf, prefValue); } },
            { "USEHOBBS", (pf, prefValue, fIsSet) => { pf.UseHobbs = fIsSet; } },
            { "USETACH", (pf, prefValue, fIsSet) => { pf.UseTach = fIsSet; } },
            { "USEBLOCK", (pf, prefValue, fIsSet) => { pf.UseBlock = fIsSet; } },
            { "USEENGINE", (pf, prefValue, fIsSet) => { pf.UseEngine = fIsSet; } },
            { "USEFLIGHT", (pf, prefValue, fIsSet) => { pf.UseFlight = fIsSet; } }
        };
        #endregion

        /// <summary>
        /// Set a preference value without requiring a postback
        /// </summary>
        /// <param name="prefName">Name of the preference (case insensitive)</param>
        /// <param name="prefValue">The value (validation all depends on the name of the preference)</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        [WebMethod(EnableSession = true)]
        public void SetLocalPref(string prefName, string prefValue)
        {
            if (prefName == null)
                throw new ArgumentNullException(nameof(prefName));
            if (prefValue == null)
                throw new ArgumentNullException(nameof(prefValue));

            CheckAuth();

            Profile pf = Profile.GetUser(HttpContext.Current.User.Identity.Name);

            bool fIsSet = prefValue.CompareCurrentCultureIgnoreCase("true") == 0;

            if (_setPrefMap.TryGetValue(prefName.ToUpperInvariant(), out Action<Profile, string, bool> action))
                action(pf, prefValue, fIsSet);
            else
                throw new ArgumentException("Unknown value for preference " + prefName, nameof(prefName));

            pf.FCommit();
        }
        #endregion

        #region Property Autocomplete 
        private static readonly char[] previouslyUsedValsSeparator = new char[] { ';' };
        private static readonly char[] metarAirportSeparator = new char[] { ' ' };

        [WebMethod(EnableSession = true)]
        [System.Web.Script.Services.ScriptMethod]
        public string[] PreviouslyUsedTextProperties(string prefixText, int count, string contextKey)
        {
            string[] rgResultDefault = Array.Empty<string>();

            if (String.IsNullOrEmpty(contextKey) || String.IsNullOrWhiteSpace(prefixText))
                return rgResultDefault;

            string[] rgsz = contextKey.Split(previouslyUsedValsSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (rgsz.Length != 2 || String.IsNullOrEmpty(rgsz[0]) || string.IsNullOrEmpty(rgsz[1]))
                return rgResultDefault;

            if (!Int32.TryParse(rgsz[1], out int idPropType))
                return rgResultDefault;

            // Handle METAR autofill a bit different from other properties: fetch raw metar.
            if (idPropType == (int)CustomPropertyType.KnownProperties.IDPropMetar)
            {
                string[] rgAirports = prefixText.Split(metarAirportSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (rgAirports == null || rgAirports.Length == 0)
                    return rgResultDefault;

                string szLastWord = rgAirports[rgAirports.Length - 1];
                if (szLastWord.Length != 4)
                    return rgResultDefault;

                // trim the last word out of the prefix
                prefixText = prefixText.Trim();
                prefixText = prefixText.Substring(0, prefixText.Length - szLastWord.Length);

                List<string> lst = new List<string>();
                foreach (Weather.ADDS.METAR m in Weather.ADDS.ADDSService.LatestMETARSForAirports(szLastWord, false))
                    lst.Add(String.Format(CultureInfo.CurrentCulture, "{0} {1}", prefixText, m.raw_text).Trim());

                return lst.ToArray();
            }
            else
            {
                Dictionary<int, string[]> d = CustomFlightProperty.PreviouslyUsedTextValuesForUser(rgsz[0]);

                string[] results = (d.TryGetValue(idPropType, out string[] value)) ? value : null;
                if (results == null)
                    return Array.Empty<string>();

                List<string> lst = new List<string>(results);

                string szSearch = prefixText.ToUpperInvariant();

                lst = lst.FindAll(sz => sz.ToUpperInvariant().StartsWith(szSearch, StringComparison.InvariantCulture));
                if (lst.Count > count && count >= 1)
                    lst.RemoveRange(count - 1, lst.Count - count);

                return lst.ToArray();
            }
        }
        #endregion

        #region Other Autocomplete
        private static string[] DoSuggestion(string szQ, string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText) || string.IsNullOrEmpty(szQ) || prefixText.Length <= 2)
                return Array.Empty<string>();

            return util.GetKeysFromDB(String.Format(CultureInfo.InvariantCulture, szQ, util.keyColumn, count), prefixText);
        }

        [WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestModels(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT model AS {0} FROM models WHERE REPLACE(model, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }

        [WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public string[] SuggestModelTypes(string prefixText, int count)
        {
            return DoSuggestion("SELECT DISTINCT typename AS {0} FROM models WHERE REPLACE(typename, '-', '') LIKE CONCAT(?prefix, '%') ORDER BY model ASC LIMIT {1}", Aircraft.NormalizeTail(prefixText, null), count);
        }
        #endregion

        #region Club Scheduling
        /// <summary>
        /// Create an event.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="id"></param>
        /// <param name="text"></param>
        /// <param name="resource"></param>
        /// <param name="clubID"></param>
        /// <returns>Empty string for success; otherwise, the error message</returns>
        [WebMethod(EnableSession = true)]
        public string CreateEvent(DateTime start, DateTime end, string id, string text, string resource, int clubID = Club.ClubIDNew)
        {
            return ScheduledEvent.CreateEvent(start, end, id, text, resource, clubID);
        }

        [WebMethod(EnableSession = true)]
        public ScheduledEvent[] ReadEvents(DateTime dtStart, DateTime dtEnd, int clubID = Club.ClubIDNew, string resourceName = null)
        {
            return ScheduledEvent.ReadEvents(dtStart, dtEnd, clubID, resourceName);
        }

        [WebMethod(EnableSession = true)]
        public string UpdateEvent(DateTime start, DateTime end, string id, string text, string resource, int clubID)
        {
            return ScheduledEvent.UpdateEvent(start, end, id, text, resource, clubID);
        }

        [WebMethod(EnableSession = true)]
        public string DeleteEvent(string id)
        {
            return ScheduledEvent.DeleteEvent(id);
        }
        #endregion
    }
}
