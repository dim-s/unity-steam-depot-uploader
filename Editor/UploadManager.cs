using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class UploadManager
    {
        private SteamCmdManager steamCmdManager;
        private SteamAccountManager accountManager;
        private DepotManager depotManager;

        private StringBuilder outputLog = new StringBuilder();
        private Queue<string> outputQueue = new Queue<string>();
        private bool isUploading = false;
        
        private string currentProgressMessage = "Initializing upload...";
        private float currentProgress = 0f;
        
        private const string KEY_UPLOAD_DESCRIPTION = "SteamDepotUploader_uploadDescription";
        private const string KEY_FILE_EXCLUSIONS = "SteamDepotUploader_fileExclusions";
        private const string DEFAULT_UPLOAD_DESCRIPTION = "Steam Depot Uploader";
        private const string DEFAULT_FILE_EXCLUSIONS = "*.pdb,*.zip,*BurstDebug*,*DontShipIt*";

        public string UploadDescription { get; set; }

        public List<string> FileExclusions { get; set; }

        public UploadManager(SteamCmdManager steamCmdManager, SteamAccountManager accountManager, DepotManager depotManager)
        {
            this.steamCmdManager = steamCmdManager;
            this.accountManager = accountManager;
            this.depotManager = depotManager;
            UploadDescription = "Steam Depot Uploader";
            
            FileExclusions = new List<string> 
            { 
                "*.pdb",
                "*.zip",
                "*BurstDebug*",
                "*DontShipIt*",
            };
        }
        
        public void AddFileExclusion(string exclusion)
        {
            var normalizedExclusion = NormalizeExclusion(exclusion);
            if (!string.IsNullOrWhiteSpace(normalizedExclusion) && !FileExclusions.Contains(normalizedExclusion))
            {
                FileExclusions.Add(normalizedExclusion);
            }
        }

        public void RemoveFileExclusion(string exclusion)
        {
            var normalizedExclusion = NormalizeExclusion(exclusion);
            FileExclusions.Remove(normalizedExclusion);
        }

        private string NormalizeExclusion(string exclusion)
        {
            exclusion = exclusion.Trim();
            if (!exclusion.StartsWith("*") && !exclusion.EndsWith("*"))
            {
                exclusion = $"*{exclusion}*";
            }
            return exclusion;
        }

        public void BuildAndUploadToSteamDepot()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            buildPlayerOptions.locationPathName = Path.Combine(depotManager.GetAbsoluteBuildPath(), "Build.exe");
            buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
            buildPlayerOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded. Now uploading to Steam Depot...");
                UploadToSteamDepot();
            }
            else
            {
                Debug.LogError("Build failed. Check the console for more details.");
                EditorUtility.DisplayDialog("Build Failed", "The build has failed. Check the console for details.", "OK");
            }
        }

        public async Task UploadToSteamDepot()
        {
            if (!ValidateSettings())
            {
                return;
            }

            string vdfPath = Path.Combine(Application.dataPath, "steam_app_build.vdf");
            CreateAppBuildVdf(vdfPath, depotManager.GetAbsoluteBuildPath());

            isUploading = true;
            EditorApplication.update += UpdateProgress;

            try
            {
                UploadResult result = await RunSteamCmdProcess(vdfPath);
                
                if (result.ExitCode != -1) ShowUploadResultWindow(result);
            }
            finally
            {
                isUploading = false;
                EditorApplication.update -= UpdateProgress;
                EditorUtility.ClearProgressBar();
            }
        }

        private void UpdateProgress()
        {
            if (isUploading)
            {
                if (outputQueue.Count > 0)
                {
                    currentProgressMessage = outputQueue.Dequeue();
                    currentProgress = Mathf.Clamp01(currentProgress + 0.01f); // Increment progress slightly
                }
                EditorUtility.DisplayProgressBar("Uploading to Steam Depot", currentProgressMessage, currentProgress);
            }
        }

        private bool ValidateSettings()
        {
            if (!steamCmdManager.IsSteamCmdInstalled())
            {
                EditorUtility.DisplayDialog("Error", "SteamCMD path is invalid.", "OK");
                return false;
            }

            if (!accountManager.AreCredentialsSet())
            {
                EditorUtility.DisplayDialog("Error", "Steam account credentials are not set.", "OK");
                return false;
            }

            if (!depotManager.AreSettingsValid())
            {
                EditorUtility.DisplayDialog("Error", "Depot settings are invalid.", "OK");
                return false;
            }

            if (!depotManager.DoesBuildOutputDirectoryExist())
            {
                EditorUtility.DisplayDialog("Error", "Build output directory does not exist.", "OK");
                return false;
            }

            return true;
        }

        private void CreateAppBuildVdf(string path, string contentRoot)
        {
            var vdfContent = new List<string>
            {
                "\"AppBuild\"",
                "{",
                $"\t\"AppID\" \"{depotManager.AppId}\"",
                $"\t\"Desc\" \"{UploadDescription}\"",
                "\t\"BuildOutput\" \"BuildOutput/\"",
                $"\t\"ContentRoot\" \"{contentRoot}\"",
                "\t\"Depots\"",
                "\t{",
                $"\t\t\"{depotManager.DepotId}\"",
                "\t\t{",
                "\t\t\t\"FileMapping\"",
                "\t\t\t{",
                "\t\t\t\t\"LocalPath\" \"*\"",
                "\t\t\t\t\"DepotPath\" \".\"",
                "\t\t\t\t\"recursive\" \"1\"",
                "\t\t\t}"
            };

            foreach (var exclusion in FileExclusions)
            {
                vdfContent.Add($"\t\t\t\"FileExclusion\" \"{exclusion}\"");
            }

            vdfContent.AddRange(new[]
            {
                "\t\t}",
                "\t}",
                "}"
            });

            File.WriteAllLines(path, vdfContent);
        }

        private async Task<UploadResult> RunSteamCmdProcess(string vdfPath)
        {
            string arguments = $"+login {accountManager.Username} {accountManager.Password} {accountManager.AuthCode} +run_app_build {vdfPath} +quit";

            ProcessStartInfo startInfo = new ProcessStartInfo(steamCmdManager.SteamCmdPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            outputLog.Clear();
            outputQueue.Clear();
            currentProgress = 0f;
            
            int exitCode = -1;
            string uploadId = "";
            string buildId = "";
            bool authCodeRequired = false;

            using (Process steamCmdProcess = new Process())
            {
                steamCmdProcess.StartInfo = startInfo;
                steamCmdProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string logMessage = $"SteamCMD: {e.Data}";
                        Debug.Log(logMessage);
                        outputLog.AppendLine(logMessage);
                        outputQueue.Enqueue(logMessage);
                        
                        if (e.Data.Contains("Invalid Login Auth Code"))
                        {
                            authCodeRequired = true;
                            Debug.LogError("Invalid or missing two-factor authentication code.");
                        }

                        // Extract build ID from the output
                        Match buildIdMatch = Regex.Match(e.Data, @"BuildID (\d+)");
                        if (buildIdMatch.Success)
                        {
                            buildId = buildIdMatch.Groups[1].Value;
                            Debug.Log($"Extracted Build ID: {buildId}");
                        }
                        
                        // Extract upload ID from the output
                        if (e.Data.Contains("Depot build ID:"))
                        {
                            uploadId = e.Data.Split(':').Last().Trim();
                            Debug.Log($"Extracted Upload ID: {uploadId}");
                        }
                    }
                };
                steamCmdProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string errorMessage = $"SteamCMD Error: {e.Data}";
                        Debug.LogError(errorMessage);
                        outputLog.AppendLine(errorMessage);
                        outputQueue.Enqueue(errorMessage);
                    }
                };

                Debug.Log("Starting SteamCMD process for upload...");
                steamCmdProcess.Start();
                steamCmdProcess.BeginOutputReadLine();
                steamCmdProcess.BeginErrorReadLine();

                await Task.Run(() =>
                {
                    steamCmdProcess.WaitForExit();
                    exitCode = steamCmdProcess.ExitCode;
                });
            }

            Debug.Log("SteamCMD process exited.");
            Debug.Log($"Full output log:\n{outputLog}");

            if (authCodeRequired)
            {
                EditorUtility.DisplayDialog("Authentication Error",
                    "Invalid or missing two-factor authentication code. Please request a new auth code in the global settings window.",
                    "OK");
                SteamGlobalSettingsWindow.ShowWindow();
                return new UploadResult { Success = false, ExitCode = -1 };
            }

            return new UploadResult
            {
                Success = exitCode == 0,
                ExitCode = exitCode,
                UploadId = uploadId,
                BuildId = buildId,
                AppId = depotManager.AppId,
                DepotId = depotManager.DepotId,
                BuildPath = depotManager.GetAbsoluteBuildPath(),
                UploadTime = DateTime.Now,
                LogOutput = outputLog.ToString()
            };
        }
        
        private void ShowUploadResultWindow(UploadResult result)
        {
            UploadResultWindow.ShowWindow(result);
        }
        
        public void LoadSettings()
        {
            UploadDescription = SteamDepotUploaderPrefs.GetString(KEY_UPLOAD_DESCRIPTION, DEFAULT_UPLOAD_DESCRIPTION);
            FileExclusions = SteamDepotUploaderPrefs.GetString(KEY_FILE_EXCLUSIONS, DEFAULT_FILE_EXCLUSIONS)
                .Split(',')
                .Select(exclusion => exclusion.Trim())
                .ToList();
        }
        
        public void SaveSettings()
        {
            SteamDepotUploaderPrefs.SetString(KEY_UPLOAD_DESCRIPTION, UploadDescription);
            SteamDepotUploaderPrefs.SetString(KEY_FILE_EXCLUSIONS, string.Join(",", FileExclusions));
        }
    }
}