using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.VR.Avatars;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class IssueWindow : BaseWindowDrawer
    {

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Issue Resolution");
                EditorGUILayout.LabelField("This window will help you Identify and resolve known issues and edge cases");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                GetSceneGameObjects();
                GetCurrentAssemblies();

                GUILayout.Space(10);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                EditorStyles.label.wordWrap = true;

                CheckUnityEditor();
                DisplayForbiddenCalls();
                CheckIncompatibility();
                CheckTagsAndLayers();
                CheckRendering();
                CheckVRAvatar();

                EditorGUILayout.EndScrollView();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                if (!_showRenderingSection && !_showRenderingSection && !_showIncompatibilitySection && !_showEditorSection && !_hasForbiddenCalls)
                    EditorGUIHelper.DrawTitle("No Outstanding Issues");

                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                if (GUILayout.Button("View Wiki"))
                    Application.OpenURL("https://github.com/LiminalVR/DeveloperWiki/wiki/Requirements-&-Optimisation");

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
        }

        private void GetSceneGameObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(_sceneGameObjects);
        }

        private void GetCurrentAssemblies()
        {
            _currentAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ScriptsCompiled()
        {
            CheckForForbiddenCalls();
        }

        private void CheckUnityEditor()
        {
            if (_showEditorSection)
                EditorGUIHelper.DrawTitle("Unity Editor");

            if (!Application.unityVersion.Contains("2019.1.10f1"))
            {
                _showEditorSection = true;
                EditorGUILayout.LabelField("Ensure you are using Unity 2019.1.10f1 as your development environment");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showEditorSection = false;
        }

        private void CheckRendering()
        {
            EditorGUIHelper.DrawTitleFoldout("Rendering", ref _showRendering);

            if (!_showRendering)
                return;

            if (!PlayerSettings.virtualRealitySupported)
            {
                _showRenderingSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Virtual Reality Must Be Supported");

                if (GUILayout.Button("Enable VR Support"))
                    PlayerSettings.virtualRealitySupported = true;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.SinglePass)
            {
                _showRenderingSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Stereo Rendering Mode Must be Set To Single Pass");

                if (GUILayout.Button("Set To Single Pass"))
                    PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showRenderingSection = false;
        }

        private void CheckVRAvatar()
        {
            VRAvatar avatar = null;

            foreach (var item in _sceneGameObjects)
            {
                if(item.GetComponentInChildren<VRAvatar>())
                {
                    avatar = item.GetComponentInChildren<VRAvatar>();
                    break;
                }
            }

            EditorGUIHelper.DrawTitleFoldout("VR Avatar", ref _showVRAvatar);

            if (!_showVRAvatar)
                return;

            if (avatar == null)
            {
                _showVRAvatarSection = true;
                EditorGUILayout.LabelField("Scene Must Contain A VR Avatar");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            if (avatar.Head.Transform.localEulerAngles != Vector3.zero)
            {
                _showVRAvatarSection = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VR Avatar Head Rotation Must be Zeroed");

                if (GUILayout.Button("Set Head Rotation To 0, 0, 0"))
                    avatar.Head.Transform.localEulerAngles = Vector3.zero;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            var eyes = new List<Camera>
            {
                avatar.Head.CenterEyeCamera,
                avatar.Head.LeftEyeCamera,
                avatar.Head.RightEyeCamera
            };

            var eyeRotWrong = false;
            var eyePosWrong = false;

            foreach (var item in eyes)
            {
                if (item.transform.localEulerAngles != Vector3.zero)
                    eyeRotWrong = true;

                if (item.transform.localPosition != Vector3.zero)
                    eyePosWrong = true;
            }

            if (eyeRotWrong || eyePosWrong)
            {
                _showVRAvatarSection = true;

                if (eyeRotWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Rotation Must be Zeroed");

                    if (GUILayout.Button("Set Local Rotation To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localEulerAngles = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                if(eyePosWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Position Must be Zeroed");

                    if (GUILayout.Button("Set Local Position To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localPosition = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                return;
            }

            _showVRAvatarSection = false;
        }

        private void CheckTagsAndLayers()
        {
            var allTags = UnityEditorInternal.InternalEditorUtility.tags;
            var allLayers = UnityEditorInternal.InternalEditorUtility.layers;

            if (allTags.Count() > 7 || allLayers.Count() > 5)
                EditorGUIHelper.DrawTitle("Tags And Layers");
            else
                return;

            if (allTags.Count() > 7)
            {
                EditorGUILayout.LabelField($"You have {allTags.Count() - 7} custom tags in your tag list. Do not use tags unless they are assigned at runtime.");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            if (allLayers.Count() > 5)
                EditorGUILayout.LabelField($"You have {allLayers.Count() - 5} custom layers in your layer list. It is not recommended to rely on layers, " +
                    $"as layers other than the default ones are not carried through in a limapp and will returns null references. If you use layers, " +
                    $"make sure to refer to their number and not their string name.");

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        private void CheckIncompatibility()
        {
            if(_showIncompatibilitySection)
                EditorGUIHelper.DrawTitle("Known Incompatibilities");

            GetIncompatibleAssemblies(out var presentAssemblies, IssuesUtility.IncompatiblePackagesTable.Keys.ToArray());
            GetIncompatibleNamespaces(out var presentNamespaces, IssuesUtility.IncompatiblePackagesTable.Keys.ToArray());

            var allItems = new List<string>();
            allItems.AddRange(presentAssemblies);
            allItems.AddRange(presentNamespaces);

            if (allItems.Count <= 0)
            {
                _showIncompatibilitySection = false;
                return;
            }

            _showIncompatibilitySection = true;

            DisplayIncompatibleItems(allItems);
        }

        private void DisplayIncompatibleItems(List<string> itemsToDisplay)
        {
            var incompatiblePackages = new List<string>();

            foreach (var item in itemsToDisplay)
            {
                IssuesUtility.IncompatiblePackagesTable.TryGetValue(item, out var value);

                if (!incompatiblePackages.Contains(value))
                    incompatiblePackages.Add(value);
            }

            if (incompatiblePackages.Count <= 0)
                return;

            EditorGUILayout.LabelField("The Following Packages Are Known To Be Incompatible With The Liminal SDK");
            EditorGUI.indentLevel++;

            foreach (var item in incompatiblePackages)
            {
                EditorGUILayout.LabelField($"* {item}");
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField($"Please Remove These Packages Before Building");
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        private void GetIncompatibleAssemblies(out List<string> presentAssemblies, params string[] assemblies)
        {
            presentAssemblies = new List<string>();

            foreach (Assembly assembly in _currentAssemblies)
            {
                if (IssuesUtility.AssembliesToIgnore.Contains(assembly.GetName().Name))
                    continue;

                foreach (var item in assemblies)
                {
                    if (assembly.GetName().Name.Equals(item))
                        presentAssemblies.Add(assembly.GetName().Name);
                }
            }
        }

        private void GetIncompatibleNamespaces(out List<string> presentNamespaces,params string[] namespaces)
        {
            presentNamespaces = new List<string>();

            foreach (Assembly assembly in _currentAssemblies)
            {
                if (IssuesUtility.AssembliesToIgnore.Contains(assembly.GetName().Name))
                    continue;

                foreach (Type type in assembly.GetTypes())
                {
                    foreach (var item in namespaces)
                    {
                        if (type.Namespace == item)
                            presentNamespaces.Add(type.Namespace);
                    }
                }
            }
        }

        private static void CheckForForbiddenCalls()
        {
            var module = ModuleDefinition.ReadModule($"Library/ScriptAssemblies/Assembly-CSharp.dll");
            var types = module.Types;
            ForbiddenCallsAndScript = new Dictionary<string, string>();

            foreach (var script in types)
            {
                var assets = AssetDatabase.FindAssets(script.Name);
                var assetPath = AssetDatabase.GUIDToAssetPath(assets.FirstOrDefault());

                foreach (var method in script.Methods)
                {
                    var forbiddenCalls = CheckMethodForForbiddenCalls(method, script.Name);
                    forbiddenCalls.ForEach(forbiddenCall => ForbiddenCallsAndScript.Add($"{forbiddenCall}", $"{assetPath}"));
                }
            }
        }

        public static List<string> CheckMethodForForbiddenCalls(MethodDefinition method, string scriptName)
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

        private void DisplayForbiddenCalls()
        {
            if (ForbiddenCallsAndScript.Count <= 0)
                return;

            EditorGUIHelper.DrawTitle("Forbidden Calls");
            _hasForbiddenCalls = true;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.wordWrap = true;

            var btnText = "Open File";
            GUIStyle btn = new GUIStyle(GUI.skin.button);
            btn.fixedWidth = btn.CalcSize(new GUIContent(btnText)).x;
            btn.fixedHeight = btn.CalcSize(new GUIContent(btnText)).y;

            EditorGUILayout.LabelField("The Following Function Calls Are Forbidden In The Liminal SDK");
            EditorGUI.indentLevel++;

            foreach (var entry in ForbiddenCallsAndScript)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"* {entry.Key}", style);

                var location = Application.dataPath + "/../" + entry.Value;

                if (GUILayout.Button(btnText, btn))
                    Application.OpenURL(location);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField($"Please Remove These Calls Before Building");
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
        
        bool _showRenderingSection;
        bool _showRendering;
        bool _showVRAvatarSection;
        bool _showVRAvatar;
        bool _showIncompatibilitySection;
        bool _showIncompatibility;
        bool _showEditorSection;
        bool _hasForbiddenCalls;
        List<GameObject> _sceneGameObjects = new List<GameObject>();
        List<Assembly> _currentAssemblies = new List<Assembly>();
        static Dictionary<string, string> ForbiddenCallsAndScript = new Dictionary<string, string>();
        Vector2 _scrollPos;
    }
}
