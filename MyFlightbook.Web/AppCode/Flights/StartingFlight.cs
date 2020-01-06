using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.StartingFlight
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
            if (ra == null)
                throw new ArgumentNullException("ra");
            this.RepresentativeAircraft = ra;
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
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: Total: {1}", RepresentativeAircraft.ToString(), TotalFlightTime);
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
        private static string sqlRepresentativeTypesBase = @"
    SELECT
    CONCAT(cc.CatClass, IF(m.typename = '', '', CONCAT(' (', m.typename, ')'))) AS catclasstype,
    CONCAT(
      cc.CatClass,
      IF(m.fTurbine, 'B', ' '),
      IF(m.fRetract, 'R', ' '),
      IF(m.fComplex, 'X', ' '),
      IF(m.fHighPerf, 'H', ' '),
      IF(m.fTailwheel, 'T', ' '),
      IF(m.fConstantProp, 'C', ' ')
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

        private static string sqlAircraftModels = String.Format(System.Globalization.CultureInfo.InvariantCulture, sqlRepresentativeTypesBase, "m.idmodel");
        private static string sqlAircraftCatClassCapabilities = String.Format(System.Globalization.CultureInfo.InvariantCulture, sqlRepresentativeTypesBase, "ModelSignature");
        private static string sqlAircraftCatClassType = String.Format(System.Globalization.CultureInfo.InvariantCulture, "SELECT * FROM ({0}) types GROUP BY types.catclasstype", sqlAircraftCatClassCapabilities);
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
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: {1} ({2}). {3}", Name, ExampleAircraft.TailNumber, ExampleAircraft.ModelCommonName, Descriptor);
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
                        ra.ExampleAircraft = ua.GetUserAircraftByID(Convert.ToInt32(rgIDs[0]));

                        ra.IsComplex = Convert.ToBoolean(dr["fComplex"]);
                        ra.IsConstantProp = Convert.ToBoolean(dr["fConstantProp"]);
                        ra.IsHighPerf = Convert.ToBoolean(dr["fHighPerf"]);
                        ra.IsRetract = Convert.ToBoolean(dr["fRetract"]);
                        ra.IsTailwheel = Convert.ToBoolean(dr["fTailwheel"]);
                        ra.IsTurbine = Convert.ToBoolean(dr["fTurbine"]);

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
                    catch
                    {
                    }
                }
             );

            return l;
        }
    }
}