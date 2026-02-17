using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2012-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Keeps track of deleted aircraft
    /// </summary>
    public class AircraftTombstone
    {
        #region properties
        /// <summary>
        /// The ID of the old aircraft (now deleted)
        /// </summary>
        public int OldAircraftID { get; set; }

        /// <summary>
        /// The ID of the new aircraft
        /// </summary>
        public int NewAircraftID { get; set; }
        #endregion

        #region constructors
        public AircraftTombstone() { }

        /// <summary>
        /// Creates a new tombstone mapping the specified old ID to the new ID
        /// </summary>
        /// <param name="idAircraftOld">The id of the aircraft to tombstone</param>
        /// <param name="idAircraftNew">The id of the new aircraft</param>
        public AircraftTombstone(int idAircraftOld, int idAircraftNew) : this()
        {
            OldAircraftID = idAircraftOld;
            NewAircraftID = idAircraftNew;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idAircraftOld"></param>
        public AircraftTombstone(int idAircraftOld) : this()
        {
            OldAircraftID = NewAircraftID = idAircraftOld;

            DBHelper dbh = new DBHelper("SELECT * FROM aircrafttombstones WHERE idDeletedAircraft=?idOld");

            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("idOld", idAircraftOld); },
                (dr) =>
                {
                    OldAircraftID = Convert.ToInt32(dr["idDeletedAircraft"], CultureInfo.InvariantCulture);
                    NewAircraftID = Convert.ToInt32(dr["idMappedAircraft"], CultureInfo.InvariantCulture);
                });
        }
        #endregion

        public bool IsValid()
        {
            return OldAircraftID != NewAircraftID && OldAircraftID > 0 && NewAircraftID > 0;
        }

        /// <summary>
        /// Maps the specified aircraft ID to a new one, if necessary
        /// </summary>
        /// <param name="idAircraft">The aircraft that could be mapped</param>
        /// <returns>The mapped ID, or the original if it's fine.</returns>
        public static int MapAircraftID(int idAircraft)
        {
            AircraftTombstone act = new AircraftTombstone(idAircraft);
            return act.IsValid() ? act.NewAircraftID : idAircraft;
        }

        /// <summary>
        /// Save the tombstone to the db.  ALWAYS DOES AN INSERT, WILL FAIL if called a second time.
        /// </summary>
        public void Commit()
        {
            if (!IsValid())
                return;

            AircraftTombstone act = new AircraftTombstone(OldAircraftID);
            if (act.IsValid())
                return;

            new DBHelper().DoNonQuery("INSERT INTO aircrafttombstones SET idDeletedAircraft=?idOld, idMappedAircraft=?idNew",
                (comm) =>
                {
                    comm.Parameters.AddWithValue("idOld", OldAircraftID);
                    comm.Parameters.AddWithValue("idNew", NewAircraftID);
                });
        }
    }

}