using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetLib.NetworkVar;
using NetLib.Script;
using NUnit.Framework;
using TestUtils;
using UnityEngine;
using UnityEngine.TestTools;
using Logger = NetLib.Utils.Logger;
using Object = UnityEngine.Object;
using NetworkPlayer = NetLib.Script.NetworkPlayer; 

namespace Standard
{
    [Category("Standard")]
    public class NetworkManagerTest
    {
        private GameObject networkManagerObject;
        private NetworkManager server;
        private NetworkManager client;
        private GameObject testPrefab;

        [SetUp]
        public void Setup()
        {
            var transportServer = new BypassServer();
            var transportClient = new BypassClient(transportServer);

            networkManagerObject = new GameObject()
            {
                hideFlags = HideFlags.DontSave
            };
            server = networkManagerObject.AddComponent<NetworkManager>();
            client = networkManagerObject.AddComponent<NetworkManager>();

            Logger.Verbosity = Logger.LogLevel.Debug;

            server.Transport = transportServer;
            client.Transport = transportClient;

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
        public IEnumerator TestInitialization()
        {
            Assert.IsFalse(server.IsRunning);
            Assert.IsFalse(client.IsRunning);

            server.StartServer();

            Assert.IsTrue(server.IsRunning);
            Assert.IsFalse(client.IsRunning);

            client.StartClient("");

            // // wait for connection
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestWrongServerInitialization()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            server.Transport = new BypassClient(null);
            server.StartServer();

            Assert.IsFalse(server.IsRunning);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestWrongClientInitialization()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            client.Transport = new BypassServer();
            client.StartClient("");

            Assert.IsFalse(client.IsRunning);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientAlreadyInitialized()
        {
            client.StartClient("");

            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            client.StartClient("");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestServerAlreadyInitialized()
        {
            server.StartServer();

            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            server.StartServer();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSetTransportAfterInitialization()
        {
            server.StartServer();

            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            server.Transport = new BypassServer();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestGetTransport()
        {
            var transport = new BypassServer();
            server.Transport = transport;
            server.StartServer();

            Assert.AreEqual(transport, server.Transport);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSpawnUninitialized()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));

            var obj = Object.Instantiate(testPrefab);
            server.SpawnLocalObject(obj);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSpawnPrefabUninitialized()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            server.SpawnPrefab("test-prefab-hash");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDestroyUninitialized()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));

            var obj = Object.Instantiate(testPrefab);
            server.DestroyNetworkObject(obj);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestChangeObjectStateOnServer()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));

            server.StartServer();

            var obj = Object.Instantiate(testPrefab);
            server.ChangeObjectState(obj.GetComponent<NetworkObject>());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCustomMessage()
        {
            int serverResult = 0;
            int clientResult = 0;
            server.OnCustomMessage = msg =>
            {
                serverResult = ((TestMessage) msg).a;
            };
            client.OnCustomMessage = msg =>
            {
                clientResult = ((TestMessage)msg).a;
            };

            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            // client sends to server
            client.SendCustomMessage(new TestMessage(42));
            // Do nothing for a whole frame
            yield return new WaitForFrames(2);
            Assert.AreEqual(42, serverResult);

            // server sends to client
            server.SendCustomMessage(new TestMessage(25), 1);
            // Do nothing for a whole frame
            yield return new WaitForFrames(2);
            Assert.AreEqual(25, clientResult);
        }

        [UnityTest]
        public IEnumerator TestCustomMessageUninitialized()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            server.SendCustomMessage(new CustomMessage());
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSceneObjects()
        {
            var serverSceneObject = new GameObject();
            serverSceneObject.AddComponent<NetworkObject>();
            serverSceneObject.AddComponent<TestBehaviour>();
            serverSceneObject.transform.position = new Vector3(1, 2, 3);
            //serverSceneObject.transform.rotation = Quaternion.Euler(20, 10, 30);
            serverSceneObject.GetComponent<TestBehaviour>().netVar.Value = 42;

            server.StartServer();
            serverSceneObject.SetActive(false);


            var clientSceneObject = new GameObject();
            clientSceneObject.AddComponent<NetworkObject>();
            clientSceneObject.AddComponent<TestBehaviour>();
            clientSceneObject.transform.position = new Vector3(0, 0, 0);
            clientSceneObject.transform.rotation = Quaternion.identity;
            clientSceneObject.GetComponent<TestBehaviour>().netVar.Value = 0;

            client.StartClient();
            yield return new WaitForFrames(10);

            Assert.AreEqual(new Vector3(1, 2, 3), clientSceneObject.transform.position);
            //Assert.AreEqual(Quaternion.Euler(20, 10, 30), clientSceneObject.transform.rotation);
            Assert.AreEqual(42, clientSceneObject.GetComponent<TestBehaviour>().netVar.Value);
        }

        [UnityTest]
        public IEnumerator TestServerSpawn()
        {
            server.StartServer();

            testPrefab.AddComponent<TestBehaviour>();

            var serverPrefabObject = Object.Instantiate(testPrefab);
            serverPrefabObject.transform.position = new Vector3(1, 2, 3);
            //serverSceneObject.transform.rotation = Quaternion.Euler(20, 10, 30);
            serverPrefabObject.GetComponent<TestBehaviour>().netVar.Value = 42;

            server.SpawnLocalObject(serverPrefabObject);
            serverPrefabObject.SetActive(false);

            client.StartClient();
            yield return new WaitForFrames(10);

            var clientPrefabObject = Object.FindObjectOfType<NetworkObject>();
            Assert.AreEqual(new Vector3(1, 2, 3), clientPrefabObject.transform.position);
            //Assert.AreEqual(Quaternion.Euler(20, 10, 30), clientSceneObject.transform.rotation);
            Assert.AreEqual(42, clientPrefabObject.GetComponent<TestBehaviour>().netVar.Value);
        }

        [UnityTest]
        public IEnumerator TestServerSpawnPrefab()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            server.SpawnPrefab("test-prefab-hash");
            yield return new WaitForFrames(10);

            AssertObjectsInScene(2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientSpawn()
        {
            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            client.SpawnPrefab("test-prefab-hash");

            // Do nothing for two whole frames, so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(3);

            AssertObjectsInScene(2);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestWrongClientSpawn()
        {
            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            client.SpawnLocalObject(new GameObject());

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientDestroy()
        {
            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            var go = Object.Instantiate(testPrefab);
            go.name = "Server Object";

            server.SpawnLocalObject(go);

            // Do nothing for two whole frames, so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(3);

            var objects = AssertObjectsInScene(2);

            var goClient = objects.ToList().First(x => x.gameObject.name != "Server Object").gameObject;

            client.DestroyNetworkObject(goClient);

            // Do nothing for two whole frames, so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(3);
            // Wait one additional frame for objects to be destroyed
            yield return null;

            // check that objects are deleted
            AssertSceneEmpty();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestServerDestroy()
        {
            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(server.IsRunning);
            Assert.IsTrue(client.IsRunning);

            var go = Object.Instantiate(testPrefab);
            go.name = "Server Object";

            server.SpawnLocalObject(go);

            // Do nothing for two whole frames, so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(3);

            var objects = AssertObjectsInScene(2);

            server.DestroyNetworkObject(go);

            // Do nothing for two whole frames, so that a broadcast is guaranteed to be dispatched
            yield return new WaitForFrames(3);
            // Wait one additional frame for objects to be destroyed
            yield return null;

            // check that objects are deleted
            AssertSceneEmpty();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBehaviourInstances()
        {
            testPrefab.AddComponent<TestBehaviourInstance>();

            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "Server Object";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);


            var objects = AssertObjectsInScene(2);

            var clientObj = objects.First(x => x.name != "Server Object");

            serverObj.GetComponent<TestBehaviourInstance>().AssertInstance(true, false);
            serverObj.GetComponent<TestBehaviourInstance>().AssertConnectionId(0);

            clientObj.GetComponent<TestBehaviourInstance>().AssertInstance(false, true);
            clientObj.GetComponent<TestBehaviourInstance>().AssertConnectionId(1);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBehaviourOnNetworkStart()
        {
            testPrefab.AddComponent<TestBehaviourInstance>();

            server.StartServer();
            client.StartClient("");
            // wait for connection
            yield return new WaitForSeconds(0.1f);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "Server Object";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);

            var clientObj = objects.First(x => x.name != "Server Object");

            Assert.IsTrue(serverObj.GetComponent<TestBehaviourInstance>().onNetworkStartCalled);
            Assert.IsTrue(clientObj.GetComponent<TestBehaviourInstance>().onNetworkStartCalled);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPcPlayerSpawnOnConnect()
        {
            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";

            server.pcPlayerPrefab = testPrefab;
            client.pcPlayerObject = clientPlayer;

            server.StartServer();
            client.clientType = NetworkManager.ClientType.DesktopClient;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestArPlayerSpawnOnConnect()
        {
            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";

            server.arPlayerPrefab = testPrefab;
            client.arPlayerObject = clientPlayer;

            server.StartServer();
            client.clientType = NetworkManager.ClientType.ArClient;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestVrPlayerSpawnOnConnect()
        {
            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";

            server.vrPlayerPrefab = testPrefab;
            client.vrPlayerObject = clientPlayer;

            server.StartServer();
            client.clientType = NetworkManager.ClientType.VrClient;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPlayerDeSpawnOnDisconnect()
        {
            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";

            server.pcPlayerPrefab = testPrefab;
            client.pcPlayerObject = clientPlayer;

            server.StartServer();
            client.clientType = NetworkManager.ClientType.DesktopClient;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);

            client.Stop();
            yield return null;

            AssertObjectsInScene(1);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPlayerUpdate()
        {
            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";
            clientPlayer.name = "client";
            clientPlayer.transform.position = new Vector3(5, 6, 7);

            server.pcPlayerPrefab = testPrefab;
            client.pcPlayerObject = clientPlayer;

            server.OnPlayerSpawn = (obj, client, type) => obj.transform.position = new Vector3(9, 8, 7);

            server.StartServer();
            client.clientType = NetworkManager.ClientType.DesktopClient;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(2);
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);
            var serverPlayer = objects[0].IsClient ? objects[1] : objects[0];

            Assert.AreEqual(new Vector3(9, 8, 7), clientPlayer.transform.position);
            Assert.AreEqual(new Vector3(9, 8, 7), serverPlayer.transform.position);

            clientPlayer.transform.position = new Vector3(1, 2, 3);

            Assert.IsTrue(clientPlayer.GetComponent<NetworkObject>().IsClient);
            Assert.IsFalse(clientPlayer.GetComponent<NetworkObject>().IsServer);
            yield return new WaitForFrames(10);

            Assert.AreEqual(new Vector3(1, 2, 3), objects[1].transform.position);
            Assert.AreEqual(new Vector3(1, 2, 3), objects[0].transform.position);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPlayerWithChildren()
        {
            var child = new GameObject()
            {
                hideFlags = HideFlags.DontSave
            };
            child.AddComponent<NetworkObject>();
            child.transform.parent = testPrefab.transform;

            var clientPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(clientPlayer.GetComponent<NetworkObject>());
            clientPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";
            Object.DestroyImmediate(clientPlayer.transform.GetChild(0).GetComponent<NetworkObject>());
            clientPlayer.transform.GetChild(0).gameObject.AddComponent<NetworkPlayer>();

            clientPlayer.name = "clientPlayer";
            clientPlayer.transform.position = new Vector3(5, 6, 7);

            server.pcPlayerPrefab = testPrefab;
            client.pcPlayerObject = clientPlayer;

            server.OnPlayerSpawn = (obj, client, type) => obj.transform.position = new Vector3(9, 8, 7);

            server.StartServer();
            client.clientType = NetworkManager.ClientType.DesktopClient;
            client.ignoreServerSpawnData = true;
            client.StartClient();
            yield return new WaitForFrames(10);

            var objects = AssertObjectsInScene(4 ,false);
            var clientObjects = objects.Where(o => o.IsClient).ToList();
            var serverObjects = objects.Where(o => o.IsServer).ToList();

            var serverPlayer = serverObjects[0].PrefabHash == "test-prefab-hash" ? serverObjects[0] : serverObjects[1];

            Assert.AreEqual(new Vector3(5, 6, 7), clientPlayer.transform.position);
            Assert.AreEqual(new Vector3(5, 6, 7), serverPlayer.transform.position);

            clientPlayer.transform.GetChild(0).transform.position = new Vector3(5, 5, 5);

            yield return new WaitForFrames(10);

            Assert.AreEqual(new Vector3(5, 6, 7), clientPlayer.transform.position);
            Assert.AreEqual(new Vector3(5, 6, 7), serverPlayer.transform.position);
            Assert.AreEqual(new Vector3(5, 5, 5), clientPlayer.transform.GetChild(0).transform.position);
            Assert.AreEqual(new Vector3(5, 5, 5), serverPlayer.transform.GetChild(0).transform.position);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPlayerWithoutComponent()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            client.pcPlayerObject = new GameObject();
            server.pcPlayerPrefab = testPrefab;
            client.clientType = NetworkManager.ClientType.DesktopClient;

            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestServerPlayer()
        {
            var serverPlayer = Object.Instantiate(testPrefab);
            Object.DestroyImmediate(serverPlayer.GetComponent<NetworkObject>());
            serverPlayer.AddComponent<NetworkPlayer>().prefabHash = "test-prefab-hash";

            client.pcPlayerPrefab = testPrefab;
            server.pcPlayerPrefab = testPrefab;
            server.pcPlayerObject = serverPlayer;
            server.serverIsPlayer = true;
            server.clientType = NetworkManager.ClientType.DesktopClient;

            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            AssertObjectsInScene(2);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestChangeObjectStateNotRunning()
        {
            var obj = Object.Instantiate(testPrefab);
            client.ChangeObjectState(obj.GetComponent<NetworkObject>());
            yield return null;
        }


        // ------------------------------------------------------------------------------------------------------------

        // Asserts that n >= 2 copies of the test prefab are in the scene.
        private NetworkObject[] AssertObjectsInScene(int n, bool testHash = true)
        {
            var objects = Object.FindObjectsOfType<NetworkObject>();

            // only n objects are in the scene
            Assert.AreEqual(n, objects.Length);
            
            // the n objects are copies of the test prefab
            for (int i = 0; i < n; i++)
            {
                if (testHash)
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

        public class TestBehaviour : NetworkBehaviour
        {
            public NetworkVar<int> netVar = new NetworkVar<int>();
        }

        private class TestBehaviourInstance : NetworkBehaviour
        {
            public bool onNetworkStartCalled;

            internal void AssertInstance(bool expectedServer, bool expectedClient)
            {
                Assert.AreEqual(expectedServer, IsServer);
                Assert.AreEqual(expectedClient, IsClient);
            }

            internal void AssertConnectionId(ulong expected)
            {
                Assert.AreEqual(expected, ConnectionId);
            }

            protected override void OnNetworkStart()
            {
                onNetworkStartCalled = true;
            }
        }
    }
}
