using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminAircraftController : AdminControllerBase
    {
        #region Manufacturers
        [Authorize]
        [HttpPost]
        public ActionResult MergeDupes(int idToKeep, int idToKill)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () => {
                AdminManufacturer am = new AdminManufacturer(idToKeep);
                am.MergeFrom(idToKill);
                return Content(string.Empty);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteManufacturer(int id)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminManufacturer.Delete(id);
                return Content(string.Empty);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditManufacturer(int id, string ManufacturerName, int restriction)
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            Manufacturer man = (id == Manufacturer.UnsavedID) ? new Manufacturer() : new Manufacturer(id);
            man.ManufacturerName = ManufacturerName;
            man.AllowedTypes = (AllowedAircraftTypes)restriction;
            man.FCommit();
            return Redirect("Manufacturers");
        }

        [Authorize]
        public ActionResult Manufacturers()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            Task.WaitAll(
                Task.Run(() => { ViewBag.dupeManufacturers = AdminManufacturer.DupeManufacturers(); }),
                Task.Run(() => { ViewBag.allManufacturers = AdminManufacturer.AllManufacturers(); })
                );

            return View("adminManufacturers");
        }
        #endregion

        #region Models
        [Authorize]
        [HttpPost]
        public ActionResult ReviewTypes()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.models = AdminMakeModel.TypeRatedModels();
            ViewBag.name = "types";
            ViewBag.includeDelete = false;
            return PartialView("_modelsTable");
        }

        [Authorize]
        [HttpPost]
        public ActionResult MergeModels(int idToKeep, int idToKill)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (idToKeep == idToKill)
                    throw new InvalidOperationException("Can't merge to self!");

                MakeModel mmToDelete = new MakeModel(idToKill);
                if (mmToDelete.MakeModelID <= 0)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid model ID {0}", idToKill));

                MakeModel mmToKeep = new MakeModel(idToKeep);
                if (mmToKeep.MakeModelID <= 0)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid model ID {0}", idToKeep));

                if (String.Compare(mmToDelete.Model.Replace(" ", string.Empty).Replace("-", string.Empty), mmToKeep.Model.Replace(" ", string.Empty).Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase) != 0)
                    throw new InvalidOperationException("These don't look like dupes");

                return Content(String.Join("\r\n", AdminMakeModel.AdminMergeDuplicateModels(idToKill, idToKeep)));
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PreviewMerge(int idToKeep, int idToKill)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (idToKeep == idToKill)
                    throw new InvalidOperationException("Can't merge to self!");
                if (idToKeep <= 0 || idToKill <= 0)
                    throw new InvalidOperationException("Invalid model ID");

                MakeModel mmToDelete = new MakeModel(idToKill);
                if (mmToDelete.MakeModelID <= 0)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid model ID {0}", idToKill));

                MakeModel mmToKeep = new MakeModel(idToKeep);
                if (mmToKeep.MakeModelID <= 0)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid model ID {0}", idToKeep));

                if (String.Compare(mmToDelete.Model.Replace(" ", string.Empty).Replace("-", string.Empty), mmToKeep.Model.Replace(" ", string.Empty).Replace("-", string.Empty), StringComparison.OrdinalIgnoreCase) != 0)
                    throw new InvalidOperationException("These don't look like dupes");

                ViewBag.modelIDToKill = idToKill;
                ViewBag.modelIDToKeep = idToKeep;
                ViewBag.previewAudit = String.Format(CultureInfo.CurrentCulture, "Model {0} will be deleted\r\nThe following airplanes will be mapped from model {1} to {2}", idToKill, idToKeep, idToKill);
                ViewBag.affectedAircraft = new UserAircraft(null).GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idToKill);
                return PartialView("_mergeModelsPreview");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteModel(int idModel)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                AdminMakeModel.DeleteModel(idModel);
                return Content(string.Empty);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshDupes(bool fExcludeSims)
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.models = AdminMakeModel.PotentialDupes(!fExcludeSims);
            ViewBag.name = "dupes";
            ViewBag.includeDelete = false;
            return PartialView("_modelsTable");
        }

        [ChildActionOnly]
        public ActionResult ModelTable(IEnumerable<MakeModel> rgModels, string name, bool fIncludeDelete = false)
        {
            ViewBag.models = rgModels;
            ViewBag.name = name;
            ViewBag.includeDelete = fIncludeDelete;
            return PartialView("_modelsTable");
        }

        [Authorize]
        public ActionResult Models()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            Task.WaitAll(
                Task.Run(() => { ViewBag.modelsThatShouldBeSims = AdminMakeModel.ModelsThatShoulBeSims(); }),
                Task.Run(() => { ViewBag.orphanedModels = AdminMakeModel.OrphanedModels(); }),
                Task.Run(() => { ViewBag.dupeModels = AdminMakeModel.PotentialDupes(true); }));

            return View("adminModels");
        }
        #endregion

        #region Aircraft
        [Authorize]
        [HttpPost]
        public ActionResult UpdateVersion(int aircraftID, int newVersion)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(aircraftID);
                if (ac.AircraftID <= 0)
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid aircraft ID: {0}", aircraftID));

                AircraftUtility.UpdateVersionForAircraft(ac, newVersion);
                ViewBag.rgac = AircraftUtility.AdminDupeAircraft();
                return PartialView("_dupeAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshDupeAircraft()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.AdminDupeAircraft();
                return PartialView("_dupeAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult keepDupeSim(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.ResolveDupeSim(idAircraft);
                return PartialView("_dupeSims");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshDupeSims()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.AdminDupeSims();
                return PartialView("_dupeSims");
            });
        }

        [Authorize]
        [HttpPost]
        public string RenameSim(int idAircraft, bool fPreview)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);
                string szNewTail = Aircraft.SuggestTail(ac.ModelID, ac.InstanceType).TailNumber;
                if (!fPreview)
                {
                    ac.TailNumber = szNewTail;
                    ac.Commit();
                }
                return szNewTail;
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AllSims()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                List<Aircraft> lst = new List<Aircraft>((new UserAircraft(User.Identity.Name)).GetAircraftForUser(UserAircraft.AircraftRestriction.AllSims, -1));
                lst.Sort((ac1, ac2) =>
                {
                    if (ac1.ModelID == ac2.ModelID)
                        return ((int)ac1.InstanceType - (int)ac2.InstanceType);
                    else
                        return String.Compare(ac1.ModelDescription, ac2.ModelDescription, StringComparison.CurrentCultureIgnoreCase);
                });

                ViewBag.rgac = lst;
                return PartialView("_allSims");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult InvalidAircraft()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.AdminAllInvalidAircraft();
                return PartialView("_invalidAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshOrphans()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.OrphanedAircraft();
                return PartialView("_orphanedAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteOrphans(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () => {

                List<Aircraft> lst = new List<Aircraft>(AircraftUtility.OrphanedAircraft());
                foreach (Aircraft aircraft in lst)
                {
                    if (idAircraft < 0 || idAircraft == aircraft.AircraftID)
                        AircraftUtility.DeleteOrphanAircraft(aircraft.AircraftID);
                }

                lst.RemoveAll(ac => idAircraft < 0 || ac.AircraftID == idAircraft);

                ViewBag.rgac = lst;
                return PartialView("_orphanedAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshPseudoGeneric()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                List<Aircraft> lst = new List<Aircraft>(AircraftUtility.PseudoGenericAircraft());
                ViewBag.rgac = lst;
                return PartialView("_pseudoGeneric");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult FindAircraft(string tailToFind)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AircraftUtility.AircraftMatchingPattern(tailToFind);
                return PartialView("_aircraftList");
            });
        }

        [Authorize]
        [HttpPost]
        public string CleanUpMaintenance()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return AircraftUtility.CleanUpMaintenance();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshCountryCodes()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgcc = CountryCodePrefix.CountryCodes();
                return PartialView("_countryCodes");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdatecountryCode(int id, string prefix, string countryName, string locale, string registrationLinkTemplate, int templateMode, int hyphenPref)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                List<CountryCodePrefix> lst = new List<CountryCodePrefix>(CountryCodePrefix.CountryCodes());
                CountryCodePrefix ccp = lst.Find(c => c.ID == id);
                ccp.Prefix = prefix;
                ccp.CountryName = countryName;
                ccp.Locale = locale;
                ccp.RegistrationLinkTemplate = registrationLinkTemplate;
                ccp.RegistrationURLTemplateMode = (CountryCodePrefix.RegistrationTemplateMode)templateMode;
                ccp.HyphenPref = (CountryCodePrefix.HyphenPreference)hyphenPref;
                ccp.Commit();

                ViewBag.rgcc = CountryCodePrefix.CountryCodes();
                return PartialView("_countryCodes");
            });
        }

        [Authorize]
        [HttpPost]
        public string FixHyphenation(int id)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                List<CountryCodePrefix> lst = new List<CountryCodePrefix>(CountryCodePrefix.CountryCodes());
                CountryCodePrefix ccp = lst.Find(c => c.ID == id);
                return String.Format(CultureInfo.CurrentCulture, "{0} aircraft updated", ccp.ADMINNormalizeMatchingAircraft());
            });
        }

        [Authorize]
        [HttpPost]
        public string MapModel(AircraftAdminModelMapping amm)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CheckAuth(ProfileRoles.maskCanManageData);
                amm.CommitChange();
                return string.Empty;
            });
        }
        #endregion

        // GET: mvc/AdminAircraft
        [Authorize]
        public ActionResult Index(HttpPostedFileBase fuMapModels = null)
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            try
            {
                if (fuMapModels != null && fuMapModels.ContentLength > 0)
                    ViewBag.modelMapping = new List<AircraftAdminModelMapping>(AircraftAdminModelMapping.MapModels(fuMapModels.InputStream));
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) 
            {
                ViewBag.modelMapError = ex.Message;
            }

            return View("adminAircraft");
        }
    }
}