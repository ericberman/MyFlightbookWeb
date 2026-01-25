using MyFlightbook.Image;
using MyFlightbook.Lint;
using MyFlightbook.Telemetry;
using MyFlightbook.Templates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class FlightEditController : FlightControllerBase
    {
        #region Web Services
        #region Commit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                // Could already have an error just from data entry - don't bother trying to commit just yet.
                if (!String.IsNullOrEmpty(le.ErrorString))
                    throw new InvalidOperationException(le.ErrorString);

                if (!CommitFlight(le) || !String.IsNullOrEmpty(le.ErrorString))
                    throw new InvalidOperationException(le.ErrorString);

                // Issue #1458: if this is a flight being added by an instructor into a student's logbook, then implicitly request a signature.
                // This is both good practice (right?) and it allows the instructor to edit the flight, in case there was a mistake.
                if (User.Identity.Name.CompareCurrentCultureIgnoreCase(le.User) != 0)
                        le.RequestSignature(User.Identity.Name, MyFlightbook.Profile.GetUser(User.Identity.Name).Email, true);

                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitFlightAsPending()
        {
            return SafeOp(() =>
            {
                PendingFlight pf = new PendingFlight(LogbookEntryFromForm());

                Aircraft ac = new Aircraft(pf.AircraftID);
                pf.TailNumDisplay = ac.DisplayTailnumber;
                pf.ModelDisplay = ac.ModelDescription;
                pf.Commit();
                return new EmptyResult();
            });
        }
        #endregion

        #region Check flights
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                // See if there are any actual errors, stick those at the top of the list.
                le.IsValid(); // will populate ErrorString.
                if (!le.IsNewFlight)
                    le.CFISignatureState = new LogbookEntry(le.FlightID, le.User).CFISignatureState;
                IEnumerable<FlightWithIssues> rgf = new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, le.User, FlightLint.DefaultOptionsForLocale);
                return FlightIssues(rgf.FirstOrDefault()?.Issues);
            });
        }
        #endregion

        #region Telemetry
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AutoFillFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                // Load from the DB if needed
                if (String.IsNullOrEmpty(le.FlightData) && !LogbookEntryCore.IsNewFlightID(le.FlightID))
                    le.FlightData = new LogbookEntry(le.FlightID, le.User, LogbookEntryCore.LoadTelemetryOption.LoadAll).FlightData;

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                AutoFillOptions afOptions = AutoFillOptions.DefaultOptionsForUser(pfTarget);
                afOptions.TimeZoneOffset = Convert.ToInt32(Request["tzOffset"], CultureInfo.InvariantCulture);

                using (FlightData fd = new FlightData())
                {
                    fd.AutoFill(le, afOptions);
                }

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTelemetry()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                le.FlightData = null;
                // Load from the DB if needed
                if (!LogbookEntryCore.IsNewFlightID(le.FlightID) && le.FLoadFromDB(le.FlightID, User.Identity.Name))
                    le.FCommit(true);

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }

        /// <summary>
        /// Reads the provided telemetry and returns a base64 encoded compressed version to store in the hidden field
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Authorize]
        [HttpPost]
        public ActionResult UploadTelemetry()
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No telemetry provided");
                return Content(Convert.ToBase64String(FlightData.ReadFromStream(Request.Files[0]?.InputStream).Compress()));
            });
        }
        #endregion

        #region Images and videos
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteVideoRef(int flightVideoToDelete)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                le.DeleteVideoRef(flightVideoToDelete);
                return EmbeddedVideos(le, true);
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddVideoRef(string flightNewVideoRef, string flightNewVideoComment)
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                le.AddVideoRef(flightNewVideoRef, flightNewVideoComment);
                return EmbeddedVideos(le, Payments.EarnedGratuity.UserQualifies(User.Identity.Name, Payments.Gratuity.GratuityTypes.Videos));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult UploadFlightImages(int szKey, bool fCanDoVideo)
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                return Content(MFBPendingImage.ProcessUploadedFile(Request.ImageFile(0), MFBImageInfoBase.ImageClass.Flight, (imgType) => { return LogbookEntryCore.ValidateFileType(imgType, fCanDoVideo); },
                    (pi, szID) =>
                    {
                        if (szKey > 0)
                            pi?.Commit(MFBImageInfoBase.ImageClass.Flight, szKey.ToString(CultureInfo.InvariantCulture));
                        else if (pi?.IsValid ?? false)
                            Session[szID] = pi;
                    }));
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddGooglePhotosNew()
        {
            return await SafeOp(async () =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                // load the flight data if needed
                if (!le.IsNewFlight && String.IsNullOrEmpty(le.FlightData))
                    le.FlightData = new LogbookEntry(le.FlightID, le.User, LogbookEntryCore.LoadTelemetryOption.LoadAll).FlightData;

                await MFBPendingImage.ProcessSelectedPhotosResult(User.Identity.Name, Request["gmrJSON"], le.FlightData, (pi, szKey) =>
                {
                    if (le.IsNewFlight)
                        Session[szKey] = pi;
                    else
                        pi?.Commit(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture));
                });
                le.PopulateImages();

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }
        #endregion

        #region Pending Flights
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeletePendingFlight(string pfID)
        {
            return SafeOp(() =>
            {
                PendingFlight pf = (PendingFlight.PendingFlightsForUser(User.Identity.Name)).FirstOrDefault(pf2 => pf2.PendingID.CompareOrdinal(pfID) == 0);
                if (pf == null || pf.User.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException(Resources.WebService.errFlightNotYours);
                pf.Delete();
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CommitPendingFlight(string pfID)
        {
            return SafeOp(() =>
            {
                PendingFlight pf = (PendingFlight.PendingFlightsForUser(User.Identity.Name)).FirstOrDefault(pf2 => pf2.PendingID.CompareOrdinal(pfID) == 0);
                if (pf == null || pf.User.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException(Resources.WebService.errFlightNotYours);
                pf.FCommit(true, true);
                return new EmptyResult();
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeleteAllPendingFlights()
        {
            return SafeOp(() =>
            {
                PendingFlight.DeletePendingFlightsForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PendingFlightsInRange(int offset, int pageSize, string sortField, SortDirection sortDirection)
        {
            return SafeOp(() =>
            { 
                return PendingFlightsTable(offset, pageSize, sortField, sortDirection);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PendingFlightsReminder()
        {
            List<PendingFlight> lst = new List<PendingFlight>(PendingFlight.PendingFlightsForUser(User.Identity.Name));
            lst.Sort((l1, l2) => { return LogbookEntryCore.CompareFlights(l1, l2, "Date", SortDirection.Ascending); });
            return lst.Count > 0 && lst[0].Date.Date.CompareTo(DateTime.Now.Date) <= 0 ? PendingFlightsTable(0, 3, "Date", SortDirection.Ascending) : new EmptyResult();
        }
        #endregion

        [Authorize]
        [HttpPost]
        public ActionResult FlightEditorForFlight(int idFlight, string targetUser = null, string nextFlightHref = null, string prevFlightHref = null, string onCancel = null, string onSave = null, int chk = 0)
        {
            return SafeOp(() =>
            {
                targetUser = targetUser ?? User.Identity.Name;
                LogbookEntry le = new LogbookEntry();
                if (!le.FLoadFromDB(idFlight, targetUser))
                    throw new UnauthorizedAccessException(le.ErrorString);
                return FlightEditor(targetUser, le, false, nextFlightHref, prevFlightHref, onCancel, onSave, chk);
            });
        }

        #region Misc
        /// <summary>
        /// Saves the current flight into the session state so that when we come back we can re-constitute it.  Does NOT work for a pending flight - only actual flights that aren't yet saved.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult SaveFlightToSession()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                if (le.IsNewFlight && !(le is PendingFlight))
                    Session[keySessionInProgress] = le;
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddApproachDesc()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                AddApproachesFromRequest(le);

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }

        [Authorize]
        [HttpPost]
        /// <summary>
        /// Returns the current time formatted in UTC or specified time-zone
        /// </summary>
        /// <returns>Now in the specified locale, adjusted for the timezone.</returns>
        public string NowInUTC()
        {
            return SafeOp(() =>
            {
                // For now, always return true UTC
                return DateTime.UtcNow.FormattedNowInUtc(MyFlightbook.Profile.GetUser(User.Identity.Name).PreferredTimeZone);
            });
        }

        [Authorize]
        [HttpPost]
        public string TaxiTime(string fsStart, string fsEnd, string szTotal)
        {
            return SafeOp(() =>
            {
                bool fUseHHMM = MyFlightbook.Profile.GetUser(User.Identity.Name).UsesHHMM;

                DateTime dtFStart = fsStart.SafeParseDate(DateTime.MinValue);
                DateTime dtFEnd = fsEnd.SafeParseDate(DateTime.MinValue);
                decimal totalTime = szTotal.SafeParseDecimal();

                decimal elapsedFlight = (dtFEnd.HasValue() && dtFStart.HasValue() && dtFEnd.CompareTo(dtFStart) > 0) ? (decimal)dtFEnd.Subtract(dtFStart).TotalHours : 0;

                decimal taxi = Math.Max(totalTime - elapsedFlight, 0);
                return taxi.FormatDecimal(fUseHHMM);
            });
        }

        [Authorize]
        [HttpPost]
        public string AirborneTime(string fsStart, string fsEnd)
        {
            return SafeOp(() =>
            {
                bool fUseHHMM = MyFlightbook.Profile.GetUser(User.Identity.Name).UsesHHMM;

                DateTime dtFStart = fsStart.SafeParseDate(DateTime.MinValue);
                DateTime dtFEnd = fsEnd.SafeParseDate(DateTime.MinValue);

                decimal elapsedFlight = (dtFEnd.HasValue() && dtFStart.HasValue() && dtFEnd.CompareTo(dtFStart) > 0) ? (decimal)dtFEnd.Subtract(dtFStart).TotalHours : 0;

                return Math.Max(elapsedFlight, 0).FormatDecimal(fUseHHMM);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdatePropset(string szTargetUser, string propTuples, bool fHHMM, int[] activeTemplateIDs, bool fStripDefault, int idFlight, string dtDefault, int idAircraft = Aircraft.idAircraftUnknown)
        {
            return SafeOp(() =>
            {
                // check that the current user can edit the target user's flights.
                if (!(CheckCanViewFlights(szTargetUser, User.Identity.Name)?.CanAddLogbook ?? true))
                    throw new UnauthorizedAccessException();

                TimeZoneInfo tz = MyFlightbook.Profile.GetUser(User.Identity.Name).PreferredTimeZone;

                // remove MRU; it will come back per issue #1262 in templatesfromiDs, if it is needed.
                HashSet<int> hsActive = new HashSet<int>(activeTemplateIDs ?? Array.Empty<int>());
                hsActive.Remove((int) KnownTemplateIDs.ID_MRU);

                IEnumerable<PropertyTemplate> userTemplates = UserPropertyTemplate.TemplatesForUser(szTargetUser, true);
                return PropSetEditor(szTargetUser, CustomFlightProperty.PropertiesFromJSONTuples(propTuples, idFlight, DateTime.Parse(dtDefault, CultureInfo.CurrentCulture, DateTimeStyles.None), CultureInfo.CurrentCulture), fHHMM, PropertyTemplate.TemplatesFromIDs(userTemplates, PropertyTemplate.RefreshForAicraft(szTargetUser, idAircraft, hsActive)), tz, fStripDefault);
            });
        }
        #endregion
        #endregion

        #region Child Views
        [ChildActionOnly]
        public ActionResult FlightIssues(IEnumerable<FlightIssue> flightIssues)
        {
            ViewBag.flightIssues = flightIssues ?? Array.Empty<FlightIssue>();
            return PartialView("_flightlint");
        }

        [ChildActionOnly]
        public ActionResult EmbeddedVideos(LogbookEntry le, bool fCanDoVideo)
        {
            ViewBag.le = le;
            ViewBag.fCanDoVideo = fCanDoVideo;
            return PartialView("_videoRefs");
        }

        private IEnumerable<Aircraft> GetAircraftForFlightEditor(string targetUser, string viewer)
        {
            HashSet<Aircraft> aircraft = new HashSet<Aircraft>(new UserAircraft(targetUser).GetAircraftForUser());
            // issue #1374: instructor adding to student should be able to add using instructor's aircraft, not just student's.
            if (CheckCanViewFlights(targetUser, viewer)?.CanAddLogbook ?? false)
            {
                IEnumerable<Aircraft> instructorAircraft = new UserAircraft(viewer).GetAircraftForUser();
                foreach (Aircraft ac in instructorAircraft)
                    aircraft.Add(ac);
            }
            return aircraft;
        }

        /// <summary>
        /// Renders the "meat" of the flight editor - JUST the fields of the flight.  This allows updating of the main fields (e.g., for autofill) without doing a full-page postback
        /// </summary>
        /// <param name="le">The flight to edit</param>
        /// <param name="pf">If it's a pending flight, this is that flight as a pending flight</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult FlightEditorBody(Profile pfTarget, Profile pfViewer, LogbookEntry le, PendingFlight pf)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (pfTarget == null || pfViewer == null)
                throw new UnauthorizedAccessException();
            // Validate that this user has authority to add/edit the user's flight.
            bool fAddStudent = (CheckCanViewFlights(pfTarget?.UserName, User.Identity.Name)?.CanAddLogbook ?? false);
            ViewBag.fAddStudent = fAddStudent;

            ViewBag.activeTemplates = PropertyTemplate.DefaultPropTemplatesForUser(pfTarget.UserName, le.AircraftID, fAddStudent);
            ViewBag.le = le;
            ViewBag.pf = pf;
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = pfViewer;
            ViewBag.rgAircraft = GetAircraftForFlightEditor(pfTarget.UserName, pfViewer.UserName);
            return PartialView("_editFlightBody");
        }

        [ChildActionOnly]
        public ActionResult FlightEditor(string targetUser, LogbookEntry le = null, bool fAsAdmin = false, string nextFlightHref = null, string prevFlightHref = null, string onCancel = null, string onSave = null, int chk = 0)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new UnauthorizedAccessException();
            le = le ?? new LogbookEntry() { User = targetUser };

            PendingFlight pf = le as PendingFlight;
            UserAircraft ua = new UserAircraft(targetUser);
            int lastTail = LastTailID;

            // If this is a new flight and we have an existing flight in progress, pick that one up instead.
            if (le.IsNewFlight && pf == null && Session[keySessionInProgress] != null)
            {
                le = (LogbookEntry)Session[keySessionInProgress];
                // Issue #1312 - coming back from add new aircraft should use that aircraft
                if (lastTail > 0)
                    le.AircraftID = lastTail;
            }

            CheckCanSaveFlight(targetUser, le);

            Session.Remove(keySessionInProgress);   // clear it regardless.

            // If no aircraft ID provided, try using the last tail, if present, otherwise take the first one from their aircraft list that isn't hidden.
            le.AircraftID = BestLastTail(le.AircraftID, ua);

            ViewBag.rgAircraft = GetAircraftForFlightEditor(targetUser, User.Identity.Name);
            ViewBag.pf = pf;
            ViewBag.le = le;
            ViewBag.pfTarget = MyFlightbook.Profile.GetUser(targetUser);
            Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.pfViewer = pfViewer;
            ViewBag.asAdmin = fAsAdmin && pfViewer.CanSupport;
            ViewBag.nextFlightHref = nextFlightHref;
            ViewBag.prevFlightHref = prevFlightHref;
            ViewBag.fAdminMode = !String.IsNullOrEmpty(Request["a"]) && pfViewer.CanSupport;
            ViewBag.onCancel = onCancel;
            ViewBag.onSave = onSave;

            // Pull in any pending images
            if (le.IsNewFlight)
                le.PopulateImages();

            ViewBag.flightIssues = (chk != 0) ? (new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, le.User, FlightLint.DefaultOptionsForLocale).FirstOrDefault()?.Issues ?? Array.Empty<FlightIssue>()) : null;

            if (le.IsNewFlight && pf == null)
            {
                // Set the starting hobbs - if a new flight - to the ending hobbs of the last flight, if present
                le.HobbsStart = ((decimal?)Session[keyLastEndingHobbs]) ?? 0.0M;
                le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachStart, ((decimal?)Session[keyLastEndingTach]) ?? 0.0M));
                le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFlightMeterStart, ((decimal?)Session[keyLastEndingMeter]) ?? 0.0M));
                le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropFuelAtStart, ((decimal?)Session[keyLastEndingFuel]) ?? 0.0M));

                // If the user has entered another flight this session, default to that date rather than today
                if (Session[keyLastEntryDate] != null)
                    le.Date = (DateTime)Session[keyLastEntryDate];

                // Now that we've used these values, clear them
                Session.Remove(keyLastEndingHobbs);
                Session.Remove(keyLastEndingTach);
                Session.Remove(keyLastEntryDate);
                Session.Remove(keyLastEndingMeter);
                Session.Remove(keyLastEndingFuel);
            }

            return PartialView("_editFlight");
        }

        /// <summary>
        /// Returns a property set editor for the given combination of user, aircraft, and selected templates
        /// </summary>
        /// <param name="szTargetUser">The user for whom the editing is being performed</param>
        /// <param name="rgfp">Any existing properties that may have values (i.e., that are in the flight) - can contain empty values</param>
        /// <param name="fHHMM">HHMM preference</param>
        /// <param name="fStripDefault">True to remove any properties that have default values</param>
        /// <param name="activeTemplateIDs">The set of currently active templates; if null, this gets initialized from aircraft and user's default or MRU</param>
        /// <param name="timeZone">The default time zone to use for datetime fields</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [ChildActionOnly]
        public ActionResult PropSetEditor(string szTargetUser, IEnumerable<CustomFlightProperty> rgfp, bool fHHMM, IEnumerable<PropertyTemplate> activeTemplates, TimeZoneInfo timeZone, bool fStripDefault = false)
        {
            if (rgfp == null)
                throw new ArgumentNullException(nameof(rgfp));
            if (string.IsNullOrEmpty(szTargetUser))
                throw new ArgumentNullException(nameof(szTargetUser));
            if (activeTemplates == null)
                throw new ArgumentNullException(nameof(activeTemplates));

            // create a merged template either the currently active templates, if any, otherwise the default templates for the user/aircraft
            HashSet<int> hsSelectedTemplates = new HashSet<int>();
            foreach (PropertyTemplate pt in activeTemplates)
                hsSelectedTemplates.Add(pt.ID);
            PropertyTemplate ptMerged = PropertyTemplate.MergedTemplate(activeTemplates);

            List<CustomFlightProperty> Properties = new List<CustomFlightProperty>(rgfp);
            List<CustomPropertyType> lstRemainingProps = new List<CustomPropertyType>();
            List<CustomFlightProperty> ActiveProperties = new List<CustomFlightProperty>();

            CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(szTargetUser);

            foreach (CustomPropertyType cpt in rgCptAll)
            {
                // see if this either has a value or is in one of the active templates.
                // if it doesn't have a value but is in a template, give it a value.
                CustomFlightProperty fp = Properties.Find(cfp => cfp.PropTypeID == cpt.PropTypeID);

                // To be included, it must be EITHER
                // a) in the merged set of templates OR
                // b) in the set of properties with a non-default value (fp != null && !fp.IsDefaultValue) OR
                // c) in the set of properties with a default value (fp != null && (!fStripDefault && fp.IsDefaultValue)
                bool fInclude = ptMerged.ContainsProperty(cpt.PropTypeID) || (fp != null && (!fStripDefault || !fp.IsDefaultValue));
                if (fp == null)
                    fp = new CustomFlightProperty(cpt);

                if (!fInclude)
                    lstRemainingProps.Add(cpt);
                else
                {
                    fp.SetPropertyType(cpt);    // make sure to pick up things like autocompletion
                    ActiveProperties.Add(fp);
                }
            }
            ActiveProperties.Sort((cfp1, cfp2) => { return cfp1.PropertyType.SortKey.CompareCurrentCultureIgnoreCase(cfp2.PropertyType.SortKey); });

            ViewBag.groupedTemplates = TemplateCollection.GroupTemplates(UserPropertyTemplate.TemplatesForUser(szTargetUser, true));
            ViewBag.propList = ActiveProperties;
            ViewBag.activeTemplateIDs = hsSelectedTemplates;
            ViewBag.remainingProps = lstRemainingProps;
            ViewBag.fHHMM = fHHMM;
            ViewBag.timeZone = timeZone;
            return PartialView("_propSetEdit");
        }

        [ChildActionOnly]
        public ActionResult PropEdit(CustomFlightProperty fp, bool fHHMM, TimeZoneInfo timeZone, bool fHidden = false)
        {
            ViewBag.fp = fp ?? throw new ArgumentNullException(nameof(fp));
            ViewBag.fHHMM = fHHMM;
            ViewBag.timeZone = timeZone;

            ViewBag.hideByDefault = fHidden;

            return PartialView("_propEdit");
        }

        [ChildActionOnly]
        public ActionResult CoreField(string id, string name, string value, string cssClass, string label, string xFillSource, bool fHHMM)
        {
            ViewBag.id = id;
            ViewBag.name = name;
            ViewBag.value = value;
            ViewBag.cssClass = cssClass;
            ViewBag.label = label;
            ViewBag.xFillSource = xFillSource;
            ViewBag.fHHMM = fHHMM;
            return PartialView("_coreField");
        }

        [ChildActionOnly]
        public ActionResult PendingFlightsTable(int offset, int pageSize, string sortField, SortDirection sortDir)
        {
            List<PendingFlight> lst = new List<PendingFlight>(PendingFlight.PendingFlightsForUser(User.Identity.Name));
            lst.Sort((l1, l2) => { return LogbookEntryCore.CompareFlights(l1, l2, sortField, sortDir); });

            ViewBag.pendingFlights = lst.GetRange(offset, Math.Min(pageSize, lst.Count - offset));
            ViewBag.pageSize = pageSize;
            ViewBag.offset = offset;
            ViewBag.curPage = (offset / pageSize);
            ViewBag.numPages = (lst.Count / pageSize) + 1;
            ViewBag.viewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.sortField = sortField;
            ViewBag.sortDir = sortDir;
            return PartialView("_pendingFlightTable");
        }
        #endregion

        #region Visible Endpoints
        #region sign flights
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignFlightForUser(int idFlight, string cfiComments, string cfiUserName, string cfiPass = null, string cfiName = null, string cfiEmail = null, string cfiCert = null, string cfiExpiration = null, string ret = null, string hdnSigData = null)
        {
            try
            {
                // always do a full force load
                LogbookEntry le = new LogbookEntry(idFlight, User.Identity.Name, fForceLoad: true);

                DateTime dtExp = DateTime.TryParse(cfiExpiration, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue;

                bool fATP = Request["ckATP"] != null;
                bool fIsGroundOrATP = fATP || le.IsGroundOnly;
                bool fSIC = Request["ckSIC"] != null;

                if (String.IsNullOrWhiteSpace(cfiUserName)) // empty user name is aka "ad-hoc"
                    le.SignFlightAdHoc(cfiName, cfiEmail, cfiCert, dtExp, cfiComments, hdnSigData.DataURLBytes(), fSIC || fIsGroundOrATP);
                else
                {
                    string szPendingSigUsername = le.PendingSignatureUserName();

                    Profile pfSigner = MyFlightbook.Profile.GetUser(cfiUserName);

                    // invalid user passed or self signing.
                    if (String.IsNullOrEmpty(pfSigner.UserName) || pfSigner.UserName.CompareOrdinal(le.User) == 0)
                        throw new UnauthorizedAccessException();

                    bool fIsUserProfile = le.User.CompareOrdinal(User.Identity.Name) == 0;

                    // If we are signed in as the owning user, validate the signer's authenticity
                    if (fIsUserProfile && !System.Web.Security.Membership.ValidateUser(cfiUserName, cfiPass))
                        throw new UnauthorizedAccessException(Resources.SignOff.errIncorrectPasswordForSigning);

                    string szError = String.Empty;
                    bool needProfileRefresh = !pfSigner.CanSignFlights(out szError, le.IsGroundOnly);
                    if (needProfileRefresh)
                    {
                        pfSigner.Certificate = cfiCert;
                        pfSigner.CertificateExpiration = dtExp;
                    }

                    // Do ANOTHER check to see if you can sign - setting these above may have fixed the problem.
                    if (!pfSigner.CanSignFlights(out szError, fIsGroundOrATP))
                        throw new UnauthorizedAccessException(szError);

                    // If we are here, then we were successful - update the profile if it needed it
                    if (needProfileRefresh)
                        pfSigner.FCommit();
                    
                    // Do the actual signing
                    le.SetPendingSignature(pfSigner.UserName);  // indicate that we are authorized to sign this flight - this will NOT be persisted.
                    le.SignFlightAuthenticated(pfSigner.UserName, cfiComments, fIsGroundOrATP);

                    // Preserve the CFI's preferences for copying flights
                    string copyToInstructor = Request["fCopyFlight"] == null ? string.Empty : (Request["copyOpt"] ?? string.Empty);  // will be empty, "live", or "pending"
                    int cpd = copyToInstructor.CompareCurrentCultureIgnoreCase("Pending") == 0 ? 1 : (copyToInstructor.CompareCurrentCultureIgnoreCase("Live") == 0 ? 2 : 0);
                    pfSigner.SetPreferenceForKey(MFBConstants.keyPrefCopyFlightToCFI, cpd, cpd == 0);

                    // Copy the flight to the CFI's logbook if needed.
                    // We modify a new copy of the flight; this avoids modifying this.Flight, but ensures we get every property
                    if (cpd != 0)
                        // Issue #593 Load any telemetry, if necessary
                        new LogbookEntry(le.FlightID, le.User, LogbookEntryCore.LoadTelemetryOption.LoadAll).CopyToInstructor(pfSigner.UserName, cfiComments, cpd == 1);

                    if ((Request["btnSubmit"] ?? string.Empty).CompareCurrentCultureIgnoreCase("submitNext") == 0)
                    {
                        List<LogbookEntryBase> lst = new List<LogbookEntryBase>(LogbookEntryBase.PendingSignaturesForStudent(pfSigner, MyFlightbook.Profile.GetUser(le.User)));
                        if (lst.Count > 0)
                            return RedirectToAction("Sign", ControllerContext.RouteData.Values["controller"].ToString(), new { id = lst[0].FlightID, ret = ret });
                    }
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ViewBag.error = ex.Message;
                ViewBag.cfiComments = cfiComments; // preserve the comments for next try
                return SignInternal(idFlight, cfiUserName, cfiEmail, cfiName, cfiCert, cfiExpiration, ret);
            }

            // if we are here, we are done - go to the return url, if provided; otherwise, just show "success".
            return SafeRedirect(ret, Url.Action("Sign", ControllerContext.RouteData.Values["controller"].ToString(), new { id = idFlight, success=1}));
        }

        private ActionResult SignInternal(int id, string cfiUser = null, string cfiEmail = null, string cfiName = null, string cfiCert = null, string cfiExpiration = null, string ret = null)
        {
            LogbookEntry le = new LogbookEntry(Convert.ToInt32(id, CultureInfo.InvariantCulture), User.Identity.Name, fForceLoad: true);
            ViewBag.le = le;
            // Remember the return URL, but only if it is relative (for security)
            ViewBag.retUrl = SafeRedirectParam(ret, string.Empty);

            Profile pfSigner = (String.IsNullOrEmpty(cfiUser) || cfiUser.CompareOrdinal(User.Identity.Name) == 0) ? null : MyFlightbook.Profile.GetUser(cfiUser);

            // Scenario #1: we are not the owner of the flight.  MUST be an instructor of the student or otherwise authorized to do an authenticated signature
            if (le.User.CompareOrdinal(User.Identity.Name) != 0)
            {
                if (!le.CanSignThisFlight(User.Identity.Name, out string error))
                    throw new UnauthorizedAccessException(error);
                // If we are here, we are authorized to sign; fall through
                pfSigner = MyFlightbook.Profile.GetUser(User.Identity.Name);
            }
            else
            {
                // here we are signed in as the owner of the flight.  MIGHT need to pick an instructor
                // We have an instructor if (a) pfSigner is non-null, or if (b) an email is provided.
                // Otherwise, we MUST display UI to pick an instructor.
                if (pfSigner == null && cfiEmail == null)
                {
                    // this is our own flight, need to select an instructor first, though.
                    ViewBag.fPickSigner = true;
                    ViewBag.pfTarget = MyFlightbook.Profile.GetUser(User.Identity.Name);
                    return View("signFlight");
                }
            }

            // If we are here, we are OK to offer signing
            ViewBag.fPickSigner = false;
            ViewBag.pfTarget = MyFlightbook.Profile.GetUser(User.Identity.Name);

            ViewBag.pfSigner = pfSigner;

            ViewBag.cfiName = pfSigner?.UserFullName ?? cfiName ?? string.Empty;
            ViewBag.cfiEmail = pfSigner?.Email ?? cfiEmail ?? string.Empty;
            ViewBag.cfiCert = pfSigner?.Certificate ?? cfiCert ?? string.Empty;
            ViewBag.cfiExpiration = pfSigner?.CertificateExpiration ?? (DateTime.TryParse(cfiExpiration, out DateTime dt) ? dt : DateTime.MinValue);

            return View("signFlight");
        }

        [Authorize]
        [HttpPost]
        [ActionName("Sign")]
        [ValidateAntiForgeryToken]
        public ActionResult SignPost(int id, string cfiUser = null, string cfiEmail = null, string cfiName = null, string cfiCert = null, string cfiExpiration = null, string ret = null)
        {
            return SignInternal(id, cfiUser, cfiEmail, cfiName, cfiCert, cfiExpiration, ret);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Sign(int id, string ret = null)
        {
            return SignInternal(id, ret: ret);
        }

        /// <summary>
        /// Endpoint that is really just backwards compatibility for mobile apps, which pass "?idFlight=" instead of "/id".
        /// </summary>
        /// <param name="idFlight"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public ActionResult SignMobile(int idFlight, string ret = null)
        {
            return Redirect(Url.Action("Sign", ControllerContext.RouteData.Values["controller"].ToString(), new { id = idFlight, ret = ret }));
        }
        #endregion

        #region Pending Flights
        [Authorize]
        public ActionResult Pending(string id = null, int clone = 0, int reverse = 0)
        {
            Profile viewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.viewer =viewer;
            List <PendingFlight> lst = new List<PendingFlight>(PendingFlight.PendingFlightsForUser(User.Identity.Name));
            ViewBag.pendingFlights = lst;
            if (!String.IsNullOrEmpty(id))
            {
                int i = lst.FindIndex(pf => pf.PendingID.CompareOrdinal(id) == 0);
                if (i >= 0)
                {
                    PendingFlight pf = lst[i];

                    // since flights are in descending chronological order, the one that is earlier in the list is the "next" flight, the one that is later in the list is "previous"
                    if (i > 0)
                        ViewBag.nextFlightHref = Url.Action(this.ControllerContext.RouteData.Values["action"].ToString(), this.ControllerContext.RouteData.Values["controller"].ToString(), new { id = lst[i - 1].PendingID });
                    if (i < lst.Count - 1)
                        ViewBag.prevFlightHref = Url.Action(this.ControllerContext.RouteData.Values["action"].ToString(), this.ControllerContext.RouteData.Values["controller"].ToString(), new { id = lst[i + 1].PendingID });

                    // Issue #1335 - clone/reverse of pending
                    if (pf != null && clone != 0)
                    {
                        pf = pf.Clone(reverse != 0);
                        pf.CleanNewClone();
                        pf.Commit();
                        // Redirect to the newly created flight so that we can lose the clone/pending parameters and so that we can pick up new next/previous
                        return RedirectToAction("Pending", new { id = pf.PendingID });
                    }

                    ViewBag.pendingFlight = pf;
                    ViewBag.onCancel = "cancelEdit";
                }
                if (ViewBag.pendingFlight == null)
                    return RedirectToAction("PendingFlights", new { id = string.Empty });
            }
            ViewBag.onSave = "flightSaved";
            ViewBag.pageSize = FlightsPerPageForUser(viewer);
            return View("reviewPending");
        }
        #endregion

        #region edit a specific flight
        // GET: mvc/FlightEdit
        [Authorize]
        public ActionResult Flight(string id, string fq = null, int clone = -1, int reverse = -1, string src = "", int a = 0)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            FlightQuery q = String.IsNullOrEmpty(fq) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fq);
            if (q.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException("invalid query - incorrect username");

            LogbookEntry le = null;
            int idFlight = LogbookEntryCore.idFlightNone;

            // Three scenarios:
            // a) src= parameter provided and no id parameter provided - initialize from that
            // b) Otherwise, if a valid id is provided, initialize from that.
            // c) Otherwise, just create a new flight.
            if (!String.IsNullOrEmpty(src) && String.IsNullOrEmpty(id))
            {
                LogbookEntry leSrc = LogbookEntry.FromShareKey(src, User.Identity.Name);

                if (leSrc.FlightID == LogbookEntryCore.idFlightNone || !String.IsNullOrEmpty(leSrc.ErrorString))
                    throw new UnauthorizedAccessException("Invalid source key");

                le = leSrc.Clone(le);
                le.User = User.Identity.Name; // for safety.

                // clear out any role like PIC/SIC that likely doesn't carry over to the target pilot.
                le.CFI = le.Dual = le.PIC = le.SIC = 0.0M;

                if (le.AircraftID != Aircraft.idAircraftUnknown)
                {
                    // Add this aircraft to the user's profile if needed
                    UserAircraft ua = new UserAircraft(User.Identity.Name);
                    Aircraft ac = new Aircraft(le.AircraftID);
                    if (!ua.CheckAircraftForUser(ac))
                        ua.FAddAircraftForUser(ac);
                }
            } else if (int.TryParse(id, out idFlight) && idFlight > 0) {
                // force load it - we will check in the flight editor if we have permission to edit it
                le = new LogbookEntry(idFlight, User.Identity.Name, fForceLoad: true);

                if (clone != -1)
                {
                    le = le.Clone(null, reverse != -1);
                    le.CleanNewClone();
                }

                le.PopulateImages();
            }
            else
                le = new LogbookEntry() { User =  User.Identity.Name };
            
            ViewBag.returnURL = "~/mvc/flights".ToAbsolute() + SetUpNextPrevious(q, idFlight, pf);
            ViewBag.pf = pf;
            ViewBag.fAsAdmin = (a != 0 && pf.CanSupport);
            ViewBag.le = le;
            return View("editFlight");
        }

        [Authorize]
        public ActionResult Index()
        {
            return Flight(string.Empty);
        }
        #endregion
        #endregion
    }
}