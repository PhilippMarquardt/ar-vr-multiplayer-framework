using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetLib.Extensions;
using NetLib.Messaging;
using NetLib.Serialization;
using NetLib.Spawning;
using NetLib.Transport;
using NetLib.Transport.LiteNetLib;
using NetLib.Transport.Telepathy;
using UnityEngine;

namespace NetLib.Script
{
    /// <summary>
    /// Base class for sending custom messages through the <see cref="NetworkManager"/>.
    /// </summary>
    /// <remarks>
    /// This class can be extended in order to specify custom message types when using
    /// <see cref="NetworkManager.SendCustomMessage"/>.
    /// </remarks>
    [Serializable]
    public class CustomMessage { }

    /// <summary>
    /// Delegate for receiving custom messages.
    /// </summary>
    /// <param name="msg">The received message.</param>
    public delegate void OnCustomMessageDelegate(CustomMessage msg);

    /// <summary>
    /// Script for managing all network related operations in the scene.
    /// </summary>
    /// <remarks>
    /// The <see cref="NetworkManager"/> can be used as either a server or a client.
    /// </remarks>
    [AddComponentMenu("NetLib/NetworkManager")]
    public partial class NetworkManager : MonoBehaviour
    {
        [Header("Quick Setup Options")]

        [Tooltip("Port to be used for the connection.")]
        public ushort defaultPort;
        [Tooltip("Ip to be used for connecting to a server.")]
        public string defaultIp;

        [Space]

        [Tooltip("Automatically starts this NetworkManager as a server when 'Auto Server' is selected or as a " +
                 "client when 'Auto Client' is selected. Uses default port and ip.")]
        public AutoStartOption autoStartOption = AutoStartOption.None;

        [Header("Runtime Options")]

        [Tooltip("Update interval in seconds. Determines how often the server sends state updates to clients.")]
        public float updateInterval;

        [Space]

        [Tooltip("Prefabs to be spawned across the network.")]
        public List<GameObject> registeredPrefabs;

        
        [Header("Player Spawning Settings")]

        [Tooltip("Type of hardware this NetworkManager runs on.")]
        public ClientType clientType;

        [Tooltip("If checked, the server will also be considered a player. If not checked the player options are " +
                 "ignored on the server.")]
        public bool serverIsPlayer;

        [Tooltip("If checked, the server will use the state of the client player object when spawning a player prefab. " +
                 "Otherwise the player object on the client will be reset to the state defined by the server " +
                 "(either the state of the prefab or the state which was manually set on the server after the prefab " +
                 "was spawned - see the OnPlayerSpawn delegate).")]
        public bool ignoreServerSpawnData;

        [Space]

        [Tooltip("The object which represents the player in the local scene if the game is run on an ar-device. " +
                 "Can be left empty if no player objects should be spawned for ar-devices.")]
        public GameObject arPlayerObject;

        [Tooltip("The prefab to be spawned globally when an ar-device connects. Leave empty if no player object " +
                 "should be spawned.")]
        public GameObject arPlayerPrefab;

        [Space]

        [Tooltip("The object which represents the player in the local scene if the game is run on a vr-device. " +
                 "Can be left empty if no player objects should be spawned for vr-devices.")]
        public GameObject vrPlayerObject;

        [Tooltip("The prefab to be spawned globally when a vr-device connects. Leave empty if no player object " +
                 "should be spawned.")]
        public GameObject vrPlayerPrefab;

        [Space]

        [Tooltip("The object which represents the player in the local scene if the game is run on a pc-device. " +
                 "Can be left empty if no player objects should be spawned for pc-devices.")]
        public GameObject pcPlayerObject;

        [Tooltip("The prefab to be spawned globally when a pc-device connects. Leave empty if no player object " +
                 "should be spawned.")]
        public GameObject pcPlayerPrefab;

        [Header("Advanced Options")]

        [Tooltip("The transport protocol to use if none is specified at runtime.")]
        public TransportType defaultTransport = TransportType.Tcp;

        [Tooltip("Specifies which unity logs that the framework throws are shown.")]
        public Utils.Logger.LogLevel logVerbosity = Utils.Logger.LogLevel.Warning;


        /// <summary>
        /// Action to be called when a player prefab is spawned for a connecting client.
        /// </summary>
        /// <remarks>
        /// Subscribing to this delegate allows the user to modify a spawned player prefab before it is distributed
        /// to all other clients. The prefab will be instantiated and the <see cref="NetworkObject"/> initialized
        /// before this delegate is called. The delegate receives the initialized object, the connection id of the
        /// client to whom it belongs to and the type of the client.
        /// <para>
        /// If the delegate is not set, the prefab will simply be instantiated without any transform modifications,
        /// i.e. the spawned player object will have the position and rotation set in the prefab. The player object on
        /// the connecting client will then receive the prefab's state and apply it according to the synchronization
        /// options set in its <see cref="NetworkObject"/>.
        /// </para>
        /// <para>
        /// A possible use case for this function would be if a player should be positioned in a certain spot on a map
        /// when the client connects. You would set the position in the callback and then the player object on the
        /// client would also start in that position when the connection process finishes.
        /// </para>
        /// </remarks>
        [NonSerialized]
        public Action<GameObject, ulong, ClientType> OnPlayerSpawn;

        /// <summary>
        /// Delegate to be called when a CustomMessage is received.
        /// </summary>
        public OnCustomMessageDelegate OnCustomMessage { get; set; }

        /// <summary>
        /// True, if this NetworkManager is initialized, false otherwise.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// The underlying Transport used for connection handling and message sending.
        /// </summary>
        public ITransport Transport
        {
            get => transport;
            set
            {
                if (isClient || isServer)
                    Utils.Logger.LogWarning("NetworkManager", "Cannot set Transport when NetworkManager is already started");
                else
                    transport = value;
            }
        }

        /// <summary>
        /// Connection id of this NetworkManager.
        /// </summary>
        private ulong connectionId;

        private ITransport transport;
        private MessageSystem messageSystem;
        private const string messageSystemChannel = "NetLib";

        private bool isServer;
        private bool isClient;

        private readonly Utils.FileHandler fileHandler = new Utils.FileHandler();


        /// <summary>
        /// Stops the NetworkManager and allows it to be started again as server or client.
        /// </summary>
        public void Stop()
        {
            switch (transport)
            {
                case IServer server:
                    server.Stop();
                    break;
                case IClient client:
                    client.Disconnect();
                    break;
            }

            transport = null;
            isServer = false;
            isClient = false;
            IsRunning = false;
        }

        /// <summary>
        /// Spawns a prefab across the network. 
        /// </summary>
        /// <remarks>
        /// The prefab must be registered in the inspector.
        /// Can be called on both server and clients.
        /// If the prefab gets spawned with ownership the calling server or client will be the owner.
        /// <para>
        /// The prefab must have a <see cref="NetworkObject"/> component.
        /// Children of the prefab without a <see cref="NetworkObject"/> component are ignored.
        /// </para>
        /// <para>
        /// The prefab must be registered in <see cref="registeredPrefabs"/>.
        /// </para>
        /// </remarks>
        /// <param name="prefabHash">The hash of the prefab to spawn</param>
        /// <param name="owned">True if the spawned prefab should be owned by the caller, false otherwise.</param>
        /// <exception cref="ArgumentNullException">The <c>prefabHash</c> is null</exception>
        /// <exception cref="ArgumentException">No prefab with hash <c>prefabHash</c> can be found</exception>
        /// <exception cref="ArgumentException">The prefab does not have a <c>NetworkObject</c> component</exception>
        public void SpawnPrefab(string prefabHash, bool owned = false)
        {
            if (!IsRunning)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to spawn prefab, but NetworkManager is not running");
                return;
            }

            if (isServer)
            {
                spawningServer.SpawnNetworkObject(prefabHash, objBase =>
                {
                    var obj = (NetworkObject)objBase;
                    obj.IsOwned = owned;
                    obj.Owner = 0;
                    OnObjectSpawn(obj);
                });
            }
            else if (isClient)
            {
                var msg = new SpawnRequest()
                {
                    PrefabHash = prefabHash,
                    IsOwned = owned
                };
                messageSystem.Send(
                    0, 
                    (byte) Utils.Constants.InternalMessageType.SpawnRequest, 
                    Serializer.Serialize(msg));
            }
        }

        /// <summary>
        /// Spawns a locally instantiated object across the network.
        /// Can only be called on the server.
        /// </summary>
        /// <remarks>
        /// The object must have a <see cref="NetworkObject"/> component.
        /// Children of the object without a <see cref="NetworkObject"/> component are ignored.
        /// <para>
        /// The object must be instantiated from a prefab which is registered in
        /// <see cref="registeredPrefabs"/>.
        /// </para>
        /// </remarks>
        /// <param name="localObject">The object to spawn.</param>
        /// <exception cref="ArgumentNullException">The <c>localObject</c> is null</exception>
        /// <exception cref="ArgumentException">The <c>localObject</c> does not have a <c>NetworkObject</c> component</exception>
        public void SpawnLocalObject(GameObject localObject)
        {
            if (!IsRunning)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to spawn object, but NetworkManager is not running");
                return;
            }

            if (isClient)
            {
                Utils.Logger.LogError("NetworkManager", "Can only spawn local objects as server");
                return;
            }

            spawningServer.SpawnNetworkObject(localObject, OnObjectSpawn);
        }

        /// <summary>
        /// Destroys a given object across the network.
        /// Can be called on both server and clients.
        /// </summary>
        /// <remarks>
        /// The object must have been previously spawned in the system.
        /// The object's children will also be removed.
        /// The object will be removed from the synchronization process and destroyed on all connected clients.
        /// </remarks>
        /// <param name="obj">The object to destroy.</param>
        public void DestroyNetworkObject(GameObject obj)
        {
            if (!IsRunning)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to destroy object, but NetworkManager is not running");
                return;
            }

            if (isServer)
            {
                spawningServer.DeSpawnNetworkObject(obj);
            }
            else if (isClient)
            {
                var msg = new DestroyRequest()
                {
                    Uuid = obj.GetComponent<NetworkObject>().Uuid
                };
                messageSystem.Send(
                    0,
                    (byte)Utils.Constants.InternalMessageType.DestroyRequest,
                    Serializer.Serialize(msg));
            }
        }

        /// <summary>
        /// Applies the state of a client object to the corresponding server object.
        /// </summary>
        /// <remarks>
        /// The state is immediately sent to the server, where it will be distributed during the next update.
        /// Should only be called on a client, as state changes on the server are automatically synchronized.
        /// </remarks>
        /// <param name="networkObject">The object whose state should be updated.</param>
        public void ChangeObjectState(NetworkObject networkObject)
        {
            if (isServer)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to explicitly update object state on server");
                return;
            }

            if (!IsRunning)
                return;

            var msg = new ChangeRequest
            {
                Uuid = networkObject.Uuid,
                Data = networkObject.Serialize()
            };
            messageSystem.Send(
                0,
                (byte)Utils.Constants.InternalMessageType.ChangeRequest,
                Serializer.Serialize(msg));
        }

        /// <summary>
        /// Sends a file to the server or a client.
        /// </summary>
        /// <remarks>
        /// The file is read from disk and split into equal sized parts, which are then asynchronously sent
        /// individually to the receiver. This allows for sending larger files without clogging the network and the
        /// main thread.
        /// <para>
        /// When called on a client, the message can only be sent to the server and thus the receiver parameter is ignored.
        /// When called on the server the receiver is either the id of a client or 0, in which case the
        /// file will be sent to all connected clients.
        /// </para>
        /// </remarks>
        /// <param name="srcFilepath">The path of the file which to send.</param>
        /// <param name="destFilepath">The destination path on the receiver.</param>
        /// <param name="maxMessageSize">The maximum size of a single part of the file in bytes.</param>
        /// <param name="sendInterval">Interval in seconds in which individual parts of the file are sent.</param>
        /// <param name="receiver">0 for a broadcast, otherwise the connection id of the receiving client.</param>
        public void SendFile(string srcFilepath, string destFilepath, int maxMessageSize, float sendInterval, ulong receiver = 0)
        {
            // Serialize file if exist
            if (!File.Exists(srcFilepath))
            {
                Utils.Logger.LogError("NetworkManager", "Trying to send a file which does not exist");
                return;
            }

            var fileData = FileSerializer.Serialize(srcFilepath);
            var arrays = fileData.Split(maxMessageSize).ToList();

            int totalParts = arrays.Count;
            var fileMessages = new List<FileExchangeRequest>();
            string fileHash = Utils.HashCode.GetFileHash(srcFilepath);
            int i = 0;

            foreach(var array in arrays) 
            {
                var msg = new FileExchangeRequest
                {
                    CurrentMessagePart = i,
                    DestinationPath = destFilepath,
                    TotalMessageParts = totalParts,
                    FileBytes = array.ToArray(),
                    FileHash = fileHash
                };
                fileMessages.Add(msg);
                i++;
            }

            StartCoroutine(SendFileCoroutine(receiver, fileMessages, sendInterval));
        }

        private IEnumerator SendFileCoroutine(ulong receiver, List<FileExchangeRequest> messages, float sendInterval)
        {
            foreach (var msg in messages)
            {
                var data = Serializer.Serialize(msg);

                if (isServer)
                {
                    if (receiver > 0)
                        messageSystem.Send(receiver, (byte)Utils.Constants.InternalMessageType.FileMessage, data);
                    else
                        connectedClients.ForEach(c => messageSystem.Send(c, (byte)Utils.Constants.InternalMessageType.FileMessage, data));
                }
                else if (isClient)
                {
                    messageSystem.Send(0, (byte)Utils.Constants.InternalMessageType.FileMessage, data);
                }
                
                yield return new WaitForSeconds(sendInterval);
            }
        }

        /// <summary>
        /// Sends a custom message to the server or one or all clients. 
        /// </summary>
        /// <remarks>
        /// When called on a client, the message can only be sent to the server and thus the receiver parameter is
        /// ignored. When called on the server the receiver is either the id of a client or 0, in which case the
        /// message will be sent to all connected clients.
        /// </remarks>
        /// <param name="msg">The custom message to send.</param>
        /// <param name="receiver">0 for a broadcast, otherwise the connection id of the receiving client.</param>
        public void SendCustomMessage(CustomMessage msg, ulong receiver = 0)
        {
            if (!IsRunning)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to send a message, but NetworkManager is not running");
                return;
            }

            var data = Serializer.Serialize(msg);

            if (isServer)
            {
                if (receiver > 0)
                    messageSystem.Send(receiver, (byte)Utils.Constants.InternalMessageType.Custom, data);
                else
                    connectedClients.ForEach(c =>
                        messageSystem.Send(c, (byte)Utils.Constants.InternalMessageType.Custom, data));
            }
            else if (isClient)
            {
                messageSystem.Send(0, (byte)Utils.Constants.InternalMessageType.Custom, data);
            }
        }

        //-------------------------------------------------------------------------------------------------------------

        private void Awake()
        {
            Utils.Logger.Verbosity = logVerbosity;
        }

        private void Start()
        {
            switch (autoStartOption)
            {
                case AutoStartOption.Server:
                    StartServer();
                    break;
                case AutoStartOption.Client:
                    StartClient();
                    break;
                case AutoStartOption.None:
                    break;
                default:
                    Utils.Logger.LogError("NetworkManager", $"Unsupported auto start option {autoStartOption}");
                    break;
            }
        }

        private void Update()
        {
            if (isClient || isServer)
                transport.Poll();
        }

        private void OnDestroy()
        {
            Stop();
        }

        private void InitializeTransport(bool asServer)
        {
            if (transport != null)
                return;

            switch (defaultTransport)
            {
                case TransportType.Tcp:
                    transport = asServer ? (ITransport)
                        // ReSharper disable once RedundantArgumentDefaultValue
                        new TcpServer(int.MaxValue) :
                        // ReSharper disable once RedundantArgumentDefaultValue
                        new TcpClient(int.MaxValue);
                    break;
                case TransportType.ReliableUdp:
                    transport = asServer ? (ITransport)
                        new UdpServer(int.MaxValue) : new UdpClient();
                    break;
                default:
                    Utils.Logger.LogError("NetworkManager", $"Unsupported transport type: {defaultTransport}");
                    break;
            }
        }

        // Returns the player object set in the inspector for this instance.
        private GameObject GetPlayerObject()
        {
            switch (clientType)
            {
                case ClientType.ArClient:
                    return arPlayerObject;
                case ClientType.VrClient:
                    return vrPlayerObject;
                case ClientType.DesktopClient:
                    return pcPlayerObject;
                default:
                    Utils.Logger.LogError("NetworkManager", $"Unsupported client type: {clientType}");
                    return null;
            }
        }

        // Returns the player prefab set in the inspector for a given client type.
        private GameObject GetPlayerPrefab(ClientType type)
        {
            switch (type)
            {
                case ClientType.ArClient:
                    return arPlayerPrefab;
                case ClientType.VrClient:
                    return vrPlayerPrefab;
                case ClientType.DesktopClient:
                    return pcPlayerPrefab;
                default:
                    Utils.Logger.LogError("NetworkManager", $"Unsupported client type: {type}");
                    return null;
            }
        }

        private void OnCustomMessageReceived(ulong sender, byte[] data) =>
            OnCustomMessage.Invoke(Serializer.Deserialize<CustomMessage>(data));

        private void OnFileMessageReceived(ulong sender, byte[] data)
        {
            var msg = Serializer.Deserialize<FileExchangeRequest>(data);
            fileHandler.AddMessage(msg.TotalMessageParts, msg.CurrentMessagePart, msg.FileHash, msg.FileBytes, msg.DestinationPath);
        }

        private void OnObjectSpawn(NetworkObjectBase objBase)
        {
            var obj = (NetworkObject)objBase;
            obj.ConnectionId = connectionId;
            obj.Initialize(this, messageSystem, updateInterval);
        }

        private bool InitializeLocalPlayer(GameObject player)
        {
            var localNetworkPlayer = player.GetComponent<NetworkPlayer>();
            if (localNetworkPlayer == null)
            {
                Utils.Logger.LogError("NetworkManager", "Player object must have a NetworkPlayer component. " +
                                                        "Start aborted!");
                return false;
            }

            SpawningBase.ForEachNetworkObjectPreOrder(localNetworkPlayer.gameObject, obj =>
            {
                var netPlayer = obj as NetworkPlayer;
                if (netPlayer != null)
                    netPlayer.IsRemotePrefab = false;
            });

            return true;
        }

        /// <summary>
        /// Represents the available options for automatically starting the script.
        /// </summary>
        public enum AutoStartOption
        {
            None,
            [InspectorName("Auto Server")]
            Server,
            [InspectorName("Auto Client")]
            Client
        }

        /// <summary>
        /// Represents the available built-in transports.
        /// </summary>
        public enum TransportType
        {
            [InspectorName("Telepathy (TCP)")]
            Tcp,
            [InspectorName("LiteNetLib (Reliable UDP)")]
            ReliableUdp
        }

        /// <summary>
        /// Represents the supported hardware targets for automatic player spawning.
        /// </summary>
        [Serializable]
        public enum ClientType
        {
            [InspectorName("AR")]
            ArClient,
            [InspectorName("VR")]
            VrClient,
            [InspectorName("PC")]
            DesktopClient
        }

        [Serializable]
        private struct SpawnRequest
        {
            internal string PrefabHash;
            internal bool IsOwned;
        }

        [Serializable]
        private struct DestroyRequest
        {
            internal ulong Uuid;
        }

        [Serializable]
        private struct ChangeRequest
        {
            internal ulong Uuid;
            internal byte[] Data;
        }

        [Serializable]
        private struct Handshake
        {
            internal ulong connectionId;
            internal ClientType ClientType;
            internal Queue<byte[]> InitData;
        }

        [Serializable]
        private struct FileExchangeRequest
        {
            internal byte[] FileBytes;

            // Filepath where the message will be reconstructed on the receiver.
            internal string DestinationPath { get; set; }

            // Hash of the file that is send for later reconstruction.
            public string FileHash { get; set; }

            // Total number of parts that the message is split in.
            public int TotalMessageParts { get; set; }

            // Current part number that the message is split in.
            public int CurrentMessagePart { get; set; }
        }
    }
}
