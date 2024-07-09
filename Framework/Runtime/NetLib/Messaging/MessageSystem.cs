using System;
using System.Collections.Generic;
using System.IO;
using NetLib.Serialization;
using NetLib.Transport;

namespace NetLib.Messaging
{
    /// <summary>
    /// Delegate for receiving messages.
    /// </summary>
    /// <param name="sender">The id of the sender of the message.</param>
    /// <param name="data">The received data.</param>
    public delegate void OnMessageReceivedDelegate(ulong sender, byte[] data);

    /// <summary>
    /// Class for sending and receiving data messages coupled to a type.
    /// </summary>
    /// <remarks>
    /// To send the messages across a network, an <see cref="ITransport"/> object is used. This means that the
    /// addressing scheme of <see cref="Send(ulong,byte,ulong,byte[])"/>, <see cref="Send(ulong,byte,ulong,byte[])"/>
    /// and <see cref="OnMessageReceivedDelegate"/> is the same as that of the <see cref="ITransport"/> with which the
    /// <see cref="MessageSystem"/> was initialized. Furthermore, messages are only received when the underlying
    /// <see cref="ITransport"/> is polled.
    /// <para>
    /// In order to receive messages an <see cref="OnMessageReceivedDelegate"/> listener must be registered.
    /// </para>
    /// <para>
    /// Data which is sent using a specific type will only be delivered to listeners for that specific type.
    /// There are 256 possible message types.
    /// </para>
    /// <para>
    /// Each <see cref="MessageSystem"/> instance is bound to a channel on which it will send and receive messages.
    /// No two <see cref="MessageSystem"/> instances can have the same channel. This allows for easier separation
    /// of message types, as message type ids can be reused on different channels without affecting each other.
    /// </para>
    /// <para>
    /// If a target object is specified, the message will be only delivered to listeners for that object.
    /// This can be used to send 'private' messages which should only be received by one object. 
    /// Objects are identified by a <c>ulong</c> id. For messages without a target object, the id 0 is used, which
    /// means that you should not use 0 as an actual object id.
    /// </para>
    /// </remarks>
    public class MessageSystem
    {
        private readonly ITransport transport;
        private readonly string channel;
        private readonly Dictionary<(byte, ulong), OnMessageReceivedDelegate> listeners;

        private static readonly HashSet<(ITransport, string)> globalChannelSet = new HashSet<(ITransport, string)>();

        /// <summary>
        /// Initializes a MessageSystem on a given channel with a specific transport implementation. 
        /// </summary>
        /// <remarks>
        /// The provided transport object will be used for sending and receiving the messages across the network.
        /// The channel will be bound to this <see cref="MessageSystem"/> instance, and no other instance using the
        /// same transport will be able to use this channel.
        /// </remarks>
        /// <param name="transport">The transport implementation to use.</param>
        /// <param name="channel">The channel on which to send messages.</param>
        /// <exception cref="ArgumentNullException">The transport is null</exception>
        /// <exception cref="ArgumentNullException">The channel is null</exception>
        /// <exception cref="ArgumentException">The channel is already in use on the given transport</exception>
        public MessageSystem(ITransport transport, string channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel), "the channel cannot be null");

            if (globalChannelSet.Contains((transport, channel)))
                throw new ArgumentException("the given channel is already in use on this transport", nameof(channel));

            this.transport = transport ?? 
                             throw new ArgumentNullException(nameof(transport), "the transport cannot be null");

            this.channel = channel;
            listeners = new Dictionary<(byte, ulong), OnMessageReceivedDelegate>();

            globalChannelSet.Add((transport, channel));
            transport.OnData += OnDataReceived;
        }

        /// <summary>
        /// Sends data of a specific type.
        /// </summary>
        /// <remarks>
        /// The <c>receiver</c> id is the same as used by the supplied <see cref="ITransport"/>.
        /// </remarks>
        /// <param name="receiver">The id of the participant to send the message to.</param>
        /// <param name="msgType">The type of the message.</param>
        /// <param name="data">The message data.</param>
        /// <exception cref="ArgumentNullException">The <c>data</c> is null.</exception>
        /// <exception cref="InvalidConnectionIdException">The <c>receiver</c> is not a valid connected peer.</exception>
        /// <exception cref="MessageNotSentException">The message could not be sent.</exception>
        public void Send(ulong receiver, byte msgType, byte[] data) => 
            Send(receiver, msgType, 0, data);

        /// <summary>
        /// Sends data of a specific type to a specific object.
        /// </summary>
        /// <remarks>
        /// The <c>receiver</c> id is the same as used by the supplied <see cref="ITransport"/>.
        /// </remarks>
        /// <param name="receiver">The id of the participant to send the message to.</param>
        /// <param name="msgType">The type of the message.</param>
        /// <param name="targetObject">The object to which the message will be sent.</param>
        /// <param name="data">The message data.</param>
        /// /// <exception cref="ArgumentNullException">The <c>data</c> is null.</exception>
        /// <exception cref="InvalidConnectionIdException">The <c>receiver</c> is not a valid connected peer.</exception>
        /// <exception cref="MessageNotSentException">The message could not be sent.</exception>
        public void Send(ulong receiver, byte msgType, ulong targetObject, byte[] data)
        {
            var message = new MsgData
            {
                Channel = channel,
                Type = msgType,
                Target = targetObject,
                Data = data
            };

            transport.Send(Serializer.Serialize(message), receiver);
        }

        /// <summary>
        /// Adds a delegate to receive messages of a specific type.
        /// </summary>
        /// <param name="msgType">The type of message to listen to.</param>
        /// <param name="handler">The delegate to be called when a message is received.</param>
        public void AddListener(byte msgType, OnMessageReceivedDelegate handler) => 
            AddListener(msgType, 0, handler);

        /// <summary>
        /// Adds a delegate to receive messages of a specific type for a specific object.
        /// </summary>
        /// <param name="msgType">The type of message to listen to.</param>
        /// <param name="targetObject">The object which listens to this message.</param>
        /// <param name="handler">The delegate to be called when a message is received.</param>
        public void AddListener(byte msgType, ulong targetObject, OnMessageReceivedDelegate handler)
        {
            if (listeners.TryGetValue((msgType, targetObject), out var delegates))
            {
                delegates += handler;
                listeners[(msgType,targetObject)] = delegates;
            }
            else
            {
                listeners.Add((msgType, targetObject), handler);
            }
        }

        // Gets called when the transport layer receives a message
        private void OnDataReceived(ulong id, byte[] data)
        {
            var message = Serializer.Deserialize<MsgData>(data);

            if (message.Channel != channel)
                return;

            if (!listeners.TryGetValue((message.Type, message.Target), out var handler))
                return;

            handler.Invoke(id, message.Data);
        }

        
        private struct MsgData : IBinarySerializable
        {
            internal string Channel;
            internal byte Type;
            internal ulong Target;
            internal byte[] Data;

            /// <inheritdoc/>
            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(Channel);
                writer.Write(Type);
                writer.Write(Target);
                writer.Write(Data.Length);
                writer.Write(Data);
            }

            /// <inheritdoc/>
            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                Channel = reader.ReadString();
                Type = reader.ReadByte();
                Target = reader.ReadUInt64();
                int dataSize = reader.ReadInt32();
                Data = reader.ReadBytes(dataSize);
            }
        }
    }
}
