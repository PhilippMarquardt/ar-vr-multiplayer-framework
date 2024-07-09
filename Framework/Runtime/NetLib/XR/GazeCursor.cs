using UnityEngine;

namespace NetLib.XR
{
    /// <summary>
    /// Cursor for the Hololens. Follows the users gaze.
    /// </summary>
    public class GazeCursor : Pointer
    {
        [Tooltip("Distance of the cursor to the camera when no object is focused")]
        public float distance = 1f;
        [Tooltip("Camera which the cursor should follow")]
        public Camera eventCamera;

        private Transform cameraTransform;
        private Vector3 scale;

        private void Awake()
        {
            if (eventCamera != null)
            {
                cameraTransform = eventCamera.transform;
                return;
            }
            
            if (Camera.main == null)
            {
                Utils.Logger.LogError("GazeCursor", "No camera found");
                return;
            }

            eventCamera = Camera.main;
            cameraTransform = eventCamera.transform;
        }

        protected override void Start()
        {
            scale = transform.localScale;
            RaycastOrigin = cameraTransform;
            base.Start();
        }

        private void Update()
        {
            if (FocusedObject != null)
                transform.position = LastRaycastHitPosition;
            else
                transform.position = cameraTransform.position + cameraTransform.forward * distance;

            if (distance > 0)
                transform.localScale = scale * (Vector3.Distance(cameraTransform.position, transform.position) / distance);
        }
    }
}
