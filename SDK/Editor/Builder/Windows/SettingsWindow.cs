using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Liminal.SDK.Editor.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Liminal.SDK.Build
{
    public class SettingsWindow : BaseWindowDrawer
    {
        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Experience Settings");
                EditorGUILayout.LabelField("This page is used to set the various settings of the experience");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
            }
        }
    }
}
