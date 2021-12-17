using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using SevenZip.Compression.LZMA;
using UnityEngine.Networking;
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

        private byte[] _limappData;

        public bool UseOriginal;

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

            if(UseOriginal)
                StartCoroutine(AutoPlay());
            else
            {
                StartCoroutine(Test());
            }
        }

        public IEnumerator Test()
        {
            // Make sure you download
            yield return UnPack();
            SceneManager.LoadScene(0);
        }

        public List<int> AppIds;
        public static int _appIndex = 0;

        public IEnumerator UnPack()
        {
            LogMemory("[Loading]");
            // read from directory
            var appId = AppIds[_appIndex];

            _appIndex++;
            if (_appIndex >= AppIds.Count - 1)
                _appIndex = 0;


            yield return DownloadAndExtractExperience(appId);

            var platformName = Application.isMobilePlatform ? "Android" : "WindowsStandalone";
            var appFolder = $"{Application.persistentDataPath}/Limapps/{appId}/{platformName}";
            var assemblyFolder = $"{appFolder}/assemblyFolder";

            var asmPaths = Directory.GetFiles(assemblyFolder);
            var assemblies = new List<Assembly>();

            //  When an asset bundle is set, it deserializes correctly using this data?
            //  ExperienceAppReflectionCache.AssetBundleField.SetValue(null, assetBundle);

            // Geez, ensuring emulator flag is falsed actually start the deserializing process.
            // App Deserializing doesn't cause GC (at least when there is errors)
            EnsureEmulatorFlagIsFalse();

            foreach (var path in asmPaths)
            {
                var asmBytes = File.ReadAllBytes(path);

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

            // See if this even causes memory leak, if yes, it's because of unpacker.
            //new MemoryStream(unpacker.Data.SceneBundle, true)

            var bundlePath = $"{appFolder}/appBundle";

            SceneContainer.SetActive(false);

            using (var fileStream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            {
                var request = AssetBundle.LoadFromStreamAsync(fileStream);
                yield return new WaitUntil(() => request.isDone);

                var assetBundle = request.assetBundle;
                var sceneName = assetBundle.GetAllScenePaths()[0];
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                SceneManager.sceneLoaded += PlayApp;

                yield return op;

                Debug.Log("App started, waiting...");
                yield return new WaitForSeconds(15);

                //yield return SceneManager.UnloadSceneAsync(sceneName);
                //assetBundle.Unload(true);

                yield return PlatformUnload(assemblies, assetBundle, sceneName);
            }

            //yield return CacheUtils.Clean();
            //yield return Unload();

            Avatar.SetActive(true);
            SceneContainer.SetActive(true);

            // OR this part! NOPE!
            void PlayApp(Scene scene, LoadSceneMode mode)
            {
                SceneContainer.gameObject.SetActive(false);

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
        }

        public IEnumerator Unload()
        {
            SerializationUtils.ClearGlobalSerializableTypes();
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();

            yield return CacheUtils.Clean();
        }

        public ExperienceApp ExperienceApp;

        private ExperienceApp GetExperienceApp()
        {
            var apps = Resources.FindObjectsOfTypeAll<ExperienceApp>();
            return apps.Length > 0 ? apps[0] : null;
        }

        private void InitializeApp()
        {
            Debug.Log("Initializes app");
            ExperienceApp.GetComponentInChildren<CompoundScreenFader>(includeInactive: true).enabled = true;

            SceneManager.SetActiveScene(ExperienceApp.gameObject.scene);
            ExperienceApp.gameObject.SetActive(true);

            //# SUPER IMPORTANT
            var deviceInitializer = GetComponentInChildren<IVRDeviceInitializer>();
            var device = deviceInitializer.CreateDevice();
            VRDevice.Replace(device);

            var method = ExperienceAppReflectionCache.InitializeMethod;
            CoroutineService.Instance.StartCoroutine((IEnumerator)method.Invoke(ExperienceApp, null));
        }


        /// <summary>
        /// Unloads the limapp and clean up to ensure the next app can be run without conflicts.
        /// This also reload the previous scenes before loading a limapp.
        /// </summary>
        public IEnumerator PlatformUnload(List<Assembly> _assemblies, AssetBundle assetBundle, string sceneName)
        {
            Debug.Log("Begin Unloading...");
            foreach (var asm in _assemblies)
            {
                var types = asm.GetTypes();

                foreach (var type in types)
                    ResetAllStaticsVariables(type);
            }

            Debug.Log("Clear seializable");
            SerializationUtils.ClearGlobalSerializableTypes();

            Debug.Log("Shutting down");

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

            yield return SceneManager.UnloadSceneAsync(sceneName);
            UnloadAssetBundle();

            ExperienceAppReflectionCache.IsEndingField.SetValue(null, false);

            yield return Resources.UnloadUnusedAssets();

            GC.Collect();

            Debug.Log("Unloaded...");

            void UnloadAssetBundle()
            {
                assetBundle.Unload(unloadAllLoadedObjects: true);
                ExperienceAppReflectionCache.AssetBundleField.SetValue(null, null);
            }
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

        public static void LogMemory(string s)
        {
            var mem = System.GC.GetTotalMemory(true);
            Debug.Log($"memory {s}: {mem / 1e+6}");
        }

        private IEnumerator AutoPlay()
        {
            for (var i = 0; i < 20; i++)
            {
                Playing = false;

                LogMemory("[Loading - Play]");
                Play();

                yield return new WaitUntil(() => Playing);
                yield return new WaitForSeconds(15);
                Stop();

                //  
            }
        }

        public void Play()
        {
            if(!ExperienceAppPlayer.IsRunning)
                StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            StartCoroutine((StopRoutine()));
        }

        public bool Playing;

        private IEnumerator PlayRoutine()
        {
            if (!UseOriginal)
                yield break;

            SceneContainer.SetActive(false);

            ResolvePlatformLimapp(out _limappData, out string fileName);

            var experience = new Experiences.Experience
            {
                Id = ExperienceAppUtils.AppIdFromName(fileName),
                Bytes = _limappData,
                CompressionType = GetCompressionType(fileName),
            };

            var loadOp = ExperienceAppPlayer.Load(experience);
            LoadingBar.Load(loadOp);
            EnsureEmulatorFlagIsFalse();
            yield return loadOp.LoadScene();
            EnsureEmulatorFlagIsFalse();

            LoadingBar.SetActiveState(false);

            ExperienceAppPlayer.Begin();

            ExperienceApp.OnComplete += OnExperienceComplete;
            ExperienceApp.Initializing += SetScreenfaderActive;

            Playing = true;
        }

        private ECompressionType GetCompressionType(string fileName)
        {
            var compression = ECompressionType.LMZA;

            if (string.IsNullOrEmpty(fileName) || string.IsNullOrWhiteSpace(fileName))
                return compression;
            
            if (Path.GetExtension(fileName).Equals(".ulimapp")) 
                compression = ECompressionType.Uncompressed;

            return compression;
        }

        private void SetScreenfaderActive()
        {
            var avatar = (VRAvatar)FindObjectOfType(typeof(VRAvatar));
            avatar.GetComponentInChildren<CompoundScreenFader>().enabled = true;
        }

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

        private void ResolvePlatformLimapp(out byte[] data, out string fileName, bool forceAndroid = false)
        {
            if (Application.platform == RuntimePlatform.Android || forceAndroid)
            {
                data = BetterStreamingAssets.ReadAllBytes(PreviewConfig.AndroidAppFullName);
                fileName = PreviewConfig.AndroidAppFullName;
            }
            else
            {
                var limappPath = PreviewConfig.EmulatorPath;
                fileName = Path.GetFileName(limappPath);
                data = File.ReadAllBytes(limappPath);
            }
        }

        private void EnsureEmulatorFlagIsFalse()
        {
            var isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            isEmulator.SetValue(null, false);
        }



        public IEnumerator DownloadAndExtractExperience(int id)
        {
            var url = $"https://liminal-resources.s3.ap-southeast-2.amazonaws.com/app/Limapp/v2/{id}.zip";
            yield return DownloadAndExtract(url, id);
        }

        public IEnumerator DownloadAndExtract(string url, int id)
        {
            Debug.Log($"[Downloading] {id}");

            var name = id.ToString();
            var downloadToPath = $"{Application.persistentDataPath}/Limapps/{name}.zip";
            var extractToPath = $"{Application.persistentDataPath}/Limapps/{name}";
            var versionExists = Directory.Exists(extractToPath);

            var www = new UnityWebRequest(url) {method = UnityWebRequest.kHttpVerbGET};
            var dh = new DownloadHandlerFile(downloadToPath) {removeFileOnAbort = true};
            www.downloadHandler = dh;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
                Debug.Log("Download saved to: " + downloadToPath);

            www.Dispose();

            // Extracting.
            if (Directory.Exists(extractToPath))
                Directory.Delete(extractToPath, true);

            ZipFile.ExtractToDirectory(downloadToPath, extractToPath);
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
