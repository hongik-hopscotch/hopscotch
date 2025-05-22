/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

namespace OnlineMaps.Editors
{
    [CustomEditor(typeof(PanoConnector))]
    public class PanoConnectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
#if !UPANO
            EditorGUILayout.HelpBox("Import uPano into the project.\nOpen Window / Infinity Code / uPano / Extension Manager.\nDownload and import Google Street View Service.\nClick Enable uPano button.", MessageType.Info);

            if (GUILayout.Button("Enable uPano"))
            {
                if (EditorUtility.DisplayDialog("Enable uPano", "You have uPano and Google Street View Service in your project?", "Yes, I have uPano", "Cancel"))
                {
                    EditorUtils.AddCompilerDirective("UPANO");
                }
            }
#else
            base.OnInspectorGUI();
#endif
        }

        private void OnEnable()
        {
            PanoConnector connector = target as PanoConnector;
            if (!connector.shader) connector.shader = RenderPipelineHelper.GetUnlitShader();
        }
    }
}