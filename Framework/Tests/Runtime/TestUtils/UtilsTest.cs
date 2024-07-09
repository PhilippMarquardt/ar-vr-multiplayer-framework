using NUnit.Framework;

namespace TestUtils
{
    [Category("TestUtils")]
    public class UtilsTest
    {
        private struct RecvMsg
        { 
            internal byte Data;
            internal ulong Sender;
        }

        [Test]
        public void TestBypassTransport()
        {
            var server = new BypassServer();
            var client1 = new BypassClient(server);
            var client2 = new BypassClient(server);
            var lastServerMsg = new RecvMsg();
            var lastClient1Msg = new RecvMsg();
            var lastClient2Msg = new RecvMsg();

            server.OnData += (id, data) =>
            {
                lastServerMsg = new RecvMsg()
                {
                    Data = data[0],
                    Sender = id
                };
            };
            client1.OnData += (id, data) =>
            {
                lastClient1Msg = new RecvMsg()
                {
                    Data = data[0],
                    Sender = id
                };
            };
            client2.OnData += (id, data) =>
            {
                lastClient2Msg = new RecvMsg()
                {
                    Data = data[0],
                    Sender = id
                };
            };


            client1.Connect("", 0);
            client2.Connect("", 0);
            server.Poll();
            client1.Poll();
            client2.Poll();


            client1.Send(new byte[] { 42 });
            server.Poll();
            Assert.AreEqual(42, lastServerMsg.Data);
            Assert.AreEqual(1, lastServerMsg.Sender);
            
            client2.Send(new byte[] { 23 });
            server.Poll();
            Assert.AreEqual(23, lastServerMsg.Data);
            Assert.AreEqual(2, lastServerMsg.Sender);

            server.Send(new byte[] { 55 }, 1);
            client1.Poll();
            Assert.AreEqual(55, lastClient1Msg.Data);
            Assert.AreEqual(0, lastClient1Msg.Sender);
            server.Send(new byte[] { 55 }, 2);
            client2.Poll();
            Assert.AreEqual(55, lastClient2Msg.Data);
            Assert.AreEqual(0, lastClient2Msg.Sender);
        }
    }
}
