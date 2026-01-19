using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    /// <summary>
    /// See https://gist.github.com/shinyzhu/4617989
    /// </summary>
    [Serializable]
    public class GoogleEncodedPath
    {
        #region Properties
        /// <summary>
        /// The encoded path
        /// </summary>
        public string EncodedPath { get; set; }

        /// <summary>
        /// The bounding box for the path.  CAN BE NULL IF PARSEPATH HASN'T BEEN CALLED
        /// </summary>
        public LatLongBox Box { get; set; }

        /// <summary>
        /// The distance covered by the path, in NM
        /// </summary>
        public double Distance { get; set; }
        #endregion

        #region Object Creation
        public GoogleEncodedPath()
        {
            Box = null;
            Distance = 0.0;
            EncodedPath = string.Empty;
        }

        public GoogleEncodedPath(IEnumerable<LatLong> rgll) : this()
        {
            ParsePath(rgll);
        }
        #endregion

        #region Display
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Distance: {0:#,###.#nm}, Length: {1:#,###}", Distance, String.IsNullOrEmpty(EncodedPath) ? 0 : EncodedPath.Length);
        }
        #endregion

        #region Encoding/Decoding
        /// <summary>
        /// Encodes a latitude or longitude.
        /// see http://code.google.com/apis/maps/documentation/polylinealgorithm.html
        /// or https://developers.google.com/maps/documentation/utilities/polylinealgorithm
        /// </summary>
        /// <param name="d">The point to encode</param>
        /// <returns>A string representing the encoded point</returns>
        protected static string EncodeLatLong(double d)
        {
            StringBuilder sb = new StringBuilder();
            // Step 1: start with the original value (no-op)
            // Step 2: multiply by 1e5 (=100000), take rounded result
            Int32 i = (Int32)Math.Round(d * 100000.0);

            // Step 2: convert to 2's complement binary representation
            // I think there is nothing to do here - should already be that way.

            // Step 4: left shift by 1 bit
            UInt32 i2 = (((UInt32)i) << 1);

            // Step 5: invert it if i < 0
            if (i < 0)
                i2 ^= 0xFFFFFFFF;

            // Step 6: break into 5-bit chunks, starting at right
            UInt32[] rgui = new uint[6];
            int j;
            for (j = 0; j < 6; j++)
                rgui[j] = (UInt32)(i2 & (0x1F << (5 * j))) >> (5 * j);

            // step 7: reverse them (no op, that's how above filled the array)

            // step 8: OR each with 0x20 if followed by others
            for (j = 5; j > 0 && rgui[j] == 0; j--) ; // skip the fully 0 bytes
            int lastchar = j;
            while (--j >= 0)  // And OR the remainders with 0x20
                rgui[j] |= 0x20;

            // Step 9: convert each value to decimal (no op)

            // Step 10-11: convert each value to ASCII by adding 63 and output
            // Don't forget to escape backslashes
            for (j = 0; j <= lastchar; j++)
            {
                sb.Append(Convert.ToChar(rgui[j] + 63, CultureInfo.InvariantCulture));
                if (Convert.ToChar(rgui[j] + 63, CultureInfo.InvariantCulture) == '\\')
                    sb.Append('\\');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reverses EncodeLatLong
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static double DecodeLatLong(string sz)
        {
            double val = 0.0;

            if (!String.IsNullOrEmpty(sz))
            {
                char[] rgch = sz.ToCharArray();
                int ich = 0;
                int cch = rgch.Length;
                UInt32 chunk;
                UInt32 intVal = 0;
                int shift = 0;

                do
                {
                    // Undo steps 10-11:
                    chunk = (UInt32)(((int)rgch[ich]) - 63);    // convert from ascii to decimal by subtracting 63 (steps 9-10)
                    intVal |= (chunk & 0x1F) << (5 * shift++);  // Re-assemble from right to left by shifting 5 bits at a time (step 6-8)
                } while (++ich < cch && (chunk & 0x20) != 0);  // the 0x20 leading bit indicates that more 5-bit chunks follow

                // Undo steps 5 and 4: if it has a trailing 1, it needs to be inverted; either way, it then needs to be shifted right
                if ((intVal & 0x1) == 1)
                    intVal = ~intVal;

                // Convert it back to a signed value
                Int32 iVal = (Int32)intVal;

                // And convert it back to a decimal, dividing by 2 and by 1e5
                val = ((double)iVal) / 2e5;
            }

            return val;
        }

        /// <summary>
        /// Encodes a latlong path per the instructions at https://developers.google.com/maps/documentation/utilities/polylinealgorithm
        /// </summary>
        /// <param name="rgll">An enumerable with points.</param>
        /// <returns></returns>
        public void ParsePath(IEnumerable<LatLong> rgll)
        {
            if (rgll == null)
                return;
            StringBuilder sb = new StringBuilder();

            LatLong ll = new LatLong(0, 0);
            LatLongBox llb = null;

            double distance = 0.0;
            bool fAddDistance = false;

            foreach (LatLong llNew in rgll)
            {
                if (llb == null)
                {
                    llb = new LatLongBox(llNew);
                }
                else
                {
                    llb.ExpandToInclude(llNew);
                }

                if (!llNew.IsSameLocation(ll, .00015))   // wait until we actually move a little bit.  .00015 of latitude is approximately 20ft 
                {
                    if (fAddDistance)
                        distance += llNew.DistanceFrom(ll);
                    else
                        fAddDistance = true;

                    sb.Append(EncodeLatLong(llNew.Latitude - ll.Latitude));
                    sb.Append(EncodeLatLong(llNew.Longitude - ll.Longitude));

                    ll = llNew;
                }
            }

            EncodedPath = sb.ToString();
            Box = llb;
            Distance = distance;
        }

        /// <summary>
        /// Returns a decoded lat/lon path from the encoded string
        /// </summary>
        /// <returns>A list of LatLong values</returns>
        public IEnumerable<LatLong> DecodedPath()
        {
            List<LatLong> lst = new List<LatLong>();
            if (string.IsNullOrEmpty(EncodedPath))
                return lst;

            char[] rgch = EncodedPath.ToCharArray();

            // break it up into strings for each number
            List<string> lstVals = new List<string>();
            int ichLast = 0;
            int cch = 0;
            for (int ichCur = 0; ichCur < rgch.Length; ichCur++)
            {
                cch++;
                if (((((int)rgch[ichCur]) - 63) & 0x20) == 0)
                {
                    // need to account for backslashes
                    if (rgch[ichCur] == '\\')
                    {
                        cch++;
                        ichCur++;
                    }
                    lstVals.Add(EncodedPath.Substring(ichLast, cch));
                    cch = 0;
                    ichLast = ichCur + 1;
                }
            }

            // Verify that we have an even number of values - don't know what to do with a trailing latitude or trailing longitude!!!
            int cVals = lstVals.Count;
            if ((cVals % 2) > 0)
                cVals--;

            LatLong ll = null;
            for (int i = 0; i < cVals; i += 2)
            {
                double lat = DecodeLatLong(lstVals[i]) + ((ll == null) ? 0.0 : ll.Latitude);
                double lon = DecodeLatLong(lstVals[i + 1]) + ((ll == null) ? 0.0 : ll.Longitude);
                lst.Add(ll = new LatLong(lat, lon));
            }

            return lst;
        }
        #endregion
    }
}
