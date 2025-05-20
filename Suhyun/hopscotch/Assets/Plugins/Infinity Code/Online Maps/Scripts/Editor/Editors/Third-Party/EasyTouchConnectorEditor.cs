/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

namespace OnlineMaps.Editors
{
    [CustomEditor(typeof(EasyTouchConnector))]
    public class EasyTouchConnectorEditor : Editor
    {
#if EASYTOUCH
        private CameraOrbit cameraOrbit;
        private EasyTouchConnector connector;
        private SerializedProperty forwarder;

        private void OnEnable()
        {
            connector = target as EasyTouchConnector;
            cameraOrbit = connector.GetComponent<CameraOrbit>();
            forwarder = serializedObject.FindProperty("forwarder");
        }
#endif

        public override void OnInspectorGUI()
        {
#if !EASYTOUCH
            if (GUILayout.Button("Enable EasyTouch"))
            {
                if (EditorUtility.DisplayDialog("Enable EasyTouch", "You have EasyTouch in your project?", "Yes, I have EasyTouch", "Cancel"))
                {
                    EditorUtils.AddCompilerDirective("EASYTOUCH");
                }
            }
#else
            serializedObject.Update();
            EditorGUILayout.PropertyField(forwarder);
            serializedObject.ApplyModifiedProperties();

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