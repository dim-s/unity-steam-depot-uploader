using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Directory = UnityEngine.Windows.Directory;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class UIManager
    {
        private SteamDepotUploaderWindow window;
        private SteamCmdManager steamCmdManager;
        private SteamAccountManager accountManager;
        private DepotManager depotManager;
        private UploadManager uploadManager;

        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle buttonStyle;

        public UIManager(SteamDepotUploaderWindow window, SteamCmdManager steamCmdManager,
            SteamAccountManager accountManager, DepotManager depotManager, UploadManager uploadManager)
        {
            this.window = window;
            this.steamCmdManager = steamCmdManager;
            this.accountManager = accountManager;
            this.depotManager = depotManager;
            this.uploadManager = uploadManager;

            InitStyles();
        }

        public void DrawGUI()
        {
            UpdateStyles();
            LoadAllSettings();

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawGlobalSettingsSummary();
            DrawDepotSettings();
            DrawUploadSettings();

            GUILayout.Space(5);

            if (GUILayout.Button("Upload to Steam Depot", buttonStyle, GUILayout.Height(30)))
            {
                SaveAllSettings();
                uploadManager.UploadToSteamDepot();
            }

            if (GUILayout.Button("Build & Upload to Steam Depot", buttonStyle, GUILayout.Height(30)))
            {
                SaveAllSettings();
                uploadManager.BuildAndUploadToSteamDepot();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGlobalSettingsSummary()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("SteamCMD Status:", 
                steamCmdManager.IsSteamCmdInstalled() ? "Installed" : "Not Installed");
            EditorGUILayout.LabelField("Steam Account:", 
                string.IsNullOrEmpty(accountManager.Username) ? "Not Set" : accountManager.Username);

            if (GUILayout.Button("Open Global Settings"))
            {
                SteamGlobalSettingsWindow.ShowWindow();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSteamCMDSettings()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("SteamCMD Settings", EditorStyles.boldLabel);

            string expectedPath = Path.GetDirectoryName(steamCmdManager.SteamCmdPath);
            bool isInstalled = steamCmdManager.IsSteamCmdInstalled();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                isInstalled ? "SteamCMD Path:" : "Expected SteamCMD Path:", expectedPath);

            bool folderExists = Directory.Exists(expectedPath);

            EditorGUI.BeginDisabledGroup(!folderExists);
            if (GUILayout.Button("Open in Explorer", GUILayout.Width(120)))
            {
                OpenInFileExplorer(expectedPath);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (isInstalled)
            {
                EditorGUILayout.LabelField("Executable:", Path.GetFileName(steamCmdManager.SteamCmdPath));
                EditorGUILayout.LabelField("Installation Status:", "Complete");

                EditorGUILayout.HelpBox(
                    "SteamCMD is installed in a special folder to avoid issues with non-English characters in the path. " +
                    "This ensures compatibility with Steam's requirements.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "SteamCMD is not installed or the installation is incomplete. " +
                    "Click the button below to install or reinstall it. " +
                    "SteamCMD will be installed in the path shown above to ensure compatibility.",
                    MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();

            if (!isInstalled)
            {
                if (GUILayout.Button(new GUIContent(" Install SteamCMD", EditorGUIUtility.FindTexture("d_Download"))))
                {
                    InstallOrReinstallSteamCMD();
                }
            }
            else
            {
                if (GUILayout.Button("Reinstall SteamCMD"))
                {
                    if (EditorUtility.DisplayDialog("Reinstall SteamCMD",
                            "Are you sure you want to reinstall SteamCMD? This will delete the current installation.",
                            "Yes", "No"))
                    {
                        InstallOrReinstallSteamCMD();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void InstallOrReinstallSteamCMD()
        {
            steamCmdManager.InstallSteamCMD().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    Debug.LogError($"Failed to install SteamCMD: {task.Exception}");
                }
                else
                {
                    Debug.Log("SteamCMD installation completed.");
                }

                window.Repaint();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OpenInFileExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
                }
                else if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    System.Diagnostics.Process.Start("open", folderPath);
                }
                else
                {
                    EditorUtility.RevealInFinder(folderPath);
                }
            }
        }

        private void DrawSteamAccountSettings()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Steam Account", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            accountManager.Username = EditorGUILayout.TextField("Username", accountManager.Username);
            accountManager.Password = EditorGUILayout.PasswordField("Password", accountManager.Password);

            EditorGUILayout.BeginHorizontal();
            accountManager.AuthCode = EditorGUILayout.PasswordField("Auth Code", accountManager.AuthCode);
            if (GUILayout.Button("Request Code", GUILayout.Width(100)))
            {
                accountManager.RequestAuthCode(steamCmdManager.SteamCmdPath);
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                SaveAllSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDepotSettings()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Depot Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            depotManager.AppId = EditorGUILayout.TextField("App ID", depotManager.AppId);
            depotManager.DepotId = EditorGUILayout.TextField("Depot ID", depotManager.DepotId);

            EditorGUILayout.BeginHorizontal();
            depotManager.BuildOutputPath = EditorGUILayout.TextField("Build Output Path", depotManager.BuildOutputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedPath =
                    EditorUtility.OpenFolderPanel("Select Build Output Folder", depotManager.BuildOutputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    depotManager.BuildOutputPath = depotManager.GetRelativePath(selectedPath);
                    SaveAllSettings();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                SaveAllSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUploadSettings()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Upload Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            uploadManager.UploadDescription = EditorGUILayout.TextField("Description", uploadManager.UploadDescription);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("File Exclusions:");

            for (int i = 0; i < uploadManager.FileExclusions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                uploadManager.FileExclusions[i] = EditorGUILayout.TextField(uploadManager.FileExclusions[i]);
                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"), GUILayout.Width(25),
                        GUILayout.Height(18)))
                {
                    uploadManager.FileExclusions.RemoveAt(i);
                    i--;
                    SaveAllSettings();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Add Exclusion", EditorGUIUtility.IconContent("Toolbar Plus").image)))
            {
                uploadManager.AddFileExclusion("*.extension");
                SaveAllSettings();
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                SaveAllSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private void InitStyles()
        {
            headerStyle = new GUIStyle();
            sectionStyle = new GUIStyle();
            buttonStyle = new GUIStyle();
        }

        private void UpdateStyles()
        {
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.margin = new RectOffset(0, 0, 10, 10);

            sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 10, 10)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }

        private void LoadAllSettings()
        {
            steamCmdManager.LoadSettings();
            accountManager.LoadSettings();
        }

        private void SaveAllSettings()
        {
            steamCmdManager.SaveSettings();
            accountManager.SaveSettings();
            depotManager.SaveSettings();
            uploadManager.SaveSettings();
        }
    }
}