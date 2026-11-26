using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static MyFlightbook.Instruction.Endorsement;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    /// <summary>
    /// Why an image that has no image endorsements is NOT pending a scan.
    /// </summary>
    public enum ImageScanStatus
    {
        /// <summary>
        /// Never scanned (or nothing recorded)
        /// </summary>
        None = 0,
        /// <summary>
        /// Sent to the model, which found nothing.
        /// </summary>
        NothingFound = 1,
        /// <summary>
        /// Had image endorsements, but the user deleted all of them.  Don't re-create them automatically.
        /// </summary>
        Dismissed = 2
    }

    /// <summary>
    /// Manages the relationship between endorsement images (Endorsement and OfflineEndorsement classes) and image endorsements.
    /// An image is "absorbed" when at least one image endorsement references it; absorbed images are shown only via their endorsements.
    /// </summary>
    public static class ImageEndorsementManager
    {
        /// <summary>
        /// Max # of images to read in one run of "Categorize images" before asking to continue.
        /// </summary>
        public const int MaxImagesPerRun = 25;

        /// <summary>
        /// Preference key for the one-time consent to send images to a third-party model.  Value is the UTC date/time of consent.
        /// </summary>
        public const string PrefKeyImageScanConsent = "EndorsementImageScanConsent";

        #region Image class / owner
        /// <summary>
        /// Is this an image class that can hold endorsements?
        /// </summary>
        public static bool IsEndorsementImageClass(MFBImageInfoBase.ImageClass ic)
        {
            return ic == MFBImageInfoBase.ImageClass.Endorsement || ic == MFBImageInfoBase.ImageClass.OfflineEndorsement;
        }

        private static void CheckClass(MFBImageInfoBase.ImageClass ic)
        {
            if (!IsEndorsementImageClass(ic))
                throw new ArgumentOutOfRangeException(nameof(ic), "Not an endorsement image class");
        }

        public static StudentTypes StudentTypeForClass(MFBImageInfoBase.ImageClass ic)
        {
            CheckClass(ic);
            return ic == MFBImageInfoBase.ImageClass.OfflineEndorsement ? StudentTypes.External : StudentTypes.Member;
        }

        /// <summary>
        /// Can this kind of file be read by the model?
        /// </summary>
        public static bool IsScannable(MFBImageInfo mfbii)
        {
            return mfbii != null && (mfbii.ImageType == MFBImageInfoBase.ImageFileType.JPEG || mfbii.ImageType == MFBImageInfoBase.ImageFileType.PDF || mfbii.ImageType == MFBImageInfoBase.ImageFileType.S3PDF);
        }
        #endregion

        #region Absorbed images and scan markers
        /// <summary>
        /// Returns the thumbnail filenames of the owner's images that have been absorbed into image endorsements
        /// </summary>
        public static HashSet<string> AbsorbedThumbnails(MFBImageInfoBase.ImageClass ic, string szOwner)
        {
            StudentTypes st = StudentTypeForClass(ic);
            HashSet<string> hs = new HashSet<string>(StringComparer.Ordinal);
            if (String.IsNullOrEmpty(szOwner))
                return hs;

            // Member image endorsements are owned by the student; offline ones by the instructor.  Image endorsements are hard-deleted, but be safe with DateDeleted.
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT DISTINCT ThumbFileName FROM endorsements WHERE Kind=?kind AND StudentType=?st AND {0}=?owner AND ThumbFileName IS NOT NULL AND DateDeleted IS NULL",
                st == StudentTypes.External ? "CFI" : "Student"));
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("kind", (int)EndorsementKind.Image);
                comm.Parameters.AddWithValue("st", (int)st);
                comm.Parameters.AddWithValue("owner", szOwner);
            }, (dr) => { hs.Add((string)dr["ThumbFileName"]); });
            return hs;
        }

        /// <summary>
        /// Is the specified image absorbed by at least one image endorsement?
        /// </summary>
        public static bool IsAbsorbed(MFBImageInfoBase.ImageClass ic, string szOwner, string szThumb)
        {
            return AbsorbedThumbnails(ic, szOwner).Contains(szThumb ?? string.Empty);
        }

        /// <summary>
        /// Returns the scan markers for the owner's images, keyed by thumbnail filename.  Images with no marker are omitted.
        /// </summary>
        public static Dictionary<string, ImageScanStatus> ScanMarkers(MFBImageInfoBase.ImageClass ic, string szOwner)
        {
            CheckClass(ic);
            Dictionary<string, ImageScanStatus> d = new Dictionary<string, ImageScanStatus>(StringComparer.Ordinal);
            DBHelper dbh = new DBHelper("SELECT ThumbFilename, Status FROM endorsementimagescans WHERE VirtPathID=?ic AND ImageKey=?owner");
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("ic", (int)ic);
                comm.Parameters.AddWithValue("owner", szOwner);
            }, (dr) => { d[(string)dr["ThumbFilename"]] = (ImageScanStatus)Convert.ToInt32(dr["Status"], CultureInfo.InvariantCulture); });
            return d;
        }

        public static void SetScanMarker(MFBImageInfoBase.ImageClass ic, string szOwner, string szThumb, ImageScanStatus status)
        {
            CheckClass(ic);
            if (status == ImageScanStatus.None)
            {
                ClearScanMarker(ic, szOwner, szThumb);
                return;
            }
            DBHelper dbh = new DBHelper("REPLACE INTO endorsementimagescans SET VirtPathID=?ic, ImageKey=?owner, ThumbFilename=?thumb, Status=?status, DateScanned=UTC_TIMESTAMP()");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("ic", (int)ic);
                comm.Parameters.AddWithValue("owner", szOwner);
                comm.Parameters.AddWithValue("thumb", szThumb);
                comm.Parameters.AddWithValue("status", (int)status);
            });
        }

        public static void ClearScanMarker(MFBImageInfoBase.ImageClass ic, string szOwner, string szThumb)
        {
            CheckClass(ic);
            DBHelper dbh = new DBHelper("DELETE FROM endorsementimagescans WHERE VirtPathID=?ic AND ImageKey=?owner AND ThumbFilename=?thumb");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("ic", (int)ic);
                comm.Parameters.AddWithValue("owner", szOwner);
                comm.Parameters.AddWithValue("thumb", szThumb);
            });
        }

        /// <summary>
        /// Returns a new image list with the absorbed images removed.  The source list must already be refreshed.
        /// </summary>
        public static ImageList UnabsorbedImages(ImageList il)
        {
            if (il == null)
                throw new ArgumentNullException(nameof(il));
            HashSet<string> hsAbsorbed = AbsorbedThumbnails(il.Class, il.Key);
            return new ImageList(il.Class, il.Key, il.ImageArray.Where(mfbii => !hsAbsorbed.Contains(mfbii.ThumbnailFile)).ToArray());
        }

        /// <summary>
        /// Returns the scan status of each displayed (unabsorbed) image, for decorating the image list.  Every scannable image gets an entry.
        /// </summary>
        public static Dictionary<string, ImageScanStatus> ScanStatusForImages(ImageList il)
        {
            if (il == null)
                throw new ArgumentNullException(nameof(il));
            Dictionary<string, ImageScanStatus> dMarkers = ScanMarkers(il.Class, il.Key);
            Dictionary<string, ImageScanStatus> d = new Dictionary<string, ImageScanStatus>(StringComparer.Ordinal);
            foreach (MFBImageInfo mfbii in il.ImageArray)
                if (IsScannable(mfbii))
                    d[mfbii.ThumbnailFile] = dMarkers.TryGetValue(mfbii.ThumbnailFile, out ImageScanStatus st) ? st : ImageScanStatus.None;
            return d;
        }

        /// <summary>
        /// Returns the images that are neither absorbed nor marked - i.e., that "Categorize images" should read.
        /// </summary>
        public static IEnumerable<MFBImageInfo> ImagesPendingScan(MFBImageInfoBase.ImageClass ic, string szOwner)
        {
            CheckClass(ic);
            ImageList il = new ImageList(ic, szOwner);
            il.Refresh(true, null, false);
            HashSet<string> hsAbsorbed = AbsorbedThumbnails(ic, szOwner);
            Dictionary<string, ImageScanStatus> dMarkers = ScanMarkers(ic, szOwner);
            return il.ImageArray.Where(mfbii => IsScannable(mfbii) && !hsAbsorbed.Contains(mfbii.ThumbnailFile) && !dMarkers.ContainsKey(mfbii.ThumbnailFile)).ToList();
        }
        #endregion

        #region Consent
        public static bool HasConsented(Profile pf)
        {
            return pf != null && pf.PreferenceExists(PrefKeyImageScanConsent);
        }

        public static void GrantConsent(Profile pf)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            pf.SetPreferenceForKey(PrefKeyImageScanConsent, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        }
        #endregion

        #region Creating, updating, deleting
        /// <summary>
        /// Creates (but does not commit) a new image endorsement for the specified image, with defaults filled in.
        /// </summary>
        /// <param name="ic">Class of the image</param>
        /// <param name="szOwner">Owner of the image (the student for Endorsement images, the instructor for OfflineEndorsement images)</param>
        /// <param name="szThumb">Thumbnail filename of the image</param>
        public static Endorsement NewImageEndorsement(MFBImageInfoBase.ImageClass ic, string szOwner, string szThumb)
        {
            StudentTypes st = StudentTypeForClass(ic);
            return new Endorsement(st == StudentTypes.External ? szOwner : string.Empty)
            {
                Kind = EndorsementKind.Image,
                StudentType = st,
                StudentName = st == StudentTypes.Member ? szOwner : string.Empty,
                ThumbFileName = szThumb,
                Date = DateTime.Now.Date,
                CFIExpirationDate = DateTime.MinValue,
                CFICertificate = string.Empty,
                CFICachedName = string.Empty,
                Category = EndorsementCategory.None,
                Source = CategorySource.None,
                PageNumber = 0,
                Confidence = -1
            };
        }

        /// <summary>
        /// Reads the image with the vision model and creates image endorsements for what it finds.  If nothing is found, marks the image so it isn't retried.
        /// The caller is responsible for authorization and consent.
        /// </summary>
        /// <param name="mfbii">The image to read</param>
        /// <returns>The newly created image endorsements (possibly empty)</returns>
        public static async Task<IEnumerable<Endorsement>> ScanImage(MFBImageInfo mfbii)
        {
            if (mfbii == null)
                throw new ArgumentNullException(nameof(mfbii));
            CheckClass(mfbii.Class);
            if (!IsScannable(mfbii))
                throw new InvalidOperationException("Only images and PDFs can be read");

            bool fOffline = mfbii.Class == MFBImageInfoBase.ImageClass.OfflineEndorsement;
            EndorsementScanResult result = await EndorsementImageScanner.ScanAsync(mfbii, fOffline);

            List<Endorsement> lst = new List<Endorsement>();
            foreach (ScannedEndorsementItem item in result.Items)
            {
                Endorsement e = NewImageEndorsement(mfbii.Class, mfbii.Key, mfbii.ThumbnailFile);
                e.Title = item.Title.Trim().LimitTo(255);
                e.FARReference = (item.Regulation ?? string.Empty).Trim().LimitTo(255);
                e.EndorsementText = (item.DisplayText ?? string.Empty).Trim();
                e.RawText = String.IsNullOrWhiteSpace(item.RawText) ? null : item.RawText.Trim();
                e.Category = item.Category;
                e.Source = CategorySource.Model;
                e.Confidence = item.ConfidencePercent;
                e.PageNumber = Math.Max(0, Math.Min(item.PageNumber ?? 1, EndorsementImageScanner.MaxPDFPages));
                e.Date = item.Date ?? DateTime.Now.Date;
                e.CFICachedName = (item.InstructorName ?? string.Empty).Trim().LimitTo(255);
                e.CFICertificate = (item.InstructorCertificate ?? string.Empty).Trim().LimitTo(255);
                e.CFIExpirationDate = item.InstructorExpiration ?? DateTime.MinValue;
                if (fOffline)
                    e.StudentName = String.IsNullOrWhiteSpace(item.StudentName) ? Resources.Endorsements.ImageEndorsementUnknownStudent : item.StudentName.Trim().LimitTo(255);
                e.FCommit();
                lst.Add(e);
            }

            if (lst.Count == 0)
                SetScanMarker(mfbii.Class, mfbii.Key, mfbii.ThumbnailFile, ImageScanStatus.NothingFound);
            else
                ClearScanMarker(mfbii.Class, mfbii.Key, mfbii.ThumbnailFile);

            return lst;
        }

        /// <summary>
        /// Deletes an image endorsement.  If it was the last one referencing its image, the image goes back to the image list (where it can be deleted like any other),
        /// marked as dismissed so that "Categorize images" doesn't re-create what the user just deleted.
        /// </summary>
        public static void DeleteImageEndorsement(Endorsement e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (!e.IsImageEndorsement)
                throw new InvalidOperationException("Not an image endorsement");

            e.FDelete();
            if (!String.IsNullOrEmpty(e.ThumbFileName) && !IsAbsorbed(e.ImageClass, e.ImageOwner, e.ThumbFileName))
                SetScanMarker(e.ImageClass, e.ImageOwner, e.ThumbFileName, ImageScanStatus.Dismissed);
        }

        /// <summary>
        /// Called when an endorsement image itself is deleted, to clean up its marker.  Absorbed images can't be deleted.
        /// </summary>
        public static void OnImageDeleted(MFBImageInfoBase.ImageClass ic, string szOwner, string szThumb)
        {
            if (IsEndorsementImageClass(ic))
                ClearScanMarker(ic, szOwner, szThumb);
        }

        /// <summary>
        /// Verifies that the user can edit or delete the specified image endorsement (i.e., that they own the underlying image)
        /// </summary>
        public static void ValidateOwnership(Endorsement e, string szUser)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (!e.IsImageEndorsement)
                throw new InvalidOperationException("Not an image endorsement");
            if (String.IsNullOrEmpty(szUser) || e.ImageOwner.CompareCurrentCultureIgnoreCase(szUser) != 0)
                throw new UnauthorizedAccessException();
        }
        #endregion
    }
}
