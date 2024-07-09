using System.Collections;
using System.Text.RegularExpressions;
using NetLib.NetworkVar;
using NetLib.Script;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Standard
{
    [Category("Standard")]
    public class NetworkBehaviourTest
    {
        private class TestNetworkBehaviour : NetworkBehaviour
        {
            public NetworkVar<int> i;
        }

        private GameObject testGameObject1;
        private TestNetworkBehaviour testBehaviour1;
        private GameObject testGameObject2;
        private TestNetworkBehaviour testBehaviour2;

        [SetUp]
        public void Setup()
        {
            testGameObject1 = new GameObject();
            testGameObject1.AddComponent<NetworkObject>();
            testBehaviour1 = testGameObject1.AddComponent<TestNetworkBehaviour>();
            testBehaviour1.i = new NetworkVar<int>();
            testBehaviour1.Initialize();

            testGameObject2 = new GameObject();
            testGameObject2.AddComponent<NetworkObject>();
            testBehaviour2 = testGameObject2.AddComponent<TestNetworkBehaviour>();
            testBehaviour2.i = new NetworkVar<int>();
            testBehaviour2.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(testGameObject1);
            Object.DestroyImmediate(testGameObject2);
        }

        [Test]
        public void TestSerialization()
        {
            // make i dirty
            testBehaviour1.i.Value = 42;
            Assert.IsTrue(testBehaviour1.i.IsDirty);

            byte[] data = testBehaviour1.Serialize();

            Assert.AreEqual(0, testBehaviour2.i.Value);
            testBehaviour2.Deserialize(data);
            Assert.AreEqual(42, testBehaviour2.i.Value);
        }

        [Test]
        public void TestDirty()
        {
            Assert.IsFalse(testBehaviour1.i.IsDirty);
            Assert.IsFalse(testBehaviour1.IsDirty());

            // make i dirty
            testBehaviour1.i.Value = 42;
            Assert.IsTrue(testBehaviour1.i.IsDirty);
            Assert.IsTrue(testBehaviour1.IsDirty());
        }

        [Test]
        public void TestResetDirty()
        {
            Assert.IsFalse(testBehaviour1.i.IsDirty);
            Assert.IsFalse(testBehaviour1.IsDirty());

            // make i dirty
            testBehaviour1.i.Value = 42;
            Assert.IsTrue(testBehaviour1.i.IsDirty);
            Assert.IsTrue(testBehaviour1.IsDirty());

            // reset dirty
            testBehaviour1.ResetDirtyFlag();
            Assert.IsFalse(testBehaviour1.i.IsDirty);
            Assert.IsFalse(testBehaviour1.IsDirty());
        }

        [Test]
        public void TestNoNetworkObject()
        {
            Object.DestroyImmediate(testGameObject1);

            testGameObject1 = new GameObject();
            testBehaviour1 = testGameObject1.AddComponent<TestNetworkBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            testBehaviour1.Initialize();
        }

        [Test]
        public void TestUninitializedNetworkVarSerialize()
        {
            var b = testGameObject1.AddComponent<TestNetworkBehaviour>();
            b.Initialize();

            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            b.Serialize();
        }

        [Test]
        public void TestUninitializedNetworkVarDeserialize()
        {
            var data = testBehaviour1.Serialize();
            var b = testGameObject1.AddComponent<TestNetworkBehaviour>();
            b.Initialize();

            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            b.Deserialize(data);
        }
    }
}
