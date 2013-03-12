using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AScore_DLL;

namespace AScore_DLL.Managers.DatasetManagers
{
	public class MsgfdbFHT : DatasetManager
	{
		public MsgfdbFHT(string fhtFileName) : base(fhtFileName) { }

		public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, 
			ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
		{
			scanNumber = int.Parse((string)dt.Rows[t]["Scan"]);
			scanCount = 1;
			chargeState = int.Parse((string)dt.Rows[t]["Charge"]);
			peptideSeq = ((string)dt.Rows[t]["Peptide"]);
			string fragtype = ((string)dt.Rows[t]["FragMethod"]).ToLower();

            switch (fragtype)
            {
                case "hcd":
                    ascoreParam.FragmentType = FragmentType.HCD;
                    break;
                case "etd":
                    ascoreParam.FragmentType = FragmentType.ETD;
                    break;
                case "cid":
                    ascoreParam.FragmentType = FragmentType.CID;
                    break;
				default:
					ascoreParam.FragmentType = FragmentType.Unspecified;
					break;
            }

			
		}

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            scanNumber = int.Parse((string)dt.Rows[t]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)dt.Rows[t]["Charge"]);
            peptideSeq = (string)dt.Rows[t]["Peptide"];
            string fragtype = ((string)dt.Rows[t]["FragMethod"]).ToLower();
            msgfScore = 0;
            double.TryParse((string)dt.Rows[t]["MSGF_SpecProb"], out msgfScore);

            switch (fragtype)
            {
                case "hcd":
                    ascoreParam.FragmentType = FragmentType.HCD;
                    break;
                case "etd":
                    ascoreParam.FragmentType = FragmentType.ETD;
                    break;
                case "cid":
                    ascoreParam.FragmentType = FragmentType.CID;
                    break;
            }
        }

	}
}
