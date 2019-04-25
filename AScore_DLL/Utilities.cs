//Joshua Aldrich

using System;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

namespace AScore_DLL
{
    class Utilities
    {
        /// <summary>
        /// Generates a DataTable with all entries being a string
        /// </summary>
        /// <param name="fileName">input file name</param>
        /// <param name="addDataSetName">whether to add the dataset name as a column</param>
        /// <returns></returns>
        public static DataTable TextFileToDataTableAssignTypeString(string fileName, bool addDataSetName)
        {
            var dt = new DataTable();

            using (var reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                // first line has headers
                if (!reader.EndOfStream)
                {
                    var headerLine = reader.ReadLine();

                    if (headerLine == null)
                    {
                        // it's empty, that's an error
                        throw new ApplicationException("The data provided is not in a valid format (empty header line)");
                    }

                    var headers = headerLine.Split('\t', ',');
                    foreach (var s in headers)
                    {
                        dt.Columns.Add(s);
                        dt.Columns[s].DefaultValue = "";
                    }
                }
                else
                {
                    // it's empty, that's an error
                    throw new ApplicationException("The data provided is not in a valid format (empty data file)");
                }

                // fill the rest of the table; positional
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var row = dt.NewRow();

                    var dataColumns = line.Split('\t', ',');
                    var i = 0;
                    foreach (var s in dataColumns)
                    {
                        row[i] = s;
                        i++;
                    }
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }

        /// <summary>
        /// Writes a DataTable to text file
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filePath"></param>
        public static void WriteDataTableToText(DataTable dt, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
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
        }
    }
}
