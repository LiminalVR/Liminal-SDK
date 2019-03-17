using Liminal.SDK.Editor.Build;
using UnityEditor;
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

                _selectedPlatform = config.SelectedPlatform;
                _selectedPlatform = (BuildPlatform)EditorGUILayout.EnumPopup("Select Platform", _selectedPlatform);
                config.SelectedPlatform = _selectedPlatform;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Build"))
                {
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

        private BuildPlatform _selectedPlatform;
    }

    public enum BuildPlatform
    {
        Current,
        Standalone,
        GearVR
    }
}