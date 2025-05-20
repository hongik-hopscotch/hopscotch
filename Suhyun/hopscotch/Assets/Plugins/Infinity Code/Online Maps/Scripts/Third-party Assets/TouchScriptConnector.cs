/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if TOUCHSCRIPT
using TouchScript.Gestures.TransformGestures;
#endif

using System;
using UnityEngine;

namespace OnlineMaps
{
    [ThirdPartyPlugin("TouchScript Connector", typeof(ControlBase))]
    [AddComponentMenu("Infinity Code/Online Maps/Third-Party Connectors/TouchScript Connector")]
    public class TouchScriptConnector : MonoBehaviour
    {
#if TOUCHSCRIPT
        public ScreenTransformGesture gesture;
        public RotationMode rotationMode = RotationMode.camera;

        private ControlBase control;
        private ControlBaseDynamicMesh dmControl;
        private CameraOrbit cameraOrbit;
        private MouseController mouseController;
        private Vector2 speed;

        private void Start()
        {
            if (gesture == null)
            {
                Debug.LogWarning("TouchScript Connector / Gesture cannot be null");
                Destroy(this);
                return;
            }

            control = ControlBase.instance;
            mouseController = control.GetComponent<MouseController>();
            mouseController.allowZoom = false;

            dmControl = control as ControlBaseDynamicMesh;

            cameraOrbit = GetComponent<CameraOrbit>();

            if (cameraOrbit != null)
            {
                speed = cameraOrbit.speed;
                cameraOrbit.speed = Vector2.zero;
            }

            gesture.Transformed += GestureOnTransformed;
        }

        private void GestureOnTransformed(object sender, EventArgs eventArgs)
        {
            if (gesture.NumPointers != 2) return;
            mouseController.isMapDrag = false;
            float deltaScale = gesture.DeltaScale - 1;

            if (mouseController.zoomMode == ZoomMode.center) control.map.view.zoom += deltaScale * mouseController.zoomSensitivity;
            else control.ZoomOnPoint(deltaScale * mouseController.zoomSensitivity, gesture.ScreenPosition);

            if (rotationMode == RotationMode.camera)
            {
                if (cameraOrbit != null)
                {
                    if (!cameraOrbit.lockTilt) cameraOrbit.rotation.x += gesture.DeltaPosition.y * speed.x;
                    if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += gesture.DeltaRotation * speed.y;
                }
            }
            else if (dmControl != null) RotateMap(gesture);
        }

        private void RotateMap(ScreenTransformGesture gesture)
        {
            GeoPoint l1 = control.ScreenToLocation(gesture.ScreenPosition);

            Vector3 center = dmControl.center;
            center = control.transform.localToWorldMatrix.MultiplyPoint(center);
            Vector3 pos = control.transform.position - center;
            pos = Quaternion.Euler(0, -gesture.DeltaRotation * speed.y, 0) * pos + center;
            control.transform.position = pos;
            control.transform.Rotate(0, -gesture.DeltaRotation * speed.y, 0);

            GeoPoint l2 = control.ScreenToLocation(gesture.ScreenPosition);
            control.map.view.center -= l2 - l1;
        }

        public enum RotationMode
        {
            camera,
            map
        }
#endif
    }
}