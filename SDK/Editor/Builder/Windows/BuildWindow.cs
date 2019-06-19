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

                DrawSceneSelector(ref _scenePath, "Target Scene", config);

                config.TargetScene = _scenePath;
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

        public void DrawSceneSelector(ref string scenePath, string name, BuildWindowConfig config)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Screen.width * 0.2F));

                if (AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset)) != null)
                {
                    _targetScene = (SceneAsset) AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset));
                }

                _targetScene = (SceneAsset)EditorGUILayout.ObjectField(_targetScene, typeof(SceneAsset), true, GUILayout.Width(Screen.width * 0.75F));

                if (_targetScene != null)
                {
                    scenePath = AssetDatabase.GetAssetPath(_targetScene);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private BuildPlatform _selectedPlatform;
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