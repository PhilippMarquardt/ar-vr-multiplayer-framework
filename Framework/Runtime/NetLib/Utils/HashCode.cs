using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetLib.Utils
{
    /// <summary>
    /// Provides methods for calculating hash values.
    /// </summary>
    public static class HashCode
    {
        /// <summary>
        /// Calculates a stable 32-bit hash for a given string.
        /// </summary>
        /// <remarks>
        /// The same string will always have the same hash value.
        /// Implementation of FNV-1 hash algorithm:
        /// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </remarks>
        /// <param name="text">The string to hash.</param>
        /// <returns>The stable hash value.</returns>
        public static uint GetStableHash32(string text)
        {
            const uint fnvOffsetBasis32 = 2166136261;
            const uint fnvPrime32 = 16777619;

            unchecked
            {
                uint hash = fnvOffsetBasis32;
                foreach (uint ch in text)
                {
                    hash *= fnvPrime32;
                    hash ^= ch;
                }
                return hash;
            }
        }

        /// <summary>
        /// Calculates a SHA-256 hash from the contents of a file on disk.
        /// </summary>
        /// <param name="filepath">The path of the file on disk to calculate the hash for.</param>
        /// <returns>The SHA-256 hash value.</returns>
        public static string GetFileHash(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(stream);

                    var builder = new StringBuilder();
                    foreach (byte c in hash)
                    {
                        builder.Append(c.ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
        }
    }
}
