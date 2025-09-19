using MyFlightbook.Currency;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2015-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.StartingFlights
{
    /// <summary>
    /// Summary description for StartingTotals
    /// </summary>
    [Serializable]
    public class StartingFlight : LogbookEntryBase
    {
        /// <summary>
        /// The representative aircraft for this starting flight.
        /// </summary>
        public RepresentativeAircraft RepresentativeAircraft { get; set; }

        public StartingFlight(string szUser, RepresentativeAircraft ra)
            : base()
        {
            this.RepresentativeAircraft = ra ?? throw new ArgumentNullException(nameof(ra));
            this.AircraftID = ra.ExampleAircraft.AircraftID;
            this.User = szUser;
            this.Comment = Resources.LogbookEntry.StartingFlightComment;
        }

        /// <summary>
        /// Get a set of starting flights for the given user, in the given mode.
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <param name="mode">The mode: catclasstype (default), catclasscapabilities, or each model</param>
        /// <returns>A series of representative starting flights.</returns>
        public static Collection<StartingFlight> StartingFlightsForUser(string szUser, RepresentativeAircraft.RepresentativeTypeMode mode)
        {
            List<RepresentativeAircraft> lra = RepresentativeAircraft.RepresentativeAircraftForUser(szUser, mode);
            List<StartingFlight> lsf = new List<StartingFlight>();
            foreach (RepresentativeAircraft ra in lra)
            {
                if (ra != null && ra.ExampleAircraft != null)   // not sure why this can sometimes be null, but it can, so check for it...
                    lsf.Add(new StartingFlight(szUser, ra));
            }
            return new Collection<StartingFlight>(lsf);
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}: Total: {1}", RepresentativeAircraft.ToString(), TotalFlightTime);
        }

        /// <summary>
        /// Deserialize a starting flight that is represented as an sequence of strings.  MUST be
        ///  - 1st string: the ID of the aircraft
        ///  - 2nd string: the PIC time
        ///  - 3rd string: the SIC time
        ///  - 4th string: the CFI time
        ///  - 5th string: the Total time
        /// </summary>
        /// <param name="rgrow">The array of strings</param>
        /// <param name="lstRa">A</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static StartingFlight DeserializeStartingFlightRow(string[] rgrow, DateTime date, string userName, List<RepresentativeAircraft> lstRa)
        {
            if (rgrow == null)
                throw new ArgumentNullException(nameof(rgrow));
            if (rgrow.Length != 5)
                throw new InvalidOperationException("Each row needs exactly 5 elements");
            int idAircraft = Convert.ToInt32(rgrow[0], CultureInfo.InvariantCulture);
            return new StartingFlight(userName, lstRa.FirstOrDefault(ra => ra.ExampleAircraft.AircraftID == idAircraft))
            {
                PIC = rgrow[1].SafeParseDecimal(),
                SIC = rgrow[2].SafeParseDecimal(),
                CFI = rgrow[3].SafeParseDecimal(),
                TotalFlightTime = rgrow[4].SafeParseDecimal(),
                Date = date
            };
        }

        /// <summary>
        /// Deserializes a JSON-encoded array of string arrays into a set of starting flights.
        /// Each string array in the big array MUST be 5 elements:
        ///  - 1st string: the ID of the aircraft
        ///  - 2nd string: the PIC time
        ///  - 3rd string: the SIC time
        ///  - 4th string: the CFI time
        ///  - 5th string: the Total time
        /// </summary>
        /// <param name="StartingTotalRows">The jsonified array</param>
        /// <param name="userName"></param>
        /// <param name="mode"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static IEnumerable<StartingFlight> DeserializeStartingFlights(string StartingTotalRows, string userName, RepresentativeAircraft.RepresentativeTypeMode mode, DateTime date)
        {
            if (String.IsNullOrEmpty(StartingTotalRows))
                throw new ArgumentNullException(nameof(StartingTotalRows));
            if (string.IsNullOrEmpty(userName)) 
                throw new ArgumentNullException(nameof(userName));
            if (!date.HasValue())
                throw new InvalidOperationException("Date must have a value");

            // Get the representative aircraft for this mode for quick access in creating the flights.
            List<RepresentativeAircraft> lstRa = RepresentativeAircraft.RepresentativeAircraftForUser(userName, mode);

            string[][] rows = JsonConvert.DeserializeObject<string[][]>(StartingTotalRows);
            List<StartingFlight> lst = new List<StartingFlight>();
            foreach (string[] rgrow in rows)
                lst.Add(DeserializeStartingFlightRow(rgrow, date, userName, lstRa));
            return lst;
        }

        public static void CommitStartingFlights(IEnumerable<StartingFlight> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            foreach (StartingFlight sf in lst)
            {
                if (sf.PIC + sf.SIC + sf.CFI + sf.TotalFlightTime > 0)
                    sf.FCommit();
            }
        }

        public static void DeleteStartingFlights(IEnumerable<StartingFlight> lst, string szUser)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            foreach (StartingFlight sf in lst)
                if (!sf.IsNewFlight)
                {
                    FDeleteEntry(sf.FlightID, szUser);
                    sf.FlightID = idFlightNew;
                }
        }

        /// <summary>
        /// Returns a 2-element array of totals-list items, the first doesn't include the set of starting flights, and the second one does.
        /// </summary>
        /// <param name="flights"></param>
        /// <param name="szUser"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TotalsItem>[] BeforeAndAfterTotalsForUser(IEnumerable<StartingFlight> flights, string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (flights == null)
                throw new ArgumentNullException(nameof(flights));
            List<IEnumerable<TotalsItem>>  lstResult = new List<IEnumerable<TotalsItem>>();
            UserTotals ut = new UserTotals(szUser, new FlightQuery(szUser), true);
            // get the "before" totals
            ut.DataBind();
            lstResult.Add(ut.Totals.ToArray());

            // get the "after" totals
            CommitStartingFlights(flights);
            ut.DataBind();
            lstResult.Add(ut.Totals.ToArray());
            DeleteStartingFlights(flights, szUser);
            return lstResult.ToArray();
        }
    }

    /// <summary>
    /// Represents a canonical aircraft type - e.g., "ASEL" or "AMEL (B767-300)", or "ASEL - Retract"
    /// </summary>
    [Serializable]
    public class RepresentativeAircraft
    {
        public enum RepresentativeTypeMode { CatClassType, CatClassCapabilities, ByModel };

        #region Querystrings
        // Issue #1461 - wasn't striping correctly by type if you have multiple type ratings.
        private const string sqlRepresentativeTypesBase = @"
    SELECT
    CONCAT(cc.CatClass, IF(m.typename = '', '', CONCAT(' (', m.typename, ')'))) AS catclasstype,
    CONCAT(
      cc.CatClass,
      IF(m.fTurbine, 'B', ' '),
      IF(m.fRetract, 'R', ' '),
      IF(m.fComplex, 'X', ' '),
      IF(m.fHighPerf, 'H', ' '),
      IF(m.fTailwheel, 'T', ' '),
      IF(m.fConstantProp, 'C', ' '),
      m.typename
      ) AS ModelSignature,
m.fComplex+m.fHighPerf+m.fTailwheel+m.fConstantProp+m.fTurbine+m.fRetract AS Capabilities,
GROUP_CONCAT(cast(ac.idAircraft AS char(8)) separator ',') AS AircraftIDs,
GROUP_CONCAT(ac.tailnumber separator ',') AS Tailnumbers,
m.*
FROM aircraft ac
INNER JOIN useraircraft ua ON ua.idAircraft = ac.idAircraft
INNER JOIN models m ON ac.idmodel=m.idmodel
INNER JOIN CategoryClass cc ON m.idcategoryclass=cc.idCatClass
WHERE ua.username=?uName AND ac.instancetype=1
GROUP BY {0}
ORDER BY catclasstype ASC, Capabilities ASC, ModelSignature DESC
    ";

        private static readonly string sqlAircraftModels = String.Format(System.Globalization.CultureInfo.InvariantCulture, sqlRepresentativeTypesBase, "m.idmodel");
        private static readonly string sqlAircraftCatClassCapabilities = String.Format(System.Globalization.CultureInfo.InvariantCulture, sqlRepresentativeTypesBase, "ModelSignature");
        private static readonly string sqlAircraftCatClassType = String.Format(System.Globalization.CultureInfo.InvariantCulture, "SELECT * FROM ({0}) types GROUP BY types.catclasstype", sqlAircraftCatClassCapabilities);
        #endregion

        #region Properties
        /// <summary>
        /// An aircraft that represents the particular CatClassType, CatClass + capabilities, or simple model.
        /// </summary>
        public Aircraft ExampleAircraft { get; set; }

        public bool IsTurbine { get; set; }
        public bool IsRetract { get; set; }
        public bool IsComplex { get; set; }
        public bool IsHighPerf { get; set; }
        public bool IsTailwheel { get; set; }
        public bool IsConstantProp { get; set; }

        /// <summary>
        /// The name for the representative aircraft type (e.g., "AMEL (B777-300)" or the model name or the CatClass + capabilities
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The descriptor for the representative aircraft (catclasstype, catclass, capabilities)
        /// </summary>
        public string Descriptor
        {
            get
            {
                List<string> l = new List<string>();
                if (IsTurbine) l.Add(Resources.FlightQuery.AircraftFeatureTurbine);
                if (IsHighPerf) l.Add(Resources.FlightQuery.AircraftFeatureHighPerformance);
                if (IsComplex)
                    l.Add(Resources.FlightQuery.AircraftFeatureComplex);
                else
                {
                    if (IsConstantProp) l.Add(Resources.FlightQuery.AircraftFeatureConstantSpeedProp);
                    if (IsRetract) l.Add(Resources.FlightQuery.AircraftFeatureRetractableGear);
                }
                if (IsTailwheel) l.Add(Resources.FlightQuery.AircraftFeatureTailwheel);
                return String.Join(", ", l.ToArray());
            }
        }
        #endregion

        public RepresentativeAircraft()
        {
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}: {1} ({2}). {3}", Name, ExampleAircraft.TailNumber, ExampleAircraft.ModelCommonName, Descriptor);
        }

        public static List<RepresentativeAircraft> RepresentativeAircraftForUser(string szUser, RepresentativeTypeMode mode)
        {
            List<RepresentativeAircraft> l = new List<RepresentativeAircraft>();

            UserAircraft ua = new UserAircraft(szUser);

            DBHelper dbh = new DBHelper();
            switch (mode)
            {
                case RepresentativeTypeMode.ByModel:
                    dbh.CommandText = sqlAircraftModels;
                    break;
                case RepresentativeTypeMode.CatClassCapabilities:
                    dbh.CommandText = sqlAircraftCatClassCapabilities;
                    break;
                default:
                case RepresentativeTypeMode.CatClassType:
                    dbh.CommandText = sqlAircraftCatClassType;
                    break;
            }

            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("?uName", szUser); },
                (dr) =>
                {
                    try
                    {
                        RepresentativeAircraft ra = new RepresentativeAircraft();

                        string aircraftIDs = (string)dr["AircraftIDs"];
                        string[] rgIDs = aircraftIDs.Split(',');
                        ra.ExampleAircraft = ua.GetUserAircraftByID(Convert.ToInt32(rgIDs[0], CultureInfo.InvariantCulture));

                        ra.IsComplex = Convert.ToBoolean(dr["fComplex"], CultureInfo.InvariantCulture);
                        ra.IsConstantProp = Convert.ToBoolean(dr["fConstantProp"], CultureInfo.InvariantCulture);
                        ra.IsHighPerf = Convert.ToBoolean(dr["fHighPerf"], CultureInfo.InvariantCulture);
                        ra.IsRetract = Convert.ToBoolean(dr["fRetract"], CultureInfo.InvariantCulture);
                        ra.IsTailwheel = Convert.ToBoolean(dr["fTailwheel"], CultureInfo.InvariantCulture);
                        ra.IsTurbine = Convert.ToBoolean(dr["fTurbine"], CultureInfo.InvariantCulture);

                        switch (mode)
                        {
                            case RepresentativeTypeMode.ByModel:
                                ra.Name = ra.ExampleAircraft.ModelCommonName;
                                break;
                            default:
                            case RepresentativeTypeMode.CatClassCapabilities:
                            case RepresentativeTypeMode.CatClassType:
                                ra.Name = (string)dr["catclasstype"];
                                break;
                        }

                        l.Add(ra);
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                    }
                }
             );

            return l;
        }
    }
}