using System;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Provides text with an optionally linked string.
    /// </summary>
    [Serializable]
    public class LinkedString
    {
        public string Value { get; set; }
        public string Link { get; set; }

        public LinkedString() { }

        public LinkedString(string szValue, string szLink)
        {
            Value = szValue;
            Link = szLink;
        }

        public LinkedString(string szValue)
        {
            Value = szValue;
            Link = null;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
