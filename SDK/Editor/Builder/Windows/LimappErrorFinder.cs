using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LimappErrorFinder
{
    private static readonly List<Assembly> _assemblies = new List<Assembly>();
    private static readonly List<string> _issues = new List<string>();
    private static Vector2 _issuesScroll;
    private static Vector2 _assembliesScroll;
    private const BindingFlags _methodsBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static void Draw()
    {
        if (GUILayout.Button("Find Issues"))
        {
            GetAssemblies();
            GetParameterIssues();
        }

        if (_assemblies.Any())
        {
            DrawLoadedAssemblies();
            DrawIssues();
        }
    }

    private static void DrawLoadedAssemblies()
    {
        GUILayout.Label("Assemblies Loaded", "SettingsHeader");
        _assembliesScroll = GUILayout.BeginScrollView(_assembliesScroll, (GUIStyle)"CurveEditorBackground", GUILayout.Height(100));
        foreach (var assembly in _assemblies)
        {
            GUILayout.Label(FormatAssemblyName(assembly.GetName().ToString()), new GUIStyle(EditorStyles.label) { richText = true });
        }
        GUILayout.EndScrollView();
    }

    private static void DrawIssues()
    {
        GUILayout.Label("Issues", "SettingsHeader");
        if (!_issues.Any())
        {
            GUILayout.Label($"<color=#59db42><b>No issues detected</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
            return;
        }

        _issuesScroll = GUILayout.BeginScrollView(_issuesScroll, (GUIStyle)"CurveEditorBackground");
        foreach (var issue in _issues)
        {
            GUILayout.Label(issue, new GUIStyle(EditorStyles.label) { richText = true });
        }
        GUILayout.EndScrollView();
    }

    private static void GetParameterIssues()
    {
        _issues.Clear();
        foreach (var assembly in _assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(_methodsBindingFlags);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var str = $"<color=#FC0><b>{assembly.GetName().Name}</b></color> type={type.Name}, method={method.Name}\n" +
                              $"<color=#ff7631>{method.Name}</color>(";

                    if (!parameters.Any(x => x.HasDefaultValue && x.DefaultValue != null && x.ParameterType.Assembly.FullName.Contains("UnityEngine.CoreModule")))
                        continue;

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        if (parameter.HasDefaultValue)
                        {
                            if (parameter.DefaultValue != null && parameter.ParameterType.Assembly.FullName.Contains("UnityEngine.CoreModule"))
                                str += $"<color=#f55151><b>{parameter.Name} = {parameter.ParameterType.Assembly.GetName().Name}.{parameter.DefaultValue ?? "null"}</b></color>";
                            else
                                str += $"{parameter.Name} = {parameter.DefaultValue ?? "null"}";
                        }
                        else
                            str += $"{parameter.Name}";

                        if (i < parameters.Length - 1)
                            str += ", ";
                    }

                    str += ")\n";
                    _issues.Add(str);
                }
            }
        }
    }

    private static string FormatAssemblyName(string assemblyName)
    {
        var space = assemblyName.IndexOf(',');
        return $"<color=#FC0><b>{assemblyName.Insert(space, "</b></color><size=9>")}</size>";
    }

    private static void GetAssemblies()
    {
        var list = new List<PluginImporter>();
        var importers = PluginImporter.GetImporters(BuildTargetGroup.Android, BuildTarget.Android);
        foreach (var plugin in importers)
        {
            // Skip Unity extensions
            if (plugin.assetPath.IndexOf("Editor/Data/UnityExtensions", StringComparison.OrdinalIgnoreCase) > -1)
                continue;

            // Skip native plugins, and anything that won't normally be included in a build
            if (plugin.isNativePlugin || !plugin.ShouldIncludeInBuild())
                continue;

            list.Add(plugin);
        }

        _assemblies.Clear();
        var assemblycs = GetAssemblyByName("Assembly-CSharp");
        _assemblies.Add(assemblycs);

        foreach (var plugin in list)
        {
            var assembly = Assembly.Load(AssemblyName.GetAssemblyName(plugin.assetPath));
            if (!assembly.GetName().FullName.Contains("Liminal.SDK"))
                _assemblies.Add(assembly);
        }
    }

    static Assembly GetAssemblyByName(string assemblyName)
        => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
}
