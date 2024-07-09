using UnityEngine;

namespace NetLib.Spawning
{
    /// <summary>
    /// Abstract base class for networked objects.
    /// </summary>
    /// <remarks>
    /// Must be inherited by a concrete implementation when using the spawning system.
    /// The derived class can specify what data gets synchronized between the <see cref="SpawningServer"/> and
    /// <see cref="SpawningClient"/>.
    /// <para>
    /// When the object is spawned on the <see cref="SpawningServer"/>, the methods <see cref="SerializeOnSpawn"/>,
    /// <see cref="Serialize"/> and <see cref="OnNetworkStart"/> are called in that order.
    /// When the object is spawned on the <see cref="SpawningClient"/>, the methods <see cref="DeserializeOnSpawn"/>,
    /// <see cref="Deserialize"/> and <see cref="OnNetworkStart"/> are called in that order.
    /// </para>
    /// </remarks>
    public abstract class NetworkObjectBase : MonoBehaviour
    {
        /// <summary>
        /// Unique network id for this object.
        /// </summary>
        /// <remarks>
        /// The network id gets assigned to a networked object when it is spawned by the <see cref="SpawningServer"/>.
        /// It is the same on the server and any client instance of the object.
        /// </remarks>
        public ulong Uuid { get; internal set; }

        /// <summary>
        /// <c>True</c> if this object is a server instance, <c>false</c> otherwise.
        /// </summary>
        public bool IsServer { get; internal set; }

        /// <summary>
        /// <c>True</c> if this object is a client instance, <c>false</c> otherwise.
        /// </summary>
        public bool IsClient { get; internal set; }


        /// <summary>
        /// Index of this object in the scene.
        /// </summary>
        /// <remarks>
        /// Used to order scene object during scene initialization.
        /// Implementations must provide a consistent ordering across all networked scene objects. The order must
        /// be the same on the server and all clients.
        /// </remarks>
        public abstract int SceneOrderIndex { get; }

        /// <summary>
        /// Hash by which this object can be found amongst all registered prefabs. 
        /// </summary>
        /// <remarks>
        /// Used by the <see cref="SpawningServer"/> and <see cref="SpawningClient"/> to identify which prefab must
        /// be spawned. Must be unique for each prefab registered in <see cref="SpawningBase"/>.
        /// <para>
        /// Derived classes should implement this property with a backing field which can be serialized by the Unity
        /// Editor. Otherwise the hash value gets lost when an object is instantiated from a prefab.
        /// </para>
        /// </remarks>
        public abstract string PrefabHash { get; set; }

        /// <summary>
        /// <c>True</c> if this object is the player object.
        /// </summary>
        /// <remarks>
        /// Used by the <see cref="SpawningServer"/> and <see cref="SpawningClient"/> to identify the player object in
        /// the scene when it is being initialized. The player object is not considered a scene object and thus does
        /// not receive an id during scene initialization. For client players the id is rather assigned when the
        /// <see cref="SpawningClient"/> receives the initial state after a player prefab was spawned by
        /// <see cref="SpawningServer.SpawnClientPlayerObject"/>.
        /// For a server player the id is assigned when <see cref="SpawningServer.SpawnServerPlayerObject"/> is called.
        /// </remarks>
        public abstract bool IsPlayer { get; }


        /// <summary>
        /// Returns whether this object is marked for serialization.
        /// </summary>
        /// <returns><c>True</c> if the object should be serialized in the next update, <c>false</c> otherwise</returns>
        public abstract bool IsDirty();

        /// <summary>
        /// Marks this object for serialization regardless of its internal state.
        /// </summary>
        /// <remarks>
        /// Derived classes must implement this in such a way, that a call to <see cref="IsDirty"/> after a call to
        /// <see cref="MarkDirty"/> always return <c>true</c>.
        /// </remarks>
        public abstract void MarkDirty();

        /// <summary>
        /// Un-marks this object for serialization regardless of its internal state.
        /// </summary>
        /// <remarks>
        /// Derived classes must implement this in such a way, that a call to <see cref="IsDirty"/> after a call to
        /// <see cref="ResetDirty"/> always return <c>false</c>.
        /// </remarks>
        public abstract void ResetDirty();

        /// <summary>
        /// Returns the serialized state of this object.
        /// </summary>
        /// <remarks>
        /// Gets called during each state update on the <see cref="SpawningServer"/> if the object is marked as dirty
        /// to synchronize the object's state with all connected <see cref="SpawningClient"/> instances.
        /// The return value of this method must be able to be processed by <see cref="Deserialize"/>.
        /// </remarks>
        /// <returns>A byte representation of this object's state.</returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Applies a serialized state on this object.
        /// </summary>
        /// <remarks>
        /// Gets called during each state update on the <see cref="SpawningClient"/> if the object was marked dirty
        /// on the server to synchronize the object's state with the connected <see cref="SpawningServer"/>.
        /// </remarks>
        /// <param name="data">A byte representation of this object's state.</param>
        public abstract void Deserialize(byte[] data);

        /// <summary>
        /// Returns the serialized initialization state of this object.
        /// </summary>
        /// <remarks>
        /// Gets called by the <see cref="SpawningServer"/> each time an initial state is sent to to a client during
        /// a call to <see cref="SpawningServer.SendInitialState"/>. Can be used by implementing classes to
        /// specify a one-time initialization process before <see cref="Serialize"/> is called for the first
        /// time.
        /// </remarks>
        /// <returns>A byte representation of this object's initialization state.</returns>
        public abstract byte[] SerializeOnSpawn();

        /// <summary>
        /// Applies a serialized initialization state on this object.
        /// </summary>
        /// <remarks>
        /// Gets called on the <see cref="SpawningClient"/> once when the initial state is received from the server.
        /// Can be used by implementing classes to specify a one-time initialization process before
        /// <see cref="Deserialize"/> is called for the first time.
        /// </remarks>
        /// <param name="data">A byte representation of this object's initialization state.</param>
        public abstract void DeserializeOnSpawn(byte[] data);

        /// <summary>
        /// Gets called after the object is initialized.
        /// </summary>
        /// <remarks>
        /// May be overriden by derived classes if they wish to be notified when the object is finished initializing.
        /// Gets called after <see cref="SerializeOnSpawn"/> and <see cref="Serialize"/> on the server or after
        /// <see cref="DeserializeOnSpawn"/> and <see cref="Deserialize"/> on a client.
        /// </remarks>
        protected internal virtual void OnNetworkStart() { }
    }
}
