namespace NetLib.Transport
{
    /// <summary>
    /// Delegate for handling connect messages.
    /// </summary>
    /// <remarks>
    /// For a server receiving this message the <c>id</c> will be a new connection id for the client which just
    /// connected. For a client receiving this message the <c>id</c> will be 0, denoting the connection id of the
    /// server.
    /// </remarks>
    /// <param name="id">Connection ID of the participant which just connected.</param>
    public delegate void OnConnect(ulong id);

    /// <summary>
    /// Delegate for handling disconnect messages.
    /// </summary>
    /// <remarks>
    /// For a server receiving this message the <c>id</c> will be a new connection id for the client which just
    /// disconnected. For a client receiving this message the <c>id</c> will be 0, denoting the connection id of the
    /// server.
    /// </remarks>
    /// <param name="id">Connection ID of the client which just disconnected.</param>
    public delegate void OnDisconnect(ulong id);

    /// <summary>
    /// Delegate for handling data messages.
    /// </summary>
    /// <param name="id">Connection ID of the participant who sent the message.</param>
    /// <param name="data">The data sent by the participant.</param>
    public delegate void OnData(ulong id, byte[] data);

    /// <summary>
    /// Represents a transport layer peer for sending and receiving messages across the network.
    /// </summary>
    /// <remarks>
    /// The transport layer peer is a participant in a client/server architecture. There can be many clients but only
    /// one server in a given connection. A connection is bound to a network port. Therefore multiple client/server
    /// connections can be run in parallel on separate ports.
    /// <para>
    /// Each participant gets assigned a connection id, which is used to identify the receiver when sending messages.
    /// The server will always have the ID 0.
    /// </para>
    /// <para>
    /// The transport object can be either a client or a server. A server can send and receive messages to and from
    /// all connected clients. A client can only send and receive messages to and from the server it is connected to.
    /// </para>
    /// <para>
    /// In order to react to received messages the <see cref="OnConnect"/>, <see cref="OnData"/> and
    /// <see cref="OnDisconnect"/> delegate members can be subscribed to. Incoming messages are internally queued and
    /// the delegates are only invoked when <see cref="Poll"/> is called.
    /// </para>
    /// Base interface for <see cref="IServer"/> and <see cref="IClient"/>.
    /// </remarks>
    public interface ITransport
	{
        /// <summary>
        /// Contains functions called if a connection is established.
        /// <remarks>
        /// See OnConnect delegate type for more information. 
        /// </remarks>
        /// </summary>
        OnConnect OnConnect { get; set; }

        /// <summary>
        /// Contains functions called if data is received.
        /// <remarks>
        /// See OnData delegate type for more information.
        /// </remarks>
        /// </summary>
        OnData OnData { get; set; }

        /// <summary>
        /// Contains functions called if peer disconnects.
        /// <remarks>
        /// See OnDisconnect delegate type for more information.
        /// </remarks>
        /// </summary>
        OnDisconnect OnDisconnect { get; set; }

        /// <summary>
        /// Whether this transport is connected and running.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Polls all received messages since the last call to <see cref="Poll"/> and invokes the corresponding
        /// delegates.
        /// </summary>
        void Poll();

        /// <summary>
        /// Sends a data message to a connected peer.
        /// </summary>
        /// <param name="message">The data to be sent.</param>
        /// <param name="id">The connection id of the peer to send the data to.</param>
        /// <exception cref="System.ArgumentNullException">The <c>message</c> is null.</exception>
        /// <exception cref="InvalidConnectionIdException">The <c>id</c> is not a valid connected peer.</exception>
        /// <exception cref="MessageNotSentException">The message could not be sent.</exception>
        void Send(byte[] message, ulong id = 0);
    }
}