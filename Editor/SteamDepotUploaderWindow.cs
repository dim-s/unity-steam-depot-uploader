using UnityEditor;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class SteamDepotUploaderWindow : EditorWindow
    {
        private SteamCmdManager steamCmdManager;
        private SteamAccountManager accountManager;
        private DepotManager depotManager;
        private UploadManager uploadManager;
        private UIManager uiManager;

        [MenuItem("Tools/Steam Depot Uploader")]
        public static void ShowWindow()
        {
            GetWindow<SteamDepotUploaderWindow>("Steam Depot Uploader");
        }

        void OnEnable()
        {
            steamCmdManager = new SteamCmdManager();
            accountManager = new SteamAccountManager();
            depotManager = new DepotManager();
            uploadManager = new UploadManager(steamCmdManager, accountManager, depotManager);
            uiManager = new UIManager(this, steamCmdManager, accountManager, depotManager, uploadManager);
            LoadSettings();

            EditorApplication.quitting += OnEditorQuitting;
        }

        void OnDisable()
        {
            EditorApplication.quitting -= OnEditorQuitting;
            SaveSettings();
        }

        void OnLostFocus()
        {
            SaveSettings();
        }

        void OnGUI()
        {
            uiManager.DrawGUI();
        }

        private void LoadSettings()
        {
            steamCmdManager.LoadSettings();
            accountManager.LoadSettings();
            depotManager.LoadSettings();
            uploadManager.LoadSettings();
        }

        public void SaveSettings()
        {
            steamCmdManager.SaveSettings();
            accountManager.SaveSettings();
            depotManager.SaveSettings();
            uploadManager.SaveSettings();
        }

        private void OnEditorQuitting()
        {
            SaveSettings();
        }

        // Метод для вызова после каждого значимого изменения настроек
        public void SaveSettingsAfterChange()
        {
            SaveSettings();
        }
    }
}