using System;
using System.IO;

namespace NetLib.Serialization
{
    /// <summary>
    /// Serializes and deserializes a file in binary format.
    /// </summary>
    /// <remarks>
    /// The maximum supported size for files is 2GB for the binary representation.
    /// </remarks>
    public static class FileSerializer
    {
        /// <summary>
        /// Serializes a file from disk into a byte array.
        /// </summary>
        /// <param name="filepath">The path to the file on disk to open for serialization.</param>
        /// <returns></returns>
        public static byte[] Serialize(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                var fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, fileBytes.Length);

                return fileBytes;
            }
        }

        /// <summary>
        /// Deserializes a file and saves it to disk.
        /// </summary>
        /// <param name="data">The data to deserialize.</param>
        /// <param name="filepath">The path to where to save the deserialized file on disk.</param>
        /// <exception cref="ArgumentNullException">The <c>data</c> is null</exception>
        public static void Deserialize(byte[] data, string filepath)
        {
            if (data == null)
                throw new ArgumentNullException();

            using (Stream file = File.OpenWrite(filepath))
            {
                file.Write(data, 0, data.Length);
            }
        }
    }
}
