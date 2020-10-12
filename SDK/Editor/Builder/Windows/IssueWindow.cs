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
        public override void OnEnabled()
        {
            base.OnEnabled();
            var warningGuids = AssetDatabase.FindAssets("sdk_warning");
            var errorGuids = AssetDatabase.FindAssets("sdk_error");

            if (warningGuids.Count() != 0)
                WarningTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(warningGuids[0]), typeof(Texture2D));

            if (errorGuids.Count() != 0)
                ErrorTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(errorGuids[0]), typeof(Texture2D));
        }

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
            EditorGUIHelper.DrawSpritedLabel("Ensure you are using Unity 2019.1.10f1 as your development environment", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));
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
                EditorGUIHelper.DrawSpritedLabel("Virtual Reality Must Be Supported", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

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
                EditorGUIHelper.DrawSpritedLabel("Stereo Rendering Mode Must be Set To Single Pass", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

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
                EditorGUIHelper.DrawSpritedLabel("Scene Must Contain A VR Avatar", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel--;
                return;
            }

            CheckEyes(avatar, out var eyePosWrong, out var eyeRotWrong, out var eyes);

            if (avatar.Head.Transform.localEulerAngles != Vector3.zero)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIHelper.DrawSpritedLabel("VR Avatar Head rotation must be Zeroed", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

                if (GUILayout.Button("Reset Head Rotation"))
                    avatar.Head.Transform.localEulerAngles = Vector3.zero;

                EditorGUILayout.EndHorizontal();
            }

            if (avatar.Head.Transform.localPosition != new Vector3(0, 1.7f, 0))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIHelper.DrawSpritedLabel("VR Avatar Head postion should be (0, 1.7f, 0)", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

                if (GUILayout.Button("Reset Head Position"))
                    avatar.Head.Transform.localPosition = new Vector3(0, 1.7f, 0);

                EditorGUILayout.EndHorizontal();
            }

            if (eyePosWrong || eyeRotWrong)
            {
                if (eyeRotWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIHelper.DrawSpritedLabel("Eye Local Rotation Must be Zeroed", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

                    if (GUILayout.Button("Reset Eye Rotation"))
                        eyes.ForEach(x => x.transform.localEulerAngles = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                if(eyePosWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIHelper.DrawSpritedLabel("Eye Local Position Must be Zeroed", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

                    if (GUILayout.Button("Reset Eye Position"))
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
                EditorGUIHelper.DrawSpritedLabel($"You have {allTags.Count() - 7} custom tags in your tag list. " +
                    $"Do not use tags unless they are assigned at runtime.", WarningTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            if (allLayers.Count() > 5)
            {

                EditorGUIHelper.DrawSpritedLabel($"You have {allLayers.Count() - 5} custom layers in your layer list. It is not recommended to rely on layers, " +
                    $"as layers other than the default ones are not carried through in a limapp and will returns null references. " +
                    $"If you use layers, make sure to refer to their number and not their string name.", WarningTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

            }

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
                EditorGUIHelper.DrawSpritedLabel($"{item}", ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));

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

            var style = new GUIStyle(GUI.skin.label)
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
                GUILayout.Label(ErrorTexture, GUILayout.MaxWidth(16), GUILayout.MaxHeight(16));
                EditorGUILayout.LabelField($"{entry.Key}", style);

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

        private bool _showRendering;
        private bool _showVRAvatar;
        private bool _showIncompatibility;
        private bool _showEditor;
        private bool _showTagsAndLayers;
        private bool _showForbiddenCalls;
        private List<GameObject> _sceneGameObjects = new List<GameObject>();
        private static List<Assembly> _currentAssemblies = new List<Assembly>();
        private static Dictionary<string, string> _forbiddenCallsAndScripts = new Dictionary<string, string>();
        private Vector2 _scrollPos;

        public Texture2D ErrorTexture;
        public Texture2D WarningTexture;
    }
}
