﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Vocup.Util
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Checks whether one of the specified chars is present in this string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string value, string chars)
        {
            foreach (var item in chars)
                if (value.Contains(item))
                    return true;
            return false;
        }
    }

    public static class CollectionExtensions
    {
        [ThreadStatic] private static Random local;

        private static Random Random
            => local ?? (local =
                new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        /// <summary>
        ///     Shuffles a <see cref="IList{T}" /> by reordering all elements randomly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <remarks>https://stackoverflow.com/questions/273313/randomize-a-listt</remarks>
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static IOrderedEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.OrderBy(x => Random.Next());
        }
    }
}