namespace AScore_DLL.Managers.PSM_Managers
{
    /// <summary>
    /// Track MS-GF+ PSM results from a _fht.txt or _syn.txt file
    /// </summary>
    public class MsgfdbFHT : PsmResultsManager
    {
        // Ignore Spelling: hcd, etd, cid, Frag

        public MsgfdbFHT(string fhtOrSynFilePath) : base(fhtOrSynFilePath) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq,
            ref ParameterFileManager ascoreParams)
        {
            if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
            {
                m_jobNum = (string)mDataTable.Rows[mCurrentRow][RESULTS_COL_JOB];
                m_JobColumnDefined = true;
            }

            scanNumber = int.Parse((string)mDataTable.Rows[mCurrentRow]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)mDataTable.Rows[mCurrentRow]["Charge"]);
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide"];

            if (mDataTable.Columns.Contains("FragMethod"))
            {
                var fragType = ((string)mDataTable.Rows[mCurrentRow]["FragMethod"]).ToLower();

                ascoreParams.FragmentType = fragType switch
                {
                    "hcd" => FragmentType.HCD,
                    "etd" => FragmentType.ETD,
                    "cid" => FragmentType.CID,
                    _ => FragmentType.Unspecified
                };
            }
            else
            {
                ascoreParams.FragmentType = FragmentType.Unspecified;
            }
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParams)
        {
            GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParams);

            msgfScore = 0;

            if (mDataTable.Columns.Contains("MSGF_SpecProb"))
            {
                if (!double.TryParse((string) mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"], out msgfScore))
                    msgfScore = 0;
            }
            else if (mDataTable.Columns.Contains("MSGFDB_SpecEValue"))
            {
                if (!double.TryParse((string)mDataTable.Rows[mCurrentRow]["MSGFDB_SpecEValue"], out msgfScore))
                    msgfScore = 0;
            }
        }
    }
}
