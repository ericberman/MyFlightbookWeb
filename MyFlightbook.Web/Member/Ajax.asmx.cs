using MyFlightbook.Printing;
using Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2022-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Member
{
    /// <summary>
    /// Provides AUTHENTICATED AJAX support for the Website.  NOT FOR EXTERNAL CONSUMPTION!!!  These APIs may change at any point.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [System.Web.Script.Services.ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    public class Ajax : System.Web.Services.WebService
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
                throw new ArgumentException(LocalizedText.ValidationEmailRequired);

            if (!Regex.IsMatch(szTargetEmail, "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*"))
                throw new ArgumentException(LocalizedText.ValidationEmailFormat);

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
        /// Autocompletion for aircraft tails
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

        [WebMethod(EnableSession = true)]
        public string[] SuggestTraining(string prefixText, int count)
        {
            CheckAuth();

            return LogbookEntryDisplay.SuggestTraining(prefixText, count);
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

            decimal elapsedFlight = (dtFEnd.HasValue() && dtFStart.HasValue() && dtFEnd.CompareTo(dtFStart) > 0) ? (decimal) dtFEnd.Subtract(dtFStart).TotalHours : 0;

            decimal taxi = totalTime - elapsedFlight;
            return taxi.FormatDecimal(fUseHHMM);
        }
        #endregion

    }
}
