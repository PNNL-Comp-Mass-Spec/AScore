//Joshua Aldrich

using System;
using System.Data;
using System.IO;

namespace AScore_DLL
{
	class Utilities
	{

		/// <summary>
		/// Generates a datatable with all entries being a string
		/// </summary>
		/// <param name="fileName">input file name</param>
		/// <param name="addDataSetName">whether to add the datasetname as a column</param>
		/// <returns></returns>
		public static DataTable TextFileToDataTableAssignTypeString(string fileName, bool addDataSetName)
		{
		    var dt = new DataTable();



			using (var sr = new StreamReader(fileName))
			{
				// first line has headers
			    string line;
			    string[] fields;
			    if ((line = sr.ReadLine()) != null)
				{
					fields = line.Split('\t', ',');
					foreach (var s in fields)
					{
						dt.Columns.Add(s);
						dt.Columns[s].DefaultValue = "";
					}

				}
				else
				{
					// it's empty, that's an error
					throw new ApplicationException("The data provided is not in a valid format.");
				}

				// fill the rest of the table; positional
				while ((line = sr.ReadLine()) != null)
				{
					var row = dt.NewRow();

					fields = line.Split('\t', ',');
					var i = 0;
					foreach (var s in fields)
					{
						row[i] = s;
						i++;
					}
					dt.Rows.Add(row);
				}
			}
			//if (!dt.Columns.Contains("DatasetName") && addDataSetName)
			//{
			//    string dataSetName = Regex.Replace(Path.GetFileName(fileName).Split('.')[0],
			//"_fht|_fht_MSGF|_fht_MSGF_full|_full|_ReporterIons|_MSGF|_cut", "");

			//    dt.Columns.Add("DataSetName", typeof(string));
			//    foreach (DataRow row in dt.Rows)
			//    {
			//        row["DataSetName"] = dataSetName;
			//    }
			//}

			return dt;


		}

		/// <summary>
		/// Writes a datatable to text file
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="filePath"></param>
		public static void WriteDataTableToText(DataTable dt, string filePath)
		{
			using (var sw = new StreamWriter(filePath))
			{
				var s = dt.Columns[0].ColumnName;
				for (var i = 1; i < dt.Columns.Count; i++)
				{
					s += "\t" + dt.Columns[i].ColumnName;
				}
				sw.WriteLine(s);

				s = string.Empty;
				foreach (DataRow row in dt.Rows)
				{
					s = "" + row[0];
					for (var i = 1; i < dt.Columns.Count; i++)
					{
						s += "\t" + row[i];
					}
					sw.WriteLine(s);
					s = string.Empty;
				}


			}
		}


	}
}
