﻿namespace AScore_DLL.Managers.DatasetManagers
{
	public class SequestFHT : DatasetManager
	{

		public SequestFHT(string fhtFileName) : base(fhtFileName) { }


		public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
			out string peptideSeq, ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
		{
			scanNumber = int.Parse((string)dt.Rows[t]["ScanNum"]);
			scanCount = int.Parse((string)dt.Rows[t]["ScanCount"]);
			chargeState = int.Parse((string)dt.Rows[t]["ChargeState"]);
			peptideSeq = (string)dt.Rows[t]["Peptide"];
		}

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            scanNumber = int.Parse((string)dt.Rows[t]["ScanNum"]);
            scanCount = int.Parse((string)dt.Rows[t]["ScanCount"]);
            chargeState = int.Parse((string)dt.Rows[t]["ChargeState"]);
            peptideSeq = (string)dt.Rows[t]["Peptide"];
            msgfScore = 0;
            double.TryParse((string)dt.Rows[t]["MSGF_SpecProb"], out msgfScore);
        }

	}
}
