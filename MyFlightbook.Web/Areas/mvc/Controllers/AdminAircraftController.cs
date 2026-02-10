using MyFlightbook.Admin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
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
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
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
        [ValidateAntiForgeryToken]
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
        public ActionResult MigrateSim(int idOriginal, int idNew, string deviceID)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                IDictionary<string, object> dResult = AdminAircraft.MigrateSim(idOriginal, idNew, deviceID);
                IEnumerable<int> flights = (IEnumerable<int>)dResult["SignedFlightIDs"];
                List<string> lstFlightsToReview = new List<string>();
                foreach (int flight in flights)
                    lstFlightsToReview.Add($"~/mvc/flightedit/flight/{flight}?a=1".ToAbsoluteBrandedUri().ToString());
                dResult.Add("Links", lstFlightsToReview);
                return Json(dResult);
            });
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

                AdminAircraft.UpdateVersionForAircraft(ac, newVersion);
                ViewBag.rgac = AdminAircraft.AdminDupeAircraft();
                return PartialView("_dupeAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshDupeAircraft()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AdminAircraft.AdminDupeAircraft();
                return PartialView("_dupeAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult keepDupeSim(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AdminAircraft.ResolveDupeSim(idAircraft);
                return PartialView("_dupeSims");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshDupeSims()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AdminAircraft.AdminDupeSims();
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
                ViewBag.rgac = AdminAircraft.AdminAllInvalidAircraft();
                return PartialView("_invalidAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult RefreshOrphans()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.rgac = AdminAircraft.OrphanedAircraft();
                return PartialView("_orphanedAircraft");
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteOrphans(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {

                List<Aircraft> lst = new List<Aircraft>(AdminAircraft.OrphanedAircraft());
                foreach (Aircraft aircraft in lst)
                {
                    if (idAircraft < 0 || idAircraft == aircraft.AircraftID)
                        AdminAircraft.DeleteOrphanAircraft(aircraft.AircraftID);
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
                List<Aircraft> lst = new List<Aircraft>(AdminAircraft.PseudoGenericAircraft());
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
                ViewBag.rgac = AdminAircraft.AircraftMatchingPattern(tailToFind);
                return PartialView("_aircraftList");
            });
        }

        [Authorize]
        [HttpPost]
        public string CleanUpMaintenance()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return AdminAircraft.CleanUpMaintenance();
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

        [Authorize]
        [HttpPost]
        public ActionResult ConvertOandI(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                AdminAircraft.AdminRenameAircraft(ac, ac.TailNumber.ToUpper(CultureInfo.CurrentCulture).Replace('O', '0').Replace('I', '1'));
                return new EmptyResult();
            });

        }

        [Authorize]
        [HttpPost]
        public ActionResult TrimLeadingN(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                if (!ac.TailNumber.StartsWith("N", StringComparison.CurrentCultureIgnoreCase))
                    return new EmptyResult();

                AdminAircraft.AdminRenameAircraft(ac, ac.TailNumber.Replace("-", string.Empty).Substring(1));
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult TrimN0(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                string szTail = ac.TailNumber.Replace("-", string.Empty);

                if (!szTail.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase) || szTail.Length <= 2)
                    return new EmptyResult();

                AdminAircraft.AdminRenameAircraft(ac, "N" + szTail.Substring(2));
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public string ReHyphenate(int idAircraft, string newTail)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);
                if (ac.IsNew)
                    throw new InvalidOperationException("Aircraft was not found");
                if (!ac.IsRegistered)
                    throw new InvalidOperationException("Cannot rehyphenate a sim or anonymous aircraft");
                if (Aircraft.NormalizeTail(newTail).CompareCurrentCultureIgnoreCase(ac.SearchTail) != 0)
                    throw new InvalidOperationException("You can change hyphenation this way, but not a tail number");

                ac.TailNumber = newTail.ToUpper(CultureInfo.InvariantCulture);
                ac.Commit();
                return ac.TailNumber;
            });
        }


        [Authorize]
        [HttpPost]
        public ActionResult MigrateGeneric(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                Aircraft acOriginal = new Aircraft(ac.AircraftID);

                // See if there is a generic for the model
                string szTailNumGeneric = Aircraft.AnonymousTailnumberForModel(acOriginal.ModelID);
                Aircraft acGeneric = new Aircraft(szTailNumGeneric);
                if (acGeneric.IsNew)
                {
                    acGeneric.TailNumber = szTailNumGeneric;
                    acGeneric.ModelID = acOriginal.ModelID;
                    acGeneric.InstanceType = AircraftInstanceTypes.RealAircraft;
                    acGeneric.Commit();
                }

                AdminAircraft.AdminMergeDupeAircraft(acGeneric, acOriginal);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult MigratePsuedoSim(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                if (AdminAircraft.MapToSim(ac) < 0)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to map aircraft {0}", ac.TailNumber));
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public string ViewFlights(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);

                if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

                StringBuilder sb = new StringBuilder("<table><tr style=\"vertical-align: top; font-weight: bold\"><td>Date</td><td>User</td><td>Grnd</tD><td>Total</td><td>Signed?</td></tr>");

                DBHelper dbh = new DBHelper("SELECT *, IF(SignatureState = 0, '', 'Yes') AS sigState FROM flights f WHERE idAircraft=?id");
                sb.AppendLine(@"");

                dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("id", idAircraft); },
                    (dr) =>
                    {
                        sb.AppendFormat(CultureInfo.CurrentCulture, @"<tr style=""vertical-align: top;""><td><a target=""_blank"" href=""{0}"">{1}</a></td>", VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/mvc/flightedit/flight/{0}?a=1", dr["idFlight"])), Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture).ToShortDateString());
                        sb.AppendFormat(CultureInfo.CurrentCulture, @"<td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>", (string)dr["username"], String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["groundSim"]), String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["totalFlightTime"]), (string)dr["sigState"]);
                        sb.AppendLine("</tr>");
                    });
                sb.AppendLine("</table>");

                return sb.ToString();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult IgnorePseudo(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);
                ac.PublicNotes += '\u2006'; // same marker as in flightlint - a very thin piece of whitespace
                DBHelper dbh = new DBHelper("UPDATE aircraft SET publicnotes=?notes WHERE idaircraft=?id");
                dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("notes", ac.PublicNotes);
                    comm.Parameters.AddWithValue("id", idAircraft);
                });
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ToggleLock(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                DBHelper dbh = new DBHelper("UPDATE aircraft SET isLocked = IF(isLocked = 0, 1, 0) WHERE idaircraft=?id");
                dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", idAircraft); });

                bool result = false;

                dbh.CommandText = "SELECT isLocked FROM aircraft WHERE idaircraft=?id";
                dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("id", idAircraft); },
                    (dr) => { result = Convert.ToInt32(dr["isLocked"], CultureInfo.InvariantCulture) != 0; });

                util.FlushCache();  // so that everybody picks up the new lock state

                return Json(result);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult MergeAircraft(int idAircraftToMerge, int idTargetAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (idAircraftToMerge <= 0)
                    throw new ArgumentOutOfRangeException(nameof(idAircraftToMerge), "Invalid id for aircraft to merge");
                if (idTargetAircraft <= 0)
                    throw new ArgumentOutOfRangeException(nameof(idTargetAircraft), "Invalid target aircraft for merge");

                Aircraft acMaster = new Aircraft(idTargetAircraft);
                Aircraft acClone = new Aircraft(idAircraftToMerge);

                if (!acMaster.IsValid())
                    throw new InvalidOperationException("Invalid target aircraft for merge");
                if (!acClone.IsValid())
                    throw new InvalidOperationException("Invalid source aircraft for merge");

                AdminAircraft.AdminMergeDupeAircraft(acMaster, acClone);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult MakeDefault(int idAircraft)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Aircraft ac = new Aircraft(idAircraft);
                if (ac.IsValid())
                    ac.MakeDefault();
                else
                    throw new InvalidOperationException(ac.ErrorString);
                return new EmptyResult();
            });
        }
        #endregion

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(HttpPostedFileBase fuMapModels)
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            try
            {
                ViewBag.modelMapping = (fuMapModels?.ContentLength ?? 0) > 0
                    ? new List<AircraftAdminModelMapping>(AircraftAdminModelMapping.MapModels(fuMapModels.InputStream))
                    : throw new InvalidOperationException("No content provided");
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ViewBag.modelMapError = ex.Message;
            }

            return View("adminAircraft");
        }

        // GET: mvc/AdminAircraft
        [Authorize]
        public ActionResult Index()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            return View("adminAircraft");
        }
    }
}