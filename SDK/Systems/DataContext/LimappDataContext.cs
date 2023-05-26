using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Liminal.Shared
{
    public class LimappDataContext : MonoBehaviour
    {
        public int ExperienceId;
        public string DataPath => $"{Application.persistentDataPath}/Data/{ExperienceId}";
        public string RuntimeSettingsPath => $"{DataPath}/runtimeSettings.json";

        public LimappRuntimeSettings Load()
        {
            if (!File.Exists(RuntimeSettingsPath))
            {
                Debug.LogError("No Runtime Settings Json to load, creating a new one. This must be done by the platform. If you're in a limapp project, ignore this.");
                CreateData();
            }

            var settingsFile = File.ReadAllText(RuntimeSettingsPath);
            var settings = JsonConvert.DeserializeObject<LimappRuntimeSettings>(settingsFile);
            return settings;
        }

        [ContextMenu("Create Data")]
        public void CreateData()
        {
            var settings = new LimappRuntimeSettings();
            var settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            File.WriteAllText(RuntimeSettingsPath, settingsJson);
        }

        [ContextMenu("Open Data Directory")]
        public void OpenDataDirectory()
        {
            // explorer doesn't like front slashes
            var directoryPath = DataPath.Replace(@"/", @"\"); 
            System.Diagnostics.Process.Start("explorer.exe", "/select," + directoryPath);
        }
    }

    /// <summary>
    /// The runtime settings is created and managed by the Platform App.
    /// It will go into persistent data path / data / {experienceId} / runtimeSettings.json
    /// </summary>
    public class LimappRuntimeSettings
    {
        public TimeSpan RuntimeDuration = TimeSpan.FromSeconds(600);
    }
}
