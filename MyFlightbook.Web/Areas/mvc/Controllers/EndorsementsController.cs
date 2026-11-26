using MyFlightbook.CSV;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static MyFlightbook.Instruction.Endorsement;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    /// <summary>
    /// Web services and partial views for endorsements (digital, offline, and image endorsements).
    /// The user-visible endorsement pages (Endorsements, AddEndorsement, EndorseStudent, Students) remain in TrainingController, so their URLs are unchanged.
    /// </summary>
    public class EndorsementsController : AdminControllerBase
    {
        #region Web services
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public async Task<ActionResult> UploadEndorsement(string szKey = null)
        {
            return await SafeOp(async () =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                MFBImageInfo mfbii = await new MFBImageInfo(MFBImageInfoBase.ImageClass.Endorsement, szKey ?? User.Identity.Name).InitWithFile(new MFBPostedFile(Request.ImageFile(0)), Request.ImageFile(0).FileName ?? string.Empty, null);
                return Content(mfbii.URLThumbnail);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public async Task<ActionResult> UploadOfflineEndorsement()
        {
            return await SafeOp(async () =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                MFBImageInfo mfbii = await new MFBImageInfo(MFBImageInfoBase.ImageClass.OfflineEndorsement, User.Identity.Name).InitWithFile(new MFBPostedFile(Request.ImageFile(0)), Request.ImageFile(0).FileName ?? string.Empty, null);  // preserve the filename as a hint for reading the image
                return Content(mfbii.URLThumbnail);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteExternalEndorsement(int id)
        {
            return SafeOp(() =>
            {
                List<Endorsement> rgEndorsements = new List<Endorsement>(EndorsementsForUser(null, User.Identity.Name));

                Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id) ?? throw new MyFlightbookException("ID of endorsement to delete is not found in owners endorsements");
                if (en.StudentType == StudentTypes.Member)
                    throw new MyFlightbookException(Resources.SignOff.errCantDeleteMemberEndorsement);

                if (en.IsImageEndorsement)
                    ImageEndorsementManager.DeleteImageEndorsement(en);
                else
                    en.FDelete();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteOwnedEndorsement(int id)
        {
            return SafeOp(() =>
            {
                List<Endorsement> rgEndorsements = new List<Endorsement>(EndorsementsForUser(User.Identity.Name, null));
                Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id) ?? throw new MyFlightbookException("Can't find endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));
                if (en.StudentType == StudentTypes.External)
                    throw new MyFlightbookException("Can't delete external endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));
                if (en.StudentType == StudentTypes.Member && en.StudentName.CompareCurrentCulture(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();

                if (en.IsImageEndorsement)
                    ImageEndorsementManager.DeleteImageEndorsement(en);
                else
                    en.FDelete();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult UpdateCategoryForEndorsement(int idEndorsement, EndorsementCategory newCategory)
        {
            return SafeOp(() =>
            {
                if (!Enum.IsDefined(typeof(EndorsementCategory), newCategory))
                    throw new ArgumentOutOfRangeException(nameof(newCategory));
                Endorsement e = EndorsementWithID(idEndorsement) ?? throw new InvalidOperationException("Can't find endorsement with ID=" + idEndorsement.ToString(CultureInfo.InvariantCulture));
                if (e.StudentType == StudentTypes.Member && e.StudentName.CompareCurrentCultureIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();
                if (e.StudentType == StudentTypes.External && e.InstructorName.CompareCurrentCultureIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();
                e.FUpdateCategory(newCategory, CategorySource.User);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult GetEndorsementTemplate(string sourceUser, string targetUser, int idTemplate, StudentTypes studentType, EndorsementMode mode, int idSrc = -1)
        {
            return SafeOp(() =>
            {
                return RenderEndorsementBody(sourceUser, targetUser, idTemplate, studentType, mode, idSrc);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddEndorsementForStudent()
        {
            return SafeOp(() =>
            {
                Endorsement endorsement;

                if (!Enum.TryParse(Request["endorsementMode"], out EndorsementMode mode))
                    throw new InvalidOperationException("Invalid endorsement mode");

                DateTime? dtCFIExp = null;
                if (DateTime.TryParse(Request["cfiCertExpiration"], CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime cfiExp))
                    dtCFIExp = cfiExp;

                switch (mode)
                {
                    default:
                    case EndorsementMode.InstructorOfflineStudent:
                    case EndorsementMode.InstructorPushAuthenticated:
                        {
                            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                            endorsement = new Endorsement(User.Identity.Name) { CFICertificate = pf.Certificate, CFIExpirationDate = pf.CertificateExpiration };
                            if (!pf.CertificateExpiration.HasValue() && dtCFIExp.HasValue)
                            {
                                endorsement.CFIExpirationDate = pf.CertificateExpiration = cfiExp.Date;
                                pf.FCommit();   // save it to the profile.
                            }
                        }
                        break;
                    case EndorsementMode.StudentPullAdHoc:
                        endorsement = new Endorsement(string.Empty) { CFICachedName = Request["cfiName"], CFICertificate = Request["cfiCert"], CFIExpirationDate = dtCFIExp ?? DateTime.MinValue };
                        string base64Data = Request["hdnSigData"];
                        byte[] rgbSig = String.IsNullOrEmpty(base64Data) ? null : ScribbleImage.FromDataLinkURL(base64Data);

                        endorsement.SetDigitizedSig(rgbSig);
                        if (endorsement.GetDigitizedSig() == null)
                            throw new InvalidOperationException(Resources.SignOff.errScribbleRequired);
                        break;
                    case EndorsementMode.StudentPullAuthenticated:
                        Profile pfSource = MyFlightbook.Profile.GetUser(Request["sourceUser"]);
                        if (pfSource == null || !System.Web.Security.Membership.ValidateUser(pfSource.UserName, Request["instructorPass"]))
                            throw new UnauthorizedAccessException(Resources.SignOff.errInstructorBadPassword);

                        endorsement = new Endorsement(pfSource.UserName) { CFICertificate = pfSource.Certificate, CFIExpirationDate = pfSource.CertificateExpiration };
                        break;
                }

                endorsement.Date = DateTime.Parse(Request["endorsementDate"], CultureInfo.CurrentCulture);
                endorsement.EndorsementText = Request["compiledBody"];
                endorsement.StudentType = (StudentTypes)Enum.Parse(typeof(StudentTypes), Request["studentType"]);
                endorsement.StudentName = Request["studentName"];
                endorsement.Title = Request["endorsementTitle"];
                endorsement.FARReference = Request["endorsementFAR"];
                endorsement.SourceTemplateID = Convert.ToInt32(Request["templateID"], CultureInfo.InvariantCulture);
                if (endorsement.SourceTemplateID > 0)
                {
                    EndorsementType et = EndorsementType.GetEndorsementByID(endorsement.SourceTemplateID);
                    if ((et?.DefaultCategory ?? EndorsementCategory.None) != EndorsementCategory.None)
                    {
                        endorsement.Category = et.DefaultCategory;
                        endorsement.Source = CategorySource.Template;
                    }
                }
                endorsement.FCommit();

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult TemplatesMatchingTerm(string searchText)
        {
            return SafeOp(() =>
            {
                List<EndorsementType> lst = new List<EndorsementType>(EndorsementType.LoadTemplates(searchText));
                if (lst.Count == 0) // if nothing found, use the custom template
                    lst.Add(EndorsementType.GetEndorsementByID(1));
                return Json(lst);
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult EndorsementEditorForUserAddEndorsement()
        {
            return SafeOp(() =>
            {
                string instructor = Request["selectedInstructor"];
                if (String.IsNullOrEmpty(instructor) && Request["acceptSelfSign"] == null)
                    throw new UnauthorizedAccessException(Resources.SignOff.errAcceptDisclaimer);

                EndorsementMode mode = String.IsNullOrEmpty(instructor) ? EndorsementMode.StudentPullAdHoc : EndorsementMode.StudentPullAuthenticated;
                return RenderEndorsementEditor(instructor, User.Identity.Name, StudentTypes.Member, mode);
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestEndorsement(string student, string instructor, int tid)
        {
            return SafeOp(() =>
            {
                InstructorStudent.RequestEndorsement(student, instructor, tid);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadEndorsements(string userName = null)
        {
            IEnumerable<Endorsement> endorsements = EndorsementsForUser(userName, User.Identity.Name, true);
            using (DataTable dt = new DataTable() { Locale = CultureInfo.CurrentCulture })
            {
                EndorsementsToDataTable(endorsements, dt);
                string szFilename = RegexUtility.UnSafeFileChars.Replace(String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.SignOff.DownloadEndorsementsFilename, String.IsNullOrEmpty(userName) ? Resources.SignOff.DownloadEndorsementsAllStudents : MyFlightbook.Profile.GetUser(userName).UserFullName), string.Empty) + ".csv";
                return File(CsvWriter.WriteToBytes(dt, true, true), "text/csv", szFilename);
            }
        }
        #endregion

        #region Image endorsements
        private static void CheckEndorsementImageClass(MFBImageInfoBase.ImageClass imageClass)
        {
            if (!ImageEndorsementManager.IsEndorsementImageClass(imageClass))
                throw new ArgumentOutOfRangeException(nameof(imageClass));
        }

        /// <summary>
        /// Parses an optional yyyy-MM-dd date from the image endorsement editor: null if empty, but an error (rather than silently dropping it) if malformed.
        /// </summary>
        private static DateTime? ParseOptionalYMD(string sz)
        {
            return String.IsNullOrWhiteSpace(sz) ? (DateTime?)null : (sz.ParseYMD() ?? throw new MyFlightbookValidationException(Resources.Endorsements.ImageEndorsementBadDate));
        }

        /// <summary>
        /// Returns the next batch (up to ImageEndorsementManager.MaxImagesPerRun) of the user's images that haven't been read, plus the total count.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult PendingImageScans(MFBImageInfoBase.ImageClass imageClass)
        {
            return SafeOp(() =>
            {
                CheckEndorsementImageClass(imageClass);
                List<MFBImageInfo> lst = new List<MFBImageInfo>(ImageEndorsementManager.ImagesPendingScan(imageClass, User.Identity.Name));
                return Json(new
                {
                    total = lst.Count,
                    consented = ImageEndorsementManager.HasConsented(MyFlightbook.Profile.GetUser(User.Identity.Name)),
                    images = lst.Take(ImageEndorsementManager.MaxImagesPerRun).Select(mfbii => new { thumb = mfbii.ThumbnailFile, comment = String.IsNullOrWhiteSpace(mfbii.Comment) ? mfbii.ThumbnailFile : mfbii.Comment })
                });
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult GrantImageScanConsent()
        {
            return SafeOp(() =>
            {
                ImageEndorsementManager.GrantConsent(MyFlightbook.Profile.GetUser(User.Identity.Name));
                return new EmptyResult();
            });
        }

        /// <summary>
        /// Reads a single endorsement image (one of the user's own) with the vision model, creating image endorsements for what it finds.
        /// </summary>
        /// <param name="imageClass">Endorsement or OfflineEndorsement</param>
        /// <param name="szThumb">Thumbnail filename</param>
        /// <param name="fForce">True to read the image even if it has already been read (e.g., "nothing found" or dismissed)</param>
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public async Task<ActionResult> ScanEndorsementImage(MFBImageInfoBase.ImageClass imageClass, string szThumb, bool fForce = false)
        {
            return await SafeOp(async () =>
            {
                CheckEndorsementImageClass(imageClass);
                if (!EndorsementImageScanner.IsConfigured)
                    throw new MyFlightbookException(Resources.Endorsements.ImageScanNotConfigured);
                if (!ImageEndorsementManager.HasConsented(MyFlightbook.Profile.GetUser(User.Identity.Name)))
                    throw new UnauthorizedAccessException(Resources.Endorsements.ImageScanNeedsConsent);

                MFBImageInfo mfbii = MFBImageInfo.LoadMFBImageInfo(imageClass, User.Identity.Name, szThumb);
                ImageAuthorization.ValidateAuth(mfbii, User.Identity.Name, ImageAuthorization.ImageAction.Annotate);
                if (!ImageEndorsementManager.IsScannable(mfbii))
                    throw new InvalidOperationException(Resources.Endorsements.ImageScanNotScannable);

                // Already handled?  Don't pay to read it again unless asked to.
                if (!fForce && (ImageEndorsementManager.IsAbsorbed(imageClass, User.Identity.Name, szThumb) || ImageEndorsementManager.ScanMarkers(imageClass, User.Identity.Name).ContainsKey(szThumb)))
                    return Json(new { skipped = true, found = 0, titles = Array.Empty<string>() });

                List<Endorsement> lst = new List<Endorsement>(await ImageEndorsementManager.ScanImage(mfbii));
                return Json(new { skipped = false, found = lst.Count, titles = lst.Select(e => e.Title) });
            });
        }

        /// <summary>
        /// Creates (id &lt;= 0) or updates an image endorsement.  Unlike regular endorsements, image endorsements are editable.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateHeaderAntiForgeryToken]
        public ActionResult SaveImageEndorsement(int id, MFBImageInfoBase.ImageClass imageClass, string szThumb, string title, string farRef, string date, string cfiName, string cfiCert, string cfiExpiration, string text, EndorsementCategory category, string student)
        {
            return SafeOp(() =>
            {
                if (!Enum.IsDefined(typeof(EndorsementCategory), category))
                    throw new ArgumentOutOfRangeException(nameof(category));

                Endorsement e;
                if (id > 0)
                {
                    e = EndorsementWithID(id) ?? throw new InvalidOperationException("Can't find endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));
                    ImageEndorsementManager.ValidateOwnership(e, User.Identity.Name);
                }
                else
                {
                    CheckEndorsementImageClass(imageClass);
                    MFBImageInfo mfbii = MFBImageInfo.LoadMFBImageInfo(imageClass, User.Identity.Name, szThumb);
                    ImageAuthorization.ValidateAuth(mfbii, User.Identity.Name, ImageAuthorization.ImageAction.Annotate);
                    if (!ImageEndorsementManager.IsScannable(mfbii))
                        throw new InvalidOperationException(Resources.Endorsements.ImageScanNotScannable);
                    e = ImageEndorsementManager.NewImageEndorsement(imageClass, User.Identity.Name, mfbii.ThumbnailFile);
                }

                e.Title = (title ?? string.Empty).Trim().LimitTo(255);
                e.FARReference = (farRef ?? string.Empty).Trim().LimitTo(255);
                e.Date = ParseOptionalYMD(date) ?? e.Date;
                e.CFICachedName = (cfiName ?? string.Empty).Trim().LimitTo(255);
                e.CFICertificate = (cfiCert ?? string.Empty).Trim().LimitTo(255);
                e.CFIExpirationDate = ParseOptionalYMD(cfiExpiration) ?? DateTime.MinValue;
                e.EndorsementText = (text ?? string.Empty).Trim();
                e.Category = category;
                if (e.StudentType == StudentTypes.External)
                    e.StudentName = String.IsNullOrWhiteSpace(student) ? Resources.Endorsements.ImageEndorsementUnknownStudent : student.Trim().LimitTo(255);

                if (e.ID > 0)
                    e.FUpdateImageEndorsement();
                else
                {
                    e.Source = CategorySource.User;
                    e.FCommit();
                    ImageEndorsementManager.ClearScanMarker(e.ImageClass, e.ImageOwner, e.ThumbFileName);
                }
                return new EmptyResult();
            });
        }

        [ChildActionOnly]
        public ActionResult RenderImageEndorsementScanner(MFBImageInfoBase.ImageClass imageClass)
        {
            CheckEndorsementImageClass(imageClass);
            ViewBag.imageClass = imageClass;
            ViewBag.pendingCount = ImageEndorsementManager.ImagesPendingScan(imageClass, User.Identity.Name).Count();
            ViewBag.consented = ImageEndorsementManager.HasConsented(MyFlightbook.Profile.GetUser(User.Identity.Name));
            ViewBag.configured = EndorsementImageScanner.IsConfigured;
            return PartialView("_imageEndorsementScanner");
        }

        [ChildActionOnly]
        public ActionResult RenderImageEndorsementEditor()
        {
            return PartialView("_imageEndorsementEditor");
        }
        #endregion

        #region Partial views/child views
        [ChildActionOnly]
        public ActionResult RenderEndorsementTemplateBody(EndorsementType endorsementType, string targetUserName, Endorsement source = null)
        {
            ViewBag.template = endorsementType;
            ViewBag.targetUser = String.IsNullOrEmpty(targetUserName) ? string.Empty : MyFlightbook.Profile.GetUser(targetUserName).UserFullName;
            ViewBag.source = source;
            return PartialView("_endorsementTemplate");
        }

        [ChildActionOnly]
        public ActionResult RenderEndorsements(IEnumerable<Endorsement> endorsements, string userName, bool fCanDelete = false, bool fCanSort = false, bool fCanDownload = false, string onCopy = "", bool fPrintView = false, bool fCanCategorize = false)
        {
            ViewBag.endorsements = endorsements;
            ViewBag.canDelete = fCanDelete;
            ViewBag.onCopy = onCopy;
            ViewBag.canSort = fCanSort;
            ViewBag.canDownload = endorsements.Any() && fCanDownload;
            ViewBag.userName = userName;
            ViewBag.fPrintView = fPrintView;
            ViewBag.fCanCategorize = fCanCategorize;
            return PartialView("_endorsementList");
        }

        [ChildActionOnly]
        public ActionResult RenderEndorsementEditor(string sourceUser, string targetUser, StudentTypes studentType, EndorsementMode mode, string searchText = "", int idTemplate = 0)
        {
            ViewBag.sourceUser = sourceUser;
            ViewBag.targetUser = targetUser;
            ViewBag.studentType = studentType;
            ViewBag.mode = mode;
            ViewBag.query = searchText;
            ViewBag.idTemplate = idTemplate;

            List<EndorsementType> lst = new List<EndorsementType>(EndorsementType.LoadTemplates(searchText));
            if (lst.Count == 0) // if nothing found, use the custom template
                lst.Add(EndorsementType.GetEndorsementByID(EndorsementType.IDCustomTemplate));

            ViewBag.templates = lst;

            return PartialView("_editEndorsement");
        }

        [ChildActionOnly]
        public ActionResult RenderEndorsementBody(string sourceUser, string targetUser, int idTemplate, StudentTypes studentType, EndorsementMode mode, int idSrc = -1)
        {
            EndorsementType endorsementType = EndorsementType.GetEndorsementByID((idSrc > 0 || idTemplate < 0) ? EndorsementType.IDCustomTemplate : idTemplate) ?? EndorsementType.GetEndorsementByID(EndorsementType.IDCustomTemplate);
            Endorsement e = (idSrc > 0) ? EndorsementWithID(idSrc) : null;
            if ((e?.SourceTemplateID ?? 0) > EndorsementType.IDCustomTemplate && endorsementType.ID == EndorsementType.IDCustomTemplate)
                endorsementType.ID = e.SourceTemplateID;  // if the source endorsement is from a template, claim to use that template for the body even though it's technically free-form.
            ViewBag.template = endorsementType;
            ViewBag.source = (studentType == StudentTypes.External || ((e?.StudentName) ?? string.Empty).CompareCurrentCulture(targetUser) == 0) ? e : null;  // don't allow copy from someone else's endorsement.
            ViewBag.targetUser = targetUser;
            ViewBag.sourceUser = sourceUser;
            ViewBag.studentType = studentType;
            ViewBag.mode = mode;
            return PartialView("_editableEndorsement");
        }
        #endregion
    }
}
