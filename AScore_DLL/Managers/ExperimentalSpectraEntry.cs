using System;
using System.Collections.Generic;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Represents an individual spectrum. Default IComparable sorts by Mz, ascending
    /// </summary>
    public class ExperimentalSpectraEntry : IComparable<ExperimentalSpectraEntry>
    {
        /// <summary>
        /// Gets the m/z in this ExperimentalSpectraEntry
        /// </summary>
        public double Mz { get; }

        /// <summary>
        /// Gets the intensity in this ExperimentalSpectraEntry
        /// </summary>
        public double Intensity { get; }

        /// <summary>
        /// Initializes a new instance of ExperimentalSpectraEntry
        /// </summary>
        /// <param name="mz">m/z of this ExperimentalSpectraEntry</param>
        /// <param name="intensity">intensity of this ExperimentalSpectraEntry</param>
        public ExperimentalSpectraEntry(double mz, double intensity)
        {
            Mz = mz;
            Intensity = intensity;
        }

        public override string ToString()
        {
            return Mz + ", " + Intensity;
        }

        public class FindValue1InTolerance : IComparer<ExperimentalSpectraEntry>
        {
            private readonly double mTolerance;

            public FindValue1InTolerance(double tolerance)
            {
                mTolerance = tolerance;
            }

            public int Compare(ExperimentalSpectraEntry x, ExperimentalSpectraEntry y)
            {
                if (Math.Abs(x.Mz - y.Mz) <= mTolerance)
                    return 0;

                return x.Mz.CompareTo(y.Mz);
            }
        }

        /// <summary>
        /// Sorts the ExperimentalSpectraEntry by Intensity in descending order
        /// </summary>
        public class SortIntensityDescend : IComparer<ExperimentalSpectraEntry>
        {
            public int Compare(ExperimentalSpectraEntry x, ExperimentalSpectraEntry y)
            {
                return -1 * x.Intensity.CompareTo(y.Intensity);
            }
        }

        /// <summary>
        /// Default comparable; sorts only by mass
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ExperimentalSpectraEntry other)
        {
            //if (ReferenceEquals(this, other)) return 0;
            if (other == null) return 1;
            //var mzComparison = Mz.CompareTo(other.Mz);
            //if (mzComparison != 0) return mzComparison;
            //return Intensity.CompareTo(other.Intensity);
            // We really only care about the m/z
            return Mz.CompareTo(other.Mz);
        }
    }
}
