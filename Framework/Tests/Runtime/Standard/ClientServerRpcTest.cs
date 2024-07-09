using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetLib.Script;
using NetLib.Script.Rpc;
using NUnit.Framework;
using TestUtils;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Standard
{
    [Category("Standard")]
    public class ClientServerRpcTest
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
            var transportClient1 = new BypassClient(transportServer);
            var transportClient2 = new BypassClient(transportServer);

            networkManagerObject = new GameObject()
            {
                hideFlags = HideFlags.DontSave
            };
            server = networkManagerObject.AddComponent<NetworkManager>();
            client = networkManagerObject.AddComponent<NetworkManager>();
            client2 = networkManagerObject.AddComponent<NetworkManager>();

            server.Transport = transportServer;
            client.Transport = transportClient1;
            client2.Transport = transportClient2;

            // setup prefab
            testPrefab = new GameObject
            {
                name = "Test Prefab",
                hideFlags = HideFlags.DontSave
            };
            var netObj = testPrefab.AddComponent<NetworkObject>();
            netObj.prefabHash = "test-prefab-hash";
            testPrefab.AddComponent<TestBehaviour>();

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
        public IEnumerator TestServerClientRpc()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();
            var clientBehaviour1 = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();
            clientBehaviour1.gameObject.name = "client1";
            serverBehaviour.gameObject.SetActive(false);
            clientBehaviour1.gameObject.SetActive(false);

            client2.StartClient();
            yield return new WaitForFrames(10);
            objects = Object.FindObjectsOfType<NetworkObject>();
            var clientBehaviour2 = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();


            clientBehaviour1.TestServerClientRpc_Server();
            yield return new WaitForFrames(10);
            serverBehaviour.AssertServerClientRpc();

            serverBehaviour.TestServerClientRpc_Client();
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertServerClientRpc();

            serverBehaviour.TestServerClientRpc_ClientAll();
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertServerClientRpc();
            clientBehaviour2.AssertServerClientRpc();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestServerClientRpcOnAll()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();
            var clientBehaviour1 = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();
            clientBehaviour1.gameObject.name = "client1";
            serverBehaviour.gameObject.SetActive(false);
            clientBehaviour1.gameObject.SetActive(false);

            client2.StartClient();
            yield return new WaitForFrames(10);
            objects = Object.FindObjectsOfType<NetworkObject>();
            var clientBehaviour2 = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();

            serverBehaviour.TestClientRpcOnAll(0);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(0);
            clientBehaviour2.AssertClientRpc(0);

            serverBehaviour.TestClientRpcOnAll(1);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(1);
            clientBehaviour2.AssertClientRpc(1);

            serverBehaviour.TestClientRpcOnAll(2);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(2);
            clientBehaviour2.AssertClientRpc(2);

            serverBehaviour.TestClientRpcOnAll(3);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(3);
            clientBehaviour2.AssertClientRpc(3);

            serverBehaviour.TestClientRpcOnAll(4);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(4);
            clientBehaviour2.AssertClientRpc(4);

            serverBehaviour.TestClientRpcOnAll(5);
            yield return new WaitForFrames(10);
            clientBehaviour1.AssertClientRpc(5);
            clientBehaviour2.AssertClientRpc(5);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientRpc()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();
            var clientBehaviour = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();

            serverBehaviour.TestClientRpc(0);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(0);

            serverBehaviour.TestClientRpc(1);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(1);

            serverBehaviour.TestClientRpc(2);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(2);

            serverBehaviour.TestClientRpc(3);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(3);

            serverBehaviour.TestClientRpc(4);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(4);

            serverBehaviour.TestClientRpc(5);
            yield return new WaitForFrames(10);
            clientBehaviour.AssertClientRpc(5);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientRpcFromClient()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var clientBehaviour = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            clientBehaviour.TestClientFromClient();
        }

        [UnityTest]
        public IEnumerator TestClientRpcOnAllFromClient()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var clientBehaviour = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            clientBehaviour.TestClientOnAllFromClient();
        }

        [UnityTest]
        public IEnumerator TestServerRpcFromServer()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            serverBehaviour.TestServerFromServer();
        }

        [UnityTest]
        public IEnumerator TestWrongRpcServer()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var clientBehaviour = objects.First(o => o.gameObject.name != "server").GetComponent<TestBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            clientBehaviour.TestWrongServer();
        }

        [UnityTest]
        public IEnumerator TestWrongRpcClient()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();
            
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            serverBehaviour.TestWrongClient();
        }

        [UnityTest]
        public IEnumerator TestWrongRpcClientOnAll()
        {
            server.StartServer();
            client.StartClient();
            yield return new WaitForFrames(10);

            var serverObj = Object.Instantiate(testPrefab);
            serverObj.name = "server";
            server.SpawnLocalObject(serverObj);

            yield return new WaitForFrames(10);

            var objects = Object.FindObjectsOfType<NetworkObject>();
            var serverBehaviour = objects.First(o => o.gameObject.name == "server").GetComponent<TestBehaviour>();

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            serverBehaviour.TestWrongClientOnAll();
        }

        private class TestBehaviour : NetworkBehaviour
        {
            private int r0;
            private int r1;
            private int r2;
            private int r3;
            private int r4;
            private int r5;

            public void TestServerFromServer()
            {
                InvokeServerRpc(ServerClientRpc);
            }
            public void TestClientFromClient()
            {
                InvokeClientRpc(1, ClientRpc);
            }
            public void TestClientOnAllFromClient()
            {
                InvokeClientRpcOnAll(ClientRpc);
            }

            public void TestWrongServer()
            {
                InvokeServerRpc(ClientRpc);
            }
            public void TestWrongClient()
            {
                InvokeClientRpc(1, ServerRpc);
            }
            public void TestWrongClientOnAll()
            {
                InvokeClientRpcOnAll(ServerRpc);
            }
            [ServerRpc]
            private void ServerRpc()
            {
                r0 = 42;
            }

            //------------------------------------------------------------------------------------------
            public void TestServerClientRpc_Server()
            {
                InvokeServerRpc(ServerClientRpc);
                r0 = 0;
            }
            public void TestServerClientRpc_Client()
            {
                InvokeClientRpc(1, ServerClientRpc);
                r0 = 0;
            }
            public void TestServerClientRpc_ClientAll()
            {
                InvokeClientRpcOnAll(ServerClientRpc);
                r0 = 0;
            }
            public void AssertServerClientRpc()
            {
                Assert.AreEqual(42, r0);
                r0 = 0;
            }
            [ServerRpc]
            [ClientRpc]
            private void ServerClientRpc()
            {
                r0 = 42;
            }
            
            // -------------------------------------------------------------------------------------------
            public void TestClientRpc(int variant)
            {
                switch (variant)
                {
                    case 0: InvokeClientRpc(1, ClientRpc); break;
                    case 1: InvokeClientRpc(1, ClientRpc, 1); break;
                    case 2: InvokeClientRpc(1, ClientRpc, 1, 2); break;
                    case 3: InvokeClientRpc(1, ClientRpc, 1, 2, 3); break;
                    case 4: InvokeClientRpc(1, ClientRpc, 1, 2, 3, 4); break;
                    case 5: InvokeClientRpc(1, ClientRpc, 1, 2, 3, 4, 5); break;
                }
            }
            public void TestClientRpcOnAll(int variant)
            {
                switch (variant)
                {
                    case 0: InvokeClientRpcOnAll(ClientRpc); break;
                    case 1: InvokeClientRpcOnAll(ClientRpc, 1); break;
                    case 2: InvokeClientRpcOnAll(ClientRpc, 1, 2); break;
                    case 3: InvokeClientRpcOnAll(ClientRpc, 1, 2, 3); break;
                    case 4: InvokeClientRpcOnAll(ClientRpc, 1, 2, 3, 4); break;
                    case 5: InvokeClientRpcOnAll(ClientRpc, 1, 2, 3, 4, 5); break;
                }
            }
            public void AssertClientRpc(int variant)
            {
                switch (variant)
                {
                    case 0: 
                        Assert.AreEqual(25, r0); r0 = 0; 
                        break;
                    case 1:
                        Assert.AreEqual(1, r1); r1 = 0;
                        break;
                    case 2:
                        Assert.AreEqual(1, r1); r1 = 0;
                        Assert.AreEqual(2, r2); r2 = 0;
                        break;
                    case 3:
                        Assert.AreEqual(1, r1); r1 = 0;
                        Assert.AreEqual(2, r2); r2 = 0;
                        Assert.AreEqual(3, r3); r3 = 0;
                        break;
                    case 4:
                        Assert.AreEqual(1, r1); r1 = 0;
                        Assert.AreEqual(2, r2); r2 = 0;
                        Assert.AreEqual(3, r3); r3 = 0;
                        Assert.AreEqual(4, r4); r4 = 0;
                        break;
                    case 5:
                        Assert.AreEqual(1, r1); r1 = 0;
                        Assert.AreEqual(2, r2); r2 = 0;
                        Assert.AreEqual(3, r3); r3 = 0;
                        Assert.AreEqual(4, r4); r4 = 0;
                        Assert.AreEqual(5, r5); r5 = 0;
                        break;
                }
            }
            [ClientRpc]
            private void ClientRpc()
            {
                r0 = 25;
            }
            [ClientRpc]
            private void ClientRpc(int i1, int i2, int i3, int i4, int i5)
            {
                r1 = i1; r2 = i2; r3 = i3; r4 = i4; r5 = i5;
            }
            [ClientRpc]
            private void ClientRpc(int i1, int i2, int i3, int i4)
            {
                r1 = i1; r2 = i2; r3 = i3; r4 = i4;
            }
            [ClientRpc]
            private void ClientRpc(int i1, int i2, int i3)
            {
                r1 = i1; r2 = i2; r3 = i3;
            }
            [ClientRpc]
            private void ClientRpc(int i1, int i2)
            {
                r1 = i1; r2 = i2;
            }
            [ClientRpc]
            private void ClientRpc(int i1)
            {
                r1 = i1;
            }
        }
    }
}
