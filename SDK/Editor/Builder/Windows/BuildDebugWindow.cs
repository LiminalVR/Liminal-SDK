using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private List<Issue> _issues = new List<Issue>();
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
                _issues = FindIssuesInSolution();
            }

            _flags = (BindingFlags)EditorGUILayout.EnumFlagsField(_flags, "OffsetDropDown");
            GUILayout.EndHorizontal();

            _textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = _fontSize };

            GUI.backgroundColor = Color.white * 0.85f;
            GUILayout.Label(_status, new GUIStyle("AnimationEventTooltip") { alignment = TextAnchor.UpperCenter, fixedHeight = 22 });

            _fontSize = EditorGUILayout.IntSlider("Zoom", _fontSize, 8, 36);
            GUILayout.Space(4);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, "PopupCurveSwatchBackground");
            if (_issues != null)
            {
                DrawIssues(_issues);
            }
            else
            {
                GUILayout.Label("No issues detected.", _textStyle, GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true));
            }
            GUILayout.EndScrollView();
            GUI.backgroundColor = Color.white;
        }

        private void DrawIssues(List<Issue> issues)
        {
            foreach (var issue in issues)
            {
                GUILayout.Space(6);
                DrawIssue(issue);
            }
        }

        private void DrawIssue(Issue issue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(issue.GetParentCount() * 16);
            if (GUILayout.Button(issue.Message, _textStyle))
                _status = $"<color=yellow>{issue.Details}</color>";
            GUILayout.EndHorizontal();
            issue.SubIssues.ForEach(DrawIssue);
        }

        private List<Issue> FindIssuesInSolution()
        {
            List<Assembly> assemblies = GetAssemblies();
            List<Issue> issues = new List<Issue>();
            _status = $"Analysing {assemblies.Count} assemblies . . . \n";

            foreach (var assembly in assemblies)
            {
                var assemblyIssue = new Issue($"<color=#6a119e>[Assembly] ⟶ {assembly.FullName}</color>");
                assemblyIssue.SubIssues.AddRange(FindIssuesInAssembly(assembly, assemblyIssue));
                if (assemblyIssue.IsValid)
                    issues.Add(assemblyIssue);
            }
            return issues;
        }

        private static IEnumerable<Issue> FindIssuesInAssembly(Assembly assembly, Issue parentIssue)
        {
            IEnumerable<Type> types = assembly.GetTypes();
            List<Issue> issues = new List<Issue>();

            foreach (var type in types)
            {
                // Find missing constructor
                if (type.IsSerializable && !HasValidConstructors(type))
                    issues.Add(new Issue($"<color=#0f8e08>[Type] ⟶ {type.Name}</color>",
                        "Serializable classes without constructors may cause build errors.",
                        parentIssue));

                var typeIssue = new Issue($"<color=#0f8e08>[Type] ⟶ {type.Name}</color>", parentIssue);
                typeIssue.SubIssues.AddRange(FindIssuesInType(type, typeIssue));
                if (typeIssue.IsValid)
                    issues.Add(typeIssue);
            }
            return issues;
        }

        private static IEnumerable<Issue> FindIssuesInType(Type type, Issue parentIssue)
        {
            IEnumerable<MethodInfo> methods = type.GetMethods(_flags);
            List<Issue> issues = new List<Issue>();

            // Find issues in methods
            foreach (var method in methods)
            {
                var methodIssue = new Issue($"<color=#c1164c>[Method] ⟶ {method.Name}</color>", parentIssue);
                methodIssue.SubIssues.AddRange(FindIssuesInMethod(method, methodIssue));
                if (methodIssue.IsValid)
                    issues.Add(methodIssue);
            }
            return issues;
        }

        /// <summary>
        /// Has valid constructors if there are no ctors or there are ctors with arguments
        /// </summary>
        /// <returns></returns>
        private static bool HasValidConstructors(Type type)
        {
            if (type.Assembly.FullName.Contains("Editor"))
                return true;

            var ctors = type.GetConstructors();
            if (!ctors.Any())
                return true;

            return ctors.Any(ctor => !ctor.GetParameters().Any());
        }

        private static IEnumerable<Issue> FindIssuesInMethod(MethodInfo method, Issue parentIssue)
        {
            var issues = method.GetParameters()
                .Where(HasDefaultParamIssue)
                .Select(p => new Issue($"<color=#0766af>({p.ParameterType.Name} {p.Name} = {p.RawDefaultValue})</color>",
                    $"Default parameters referencing UnityEngine.CoreModule may cause build errors.",
                    parentIssue));
            return issues;
        }

        private static bool HasDefaultParamIssue(ParameterInfo param)
        {
            if (!param.HasDefaultValue)
                return false;

            if (param.DefaultValue == null)
                return false;

            return param.DefaultValue.GetType().Assembly.FullName.Contains("UnityEngine.CoreModule");
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
    }
}

public class Issue
{
    public Issue(string message, Issue parent)
    {
        Message = message;
        Parent = parent;
    }

    public Issue(string message, string detail, Issue parent)
    {
        Message = message;
        Details = detail;
        Parent = parent;
    }

    public Issue(string message)
    {
        Message = message;
        Parent = null;
    }

    public Issue Parent;
    public List<Issue> SubIssues = new List<Issue>();
    public string Message;
    public string Details;
    public bool IsValid => SubIssues.Any();

    public int GetParentCount()
    {
        var count = 0;
        Issue parent = Parent;
        while (parent != null)
        {
            count++;
            parent = parent.Parent;
        }
        return count;
    }
}