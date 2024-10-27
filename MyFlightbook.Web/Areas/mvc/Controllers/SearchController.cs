using System;
using System.Collections.Generic;
using System.Web.Mvc;

/******************************************************
    * 
    * Copyright (c) 2022-2024 MyFlightbook LLC
    * Contact myflightbook-at-gmail.com for more information
    *
   *******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class SearchController : AdminController
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
        public ActionResult DeleteCannedQuery(CannedQuery cq)
        {
            return SafeOp(() =>
            {
                if ((cq?.UserName ?? string.Empty).CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();

                if (String.IsNullOrEmpty(cq?.QueryName))
                    throw new InvalidOperationException("Attempt to delete unnamed query");

                cq.Delete();

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddCannedQuery(CannedQuery cq)
        {
            return SafeOp(() =>
            {
                if (cq == null || cq.UserName.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();

                if (String.IsNullOrEmpty(cq.QueryName))
                    return new EmptyResult();

                cq.Commit(true);

                return new EmptyResult();
            });
        }
        #endregion

        [ChildActionOnly]
        public ActionResult SearchForm(FlightQuery fq, string onClientSearch, string onClientReset)
        {
            if (fq == null && !User.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException();

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
            ViewBag.properties = CustomPropertyType.AllPreviouslyUsedPropsForUser(fq.UserName);
            ViewBag.cannedQueries = CannedQuery.QueriesForUser(fq.UserName);
            ViewBag.onClientSearch = onClientSearch;
            ViewBag.onClientReset = onClientReset;
            return PartialView("_searchForm");
        }
   }
}