using System;
using System.Collections;
using System.IO;
using NetLib.Messaging;
using NetLib.Script;
using NetLib.Script.Rpc;
using NetLib.Serialization;
using NetLib.Transport;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
// ReSharper disable MemberCanBePrivate.Local

namespace Standard
{
    [Category("Standard")]
    public class RpcTest
    {
        [Serializable]
        private class TestType
        {
            internal int A;
        }

        [Serializable]
        private class TestTypeSerializable : IBinarySerializable
        {
            internal int A;

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(A);
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);
                A = reader.ReadInt32();
            }
        }

        private class TestBehaviour : NetworkBehaviour
        {
            internal int TestResult;
            internal string TestResultString;
            internal static string TestResultStringStatic;
            internal TestType NullResultA = new TestType();
            internal TestTypeSerializable NullResultB = new TestTypeSerializable();

            public void TestRpcOverloading()
            {
                InvokeServerRpc(ServerRpcOverload);
                Assert.AreEqual(-1, TestResult);
                InvokeServerRpc(ServerRpcOverload, 1);
                Assert.AreEqual(1, TestResult);
                InvokeServerRpc(ServerRpcOverload, 4, 2);
                Assert.AreEqual(6, TestResult);
                InvokeServerRpc(ServerRpcOverload, 5, 1, -8);
                Assert.AreEqual(-2, TestResult);
                InvokeServerRpc(ServerRpcOverload, 1, 2, 3, 4);
                Assert.AreEqual(10, TestResult);
                InvokeServerRpc(ServerRpcOverload, 7, 1, 4, 2, -3);
                Assert.AreEqual(11, TestResult);
            }

            public void TestRpcPrimitives()
            {
                InvokeServerRpc(ServerRpcPrimitives, 1, 0.4f, "test", new ulong[]{2, 5, 1});
            }

            public void TestRpcCustomType()
            {
                InvokeServerRpc(ServerRpcCustomTypes, new TestType(){A = 42});
                Assert.AreEqual(42, TestResult);
            }

            public void TestRpcCustomTypeSerializable()
            {
                InvokeServerRpc(ServerRpcCustomTypesSerializable, new TestTypeSerializable() { A = 42 });
                Assert.AreEqual(42, TestResult);
            }

            public void TestRpcCustomTypeNull()
            {
                InvokeServerRpc<TestType>(ServerRpcCustomTypes, null);
                Assert.IsNull(NullResultA);
            }

            public void TestRpcCustomTypeSerializableNull()
            {
                InvokeServerRpc<TestTypeSerializable>(ServerRpcCustomTypesSerializable, null);
                Assert.IsNull(NullResultB);
            }

            public void TestModifiersRpc()
            {
                InvokeServerRpc(ServerRpcPrivate);
                Assert.AreEqual("private", TestResultString);
                InvokeServerRpc(ServerRpcPrivateStatic);
                Assert.AreEqual("private static", TestResultStringStatic);
                InvokeServerRpc(ServerRpcPublicStatic);
                Assert.AreEqual("public static", TestResultStringStatic);
                InvokeServerRpc(ServerRpcProtected);
                Assert.AreEqual("protected", TestResultString);
                InvokeServerRpc(ServerRpcProtectedStatic);
                Assert.AreEqual("protected static", TestResultStringStatic);
                InvokeServerRpc(ServerRpcInternal);
                Assert.AreEqual("internal", TestResultString);
                InvokeServerRpc(ServerRpcInternalStatic);
                Assert.AreEqual("internal static", TestResultStringStatic);
            }

            [ServerRpc]
            public void ServerRpcPrimitives(int a, float b, string c, ulong[] d)
            {
                Assert.AreEqual(1, a);
                Assert.AreEqual(0.4f, b);
                Assert.AreEqual("test", c);
                Assert.AreEqual(new byte[]{2, 5, 1}, d);
            }

            [ServerRpc]
            public void ServerRpcCustomTypes(TestType to)
            {
                if (to == null)
                    NullResultA = null;
                else
                   TestResult = to.A;
            }
            [ServerRpc]
            public void ServerRpcCustomTypesSerializable(TestTypeSerializable to)
            {
                if (to == null)
                    NullResultB = null;
                else
                    TestResult = to.A;
            }
            [ServerRpc]
            public void ServerRpcOverload()
            {
                TestResult = -1;
            }
            [ServerRpc]
            public void ServerRpcOverload(int a)
            {
                TestResult = a;
            }
            [ServerRpc]
            public void ServerRpcOverload(int a, int b)
            {
                TestResult = a + b;
            }
            [ServerRpc]
            public void ServerRpcOverload(int a, int b, int c)
            {
                TestResult = a + b + c;
            }
            [ServerRpc]
            public void ServerRpcOverload(int a, int b, int c, int d)
            {
                TestResult = a + b + c + d;
            }
            [ServerRpc]
            public void ServerRpcOverload(int a, int b, int c, int d, int e)
            {
                TestResult = a + b + c + d + e;
            }
            [ServerRpc]
            private void ServerRpcPrivate()
            {
                TestResultString = "private";
            }
            [ServerRpc]
            private static void ServerRpcPrivateStatic()
            {
                TestResultStringStatic = "private static";
            }
            [ServerRpc]
            public static void ServerRpcPublicStatic()
            {
                TestResultStringStatic = "public static";
            }
            [ServerRpc]
            protected void ServerRpcProtected()
            {
                TestResultString = "protected";
            }
            [ServerRpc]
            protected static void ServerRpcProtectedStatic()
            {
                TestResultStringStatic = "protected static";
            }
            [ServerRpc]
            internal void ServerRpcInternal()
            {
                TestResultString = "internal";
            }
            [ServerRpc]
            internal static void ServerRpcInternalStatic()
            {
                TestResultStringStatic = "internal static";
            }
        }

        private GameObject go;
        private NetworkManager nm;
        private NetworkObject no;
        private TestBehaviour bo;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            nm = go.AddComponent<NetworkManager>();
            no = go.AddComponent<NetworkObject>();
            bo = go.AddComponent<TestBehaviour>();

            var ms = new MessageSystem(new CustomTransport(), "");
            no.Initialize(nm, ms, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TestOverloading()
        {
            bo.TestRpcOverloading();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestPrimitiveParameters()
        {
            bo.TestRpcPrimitives();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestCustomParameters()
        {
            bo.TestRpcCustomType();
            bo.TestRpcCustomTypeSerializable();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestNullParameters()
        {
            bo.TestRpcCustomTypeNull();
            bo.TestRpcCustomTypeSerializableNull();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestMultipleBehaviours()
        {
            var bo2 = go.AddComponent<TestBehaviour>();
            no.Initialize(nm, new MessageSystem(new CustomTransport(), ""), 0);

            bo2.TestRpcOverloading();
            Assert.AreEqual(0, bo.TestResult);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestModifiersRpc()
        {
            bo.TestModifiersRpc();
            yield return null;
        }

        // custom transport to bypass network stuff
        private class CustomTransport : ITransport
        {
            public OnConnect OnConnect { get; set; }
            public OnData OnData { get; set; }
            public OnDisconnect OnDisconnect { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public bool IsActive { get; set; }

            public void Poll() { }

            public void Send(byte[] message, ulong id = 0)
            {
                OnData.Invoke(id, message);
            }
        }
    }
}
