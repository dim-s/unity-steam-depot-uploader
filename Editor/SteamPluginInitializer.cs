using UnityEditor;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    [InitializeOnLoad]
    public class SteamPluginInitializer
    {
        private const string InitializedKey = "SteamDepotUploader_Initialized";

        static SteamPluginInitializer()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (!EditorPrefs.GetBool(InitializedKey, false))
            {
                // Показываем окно настроек
                SteamGlobalSettingsWindow.ShowWindow();

                // Отмечаем, что плагин был инициализирован
                EditorPrefs.SetBool(InitializedKey, true);

                // Показываем приветственное сообщение
                EditorUtility.DisplayDialog("Steam Depot Uploader",
                    "Welcome to Steam Depot Uploader!\n\nThe global settings window has been opened. " +
                    "Please configure your SteamCMD and Account settings to get started. " +
                    "\n\nThen open 'Main Menu -> Tools -> Steam Depot Uploader' to upload your depots.",
                    "OK");
            }
        }
    }
}