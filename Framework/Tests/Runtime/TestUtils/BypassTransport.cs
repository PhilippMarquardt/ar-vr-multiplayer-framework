using System.Collections.Generic;
using System.Linq;
using NetLib.Transport;
using Debug = UnityEngine.Debug;

namespace TestUtils
{
    internal abstract class BypassTransport : ITransport
    {
        public OnConnect OnConnect { get; set; }
        public OnData OnData { get; set; }
        public OnDisconnect OnDisconnect { get; set; }
        public bool IsActive => true;

        internal readonly Queue<(ulong, byte[])> MessageQueue;
        internal readonly Queue<ulong> ConnectQueue;
        internal readonly Queue<ulong> DisconnectQueue;

        protected BypassTransport()
        {
            MessageQueue = new Queue<(ulong, byte[])>();
            ConnectQueue = new Queue<ulong>();
            DisconnectQueue = new Queue<ulong>();
        }

        public abstract void Send(byte[] message, ulong id = 0);

        public void Poll()
        {
            for (int i = 0; i < ConnectQueue.Count; i++)
            {
                ulong sender = ConnectQueue.Dequeue();
                OnConnect?.Invoke(sender);
            }
            for (int i = 0; i < DisconnectQueue.Count; i++)
            {
                ulong sender = DisconnectQueue.Dequeue();
                OnDisconnect?.Invoke(sender);
            }
            for (int i = 0; i < MessageQueue.Count; i++)
            {
                (ulong sender, var data) = MessageQueue.Dequeue();
                OnData?.Invoke(sender, data);
            }
        }

        internal void AddMessage(ulong sender, byte[] data)
        {
            MessageQueue.Enqueue((sender, data));
        }
    }

    internal class BypassServer : BypassTransport, IServer
    {
        private ulong clientId;
        private readonly Dictionary<ulong, BypassTransport> clients;

        public BypassServer()
        {
            clients = new Dictionary<ulong, BypassTransport>();
        }

        public void Start(ushort port) { }

        public void Stop() { }

        public string GetClientAddressIpV4(ulong id)
        {
            return "";
        }

        public override void Send(byte[] message, ulong id = 0)
        {
            if (!clients.TryGetValue(id, out var client))
                Debug.LogError("BypassServer | Sending to client " + id + ", but no such client available");
            client?.AddMessage(0, message);
        }

        internal void AddNewClient(BypassClient client)
        {
            ulong id = ++clientId;
            clients.Add(id, client);
            client.ConnectQueue.Enqueue(0);
            ConnectQueue.Enqueue(id);
        }

        internal void RemoveClient(BypassClient client)
        {
            ulong id = clients.ToList().First(kv => kv.Value == client).Key;
            clients.Remove(id);
            client.DisconnectQueue.Enqueue(0);
            DisconnectQueue.Enqueue(id);
        }

        internal void SendInternal(BypassClient client, byte[] data)
        {
            ulong id = clients.ToList().First(kv => kv.Value == client).Key;
            MessageQueue.Enqueue((id, data));
        }
    }

    internal class BypassClient : BypassTransport, IClient
    {
        private readonly BypassServer server;
        private bool isConnected;

        public BypassClient(BypassServer server)
        {
            this.server = server;
            OnConnect += id => isConnected = true;
        }

        public void Connect(string ip, ushort port)
        {
            server.AddNewClient(this);
        }

        public void Disconnect()
        {
            if (isConnected)
                server.RemoveClient(this);
        }

        public override void Send(byte[] message, ulong id = 0)
        {
            if (!isConnected)
                Debug.LogError("BypassClient | Sending, but not connected");
            server.SendInternal(this, message);
        }
    }
}
