using ImageMagick;
using MyFlightbook.Clubs;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class ImageController : AdminControllerBase
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

        #region Web Services
        [Authorize]
        [HttpPost]
        public ActionResult DeleteImage(MFBImageInfoBase.ImageClass ic, string key, string szThumb, bool fAsAdmin = false)
        {
            return SafeOp(() =>
            {
                // First check if this is a pending image
                // No need to do admin check on something that has yet to actually be committed
                MFBPendingImage pf = new List<MFBPendingImage>(MFBPendingImage.PendingImagesInSession(Session)).FirstOrDefault(mfbpf => mfbpf.ThumbnailFile.CompareOrdinal(szThumb) == 0);
                if (pf != null)
                    pf.DeleteImage();
                else
                {
                    MFBImageInfo mfbii = MFBImageInfo.LoadMFBImageInfo(ic, key, szThumb);

                    // Check ownership.
                    bool fCanActAsAdmin = (fAsAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData);
                    if (!fCanActAsAdmin)
                        ImageAuthorization.ValidateAuth(mfbii, User.Identity.Name, ImageAuthorization.ImageAction.Delete, true);
                    mfbii.DeleteImage();
                }
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AnnotateImage(MFBImageInfoBase.ImageClass ic, string key, string szThumb, string newAnnotation, bool fAsAdmin = false)
        {
            return SafeOp(() =>
            {
                // First check if this is a pending image
                // No need to do admin check on something that has yet to actually be committed
                MFBPendingImage pf = new List<MFBPendingImage>(MFBPendingImage.PendingImagesInSession(Session)).FirstOrDefault(mfbpf => mfbpf.ThumbnailFile.CompareOrdinal(szThumb) == 0);
                if (pf != null)
                    pf.UpdateAnnotation(newAnnotation);
                else
                {
                    MFBImageInfo mfbii = MFBImageInfo.LoadMFBImageInfo(ic, key, szThumb);

                    // Check ownership.
                    bool fCanActAsAdmin = (fAsAdmin && MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData);
                    if (!fCanActAsAdmin)
                        ImageAuthorization.ValidateAuth(mfbii, User.Identity.Name, ImageAuthorization.ImageAction.Annotate);

                    mfbii.UpdateAnnotation(newAnnotation);
                }
                return new EmptyResult();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageClass"></param>
        /// <param name="szKey"></param>
        /// <param name="altText"></param>
        /// <param name="fCanDelete"></param>
        /// <param name="fCanEdit"></param>
        /// <param name="fCanMakeDefault"></param>
        /// <param name="zoomLinkType"></param>
        /// <param name="fIsDefault"></param>
        /// <param name="confirmText"></param>
        /// <param name="defaultImage"></param>
        /// <param name="onMakeDefault"></param>
        /// <param name="onDelete"></param>
        /// <param name="onAnnotate"></param>
        /// <param name="fIncludeDocs"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult ImagesForClassAndKey(MFBImageInfoBase.ImageClass imageClass, string szKey, string altText = "", bool fCanDelete = false, bool fCanEdit = false, bool fCanMakeDefault = false, GeoLinkType zoomLinkType = GeoLinkType.None, string confirmText = "", string defaultImage = "", string onMakeDefault = "", string onDelete = "", string onAnnotate = "", bool fIncludeDocs = false)
        {
            return SafeOp(() =>
            {
                ImageList il = null;
                // Pending Images are special - find them in the session object
                if ((imageClass == MFBImageInfoBase.ImageClass.Flight || imageClass == MFBImageInfoBase.ImageClass.Aircraft || imageClass == MFBImageInfoBase.ImageClass.BasicMed) && szKey.CompareOrdinal((-1).ToString(CultureInfo.InvariantCulture)) == 0)
                    il = new ImageList(MFBPendingImage.PendingImagesInSession(Session).ToArray()) { Class = imageClass };
                else
                {
                    il = new ImageList(imageClass, szKey);
                    il.Refresh(fIncludeDocs);
                }
                return ImageListDisplay(il, altText, fCanDelete, fCanEdit, fCanMakeDefault, zoomLinkType, confirmText, defaultImage, onMakeDefault, onDelete, onAnnotate);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult ImageDebug()
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("Invalid Image");
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<hr /><div style='font-weight: bold;'>File: {0}</div>", file.FileName);
                    try
                    {
                        using (IMagickImage image = new MagickImage(file.InputStream))
                        {
                            IExifProfile exif = image.GetExifProfile();

                            if (exif == null)
                                continue;

                            // Write all values to the console
                            foreach (IExifValue value in exif.Values)
                            {
                                if (value.IsArray)
                                {
                                    List<string> lst = new List<string>();
                                    object o = value.GetValue();

                                    if (o is byte[] rgbyte)
                                    {
                                        foreach (byte b in rgbyte)
                                            lst.Add(b.ToString("X", CultureInfo.InvariantCulture));
                                    }
                                    else if (o is ushort[] rgshort)
                                    {
                                        foreach (ushort u in rgshort)
                                            lst.Add(u.ToString(CultureInfo.InvariantCulture));
                                    }
                                    else if (o is Rational[] rgrational)
                                    {
                                        foreach (Rational r in rgrational)
                                            lst.Add(r.ToString(CultureInfo.InvariantCulture));
                                    }
                                    else
                                        lst.Add(o.ToString());

                                    sb.AppendFormat(CultureInfo.CurrentCulture, "<div>{0}({1}): [{2}]</div>", value.Tag, value.DataType, String.Join(", ", lst));

                                }
                                else
                                    sb.AppendFormat(CultureInfo.CurrentCulture, "<div>{0}({1}): {2}</div>", value.Tag, value.DataType, value.ToString());
                            }
                        }
                    }
                    catch (MagickException ex)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "<div class='error'>{0}</div>", ex.Message);
                    }
                }

                return Content(sb.ToString());
            });
        }
        #endregion

        #region displaying of images
        [ChildActionOnly]
        public ActionResult ImageListDisplay(ImageList il, string altText = "", bool fCanDelete = false, bool fCanEdit = false, bool fCanMakeDefault = false, GeoLinkType zoomLinkType = GeoLinkType.None, string confirmText = "", string defaultImage = "", string onMakeDefault = "", string onDelete = "", string onAnnotate = "")
        {
            if (il == null)
                throw new ArgumentNullException(nameof(il));
            ViewBag.rgmfbii = il.ImageArray;
            ViewBag.altText = altText;
            ViewBag.fCanDelete = fCanDelete;
            ViewBag.fCanEdit = fCanEdit;
            ViewBag.fCanMakeDefault = fCanMakeDefault;
            ViewBag.zoomLinkType = zoomLinkType;
            ViewBag.confirmText = confirmText;
            ViewBag.defaultImage = defaultImage;
            ViewBag.onDelete = onDelete;
            ViewBag.onAnnotate = onAnnotate;
            ViewBag.onMakeDefault = onMakeDefault;

            return PartialView("_imageList");
        }

        [ChildActionOnly]
        public ActionResult EditableImage(MFBImageInfoBase mfbii, string altText = "", bool fCanDelete = false, bool fCanEdit = false, bool fCanMakeDefault = false, GeoLinkType zoomLinkType = GeoLinkType.None, bool fIsDefault = false, string confirmText = "", string onMakeDefault = "", string onDelete = "", string onAnnotate = "")
        {
            ViewBag.mfbii = mfbii;

            ViewBag.altText = altText;
            ViewBag.fCanDelete = fCanDelete;
            ViewBag.fCanEdit = fCanEdit;
            ViewBag.fCanMakeDefault = fCanMakeDefault;
            ViewBag.zoomLinkType = zoomLinkType;
            ViewBag.fIsDefault = fIsDefault;
            ViewBag.confirmText = confirmText;
            ViewBag.onDelete = onDelete;
            ViewBag.onAnnotate = onAnnotate;
            ViewBag.onMakeDefault = onMakeDefault;
            return PartialView("_editableImage");
        }

        [ChildActionOnly]
        public ActionResult HoverImageList(IEnumerable<MFBImageInfo> rgmfbii, string imageClasses = null)
        {
            ViewBag.images = rgmfbii;
            ViewBag.imageClasses = imageClasses;
            return PartialView("_hoverImageList");
        }

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

        public ActionResult ViewUserWithKey(string id, string key)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            long utcNow = DateTime.UtcNow.Ticks;
            bool fKey = (long.TryParse(new Encryptors.SharedDataEncryptor().Decrypt(key ?? string.Empty), out long ticks) && utcNow - ticks > 0 && TimeSpan.FromTicks(utcNow - ticks).TotalMinutes < 5);

            Profile pf = MyFlightbook.Profile.GetUser(id);
            return fKey && pf.HasHeadShot ? (ActionResult) File(pf.HeadShot.ToArray(), "image/jpeg") : File(Server.MapPath("~/Public/tabimages/ProfileTab.png"), "image/png");


        }

        public ActionResult ViewPic(string r, string k, string t)
        {
            // issue #1389 - seeing a bunch of requests that include an encoded ampersand.  If we see that, redirect to notfound
            if (t?.Contains('\\') ?? true)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

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