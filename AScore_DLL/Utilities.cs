using System;
using System.Data;
using System.IO;
using AScore_DLL.Mod;
using PRISM;

namespace AScore_DLL
{
    internal static class Utilities
    {
        /// <summary>
        /// Reads data from a tab-delimited text file, storing in a DataTable instance
        /// </summary>
        /// <param name="psmResultsFilePath">PSM results file path</param>
        public static DataTable TextFileToDataTableAssignTypeString(string psmResultsFilePath)
        {
            var dt = new DataTable();

            using var reader = new StreamReader(new FileStream(psmResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            // first line has headers
            if (!reader.EndOfStream)
            {
                var headerLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    // it's empty, that's an error
                    throw new ApplicationException(string.Format(
                        "The data provided in {0} is not in a valid format (empty header line found by {1})",
                        psmResultsFilePath, "TextFileToDataTableAssignTypeString"));
                }

                var headers = headerLine.Split('\t');
                foreach (var s in headers)
                {
                    dt.Columns.Add(s);
                    dt.Columns[s].DefaultValue = "";
                }
            }
            else
            {
                // it's empty, that's an error
                throw new ApplicationException(string.Format(
                    "The data provided in {0} is not in a valid format (empty data file found by {1})",
                    psmResultsFilePath, "TextFileToDataTableAssignTypeString"));
            }

            var columnCount = dt.Columns.Count;

            var lineNumber = 1;

            // fill the rest of the table; positional
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var row = dt.NewRow();

                // Only split on tabs, since the Protein column may contain a comma
                var dataColumns = line.Split('\t');

                var i = 0;
                foreach (var s in dataColumns)
                {
                    if (i >= columnCount)
                    {
                        ConsoleMsgUtils.ShowWarning(
                            "Line {0} in the input file has {1} tab-delimited columns, but the header row only has {2} columns; skipping the extra columns",
                            lineNumber, dataColumns.Length, columnCount);

                        break;
                    }

                    row[i] = s;
                    i++;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        /// <summary>
        /// If value ends with suffixToRemove, remove it; otherwise, return value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="suffixToRemove"></param>
        /// <param name="caseSensitiveMatch"></param>
        /// <returns></returns>
        public static string TrimEnd(string value, string suffixToRemove, bool caseSensitiveMatch = true)
        {
            if (!value.EndsWith(suffixToRemove, caseSensitiveMatch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return value.Substring(0, value.Length - suffixToRemove.Length);
        }

        /// <summary>
        /// Writes a DataTable to text file
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filePath"></param>
        public static void WriteDataTableToText(DataTable dt, string filePath)
        {
            using var writer = new StreamWriter(filePath);

            var headerLine = dt.Columns[0].ColumnName;
            for (var i = 1; i < dt.Columns.Count; i++)
            {
                headerLine += "\t" + dt.Columns[i].ColumnName;
            }
            writer.WriteLine(headerLine);

            foreach (DataRow row in dt.Rows)
            {
                var dataLine = row[0].ToString();
                for (var i = 1; i < dt.Columns.Count; i++)
                {
                    dataLine += "\t" + row[i];
                }
                writer.WriteLine(dataLine);
            }
        }

        public static string GetModDescription(string type, Modification mod, string prefix = "    ")
        {
            var residues = string.Empty;
            foreach (var res in mod.PossibleModSites)
            {
                residues += res;
            }

            if (mod.cTerminus)
            {
                residues += ">";
            }

            if (mod.nTerminus)
            {
                residues += "<";
            }

            return prefix + type + mod.MassMonoisotopic + " on " + residues;
        }
    }
}
