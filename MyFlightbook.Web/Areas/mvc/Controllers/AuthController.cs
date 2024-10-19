using System;
using System.Web.Mvc;
using System.Web.Security;

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
        [HttpPost]
        public ActionResult Check2fa(string userEmail, string tfaCode)
        {
            return SafeOp(() =>
            {
                string szUser = Membership.GetUserNameByEmail(userEmail);
                Profile pf = MyFlightbook.Profile.GetUser(szUser);
                return pf.PreferenceExists(MFBConstants.keyTFASettings) && !Check2FA(pf, tfaCode) ? Json(Resources.Profile.TFACodeFailed) : Json(true);
            });
        }
        #endregion

        [ChildActionOnly]
        public ActionResult TwoFactorAuthGuard(string onSubmit, string tfaErr = null)
        {
            ViewBag.onSubmit = onSubmit;
            ViewBag.tfaErr = tfaErr;
            return PartialView("_tfaGuard");
        }

        // POST: mvc/Auth - postback for a sign-in page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string userEmail, string userPW, string tfaCode)
        {

            if (String.IsNullOrEmpty(userEmail))
            {
                ViewBag.error = Resources.Profile.errEmailRequired;
                ViewBag.step = 0;   // on step 0 - collect email
                return View("signin");
            }
            ViewBag.email = userEmail;

            string szUser = Membership.GetUserNameByEmail(userEmail);
            bool fCollectTFA = !String.IsNullOrEmpty(szUser) && MyFlightbook.Profile.GetUser(szUser ?? string.Empty).PreferenceExists(MFBConstants.keyTFASettings);

            ViewBag.step = 1;   // likely on step 1 - verify password, if provided
            ViewBag.usesTFA = fCollectTFA;

            if (!String.IsNullOrEmpty(userPW))
            {
                string szErr = Membership.ValidateUser(szUser, userPW) ?
                    ((fCollectTFA && !Check2FA(MyFlightbook.Profile.GetUser(szUser), tfaCode)) ? Resources.Profile.TFACodeFailed : string.Empty) :
                    Resources.Profile.errInvalidPassword;

                if (String.IsNullOrEmpty(szErr))
                {
                    Profile pf = MyFlightbook.Profile.GetUser(szUser);
                    FormsAuthentication.SetAuthCookie(szUser, Request["rememberMe"] != null);
                    Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                        ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                        : (object)null;
                    Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
                    string redir = Request["returnUrl"];
                    return SafeRedirect(redir ?? string.Empty, "~/mvc/flights");
                }
                ViewBag.error = szErr;  // if we are here, we have an error - pass it down
            }

            return View("signin");
        }

        // GET: mvc/Auth - present a sign-in page
        public ActionResult Index()
        {
            return View("signin");
        }
    }
}