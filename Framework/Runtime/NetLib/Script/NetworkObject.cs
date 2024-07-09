using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetLib.Messaging;
using NetLib.Serialization;
using NetLib.Spawning;
using NetLib.Utils;
using UnityEngine;
using Logger = NetLib.Utils.Logger;

namespace NetLib.Script
{
    /// <summary>
    /// Unity component for synchronizing networked GameObjects.
    /// </summary>
    /// <remarks>
    /// The position and rotation of the <see cref="GameObject"/> as well as <see cref="NetworkVar.INetworkVar"/>
    /// variables of attached <see cref="NetworkBehaviour"/> scripts can be synchronized automatically.
    /// <para>
    /// The position can be synchronized in three different ways, namely as the local position of the object in
    /// relation to its parent, as the global position in world space, or as the relative position to another object
    /// in the scene.
    /// </para>
    /// <para>
    /// When using an update interval in the <see cref="NetworkManager"/>, the position and rotation may also be
    /// interpolated on the client. In that case a new position or rotation is not directly applied but rather
    /// interpolated until the next update is received.
    /// </para>
    /// <para>
    /// When using this script on prefabs that should be spawned dynamically, a hash for that prefab must be specified.
    /// </para>
    /// </remarks>
    [AddComponentMenu("NetLib/NetworkObject")]
    [DisallowMultipleComponent]
    public class NetworkObject : NetworkObjectBase
    {
        [Header("Synchronization Options")]

        [Tooltip("Synchronize the position across all clients")]
        public bool syncPosition = true;
        [Tooltip("Synchronize the rotation across all clients")]
        public bool syncRotation = true;
        [Tooltip("Synchronize NetworkBehaviors across all clients")]
        public bool syncVars = true;

        [Header("Lerping Options")] 
        
        [Tooltip("Interpolates the position between received state updates.")]
        public bool lerpPosition;
        [Tooltip("Interpolates the rotation between received state updates")]
        public bool lerpRotation;

        [Header("Dynamic Spawning Settings")]

        [Tooltip("Hash for this Prefab. Must be unique across all networked Prefabs. Can be ignored for non-prefab GameObjects")]
        public string prefabHash = "";

        [Header("Position Syncing Options")] 
        
        [Tooltip("Specifies how the position is synchronized. " +
                 "'Local Position' uses the position relative to the parent object. " +
                 "'Global Position' uses the position in world space coordinates. " +
                 "'Relative Position' uses the position relative to the object specified in 'Anchor'.")]
        public PositionSyncOption positionSyncMode = PositionSyncOption.LocalPosition;

        [Tooltip("The GameObject used for relative position syncing.")]
        public string anchor = "";


        /// <inheritdoc/>
        public override int SceneOrderIndex => sceneIndex;

        /// <inheritdoc/>
        public override string PrefabHash
        {
            get => prefabHash;
            set => prefabHash = value;
        }

        /// <inheritdoc/>
        public override bool IsPlayer => false;
        

        /// <summary>
        /// True if this object has an owner.
        /// </summary>
        public bool IsOwned { get; internal set; }

        /// <summary>
        /// Connection id of this object's owner, or 0 if it has no owner.
        /// </summary>
        public ulong Owner { get; internal set; }

        /// <summary>
        /// Connection id of the server or client, on which this instance is running.
        /// </summary>
        public ulong ConnectionId { get; internal set; }

        /// <summary>
        /// Delay used for interpolation. Should match the update interval of the <see cref="NetworkManager"/>.
        /// </summary>
        private float lerpDelay;

        /// <summary>
        /// Reference to the <see cref="NetworkManager"/> which initialized this object.
        /// </summary>
        protected NetworkManager networkManager;

        /// <summary>
        /// Ordering index for scene objects. Set automatically on build.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private int sceneIndex;

        // MessageSystem for sending rpc calls between NetworkBehaviours
        private MessageSystem messageSystem;
        // list of NetworkBehaviours on this object
        private List<NetworkBehaviour> networkBehaviours = new List<NetworkBehaviour>();

        private bool isMarkedDirty;

        // caching the transform improves performance
        private Transform cachedTransformBackingField;
        private Transform CachedTransform
        {
            get
            {
                if (cachedTransformBackingField == null)
                    cachedTransformBackingField = gameObject.transform;
                return cachedTransformBackingField;
            }
        }

        private Transform anchorTransformBackingField;
        private Transform AnchorTransform
        {
            get
            {
                if (anchorTransformBackingField == null)
                    SetupAnchor();
                return anchorTransformBackingField;
            }
        }


        private readonly TransformWrapper lastState = new TransformWrapper();

        private readonly TransformWrapper lerpStart = new TransformWrapper();
        private readonly TransformWrapper lerpTarget = new TransformWrapper();
        private float lerpElapsedTime;
        private bool doLerp;
        
        private Vector3 CurrentPosition
        {
            get
            {
                switch (positionSyncMode)
                {
                    case PositionSyncOption.LocalPosition:
                        return CachedTransform.localPosition;
                    case PositionSyncOption.GlobalPosition:
                        return CachedTransform.position;
                    case PositionSyncOption.RelativePosition:
                    {
                        var pos = AnchorTransform.position;
                        return CachedTransform.position - pos;
                    }
                    default: return Vector3.zero;
                }
            }
            set
            {
                switch (positionSyncMode)
                {
                    case PositionSyncOption.LocalPosition:
                        CachedTransform.localPosition = value;
                        break;
                    case PositionSyncOption.GlobalPosition:
                        CachedTransform.position = value;
                        break;
                    case PositionSyncOption.RelativePosition:
                    {
                        var pos = AnchorTransform.position;
                        CachedTransform.position = pos + value;
                        break;
                    }
                }
            }
        }


        /// <inheritdoc/>
        public override bool IsDirty()
        {
            if (isMarkedDirty)
                return true;

            return (syncPosition && lastState.Position != CurrentPosition) ||
                   (syncRotation && lastState.Rotation != CachedTransform.rotation) ||
                   (syncVars && networkBehaviours.Exists(x => x.IsDirty()));
        }

        /// <inheritdoc/>
        public override void MarkDirty()
        {
            isMarkedDirty = true;
        }

        /// <inheritdoc/>
        public override void ResetDirty()
        {
            isMarkedDirty = false;
            lastState.Position = CurrentPosition;
            lastState.Rotation = CachedTransform.rotation;
            foreach (var networkBehaviour in networkBehaviours)
            {
                networkBehaviour.ResetDirtyFlag();
            }
        }

        /// <inheritdoc/>
        public override byte[] SerializeOnSpawn()
        {
            var spawnState = new SpawnState()
            {
                Anchor = anchor,
                IsOwned = IsOwned,
                Owner = Owner,
                LerpDelay = lerpDelay,
                NBehaviours = networkBehaviours.Count,
                StartPosition = lastState.Position,
                StartRotation = lastState.Rotation
            };

            return Serializer.Serialize(spawnState);
        }

        /// <inheritdoc/>
        public override void DeserializeOnSpawn(byte[] data)
        {
            var spawnState = Serializer.Deserialize<SpawnState>(data);

            if (spawnState.NBehaviours != networkBehaviours.Count)
            {
                Logger.LogError("NetworkObject", "NetworkBehaviour mismatch found - " +
                                                 "server and client instances do not have the same amount " +
                                                 "of NetworkBehaviours on the GameObject!");
            }

            anchor = spawnState.Anchor;
            IsOwned = spawnState.IsOwned;
            Owner = spawnState.Owner;
            lerpDelay = spawnState.LerpDelay;
            CurrentPosition = spawnState.StartPosition;
            lerpTarget.Position = spawnState.StartPosition;
            CachedTransform.rotation = spawnState.StartRotation;
            lerpTarget.Rotation = spawnState.StartRotation;
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
            var state = new State
            {
                Position = CurrentPosition,
                Rotation = CachedTransform.rotation,
                NetworkBehaviourData = new Dictionary<int, byte[]>()
            };

            for (int i = 0; i < networkBehaviours.Count; ++i)
            {
                state.NetworkBehaviourData.Add(i, networkBehaviours[i].Serialize());
            }

            return Serializer.Serialize(state);
        }

        /// <inheritdoc/>
        public override void Deserialize(byte[] data)
        {
            var state = Serializer.Deserialize<State>(data);

            if (syncPosition)
            {
                if (lerpPosition)
                {
                    lerpStart.Position = lerpTarget.Position;
                    lerpTarget.Position = state.Position;
                    doLerp = true;
                    lerpElapsedTime = 0f;
                }
                else
                {
                    CurrentPosition = state.Position;
                }
            }

            if (syncRotation)
            {
                if (lerpRotation)
                {
                    lerpStart.Rotation = lerpTarget.Rotation;
                    lerpTarget.Rotation = state.Rotation;
                    doLerp = true;
                    lerpElapsedTime = 0f;
                }
                else
                {
                    CachedTransform.rotation = state.Rotation;
                }
            }

            if (syncVars)
            {
                foreach (var keyValuePair in state.NetworkBehaviourData)
                {
                    networkBehaviours[keyValuePair.Key].Deserialize(keyValuePair.Value);
                }
            }
        }

        /// <summary>
        /// Initializes this NetworkObject for use in the framework.
        /// </summary>
        /// <param name="nm">The <see cref="NetworkManager"/> which initializes this object.</param>
        /// <param name="ms">The message system this object should use internally.</param>
        /// <param name="interpolationDelay">The delay to use when interpolating.</param>
        /// <exception cref="ArgumentNullException">The network manager is null</exception>
        /// <exception cref="ArgumentNullException">The message system is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">The interpolation delay is less than zero</exception>
        public void Initialize(NetworkManager nm, MessageSystem ms, float interpolationDelay)
        {
            if (nm == null)
                throw new ArgumentNullException(nameof(nm), "network manager cannot be null");

            messageSystem = ms ??
                            throw new ArgumentNullException(nameof(ms), "message system cannot be null");

            if (interpolationDelay < 0)
                throw new ArgumentOutOfRangeException(nameof(interpolationDelay), "interpolation delay cannot be less than zero");

            lerpDelay = interpolationDelay;
            networkManager = nm;
            
            messageSystem.AddListener((byte)Constants.InternalMessageType.Rpc, Uuid, (sender, data) =>
            {
                var msg = Serializer.Deserialize<RpcMsg>(data);
                networkBehaviours[msg.BehaviourIndex].ReceiveRpcMessage(msg.Data);
            });

            // GetComponents order is stable
            networkBehaviours = gameObject.GetComponents<NetworkBehaviour>().ToList();
            for (int i = 0; i < networkBehaviours.Count; i++)
            {
                var nb = networkBehaviours[i];
                nb.Index = i;

                nb.Initialize();
            }
        }

        // can be called to trigger OnNetworkStart on all NetworkBehaviours
        protected internal override void OnNetworkStart()
        {
            networkBehaviours.ForEach(x => x.OnNetworkStart());
        }

        // Called by NetworkBehaviour to send an rpc call through the MessageSystem
        internal void SendServerRpc(int behaviourIndex, byte[] rpcData)
        {
            var msg = new RpcMsg()
            {
                BehaviourIndex = behaviourIndex,
                Data = rpcData
            };
            messageSystem.Send(0, (byte)Constants.InternalMessageType.Rpc, Uuid, Serializer.Serialize(msg));
        }
        // Called by NetworkBehaviour to send an rpc call through the MessageSystem
        internal void SendClientRpc(int behaviourIndex, byte[] rpcData, ulong receiver)
        {
            var msg = new RpcMsg()
            {
                BehaviourIndex = behaviourIndex,
                Data = rpcData
            };
            messageSystem.Send(receiver, (byte)Constants.InternalMessageType.Rpc, Uuid, Serializer.Serialize(msg));
        }
        // Called by NetworkBehaviour to send an rpc call through the MessageSystem
        internal void SendClientRpcToAll(int behaviourIndex, byte[] rpcData)
        {
            var msg = new RpcMsg()
            {
                BehaviourIndex = behaviourIndex,
                Data = rpcData
            };
            networkManager.SendToAll((byte)Constants.InternalMessageType.Rpc, Uuid, Serializer.Serialize(msg));
        }


        protected virtual void Awake()
        {
            lastState.Position = CurrentPosition;
            lastState.Rotation = CachedTransform.rotation;
        }

        private void Update()
        {
            if (doLerp)
                lerpElapsedTime += Time.deltaTime;

            if (doLerp && lerpPosition)
            {
                CurrentPosition = Vector3.Lerp(lerpStart.Position, lerpTarget.Position, lerpElapsedTime / lerpDelay);
            }

            if (doLerp && lerpRotation)
            {
                CachedTransform.rotation = Quaternion.Slerp(lerpStart.Rotation, lerpTarget.Rotation, lerpElapsedTime / lerpDelay);
            }

            if (lerpElapsedTime > lerpDelay)
                doLerp = false;
        }

        private void OnEnable()
        {
            lastState.Position = CurrentPosition;
            lastState.Rotation = CachedTransform.rotation;
        }

        private void OnValidate()
        {
            sceneIndex = transform.GetSiblingIndex();
        }

        private void SetupAnchor()
        {
            if (string.IsNullOrEmpty(anchor))
            {
                Logger.LogError("NetworkObject", "Using relative positioning but no anchor was specified");
                return;
            }

            var anchorObject = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == anchor);
            if (anchorObject != null)
                anchorTransformBackingField = anchorObject.transform;
            else
                Logger.LogError("NetworkObject", "Using relative positioning but could not find anchor with name " + anchor);
        }

        /// <summary>
        /// Options when synchronizing the object's position.
        /// </summary>
        public enum PositionSyncOption
        {
            LocalPosition,
            GlobalPosition,
            RelativePosition
        }

        private class TransformWrapper
        {
            internal Vector3 Position;
            internal Quaternion Rotation;
        }

        [Serializable]
        private struct SpawnState : IBinarySerializable
        {
            internal string Anchor;
            internal bool IsOwned;
            internal ulong Owner;
            internal float LerpDelay;
            internal int NBehaviours;
            internal Vector3 StartPosition;
            internal Quaternion StartRotation;

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(Anchor);
                writer.Write(IsOwned);
                writer.Write(Owner);
                writer.Write(LerpDelay);
                writer.Write(NBehaviours);

                writer.Write(StartPosition.x);
                writer.Write(StartPosition.y);
                writer.Write(StartPosition.z);
                writer.Write(StartRotation.x);

                writer.Write(StartRotation.y);
                writer.Write(StartRotation.z);
                writer.Write(StartRotation.w);
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                Anchor = reader.ReadString();
                IsOwned = reader.ReadBoolean();
                Owner = reader.ReadUInt64();
                LerpDelay = reader.ReadSingle();
                NBehaviours = reader.ReadInt32();

                StartPosition.x = reader.ReadSingle();
                StartPosition.y = reader.ReadSingle();
                StartPosition.z = reader.ReadSingle();

                StartRotation.x = reader.ReadSingle();
                StartRotation.y = reader.ReadSingle();
                StartRotation.z = reader.ReadSingle();
                StartRotation.w = reader.ReadSingle();
            }
        }

        [Serializable]
        private struct State : IBinarySerializable
        {
            internal Vector3 Position;
            internal Quaternion Rotation;
            // <int, byte[]> = <list index, serialized data>
            internal Dictionary<int, byte[]> NetworkBehaviourData;

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(Position.x);
                writer.Write(Position.y);
                writer.Write(Position.z);

                writer.Write(Rotation.x);
                writer.Write(Rotation.y);
                writer.Write(Rotation.z);
                writer.Write(Rotation.w);

                writer.Write(NetworkBehaviourData.Count);
                foreach (var keyValuePair in NetworkBehaviourData)
                {
                    writer.Write(keyValuePair.Key);
                    writer.Write(keyValuePair.Value.Length);
                    writer.Write(keyValuePair.Value);
                }
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                Position.x = reader.ReadSingle();
                Position.y = reader.ReadSingle();
                Position.z = reader.ReadSingle();

                Rotation.x = reader.ReadSingle();
                Rotation.y = reader.ReadSingle();
                Rotation.z = reader.ReadSingle();
                Rotation.w = reader.ReadSingle();

                int size = reader.ReadInt32();
                NetworkBehaviourData = new Dictionary<int, byte[]>(size);
                for (int i = 0; i < size; i++)
                {
                    int key = reader.ReadInt32();
                    int dataSize = reader.ReadInt32();
                    var data = reader.ReadBytes(dataSize);
                    NetworkBehaviourData.Add(key, data);
                }
            }
        }

        [Serializable]
        private struct RpcMsg : IBinarySerializable
        {
            internal int BehaviourIndex;
            internal byte[] Data;

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(BehaviourIndex);
                writer.Write(Data.Length);
                writer.Write(Data);
            }

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                BehaviourIndex = reader.ReadInt32();
                int size = reader.ReadInt32();
                Data = reader.ReadBytes(size);
            }
        }
    }
}
