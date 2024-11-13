using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
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

        /// <summary>
        /// Step 0 - just hit the page ab-initio
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ResetPass()
        {
            return View("resetPass");
        }

        /// <summary>
        /// Step 1 - send the email and tell "OK"
        /// </summary>
        /// <param name="resetEmail"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPass(string resetEmail)
        {
            ViewBag.step = 1;
            ViewBag.emailSent = resetEmail ?? string.Empty;
            string szUser = Membership.GetUserNameByEmail(resetEmail);
            if (!String.IsNullOrEmpty(szUser))  // fail silently on empty user - don't do anything to acknowledge the existence or lack thereof of an account
            {
                PasswordResetRequest prr = new PasswordResetRequest() { UserName = szUser };
                prr.FCommit();

                var nvc = HttpUtility.ParseQueryString(string.Empty);
                nvc["t"] = prr.ID;

                UriBuilder uriBuilder = new UriBuilder(Request.Url.Scheme, Request.Url.Host)
                {
                    Path = "~/mvc/Auth/ProcessReset".ToAbsolute(),
                    Query = nvc.ToString()
                };

                string szEmailBody = Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ResetPassEmail)).Replace("<% RESET_LINK %>", uriBuilder.Uri.ToString());
                Profile pf = MyFlightbook.Profile.GetUser(szUser);
                util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.ResetPasswordSubjectNew), szEmailBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
            }
            return View("resetPass");
        }

        /// <summary>
        /// Handle a reset link in an email
        /// </summary>
        /// <param name="t">The ID of the password reset request</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [HttpGet]
        public ActionResult ProcessReset(string t)
        {
            try
            {
                PasswordResetRequest prr = String.IsNullOrEmpty(t) ? null : new PasswordResetRequest(t);
                if (prr != null)
                {
                    switch (prr.Status)
                    {
                        case PasswordResetRequest.RequestStatus.Expired:
                            throw new InvalidOperationException(Resources.LocalizedText.ResetPasswordRequestExpired);
                        case PasswordResetRequest.RequestStatus.Failed:
                        case PasswordResetRequest.RequestStatus.Success:
                            throw new InvalidOperationException(Resources.LocalizedText.ResetPasswordRequestAlreadyUsed);
                    }
                    ViewBag.prr = prr;
                }
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException)
            {
                ViewBag.error = Resources.LocalizedText.ResetPasswordInvalidRequest;
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                ViewBag.error = ex.Message;
            }

            return View("processReset");
        }

        /// <summary>
        /// Step 2 of processing - validate the user's answer and, if needed, TFA code, and 
        /// Step 3 - let the user change their password
        /// </summary>
        /// <param name="prrID">The ID of the password reset request</param>
        /// <param name="answer">The user's answer</param>
        /// <param name="tfaCode">A tfa code, if required</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessReset()
        {
            PasswordResetRequest prr = null;
            try
            {
                prr = new PasswordResetRequest(Request["prrID"]);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ViewBag.error = ex.Message;
                return View("processReset");
            }
            ViewBag.prr = prr;

            string tmpp = Request["tmpp"];  // see if the password itself has been reset
            if (String.IsNullOrEmpty(tmpp)) // still need to verify identity
            {
                try
                {
                    string szUser = prr.UserName;
                    Profile pf = MyFlightbook.Profile.GetUser(szUser);
                    if (pf.PreferenceExists(MFBConstants.keyTFASettings) && !Check2FA(pf, Request["tfaCode"] ?? string.Empty))
                        throw new UnauthorizedAccessException(Resources.Profile.TFACodeFailed);

                    ViewBag.tmpp = Membership.GetUser(szUser).ResetPassword(Request["answer"] ?? string.Empty);
                    ViewBag.step = 1;
                }
                catch (Exception ex) when (ex is MembershipPasswordException || ex is UnauthorizedAccessException || ex is ArgumentOutOfRangeException)
                {
                    if (prr != null)
                    {
                        prr.Status = PasswordResetRequest.RequestStatus.Failed;
                        prr.FCommit();
                    }
                    ViewBag.error = ex.Message;
                }
                return View("processReset");
            }
            else
            {
                string accountPass = Request["accountPass"];

                try
                {
                    if ((accountPass?.Length ?? 0) < 8)
                        throw new MyFlightbookException(Resources.Profile.errBadPasswordLength);
                    if (!Membership.Provider.ChangePassword(prr.UserName, tmpp, accountPass))
                        throw new MyFlightbookException(Resources.Profile.errChangePasswordFailed);

                    if (Membership.ValidateUser(prr.UserName, accountPass))
                        FormsAuthentication.SetAuthCookie(prr.UserName, false);

                    prr.Status = PasswordResetRequest.RequestStatus.Success;
                    prr.FCommit();
                    return Redirect("~/mvc/pub");
                }
                catch (MyFlightbookException ex)
                {
                    prr.Status = PasswordResetRequest.RequestStatus.Failed;
                    prr.FCommit();
                    ViewBag.tmpp = tmpp;
                    ViewBag.error = ex.Message;
                    ViewBag.step = 1;   // stay on this step
                    return View("processReset");
                }
            }
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

                if (!string.IsNullOrEmpty(LocalConfig.SettingForKey("recaptchaKey")) && String.IsNullOrEmpty(Request["g-recaptcha-response"]))
                    throw new InvalidOperationException(Resources.LocalizedText.ValidationRecaptchaFailed);

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