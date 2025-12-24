using MyFlightbook.Image;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Holds the web services for aircraft stuff, helps with some code cleanliness/coupling.
    /// </summary>
    public class AircraftControllerBase : AdminControllerBase
    {
        #region Utility functions
        protected string DefaultLocale
        {
            get
            {

                string szDefLocale = Request.UserLanguages != null && Request.UserLanguages.Length > 0 ? Request.UserLanguages[0] : string.Empty;
                if (szDefLocale.Length > 4)
                    szDefLocale = szDefLocale.Substring(3, 2);
                return szDefLocale;
            }
        }

        protected string DefaultTail => CountryCodePrefix.DefaultCountryCodeForLocale(DefaultLocale).HyphenatedPrefix;

        protected bool IsAdminMode => util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(User.Identity.Name).CanSupport;

        protected DateTime ReadRequestDate(string key)
        {
            return DateTime.TryParse(Request[key], CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue;
        }

        protected Aircraft AircraftFromForm(MaintenanceRecord mrOriginal = null)
        {
            int modelID = int.TryParse(Request["aircraftAutoCompleteModel"], out int idAutocomplete) ? idAutocomplete : (int.TryParse(Request["aircraftSelectedModel"], out int idModel) ? idModel : MakeModel.UnknownModel);
            string type = Request["aircraftType"] ?? "Registered";
            AircraftInstanceTypes instanceType = Enum.TryParse(Request["aircraftInstanceType"] ?? string.Empty, true, out AircraftInstanceTypes acInstType) ?
                acInstType :
                (type.CompareCurrentCultureIgnoreCase("Sim") == 0 ? (Enum.TryParse(Request["aircraftSimType"] ?? string.Empty, true, out AircraftInstanceTypes ait) ? ait : AircraftInstanceTypes.UncertifiedSimulator) : AircraftInstanceTypes.RealAircraft);

            MakeModel.AvionicsTechnologyType avionicsTechnology = Enum.TryParse(Request["aircraftAvionics"] ?? string.Empty, true, out MakeModel.AvionicsTechnologyType att) ? att : MakeModel.AvionicsTechnologyType.None;

            Aircraft ac = new Aircraft()
            {
                AircraftID = util.GetIntParam(Request, "aircraftID", Aircraft.idAircraftUnknown),
                Revision = util.GetIntParam(Request, "aircraftRev", -1),
                ModelID = modelID,
                InstanceType = instanceType,
                TailNumber = (instanceType == AircraftInstanceTypes.RealAircraft) ? (type.CompareCurrentCultureIgnoreCase("Anonymous") == 0 ? CountryCodePrefix.szAnonPrefix : Request["aircraftTail"] ?? DefaultTail) : CountryCodePrefix.szSimPrefix, // will be fixed up below
                GlassUpgradeDate = avionicsTechnology == MakeModel.AvionicsTechnologyType.None ? null : ReadRequestDate("aircraftUpgradeDate").AsNulluble(),
                AvionicsTechnologyUpgrade = avionicsTechnology,
                Version = util.GetIntParam(Request, "aircraftVersion", 0)
            };

            // fix up any obviously invalid things:
            ac.FixUpPossibleInvalidAircraft(DefaultTail);

            if (!ac.IsNew)
            {
                ac.PopulateImages();
                ac.PrivateNotes = Request["aircraftPrivateNotes"] ?? string.Empty;
                ac.PublicNotes = Request["aircraftPublicNotes"] ?? string.Empty;

                if (ac.IsRegistered)
                {
                    MaintenanceRecord mr = new MaintenanceRecord()
                    {
                        LastAnnual = ReadRequestDate("aircraftAnnual"),
                        LastTransponder = ReadRequestDate("aircraftXPonder"),
                        LastStatic = ReadRequestDate("aircraftStatic"),
                        LastAltimeter = ReadRequestDate("aircraftAltimeter"),
                        LastELT = ReadRequestDate("aircraftELT"),
                        LastVOR = ReadRequestDate("aircraftVOR"),
                        RegistrationExpiration = ReadRequestDate("aircraftRegistration"),
                        Last100 = decimal.TryParse(Request["aircraftLast100"], NumberStyles.Number, CultureInfo.CurrentCulture, out decimal last100) ? last100 : 0,
                        LastOilChange = decimal.TryParse(Request["aircraftLastOil"], NumberStyles.Number, CultureInfo.CurrentCulture, out decimal lastOil) ? lastOil : 0,
                        LastNewEngine = decimal.TryParse(Request["aircraftLastEngine"], NumberStyles.Number, CultureInfo.CurrentCulture, out decimal lastEngine) ? lastEngine : 0,
                        Notes = Request["aircraftMaintenanceNotes"] ?? string.Empty
                    };
                    ac.UpdateMaintenanceForUser(mr, mrOriginal, User.Identity.Name);
                }
            }
            return ac;
        }        
        #endregion

        #region WebServices
        #region Manufacturers and models
        public ActionResult MakeRows(int skip, int limit, ModelQuery.ModelSortMode sortMode, ModelQuery.ModelSortDirection sortDir, string queryJSON)
        {
            return SafeOp(() =>
            {
                ModelQuery mq = JsonConvert.DeserializeObject<ModelQuery>(queryJSON);
                ViewBag.mq = mq;
                mq.Limit = Math.Min(limit, 50);
                mq.Skip = skip;
                mq.SortMode = sortMode;
                mq.SortDir = sortDir;
                IEnumerable<MakeModel> rgMakes = MakeModel.MatchingMakes(mq);
                ViewBag.rgMakes = rgMakes;
                ViewBag.imageSamples = MakeModel.SampleImagesForModels(rgMakes, 1);
                return PartialView("_modelRows");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult CheckDupeModels(string funcName)
        {
            return SafeOp(() =>
            {
                MakeModel mm = ModelFromRequest();
                MakeModel[] matches = mm.PossibleMatches();
                ViewBag.dupeModels = matches;
                ViewBag.funcName = funcName;
                return (matches.Length > 0) ? (ActionResult)PartialView("_dupeModels") : new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult GetManufacturerRestriction(int idMan)
        {
            return SafeOp(() =>
            {
                return Json(new Manufacturer(idMan).AllowedTypes);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddManufacturer(string name)
        {
            return SafeOp(() =>
            {
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentNullException(Resources.Makes.errManufacturerNameRequired);

                Manufacturer m = new Manufacturer(name);
                if (m.IsNew)
                    m.FCommit();
                return Json(m);
            });
        }

        private MakeModel ModelFromRequest()
        {
            bool fHighPerfChecked = Request["isHighPerf"] != null;
            bool f200Checked = Request["isLegacyHighPerf"] != null;
            return new MakeModel()
            {
                MakeModelID = Convert.ToInt32(Request["idmodel"], CultureInfo.InvariantCulture),
                ManufacturerID = Convert.ToInt32(Request["manufacturer"], CultureInfo.InvariantCulture),
                Model = Request["model"],
                FamilyName = Request["familyName"],
                ModelName = Request["modelName"],
                TypeName = Request["typeName"],
                ArmyMDS = Request["armyMDS"],
                CategoryClassID = Enum.TryParse(Request["catClass"], true, out CategoryClass.CatClassID ccid) ? ccid : throw new InvalidCastException("Unknown category class"),
                IsComplex = Request["isComplex"] != null,
                IsConstantProp = Request["isConstantProp"] != null,
                HasFlaps = Request["hasFlaps"] != null,
                IsRetract = Request["isRetract"] != null,
                IsTailWheel = Request["isTailwheel"] != null,
                IsMultiEngineHelicopter = Request["isMulti"] != null,
                IsMotorGlider = Request["isTMG"] != null,
                PerformanceType = fHighPerfChecked ? (f200Checked ? MakeModel.HighPerfType.Is200HP : MakeModel.HighPerfType.HighPerf) : MakeModel.HighPerfType.NotHighPerf,
                AvionicsTechnology = Enum.TryParse(Request["avionics"], true, out MakeModel.AvionicsTechnologyType type) ? type : throw new InvalidCastException("Unknown Avionics"),
                EngineType = Enum.TryParse(Request["engineType"], true, out MakeModel.TurbineLevel enginetype) ? enginetype : throw new InvalidCastException("invalid engine type"),
                IsCertifiedSinglePilot = Request["isSinglePilot"] != null,
                AllowedTypes = Enum.TryParse(Request["allowedTypes"], true, out AllowedAircraftTypes allowedTypes) ? allowedTypes : AllowedAircraftTypes.Any
            };
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CommitModel()
        {
            return SafeOp(() =>
            {
                MakeModel model = ModelFromRequest();
                model.CommitForUser(User.Identity.Name);
                MakeModel m = new MakeModel(model.MakeModelID); // reload from the database to pick up everything
                return Json(m);
            });
        }
        #endregion

        #region Aircraft model and list management
        [HttpPost]
        [Authorize]
        public ActionResult DeleteAircraftForUser(int idAircraft)
        {
            return SafeOp(() =>
            {
                (new UserAircraft(User.Identity.Name)).FDeleteAircraftforUser(idAircraft);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SuggestAircraft(string prefixText, int count)
        {
            return SafeOp(() =>
            {
                List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();
                foreach (Aircraft ac in Aircraft.AircraftWithPrefix(prefixText, count))
                    lst.Add(new Dictionary<string, object>() { { "label", ac.DisplayTailnumberWithModel }, { "value", ac.AircraftID } });

                return Json(lst);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SuggestModels(string prefixText, int count, bool fRegisteredOnly)
        {
            return SafeOp(() =>
            {
                return Json(AircraftUtility.SuggestFullModelsWithTargets(User.Identity.Name, prefixText, count, fRegisteredOnly));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ChangeCountryForTail(string prefix, string oldTail)
        {
            return SafeOp(() =>
            {
                if (string.IsNullOrEmpty(oldTail))
                    return Content(prefix);
                return Content(CountryCodePrefix.SetCountryCodeForTail(new CountryCodePrefix(string.Empty, prefix), oldTail));
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateForModelNewAircraft()
        {
            return SafeOp(() =>
            {
                Aircraft ac = AircraftFromForm();
                ViewBag.aircraft = ac;
                ViewBag.model = ac.ModelID > 0 ? MakeModel.GetModel(ac.ModelID) : null;
                return PartialView("_newAircraftBody");
            });
        }
        #endregion

        #region Admin stuff
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminCloneAircraft(int adminCloneAircraftID, int adminCloneTargetModel)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(adminCloneAircraftID);
                if (adminCloneAircraftID < 0 || ac.IsNew)
                    throw new InvalidOperationException($"Aircraft {adminCloneAircraftID} is not valid");
                MakeModel mm = MakeModel.GetModel(adminCloneTargetModel);
                if (mm.IsNew || adminCloneTargetModel < 0)
                    throw new InvalidOperationException($"Target model {adminCloneTargetModel} is not valid");

                ac.Clone(adminCloneTargetModel, (Request["adminUserToMigrate"] ?? string.Empty).SplitCommas());
                return new EmptyResult();
            });
        }
        #endregion

        #region Aircraft Images
        [HttpPost]
        [Authorize]
        public ActionResult UploadAircraftImages(int szKey)
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                return Content(MFBPendingImage.ProcessUploadedFile(Request.Files[0], false, MFBImageInfoBase.ImageClass.Flight,
                    (imgType) => { return LogbookEntryCore.ValidateFileType(imgType, false); },
                    (pi, szID) =>
                    {
                        if (szKey > 0)
                            pi?.Commit(MFBImageInfoBase.ImageClass.Aircraft, szKey.ToString(CultureInfo.InvariantCulture));
                        else if (pi?.IsValid ?? false)
                            Session[szID] = pi;
                    }));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetDefaultImage(int idAircraft, string szThumb)
        {
            return SafeOp(() =>
            {
                UserAircraft ua = new UserAircraft(User.Identity.Name);
                Aircraft acToUpdate = ua[idAircraft] ?? throw new UnauthorizedAccessException();
                if (!String.IsNullOrEmpty(szThumb))
                {
                    acToUpdate.DefaultImage = szThumb;
                    ua.FAddAircraftForUser(acToUpdate);
                    acToUpdate.PopulateImages();
                }
                return new EmptyResult();
            });
        }
        #endregion

        #region High water mark stuff
        /// <summary>
        /// Return the high-water mark for an aircraft's tach or hobbs
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>A human readable localized string of the result</returns>
        [HttpPost]
        [Authorize]
        public ActionResult HighWatermarksForAircraft(int idAircraft)
        {
            return SafeOp(() =>
            {
                if (String.IsNullOrEmpty(User.Identity.Name))
                    throw new UnauthorizedAccessException();

                if (idAircraft <= 0)
                    return Content(string.Empty);

                decimal hwHobbs = AircraftUtility.HighWaterMarkHobbsForUserInAircraft(idAircraft, User.Identity.Name);
                decimal hwTach = AircraftUtility.HighWaterMarkTachForUserInAircraft(idAircraft, User.Identity.Name);

                string szResult = string.Empty;

                if (hwTach == 0)
                    szResult = hwHobbs == 0 ? String.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkHobbsOnly, hwHobbs);
                else szResult = hwHobbs == 0
                    ? String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachOnly, hwTach)
                    : String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.HighWaterMarkTachAndHobbs, hwTach, hwHobbs);
                return Content(szResult);
            });
        }

        /// <summary>
        /// Returns the high-watermark starting hobbs for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [HttpPost]
        [Authorize]
        public ActionResult HighWaterMarkHobbsForAircraft(int idAircraft)
        {
            return SafeOp(() =>
            {
                return Content(AircraftUtility.HighWaterMarkHobbsForUserInAircraft(idAircraft, User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture));
            });
        }

        /// <summary>
        /// Returns the high-watermark starting hobbs for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [HttpPost]
        [Authorize]
        public ActionResult HighWaterMarkTachForAircraft(int idAircraft)
        {
            return SafeOp(() =>
            {
                return Content(AircraftUtility.HighWaterMarkTachForUserInAircraft(idAircraft, User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture));
            });
        }

        /// <summary>
        /// Returns the high-watermark flight meter for the specified aircraft.
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <returns>0 if unknown.</returns>
        [HttpPost]
        [Authorize]
        public ActionResult HighWaterMarkFlightMeter(int idAircraft)
        {
            return SafeOp(() =>
            {
                return Content(AircraftUtility.HighWaterMarkFlightMeter(idAircraft, User.Identity.Name).ToString("0.0#", CultureInfo.CurrentCulture));
            });
        }
        #endregion

        #region Context menu stuff
        /// <summary>
        /// Verifies that the specified idAircraft is in the user's aircraft list, returning the aircraft itself and the corresponding useraircraft
        /// </summary>
        /// <param name="idAircraft"></param>
        /// <param name="ua"></param>
        /// <returns></returns>
        private Aircraft CheckValidUserAircraft(int idAircraft, out UserAircraft ua)
        {
            if (idAircraft <= 0)
                throw new ArgumentOutOfRangeException("Invalid aircraft ID");

            ua = new UserAircraft(User.Identity.Name);
            Aircraft ac = ua[idAircraft];
            if (ac == null || ac.AircraftID == Aircraft.idAircraftUnknown)
                throw new UnauthorizedAccessException("This is not your aircraft");
            return ac;
        }

        /// <summary>
        /// Toggles the active state of a given aircraft.
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="fIsActive">Active or inactive</param>
        [HttpPost]
        [Authorize]
        public ActionResult SetActive(int idAircraft, bool fIsActive)
        {
            return SafeOp(() =>
            {
                Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);

                ac.HideFromSelection = !fIsActive;
                ua.FAddAircraftForUser(ac);
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Sets the role for flights in the given aircraft
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="fAddPICName">True to copy the pic name when autofilling PIC</param>
        /// <param name="Role">The role to assign</param>
        [HttpPost]
        [Authorize]
        public ActionResult SetRole(int idAircraft, string Role, bool fAddPICName)
        {
            return SafeOp(() =>
            {
                Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);
                if (!Enum.TryParse(Role, true, out Aircraft.PilotRole role))
                    throw new ArgumentOutOfRangeException("Invalid role - " + Role);

                ua.SetRoleForUser(ac, role, fAddPICName);
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Sets the template for flights in the given aircraft
        /// </summary>
        /// <param name="idAircraft">The ID of the aircraft to update</param>
        /// <param name="idTemplate">The ID of the template to add or remove.</param>
        /// <param name="fAdd">True to add the template, false to remove it</param>
        [HttpPost]
        [Authorize]
        public ActionResult AddRemoveTemplate(int idAircraft, int idTemplate, bool fAdd)
        {
            return SafeOp(() =>
            {
                Aircraft ac = CheckValidUserAircraft(idAircraft, out UserAircraft ua);

                if (fAdd)
                    ac.DefaultTemplates.Add(idTemplate);
                else
                    ac.DefaultTemplates.Remove(idTemplate);
                ua.FAddAircraftForUser(ac);
                return new EmptyResult();
            });
        }
        #endregion
        #endregion


    }
}