﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Linq.Extras.Internal;

namespace Linq.Extras
{
    /// <summary>
    /// Provides extension and helper methods to create, combine and work with comparers.
    /// </summary>
    [PublicAPI]
    public static class XComparer
    {
        /// <summary>
        /// Returns a comparer that has the reverse logic of the original comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="comparer">The original comparer.</param>
        /// <returns>A comparer with the reverse logic of the original one.</returns>
        [Pure]
        public static IComparer<T> Reverse<T>([NotNull] this IComparer<T> comparer)
        {
            comparer.CheckArgumentNull("comparer");
            return new ReverseComparer<T>(comparer);
        }

        /// <summary>
        /// Chains two comparers. The resulting comparer will first compare items using <c>comparer</c>,
        /// and if it's not enough to decide which is greater, it will use <c>nextComparer</c> to decide.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="comparer">The primary comparer.</param>
        /// <param name="nextComparer">The secondary comparer.</param>
        /// <returns>A comparer that will use the primary comparer, then the secondary comparer if necessary.</returns>
        [Pure]
        public static IComparer<T> ChainWith<T>(
            [NotNull] this IComparer<T> comparer,
            [NotNull] IComparer<T> nextComparer)
        {
            comparer.CheckArgumentNull("comparer");
            nextComparer.CheckArgumentNull("nextComparer");

            // Optimized to avoid nested chained comparers
            var chained = comparer as ChainedComparer<T>;
            var nextChained = nextComparer as ChainedComparer<T>;
            if (chained != null && nextChained != null)
                return new ChainedComparer<T>(chained, nextChained);
            if (chained != null)
                return new ChainedComparer<T>(chained, nextComparer);
            if (nextChained != null)
                return new ChainedComparer<T>(comparer, nextChained);
            return new ChainedComparer<T>(new[] { comparer, nextComparer });
        }

        /// <summary>
        /// Creates a comparer with ascending order based on the specified comparison key and key comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer based on the specified comparison key and key comparer.</returns>
        [Pure]
        public static IComparer<T> By<T, TKey>(
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            keySelector.CheckArgumentNull("keySelector");
            return new ByKeyComparer<T, TKey>(keySelector, keyComparer);
        }

        /// <summary>
        /// Creates a comparer with descending order based on the specified comparison key and key comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer based on the specified comparison key and key comparer.</returns>
        [Pure]
        public static IComparer<T> ByDescending<T, TKey>(
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            return By(keySelector, keyComparer).Reverse();
        }

        /// <summary>
        /// Chains a secondary comparer with ascending order based on the specified key to an existing comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="comparer">The primary comparer.</param>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer that will use the primary comparer, then the secondary comparer if necessary.</returns>
        [Pure]
        public static IComparer<T> ThenBy<T, TKey>(
            [NotNull] this IComparer<T> comparer,
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            comparer.CheckArgumentNull("comparer");
            return comparer.ChainWith(By(keySelector, keyComparer));
        }

        /// <summary>
        /// Chains a secondary comparer with descending order based on the specified key to an existing comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="comparer">The primary comparer.</param>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer that will use the primary comparer, then the secondary comparer if necessary.</returns>
        [Pure]
        public static IComparer<T> ThenByDescending<T, TKey>(
            [NotNull] this IComparer<T> comparer,
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            comparer.CheckArgumentNull("comparer");
            return comparer.ChainWith(ByDescending(keySelector, keyComparer));
        }

        /// <summary>
        /// Returns the lesser of two items according to <c>comparer</c>.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="comparer">The comparer that performs the comparison.</param>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns><c>x</c> if <c>x</c> is lesser than or equal to <c>y</c>; otherwise, <c>y</c>.</returns>
        [Pure]
        public static T Min<T>([NotNull] this IComparer<T> comparer, T x, T y)
        {
            comparer.CheckArgumentNull("comparer");
            int cmp = comparer.Compare(x, y);
            return cmp <= 0 ? x : y;
        }

        /// <summary>
        /// Returns the greater of two items according to <c>comparer</c>.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="comparer">The comparer that performs the comparison.</param>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns><c>x</c> if <c>x</c> is greater than or equal to <c>y</c>; otherwise, <c>y</c>.</returns>
        [Pure]
        public static T Max<T>([NotNull] this IComparer<T> comparer, T x, T y)
        {
            comparer.CheckArgumentNull("comparer");
            int cmp = comparer.Compare(x, y);
            return cmp >= 0 ? x : y;
        }

        #region Comparers

        sealed class ByKeyComparer<T, TKey> : IComparer<T>
        {
            private readonly Func<T, TKey> _keySelector;
            private readonly IComparer<TKey> _keyComparer;

            public ByKeyComparer([NotNull] Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
            {
                keySelector.CheckArgumentNull("keySelector");
                _keySelector = keySelector;
                _keyComparer = keyComparer ?? Comparer<TKey>.Default;
            }

            public int Compare(T x, T y)
            {
                return _keyComparer.Compare(_keySelector(x), _keySelector(y));
            }
        }

        sealed class ReverseComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> _baseComparer;

            public ReverseComparer([NotNull] IComparer<T> baseComparer)
            {
                baseComparer.CheckArgumentNull("baseComparer");
                _baseComparer = baseComparer;
            }

            public int Compare(T x, T y)
            {
                return _baseComparer.Compare(y, x);
            }
        }

        sealed class ChainedComparer<T> : IComparer<T>
        {
            private readonly IComparer<T>[] _comparers;

            public ChainedComparer([NotNull] IComparer<T>[] comparers)
            {
                comparers.CheckArgumentNull("comparers");
                _comparers = comparers;
            }

            public ChainedComparer([NotNull] ChainedComparer<T> first, [NotNull] IComparer<T> next)
            {
                first.CheckArgumentNull("first");
                next.CheckArgumentNull("next");
                _comparers = first._comparers.Append(next).ToArray();
            }

            public ChainedComparer([NotNull] IComparer<T> first, [NotNull] ChainedComparer<T> next)
            {
                first.CheckArgumentNull("first");
                next.CheckArgumentNull("next");
                _comparers = next._comparers.Prepend(first).ToArray();
            }

            public ChainedComparer([NotNull] ChainedComparer<T> first, [NotNull] ChainedComparer<T> next)
            {
                first.CheckArgumentNull("first");
                next.CheckArgumentNull("next");
                _comparers = first._comparers.Concat(next._comparers).ToArray();
            }

            public int Compare(T x, T y)
            {
                foreach (var comparer in _comparers)
                {
                    int cmp = comparer.Compare(x, y);
                    if (cmp != 0)
                        return cmp;
                }
                return 0;
            }
        }

        #endregion

    }

    /// <summary>
    /// Provides helper methods to create comparers by taking advantage of generic type inference
    /// </summary>
    /// <typeparam name="T">The type of the objects to compare.</typeparam>
    [PublicAPI]
    [ExcludeFromCodeCoverage] // Nothing to test here, these are just shorcuts for convenience
    public static class XComparer<T>
    {
        /// <summary>
        /// Creates a comparer with ascending order based on the specified comparison key and key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer based on the specified comparison key and key comparer.</returns>
        [Pure]
        public static IComparer<T> By<TKey>(
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            return XComparer.By(keySelector, keyComparer);
        }

        /// <summary>
        /// Creates a comparer with descending order based on the specified comparison key and key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the comparison key.</typeparam>
        /// <param name="keySelector">A function that returns the comparison key.</param>
        /// <param name="keyComparer">An optional comparer used to compare the keys.</param>
        /// <returns>A comparer based on the specified comparison key and key comparer.</returns>
        [Pure]
        public static IComparer<T> ByDescending<TKey>(
            [NotNull] Func<T, TKey> keySelector,
            IComparer<TKey> keyComparer = null)
        {
            return XComparer.ByDescending(keySelector, keyComparer);
        }
    }
}
