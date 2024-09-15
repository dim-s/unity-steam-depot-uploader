using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    public class SteamAccountManager
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthCode { get; set; }

        private readonly StringBuilder outputLog = new StringBuilder();

        private bool isRequestingAuthCode = false;

        public SteamAccountManager()
        {
            LoadSettings();
        }

        public async Task RequestAuthCode(string steamCmdPath)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                EditorUtility.DisplayDialog("Error", "Username or password is empty. Cannot request auth code.", "OK");
                return;
            }

            isRequestingAuthCode = true;
            EditorApplication.update += UpdateProgress;

            string arguments = $"+login {Username} {Password} +quit";
            ProcessStartInfo startInfo = new ProcessStartInfo(steamCmdPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            bool authCodeSent = false;
            string authMethod = "";

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            string logMessage = $"SteamCMD: {e.Data}";
                            Debug.Log(logMessage);
                            outputLog.AppendLine(logMessage);

                            if (e.Data.Contains("Steam Guard code:"))
                            {
                                authCodeSent = true;
                                authMethod = "email";
                            }
                            else if (e.Data.Contains("Two-factor code:"))
                            {
                                authCodeSent = true;
                                authMethod = "mobile app";
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    await Task.Run(() => process.WaitForExit());
                }

                Debug.Log("Auth Code request process completed.");
                Debug.Log($"Full output log:\n{outputLog}");

                if (authCodeSent)
                {
                    EditorUtility.DisplayDialog("Auth Code Sent",
                        $"An authentication code has been sent to your {authMethod}. Please check and enter the code in the Auth Code field.",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Auth Code Request",
                        "The auth code request process has completed, but we couldn't confirm if a code was sent. Please check your email and mobile app for any authentication messages from Steam.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during auth code request: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"An error occurred while requesting the auth code: {ex.Message}",
                    "OK");
            }
            finally
            {
                isRequestingAuthCode = false;
                EditorApplication.update -= UpdateProgress;
                EditorUtility.ClearProgressBar();
            }
        }

        private void UpdateProgress()
        {
            if (isRequestingAuthCode)
            {
                EditorUtility.DisplayProgressBar("Requesting Auth Code", "Processing...", UnityEngine.Random.Range(0f, 0.9f));
            }
        }

        public bool IsRequestingAuthCode()
        {
            return isRequestingAuthCode;
        }

        public void LoadSettings()
        {
            Username = EditorPrefs.GetString("SteamDepotUploader_username", "");
            Password = EditorPrefs.GetString("SteamDepotUploader_password", "");
            AuthCode = EditorPrefs.GetString("SteamDepotUploader_authCode", "");
        }

        public void SaveSettings()
        {
            EditorPrefs.SetString("SteamDepotUploader_username", Username);
            EditorPrefs.SetString("SteamDepotUploader_password", Password);
            EditorPrefs.SetString("SteamDepotUploader_authCode", AuthCode);
        }

        public void ClearSettings()
        {
            Username = string.Empty;
            Password = string.Empty;
            AuthCode = string.Empty;
            SaveSettings();
        }

        public bool AreCredentialsSet()
        {
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        }
    }
}