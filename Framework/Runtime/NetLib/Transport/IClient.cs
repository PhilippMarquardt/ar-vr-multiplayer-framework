namespace NetLib.Transport
{
    /// <summary>
    /// Represents a transport layer client.
    /// </summary>
    /// <remarks>
    /// For more information see <see cref="ITransport"/>.
    /// </remarks>
    public interface IClient : ITransport
    {
        /// <summary>
        /// Connects this client to a server.
        /// </summary>
        /// <param name="ip">The ip address of the server to which to connect.</param>
        /// <param name="port">The network port of the server on which to connect to.</param>
        /// <exception cref="FailedToStartClientException">The client could not be initialized.</exception>
        void Connect(string ip, ushort port);

        /// <summary>
        /// Disconnects this client from the connected server.
        /// </summary>
        void Disconnect();
    }
}