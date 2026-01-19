using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2015-2025 MyFlightbook LLC
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
            using (StringWriter writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                WriteToStream(writer, table, header, quoteall);
                return writer.ToString();
            }
        }

        public static byte[] WriteToBytes(DataTable dt, bool header, bool quoteall)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024))
                {
                    WriteToStream(sw, dt, header, quoteall);
                    sw.Flush();
                    return ms.ToArray();
                }
            }
        }

        public static void WriteToStream(TextWriter stream, DataTable table, bool header, bool quoteall)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (table == null)
                throw new ArgumentNullException(nameof(header));

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