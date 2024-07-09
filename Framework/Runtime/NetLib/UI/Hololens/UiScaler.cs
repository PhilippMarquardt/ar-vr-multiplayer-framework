using UnityEngine;

namespace NetLib.UI.Hololens
{
    /// <summary>
    /// Scales a canvas in relation to its distance from a player camera.
    /// Used to provide consistently sized menus on different devices.
    /// </summary>
    public class UiScaler : MonoBehaviour
    {
        [Tooltip("Scaling to use for the Hololens")]
        public float scaleFactorHololens = 0.000415f;

        [Tooltip("Scaling to use for the Vive")]
        public float scaleFactorVive = 0.00415f;

        [Tooltip("Scaling to use for the desktop")]
        public float scaleFactorDesktop = 0.00415f;

        [Tooltip("Fallback camera to use when the canvas has no event camera")]
        public Camera uiCamera;

        [Tooltip("Selects which scaling factor is used.")]
        public ClientType hardwareType = ClientType.DesktopClient;


        private void Awake()
        {
            var canvasCamera = GetComponent<Canvas>().worldCamera;
            if (canvasCamera != null)
                uiCamera = GetComponent<Canvas>().worldCamera;

            if (uiCamera == null)
            {
                Utils.Logger.LogError("UiScaler", "No event camera found");
                return;
            }

            var myTransform = transform; // for performance
            float distance = (uiCamera.transform.position - myTransform.position).magnitude;

            float scale = distance * scaleFactorDesktop;
            switch (hardwareType)
            {
                case ClientType.ArClient:
                    scale = distance * scaleFactorHololens;
                    break;
                case ClientType.VrClient:
                    scale = distance * scaleFactorVive;
                    break;
                case ClientType.DesktopClient:
                    scale = distance * scaleFactorDesktop;
                    break;
            }

            myTransform.localScale = new Vector3(scale, scale);
        }

        /// <summary>
        /// Represents the supported hardware targets for automatic scaling.
        /// </summary>
        public enum ClientType
        {
            [InspectorName("AR")]
            ArClient,
            [InspectorName("VR")]
            VrClient,
            [InspectorName("PC")]
            DesktopClient
        }
    }
}
