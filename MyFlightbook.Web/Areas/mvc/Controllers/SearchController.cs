using System;
using System.Collections.Generic;
using System.Web.Mvc;

/******************************************************
    * 
    * Copyright (c) 2022-2023 MyFlightbook LLC
    * Contact myflightbook-at-gmail.com for more information
    *
   *******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class SearchController : Controller
    {
        #region Query description
        [ChildActionOnly]
        public ActionResult QueryDescriptorItem(QueryFilterItem qfe, string onClientClick)
        {
            ViewBag.qfe = qfe;
            ViewBag.onClientClick = onClientClick;
            return PartialView("_queryDescriptorItem");
        }

        [ChildActionOnly]
        public ActionResult QueryDescription(FlightQuery fq, string onClientClick)
        {
            ViewBag.query = fq;
            ViewBag.onClientClick = onClientClick;
            return PartialView("_queryDescriptor");
        }
        #endregion

        #region web services for canned queries
        [HttpPost]
        [Authorize]
        public void DeleteCannedQuery(CannedQuery cq)
        {
            if (cq == null || cq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            if (String.IsNullOrEmpty(cq.QueryName))
                throw new InvalidOperationException("Attempt to delete unnamed query");

            cq.Delete();
        }

        [HttpPost]
        [Authorize]
        public void AddCannedQuery(CannedQuery cq)
        {
            if (cq == null || cq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException();

            if (String.IsNullOrEmpty(cq.QueryName))
                return;

            cq.Commit(true);
        }
        #endregion

        [Authorize]
        public ActionResult SearchForm(FlightQuery fq, string onClientSearch, string onClientReset)
        {
            ViewBag.query = fq ?? new FlightQuery(User.Identity.Name);

            // Sort the aircraft so that everything NOT in the query is not shown by default.
            List<Aircraft> lstAc = new List<Aircraft>(new UserAircraft(User.Identity.Name).GetAircraftForUser());

            lstAc.Sort((ac1, ac2) =>
            {
                bool fSuppress1 = ac1.HideFromSelection && !fq.AircraftList.Contains(ac1);
                bool fSuppress2 = ac2.HideFromSelection && !fq.AircraftList.Contains(ac2);

                if (fSuppress1 && !fSuppress2)
                    return 1;
                else if (fSuppress2 && !fSuppress1)
                    return -1;
                else
                    return ac1.DisplayTailnumber.CompareCurrentCultureIgnoreCase(ac2.DisplayTailnumber);
            });
            ViewBag.aircraft = lstAc;

            ViewBag.models = MakeModel.ModelsForAircraft(lstAc);
            ViewBag.categoryClasses = CategoryClass.CategoryClasses();
            ViewBag.properties = CustomPropertyType.AllPreviouslyUsedPropsForUser(User.Identity.Name);
            ViewBag.cannedQueries = CannedQuery.QueriesForUser(User.Identity.Name);
            ViewBag.onClientSearch = onClientSearch;
            ViewBag.onClientReset = onClientReset;
            return PartialView("_searchForm");
        }

        // GET: mvc/Search
        public ActionResult Index()
        {
            return View();
        }
    }
}