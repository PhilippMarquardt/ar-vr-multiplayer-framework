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
    /// Handles the spawning, despawning and synchronization of objects across the network on the client side. 
    /// </summary>
    /// <remarks>
    /// The <see cref="SpawningClient"/> communicates with a <see cref="SpawningServer"/> instances using the
    /// <see cref="MessageSystem"/>. Therefore the <see cref="MessageSystem"/> on the <see cref="SpawningServer"/>
    /// and <see cref="SpawningClient"/> must be connected before the <see cref="SpawningClient"/> can start receiving
    /// data.
    /// </remarks>
    public class SpawningClient : SpawningBase
    {
        /// <summary>
        /// Method to be called for each object when it is spawned from the server.
        /// </summary>
        /// <remarks>
        /// Setting this callback allows modifications on spawned <see cref="NetworkObjectBase"/> objects.
        /// It is called after the <see cref="NetworkObjectBase"/> internal members are initialized by the system
        /// but before the <see cref="NetworkObjectBase.DeserializeOnSpawn"/> and
        /// <see cref="NetworkObjectBase.Deserialize"/> methods are called on the object.
        /// </remarks>
        public Action<NetworkObjectBase> OnObjectSpawn;

        /// <summary>
        /// The local GameObject which acts as the player for this client.
        /// </summary>
        public NetworkObjectBase Player;

        private bool isInitialized;

        //-------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Construct a new spawning client which uses the given message system for receiving messages from the
        /// spawning server.
        /// </summary>
        /// <param name="messageSystem">The message system from which to receive messages.</param>
        /// <exception cref="ArgumentNullException">The <c>messageSystem</c> is null</exception>
        public SpawningClient(MessageSystem messageSystem) : base(messageSystem)
        {
            RegisteredPrefabs = new List<GameObject>();

            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.StateUpdate, (sender, data) =>
            {
                if (!isInitialized)
                {
                    Utils.Logger.Log("SpawningClient", "Got StateUpdate but scene is not initialized");
                    return;
                }
                ApplyStateUpdateOnClient(Serializer.Deserialize<StateUpdate>(data));
            });

            messageSystem.AddListener((byte)Utils.Constants.InternalMessageType.InitialState, (sender, data) =>
            {
                if (isInitialized)
                {
                    Utils.Logger.Log("SpawningClient", "Got InitialState but scene is already initialized. This is okay.");
                    return;
                }
                ApplyInitialStateOnClient(Serializer.Deserialize<InitialState>(data));
                isInitialized = true;
            });
        }

        /// <inheritdoc/>
        public override void InitializeScene()
        {
            ulong uuidGenerator = 0;
            foreach (var networkObject in Object.FindObjectsOfType<NetworkObjectBase>().OrderBy(x => x.SceneOrderIndex).ToArray())
            {
                // skip the player object and its children as they are initialized later
                if (IsObjectOrParentPlayer(networkObject))
                    continue;

                networkObject.Uuid = ++uuidGenerator;

                NetworkObjects.Add(networkObject.Uuid, networkObject);
                networkObject.gameObject.SetActive(false);
            }
        }

        //-------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets called when the client receives a state update from the server.
        /// </summary>
        /// <param name="msg">The state update data.</param>
        private void ApplyStateUpdateOnClient(StateUpdate msg)
        {
            msg.PendingSpawnedObjects.ForEach(ApplySpawnOnClient);

            msg.PendingDestroyedObjects.ForEach(ApplyDestroyOnClient);

            foreach (var keyValuePair in msg.Objects)
            {
                if (NetworkObjects.TryGetValue(keyValuePair.Key, out var networkObject) && networkObject != null)
                {
                    if (Player == null || Player.Uuid != networkObject.Uuid)
                        networkObject.Deserialize(keyValuePair.Value);
                }
                else
                {
                    Utils.Logger.LogError("SpawningClient", $"Received data for NetworkObject with id '{keyValuePair.Key}', but no such object exists");
                }
            }
        }

        /// <summary>
        /// Gets called when the client receives the initial state from the server.
        /// </summary>
        /// <param name="msg">The state data.</param>
        private void ApplyInitialStateOnClient(InitialState msg)
        {
            msg.Objects.ForEach(ApplySpawnOnClient);
        }

        private void ApplySpawnOnClient(SpawnMsg msg)
        {
            switch (msg.SpawnType)
            {
                case SpawnMsg.ObjectType.SceneObject:
                    SpawnSceneObject(msg);
                    break;

                case SpawnMsg.ObjectType.PlayerObject:
                    SpawnPlayerObject(msg);
                    break;

                case SpawnMsg.ObjectType.PrefabObject:
                    SpawnPrefabObject(msg);
                    break;
            }
        }

        private void SpawnSceneObject(SpawnMsg msg)
        {
            // check if object can be found
            if (!NetworkObjects.TryGetValue(msg.Uuid, out var networkObjectToActivate) || networkObjectToActivate == null)
            {
                Utils.Logger.LogError("SpawningClient", $"Trying to initialize SceneObject with id '{msg.Uuid}', but no such object exists");
                return;
            }

            // internal initialization
            networkObjectToActivate.IsServer = false;
            networkObjectToActivate.IsClient = true;

            // custom initialization
            OnObjectSpawn?.Invoke(networkObjectToActivate);

            // deserialization
            networkObjectToActivate.DeserializeOnSpawn(msg.SpawnData.Dequeue());
            networkObjectToActivate.Deserialize(msg.InitData.Dequeue());

            // the network objects was deactivated when the scene initialized so we reactivate it on spawn
            networkObjectToActivate.gameObject.SetActive(true);

            networkObjectToActivate.OnNetworkStart();
        }

        private void SpawnPlayerObject(SpawnMsg msg)
        {
            // the player object only needs to have its id assigned
            // it will not get data for deserialization
            ulong uuidChildGenerator = msg.Uuid;
            ForEachNetworkObjectPreOrder(Player.gameObject, obj =>
            {
                // internal initialization
                obj.Uuid = uuidChildGenerator++;
                obj.IsServer = false;
                obj.IsClient = true;

                // custom initialization
                OnObjectSpawn?.Invoke(obj);

                // deserialization
                obj.DeserializeOnSpawn(msg.SpawnData.Dequeue());
                obj.Deserialize(msg.InitData.Dequeue());

                // when we get an objects current state from the server, the object should not be marked dirty
                obj.ResetDirty();

                obj.OnNetworkStart();

                if (NetworkObjects.ContainsKey(obj.Uuid))
                {
                    Utils.Logger.LogError("SpawningClient", $"Trying to spawn object with id {obj.Uuid} " +
                                                            $"but id is already in use. This might occur when server " +
                                                            $"and client do not start with the same scene objects.");
                    return;
                }
                NetworkObjects.Add(obj.Uuid, obj);
            });
        }

        private void SpawnPrefabObject(SpawnMsg msg)
        {
            // ignore object if it is the player
            if (Player != null && msg.Uuid == Player.Uuid)
                return;

            // instantiate the prefab
            var prefabToAdd = GetPrefabFromHash(msg.PrefabHash);
            if (prefabToAdd == null)
            {
                Utils.Logger.LogError("SpawningClient", $"Trying to spawn prefab with hash '{msg.PrefabHash}', but no such prefab was found");
                return;
            }

            // deactivate prefab to allow setting initial values on the copy before making it visible
            prefabToAdd.SetActive(false); 
            
            // find the parent
            NetworkObjectBase parent = null;
            if (msg.Parent != 0 && (!NetworkObjects.TryGetValue(msg.Parent, out parent) || parent == null))
            {
                Utils.Logger.LogWarning("SpawningClient", $"Trying to spawn prefab as child of object with id {msg.Parent}, but no such object was found." +
                                                          $"The object will instead be spawned without a parent!");
            }

            // instantiate the object from the prefab
            var gameObjectToAdd = Object.Instantiate(prefabToAdd, parent == null ? null : parent.transform);

            // register and initialize object and all its children with a NetworkObjectBase
            ulong uuidChildGenerator = msg.Uuid;
            ForEachNetworkObjectPreOrder(gameObjectToAdd, obj =>
            {
                // internal initialization
                obj.Uuid = uuidChildGenerator++;
                obj.IsServer = false;
                obj.IsClient = true;

                // custom initialization
                OnObjectSpawn?.Invoke(obj);

                // deserialization
                obj.DeserializeOnSpawn(msg.SpawnData.Dequeue());
                obj.Deserialize(msg.InitData.Dequeue());

                // when we get an objects current state from the server, the object should not be marked dirty
                obj.ResetDirty();

                obj.OnNetworkStart();

                if (NetworkObjects.ContainsKey(obj.Uuid))
                {
                    Utils.Logger.LogError("SpawningClient", $"Trying to spawn object with id {obj.Uuid} " +
                                                            $"but id is already in use. This might occur when server " +
                                                            $"and client do not start with the same scene objects.");
                    return;
                }
                NetworkObjects.Add(obj.Uuid, obj);
            });

            gameObjectToAdd.SetActive(true);
            // change prefab back to its original state so it does not get overwritten
            prefabToAdd.SetActive(true); 
        }

        private void ApplyDestroyOnClient(DestroyMsg msg)
        {
            if (!NetworkObjects.TryGetValue(msg.Uuid, out var networkObject) || networkObject == null)
            {
                Utils.Logger.LogError("SpawningClient", $"Trying to destroy object with id '{msg.Uuid}' but no such object exists");
                return;
            }

            NetworkObjects.Remove(msg.Uuid);
            Object.Destroy(networkObject.gameObject);
        }
    }
}
