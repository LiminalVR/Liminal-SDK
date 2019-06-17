using System.IO;

using Liminal.SDK.Editor.Build;

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
        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Build Limapp");
                EditorGUILayout.LabelField("This process will build a .limapp file that will run on the Liminal Platform");

                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                DrawSceneSelection(ref _scenePath, "Target Scene");
                EditorGUILayout.Space();

                _selectedPlatform = config.SelectedPlatform;
                _selectedPlatform = (BuildPlatform)EditorGUILayout.EnumPopup("Select Platform", _selectedPlatform);
                config.SelectedPlatform = _selectedPlatform;

                GUILayout.FlexibleSpace();

                GUI.enabled = !_scenePath.Equals(string.Empty);

                if (GUILayout.Button("Build"))
                {
                    EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Single);

                    switch (_selectedPlatform)
                    {
                        case BuildPlatform.Current:
                            AppBuilder.BuildCurrentPlatform();
                            break;

                        case BuildPlatform.GearVR:
                            AppBuilder.BuildAndroid();
                            break;

                        case BuildPlatform.Standalone:
                            AppBuilder.BuildStandalone();
                            break;
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        public void DrawSceneSelection(ref string scenePath, string name)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Screen.width * 0.2F));

                scenePath = File.Exists(scenePath) ? scenePath : string.Empty;
                scenePath = GUILayout.TextField(scenePath, GUILayout.Width(Screen.width * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Screen.width * 0.05F)))
                {
                    scenePath = EditorUtility.OpenFilePanelWithFilters("Scene Finder", scenePath, new string[] { "FileType", "unity" });
                    scenePath = DirectoryUtils.ReplaceBackWithForwardSlashes(scenePath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private BuildPlatform _selectedPlatform;
        private string _scenePath = string.Empty;
    }

    public enum BuildPlatform
    {
        Current,
        Standalone,
        GearVR
    }
}