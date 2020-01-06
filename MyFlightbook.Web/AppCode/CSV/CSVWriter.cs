using System;
using System.Data;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.CSV
{
    /// <summary>
    /// Writes a datatable to a CSV file.  Code stolen (thanks!) from http://knab.ws/blog/index.php?/archives/3-CSV-file-parser-and-writer-in-C-Part-1.html.
    /// </summary>
    public static class CsvWriter
    {
        public static string WriteToString(DataTable table, bool header, bool quoteall)
        {
            using (StringWriter writer = new StringWriter(System.Globalization.CultureInfo.CurrentCulture))
            {
                WriteToStream(writer, table, header, quoteall);
                return writer.ToString();
            }
        }

        public static void WriteToStream(TextWriter stream, DataTable table, bool header, bool quoteall)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (table == null)
                throw new ArgumentNullException("header");

            if (header)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, table.Columns[i].Caption, quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    else
                        stream.Write("\r\n");
                }
            }
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, row[i], quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    else
                        stream.Write("\r\n");
                }
            }
        }

        private static void WriteItem(TextWriter stream, object item, bool quoteall)
        {
            if (item == null)
                return;
            string s = item.ToString();
            if (quoteall || s.IndexOfAny("\",;\x0A\x0D".ToCharArray()) > -1)
                stream.Write("\"" + s.Replace("\"", "\"\"") + "\"");
            else
                stream.Write(s);
        }
    }
}