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
			peptideSeq = (string)dt.Rows[t]["Peptide"];

			if (dt.Columns.Contains("FragMethod"))
			{
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
			else
				ascoreParam.FragmentType = FragmentType.Unspecified;

			
		}

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
			this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

			msgfScore = 0;

			if (dt.Columns.Contains("MSGF_SpecProb"))
				if (!double.TryParse((string)dt.Rows[t]["MSGF_SpecProb"], out msgfScore))
					msgfScore = 0;
        }

	}
}
