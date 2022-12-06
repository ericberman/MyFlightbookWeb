using JouniHeikniemi.Tools.Text;
using MyFlightbook.Geography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Airports
{
    public enum AirportImportSource { FAA, ICAO, IATA }
    public enum AirportImportRowCommand { FixLocation, FixType, AddAirport, Overwrite };

    internal class ImportAirportContext
    {
        public int iColFAA { get; set; } = -1;
        public int iColICAO { get; set; } = -1;
        public int iColIATA { get; set; } = -1;
        public int iColName { get; set; } = -1;
        public int iColLatitude { get; set; } = -1;
        public int iColLongitude { get; set; } = -1;
        public int iColLatLong { get; set; } = -1;
        public int iColType { get; set; } = -1;

        public int iColCountry { get; set; } = -1;

        public int iColAdmin1 { get; set; } = -1;

        public ImportAirportContext()
        {
        }

        public void InitFromHeader(string[] rgCols)
        {
            if (rgCols == null)
                throw new ArgumentNullException(nameof(rgCols));

            for (int i = 0; i < rgCols.Length; i++)
            {
                string sz = rgCols[i];
                if (String.Compare(sz, "FAA", StringComparison.OrdinalIgnoreCase) == 0)
                    iColFAA = i;
                if (String.Compare(sz, "ICAO", StringComparison.OrdinalIgnoreCase) == 0)
                    iColICAO = i;
                if (String.Compare(sz, "IATA", StringComparison.OrdinalIgnoreCase) == 0)
                    iColIATA = i;
                if (String.Compare(sz, "Name", StringComparison.OrdinalIgnoreCase) == 0)
                    iColName = i;
                if (String.Compare(sz, "Latitude", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLatitude = i;
                if (String.Compare(sz, "Longitude", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLongitude = i;
                if (String.Compare(sz, "LatLong", StringComparison.OrdinalIgnoreCase) == 0)
                    iColLatLong = i;
                if (String.Compare(sz, "Type", StringComparison.OrdinalIgnoreCase) == 0)
                    iColType = i;
                if (String.Compare(sz, "Country", StringComparison.OrdinalIgnoreCase) == 0)
                    iColCountry = i;
                if (String.Compare(sz, "Admin1", StringComparison.OrdinalIgnoreCase) == 0)
                    iColAdmin1 = i;
            }

            if (iColFAA == -1 && iColIATA == -1 && iColICAO == -1)
                throw new MyFlightbookException("No airportid codes found");
            if (iColName == -1)
                throw new MyFlightbookException("No name column found");
            if (iColLatLong == -1 && iColLatitude == -1 && iColLongitude == -1)
                throw new MyFlightbookException("No position found");
        }
    }

    /// <summary>
    /// Represents a row of data with a code, name, and lat/lon which can be imported into the airports data table.
    /// </summary>
    [Serializable]
    public class airportImportCandidate : airport
    {
        public enum MatchStatus { Unknown, InDB, InDBWrongLocation, NotInDB, WrongType, NotApplicable };

        public const double LocationTolerance = 0.01;

        #region properties
        private string m_MatchedFAAName, m_MatchedIATAName, m_MatchedICAOName;
        private airport m_FAAMatch, m_IATAMatch, m_ICAOMatch;

        public string IATA { get; set; }
        public string ICAO { get; set; }
        public string FAA { get; set; }
        public MatchStatus MatchStatusFAA { get; set; }
        public MatchStatus MatchStatusIATA { get; set; }
        public MatchStatus MatchStatusICAO { get; set; }

        public string MatchNotes { get; set; }

        public airport IATAMatch
        {
            get { return m_IATAMatch; }
            set { m_IATAMatch = value; }
        }

        public airport FAAMatch
        {
            get { return m_FAAMatch; }
            set { m_FAAMatch = value; }
        }

        public airport ICAOMatch
        {
            get { return m_ICAOMatch; }
            set { m_ICAOMatch = value; }
        }

        public string MatchedICAOName
        {
            get { return m_MatchedICAOName; }
            set { m_MatchedICAOName = value; }
        }

        public string MatchedIATAName
        {
            get { return m_MatchedIATAName; }
            set { m_MatchedIATAName = value; }
        }

        public string MatchedFAAName
        {
            get { return m_MatchedFAAName; }
            set { m_MatchedFAAName = value; }
        }

        public string MatchStatusDescription
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrEmpty(FAA))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "FAA: {0}\r\n", MatchStatusFAA.ToString());
                if (!String.IsNullOrEmpty(ICAO))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "ICAO: {0}\r\n", MatchStatusICAO.ToString());
                if (!String.IsNullOrEmpty(IATA))
                    sb.AppendFormat(CultureInfo.CurrentCulture, "IATA: {0}\r\n", MatchStatusIATA.ToString());
                return sb.ToString();
            }
        }

        public bool IsOK
        {
            get { return StatusIsOK(MatchStatusFAA) && StatusIsOK(MatchStatusIATA) && StatusIsOK(MatchStatusICAO); }
        }

        public bool IsKHack
        {
            get
            {
                return MatchStatusFAA == MatchStatus.InDB && MatchStatusIATA == MatchStatus.NotApplicable && MatchStatusICAO == MatchStatus.NotInDB &&
                String.Compare(ICAO, String.Format(CultureInfo.InvariantCulture, "K{0}", FAA), StringComparison.CurrentCultureIgnoreCase) == 0 && m_FAAMatch.LatLong.IsSameLocation(this.LatLong, LocationTolerance);
            }
        }

        #endregion

        public airportImportCandidate() : base()
        {
            IATA = ICAO = FAA = string.Empty;
            FacilityTypeCode = "A";
            MatchStatusFAA = MatchStatusIATA = MatchStatusICAO = MatchStatus.Unknown;
        }

        public void CheckStatus(AirportList al = null)
        {
            if (al == null)
                al = new AirportList(String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", IATA, ICAO, FAA));

            airport[] rgap = al.GetAirportList();

            MatchStatusFAA = GetStatus(FAA, rgap, out m_MatchedFAAName, out m_FAAMatch);
            MatchStatusIATA = GetStatus(IATA, rgap, out m_MatchedIATAName, out m_IATAMatch);
            MatchStatusICAO = GetStatus(ICAO, rgap, out m_MatchedICAOName, out m_ICAOMatch);
        }

        public static bool StatusIsOK(MatchStatus ms)
        {
            return ms == MatchStatus.InDB || ms == MatchStatus.NotApplicable;
        }

        private MatchStatus GetStatus(string szcode, airport[] rgap, out string matchedName, out airport matchedAirport)
        {
            matchedName = string.Empty;
            matchedAirport = null;
            if (String.IsNullOrEmpty(szcode))
                return MatchStatus.NotApplicable;
            matchedAirport = Array.Find<airport>(rgap, ap2 => String.Compare(szcode, ap2.Code, StringComparison.InvariantCultureIgnoreCase) == 0);
            if (matchedAirport == null)
                return MatchStatus.NotInDB;

            matchedName = matchedAirport.Name;
            if (String.Compare(matchedAirport.FacilityTypeCode, FacilityTypeCode, StringComparison.InvariantCultureIgnoreCase) != 0)
                return MatchStatus.WrongType;
            if (this.LatLong == null || !matchedAirport.LatLong.IsSameLocation(this.LatLong, LocationTolerance))
            {
                MatchNotes = String.Format(CultureInfo.CurrentCulture, "{0}\r\nLoc In DB: {1}\r\nLoc in import: {2}\r\n", MatchNotes, matchedAirport.LatLong.ToString(), this.LatLong.ToString());
                return MatchStatus.InDBWrongLocation;
            }
            return MatchStatus.InDB;
        }

        private static string GetCol(string[] rgsz, int icol)
        {
            if (icol < 0 || icol > rgsz.Length)
                return string.Empty;
            return rgsz[icol];
        }

        public static IEnumerable<airportImportCandidate> Candidates(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            ImportAirportContext ic = new ImportAirportContext();

            List<airportImportCandidate> lst = new List<airportImportCandidate>();

            using (CSVReader csvReader = new CSVReader(s))
            {
                ic.InitFromHeader(csvReader.GetCSVLine(true));

                string[] rgCols = null;

                while ((rgCols = csvReader.GetCSVLine()) != null)
                {
                    airportImportCandidate aic = new airportImportCandidate()
                    {
                        FAA = GetCol(rgCols, ic.iColFAA).Replace("-", ""),
                        IATA = GetCol(rgCols, ic.iColIATA).Replace("-", ""),
                        ICAO = GetCol(rgCols, ic.iColICAO).Replace("-", ""),
                        Name = GetCol(rgCols, ic.iColName),
                        FacilityTypeCode = GetCol(rgCols, ic.iColType),
                        Country = GetCol(rgCols, ic.iColCountry),
                        Admin1 = GetCol(rgCols, ic.iColAdmin1)
                    };
                    if (String.IsNullOrEmpty(aic.FacilityTypeCode))
                        aic.FacilityTypeCode = "A";     // default to airport
                    aic.Name = GetCol(rgCols, ic.iColName);
                    aic.Code = "(TBD)";
                    string szLat = GetCol(rgCols, ic.iColLatitude);
                    string szLon = GetCol(rgCols, ic.iColLongitude);
                    string szLatLong = GetCol(rgCols, ic.iColLatLong);
                    aic.LatLong = null;

                    if (!String.IsNullOrEmpty(szLatLong))
                    {
                        // see if it is decimal; if so, we'll fall through.
                        if (Regex.IsMatch(szLatLong, "[NEWS]", RegexOptions.IgnoreCase))
                            aic.LatLong = DMSAngle.LatLonFromDMSString(GetCol(rgCols, ic.iColLatLong));
                        else
                        {
                            string[] rgsz = szLatLong.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (rgsz.Length == 2)
                            {
                                szLat = rgsz[0];
                                szLon = rgsz[1];
                            }
                        }
                    }
                    if (aic.LatLong == null)
                    {
                        aic.LatLong = new LatLong
                        {
                            Latitude = double.TryParse(szLat, out double d) ? d : new DMSAngle(szLat).Value,
                            Longitude = double.TryParse(szLon, out d) ? d : new DMSAngle(szLon).Value
                        };
                    }

                    lst.Add(aic);
                }

                return lst;
            }
        }

        public static void PopulateAirport(Control plc, airport ap, MatchStatus ms, airport aicBase)
        {
            if (ap == null)
                return;

            if (plc == null)
                throw new ArgumentNullException(nameof(plc));

            Panel p = new Panel();
            plc.Controls.Add(p);
            p.Controls.Add(new Label() { Text = ap.ToString() });
            p.Controls.Add(new LiteralControl("<br />"));
            p.Controls.Add(new Label() { Text = String.Format(CultureInfo.CurrentCulture, "Country: {0}, Admin1: {1}", 
                String.IsNullOrEmpty(ap.Country) ? "(NONE)" : ap.Country,
                String.IsNullOrEmpty(ap.Admin1) ? "(NONE)" : ap.Admin1) } );
            p.Controls.Add(new LiteralControl("<br />"));
            if (!StatusIsOK(ms))
            {
                p.CssClass = "notOK";
                Label lblStatus = new Label();
                p.Controls.Add(lblStatus);
                lblStatus.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.ImportAirportStatusTemplate, ms.ToString());
                lblStatus.ForeColor = System.Drawing.Color.Red;
                if (aicBase != null && ap.LatLong != null && aicBase.LatLong != null)
                {
                    Label lblDist = new Label();
                    p.Controls.Add(lblDist);
                    lblDist.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.ImportAirportDistanceTemplate, aicBase.DistanceFromAirport(ap));
                }
            }
            HyperLink h = new HyperLink();
            p.Controls.Add(h);
            h.Text = ap.LatLong.ToDegMinSecString();
            h.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:updateForAirport({0});", JsonConvert.SerializeObject(ap, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore }));
            if (!String.IsNullOrEmpty(ap.UserName))
                p.Controls.Add(new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<br />{0}<br />", ap.UserName)));
        }
    }
}