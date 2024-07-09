using System.Collections;
using NetLib.NetworkVar;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Standard
{
    [Category("Standard")]
    public class NetworkVarTest
    {
        [UnityTest]
        public IEnumerator NetworkVarTestConversion()
        {
            // explicit NetworkVar -> T
            NetworkVar<int> a = new NetworkVar<int>(42);
            Assert.AreEqual(42, (int)a);

            // implicit NetworkVar -> T
            NetworkVar<int> b = new NetworkVar<int>(56);
            int c = b;
            Assert.AreEqual(56, c);

            // explicit T -> NetworkVar
            int d = 42;
            Assert.AreEqual(42, ((NetworkVar<int>)(d)).Value);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NetworkVarTestAssignment()
        {
            NetworkVar<int> a = new NetworkVar<int>();
            Assert.AreEqual(0, a.Value);

            a.Value = 20;
            Assert.AreEqual(20, a.Value);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NetworkVarTestDirtyFlag()
        {
            NetworkVar<int> a = new NetworkVar<int>(0);
            Assert.IsFalse(a.IsDirty);

            a.Value = 42;
            Assert.IsTrue(a.IsDirty);

            a.ResetDirty();
            Assert.IsFalse(a.IsDirty);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NetworkVarTestReadWriteInt()
        {
            NetworkVar<int> original;
            NetworkVar<int> copy;
            byte[] serializedValue;

            // test int
            original = new NetworkVar<int>(42);
            original.ReadValue(out serializedValue);

            copy = new NetworkVar<int>(12);
            Assert.AreEqual(12, copy.Value);
            copy.WriteValue(serializedValue);
            Assert.AreEqual(42, copy.Value);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NetworkVarTestReadWriteFloat()
        {
            NetworkVar<float> original;
            NetworkVar<float> copy;
            byte[] serializedValue;

            // test float
            original = new NetworkVar<float>(4.2f);
            original.ReadValue(out serializedValue);

            copy = new NetworkVar<float>(1.2f);
            Assert.AreEqual(1.2f, copy.Value);
            copy.WriteValue(serializedValue);
            Assert.AreEqual(4.2f, copy.Value);

            yield return null;
        }
    }
}
