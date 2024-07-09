using System;
using UnityEngine;

#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

namespace NetLib.XR
{
    /// <summary>
    /// Captures user gestures from the Hololens and dispatches events to objects selected by a provided Pointer.
    /// </summary>
    public class GestureManager : MonoBehaviour
    {
        [Tooltip("Pointer to use for selecting objects")]
        public Pointer pointer;


#if UNITY_WSA
        private GameObject manipulatedObject;

        private GestureRecognizer gestureRecognizer;

        // Store lambdas to be able to unsubscribe in OnDestroy
        private Action<ManipulationCanceledEventArgs> onManipulationCanceled;
        private Action<ManipulationCompletedEventArgs> onManipulationCompleted;
        private Action<ManipulationStartedEventArgs> onManipulationStarted;
        private Action<ManipulationUpdatedEventArgs> onManipulationUpdated;
        private Action<TappedEventArgs> onTapEvent;
#endif


#if UNITY_WSA
        private void Awake()
        {
            gestureRecognizer = new GestureRecognizer();
            gestureRecognizer.SetRecognizableGestures(
                GestureSettings.ManipulationTranslate |
                GestureSettings.Tap);

            // Define event handlers
            onManipulationCanceled = (args) =>
            {
                manipulatedObject = null;
            };
            onManipulationCompleted = (args) =>
            {
                manipulatedObject = null;
            };
            onManipulationStarted = (args) =>
            {
                manipulatedObject = pointer.FocusedObject;
                if (manipulatedObject == null)
                    return;

                var manipulationHandler = manipulatedObject.GetComponent<ManipulationHandler>();
                if (manipulationHandler == null || !manipulationHandler.isManipulatable)
                    return;

                manipulationHandler.StartManipulation();
            };
            onManipulationUpdated = (args) =>
            {
                if (manipulatedObject == null)
                    return;

                var manipulationHandler = manipulatedObject.GetComponent<ManipulationHandler>();
                if (manipulationHandler == null || !manipulationHandler.isManipulatable)
                    return;

                manipulationHandler.UpdateManipulation(args.cumulativeDelta);
            };

            onTapEvent = (args) =>
            {
                if (pointer.FocusedObject == null)
                    return;

                var manipulationHandler = pointer.FocusedObject.GetComponent<ManipulationHandler>();
                if (manipulationHandler == null || !manipulationHandler.isTapable)
                    return;

                manipulationHandler.onTap?.Invoke();
            };

            // Subscribe event handlers
            gestureRecognizer.ManipulationStarted += onManipulationStarted;
            gestureRecognizer.ManipulationUpdated += onManipulationUpdated;
            gestureRecognizer.ManipulationCanceled += onManipulationCanceled;
            gestureRecognizer.ManipulationCompleted += onManipulationCompleted;
            gestureRecognizer.Tapped += onTapEvent;

            gestureRecognizer.StartCapturingGestures();
        }


        private void OnEnable()
        {
            gestureRecognizer.StartCapturingGestures();
        }

        private void OnDisable()
        {
            gestureRecognizer.StopCapturingGestures();
        }

        public void OnDestroy()
        {
            // Unsubscribe event handlers
            gestureRecognizer.ManipulationStarted -= onManipulationStarted;
            gestureRecognizer.ManipulationUpdated -= onManipulationUpdated;
            gestureRecognizer.ManipulationCanceled -= onManipulationCanceled;
            gestureRecognizer.ManipulationCompleted -= onManipulationCompleted;
            gestureRecognizer.Tapped -= onTapEvent;

            gestureRecognizer.Dispose();
        }
#endif
    }

}
