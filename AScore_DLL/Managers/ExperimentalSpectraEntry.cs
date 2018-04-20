using System;
using System.Collections.Generic;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Represents an individual entry within a dta file. Default IComparable sorts by Mz, ascending
    /// </summary>
    public class ExperimentalSpectraEntry : IComparable<ExperimentalSpectraEntry>
    {
        #region Class Members

        #region Properties

        /// <summary>
        /// Gets the m/z in this ExperimentalSpectraEntry
        /// </summary>
        public double Mz { get; }

        /// <summary>
        /// Gets the intensity in this ExperimentalSpectraEntry
        /// </summary>
        public double Intensity { get; }

        #endregion // Properties

        #endregion // Class Members

        #region Constructor

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

        #endregion // Constructor

        #region Comparison Classes

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
        /// Sorts Value2 of the ExperimentalSpectraEntry's in descending order
        /// </summary>
        public class SortValue2Descend : IComparer<ExperimentalSpectraEntry>
        {
            public int Compare(ExperimentalSpectraEntry x, ExperimentalSpectraEntry y)
            {
                return -1 * x.Intensity.CompareTo(y.Intensity);
            }
        }

        #endregion // Comparison Classes

        /// <summary>
        /// Default comparable; sorts only by mass
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ExperimentalSpectraEntry other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            //var mzComparison = Mz.CompareTo(other.Mz);
            //if (mzComparison != 0) return mzComparison;
            //return Intensity.CompareTo(other.Intensity);
            // We really only care about the m/z
            return Mz.CompareTo(other.Mz);
        }
    }
}
