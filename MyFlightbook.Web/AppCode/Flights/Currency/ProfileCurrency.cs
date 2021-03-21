using MyFlightbook.BasicmedTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Type of medical
    /// </summary>
    [System.ComponentModel.DefaultValue(MedicalType.Other)]
    public enum MedicalType { Other, FAA1stClass, FAA2ndClass, FAA3rdClass, EASA1stClass, EASA2ndClass, EASALAPL }

    /// <summary>
    /// Encapsulates currencies that are based not on the flying that you do but on attributes of your profile - including medical/basicmed, flight reviews, and english-proficiency
    /// </summary>
    public class ProfileCurrency
    {
        protected const string szPrefKeyMedicalType = "medicalType";

        #region properties
        protected Profile CurrentUser { get; set; }

        protected bool IsKnownBirthdate { get { return CurrentUser.DateOfBirth != null && CurrentUser.DateOfBirth.HasValue; } }

        public MedicalType TypeOfMedical
        {
            get { return CurrentUser.PreferenceExists(szPrefKeyMedicalType) ? CurrentUser.GetPreferenceForKey<MedicalType>(szPrefKeyMedicalType) : MedicalType.Other; }
            set { CurrentUser.SetPreferenceForKey(szPrefKeyMedicalType, value, value == MedicalType.Other); }
        }

        public string MedicalDescription
        {
            get
            {
                switch (TypeOfMedical)
                {
                    case MedicalType.Other:
                        return String.Format(CultureInfo.CurrentCulture, Resources.Preferences.MedicalDescriptionOther, CurrentUser.MonthsToMedical, CurrentUser.UsesICAOMedical ? Resources.Preferences.MedicalDescriptionMonths : Resources.Preferences.MedicalDescriptionCalendarMonths);
                    case MedicalType.EASA1stClass:
                        return Resources.Preferences.MedicalTypeEASA1stClass;
                    case MedicalType.EASA2ndClass:
                        return Resources.Preferences.MedicalTypeEASA2ndClass;
                    case MedicalType.EASALAPL:
                        return Resources.Preferences.MedicalTypeEASALAPL;
                    case MedicalType.FAA1stClass:
                        return Resources.Preferences.MedicalTypeFAA1stClass;
                    case MedicalType.FAA2ndClass:
                        return Resources.Preferences.MedicalTypeFAA2ndClass;
                    case MedicalType.FAA3rdClass:
                        return Resources.Preferences.MedicalTypeFAA3rdClass;
                }
                return string.Empty;
            }
        }

        public static bool RequiresBirthdate(MedicalType mt)
        {
            return mt != MedicalType.Other;
        }
        #endregion

        public ProfileCurrency(Profile pf)
        {
            CurrentUser = pf;
        }

        #region BFR and Medical functions
        /// <summary>
        /// Returns a pseudo-event for the last BFR.
        /// </summary>
        public ProfileEvent LastBFREvent
        {
            get
            {
                if (CurrentUser.LastBFRInternal.CompareTo(DateTime.MinValue) == 0)
                    return null;
                else
                {
                    // BFR's used to be stored in the profile.  If so, we'll convert it to a pseudo flight property here,
                    // and attempt to de-dupe it later.
                    ProfileEvent pf = new ProfileEvent() { Date = CurrentUser.LastBFRInternal };
                    pf.PropertyType.FormatString = Resources.Profile.BFRFromProfile;
                    return pf;
                }
            }
        }

        /// <summary>
        /// internal cache of BFR events; go easy on the database!
        /// </summary>
        private ProfileEvent[] BFREvents { get; set; }

        /// <summary>
        /// Same as BFREvents except that if BFREvents is null it hits the database first.
        /// </summary>
        private ProfileEvent[] CachedBFREvents()
        {
            if (BFREvents == null)
                BFREvents = ProfileEvent.GetBFREvents(CurrentUser.UserName, LastBFREvent);
            return BFREvents;
        }

        /// <summary>
        /// Last BFR Date (uses flight properties).
        /// </summary>
        protected DateTime LastBFR()
        {
            ProfileEvent[] rgPfe = CachedBFREvents();

            if (rgPfe.Length > 0)
                return rgPfe[rgPfe.Length - 1].Date;
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// Predicted date that next BFR is due
        /// </summary>
        /// <param name="bfrLast">Date of the last BFR</param>
        /// <returns>Datetime representing the date of the next BFR, Datetime.minvalue for unknown</returns>
        public static DateTime NextBFR(DateTime bfrLast)
        {
            return bfrLast.AddCalendarMonths(24);
        }
        #endregion

        private static CurrencyStatusItem StatusForDate(DateTime dt, string szLabel, CurrencyStatusItem.CurrencyGroups rt)
        {
            if (dt.CompareTo(DateTime.MinValue) != 0)
            {
                TimeSpan ts = dt.Subtract(DateTime.Now);
                int days = (int)Math.Ceiling(ts.TotalDays);
                CurrencyState cs = (days < 0) ? CurrencyState.NotCurrent : ((ts.Days < 30) ? CurrencyState.GettingClose : CurrencyState.OK);
                return new CurrencyStatusItem(szLabel, CurrencyExaminer.StatusDisplayForDate(dt), cs, (cs == CurrencyState.GettingClose) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusClose, days) :
                                                                                   (cs == CurrencyState.NotCurrent) ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileCurrencyStatusNotCurrent, -days) : string.Empty)
                { CurrencyGroup = rt };
            }
            else
                return null;
        }

        /// <summary>
        /// Predicted date that next medical is due
        /// </summary>
        /// <returns>Datetime representing the date of the next medical, null for unknown.</returns>
        protected DateTime? NextMedical
        {
            get
            {
                if (!CurrentUser.LastMedical.HasValue() || CurrentUser.MonthsToMedical == 0)
                    return null;
                else
                    return CurrentUser.UsesICAOMedical ? CurrentUser.LastMedical.AddMonths(CurrentUser.MonthsToMedical) : CurrentUser.LastMedical.AddCalendarMonths(CurrentUser.MonthsToMedical);
            }
        }

        private static void AddEASA1stClassItems(List<CurrencyStatusItem> lst, DateTime lastMedical, bool fWas40AtExam, bool fWas60AtExam)
        {
            CurrencyStatusItem cs;
            /*
             * Class 1: (per MED.A.045 Validity, revalidation and renewal of medical certificates) - see https://www.easa.europa.eu/sites/default/files/dfu/Easy_Access_Rules_for_Medical_Requirements.pdf
             *   Under 40 - 12 months (day-for-day)
             *   Over 60 - 6 months (day-for-day)
             *   Over 40 but under 60: 12 months for certificate overall, but atp commercial operations for only 6 months.
            */
            cs = StatusForDate(lastMedical.AddMonths(6), Resources.Currency.NextMedical, CurrencyStatusItem.CurrencyGroups.Medical);
            if (fWas60AtExam)
                lst.Add(cs);
            else if (fWas40AtExam)
            {
                lst.Add(StatusForDate(lastMedical.AddMonths(6), Resources.Currency.NextMedicalEASA1stClassTransport, CurrencyStatusItem.CurrencyGroups.Medical));
                lst.Add(StatusForDate(lastMedical.AddMonths(12), Resources.Currency.NextMedicalEASA1stClassOverall, CurrencyStatusItem.CurrencyGroups.Medical));
            }
            else
                lst.Add(StatusForDate(lastMedical.AddMonths(12), Resources.Currency.NextMedicalEASA1stClassOverall, CurrencyStatusItem.CurrencyGroups.Medical));
        }

        private static void AddEASA2ndClassItems(List<CurrencyStatusItem> lst, DateTime lastMedical, bool fWas40AtExam, bool fWas50AtExam, DateTime dob)
        {
            /*
             * Class 2: (per MED.A.045 Validity, revalidation and renewal of medical certificates) - see https://www.easa.europa.eu/sites/default/files/dfu/Easy_Access_Rules_for_Medical_Requirements.pdf
             *  - Under 40: MIN(date + 60 months, 42nd birthday)
             *  - Under 50: MIN(date + 24 months, 51st birthday)
             *  - Otherwise, date + 12 months.
             */
            if (!fWas40AtExam)
                lst.Add(StatusForDate(lastMedical.AddMonths(60).EarlierDate(dob.AddYears(42)), Resources.Currency.NextMedicalEASA2ndClass, CurrencyStatusItem.CurrencyGroups.Medical));
            else if (!fWas50AtExam)
                lst.Add(StatusForDate(lastMedical.AddMonths(24).EarlierDate(dob.AddYears(51)), Resources.Currency.NextMedicalEASA2ndClass, CurrencyStatusItem.CurrencyGroups.Medical));
            else
                lst.Add(StatusForDate(lastMedical.AddMonths(12), Resources.Currency.NextMedicalEASA2ndClass, CurrencyStatusItem.CurrencyGroups.Medical));
        }

        private static void AddEASALAPLItems(List<CurrencyStatusItem> lst, DateTime lastMedical, bool fWas40AtExam, DateTime dob)
        {           
            /*
            * LAPL: (per MED.A.045 Validity, revalidation and renewal of medical certificates) - see https://www.easa.europa.eu/sites/default/files/dfu/Easy_Access_Rules_for_Medical_Requirements.pdf
            *  - Under 40: MIN(date + 60 months, 42nd birthday)
            *  - Otherwise, date + 24 months.
            */
            if (!fWas40AtExam)
                lst.Add(StatusForDate(lastMedical.AddMonths(60).EarlierDate(dob.AddYears(42)), Resources.Currency.NextMedicalEASALAPL, CurrencyStatusItem.CurrencyGroups.Medical));
            else
                lst.Add(StatusForDate(lastMedical.AddMonths(24), Resources.Currency.NextMedicalEASALAPL, CurrencyStatusItem.CurrencyGroups.Medical));
        }

        private static void AddFAA1stClassItems(List<CurrencyStatusItem> lst, DateTime lastMedical, bool fWas40AtExam)
        {
            CurrencyStatusItem cs = StatusForDate(lastMedical.AddCalendarMonths(fWas40AtExam ? 6 : 12), Resources.Currency.NextMedical1stClass, CurrencyStatusItem.CurrencyGroups.Medical);
            lst.Add(cs);
            if (cs.Status == CurrencyState.NotCurrent)
            {
                if (fWas40AtExam)   // may still have some non-ATP commercial time left
                    lst.Add(StatusForDate(lastMedical.AddCalendarMonths(12), Resources.Currency.NextMedical1stClassCommercial, CurrencyStatusItem.CurrencyGroups.Medical));
                lst.Add(StatusForDate(lastMedical.AddCalendarMonths(fWas40AtExam ? 24 : 60), Resources.Currency.NextMedical3rdClassPrivs, CurrencyStatusItem.CurrencyGroups.Medical));
            }
        }

        private static void AddFAA2ndClassItems(List<CurrencyStatusItem> lst, DateTime lastMedical, bool fWas40AtExam)
        {
            CurrencyStatusItem cs = StatusForDate(lastMedical.AddCalendarMonths(12), Resources.Currency.NextMedical2ndClass, CurrencyStatusItem.CurrencyGroups.Medical);
            lst.Add(cs);
            if (cs.Status == CurrencyState.NotCurrent)
                lst.Add(StatusForDate(lastMedical.AddCalendarMonths(fWas40AtExam ? 24 : 60), Resources.Currency.NextMedical3rdClassPrivs, CurrencyStatusItem.CurrencyGroups.Medical));
        }

        /// <summary>
        /// Returns an enumerable of medical status based on https://www.law.cornell.edu/cfr/text/14/61.23 for FAA medicals, or else expiration and type
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CurrencyStatusItem> MedicalStatus(DateTime lastMedical, int monthsToMedical, MedicalType mt, DateTime? dob, bool fUsesICAOMedical)
        {
            // if no last medical, then this is easy: we know nothing.
            if (!lastMedical.HasValue() || monthsToMedical == 0)
                return Array.Empty<CurrencyStatusItem>();

            if (RequiresBirthdate(mt) && (dob == null || !dob.HasValue))
                return new CurrencyStatusItem[] { new CurrencyStatusItem(Resources.Currency.NextMedical, Resources.Currency.NextMedicalRequiresBOD, CurrencyState.NotCurrent) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Medical } };
            
            bool fWas40AtExam = dob != null && dob.Value.AddYears(40).CompareTo(lastMedical) < 0;
            bool fWas60AtExam = dob != null && dob.Value.AddYears(60).CompareTo(lastMedical) < 0;
            bool fWas50AtExam = dob != null && dob.Value.AddYears(50).CompareTo(lastMedical) < 0;

            List<CurrencyStatusItem> lst = new List<CurrencyStatusItem>();

            switch (mt)
            {
                case MedicalType.Other:
                    lst.Add(StatusForDate(fUsesICAOMedical ?
                            lastMedical.AddMonths(monthsToMedical) :
                            lastMedical.AddCalendarMonths(monthsToMedical), Resources.Currency.NextMedical, CurrencyStatusItem.CurrencyGroups.Medical));
                    break;
                case MedicalType.EASA1stClass:
                    AddEASA1stClassItems(lst, lastMedical, fWas40AtExam, fWas60AtExam);
                    break;
                case MedicalType.EASA2ndClass:
                    AddEASA2ndClassItems(lst, lastMedical, fWas40AtExam, fWas50AtExam, dob.Value);
                    break;
                case MedicalType.EASALAPL:
                    AddEASALAPLItems(lst, lastMedical, fWas40AtExam, dob.Value);
                    break;
                case MedicalType.FAA1stClass:
                    AddFAA1stClassItems(lst, lastMedical, fWas40AtExam);
                    break;
                case MedicalType.FAA2ndClass:
                    AddFAA2ndClassItems(lst, lastMedical, fWas40AtExam);
                    break;
                case MedicalType.FAA3rdClass:
                    lst.Add(StatusForDate(lastMedical.AddCalendarMonths(fWas40AtExam ? 24 : 60), Resources.Currency.NextMedical, CurrencyStatusItem.CurrencyGroups.Medical));
                    break;
            }
            return lst;
        }

        public IEnumerable<CurrencyStatusItem> MedicalStatus()
        {
            return MedicalStatus(CurrentUser.LastMedical, CurrentUser.MonthsToMedical, TypeOfMedical, CurrentUser.DateOfBirth, CurrentUser.UsesICAOMedical);
        }

        public IEnumerable<CurrencyStatusItem> CurrencyForUser()
        {
            List<CurrencyStatusItem> rgCS = new List<CurrencyStatusItem>();

            IEnumerable<CurrencyStatusItem> lstMedicals = MedicalStatus();
            CurrencyStatusItem csBasicMed = new BasicMed(CurrentUser.UserName).Status;

            /* Scenarios for combining regular medical and basicmed.
             * 
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |\_____    Medical   |                    |                    |                    |
             *   |      \______       |   Never Current    |     Expired        |      Valid         |
             *   | BasicMed    \_____ |                    |                    |                    |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |  Never Current     |    Show Nothing    |   Show Expired     |    Show Valid      |
             *   |                    |                    |     Medical        |     Medical        |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |      Expired       |        N/A         |   Show Expired     | Show Valid Medical |
             *   |                    |                    | Medical, BasicMed  | Suppress BasicMed  |
             *   +--------------------+--------------------+--------------------+--------------------+
             *   |                    |                    |    Show Valid      |    Show Valid      |
             *   |       Valid        |        N/A         |   BasicMed, note   |   Medical, show    |
             *   |                    |                    |    BasicMed only   |   BasicMed too     |
             *   +--------------------+--------------------+--------------------+--------------------+
             * 
             * 
             * a) Medical has never been valid -> by definition, neither has basic med: NO STATUS
             * b) Medical expired
             *      1) BasicMed never valid -> show only expired medical
             *      2) BasicMed expired -> show expired medical, expired basicmed (so you can tell which is easier to renew)
             *      3) BasicMed is valid -> Show valid BasicMed, that you are ONLY basicmed
             * c) Medical valid:
             *      1) BasicMed never valid -> Show only valid medical
             *      2) BasicMed expired -> show valid medical don't bother showing the expired basicmed since it's kinda pointless
             *      3) BasicMed is still valid -> show both (hey, seeing green is good)
            */

            if (lstMedicals.Any()) // (a) above - i.e., left column of chart; nothing to add if never had valid medical
            {
                if (csBasicMed == null) // b.1 and c.1 above, i.e., top row of chart - Just show medical status
                    rgCS.AddRange(lstMedicals);
                else
                {
                    CurrencyState state = CurrencyState.NotCurrent;

                    foreach (CurrencyStatusItem cs in lstMedicals)
                        state = (CurrencyState)Math.Max((int)state, (int)cs.Status);
                    switch (state)
                    {
                        case CurrencyState.OK:
                        case CurrencyState.GettingClose:
                            // Medical valid - c.2 and c.3 above - show medical, show basic med if valid
                            rgCS.AddRange(lstMedicals);
                            if (csBasicMed.Status != CurrencyState.NotCurrent)
                                rgCS.Add(csBasicMed);
                            break;
                        case CurrencyState.NotCurrent:
                            // Medical is not current but basicmed has been - always show basicmed, show medical only if basicmed is also expired
                            rgCS.Add(csBasicMed);
                            if (csBasicMed.Status == CurrencyState.NotCurrent)
                                rgCS.AddRange(lstMedicals);
                            break;
                    }
                }
            }

            BFREvents = null; // clear the cache - but this will let the next three calls (LastBFR/LastBFRR22/LastBFRR44) hit the DB only once.
            DateTime dtBfrLast = LastBFR();
            if (dtBfrLast.HasValue())
                rgCS.Add(StatusForDate(NextBFR(dtBfrLast), Resources.Currency.NextFlightReview, CurrencyStatusItem.CurrencyGroups.FlightReview));

            BFREvents = null; // clear the cache again (memory).

            if (CurrentUser.CertificateExpiration.HasValue())
                rgCS.Add(StatusForDate(CurrentUser.CertificateExpiration, Resources.Currency.CertificateExpiration, CurrencyStatusItem.CurrencyGroups.Certificates));

            if (CurrentUser.EnglishProficiencyExpiration.HasValue())
                rgCS.Add(StatusForDate(CurrentUser.EnglishProficiencyExpiration, Resources.Currency.NextLanguageProficiency, CurrencyStatusItem.CurrencyGroups.Certificates));

            return rgCS;
        }
    }
}