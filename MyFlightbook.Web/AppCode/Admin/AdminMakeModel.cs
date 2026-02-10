using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Admin
{

    /// <summary>
    /// Admin Utility functions for models.  All static functions.
    /// </summary>
    public class AdminMakeModel : MakeModel
    {
        /// <summary>
        /// Admin function to merge two duplicate models
        /// </summary>
        /// <param name="idModelToDelete">The id of the model that is redundant</param>
        /// <param name="idModelToMergeInto">The id of the model into which the redundant model should be merged</param>
        /// <returns>Audit of the operations that occor</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public static IEnumerable<string> AdminMergeDuplicateModels(int idModelToDelete, int idModelToMergeInto)
        {
            if (idModelToDelete < 0)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Invalid model to delete: {0}", idModelToDelete));
            if (idModelToDelete < 0)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Invalid model to merge into: {0}", idModelToMergeInto));

            List<string> lst = new List<string>() { "Audit of changes made" };

            // Before we migrate old aircraft, see if there are generics.
            Aircraft acGenericSource = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToDelete));
            Aircraft acGenericTarget = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToMergeInto));

            if (acGenericSource.AircraftID != Aircraft.idAircraftUnknown)
            {
                // if the generic for the target doesn't exist, then no problem - just rename it and remap it!
                if (acGenericTarget.AircraftID == Aircraft.idAircraftUnknown)
                {
                    acGenericSource.ModelID = idModelToMergeInto;
                    acGenericSource.TailNumber = Aircraft.AnonymousTailnumberForModel(idModelToMergeInto);
                    acGenericSource.Commit();
                }
                else
                {
                    // if the generic for the target also exists, need to merge the aircraft (creating a tombstone).
                    AdminAircraft.AdminMergeDupeAircraft(acGenericTarget, acGenericSource);
                }
            }

            IEnumerable<Aircraft> rgac = new UserAircraft(null).GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idModelToDelete);

            foreach (Aircraft ac in rgac)
            {
                // Issue #1068: if Aircraft already exists with idModelToMergeInto, do a merge
                List<Aircraft> lstSimilarTails = Aircraft.AircraftMatchingTail(ac.TailNumber);
                Aircraft acDupe = lstSimilarTails.FirstOrDefault(a => a.ModelID == idModelToMergeInto);

                if (acDupe == null) // no collision
                {
                    ac.ModelID = idModelToMergeInto;
                    ac.Commit();
                    lst.Add(String.Format(CultureInfo.CurrentCulture, "Updated aircraft {0} to model {1}", ac.AircraftID, idModelToMergeInto));
                }
                else
                {
                    // collision - do a merge instead!
                    AdminAircraft.AdminMergeDupeAircraft(acDupe, ac);
                    lst.Add(string.Format(CultureInfo.CurrentCulture, "Aircraft {0} exists with both models!  Merged", ac.TailNumber));
                }
            }

            // Update any custom currency references to the old model
            DBHelper dbhCCR = new DBHelper("UPDATE CustCurrencyRef SET value=?newID WHERE value=?oldID AND type=?modelsType");
            dbhCCR.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("newID", idModelToMergeInto);
                comm.Parameters.AddWithValue("oldID", idModelToDelete);
                comm.Parameters.AddWithValue("modelsType", (int)Currency.CustomCurrency.CurrencyRefType.Models);
            });

            // Then delete the old model
            string szQ = "DELETE FROM models WHERE idmodel=?idOldModel";
            DBHelper dbhDelete = new DBHelper(szQ);
            if (!dbhDelete.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idOldModel", idModelToDelete); }))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Error deleting model {0}: {1}", idModelToDelete, dbhDelete.LastError));
            lst.Add(String.Format(CultureInfo.CurrentCulture, "Deleted model {0}", idModelToDelete));
            util.FlushCache();
            return lst;
        }

        public static IEnumerable<MakeModel> ModelsThatShoulBeSims()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, "manufacturers.defaultSim <> 0 AND models.fSimOnly = 0"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> OrphanedModels()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, "LEFT JOIN aircraft ac ON models.idmodel=ac.idmodel", "ac.idaircraft IS NULL"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> TypeRatedModels()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty, "typename <> '' ORDER BY categoryclass.idcatclass ASC, manufacturers.manufacturer ASC, models.model ASC, models.typename ASC"));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static IEnumerable<MakeModel> PotentialDupes(bool fIncludeSims)
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szSQLSelectTemplate, string.Empty,
                String.Format(CultureInfo.InvariantCulture, @"UPPER(REPLACE(REPLACE(CONCAT(models.model, models.idcategoryclass,models.typename), '-', ''), ' ', '')) IN
                    (SELECT modelandtype FROM (SELECT model, COUNT(model) AS cModel, UPPER(REPLACE(REPLACE(CONCAT(m2.model,m2.idcategoryclass,m2.typename), '-', ''), ' ', '')) AS modelandtype FROM models m2 GROUP BY modelandtype HAVING cModel > 1) AS dupes)
                    {0}
                ORDER BY models.model", fIncludeSims ? string.Empty : "HAVING models.fSimOnly = 0")));
            List<MakeModel> lst = new List<MakeModel>();
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new MakeModel(dr)); });
            return lst;
        }

        public static void DeleteModel(int id)
        {
            DBHelper dbh = new DBHelper("DELETE FROM models WHERE idmodel=?idmodel");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idmodel", id); });
            util.FlushCache();
        }
    }

}