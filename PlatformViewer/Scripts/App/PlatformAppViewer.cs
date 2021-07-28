using System;
using System.Collections;
using System.IO;
using System.Reflection;
using App;
using UnityEngine;
using UnityEngine.Assertions;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.Platform.Experimental.Utils;
using Liminal.Platform.Experimental.VR;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.Core.Fader;
using Liminal.SDK.Serialization;
using Liminal.Platform.Experimental.App.BundleLoader.Impl;
using UnityEngine.SceneManagement;
using Limapp.Test;
using Liminal.Platform.Experimental.Services;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Linq;
using Liminal.SDK.VR.Input;
using UnityEngine.AddressableAssets.ResourceLocators;
using Object = UnityEngine.Object;

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

        public bool Extract;
        public bool ExtractForceAndroid;
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

            if (UseOriginal)
            {
                CoroutineService.Instance.StartCoroutine(LoopPlay());
                return;
            }

            if (Extract)
                CoroutineService.Instance.StartCoroutine(ExtractPack());
            else
            {
                CoroutineService.Instance.StartCoroutine(Test());
            }
        }

        private IEnumerator PlayForSeconds(float seconds)
        {
            while (true)
            {
                Play();
                yield return new WaitForSeconds(seconds);
                Stop();

                yield return new WaitForSeconds(3);
            }
        }

        [ContextMenu("Play")]
        public void Play()
        {

        }

        private IEnumerator LoopPlay()
        {
            for (int i = 0; i < 20; i++)
            {
                if (!ExperienceAppPlayer.IsRunning)
                    yield return StartCoroutine(PlayRoutine());

                yield return new WaitForSeconds(8);

                Stop();
            }
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            StartCoroutine((StopRoutine()));
        }

        private IEnumerator PlayRoutine()
        {
            Debug.Log("Play Routine");

            SceneContainer.SetActive(false);

            ResolvePlatformLimapp(out _limappData, out string fileName);

            var experience = new Experience
            {
                Id = ExperienceAppUtils.AppIdFromName(fileName),
                Bytes = _limappData,
                CompressionType = GetCompressionType(fileName),
            };

            var loadOp = ExperienceAppPlayer.Load(experience);
            LoadingBar.Load(loadOp);
            EnsureEmulatorFlagIsFalse();
            
            
            yield return loadOp.LoadScene();

            experience.Bytes = null;
            _limappData = null;

            EnsureEmulatorFlagIsFalse();

            LoadingBar.SetActiveState(false);

            ExperienceAppPlayer.Begin();

            // Try clear it all.
            var experienceApp = GetExperienceApp();
            var type = experienceApp.GetType();
            var appDataField = type.GetField("m_AppData", BindingFlags.NonPublic | BindingFlags.Instance);
            appDataField.SetValue(experienceApp, null); //We need this overload for .NET < 4.51

            var assetLookUpField = type.GetField("m_AssetLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            assetLookUpField.SetValue(experienceApp, null); //We need this overload for .NET < 4.51

            /*
            var rootGO = type.GetField("m_RootGameObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            rootGO.SetValue(experienceApp, default); //We need this overload for .NET < 4.51
            */
            //_$AssetLookup
            // reset state;

            var assetLookUpObject = GameObject.Find("_$AssetLookup");
            Destroy(assetLookUpObject);

            ExperienceApp.OnComplete += OnExperienceComplete;
            ExperienceApp.Initializing += SetScreenfaderActive;
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

        private string GetPath()
        {
            if (Application.platform == RuntimePlatform.Android )
            {
                return $"{Application.persistentDataPath}/bundle";
            }
            else
            {
                var limappPath = PreviewConfig.EmulatorPath;
                return $"{Application.dataPath}/_Builds/assetBundle";
            }
        }

        private void EnsureEmulatorFlagIsFalse()
        {
            var isEmulator = typeof(ExperienceApp).GetField("_isEmulator", BindingFlags.Static | BindingFlags.NonPublic);
            isEmulator.SetValue(null, false);
        }

        /*public Coroutine InitializeApp()
        {
            // TODO Investigate if activating the pointer is necessary.
            try
            {
                if (VRAvatar.Active.PrimaryHand != null && VRAvatar.Active.PrimaryHand.IsActive)
                {
                    VRDevice.Device.PrimaryInputDevice.Pointer.Activate();
                }
                else
                {
                    Debug.Log("Could not activate pointer");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Could not activate pointer {e}");
            }

            return StartCoroutine(_InitializeApp());
        }*/

        public HashSet<Object> StartObjects = new HashSet<Object>();

        // TEST
        public IEnumerator Test()
        {
            var count = 20;
            for (int i = 0; i < count; i++)
            {
                yield return UnPack();
                SceneManager.LoadScene(0);
                break;
            }
        }

        public IEnumerator ExtractPack()
        {
            ResolvePlatformLimapp(out var appBytes, out string fileName, ExtractForceAndroid);
            var unpacker = new AppUnpacker();
            unpacker.UnpackAsync(appBytes);

            yield return new WaitUntil(() => unpacker.IsDone);

            // write all assemblies on disk
            var assmeblies = unpacker.Data.Assemblies;

            var appFolder = $"{Application.persistentDataPath}/{unpacker.Data.ApplicationId}";
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
            Debug.Log("Done!");
            yield break;
        }

        public int AppId = 87;

        public IEnumerator UnPack()
        {
            /*// This creases memory issues.
            BundleAsyncLoadOperation.LogMemory("[Before Extract]");
            yield return ExtractPack();
            BundleAsyncLoadOperation.LogMemory("[After Extract]");*/

            BundleAsyncLoadOperation.LogMemory("[Loading]");
            // read from directory
            var appId = AppId;
            var appFolder = $"{Application.persistentDataPath}/{appId}";
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

        // Pass in a .limapp
        public IEnumerator Unpack(byte[] bytes)
        {
            yield return CacheUtils.Clean();

            BundleAsyncLoadOperation.LogMemory("[Loading]");

            var unpacker = new AppUnpacker();
            unpacker.UnpackAsync(bytes);
            bytes = null;

            yield return new WaitUntil(() => unpacker.IsDone);

            var data = unpacker?.Data;
            var assemblies = data.Assemblies;



            var assetBundleRequest = AssetBundle.LoadFromMemoryAsync(data.SceneBundle);
            //var assetBundleRequest = LoadBundleFromFile();

            yield return assetBundleRequest;
            
            BundleAsyncLoadOperation.LogMemory("[Loaded]");

            var assetBundle = assetBundleRequest.assetBundle;

            // The scene part
            var sceneName = assetBundle.GetAllScenePaths()[0];

            // down this as a unity scene?
            //var op = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return op;
            yield return new WaitForSeconds(2);

            assetBundleRequest.assetBundle.Unload(true);

            yield return SceneManager.UnloadSceneAsync(sceneName);

            // Load an empty scene? 

            yield return CacheUtils.Clean();
            //assetBundleRequest.assetBundle.Unload(true);
            assetBundleRequest = null;
            yield return CacheUtils.Clean();
            BundleAsyncLoadOperation.LogMemory("[Unloaded]");
        }

        List<GameObject> FindSceneObjects(string sceneName)
        {
            List<GameObject> objs = new List<GameObject>();
            foreach (GameObject obj in Object.FindObjectsOfType(typeof(GameObject)))
            {
                if (obj.scene.name.CompareTo(sceneName) == 0)
                {
                    objs.Add(obj);
                }
            }
            return objs;
        }

        public static void Restart()
        {
            var scenes = SceneManager.GetAllScenes();
            foreach(var s in scenes)
                SceneManager.UnloadScene(s);

            // Destroy all remaining textures.
            // Unload dont destroy on load

            Resources.UnloadUnusedAssets();

            System.GC.Collect(4, System.GCCollectionMode.Forced);
            CoroutineService.Instance.StopAllCoroutines();

            SceneManager.LoadScene(0);
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

            if(ExperienceApp == null)
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

            /*_avatarBefore.SetActive(true);

            unpacker = null;
            _appPack = null;
            SceneLoadingOperation = null;
            _loadProgress = 0f;
            */

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
            Debug.Log("Resetting Static Variables");

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

    }
}


