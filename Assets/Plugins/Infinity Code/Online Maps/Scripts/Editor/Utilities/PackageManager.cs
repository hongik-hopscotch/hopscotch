/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

namespace OnlineMaps.Editors
{
    public class PackageManager
    {
        [MenuItem(EditorUtils.MenuPath + "Playmaker Integration Kit", false, 1)]
        public static void ImportPlayMakerIntegrationKit()
        {
            EditorUtils.ImportPackage("Packages\\OnlineMaps-Playmaker-Integration-Kit.unitypackage", 
                new EditorUtils.Warning
                {
                    title = "Playmaker Integration Kit",
                    message = "You have Playmaker in your project?",
                    ok = "Yes, I have a Playmaker"
                },
                "Could not find Playmaker Integration Kit."
            );
        }

        [MenuItem(EditorUtils.MenuPath + "Visual Scripting Integration Kit", false, 1)]
        public static void ImportVisualScriptingIntegrationKit()
        {
            EditorUtils.ImportPackage("Packages\\OnlineMaps-Visual-Scripting-Integration-Kit.unitypackage", 
                new EditorUtils.Warning
                {
                    title = "Visual Scripting Integration Kit",
                    message = "You have Visual Scripting in your project?",
                    ok = "Yes, I have a Visual Scripting"
                },
                "Could not find Visual Scripting Integration Kit."
            );
        }
    }
}