using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Oculus.VR.Editor;
using Liminal.SDK.Serialization;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A window for developers to configure and build for Limapps
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        private const int _width = 550;
        private const int _height = 300;

        public static EditorWindow Window;
        public static Dictionary<BuildSettingMenus, BaseWindowDrawer> BuildSettingLookup = new Dictionary<BuildSettingMenus, BaseWindowDrawer>();

        private BuildSettingMenus _selectedMenu = BuildSettingMenus.Setup;
        private BuildWindowConfig _windowConfig = new BuildWindowConfig();

        public int SelectedMenuIndex { get { return (int)_selectedMenu; } }

        [MenuItem("Liminal/Build Window")]
        public static void OpenBuildWindow()
        {
            Window = GetWindow(typeof(BuildSettingsWindow), false, "Build Settings");
            Window.minSize = new Vector2(_width, _height);
            Window.Show();
        }

        [MenuItem("Liminal/Update Package")]
        public static void RefreshPackage()
        {
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithoutLock);
            AssetDatabase.Refresh();
        }

        [MenuItem("Liminal/Use Legacy SDK")]
        public static void UseLegacy()
        {
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithoutXR);
            AssetDatabase.Refresh();

            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Android, new string[] { "Oculus" });

            var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            currentSymbols = currentSymbols.Replace("UNITY_XR", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, $"{currentSymbols}");
        }

        [MenuItem("Liminal/Use Unity XR")]
        public static void UseUnityXR()
        {
            PlayerSettings.virtualRealitySupported = false;
            
            File.WriteAllText(UnityPackageManagerUtils.ManifestPath, UnityPackageManagerUtils.ManifestWithXR);
            AssetDatabase.Refresh();
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, $"{currentSymbols};UNITY_XR");
        }

        private void OnEnable()
        {
            var fileExists = Directory.Exists(BuildWindowConsts.ConfigFolderPath) || File.Exists(BuildWindowConsts.BuildWindowConfigPath);

            if (fileExists)
            {
                if (File.Exists(BuildWindowConsts.BuildWindowConfigPath))
                {
                    var json = File.ReadAllText(BuildWindowConsts.BuildWindowConfigPath);
                    _windowConfig = JsonUtility.FromJson<BuildWindowConfig>(json);
                    AssetDatabase.Refresh();
                }
            }

            SetupFolderPaths();
            SetupPreviewScene();

            SetupMenuWindows();
            
            var activeWindow = BuildSettingLookup[_selectedMenu];
            activeWindow.OnEnabled();
        }

        private void OnGUI()
        {
            var tabs = Enum.GetNames(typeof(BuildSettingMenus));
            EditorGUI.BeginChangeCheck();
            _selectedMenu = (BuildSettingMenus) GUILayout.Toolbar(SelectedMenuIndex, tabs);
            var activeWindow = BuildSettingLookup[_selectedMenu];
            if(EditorGUI.EndChangeCheck())
                activeWindow.OnEnabled();

            activeWindow.Draw(_windowConfig);

            if (!Directory.Exists(BuildWindowConsts.ConfigFolderPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.ConfigFolderPath);
            }

            // boolean true is used to format the resulting string for maximum readability. False would format it for minimum size.
            var configJson = JsonUtility.ToJson(_windowConfig, true);
            File.WriteAllText(BuildWindowConsts.BuildWindowConfigPath, configJson);


            foreach (var entry in BuildSettingLookup)
            {
                entry.Value.Size = position.size;
            }
        }

        private void SetupMenuWindows()
        {
            BuildSettingLookup.AddSafe(BuildSettingMenus.Build, new BuildWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Issues, new IssueWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Publishing, new PublishConfigurationWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Setup, new SetupWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Preview, new AppPreviewWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Settings, new SettingsWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Explorer, new LimappExplorer());
        }

        private void SetupFolderPaths()
        {
            if (!Directory.Exists(BuildWindowConsts.PlatformSceneFolderPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.PlatformSceneFolderPath);
            }

            if (!Directory.Exists(BuildWindowConsts.BuildPath))
            {
                Directory.CreateDirectory(BuildWindowConsts.BuildPath);
            }

            AssetDatabase.Refresh();
        }

        private void SetupPreviewScene()
        {
            var sceneExists = File.Exists(BuildWindowConsts.PreviewAppScenePath);
            if (!sceneExists)
            {
                var scenePath = $"{UnityPackageManagerUtils.FullPackageLocation}/{BuildWindowConsts.PackagePreviewAppScenePath}";
                File.Copy(scenePath, BuildWindowConsts.PreviewAppScenePath);
                AssetDatabase.Refresh();
            }
        }
    }

    public class LimappExplorer : BaseWindowDrawer
    {
        public static string OutputDirectory;
        public static string InputDirectory;
        public static HashSet<int> ProcessedFile = new HashSet<int>();

        public override void Draw(BuildWindowConfig config)
        {
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref InputDirectory, "Input Directory");

            if (GUILayout.Button("Extract"))
            {
                ProcessedFile.Clear();

                var limapps = Directory.GetFiles(InputDirectory);
                EditorCoroutineUtility.StartCoroutineOwnerless(ExtractAll(limapps));
            }

            IEnumerator ExtractAll(string[] paths)
            {
                var limappPaths = paths.Where(x => Path.GetExtension(x) == ".limapp").ToArray();
                for (var i = 0; i < limappPaths.Length; i++)
                {
                    var limappPath = limappPaths[i];

                    if (Path.GetExtension(limappPath) != ".limapp")
                        continue;

                    EditorUtility.DisplayProgressBar("Extracting...", limappPath, i / (float)limappPath.Length);

                    Debug.Log($"Processing: {limappPath}");
                    var bytes = File.ReadAllBytes(limappPath);
                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExtractPack(bytes, limappPath));
                }

                EditorUtility.ClearProgressBar();
            }


            IEnumerator ExtractPack(byte[] appBytes, string limappPath)
            {
                Debug.Log("Unpacking...");
                var unpacker = new AppUnpacker();
                unpacker.UnpackAsync(appBytes);

                yield return new WaitUntil(() => unpacker.IsDone);

                var fileName = Path.GetFileNameWithoutExtension(limappPath);

                // write all assemblies on disk
                var assmeblies = unpacker.Data.Assemblies;
                //Application.persistentDataPath
                var appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}/{unpacker.Data.TargetPlatform}";

                if (ProcessedFile.Contains(unpacker.Data.ApplicationId))
                    appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}-{fileName}/{unpacker.Data.TargetPlatform}";

                var assemblyFolder = $"{appFolder}/assemblyFolder";
                
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                if (!Directory.Exists(assemblyFolder))
                    Directory.CreateDirectory(assemblyFolder);

                // Wait, in theory, I can rewrite the assembly to match ah, but that's not it.

                for (var i = 0; i < assmeblies.Count; i++)
                {
                    var asmBytes = assmeblies[i];
                    var asm = Assembly.Load(asmBytes);
                    File.WriteAllBytes($"{assemblyFolder}/{asm.GetName()}", asmBytes);
                }

                File.WriteAllBytes($"{appFolder}/appBundle", unpacker.Data.SceneBundle);
                File.WriteAllText($"{appFolder}/manifest.txt", $"Filename: {Path.GetFileName(limappPath)}");

                ProcessedFile.Add(unpacker.Data.ApplicationId);
                Debug.Log("Done!");

                yield break;
            }

        }

        public void DrawDirectorySelection(ref string directoryPath, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(Size.x * 0.15F));
                directoryPath = GUILayout.TextField(directoryPath, GUILayout.Width(Size.x * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Size.x * 0.1F)))
                {
                    directoryPath = EditorUtility.OpenFolderPanel("Select Limapp Folder", "", "");
                    directoryPath = DirectoryUtils.ReplaceBackWithForwardSlashes(directoryPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
