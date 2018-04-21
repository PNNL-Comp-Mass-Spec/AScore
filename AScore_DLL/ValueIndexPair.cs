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

namespace AScore_DLL
{
    /// <summary>
    /// Stores a value and a specified index associated with the value. Default sort orders by Value (descending), then by Index
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    public struct ValueIndexPair<T> : IComparable<ValueIndexPair<T>>
        where T : IComparable
    {
        /// <summary>
        /// Gets the value associated with this ValueIndexPair
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the index associated with this ValueIndexPair
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Initializes a new instance of ValueIndexPair
        /// </summary>
        /// <param name="val">Value to store in this ValueIndexPair</param>
        /// <param name="ind">Index to store in this ValueIndexPair</param>
        public ValueIndexPair(T val, int ind)
        {
            Value = val;
            Index = ind;
        }

        /// <summary>
        /// Default sort implementation: Sort by value descending, then by index ascending
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ValueIndexPair<T> other)
        {
            var valueComparison = other.Value.CompareTo(Value);
            if (valueComparison != 0) return valueComparison;
            return Index.CompareTo(other.Index);
        }
    }
}
