using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace NetLib.Transport.LiteNetLib
{
    /// <summary>
    /// Client implementation using the UDP protocol.
    /// </summary>
    /// <remarks>
    /// Uses the LiteNetLib library https://github.com/RevenantX/LiteNetLib.
    /// </remarks>
    public class UdpClient : IClient
    {
        /// <inheritdoc />
        public OnConnect OnConnect { get; set; }

        /// <inheritdoc />
        public OnData OnData { get; set; }

        /// <inheritdoc />
        public OnDisconnect OnDisconnect { get; set; }

        /// <inheritdoc/>
        public bool IsActive => client.IsRunning;

        private readonly NetManager client;
        private NetPeer connectedPeer;
        // connection Key has to be the same on server and client in order to establish the connection
        private readonly string connectionKey;


        /// <summary>
        /// Creates a new UdpClient.
        /// </summary>
        /// <param name="connectionKey">The key required to connect to the server. May be empty.</param>
        public UdpClient(string connectionKey = "")
        {
            var listener = new EventBasedNetListener();
            client = new NetManager(listener);
            this.connectionKey = connectionKey;

            listener.PeerConnectedEvent += peer =>
            {
                OnConnect?.Invoke((ulong)peer.Id);
            };

            listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                OnData?.Invoke((ulong)peer.Id, reader.GetRemainingBytes());
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                OnDisconnect?.Invoke((ulong)peer.Id);
            };

            OnConnect += id =>
            {
                Utils.Logger.Log("UdpClient", "Connected to server");
            };
            OnDisconnect += id =>
            {
                Utils.Logger.Log("UdpClient", "Disconnected from server");
            };
        }

        /// <inheritdoc />
        public void Poll()
        {
            client.PollEvents();
        }

        /// <inheritdoc />
        public void Send(byte[] message, ulong id = 0)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (id != 0)
                throw new InvalidConnectionIdException((int)id, message, "A client can only send to the server");

            var writer = new NetDataWriter();
            writer.Put(message);
            connectedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        /// <inheritdoc />
        public void Connect(string ip, ushort port)
        {
            if (!client.Start())
            {
                throw new FailedToStartClientException(port, ip, "Unable to start client");
            }
            connectedPeer = client.Connect(ip, port, connectionKey);
            Utils.Logger.Log("UdpClient", $"Connecting to {ip} on port {port}");
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            client.DisconnectPeer(client.FirstPeer);
            connectedPeer = null;
            Utils.Logger.Log("UdpClient", "Disconnecting from server");
        }
    }
}