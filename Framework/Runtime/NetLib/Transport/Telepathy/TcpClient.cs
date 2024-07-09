using System;
using Telepathy;

namespace NetLib.Transport.Telepathy
{
    /// <summary>
    /// Client implementation using the TCP protocol.
    /// </summary>
    /// <remarks>
    /// Uses the Telepathy library https://github.com/vis2k/Telepathy.
    /// </remarks>
    public class TcpClient : IClient
    {
        /// <inheritdoc/>
        public OnConnect OnConnect { get; set; }

        /// <inheritdoc/>
        public OnData OnData { get; set; }

        /// <inheritdoc/>
        public OnDisconnect OnDisconnect { get; set; }

        /// <inheritdoc/>
        public bool IsActive => client.Connected;

        private readonly Client client;


        /// <summary>
        /// Creates a new TcpClient. 
        /// </summary>
        /// <param name="maxMessageSize">The maximum message size in bytes.</param>
        public TcpClient(int maxMessageSize = int.MaxValue)
        {
            client = new Client
            {
                MaxMessageSize = maxMessageSize
            };

            OnConnect += id =>
            {
                Utils.Logger.Log("TcpClient", "Connected to server");
            };
            OnDisconnect += id =>
            {
                Utils.Logger.Log("TcpClient", "Disconnected from server");
            };

            Logger.Log = str => Utils.Logger.Log("Telepathy", str);
            Logger.LogWarning = str => Utils.Logger.LogWarning("Telepathy", str);
            Logger.LogError = str => Utils.Logger.LogError("Telepathy", str);
        }

        /// <inheritdoc/>
        public void Poll()
        {
            while (client.GetNextMessage(out var message))
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

            if (id != 0)
                throw new InvalidConnectionIdException((int)id, message, "A client can only send to the server");

            if (!client.Send(message))
            {
                throw new MessageNotSentException((int)id, message, 
                    "Failed to send message to server. Check if it is connected.");
            }
        }

        /// <inheritdoc/>
        public void Connect(string ip, ushort port)
        {
            client.Connect(ip, port);
            Utils.Logger.Log("TcpClient", $"Connecting to {ip} on port {port}");
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            client.Disconnect();
            Utils.Logger.Log("TcpClient", "Disconnecting from server");
        }
    }
}
