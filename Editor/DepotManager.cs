using System.IO;
using UnityEngine;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    /// <summary>
    /// Manages Steam depot settings and build output paths.
    /// </summary>
    public class DepotManager
    {
        // Constants for default values
        private const string DEFAULT_APP_ID = "";
        private const string DEFAULT_DEPOT_ID = "";
        private const string DEFAULT_BUILD_OUTPUT_PATH = "Build";

        // Constants for EditorUserSettings keys
        private const string KEY_APP_ID = "SteamDepotUploader_appId";
        private const string KEY_DEPOT_ID = "SteamDepotUploader_depotId";
        private const string KEY_BUILD_OUTPUT_PATH = "SteamDepotUploader_buildOutputPath";

        // Properties for Steam depot settings
        public string AppId {get; set;}
        
        public string DepotId {get; set;}
        
        public string BuildOutputPath {get; set;}

        /// <summary>
        /// Initializes a new instance of the DepotManager class with default values.
        /// </summary>
        public DepotManager()
        {
            AppId = DEFAULT_APP_ID;
            DepotId = DEFAULT_DEPOT_ID;
            BuildOutputPath = DEFAULT_BUILD_OUTPUT_PATH;
        }

        /// <summary>
        /// Gets the absolute path to the build output directory.
        /// </summary>
        /// <returns>The absolute path to the build output directory.</returns>
        public string GetAbsoluteBuildPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildOutputPath));
        }

        /// <summary>
        /// Checks if the build output directory exists.
        /// </summary>
        /// <returns>True if the directory exists, false otherwise.</returns>
        public bool DoesBuildOutputDirectoryExist()
        {
            return Directory.Exists(GetAbsoluteBuildPath());
        }

        /// <summary>
        /// Creates the build output directory if it doesn't exist.
        /// </summary>
        public void CreateBuildOutputDirectoryIfNotExists()
        {
            string path = GetAbsoluteBuildPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created build output directory: {path}");
            }
        }

        /// <summary>
        /// Validates the current settings.
        /// </summary>
        /// <returns>True if all settings are valid, false otherwise.</returns>
        public bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(AppId) || string.IsNullOrEmpty(DepotId) || string.IsNullOrEmpty(BuildOutputPath))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            AppId = DEFAULT_APP_ID;
            DepotId = DEFAULT_DEPOT_ID;
            BuildOutputPath = DEFAULT_BUILD_OUTPUT_PATH;
        }

        /// <summary>
        /// Gets the relative path of a file or directory within the build output directory.
        /// </summary>
        /// <param name="path">The full path to the file or directory.</param>
        /// <returns>The relative path within the build output directory.</returns>
        public string GetRelativePath(string path)
        {
            string absoluteBuildPath = GetAbsoluteBuildPath()
                .Replace(Path.DirectorySeparatorChar, '/');
            
            if (path.Replace(Path.DirectorySeparatorChar, '/').StartsWith(absoluteBuildPath))
            {
                return path
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Substring(absoluteBuildPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar)
                    .TrimStart('/');
            }

            return path;
        }
        
        public void LoadSettings()
        {
            AppId = SteamDepotUploaderPrefs.GetString(KEY_APP_ID, DEFAULT_APP_ID);
            DepotId = SteamDepotUploaderPrefs.GetString(KEY_DEPOT_ID, DEFAULT_DEPOT_ID);
            BuildOutputPath = SteamDepotUploaderPrefs.GetString(KEY_BUILD_OUTPUT_PATH, DEFAULT_BUILD_OUTPUT_PATH);
        }
        
        public void SaveSettings()
        {
            SteamDepotUploaderPrefs.SetString(KEY_APP_ID, AppId);
            SteamDepotUploaderPrefs.SetString(KEY_DEPOT_ID, DepotId);
            SteamDepotUploaderPrefs.SetString(KEY_BUILD_OUTPUT_PATH, BuildOutputPath);
        }
    }
}