using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetLib.Messaging;
using NetLib.Spawning;
using NUnit.Framework;
using TestUtils;
using UnityEngine;
using UnityEngine.TestTools;
using Logger = NetLib.Utils.Logger;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace Standard
{
    [Category("Standard")]
    public class SpawnManagerTest
    {
        private SpawningServer spawningServer;
        private SpawningClient spawningClient;
        private GameObject testPrefab;

        private BypassClient clientTransport;
        private BypassServer serverTransport;
        private List<ulong> clientList;
        private List<ulong> emptyClientList;

        [SetUp]
        public void Setup()
        {
            Logger.Verbosity = Logger.LogLevel.Debug;

            serverTransport = new BypassServer();
            clientTransport = new BypassClient(serverTransport);
            serverTransport.Start(0);
            clientTransport.Connect("", 0);
            serverTransport.Poll();
            clientTransport.Poll();

            clientList = new List<ulong>() { 1 };
            emptyClientList = new List<ulong>();

            var messageSystemClient = new MessageSystem(clientTransport, "");
            spawningClient = new SpawningClient(messageSystemClient);

            var messageSystemServer = new MessageSystem(serverTransport, "");
            spawningServer = new SpawningServer(messageSystemServer);

            testPrefab = new GameObject
            {
                name = "Test Prefab",
                hideFlags = HideFlags.DontSave
            };
            var netObj = testPrefab.AddComponent<NetworkObjectDerived>();
            netObj.PrefabHash = "test-prefab-hash";

            spawningServer.RegisteredPrefabs = new List<GameObject>()
            {
                testPrefab
            };
            spawningClient.RegisteredPrefabs = new List<GameObject>()
            {
                testPrefab
            };
        }

        [TearDown]
        public void TearDown()
        {
            // destroy all objects
            Object.DestroyImmediate(testPrefab);

            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
                if (go.activeInHierarchy)
                    Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TestSpawningBase()
        {
            Assert.AreEqual(
                "test-prefab-hash", 
                spawningServer.GetPrefabFromHash("test-prefab-hash").GetComponent<NetworkObjectBase>().PrefabHash);

            spawningServer.SpawnNetworkObject(testPrefab);

            Assert.AreEqual(
                "test-prefab-hash",
                spawningServer.GetGameObjectFromUuid(1).GetComponent<NetworkObjectBase>().PrefabHash);

            Assert.AreEqual(
                "test-prefab-hash",
                spawningServer.GetNetworkObjectFromUuid(1).PrefabHash);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestOnNetworkStart()
        {
            spawningServer.SpawnNetworkObject(testPrefab);

            var go = new GameObject();
            go.AddComponent<NetworkObjectDerivedWithoutOnNetworkStart>();

            spawningServer.SpawnNetworkObject(go);

            spawningServer.OnNetworkStart();

            Assert.IsTrue(((NetworkObjectDerived)spawningServer.GetNetworkObjectFromUuid(1)).OnNetworkStartCalled);

            yield return null;
        }

        [Test]
        public void TestConstructors()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new SpawningClient(null));

            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new SpawningServer(null));
        }

        [Test]
        public void TestExceptionsSpawn()
        {
            string nullString = null;
            GameObject nullObject = null;
            var emptyObject = new GameObject();

            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => spawningServer.SpawnNetworkObject(nullObject));
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => spawningServer.SpawnNetworkObject(nullString));

            Assert.Throws<ArgumentException>(() => spawningServer.SpawnNetworkObject(emptyObject));
            Assert.Throws<ArgumentException>(() => spawningServer.SpawnNetworkObject(""));


            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => spawningServer.SpawnClientPlayerObject(nullObject, 0));
            Assert.Throws<ArgumentException>(() => spawningServer.SpawnClientPlayerObject(emptyObject, 0));

            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => spawningServer.SpawnServerPlayerObject(testPrefab, nullObject));
            Assert.Throws<ArgumentException>(() => spawningServer.SpawnServerPlayerObject(testPrefab, emptyObject));
            Assert.Throws<ArgumentNullException>(() => spawningServer.SpawnServerPlayerObject(null, testPrefab));
            Assert.Throws<ArgumentException>(() => spawningServer.SpawnServerPlayerObject(emptyObject, testPrefab));
        }

        [Test]
        public void TestExceptionsDeSpawn()
        {
            var emptyObject = new GameObject();

            Assert.Throws<ArgumentNullException>(() => spawningServer.DeSpawnNetworkObject(null));
            Assert.Throws<ArgumentException>(() => spawningServer.DeSpawnNetworkObject(emptyObject));
            Assert.Throws<ArgumentException>(() => spawningServer.DeSpawnNetworkObject(testPrefab));
        }

        [Test]
        public void TestEmptySend()
        {
            spawningServer.SendInitialState(emptyClientList);
            spawningServer.SendStateUpdate(emptyClientList);
        }

        [UnityTest]
        public IEnumerator TestSceneObjectsInitialization()
        {
            var serverSceneObject = new GameObject();
            serverSceneObject.AddComponent<NetworkObjectDerived>();

            spawningServer.OnObjectSpawn = obj => ((NetworkObjectDerived) obj).StaticData = 25;
            serverSceneObject.GetComponent<NetworkObjectDerived>().SpawnData = 40;
            serverSceneObject.GetComponent<NetworkObjectDerived>().DynamicData = 42;

            spawningServer.InitializeScene();
            
            // test custom initialization server
            Assert.AreEqual(25, serverSceneObject.GetComponent<NetworkObjectDerived>().StaticData);

            serverSceneObject.SetActive(false);


            var clientSceneObject = new GameObject();
            clientSceneObject.AddComponent<NetworkObjectDerived>();

            spawningClient.OnObjectSpawn = obj => ((NetworkObjectDerived)obj).StaticData = 255;
            spawningClient.InitializeScene();
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // test custom initialization client
            Assert.AreEqual(255, clientSceneObject.GetComponent<NetworkObjectDerived>().StaticData);

            Assert.AreEqual(40, clientSceneObject.GetComponent<NetworkObjectDerived>().SpawnData);
            Assert.AreEqual(42, clientSceneObject.GetComponent<NetworkObjectDerived>().DynamicData);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSceneObjectsDestroyed()
        {
            var serverSceneObject = new GameObject();
            serverSceneObject.AddComponent<NetworkObjectDerived>();

            spawningServer.InitializeScene();
            spawningServer.DeSpawnNetworkObject(serverSceneObject);

            spawningServer.SendStateUpdate(emptyClientList);

            var clientSceneObject = new GameObject();
            clientSceneObject.AddComponent<NetworkObjectDerived>();
            Assert.IsTrue(clientSceneObject.activeInHierarchy);

            spawningClient.InitializeScene();
            Assert.IsFalse(clientSceneObject.activeInHierarchy);

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // test custom initialization client
            Assert.IsFalse(clientSceneObject.activeInHierarchy);

            yield return null;
        }

        [UnityTest]
        // Tests normal initialization of a new client
        public IEnumerator TestInitialization()
        {
            var obj = Object.Instantiate(testPrefab);
            spawningServer.SpawnNetworkObject(obj);

            spawningServer.SendStateUpdate(emptyClientList);

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            AssertObjectsInScene();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestUpdateBeforeInitialState()
        {
            LogAssert.Expect(LogType.Log, new Regex(".*"));

            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestMultipleInitialState()
        {
            LogAssert.Expect(LogType.Log, new Regex(".*"));

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();
            yield return null;
        }

        [UnityTest]
        // Tests edge case when the scene is empty during initialization
        public IEnumerator TestInitializationEmptyScene()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            AssertSceneEmpty();

            yield return null;
        }

        [UnityTest]
        // Tests spawning and despawning an object
        public IEnumerator TestSpawnDespawn()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // spawning an object
            var obj = Object.Instantiate(testPrefab);
            spawningServer.SpawnNetworkObject(obj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            AssertObjectsInScene();

            // despawning an object
            spawningServer.DeSpawnNetworkObject(obj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            // wait a frame for unity to destroy objects
            yield return null;

            AssertSceneEmpty();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSpawnFromString()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            spawningServer.SpawnNetworkObject("test-prefab-hash");

            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            AssertObjectsInScene();

            yield return null;
        }

        [UnityTest]
        // Tests updating an object's data
        public IEnumerator TestSynchronization()
        {
            var obj = Object.Instantiate(testPrefab);
            obj.name = "Server Object";
            obj.GetComponent<NetworkObjectDerived>().SpawnData = 255;
            spawningServer.SpawnNetworkObject(obj);

            spawningServer.SendStateUpdate(emptyClientList);

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            var objects = AssertObjectsInScene();

            // check client state before update
            var clientObject = objects.ToList().First(x => x.gameObject.name != "Server Object");
            Assert.AreEqual(0, clientObject.GetComponent<NetworkObjectDerived>().DynamicData);
            Assert.AreEqual(255, clientObject.GetComponent<NetworkObjectDerived>().SpawnData);

            // change data
            obj.GetComponent<NetworkObjectDerived>().DynamicData = 42;
            obj.GetComponent<NetworkObjectDerived>().MarkDirty();

            // update client
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            // check dynamic data has changed
            Assert.AreEqual(42, clientObject.GetComponent<NetworkObjectDerived>().DynamicData);
            // check spawn data has not changed
            Assert.AreEqual(255, clientObject.GetComponent<NetworkObjectDerived>().SpawnData);

            yield return null;
        }

        [UnityTest]
        // Tests edge case when sending the initial state while a spawn is queued
        public IEnumerator TestUpdateSpawnInitial()
        {
            var obj = Object.Instantiate(testPrefab);
            spawningServer.SpawnNetworkObject(obj);

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            AssertObjectsInScene();

            yield return null;
        }

        [UnityTest]
        // Tests edge case when sending the initial state while a destroy is queued
        public IEnumerator TestUpdateDestroyInitial()
        {
            var obj = Object.Instantiate(testPrefab);
            spawningServer.SpawnNetworkObject(obj);

            spawningServer.SendStateUpdate(emptyClientList);

            spawningServer.DeSpawnNetworkObject(obj);

            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            // wait a frame for unity to destroy objects
            yield return null;

            AssertSceneEmpty();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSpawnDespawnPrefabChildren()
        {
            // setup
            var testPrefabParent = new GameObject
            {
                name = "Test Prefab",
                hideFlags = HideFlags.DontSave
            };
            var netObj = testPrefabParent.AddComponent<NetworkObjectDerived>();
            netObj.PrefabHash = "test-prefab-hash";

            var testPrefabChild = new GameObject
            {
                name = "Test Prefab",
                hideFlags = HideFlags.DontSave
            };
            testPrefabChild.AddComponent<NetworkObjectDerived>();
            testPrefabChild.transform.parent = testPrefabParent.transform;

            spawningServer.RegisteredPrefabs = new List<GameObject>()
            {
                testPrefabParent
            };
            spawningClient.RegisteredPrefabs = new List<GameObject>()
            {
                testPrefabParent
            };

            // test spawn
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            var obj = Object.Instantiate(testPrefabParent);
            spawningServer.SpawnNetworkObject(obj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            var objects = Object.FindObjectsOfType<NetworkObjectBase>();

            Assert.AreEqual(4, objects.Length);

            // test despawn
            spawningServer.DeSpawnNetworkObject(obj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            // wait a frame for unity to destroy objects
            yield return null;

            objects = Object.FindObjectsOfType<NetworkObjectBase>();
            Assert.AreEqual(0, objects.Length);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientPlayerObjectInScene()
        {
            var playerObject = new GameObject();
            playerObject.AddComponent<NetworkObjectDerived>().isPlayer = true;
            var playerObjectChild = new GameObject();
            playerObjectChild.AddComponent<NetworkObjectDerived>();
            playerObjectChild.transform.parent = playerObject.transform;

            spawningClient.InitializeScene();

            // test that player object was ignored
            Assert.AreEqual(0, playerObject.GetComponent<NetworkObjectDerived>().Uuid);
            Assert.AreEqual(0, playerObjectChild.GetComponent<NetworkObjectDerived>().Uuid);

            spawningServer.InitializeScene();

            // test that player object was ignored
            Assert.AreEqual(0, playerObject.GetComponent<NetworkObjectDerived>().Uuid);
            Assert.AreEqual(0, playerObjectChild.GetComponent<NetworkObjectDerived>().Uuid);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientPlayerObjectAddDespawn()
        {
            spawningServer.InitializeScene();

            var playerObject = new GameObject();
            playerObject.AddComponent<NetworkObjectDerived>().isPlayer = true;
            spawningClient.Player = playerObject.GetComponent<NetworkObjectBase>();

            spawningClient.InitializeScene();

            var obj = Object.Instantiate(testPrefab);
            obj.GetComponent<NetworkObjectDerived>().DynamicData = 42;
            spawningServer.SpawnClientPlayerObject(obj, 1);

            spawningServer.SendStateUpdate(emptyClientList);
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            var objects = Object.FindObjectsOfType<NetworkObjectBase>();
            Assert.AreEqual(2, objects.Length);

            var prefab = objects.First(x => !x.IsPlayer);

            Assert.AreEqual(playerObject.GetComponent<NetworkObjectDerived>().Uuid, prefab.GetComponent<NetworkObjectDerived>().Uuid);
            // player gets data from server once
            Assert.AreEqual(42, playerObject.GetComponent<NetworkObjectDerived>().DynamicData);

            // test that server does not override player state
            prefab.GetComponent<NetworkObjectDerived>().DynamicData = 255;
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            Assert.AreEqual(42, playerObject.GetComponent<NetworkObjectDerived>().DynamicData);

            // despawn non existent player - no error
            spawningServer.DeSpawnClientPlayerObject(100);

            // despawn existing player
            spawningServer.DeSpawnClientPlayerObject(1);
            
            objects = Object.FindObjectsOfType<NetworkObjectBase>();
            // local player object is still there, but prefab should be gone
            Assert.AreEqual(1, objects.Length); 
            Assert.IsTrue(objects[0].IsPlayer);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestSpawnServerPlayerObject()
        {
            spawningClient.InitializeScene();
            
            var playerObject = new GameObject();
            playerObject.AddComponent<NetworkObjectDerived>().isPlayer = true;
            playerObject.GetComponent<NetworkObjectDerived>().DynamicData = 42;

            spawningServer.InitializeScene();

            spawningServer.SpawnServerPlayerObject(testPrefab, playerObject);

            spawningServer.SendInitialState(clientList);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();
            clientTransport.Poll();

            var objects = Object.FindObjectsOfType<NetworkObjectBase>();
            Assert.AreEqual(2, objects.Length);
            var prefab = objects.First(x => !x.IsPlayer);

            Assert.AreEqual(playerObject.GetComponent<NetworkObjectDerived>().Uuid, prefab.GetComponent<NetworkObjectDerived>().Uuid);
            Assert.AreEqual(42, prefab.GetComponent<NetworkObjectDerived>().DynamicData);

            yield return null;
        }


        [UnityTest]
        public IEnumerator TestClientErrorStateForNoObject()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnNetworkObject(serverObj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            AssertObjectsInScene();

            var clientObj = Object.FindObjectsOfType<NetworkObjectBase>().First(x => x.gameObject.name != "server");

            // destroy object on client
            Object.Destroy(clientObj.gameObject);
            yield return null;
            yield return null;

            // send update
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            serverObj.GetComponent<NetworkObjectDerived>().MarkDirty();
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientErrorSpawnForObjectAlreadyInUse()
        {
            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnNetworkObject(serverObj);
            spawningServer.SendStateUpdate(emptyClientList);
            
            // client has scene object but server not
            spawningClient.InitializeScene();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            yield return null;
        }
        [UnityTest]
        public IEnumerator TestClientErrorSpawnPlayerForObjectAlreadyInUse()
        {
            spawningServer.InitializeScene();

            // setup player
            var playerObject = new GameObject();
            playerObject.AddComponent<NetworkObjectDerived>().isPlayer = true;
            spawningClient.Player = playerObject.GetComponent<NetworkObjectBase>();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnClientPlayerObject(serverObj, 1);
            spawningServer.SendStateUpdate(emptyClientList);

            // client has scene object but server not
            spawningClient.InitializeScene();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientErrorDespawnForNoObject()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnNetworkObject(serverObj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            var clientObj = Object.FindObjectsOfType<NetworkObjectBase>().First(x => x.gameObject.name != "server");

            // destroy object on client
            Object.DestroyImmediate(clientObj.gameObject);

            // despawn object
            spawningServer.DeSpawnNetworkObject(serverObj);

            // send update
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientErrorDespawnForInactiveObject()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnNetworkObject(serverObj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            var clientObj = Object.FindObjectsOfType<NetworkObjectBase>().First(x => x.gameObject.name != "server");

            // destroy object on client
            clientObj.gameObject.SetActive(false);

            // despawn object
            spawningServer.DeSpawnNetworkObject(serverObj);

            // send update
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientWarningSpawnParentNotFound()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            spawningServer.SpawnNetworkObject(serverObj);
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            var clientObj = Object.FindObjectsOfType<NetworkObjectBase>().First(x => x.gameObject.name != "server");

            // destroy object on client
            Object.DestroyImmediate(clientObj.gameObject);


            // spawn child
            var serverChild = Object.Instantiate(testPrefab, serverObj.transform, true);
            serverChild.name = "serverChild";
            spawningServer.SpawnNetworkObject(serverChild);


            // send update
            LogAssert.Expect(LogType.Warning, new Regex(".*"));
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientErrorStateForNoSceneObject()
        {
            var serverSceneObject = new GameObject();
            serverSceneObject.AddComponent<NetworkObjectDerived>();

            spawningServer.InitializeScene();

            serverSceneObject.SetActive(false);

            spawningClient.InitializeScene();

            // server has scene object but client not
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientErrorPrefabNotFound()
        {
            spawningServer.SendInitialState(clientList);
            clientTransport.Poll();

            // client does not have prefab
            spawningClient.RegisteredPrefabs = new List<GameObject>();

            // spawning an object
            var serverObj = Object.Instantiate(testPrefab);
            spawningServer.SpawnNetworkObject(serverObj);

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            spawningServer.SendStateUpdate(clientList);
            clientTransport.Poll();

            yield return null;
        }

        // ------------------------------------------------------------------------------------------------------------

        // Asserts that 2 copies of the test prefab are in the scene.
        private static NetworkObjectBase[] AssertObjectsInScene()
        {
            var objects = Object.FindObjectsOfType<NetworkObjectBase>();

            // only 2 objects are in the scene
            Assert.AreEqual(2, objects.Length);

            // the 2 objects are copies of the test prefab
            Assert.AreEqual("test-prefab-hash", objects[0].PrefabHash);
            Assert.AreEqual("test-prefab-hash", objects[1].PrefabHash);

            // the 2 objects are mapped together
            Assert.AreEqual(objects[0].Uuid, objects[1].Uuid);

            // the 2 objects are distinct
            Assert.AreNotEqual(objects[0].gameObject, objects[1].gameObject);

            return objects;
        }

        // Asserts that no spawned objects are in the scene.
        private static void AssertSceneEmpty()
        {
            var objects = Object.FindObjectsOfType<NetworkObjectBase>();
            Assert.AreEqual(0, objects.Length);
        }


        private class NetworkObjectDerived : NetworkObjectBase
        {
            // ReSharper disable once MemberCanBePrivate.Local
#pragma warning disable 649
            internal int sceneOrderIndex;
#pragma warning restore 649
            internal bool isPlayer;
            private bool isDirty;
            public byte StaticData;
            public byte SpawnData;
            public byte DynamicData;
            public bool OnNetworkStartCalled;
            [SerializeField]
            private string hash;

            public override int SceneOrderIndex => sceneOrderIndex;

            public override string PrefabHash
            {
                get => hash; 
                set => hash = value;
            }

            public override bool IsPlayer => isPlayer;

            
            public override bool IsDirty() => isDirty;

            public override void MarkDirty() => isDirty = true;

            public override void ResetDirty() => isDirty = false;

            public override byte[] Serialize() => new[] {DynamicData};

            public override void Deserialize(byte[] data) => DynamicData = data[0];

            public override byte[] SerializeOnSpawn() => new[] { SpawnData };

            public override void DeserializeOnSpawn(byte[] data) => SpawnData = data[0];

            protected override void OnNetworkStart()
            {
                OnNetworkStartCalled = true;
            }
        }

        private class NetworkObjectDerivedWithoutOnNetworkStart : NetworkObjectBase
        {
            public override int SceneOrderIndex => 0;
            public override string PrefabHash { get; set; }
            public override bool IsPlayer => false;
            public override void Deserialize(byte[] data) { }
            public override void DeserializeOnSpawn(byte[] data) { }
            public override bool IsDirty() => false;
            public override void MarkDirty() { }
            public override void ResetDirty() { }
            public override byte[] Serialize() => new byte[0];
            public override byte[] SerializeOnSpawn() => new byte[0];
        }
    }
}
