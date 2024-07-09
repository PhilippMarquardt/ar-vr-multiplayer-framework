using System;
using System.Collections;
using System.Text.RegularExpressions;
using NetLib.Messaging;
using NetLib.NetworkVar;
using NetLib.Script;
using NUnit.Framework;
using TestUtils;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// ReSharper disable Unity.InefficientPropertyAccess

namespace Standard
{
    [Category("Standard")]
    public class NetworkObjectTest
    {
        private class TestBehaviour : NetworkBehaviour
        {
            public NetworkVar<int> i = new NetworkVar<int>(0);
        }

        private GameObject go1;
        private NetworkObject no1;
        private GameObject go2;
        private NetworkObject no2;

        private GameObject networkManagerObject;
        private NetworkManager networkManager;

        [SetUp]
        public void Setup()
        {
            networkManagerObject = new GameObject();
            networkManager = networkManagerObject.AddComponent<NetworkManager>();

            go1 = new GameObject();
            go1.SetActive(false);
            no1 = go1.AddComponent<NetworkObject>();
            no1.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);
            
            go2 = new GameObject();
            go2.SetActive(false);
            no2 = go2.AddComponent<NetworkObject>();
            no2.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(networkManagerObject);
            Object.DestroyImmediate(go1);
            Object.DestroyImmediate(go2);
        }

        [UnityTest]
        public IEnumerator TestPositionDirty()
        {
            no1.syncPosition = true;
            no1.syncRotation = false;
            no1.syncVars = false;
            no1.transform.position = Vector3.zero;
            no1.transform.rotation = Quaternion.identity;

            go1.SetActive(true);

            Assert.IsFalse(no1.IsDirty());

            no1.transform.rotation = Quaternion.Euler(0, 45, 45);

            Assert.IsFalse(no1.IsDirty());

            go1.transform.position = new Vector3(1, 2, 3);

            Assert.IsTrue(no1.IsDirty());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestRotationDirty()
        {
            no1.syncPosition = false;
            no1.syncRotation = true;
            no1.syncVars = false;
            no1.transform.position = Vector3.zero;
            no1.transform.rotation = Quaternion.identity;

            go1.SetActive(true);

            Assert.IsFalse(no1.IsDirty());

            go1.transform.position = new Vector3(1, 2, 3);

            Assert.IsFalse(no1.IsDirty());

            no1.transform.rotation = Quaternion.Euler(0, 45, 45);

            Assert.IsTrue(no1.IsDirty());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBehaviourDirty()
        {
            no1.syncPosition = false;
            no1.syncRotation = false;
            no1.syncVars = true;
            no1.transform.position = Vector3.zero;
            no1.transform.rotation = Quaternion.identity;
            go1.AddComponent<TestBehaviour>();
            no1.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);

            go1.SetActive(true);

            Assert.IsFalse(no1.IsDirty());

            go1.transform.position = new Vector3(1, 2, 3);

            Assert.IsFalse(no1.IsDirty());

            no1.transform.rotation = Quaternion.Euler(0, 45, 45);

            Assert.IsFalse(no1.IsDirty());

            no1.GetComponent<TestBehaviour>().i.Value = 42;

            Assert.IsTrue(no1.IsDirty());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestManualDirty()
        {
            no1.syncPosition = false;
            no1.syncRotation = false;
            no1.syncVars = false;
            no1.transform.position = Vector3.zero;

            go1.SetActive(true);

            no1.transform.position = new Vector3(1, 2, 3);

            Assert.IsFalse(no1.IsDirty());

            no1.MarkDirty();

            Assert.IsTrue(no1.IsDirty());

            no1.ResetDirty();

            Assert.IsFalse(no1.IsDirty());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSerializeDeserialize()
        {
            // setup
            no1.syncPosition = true;
            no1.syncRotation = true;
            no1.syncVars = true;
            no2.syncPosition = true;
            no2.syncRotation = true;
            no2.syncVars = true;

            no1.transform.position = Vector3.zero;
            no1.transform.rotation = Quaternion.identity;
            var testBehaviour1A = go1.AddComponent<TestBehaviour>();
            var testBehaviour1B = go1.AddComponent<TestBehaviour>();
            no1.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);

            var testBehaviour2A = go2.AddComponent<TestBehaviour>();
            var testBehaviour2B = go2.AddComponent<TestBehaviour>();
            no2.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);

            go1.SetActive(true);
            go2.SetActive(true);

            go1.transform.position = new Vector3(1, 2, 3);
            go1.transform.rotation = Quaternion.Euler(0, 45, 45);
            testBehaviour1B.i.Value = 42;

            // serialization
            var data = no1.Serialize();
            no2.Deserialize(data);

            // assertions
            Assert.AreEqual(new Vector3(1, 2, 3), go2.transform.position);
            Assert.AreEqual(go1.transform.position, go2.transform.position);
            
            Assert.AreEqual(Quaternion.Euler(0, 45, 45), go2.transform.rotation);
            Assert.AreEqual(go1.transform.rotation, go2.transform.rotation);

            Assert.AreEqual(42, testBehaviour2B.i.Value);
            Assert.AreEqual(testBehaviour1B.i.Value, testBehaviour2B.i.Value);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestAnchorRelativePosition()
        {
            var testAnchor1 = new GameObject();
            testAnchor1.name = "testAnchor1";
            var testAnchor2 = new GameObject();
            testAnchor2.name = "testAnchor2";

            no1.syncVars = false;
            no1.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no1.anchor = testAnchor1.name;

            no2.syncVars = false;
            no2.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no2.anchor = testAnchor2.name;

            go1.SetActive(true);
            go2.SetActive(true);

            testAnchor1.transform.position = new Vector3(1, 0, 9);
            no1.transform.position = new Vector3(5, 2, 3);

            testAnchor2.transform.position = new Vector3(-2, 0, 1);

            // test serialization
            var data = no1.Serialize();
            no2.Deserialize(data);

            Assert.AreEqual(new Vector3(1, 0, 9), testAnchor1.transform.position);
            Assert.AreEqual(new Vector3(5, 2, 3), no1.transform.position);

            Assert.AreEqual(new Vector3(-2, 0, 1), testAnchor2.transform.position);
            Assert.AreEqual(new Vector3(2, 2, -5), no2.transform.position);

            // test moving
            no1.transform.position += new Vector3(10f, 27f, 15f);

            Assert.IsTrue(no1.IsDirty());

            data = no1.Serialize();
            no2.Deserialize(data);

            Assert.AreEqual(new Vector3(1, 0, 9), testAnchor1.transform.position);
            Assert.AreEqual(new Vector3(15, 29, 18), no1.transform.position);

            Assert.AreEqual(new Vector3(-2, 0, 1), testAnchor2.transform.position);
            Assert.AreEqual(new Vector3(12, 29, 10), no2.transform.position);

            yield return null;
        }

        [Test]
        public void TestGlobalPosition()
        {
            // setup
            no1.syncPosition = true;
            no1.syncRotation = false;
            no1.syncVars = false;
            no2.syncPosition = true;
            no2.syncRotation = false;
            no2.syncVars = false;

            no1.positionSyncMode = NetworkObject.PositionSyncOption.GlobalPosition;
            no2.positionSyncMode = NetworkObject.PositionSyncOption.GlobalPosition;

            var parent1 = new GameObject();
            parent1.transform.position = new Vector3(5, 1, 7);
            var parent2 = new GameObject();
            parent2.transform.position = new Vector3(90, 5, 1);

            no1.transform.parent = parent1.transform;
            no2.transform.parent = parent2.transform;

            no1.transform.position = new Vector3(1, 2, 3);
            no2.Deserialize(no1.Serialize());

            Assert.AreEqual(new Vector3(1, 2, 3), no2.transform.position);
        }

        [Test]
        public void TestInitialize()
        {
            Assert.Throws<ArgumentNullException>(() =>
                no1.Initialize(null, new MessageSystem(new BypassServer(), ""), 0));

            Assert.Throws<ArgumentNullException>(() =>
                no1.Initialize(networkManager, null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                no1.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), -1));
        }

        [Test]
        public void TestBehaviourMismatch()
        {
            no1.gameObject.AddComponent<TestBehaviour>();
            no1.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 0);

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            no2.DeserializeOnSpawn(no1.SerializeOnSpawn());
        }

        [Test]
        public void TestNoAnchorSpecified()
        {
            no1.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no2.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no1.anchor = null;

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            Assert.Throws<NullReferenceException>(() => no2.Deserialize(no1.Serialize()));
        }

        [Test]
        public void TestAnchorNotFound()
        {
            no1.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no2.positionSyncMode = NetworkObject.PositionSyncOption.RelativePosition;
            no1.anchor = "hallo";

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            Assert.Throws<NullReferenceException>(() => no2.Deserialize(no1.Serialize()));
        }

        [UnityTest]
        public IEnumerator TestLerping()
        {
            no1.lerpPosition = true;
            no1.lerpRotation = true;
            no2.lerpPosition = true;
            no2.lerpRotation = true;
            no2.Initialize(networkManager, new MessageSystem(new BypassServer(), ""), 1);

            no2.transform.position = new Vector3(0, 0, 0);
            no1.transform.position = new Vector3(1, 2, 3);

            no2.Deserialize(no1.Serialize());
            go2.SetActive(true);

            yield return new WaitForSeconds(0.1f);

            Assert.AreNotEqual(new Vector3(0, 0, 0), no2.transform.position);
            Assert.AreNotEqual(new Vector3(1, 2, 3), no2.transform.position);

            yield return null;
        }
    }
}
