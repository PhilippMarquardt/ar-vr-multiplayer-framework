using System;
using System.Collections.Generic;
using NetLib.Extensions;
using NUnit.Framework;
// ReSharper disable IteratorMethodResultIsIgnored

namespace Standard
{
    [Category("Standard")]
    public class ExtensionsTest
    {
        [Test]
        public void TestArraySplitEven()
        {
            var input = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var result = input.Split(2);

            var expected = new List<List<byte>>()
            {
                new List<byte>() {0, 1},
                new List<byte>() {2, 3},
                new List<byte>() {4, 5},
                new List<byte>() {6, 7},
                new List<byte>() {8, 9}
            };

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void TestArraySplitOdd()
        {
            var input = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var result = input.Split(3);

            var expected = new List<List<byte>>()
            {
                new List<byte>() {0, 1, 2},
                new List<byte>() {3, 4, 5},
                new List<byte>() {6, 7, 8},
                new List<byte>() {9},
            };

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void TestArraySplitEmpty()
        {
            var input = new byte[0];
            var result = input.Split(3);

            CollectionAssert.AreEqual(new List<List<byte>>(), result);
        }

        [Test]
        public void TestArraySplitZeroSize()
        {
            var input = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Throws<ArgumentOutOfRangeException>(() => input.Split(0));
        }

        [Test]
        public void TestArraySplitNegativeSize()
        {
            var input = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Throws<ArgumentOutOfRangeException>(() => input.Split(-1));
        }

        [Test]
        public void TestArraySplitNull()
        {
            byte[] input = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => input.Split(-1));
        }
    }
}
