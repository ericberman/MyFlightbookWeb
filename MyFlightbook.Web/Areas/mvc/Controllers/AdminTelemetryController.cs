using MyFlightbook.Telemetry;
using System;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminTelemetryController : Controller
    {
        [HttpPost]
        [Authorize]
        public ActionResult TelemetryForUser(string szUser)
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            ViewBag.Telemetries = String.IsNullOrEmpty(szUser) ? Array.Empty<TelemetryReference>() : TelemetryReference.ADMINTelemetryForUser(szUser);

            return PartialView("_userTelemetry");
        }

        // GET: mvc/AdminTelemetry
        [Authorize]
        public ActionResult Index()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return View("adminTelemetry");
        }
    }
}