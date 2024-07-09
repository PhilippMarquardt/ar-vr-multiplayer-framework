namespace NetLib.Utils
{
    /// <summary>
    /// Collection of constants used by the framework.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Message type ids for internal messages sent by the framework through the
        /// <see cref="Messaging.MessageSystem"/>.
        /// </summary>
        internal enum InternalMessageType : byte
        {
            StateUpdate,
            InitialState,
            SpawnRequest,
            DestroyRequest,
            ChangeRequest,
            Handshake,
            Custom,
            Rpc,
            FileMessage
        }
    }
}
