using MyFlightbook.CSV;
using MyFlightbook.Image;
using MyFlightbook.Templates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AircraftController : AircraftControllerBase
    {
        #region Web services or utilities that return views
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateForModelExistingAircraft()
        {
            return SafeOp(() =>
            {
                Aircraft ac = AircraftFromForm();
                return ModelDropdowns(ac.ModelID, Request["onModelChange"], ac.AircraftID);
            });
        }

        protected ViewResult modelBrowser(ModelQuery mq, bool fAdvancedSearch, bool getResults)
        {
            ViewBag.mq = mq ?? throw new ArgumentNullException(nameof(mq));
            ViewBag.fAdvanced = fAdvancedSearch;
            ViewBag.pageSize = mq?.Limit;
            ViewBag.getResults = getResults;
            return View("browseMakes");
        }
        #endregion

        #region Child actions
        [ChildActionOnly]
        public ActionResult ModelEditor(int idModel, string onCommitfunc)
        {
            ViewBag.idModel = idModel;
            MakeModel model = MakeModel.GetModel(idModel);
            ViewBag.model = model;
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.fAdmin = User.Identity.IsAuthenticated && pf.CanManageData;
            ViewBag.useArmyCurrency = pf.UsesArmyCurrency;
            ViewBag.onCommitFunc = onCommitfunc;
            return PartialView("_modelEditor");
        }

        [ChildActionOnly]
        public ActionResult AircraftListItem(IEnumerable<Aircraft> rgac, bool fAdminMode = false, string deleteFunc = null, string migrateFunc = null)
        {
            ViewBag.aircraft = rgac;
            ViewBag.deleteFunc = deleteFunc;
            ViewBag.migrateFunc = migrateFunc;
            IEnumerable<TemplateCollection> tc = TemplateCollection.GroupTemplates(UserPropertyTemplate.TemplatesForUser(User.Identity.Name, false));
            ViewBag.templates = tc;
            ViewBag.hasTemplates = tc.Any();
            return PartialView(fAdminMode ? "_adminAircraftList" : "_aircraftListItem");
        }

        [ChildActionOnly]
        public ActionResult ATDFTDNote()
        {
            return PartialView("_noteATDFTD");
        }

        [HttpPost]
        [Authorize]
        public ActionResult MaintenanceLogTable(int idAircraft, int start = 0, int pageSize = 10)
        {
            return SafeOp(() =>
            {
                if (start < 0)
                    throw new ArgumentOutOfRangeException(nameof(start));
                if (pageSize < 1)
                    throw new ArgumentOutOfRangeException(nameof(pageSize));
                return MaintenanceLogTable(idAircraft, MaintenanceLog.ChangesByAircraftID(idAircraft), start, pageSize);
            });
        }


        [ChildActionOnly]
        public ActionResult MaintenanceLogTable(int idAircraft, IEnumerable<MaintenanceLog> rgml, int start, int pageSize)
        {
            ViewBag.rgml = rgml;
            ViewBag.start = start;
            ViewBag.pageSize = pageSize;
            ViewBag.idAircraft = idAircraft;
            return PartialView("_maintenanceLog");
        }

        [ChildActionOnly]
        public ActionResult MaintenanceDueDate(DateTime dt)
        {
            ViewBag.dt = dt;
            return PartialView("_maintenanceDate");
        }

        [ChildActionOnly]
        public ActionResult ModelDropdowns(int idModel, string onModelChange, int idAircraft)
        {
            MakeModel mm = MakeModel.GetModel(idModel);
            int idManufacturer = mm.IsNew ? (int.TryParse(Request["aircraftManufacturer"] ?? string.Empty, out int selectedMan) ? selectedMan :  Manufacturer.UnsavedID) : mm.ManufacturerID;

            List<Manufacturer> lstMan = new List<Manufacturer>(Manufacturer.CachedManufacturers());
            List<MakeModel> lstMakes = idManufacturer > 0 ? new List<MakeModel>(MakeModel.MatchingMakes(idManufacturer)) : new List<MakeModel>();

            // Issue #1349 - Only real, registered aircraft can be edited, and they can't be edited to a model that must be sim or anonymous
            if (idAircraft > 0)
            {
                lstMan.RemoveAll(mf => mf.AllowedTypes != AllowedAircraftTypes.Any);
                lstMakes.RemoveAll(m => m.AllowedTypes != AllowedAircraftTypes.Any);
                if (mm.AllowedTypes != AllowedAircraftTypes.Any || lstMan.FirstOrDefault(mf => mf.ManufacturerID == idManufacturer) == null)
                {
                    idManufacturer = Manufacturer.UnsavedID;
                    idModel = MakeModel.UnknownModel;
                    lstMakes.Clear();
                }
            }
            ViewBag.fNew = idAircraft < 0;
            ViewBag.idManufacturer = idManufacturer;
            ViewBag.idModel = idModel;
            ViewBag.selectedModel = mm.IsNew ? null : mm;
            ViewBag.manufacturers = lstMan;
            ViewBag.models = lstMakes;

            List<LinkedString> lstAttributes = new List<LinkedString>();
            if (mm != null)
            {
                if (!String.IsNullOrEmpty(mm.FamilyName))
                    lstAttributes.Add(new LinkedString(ModelQuery.ICAOPrefix + mm.FamilyName));
                foreach (string sz in mm.AttributeList())
                    lstAttributes.Add(new LinkedString(sz));
            }

            ViewBag.attributes = lstAttributes;
            ViewBag.onModelChange = onModelChange;
            return PartialView("_modelDrops");
        }
        #endregion

        #region Endpoints
        #region Make View details/Edit
        private ViewResult MakeView(int idModel, bool fEditMode)
        {
            ViewBag.idModel = idModel;
            MakeModel model = MakeModel.GetModel(idModel);
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.fAdmin = User.Identity.IsAuthenticated && pf.CanManageData;
            MakeModelStats stats = model.StatsForUser(User.Identity.Name);
            ViewBag.userStats = stats;
            ViewBag.useArmyCurrency = pf.UsesArmyCurrency;
            if (fEditMode)
                ViewBag.userFlights = AircraftStats.UserFlightsTotal(User.Identity.Name, model, stats);
            else
            {
                IEnumerable<Aircraft> rgac = (new UserAircraft(User.Identity.Name)).FindMatching(ac => ac.ModelID == idModel);
                foreach (Aircraft ac  in rgac)
                    ac.PopulateImages();
                ViewBag.userAircraft = rgac;
            }
            ViewBag.model = model;
            return View(fEditMode ? "makeEdit" : "makeView");
        }

        [HttpGet]
        [Authorize]
        public ActionResult EditModel(int id)
        {
            return MakeView(id, true);
        }

        [HttpGet]
        [Authorize]
        public ActionResult ViewModel(int id)
        {
            return MakeView(id, id == MakeModel.UnknownModel);
        }
        #endregion

        #region Makes searching/browsing
        [HttpPost]
        [Authorize]
        public ActionResult Makes(bool fAdvancedSearch)
        {
            ModelQuery mq;
            mq = (fAdvancedSearch) ? new ModelQuery()
            {
                ManufacturerName = Request["manufacturer"],
                Model = Request["model"],
                ModelName = Request["modelName"],
                TypeName = Request["typeName"],
                CatClass = Request["catClass"]
            } :
                new ModelQuery() { FullText = Request["searchText"] };

            if (Enum.TryParse(Request["sortDir"], true, out SortDirection sortDirection))
                mq.SortDir = sortDirection;
            if (Enum.TryParse(Request["sortKey"], true, out ModelQuery.ModelSortMode sortMode))
                mq.SortMode = sortMode;
            mq.Skip = 0;
            mq.Limit = 25;

            string szJSon = JsonConvert.SerializeObject(mq, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
            return Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/Aircraft/Makes?q={0}", szJSon.ToSafeParameter()));
        }

        [HttpGet]
        public ActionResult Makes(string q = null)
        {
            if (q == null)
                return modelBrowser(new ModelQuery() { SortDir = SortDirection.Ascending, SortMode = ModelQuery.ModelSortMode.ModelName }, false, false);

            ModelQuery mq = JsonConvert.DeserializeObject<ModelQuery>(q.FromSafeParameter(), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });

            return modelBrowser(mq, mq.IsAdvanced, true);
        }
        #endregion // Makes

        #region View Aircraft
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadAircraft()
        {
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture }) 
            {
                string szFilename = RegexUtility.UnSafeFileChars.Replace(String.Format(CultureInfo.InvariantCulture, "Aircraft-{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName, DateTime.Now.YMDString()), string.Empty) + ".csv";
                ua.ToDataTable(dt, true);
                return File(CsvWriter.WriteToBytes(dt, true, true), "text/csv", szFilename);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MigrateAircraft(int idSrc, int idTarget)
        {
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            Aircraft acSrc = ua.GetUserAircraftByID(idSrc);
            Aircraft acTarg = ua.GetUserAircraftByID(idTarget);
            if (acSrc != null && acTarg != null)
            {
                AircraftUtility.MigrateFlightsForUser(User.Identity.Name, acSrc, acTarg);
                if (Request["fDeleteAfterMigrate"] != null)
                    ua.FDeleteAircraftforUser(acSrc.AircraftID);
            }
            return RedirectToAction("Index");
        }

        // GET: mvc/Aircraft
        [Authorize]
        [HttpGet]
        public ViewResult Index(AircraftGroup.GroupMode gm = AircraftGroup.GroupMode.Activity, int m = -1, int a = 0)
        {
            bool fAdminMode = m > 0 && a != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData;
            UserAircraft ua = new UserAircraft(User.Identity.Name);

            IEnumerable<Aircraft> lst = m > 0 ? ua.GetAircraftForUser(fAdminMode ? UserAircraft.AircraftRestriction.AllMakeModel : UserAircraft.AircraftRestriction.UserAircraft, m) : ua.GetAircraftForUser();
            foreach (Aircraft ac in lst)
                ac.PopulateImages();
            ViewBag.sourceAircraft = lst;
            if (gm == AircraftGroup.GroupMode.Recency)
                AircraftStats.PopulateStatsForAircraft(lst, User.Identity.Name);
            ViewBag.groupedAircraft = AircraftGroup.AssignToGroups(lst, fAdminMode ? AircraftGroup.GroupMode.All : gm);
            ViewBag.groupMode = gm;
            ViewBag.fAdminMode = fAdminMode;
            return View("myAircraft");
        }
        #endregion

        #region New and Update Aircraft
        [Authorize]
        public ActionResult DeadlineEdit(int id)
        {
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            Aircraft ac = ua[id] ?? throw new UnauthorizedAccessException();
            if (!ac.IsRegistered)
                throw new InvalidOperationException($"{ac.TailNumber} cannot have a deadline");
            ViewBag.ac = ac;
            return View("deadlineEditor");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SwitchAircraft(int switchSrcAircraftID, int switchTargetAircraftID, bool switchMigrateFlights)
        {
            Aircraft acSrc = new Aircraft(switchSrcAircraftID);
            Aircraft acTarget = new Aircraft(switchTargetAircraftID);
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            ua.ReplaceAircraftForUser(acTarget, acSrc, switchMigrateFlights);

            return switchMigrateFlights ? RedirectToAction("Edit", new { switchTargetAircraftID }) : RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult CommitEditAircraft(int aircraftID)
        {
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            // get the existing aircraft.  SHOULD be in the user's aircraft list, but it might not be (e.g., if admin)
            Aircraft acInAccount = ua[aircraftID];
            Aircraft acInDatabase = new Aircraft(aircraftID);
            Aircraft acExisting = acInAccount ?? acInDatabase;
            Aircraft acNew = AircraftFromForm(acInDatabase.Maintenance);

            bool fAdminMode = IsAdminMode;
            // See if we're changing the model.  
            bool fChangedModel = acExisting.ModelID != acNew.ModelID;

            // check if this isn't already in our aircraft list
            int addToUserCount = (fChangedModel && !fAdminMode && acInAccount == null) ? 1 : 0;
            bool fOtherUsers = fChangedModel && new AircraftStats(User.Identity.Name, aircraftID).Users + addToUserCount > 1;

            // Check for model change without tail number change on an existing aircraft
            if (!fAdminMode && fChangedModel && fOtherUsers && acInDatabase.HandlePotentialClone(acNew.ModelID, User.Identity.Name, (mmOld, mmNew) =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                util.NotifyAdminEvent($"Aircraft {acInDatabase.DisplayTailnumber} cloned",
                    $"User: {pf.DetailedName}\r\n\r\n{$"~/mvc/aircraft/edit/{acInDatabase.AircraftID}?a=1".ToAbsoluteBrandedUri()}\r\n\r\nOld Model: {mmOld.DisplayName + " " + mmOld.ICAODisplay}, (modelID: {mmOld.MakeModelID})\r\n\r\nNew Model: {mmNew.DisplayName + " " + mmNew.ICAODisplay}, (modelID: {mmNew.MakeModelID})", ProfileRoles.maskCanManageData | ProfileRoles.maskCanSupport);

            }))
                return RedirectToAction("Index");

            // Issue #1427 - not preserving locked state for aircraft NOT in the admin's profile
            acNew.CopyFlags(acInAccount ?? acInDatabase);
            if (acInAccount != null)
            {
                // Issue #1055 - preserve existing templates.
                acNew.DefaultTemplates.Clear();
                foreach (int idtemplate in acExisting.DefaultTemplates)
                    acNew.DefaultTemplates.Add(idtemplate);
                // Issue #1379 - preserve any default image
                acNew.DefaultImage = acInAccount.DefaultImage;
            }

            try
            {
                if (fAdminMode)
                    acNew.Commit();
                else
                    acNew.Commit(User.Identity.Name);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ViewBag.error = String.IsNullOrEmpty(acNew.ErrorString) ? ex.Message : acNew.ErrorString;
                ViewBag.aircraft = acExisting;
                ViewBag.fAdminMode = fAdminMode;
                ViewBag.model = MakeModel.GetModel(acNew.ModelID);
                ViewBag.ret = Request["returnURL"];
                ViewBag.stats = new AircraftStats(User.Identity.Name, acNew.AircraftID);
                return View("editAircraft");
            }
            return SafeRedirect(Request["returnURL"], "~/mvc/Aircraft");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Edit(string id)
        {
            int idProposed = int.TryParse(id, out int aircraftId) ? aircraftId : Aircraft.idAircraftUnknown;
            // handle possibly tombstone'd aircraft by redirecting to the mapped version.
            int idMapped = AircraftTombstone.MapAircraftID(idProposed);
            if (idMapped != idProposed)
                return RedirectToAction("Edit", new { id = idMapped });

            // Always get the latest version of the aircraft
            UserAircraft ua = new UserAircraft(User.Identity.Name);
            Aircraft acInAccount = ua[idProposed];
            Aircraft acInDatabase = new Aircraft(idProposed);
            if ((acInDatabase?.Revision ?? 0) > (acInAccount?.Revision ?? 0))
            {
                ua.InvalidateCache();
                acInAccount = ua[idProposed];
            }

            Aircraft ac = acInAccount ?? acInDatabase;

            if (ac.IsNew)
                return RedirectToAction("Index");

            ViewBag.aircraft = ac;
            ViewBag.fAdminMode = IsAdminMode;
            ViewBag.model = MakeModel.GetModel(ac.ModelID);
            ViewBag.ret = Request["ret"] ?? string.Empty;
            ViewBag.stats = new AircraftStats(User.Identity.Name, ac.AircraftID);
            return View("editAircraft");
        }

        [Authorize]
        [HttpPost]
        [ActionName("New")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CommitNewAircraft(int aircraftID)
        {
            // idAircraft in this scenario can only mean that the user auto-completed to an existing aircraft ID.
            Aircraft ac = aircraftID > 0 ? new Aircraft(aircraftID) : AircraftFromForm();

            if (aircraftID > 0)
            {
                if (ac.IsNew)   // aircraft id was not found.  Should never happen, but just go back to this page.
                    return RedirectToAction("New", new { ret = Request["returnURL"] });

                new UserAircraft(User.Identity.Name).FAddAircraftForUser(ac);
                AircraftUtility.LastTail = ac.AircraftID;
                return SafeRedirect(Request["returnURL"], "~/mvc/Aircraft");
            } 
            else if (ac.IsValid())
            {
                ac.Commit(User.Identity.Name);
                foreach (MFBPendingImage pendingImage in MFBPendingImage.PendingImagesInSession())
                {
                    if (pendingImage.ImageType != MFBImageInfoBase.ImageFileType.S3VideoMP4)
                        _ = await pendingImage.Commit(MFBImageInfoBase.ImageClass.Aircraft, ac.AircraftID.ToString(CultureInfo.InvariantCulture));
                    pendingImage.DeleteImage();     // clean it up!
                }
                AircraftUtility.LastTail = ac.AircraftID;
                return SafeRedirect(Request["returnURL"], "~/mvc/Aircraft");
            }
            else
            {
                ViewBag.fAdminMode = false;
                ViewBag.model = ac.ModelID > 0 ? MakeModel.GetModel(ac.ModelID) : null;
                ViewBag.aircraft = ac;
                ViewBag.ret = Request["returnURL"];
                return View("editAircraft");
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult New()
        {
            ViewBag.aircraft = new Aircraft() { TailNumber = DefaultTail };
            ViewBag.model = null;
            ViewBag.fAdminMode = false;
            ViewBag.ret = Request["ret"] ?? string.Empty;
            return View("editAircraft");
        }
        #endregion
        #endregion // Endpoints
    }
}