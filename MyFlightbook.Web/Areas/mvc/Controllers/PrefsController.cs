using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrefsController : AdminControllerBase
    {
        #region Web Services
        [HttpPost]
        [Authorize]
        public ActionResult SetAutofillOptions(AutoFillOptions afo)
        {
            return SafeOp(() =>
            {
                if (afo == null)
                    throw new ArgumentNullException(nameof(afo));
                if (!(new HashSet<int>(AutoFillOptions.DefaultSpeeds).Contains((int) afo.TakeOffSpeed)))
                    afo.TakeOffSpeed = AutoFillOptions.DefaultTakeoffSpeed;
                afo.LandingSpeed = AutoFillOptions.BestLandingSpeedForTakeoffSpeed((int) afo.TakeOffSpeed);
                afo.IgnoreErrors = true;
                afo.SaveForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }
        #endregion

        #region Autofill options
        [ChildActionOnly]
        public ActionResult AutoFillOptionsEditor(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            ViewBag.afo = AutoFillOptions.DefaultOptionsForUser(szUser);
            return PartialView("_autofillOptions");
        }
        #endregion

        // GET: mvc/Prefs
        public ActionResult Index()
        {
            throw new NotImplementedException();
        }
    }
}