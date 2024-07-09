namespace NetLib.Transport
{
    /// <summary>
    /// Represents a transport layer server.
    /// </summary>
    /// <remarks>
    /// For more information see <see cref="ITransport"/>.
    /// </remarks>
    public interface IServer : ITransport
    {
        /// <summary>
        /// Starts the server on a given network port.
        /// </summary>
        /// <param name="port">The network port on which to start the server.</param>
        /// <exception cref="FailedToStartServerException">The server could not be initialized.</exception>
        void Start(ushort port);

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <remarks>
        /// Does nothing if the server is not started.
        /// </remarks>
        void Stop();

        /// <summary>
        /// Returns the IPv4 address of a connected client.
        /// </summary>
        /// <param name="id">The connection id of the client.</param>
        /// <returns>The IP address in string format or an empty string if the client id is not in use.</returns>
        string GetClientAddressIpV4(ulong id);
    }
}