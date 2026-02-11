using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
    /// Structured search for a model
    /// </summary>
    [Serializable]
    public class ModelQuery
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ModelSortMode { ModelName, CatClass, Manufacturer };

        static readonly LazyRegex rNormalizeModel = new LazyRegex("[- ]+");

        /// <summary>
        /// Prefix to use for modelname to force to Family.
        /// </summary>
        public const string ICAOPrefix = "ICAO:";

        #region properties
        /// <summary>
        /// Text to find anywhere in the search string
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string FullText { get; set; }

        /// <summary>
        /// Text to find in the manufacturer name
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string ManufacturerName { get; set; }

        /// <summary>
        /// Manufacturer ID (i.e., deterministic)
        /// </summary>
        [System.ComponentModel.DefaultValue(-1)]
        public int ManufacturerID { get; set; }

        /// <summary>
        /// Text to find in the model (e.g., "C-172")
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string Model { get; set; }

        /// <summary>
        /// Text to find in the model marketing name (e.g., "Skyhawk")
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string ModelName { get; set; }

        /// <summary>
        /// Text to find in the category/class
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string CatClass { get; set; }

        /// <summary>
        /// Text to find in the type name.
        /// </summary>
        [System.ComponentModel.DefaultValue("")]
        public string TypeName { get; set; }

        /// <summary>
        /// Maximum # of results to return; -1 for no limit
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// # of results to skip, -1 for no skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// True to get images.  Images are a bit slower, so this is off by default.
        /// </summary>
        public bool IncludeSampleImages { get; set; }

        /// <summary>
        /// On which field should the results be sorted?
        /// </summary>
        [System.ComponentModel.DefaultValue(ModelSortMode.ModelName)]
        public ModelSortMode SortMode { get; set; }

        /// <summary>
        /// Should the sort be ascending or descending?
        /// </summary>
        [System.ComponentModel.DefaultValue(SortDirection.Ascending)]
        public SortDirection SortDir { get; set; }

        /// <summary>
        /// Should we give a boost to models where the model itself matches?
        /// </summary>
        public bool PreferModelNameMatch { get; set; }

        /// <summary>
        /// Is this an empty query?
        /// </summary>
        public bool IsEmpty
        {
            get { return (FullText?.Length ?? 0) + (ManufacturerName?.Length ?? 0) + (ModelName?.Length ?? 0) + (CatClass?.Length ?? 0) + (TypeName?.Length ?? 0) == 0; }
        }

        /// <summary>
        /// Is this an advanced query?
        /// </summary>
        public bool IsAdvanced
        {
            get { return (FullText?.Length ?? 0) == 0 && !IsEmpty; }
        }

        private static readonly char[] searchSeparator = new char[] { ' ' };
        #endregion

        #region Query utilities
        /// <summary>
        /// The MySqlCommand initialized for this query, including any and all parameters.
        /// </summary>
        public DBHelperCommandArgs ModelQueryCommand()
        {
            List<string> lstWhereTerms = new List<string>();
            List<MySqlParameter> lstParams = new List<MySqlParameter>();

            // Add each of the terms
            string[] rgTerms = FullText.Replace("-", string.Empty).Split(searchSeparator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rgTerms.Length; i++)
                AddQueryTerm(rgTerms[i], String.Format(CultureInfo.InvariantCulture, "FullText{0}", i), "REPLACE(Concat(model, ' ', manufacturers.manufacturer, ' ', typename, ' ', family, ' ', modelname, ' ', categoryclass.CatClass), '-', '')", lstWhereTerms, lstParams);

            string szPreferred = "0";
            if (PreferModelNameMatch)
            {
                string szModelMatch = String.Format(CultureInfo.InvariantCulture, "%{0}%", FullText.EscapeMySQLWildcards().ConvertToMySQLWildcards());
                szPreferred = "IF(model LIKE ?modelMatch, 1, 0)";
                lstParams.Add(new MySqlParameter("modelMatch", szModelMatch));
            }

            AddQueryTerm(CatClass, "qCatClass", "catclass", lstWhereTerms, lstParams);
            AddQueryTerm(rNormalizeModel.Replace(Model, string.Empty), "qModel", "REPLACE(REPLACE(model, ' ', ''), '-', '')", lstWhereTerms, lstParams);
            if (ModelName.StartsWith(ICAOPrefix, StringComparison.CurrentCultureIgnoreCase))
                AddQueryTerm(ModelName.Substring(ICAOPrefix.Length), "qFamilyName", "family", lstWhereTerms, lstParams);
            else
                AddQueryTerm(ModelName, "qModelName", "modelname", lstWhereTerms, lstParams);
            AddQueryTerm(ManufacturerName, "qMan", "manufacturer", lstWhereTerms, lstParams);
            AddQueryTerm(TypeName, "qType", "typename", lstWhereTerms, lstParams);

            if (ManufacturerID != Manufacturer.UnsavedID)
            {
                lstWhereTerms.Add(" (models.idManufacturer = ?manID) ");
                lstParams.Add(new MySqlParameter("manID", ManufacturerID));
            }

            string szHavingPredicate = String.Join(" AND ", lstWhereTerms.ToArray());

            const string szQTemplate = @"SELECT
models.*,
manufacturers.manufacturer,
categoryclass.CatClass as 'Category/Class',
{0} AS AircraftIDs,
{1} AS preferred
FROM models
  INNER JOIN manufacturers on manufacturers.idManufacturer = models.idmanufacturer
  INNER JOIN categoryclass on categoryclass.idCatClass = models.idcategoryclass
{2}
{3}
{4}
{5}";
            const string szQSamplesTemplate = @"LEFT OUTER JOIN (SELECT ac.idmodel, group_concat(DISTINCT img.imageKey separator ',') AS AircraftIDs
                    FROM Images img
                    INNER JOIN aircraft ac ON CAST(img.imageKey AS Unsigned)=ac.idaircraft
                    WHERE img.VirtPathID=1
                    GROUP BY ac.idmodel) Samples
       ON models.idmodel=Samples.idmodel";

            string szQ = String.Format(CultureInfo.InvariantCulture, szQTemplate,
                IncludeSampleImages ? "Samples.AircraftIDs" : "NULL",
                szPreferred,
                IncludeSampleImages ? szQSamplesTemplate : string.Empty,
                szHavingPredicate.Length == 0 ? string.Empty : String.Format(CultureInfo.InvariantCulture, " WHERE {0} ", szHavingPredicate),
                SortOrderFromSortModeAndDirection(SortMode, SortDir),
                (Limit > 0 && Skip >= 0) ? String.Format(CultureInfo.InvariantCulture, " LIMIT {0},{1} ", Skip, Limit) : string.Empty);

            DBHelperCommandArgs args = new DBHelperCommandArgs(szQ, lstParams);
            return args;
        }

        /// <summary>
        /// Adds a query term
        /// </summary>
        /// <param name="szQ">The search text</param>
        /// <param name="szParamName">Parameter name for this search term</param>
        /// <param name="szMatchField">The DB query field against which to match</param>
        /// <param name="lstTerms">The list of WHERE terms to which to add</param>
        /// <param name="lstParams">The list of MySQLParameters to which to add</param>
        private static void AddQueryTerm(string szQ, string szParamName, string szMatchField, List<string> lstTerms, List<MySqlParameter> lstParams)
        {
            if (String.IsNullOrEmpty(szQ))
                return;

            lstTerms.Add(String.Format(CultureInfo.InvariantCulture, " ({0} LIKE ?{1}) ", szMatchField, szParamName));
            lstParams.Add(new MySqlParameter(szParamName, String.Format(CultureInfo.InvariantCulture, "%{0}%", szQ.EscapeMySQLWildcards().ConvertToMySQLWildcards())));
        }

        private static string SortOrderFromSortModeAndDirection(ModelSortMode sortmode, SortDirection sortDirection)
        {
            string szOrderString;
            string szDirString = sortDirection.ToMySQLSort();

            switch (sortmode)
            {
                case ModelSortMode.CatClass:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "categoryclass.CatClass {0}, manufacturer {0}, model {0}", szDirString);
                    break;
                case ModelSortMode.Manufacturer:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "manufacturer {0}, model {0}", szDirString);
                    break;
                default:
                case ModelSortMode.ModelName:
                    szOrderString = String.Format(CultureInfo.InvariantCulture, "Model {0}, modelname {0}, typename {0}", szDirString);
                    break;
            }
            return String.Format(CultureInfo.InvariantCulture, " ORDER BY Preferred DESC, {0}", szOrderString);
        }
        #endregion

        public ModelQuery()
        {
            FullText = ManufacturerName = Model = ModelName = CatClass = TypeName = string.Empty;
            Limit = -1;
            Skip = -1;
            ManufacturerID = Manufacturer.UnsavedID;
            SortMode = ModelSortMode.Manufacturer;
            SortDir = SortDirection.Ascending;
            IncludeSampleImages = false;
        }
    }

}