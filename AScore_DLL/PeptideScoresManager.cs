//Joshua Aldrich

using System.Collections.Generic;
using System.IO;
using System;

namespace AScore_DLL
{
	/// <summary>
	/// Maintains a list of available peptide scores
	/// </summary>
	public static class PeptideScoresManager
	{
		#region Class Members

		#region Variables

		#endregion // Variables

		#endregion // Class Members

		#region Public Methods

		/// <summary>
		/// Gets the peptide score based on the input parameters.
		/// </summary>
		/// <param name="matchedIons">Number of matched ions</param>
		/// <param name="numTheoSpec">Number of theoretical spectra</param>
		/// <param name="peakDepth">Peak depth</param>
		/// <returns>The peptide score if it exists, -1 if it does not.</returns>
		public static double GetPeptideScore(double prob, int numPossMatch, int matches)
		{
			double sum = 0.0;

			double success = (double)System.Math.Log10(prob);
			double fail = (double)System.Math.Log10(1 - prob);
			for (int i = matches; i <= numPossMatch; i++)
			{
				double logTotal = 0;
				logTotal = success * (i) + fail * (numPossMatch - i);
				logTotal += (double)LogAChooseB(numPossMatch, i);
				sum += Math.Pow(10, logTotal);
			}
			sum = -10 * Math.Log(sum, 10);
			return sum;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private static double LogAChooseB(int a, int b) // 10 choose 2 returns 10.9/1.2
		{
			if (a == 0) return 0.0f;
			if (b == 0) return 0.0f;
			if (a == b) return 0.0f;
			double total = 0.0;
			total += LogFactorial(a);
			total -= LogFactorial(b);
			total -= LogFactorial(a - b);
			return (double)total;
		}

		/// <summary>
		/// Performs the log 10 factorial
		/// </summary>
		/// <param name="n">number of terms</param>
		/// <returns></returns>
		private static double LogFactorial(int n)
		{
			//log n! = 0.5log(2.pi) + 0.5logn + nlog(n/e) + log(1 + 1/(12n))
			return (double)0.5 * (
				System.Math.Log10(2 * System.Math.PI * n))
				+ n * (System.Math.Log10(n / System.Math.E))
				+ (System.Math.Log10(1.0 + 1.0 / (12 * n)));
		}

		#endregion // Public Methods
	}
}