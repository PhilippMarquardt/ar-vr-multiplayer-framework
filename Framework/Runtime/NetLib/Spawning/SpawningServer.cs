using System;
using System.Collections.Generic;
using System.Linq;
using NetLib.Messaging;
using NetLib.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NetLib.Spawning
{
    /// <summary>
    /// Handles the spawning, despawning and synchronization of objects across the network on the server side. 
    /// </summary>
    /// <remarks>
    /// The <see cref="SpawningServer"/> communicates with multiple <see cref="SpawningClient"/> instances using the
    /// <see cref="MessageSystem"/>. Therefore the <see cref="MessageSystem"/> on the <see cref="SpawningServer"/>
    /// and <see cref="SpawningClient"/> must be connected before the <see cref="SpawningServer"/> can start sending
    /// data.
    /// </remarks>
    public class SpawningServer : SpawningBase
    {
        /// <summary>
        /// Method to be called for each scene object when it is initialized.
        /// </summary>
        /// <remarks>
        /// Setting this callback allows modifications on <see cref="NetworkObjectBase"/> scene objects.
        /// It is called after the <see cref="NetworkObjectBase"/> internal members are initialized by the system.
        /// To modify prefab objects in the same manner use the equivalent method parameter in
        /// <see cref="SpawnNetworkObject(GameObject,Action{NetworkObjectBase})"/> and
        /// <see cref="SpawnNetworkObject(string,Action{NetworkObjectBase})"/>.
        /// To modify player objects in the same manner use the equivalent method parameter in
        /// <see cref="SpawnClientPlayerObject"/> and <see cref="SpawnServerPlayerObject"/>.
        /// </remarks>
        public Action<NetworkObjectBase> OnObjectSpawn;


        /// <summary>
        /// Counter for getting new network ids.
        /// </summary>
        private ulong uuidGenerator;

        // objects which are already part of the scene when it is loaded
        private readonly List<ulong> sceneObjects;

        // these objects will get spawned on a new connecting client (all dynamically spawned objects)
        private readonly List<ulong> spawnedObjects;

        // these objects will be spawned on all clients in the next update (spawned since the last broadcast)
        private readonly List<ulong> pendingSpawnedObjects;

        // these objects will be destroyed on all clients in the next update (destroyed since the last broadcast)
        private readonly List<ulong> pendingDestroyedObjects;

        // player clients and their player objects | <playerId, objectId>
        private readonly Dictionary<ulong, ulong> players;

        //-------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Construct a new spawning server which uses the given message system for sending messages to the
        /// spawning client.
        /// </summary>
        /// <param name="messageSystem">The message system from which to send messages.</param>
        /// <exception cref="ArgumentNullException">The <c>messageSystem</c> is null</exception>
        public SpawningServer(MessageSystem messageSystem) : base(messageSystem)
        {
            RegisteredPrefabs = new List<GameObject>();
            sceneObjects = new List<ulong>();
            spawnedObjects = new List<ulong>();
            pendingSpawnedObjects = new List<ulong>();
            pendingDestroyedObjects = new List<ulong>();
            players = new Dictionary<ulong, ulong>();
        }

        /// <summary>
        /// Adds the given object with its children to the queue of objects to be spawned on clients in the next update.
        /// </summary>
        /// <remarks>
        /// The object must have a <see cref="NetworkObjectBase"/> component.
        /// Children of the object without a <see cref="NetworkObjectBase"/> component are ignored.
        /// <para>
        /// The object must be instantiated from a prefab which is registered in
        /// <see cref="SpawningBase.RegisteredPrefabs"/> on the receiving <see cref="SpawningClient"/>.
        /// </para>
        /// <para>
        /// To modify the objects after they are initialized by the system use the <see cref="onObjectSpawn"/>
        /// parameter.
        /// </para>
        /// </remarks>
        /// <param name="localObject">The object to spawn across the network.</param>
        /// <param name="onObjectSpawn">Method to be called for each <see cref="NetworkObjectBase"/> in the object graph.</param>
        /// <returns>The initialized <see cref="NetworkObjectBase"/> of the <see cref="localObject"/>.</returns>
        /// <exception cref="ArgumentNullException">The <c>localObject</c> is null</exception>
        /// <exception cref="ArgumentException">The <c>localObject</c> does not have a <c>NetworkObjectBase</c> component</exception>
        public NetworkObjectBase SpawnNetworkObject(GameObject localObject, Action<NetworkObjectBase> onObjectSpawn = null)
        {
            if (localObject == null)
                throw new ArgumentNullException(nameof(localObject), "the object to spawn cannot be null");

            var networkObject = localObject.GetComponent<NetworkObjectBase>();

            if (networkObject == null)
                throw new ArgumentException("the object must have a NetworkObjectBase component", nameof(localObject));

            // spawn the object and all its children with a NetworkObjectBase
            ForEachNetworkObjectPreOrder(localObject, obj =>
            {
                // internal initialization
                obj.Uuid = ++uuidGenerator;
                obj.IsServer = true;
                obj.IsClient = false;

                // custom initialization
                onObjectSpawn?.Invoke(obj);

                NetworkObjects.Add(obj.Uuid, obj);
            });

            pendingSpawnedObjects.Add(networkObject.Uuid);

            return networkObject;
        }

        /// <summary>
        /// Instantiates a prefab from a given hash and adds the object with its children to the queue of objects
        /// to be spawned on clients in the next update.
        /// </summary>
        /// <remarks>
        /// The prefab must have a <see cref="NetworkObjectBase"/> component.
        /// Children of the prefab without a <see cref="NetworkObjectBase"/> component are ignored.
        /// <para>
        /// The prefab must be registered in <see cref="SpawningBase.RegisteredPrefabs"/> of this
        /// <see cref="SpawningServer"/> and on the receiving <see cref="SpawningClient"/>.
        /// </para>
        /// <para>
        /// To modify the objects after they are initialized by the system use the <see cref="onObjectSpawn"/>
        /// parameter.
        /// </para>
        /// </remarks>
        /// <param name="prefabHash">Hash of the prefab to spawn. Must be the hash specified in <see cref="NetworkObjectBase.PrefabHash"/>.</param>
        /// <param name="onObjectSpawn">Method to be called for each <see cref="NetworkObjectBase"/> in the object graph.</param>
        /// <returns>The initialized <see cref="NetworkObjectBase"/> of the prefab.</returns>
        /// <exception cref="ArgumentNullException">The <c>prefabHash</c> is null</exception>
        /// <exception cref="ArgumentException">No prefab with hash <c>prefabHash</c> can be found</exception>
        /// <exception cref="ArgumentException">The prefab does not have a <c>NetworkObjectBase</c> component</exception>
        public NetworkObjectBase SpawnNetworkObject(string prefabHash, Action<NetworkObjectBase> onObjectSpawn = null)
        {
            if (prefabHash == null)
                throw new ArgumentNullException(nameof(prefabHash), "prefabHash cannot be null");

            var prefabToAdd = GetPrefabFromHash(prefabHash);

            if (prefabToAdd == null)
                throw new ArgumentException($"no prefab with hash {prefabHash} found", nameof(prefabHash));

            // instantiate
            var gameObjectToAdd = Object.Instantiate(prefabToAdd);
            return SpawnNetworkObject(gameObjectToAdd, onObjectSpawn);
        }

        /// <summary>
        /// Spawns a client player object with its children.
        /// </summary>
        /// <remarks>
        /// The object will be considered as the player for the client specified by the given id.
        /// The player object will be replicated on all clients except the one whom it belongs to,
        /// since that client already has a local object representing the player.
        /// On the client represented by this player object, the <see cref="SpawningClient.Player"/> object is mapped
        /// to the object spawned by this method.
        /// The <see cref="SpawningClient.Player"/> does not have to be instantiated from the <c>playerObject</c>
        /// object but both objects need to have the same <see cref="NetworkObjectBase"/> topology,
        /// i.e. if one object has children with a <see cref="NetworkObjectBase"/> then the other object must have
        /// the same amount of children in the same hierarchic order.
        /// <para>
        /// The <c>playerObject</c> must have a <see cref="NetworkObjectBase"/> component.
        /// Children of the object without a <see cref="NetworkObjectBase"/> component are ignored.
        /// </para>
        /// <para>
        /// The object must be instantiated from a prefab which is registered in
        /// <see cref="SpawningBase.RegisteredPrefabs"/> on the receiving <see cref="SpawningClient"/>.
        /// </para>
        /// <para>
        /// The object must have a <see cref="NetworkObjectBase"/> component.
        /// Children of the object without a <see cref="NetworkObjectBase"/> component are ignored.
        /// </para>
        /// <para>
        /// To modify the objects after they are initialized by the system use the <see cref="onObjectSpawn"/>
        /// parameter.
        /// </para>
        /// </remarks>
        /// <param name="playerObject">The object to replicate.</param>
        /// <param name="playerId">The connection id of the client this player object belongs to.</param>
        /// <param name="onObjectSpawn">Method to be called for each <see cref="NetworkObjectBase"/> in the object graph.</param>
        /// <returns>The initialized <see cref="NetworkObjectBase"/> of the <see cref="playerObject"/>.</returns>
        /// <exception cref="ArgumentNullException">The <c>playerObject</c> is null</exception>
        /// <exception cref="ArgumentException">The <c>playerObject</c> does not have a <c>NetworkObjectBase</c> component</exception>
        public NetworkObjectBase SpawnClientPlayerObject(GameObject playerObject, ulong playerId, Action<NetworkObjectBase> onObjectSpawn = null)
        {
            var networkObject = SpawnNetworkObject(playerObject, onObjectSpawn);

            players.Add(playerId, networkObject.Uuid);

            return networkObject;
        }

        /// <summary>
        /// Spawns a player object with its children belonging to the server. 
        /// </summary>
        /// <remarks>
        /// The <c>player</c> object will be considered as the player for the server. The <c>prefab</c> object will be
        /// replicated on all clients and mapped to the <c>player</c> object.
        /// The <c>player</c> does not have to be instantiated from the <c>prefab</c> object but both objects need to
        /// have the same <see cref="NetworkObjectBase"/> topology, i.e. if one object has children with a
        /// <see cref="NetworkObjectBase"/> then the other object must have the same amount of children in the same
        /// hierarchic order.
        /// <para>
        /// The <c>player</c> object must have a <see cref="NetworkObjectBase"/> component.
        /// Children of the object without a <see cref="NetworkObjectBase"/> component are ignored.
        /// </para>
        /// <para>
        /// The <c>prefab</c> object must be a prefab which is registered in
        /// <see cref="SpawningBase.RegisteredPrefabs"/> on the receiving <see cref="SpawningClient"/>.
        /// </para>
        /// <para>
        /// To modify the objects after they are initialized by the system use the <see cref="onObjectSpawn"/>
        /// parameter.
        /// </para>
        /// </remarks>
        /// <param name="prefab">The non-instantiated prefab to be replicated on all clients.</param>
        /// <param name="player">An already existing object which represent the local player.</param>
        /// <param name="onObjectSpawn">Method to be called for each <see cref="NetworkObjectBase"/> in the object graph of <see cref="player"/>.</param>
        /// <exception cref="ArgumentNullException">The <c>player</c> is null</exception>
        /// <exception cref="ArgumentNullException">The <c>prefab</c> is null</exception>
        /// <exception cref="ArgumentException">The <c>player</c> does not have a <c>NetworkObjectBase</c> component</exception>
        /// <exception cref="ArgumentException">The <c>prefab</c> does not have a <c>NetworkObjectBase</c> component</exception>
        public NetworkObjectBase SpawnServerPlayerObject(GameObject prefab, GameObject player, Action<NetworkObjectBase> onObjectSpawn = null)
        {
            var playerNetworkObject = SpawnNetworkObject(player, onObjectSpawn);

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "prefab cannot be null");

            // overwrite prefab hash of local player, so that the clients spawn the prefab instead
            var prefabNetworkObject = prefab.GetComponent<NetworkObjectBase>();
            if (prefabNetworkObject == null)
                throw new ArgumentException("prefab does not have a NetworkObjectBase component", nameof(prefab));

            playerNetworkObject.PrefabHash = prefabNetworkObject.PrefabHash;

            return playerNetworkObject;
        }

        /// <summary>
        /// Despawns a client player object.
        /// </summary>
        /// <remarks>
        /// Does nothing if no player with <c>playerId</c> was spawned.
        /// For despawning the server player object simply call <see cref="DeSpawnNetworkObject(GameObject)"/> instead. 
        /// </remarks>
        /// <param name="playerId">The connection id of the client whose player object to destroy.</param>
        public void DeSpawnClientPlayerObject(ulong playerId)
        {
            if (!players.TryGetValue(playerId, out ulong playerObjectId))
                return;

            DeSpawnNetworkObject(playerObjectId);
            players.Remove(playerId);
        }

        /// <summary>
        /// Adds a given object to the queue of objects to be destroyed on clients at the next update.
        /// </summary>
        /// <remarks>
        /// The object must have been previously spawned in the system.
        /// The object's children will also be removed.
        /// The object will be removed from the synchronization process and destroyed on all connected clients.
        /// </remarks>
        /// <param name="localObject">The object to despawn across the network.</param>
        /// <exception cref="ArgumentNullException">The <c>localObject</c> is null</exception>
        /// <exception cref="ArgumentException">The <c>localObject</c> does not have a <see cref="NetworkObjectBase"/> component.</exception>
        /// <exception cref="ArgumentException">The object is not registered in the system</exception>
        public void DeSpawnNetworkObject(GameObject localObject)
        {
            if (localObject == null)
                throw new ArgumentNullException(nameof(localObject), "the object cannot be null");

            var networkObject = localObject.GetComponent<NetworkObjectBase>();
            
            if (networkObject == null)
                throw new ArgumentException("the object must have a NetworkObjectBase component", nameof(localObject));

            DeSpawnNetworkObject(networkObject.Uuid);
        }

        /// <summary>
        /// Adds the object with a given id to the queue of objects to be destroyed on clients at the next update.
        /// </summary>
        /// <remarks>
        /// The object must have been previously spawned in the system.
        /// The object's children will also be removed.
        /// The object will be removed from the synchronization process and destroyed on all connected clients.
        /// </remarks>
        /// <param name="uuid">The id of the object to despawn across the network.</param>
        /// <exception cref="ArgumentException">No object with <c>uuid</c> exists</exception>
        public void DeSpawnNetworkObject(ulong uuid)
        {
            if (!NetworkObjects.TryGetValue(uuid, out var networkObject))
                throw new ArgumentException($"the object you are trying to destroy does not exist, id = {uuid}");

            // add object and all its children to the despawn queue
            ForEachNetworkObjectPostOrder(networkObject.gameObject, obj =>
            {
                pendingDestroyedObjects.Add(obj.Uuid);
            });
            networkObject.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sends a state update to a list of <see cref="SpawningClient"/> instances.
        /// </summary>
        /// <remarks>
        /// The state update contains all newly spawned, destroyed and changed objects since the last call to
        /// <see cref="SendStateUpdate"/>.
        /// <para>
        /// Objects previously marked for despawning are now destroyed on the <see cref="SpawningServer"/>.
        /// On the <see cref="SpawningClient"/> these object are destroyed when the state update is received.
        /// </para>
        /// </remarks>
        /// <param name="receivers">The ids if the clients to which the update will be sent.</param>
        public void SendStateUpdate(List<ulong> receivers)
        {
            var msg = new StateUpdate()
            {
                Objects = new Dictionary<ulong, byte[]>(),
                PendingSpawnedObjects = new List<SpawnMsg>(),
                PendingDestroyedObjects = new List<DestroyMsg>()
            };

            // add pending spawned objects
            foreach (ulong uuid in pendingSpawnedObjects)
            {
                // spawn local object
                spawnedObjects.Add(uuid);

                // add to message
                msg.PendingSpawnedObjects.Add(GetSpawnForPrefabObject(uuid));

                // a spawned object is not dirty
                NetworkObjects[uuid].ResetDirty();

                // call OnNetworkStart for spawned object
                NetworkObjects[uuid].OnNetworkStart();
            }

            // add pending destroyed objects -
            // ignore duplicate destroy statements (it doesn't matter, since the object will be gone regardless)
            foreach (ulong uuid in pendingDestroyedObjects.Distinct())
            {
                // destroy local object
                if (NetworkObjects.TryGetValue(uuid, out var localObject))
                {
                    NetworkObjects.Remove(uuid);
                    Object.Destroy(localObject.gameObject);
                }
                sceneObjects.Remove(uuid);
                spawnedObjects.Remove(uuid);
                pendingSpawnedObjects.Remove(uuid);

                // add to message
                msg.PendingDestroyedObjects.Add(new DestroyMsg()
                {
                    Uuid = uuid,
                });
            }

            // add state updates for existing objects
            foreach (var keyValuePair in NetworkObjects.Where(x => x.Value.IsDirty()))
            {
                msg.Objects.Add(keyValuePair.Key, keyValuePair.Value.Serialize());
                // after the update distribution the object's state is definitely up to date, so we reset any
                // manually set dirty flags and update the objects internal state tracking
                keyValuePair.Value.ResetDirty();
            }

            if (receivers != null && receivers.Count > 0)
            {
                receivers.ForEach(rec =>
                    MessageSystem.Send(rec, (byte)Utils.Constants.InternalMessageType.StateUpdate, Serializer.Serialize(msg)));
            }

            pendingSpawnedObjects.Clear();
            pendingDestroyedObjects.Clear();
        }

        /// <summary>
        /// Sends an initial state to a list of <see cref="SpawningClient"/> instances.
        /// </summary>
        /// <remarks>
        /// The initial state consists of spawn messages for all objects in the scene.
        /// A <see cref="SpawningClient"/> needs to receive an initial state before it can process state updates.
        /// A <see cref="SpawningClient"/> will only process the first initial state it receives.
        /// </remarks>
        /// <param name="receivers">The ids if the clients to which the initial state will be sent.</param>
        public void SendInitialState(List<ulong> receivers)
        {
            if (receivers == null || receivers.Count < 1)
                return;

            // scene objects are same for each client
            var sceneObjectSpawns = GetSceneObjects();

            // each client gets a custom list of spawned objects where he client's public player object is excluded,
            // since the client already has a local player object in its scene 
            receivers.ForEach(rec =>
            {
                var msg = new InitialState()
                {
                    Objects = new List<SpawnMsg>()
                };

                // only add the client's player object if one was spawned for this client
                // this ensures the local player object's uuid matches that of the globally spawned one
                if (players.TryGetValue(rec, out ulong playerObjectUuid))
                {
                    var playerSpawnMsg = GetSpawnForPrefabObject(playerObjectUuid);
                    playerSpawnMsg.SpawnType = SpawnMsg.ObjectType.PlayerObject;
                    playerSpawnMsg.PrefabHash = "";
                    playerSpawnMsg.Parent = 0;
                    msg.Objects.Add(playerSpawnMsg);
                }

                msg.Objects.AddRange(sceneObjectSpawns);
                // add all spawned objects except the public player object of the client
                msg.Objects.AddRange(GetPrefabObjectsWithoutPlayer(rec));

                MessageSystem.Send(rec, (byte)Utils.Constants.InternalMessageType.InitialState, Serializer.Serialize(msg));
            });
        }

        /// <inheritdoc/>
        public override void InitializeScene()
        {
            foreach (var networkObject in Object.FindObjectsOfType<NetworkObjectBase>().OrderBy(x => x.SceneOrderIndex).ToArray())
            {
                // skip the player object and its children as they are initialized later
                if (IsObjectOrParentPlayer(networkObject))
                    continue;

                networkObject.Uuid = ++uuidGenerator;
                networkObject.IsServer = true;
                networkObject.IsClient = false;
                
                OnObjectSpawn?.Invoke(networkObject);

                NetworkObjects.Add(networkObject.Uuid, networkObject);
                sceneObjects.Add(networkObject.Uuid);
            }
        }

        //-------------------------------------------------------------------------------------------------------------

        private SpawnMsg GetSpawnForPrefabObject(ulong id)
        {
            var networkObject = NetworkObjects[id];

            var parent = networkObject.transform.parent;
            var parentNetworkObject = parent == null ? null : parent.gameObject.GetComponent<NetworkObjectBase>();

            var spawnState = new SpawnMsg
            {
                SpawnType = SpawnMsg.ObjectType.PrefabObject,
                PrefabHash = networkObject.PrefabHash ?? "",
                Parent = parentNetworkObject != null ? parentNetworkObject.Uuid : 0,
                Uuid = networkObject.Uuid,
                SpawnData = new Queue<byte[]>(),
                InitData = new Queue<byte[]>(),
            };

            // add initData for object and all its children with a NetworkObject
            ForEachNetworkObjectPreOrder(networkObject.gameObject, obj =>
            {
                spawnState.SpawnData.Enqueue(obj.SerializeOnSpawn());
                spawnState.InitData.Enqueue(obj.Serialize());
            });

            return spawnState;
        }

        private List<SpawnMsg> GetSceneObjects()
        {
            var sceneObjectSpawns = new List<SpawnMsg>();
            foreach (var networkObject in sceneObjects.Select(uuid => NetworkObjects[uuid].GetComponent<NetworkObjectBase>()))
            {
                sceneObjectSpawns.Add(new SpawnMsg
                {
                    SpawnType = SpawnMsg.ObjectType.SceneObject,
                    // scene objects don't need the prefabHash and parent since they are already instantiated on the client
                    PrefabHash = "",
                    Parent = 0,
                    Uuid = networkObject.Uuid,
                    SpawnData = new Queue<byte[]>(new [] { networkObject.SerializeOnSpawn() }),
                    InitData = new Queue<byte[]>(new [] { networkObject.Serialize() })
                });
            }

            return sceneObjectSpawns;
        }

        private List<SpawnMsg> GetPrefabObjectsWithoutPlayer(ulong playerId)
        {
            var prefabObjectSpawns = new List<SpawnMsg>();
            foreach (ulong networkObjectId in spawnedObjects)
            {
                // exclude player object
                if (players.TryGetValue(playerId, out ulong playerObjectId) && networkObjectId == playerObjectId)
                    continue;

                // workaround for edge case where a new client would get the same spawn in the initial
                // and next update state
                if (pendingSpawnedObjects.Contains(networkObjectId))
                    continue;

                prefabObjectSpawns.Add(GetSpawnForPrefabObject(networkObjectId));
            }

            return prefabObjectSpawns;
        }
    }
}
