using System;
using System.Collections.Generic;

namespace AScore_DLL
{
    /// <summary>
    /// Represents an individual entry within a dta file
    /// </summary>
    public class ExperimentalSpectraEntry
    {
        #region Class Members

        #region Variables

        public double value1;
        public double value2;

        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets the first value in this ExperimentalSpectraEntry
        /// </summary>
        public double Value1
        {
            get { return value1; }
        }

        /// <summary>
        /// Gets the second value in this ExperimentalSpectraEntry
        /// </summary>
        public double Value2
        {
            get { return value2; }
        }

        #endregion // Properties

        #endregion // Class Members

        #region Constructor

        /// <summary>
        /// Initializes a new instance of ExperimentalSpectraEntry
        /// </summary>
        /// <param name="val1">First value of this ExperimentalSpectraEntry</param>
        /// <param name="val2">Second value of this ExperimentalSpectraEntry</param>
        public ExperimentalSpectraEntry(double val1, double val2)
        {
            value1 = val1;
            value2 = val2;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">ExperimentalSpectraEntry to make a copy of</param>
        public ExperimentalSpectraEntry(ExperimentalSpectraEntry copy)
        {
            value1 = copy.value1;
            value2 = copy.value2;
        }

        #endregion // Constructor

        #region Comparison Classes

        /// <summary>
        /// Sorts Value2 of the ExperimentalSpectraEntry's in descending order
        /// </summary>
        public class SortValue2Descend : IComparer<ExperimentalSpectraEntry>
        {
            public int Compare(ExperimentalSpectraEntry x, ExperimentalSpectraEntry y)
            {
                return (-1 * x.value2.CompareTo(y.value2));
            }
        }

        #endregion // Comparison Classes
    }
}