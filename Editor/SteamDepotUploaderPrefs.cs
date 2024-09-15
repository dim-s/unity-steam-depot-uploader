using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.GalacticWorkshop.SteamDepotUploader.Editor
{
    [InitializeOnLoad]
    public static class SteamDepotUploaderPrefs
    {
        private const string PrefsFileName = "SteamDepotUploaderPrefs.json";
        private static string PrefsFilePath => Path.Combine(Application.dataPath, "..", "ProjectSettings", PrefsFileName);

        private static Dictionary<string, string> preferences;
        
        static SteamDepotUploaderPrefs()
        {
            LoadPrefs();
        }

        private static Dictionary<string, string> Preferences
        {
            get
            {
                if (preferences == null)
                {
                    LoadPrefs();
                }
            
                return preferences;
            }
        }

        private static void LoadPrefs()
        {
            if (File.Exists(PrefsFilePath))
            {
                string json = File.ReadAllText(PrefsFilePath);
                preferences = JsonUtility.FromJson<SerializableDictionary>(json).ToDictionary();
            }
            else
            {
                preferences = new Dictionary<string, string>();
            }
        }

        private static void SavePrefs()
        {
            string json = JsonUtility.ToJson(new SerializableDictionary(Preferences), true);
            File.WriteAllText(PrefsFilePath, json);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return Preferences.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public static void SetString(string key, string value)
        {
            Preferences[key] = value;
            SavePrefs();
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            if (Preferences.TryGetValue(key, out string value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        public static void SetInt(string key, int value)
        {
            SetString(key, value.ToString());
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (Preferences.TryGetValue(key, out string value) && float.TryParse(value, out float result))
            {
                return result;
            }
            return defaultValue;
        }

        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString());
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (Preferences.TryGetValue(key, out string value) && bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
            SetString(key, value.ToString());
        }

        public static void DeleteKey(string key)
        {
            Preferences.Remove(key);
            SavePrefs();
        }

        public static void DeleteAll()
        {
            Preferences.Clear();
            SavePrefs();
        }

        [System.Serializable]
        private class SerializableDictionary
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();

            public SerializableDictionary() { }

            public SerializableDictionary(Dictionary<string, string> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }

            public Dictionary<string, string> ToDictionary()
            {
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }
    }
}
