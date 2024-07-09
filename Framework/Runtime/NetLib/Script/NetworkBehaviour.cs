using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NetLib.Extensions;
using NetLib.NetworkVar;
using NetLib.Script.Rpc;
using NetLib.Serialization;
using UnityEngine;

namespace NetLib.Script
{
    /// <summary>
    /// Base class for <see cref="MonoBehaviour"/> scripts with network functionality.
    /// </summary>
    /// <remarks>
    /// Can be inherited by scripts to gain access to synchronized script variables and remote calling of script
    /// methods.
    /// The <see cref="GameObject"/> on which this <see cref="NetworkBehaviour"/> is attached also needs a
    /// <see cref="NetworkObject"/> component.
    /// </remarks>
    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        /// <summary>
        /// True if this NetworkBehaviour instance is running on a server, false otherwise.
        /// </summary>
        protected bool IsServer => parent != null && parent.IsServer;

        /// <summary>
        /// True if this NetworkBehaviour instance is running on a client, false otherwise.
        /// </summary>
        protected bool IsClient => parent != null && parent.IsClient;

        /// <summary>
        /// Connection id of the server or client on which this script is running.
        /// </summary>
        protected ulong ConnectionId => parent.ConnectionId;

        // index of this behaviour in the object component hierarchy
        internal int Index;

        // NetworkObject which manages this behaviour
        private NetworkObject parent;
        // NetworkVars and their identifiers on inheriting scripts - <identifier, object>
        private Dictionary<string, INetworkVar> networkVars;
        // rpc methods on inheriting scripts - <method signature hash, method object>
        private Dictionary<uint, MethodInfo> rpcMethods;


        /// <summary>
        /// Registers NetworkVars and RpcMethods on this script..
        /// </summary>
        /// <remarks>
        /// This must be called whenever a NetworkVar variable on this NetworkBehaviour is reassigned to a new Object.
        /// </remarks>
        public void Initialize()
        {
            parent = GetComponent<NetworkObject>();
            if (parent == null)
            {
                Utils.Logger.LogError("NetworkBehaviour", "No NetworkObject found on this GameObject");
            }

            // initialize networked variables
            networkVars = new Dictionary<string, INetworkVar>();
            CollectNetworkVars();

            // initialize rpc methods
            rpcMethods = new Dictionary<uint, MethodInfo>();
            CollectRpcMethods();
        }

        /// <summary>
        /// Returns true if the state of this NetworkBehaviour has changed since the last call to
        /// <see cref="Serialize"/>.
        /// </summary>
        /// <returns>True if the state has changed, false otherwise.</returns>
        public virtual bool IsDirty() => networkVars.Any(x => x.Value.IsDirty);

        /// <summary>
        /// Marks all <see cref="NetworkVar{T}"/> objects on this script as non-dirty, regardless of their current
        /// state.
        /// </summary>
        public virtual void ResetDirtyFlag()
        {
            foreach (var entry in networkVars)
            {
                entry.Value.ResetDirty();
            }
        }

        /// <summary>
        /// Returns the serialized state of this object containing all NetworkVars.
        /// </summary>
        /// <returns>The serialized state.</returns>
        public virtual byte[] Serialize()
        {
            var state = new StateMessage()
            {
                NetworkVars = new Dictionary<string, byte[]>()
            };

            foreach (var entry in networkVars)
            {
                if (entry.Value == null)
                {
                    Utils.Logger.LogWarning("NetworkBehaviour", $"Uninitialized NetworkVar with identifier {entry.Key} found! " +
                                                                $"Make sure to initialize the object at its definition and to not reassign the identifier.");
                    continue;
                }

                entry.Value.ReadValue(out var data);
                state.NetworkVars.Add(entry.Key, data);
            }

            return Serializer.Serialize(state);
        }

        /// <summary>
        /// Applies a serialized state on this object.
        /// </summary>
        /// <remarks>
        /// Updates all <see cref="NetworkVar{T}"/> objects with the given data.
        /// The state data must come from a call to <see cref="Serialize"/>.
        /// </remarks>
        /// <param name="data">The state which to apply.</param>
        public virtual void Deserialize(byte[] data)
        {
            var state = Serializer.Deserialize<StateMessage>(data);

            foreach (var entry in state.NetworkVars)
            {
                if (networkVars[entry.Key] == null)
                {
                    Utils.Logger.LogWarning("NetworkBehaviour", $"Uninitialized NetworkVar with identifier {entry.Key} found! " +
                                                                $"Make sure to initialize the object at its definition and to not reassign the identifier.");
                    continue;
                }

                networkVars[entry.Key].WriteValue(entry.Value);
            }
        }

        /// <summary>
        /// Gets called when this <see cref="NetworkBehaviour"/> gets initialized by the networking system.
        /// </summary>
        /// <remarks>
        /// Can be overriden by deriving classes to implement logic when an object is spawned and ready across the
        /// network.
        /// </remarks>
        protected internal virtual void OnNetworkStart() { }
        

        // gets called by NetworkObject when a rpc call is received
        internal void ReceiveRpcMessage(byte[] rpcData)
        {
            var msg = Serializer.Deserialize<RpcMessage>(rpcData);

            if (rpcMethods.TryGetValue(msg.MethodSignature, out var method))
            {
                method.Invoke(this, msg.Parameters.ToArray());
            }
        }

        private static bool HasInterface(Type objectType, Type interfaceType) =>
            objectType.GetInterfaces().Any(x => x == interfaceType);

        private void CollectNetworkVars()
        {
            var fields = GetType().GetFields();

            // filter NetworkVars from all fields of behaviour
            foreach (var fieldInfo in fields.ToList().Where(f => HasInterface(f.FieldType, typeof(INetworkVar))))
            {
                networkVars.Add(fieldInfo.Name, fieldInfo.GetValue(this) as INetworkVar);
            }
        }

        private void CollectRpcMethods()
        {
            foreach (var method in GetType().GetMethods(BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance | 
                                                        BindingFlags.Static))
            {
                if (!IsServerRpc(method) && !IsClientRpc(method)) 
                    continue;

                uint signatureHash = method.GetMethodSignatureHash();
                if (rpcMethods.ContainsKey(signatureHash))
                {
                    Utils.Logger.LogError("NetworkBehaviour", $"Conflicting rpc methods: {method.Name} and {rpcMethods[signatureHash].Name}. Try changing the method signature.");
                    continue;
                }
                    
                rpcMethods.Add(method.GetMethodSignatureHash(), method);
            }
        }

        private static bool IsServerRpc(MethodInfo method) =>
            method.GetCustomAttributes(typeof(ServerRpcAttribute), false).Length == 1;

        private static bool IsClientRpc(MethodInfo method) =>
            method.GetCustomAttributes(typeof(ClientRpcAttribute), false).Length == 1;


        [Serializable]
        private struct RpcMessage : IBinarySerializable
        {
            internal uint MethodSignature;
            internal List<object> Parameters;

            /// <inheritdoc/>
            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(MethodSignature);
                writer.Write(Parameters.Count);
                foreach (var parameter in Parameters)
                {
                    if (parameter != null)
                    {
                        var data = Serializer.Serialize(parameter, true);
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                    else
                    {
                        writer.Write(0);
                    }
                }
            }

            /// <inheritdoc/>
            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                MethodSignature = reader.ReadUInt32();
                int nParameters = reader.ReadInt32();

                Parameters = new List<object>(nParameters);
                for (int i = 0; i < nParameters; i++)
                {
                    int dataSize = reader.ReadInt32();
                    if (dataSize > 0)
                    {
                        var data = reader.ReadBytes(dataSize);
                        Parameters.Add(Serializer.Deserialize<object>(data));
                    }
                    else
                    {
                        Parameters.Add(null);
                    }
                }
            }
        }

        [Serializable]
        private struct StateMessage : IBinarySerializable
        {
            internal Dictionary<string, byte[]> NetworkVars;

            /// <inheritdoc/>
            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);

                writer.Write(NetworkVars.Count);
                foreach (var keyValuePair in NetworkVars)
                {
                    writer.Write(keyValuePair.Key);
                    writer.Write(keyValuePair.Value.Length);
                    writer.Write(keyValuePair.Value);
                }
            }

            /// <inheritdoc/>
            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);

                NetworkVars = new Dictionary<string, byte[]>();

                int size = reader.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    string key = reader.ReadString();
                    int count = reader.ReadInt32();
                    var bytes = reader.ReadBytes(count);
                    NetworkVars.Add(key, bytes);
                }
            }
        }
    }
}
