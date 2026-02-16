using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.AppCode.Admin
{
    /// <summary>
    /// Concrete implementation of IAircraftDataChangeNotifier, handling notifications that need to go out for various events.
    /// </summary>
    public class AdminAircraftDataChangeNotifier : IAircraftDataChangeNotifier
    {
        public void NotifyAdminAircraftCloned(string szUser, Aircraft ac, MakeModel mmOld, MakeModel mmNew)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (mmOld == null)
                throw new ArgumentNullException(nameof(mmOld));
            if (mmNew == null)
                throw new ArgumentNullException(nameof(mmNew));

            Profile pf = Profile.GetUser(szUser);
            util.NotifyAdminEvent($"Aircraft {ac.DisplayTailnumber} cloned",
                $"User: {pf.DetailedName}\r\n\r\n{$"~/mvc/aircraft/edit/{ac.AircraftID}?a=1".ToAbsoluteBrandedUri()}\r\n\r\nOld Model: {mmOld.DisplayName + " " + mmOld.ICAODisplay}, (modelID: {mmOld.MakeModelID})\r\n\r\nNew Model: {mmNew.DisplayName + " " + mmNew.ICAODisplay}, (modelID: {mmNew.MakeModelID})", ProfileRoles.maskCanManageData | ProfileRoles.maskCanSupport);
        }

        public void NotifyUsersAircraftCloned(Aircraft ac, int idAircraftOriginal, MakeModel mmOld, MakeModel mmNew, string szAlternatives, IEnumerable<string> migratedUsers)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (mmOld == null)
                throw new ArgumentNullException(nameof(mmOld));
            if (mmNew == null)
                throw new ArgumentNullException(nameof(mmNew));
            migratedUsers = migratedUsers ?? Array.Empty<string>();
            AircraftStats acs = new AircraftStats(String.Empty, idAircraftOriginal);

            // Notify all users of the aircraft about the change
            foreach (string sz in acs.UserNames ?? Array.Empty<string>())
            {
                Profile pf = Profile.GetUser(sz);
                string szEmailNotification =
                    util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.EmailTemplates.AircraftTailSplit),
                    pf.UserFullName,
                    ac.TailNumber,
                    mmOld.DisplayName,
                    mmNew.DisplayName,
                    migratedUsers.Contains(sz) ? mmNew.DisplayName : mmOld.DisplayName,
                    szAlternatives), false);

                util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ModelCollisionSubjectLine, ac.TailNumber, Branding.CurrentBrand.AppName), szEmailNotification, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);
            }
        }

        public void NotifyAircraftSpecificationIgnored(string szUser, Aircraft ac, int idMatchedModel, string versionList)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));

            if (!String.IsNullOrEmpty(szUser))
            {
                Profile pf = Profile.GetUser(szUser);
                string szNotification = String.Format(CultureInfo.CurrentCulture, Branding.ReBrand(Resources.Aircraft.AircraftModelNewModelIgnored),
                    pf.UserFullName,
                    ac.TailNumber,
                    new MakeModel(ac.ModelID).DisplayName,
                    new MakeModel(idMatchedModel).DisplayName,
                    versionList);

                string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ModelCollisionSubjectLine, ac.TailNumber, Branding.CurrentBrand.AppName);
                util.NotifyUser(szSubject, util.ApplyHtmlEmailTemplate(szNotification, false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);

            }
        }

        public void NotifyModelChanged(string szUser, Aircraft ac, Aircraft acMatch)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (acMatch == null)
                throw new ArgumentNullException(nameof(acMatch));

            if (ac.ModelID == acMatch.ModelID)
                return;

            // See if there's even an issue
            // If it's not used in any flights, or there's exactly one user (the owner), no problem.
            AircraftStats acs = new AircraftStats(String.Empty, acMatch.AircraftID);
            if (acs.Flights == 0 || (acs.Users == 1 && String.Compare(szUser, acs.UserNames.ElementAt(0), StringComparison.CurrentCultureIgnoreCase) == 0))
                return;

            // Notify the admin here - model changed, I want to see it.
            MakeModel mmMatch = MakeModel.GetModel(acMatch.ModelID);
            MakeModel mmThis = MakeModel.GetModel(ac.ModelID);
            string szMakeMatch = mmMatch.DisplayName + mmMatch.ICAODisplay;
            string szMakeThis = mmThis.DisplayName + mmThis.ICAODisplay;

            string szRegLink = Aircraft.LinkForTailnumberRegistry(acMatch.TailNumber);
            string szReg = String.IsNullOrWhiteSpace(szRegLink) ? string.Empty : "\r\n" + String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.RegistrationLinkForAircraft, szRegLink) + "\r\n";

            // Admin events can merge duplicate models, so detect that
            if (String.Compare(szMakeMatch, szMakeThis, StringComparison.CurrentCultureIgnoreCase) == 0)
                return;

            if (ac == null)
                throw new ArgumentNullException(nameof(ac));

            string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.ModelCollisionSubjectLine, ac.TailNumber, Branding.CurrentBrand.AppName);

            util.NotifyAdminEvent(szSubject, util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, "User: {0}\r\n\r\n{1}\r\n\r\nMessage that was sent to other users:\r\n\r\n{2}", Profile.GetUser(szUser).DetailedName.Replace("_", "__"),
    String.Format(CultureInfo.InvariantCulture, "https://{0}{1}{2}?a=1", Branding.CurrentBrand.HostName, "~/mvc/aircraft/edit/".ToAbsolute(), ac.AircraftID),
    String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftModelChangedNotification, "(username)", ac.TailNumber, szMakeMatch, szMakeThis, szReg)), false), ProfileRoles.maskCanManageData);

            // If we're here, then there are other users - need to notify all of them of the change.
            if (!String.IsNullOrEmpty(szUser))
            {
                foreach (string szName in acs.UserNames ?? Array.Empty<string>())
                {
                    // Don't send to the person initiating the change
                    if (String.Compare(szName, szUser, StringComparison.CurrentCultureIgnoreCase) == 0)
                        continue;

                    Profile pf = Profile.GetUser(szName);
                    util.NotifyUser(szSubject, util.ApplyHtmlEmailTemplate(String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftModelChangedNotification, pf.UserFullName, ac.TailNumber, szMakeMatch, szMakeThis, szReg), false), new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);
                }
            }
        }

        public bool AircraftHasUsers(int idAircraft)
        {
            return new AircraftStats(string.Empty, idAircraft).Users > 0;
        }
    }
}