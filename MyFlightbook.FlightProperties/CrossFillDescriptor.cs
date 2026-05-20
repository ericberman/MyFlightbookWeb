/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.FlightProperties
{
    /// <summary>
    /// Describes cross-fill for a specific property with a tip and a script reference
    /// </summary>
    public class CrossFillDescriptor
    {
        /// <summary>
        /// The top for the cross-fill arrow
        /// </summary>
        public string Tip { get; set; }

        /// <summary>
        /// Client-resolved reference to the script to execute on cross-fill
        /// </summary>
        public string ScriptRef { get; set; }

        public CrossFillDescriptor()
        {
            Tip = ScriptRef = string.Empty;
        }

        public CrossFillDescriptor(string tip, string scriptref)
        {
            Tip = tip;
            ScriptRef = scriptref;
        }
    }
}