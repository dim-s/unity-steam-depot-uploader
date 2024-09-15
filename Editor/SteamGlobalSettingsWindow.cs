using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class SteamGlobalSettingsWindow : EditorWindow
    {
        private SteamCmdManager steamCmdManager;
        private SteamAccountManager accountManager;
        private Vector2 scrollPosition;
        private const float WindowWidth = 450f;
        private float minHeight;
        private bool isInstalling = false;
        private float installProgress = 0f;
        private GUIStyle defaultButtonStyle;

        public static void ShowWindow()
        {
            var window = GetWindow<SteamGlobalSettingsWindow>("Steam Global Settings");
            window.minSize = new Vector2(WindowWidth, 100f);
            window.maxSize = new Vector2(WindowWidth, 1000f);
        }

        private void OnEnable()
        {
            steamCmdManager = new SteamCmdManager();
            accountManager = new SteamAccountManager();
        }

        private void OnGUI()
        {
            // Создаем стиль для кнопки
            defaultButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 30, // Увеличиваем высоту кнопки
                fontSize = 12, // Увеличиваем размер шрифта
                padding = new RectOffset(10, 10, 5, 5), // Добавляем отступы
                alignment = TextAnchor.MiddleCenter // Центрируем содержимое
            };
            
            minHeight = 0;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            DrawSteamCMDSettings();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            DrawSteamAccountSettings();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                float contentHeight = GUILayoutUtility.GetLastRect().yMax;
                if (Math.Abs(position.height - contentHeight) > 1)
                {
                    position = new Rect(position.x, position.y, WindowWidth, contentHeight);
                    minSize = maxSize = new Vector2(WindowWidth, contentHeight);
                }
            }
        }

        private void DrawSteamCMDSettings()
        {
            EditorGUILayout.LabelField("SteamCMD Settings", EditorStyles.boldLabel);
        
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Install Path:");
        
            string currentPath = steamCmdManager.CustomInstallPath ?? 
                                 steamCmdManager.GetDefaultSteamCmdFolder();
            string newPath = EditorGUILayout.TextField(currentPath);
        
            if (newPath != currentPath)
            {
                steamCmdManager.SetCustomInstallPath(newPath);
            }
        
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select SteamCMD Install Directory", currentPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    steamCmdManager.SetCustomInstallPath(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            bool isInstalled = steamCmdManager.IsSteamCmdInstalled(currentPath);
            EditorGUILayout.LabelField("Installation Status:", isInstalled ? "Installed" : "Not Installed");

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isInstalling;
            
            // Создаем GUIContent с текстом и иконкой
            GUIContent installButtonContent = new GUIContent(
                isInstalled ? "Reinstall SteamCMD" : "Install SteamCMD",
                EditorGUIUtility.FindTexture("downloadicon") // Используем стандартную иконку загрузки
            );

            // Рисуем кнопку во всю ширину окна
            if (GUILayout.Button(installButtonContent, defaultButtonStyle, GUILayout.ExpandWidth(true)))
            {
                InstallSteamCMD();
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (isInstalling)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20f), installProgress, "Installing...");
            }
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
        
        private async void InstallSteamCMD()
        {
            isInstalling = true;
            installProgress = 0f;

            IProgress<float> progress = new Progress<float>(p =>
            {
                installProgress = p;
                Repaint();
            });

            await steamCmdManager.InstallSteamCMD(progress);

            isInstalling = false;
            Repaint();
        }

        private void DrawSteamAccountSettings()
        {
            EditorGUILayout.LabelField("Steam Account Settings", EditorStyles.boldLabel);
            accountManager.Username = EditorGUILayout.TextField("Username", accountManager.Username);
            accountManager.Password = EditorGUILayout.PasswordField("Password", accountManager.Password);

            EditorGUILayout.BeginHorizontal();
            accountManager.AuthCode = EditorGUILayout.TextField("Auth Code", accountManager.AuthCode);
        
            EditorGUI.BeginDisabledGroup(accountManager.IsRequestingAuthCode());
            if (GUILayout.Button("Request Code", GUILayout.Width(100)))
            {
                RequestAuthCode();
            }
            EditorGUI.EndDisabledGroup();
        
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Account Settings", defaultButtonStyle, GUILayout.ExpandWidth(true)))
            {
                accountManager.SaveSettings();
                EditorUtility.DisplayDialog("Settings Saved", "Steam account settings have been saved.", "OK");
            }
        }

        private async void RequestAuthCode()
        {
            await accountManager.RequestAuthCode(steamCmdManager.SteamCmdPath);
            Repaint(); // Обновляем окно после завершения запроса
        }
    }
}