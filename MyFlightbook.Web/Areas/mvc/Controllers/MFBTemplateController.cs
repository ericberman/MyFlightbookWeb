using MyFlightbook;
using MyFlightbook.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

/******************************************************
    * 
    * Copyright (c) 2022 MyFlightbook LLC
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

        protected string FixLink(string s)
        {
            return (s == null || !s.StartsWith("~")) ? s : VirtualPathUtility.ToAbsolute(s);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Search(string searchText)
        {
            Response.Redirect(VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/Logbooknew.aspx?s={0}", HttpUtility.HtmlEncode(searchText))));
            return null;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult SignOut()
        {
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
            ViewBag.Greeting = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LoginStatusWelcome, HttpUtility.HtmlEncode(pf.PreferredGreeting));
            ViewBag.MemberDate = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberSinceShort, pf.CreationDate);
            ViewBag.LastLogin = (pf.LastLogon.HasValue()) ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastLogonShort, pf.LastLogon) : String.Empty;
            ViewBag.LastActivity = pf.LastActivity.Date.CompareTo(pf.LastLogon.Date) != 0 ? String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastActivityShort, pf.LastActivity) : String.Empty;
        }

        [ChildActionOnly]
        public ActionResult RenderPrivacyLink()
        {
            Profile pf = User.Identity.IsAuthenticated ? MyFlightbook.Profile.GetUser(User.Identity.Name) : null;
            bool fDeclineCookies = int.TryParse(Request?["declinecookie"] ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out int declinecookies) && declinecookies != 0;
            if (fDeclineCookies)
            {
                Response.Cookies[MFBConstants.keyCookiePrivacy].Expires = DateTime.UtcNow.AddDays(-5);
                if (pf != null)
                    pf.SetPreferenceForKey(MFBConstants.keyCookiePrivacy, false, true);
            }

            bool fHasCookie = Request.Cookies[MFBConstants.keyCookiePrivacy] != null && Request.Cookies[MFBConstants.keyCookiePrivacy].Value.CompareOrdinalIgnoreCase(true.ToString()) == 0;

            // Need a cookie if you've neither accepted them in a cookie nor in your profile.
            bool fNeedsCookieOK = fDeclineCookies || (!fHasCookie && (pf == null || !pf.GetPreferenceForKey<bool>(MFBConstants.keyCookiePrivacy, false)));

            return fNeedsCookieOK ? PartialView("_privacyfooter") : (ActionResult) Content(string.Empty);
        }

        [ChildActionOnly]
        public ActionResult RenderFooter()
        {
            IDictionary<Brand.FooterLinkKey, BrandLink> d;
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
            Response.Redirect(VirtualPathUtility.ToAbsolute("~/Admin/Admin.aspx"));
            return null;
        }

        [ChildActionOnly]
        public ActionResult RenderHeader(tabID selectedTab = tabID.tabHome)
        {
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
                string[] rgLines = se.Body.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                ViewBag.WebinarText = String.Format(CultureInfo.CurrentCulture, "Join \"{0}\" on {1}", (rgLines == null || rgLines.Length == 0) ? string.Empty : rgLines[0], se.EndUtc.ToShortDateString()).Linkify();
                ViewBag.WebinarDetails = se.Body.Linkify(true);
            }

            return PartialView("_header");
        }

        [ChildActionOnly]
        public ActionResult RenderHead(string Title)
        {
            ViewBag.Title = Title;

            string szUserAgent = Request.UserAgent.ToUpperInvariant();
            ViewBag.IsIOSOrAndroid = szUserAgent.Contains("IPHONE") || szUserAgent.Contains("IPAD") || szUserAgent.Contains("ANDROID");

            // We're going to set IsNight explicitly if it's in the url, but otherwise use the session object.
            string nightRequest = util.GetStringParam(System.Web.HttpContext.Current.Request, "night");
            if (nightRequest.CompareCurrentCultureIgnoreCase("yes") == 0)
                IsNight = true;
            else if (nightRequest.CompareCurrentCultureIgnoreCase("no") == 0)
                IsNight = false;
            ViewBag.NightCSS = IsNight ? VirtualPathUtility.ToAbsolute("~/Public/CSS/NightMode.css?v=563nh4h") : string.Empty;
            ViewBag.BrandCSS = String.IsNullOrEmpty(Branding.CurrentBrand.StyleSheet) ? String.Empty : VirtualPathUtility.ToAbsolute(Branding.CurrentBrand.StyleSheet);
            ViewBag.MobileCSS = System.Web.HttpContext.Current.Request.IsMobileSession() ? VirtualPathUtility.ToAbsolute("~/Public/CSS/MobileSheet.css?v=8") : string.Empty;
            
            return PartialView("_templatehead");
        }

        // GET: mvc/MFBTemplate
        public ActionResult Index()
        {
            return View();
        }
    }
}