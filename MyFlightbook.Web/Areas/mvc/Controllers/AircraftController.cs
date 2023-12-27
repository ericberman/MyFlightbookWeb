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

        #region Endpoints
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