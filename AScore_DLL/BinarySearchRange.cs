using System;
using System.Collections.Generic;
using AScore_DLL.Managers;

namespace AScore_DLL
{
    static class BinarySearchRange
    {
        public static bool FindValueRange(List<ExperimentalSpectraEntry> data, double searchMZ, double toleranceHalfWidth, out int matchIndexStart, out int matchIndexEnd)
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
                if (Math.Abs(searchMZ - data[0].Mz) > toleranceHalfWidth)
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

        private static void BinarySearch(
            IReadOnlyList<ExperimentalSpectraEntry> data,
            double searchMZ,
            double toleranceHalfWidth,
            ref int matchIndexStart,
            ref int matchIndexEnd)
        {
            while (true)
            {
                // Recursive search function

                var blnLeftDone = false;
                var blnRightDone = false;

                var intIndexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
                if (intIndexMidpoint == matchIndexStart)
                {
                    // Min and Max are next to each other
                    if (Math.Abs(searchMZ - data[matchIndexStart].Mz) > toleranceHalfWidth)
                    {
                        matchIndexStart = matchIndexEnd;
                    }
                    if (Math.Abs(searchMZ - data[matchIndexEnd].Mz) > toleranceHalfWidth)
                    {
                        matchIndexEnd = intIndexMidpoint;
                    }
                    return;
                }

                if (data[intIndexMidpoint].Mz > searchMZ + toleranceHalfWidth)
                {
                    // Out of range on the right
                    matchIndexEnd = intIndexMidpoint;
                    continue;
                }

                if (data[intIndexMidpoint].Mz < searchMZ - toleranceHalfWidth)
                {
                    // Out of range on the left
                    matchIndexStart = intIndexMidpoint;
                    continue;
                }

                // Inside range; figure out the borders
                var intLeftIndex = intIndexMidpoint;
                do
                {
                    intLeftIndex = intLeftIndex - 1;
                    if (intLeftIndex < matchIndexStart)
                    {
                        blnLeftDone = true;
                    }
                    else
                    {
                        if (Math.Abs(searchMZ - data[intLeftIndex].Mz) > toleranceHalfWidth)
                        {
                            blnLeftDone = true;
                        }
                    }
                } while (!blnLeftDone);

                var intRightIndex = intIndexMidpoint;
                do
                {
                    intRightIndex = intRightIndex + 1;
                    if (intRightIndex > matchIndexEnd)
                    {
                        blnRightDone = true;
                    }
                    else
                    {
                        if (Math.Abs(searchMZ - data[intRightIndex].Mz) > toleranceHalfWidth)
                        {
                            blnRightDone = true;
                        }
                    }
                } while (!blnRightDone);

                matchIndexStart = intLeftIndex + 1;
                matchIndexEnd = intRightIndex - 1;

                break;
            }
        }
    }
}
