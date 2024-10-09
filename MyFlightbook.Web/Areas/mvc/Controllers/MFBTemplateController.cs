using MyFlightbook.Schedule;
using MyFlightbook.SponsoredAds;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

/******************************************************
    * 
    * Copyright (c) 2022-2024 MyFlightbook LLC
    * Contact myflightbook-at-gmail.com for more information
    *
   *******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class MFBTemplateController : Controller
    {
        private const string szKeyIsNightSession = "IsNightSession";
        protected bool IsNight
        {
            get { return (Session[szKeyIsNightSession] != null && (Boolean)Session[szKeyIsNightSession] == true); }
            set { Session[szKeyIsNightSession] = value; }
        }

        private const string szKeyIsNakedSession = "IsNakedSession";
        protected bool IsNaked
        {
            get { return (Session[szKeyIsNakedSession] != null && (Boolean)Session[szKeyIsNakedSession] == true); }
            set { Session[szKeyIsNakedSession] = value; }
        }

        private static readonly char[] newlineSeparator = new char[] { '\r', '\n' };

        protected static string FixLink(string s)
        {
            return (s == null || !s.StartsWith("~")) ? s : VirtualPathUtility.ToAbsolute(s);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Search(string searchText)
        {
            Response.Redirect(VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/mvc/flights?s={0}", HttpUtility.UrlEncode(searchText))));
            return null;
        }

        [HttpPost]
        public ActionResult SignOut()
        {
            if (User.Identity.IsAuthenticated)
                FormsAuthentication.SignOut();
            Response.Redirect(VirtualPathUtility.ToAbsolute("~/Secure/login.aspx"));
            return null;
        }

        [HttpPost]
        public ActionResult AcceptCookies()
        {
            if (User.Identity.IsAuthenticated)
                MyFlightbook.Profile.GetUser(User.Identity.Name).SetPreferenceForKey(MFBConstants.keyCookiePrivacy, true);

            HttpCookie cookie = new HttpCookie("cookie") { Value = true.ToString(), Expires = DateTime.UtcNow.AddYears(5) };
            Response.Cookies.Add(cookie);

            // Return the cookie to save in the calling browser
            return Json(new { cookie = String.Format(CultureInfo.InvariantCulture, "{0}=true; expires={1}; path=/", MFBConstants.keyCookiePrivacy, cookie.Expires.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'UTC'", CultureInfo.InvariantCulture)) });
        }

        private void AddProfileToViewBag(Profile pf)
        {
            ViewBag.HeadShot = VirtualPathUtility.ToAbsolute(pf.HeadShotHRef);
            ViewBag.Greeting = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LoginStatusWelcome, pf.PreferredGreeting);
            ViewBag.MemberDate = pf.CreationDate.HasValue() ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberSinceShort, pf.CreationDate) : string.Empty;
            ViewBag.LastLogin = (pf.LastLogon.HasValue()) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastLogonShort, pf.LastLogon) : string.Empty;
            ViewBag.LastActivity = pf.LastActivity.Date.CompareTo(pf.LastLogon.Date) != 0 ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastActivityShort, pf.LastActivity) : string.Empty;
        }

        #region shared Partial views
        [ChildActionOnly]
        public ActionResult RenderSponsoredAd(int id)
        {
            ViewBag.sponsoredAd = SponsoredAd.GetAd(id) ?? new SponsoredAd();
            return PartialView("_sponsoredAd");
        }

        [ChildActionOnly]
        public ActionResult RenderTooltip(string tipID, string tipTextHTML, string tipTextSrcID = "", string tipPrompt = "[?]")
        {
            ViewBag.tipPrompt = tipPrompt;
            ViewBag.tipID = tipID;
            ViewBag.tipSrcID = String.IsNullOrEmpty(tipTextSrcID) ? ("idTipFor" + tipID) : tipTextSrcID;
            ViewBag.externalSrc = !String.IsNullOrEmpty(tipTextSrcID);
            ViewBag.tipText = tipTextHTML;
            return PartialView("_tooltip");
        }

        [ChildActionOnly]
        public ActionResult RenderSearchBox(string id, string placeholder, string text = "", string onEnterScript = "")
        {
            ViewBag.searchID = id;
            ViewBag.searchBtnID = id + "btn";
            ViewBag.searchTextID = id + "txt";
            ViewBag.searchPrompt = placeholder;
            ViewBag.searchText = text;
            ViewBag.onEnterScript = onEnterScript;
            return PartialView("_searchbox");
        }

        [ChildActionOnly]
        public ActionResult RenderAccordion(string containerID, string active)
        {
            ViewBag.containerID = containerID;
            ViewBag.active = active;
            return PartialView("_accordion");
        }

        [ChildActionOnly]
        public ActionResult RenderExpandoImg(bool fExpanded, string targetID, string onExpand = null, string onCollapse = null)
        {
            ViewBag.Expanded = fExpanded;
            ViewBag.TargetID = targetID;
            ViewBag.onExpand = String.IsNullOrEmpty(onExpand) ? "null" : onExpand;
            ViewBag.onCollapse = String.IsNullOrEmpty(onCollapse) ? "null" : onCollapse;
            return PartialView("_expandoImg");
        }

        [ChildActionOnly]
        public ActionResult RenderExpandoText(bool fExpanded, string targetID, string expandText = null, string collapseText = null, string labelText = null, string labelClass = null, string onExpand = null, string onCollapse = null)
        {
            ViewBag.Expanded = fExpanded;
            ViewBag.TargetID = targetID;
            ViewBag.CollapseText = collapseText ?? Resources.LocalizedText.ClickToHide;
            ViewBag.ExpandText = expandText ?? Resources.LocalizedText.ClickToShow;
            ViewBag.labelClass = labelClass ?? string.Empty;
            ViewBag.labelText = labelText ?? string.Empty;
            ViewBag.onExpand = String.IsNullOrEmpty(onExpand) ? "null" : onExpand;
            ViewBag.onCollapse = String.IsNullOrEmpty(onCollapse) ? "null" : onCollapse;
            return PartialView("_expandoText");
        }

        private PartialViewResult RenderNumericFieldInternal(EditMode mode, string id, string name, decimal value, bool fRequired, CrossFillDescriptor cfd)
        {
            ViewBag.value = value;
            switch (mode)
            {
                case EditMode.Decimal:
                case EditMode.Currency:
                    ViewBag.placeholder = 0.FormatDecimal(false, true);
                    ViewBag.textValue = (value == 0.0M) ? string.Empty : value.ToString("0.0#", CultureInfo.CurrentCulture);
                    ViewBag.regexp = String.Format(CultureInfo.InvariantCulture, "^\\d*([{0}]\\d*)?$", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                    ViewBag.inputMode = "decimal";
                    break;
                case EditMode.Integer:
                    ViewBag.placeholder = 0.ToString(CultureInfo.CurrentCulture);
                    ViewBag.textValue = (value == 0.0M) ? string.Empty : value.ToString("0", CultureInfo.CurrentCulture);
                    ViewBag.regexp = "^\\d*$";
                    ViewBag.inputMode = "numeric";
                    break;
                case EditMode.HHMMFormat:
                    ViewBag.placeholder = 0.FormatDecimal(true, true);
                    ViewBag.textValue = (value == 0.0M) ? string.Empty : value.FormatDecimal(true);
                    ViewBag.regexp = "^\\d*(:[0-5][\\d])?$";
                    ViewBag.inputMode = "text";
                    break;
            }
            ViewBag.cfd = cfd;
            ViewBag.fRequired = fRequired ? "required" : string.Empty;
            ViewBag.id = id;
            ViewBag.name = name;
            return PartialView("_decimalEdit");
        }

        [ChildActionOnly]
        public ActionResult RenderIntegerField(string id, string name, int value = 0, bool fRequired = false, CrossFillDescriptor cfd = null)
        {
            return RenderNumericFieldInternal(EditMode.Integer, id, name, (decimal)value, fRequired, cfd);
        }

        [ChildActionOnly]
        public ActionResult RenderDecimalField(EditMode mode, string id, string name, decimal value = 0.0M, bool fRequired = false, CrossFillDescriptor cfd = null)
        {
            return RenderNumericFieldInternal(mode, id, name, value, fRequired, cfd);
        }

        [ChildActionOnly]
        public ActionResult RenderDateTimeField(string id, string name, DateTime value, TimeZoneInfo timeZone, bool fAllowNakedTime = true)
        {
            ViewBag.id = id;
            ViewBag.name = name;
            ViewBag.value = value;
            ViewBag.timeZone = timeZone;
            ViewBag.fAllowNakedTime = fAllowNakedTime;
            return PartialView("_dateTime");
        }

        [ChildActionOnly]
        public ActionResult RenderDateField(string id, string name, DateTime value, bool fRequired = false)
        {
            ViewBag.id = id; 
            ViewBag.name = name; 
            ViewBag.value = value; 
            ViewBag.fRequired = fRequired; 
            return PartialView("_dateEdit");
        }

        [ChildActionOnly]
        public ActionResult RenderGoogleAd(bool fVertical)
        {
            ViewBag.Vertical = fVertical;
            return PartialView("_googleAd");
        }
        #endregion

        [ChildActionOnly]
        public ActionResult RenderPrivacyLink()
        {
            if (IsNaked)
                return Content(string.Empty);

            Profile pf = User.Identity.IsAuthenticated ? MyFlightbook.Profile.GetUser(User.Identity.Name) : null;
            bool fDeclineCookies = int.TryParse(Request?["declinecookie"] ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out int declinecookies) && declinecookies != 0;
            if (fDeclineCookies)
            {
                Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.UtcNow.AddDays(-5);
                pf?.SetPreferenceForKey(MFBConstants.keyCookiePrivacy, false, true);
            }

            bool fHasCookie = Request.Cookies[MFBConstants.keyCookiePrivacy] != null && Request.Cookies[MFBConstants.keyCookiePrivacy].Value.CompareOrdinalIgnoreCase(true.ToString()) == 0;

            // Need a cookie if you've neither accepted them in a cookie nor in your profile.
            bool fNeedsCookieOK = fDeclineCookies || (!fHasCookie && (pf == null || !pf.GetPreferenceForKey<bool>(MFBConstants.keyCookiePrivacy, false)));

            return fNeedsCookieOK ? PartialView("_privacyfooter") : (ActionResult) Content(string.Empty);
        }

        [ChildActionOnly]
        public ActionResult RenderFooter()
        {
            if (IsNaked)
                return Content(string.Empty);

            IReadOnlyDictionary<Brand.FooterLinkKey, BrandLink> d;
            ViewBag.FooterLinks = d = Branding.CurrentBrand.FooterLinks();

            // Fix up any relative links
            foreach (BrandLink bl in d.Values)
            {
                bl.LinkRef = FixLink(bl.LinkRef);
                bl.ImageRef = FixLink(bl.ImageRef);
            }
            // Be sure to use the correct httpcontext.current request - otherwise you get an httprequestbase, which doesn't work.
            return PartialView(System.Web.HttpContext.Current.Request.IsMobileSession() ? "_footermobile" : "_footer");
        }

        [ChildActionOnly]
        public ActionResult RenderImpersonation()
        {
            ViewBag.ImpersonatingUser = ProfileRoles.OriginalAdmin;
            ViewBag.IsImpersonating = ProfileRoles.IsImpersonating(User.Identity.Name);
            return PartialView("_impersonation");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult StopImpersonation()
        {
            ProfileRoles.StopImpersonating();
            return Redirect("~/mvc/AdminUser");
        }

        [ChildActionOnly]
        public ActionResult RenderHeader(tabID selectedTab = tabID.tabHome)
        {
            if (IsNaked)
                return Content(string.Empty);

            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.TabHTML = TabList.CurrentTabList("~/NavLinks.xml").WriteTabsHtml(Request != null && Request.UserAgent != null && Request.UserAgent.ToUpper(CultureInfo.CurrentCulture).Contains("ANDROID"),
                pf.Role, selectedTab);

            AddProfileToViewBag(pf);

            // see if we need to show an upcoming event; we repurpose a known GUID for this.  
            // If it's in the database AND in the future, we show it.
            // Since header is loaded on every page load, cache it, using a dummy expired one if there was none.
            ScheduledEvent se = (ScheduledEvent)System.Web.HttpContext.Current.Cache["upcomingWebinar"];
            if (se == null)
            {
                se = ScheduledEvent.AppointmentByID("00000000-fe32-5932-bef8-000000000001", TimeZoneInfo.Utc);
                if (se == null)
                    se = new ScheduledEvent() { EndUtc = DateTime.Now.AddDays(-2) };
                System.Web.HttpContext.Current.Cache.Add("upcomingWebinar", se, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.Default, null);
            }
            if (se != null && DateTime.UtcNow.CompareTo(se.EndUtc) < 0)
            {
                string[] rgLines = se.Body.Split(newlineSeparator, StringSplitOptions.RemoveEmptyEntries);
                ViewBag.WebinarText = String.Format(CultureInfo.CurrentCulture, "Join \"{0}\" on {1}", (rgLines == null || rgLines.Length == 0) ? string.Empty : rgLines[0], se.LocalStart.ToShortDateString()).Linkify();
                ViewBag.WebinarDetails = se.Body.Linkify(true);
            }

            return PartialView("_header");
        }

        [ChildActionOnly]
        public ActionResult RenderHead(string Title, bool UseCharting = false, bool UseMaps = false, bool AddBaseRef = false)
        {
            int idBrand = util.GetIntParam(Request, "bid", -1);
            if (idBrand >= 0 && idBrand < Enum.GetNames(typeof(BrandID)).Length)
                Branding.CurrentBrandID = (BrandID)idBrand;

            // Handle parameters here.
            if (ShuntState.IsShunted && String.IsNullOrEmpty(Request["noshunt"]))
            {
                Response.Redirect(VirtualPathUtility.ToAbsolute("~/mvc/pub/shunt"));
                Response.End();
                return null;
            }
            // if "m=no" is passed in, override mobile detection and force classic view
            if (util.GetStringParam(Request, "m") == "no")
                util.SetMobile(false);

            if (Request.Url.GetLeftPart(UriPartial.Path).Contains("/wp-includes"))
                throw new HttpException(404, "Why are you probing me for wordpress, you jerks?");

            if (User.Identity.IsAuthenticated)
                Session[User.Identity.Name + "-LastPage"] = String.Format(CultureInfo.InvariantCulture, "{0}:{1}{2}", Request.IsSecureConnection ? "https" : "http", Request.Url.Host, Request.Url.PathAndQuery);
            else
                ProfileRoles.StopImpersonating();

            int naked = util.GetIntParam(Request, "naked", 0);
            if (naked == -1) // kill naked
                Session["IsNaked"] = IsNaked = false;
            else
                IsNaked = naked > 0 || IsNaked || (Session["IsNaked"] != null && ((bool)Session["IsNaked"]) == true);

            // Now do all of the main header stuff.
            ViewBag.Title = Title ?? Branding.CurrentBrand.AppName;
            ViewBag.UseCharting = UseCharting;
            ViewBag.UseMaps = UseMaps;
            ViewBag.ForceNaked = false; // provide a default value but allow pages to override the session state

            string szUserAgent = Request.UserAgent?.ToUpperInvariant() ?? string.Empty;
            ViewBag.IsIOSOrAndroid = szUserAgent.Contains("IPHONE") || szUserAgent.Contains("IPAD") || szUserAgent.Contains("ANDROID");

            // We're going to set IsNight explicitly if it's in the url, but otherwise use the session object.
            string nightRequest = util.GetStringParam(System.Web.HttpContext.Current.Request, "night");
            if (nightRequest.CompareCurrentCultureIgnoreCase("yes") == 0)
                IsNight = true;
            else if (nightRequest.CompareCurrentCultureIgnoreCase("no") == 0)
                IsNight = false;
            ViewBag.NightCSS = IsNight ? VirtualPathUtility.ToAbsolute(MFBConstants.BaseNightStylesheet) : string.Empty;
            ViewBag.BrandCSS = String.IsNullOrEmpty(Branding.CurrentBrand.StyleSheet) ? String.Empty : VirtualPathUtility.ToAbsolute(Branding.CurrentBrand.StyleSheet) + "?v=1";
            ViewBag.MobileCSS = System.Web.HttpContext.Current.Request.IsMobileSession() ? VirtualPathUtility.ToAbsolute("~/Public/CSS/MobileSheet.css?v=8") : string.Empty;

            ViewBag.BaseRef = AddBaseRef ? Request.Url.GetLeftPart(UriPartial.Authority) : null;
            
            return PartialView("_templatehead");
        }

        // GET: mvc/MFBTemplate
        public ActionResult Index()
        {
            return View();
        }
    }
}