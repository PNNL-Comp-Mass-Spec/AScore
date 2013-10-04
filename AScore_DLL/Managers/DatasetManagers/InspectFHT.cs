namespace AScore_DLL.Managers.DatasetManagers
{
	public class InspectFHT : DatasetManager
	{


		public InspectFHT(string fhtFileName) : base(fhtFileName) { }

		public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, 
			ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
		{
			scanNumber = int.Parse((string)dt.Rows[t]["Scan"]);
			scanCount = 1;
			chargeState = int.Parse((string)dt.Rows[t]["Charge"]);
			peptideSeq = (string)dt.Rows[t]["Peptide"];
		}

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            scanNumber = int.Parse((string)dt.Rows[t]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)dt.Rows[t]["Charge"]);
            peptideSeq = (string)dt.Rows[t]["Peptide"];
            msgfScore = 0;
            double.TryParse((string)dt.Rows[t]["MSGF_SpecProb"],out msgfScore);
        }

	}


}
