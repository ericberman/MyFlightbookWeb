using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2007-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public class ImageList
    {
        private string m_szBase;
        private string m_szKey;
        private MFBImageInfo[] rgMfbii = null;
        private MFBImageInfo.ImageClass m_imageClass = MFBImageInfo.ImageClass.Unknown;

        #region properties
        /// <summary>
        /// The class of image - flight, aircraft, endorsement, etc.
        /// </summary>
        public MFBImageInfo.ImageClass Class 
        {
            get { return m_imageClass; }
            set
            {
                m_imageClass = value;
                m_szBase = MFBImageInfo.BasePathFromClass(value);
            }
        }

        /// <summary>
        /// The key for the specific images
        /// </summary>
        public string Key
        {
            get { return m_szKey; }
            set { m_szKey = value; }
        }

        /// <summary>
        /// Returns the virtual path to use for this base/key pair.
        /// </summary>
        public string VirtPath
        {
            get { return m_szBase + m_szKey + "/"; }
        }

        /// <summary>
        /// The array of images associated with this imagelist
        /// </summary>
        public ReadOnlyCollection<MFBImageInfo> ImageArray
        {
            get { return new ReadOnlyCollection<MFBImageInfo>(rgMfbii); }
        }

        // removes all images in the image list, setting it to null
        public void Clear()
        {
            rgMfbii = null;
        }
        #endregion

        public ImageList(MFBImageInfo[] rgImages = null)
        {
            m_szBase = m_szKey = string.Empty;
            rgMfbii = rgImages;
        }

        public ImageList(MFBImageInfo.ImageClass imageClass, string szKey, MFBImageInfo[] rgImages = null)
        {
            Class = imageClass;
            m_szKey = szKey;
            rgMfbii = rgImages;
        }

        // Image utilities

        private void AddFilesToList(DirectoryInfo dir, string szMask, List<MFBImageInfo> lst)
        {
            FileInfo[] rgFiles = dir.GetFiles(szMask);
            if (rgFiles != null)
                foreach(FileInfo fi in rgFiles)
                    lst.Add(MFBImageInfo.LoadMFBImageInfo(Class, Key, fi.Name));
        }

        public void RemoveImage(MFBImageInfo mfbii)
        {
            List<MFBImageInfo> lst = new List<MFBImageInfo>(rgMfbii);
            lst.RemoveAll(mfbii2 => mfbii2.PrimaryKey.CompareOrdinal(mfbii.PrimaryKey) == 0);
            rgMfbii = lst.ToArray();
        }

        /// <summary>
        /// Returns a set of MFBImageInfo objects that represent the images for this base/key pair.  Results are cached.
        /// </summary>
        /// <param name="fIncludeDocs">Include PDF files?</param>
        /// <param name="fIncludeVids">Include videos? (true by default)</param>
        /// <param name="szDefault">True for a "Default" image to put at the front</param>
        /// <returns>Array of MFBImageInfo objects</returns>
        public void Refresh(bool fIncludeDocs = false, string szDefault = null, bool fIncludeVids = true)
        {
            DirectoryInfo dir = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(VirtPath));
            List<MFBImageInfo> lstMfbii = new List<MFBImageInfo>();

            if (dir != null && dir.Exists)
            {
                AddFilesToList(dir, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}*{1}", MFBImageInfo.ThumbnailPrefix, FileExtensions.JPG), lstMfbii);
                if (fIncludeVids)
                    AddFilesToList(dir, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}*{1}", MFBImageInfo.ThumbnailPrefixVideo, FileExtensions.JPG), lstMfbii);

                if (fIncludeDocs)
                {
                    AddFilesToList(dir, String.Format(System.Globalization.CultureInfo.InvariantCulture, "*{0}", FileExtensions.PDF), lstMfbii);
                    AddFilesToList(dir, String.Format(System.Globalization.CultureInfo.InvariantCulture, "*{0}", FileExtensions.S3PDF), lstMfbii);
                }
            }
            dir = null;

            lstMfbii.Sort();

            if (!String.IsNullOrEmpty(szDefault))
            {
                MFBImageInfo mfbii = null;
                if ((mfbii = lstMfbii.Find(img => img.ThumbnailFile.CompareOrdinalIgnoreCase(szDefault) == 0)) != null)
                {
                    lstMfbii.Remove(mfbii);
                    lstMfbii.Insert(0, mfbii);
                }
            }

            rgMfbii = lstMfbii.ToArray();
        }
    }
}

