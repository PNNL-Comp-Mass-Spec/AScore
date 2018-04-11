////////////////////////////////////////////////////////////////////////////////
// © 2010 Pacific Northwest National Laboratories
//
// File: ValueIndexPair.cs
// Author: Jeremy Rehkop
// Date Created: 2/15/2010
//
// Last Updated: 2/15/2010 - Jeremy Rehkop
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace AScore_DLL
{
    /// <summary>
    /// Stores a value and a specified index associated with the value.
    /// </summary>
    /// <typeparam name="ValueType">The type of value to store.</typeparam>
    public struct ValueIndexPair<T> where T : IComparable
    {
        #region Class Members

        #region Variables

        private readonly T value;
        private readonly int index;

        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets the value associated with this ValueIndexPair
        /// </summary>
        public T Value => value;

        /// <summary>
        /// Gets the index associated with this ValueIndexPair
        /// </summary>
        public int Index => index;

        #endregion // Properties

        #endregion // Class Members

        #region Constructor

        /// <summary>
        /// Initializes a new instance of ValueIndexPair
        /// </summary>
        /// <param name="val">Value to store in this ValueIndexPair</param>
        /// <param name="ind">Index to store in this ValueIndexPair</param>
        public ValueIndexPair(T val, int ind)
        {
            value = val;
            index = ind;
        }

        #endregion // Constructor

        #region Comparison Classes

        /// <summary>
        /// Implements sorting of ExperimentalSpectraEntry by value2 in
        /// descending order
        /// </summary>
        public class SortValueDescend : IComparer<ValueIndexPair<T>>
        {
            public int Compare(ValueIndexPair<T> x, ValueIndexPair<T> y)
            {
                var valueCompare = x.value.CompareTo(y.value);
                if (valueCompare != 0)
                {
                    return -1 * valueCompare;
                }
                else
                {
                    return x.index.CompareTo(y.index);
                }
            }
        }

        #endregion // Comparison Classes
    }
}
