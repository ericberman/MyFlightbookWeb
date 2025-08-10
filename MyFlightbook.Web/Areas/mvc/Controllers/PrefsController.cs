using MyFlightbook.BasicmedTools;
using MyFlightbook.Currency;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using MyFlightbook.Templates;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrefsController : AdminControllerBase
    {
        #region Web Services
        #region autofill
        [HttpPost]
        [Authorize]
        public ActionResult SetAutofillOptions(AutoFillOptions afo)
        {
            return SafeOp(() =>
            {
                if (afo == null)
                    throw new ArgumentNullException(nameof(afo));
                if (!(new HashSet<int>(AutoFillOptions.DefaultSpeeds).Contains((int) afo.TakeOffSpeed)))
                    afo.TakeOffSpeed = AutoFillOptions.DefaultTakeoffSpeed;
                afo.LandingSpeed = AutoFillOptions.BestLandingSpeedForTakeoffSpeed((int) afo.TakeOffSpeed);
                afo.IgnoreErrors = true;
                afo.SaveForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }
        #endregion

        #region Properties and Templates
        [HttpPost]
        [Authorize]
        public ActionResult EditPropBlockList(int id, bool fAllow)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (fAllow)
                {
                    if (pf.BlocklistedProperties.Contains(id))
                    {
                        pf.BlocklistedProperties.RemoveAll(idPropType => id == idPropType);
                        pf.FCommit();
                        // refresh the cache
                        CustomPropertyType.GetCustomPropertyTypes(User.Identity.Name, true);
                    }
                }
                else if (!pf.BlocklistedProperties.Contains(id))
                {
                    pf.BlocklistedProperties.Add(id);
                    pf.FCommit();
                    // refresh the cache
                    CustomPropertyType.GetCustomPropertyTypes(User.Identity.Name, true);
                }
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult PropTemplateEditor(int idTemplate, string containerID, bool fCopy)
        {
            return SafeOp(() =>
            {
                ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                ViewBag.idTemplate = idTemplate;
                ViewBag.containerID = containerID;
                ViewBag.fCopy = fCopy;
                return PartialView("_prefEditPropTemplate");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult CommitPropTemplate()
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate()
                {
                    ID = Convert.ToInt32(Request["propTemplateID"], CultureInfo.InvariantCulture),
                    Name = Request["propTemplateName"],
                    Group = (PropertyTemplateGroup)Enum.Parse(typeof(PropertyTemplateGroup), Request["propTemplateCategory"]),
                    Owner = User.Identity.Name,
                    OriginalOwner = Request["propTemplateOriginalOwner"],
                    Description = Request["propTemplateDescription"],
                    IsDefault = Convert.ToBoolean(Request["propTemplateDefault"], CultureInfo.InvariantCulture),
                    IsPublic = Convert.ToBoolean(Request["propTemplatePublic"], CultureInfo.InvariantCulture)
                };
                IEnumerable<int> propIDs = Request["propTemplateIncludedIDs"].ToInts();
                foreach (int propTypeID in propIDs)
                    pt.PropertyTypes.Add(propTypeID);
                pt.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddPublicTemplate(int idTemplate)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate);
                IEnumerable<PropertyTemplate> currentTemplates = UserPropertyTemplate.TemplatesForUser(User.Identity.Name);
                PersistablePropertyTemplate pptNew = pt.CopyPublicTemplate(User.Identity.Name);
                // Override the existing one if it exists with the same name.
                PropertyTemplate ptMatch = currentTemplates.FirstOrDefault(ptUser => ptUser.Group == pt.Group && ptUser.Name.CompareCurrentCultureIgnoreCase(pt.Name) == 0);
                if (ptMatch != null)
                    pptNew.ID = ptMatch.ID;
                pptNew.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetTemplateFlags(int idTemplate, bool fPublic, bool fDefault)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate)
                {
                    IsDefault = fDefault,
                    IsPublic = fPublic
                };
                pt.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePropTemplate(int idTemplate)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate);
                pt.DeleteForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }
        #endregion

        #region Deadlines
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDeadline(int idDeadline, int idAircraft)
        {
            return SafeOp(() =>
            {
                DeadlineCurrency dc = DeadlineCurrency.DeadlineForUser(User.Identity.Name, idDeadline, idAircraft) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Deadline {0} doesn't exist for user {1}", idDeadline, User.Identity.Name));
                dc.FDelete();
                if (dc.IsSharedAircraftDeadline)
                    new MaintenanceLog() { AircraftID = idAircraft, ChangeDate = DateTime.Now, User = User.Identity.Name, Description = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineDeleted, dc.DisplayName), Comment = string.Empty }.FAddToLog();

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDeadline(int idDeadline, int idAircraft)
        {
            return SafeOp(() =>
            {
                DeadlineCurrency dc = DeadlineCurrency.DeadlineForUser(User.Identity.Name, idDeadline, idAircraft) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Deadline {0} doesn't exist for user {1}", idDeadline, User.Identity.Name));
                DeadlineCurrency dcOriginal = dc.IsSharedAircraftDeadline ? DeadlineCurrency.DeadlineForUser(User.Identity.Name, idDeadline, idAircraft) : null; 

                if (dc.AircraftHours > 0)
                    dc.AircraftHours = dc.NewHoursBasedOnHours(decimal.Parse(Request["deadlineNewHours"], CultureInfo.CurrentCulture));
                else
                    dc.Expiration = dc.NewDueDateBasedOnDate(DateTime.Parse(Request["deadlineNewDate"], CultureInfo.CurrentCulture));
                if (!dc.IsValid() || !dc.FCommit())
                    throw new InvalidOperationException(dc.ErrorString);

                if (dc.IsSharedAircraftDeadline)
                    new MaintenanceLog() { AircraftID = dc.AircraftID, ChangeDate = DateTime.Now, User = User.Identity.Name, Description = dc.DifferenceDescription(dcOriginal), Comment = string.Empty }.FAddToLog();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeadlineList(int idAircraft = Aircraft.idAircraftUnknown)
        {
            return SafeOp(() => {
                ViewBag.aircraftID = idAircraft;
                return PartialView("_prefDeadlineList"); 
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddAircraftOilDeadline(int idAircraft, int interval, int curValue, string postEdit)
        {
            return SafeOp(() =>
            {
                DeadlineCurrency dc = new DeadlineCurrency(null, Resources.Aircraft.DeadlineOilChangeTitle, DateTime.MinValue, interval, DeadlineCurrency.RegenUnit.Hours, idAircraft, curValue + interval);
                if (dc.FCommit())
                {
                    MaintenanceLog ml = new MaintenanceLog() { AircraftID = idAircraft, ChangeDate = DateTime.Now, User = User.Identity.Name, Description = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineCreated, Resources.Aircraft.DeadlineOilChangeTitle), Comment = string.Empty };
                    ml.FAddToLog();

                    return AircraftDeadlineSection(idAircraft, postEdit);
                }
                else
                    throw new InvalidOperationException(dc.ErrorString);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeadlineEditor(int idDeadline, bool fShared = false, int idAircraft = Aircraft.idAircraftUnknown)
        {
            ViewBag.idDeadline = idDeadline;
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.fShared = fShared;
            ViewBag.idAircraft = idAircraft;
            return PartialView("_prefDeadlineEdit");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitDeadline()
        {
            return SafeOp(() =>
            {
                bool fUseHours = !String.IsNullOrEmpty(Request["deadlineUsesHours"]);
                DeadlineCurrency.RegenUnit ru = Request["deadlineRegenType"].CompareCurrentCultureIgnoreCase("None") == 0 ?
                    DeadlineCurrency.RegenUnit.None : (fUseHours ? DeadlineCurrency.RegenUnit.Hours : (DeadlineCurrency.RegenUnit)Enum.Parse(typeof(DeadlineCurrency.RegenUnit), Request["deadlineRegenRange"]));
                bool fCreateShared = bool.Parse(Request["deadlineShared"]);
                DeadlineCurrency dc = new DeadlineCurrency()
                {
                    ID = Convert.ToInt32(Request["idDeadline"], CultureInfo.InvariantCulture),
                    Username = fCreateShared ? null : User.Identity.Name,
                    Name = Request["deadlineName"],
                    Expiration = fUseHours ? DateTime.MinValue : Convert.ToDateTime(Request["deadlineNewDate"], CultureInfo.CurrentCulture),
                    AircraftHours = fUseHours ? Convert.ToDecimal(Request["deadlineNewHours"], CultureInfo.InvariantCulture) : 0,
                    AircraftID = int.TryParse(Request["deadlineAircraftID"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int acID) ? acID : Aircraft.idAircraftUnknown,
                    RegenType = ru,
                    RegenSpan = ru == DeadlineCurrency.RegenUnit.None ? 0 : Convert.ToInt32(Request["deadlineRegenInterval"], CultureInfo.InvariantCulture),
                };
                if (!dc.IsValid() || !dc.FCommit())
                    throw new InvalidOperationException(dc.ErrorString);

                if (dc.IsSharedAircraftDeadline)
                    new MaintenanceLog() { AircraftID = dc.AircraftID, ChangeDate = DateTime.Now, User = User.Identity.Name, Description = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DeadlineCreated, dc.DisplayName), Comment = string.Empty }.FAddToLog();

                return new EmptyResult();
            });
        }
        #endregion

        #region Custom Currency
        [HttpPost]
        [Authorize]
        public ActionResult CustCurrencyEditor(int idCustCurrency)
        {
            return SafeOp(() =>
            {
                ViewBag.idCustCurrency = idCustCurrency;
                ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                return PartialView("_prefCustCurrencyEdit");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult CustCurrencyList()
        {
            return SafeOp(() =>
            {
                return PartialView("_prefCustCurrencyList");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCustCurrency(int idCustCurrency)
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = CustomCurrency.CustomCurrencyForUser(User.Identity.Name, idCustCurrency) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "No such currency {0} for use {1}", idCustCurrency, User.Identity.Name));
                cc.FDelete();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitCustCurrency()
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = new CustomCurrency
                {
                    ID = Convert.ToInt32(Request["custCurrencyID"], CultureInfo.InvariantCulture),
                    UserName = User.Identity.Name,
                    DisplayName = Request["custCurrencyName"],
                    RequiredEvents = Convert.ToDecimal(Request["custCurrencyMinEvents"], CultureInfo.InvariantCulture),
                    EventType = (CustomCurrency.CustomCurrencyEventType)Enum.Parse(typeof(CustomCurrency.CustomCurrencyEventType), Request["custCurrencyEventType"]),
                    ExpirationSpan = Convert.ToInt32(Request["custCurrencyTimeFrame"], CultureInfo.InvariantCulture),
                    CurrencyTimespanType = (TimespanType)Enum.Parse(typeof(TimespanType), Request["custCurrencyMonthsDays"]),
                    ModelsRestriction = Request["custCurrencyModels"].ToInts(),
                    AircraftRestriction = Request["custCurrencyAircraft"].ToInts(),
                    CategoryRestriction = Request["custCurrencyCategory"],
                    AirportRestriction = Request["custCurrencyAirport"],
                    TextRestriction = Request["custCurrencyText"],
                    PropertyRestriction = Request["custCurrencyProps"].ToInts()
                };
                if (Enum.TryParse(Request["custCurrencyLimitType"], out CustomCurrency.LimitType lt))
                    cc.CurrencyLimitType = lt;
                cc.CatClassRestriction = Enum.TryParse(Request["custCurrencyCatClass"], out CategoryClass.CatClassID ccid) ? ccid : 0;

                if (!cc.FCommit())
                    throw new InvalidOperationException(cc.ErrorString);

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetCustCurrencyActive(int idCustCurrency, bool fActive)
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = CustomCurrency.CustomCurrencyForUser(User.Identity.Name, idCustCurrency) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "No such currency {0} for use {1}", idCustCurrency, User.Identity.Name));
                cc.IsActive = fActive;
                if (!cc.FCommit())
                    throw new InvalidOperationException(cc.ErrorString);
                return new EmptyResult();
            });
        }
        #endregion

        #region Share keys
        [HttpPost]
        [Authorize]
        public ActionResult UpdateShareKey(string idShareKey, bool fFlights, bool fTotals, bool fCurrency, bool fAchievements, bool fAirports, string queryName)
        {
            return SafeOp(() =>
            {
                ShareKey sk = ShareKey.ShareKeyWithID(idShareKey) ?? throw new InvalidOperationException("Unknown key: " + idShareKey);
                if (sk.Username.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("User " + User.Identity.Name + " does not own this share link!");

                sk.CanViewFlights = fFlights;
                sk.CanViewTotals = fTotals;
                sk.CanViewCurrency = fCurrency;
                sk.CanViewAchievements = fAchievements;
                sk.CanViewVisitedAirports = fAirports;
                sk.QueryName = String.IsNullOrEmpty(queryName) ? null : queryName;
                sk.FCommit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteShareKey(string id)
        {
            return SafeOp(() =>
            {
                ShareKey sk = ShareKey.ShareKeyWithID(id) ?? throw new InvalidOperationException("Unknown key: " + id);
                if (sk.Username.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();
                sk.FDelete();
                return new EmptyResult();
            });
        }
        #endregion

        #region Account Options
        #region name and email
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateNameEmail(string accountFirstName, string accountLastName, string accountEmail, string accountAddress, string accountDOB, string accountGreeting, string accountMobilePhone)
        {
            return SafeOp(() =>
            {
                if (accountFirstName == null)
                    throw new ArgumentNullException(nameof(accountFirstName));
                if (accountLastName == null)
                    throw new ArgumentNullException(nameof(accountLastName));
                if (accountEmail == null)
                    throw new ArgumentNullException(nameof(accountEmail));
                if (accountAddress == null)
                    throw new ArgumentNullException(nameof(accountAddress));
                if (accountDOB == null)
                    throw new ArgumentNullException(nameof(accountDOB));
                if (accountGreeting == null)
                    throw new ArgumentNullException(nameof(accountGreeting));
                if (accountMobilePhone == null)
                    throw new ArgumentNullException(nameof(accountMobilePhone));

                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);

                if (!pf.CheckCanUseEmail(accountEmail))
                    throw new InvalidOperationException(Resources.Profile.errEmailInUse2);
                ViewBag.requestedPane = "account";
                ViewBag.pf = pf;
                pf.ChangeNameAndEmail(accountFirstName.Trim(), accountLastName.Trim(), accountEmail.Trim(), accountAddress.Trim());
                pf.DateOfBirth = DateTime.TryParse(accountDOB, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dob) ? dob : (DateTime?)null;
                string preferredGreeting = accountGreeting.Trim();
                pf.PreferredGreeting = preferredGreeting.CompareCurrentCultureIgnoreCase(pf.FirstName) == 0 ? string.Empty : preferredGreeting;
                pf.MobilePhone = accountMobilePhone.Trim();
                pf.FCommit();
                return Content(Resources.Profile.accountPersonalInfoSuccess);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SendVerificationEmail(string email)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.SendVerificationEmail(email, "~/mvc/prefs/account".ToAbsoluteURL(Request).ToString() + "?ve={0}");
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetHeadShot()
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);

                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                Stream s = Request.Files[0].InputStream;
                byte[] rgb = s == null ? Array.Empty<byte>() : MFBImageInfo.ScaledImage(s, 90, 90);
                if (rgb != null && rgb.Length > 0)
                {
                    if (rgb.Length > 65000)
                        throw new InvalidOperationException(Resources.Preferences.AccountHeadshotTooBig);
                    pf.HeadShot = rgb;
                    pf.FCommit();
                }
                return Content(pf.HeadShotHRef.ToAbsolute());
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteHeadShot()
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.HeadShot = null;
                pf.FCommit();
                return Content(pf.HeadShotHRef);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddAlias(string accountAlias)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.SendVerificationEmail(accountAlias, "~/mvc/prefs/account".ToAbsoluteURL(Request).ToString() + "?ve={0}");
                return Content(Resources.Profile.accountVerifyEmailSent);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAlias(string accountAliasToDelete)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.DeleteVerifiedEmail(accountAliasToDelete);
                return new EmptyResult();
            });
        }
        #endregion

        #region Password and Q&A
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePassword(string accountCurPass, string accountNewPass, string accountNewPass2)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                if (accountNewPass.CompareCurrentCulture(accountNewPass2) != 0) // should never happen - validation should have caught this.
                    throw new MyFlightbookException(Resources.Profile.errPasswordsDontMatch);
                pf.ChangePassword(accountCurPass, accountNewPass);
                return Content(Resources.Preferences.AccountPasswordSuccess);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQA(string accountQACurrentPass, string accountQAQuestionCustom, string accountQAAnswer)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.ChangeQAndA(accountQACurrentPass, accountQAQuestionCustom, accountQAAnswer);
                return Content(Resources.Preferences.AccountQAChangeSuccess);
            });
        }
        #endregion

        #region two-factor Auth
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Disable2fa()
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                pf.SetPreferenceForKey(MFBConstants.keyTFASettings, null, true);
                return Content(Resources.Preferences.Account2FADisabled);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Enable2fa(string seed, string verification)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name, true);
                SetUp2FA(pf, seed, verification);
                return Content(Resources.Preferences.Account2FAEnabled);
            });
        }
        #endregion

        #region Big Red buttons
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUnusedAircraft()
        {
            return SafeOp(() =>
            {
                int i = ProfileAdmin.DeleteUnusedAircraftForUser(User.Identity.Name);
                return Content(String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileBulkDeleteAircraftDeleted, i));
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFlights()
        {
            return SafeOp(() =>
            {
                ProfileAdmin.DeleteFlightsForUser(User.Identity.Name);
                return Content(Resources.Profile.ProfileDeleteFlightsCompleted);
            });
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CloseAccount()
        {
            return SafeOp(() =>
            {
                ProfileAdmin.DeleteEntireUser(User.Identity.Name);
                return Content("~".ToAbsolute());
            });
        }
        #endregion
        #endregion

        #region Pilot Info Options
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePilotCertificates(string pilotInfoCertificate, string pilotInfoCertificateCFI, string pilotInfoCFIExp, string pilotInfoEnglishProficiency)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.License = pilotInfoCertificate;
                pf.Certificate = pilotInfoCertificateCFI.LimitTo(30);
                pf.CertificateExpiration = DateTime.TryParse(pilotInfoCFIExp, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtCFIExp) ? dtCFIExp : DateTime.MinValue;
                pf.EnglishProficiencyExpiration = DateTime.TryParse(pilotInfoEnglishProficiency, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtEnglish) ? dtEnglish : DateTime.MinValue;
                pf.FCommit();
                return Content(Resources.Profile.ProfilePilotInfoCertificatesUpdated);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePilotMedical(string pilotInfoMedicalDate, int pilotInfoMedicalDuration, MedicalType pilotInfoMedicalType, string pilotInfoDateOfBirth, string pilotInfoMedicalNotes, bool pilotInfoUseICAOMedical)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);

                DateTime dtMedical = DateTime.TryParse(pilotInfoMedicalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtmed) ? dtmed : pf.LastMedical;
                DateTime dob = DateTime.TryParse(pilotInfoDateOfBirth, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtdob) ? dtdob : pf.DateOfBirth ?? DateTime.MinValue;

                pf.LastMedical = dtMedical;
                pf.MonthsToMedical = pilotInfoMedicalDuration;
                pf.UsesICAOMedical = pilotInfoUseICAOMedical;
                // Type of medical, notes, and date of birth are all set synchronously and will commit.
                ProfileCurrency _ = new ProfileCurrency(pf) { TypeOfMedical = pilotInfoMedicalType };
                pf.SetPreferenceForKey(MFBConstants.keyMedicalNotes, pilotInfoMedicalNotes, String.IsNullOrWhiteSpace(pilotInfoMedicalNotes));
                pf.DateOfBirth = dob;
                pf.FCommit();   // see issue #1457 - may not have committed if typeofmedical is "Other" or notes are empty...

                return Content(Resources.Preferences.PilotInfoMedicalUpdated);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult MedicalExpirationDates(string pilotInfoMedicalDate, int pilotInfoMedicalDuration, MedicalType pilotInfoMedicalType, string pilotInfoDateOfBirth, string pilotInfoMedicalNotes, bool pilotInfoUseICAOMedical)
        {
            return SafeOp(() =>
            {
                DateTime dtMedical = DateTime.TryParse(pilotInfoMedicalDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtmed) ? dtmed : DateTime.MinValue;
                DateTime dob = DateTime.TryParse(pilotInfoDateOfBirth, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtdob) ? dtdob : DateTime.MinValue;
                return NextMedicalForConditions(dtMedical, pilotInfoMedicalDuration, pilotInfoMedicalType, dob, pilotInfoUseICAOMedical);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddBasicMedEvent(string basicMedDate, BasicMedEvent.BasicMedEventType basicMedType, string basicMedDesc)
        {
            return SafeOp(() =>
            {
                BasicMedEvent bme = new BasicMedEvent(basicMedType, User.Identity.Name)
                {
                    EventDate = DateTime.TryParse(basicMedDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue,
                    Description = basicMedDesc
                };
                if (!bme.IsValid())
                    throw new InvalidOperationException(bme.LastError);

                bme.Commit();
                // process pending images, if this was a new pending image
                foreach (MFBPendingImage pendingImage in MFBPendingImage.PendingImagesInSession(Session))
                {
                    if (pendingImage.ImageType == MFBImageInfoBase.ImageFileType.JPEG || pendingImage.ImageType == MFBImageInfoBase.ImageFileType.PDF)
                    {
                        pendingImage.Commit(MFBImageInfoBase.ImageClass.BasicMed, bme.ID.ToString(CultureInfo.InvariantCulture));
                        pendingImage.DeleteImage();
                    }
                }

                return Content("~/mvc/Prefs/pilotinfo?pane=basicmed".ToAbsolute());
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult BasicMedEventList()
        {
            return SafeOp(() =>
            {
                return PartialView("_pilotInfoBasicMedEvents");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBasicMedEvent(int idBasicMed)
        {
            return SafeOp(() =>
            {
                BasicMedEvent bme = BasicMedEvent.EventsForUser(User.Identity.Name).FirstOrDefault<BasicMedEvent>(bme2 => bme2.ID == idBasicMed) ?? throw new UnauthorizedAccessException();
                bme.Delete();
                return PartialView("_pilotInfoBasicMedEvents");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult UploadBasicMedImages(int szKey)
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                MFBPostedFile pf = new MFBPostedFile(Request.Files[0]);
                string szID = String.Format(CultureInfo.InvariantCulture, "{0}-pendingImage-{1}-{2}", MFBImageInfoBase.ImageClass.BasicMed.ToString(), (pf.FileName ?? string.Empty).Replace(".", "_"), pf.GetHashCode());
                MFBPendingImage pi = new MFBPendingImage(pf, szID);

                switch (MFBImageInfo.ImageTypeFromFile(pf))
                {
                    default:
                        return new EmptyResult();
                    case MFBImageInfoBase.ImageFileType.JPEG:
                    case MFBImageInfoBase.ImageFileType.PDF:
                        break;
                }

                if (szKey > 0)
                    pi?.Commit(MFBImageInfoBase.ImageClass.BasicMed, szKey.ToString(CultureInfo.InvariantCulture));
                else if (pi?.IsValid ?? false)
                    Session[szID] = pi;

                BasicMedEvent.ClearCache(User.Identity.Name);
                return Content(pi.URLThumbnail.ToAbsolute());
            });
        }
        #endregion
        #endregion

        #region child actions
        #region Autofill options
        [ChildActionOnly]
        public ActionResult AutoFillOptionsEditor(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            ViewBag.afo = AutoFillOptions.DefaultOptionsForUser(szUser);
            return PartialView("_autofillOptions");
        }
        #endregion

        #region Basicmed and medical
        [ChildActionOnly]
        public ActionResult NextMedicalForConditions(DateTime dtMedical, int monthsToMedical, MedicalType mt, DateTime? dob, bool fUseICAOMedical)
        {
            ViewBag.rgcs = ProfileCurrency.MedicalStatus(dtMedical, monthsToMedical, mt, dob, fUseICAOMedical);
            return PartialView("_pilotInfoNextMedical");
        }
        #endregion

        #region deadlines
        [ChildActionOnly]
        public ActionResult AircraftDeadlineSection(int idAircraft, string postEdit = null)
        {
            ViewBag.aircraftID = idAircraft;
            ViewBag.fShared = true;
            ViewBag.postEdit = postEdit;
            return PartialView("_prefDeadline");
        }
        #endregion
        #endregion

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShareKey()
        {
            ShareKey sk = new ShareKey(User.Identity.Name)
            {
                Name = Request["prefShareLinkName"],
                CanViewFlights = Request["prefShareLinkFlights"] != null,
                CanViewTotals = Request["prefShareLinkTotals"] != null,
                CanViewCurrency = Request["prefShareLinkCurrency"] != null,
                CanViewAchievements = Request["prefShareLinkAchievements"] != null,
                CanViewVisitedAirports = Request["prefShareLinkAirports"] != null,
                QueryName = Request["prefShareLinkAssociatedQuery"],
            };
            try
            {
                sk.FCommit();
                return RedirectToAction("Index", new { pane = "social" });
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return RedirectToAction("Index", new { pane = "social", shareKeyErr = ex.Message });
            }
        }

        // GET: mvc/Prefs
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            return View("mainPrefs");
        }

        [Authorize]
        public ActionResult BrowseTemplates()
        {
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            return View("browseTemplates");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Account")]
        public ActionResult AccountWithTFA(string tfaCode)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.pf = pf;
            if (ViewBag.collectTFA = !Check2FA(pf, tfaCode))
                ViewBag.tfaErr = Resources.Profile.TFACodeFailed;
            return View("mainAccount");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Account()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.pf = pf;
            ViewBag.collectTFA = !Check2FA(pf, string.Empty);
            return View("mainAccount");
        }

        [Authorize]
        public ActionResult PilotInfo()
        {
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            return View("mainPilotInfo");
        }
    }
}