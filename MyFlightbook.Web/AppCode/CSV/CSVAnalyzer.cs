using JouniHeikniemi.Tools.Text;
using MyFlightbook;
using MyFlightbook.CSV;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2007-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Parses a CSV file into a datatable, checking for (and in some cases fixing) common issues like multi-line data not in quotes or duplicated/missing column headers.
    /// This has NO semantic understanding of the data being imported.
    /// </summary>
    public class CSVAnalyzer : IDisposable
    {
        public enum CSVStatus { OK, Fixed, Broken }

        private DataTable m_dt = null;

        private StringBuilder sbAudit = new StringBuilder();

        #region Properties
        /// <summary>
        /// Description of issues found and any corrections applied
        /// </summary>
        public string Audit { get { return sbAudit.ToString(); } }

        /// <summary>
        /// Result of the analysis
        /// </summary>
        public CSVStatus Status { get; private set; }

        public string DataAsCSV { get { return CsvWriter.WriteToString(Data, true, true); } }

        public DataTable Data
        {
            get { return m_dt; }
        }
        #endregion

        #region IDisposable Implementation
        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (m_dt != null)
                        m_dt.Dispose();
                }
                m_dt = null;

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CSVAnalyzer()
        {
            Dispose(false);
        }
        #endregion

        #region Constructors
        protected CSVAnalyzer()
        {
            Status = CSVStatus.OK;
            m_dt = new DataTable();
            m_dt.Locale = CultureInfo.CurrentCulture ;
        }

        public CSVAnalyzer(Stream sIn) : this() { Validate(sIn); }
        #endregion

        /// <summary>
        /// Analyzes the incoming stream to validate row/column integrity.  Fixes up line breaks as needed
        /// </summary>
        /// <param name="sIn">Input stream</param>
        /// <returns>True if no changes made or if it can be fixed, false if changes made (see Audit)</returns>
        public CSVStatus Validate(Stream sIn)
        {
            sbAudit = new StringBuilder();

            // First pass - see how many columns we should have, see if all is good.
            CSVReader reader = new CSVReader(sIn);
            try
            {
                string[] rgszHeader = reader.GetCSVLine(true);
                int cColumns = rgszHeader.Length;
                if (cColumns == 0)
                    throw new MyFlightbookValidationException(Resources.LogbookEntry.errImportNoHeaders);

                // look for empty or duplicate columns
                Dictionary<string, string> dictHeaders = new Dictionary<string, string>();
                int iCol = 0;
                foreach (string sz in rgszHeader)
                {
                    string szH = sz.Trim();
                    iCol++;
                    if (String.IsNullOrWhiteSpace(szH))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportEmptyColumn, iCol));
                    if (dictHeaders.ContainsKey(szH))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportDuplicateColumn, sz));
                    dictHeaders[szH] = szH;
                    Data.Columns.Add(new DataColumn(szH, typeof(string)));
                }

                sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvauditColumnsFound, cColumns);
                sbAudit.AppendLine();

                int iRow = 0;
                string[] rgszRow = null;
                bool fShortRowsFound = false;
                int cColsSoFar = 0;
                while ((rgszRow = reader.GetCSVLine()) != null)
                {
                    ++iRow;
                    int cColsThisRow = rgszRow.Length;  // number of columns in this row
                    if (cColsThisRow == 0)
                    {
                        sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditEmptyRow, iRow);
                        sbAudit.AppendLine();
                        continue;
                    }

                    int cColsStarting = cColsSoFar;     // number of columns seen so far in this row
                    cColsSoFar += (cColsSoFar == 0 ? 0 : -1) + cColsThisRow; // if we've already got some columns in progress, subtract one to merge the first column of this row with the last column of the prior row.
                    if (cColsSoFar > cColumns)
                    {
                        sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditAppendedRowTooLong, iRow, cColsThisRow, cColsSoFar);
                        sbAudit.AppendLine();
                        cColsSoFar = cColsStarting = 0;
                    }

                    if (rgszRow.Length > cColumns)
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportTooManyColumns, iRow, rgszRow.Length, cColumns));

                    if (cColsStarting == 0)
                        Data.Rows.Add(Data.NewRow());
                    DataRow dr = Data.Rows[Data.Rows.Count - 1];

                    for (int i = 0; i < cColsThisRow; i++)
                    {
                        if (cColsStarting == 0)
                            dr[cColsStarting + i] = rgszRow[i];
                        else if (i == 0)
                            dr[cColsStarting - 1] = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, dr[cColsStarting - 1], rgszRow[0]);
                        else
                            dr[cColsStarting - 1 + i] = rgszRow[i];
                    }

                    if (cColsSoFar == cColumns)
                    {
                        if (cColsStarting > 0)
                        {
                            sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditRowMergedWithPrior, iRow, rgszRow.Length);
                            sbAudit.AppendLine();
                        }
                        cColsSoFar = 0;
                    }
                    else if (rgszRow.Length < cColumns)
                    {
                        fShortRowsFound = true;
                        sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditRowIncomplete, iRow, rgszRow.Length, cColumns);
                        sbAudit.AppendLine();
                    }
                }

                if (Data.Rows.Count == 0) // no data rows found
                    throw new MyFlightbookValidationException(Resources.LogbookEntry.errImportNoData);

                sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditTotalRows, Data.Rows.Count);
                sbAudit.AppendLine();

                if (!fShortRowsFound)
                {
                    sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditSuccess);
                    sbAudit.AppendLine();
                    return Status = CSVStatus.OK;
                }
                else
                {
                    return Status = CSVStatus.Fixed;
                }
            }
            catch (CSVReaderInvalidCSVException ex)
            {
                sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditErrorFound, ex.Message);
                sbAudit.AppendLine();
            }
            catch (MyFlightbookValidationException ex)
            {
                sbAudit.AppendFormat(CultureInfo.CurrentCulture, Resources.LogbookEntry.csvAuditErrorFound, ex.Message);
                sbAudit.AppendLine();
            }

            return Status = CSVStatus.Broken;
        }
    }
}