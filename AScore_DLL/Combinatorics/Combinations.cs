// Copyright 2008 Adrian Akison
// Distributed under license terms of CPOL http://www.codeproject.com/info/cpol10.aspx

using System.Collections;
using System.Collections.Generic;

namespace AScore_DLL.Combinatorics
{
    /// <summary>
    /// Combinations defines a meta-collection, typically a list of lists, of all possible
    /// subsets of a particular size from the set of values.  This list is enumerable and
    /// allows the scanning of all possible combinations using a simple for each() loop.
    /// Within the returned set, there is no prescribed order.  This follows the mathematical
    /// concept of choose.  For example, put 10 dominoes in a hat and pick 5.  The number of possible
    /// combinations is defined as "10 choose 5", which is calculated as (10!) / ((10 - 5)! * 5!).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MetaCollectionType parameter of the constructor allows for the creation of
    /// two types of sets,  those with and without repetition in the output set when
    /// presented with repetition in the input set.
    /// </para>
    /// <para>
    /// When given a input collect {A B C} and lower index of 2, the following sets are generated:
    /// MetaCollectionType.WithRepetition =>
    /// {A A}, {A B}, {A C}, {B B}, {B C}, {C C}
    /// MetaCollectionType.WithoutRepetition =>
    /// {A B}, {A C}, {B C}
    /// </para>
    /// <para>
    /// Input sets with multiple equal values will generate redundant combinations in proportion
    /// to the likelihood of outcome.  For example, {A A B B} and a lower index of 3 will generate:
    /// {A A B} {A A B} {A B B} {A B B}
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the values within the list.</typeparam>
    public class Combinations<T> : IMetaCollection<T>
    {
        // Ignore Spelling: Akison

        #region Constructors

        /// <summary>
        /// No default constructor, must provided a list of values and size.
        /// </summary>
        protected Combinations()
        {
        }

        /// <summary>
        /// Create a combination set from the provided list of values.
        /// The upper index is calculated as values.Count, the lower index is specified.
        /// Collection type defaults to MetaCollectionType.WithoutRepetition
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        public Combinations(ICollection<T> values, int lowerIndex)
        {
            Initialize(values, lowerIndex, GenerateOption.WithoutRepetition);
        }

        /// <summary>
        /// Create a combination set from the provided list of values.
        /// The upper index is calculated as values.Count, the lower index is specified.
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        /// <param name="type">The type of Combinations set to generate.</param>
        public Combinations(ICollection<T> values, int lowerIndex, GenerateOption type)
        {
            Initialize(values, lowerIndex, type);
        }

        #endregion

        #region IEnumerable Interface

        /// <summary>
        /// Gets an enumerator for collecting the list of combinations.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<IList<T>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Gets an enumerator for collecting the list of combinations.
        /// </summary>
        /// <returns>The enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region Enumerator Inner Class

        /// <summary>
        /// The enumerator that enumerates each meta-collection of the enclosing Combinations class.
        /// </summary>
        public class Enumerator : IEnumerator<IList<T>>
        {
            #region Constructors

            /// <summary>
            /// Construct a enumerator with the parent object.
            /// </summary>
            /// <param name="source">The source combinations object.</param>
            public Enumerator(Combinations<T> source)
            {
                myParent = source;
                myPermutationsEnumerator = (Permutations<bool>.Enumerator)myParent.myPermutations.GetEnumerator();
            }

            #endregion

            #region IEnumerator interface
            /// <summary>
            /// Resets the combinations enumerator to the first combination.
            /// </summary>
            public void Reset()
            {
                myPermutationsEnumerator.Reset();
            }

            /// <summary>
            /// Advances to the next combination of items from the set.
            /// </summary>
            /// <returns>True if successfully moved to next combination, False if no more unique combinations exist.</returns>
            /// <remarks>
            /// The heavy lifting is done by the permutations object, the combination is generated
            /// by creating a new list of those items that have a true in the permutation parallel array.
            /// </remarks>
            public bool MoveNext()
            {
                var ret = myPermutationsEnumerator.MoveNext();
                myCurrentList = null;
                return ret;
            }

            /// <summary>
            /// The current combination
            /// </summary>
            public IList<T> Current
            {
                get
                {
                    ComputeCurrent();
                    return myCurrentList;
                }
            }

            /// <summary>
            /// The current combination
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    ComputeCurrent();
                    return myCurrentList;
                }
            }

            /// <summary>
            /// Cleans up non-managed resources, of which there are none used here.
            /// </summary>
            public void Dispose()
            {
            }

            #endregion

            #region Heavy Lifting Members

            /// <summary>
            /// The only complex function of this entire wrapper, ComputeCurrent() creates
            /// a list of original values from the bool permutation provided.
            /// The exception for accessing current (InvalidOperationException) is generated
            /// by the call to .Current on the underlying enumeration.
            /// </summary>
            /// <remarks>
            /// <para>
            /// To compute the current list of values, the underlying permutation object
            /// which moves with this enumerator, is scanned differently based on the type.
            /// The items have only two values, true and false, which have different meanings:
            /// </para>
            /// <para>
            /// For type WithoutRepetition, the output is a straightforward subset of the input array.
            /// E.g. 6 choose 3 without repetition
            /// Input array:   {A B C D E F}
            /// Permutations:  {0 1 0 0 1 1}
            /// Generates set: {A   C D    }
            /// Note: size of permutation is equal to upper index.
            /// </para>
            /// <para>
            /// For type WithRepetition, the output is defined by runs of characters and when to
            /// move to the next element.
            /// E.g. 6 choose 5 with repetition
            /// Input array:   {A B C D E F}
            /// Permutations:  {0 1 0 0 1 1 0 0 1 1}
            /// Generates set: {A   B B     D D    }
            /// Note: size of permutation is equal to upper index - 1 + lower index.
            /// </para>
            /// </remarks>
            private void ComputeCurrent()
            {
                if (myCurrentList != null)
                    return;

                myCurrentList = new List<T>();
                var currentPermutation = (IList<bool>)myPermutationsEnumerator.Current;
                if (currentPermutation == null)
                    return;

                var index = 0;

                foreach (var item in currentPermutation)
                {
                    if (!item)
                    {
                        myCurrentList.Add(myParent.myValues[index]);
                        if (myParent.Type == GenerateOption.WithoutRepetition)
                        {
                            ++index;
                        }
                    }
                    else
                    {
                        ++index;
                    }
                }
            }

            #endregion

            #region Data

            /// <summary>
            /// Parent object this is an enumerator for.
            /// </summary>
            private readonly Combinations<T> myParent;

            /// <summary>
            /// The current list of values, this is lazy evaluated by the Current property.
            /// </summary>
            private List<T> myCurrentList;

            /// <summary>
            /// An enumerator of the parent's list of lexicographic orderings.
            /// </summary>
            private readonly Permutations<bool>.Enumerator myPermutationsEnumerator;

            #endregion
        }
        #endregion

        #region IMetaList Interface

        /// <summary>
        /// The number of unique combinations that are defined in this meta-collection.
        /// This value is mathematically defined as Choose(M, N) where M is the set size
        /// and N is the subset size.  This is M! / (N! * (M-N)!).
        /// </summary>
        public long Count => myPermutations.Count;

        /// <summary>
        /// The type of Combinations set that is generated.
        /// </summary>
        public GenerateOption Type { get; private set; }

        /// <summary>
        /// The upper index of the meta-collection, equal to the number of items in the initial set.
        /// </summary>
        public int UpperIndex => myValues.Count;

        /// <summary>
        /// The lower index of the meta-collection, equal to the number of items returned each iteration.
        /// </summary>
        public int LowerIndex => myLowerIndex;

        #endregion

        #region Heavy Lifting Members

        /// <summary>
        /// Initialize the combinations by settings a copy of the values from the
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        /// <param name="type">The type of Combinations set to generate.</param>
        /// <remarks>
        /// <para>
        /// Copies the array and parameters and then creates a map of booleans that will
        /// be used by a permutations object to reference the subset.  This map is slightly
        /// different based on whether the type is with or without repetition.
        /// </para>
        /// <para>
        /// When the type is WithoutRepetition, then a map of upper index elements is
        /// created with lower index false values.
        /// E.g. 8 choose 3 generates:
        /// Map: {1 1 1 1 1 0 0 0}
        /// Note: For sorting reasons, false denotes inclusion in output.
        /// </para>
        /// <para>
        /// When the type is WithRepetition, then a map of upper index - 1 + lower index
        /// elements is created with the false values indicating that the 'current' element should
        /// be included and the trues meaning to advance the 'current' element by one.
        /// E.g. 8 choose 3 generates:
        /// Map: {1 1 1 1 1 1 1 1 0 0 0} (7 true, 3 false).
        /// </para>
        /// </remarks>
        private void Initialize(ICollection<T> values, int lowerIndex, GenerateOption type)
        {
            Type = type;
            myLowerIndex = lowerIndex;
            myValues = new List<T>();
            myValues.AddRange(values);
            var myMap = new List<bool>();
            if (type == GenerateOption.WithoutRepetition)
            {
                for (var i = 0; i < myValues.Count; ++i)
                {
                    if (i >= myValues.Count - myLowerIndex)
                    {
                        myMap.Add(false);
                    }
                    else
                    {
                        myMap.Add(true);
                    }
                }
            }
            else
            {
                for (var i = 0; i < values.Count - 1; ++i)
                {
                    myMap.Add(true);
                }
                for (var i = 0; i < myLowerIndex; ++i)
                {
                    myMap.Add(false);
                }
            }
            myPermutations = new Permutations<bool>(myMap);
        }

        #endregion

        #region Data

        /// <summary>
        /// Copy of values object is initialized with, required for enumerator reset.
        /// </summary>
        private List<T> myValues;

        /// <summary>
        /// Permutations object that handles permutations on booleans for combination inclusion.
        /// </summary>
        private Permutations<bool> myPermutations;

        /// <summary>
        /// The lower index defined in the constructor.
        /// </summary>
        private int myLowerIndex;

        #endregion
    }
}
