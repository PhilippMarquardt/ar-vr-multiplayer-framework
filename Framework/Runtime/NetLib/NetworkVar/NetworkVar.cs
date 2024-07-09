using NetLib.Serialization;

namespace NetLib.NetworkVar
{
    /// <summary>
    /// Generic container for networked variables inside of a NetworkBehaviour.
    /// </summary>
    /// <typeparam name="T">A serializable type for the internal value</typeparam>
    public class NetworkVar<T> : INetworkVar
    {
        private T internalValue;

        /// <summary>
        /// Used to get and set the value of this <see cref="NetworkVar{T}"/>.
        /// Use this instead of reassigning the NetworkVar object.
        /// </summary>
        public T Value
        {
            get => internalValue;
            set
            {
                internalValue = value;
                IsDirty = true;
            }
        }

        /// <inheritdoc/>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Initializes the internal value to its type's default value.
        /// </summary>
        public NetworkVar()
        {
            internalValue = default;
        }

        /// <summary>
        /// Initializes the internal value to the given value.
        /// </summary>
        /// <param name="value">the initial value</param>
        public NetworkVar(T value)
        {
            internalValue = value;
        }

        public static implicit operator T(NetworkVar<T> networkVar) => networkVar.internalValue;
        public static explicit operator NetworkVar<T>(T t) => new NetworkVar<T>(t);

        /// <inheritdoc/>
        public void ResetDirty()
        {
            IsDirty = false;
        }

        /// <inheritdoc/>
        public void WriteValue(byte[] data)
        {
            Serializer.Deserialize(ref internalValue, data);
        }

        /// <inheritdoc/>
        public void ReadValue(out byte[] data)
        {
            data = Serializer.Serialize(internalValue);
        }
    }
}
