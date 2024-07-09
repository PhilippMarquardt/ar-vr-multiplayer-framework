using System;

namespace NetLib.Transport
{
    /// <summary>
    /// Exception which gets thrown when a message could not be sent.
    /// </summary>
    public class MessageNotSentException : Exception
    {
        /// <summary>
        /// Target connection id of the failed message.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Message data of the failed message.
        /// </summary>
        public byte[] MessageData { get; }


        /// <summary>
        /// Creates new MessageNotSentException.
        /// </summary>
        /// <param name="id">The target connection id of the failed message.</param>
        /// <param name="data">The message data of the failed message.</param>
        /// <param name="message">The message that describes the error.</param>
        public MessageNotSentException(int id, byte[] data, string message = "") : base(message)
        {
            Id = id;
            MessageData = data;
        }
    }

    /// <summary>
    /// Exception which gets thrown when attempting to send a message to an invalid connection id.
    /// </summary>
    public class InvalidConnectionIdException : MessageNotSentException
    {
        /// <summary>
        /// Creates a new InvalidConnectionException.
        /// </summary>
        /// <param name="id">The target connection id of the failed message.</param>
        /// <param name="data">The message data of the failed message.</param>
        /// <param name="message">The message that describes the error.</param>
        public InvalidConnectionIdException(int id, byte[] data, string message = "") : base(id, data, message) { }
    }
}
