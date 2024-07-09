using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetLib.Messaging;
using NetLib.Serialization;
using UnityEngine;

namespace NetLib.Spawning
{
    /// <summary>
    /// Base class for <see cref="SpawningServer"/> and <see cref="SpawningClient"/>.
    /// </summary>
    public abstract class SpawningBase
    {
        /// <summary>
        /// Prefabs to be spawned across the network.
        /// </summary>
        /// <remarks>
        /// Must be set in order to allow objects to be spawned dynamically from prefabs.
        /// </remarks>
        public List<GameObject> RegisteredPrefabs { get; set; }

        /// <summary>
        /// Collection of networked objects managed by the spawning system.
        /// </summary>
        protected readonly Dictionary<ulong, NetworkObjectBase> NetworkObjects;

        /// <summary>
        /// The message system instance used for sending and receiving messages.
        /// </summary>
        protected readonly MessageSystem MessageSystem;

        /// <summary>
        /// Constructs a new <see cref="SpawningBase"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">THe <c>messageSystem</c> is null</exception>
        protected SpawningBase(MessageSystem messageSystem)
        {
            MessageSystem = messageSystem ??
                            throw new ArgumentNullException(nameof(messageSystem), "message system cannot be null");

            NetworkObjects = new Dictionary<ulong, NetworkObjectBase>();
        }

        /// <summary>
        /// Initializes the scene for synchronization.
        /// Must be called when there are network objects in the scene on startup.
        /// </summary>
        public abstract void InitializeScene();

        /// <summary>
        /// Gets the first prefab matching a given prefab hash from the list of registered prefabs.
        /// </summary>
        /// <param name="hash">The prefab hash to compare to.</param>
        /// <returns>The first found prefab with the given hash.</returns>
        public GameObject GetPrefabFromHash(string hash) =>
            RegisteredPrefabs.FirstOrDefault(x => x.GetComponent<NetworkObjectBase>().PrefabHash == hash);

        /// <summary>
        /// Gets the GameObject with a given id.
        /// </summary>
        /// <param name="uuid">The <see cref="NetworkObjectBase.Uuid"/> of the searched object.</param>
        /// <returns>The found object or null if no object with the id was found.</returns>
        public GameObject GetGameObjectFromUuid(ulong uuid) =>
            NetworkObjects.TryGetValue(uuid, out var obj) ? obj.gameObject : null;

        /// <summary>
        /// Gets the <see cref="NetworkObjectBase"/> with a given id.
        /// </summary>
        /// <param name="uuid">The <see cref="NetworkObjectBase.Uuid"/> of the searched object.</param>
        /// <returns>The found object or null if no object with the id was found.</returns>
        public NetworkObjectBase GetNetworkObjectFromUuid(ulong uuid) =>
            NetworkObjects.TryGetValue(uuid, out var obj) ? obj : null;

        /// <summary>
        /// Calls <see cref="NetworkObjectBase.OnNetworkStart"/> on every spawned <see cref="NetworkObjectBase"/>.
        /// </summary>
        public void OnNetworkStart() =>
            NetworkObjects.ToList().ForEach(x => x.Value.OnNetworkStart());

        /// <summary>
        /// Traverses the hierarchy of a GameObject and invokes a function for each <see cref="NetworkObjectBase"/>
        /// component found.
        /// </summary>
        /// <remarks>
        /// The function is invoked for a node before the traversal continues.
        /// The GameObject is traversed depth-first.
        /// </remarks>
        /// <param name="obj">The starting node.</param>
        /// <param name="func">The function to invoke.</param>
        public static void ForEachNetworkObjectPreOrder(GameObject obj, Action<NetworkObjectBase> func)
        {
            var networkObject = obj.GetComponent<NetworkObjectBase>();
            if (networkObject != null)
                func.Invoke(networkObject);

            foreach (Transform child in obj.transform)
            {
                ForEachNetworkObjectPreOrder(child.gameObject, func);
            }
        }

        /// <summary>
        /// Traverses the hierarchy of a GameObject and invokes a function for each <see cref="NetworkObjectBase"/>
        /// component found.
        /// </summary>
        /// <remarks>
        /// The function is invoked for a node after the traversal of its children is complete.
        /// The GameObject is traversed depth-first.
        /// </remarks>
        /// <param name="obj">the starting node</param>
        /// <param name="func">the function to invoke</param>
        public static void ForEachNetworkObjectPostOrder(GameObject obj, Action<NetworkObjectBase> func)
        {
            foreach (Transform child in obj.transform)
            {
                ForEachNetworkObjectPostOrder(child.gameObject, func);
            }

            var networkObject = obj.GetComponent<NetworkObjectBase>();
            if (networkObject != null)
                func.Invoke(networkObject);
        }

        /// <summary>
        /// Recursively checks if a given object or any parent is a player object.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if any object or parent is a player object, false otherwise.</returns>
        protected static bool IsObjectOrParentPlayer(NetworkObjectBase obj)
        {
            if (obj == null)
                return false;

            if (obj.transform.parent == null)
                return obj.IsPlayer;

            return obj.IsPlayer || IsObjectOrParentPlayer(obj.transform.parent.GetComponent<NetworkObjectBase>());
        }

        //-------------------------------------------------------------------------------------------------------------

        [Serializable]
        protected struct SpawnMsg : IBinarySerializable
        {
            internal enum ObjectType : byte
            {
                SceneObject,
                PrefabObject,
                PlayerObject
            }

            internal ObjectType SpawnType;
            internal ulong Uuid;
            internal string PrefabHash;
            internal ulong Parent;
            // data for object and its children
            internal Queue<byte[]> SpawnData;
            internal Queue<byte[]> InitData;


            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write((byte)SpawnType);
                writer.Write(Uuid);
                writer.Write(PrefabHash);
                writer.Write(Parent);

                writer.Write(SpawnData.Count);
                foreach (var i in SpawnData)
                {
                    writer.Write(i.Length);
                    writer.Write(i);
                }

                writer.Write(InitData.Count);
                foreach (var i in InitData)
                {
                    writer.Write(i.Length);
                    writer.Write(i);
                }
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                SpawnType = (ObjectType)reader.ReadByte();
                Uuid = reader.ReadUInt64();
                PrefabHash = reader.ReadString();
                Parent = reader.ReadUInt64();

                int sizeSpawnData = reader.ReadInt32();
                SpawnData = new Queue<byte[]>(sizeSpawnData);
                for (int i = 0; i < sizeSpawnData; i++)
                {
                    int len = reader.ReadInt32();
                    var arr = reader.ReadBytes(len);
                    SpawnData.Enqueue(arr);
                }

                int sizeInitData = reader.ReadInt32();
                InitData = new Queue<byte[]>(sizeInitData);
                for (int i = 0; i < sizeInitData; i++)
                {
                    int len = reader.ReadInt32();
                    var arr = reader.ReadBytes(len);
                    InitData.Enqueue(arr);
                }
            }
        }

        [Serializable]
        protected struct DestroyMsg : IBinarySerializable
        {
            internal ulong Uuid;

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(Uuid);
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);
                Uuid = reader.ReadUInt64();
            }
        }

        [Serializable]
        protected struct InitialState : IBinarySerializable
        {
            internal List<SpawnMsg> Objects;


            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(Objects.Count);
                foreach (var i in Objects)
                {
                    i.Serialize(stream);
                }
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                int size = reader.ReadInt32();
                Objects = new List<SpawnMsg>(size);
                for (int i = 0; i < size; i++)
                {
                    var obj = new SpawnMsg();
                    obj.Deserialize(stream);
                    Objects.Add(obj);
                }
            }
        }

        [Serializable]
        protected struct StateUpdate : IBinarySerializable
        {
            internal Dictionary<ulong, byte[]> Objects;
            internal List<SpawnMsg> PendingSpawnedObjects;
            internal List<DestroyMsg> PendingDestroyedObjects;


            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(Objects.Count);
                foreach (var i in Objects)
                {
                    writer.Write(i.Key);
                    writer.Write(i.Value.Length);
                    writer.Write(i.Value);
                }

                writer.Write(PendingSpawnedObjects.Count);
                foreach (var i in PendingSpawnedObjects)
                {
                    i.Serialize(stream);
                }

                writer.Write(PendingDestroyedObjects.Count);
                foreach (var i in PendingDestroyedObjects)
                {
                    i.Serialize(stream);
                }
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                Objects = new Dictionary<ulong, byte[]>();
                PendingSpawnedObjects = new List<SpawnMsg>();
                PendingDestroyedObjects = new List<DestroyMsg>();

                int size = reader.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    Objects.Add(reader.ReadUInt64(), reader.ReadBytes(reader.ReadInt32()));
                }

                size = reader.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    var obj = new SpawnMsg();
                    obj.Deserialize(stream);
                    PendingSpawnedObjects.Add(obj);
                }

                size = reader.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    var obj = new DestroyMsg();
                    obj.Deserialize(stream);
                    PendingDestroyedObjects.Add(obj);
                }
            }
        }
    }
}
