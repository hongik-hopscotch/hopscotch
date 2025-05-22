/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

namespace OnlineMaps.Editors
{
    [CustomEditor(typeof(FingersTouchGesturesConnector))]
    public class FingersTouchGesturesConnectorEditor : Editor
    {
#if FINGERS_TG
        private CameraOrbit cameraOrbit;
        private FingersTouchGesturesConnector connector;

        private void OnEnable()
        {
            connector = target as FingersTouchGesturesConnector;
            cameraOrbit = connector.GetComponent<CameraOrbit>();
        }
#endif

        public override void OnInspectorGUI()
        {
#if !FINGERS_TG
            if (GUILayout.Button("Enable Fingers - Touch Gestures"))
            {
                if (EditorUtility.DisplayDialog("Enable Fingers - Touch Gestures", "You have Fingers - Touch Gestures in your project?", "Yes, I have Fingers - Touch Gestures", "Cancel"))
                {
                    EditorUtils.AddCompilerDirective("FINGERS_TG");
                }
            }
#else 
            base.OnInspectorGUI();
            if (cameraOrbit == null)
            {
                EditorGUILayout.HelpBox("To use twist and tilt gestures, add Camera Orbit component.", MessageType.Warning);
                if (GUILayout.Button("Add Camera Orbit"))
                {
                    connector.gameObject.AddComponent<CameraOrbit>();
                }
            }
#endif
        }
    }
}