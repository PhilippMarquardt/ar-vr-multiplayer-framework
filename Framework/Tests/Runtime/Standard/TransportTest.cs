using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using NetLib.Transport;
using NetLib.Transport.LiteNetLib;
using NetLib.Transport.Telepathy;

namespace Standard
{
    [Category("Standard")]
    public class TransportTest
    {
        private const int millisecondsTimeout = 30;
        private ushort port = 30000;


        // Tests for Telepathy TCP implementation ---------------------------------------------------------------------

        [Test]
        public void TcpWrongInputs()
        {
            var server = new TcpServer(8);
            var client = new TcpClient(8);

            TestWrongInputsInternal(server, client, ++port);
        }

        [Test]
        public void TcpConnection()
        {
            var server = new TcpServer(8);
            var client1 = new TcpClient(8);
            var client2 = new TcpClient(8);
            var client3 = new TcpClient(8);

            TestConnectionInternal(server, client1, client2, client3, ++port);
        }

        [Test]
        public void TcpSendReceive()
        {
            var server = new TcpServer(8);
            var client = new TcpClient(8);

            TestSendReceiveInternal(server, client, ++port);
        }

        [Test]
        public void TcpSendReceiveEmpty()
        {
            var server = new TcpServer(8);
            var client = new TcpClient(8);

            TestSendReceiveEmptyInternal(server, client, ++port);
        }

        [Test]
        public void TcpGetClientAddress()
        {
            var server = new TcpServer(8);
            var client = new TcpClient(8);

            TestGetClientAddressInternal(server, client, port);
        }


        // Tests for LiteNetLib UDP implementation --------------------------------------------------------------------

        [Test]
        public void UdpWrongInputs()
        {
            var server = new UdpServer(8);
            var client = new UdpClient();

            TestWrongInputsInternal(server, client, ++port);
        }

        [Test]
        public void UdpConnection()
        {
            var server = new UdpServer(3);
            var client1 = new UdpClient();
            var client2 = new UdpClient();
            var client3 = new UdpClient();

            TestConnectionInternal(server, client1, client2, client3, ++port);
        }

        [Test]
        public void UdpSendReceive()
        {
            var server = new UdpServer(1);
            var client = new UdpClient();

            TestSendReceiveInternal(server, client, ++port);
        }

        [Test]
        public void UdpSendReceiveEmpty()
        {
            var server = new UdpServer(1);
            var client = new UdpClient();

            TestSendReceiveEmptyInternal(server, client, ++port);
        }

        [Test]
        public void UdpGetClientAddress()
        {
            var server = new UdpServer(1);
            var client = new UdpClient();

            TestGetClientAddressInternal(server, client, port);
        }


        // Test exception classes -------------------------------------------------------------------------------------

        [Test]
        public void TestMessageNotSentException()
        {
            bool thrown;

            try
            {
                throw new MessageNotSentException(42, new byte[] { 1, 2, 3 }, "test message");
            }
            catch (MessageNotSentException e)
            {
                thrown = true;
                Assert.AreEqual(42, e.Id);
                Assert.AreEqual(new byte[] { 1, 2, 3 }, e.MessageData);
                Assert.AreEqual("test message", e.Message);
            }

            Assert.IsTrue(thrown);
        }

        [Test]
        public void TestInvalidConnectionIdException()
        {
            bool thrown1;
            bool thrown2;

            try
            {
                throw new InvalidConnectionIdException(42, new byte[] { 1, 2, 3 }, "test message");
            }
            catch (InvalidConnectionIdException e)
            {
                thrown1 = true;
                Assert.AreEqual(42, e.Id);
                Assert.AreEqual(new byte[] { 1, 2, 3 }, e.MessageData);
                Assert.AreEqual("test message", e.Message);
            }

            try
            {
                throw new InvalidConnectionIdException(42, new byte[] { 1, 2, 3 }, "test message");
            }
            catch (MessageNotSentException e)
            {
                thrown2 = true;
                Assert.AreEqual(42, e.Id);
                Assert.AreEqual(new byte[] { 1, 2, 3 }, e.MessageData);
                Assert.AreEqual("test message", e.Message);
            }

            Assert.IsTrue(thrown1);
            Assert.IsTrue(thrown2);
        }

        [Test]
        public void TestInitializationException()
        {
            bool thrown;

            try
            {
                throw new InitializationFailedException(1337, "localhost", "test message");
            }
            catch (InitializationFailedException e)
            {
                thrown = true;
                Assert.AreEqual(1337, e.Port);
                Assert.AreEqual("localhost", e.Ip);
                Assert.AreEqual("test message", e.Message);
            }

            Assert.IsTrue(thrown);
        }

        [Test]
        public void TestFailedToStartServerException()
        {
            bool thrown1;
            bool thrown2;

            try
            {
                throw new FailedToStartServerException(1337, "test message");
            }
            catch (FailedToStartServerException e)
            {
                thrown1 = true;
                Assert.AreEqual(1337, e.Port);
                Assert.AreEqual("", e.Ip);
                Assert.AreEqual("test message", e.Message);
            }

            try
            {
                throw new FailedToStartServerException(1337, "test message");
            }
            catch (InitializationFailedException e)
            {
                thrown2 = true;
                Assert.AreEqual(1337, e.Port);
                Assert.AreEqual("", e.Ip);
                Assert.AreEqual("test message", e.Message);
            }

            Assert.IsTrue(thrown1);
            Assert.IsTrue(thrown2);
        }

        [Test]
        public void TestFailedToStartClientException()
        {
            bool thrown1;
            bool thrown2;

            try
            {
                throw new FailedToStartClientException(1337, "localhost", "test message");
            }
            catch (FailedToStartClientException e)
            {
                thrown1 = true;
                Assert.AreEqual(1337, e.Port);
                Assert.AreEqual("localhost", e.Ip);
                Assert.AreEqual("test message", e.Message);
            }

            try
            {
                throw new FailedToStartClientException(1337, "localhost", "test message");
            }
            catch (InitializationFailedException e)
            {
                thrown2 = true;
                Assert.AreEqual(1337, e.Port);
                Assert.AreEqual("localhost", e.Ip);
                Assert.AreEqual("test message", e.Message);
            }

            Assert.IsTrue(thrown1);
            Assert.IsTrue(thrown2);
        }


        // Generic test methods ---------------------------------------------------------------------------------------

        private static void TestGetClientAddressInternal(IServer server, IClient client, ushort port)
        {
            server.Start(port);
            client.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client.Poll();

            // Test GetClientAddress
            Assert.AreEqual("127.0.0.1", server.GetClientAddressIpV4(1));
            Assert.AreEqual("", server.GetClientAddressIpV4(50));

            client.Disconnect();
            server.Stop();
        }

        private static void TestWrongInputsInternal(IServer server, IClient client, ushort port)
        {
            // Setup
            server.Start(port);
            client.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client.Poll();

            // Test null message on server
            Assert.Throws<ArgumentNullException>(() => server.Send(null, 1));

            // Test null message on client
            Assert.Throws<ArgumentNullException>(() => client.Send(null));

            // Test wrong id on server
            // ReSharper disable once RedundantArgumentDefaultValue
            Assert.Throws<InvalidConnectionIdException>(() => server.Send(Array.Empty<byte>(), 0));

            // Test wrong id on client
            Assert.Throws<InvalidConnectionIdException>(() => client.Send(Array.Empty<byte>(), 1));

            // Cleanup
            client.Disconnect();
            server.Stop();
        }

        private static void TestConnectionInternal(IServer server, IClient client1, IClient client2, IClient client3, ushort port)
        {
            var clientIds = new List<ulong>();
            var serverIds = new List<ulong>();

            // Setup OnConnect delegates
            server.OnConnect += id =>
            {
                clientIds.Add(id);
            };
            client1.OnConnect += id =>
            {
                serverIds.Add(id);
            };
            client2.OnConnect += id =>
            {
                serverIds.Add(id);
            };
            client3.OnConnect += id =>
            {
                serverIds.Add(id);
            };

            // Setup OnDisconnect delegates
            server.OnDisconnect += id =>
            {
                clientIds.Remove(id);
            };
            client1.OnDisconnect += id =>
            {
                serverIds.Remove(id);
            };
            client2.OnDisconnect += id =>
            {
                serverIds.Remove(id);
            };
            client3.OnDisconnect += id =>
            {
                serverIds.Remove(id);
            };

            // Connect in specific order
            server.Start(port);
            client1.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            client2.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            client3.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client1.Poll();
            client2.Poll();
            client3.Poll();

            // Test IsActive flag
            Assert.IsTrue(server.IsActive);
            Assert.IsTrue(client1.IsActive);
            Assert.IsTrue(client2.IsActive);
            Assert.IsTrue(client3.IsActive);

            // Test OnConnect delegates
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2, 3 }, clientIds);
            CollectionAssert.AreEquivalent(new List<int>() { 0, 0, 0 }, serverIds);

            // Disconnect client 2 from server
            client2.Disconnect();
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client1.Poll();
            client2.Poll();
            client3.Poll();

            // Test OnDisconnect delegates
            CollectionAssert.AreEquivalent(new List<int>() { 1, 3 }, clientIds);
            CollectionAssert.AreEquivalent(new List<int>() { 0, 0 }, serverIds);

            // Cleanup
            client1.Disconnect();
            client2.Disconnect();
            client3.Disconnect();
            server.Stop();
        }

        private static void TestSendReceiveInternal(IServer server, IClient client, ushort port)
        {
            ulong serverReceivedId = int.MaxValue;
            var serverReceivedData = Array.Empty<byte>();
            ulong clientReceivedId = int.MaxValue;
            var clientReceivedData = Array.Empty<byte>();

            // Setup OnData delegates
            server.OnData += (id, data) =>
            {
                serverReceivedId = id;
                serverReceivedData = data;
            };
            client.OnData += (id, data) =>
            {
                clientReceivedId = id;
                clientReceivedData = data;
            };

            // Connect
            server.Start(port);
            client.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client.Poll();


            // Test client send
            client.Send(new byte[] { 7, 9, 11 });
            Thread.Sleep(millisecondsTimeout);
            server.Poll();

            Assert.AreEqual(1, serverReceivedId);
            Assert.AreEqual(new byte[] { 7, 9, 11 }, serverReceivedData);

            // Test server send
            server.Send(new byte[] { 2, 3, 4 }, 1);
            Thread.Sleep(millisecondsTimeout);
            client.Poll();

            Assert.AreEqual(0, clientReceivedId);
            Assert.AreEqual(new byte[] { 2, 3, 4 }, clientReceivedData);


            // Cleanup
            client.Disconnect();
            server.Stop();
        }

        private static void TestSendReceiveEmptyInternal(IServer server, IClient client, ushort port)
        {
            ulong serverReceivedId = int.MaxValue;
            var serverReceivedData = new byte[] { 1, 2, 3 };
            ulong clientReceivedId = int.MaxValue;
            var clientReceivedData = new byte[] { 1, 2, 3 };

            // Setup OnData delegates
            server.OnData += (id, data) =>
            {
                serverReceivedId = id;
                serverReceivedData = data;
            };
            client.OnData += (id, data) =>
            {
                clientReceivedId = id;
                clientReceivedData = data;
            };

            // Connect
            server.Start(port);
            client.Connect("127.0.0.1", port);
            Thread.Sleep(millisecondsTimeout);
            server.Poll();
            client.Poll();


            // Test client send empty message
            client.Send(Array.Empty<byte>());
            Thread.Sleep(millisecondsTimeout);
            server.Poll();

            Assert.AreEqual(1, serverReceivedId);
            Assert.AreEqual(Array.Empty<byte>(), serverReceivedData);

            // Test server send empty message
            server.Send(Array.Empty<byte>(), 1);
            Thread.Sleep(millisecondsTimeout);
            client.Poll();

            Assert.AreEqual(0, clientReceivedId);
            Assert.AreEqual(Array.Empty<byte>(), clientReceivedData);


            // Cleanup
            client.Disconnect();
            server.Stop();
        }
    }
}
