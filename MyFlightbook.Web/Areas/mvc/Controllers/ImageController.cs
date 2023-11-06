using MyFlightbook.Clubs;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class ImageController : Controller
    {
        #region image widgets
        [ChildActionOnly]
        public ActionResult ImageSlider(IEnumerable<MFBImageInfo> rgimages, string id)
        {
            if (rgimages.Any())
            {
                ViewBag.sliderID = id;
                ViewBag.images = rgimages;
                return PartialView("_imgSlider");
            }
            else
                return new EmptyResult();
        }
        #endregion

        #region displaying of images
        [Authorize]
        public ActionResult PendingImage(string id, int full = 0)
        {
            string szKey = id;
            MFBPendingImage mfbpb;
            if (!String.IsNullOrEmpty(szKey) && ((mfbpb = (MFBPendingImage)Session[szKey]) != null))
            {
                if (mfbpb.ImageType == MFBImageInfoBase.ImageFileType.PDF)
                    return File(mfbpb.PostedFile.TempFileName, "application/pdf");
                else if (mfbpb.ImageType == MFBImageInfoBase.ImageFileType.JPEG)
                    return File(full != 0 ? mfbpb.PostedFile.CompatibleContentData() : mfbpb.PostedFile.ThumbnailBytes(), "image/jpeg");
            }
            return Redirect("~/Images/x.gif");
        }

        [Authorize]
        public ActionResult ViewUser(string id)
        {
            string szUser = id;

            if (!String.IsNullOrEmpty(szUser) && User.Identity.IsAuthenticated)
            {
                // Two scenarios where we're allowed to see the image:
                // (a) we're the user or 
                // (b) we are both in the same club.
                // ClubMember.ShareClub returns true for both of these.
                if (ClubMember.CheckUsersShareClub(User.Identity.Name, szUser, true, false))
                {
                    Profile pf = MyFlightbook.Profile.GetUser(szUser);
                    if (pf.HasHeadShot)
                        return File(pf.HeadShot.ToArray(), "image/jpeg");
                }
            }
            return File(Server.MapPath("~/Public/tabimages/ProfileTab.png"), "image/png");
        }

        public ActionResult ViewPic(string r, string k, string t)
        {
            if (Enum.TryParse(r, out MFBImageInfoBase.ImageClass ic) && !String.IsNullOrEmpty(k) && !String.IsNullOrEmpty(t) && !t.Contains("?"))  // Googlebot seems to be adding "?resize=300,300
            {
                MFBImageInfo mfbii = new MFBImageInfo(ic, k, t);
                return (mfbii == null) ? (ActionResult)HttpNotFound() : Redirect(mfbii.ResolveFullImage());
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        public ActionResult FlightSig(int id)
        {
            if (id == LogbookEntryCore.idFlightNew)
                return HttpNotFound();

            LogbookEntry le = new LogbookEntry { FlightID = id };
            le.LoadDigitalSig();

            byte[] sig = le.GetDigitizedSignature();

            return (sig != null && sig.Length > 0) ? (ActionResult)File(sig, "image/png") : HttpNotFound();
        }
        #endregion

        // GET: mvc/Image
        public ActionResult Index()
        {
            // Should never call this directly.
            return HttpNotFound();
        }
    }
}