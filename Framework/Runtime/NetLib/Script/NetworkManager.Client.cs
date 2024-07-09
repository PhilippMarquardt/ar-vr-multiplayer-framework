using System.Collections;
using System.Collections.Generic;
using NetLib.Messaging;
using NetLib.Serialization;
using NetLib.Spawning;
using NetLib.Transport;

namespace NetLib.Script
{
    public partial class NetworkManager
    {
        private SpawningClient spawningClient;


        /// <summary>
        /// Starts this NetworkManager as a client instance and connects to a the default ip address set in the
        /// inspector on the default network port set in the inspector.
        /// </summary>
        public void StartClient() => StartClient(defaultIp, defaultPort);

        /// <summary>
        /// Starts this NetworkManager as a client instance and connects to a given ip address on the default network
        /// port set in the inspector.
        /// </summary>
        /// <param name="ip">the address to connect to</param>
        public void StartClient(string ip) => StartClient(ip, defaultPort);

        /// <summary>
        /// Starts this NetworkManager as a client instance and connects to a given ip address on a given network port.
        /// </summary>
        /// <param name="ip">the address to connect to</param>
        /// <param name="port">the port to connect to</param>
        public void StartClient(string ip, ushort port)
        {
            if (isServer || isClient)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to start a client but NetworkManager was already started");
                return;
            }

            isServer = false;
            isClient = true;

            // initialize transport layer -----------------------------------------------------------------------------
            InitializeTransport(false);

            if (!(transport is IClient client))
            {
                Utils.Logger.LogError("NetworkManager", "Trying to start a client but provided Transport is not a client");
                Stop();
                return;
            }


            // initialize messaging system ----------------------------------------------------------------------------
            messageSystem = new MessageSystem(transport, messageSystemChannel);

            // initialize spawning system -----------------------------------------------------------------------------
            var localPlayer = GetPlayerObject();
            if (localPlayer != null)
            {
                if (!InitializeLocalPlayer(localPlayer))
                    return;

            }

            spawningClient = new SpawningClient(messageSystem)
            {
                OnObjectSpawn = OnObjectSpawn,
                RegisteredPrefabs = registeredPrefabs,
                Player = localPlayer != null ? localPlayer.GetComponent<NetworkPlayer>() : null
            };

            spawningClient.InitializeScene();

            // initialize event handlers ------------------------------------------------------------------------------

            // On handshake initiation received from server
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.Handshake, ClientOnHandshakeRequestReceived);
            // On custom message from server
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.Custom, OnCustomMessageReceived);
            // On file message from server
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.FileMessage, OnFileMessageReceived);

            // Connect to server
            client.Connect(ip, port);
        }

        private void ClientOnHandshakeRequestReceived(ulong sender, byte[] data)
        {
            if (IsRunning)
            {
                Utils.Logger.Log("NetworkManager", "Got handshake but is already running");
                return;
            }

            Utils.Logger.Log("NetworkManager", "Got handshake request from server");

            // read message
            var msg = Serializer.Deserialize<Handshake>(data);
            connectionId = msg.connectionId;

            // reply to server
            var initData = new Queue<byte[]>();
            if (GetPlayerObject() != null && ignoreServerSpawnData)
            {
                SpawningBase.ForEachNetworkObjectPreOrder(GetPlayerObject(), obj =>
                {
                    initData.Enqueue(obj.Serialize());
                });
            }

            var reply = new Handshake()
            {
                ClientType = clientType,
                InitData = initData
            };
            messageSystem.Send(
                0,
                (byte)Utils.Constants.InternalMessageType.Handshake,
                Serializer.Serialize(reply));

            IsRunning = true;
        }
    }
}
