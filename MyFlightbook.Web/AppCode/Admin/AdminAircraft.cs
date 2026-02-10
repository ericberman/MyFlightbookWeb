using MyFlightbook.CSV;
using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static MyFlightbook.AircraftInstance;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Admin
{
    public static class AdminAircraft
    {
        /// <summary>
        /// Admin utility to quickly find all invalid aircraft (since examining them one at a time is painfully slow and pounds the database)
        /// KEEP IN SYNC WITH IsValid!!
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Aircraft> AdminAllInvalidAircraft()
        {
            List<Aircraft> lst = new List<Aircraft>();

            List<string> lstNakedPrefix = new List<string>();
            foreach (CountryCodePrefix cc in CountryCodePrefix.CountryCodes())
                lstNakedPrefix.Add(cc.NormalizedPrefix);

            string szNaked = String.Join("', '", lstNakedPrefix);

            const string szQInvalidAircraftRestriction = @"WHERE
(aircraft.idmodel < 0 OR models.idmodel IS NULL)
OR (aircraft.tailnumber = '') OR (LENGTH(aircraft.tailnumber) > {0})
OR (aircraft.tailnumber LIKE '{2}%' AND aircraft.tailnumber <> CONCAT('{2}', LPAD(aircraft.idmodel, 6, '0')))
OR (aircraft.tailnumber NOT LIKE '{2}%' AND aircraft.tailnumber NOT RLIKE '{1}')
OR (aircraft.instancetype = 1 AND aircraft.tailnumber LIKE '{3}%')
OR (aircraft.instancetype <> 1 AND aircraft.tailnumber NOT LIKE '{3}%')
OR (models.fSimOnly = 1 AND aircraft.instancetype={4})
OR (models.fSimOnly = 2 AND aircraft.InstanceType={4} AND aircraft.tailnumber NOT LIKE '{2}%')
OR (aircraft.tailnormal IN ('{5}'))";

            string szQInvalid = String.Format(CultureInfo.InvariantCulture, szQInvalidAircraftRestriction,
                Aircraft.maxTailLength,
                AircraftUtility.RegexValidTail,
                CountryCodePrefix.szAnonPrefix,
                CountryCodePrefix.szSimPrefix,
                (int)AircraftInstanceTypes.RealAircraft,
                szNaked);

            string szQ = String.Format(CultureInfo.InvariantCulture, Aircraft.szAircraftForUserCore, 0, "''", "''", "''", szQInvalid);

            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new Aircraft(dr)); });

            // set the error for each one
            foreach (Aircraft ac in lst)
                ac.IsValid(true);

            return lst;
        }

        /// <summary>
        /// Admin function to merge an old aircraft into a new one
        /// </summary>
        /// <param name="acMaster">The TARGET aircraft</param>
        /// <param name="ac">The aircraft being merged - this one will be DELETED (but a tombstone will remain)</param>
        public static void AdminMergeDupeAircraft(Aircraft acMaster, Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (acMaster == null)
                throw new ArgumentNullException(nameof(acMaster));
            if (ac.AircraftID == acMaster.AircraftID)
                return;

            // merge the aircraft into the master.  This will merge maintenance and images.
            acMaster.MergeWith(ac, true);

            // map all future references to this aircraft to the new master
            AircraftTombstone act = new AircraftTombstone(ac.AircraftID, acMaster.AircraftID);
            act.Commit();

            // It's slower than doing a simple "UPDATE Flights f SET f.idAircraft=?idAircraftNew WHERE f.idAircraft=?idAircraftOld",
            // but first find all of the users with flights in the old aircraft and then call ReplaceAircraftForUser on each one
            // This ensures that useraircraft is correctly updated.
            DBHelperCommandArgs dba = new DBHelperCommandArgs("SELECT DISTINCT username FROM flights WHERE idAircraft=?idAircraftOld");
            dba.AddWithValue("idAircraftNew", acMaster.AircraftID);
            dba.AddWithValue("idAircraftOld", ac.AircraftID);
            // Remap any flights that use the old aircraft
            DBHelper dbh = new DBHelper(dba);

            List<string> lstAffectedUsers = new List<string>();
            dbh.ReadRows((comm) => { }, (dr) => { lstAffectedUsers.Add((string)dr["username"]); });

            foreach (string szUser in lstAffectedUsers)
                new UserAircraft(szUser).ReplaceAircraftForUser(acMaster, ac, true);

            // remap any club aircraft and scheduled events.
            dbh.CommandText = "UPDATE clubaircraft ca SET ca.idaircraft=?idAircraftNew WHERE ca.idAircraft=?idAircraftOld";
            dbh.DoNonQuery();
            dbh.CommandText = "UPDATE scheduledEvents se SET se.resourceid=?idAircraftNew WHERE se.resourceid=?idAircraftOld";
            dbh.DoNonQuery();

            // User aircraft might already have the target, which can lead to a duplicate primary key error.
            // So first find everybody that has both in their account; for them, we'll just delete the old (since it's redundant).
            List<string> lstUsersWithBoth = new List<string>();
            dbh.CommandText = "SELECT ua1.username FROM useraircraft ua1 INNER JOIN useraircraft ua2 ON ua1.username = ua2.username WHERE ua1.idaircraft=?idAircraftOld AND ua2.idaircraft=?idAircraftNew";
            dbh.ReadRows((comm) => { }, (dr) => { lstUsersWithBoth.Add((string)dr["username"]); });
            foreach (string szUser in lstUsersWithBoth)
            {
                DBHelper dbhDelete = new DBHelper("DELETE FROM useraircraft WHERE username=?user AND idAircraft=?idAircraftOld");
                dbhDelete.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("user", szUser);
                    comm.Parameters.AddWithValue("idAircraftOld", ac.AircraftID);
                });
            }
            // should now be safe to map the remaining ones.
            dbh.CommandText = "UPDATE useraircraft ua SET ua.idAircraft=?idAircraftNew WHERE ua.idAircraft=?idAircraftOld";
            dbh.DoNonQuery();

            // And fix up any pre-existing tombstones that point to this aircraft
            dbh.CommandText = "UPDATE aircrafttombstones SET idMappedAircraft=?idAircraftNew WHERE idMappedAircraft=?idAircraftOld";
            dbh.DoNonQuery();

            // Finally, it should now be safe to delete the aircraft
            dbh.CommandText = "DELETE FROM aircraft WHERE idAircraft=?idAircraftOld";
            dbh.DoNonQuery();
        }

        public static IEnumerable<Aircraft> AdminDupeAircraft()
        {
            List<Aircraft> lst = new List<Aircraft>();

            DBHelper dbh = new DBHelper(@"SELECT
    ac.*,
    models.*,
	IF (ac.Tailnumber LIKE '#%', CONCAT('#', models.model), ac.TailNormal) AS sortKey,
    IF (ac.InstanceType = 1, '', Concat(' (', aircraftinstancetypes.Description, ')')) as 'InstanceTypeDesc',
    TRIM(CONCAT(manufacturers.manufacturer, ' ', CONCAT(COALESCE(models.typename, ''), ' '), models.modelname)) AS 'ModelCommonName',
    0 AS Flags,
    '' AS DefaultImage,
    '' AS UserNotes,
    '' AS TemplateIDs,
    COUNT(DISTINCT f.idflight) AS numFlights,
    COUNT(DISTINCT ua.username) AS numUsers,
    0 AS flightsForUser,
    '' AS userNames,
    NULL AS EarliestDate,
    NULL AS LatestDate,
    0 AS hours
FROM Aircraft ac
    INNER JOIN models ON ac.idmodel=models.idmodel
    INNER JOIN manufacturers ON manufacturers.idManufacturer=models.idmanufacturer
    INNER JOIN aircraftinstancetypes ON ac.InstanceType=aircraftinstancetypes.ID
    LEFT JOIN flights f on f.idaircraft = ac.idaircraft
    LEFT JOIN useraircraft ua ON ua.idaircraft=ac.idaircraft
WHERE ac.tailnormal IN
    (SELECT NormalizedTail FROM
        (SELECT ac.tailnormal AS NormalizedTail,
             CONCAT(ac.tailnormal, ',', Version) AS TailMatch,
             COUNT(idAircraft) AS cAircraft
         FROM Aircraft ac
         GROUP BY TailMatch
         HAVING cAircraft > 1) AS Dupes)
GROUP BY ac.idaircraft
ORDER BY tailnormal ASC, version, numUsers DESC, idaircraft ASC");

            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new Aircraft(dr) { Stats = new AircraftStats(dr) }); });

            return lst;
        }

        public static IEnumerable<Aircraft> AdminDupeSims()
        {
            List<Aircraft> lst = new List<Aircraft>();
            DBHelper dbh = new DBHelper(@"SELECT 
	aircraft.*, 
	models.*, 
	IF (aircraft.Tailnumber LIKE '#%', CONCAT('#', models.model), aircraft.TailNormal) AS sortKey,
	if (aircraft.InstanceType = 1, '', Concat(' (', aircraftinstancetypes.Description, ')')) as 'InstanceTypeDesc',
	TRIM(CONCAT(manufacturers.manufacturer, ' ', CONCAT(COALESCE(models.typename, ''), ' '), models.modelname)) AS 'ModelCommonName',
	0 AS Flags,
	'' AS DefaultImage,
	'' AS UserNotes,
	'' AS TemplateIDs,
	COUNT(DISTINCT f.idflight) AS numFlights,
	COUNT(DISTINCT ua.username) AS numUsers,
	0 AS flightsForUser,
	'' AS userNames,
    NULL AS EarliestDate,
    NULL AS LatestDate,
    0 AS hours
FROM aircraft 
	INNER JOIN models ON aircraft.idmodel=models.idmodel 
	INNER JOIN manufacturers ON manufacturers.idManufacturer=models.idmanufacturer 
	INNER JOIN aircraftinstancetypes ON aircraft.InstanceType=aircraftinstancetypes.ID
	INNER JOIN Aircraft ac2 on aircraft.idmodel = ac2.idmodel AND aircraft.instancetype=ac2.instancetype AND aircraft.idaircraft <> ac2.idaircraft
	LEFT JOIN flights f on f.idaircraft = aircraft.idaircraft
	LEFT JOIN useraircraft ua ON ua.idaircraft=aircraft.idaircraft
WHERE aircraft.instancetype <> 1 AND ac2.instancetype <> 1 
GROUP BY aircraft.idaircraft
ORDER BY aircraft.instancetype, aircraft.idmodel");

            dbh.ReadRows((comm) => { },
    (dr) => { lst.Add(new Aircraft(dr) { Stats = new AircraftStats(dr) }); });

            return lst;
        }

        public static IEnumerable<Aircraft> ResolveDupeSim(int idAircraft)
        {
            Aircraft acMaster = new Aircraft(idAircraft);
            if (acMaster.AircraftID < 0)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Invalid ID to resolve: {0}", idAircraft));
            if (acMaster.InstanceType == AircraftInstanceTypes.RealAircraft)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Aircraft is not a sim: {0}", idAircraft));
            List<int> AircraftToMergeToThis = new List<int>();
            DBHelper dbh = new DBHelper("SELECT idAircraft FROM Aircraft WHERE idmodel=?m AND instanceType=?i AND idAircraft <> ?acid");
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("m", acMaster.ModelID);
                comm.Parameters.AddWithValue("i", acMaster.InstanceTypeID);
                comm.Parameters.AddWithValue("acid", idAircraft);
            },
            (dr) => { AircraftToMergeToThis.Add(Convert.ToInt32(dr["idAircraft"], CultureInfo.InvariantCulture)); });

            // Merge each of the dupes to the one we want to keep
            foreach (int acID in AircraftToMergeToThis)
            {
                Aircraft acToMerge = new Aircraft(acID);
                AdminMergeDupeAircraft(acMaster, acToMerge);
            }

            return AdminDupeSims();
        }

        public static IEnumerable<Aircraft> OrphanedAircraft()
        {
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, Aircraft.szAircraftForUserCore, 0, "''", "''", "''", @"LEFT JOIN useraircraft ua ON ua.idaircraft=aircraft.idaircraft WHERE ua.idaircraft IS NULL"));

            List<Aircraft> lstAc = new List<Aircraft>();
            dbh.ReadRows(
                (comm) => { }, (dr) => { lstAc.Add(new Aircraft(dr)); });
            return lstAc;
        }

        private static readonly LazyRegex rWild = new LazyRegex("[^a-zA-Z0-9#?]");

        public static IEnumerable<Aircraft> AircraftMatchingPattern(string szPattern)
        {
            if (String.IsNullOrEmpty(szPattern))
                return Array.Empty<Aircraft>();

            string szTailToMatch = rWild.Replace(szPattern, "*").ConvertToMySQLWildcards();

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, Aircraft.szAircraftForUserCore, 0, "''", "''", "''", @"WHERE tailnormal LIKE ?tailNum"));

            List<Aircraft> lstAc = new List<Aircraft>();
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("tailNum", szTailToMatch); },
                (dr) => { lstAc.Add(new Aircraft(dr)); });
            return lstAc;
        }

        public static IEnumerable<Aircraft> PseudoGenericAircraft()
        {
            DBHelper dbh = new DBHelper(@"SELECT 
	aircraft.*, 
	models.*, 
	IF (aircraft.Tailnumber LIKE '#%', CONCAT('#', models.model), aircraft.TailNormal) AS sortKey,
	if (aircraft.InstanceType = 1, '', Concat(' (', aircraftinstancetypes.Description, ')')) as 'InstanceTypeDesc',
	TRIM(CONCAT(manufacturers.manufacturer, ' ', CONCAT(COALESCE(models.typename, ''), ' '), models.modelname)) AS 'ModelCommonName',
	0 AS Flags,
	'' AS DefaultImage,
	'' AS UserNotes,
	'' AS TemplateIDs,
	COUNT(DISTINCT f.idflight) AS numFlights,
	COUNT(DISTINCT ua.username) AS numUsers,
	0 AS flightsForUser,
	'' AS userNames,
    NULL AS EarliestDate,
    NULL AS LatestDate,
    0 AS hours
FROM aircraft
	INNER JOIN models ON aircraft.idmodel=models.idmodel
	INNER JOIN manufacturers ON models.idmanufacturer=manufacturers.idmanufacturer
	INNER JOIN aircraftinstancetypes ON aircraft.InstanceType=aircraftinstancetypes.ID
	LEFT JOIN Flights f ON f.idaircraft=aircraft.idaircraft
	LEFT JOIN useraircraft ua ON ua.idaircraft=aircraft.idaircraft
WHERE
	((aircraft.tailnormal RLIKE '^N[ABD-FH-KM-QT-WYZ][-0-9A-Z]+' AND aircraft.tailnormal NOT RLIKE '^NZ[0-9]{2,4}$')
    OR aircraft.tailnormal RLIKE '^N.*[ioIO]'
    OR aircraft.tailnormal LIKE 'N0%'
    OR LEFT(aircraft.tailnormal, 4) = LEFT(REPLACE(models.model, '-', ''), 4)
    OR RIGHT(aircraft.tailnormal, LENGTH(aircraft.tailnumber) - 1) = REPLACE(RIGHT(models.model, LENGTH(models.model) - 1), '-', '')
    OR (LEFT(aircraft.tailnumber, 3) <> 'SIM' AND (LEFT(aircraft.tailnormal, 4) = LEFT(manufacturers.manufacturer, 4)))
    OR (aircraft.instancetype=1 AND aircraft.tailnormal RLIKE 'SIM|FTD|ATD|FFS|REDB|FSTD|ANON|FRAS|ELIT|CAE|ALSIM|FLIG|SAFE|PREC|TRUF|FMX|GROU|VARI|MISC|NONE|UNKN|OTHE|FAA|MENTO|TAIL'))
    AND aircraft.publicnotes NOT LIKE '% '
GROUP BY aircraft.idaircraft
ORDER BY tailnumber ASC");

            List<Aircraft> lstAc = new List<Aircraft>();
            dbh.ReadRows((comm) => { }, (dr) => { lstAc.Add(new Aircraft(dr) { Stats = new AircraftStats(dr) }); });
            return lstAc;
        }

        public static string FixedPsuedoTail(string szTail)
        {
            if (String.IsNullOrEmpty(szTail))
                return szTail;

            GroupCollection gc = RegexUtility.ADMINPseudoSim.Match(szTail).Groups;
            string szTailnumFixed = szTail;
            if (gc != null && gc.Count > 1)
                szTailnumFixed = String.Format(CultureInfo.InvariantCulture, "N{0}", gc[1].Value);
            else if (RegexUtility.ADMINZeroOrIConfusion.IsMatch(szTail))
                szTailnumFixed = szTail.ToUpper(CultureInfo.CurrentCulture).Replace('I', '1').Replace('O', '0');
            else if (szTailnumFixed.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase))
                szTailnumFixed = "N" + szTailnumFixed.Substring(2);

            return szTailnumFixed;
        }

        public static bool HasMixedOorI(string szTail)
        {
            return RegexUtility.ADMINZeroOrIConfusion.IsMatch(szTail ?? string.Empty);
        }

        public static void UpdateVersionForAircraft(Aircraft aircraft, int newVersion)
        {
            if (aircraft == null)
                throw new ArgumentNullException(nameof(aircraft));
            if (newVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(newVersion));

            DBHelper dbh = new DBHelper("UPDATE aircraft SET version=?Version WHERE idaircraft=?idaircraft");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Version", newVersion);
                comm.Parameters.AddWithValue("idaircraft", aircraft.AircraftID);
            });
        }

        public static string CleanUpMaintenance()
        {
            const string szSQLMaintainedVirtualAircraft = @"SELECT ac.*, group_concat(ml.id), group_concat(ml.Description)
FROM aircraft ac 
INNER JOIN maintenancelog ml ON ac.idaircraft=ml.idaircraft
WHERE (ac.tailnumber LIKE 'SIM%' OR ac.tailnumber LIKE '#%' OR ac.InstanceType <> 1) AND ml.idAircraft IS NOT NULL
GROUP BY ac.idaircraft";
            const string szSQLDeleteVirtualMaintenanceDates = @"UPDATE aircraft
SET lastannual=null, lastPitotStatic=null, lastVOR=null, lastAltimeter=null, lasttransponder=null, registrationdue=null, glassupgradedate=null
WHERE (tailnumber LIKE 'SIM%' OR tailnumber LIKE '#%' OR InstanceType <> 1) ";

            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper(szSQLMaintainedVirtualAircraft);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture)); });
            if (lst.Count != 0)
            {
                IEnumerable<Aircraft> rgac = Aircraft.AircraftFromIDs(lst);
                DBHelper dbhDelMaintenance = new DBHelper("DELETE FROM maintenancelog WHERE idAircraft=?idac");
                foreach (Aircraft ac in rgac)
                {
                    // clean up the maintenance
                    ac.Last100 = ac.LastNewEngine = ac.LastOilChange = 0.0M;
                    ac.LastAltimeter = ac.LastAnnual = ac.LastELT = ac.LastStatic = ac.LastTransponder = ac.LastVOR = ac.RegistrationDue = DateTime.MinValue;
                    ac.Commit();

                    // and then delete any maintenance records for this.
                    dbhDelMaintenance.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idac", ac.AircraftID); });
                }
            }
            dbh.CommandText = szSQLDeleteVirtualMaintenanceDates;
            dbh.DoNonQuery();
            return String.Format(CultureInfo.CurrentCulture, "Maintenance cleaned up, {0} maintenance logs cleaned, all virtual aircraft had dates nullified", lst.Count);
        }

        /// <summary>
        /// Determines the likely tim type based on the name, in case it contains something that looks like a psuedo-sim.
        /// </summary>
        /// <param name="szTail"></param>
        /// <returns></returns>
        private static AircraftInstanceTypes PseudoSimTypeFromTail(string szTail)
        {
            if (szTail == null)
                throw new ArgumentNullException(nameof(szTail));
            szTail = szTail.ToUpper(CultureInfo.CurrentCulture).Replace("-", string.Empty);
            if (szTail.Contains("ATD"))
                return AircraftInstanceTypes.CertifiedATD;
            else if (RegexUtility.AdminPseudoFFS.IsMatch(szTail))
                return AircraftInstanceTypes.CertifiedIFRAndLandingsSimulator;
            else if (RegexUtility.ADMINPseudoCertifiedSim.IsMatch(szTail))
                return AircraftInstanceTypes.CertifiedIFRSimulator;
            else
                return AircraftInstanceTypes.RealAircraft;
        }

        /// <summary>
        /// Determines if the specified aircraft could be a sim based on its name
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static bool CouldBeSim(Aircraft ac)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            return ac.InstanceType == AircraftInstanceTypes.RealAircraft && PseudoSimTypeFromTail(ac.TailNumber) != AircraftInstanceTypes.RealAircraft;
        }

        /// <summary>
        /// Maps the specified aircraft to an appropriate sim based on its name.
        /// </summary>
        /// <param name="acOriginal"></param>
        /// <returns>The ID of the new (mapped) aircraft; this is idAircraftUnknown if no mapping occurs.</returns>
        public static int MapToSim(Aircraft acOriginal)
        {
            if (acOriginal == null)
                throw new ArgumentNullException(nameof(acOriginal));

            if (!CouldBeSim(acOriginal))
                return Aircraft.idAircraftUnknown;

            // detect likely sim type
            AircraftInstanceTypes ait = PseudoSimTypeFromTail(acOriginal.TailNumber);

            // see if the specified sim exists
            string szSimTail = Aircraft.SuggestSims(acOriginal.ModelID, ait).First().TailNumber;
            Aircraft acNew = new Aircraft(szSimTail);

            if (acNew.IsNew)
            {
                acNew.TailNumber = szSimTail;
                acNew.ModelID = acOriginal.ModelID;
                acNew.InstanceType = ait;
                acNew.Commit();
            }

            // Issue #830 - if this looks like FAA####, then we want to preserve that in the Simulator/device identifier field.
            if (acOriginal.TailNumber.Replace("-", string.Empty).ToUpper(CultureInfo.CurrentCulture).Contains("FAA"))
            {
                // Get a list of the flights in this aircraft and add the sim
                // don't modify signed flights or flights that already have a simulator/device identifier
                List<int> lst = new List<int>();
                DBHelper dbh = new DBHelper("SELECT DISTINCT(f.idFlight) FROM flights f LEFT JOIN flightproperties fp ON fp.idPropType=354 AND fp.idflight=f.idflight WHERE f.idAircraft=?id AND f.signatureState=0 AND fp.idprop IS NULL");
                dbh.ReadRows(
                    (comm) => { comm.Parameters.AddWithValue("id", acOriginal.AircraftID); },
                    (dr) => { lst.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });

                CustomFlightProperty cfp = new CustomFlightProperty(CustomPropertyType.GetCustomPropertyType((int)CustomPropertyType.KnownProperties.IDPropSimRegistration)) { TextValue = acOriginal.TailNumber };

                foreach (int idFlight in lst)
                {
                    cfp.PropID = CustomFlightProperty.idCustomFlightPropertyNew;
                    cfp.FlightID = idFlight;
                    cfp.FCommit();
                }
            }

            // set the original's instance type so that merge works.
            acOriginal.InstanceType = ait;
            AdminMergeDupeAircraft(acNew, acOriginal);

            return acNew.AircraftID;
        }

        /// <summary>
        /// Gets details for who has which flights in a given aircraft.
        /// </summary>
        /// <param name="aircraftID"></param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<string, object>> AdminAircraftUsersDetails(int aircraftID)
        {
            List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();
            /* Union of an inner join with a not-exists is orders of magnitude faster than a single left join, for reasons I don't quite understand */
            DBHelper dbh = new DBHelper(@"SELECT 
    u.email AS 'Email Address',
    u.username AS User,
    u.FirstName,
    u.LastName,
    COUNT(f.idflight) AS 'Number of Flights'
FROM
    Useraircraft ua
        INNER JOIN
    users u ON ua.username = u.username
        LEFT JOIN
    flights f ON (f.username = ua.username)
WHERE
    ua.idaircraft = ?idaircraft
        AND f.idaircraft = ?idaircraft
GROUP BY ua.username 
UNION SELECT 
    u.email AS 'Email Address',
    u.username AS User,
    u.FirstName,
    u.LastName,
    0 AS 'Number of Flights'
FROM
    Useraircraft ua
        INNER JOIN
    users u ON ua.username = u.username
WHERE
    ua.idaircraft = ?idaircraft
        AND NOT EXISTS( SELECT 
            *
        FROM
            flights f
        WHERE
            f.username = u.username
                AND f.idaircraft = ?idaircraft)
GROUP BY ua.username
ORDER BY user ASC");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("idaircraft", aircraftID); },
                (dr) =>
                {
                    lst.Add(new Dictionary<string, object>()
                    {
                        {"User", (string) dr["User"] },
                        { "FirstName", util.ReadNullableString(dr, "FirstName") },
                        { "LastName", util.ReadNullableString(dr, "LastName") },
                        { "Email Address", (string) dr["Email Address"] },
                        { "Number of Flights", Convert.ToInt32(dr["Number of Flights"], CultureInfo.InvariantCulture) }
                    });
                });
            return lst;
        }

        /// <summary>
        /// Admin function to map a suspicious sim model to another similar model (issue #1428)
        /// This happens because people often fly a sim with identifier, say, FAA2034 and so they create a model that is, say, "Redbird FAA2034".  
        /// This should properly be some "Redbird AMEL Sim" or whatever, with "FAA2034" in the Simulator/Training Device Identifier field.
        /// This method migrates everyone in this "bogus" sim to a better model, as follows:
        /// a) For each aircraft (Should only ever be a maximum of 4: uncertified, ATD, FTD, FFS), it finds the corresponding sim in the target model
        /// b) For each user with flights in that aircraft, it finds their flights
        /// c) For each of those flights, it maps to the target aircraft and preserves the correct identifier ("DeviceID") into the Simulator/Training Device Identifier property.
        /// Since this can impact signed flights, it returns a list of any VALIDLY signed flights that were so affected, so that they can then be force-validated. 
        /// (Flights with an invalid signature, obviously, were already invalid).
        /// </summary>
        /// <param name="idOriginal">The id of the source (suspicious) model</param>
        /// <param name="idNew">The id of the target model</param>
        /// <param name="deviceID">Text to use in the Simulator/Training Device Identifier field</param>
        /// <returns>A list of ids of flights that were signed but now modified, and thus need review.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IDictionary<string, object> MigrateSim(int idOriginal, int idNew, string deviceID)
        {
            if (idOriginal < 0)
                throw new InvalidOperationException("Invalid source model");
            if (idNew < 0 || idNew == idOriginal)
                throw new InvalidOperationException("Invalid target model");
            if (String.IsNullOrEmpty(deviceID))
                throw new ArgumentNullException(nameof(deviceID));

            MakeModel mmSource = MakeModel.GetModel(idOriginal);
            MakeModel mmTarget = MakeModel.GetModel(idNew);

            if (mmSource.AllowedTypes != AllowedAircraftTypes.SimulatorOnly || mmTarget.AllowedTypes != AllowedAircraftTypes.SimulatorOnly)
                throw new InvalidOperationException("Both source and target models MUST be sim-only");

            List<int> lstSignedFlightsToReview = new List<int>();

            // Step one: get the aircraft for the model
            UserAircraft uaAdmin = new UserAircraft(string.Empty);
            IEnumerable<Aircraft> srcAircraft = uaAdmin.GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idOriginal);
            IEnumerable<Aircraft> targetAircraft = uaAdmin.GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idNew);

            Dictionary<AircraftInstanceTypes, Aircraft> dTargetByType = new Dictionary<AircraftInstanceTypes, Aircraft>();
            foreach (Aircraft acTarget in targetAircraft)
                dTargetByType[acTarget.InstanceType] = acTarget;

            // Sanity check for any real aircraft!
            foreach (Aircraft ac in srcAircraft)
                if (ac.InstanceType == AircraftInstanceTypes.RealAircraft)
                    throw new InvalidOperationException("One or more of the aircraft tied to the source model is real!!!");
            foreach (Aircraft ac in targetAircraft)
                if (ac.InstanceType == AircraftInstanceTypes.RealAircraft)
                    throw new InvalidOperationException("One or more of the aircraft tied to the target model is real!!!");

            int cAircraft = srcAircraft.Count();
            HashSet<string> hsUsers = new HashSet<string>();
            int cFlights = 0;

            // First find the aircraft from the source model and figure out the correct target aircraft to map it to.
            foreach (Aircraft aircraft in srcAircraft)
            {
                // Find or create the appropriate target aircraft
                if (!dTargetByType.TryGetValue(aircraft.InstanceType, out Aircraft acTarget))
                {
                    acTarget = Aircraft.SuggestSims(idNew, aircraft.InstanceType).First();
                    acTarget.Commit();
                }

                // now get all of the users with flights in the current aircraft
                IEnumerable<Dictionary<string, object>> acs = AdminAircraftUsersDetails(aircraft.AircraftID);
                foreach (Dictionary<string, object> d in acs)
                {
                    // Make sure that the target aircraft is in the user's account
                    string szUser = (string)d["User"];
                    hsUsers.Add(szUser);
                    if (String.IsNullOrEmpty(szUser))
                        throw new InvalidOperationException($"No specified user in dictionary of flights for aircraft {aircraft.AircraftID}- how did this happen?");
                    FlightQuery fq = new FlightQuery(szUser);
                    fq.AircraftList.Add(aircraft);

                    UserAircraft ua = new UserAircraft(szUser);
                    if (ua[acTarget.AircraftID] == null)
                        ua.FAddAircraftForUser(acTarget);

                    // Finally, migrate the flights for that user that are in the source aircraft
                    IEnumerable<LogbookEntry> lstFlightsToMap = LogbookEntry.GetFlightsForUser(fq, -1, -1);
                    foreach (LogbookEntry le in lstFlightsToMap)
                    {
                        cFlights++;
                        // If the flight has a valid signature, we're going to need to review it
                        if (le.CFISignatureState == LogbookEntryCore.SignatureState.Valid)
                            lstSignedFlightsToReview.Add(le.FlightID);

                        // Update the aircraft
                        le.AircraftID = acTarget.AircraftID;

                        // If the flight already used a simulator/training device identifier, then super, that wins.  Otherwise, add the new one.
                        if (!le.CustomProperties.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropSimRegistration))
                            le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSimRegistration, deviceID));
                        le.FCommit();
                    }

                    // Finally remove the old aircraft from their profile so that, hopefully, the model becomes orphaned.
                    ua.FDeleteAircraftforUser(aircraft.AircraftID);
                }
            }

            return new Dictionary<string, object>() { { "SignedFlightIDs", lstSignedFlightsToReview }, { "AircraftCount", cAircraft }, { "UserCount", hsUsers.Count }, { "FlightCount", cFlights } };
        }

        /// <summary>
        /// Deletes an aircraft that is completely unused.
        /// </summary>
        /// <param name="idAircraft"></param>
        public static void DeleteOrphanAircraft(int idAircraft)
        {
            // Verify that the aircraft is still an orphan
            DBHelper dbh = new DBHelper("select count(username) AS num from useraircraft where idaircraft=?idaircraft GROUP BY idaircraft");
            int cRows = 0;
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("idaircraft", idAircraft); },
                (dr) => { cRows = Convert.ToInt32(util.ReadNullableField(dr, "num", 0), CultureInfo.InvariantCulture); });

            if (cRows > 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Aircraft {0} is not an orphan!", idAircraft));

            Aircraft ac = new Aircraft(idAircraft);
            ac.PopulateImages();
            foreach (MFBImageInfo mfbii in ac.AircraftImages)
                mfbii.DeleteImage();

            ImageList il = new ImageList(MFBImageInfoBase.ImageClass.Aircraft, ac.AircraftID.ToString(CultureInfo.InvariantCulture));
            DirectoryInfo di = new DirectoryInfo(il.VirtPath.MapAbsoluteFilePath());
            if (di.Exists)
                di.Delete(true);

            // Delete any tombstone that might point to *this*
            dbh = new DBHelper("DELETE FROM aircraftTombstones WHERE idMappedAircraft=?idAc");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idAc", ac.AircraftID); });

            // Now delete this.
            dbh.CommandText = "DELETE FROM Aircraft WHERE idAircraft=?idaircraft";
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idaircraft", idAircraft); });
        }

        public static IEnumerable<AircraftInstanceTypeStat> AdminInstanceTypeCounts()
        {
            List<AircraftInstanceTypeStat> lst = new List<AircraftInstanceTypeStat>();
            DBHelper dbh = new DBHelper(@"SELECT 
    IF(ac.instancetype = 1,
        IF(ac.Tailnumber LIKE '#%',
            'Anonymous',
            'Real'),
        aic.Description) AS AircraftInstance,
    COUNT(ac.idaircraft) AS 'Number of Aircraft'
FROM
    Aircraft ac
        INNER JOIN
    aircraftinstancetypes aic ON ac.instancetype = aic.id
GROUP BY AircraftInstance
ORDER BY ac.instancetype ASC");
            dbh.ReadRows((comm) => { comm.CommandTimeout = 300; },
                (dr) => { lst.Add(new AircraftInstanceTypeStat() { InstanceType = (string)dr["AircraftInstance"], NumAircraft = Convert.ToInt32(dr["Number of Aircraft"], CultureInfo.InvariantCulture) }); });

            return lst;
        }

        /// <summary>
        /// Map an aircraft's tail number, updating all relevant users.  Typically for NNxxxx to Nxxxx or N0xxx to 0xxxx
        /// </summary>
        /// <param name="szTailOld">The old tail number</param>
        /// <param name="szTailNew">The new tail number</param>
        public static void AdminRenameAircraft(Aircraft ac, string szTailNew)
        {
            if (ac == null)
                throw new ArgumentNullException(nameof(ac));
            if (ac.AircraftID <= 0 || String.IsNullOrEmpty(ac.TailNumber))
                throw new MyFlightbookValidationException("Invalid aircraft");
            if (szTailNew == null)
                throw new ArgumentNullException(nameof(szTailNew));
            if (!AircraftUtility.rValidTail.IsMatch(szTailNew))
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "{0} is not a valid tail", szTailNew));
            if (ac.TailNumber.CompareCurrentCultureIgnoreCase(szTailNew) == 0)
                return; // nothing to do - nothing changed.

            if (Regex.IsMatch(szTailNew, "^[0-9]{3,}$"))   // if all-numeric, insert a hyphen
                szTailNew = szTailNew.Insert(2, "-");

            /*
             * 3 scenarios:
             *   - szTailNew does NOT exist in the system - simply rename this one.  Easy peasy - just rename
             *   - szTailnew DOES exist, but not with the same model.  Rename the aircraft as a new version.
             *   - szTailNew DOES exist in the system, with the same model.  Migrate all users to the existing aircraft and delete the now orphaned one
             * */

            List<Aircraft> lstAc = Aircraft.AircraftMatchingTail(szTailNew);

            if (lstAc.Count == 0)
            {
                // No collision - simple rename, no need to send notifications or update useraircraft or similar.
                ac.TailNumber = szTailNew;
                ac.Commit();
                return;
            }

            Aircraft acMatch = lstAc.Find(ac2 => ac.ModelID == ac2.ModelID);
            if (acMatch == null)
            {
                // Collision, but this is now a new version.  Save this as yet another version.  No need to update useraircraft or send notifications
                int v = 0;
                lstAc.ForEach((ac2) => v = Math.Max(v, ac2.Version));
                ac.TailNumber = szTailNew;
                ac.Version = ++v;
                ac.AdminUpdateInPlace();
            }
            else
            {
                // Exists with the same model.  Now we need to map everybody to that aircraft, orphaning this one, which we can then delete.
                AircraftStats aircraftStats = new AircraftStats(string.Empty, ac.AircraftID);
                foreach (string szUser in aircraftStats.UserNames)
                {
                    UserAircraft ua = new UserAircraft(szUser);
                    ua.ReplaceAircraftForUser(acMatch, ac, true);
                }
            }
        }
    }

    [Serializable]
    public class AircraftAdminModelMapping
    {
        #region properties
        public Aircraft aircraft { get; set; }
        public MakeModel currentModel { get; set; }
        public MakeModel targetModel { get; set; }
        #endregion

        public AircraftAdminModelMapping()
        {
            aircraft = null;
            currentModel = targetModel = null;
        }

        // Commits the mapping - NO EMAIL sent, also very efficient on the database (simple update)
        public void CommitChange()
        {
            DBHelper dbh = new DBHelper("UPDATE aircraft SET idmodel=?newmodel WHERE idaircraft=?idac");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("newmodel", targetModel.MakeModelID);
                comm.Parameters.AddWithValue("idac", aircraft.AircraftID);
            });
        }

        public static IEnumerable<AircraftAdminModelMapping> MapModels(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            List<AircraftAdminModelMapping> lstMappings = new List<AircraftAdminModelMapping>();

            using (CSVReader reader = new CSVReader(s))
            {
                try
                {
                    int iColAircraftID = -1;
                    int iColTargetModelID = -1;

                    string[] rgCols = reader.GetCSVLine(true) ?? throw new MyFlightbookValidationException("No column headers found.");
                    for (int i = 0; i < rgCols.Length; i++)
                    {
                        string sz = rgCols[i];
                        if (String.Compare(sz, "idaircraft", StringComparison.OrdinalIgnoreCase) == 0)
                            iColAircraftID = i;
                        if (String.Compare(sz, "idModelProper", StringComparison.OrdinalIgnoreCase) == 0)
                            iColTargetModelID = i;
                    }

                    if (iColAircraftID < 0)
                        throw new MyFlightbookValidationException("No \"idaircraft\" column found.");
                    if (iColTargetModelID < 0)
                        throw new MyFlightbookValidationException("No \"idModelProper\" column found.");

                    while ((rgCols = reader.GetCSVLine()) != null)
                    {
                        int idAircraft = Convert.ToInt32(rgCols[iColAircraftID], CultureInfo.InvariantCulture);
                        int idTargetModel = Convert.ToInt32(rgCols[iColTargetModelID], CultureInfo.InvariantCulture);
                        Aircraft ac = new Aircraft(idAircraft);
                        if (ac.AircraftID != Aircraft.idAircraftUnknown && ac.ModelID != idTargetModel)
                        {
                            AircraftAdminModelMapping amm = new AircraftAdminModelMapping()
                            {
                                aircraft = ac,
                                currentModel = MakeModel.GetModel(ac.ModelID),
                                targetModel = MakeModel.GetModel(idTargetModel)
                            };
                            lstMappings.Add(amm);
                        }
                    }
                }
                catch (CSVReaderInvalidCSVException ex)
                {
                    throw new MyFlightbookException(ex.Message, ex);
                }
            }

            return lstMappings;
        }
    }
}