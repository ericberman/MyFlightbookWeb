using DotNetOpenAuth.OAuth2;
using MyFlightbook.OAuth.RosterBuster;
using QRCoder;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Controller for oAuth connection to RosterBuster
    /// </summary>
    public class rbredirectController : Controller
    {
        protected string RedirLink
        {
            get { return Request.Url.GetLeftPart(UriPartial.Path); }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize]
        public ActionResult DeAuth()
        {
            MyFlightbook.Profile.GetUser(User.Identity.Name).SetPreferenceForKey(RosterBusterClient.TokenPrefKey, null, true);
            Response.Redirect(VirtualPathUtility.ToAbsolute("~/mvc/rbredirect"));
            return null;
        }

        [Authorize]
        public async Task<ActionResult> Index()
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            string szCode = Request["code"] ?? string.Empty;
            string szState = Request["state"];
            ViewBag.Title = Resources.LogbookEntry.RosterBusterHeader;

            szState = String.IsNullOrEmpty(szState) && pf.AssociatedData.TryGetValue(RosterBusterClient.szCachedPrefStateKey, out object o)
                ? (string)o
                : string.Empty;

            ViewBag.AuthError = string.Empty;

            // See if we are being redirected back or need to initiate
            if (!String.IsNullOrEmpty(szCode) && !String.IsNullOrEmpty(szState) && !pf.PreferenceExists(RosterBusterClient.TokenPrefKey)) // BUG/TODO: They are not passing back state on successful auth!
            {
                try
                {
                    IAuthorizationState authState = await new RosterBusterClient(Request.Url.Host).ConvertToken(RedirLink, szCode, szState, User.Identity.Name);
                    pf.SetPreferenceForKey(RosterBusterClient.TokenPrefKey, authState, false);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ViewBag.AuthError = ex.Message;
                }
            }

            bool isAuthed = pf.PreferenceExists(RosterBusterClient.TokenPrefKey);
            if (!isAuthed)
                ViewBag.AuthUri = new RosterBusterClient(Request.Url.Host).AuthorizationUri(RedirLink, User.Identity.Name);

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(RosterBusterClient.RBDownloadLink, QRCodeGenerator.ECCLevel.Q))
                {
                    using (Base64QRCode qrCode = new Base64QRCode(qrCodeData))
                    {
                        ViewBag.QRCode = String.Format(CultureInfo.InvariantCulture, "data:image/png;base64,{0}", qrCode.GetGraphic(20));
                        ViewBag.RBDownload = RosterBusterClient.RBDownloadLink;
                    }
                }
            }

            return View(isAuthed ? "rbAuthorized" : "rbNotAuthorized");
        }
    }
}