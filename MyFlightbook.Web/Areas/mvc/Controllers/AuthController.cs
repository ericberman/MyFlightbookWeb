using System;
using System.Globalization;
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

        private readonly static object lockObject = new object();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewUser(string firstName, string lastName, string accountEmail, string accountPass, string accountQAQuestionCustom, string accountQAQuestionCanned, string accountQAAnswer)
        {
            try
            {
                ViewBag.firstName = firstName;
                ViewBag.lastName = lastName;
                ViewBag.email = accountEmail;

                string szQuestion = String.IsNullOrEmpty(accountQAQuestionCustom) ? accountQAQuestionCanned : accountQAQuestionCustom;
                ViewBag.question = szQuestion;
                ViewBag.answer = accountQAAnswer;

                if (String.IsNullOrEmpty(accountEmail))
                    throw new ArgumentNullException(nameof(accountEmail), Resources.Profile.errEmailRequired);
                if (String.IsNullOrEmpty(accountPass))
                    throw new ArgumentNullException(nameof(accountPass), Resources.Profile.errInvalidPassword);
                if (String.IsNullOrEmpty(szQuestion))
                    throw new InvalidOperationException(Resources.LocalizedText.AccountQuestionRequired);
                if (String.IsNullOrEmpty(accountQAAnswer))
                    throw new ArgumentNullException(nameof(accountQAAnswer), Resources.Preferences.errAccountQAAnswerMissing);

                string szUser = Membership.GetUserNameByEmail(accountEmail);
                if (!String.IsNullOrEmpty(szUser))
                    throw new InvalidOperationException(Resources.Profile.NewUserEmailAlreadyInUse);

                lock (lockObject)
                {
                    szUser = UserEntity.UserNameForEmail(accountEmail);
                    UserEntity e = UserEntity.CreateUser(szUser, accountPass, accountEmail, szQuestion, accountQAAnswer);
                }

                ProfileAdmin.FinalizeUser(szUser, firstName ?? string.Empty, lastName ?? string.Empty);
                FormsAuthentication.SetAuthCookie(szUser, false);

                Response.Cookies[MFBConstants.keyNewUser].Value = true.ToString(CultureInfo.InvariantCulture);

                // Redirect to the next page, but only if it is relative (for security)
                return SafeRedirect(Request["returnUrl"] ?? string.Empty, "~/mvc/flights");
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException || ex is UserEntityException)
            {
                ViewBag.error = ex.Message;
                return View("newuser");
            }
        }

        [HttpGet]
        public ActionResult NewUser()
        {
            return View("newuser");
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
                    return SafeRedirect(Request["returnUrl"] ?? string.Empty, "~/mvc/flights");
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