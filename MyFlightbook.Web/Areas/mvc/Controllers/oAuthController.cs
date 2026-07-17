using DotNetOpenAuth.OAuth2;
using MyFlightbook.AircraftSupport.Maintenance;
using MyFlightbook.CloudStorage;
using MyFlightbook.OAuth;
using MyFlightbook.OAuth.CloudAhoy;
using MyFlightbook.OAuth.FlightCrewView;
using MyFlightbook.OAuth.Leon;
using MyFlightbook.OAuth.Maintenance;
using MyFlightbook.OAuth.MyTailLog;
using MyFlightbook.OAuth.TachTime;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class oAuthController : oAuthControllerServer
    {
        #region FlightCrewView

        [Authorize]
        public async Task<ActionResult> FlightCrewViewRedir()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            IAuthorizationState auth = await new FlightCrewViewClient().ConvertToken(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, "~/mvc/oauth/flightcrewviewredir".ToAbsolute()), Request["code"]);
            pf.SetPreferenceForKey(FlightCrewViewClient.AccessTokenPrefKey, auth);
            return Redirect("~/mvc/oauth/ManageFlightCrewView");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageFlightCrewView(string action)
        {
            if (action.CompareOrdinal("deAuth") == 0)
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pf.PreferenceExists(FlightCrewViewClient.AccessTokenPrefKey))
                {
                    try
                    {
                        var _ = await new FlightCrewViewClient(pf.GetPreferenceForKey<AuthorizationState>(FlightCrewViewClient.AccessTokenPrefKey)).RevokeTokenBasicAuth();
                    }
                    catch (HttpRequestException) { }
                    pf.SetPreferenceForKey(FlightCrewViewClient.AccessTokenPrefKey, null, true);
                }
                return Redirect("ManageFlightCrewView");
            }
            else if (action.CompareOrdinal("auth") == 0)
            {
                new FlightCrewViewClient().Authorize("~/mvc/oauth/flightcrewviewredir".ToAbsoluteURL(Request));
                return new EmptyResult();
            }
            throw new InvalidOperationException("Unknown action: " + action);
        }

        [Authorize]
        public ActionResult ManageFlightCrewView()
        {
            return View("flightcrewviewmanage");
        }
        #endregion

        #region Leon
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageLeon(string action)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (action.CompareOrdinal("deAuth") == 0)
            {
                pf.SetPreferenceForKey(LeonClient.TokenPrefKey, null, true);
                pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, null, true);
                return Redirect(Request.Path);
            }
            else if (action.CompareOrdinal("auth") == 0)
            {
                string subDomain = Request["leonSubDomain"];
                if (String.IsNullOrEmpty(subDomain))
                {
                    ViewBag.error = Resources.LogbookEntry.LeonSubDomainRequired;
                    return View("leonmanage");
                }

                pf.SetPreferenceForKey(LeonClient.SubDomainPrefKey, subDomain, String.IsNullOrEmpty(subDomain));
                new LeonClient(subDomain, LeonClient.UseSandbox(Request.Url.Host)).Authorize("~/mvc/oauth/ManageLeon".ToAbsoluteURL(Request));
                return new EmptyResult();
            }
            throw new InvalidOperationException("Unknown action: " + action);
        }

        [Authorize]
        public ActionResult ManageLeon()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            string szSubDomain = pf.GetPreferenceForKey<string>(LeonClient.SubDomainPrefKey);
            string code = Request["code"];
            if (!String.IsNullOrEmpty(code) && !String.IsNullOrWhiteSpace(szSubDomain))
            {
                IAuthorizationState AuthState = new LeonClient(szSubDomain, LeonClient.UseSandbox(Request.Url.Host)).ConvertToken(Request);
                pf.SetPreferenceForKey(LeonClient.TokenPrefKey, AuthState, AuthState == null);
            }
            return View("leonmanage");
        }
        #endregion

        #region Google Photos
        [Authorize]
        public ActionResult GooglePhotoRedir()
        {
            string szErr = GetStringParam("error");
            if (String.IsNullOrEmpty(szErr))
            {
                IAuthorizationState token = new GooglePhoto().ConvertToken(Request);

                // Remove old token if we successfully converted the new one.
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (pf.PreferenceExists(GooglePhoto.ObsoletePrefKeyAuthToken))
                    pf.SetPreferenceForKey(GooglePhoto.ObsoletePrefKeyAuthToken, null, true);

                pf.SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, token);
            }
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            nvc["pane"] = "social";
            if (!String.IsNullOrEmpty(szErr))
                nvc["oauthErr"] = szErr;
            return Redirect(String.Format(CultureInfo.InvariantCulture, "~/mvc/prefs?{0}", nvc.ToString()));
        }

        [Authorize]
        public ActionResult AuthorizeGPhotoNew()
        {
            new GooglePhoto().Authorize(String.Format(CultureInfo.InvariantCulture, "~/mvc/oAuth/GooglePhotoRedir?{0}=1", GooglePhoto.szParamGPhotoAuth).ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeGPhotoNew()
        {
            MyFlightbook.Profile.GetUser(User.Identity.Name).SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, null, true);
            return Redirect("~/mvc/prefs?pane=social");
        }

        [Authorize]
        public async Task<ActionResult> GooglePhotoPickerSession()
        {
            return await SafeOp(async () =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                GooglePhoto gpn = new GooglePhoto(pf);
                return Json(await gpn.GetSession());
            });
        }
        #endregion

        #region Cloud Storage
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCloudPrefs()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.OverwriteCloudBackup = Request["prefCloudOverwrite"].CompareCurrentCultureIgnoreCase("overwrite") == 0;
            if (Enum.TryParse(Request["prefCloudDefault"], out StorageID sid))
                pf.DefaultCloudStorage = sid;
            pf.FCommit();
            CloudStorageBase.SetUsesFlatHierarchy(pf, Request["prefCloudGroup"] == null);

            return Redirect("~/mvc/prefs?pane=backup");
        }

        #region Dropbox
        [Authorize]
        public ActionResult DeAuthorizeDropbox()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.DropboxAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeDropbox()
        {
            new MFBDropbox().Authorize(Request, "~/mvc/oAuth/DropboxRedir".ToAbsolute(), MFBDropbox.szParamDropboxAuth);
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DropboxRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.DropboxAccessToken = JsonConvert.SerializeObject(new MFBDropbox().ConvertToken(Request));
                pf.FCommit();
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region Box
        [Authorize]
        public ActionResult DeAuthorizeBox()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(BoxDrive.PrefKeyBoxAuthToken, null, true);
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeBox()
        {
            new BoxDrive().Authorize("~/mvc/oAuth/BoxRedir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult BoxRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.SetPreferenceForKey(BoxDrive.PrefKeyBoxAuthToken, new BoxDrive().ConvertToken(Request));
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region GoogleDrive
        [Authorize]
        public ActionResult DeAuthorizeGDrive()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.GoogleDriveAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeGDrive()
        {
            new GoogleDrive().Authorize(Request, "~/mvc/oAuth/GDriveRedir".ToAbsolute(), GoogleDrive.szParamGDriveAuth);
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult GDriveRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (String.IsNullOrEmpty(GetStringParam("error")))
                {
                    pf.GoogleDriveAccessToken = new GoogleDrive().ConvertToken(Request);
                    pf.FCommit();
                }
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion

        #region OneDrive
        [Authorize]
        public ActionResult DeAuthorizeOneDrive()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.OneDriveAccessToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=backup");
        }

        [Authorize]
        public ActionResult AuthorizeOneDrive()
        {
            // Note - can configure onedrive redir at https://apps.dev.microsoft.com/#/application/sapi/{applicationID}
            new OneDrive().Authorize("~/mvc/oauth/onedriveredir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult OneDriveRedir()
        {
            string szErr = GetStringParam("error");
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(szErr))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.OneDriveAccessToken = new OneDrive().ConvertToken(Request);
                pf.FCommit();
            }
            else
            {
                nvc["cloudErr"] = szErr;
            }
            return Redirect("~/mvc/prefs?pane=backup&" + nvc.ToString());
        }
        #endregion
        #endregion

        #region Debriefing
        #region CloudAhoy
        [Authorize]
        public ActionResult AuthorizeCloudAhoy()
        {
            new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).Authorize("~/mvc/oAuth/CloudAhoyRedir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeCloudAhoy()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.CloudAhoyToken = null;
            pf.FCommit();
            return Redirect("~/mvc/prefs?pane=debrief");
        }

        [Authorize]
        public ActionResult CloudAhoyRedir()
        {
            var nvc = HttpUtility.ParseQueryString(string.Empty);
            if (String.IsNullOrEmpty(Request["error"]))
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                pf.CloudAhoyToken = new CloudAhoyClient(!Branding.CurrentBrand.MatchesHost(Request.Url.Host)).ConvertToken(Request);
                pf.FCommit();
            }
            else
                nvc["debriefErr"] = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Request["error"], Request["error_description"] ?? string.Empty);

            return Redirect("~/mvc/prefs?pane=debrief" + (nvc.Count > 0 ? "&" + nvc.ToString() : string.Empty));
        }
        #endregion

        #region FlySto
        [Authorize]
        public ActionResult AuthorizeFlySto()
        {
            new FlyStoClient().Authorize("~/mvc/oauth/flystoredir".ToAbsoluteURL(Request));
            return new EmptyResult();
        }

        [Authorize]
        public ActionResult DeAuthorizeFlySto()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(FlyStoClient.AccessTokenPrefKey, null, true);
            return Redirect("~/mvc/prefs?pane=debrief");
        }

        [Authorize]
        public ActionResult FlyStoRedir()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            pf.SetPreferenceForKey(FlyStoClient.AccessTokenPrefKey, new FlyStoClient().ConvertToken(Request));
            return Redirect("~/mvc/prefs?pane=debrief");
        }
        #endregion
        #endregion

        #region External Maintenance apps
        /// <summary>
        /// Update tach and hobbs times based on flying with any linked external maintenance apps.
        /// MUST BE CALLED FROM LOCAL MACHINE
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [HttpGet]
        public async Task<ActionResult> PushHighWaterMaintenance()
        {
            if (!IsLocalCall())
                throw new UnauthorizedAccessException("Attempt to call PushHighWaterMaintenance from other than localhost");
            return await ExternalMaintenanceRecord.PushHighWaterMarks((id, user) => id.PushHighWaterForUser(user)) ? Content("Success") : Content("Failure");
        }

        #region TachTime
        [Authorize]
        public async Task<ActionResult> TachTimeRedir(string code)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            PKCEPair pkce = TachTimeClient.PendingCodeVerifier(pf);
            IAuthorizationState authorizationState = await new TachTimeClient(Request.Url.Host).ConvertToken(Url.Action("TachTimeRedir", "oAuth", new { area = "mvc" }, Request.Url.Scheme), code, pkce.CodeVerifier);
            pf.SetPreferenceForKey(ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey(), authorizationState);
            return Redirect("~/mvc/Prefs?pane=maint");
        }

        [Authorize]
        public ActionResult TachTimeRevoke()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (!pf.PreferenceExists(ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey()))
                throw new InvalidOperationException("Can't revoke a non-existent authtoken!");
            TachTimeClient.Revoke(User.Identity.Name);
            pf.SetPreferenceForKey(ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey(), null, true);
            return Redirect("~/mvc/Prefs?pane=maint");
        }

        [Authorize]
        public async Task<ActionResult> TachTimeRefresh()
        {
            return await SafeOp(async () =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (!pf.PreferenceExists(ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey()))
                    throw new UnauthorizedAccessException();
                ViewBag.summaryLog = await new TachTimeClient(pf.GetPreferenceForKey<AuthorizationState>(ExternalMaintenanceSourceID.TachTime.TokenPreferenceKey()), Request.Url.Host).UpdateMaintenanceFromTachTime(User.Identity.Name);
                return PartialView("_actionSummary");
            });
        }
        #endregion

        #region MyTailLog
        [Authorize]
        public async Task<ActionResult> MyTailLogRedir(string code)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            PKCEPair pkce = MyTailLogClient.PendingCodeVerifier(pf);
            IAuthorizationState authorizationState = await new MyTailLogClient().ConvertToken(Url.Action("MyTailLogRedir", "oAuth", new { area = "mvc" }, Request.Url.Scheme), code, pkce.CodeVerifier);
            pf.SetPreferenceForKey(ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey(), authorizationState);
            return Redirect("~/mvc/Prefs?pane=maint");
        }

        [Authorize]
        public ActionResult MyTailLogRevoke()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            if (!pf.PreferenceExists(ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey()))
                throw new InvalidOperationException("Can't revoke a non-existent authtoken!");
            MyTailLogClient.Revoke(User.Identity.Name);
            pf.SetPreferenceForKey(ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey(), null, true);
            return Redirect("~/mvc/Prefs?pane=maint");
        }

        [Authorize]
        public async Task<ActionResult> MyTailLogRefresh()
        {
            return await SafeOp(async () =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (!pf.PreferenceExists(ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey()))
                    throw new UnauthorizedAccessException();
                ViewBag.summaryLog = await new MyTailLogClient(pf.GetPreferenceForKey<AuthorizationState>(ExternalMaintenanceSourceID.MyTailLog.TokenPreferenceKey())).UpdateMaintenanceFromMyTailLog(User.Identity.Name);
                return PartialView("_actionSummary");
            });
        }
        #endregion
        #endregion
    }
}