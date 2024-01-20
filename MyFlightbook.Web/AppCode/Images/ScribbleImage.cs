using System;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2008-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Encapsulates utilities for scribbled PNG signatures, including presentation as a data: URL
    /// </summary>
    public static class ScribbleImage
    {
        private const string DataURLPrefix = "data:image/png;base64,";

        /// <summary>
        /// Returns a data:... URL for the specified byte array
        /// </summary>
        /// <param name="rgb">The byte array</param>
        /// <returns>A url which can be used in-line for images</returns>
        public static string DataLinkForByteArray(byte[] rgb)
        {
            if (rgb == null)
                return string.Empty;

            return DataURLPrefix + Convert.ToBase64String(rgb);
        }

        /// <summary>
        /// Validates that the specified data URL is well formed and contains data
        /// </summary>
        /// <param name="szLink"></param>
        /// <returns></returns>
        public static bool IsValidDataURL(string szLink)
        {
            if (String.IsNullOrWhiteSpace(szLink))
                return false;

            if (!szLink.StartsWith(DataURLPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            return szLink.Length > DataURLPrefix.Length;
        }

        /// <summary>
        /// Retrieves the bytes from a data: URL
        /// </summary>
        /// <param name="szLink">The data: URL</param>
        /// <returns>The bytes of the PNG.</returns>
        public static byte[] FromDataLinkURL(string szLink)
        {
            if (String.IsNullOrEmpty(szLink))
                return Array.Empty<byte>();

            string szSigB64 = szLink.Substring(ScribbleImage.DataURLPrefix.Length);
            byte[] rgbSignature = Convert.FromBase64CharArray(szSigB64.ToCharArray(), 0, szSigB64.Length);

            if (rgbSignature.Length > 10000) // this may not be compressed (e.g., from Android) - compress it.
            {
                using (Stream st = new MemoryStream(rgbSignature))
                {
                    using (MemoryStream stDst = new MemoryStream())
                    {
                        // This is a PNG, so no need to handle temp files/conversion.
                        using (System.Drawing.Image image = MFBImageInfo.DrawingCompatibleImageFromStream(st))
                        {
                            image.Save(stDst, System.Drawing.Imaging.ImageFormat.Png);
                            rgbSignature = new byte[stDst.Length];
                            stDst.Position = 0;
                            stDst.Read(rgbSignature, 0, (int)stDst.Length);
                        }
                    }
                }
            }

            return rgbSignature;

        }
    }
}