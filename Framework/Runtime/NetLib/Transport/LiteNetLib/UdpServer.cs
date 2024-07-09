using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace NetLib.Transport.LiteNetLib
{
    /// <summary>
    /// Server implementation using the TCP protocol.
    /// </summary>
    /// <remarks>
    /// Uses the LiteNetLib library https://github.com/RevenantX/LiteNetLib.
    /// Client ids are simply incremented with each connecting client. Old ids of disconnected clients are
    /// immediately reused for new clients.
    /// </remarks>
    public class UdpServer : IServer
    {
        /// <inheritdoc/>
        public OnConnect OnConnect { get; set; }

        /// <inheritdoc/>
        public OnData OnData { get; set; }

        /// <inheritdoc/>
        public OnDisconnect OnDisconnect { get; set; }

        /// <inheritdoc/>
        public bool IsActive => server.IsRunning;

        private readonly NetManager server;
        private readonly Dictionary<int, NetPeer> connectedPeers;


        /// <summary>
        /// Creates a new UdpServer.
        /// </summary>
        /// <param name="maxConnections">Maximum number of connections accepted</param>
        /// <param name="connectionKey">key required to connect to this server </param>
        public UdpServer(int maxConnections, string connectionKey = "")
        {
            var listener = new EventBasedNetListener();
            server = new NetManager(listener);
            connectedPeers = new Dictionary<int, NetPeer>();

            // subscribe delegates to corresponding events
            listener.PeerConnectedEvent += peer =>
            {
                int id = peer.Id + 1; // LiteNetLib starts counting clients at 0, but 0 is reserved for the server
                connectedPeers.Add(id, peer);
                OnConnect?.Invoke((ulong)id);
            };

            listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                int id = peer.Id + 1; // LiteNetLib starts counting clients at 0, but 0 is reserved for the server
                OnData?.Invoke((ulong)id, reader.GetRemainingBytes());
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                int id = peer.Id + 1; // LiteNetLib starts counting clients at 0, but 0 is reserved for the server
                connectedPeers.Remove(id);
                OnDisconnect?.Invoke((ulong)id);
            };

            listener.ConnectionRequestEvent += request =>
            {
                Utils.Logger.Log("UdpServer", $"Connection requested by {request.RemoteEndPoint.Address}");

                if (server.PeersCount < maxConnections)
                {
                    request.AcceptIfKey(connectionKey);
                }
                else
                {
                    request.Reject();
                }
            };

            // Add log functionality
            OnConnect += id =>
            {
                Utils.Logger.Log("UdpServer", $"A client connected with id {id}");
            };
            OnDisconnect += id =>
            {
                Utils.Logger.Log("UdpServer", $"Client with id {id} disconnected");
            };
        }

        /// <inheritdoc/>
        public void Poll()
        {
            server.PollEvents();
        }

        /// <inheritdoc/>
        public void Send(byte[] message, ulong id = 0)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (id == 0)
                throw new InvalidConnectionIdException((int)id, message, "The server can only send to clients");

            if (!connectedPeers.TryGetValue((int)id, out var peer) || peer == null)
                throw new MessageNotSentException((int)id, message, $"No client with id {id} is connected");

            var writer = new NetDataWriter();
            writer.Put(message);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        /// <inheritdoc/>
        public void Start(ushort port)
        {
            if (!server.Start(port))
            {
                throw new FailedToStartServerException(port);
            }

            Utils.Logger.Log("UdpServer", $"Started server on port {port}");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            server.Stop();
            Utils.Logger.Log("UdpServer", "Stopping the server");
        }

        /// <inheritdoc/>
        public string GetClientAddressIpV4(ulong id)
        {
            if (!connectedPeers.TryGetValue((int)id, out var client))
                return "";

            return client.EndPoint.Address.MapToIPv4().ToString();
        }
    }
}