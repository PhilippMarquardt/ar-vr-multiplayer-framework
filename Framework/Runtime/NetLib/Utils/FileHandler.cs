using System.Collections.Generic;
using NetLib.Serialization;

namespace NetLib.Utils
{
    /// <summary>
    /// Responsible for reconstructing file messages that are split into multiple parts.
    /// </summary>
    public class FileHandler
    {
        // Caches the parts of the messages for later reconstruction
        private readonly Dictionary<string, List<byte>> fileArrays = new Dictionary<string, List<byte>>();

        /// <summary>
        /// Adds a message part to the message cache and writes the files, if all parts of the message are received.
        /// </summary>
        /// <param name="totalMessageParts">Total number of parts the message is split into.</param>
        /// <param name="currentMessagePart">The current message part which will get added.</param>
        /// <param name="fileHash">The hash value by which to identify the file to which the message belongs to.</param>
        /// <param name="fileBytes">The file bytes of the current message part will will get added.</param>
        /// <param name="destinationPath">Path to where to save the completed file on the target system.</param>
        public void AddMessage(int totalMessageParts, int currentMessagePart, string fileHash, byte[] fileBytes, string destinationPath)
        {
            // add new part
            if (currentMessagePart < totalMessageParts)
            {
                if (!fileArrays.ContainsKey(fileHash))
                    fileArrays.Add(fileHash, new List<byte>(fileBytes));
                else
                    fileArrays[fileHash].AddRange(fileBytes);
            }

            // save file if parts are complete
            if (currentMessagePart == totalMessageParts - 1)
            {
                ReconstructFullMessage(fileArrays[fileHash], destinationPath);
                fileArrays.Remove(fileHash);
            }
        }

        private void ReconstructFullMessage(List<byte> msg, string path)
        {
            FileSerializer.Deserialize(msg.ToArray(), path);
        }
    }
}
