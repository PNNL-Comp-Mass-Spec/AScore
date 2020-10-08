using System.Linq;
using System.Data;

namespace AScore_DLL.Managers.DatasetManagers
{
    /// <summary>
    /// Base class for classes that track PSM results for a dataset
    /// </summary>
    public abstract class DatasetManager
    {
        // Ignore Spelling: ascore

        #region "Constants"

        public const string RESULTS_COL_JOB = "Job";
        public const string RESULTS_COL_SCAN = "Scan";
        public const string RESULTS_COL_ORIGINAL_SEQUENCE = "OriginalSequence";
        public const string RESULTS_COL_BEST_SEQUENCE = "BestSequence";
        public const string RESULTS_COL_PEPTIDE_SCORE = "PeptideScore";
        public const string RESULTS_COL_ASCORE = "AScore";
        public const string RESULTS_COL_NUM_SITE_IONS_POSS = "numSiteIonsPoss";
        public const string RESULTS_COL_NUM_SITE_IONS_MATCHED = "numSiteIonsMatched";
        public const string RESULTS_COL_SECOND_SEQUENCE = "SecondSequence";
        public const string RESULTS_COL_MOD_INFO = "ModInfo";

        #endregion

        /// <summary>
        /// Data read from the _fht.txt, _syn.txt, or .mzid file
        /// </summary>
        protected DataTable mDataTable;

        protected DataTable mAScoresTable;
        protected int mCurrentRow;
        protected int maxSteps;
        public bool AtEnd { get; set; }
        protected string m_jobNum;
        protected string mDatasetFilePath;
        protected bool m_MSGFSpecProbColumnPresent;

        #region "Properties"

        public string DatasetFilePath => mDatasetFilePath;

        public string JobNum => m_jobNum;

        public bool MSGFSpecProbColumnPresent => m_MSGFSpecProbColumnPresent;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fhtFileName"></param>
        /// <param name="isFht"></param>
        protected DatasetManager(string fhtFileName, bool isFht = true)
        {
            mDatasetFilePath = fhtFileName;
            if (isFht)
            {
                mDataTable = Utilities.TextFileToDataTableAssignTypeString(fhtFileName, false);

                if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
                    m_jobNum = (string) mDataTable.Rows[0][RESULTS_COL_JOB];
                else
                    m_jobNum = "0";

                m_MSGFSpecProbColumnPresent = mDataTable.Columns.Contains("MSGF_SpecProb");

                maxSteps = mDataTable.Rows.Count;
            }
            else
            {
                m_jobNum = "0";
            }

            AtEnd = false;
            InitializeAScores();
        }

        private void InitializeAScores()
        {
            mAScoresTable = new DataTable();

            mAScoresTable.Columns.Add(RESULTS_COL_JOB, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_SCAN, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_ORIGINAL_SEQUENCE, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_BEST_SEQUENCE, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_PEPTIDE_SCORE, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_ASCORE, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_NUM_SITE_IONS_POSS, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_NUM_SITE_IONS_MATCHED, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_SECOND_SEQUENCE, typeof(string));
            mAScoresTable.Columns.Add(RESULTS_COL_MOD_INFO, typeof(string));
        }

        /// <summary>
        /// Number of PSMs loaded from the results file
        /// </summary>
        /// <returns></returns>
        public virtual int GetRowLength()
        {
            return mDataTable.Rows.Count;
        }

        /// <summary>
        /// Get the row information needed to process next spectra
        /// </summary>
        /// <param name="scanNumber">scan number</param>
        /// <param name="scanCount">number of scans, usually 1</param>
        /// <param name="chargeState">charge state</param>
        /// <param name="peptideSeq">peptide sequence</param>
        /// <param name="ascoreParam">Parameter file manager</param>
        public abstract void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq, ref ParameterFileManager ascoreParam);

        public abstract void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam);

        /// <summary>
        /// Resets the counter to the beginning
        /// </summary>
        public void ResetToStart()
        {
            mCurrentRow = 0;
        }

        public int CurrentRowNum => mCurrentRow;

        public int ResultsCount => mAScoresTable.Rows.Count;

        /// <summary>
        /// Writes AScore information to table
        /// </summary>
        /// <param name="peptideSeq"></param>
        /// <param name="bestSeq"></param>
        /// <param name="scanNum"></param>
        /// <param name="topPeptideScore"></param>
        /// <param name="ascoreResult"></param>
        public virtual void WriteToTable(string peptideSeq, string bestSeq, int scanNum, double topPeptideScore, AScoreResult ascoreResult)
        {
            //if (peptideSeq == "R.HGTDLWIDNM@SSAVPNHS*PEKK.D")
            //{
            //    Console.WriteLine("Debug: found " + peptideSeq);
            //}

            var newRow = mAScoresTable.NewRow();

            newRow[RESULTS_COL_JOB] = m_jobNum;
            newRow[RESULTS_COL_SCAN] = scanNum;
            newRow[RESULTS_COL_ORIGINAL_SEQUENCE] = peptideSeq;
            newRow[RESULTS_COL_BEST_SEQUENCE] = bestSeq;
            newRow[RESULTS_COL_PEPTIDE_SCORE] = PRISM.StringUtilities.ValueToString(topPeptideScore);

            newRow[RESULTS_COL_ASCORE] = PRISM.StringUtilities.ValueToString(ascoreResult.AScore);
            newRow[RESULTS_COL_NUM_SITE_IONS_POSS] = ascoreResult.NumSiteIons;
            newRow[RESULTS_COL_NUM_SITE_IONS_MATCHED] = ascoreResult.SiteDetermineMatched;
            newRow[RESULTS_COL_SECOND_SEQUENCE] = ascoreResult.SecondSequence;
            newRow[RESULTS_COL_MOD_INFO] = ascoreResult.ModInfo;

            mAScoresTable.Rows.Add(newRow);
        }

        /// <summary>
        /// Writes ascore information to table
        /// Call this function if a peptide has zero or just one modifiable site
        /// </summary>
        /// <param name="peptideSeq"></param>
        /// <param name="scanNum"></param>
        /// <param name="pScore"></param>
        /// <param name="positionList"></param>
        /// <param name="modInfo"></param>
        public virtual void WriteToTable(string peptideSeq, int scanNum, double pScore, int[] positionList, string modInfo)
        {
            //if (peptideSeq == "R.HGTDLWIDNM@SSAVPNHS*PEKK.D")
            //{
            //    Console.WriteLine("Debug: found " + peptideSeq);
            //}

            var dataRow = mAScoresTable.NewRow();

            dataRow[RESULTS_COL_JOB] = m_jobNum;
            dataRow[RESULTS_COL_SCAN] = scanNum;
            dataRow[RESULTS_COL_ORIGINAL_SEQUENCE] = peptideSeq;
            dataRow[RESULTS_COL_BEST_SEQUENCE] = peptideSeq;
            dataRow[RESULTS_COL_PEPTIDE_SCORE] = PRISM.StringUtilities.ValueToString(pScore);

            var intNonZeroCount = (from item in positionList where item > 0 select item).Count();

            if (intNonZeroCount == 0)
            {
                dataRow[RESULTS_COL_ASCORE] = "-1";
            }
            else
            {
                dataRow[RESULTS_COL_ASCORE] = "1000";
            }
            dataRow[RESULTS_COL_NUM_SITE_IONS_MATCHED] = 0;
            dataRow[RESULTS_COL_NUM_SITE_IONS_POSS] = 0;
            dataRow[RESULTS_COL_SECOND_SEQUENCE] = "---";
            dataRow[RESULTS_COL_MOD_INFO] = modInfo;

            mAScoresTable.Rows.Add(dataRow);
        }

        /// <summary>
        /// Writes the current dataset to file
        /// </summary>
        /// <param name="outputFilePath"></param>
        public void WriteToFile(string outputFilePath)
        {
            Utilities.WriteDataTableToText(mAScoresTable, outputFilePath);
        }

        /// <summary>
        /// Increment the row number
        /// </summary>
        public void IncrementRow()
        {
            mCurrentRow++;
            if (mCurrentRow == maxSteps)
            {
                AtEnd = true;
            }
        }
    }
}
