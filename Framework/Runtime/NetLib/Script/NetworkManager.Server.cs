using System.Collections;
using System.Collections.Generic;
using NetLib.Messaging;
using NetLib.Serialization;
using NetLib.Spawning;
using NetLib.Transport;
using UnityEngine;

namespace NetLib.Script
{
    public partial class NetworkManager
    {
        private SpawningServer spawningServer;

        /// <summary>
        /// Clients which have completed the handshake.
        /// </summary>
        private List<ulong> connectedClients;

        // ReSharper disable once CollectionNeverQueried.Local
        private Dictionary<ulong, ClientType> connectedClientTypes;

        /// <summary>
        /// Clients which have not yet completed the handshake.
        /// </summary>
        private List<ulong> pendingClients;


        /// <summary>
        /// Starts this NetworkManager as a server instance using the default network port set in the inspector.
        /// </summary>
        public void StartServer() => StartServer(defaultPort);

        /// <summary>
        /// Starts this NetworkManager as a server instance on a given network port.
        /// </summary>
        /// <param name="port">the port on which to run the server</param>
        public void StartServer(ushort port)
        {
            if (isClient || isServer)
            {
                Utils.Logger.LogWarning("NetworkManager", "Trying to start a server but NetworkManager was already started");
                return;
            }

            isServer = true;
            isClient = false;

            connectedClients = new List<ulong>();
            connectedClientTypes = new Dictionary<ulong, ClientType>();
            pendingClients = new List<ulong>();

            // initialize transport layer -----------------------------------------------------------------------------
            InitializeTransport(true);

            if (!(transport is IServer server))
            {
                Utils.Logger.LogError("NetworkManager", "Trying to start a server but provided Transport is not a server");
                Stop();
                return;
            }

            server.OnConnect += ServerOnConnect;
            server.OnDisconnect += ServerOnDisconnect;
            

            // initialize messaging system ----------------------------------------------------------------------------
            messageSystem = new MessageSystem(transport, messageSystemChannel);

            // initialize spawning system -----------------------------------------------------------------------------
            spawningServer = new SpawningServer(messageSystem)
            {
                OnObjectSpawn = OnObjectSpawn,
                RegisteredPrefabs = registeredPrefabs,
            };

            spawningServer.InitializeScene();

            var playerObjectServer = GetPlayerObject();
            if (playerObjectServer != null)
            {
                if (!InitializeLocalPlayer(playerObjectServer))
                    return;
            }

            var playerPrefabServer = GetPlayerPrefab(clientType);

            if (serverIsPlayer && playerObjectServer != null && playerPrefabServer != null)
            {
                spawningServer.SpawnServerPlayerObject(playerPrefabServer, playerObjectServer, objBase =>
                {
                    var obj = (NetworkObject)objBase;
                    obj.IsOwned = true;
                    obj.Owner = 0;
                    OnObjectSpawn(obj);
                });
            }

            // initialize event handlers ------------------------------------------------------------------------------

            // On handshake response received from client
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.Handshake, ServerOnHandshakeResponseReceived);
            // On spawn request from client
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.SpawnRequest, ServerOnSpawnRequestReceived);
            // On destroy request from client
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.DestroyRequest, ServerOnDestroyRequestReceived);
            // On change request from client
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.ChangeRequest, ServerOnChangeRequestReceived);
            // On custom message from client
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.Custom, OnCustomMessageReceived);
            // On file message from server
            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.FileMessage, OnFileMessageReceived);

            // start networking ---------------------------------------------------------------------------------------
            server.Start(port);

            Utils.Logger.Log("NetworkManager", $"Starting server on port: {port}");
            IsRunning = true;
            spawningServer.OnNetworkStart();
            StartCoroutine(BroadcastStateUpdate());
        }


        internal void SendToAll(byte msgType, ulong target, byte[] data)
        {
            foreach (ulong client in connectedClients)
            {
                messageSystem.Send(client, msgType, target, data);
            }
        }


        private void ServerOnConnect(ulong id)
        {
            Utils.Logger.Log("NetworkManager", "Client " + id + " connected");
            pendingClients.Add(id);
        }

        private void ServerOnDisconnect(ulong id)
        {
            Utils.Logger.Log("NetworkManager", "Client " + id + " disconnected");
            spawningServer.DeSpawnClientPlayerObject(id);
            connectedClients.Remove(id);
            connectedClientTypes.Remove(id);
            pendingClients.Remove(id);
        }

        private void ServerOnHandshakeResponseReceived(ulong sender, byte[] data)
        {
            var msg = Serializer.Deserialize<Handshake>(data);
            Utils.Logger.Log("NetworkManager", $"Client {sender} of type {msg.ClientType} is ready");
            pendingClients.Remove(sender);

            var playerPrefab = GetPlayerPrefab(msg.ClientType);

            if (playerPrefab != null)
            {
                var spawnedPlayerObject = Instantiate(playerPrefab);
                spawningServer.SpawnClientPlayerObject(spawnedPlayerObject, sender, objBase =>
                {
                    var obj = (NetworkObject)objBase;
                    obj.IsOwned = true;
                    obj.Owner = sender;
                    OnObjectSpawn(obj);
                });

                // Allows users to modify the object before it gets spawned on all clients
                if (msg.InitData == null || msg.InitData.Count == 0)
                {
                    OnPlayerSpawn?.Invoke(spawnedPlayerObject, sender, msg.ClientType);
                }
                else
                {
                    SpawningBase.ForEachNetworkObjectPreOrder(spawnedPlayerObject, obj =>
                    {
                        obj.Deserialize(msg.InitData.Dequeue());
                    });
                }   
            }

            spawningServer.SendInitialState(new List<ulong>() { sender });
            connectedClients.Add(sender);
            connectedClientTypes.Add(sender, msg.ClientType);
        }

        private void ServerOnSpawnRequestReceived(ulong sender, byte[] data)
        {
            var msg = Serializer.Deserialize<SpawnRequest>(data);

            if (msg.IsOwned)
            {
                spawningServer.SpawnNetworkObject(msg.PrefabHash, objBase =>
                {
                    var obj = (NetworkObject)objBase;
                    obj.IsOwned = true;
                    obj.Owner = sender;
                    OnObjectSpawn(obj);
                });
            }
            else
            {
                spawningServer.SpawnNetworkObject(msg.PrefabHash, objBase =>
                {
                    var obj = (NetworkObject)objBase;
                    obj.IsOwned = false;
                    obj.Owner = 0;
                    OnObjectSpawn(obj);
                });
            }
        }

        private void ServerOnDestroyRequestReceived(ulong sender, byte[] data)
        {
            var msg = Serializer.Deserialize<DestroyRequest>(data);
            var netObj = spawningServer.GetNetworkObjectFromUuid(msg.Uuid);

            if (netObj == null)
            {
                Utils.Logger.Log("NetworkManager", $"Received a request to destroy object with id " +
                                                   $"{msg.Uuid} but the object does not exist. " +
                                                   $"The request will be dropped");
                return;
            }

            //destroy object if sender has permission
            if (!((NetworkObject)netObj).IsOwned || ((NetworkObject)netObj).Owner == sender)
            {
                spawningServer.DeSpawnNetworkObject(msg.Uuid);
            }
        }

        private void ServerOnChangeRequestReceived(ulong sender, byte[] data)
        {
            var msg = Serializer.Deserialize<ChangeRequest>(data);
            var netObj = spawningServer.GetNetworkObjectFromUuid(msg.Uuid);
            if (netObj == null)
            {
                Utils.Logger.Log(
                    "NetworkManager", 
                    $"Received a request to change state of object with id " +
                                                   $"{msg.Uuid} but the object does not exist. " +
                                                   $"The request will be dropped");
                return;
            }

            // change object state if sender has permission
            if (!((NetworkObject)netObj).IsOwned || ((NetworkObject)netObj).Owner == sender)
            {
                netObj.Deserialize(msg.Data);
            }
            // mark object for update, so that either the new state gets distributed or the old state gets forced
            // on all clients
            netObj.MarkDirty();
        }

        private IEnumerator BroadcastStateUpdate()
        {
            while (IsRunning)
            {
                // send updates to connected clients
                spawningServer.SendStateUpdate(connectedClients);

                // send handshakes to pending clients
                foreach (ulong client in pendingClients)
                {
                    Utils.Logger.Log("NetworkManager", "Sending handshake to client " + client);
                    messageSystem.Send(
                        client,
                        (byte)Utils.Constants.InternalMessageType.Handshake,
                        Serializer.Serialize(new Handshake()
                        {
                            connectionId = client
                        }));
                }

                yield return new WaitForSeconds(updateInterval);
            }
        }
    }
}
