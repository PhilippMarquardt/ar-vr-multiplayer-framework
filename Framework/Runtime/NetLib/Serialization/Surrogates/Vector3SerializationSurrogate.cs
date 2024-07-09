using System.Runtime.Serialization;
using UnityEngine;

namespace NetLib.Serialization.Surrogates
{
    internal sealed class Vector3SerializationSurrogate : ISerializationSurrogate
    {
        /// <inheritdoc/>
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var vector = (Vector3)obj;
            info.AddValue("x", vector.x);
            info.AddValue("y", vector.y);
            info.AddValue("z", vector.z);
        }

        /// <inheritdoc/>
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var vector = (Vector3)obj;
            vector.x = info.GetSingle("x");
            vector.y = info.GetSingle("y");
            vector.z = info.GetSingle("z");
            return vector;
        }
    }
}
