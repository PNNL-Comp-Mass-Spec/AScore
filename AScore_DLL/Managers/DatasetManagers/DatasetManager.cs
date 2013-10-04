using System.Linq;
using System.Data;
using PNNLOmics.Utilities;

namespace AScore_DLL.Managers.DatasetManagers
{
	public abstract class DatasetManager
	{
		#region "Constants"

		public const string RESULTS_COL_JOB = "Job";
		public const string RESULTS_COL_SCAN = "Scan";
		public const string RESULTS_COL_ORIGINALSEQUENCE = "OriginalSequence";
		public const string RESULTS_COL_BESTSEQUENCE = "BestSequence";
		public const string RESULTS_COL_PEPTIDESCORE = "PeptideScore";
		public const string RESULTS_COL_ASCORE = "AScore";
		public const string RESULTS_COL_NUMSITEIONSPOSS = "numSiteIonsPoss";
		public const string RESULTS_COL_NUMSITEIONSMATCHED = "numSiteIonsMatched";
		public const string RESULTS_COL_SECONDSEQUENCE = "SecondSequence";
		public const string RESULTS_COL_MODINFO = "ModInfo";

		#endregion

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
			jobNum = (string)dt.Rows[0][RESULTS_COL_JOB];
			maxSteps = dt.Rows.Count;
			AtEnd = false;
			InitializeAscores();
            
		}


        private void InitializeAscores()
        {
            dAscores = new DataTable();
			dAscores.Columns.Add(RESULTS_COL_JOB, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_SCAN, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_ORIGINALSEQUENCE, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_BESTSEQUENCE, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_PEPTIDESCORE, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_ASCORE, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_NUMSITEIONSPOSS, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_NUMSITEIONSMATCHED, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_SECONDSEQUENCE, typeof(string));
			dAscores.Columns.Add(RESULTS_COL_MODINFO, typeof(string));


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
			drow[RESULTS_COL_JOB] = jobNum;
			drow[RESULTS_COL_SCAN] = scanNum;
			drow[RESULTS_COL_ORIGINALSEQUENCE] = peptideSeq;
			drow[RESULTS_COL_BESTSEQUENCE] = bestSeq;
			drow[RESULTS_COL_PEPTIDESCORE] = MathUtilities.ValueToString(topPeptideScore);

			drow[RESULTS_COL_ASCORE] = MathUtilities.ValueToString(ascoreResult.AScore);
			drow[RESULTS_COL_NUMSITEIONSPOSS] = ascoreResult.NumSiteIons;
			drow[RESULTS_COL_NUMSITEIONSMATCHED] = ascoreResult.SiteDetermineMatched;
			drow[RESULTS_COL_SECONDSEQUENCE] = ascoreResult.SecondSequence;
			drow[RESULTS_COL_MODINFO] = ascoreResult.ModInfo;

            dAscores.Rows.Add(drow);

		}

		/// <summary>
		/// Writes ascore information to table
		/// Call this function if a peptide has zero or just one modifiable site
		/// </summary>
		/// <param name="bestSeq"></param>
		/// <param name="pScore"></param>
		/// <param name="positionList"></param>
        public void WriteToTable(string peptideSeq, int scanNum, double pScore, int[] positionList, string modInfo)
		{
			//if (peptideSeq == "R.HGTDLWIDNM@SSAVPNHS*PEKK.D")
			//{
			//    Console.WriteLine("Debug: found " + peptideSeq);
			//}

            DataRow drow = dAscores.NewRow();
			drow[RESULTS_COL_JOB] = jobNum;
			drow[RESULTS_COL_SCAN] = scanNum;
			drow[RESULTS_COL_ORIGINALSEQUENCE] = peptideSeq;
			drow[RESULTS_COL_BESTSEQUENCE] = peptideSeq;
			drow[RESULTS_COL_PEPTIDESCORE] = MathUtilities.ValueToString(pScore);

			int intNonZeroCount = (from item in positionList where item > 0 select item).Count();

			if (intNonZeroCount == 0)
			{
				drow[RESULTS_COL_ASCORE] = "-1";
               
			}
			else
			{
				drow[RESULTS_COL_ASCORE] = "1000";
			}
			drow[RESULTS_COL_NUMSITEIONSMATCHED] = 0;
			drow[RESULTS_COL_NUMSITEIONSPOSS] = 0;
			drow[RESULTS_COL_SECONDSEQUENCE] = "---";
			drow[RESULTS_COL_MODINFO] = modInfo;

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
