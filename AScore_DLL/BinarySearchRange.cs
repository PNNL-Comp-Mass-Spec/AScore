using System;
using System.Collections.Generic;
using AScore_DLL.Managers;

namespace AScore_DLL
{
	class BinarySearchRange
	{

		public bool FindValueRange(List<ExperimentalSpectraEntry> data, double searchMZ, double toleranceHalfWidth, out int matchIndexStart, out int matchIndexEnd)
		{
			// Searches the list for searchValue with a tolerance of +-toleranceHalfWidth
			// Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
			// Otherwise, returns false

			matchIndexStart = 0;
			matchIndexEnd = data.Count - 1;

			if (data.Count == 0)
			{
				matchIndexEnd = -1;
				return false;
			}
			
			if (data.Count == 1)
			{
				if (Math.Abs(searchMZ - data[0].value1) > toleranceHalfWidth)
				{
					// Only one data point, and it is not within tolerance
					matchIndexEnd = -1;
					return false;
				}
			}
			else
			{
				BinarySearch(data, searchMZ, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
			}

			if (matchIndexStart > matchIndexEnd)
			{
				matchIndexStart = -1;
				matchIndexEnd = -1;
				return false;
			}

			return true;
	
		}

		private void BinarySearch(List<ExperimentalSpectraEntry> data, double searchMZ, double toleranceHalfWidth, ref int matchIndexStart, ref int matchIndexEnd)
		{
			while (true)
			{
				// Recursive search function

				int intIndexMidpoint = 0;
				bool blnLeftDone = false;
				bool blnRightDone = false;

				intIndexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
				if (intIndexMidpoint == matchIndexStart)
				{
					// Min and Max are next to each other
					if (Math.Abs(searchMZ - data[matchIndexStart].value1) > toleranceHalfWidth)
					{
						matchIndexStart = matchIndexEnd;
					}
					if (Math.Abs(searchMZ - data[matchIndexEnd].value1) > toleranceHalfWidth)
					{
						matchIndexEnd = intIndexMidpoint;
					}
					return;
				}

				if (data[intIndexMidpoint].value1 > searchMZ + toleranceHalfWidth)
				{
					// Out of range on the right
					matchIndexEnd = intIndexMidpoint;
					continue;
				}
				else if (data[intIndexMidpoint].value1 < searchMZ - toleranceHalfWidth)
				{
					// Out of range on the left
					matchIndexStart = intIndexMidpoint;
					continue;
				}
				else
				{
					// Inside range; figure out the borders
					int intLeftIndex = intIndexMidpoint;
					do
					{
						intLeftIndex = intLeftIndex - 1;
						if (intLeftIndex < matchIndexStart)
						{
							blnLeftDone = true;
						}
						else
						{
							if (Math.Abs(searchMZ - data[intLeftIndex].value1) > toleranceHalfWidth)
							{
								blnLeftDone = true;
							}
						}
					} while (!blnLeftDone);

					int intRightIndex = intIndexMidpoint;
					do
					{
						intRightIndex = intRightIndex + 1;
						if (intRightIndex > matchIndexEnd)
						{
							blnRightDone = true;
						}
						else
						{
							if (Math.Abs(searchMZ - data[intRightIndex].value1) > toleranceHalfWidth)
							{
								blnRightDone = true;
							}
						}
					} while (!blnRightDone);

					matchIndexStart = intLeftIndex + 1;
					matchIndexEnd = intRightIndex - 1;
				}

				break;
			}
		}

	}
}
