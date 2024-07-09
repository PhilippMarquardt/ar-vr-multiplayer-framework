using System.Collections;
using UnityEngine;

namespace NetLib.XR
{
    /// <summary>
    /// Implements a raycast pointer which can select GameObjects with a collider. Can be derived from in order to
    /// implement different types of cursors.
    /// </summary>
    public class Pointer : MonoBehaviour
    {
        [Tooltip("Interval in seconds in which to update the raycast")]
        public float raycastInterval;
        [Tooltip("Maximum distance in which an object can be selected. " +
                 "Non positive values will cause the distance to be infinite")]
        public float raycastMaxDistance;

        /// <summary>
        /// GameObject which is currently in focus of the pointer.
        /// </summary>
        public GameObject FocusedObject { get; private set; }

        /// <summary>
        /// Transform from which to start the raycast.
        /// Must be set by an implementation before calling Start().
        /// </summary>
        protected Transform RaycastOrigin;

        /// <summary>
        /// The position of the last raycast hit.
        /// Can be used by an implementation, e.g. to adjust a cursors position.
        /// </summary>
        protected Vector3 LastRaycastHitPosition;

        /// <summary>
        /// Must be called by deriving scripts in order to start raycasting.
        /// </summary>
        protected virtual void Start()
        {
            if (raycastMaxDistance <= 0)
                raycastMaxDistance = Mathf.Infinity;
            if (RaycastOrigin != null)
                StartCoroutine(DoRaycast());
        }

        private IEnumerator DoRaycast()
        {
            while (isActiveAndEnabled)
            {
                if (Physics.Raycast(RaycastOrigin.position, RaycastOrigin.forward, out var hitInfo, raycastMaxDistance))
                {
                    var hitObject = hitInfo.collider.gameObject;

                    if (FocusedObject == hitObject)
                    {
                        LastRaycastHitPosition = hitInfo.point;
                        yield return new WaitForSeconds(raycastInterval);
                        continue;
                    }

                    var manipulationHandlerOld = FocusedObject == null ? null : FocusedObject.GetComponent<ManipulationHandler>();
                    var manipulationHandlerNew = hitObject.GetComponent<ManipulationHandler>();

                    if (manipulationHandlerOld != null)
                        manipulationHandlerOld.onFocusLeave?.Invoke();

                    if (manipulationHandlerNew != null)
                        manipulationHandlerNew.onFocusEnter?.Invoke();

                    LastRaycastHitPosition = hitInfo.point;
                    FocusedObject = hitObject;
                }
                else
                {
                    if (FocusedObject == null)
                    {
                        yield return new WaitForSeconds(raycastInterval);
                        continue;
                    }

                    var manipulationHandlerOld = FocusedObject.GetComponent<ManipulationHandler>();
                    if (manipulationHandlerOld != null)
                        manipulationHandlerOld.onFocusLeave?.Invoke();

                    FocusedObject = null;
                }

                yield return new WaitForSeconds(raycastInterval);
            }
        }
    }
}
