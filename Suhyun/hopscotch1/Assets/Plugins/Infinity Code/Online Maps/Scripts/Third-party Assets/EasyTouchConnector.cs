/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if EASYTOUCH
using HedgehogTeam.EasyTouch;
#endif

using System;
using UnityEngine;

namespace OnlineMaps
{
    [AddComponentMenu("Infinity Code/Online Maps/Third-Party Connectors/EasyTouch Connector")]
    [ThirdPartyPlugin("EasyTouch Connector", typeof(ControlBase))]
    public class EasyTouchConnector:MonoBehaviour
    {
#if EASYTOUCH
        public RawImageTouchForwarder forwarder;

        private Vector2 speed = Vector2.one;

        private ControlBase control;
        private CameraOrbit cameraOrbit;

        private void EasyTouchOnOnTwist(Gesture gesture)
        {
            control.isMapDrag = false;
            if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += gesture.twistAngle * speed.y;
        }

        private void EasyTouchOnOnDrag2Fingers(Gesture gesture)
        {
            control.isMapDrag = false;
            if (!cameraOrbit.lockTilt) cameraOrbit.rotation.x += gesture.deltaPosition.y * speed.x * 0.5f;
        }

        private void EasyTouchOnOnPinch(Gesture gesture)
        {
            control.isMapDrag = false;
            float delta = gesture.deltaPinch / 100;
            if (control.zoomMode == ZoomMode.center) Map.instance.floatZoom += delta;
            else
            {
                Vector2 pos = gesture.position;
                if (forwarder != null) pos = forwarder.ForwarderToMapSpace(pos);
                control.ZoomOnPoint(delta, pos);
            }
        }

        private void Start()
        {
            control = ControlBase.instance;
            control.allowZoom = false;

            EasyTouch.On_Pinch += EasyTouchOnOnPinch;

            cameraOrbit = GetComponent<CameraOrbit>();
            if (cameraOrbit != null)
            {
                speed = cameraOrbit.speed;
                cameraOrbit.speed = Vector2.zero;
                EasyTouch.On_Drag2Fingers += EasyTouchOnOnDrag2Fingers;
                EasyTouch.On_Twist += EasyTouchOnOnTwist;
            }
        }
#endif
    }
}