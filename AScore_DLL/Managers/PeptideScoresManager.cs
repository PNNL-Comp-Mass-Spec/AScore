//Joshua Aldrich

using System.Collections.Generic;
using System;

namespace AScore_DLL.Managers
{
	/// <summary>
	/// Maintains a list of available peptide scores
	/// </summary>
	public static class PeptideScoresManager
	{
		#region Class Members
	
		private static readonly Dictionary<int, double> mCachedFactorial = new Dictionary<int, double>();

		#endregion // Class Members

		#region Public Methods

		/// <summary>
		/// Gets the peptide score based on the input parameters.
		/// </summary>
		/// <returns>The peptide score if it exists, -1 if it does not.</returns>
		public static double GetPeptideScore(double prob, int numPossMatch, int matches)
		{
			double sum = 0.0;

			double success = Math.Log10(prob);
			double fail = Math.Log10(1 - prob);
			for (int i = matches; i <= numPossMatch; i++)
			{
				double logTotal = 0;
				logTotal = success * (i) + fail * (numPossMatch - i);
				logTotal += LogAChooseB(numPossMatch, i);
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
			double value;
			if (mCachedFactorial.TryGetValue(n, out value))
				return value;

			//log n! = 0.5log(2.pi) + 0.5logn + nlog(n/e) + log(1 + 1/(12n))
			value = 0.5 * (
				Math.Log10(2 * Math.PI * n))
				+ n * (Math.Log10(n / Math.E))
				+ (Math.Log10(1.0 + 1.0 / (12 * n)));

			mCachedFactorial.Add(n, value);

			return value;
		}

		#endregion // Public Methods
	}
}