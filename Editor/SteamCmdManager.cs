using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class SteamCmdManager
    {
        private const string STEAMCMD_DOWNLOAD_URL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        private const string STEAMCMD_FOLDER = "SteamCMD_Unity";

        public string SteamCmdPath { get; private set; }
        public string CustomInstallPath { get; private set; }

        public SteamCmdManager()
        {
            LoadSettings();
        }

        public void SetCustomInstallPath(string path)
        {
            CustomInstallPath = path;
            SaveSettings();
        }

        public async Task InstallSteamCMD(IProgress<float> progress = null)
        {
            string extractPath = CustomInstallPath ?? GetSteamCmdFolder();

            // Check if SteamCMD is already installed in the selected directory
            if (IsSteamCmdInstalled(extractPath))
            {
                SteamCmdPath = GetSteamCmdExecutablePath(extractPath);
                SaveSettings();
                EditorUtility.DisplayDialog("Info", "SteamCMD is already installed in the selected directory.", "OK");
                return;
            }

            string downloadPath = Path.Combine(Path.GetTempPath(), "steamcmd.zip");

            try
            {
                // Download SteamCMD
                progress?.Report(0.1f);
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        progress?.Report(0.1f + 0.3f * e.ProgressPercentage / 100f);
                    };
                    await client.DownloadFileTaskAsync(STEAMCMD_DOWNLOAD_URL, downloadPath);
                }

                // Extract SteamCMD
                progress?.Report(0.4f);
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(downloadPath, extractPath);

                // Delete the zip file
                progress?.Report(0.5f);
                File.Delete(downloadPath);

                SteamCmdPath = GetSteamCmdExecutablePath(extractPath);
                SaveSettings();

                // Initialize SteamCMD
                progress?.Report(0.6f);
                await InitializeSteamCMD(progress);

                progress?.Report(1f);
                EditorUtility.DisplayDialog("Success", "SteamCMD has been successfully installed and initialized.",
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to install or initialize SteamCMD: {e.Message}", "OK");
                Debug.LogError($"SteamCMD installation/initialization error: {e}");
            }
        }

        private async Task InitializeSteamCMD(IProgress<float> progress = null)
        {
            string arguments = "+quit";

            ProcessStartInfo startInfo = new ProcessStartInfo(SteamCmdPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(SteamCmdPath)
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        Debug.Log($"SteamCMD: {e.Data}");
                        if (e.Data.Contains("Loading Steam API...") || e.Data.Contains("Logged in OK"))
                        {
                            progress?.Report(0.7f);
                        }
                        else if (e.Data.Contains("Waiting for user info...Done"))
                        {
                            progress?.Report(0.9f);
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        Debug.LogWarning($"SteamCMD Error: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                // Проверяем выходной код и вывод процесса
                if (process.ExitCode == 7)
                {
                    Debug.Log("SteamCMD initialization completed successfully with exit code 7.");
                }
                else if (process.ExitCode != 0)
                {
                    Debug.LogWarning($"SteamCMD exited with unexpected non-zero code: {process.ExitCode}");
                    throw new Exception(
                        $"SteamCMD initialization failed. Exit code: {process.ExitCode}. Error: {error}");
                }

                // Дополнительная проверка на успешность инициализации
                string steamDllPath = Path.Combine(Path.GetDirectoryName(SteamCmdPath), "steam.dll");
                if (!File.Exists(steamDllPath))
                {
                    throw new Exception("SteamCMD initialization failed: steam.dll not found after initialization.");
                }

                Debug.Log($"Full SteamCMD output:\n{output}");
                File.WriteAllText(Path.Combine(Application.dataPath, "SteamCMD_log.txt"), output.ToString());
            }
        }

        public bool IsSteamCmdInstalled(string path = null)
        {
            path = path ?? Path.GetDirectoryName(SteamCmdPath);
            string executablePath = GetSteamCmdExecutablePath(path);
            return File.Exists(executablePath) && Directory.GetFiles(path).Length > 1;
        }

        public void LoadSettings()
        {
            CustomInstallPath = EditorPrefs.GetString("SteamDepotUploader_customInstallPath", null);
            SteamCmdPath = EditorPrefs.GetString("SteamDepotUploader_steamCmdPath",
                GetSteamCmdExecutablePath(CustomInstallPath ?? GetSteamCmdFolder()));
        }

        public void SaveSettings()
        {
            EditorPrefs.SetString("SteamDepotUploader_customInstallPath", CustomInstallPath);
            EditorPrefs.SetString("SteamDepotUploader_steamCmdPath", SteamCmdPath);
        }

        private string GetSteamCmdFolder()
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
            return Path.Combine(systemDrive, STEAMCMD_FOLDER);
        }

        private string GetSteamCmdExecutablePath(string folder)
        {
            string fileName = Application.platform == RuntimePlatform.WindowsEditor ? "steamcmd.exe" : "steamcmd.sh";
            return Path.Combine(folder, fileName);
        }
        
        public string GetDefaultSteamCmdFolder()
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
            return Path.Combine(systemDrive, STEAMCMD_FOLDER);
        }
    }
}