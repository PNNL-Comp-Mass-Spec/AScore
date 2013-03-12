using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AScore_DLL.Managers;

namespace AScore_DLL
{

	public class IonWriter
	{

		string fileName = "";
		public IonWriter(string fileNamer)
		{
			fileName = fileNamer;
		}

		public void WriteIons(int scanNumber, string sequence, Dictionary<int, ChargeStateIons> theoIons)
		{
			using (StreamWriter sw = new StreamWriter(fileName, true))
			{
				sw.WriteLine(scanNumber + "\t" + sequence);
				List<int> keys = theoIons.Keys.ToList();
				keys.Sort();
				foreach (int n in keys)
				{
					sw.WriteLine(n);
					sw.WriteLine("Bions");
					foreach (double z in theoIons[n].BIons)
					{
						sw.WriteLine(z);
					}
					sw.WriteLine("YIons");
					{
						foreach (double z in theoIons[n].YIons)
						{
							sw.WriteLine(z);
						}
					}

				}

			}
		}

		public void MatchList(List<double> myIons)
		{
			using (StreamWriter sw = new StreamWriter(fileName, true))
			{
				sw.WriteLine("Matched Ions");
				foreach (double ion in myIons)
				{
					sw.WriteLine(ion);
				}
			}
		}
	}
}
