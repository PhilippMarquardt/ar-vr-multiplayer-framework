using System;
using NUnit.Framework;
using NetLib.Messaging;
using TestUtils;

namespace Standard
{
    [Category("Standard")]
    public class MessageTest
    {
        private BypassServer server;
        private BypassClient client1;
        private BypassClient client2;

        private MessageSystem msServer;
        private MessageSystem msClient1;
        private MessageSystem msClient2;

        [SetUp]
        public void Setup()
        {
            server = new BypassServer();
            client1 = new BypassClient(server);
            client1.Connect("", 0);
            client2 = new BypassClient(server);
            client2.Connect("", 0);

            server.Poll();
            client1.Poll();
            client2.Poll();

            msServer = new MessageSystem(server, "");
            msClient1 = new MessageSystem(client1, "");
            msClient2 = new MessageSystem(client2, "");
        }

        /// <summary>
        /// Test case: single receiver, single listener, same type, no target object
        /// </summary>
        [Test]
        public void TestSameMessageType()
        {
            const byte msgType = 100;
            byte[] receivedMessage = null;

            msServer.AddListener(msgType, (sender, data) =>
            {
                receivedMessage = data;
                Assert.AreEqual(1, sender);
            });

            msClient1.Send(0, msgType, new byte[]{5, 6, 7, 8, 9});

            server.Poll();

            Assert.AreEqual(new byte[]{5, 6, 7, 8, 9}, receivedMessage);
        }

        /// <summary>
        /// Test case: single receiver, single listener, different message types, no target object
        /// </summary>
        [Test]
        public void TestDifferentMessageTypes()
        {
            const byte msgType1 = 100;
            const byte msgType2 = 101;
            byte[] receivedMessage = null;

            msServer.AddListener(msgType1, (sender, data) =>
            {
                receivedMessage = data;
                Assert.AreEqual(1, sender);
            });

            msClient1.Send(0, msgType2, new byte[] { 5, 6, 7, 8, 9 });

            server.Poll();

            Assert.IsNull(receivedMessage);
        }

        /// <summary>
        /// Test case: single receiver, single listener, same message type, same target object
        /// </summary>
        [Test]
        public void TestSameTargetObject()
        {
            const byte msgType = 100;
            const ulong target = 42;
            byte[] receivedMessage1 = null;

            msServer.AddListener(msgType, target, (sender, data) =>
            {
                receivedMessage1 = data;
                Assert.AreEqual(1, sender);
            });

            msClient1.Send(0, msgType, target, new byte[] { 5, 6, 7, 8, 9 });

            server.Poll();

            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage1);

            
        }

        /// <summary>
        /// Test case: single receiver, single listener, same message type, same target object
        /// </summary>
        [Test]
        public void TestDifferentTargetObjects()
        {
            const byte msgType = 100;
            const ulong target1 = 42;
            const ulong target2 = 25;
            byte[] receivedMessage = null;

            msServer.AddListener(msgType, target2, (sender, data) =>
            {
                receivedMessage = data;
                Assert.AreEqual(1, sender);
            });

            msClient1.Send(0, msgType, target1, new byte[] { 5, 6, 7, 8, 9 });

            server.Poll();

            Assert.IsNull(receivedMessage);
        }

        /// <summary>
        /// Test case: multiple receivers, single listener, same message type, no target object
        /// </summary>
        [Test]
        public void TestMultipleReceiversSameType()
        {
            const byte msgType = 100;
            byte[] receivedMessage1 = null;
            byte[] receivedMessage2 = null;

            msClient1.AddListener(msgType, (sender, data) =>
            {
                receivedMessage1 = data;
                Assert.AreEqual(0, sender);
            });
            msClient2.AddListener(msgType, (sender, data) =>
            {
                receivedMessage2 = data;
                Assert.AreEqual(0, sender);
            });

            msServer.Send(1, msgType, new byte[] { 5, 6, 7, 8, 9 });
            msServer.Send(2, msgType, new byte[] { 5, 6, 7, 8, 9 });

            client1.Poll();
            client2.Poll();

            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage1);
            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage2);
        }

        /// <summary>
        /// Test case: multiple receivers, single listener, different message types, no target object
        /// </summary>
        [Test]
        public void TestMultipleReceiversDifferentTypes()
        {
            const byte msgType1 = 100;
            const byte msgType2 = 101;
            byte[] receivedMessage1 = null;
            byte[] receivedMessage2 = null;

            msClient1.AddListener(msgType1, (sender, data) =>
            {
                receivedMessage1 = data;
                Assert.AreEqual(0, sender);
            });
            msClient2.AddListener(msgType2, (sender, data) =>
            {
                receivedMessage2 = data;
                Assert.AreEqual(0, sender);
            });

            msServer.Send(1, msgType1, new byte[] { 5, 6, 7, 8, 9 });
            msServer.Send(2, msgType2, new byte[] { 1, 2, 3, 2, 1 });

            client1.Poll();
            client2.Poll();

            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage1);
            Assert.AreEqual(new byte[] { 1, 2, 3, 2, 1 }, receivedMessage2);
        }

        /// <summary>
        /// Test case: single receiver, multiple listeners, same message type, no target object
        /// </summary>
        [Test]
        public void TestMultipleListenersOnSameReceiver()
        {
            const byte msgType = 100;
            byte[] receivedMessage1 = null;
            byte[] receivedMessage2 = null;

            msServer.AddListener(msgType, (sender, data) =>
            {
                receivedMessage1 = data;
                Assert.AreEqual(1, sender);
            });
            msServer.AddListener(msgType, (sender, data) =>
            {
                receivedMessage2 = data;
                Assert.AreEqual(1, sender);
            });

            msClient1.Send(0, msgType, new byte[] { 5, 6, 7, 8, 9 });

            server.Poll();

            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage1);
            Assert.AreEqual(new byte[] { 5, 6, 7, 8, 9 }, receivedMessage2);
        }

        [Test]
        public void TestExceptions()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new MessageSystem(null, ""));

            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new MessageSystem(server, null));

            // ReSharper disable once UnusedVariable
            var ms = new MessageSystem(server, "channel1");
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentException>(() => new MessageSystem(server, "channel1"));
        }

        [Test]
        public void TestMultipleChannels()
        {
            var cServer1 = new MessageSystem(server, "channel1");
            var cClient1 = new MessageSystem(client1, "channel1");

            var cServer2 = new MessageSystem(server, "channel2");
            var cClient2 = new MessageSystem(client1, "channel2");

            byte[] receivedMessage1 = null;
            byte[] receivedMessage2 = null;

            cServer1.AddListener(10, (sender, data) =>
            {
                receivedMessage1 = data;
                Assert.AreEqual(1, sender);
            });

            cServer2.AddListener(10, (sender, data) =>
            {
                receivedMessage2 = data;
                Assert.AreEqual(1, sender);
            });


            cClient1.Send(0, 10, new byte[]{ 1, 2, 3, 4 });
            server.Poll();
            Assert.AreEqual(new byte[] { 1, 2, 3, 4 }, receivedMessage1);
            Assert.IsNull(receivedMessage2);

            receivedMessage1 = null;
            receivedMessage2 = null;

            cClient2.Send(0, 10, new byte[]{ 5, 6, 7, 8 });
            server.Poll();
            Assert.AreEqual(new byte[]{ 5, 6, 7, 8 }, receivedMessage2);
        }
    }
}
