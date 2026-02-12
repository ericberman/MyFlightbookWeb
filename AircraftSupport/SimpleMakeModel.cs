using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A very simple version of make model for sending to mobile devices - just a display name + ID.  Other than the ID, unrelated to MakeModel
    /// </summary>
    [Serializable]
    public class SimpleMakeModel
    {
        /// <summary>
        /// The ID of the model
        /// </summary>
        public int ModelID { get; set; }

        /// <summary>
        /// The human-readable description of the model
        /// </summary>
        public string Description { get; set; }

        public SimpleMakeModel()
        {
            ModelID = -1;
            Description = string.Empty;
        }

        public static SimpleMakeModel[] GetAllMakeModels()
        {
            List<SimpleMakeModel> al = new List<SimpleMakeModel>();
            DBHelper dbh = new DBHelper(@"SELECT models.idmodel AS idmodel,
CONCAT(manufacturers.manufacturer, ' (', TRIM(CONCAT(models.model, IF(models.modelname='', '', CONCAT(' &quot;', models.modelname, '&quot;')))), ') - ', categoryclass.Catclass) AS MakeName
FROM models INNER JOIN manufacturers ON models.idmanufacturer = manufacturers.idManufacturer INNER JOIN categoryclass ON models.idcategoryclass=categoryclass.idcatclass
ORDER BY MakeName");
            if (!dbh.ReadRows(
                (comm) => { },
                (dr) =>
                {
                    SimpleMakeModel smm = new SimpleMakeModel() { Description = dr["MakeName"].ToString(), ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture) };
                    al.Add(smm);
                }))
                throw new MyFlightbookException("Error in GetAllMakeModels: " + dbh.LastError);

            return al.ToArray();
        }


        /// <summary>
        /// Returns a very simple list of models that are similar to the specified model.  ONLY WORKS FOR SIM-ONLY MODELS
        /// </summary>
        /// <param name="idModel"></param>
        /// <returns></returns>
        public static IEnumerable<SimpleMakeModel> ADMINModelsSimilarToSIM(int idModel)
        {
            List<SimpleMakeModel> lst = new List<SimpleMakeModel>();
            DBHelper dbh = new DBHelper(@"SELECT 
    m1.idmodel,
    m1.model,
    m1.typename,
    m1.family,
    man.manufacturer
FROM
    models m1
        INNER JOIN
    manufacturers man on m1.idmanufacturer = man.idmanufacturer
        LEFT JOIN
    models m2 on m2.idmodel = ?targetID
WHERE
    m1.idmodel <> m2.idmodel
		AND m1.family=m2.family
        AND m1.fSimOnly = 1
        AND m1.idcategoryclass = m2.idcategoryclass
        AND m1.fComplex = m2.fComplex
        AND m1.fhighperf = m2.fHighPerf
        AND m1.f200HP = m2.f200HP
        AND m1.fTailwheel = m2.fTailwheel
        AND m1.fConstantProp = m2.fConstantProp
        AND m1.fturbine = m2.fturbine
        AND m1.fretract = m2.fretract
        AND m1.fcertifiedsinglepilot = m2.fcertifiedsinglepilot
        AND m1.fcowlflaps = m2.fCowlFlaps
        AND m1.armymissiondesignseries = m2.armymissiondesignseries
        AND m1.fTAA = m2.fTAA 
        AND m1.fMotorGlider = m2.fMotorGlider
        AND m1.fMultiHelicopter = m2.fMultiHelicopter
ORDER BY man.manufacturer ASC, m1.model ASC");

            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("targetID", idModel); },
                (dr) => {
                    lst.Add(new SimpleMakeModel() { Description = $"{dr["manufacturer"]} - {dr["model"]}/{dr["family"]} Type: {dr["typename"]}", ModelID = Convert.ToInt32(dr["idmodel"], CultureInfo.InvariantCulture) });
                });
            return lst;
        }
    }


}