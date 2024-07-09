using System;
using System.Collections.Generic;
using System.Linq;

namespace NetLib.Extensions
{
    /// <summary>
    /// Contains extension methods for array types.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Splits the array into several smaller enumerable objects.
        /// </summary>
        /// <remarks>
        /// The array is split into evenly sized chunks of a given length. The last chunk may be smaller if the
        /// length of the array is not divisible by the chunk length.
        /// The order of the elements is preserved.
        /// </remarks>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The array to split.</param>
        /// <param name="size">The size of the smaller arrays.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> containing smaller <see cref="IEnumerable{T}"/>
        /// or an empty <see cref="IEnumerable{T}"/> if the array has length zero.
        /// </returns>
        /// <exception cref="ArgumentNullException">The <c>array</c> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <c>size</c> is less than <c>1</c></exception>
        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size), "split size muss be greater than 0");

            return SplitInner(array, size);
        }

        private static IEnumerable<IEnumerable<T>> SplitInner<T>(T[] array, int size)
        {
            for (int i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }
    }
}
