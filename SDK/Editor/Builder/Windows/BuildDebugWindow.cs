using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Liminal.SDK.Editor.Build;
using UnityEditor;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace Liminal.SDK.Build
{
    public class BuildDebugWindow
    : EditorWindow
    {
        private static BindingFlags _flags =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private List<AssemblyIssue> _assemblyIssues = new List<AssemblyIssue>();
        private bool[] _assemblyIssueFoldouts;
        private Vector2 _scrollPos = new Vector2(0, 0);
        private string _status = "Ready to begin analysis . . . ";
        private static GUIStyle _textStyle;
        private int _fontSize = 11;

        [MenuItem("Liminal/Build Debug Tool")]
        static void Init()
        {
            BuildDebugWindow window = (BuildDebugWindow)GetWindow(typeof(BuildDebugWindow), true, "Build Debug Tool", true);
            window.minSize = new Vector2(600, 275);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Analyse Project", "LargeButtonRight"))
            {
                _assemblyIssues = AnalyseSolution();
                if (_assemblyIssues != null)
                    _assemblyIssueFoldouts = new bool[_assemblyIssues.Count];
            }

            _flags = (BindingFlags)EditorGUILayout.EnumFlagsField(_flags, "OffsetDropDown");
            GUILayout.EndHorizontal();

            _textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = _fontSize };
            GUI.backgroundColor = Color.white * 0.85f;

            if (_status != null)
                GUILayout.Label(_status, new GUIStyle("AnimationEventTooltip") { alignment = TextAnchor.UpperCenter, fixedHeight = 22 });

            _fontSize = EditorGUILayout.IntSlider("Zoom", _fontSize, 8, 36);
            GUILayout.Space(4);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, "PopupCurveSwatchBackground");
            if (_assemblyIssues != null)
            {
                for (var i = 0; i < _assemblyIssues.Count; i++)
                {
                    var assemblyIssue = _assemblyIssues[i];
                    _assemblyIssueFoldouts[i] = EditorGUILayout.Foldout(_assemblyIssueFoldouts[i], assemblyIssue.Assembly.GetName().Name);
                    if (_assemblyIssueFoldouts[i])
                        DrawAssemblyFoldout(assemblyIssue);
                }
            }
            else
            {
                GUILayout.Label("No issues detected.", _textStyle, GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true));
            }
            GUILayout.EndScrollView();
            GUI.backgroundColor = Color.white;
        }

        private static void DrawAssemblyFoldout(AssemblyIssue assemblyIssue)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine($"<color=#6a119e>[Assembly ► {assemblyIssue.Assembly.FullName}]</color>");
            foreach (var typeIssue in assemblyIssue.TypeIssues)
            {
                strBuilder.AppendLine($"\t<color=#0f8e08>[Type ► {typeIssue.Type.Name}]</color> ");
                foreach (var methodIssue in typeIssue.MethodIssues)
                {
                    strBuilder.AppendLine($"\t\t<color=#c1164c>[Method ► {methodIssue.MethodInfo.Name}]</color>");
                    strBuilder.Append($"\t\t\t");
                    foreach (var parameterIssue in methodIssue.ParameterIssues)
                    {
                        strBuilder.Append($"<color=#0766af>[{parameterIssue.Name} = {parameterIssue.RawDefaultValue}]</color> ");
                    }
                    strBuilder.AppendLine();
                }
            }

            GUILayout.Label(strBuilder.ToString(), _textStyle, GUILayout.ExpandWidth(true));
        }

        private List<AssemblyIssue> AnalyseSolution()
        {
            List<Assembly> assemblies = GetAssemblies();
            _status = $"Analysing {assemblies.Count} assemblies . . . \n";
            List<AssemblyIssue> issues = new List<AssemblyIssue>();

            foreach (var assembly in assemblies)
            {
                var assemblyIssue = AnalyseAssembly(assembly);
                if (assemblyIssue != null)
                    issues.Add(assemblyIssue);

            }
            if (issues.Any())
                return issues;
            return null;
        }

        private static List<Assembly> GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            var importers = PluginImporter.GetAllImporters();
            var projectPath = Directory.GetParent(Application.dataPath).FullName;
            foreach (var plugin in importers)
            {
                // Skip anything in the /Liminal folder
                if (plugin.assetPath.IndexOf("Assets/Liminal", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip Unity extensions
                if (plugin.assetPath.IndexOf("Editor/Data/UnityExtensions", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip anything located in the Packages/ folder of the main project
                if (plugin.assetPath.IndexOf("Packages/", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                // Skip native plugins, and anything that won't normally be included in a build
                if (plugin.isNativePlugin || !plugin.ShouldIncludeInBuild())
                    continue;

                var pluginPath = Path.Combine(projectPath, plugin.assetPath);
                assemblies.Add(Assembly.LoadFile(pluginPath));
            }

            var a = Assembly.LoadFile(Path.Combine(projectPath, @"Library\ScriptAssemblies\Assembly-CSharp.dll"));
            assemblies.Add(a);

            return assemblies;
        }

        private static AssemblyIssue AnalyseAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes();
            var assemblyIssue = new AssemblyIssue(assembly);
            foreach (var type in types)
            {
                var typeIssue = AnalyseType(type);
                if (typeIssue != null)
                    assemblyIssue.TypeIssues.Add(typeIssue);
            }
            if (assemblyIssue.TypeIssues.Any())
                return assemblyIssue;
            return null;
        }

        private static TypeIssue AnalyseType(Type type)
        {
            var methods = type.GetMethods(_flags);
            var typeIssue = new TypeIssue(type);

            foreach (var method in methods)
            {
                var methodIssue = AnalyseMethod(method);
                if (methodIssue != null)
                    typeIssue.MethodIssues.Add(methodIssue);
            }
            if (typeIssue.MethodIssues.Any())
                return typeIssue;
            return null;
        }

        private static MethodIssue AnalyseMethod(MethodInfo method)
        {
            var methodIssue = new MethodIssue(method)
            {
                ParameterIssues = method.GetParameters().Where(
                    param =>
                    {
                        if (!param.HasDefaultValue)
                            return false;

                        if (param.DefaultValue == null)
                            return false;

                        return param.DefaultValue.GetType().Assembly.FullName.Contains("UnityEngine.CoreModule");
                    }).ToList()
            };
            if (methodIssue.ParameterIssues.Any())
                return methodIssue;
            return null;
        }
    }
}

public class AssemblyIssue
{
    public AssemblyIssue(Assembly assembly)
    {
        Assembly = assembly;
    }

    public Assembly Assembly;
    public List<TypeIssue> TypeIssues = new List<TypeIssue>();
}

public class TypeIssue
{
    public TypeIssue(Type type)
    {
        Type = type;
    }

    public Type Type;
    public List<MethodIssue> MethodIssues = new List<MethodIssue>();
}

public class MethodIssue
{
    public MethodIssue(MethodInfo methodInfo)
    {
        MethodInfo = methodInfo;
    }

    public MethodInfo MethodInfo;
    public List<ParameterInfo> ParameterIssues = new List<ParameterInfo>();
}