namespace NetLib.NetworkVar
{
    /// <summary>
    /// Interface for networked variables.
    /// </summary>
    public interface INetworkVar
    {
        /// <summary>
        /// Returns whether the value has changed since the last synchronization.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Resets the dirty flag.
        /// </summary>
        void ResetDirty();

        /// <summary>
        /// Sets the internal value of this NetworkVar to the given serialized value. 
        /// </summary>
        /// <param name="data">A serialized value of type T</param>
        void WriteValue(byte[] data);

        /// <summary>
        /// Returns the internal value in serialized form.
        /// </summary>
        /// <param name="data">The array to store the serialized value in</param>
        void ReadValue(out byte[] data);
    }
}
