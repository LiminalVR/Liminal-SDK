using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                EditorGUILayout.LabelField("This window will help you identify and resolve known issues and edge cases");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                GetSceneGameObjects();

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

                GUILayout.FlexibleSpace();
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);

                if (GUILayout.Button("View Wiki"))
                    Application.OpenURL("https://github.com/LiminalVR/DeveloperWiki/wiki/Requirements-&-Optimisation");

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.EndVertical();
            }
        }

        private void GetSceneGameObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(_sceneGameObjects);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ScriptsCompiled()
        {
            IssuesUtility.CheckForForbiddenCalls("Library/ScriptAssemblies/Assembly-CSharp.dll", ref _forbiddenCallsAndScripts);
            _currentAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        }

        private void CheckUnityEditor()
        {
            if (Application.unityVersion.Equals("2019.1.10f1"))
                return;

            EditorGUIHelper.DrawTitleFoldout("Unity Editor", ref _showEditor);

            if (!_showEditor)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Ensure you are using Unity 2019.1.10f1 as your development environment");
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUI.indentLevel--;
        }

        private void CheckRendering()
        {
            if (PlayerSettings.virtualRealitySupported && PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass)
                return;

            EditorGUIHelper.DrawTitleFoldout("Rendering", ref _showRendering);

            if (!_showRendering)
                return;

            EditorGUI.indentLevel++;

            if (!PlayerSettings.virtualRealitySupported)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Virtual Reality Must Be Supported");

                if (GUILayout.Button("Enable VR Support"))
                    PlayerSettings.virtualRealitySupported = true;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel--;
                return;
            }

            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.SinglePass)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Stereo Rendering Mode Must be Set To Single Pass");

                if (GUILayout.Button("Set To Single Pass"))
                    PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight); 
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUI.indentLevel--;
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

            if (avatar != null)
            {
                CheckEyes(avatar, out var posWrong, out var rotWrong, out var eyeList);

                if (!posWrong && !rotWrong)
                    return;
            }

            EditorGUIHelper.DrawTitleFoldout("VR Avatar", ref _showVRAvatar);

            if (!_showVRAvatar)
                return;

            EditorGUI.indentLevel++;

            if (avatar == null)
            {
                EditorGUILayout.LabelField("Scene Must Contain A VR Avatar");
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel--;
                return;
            }

            CheckEyes(avatar, out var eyePosWrong, out var eyeRotWrong, out var eyes);

            if (avatar.Head.Transform.localEulerAngles != Vector3.zero)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VR Avatar Head Rotation Must be Zeroed");

                if (GUILayout.Button("Set Head Rotation To 0, 0, 0"))
                    avatar.Head.Transform.localEulerAngles = Vector3.zero;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            if (eyePosWrong || eyeRotWrong)
            {
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
            }

            EditorGUI.indentLevel--;
        }

        private void CheckEyes(VRAvatar avatar, out bool eyePosWrong, out bool eyeRotWrong, out List<Camera> eyes)
        {
            eyePosWrong = false;
            eyeRotWrong = false;

            eyes = new List<Camera>
            {
                avatar.Head.CenterEyeCamera,
                avatar.Head.LeftEyeCamera,
                avatar.Head.RightEyeCamera
            };

            foreach (var item in eyes)
            {
                if (item.transform.localEulerAngles != Vector3.zero)
                    eyeRotWrong = true;

                if (item.transform.localPosition != Vector3.zero)
                    eyePosWrong = true;
            }
        }

        private void CheckTagsAndLayers()
        {
            var allTags = UnityEditorInternal.InternalEditorUtility.tags;
            var allLayers = UnityEditorInternal.InternalEditorUtility.layers;

            if (allTags.Count() <= 7 && allLayers.Count() <= 5)
                return;
            
            EditorGUIHelper.DrawTitleFoldout("Tags And Layers", ref _showTagsAndLayers);

            if (!_showTagsAndLayers)
                return;
            EditorGUI.indentLevel++;
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
            EditorGUI.indentLevel--;
        }

        private void CheckIncompatibility()
        {
            GetIncompatibleItems(out var allItems, IssuesUtility.IncompatiblePackagesTable.Keys.ToArray());

            if (allItems.Count <= 0)
                return;

            EditorGUIHelper.DrawTitleFoldout("Known Incompatibilities", ref _showIncompatibility);

            if (!_showIncompatibility)
                return;
            EditorGUI.indentLevel++;
            DisplayIncompatibleItems(allItems);
            EditorGUI.indentLevel--;
        }

        private void GetIncompatibleItems(out List<string> incompatibleItems, params string[] packages)
        {
            incompatibleItems = new List<string>();

            foreach (Assembly assembly in _currentAssemblies)
            {
                if (IssuesUtility.AssembliesToIgnore.Contains(assembly.GetName().Name))
                    continue;

                //looks for forbidden assemblies
                foreach (var item in packages)
                {
                    if (assembly.GetName().Name.Equals(item))
                        incompatibleItems.Add(assembly.GetName().Name);
                }

                //Looks for forbidden namespaces in assemblies
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (var item in packages)
                    {
                        if (type.Namespace == item)
                            incompatibleItems.Add(type.Namespace);
                    }
                }
            }
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

        private void DisplayForbiddenCalls()
        {
            if (_forbiddenCallsAndScripts.Count <= 0)
                return;

            EditorGUIHelper.DrawTitleFoldout("Forbidden Calls", ref _showForbiddenCalls);

            if (!_showForbiddenCalls)
                return;

            EditorGUI.indentLevel++;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true
            };

            var btnText = "Open File";
            GUIStyle btn = new GUIStyle(GUI.skin.button);
            btn.fixedWidth = btn.CalcSize(new GUIContent(btnText)).x;
            btn.fixedHeight = btn.CalcSize(new GUIContent(btnText)).y;

            EditorGUILayout.LabelField("The Following Function Calls Are Forbidden In The Liminal SDK");
            EditorGUI.indentLevel++;

            foreach (var entry in _forbiddenCallsAndScripts)
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
            EditorGUI.indentLevel--;
        }
        
        bool _showRendering;
        bool _showVRAvatar;
        bool _showIncompatibility;
        bool _showEditor;
        bool _showTagsAndLayers;
        bool _showForbiddenCalls;
        List<GameObject> _sceneGameObjects = new List<GameObject>();
        static List<Assembly> _currentAssemblies = new List<Assembly>();
        static Dictionary<string, string> _forbiddenCallsAndScripts = new Dictionary<string, string>();
        Vector2 _scrollPos;
    }
}
