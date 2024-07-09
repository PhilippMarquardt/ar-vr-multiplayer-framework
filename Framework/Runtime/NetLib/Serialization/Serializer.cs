using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NetLib.Serialization.Surrogates;

namespace NetLib.Serialization
{
    /// <summary>
    /// Serializes and deserializes an object in binary format.
    /// </summary>
    /// <remarks>
    /// Supported types for serialization are types marked with <see cref="SerializableAttribute"/>, types
    /// which implement the <see cref="IBinarySerializable"/> interface, as well as the Unity types
    /// <see cref="UnityEngine.Vector3"/> and <see cref="UnityEngine.Quaternion"/>.
    /// <para>
    /// The standard serialization includes a lot of information about the type, which might not be necessary for some
    /// applications. In that case you can use the <see cref="IBinarySerializable"/> interface to provide your own
    /// optimized serialization.
    /// </para>
    /// </remarks>
    public static class Serializer
    {
        /// <summary>
        /// The formatter used for standard serialization.
        /// </summary>
        private static readonly BinaryFormatter formatter = new BinaryFormatter();

        /// <summary>
        /// Constructs a <seealso cref="Serializer"/> with surrogates for <see cref="UnityEngine.Vector3"/> and
        /// <see cref="UnityEngine.Quaternion"/>.
        /// </summary>
        static Serializer()
        {
            var surrogateSelector = new SurrogateSelector();

            surrogateSelector.AddSurrogate(
                typeof(UnityEngine.Vector3),
                new StreamingContext(StreamingContextStates.All),
                new Vector3SerializationSurrogate());
            surrogateSelector.AddSurrogate(
                typeof(UnityEngine.Quaternion),
                new StreamingContext(StreamingContextStates.All),
                new QuaternionSerializationSurrogate());

            formatter.SurrogateSelector = surrogateSelector;
        }

        /// <summary>
        /// Serializes the given object into a byte array.
        /// </summary>
        /// <remarks>
        /// The object must either have the <see cref="SerializableAttribute"/> attribute specified or
        /// implement the <see cref="IBinarySerializable"/> interface.
        /// <para>
        /// If the object implements the <see cref="IBinarySerializable"/> interface, the serialization process
        /// defined by the implementation will be used. For all other objects, the <see cref="BinaryFormatter"/> will
        /// be used for serialization.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Type of the object which is being serialized</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="forceFullSerialization">If true, the <see cref="IBinarySerializable"/> interface will be ignored</param>
        /// <returns>the serialized object as a byte array</returns>
        /// <exception cref="ArgumentNullException">The object is null</exception>
        public static byte[] Serialize<T>(T obj, bool forceFullSerialization = false)
        {
            if (obj ==  null)
                throw new ArgumentNullException();

            using (var stream = new MemoryStream())
            {
                if (!forceFullSerialization && obj is IBinarySerializable o)
                {
                    stream.WriteByte(1);
                    o.Serialize(stream);
                }
                else
                {
                    stream.WriteByte(0);
                    formatter.Serialize(stream, obj);
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the given byte array into a new object.
        /// </summary>
        /// <remarks>
        /// The object must either have the <see cref="SerializableAttribute"/> attribute specified or
        /// implement the <see cref="IBinarySerializable"/> interface.
        /// <para>
        /// If the object implements the <see cref="IBinarySerializable"/> interface, the deserialization process
        /// defined by the implementation will be used. For all other objects, the <see cref="BinaryFormatter"/> will
        /// be used for deserialization.
        /// </para>
        /// <para>
        /// If type <see cref="T"/> implements <see cref="IBinarySerializable"/> then the <see cref="data"/> must come
        /// from the serialization of an instance of exactly the same type <see cref="T"/>. Note that this also means
        /// that you cannot deserialize to a base type if the serialization used the <see cref="IBinarySerializable"/>
        /// implementation of a derived type, i.e. if the object you serialized is an instance of a derived type with
        /// an overridden <see cref="IBinarySerializable.Serialize"/> method.
        /// If you need polymorphic deserialization you can either use types that do not implement
        /// <see cref="IBinarySerializable"/> or use <see cref="Deserialize{T}(ref T,byte[])"/> instead.
        /// </para>
        /// This method does not call any constructors on the deserialized object. Thus the returned object may not be
        /// in a valid state if the serialization does not include all fields.
        /// If you need to initialize the object, you can instead use <see cref="Deserialize{T}(ref T,byte[])"/>.
        /// </remarks>
        /// <typeparam name="T">Type of the object which was serialized into the byte array</typeparam>
        /// <param name="data">The data to deserialize</param>
        /// <returns>A new deserialized object</returns>
        /// <exception cref="ArgumentNullException">The <c>data</c> ist <c>null</c></exception>
        /// <exception cref="ArgumentException">The <c>data</c> is empty</exception>
        /// <exception cref="InvalidOperationException">The <c>data</c> was serialized as IBinarySerializable but <c>T</c>
        /// does not implement the interface</exception>
        public static T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();
            if (data.Length == 0)
                throw new ArgumentException("deserialization data cannot be empty", nameof(data));

            using (var stream = new MemoryStream(data))
            {
                int isIBinarySerializable = stream.ReadByte();
                if (isIBinarySerializable == 1)
                {
                    if (!typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
                        throw new InvalidOperationException("the object was serialized as IBinarySerializable but T does not implement IBinarySerializable");

                    // 'unsafe' because object might be in invalid state, kept for backward compability
                    var obj = (IBinarySerializable)FormatterServices.GetUninitializedObject(typeof(T));
                    obj.Deserialize(stream);
                    return (T)obj;
                }
                
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Deserializes the given byte array into an existing object.
        /// </summary>
        /// <remarks>
        /// The object must either have the <see cref="SerializableAttribute"/> attribute specified or
        /// implement the <see cref="IBinarySerializable"/> interface.
        /// <para>
        /// If the object implements the <see cref="IBinarySerializable"/> interface, the deserialization process
        /// defined by the implementation will be used. For all other objects, the <see cref="BinaryFormatter"/> will
        /// be used for deserialization.
        /// </para>
        /// In contrast to <see cref="Deserialize{T}(byte[])"/> this method does support type polymorphism for
        /// <see cref="IBinarySerializable"/> types.
        /// </remarks>
        /// <typeparam name="T">Type of the object which was serialized into the byte array</typeparam>
        /// <param name="obj">The object to overwrite with the deserialized data</param>
        /// <param name="data">The data to deserialize</param>
        /// <exception cref="ArgumentNullException">The <c>obj</c> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">The <c>data</c> ist <c>null</c></exception>
        /// <exception cref="ArgumentException">The <c>data</c> is empty</exception>
        /// <exception cref="InvalidOperationException">The <c>data</c> was serialized as IBinarySerializable but <c>T</c>
        /// does not implement the interface</exception>
        public static void Deserialize<T>(ref T obj, byte[] data)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException("deserialization data cannot be empty", nameof(data));

            using (var stream = new MemoryStream(data))
            {
                int isIBinarySerializable = stream.ReadByte();

                if (isIBinarySerializable == 1)
                {
                    if (!(obj is IBinarySerializable o))
                        throw new InvalidOperationException("the object was serialized as IBinarySerializable but T does not implement IBinarySerializable");

                    o.Deserialize(stream);
                    return;
                }

                obj = (T)formatter.Deserialize(stream);
            }
        }
    }
}
