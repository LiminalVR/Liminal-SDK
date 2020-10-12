using UnityEditor;
using UnityEngine;

public static class EditorGUIHelper
{
    public static void DrawTitle(string label)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(30));
        EditorGUILayout.LabelField(label, EditorStyles.whiteLargeLabel);
        EditorGUILayout.EndVertical();
    }

    public static void DrawTitleFoldout(string label, ref bool boolToSet)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(30));
        boolToSet = EditorGUILayout.Foldout(boolToSet, label);
        EditorGUILayout.EndVertical();
    }
}