#if !UNITY_2019_3_OR_NEWER
using NetLib.Script;
using UnityEngine;


#if NETLIB_CLIENT_AR
using System;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
#endif

namespace NetLib.XR
{
    /// <summary>
    /// World Anchor for attaching Holograms to objects in the real world. Includes capabilities for saving anchors 
    /// between runs and synchronizing anchors between multiple Hololenses (the latter also requires the XRAnchorManager).
    /// </summary>
    public class Anchor : NetworkBehaviour
    {
        [Tooltip("Unique name for this anchor")]
        public string id;
        [Tooltip("If the anchor is found in the Hololens storage it will be loaded on startup. " +
            "Note that Unity will not be able to locate a loaded anchor when in the Editor Play Mode.")]
        public bool loadOnStart;
        [Tooltip("A shared anchor will be synchronized between all users in the same physical space")]
        public bool isShared;



        /// <summary>
        /// True if the anchor is locked.
        /// </summary>
        public bool IsLocked { get; private set; }


        /// <summary>
        /// True if the anchor is stored in the Hololens internal storage.
        /// </summary>
        public bool IsPersisted =>
#if NETLIB_CLIENT_AR
            anchorStore != null && Array.Exists(anchorStore.GetAllIds(), s => s.Equals(id));
#else
            false;
#endif

        /// <summary>
        /// True if the anchor is ready to be exported.
        /// </summary>
        public bool IsExportable => IsLocated && isShared;

        /// <summary>
        /// True if the Hololens tracking system has located the anchor in the physical space.
        /// </summary>
        public bool IsLocated =>
#if NETLIB_CLIENT_AR
            IsLocked && UnityAnchor.isLocated;
#else
            false;
#endif

#if NETLIB_CLIENT_AR
        /// <summary>
        /// The underlying Unity AR WorldAnchor component. Null if the anchor is not locked.
        /// </summary>
        public WorldAnchor UnityAnchor { get; private set; }

        private WorldAnchorStore anchorStore;
#endif

        //-------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates the internal state of the component. Should be called if the GameObject's Unity AR WorldAnchor 
        /// component was modified from outside this component.
        /// </summary>
        public void UpdateAnchor()
        {
#if NETLIB_CLIENT_AR
            UnityAnchor = GetComponent<WorldAnchor>();
            IsLocked = UnityAnchor != null;
#endif
        }

        /// <summary>
        /// Anchors the GameObject in the physical space. The GameObject's transform cannot be modified while the
        /// anchor is locked.
        /// </summary>
        public void LockAnchor()
        {
#if NETLIB_CLIENT_AR
            if (IsLocked) return;

            UnityAnchor = gameObject.AddComponent(typeof(WorldAnchor)) as WorldAnchor;
            IsLocked = true;
            Debug.LogFormat("XRAnchor: Locking anchor with id='{0}'", id);
#endif
        }

        /// <summary>
        /// Removes anchorage from the GameObject.
        /// </summary>
        public void UnlockAnchor()
        {
#if NETLIB_CLIENT_AR
            if (!IsLocked) return;

            DestroyImmediate(UnityAnchor);
            IsLocked = false;
            Debug.LogFormat("XRAnchor: Unlocking anchor with id='{0}'", id);
#endif
        }

        /// <summary>
        /// Saves the anchor to the Hololens internal storage, which is persistent between runs of the application.
        /// If the anchor is already stored, it will be overwritten by this anchor.
        /// </summary>
        public void SaveToStore()
        {
#if NETLIB_CLIENT_AR
            if (!IsLocated)
            {
                Debug.LogWarningFormat("XRAnchor: Anchor with id='{0}' is not located", id);
                return;
            }

            // the store does not allow updating, so we delete and save again
            if (IsPersisted)
                DeleteFromStore();

            bool success = anchorStore.Save(id, UnityAnchor);

            if (!success)
            {
                Debug.LogWarningFormat("XRAnchor: Could not save anchor with id='{0}' to store", id);
            }
            else
            {
                Debug.LogFormat("XRAnchor: Saved anchor with id='{0}' to store", id);
            }
#endif
        }

        /// <summary>
        /// Loads the anchor from the Hololens internal storage.
        /// </summary>
        public void LoadFromStore()
        {
#if NETLIB_CLIENT_AR
            if (!IsPersisted)
            {
                Debug.LogWarningFormat("XRAnchor: No anchor with id='{0}' found", id);
                return;
            }

            bool success = anchorStore.Load(id, this.gameObject);

            if (!success)
            {
                Debug.LogWarningFormat("XRAnchor: Could not load anchor with id='{0}' from store", id);
            }
            else
            {
                UpdateAnchor();
                Debug.LogFormat("XRAnchor: Loaded anchor with id='{0}' from store", id);
            }
#endif
        }

        /// <summary>
        /// Deletes the anchor from the Hololens internal storage.
        /// </summary>
        public void DeleteFromStore()
        {
#if NETLIB_CLIENT_AR
            // TODO - more verbose logging
            bool success = anchorStore.Delete(id);
            if (success) Debug.LogFormat("XRAnchor: Deleted anchor with id='{0}' from store", id);
            else Debug.LogWarningFormat("XRAnchor: Could not delete anchor with id='{0}' from store", id);
#endif
        }

        //-------------------------------------------------------------------------------------------------------------

        private void Awake()
        {
            if (id == null)
            {
                Debug.LogError("XRAnchor: Anchor is missing a name");
            }

            IsLocked = false;

#if NETLIB_CLIENT_AR
            WorldAnchorStore.GetAsync(store =>
            {
                anchorStore = store;
                Debug.Log("XRAnchor: World anchor store ready!");
                if (!loadOnStart || !IsPersisted) return;
                // Note: Unity cannot locate anchors from the store in play mode
                Debug.LogFormat("XRAnchor: Found Anchor with id='{0}' in store, loading...", id);
                LoadFromStore();
            });
#endif
        }

        private void Start()
        {
            AnchorManager.Instance.AddAnchor(this);
            Debug.Log("XRAnchor: Starting world anchor script");
        }

        private void OnDestroy()
        {
            AnchorManager.Instance.RemoveAnchor(this);
        }
    }
}
#endif
