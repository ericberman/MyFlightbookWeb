using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AuthController : AdminControllerBase
    {
        #region Web Services
        #endregion

        [ChildActionOnly]
        public ActionResult TwoFactorAuthGuard(string onSubmit, string tfaErr = null)
        {
            ViewBag.onSubmit = onSubmit;
            ViewBag.tfaErr = tfaErr;
            return PartialView("_tfaGuard");
        }

        // GET: mvc/Auth
        public ActionResult Index()
        {
            return Redirect("~/secure/login.aspx");
        }
    }
}