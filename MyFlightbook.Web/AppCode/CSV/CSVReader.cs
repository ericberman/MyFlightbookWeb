using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JouniHeikniemi.Tools.Text
{

    /// <summary>
    /// A data-reader style interface for reading CSV files.
    /// </summary>
    public sealed class CSVReader : IDisposable
    {
        #region Private variables
        private Stream stream;
        private StreamReader reader;
        private bool fNoReadahead = false;  // suppresses read-ahead (for multi-line).
        private const string KnownCSVSeparators = ", ; \t"; // space separates them
        private const string QuoteAsString = "\"";
        private const char QuoteChar = '"';
        #endregion

        #region Properties
        /// <summary>
        /// The list separator to use - defaults to System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator
        /// </summary>
        public string ListSeparator { get; set; }

        /// <summary>
        /// Try to figure out the list separator in use by looking at a line.
        /// </summary>
        public string BestGuessSeparator(string szLine)
        {
            string szSep = ListSeparator;

            if (reader.BaseStream.CanSeek)
            {
                fNoReadahead = true;    // prevent any readahead on the stream
                int cElements = -1;

                string szSepSave = ListSeparator;
                // try each possible separator on the row, see which one yields the most columns - that's our best guess.
                string[] rgSeps = KnownCSVSeparators.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sep in rgSeps)
                {
                    ListSeparator = sep;
                    string[] rgsz = ParseCSVRow(szLine);
                    if (rgsz.Length > cElements)
                    {
                        szSep = sep;
                        cElements = rgsz.Length;
                    }
                }

                // Restore the prior list separator
                ListSeparator = szSepSave;
                fNoReadahead = false;
            }

            return szSep;
        }
        #endregion

        #region Object Creation
        /// <summary>
        /// Create a new reader for the given stream.
        /// </summary>
        /// <param name="s">The stream to read the CSV from.</param>
        public CSVReader(Stream s) : this(s, null) { }

        /// <summary>
        /// Create a new reader for the given stream and encoding.
        /// </summary>
        /// <param name="s">The stream to read the CSV from.</param>
        /// <param name="enc">The encoding used.</param>
        public CSVReader(Stream s, Encoding enc)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            this.stream = s;
            if (!s.CanRead)
            {
                throw new CSVReaderException("Could not read the given CSV stream!");
            }
            reader = (enc != null) ? new StreamReader(s, enc) : new StreamReader(s);

            this.ListSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        }
        #endregion

        /// <summary>
        /// Returns the fields for the next row of CSV data (or null if at eof)
        /// </summary>
        /// <param name="fInferSeparator">True to examine the first line to determine most likely separator</param>
        /// <returns>A string array of fields or null if at the end of file.</returns>
        public string[] GetCSVLine(bool fInferSeparator = false)
        {
            string data = reader.ReadLine();

            if (data == null) return null;

            // Check for XLSX ('P' 'K' 0x3 0x4) or XLS (unicode 0xFFFD unknown bytes, from UTF-8 conversion) file signature
            if (fInferSeparator && data.Length > 4 &&
                ((data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4) ||
                 (data[0] == 0xFFFD && data[1] == 0xFFFD)))
                throw new CSVReaderInvalidCSVException(Resources.LocalizedText.errImportXLSNotCSV);
            if (data.Length == 0) return new string[0];

            if (fInferSeparator)
                ListSeparator = BestGuessSeparator(data);

            return ParseCSVRow(data);
        }

        // Parses the CSV fields and pushes the fields into the result arraylist
        private string[] ParseCSVRow(string data)
        {
            List<string> lst = new List<string>();

            int pos = -1;
            while (pos < data.Length)
                lst.Add(ParseCSVField(ref data, ref pos));
            return lst.ToArray();
        }

        // Parses the field at the given position of the data, modified pos to match
        // the first unparsed position and returns the parsed field
        private string ParseCSVField(ref string data, ref int startSeparatorPosition)
        {

            if (startSeparatorPosition == data.Length - 1)
            {
                startSeparatorPosition++;
                // The last field is empty
                return string.Empty;
            }

            int fromPos = startSeparatorPosition + 1;

            // Determine if this is a quoted field
            if (data[fromPos] == QuoteChar)
            {
                // If we're at the end of the string, let's consider this a field that
                // only contains the quote
                if (fromPos == data.Length - 1)
                {
                    fromPos++;
                    startSeparatorPosition = data.Length;
                    return QuoteAsString;
                }

                // Otherwise, return a string of appropriate length with double quotes collapsed
                int nextSingleQuote = -1;
                string szNextLine = null;

                do
                {
                    nextSingleQuote = NextNakedDoubleQuote(data, fromPos + 1);
                    if (nextSingleQuote == -1) // not found - suck in another line
                    {
                        szNextLine = fNoReadahead ? null : reader.ReadLine();
                        if (szNextLine != null)
                            data = data + "\r\n" + szNextLine;
                    }
                } while (nextSingleQuote == -1 && szNextLine != null);

                if (nextSingleQuote == -1)  // shouldn't happen - this means that we hit the end of the file without finding the closing quote!  Just return all of what we found.
                {
                    startSeparatorPosition = data.Length;   // advance the pointer to avoid an infinite loop
                    return data.Substring(fromPos + 1);
                }

                startSeparatorPosition = nextSingleQuote + 1;
                return data.Substring(fromPos + 1, nextSingleQuote - fromPos - 1).Replace("\"\"", "\"");
            }

            // The field ends in the next comma or EOL
            int nextComma = data.IndexOf(ListSeparator, fromPos, StringComparison.Ordinal);
            if (nextComma == -1)
            {
                startSeparatorPosition = data.Length;
                return data.Substring(fromPos);
            }
            else
            {
                startSeparatorPosition = nextComma;
                return data.Substring(fromPos, nextComma - fromPos);
            }
        }

        /// <summary>
        /// Returns the index of the next single instance of naked double quote mark (i.e., ", not "") in the string 
        /// (starting from startFrom)
        /// </summary>
        /// <param name="data">The row's data</param>
        /// <param name="startFrom">The index at which to begin searching</param>
        /// <returns>The index of the next naked double-quote mark.  If one isn't found, it returns -1</returns>
        private int NextNakedDoubleQuote(string data, int startFrom)
        {

            int i = startFrom - 1;
            while (++i < data.Length)
                if (data[i] == QuoteChar)
                {
                    // If this is a double quote, bypass the chars
                    if (i < data.Length - 1 && data[i + 1] == QuoteChar)
                    {
                        i++;
                        continue;
                    }
                    else
                        return i;
                }
            // If no quote found, return the end value of i (data.Length)
            return -1;
        }

        /// <summary>
        /// Disposes the CSVReader. The underlying stream is closed.
        /// </summary>
        public void Dispose()
        {
            // Closing the reader closes the underlying stream, too
            if (reader != null) reader.Close();
            else if (stream != null)
                stream.Close(); // In case we failed before the reader was constructed
            GC.SuppressFinalize(this);
        }
    }


    /// <summary>
    /// Exception class for CSVReader exceptions.
    /// </summary>
    [Serializable]
    public class CSVReaderException : Exception
    {

        /// <summary>
        /// Constructs a new exception object with the given message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CSVReaderException(string message) : base(message) { }

        public CSVReaderException(string message, Exception e) : base(message, e) { }

        protected CSVReaderException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public CSVReaderException() : base() { }
    }

    /// <summary>
    /// Exception class for invalid CSV Data CSVReader exceptions.
    /// </summary>
    [Serializable]
    public class CSVReaderInvalidCSVException : Exception
    {

        /// <summary>
        /// Constructs a new exception object with the given message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public CSVReaderInvalidCSVException(string message) : base(message) { }

        public CSVReaderInvalidCSVException(string message, Exception e) : base(message, e) { }

        protected CSVReaderInvalidCSVException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public CSVReaderInvalidCSVException() : base() { }
    }
}
