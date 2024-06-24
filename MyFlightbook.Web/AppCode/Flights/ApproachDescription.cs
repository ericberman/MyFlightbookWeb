using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2008-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Parses an approach description into something displayable
    /// </summary>
    public class ApproachDescription
    {
        #region properties
        /// <summary>
        /// Number of approaches performed
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Description of the approach (e.g., ILS, VOR, GPS, etc.)
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Runway to which the approch was made
        /// </summary>
        public string Runway { get; private set; }

        private string CanonicalRunway
        {
            get { return (String.IsNullOrEmpty(Runway) ? string.Empty : (Runway.StartsWith("RWY", StringComparison.CurrentCultureIgnoreCase) ? Runway : "RWY" + Runway)); }
        }

        /// <summary>
        /// Airport Identifier where the approach was performed
        /// </summary>
        public string AirportCode { get; private set; }

        public string ApproachString
        {
            get { return String.Format(CultureInfo.CurrentCulture, Count == 1 ? Resources.LogbookEntry.ApproachDescApproach : Resources.LogbookEntry.ApproachDescApproaches, Count, Description); }
        }
        #endregion

        #region Constructors
        public ApproachDescription(Match m)
        {
            if (m == null)
                throw new ArgumentNullException(nameof(m));
            Count = Convert.ToInt32(m.Groups["count"].Value, CultureInfo.CurrentCulture);
            Description = m.Groups["desc"].Value;
            Runway = m.Groups["rwy"].Value;
            AirportCode = m.Groups["airport"].Value;
        }

        public ApproachDescription(int count, string desc, string runway, string code)
        {
            Count = count;
            Description = desc;
            Runway = runway;
            AirportCode = code;
        }
        #endregion

        #region Creation and parsing
        public static IEnumerable<ApproachDescription> ExtractApproaches(string szSource)
        {
            List<ApproachDescription> lst = new List<ApproachDescription>();

            if (szSource != null)
            {
                MatchCollection mc = RegexUtility.ApproachDescription.Matches(szSource);
                foreach (Match m in mc)
                    lst.Add(new ApproachDescription(m));
            }

            return lst;
        }

        public static string ReplaceApproaches(string szSource)
        {
            if (szSource == null)
                return string.Empty;

            return RegexUtility.ApproachDescription.Replace(szSource, (m) => { return String.Format(CultureInfo.InvariantCulture, "<span title='{0}' style=\"display:inline-block; border-bottom: 1px dotted #000; \">{1}</span>", new ApproachDescription(m).ToString(), m.Groups[0].Value); });
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.ApproachDescTemplate, ApproachString, Runway, AirportCode);
        }

        public string ToCanonicalString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}@{3}", Count, Description, CanonicalRunway, AirportCode);
        }
    }
}

