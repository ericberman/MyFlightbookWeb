using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2023-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency.WINGS
{
    public class WINGSActivity
    {
        public const string FAAGrantWINGSCreditEndpoint = "https://www.faasafety.gov/cfi/pub/cfiactivitygivecredit_1.aspx";

        #region properties
        public int ActivityId { get; protected set; }
        public string ActivityName { get; protected set; }
        public string ActivityNumber { get; protected set; }
        public string SyllabusNumbers { get; protected set; }
        #endregion

        #region constructors
        protected WINGSActivity() { }

        protected WINGSActivity(IDataReader dr)
        {
            ActivityId = Convert.ToInt32(dr["AccreditedActivityId"], CultureInfo.InvariantCulture);
            ActivityName = (string)dr["ActivityName"];
            ActivityNumber = (string) dr["ActivityNumber"];
            SyllabusNumbers = (string) dr["syllabiNumbersString"];
        }
        #endregion

        #region Database
        /// <summary>
        /// Returns a set of activities that match the given prefix.  The prefix must be at least 2 characters long, or an empty list will be returned.
        /// </summary>
        /// <param name="szPrefix">The prefix to search for in activity names and numbers.</param>
        /// <returns>A collection of WINGSActivity objects that match the given prefix.</returns>
        static public IEnumerable<WINGSActivity> ActivitiesForPrefix(string szPrefix)
        {
            List<WINGSActivity> activities = new List<WINGSActivity>();
            if (string.IsNullOrEmpty(szPrefix) || szPrefix.Length < 2)
                return activities;
            DBHelper dbh = new DBHelper("SELECT * FROM wingsactivities WHERE ActivityName LIKE ?prefix OR ActivityNumber LIKE ?prefix");
            dbh.ReadRows((comm) => {                 comm.Parameters.AddWithValue("?prefix", szPrefix + "%"); },
                (dr) => { activities.Add(new WINGSActivity(dr)); });
            return activities;
        }

        /// <summary>
        /// Admin utility to upload a CSV file of WINGS activities into the database.  The CSV file must have the following columns:
        ///  - AccreditedActivityId
        ///  - ActivityName
        ///  - ActivityNumber
        ///  - syllabiNumbersString
        ///  
        /// These come directly from the faasafety website, so should not require changes.
        /// </summary>
        /// <param name="s">Input stream with the CSV</param>
        /// <returns>Number of new rows added</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        static public int AdminUpdateWingsActivitiesFromCSV(Stream s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            int newRows = 0;
            using (CSVReader csvReader = new CSVReader(s))
            {
                List<string> rgHeaders = new List<string>(csvReader.GetCSVLine(true));
                if ((rgHeaders?.Count ?? 0) == 0)
                    throw new InvalidOperationException("No headers found in CSV file");
                int iColActivityID = rgHeaders.FindIndex((sz) => sz.Equals("AccreditedActivityId", StringComparison.OrdinalIgnoreCase));
                int iColActivityName = rgHeaders.FindIndex((sz) => sz.Equals("ActivityName", StringComparison.OrdinalIgnoreCase));
                int iColActivityNumber = rgHeaders.FindIndex((sz) => sz.Equals("ActivityNumber", StringComparison.OrdinalIgnoreCase));
                int iColSyllabusNumbers = rgHeaders.FindIndex((sz) => sz.Equals("syllabiNumbersString", StringComparison.OrdinalIgnoreCase));
                if (iColActivityID < 0)
                    throw new InvalidOperationException("No activity ID column found");
                if (iColActivityName < 0) 
                    throw new InvalidOperationException("No activity name column found");
                if (iColActivityNumber < 0)
                    throw new InvalidOperationException("No activity number column found");
                if (iColSyllabusNumbers < 0)
                    throw new InvalidOperationException("No syllabus numbers column found");

                string[] rgRow = null;
                while ((rgRow = csvReader.GetCSVLine()) != null)
                {
                    int iActivityId = Convert.ToInt32(rgRow[iColActivityID], CultureInfo.InvariantCulture);
                    string szActivityName = rgRow[iColActivityName];
                    string szActivityNumber = rgRow[iColActivityNumber];
                    string szSyllabusNumbers = rgRow[iColSyllabusNumbers];
                    DBHelper dbh = new DBHelper("REPLACE INTO wingsactivities (AccreditedActivityId, ActivityName, ActivityNumber, syllabiNumbersString) VALUES (?AccreditedActivityId, ?ActivityName, ?ActivityNumber, ?syllabiNumbersString)");
                    dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("?AccreditedActivityId", iActivityId);
                        comm.Parameters.AddWithValue("?ActivityName", szActivityName);
                        comm.Parameters.AddWithValue("?ActivityNumber", szActivityNumber);
                        comm.Parameters.AddWithValue("?syllabiNumbersString", szSyllabusNumbers);
                    });
                    newRows += dbh.AffectedRowCount;
                }
            }
            return newRows;
        }
        #endregion
    }
}
