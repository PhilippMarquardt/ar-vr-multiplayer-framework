using System;
using System.Net;
using Telepathy;

namespace NetLib.Transport.Telepathy
{
    /// <summary>
    /// Server implementation using the TCP protocol.
    /// </summary>
    /// <remarks>
    /// Uses the Telepathy library https://github.com/vis2k/Telepathy.
    /// Client ids are simply incremented with each connecting client. Old ids of disconnected clients are not
    /// immediately reused.
    /// </remarks>
    public class TcpServer : IServer
    {
        /// <inheritdoc/>
        public OnConnect OnConnect { get; set; }

        /// <inheritdoc/>
        public OnData OnData { get; set; }

        /// <inheritdoc/>
        public OnDisconnect OnDisconnect { get; set; }

        /// <inheritdoc/>
        public bool IsActive => server.Active;

        private readonly Server server;


        /// <summary>
        /// Creates a new TcpServer.
        /// </summary>
        /// <param name="maxMessageSize">The Maximum message size in bytes.</param>
        public TcpServer(int maxMessageSize = int.MaxValue)
        {
            server = new Server
            {
                MaxMessageSize = maxMessageSize
            };

            OnConnect += id =>
            {
                Utils.Logger.Log("TcpServer", $"A client connected with id {id}");
            };
            OnDisconnect += id =>
            {
                Utils.Logger.Log("TcpServer", $"Client with id {id} disconnected");
            };

            Logger.Log = str => Utils.Logger.Log("Telepathy", str);
            Logger.LogWarning = str => Utils.Logger.LogWarning("Telepathy", str);
            Logger.LogError = str => Utils.Logger.LogError("Telepathy", str);
        }

        /// <inheritdoc/>
        public void Poll()
        {
            while (server.GetNextMessage(out var message))
            {
                switch (message.eventType)
                {
                    case EventType.Connected:
                        OnConnect?.Invoke((ulong)message.connectionId);
                        break;
                    case EventType.Data:
                        OnData?.Invoke((ulong)message.connectionId, message.data);
                        break;
                    case EventType.Disconnected:
                        OnDisconnect?.Invoke((ulong)message.connectionId);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void Send(byte[] message, ulong id = 0)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (id == 0)
                throw new InvalidConnectionIdException((int)id, message, "The server can only send to clients");

            if (!server.Send((int)id, message))
            {
                throw new MessageNotSentException((int)id, message, 
                    $"Failed to send message to client with id {id}. Check if it is connected.");
            }
        }

        /// <inheritdoc/>
        public void Start(ushort port)
        {
            if (!server.Start(port))
            {
                throw new FailedToStartServerException(port, 
                    $"Failed to start server on port {port}. Check if it is already running.");
            }
            Utils.Logger.Log("TcpServer", $"Starting server on port {port}");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            server.Stop();
            Utils.Logger.Log("TcpServer", "Stopping the server");
        }

        /// <inheritdoc/>
        public string GetClientAddressIpV4(ulong id)
        {
            string addressString = server.GetClientAddress((int)id);
            return addressString.Length == 0 ? "" : IPAddress.Parse(addressString).MapToIPv4().ToString();
        }
    }
}
