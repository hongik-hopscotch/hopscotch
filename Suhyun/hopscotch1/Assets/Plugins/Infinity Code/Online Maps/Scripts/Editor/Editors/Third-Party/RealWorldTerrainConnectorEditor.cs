/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OnlineMaps.Editors
{
    [CustomEditor(typeof(RealWorldTerrainConnector))]
    public class RealWorldTerrainConnectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
#if !RWT && !RWT3
            if (GUILayout.Button("Enable Real World Terrain"))
            {
                if (EditorUtility.DisplayDialog("Enable Real World Terrain", "You have Real World Terrain in your project?", "Yes, I have Real World Terrain", "Cancel"))
                {
                    Assembly assembly = typeof(RealWorldTerrainConnectorEditor).Assembly;
                    if (assembly.GetType("InfinityCode.RealWorldTerrain.Windows.RealWorldTerrainWindow") != null)
                    {
                        EditorUtils.AddCompilerDirective("RWT3");
                    }
                    else
                    {
                        EditorUtils.AddCompilerDirective("RWT");
                    }
                }
            }

#else
            RWTConnector connector = (RWTConnector)target;

            connector.mode = (RWTConnector.Mode) EditorGUILayout.EnumPopup("Mode: ", connector.mode);

            if (connector.mode == RWTConnector.Mode.markerOnPosition)
            {
                connector.markerTexture = (Texture2D)EditorGUILayout.ObjectField("Marker Texture", connector.markerTexture, typeof (Texture2D), false);
                connector.markerLabel = EditorGUILayout.TextField("Marker Tooltip:", connector.markerLabel);
            }

            connector.positionMode = (RWTConnector.PositionMode) EditorGUILayout.EnumPopup("Position mode: ", connector.positionMode);

            if (connector.positionMode == RWTConnector.PositionMode.transform)
            {
                connector.targetTransform = (Transform) EditorGUILayout.ObjectField("Target Transform", connector.targetTransform, typeof (Transform), true);
            }
            else if (connector.positionMode == RWTConnector.PositionMode.scenePosition)
            {
                connector.scenePosition = EditorGUILayout.Vector3Field("Position: ", connector.scenePosition);
            }
            else if (connector.positionMode == RWTConnector.PositionMode.coordinates)
            {
                connector.coordinates = EditorGUILayout.Vector2Field("Coordinates: ", connector.coordinates);
            }
#endif
        }
    }
}