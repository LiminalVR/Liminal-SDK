using System;
using Liminal.SDK.Editor.Build;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class BuildWindow : BaseWindowDrawer
    {
        private string _referenceInput;
        private int _selectedBuildTarget;

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Build Limapp");
                EditorGUILayout.LabelField("This process will build a limapp file that will run on the Liminal Platform");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);

                DrawSceneSelector(ref _scenePath, "Target Scene", config);

                config.TargetScene = _scenePath;
                EditorGUILayout.Space();

                var target = string.Empty;

                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.Android:
                        target = "Android";
                        break;
                    case BuildTarget.StandaloneWindows:
                        target = "Windows Standalone | Emulator | Editor";
                        break;
                    default:
                        target = "UNSUPPORTED PLATFORM";
                        break;
                }

                var buildTargetNames = new[] { $"Current ({target})", "Standalone Windows", "Android" };
                var sizes = new[] { 0, 1, 2 };


                _selectedPlatform = config.SelectedPlatform;
                _selectedBuildTarget = (int)config.SelectedPlatform;
                _selectedBuildTarget = EditorGUILayout.IntPopup("Build Target: ", _selectedBuildTarget, buildTargetNames, sizes);
                _selectedPlatform = (BuildPlatform)_selectedBuildTarget;

                config.SelectedPlatform = _selectedPlatform;

                _compressionType = config.CompressionType;
                _compressionType = (ECompressionType)EditorGUILayout.EnumPopup("Compression Format", _compressionType);
                config.CompressionType = _compressionType;

                if (_compressionType == ECompressionType.Uncompressed)
                {
                    EditorGUILayout.LabelField("Uncompressed limapps are not valid for release.", EditorStyles.boldLabel);
                }

                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.LabelField("Additional References");
                EditorGUI.indentLevel++;

                var toRemove = new List<string>();
                foreach (var reference in config.AdditionalReferences)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(reference);
                        if (GUILayout.Button("X"))
                        {
                            toRemove.Add(reference);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                foreach (var reference in toRemove)
                {
                    config.AdditionalReferences.Remove(reference);
                }

                GUILayout.BeginHorizontal();
                {
                    _referenceInput = EditorGUILayout.TextField("Reference: ", _referenceInput);
                    if (GUILayout.Button("+"))
                    {
                        if (string.IsNullOrEmpty(_referenceInput))
                            return;

                        if (config.DefaultAdditionalReferences.Contains(_referenceInput))
                        {
                            Debug.Log($"The default references already included {_referenceInput}");
                            return;
                        }

                        var refAsm = Assembly.Load(_referenceInput);
                        if (refAsm == null)
                        {
                            Debug.LogError($"Assembly: {_referenceInput} does not exist.");
                            return;
                        }

                        if (!config.AdditionalReferences.Contains(_referenceInput))
                            config.AdditionalReferences.Add(_referenceInput);

                        _referenceInput = "";
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;

                GUILayout.FlexibleSpace();
                var enabled = !_scenePath.Equals(string.Empty);
                if(!enabled)
                    GUILayout.Label("Scene cannot be empty", "CN StatusWarn");

                GUI.enabled = !EditorApplication.isCompiling;

                if (GUILayout.Button("Build"))
                {
                    //run checks here.

                    var buildTargetOkay = true;

                    switch (_selectedPlatform)
                    {
                        case BuildPlatform.GearVR when EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android:
                        case BuildPlatform.Standalone when EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows:
                            buildTargetOkay = false;
                            break;
                    }

                    if (!buildTargetOkay)
                    {
                        if (EditorUtility.DisplayDialog("Platform Issue Detected", "Outstanding platform issues have been detected in your project. " +
                                "To reduce build time, ensure your current build target matches your selected platform", "Build Anyway", "Cancel Build"))
                        {
                            buildTargetOkay = true;
                        }
                        else
                        {
                            return;
                        }
                    }

                    IssuesUtility.CheckForAllIssues();

                    var hasBuildIssues = EditorPrefs.GetBool("HasBuildIssues");

                    if (hasBuildIssues)
                    {
                        if (EditorUtility.DisplayDialog("Build Issues Detected", "Outstanding issues have been detected in your project. " +
                                "Navigate to Build Settings->Issues for help resolving them", "Build Anyway", "Cancel Build"))
                        {
                            hasBuildIssues = false;
                        }
                    }

                    if (buildTargetOkay && !hasBuildIssues)
                    {

                        Build();
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void Build()
        {
            SettingsUtils.CopyProjectSettingsToProfile();
            EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Single);

            switch (_selectedPlatform)
            {
                case BuildPlatform.Current:
                    AppBuilder.BuildCurrentPlatform();
                    break;

                case BuildPlatform.GearVR:
                    AppBuilder.BuildLimapp(BuildTarget.Android, AppBuildInfo.BuildTargetDevices.GearVR,
                        _compressionType);
                    break;

                case BuildPlatform.Standalone:
                    AppBuilder.BuildLimapp(BuildTarget.StandaloneWindows, AppBuildInfo.BuildTargetDevices.Emulator,
                        _compressionType);
                    break;
            }
        }

        public void DrawSceneSelector(ref string scenePath, string name, BuildWindowConfig config)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Size.x * 0.2F));

                if (AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset)) != null)
                {
                    _targetScene = (SceneAsset) AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset));
                }

                _targetScene = (SceneAsset)EditorGUILayout.ObjectField(_targetScene, typeof(SceneAsset), true, GUILayout.Width(Size.x * 0.75F));

                if (_targetScene != null)
                {
                    scenePath = AssetDatabase.GetAssetPath(_targetScene);
                }
                else
                {
                    _targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneManager.GetActiveScene().path);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private BuildPlatform _selectedPlatform;
        private ECompressionType _compressionType;
        private SceneAsset _targetScene;
        private string _scenePath = string.Empty;
    }

    public enum BuildPlatform
    {
        Current,
        Standalone,
        GearVR
    }
}
