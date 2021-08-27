using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using App;
using Limapp.Test;
using UnityEngine;
using UnityEngine.Assertions;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.Platform.Experimental.Utils;
using Liminal.Platform.Experimental.VR;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.Core.Fader;
using Liminal.Platform.Experimental.App.BundleLoader.Impl;
using Liminal.Platform.Experimental.Services;
using Liminal.SDK.Serialization;
using UnityEngine.SceneManagement;

// TODO Rename the namespace and class name. The world Platform shouldn't be in either.
namespace Liminal.Platform.Experimental.App
{
    /// <summary>
    /// A component to view a limapp within the SDK based on the AppPreviewConfig.
    /// </summary>
    public class PlatformAppViewer : MonoBehaviour
    {
        public VRAvatar Avatar;
        public ExperienceAppPlayer ExperienceAppPlayer;
        public AppPreviewConfig PreviewConfig;
        public BaseLoadingBar LoadingBar;
        public GameObject SceneContainer;

        public bool AutoRun;

        public Limapp.v2.LimappLoader LimappLoader = new Limapp.v2.LimappLoader();

        private void OnValidate()
        {
            Assert.IsNotNull(LoadingBar, "LoadingBar must have a value or else the progress towards loading an experience will not be displayed.");
        }

        private void Start()
        {
            var deviceInitializer = GetComponentInChildren<IVRDeviceInitializer>();
            var device = deviceInitializer.CreateDevice();
            VRDevice.Initialize(device);
            VRDevice.Device.SetupAvatar(Avatar);
            BetterStreamingAssets.Initialize();

            if (!AutoRun)
                return;

            StartCoroutine(Test());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                CoroutineService.Instance.StartCoroutine(LimappLoader.Unload());
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                CoroutineService.Instance.StartCoroutine(Test());
            }
        }

        public IEnumerator Test()
        {
            for (var i = 0; i <= AppIds.Count; i++)
            {
                yield return LimappLoader.Load(AppIds[i]);
                yield return new WaitForSeconds(10);
                yield return LimappLoader.Unload();
            }
        }

        public List<int> AppIds;
        public static int _appIndex = 0;

        public ExperienceApp ExperienceApp;

        public static void LogMemory(string s)
        {
            var mem = System.GC.GetTotalMemory(true);
            Debug.Log($"memory {s}: {mem / 1e+6}");
        }

        public void Play()
        {
        }

        public void Stop()
        {
            StartCoroutine((StopRoutine()));
        }

        public bool Playing;

        private void OnExperienceComplete(bool completed)
        {
            Stop();   
        }

        private IEnumerator StopRoutine()
        {
            yield return ExperienceAppPlayer.Unload();
            Avatar.SetActive(true);
            SceneContainer.SetActive(true);
        }
    }
}


namespace Limapp.Test
{
    public static class CacheUtils
    {
        public static Coroutine Clean(int iteration = 4)
        {
            return CoroutineService.Instance.StartCoroutine(Routine());

            IEnumerator Routine()
            {
                for (int i = 0; i < iteration; i++)
                {
                    Caching.ClearCache();
                    yield return Resources.UnloadUnusedAssets();
                    GC.Collect();
                    yield return new WaitForSeconds(0.2F);
                }
            }
        }
    }
}

namespace Liminal.Limapp.v2
{
    public partial class LimappLoader
    {
        public ExperienceApp ExperienceApp;

        public AssetBundle CurrentBundle;
        public string SceneName;
        public List<Assembly> Assemblies;

        public IEnumerator Load(int appId)
        {
            LogMemory("[Loading]");

            var platformName = Application.isMobilePlatform ? "Android" : "WindowsStandalone";
            var appFolder = $"/Limapps/{appId}/{platformName}";
            var assemblyFolder = $"{appFolder}/assemblyFolder";
            var asmPaths = BetterStreamingAssets.GetFiles(assemblyFolder, "*");
            var assemblies = new List<Assembly>();
            Assemblies = assemblies;

            EnsureEmulatorFlagIsFalse();
            LoadAssemblies();

            // See if this even causes memory leak, if yes, it's because of unpacker.
            //new MemoryStream(unpacker.Data.SceneBundle, true)

            var bundlePath = $"{appFolder}/appBundle";
            // Run a preload function

            //using (var fileStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            {
                //var request = AssetBundle.LoadFromStreamAsync(fileStream);
                var request = BetterStreamingAssets.LoadAssetBundleAsync(bundlePath);

                yield return new WaitUntil(() => request.isDone);

                var assetBundle = request.assetBundle;
                var sceneName = assetBundle.GetAllScenePaths()[0];
                CurrentBundle = assetBundle;
                SceneName = sceneName;

                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                SceneManager.sceneLoaded += PlayApp;
                yield return op;
            }

            void PlayApp(Scene scene, LoadSceneMode mode)
            {
                ExperienceApp = GetExperienceApp();
                if (ExperienceApp == null)
                {
                    Debug.LogError("Cannot find experience app.");
                    return;
                }

                Debug.Log("Initialize app.");

                ExperienceApp.gameObject.SetActive(true);

                SceneManager.SetActiveScene(scene);
                InitializeApp();

                SceneManager.sceneLoaded -= PlayApp;
            }

            void LoadAssemblies()
            {
                foreach (var path in asmPaths)
                {
                    var asmBytes = BetterStreamingAssets.ReadAllBytes(path);

                    if (asmBytes == null || asmBytes.Length == 0)
                        Debug.LogError("Assembly has no bytes");

                    var asm = Assembly.Load(asmBytes);
                    assemblies.Add(asm);

                    // Note: This is required by the app to be able to correctly deserialize some types after being imported.
                    try
                    {
                        SerializationUtils.AddGlobalSerializableTypes(asm);
                        Debug.Log($"Assembly Loaded {asm}");
                    }
                    catch
                    {
                        Debug.LogError("Unable to fully load assembly");
                    }
                }
            }
        }

        /// <summary>
        /// Unloads the limapp and clean up to ensure the next app can be run without conflicts.
        /// This also reload the previous scenes before loading a limapp.
        /// </summary>
        public IEnumerator Unload()
        {
            foreach (var asm in Assemblies)
            {
                var types = asm.GetTypes();

                foreach (var type in types)
                    ResetAllStaticsVariables(type);
            }

            SerializationUtils.ClearGlobalSerializableTypes();

            if (ExperienceApp == null)
                Debug.Log("Experience app is null");

            try
            {
                ExperienceAppReflectionCache.ShutdownMethod.Invoke(GetExperienceApp(), null);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            ExperienceApp = null;
            Assemblies = null;

            yield return SceneManager.UnloadSceneAsync(SceneName);

            UnloadAssetBundle();
            ExperienceAppReflectionCache.IsEndingField.SetValue(null, false);

            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            
            Debug.Log("Unloaded...");

            void UnloadAssetBundle()
            {
                CurrentBundle.Unload(unloadAllLoadedObjects: true);
                ExperienceAppReflectionCache.AssetBundleField.SetValue(null, null);
                CurrentBundle = null;
            }
        }
    }

    public partial class LimappLoader
    {
        private void EnsureEmulatorFlagIsFalse()
        {
            var isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            isEmulator.SetValue(null, false);
        }

        public void ResetAllStaticsVariables(Type type)
        {
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Public);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                    continue;

                if (fieldInfo.FieldType == typeof(Texture2D) ||
                    fieldInfo.FieldType == typeof(AudioClip) ||
                    fieldInfo.FieldType == typeof(List<AudioClip>) ||
                    fieldInfo.FieldType == typeof(List<Texture2D>) ||
                    fieldInfo.FieldType == typeof(AudioClip[]) ||
                    fieldInfo.FieldType == typeof(Texture2D[]) ||
                    fieldInfo.FieldType == typeof(Texture) ||
                    fieldInfo.FieldType == typeof(RenderTexture) ||
                    fieldInfo.FieldType == typeof(List<Texture>) ||
                    fieldInfo.FieldType == typeof(List<RenderTexture>) ||
                    fieldInfo.FieldType == typeof(Texture[]) ||
                    fieldInfo.FieldType == typeof(Material[]) ||
                    fieldInfo.FieldType == typeof(Material) ||
                    fieldInfo.FieldType == typeof(List<Material>) ||
                    fieldInfo.FieldType == typeof(RenderTexture[]))
                {
                    fieldInfo.SetValue(null, GetDefault(type));
                }
            }
        }

        public object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        private ExperienceApp GetExperienceApp()
        {
            var apps = Resources.FindObjectsOfTypeAll<ExperienceApp>();
            return apps.Length > 0 ? apps[0] : null;
        }

        private void InitializeApp()
        {
            ExperienceApp.GetComponentInChildren<CompoundScreenFader>(includeInactive: true).enabled = true;

            SceneManager.SetActiveScene(ExperienceApp.gameObject.scene);
            ExperienceApp.gameObject.SetActive(true);

            //# SUPER IMPORTANT // Oh this replaces the device.... Maybe this is why it worked.
            // Temporarily commented out.
            /*var deviceInitializer = GetComponentInChildren<IVRDeviceInitializer>();
            var device = deviceInitializer.CreateDevice();
            VRDevice.Replace(device);*/

            var method = ExperienceAppReflectionCache.InitializeMethod;
            CoroutineService.Instance.StartCoroutine((IEnumerator)method.Invoke(ExperienceApp, null));
        }

        public static void LogMemory(string s)
        {
            var mem = System.GC.GetTotalMemory(true);
            Debug.Log($"memory {s}: {mem / 1e+6}");
        }
    }
}
