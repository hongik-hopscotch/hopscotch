/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

namespace OnlineMaps.Editors
{
    [CustomEditor(typeof(TouchScriptConnector))]
    public class TouchScriptConnectorEditor : Editor
    {
#if TOUCHSCRIPT
        private CameraOrbit cameraOrbit;
        private TouchScriptConnector connector;

        private void OnEnable()
        {
            connector = target as TouchScriptConnector;
            cameraOrbit = connector.GetComponent<CameraOrbit>();
        }
#endif

        public override void OnInspectorGUI()
        {
#if !TOUCHSCRIPT
            if (GUILayout.Button("Enable TouchScript"))
            {
                if (EditorUtility.DisplayDialog("Enable TouchScript", "You have TouchScript in your project?", "Yes, I have TouchScript", "Cancel"))
                {
                    EditorUtils.AddCompilerDirective("TOUCHSCRIPT");
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