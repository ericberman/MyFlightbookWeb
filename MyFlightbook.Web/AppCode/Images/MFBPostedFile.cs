using gma.Drawing.ImageInfo;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Pseudo HTTPPostedFile, since I can't create those.
    /// </summary>
    [Serializable]
    public class MFBPostedFile
    {
        #region Constructors
        public MFBPostedFile()
        {
        }

        public MFBPostedFile(HttpPostedFile pf) : this()
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            WriteStreamToTempFile(pf.InputStream);
            FileID = FileName = pf.FileName;

            ContentType = pf.ContentType;
            ContentLength = pf.ContentLength;
        }

        public MFBPostedFile(HttpPostedFileBase pf) : this()
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            WriteStreamToTempFile(pf.InputStream);
            FileID = FileName = pf.FileName;

            ContentType = pf.ContentType;
            ContentLength = pf.ContentLength;
        }
        public MFBPostedFile(Stream s, string fileName, string contentType, int contentLength) : this()
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            WriteStreamToTempFile(s);

            FileID = FileName = fileName;
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            ContentLength = contentLength;
        }

        private static readonly Regex rDataUrl = new Regex("^data:((?<type>[\\w\\/]+))?;base64,(?<data>.+)$", RegexOptions.Compiled);

        /// <summary>
        /// Create an MFBPostedFile from a dataURL
        /// </summary>
        /// <param name="s"></param>
        public MFBPostedFile(String s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (!rDataUrl.IsMatch(s))
                throw new InvalidDataException("Attempt to initialize MFBPostedFile from a non-data URL");

            GroupCollection gc = Regex.Match(s, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
            ContentType = gc["type"].Value;

            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(gc["data"].Value)))
            {
                WriteStreamToTempFile(ms);
                ContentLength = ms.Length;
                FileName = TempFileName;
            }
        }

        public static MFBPostedFile PostedFileFromURL(Uri uri, string szFilename, string szContentType)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (szContentType == null)
                throw new ArgumentNullException(szContentType);

            using (WebClient wc = new WebClient())
            {
                string szTempFile = Path.GetTempFileName();
                wc.DownloadFile(uri, szTempFile);
                FileInfo fi = new FileInfo(szTempFile);
                return new MFBPostedFile() { ContentLength = (int) fi.Length, ContentType = szContentType, FileID = szFilename, FileName = szFilename, TempFileName = szTempFile };
            }
        }

        public MFBPostedFile(AjaxControlToolkit.AjaxFileUploadEventArgs e) : this()
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            using (Stream s = e.GetStreamContents())
                WriteStreamToTempFile(s);

            FileID = e.FileId;
            FileName = e.FileName;

            ContentType = e.ContentType;
            ContentLength = e.FileSize;
        }

        ~MFBPostedFile()
        {
            // We can't make MFBPostedFile be disposable because we hold it indefinitely in the session
            // But we're not holding anything open either. 
            // So on object deletion, clean up any temp files.
#pragma warning disable IDISP023 // Don't use reference types in finalizer context.
            CleanUp();
#pragma warning restore IDISP023 // Don't use reference types in finalizer context.
        }

        private void WriteStreamToTempFile(Stream s)
        {
            TempFileName = Path.GetTempFileName();
            using (FileStream fs = File.OpenWrite(TempFileName))
            {
                s.Seek(0, SeekOrigin.Begin);
                s.CopyTo(fs);
            }
        }
        #endregion

        #region properties
        /// <summary>
        /// File name for the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// MIME content type for the file
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The length of the file
        /// </summary>
        public long ContentLength { get; set; }

        /// <summary>
        /// The name of the temp file to which the data has been written, so that we're not holding a potentially huge file in memory.
        /// The file can be safely deleted at any time.
        /// </summary>
        public string TempFileName { get; private set; }

        /// <summary>
        /// The unique ID provided by the AJAX file upload control
        /// </summary>
        public string FileID { get; set; }

        private byte[] m_ThumbBytes;

        /// <summary>
        /// Returns the bytes of the posted file, converted if needed from HEIC.
        /// </summary>
        public byte[] CompatibleContentData()
        {
            if (TempFileName == null)
                return null;

            using (FileStream fs = File.OpenRead(TempFileName))
            {
                // Check for HEIC
                try
                {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(fs))
                    {
                        // If we got here then the content is Drawing Compatible - i.e., not HEIC; just return contentdata
                        fs.Seek(0, SeekOrigin.Begin);
                        byte[] rgb = new byte[fs.Length];
                        fs.Read(rgb, 0, rgb.Length);
                        return rgb;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException)
                {
                    return MFBImageInfo.ConvertStreamToJPG(fs);
                }
            }
        }
        
        /// <summary>
        /// The stream of the thumbnail file - computed once and cached in ThumbnailData
        /// </summary>
        public byte[] ThumbnailBytes()
        {
            if (m_ThumbBytes != null)
                return m_ThumbBytes;

            using (Stream s = GetInputStream())
            {
                string szTempFile = null;
                try
                {
                    using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(s, out szTempFile))
                    {
                        Info inf = MFBImageInfo.InfoFromImage(image);
                        using (Bitmap bmp = MFBImageInfo.BitmapFromImage(inf.Image, MFBImageInfo.ThumbnailHeight, MFBImageInfo.ThumbnailWidth))
                        {
                            using (MemoryStream sOut = new MemoryStream())
                            {
                                bmp.Save(sOut, ImageFormat.Jpeg);
                                m_ThumbBytes = sOut.ToArray();
                            }
                        }
                    }
                }
                finally
                {
                    if (!String.IsNullOrEmpty(szTempFile) && File.Exists(szTempFile))
                        File.Delete(szTempFile);
                }
            }
            return m_ThumbBytes;
        }

        /// <summary>
        /// Returns a stream to the underlying data.
        /// MUST BE DISPOSED BY CALLER
        /// </summary>
        /// <returns></returns>
        public Stream GetInputStream()
        {
            return File.OpenRead(TempFileName);
        }

        public void CleanUp()
        {
            if (!String.IsNullOrEmpty(TempFileName) && File.Exists(TempFileName))
                File.Delete(TempFileName);
        }
        #endregion
    }
}