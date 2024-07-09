using System.IO;

namespace NetLib.Serialization
{
    /// <summary>
    /// Allows an object to provide its own serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// If a class needs to control its serialization process, it can implement the <see cref="IBinarySerializable"/>
    /// interface.
    /// The <see cref="Serializer"/> calls the <see cref="Serialize"/> method at serialization time and provides a
    /// <see cref="Stream"/> to which the implementation should write its serialized data. At deserialization time the
    /// <see cref="Deserialize"/> method is called with a <see cref="Stream"/> from which the implementation should
    /// read its data for deserializing.
    /// The <see cref="BinaryWriter"/> and <see cref="BinaryWriter"/> can be used for writing and reading from the
    /// stream.
    /// Make sure you use the same memory layout when serializing and deserializing.
    /// </remarks>
    public interface IBinarySerializable
    {
        /// <summary>
        /// Serializes this object into binary format and writes the result to the given stream.
        /// </summary>
        /// <param name="stream">The stream into which the binary data is written</param>
        void Serialize(Stream stream);

        /// <summary>
        /// Deserializes the binary data from the given stream into this object.
        /// </summary>
        /// <param name="stream">The stream from which the binary data is read</param>
        void Deserialize(Stream stream);
    }
}
