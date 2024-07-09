using System;

namespace NetLib.Transport
{
    /// <summary>
    /// Exception which gets thrown when a transport peer could not be initialized.
    /// </summary>
    public class InitializationFailedException : Exception
    {
        /// <summary>
        /// Port used for the connection which triggered this exception.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Server IP address used for the connection which triggered this exception.
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// Creates a new ConnectionEstablishException.
        /// </summary>
        /// <param name="port">The network port of the connection.</param>
        /// <param name="ip">The server IP address.</param>
        /// <param name="message">THe message describing the error</param>
        public InitializationFailedException(int port, string ip, string message = "") : base(message)
        {
            Port = port;
            Ip = ip;
        }
    }

    /// <summary>
    /// Exception which gets thrown when a server could not be started.
    /// </summary>
    public class FailedToStartServerException : InitializationFailedException
    {
        /// <summary>
        /// Creates a new FailedToStartServerException
        /// </summary>
        /// <param name="port">The network port of the connection.</param>
        /// <param name="message">THe message describing the error</param>
        public FailedToStartServerException(int port, string message = "") : base(port, "", message) { }
    }

    /// <summary>
    /// Exception which gets thrown when a client could not be started.
    /// </summary>
    public class FailedToStartClientException : InitializationFailedException
    {
        /// <summary>
        /// Creates a new FailedToStartServerException
        /// </summary>
        /// <param name="port">The network port of the connection.</param>
        /// /// <param name="ip">The server IP address.</param>
        /// <param name="message">THe message describing the error</param>
        public FailedToStartClientException(int port, string ip, string message = "") : base(port, ip, message) { }
    }
}