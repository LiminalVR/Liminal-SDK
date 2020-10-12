using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class IssuesUtility
{
    public static Dictionary<string, string> IncompatiblePackagesTable = new Dictionary<string, string>()
    {
        {"Unity.Postprocessing.Runtime","Post Processing"},
        {"FluffyUnderware.Curvy","Curvy"},
        {"DOTween","DOTween"}
    };

    //key is the function call in IL code, value is what the IL code translates into
    public static Dictionary<string, string> ForbiddenFunctionCalls = new Dictionary<string, string>()
    {
        { "System.Void UnityEngine.Application::Quit()","Application.Quit()"},
        { "System.Void UnityEngine.SceneManagement.SceneManager::LoadScene(System.String)","SceneManager.LoadScene()"},
        { "UnityEngine.AsyncOperation UnityEngine.SceneManagement.SceneManager::LoadSceneAsync(System.String)","SceneManager.LoadSceneAsync()"},
        { "System.Boolean UnityEngine.SceneManagement.SceneManager::UnloadScene(System.String)","SceneManager.UnloadScene()"},
        { "UnityEngine.AsyncOperation UnityEngine.SceneManagement.SceneManager::UnloadSceneAsync(System.String)","SceneManager.UnloadSceneAsync()"},
        { "System.Void UnityEngine.Object::DontDestroyOnLoad(UnityEngine.Object)","DontDestroyOnLoad()"}
    };

    public static List<string> AssembliesToIgnore = new List<string>()
    {
        "mscorlib",
        "UnityEngine",
        "UnityEngine.AIModule",
        "UnityEngine.ARModule",
        "UnityEngine.AccessibilityModule",
        "UnityEngine.AnimationModule",
        "UnityEngine.AssetBundleModule",
        "UnityEngine.AudioModule",
        "UnityEngine.ClothModule",
        "UnityEngine.ClusterInputModule",
        "UnityEngine.ClusterRendererModule",
        "UnityEngine.CoreModule",
        "UnityEngine.CrashReportingModule",
        "UnityEngine.DirectorModule",
        "UnityEngine.FileSystemHttpModule",
        "UnityEngine.GameCenterModule",
        "UnityEngine.GridModule",
        "UnityEngine.HotReloadModule",
        "UnityEngine.IMGUIModule",
        "UnityEngine.ImageConversionModule",
        "UnityEngine.InputModule",
        "UnityEngine.JSONSerializeModule",
        "UnityEngine.LocalizationModule",
        "UnityEngine.ParticleSystemModule",
        "UnityEngine.PerformanceReportingModule",
        "UnityEngine.PhysicsModule",
        "UnityEngine.Physics2DModule",
        "UnityEngine.ProfilerModule",
        "UnityEngine.ScreenCaptureModule",
        "UnityEngine.SharedInternalsModule",
        "UnityEngine.SpriteMaskModule",
        "UnityEngine.SpriteShapeModule",
        "UnityEngine.StreamingModule",
        "UnityEngine.StyleSheetsModule",
        "UnityEngine.SubstanceModule",
        "UnityEngine.TLSModule",
        "UnityEngine.TerrainModule",
        "UnityEngine.TerrainPhysicsModule",
        "UnityEngine.TextCoreModule",
        "UnityEngine.TextRenderingModule",
        "UnityEngine.TilemapModule",
        "UnityEngine.UIModule",
        "UnityEngine.UIElementsModule",
        "UnityEngine.UNETModule",
        "UnityEngine.UmbraModule",
        "UnityEngine.UnityAnalyticsModule",
        "UnityEngine.UnityConnectModule",
        "UnityEngine.UnityTestProtocolModule",
        "UnityEngine.UnityWebRequestModule",
        "UnityEngine.UnityWebRequestAssetBundleModule",
        "UnityEngine.UnityWebRequestAudioModule",
        "UnityEngine.UnityWebRequestTextureModule",
        "UnityEngine.UnityWebRequestWWWModule",
        "UnityEngine.VFXModule",
        "UnityEngine.VRModule",
        "UnityEngine.VehiclesModule",
        "UnityEngine.VideoModule",
        "UnityEngine.WindModule",
        "UnityEngine.XRModule",
        "UnityEditor",
        "Unity.Locator",
        "System.Core","System",
        "Mono.Security",
        "System.Configuration",
        "System.Xml",
        "Unity.Cecil","Unity.DataContract",
        "Unity.PackageManager",
        "UnityEngine.UI",
        "UnityEditor.UI",
        "UnityEditor.TestRunner",
        "UnityEngine.TestRunner",
        "nunit.framework",
        "UnityEditor.VR",
        "UnityEditor.Graphs",
        "UnityEditor.WindowsStandalone.Extensions",
        "UnityEditor.Android.Extensions",
        "SyntaxTree.VisualStudio.Unity.Bridge",
        "Unity.Timeline.Editor",
        "Editor",
        "SteamVR_Actions",
        "Platform",
        "Unity.TextMeshPro.Editor",
        "Unity.Timeline",
        "Tests",
        "Unity.CollabProxy.Editor",
        "UnityEngine.XR.LegacyInputHelpers",
        "Oculus.VR.Editor",
        "Oculus.VR.Scripts.Editor",
        "External",
        "Examples",
        "LiminalSdk","Oculus.VR",
        "UnityEditor.SpatialTracking",
        "UnityEngine.SpatialTracking",
        "SteamVR_Input_Editor",
        "SteamVR_Editor","SteamVR","Unity.TextMeshPro",
        "Unity.Analytics.DataPrivacy",
        "UnityEditor.XR.LegacyInputHelpers",
        "SteamVR_Windows_EditorHelper",
        "Google.ProtocolBuffers",
        "Liminal.Cecil",
        "Newtonsoft.Json",
        "SevenZip",
        "GVR","Liminal.SDK",
        "Liminal.SDK.VR.Daydream",
        "GVR.Editor",
        "Valve.Newtonsoft.Json",
        "Unity.Analytics.Editor",
        "Unity.Analytics.StandardEvents",
        "Unity.Analytics.Tracker",
        "UnityEditor.Purchasing",
        "netstandard",
        "System.Runtime.Serialization",
        "System.Xml.Linq",
        "SyntaxTree.VisualStudio.Unity.Messaging",
        "Unity.IvyParser",
        "Unity.SerializationLogic",
        "Unity.Legacy.NRefactory",
        "ExCSS.Unity","Microsoft.GeneratedCode",
        "Microsoft.GeneratedCode",
        "Microsoft.GeneratedCode",
        "Microsoft.GeneratedCode",
        "Microsoft.GeneratedCode",
        "System.ServiceModel.Internals"
    };


    public static void CheckForForbiddenCalls(string modulePath, ref Dictionary<string, string> keyValuePairs)
    {
        var module = ModuleDefinition.ReadModule(modulePath);

        if (module == null)
            return;

        var types = module.Types;
        keyValuePairs = new Dictionary<string, string>();

        foreach (var script in types)
        {
            var assets = AssetDatabase.FindAssets(script.Name);
            var assetPath = AssetDatabase.GUIDToAssetPath(assets.FirstOrDefault());

            foreach (var method in script.Methods)
            {
                var forbiddenCalls = CheckMethodForForbiddenCalls(method, script.Name);

                foreach (var call in forbiddenCalls)
                    keyValuePairs.AddSafe($"{call}", $"{assetPath}");

                //forbiddenCalls.ForEach(forbiddenCall => keyValuePairs.AddSafe($"{forbiddenCall}", $"{assetPath}"));
            }
        }
    }

    private static List<string> CheckMethodForForbiddenCalls(MethodDefinition method, string scriptName)
    {
        var temp = string.Empty;
        var textOutput = new List<string>();

        if (!method.HasBody)
            return textOutput;

        var methodCalls = method.Body.Instructions
                .Where(x => x.OpCode == OpCodes.Call)
                .ToArray();

        foreach (var item in methodCalls)
        {
            var mRef = item.Operand as MethodReference;

            if (mRef == null)
                continue;

            //Debug.Log(mRef.FullName);

            foreach (var key in IssuesUtility.ForbiddenFunctionCalls.Keys)
            {
                if (mRef.FullName.Equals(key))
                {
                    textOutput.Add($"Please remove <color=red>{IssuesUtility.ForbiddenFunctionCalls[key]}</color> from method: {method.Name} in script: <color=Cyan>{scriptName}</color>");
                    break;
                }
            }
        }

        return textOutput;
    }
}
