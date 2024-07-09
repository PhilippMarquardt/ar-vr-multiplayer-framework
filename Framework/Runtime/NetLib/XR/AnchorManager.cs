#if !UNITY_2019_3_OR_NEWER
using System.Collections.Generic;
using NetLib.Script;
using UnityEngine;

#if NETLIB_CLIENT_AR
using System.Linq;
using NetLib.Serialization;
using UnityEngine.XR.WSA.Sharing;
#endif

namespace NetLib.XR
{
    /// <summary>
    /// Keeps track of all XRAnchors in the scene. Can import/export anchors for anchor synchronization between
    /// multiple Hololenses.
    /// </summary>
    public class AnchorManager : NetworkBehaviour
    {
        [Tooltip("How often an import of anchors should be retried upon failure")]
        public int importRetryCount = 10;
        [Tooltip("How often an export of anchors should be retried upon failure")]
        public int exportRetryCount = 1;

        /// <summary>
        /// Singleton instance of this XRAnchorManager.
        /// </summary>
        public static AnchorManager Instance { get; private set; }

        // ReSharper disable once CollectionNeverQueried.Local
        private Dictionary<string, Anchor> anchors;

#if NETLIB_CLIENT_AR && UNITY_EDITOR
        private bool isDirty = false;
#elif NETLIB_SERVER
        private byte[] serializedData;
#endif

#if NETLIB_CLIENT_AR
        private List<byte> exportBuffer;
        private byte[] importBuffer;
        private WorldAnchorTransferBatch transferBatch;
#endif

        // ------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool IsDirty()
        {
#if NETLIB_CLIENT_AR && UNITY_EDITOR
            return isDirty;
#elif NETLIB_CLIENT_AR && !UNITY_EDITOR
            return exportBuffer.Count > 0;
#elif NETLIB_SERVER
            return serializedData.Length > 0;
#else
            return false;
#endif
        }

        /// <inheritdoc/>
        public override byte[] Serialize()
        {
#if NETLIB_CLIENT_AR && UNITY_EDITOR
            return Serializer.Serialize(42);
#elif NETLIB_CLIENT_AR && !UNITY_EDITOR
            var data = exportBuffer.ToArray();
            //exportBuffer.Clear();
            return data;
#elif NETLIB_SERVER
            return serializedData;
#else
            return null;
#endif
        }

        /// <inheritdoc/>
        public override void Deserialize(byte[] data)
        {
#if NETLIB_CLIENT_AR && UNITY_EDITOR
            Debug.Log("XRAnchor: received serialized value: " + Serializer.Deserialize<int>(data));
#elif NETLIB_CLIENT_AR && !UNITY_EDITOR
            ImportAnchor(data);
#elif NETLIB_SERVER
            serializedData = data;
#endif
        }

        /// <summary>
        /// Starts the export of all shared anchors in the scene. Shared anchors that are not located will be ignored.
        /// </summary>
        public void ExportAnchors()
        {
#if NETLIB_CLIENT_AR && UNITY_EDITOR
            isDirty = true;
            // TODO: wip
#elif NETLIB_CLIENT_AR && !UNITY_EDITOR
            transferBatch = new WorldAnchorTransferBatch();
            foreach (var anchor in anchors.Values.Where(anchor => anchor.IsExportable))
            {
                Debug.LogFormat("XRAnchor: Adding {0} to export", anchor.id);
                transferBatch.AddWorldAnchor(anchor.id, anchor.UnityAnchor);
            }

            if (transferBatch.anchorCount == 0)
            {
                Debug.LogWarning("XRAnchor: No exportable anchors found");
                transferBatch.Dispose();
                transferBatch = null;
                return;
            }

            WorldAnchorTransferBatch.ExportAsync(transferBatch, OnExportDataAvailable, OnExportComplete);
            Debug.LogFormat("XRAnchor: Starting export of {0} anchors", transferBatch.anchorCount);
#endif
        }


        /// <summary>
        /// Starts the import of anchors from a serialized byte array.
        /// </summary>
        /// <param name="data">The serialized data from an anchor export</param>
        public void ImportAnchor(byte[] data)
        {
#if NETLIB_CLIENT_AR
            importBuffer = data;
            importRetryCount = 10;
            WorldAnchorTransferBatch.ImportAsync(importBuffer, OnImportComplete);
#endif
        }

        internal void AddAnchor(Anchor anchor)
        {
            anchors.Add(anchor.id, anchor);
        }

        internal void RemoveAnchor(Anchor anchor)
        {
            anchors.Remove(anchor.id);
        }

        internal void ClearAnchors()
        {
            anchors.Clear();
        }

        // ------------------------------------------------------------------------------------------------------------

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            anchors = new Dictionary<string, Anchor>();

#if NETLIB_CLIENT_AR
            exportBuffer = new List<byte>();
#elif NETLIB_SERVER
            serializedData = new byte[0];
#endif
        }

#if NETLIB_CLIENT_AR
        private void OnExportDataAvailable(byte[] data)
        {
            // TODO: probably not efficient
            exportBuffer.AddRange(data.ToList());
        }

        private void OnExportComplete(SerializationCompletionReason completionReason)
        {
            transferBatch.Dispose();
            transferBatch = null;

            if (completionReason != SerializationCompletionReason.Succeeded)
            {
                // export failed, discard data
                exportBuffer.Clear();
                Debug.LogErrorFormat("XRAnchor: Failed to export anchors: {0}", completionReason);
                return;
            }

            // export succeeded, send data
            Debug.Log("XRAnchor: Anchor export succeeded");
            // TODO: wip
            //GameObject.Find("NetworkManager").GetComponent<Client>().OnStateChange(GetComponent<NetworkObject>());
        }

        private void OnImportComplete(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
        {
            if (completionReason != SerializationCompletionReason.Succeeded)
            {
                Debug.LogErrorFormat("XRAnchor: Failed to import anchors: {0}", completionReason);
                if (importRetryCount <= 0)
                    return;
                importRetryCount--;
                ImportAnchor(importBuffer);
                return;
            }

            // import succeeded
            var importedIds = deserializedTransferBatch.GetAllIds();
            foreach (string id in importedIds)
            {
                if (!anchors.TryGetValue(id, out var anchor)) 
                    continue;
                deserializedTransferBatch.LockObject(id, anchor.gameObject);
                anchor.UpdateAnchor();
            }
            importBuffer = null;
        }
#endif
    }
}
#endif
