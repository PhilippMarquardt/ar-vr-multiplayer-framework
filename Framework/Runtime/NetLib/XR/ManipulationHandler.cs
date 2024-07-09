using UnityEngine;
using UnityEngine.Events;

namespace NetLib.XR
{
    /// <summary>
    /// Can be attached to a GameObject to allow it to receive gesture events.
    /// </summary>
    public class ManipulationHandler : MonoBehaviour
    {
        [Tooltip("Set this if this object should receive tap gestures")]
        public bool isTapable;

        [Tooltip("Set this if this object should be movable by a manipulation gesture")]
        public bool isManipulatable;

        [Tooltip("Factor by which to scale the manipulation distance")]
        public float moveFactor = 2f;

        [Tooltip("Can be set to an object which should be moved by a manipulation instead of this object." +
                 "Can be useful for moving Anchors which are not linked to a visible GameObject")]
        public GameObject proxy;

        /// <summary>
        /// Gets called when the object is tapped.
        /// </summary>
        public UnityEvent onTap;

        /// <summary>
        /// Gets called when the object enters the focus of a pointer. 
        /// </summary>
        public UnityEvent onFocusEnter;

        /// <summary>
        /// Gets called when the object leaves the focus of a pointer.
        /// </summary>
        public UnityEvent onFocusLeave;

        private Transform cachedTransform;
        private Vector3 lastCumulativeDelta;

        internal void StartManipulation()
        {
            lastCumulativeDelta = new Vector3(0, 0, 0);
        }

        internal void UpdateManipulation(Vector3 cumulativeDelta)
        {
            var delta = (cumulativeDelta - lastCumulativeDelta) * moveFactor;

            cachedTransform.position += delta;

            lastCumulativeDelta = cumulativeDelta;
        }

        private void Awake()
        {
            if (proxy == null)
                proxy = gameObject;

            cachedTransform = proxy.transform;
        }
    }
}
