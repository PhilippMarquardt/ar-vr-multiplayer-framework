using System.Runtime.Serialization;
using UnityEngine;

namespace NetLib.Serialization.Surrogates
{
    internal sealed class QuaternionSerializationSurrogate : ISerializationSurrogate
    {
        /// <inheritdoc/>
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var quaternion = (Quaternion)obj;
            info.AddValue("x", quaternion.x);
            info.AddValue("y", quaternion.y);
            info.AddValue("z", quaternion.z);
            info.AddValue("w", quaternion.w);
        }

        /// <inheritdoc/>
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var quaternion = (Quaternion)obj;
            quaternion.x = info.GetSingle("x");
            quaternion.y = info.GetSingle("y");
            quaternion.z = info.GetSingle("z");
            quaternion.w = info.GetSingle("w");
            return quaternion;
        }
    }
}
