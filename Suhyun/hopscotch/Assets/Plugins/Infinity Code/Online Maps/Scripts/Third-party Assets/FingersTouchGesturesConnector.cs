/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if FINGERS_TG
using DigitalRubyShared;
#endif

using UnityEngine;

namespace OnlineMaps
{
#if FINGERS_TG
    [RequireComponent(typeof(FingersScript))]
#endif
    [AddComponentMenu("Infinity Code/Online Maps/Third-Party Connectors/Fingers - Touch Gestures Connector")]
    [ThirdPartyPlugin("Fingers - Touch Gestures Connector", typeof(ControlBase))]
    public class FingersTouchGesturesConnector : MonoBehaviour
    {
#if FINGERS_TG
        public float scaleSpeed = 0.1f;
        public Vector2 speed = Vector2.one;

        private ScaleGestureRecognizer scaleGesture;
        private RotateGestureRecognizer rotateGesture;
        private ControlBase control;
        private CameraOrbit cameraOrbit;

        private void Start()
        {
            control = ControlBase.instance;
            cameraOrbit = CameraOrbit.instance;

            scaleGesture = new ScaleGestureRecognizer();
            scaleGesture.StateUpdated += ScaleGestureCallback;
            FingersScript.Instance.AddGesture(scaleGesture);

            if (cameraOrbit != null)
            {
                rotateGesture = new RotateGestureRecognizer();
                rotateGesture.StateUpdated += RotateGestureCallback;
                FingersScript.Instance.AddGesture(rotateGesture);
            }
        }

        private bool IsCursorOnUIElement()
        {
            Vector2 inputPosition = control.GetInputPosition();

            if (InputManager.touchSupported && InputManager.touchCount > 1)
            {
                Vector2 p1 = InputManager.GetTouch(0).position;
                Vector2 p2 = InputManager.GetTouch(1).position;

                inputPosition = Vector2.Lerp(p1, p2, 0.5f);
            }

            return control.IsCursorOnUIElement(inputPosition);
        }

        private void ScaleGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Executing && !IsCursorOnUIElement())
            {
                OnlineMaps.instance.floatZoom *= (scaleGesture.ScaleMultiplier - 1) * scaleSpeed + 1;
            }
        }

        private void RotateGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Executing && !IsCursorOnUIElement())
            {
                
                control.isMapDrag = false;
                if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += rotateGesture.RotationDegreesDelta * speed.y;
            }
        }
#endif
    }
}