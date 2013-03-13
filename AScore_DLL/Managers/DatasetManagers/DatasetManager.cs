﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace AScore_DLL.Managers.DatasetManagers
{
	public abstract class DatasetManager
	{
		protected DataTable dt;
        protected DataTable dAscores;
		protected int t = 0;
		protected int maxSteps;
		public bool AtEnd{get; set;}
        protected string jobNum;
		protected string mDatasetFilePath = string.Empty;

		public string DatasetFilePath { 
			get {
				return mDatasetFilePath;
			}
		}

		public DatasetManager()
		{
		}


		public DatasetManager(string fhtFileName)
		{
			mDatasetFilePath = fhtFileName;
			dt = Utilities.TextFileToDataTableAssignTypeString(fhtFileName, false);
            jobNum = (string)dt.Rows[0]["Job"];
			maxSteps = dt.Rows.Count;
			AtEnd = false;
			InitializeAscores();
            
		}


        private void InitializeAscores()
        {
            dAscores = new DataTable();
            dAscores.Columns.Add("Job", typeof(string));
            dAscores.Columns.Add("Scan", typeof(string));
            dAscores.Columns.Add("OriginalSequence", typeof(string));
            dAscores.Columns.Add("BestSequence", typeof(string));
            dAscores.Columns.Add("PeptideScore", typeof(string));
            dAscores.Columns.Add("AScore", typeof(string));
            dAscores.Columns.Add("numSiteIonsPoss", typeof(string));
            dAscores.Columns.Add("numSiteIonsMatched", typeof(string));
            dAscores.Columns.Add("SecondSequence", typeof(string));
			dAscores.Columns.Add("ModInfo", typeof(string));


        }

		public int GetRowLength()
		{
			return dt.Rows.Count;
		}

		/// <summary>
		/// Get the row information needed to process next spectra
		/// </summary>
		/// <param name="scanNumber">scan number</param>
		/// <param name="scanCount">number of scans, usually 1</param>
		/// <param name="chargeState">charge state</param>
		/// <param name="peptideSeq">peptide sequence</param>
        /// 
        public abstract void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
			out string peptideSeq, ref AScore_DLL.Managers.ParameterFileManager ascoreParam);

		public abstract void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
			out string peptideSeq, out double msgfScore, ref AScore_DLL.Managers.ParameterFileManager ascoreParam);

		/// <summary>
		/// Resets the counter to the beginning
		/// </summary>
		public void ResetToStart()
		{
			t = 0;
		}

        public int CurrentRowNum
        {
            get {return t;}
        }

		public int ResultsCount
		{
			get { return dAscores.Rows.Count; }
		}

		/// <summary>
		/// Writes ascore information to table
		/// </summary>
		/// <param name="bestSeq"></param>
		/// <param name="topPeptideScore"></param>
		/// <param name="ascoreResult"></param>
		public void WriteToTable(string peptideSeq, string bestSeq, int scanNum, double topPeptideScore, AScoreResult ascoreResult)
		{
			//if (peptideSeq == "R.HGTDLWIDNM@SSAVPNHS*PEKK.D")
			//{
			//    Console.WriteLine("Debug: found " + peptideSeq);
			//}

            DataRow drow = dAscores.NewRow();
            drow["Job"] = jobNum;
            drow["Scan"] = "" + scanNum;
            drow["OriginalSequence"] = peptideSeq;
			drow["BestSequence"] = bestSeq;
			drow["PeptideScore"] = "" + topPeptideScore;

			drow["AScore"] = "" + ascoreResult.AScore;
			drow["numSiteIonsPoss"] = ascoreResult.NumSiteIons;
			drow["numSiteIonsMatched"] = "" + ascoreResult.SiteDetermineMatched;
			drow["SecondSequence"] = ascoreResult.SecondSequence;
			drow["ModInfo"] = ascoreResult.ModInfo;

            dAscores.Rows.Add(drow);

		}

		/// <summary>
		/// Writes ascore information to table
		/// Call this function if a peptide has zero or just one modifiable site
		/// </summary>
		/// <param name="bestSeq"></param>
		/// <param name="pScore"></param>
		/// <param name="positionList"></param>
        public void WriteToTable(string peptideSeq, int scanNum, double pScore, int[] positionList)
		{
			//if (peptideSeq == "R.HGTDLWIDNM@SSAVPNHS*PEKK.D")
			//{
			//    Console.WriteLine("Debug: found " + peptideSeq);
			//}

            DataRow drow = dAscores.NewRow();
            drow["Job"] = jobNum;
            drow["Scan"] = "" + scanNum;
            drow["OriginalSequence"] = peptideSeq;
            drow["BestSequence"] = peptideSeq;
            drow["PeptideScore"] = "" + pScore;

			int intNonZeroCount = (from item in positionList where item > 0 select item).Count();

			if (intNonZeroCount == 0)
			{
                drow["AScore"] = "-1";
               
			}
			else
			{
                drow["AScore"] = "1000";
			}
            drow["numSiteIonsMatched"] = 0;
            drow["numSiteIonsPoss"] = 0;
            drow["SecondSequence"] = "---";
			drow["ModInfo"] = "-";

            dAscores.Rows.Add(drow);

		}


		/// <summary>
		/// Writes the current dataset to file
		/// </summary>
		/// <param name="outputFilePath"></param>
		public void WriteToFile(string outputFilePath)
		{
			Utilities.WriteDataTableToText(dAscores, outputFilePath);
		}

		/// <summary>
		/// Increment the row number 
		/// </summary>
		public void IncrementRow()
		{
			t++;
			if (t == maxSteps)
			{
				AtEnd = true;
			}
		}

	

	}
}
