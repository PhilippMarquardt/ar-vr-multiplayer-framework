using UnityEngine;

namespace NetLib.Script
{
    /// <summary>
    /// Script to be used instead of <see cref="NetworkObject"/> on player <see cref="GameObject"/>.
    /// </summary>
    /// <returns>
    /// The <see cref="NetworkPlayer"/> automatically send its local state on the controlling client to the server
    /// where it is applied to the global object and synchronized with all other clients.
    /// <para>
    /// If the object has children which should also be considered part of the player object then these children also
    /// need <see cref="NetworkPlayer"/> components.
    /// </para>
    /// </returns>
    [AddComponentMenu("NetLib/NetworkPlayer")]
    public class NetworkPlayer : NetworkObject
    {
        /// <inheritdoc/>
        public override bool IsPlayer => true;

        /// <summary>
        /// True if this a a player prefab controlled by a remote client, false if this is the local player object.
        /// </summary>
        public bool IsRemotePrefab { get; internal set; } = true;

        private void Update()
        {
            // only manually update position if we are spawned and on a client. On the server the state is updated
            // automatically
            if (!IsRemotePrefab && Uuid != 0 && IsClient && IsDirty())
            {
                networkManager.ChangeObjectState(this);
                // after the update we should not be dirty anymore
                ResetDirty();
            }
        }
    }
}
