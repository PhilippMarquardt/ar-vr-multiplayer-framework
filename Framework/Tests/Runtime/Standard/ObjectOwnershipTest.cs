using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetLib.Messaging;
using NetLib.Script;
using NetLib.Transport;
using NUnit.Framework;
using TestUtils;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Standard
{
    [Category("Standard")]
    public class ObjectOwnershipTest
    {
        private GameObject networkManagerObject;
        private NetworkManager server;
        private NetworkManager client;
        private NetworkManager client2;
        private GameObject testPrefab;

        [SetUp]
        public void Setup()
        {
            var transportServer = new BypassServer();
            var transportClient = new BypassClient(transportServer);
            var transportClient2 = new BypassClient(transportServer);

            networkManagerObject = new GameObject()
            {
                hideFlags = HideFlags.DontSave
            };
            server = networkManagerObject.AddComponent<NetworkManager>();
            client = networkManagerObject.AddComponent<NetworkManager>();
            client2 = networkManagerObject.AddComponent<NetworkManager>();

            server.Transport = transportServer;
            client.Transport = transportClient;
            client2.Transport = transportClient2;

            // setup prefab
            testPrefab = new GameObject
            {
                name = "Test Prefab",
                hideFlags = HideFlags.DontSave
            };
            var netObj = testPrefab.AddComponent<NetworkObject>();
            netObj.prefabHash = "test-prefab-hash";

            server.registeredPrefabs = new List<GameObject>()
            {
                testPrefab
            };
            client.registeredPrefabs = new List<GameObject>()
            {
                testPrefab
            };
            client2.registeredPrefabs = new List<GameObject>()
            {
                testPrefab
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(networkManagerObject);
            Object.DestroyImmediate(testPrefab);
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
                if (go.activeInHierarchy)
                    Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TestClientOwnedDestroy()
        {
            server.StartServer();
            client.StartClient("");
            client2.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);
            Assert.IsTrue(client2.IsRunning);

            client.SpawnPrefab("test-prefab-hash",true);

            // Do nothing so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(4);

            var obj = AssertObjectsInScene(3);

            //try to destroy all objects in scene
            foreach (var o in obj)
            {
                client2.DestroyNetworkObject(o.gameObject);
            }

            yield return new WaitForFrames(6);

            AssertObjectsInScene(3);

            // Destroy object in scene with authorized client
            client.DestroyNetworkObject(obj[1].gameObject);

            // Do nothing so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(6);

            AssertSceneEmpty();
        }

        [UnityTest]
        public IEnumerator TestClientOwnedChange()
        {
            server.StartServer();
            client.StartClient("");
            client2.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);
            Assert.IsTrue(client2.IsRunning);

            client.SpawnPrefab("test-prefab-hash", true);

            // Do nothing so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(4);

            var obj = AssertObjectsInScene(3);

            var positionRef = obj[0].gameObject.transform.position;

            //change position of game object
            obj[0].gameObject.transform.position = new Vector3(3,4,5);

            //send unauthorized change request 
            client2.ChangeObjectState(obj[0]);

            // Do nothing so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(6);

            //check that all positions are reset to previous value 
            foreach (var o in obj)
            {
                Assert.AreEqual(positionRef, o.gameObject.transform.position);
            }

            //change position of game object
            obj[0].gameObject.transform.position = new Vector3(3, 4, 5);

            //send authorized change request
            client.ChangeObjectState(obj[0]);

            // Do nothing so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(6);

            // check that position change got accepted and has been forwarded to all clients 
            foreach (var o in obj)
            {
                Assert.AreEqual(new Vector3(3, 4, 5), o.transform.position);
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        // Asserts that n >= 2 copies of the test prefab are in the scene.
        private NetworkObject[] AssertObjectsInScene(int n)
        {
            var objects = Object.FindObjectsOfType<NetworkObject>();

            // only n objects are in the scene
            Assert.AreEqual(n, objects.Length);

            // the n objects are copies of the test prefab
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual("test-prefab-hash", objects[i].prefabHash);
            }

            // the n objects are distinct
            for (int i = 1; i < n; i++)
            {
                Assert.AreNotEqual(objects[0].gameObject, objects[i].gameObject);
            }

            return objects;
        }

        // Asserts that no spawned objects are in the scene.
        private static void AssertSceneEmpty()
        {
            var objects = Object.FindObjectsOfType<NetworkObject>();
            Assert.AreEqual(0, objects.Length);
        }

        [System.Serializable]
        private class TestMessage : CustomMessage
        {
            public int a;

            public TestMessage(int a)
            {
                this.a = a;
            }
        }
    }
}