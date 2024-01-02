using MyFlightbook.Templates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AircraftController : AdminControllerBase
    {
        #region WebServices
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
        #endregion

        private ViewResult modelBrowser(ModelQuery mq, bool fAdvancedSearch, bool getResults)
        {
            ViewBag.mq = mq;
            ViewBag.fAdvanced = fAdvancedSearch;
            ViewBag.pageSize = mq.Limit;
            ViewBag.getResults = getResults;
            return View("browseMakes");
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
                return (matches.Length > 0) ? (ActionResult) PartialView("_dupeModels") : new EmptyResult();
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
        public ActionResult AircraftListItem(Aircraft ac, bool fAdminMode = false, string deleteFunc = null, string migrateFunc = null)
        {
            ViewBag.aircraft = ac;
            ViewBag.isAdminMode = fAdminMode;
            ViewBag.deleteFunc = deleteFunc;
            ViewBag.migrateFunc = migrateFunc;
            return PartialView("_aircraftListItem");
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
                ViewBag.userFlights = model.UserFlightsTotal(User.Identity.Name, stats);
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

            if (Enum.TryParse(Request["sortDir"], true, out ModelQuery.ModelSortDirection sortDirection))
                mq.SortDir = sortDirection;
            if (Enum.TryParse(Request["sortKey"], true, out ModelQuery.ModelSortMode sortMode))
                mq.SortMode = sortMode;
            mq.Skip = 0;
            mq.Limit = 25;

            string szJSon = JsonConvert.SerializeObject(mq, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
            string szQ = Convert.ToBase64String(szJSon.Compress());
            return Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/Aircraft/Makes?q={0}", HttpUtility.UrlEncode(szQ)));
        }

        [HttpGet]
        public ActionResult Makes(string q = null)
        {
            if (q == null)
                return modelBrowser(new ModelQuery() { SortDir = ModelQuery.ModelSortDirection.Ascending, SortMode = ModelQuery.ModelSortMode.ModelName }, false, false);

            ModelQuery mq = JsonConvert.DeserializeObject<ModelQuery>(Convert.FromBase64String(q).Uncompress(), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });

            return modelBrowser(mq, mq.IsAdvanced, true);
        }
        #endregion // Makes
        #endregion // Endpoints

        // GET: mvc/Aircraft
        public ActionResult Index()
        {
            return View();
        }
    }
}