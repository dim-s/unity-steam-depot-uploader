using UnityEditor;
using UnityEngine;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class UploadResultWindow : EditorWindow
    {
        private UploadResult result;
        private Vector2 scrollPosition;

        public static void ShowWindow(UploadResult uploadResult)
        {
            UploadResultWindow window = GetWindow<UploadResultWindow>("Upload Result");
            window.result = uploadResult;
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            if (result == null) return;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Upload Result", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Status with icon
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(100));
            
            // Use built-in editor icons
            GUIContent statusContent = new GUIContent(
                result.Success ? "Success" : "Failed",
                result.Success ? EditorGUIUtility.FindTexture("console.infoicon") : EditorGUIUtility.FindTexture("console.erroricon")
            );
            EditorGUILayout.LabelField(statusContent);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Exit Code:", result.ExitCode.ToString());
            EditorGUILayout.LabelField("Build ID:", string.IsNullOrEmpty(result.BuildId) ? "Not found" : result.BuildId);
            EditorGUILayout.LabelField("App ID:", result.AppId);
            EditorGUILayout.LabelField("Depot ID:", result.DepotId);
            EditorGUILayout.LabelField("Build Path:", result.BuildPath);
            EditorGUILayout.LabelField("Upload Time:", result.UploadTime.ToString());

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(result.BuildId))
            {
                if (GUILayout.Button("Open Build Details in Steam Partner"))
                {
                    string url = $"https://partner.steamgames.com/apps/builddetails/{result.AppId}/{result.BuildId}";
                    Application.OpenURL(url);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Build ID not found. Unable to generate Steam Partner link.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Log Output", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(result.LogOutput, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("OK"))
            {
                Close();
            }
        }
    }
}