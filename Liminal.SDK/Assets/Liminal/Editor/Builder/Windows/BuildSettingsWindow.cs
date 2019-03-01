using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// A window for developers to configure and build for Limapps
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        public static EditorWindow Window = null;
        public static BuildSettingMenus SelectedMenu = BuildSettingMenus.Setup;
        public static Dictionary<BuildSettingMenus, BaseWindowDrawer> BuildSettingLookup = new Dictionary<BuildSettingMenus, BaseWindowDrawer>();

        public static int SelectedMenuIndex { get { return (int)SelectedMenu; } }

        [MenuItem("Liminal/Build Window")]
        public static void OpenBuildWindow()
        {
            Window = GetWindow(typeof(BuildSettingsWindow), true, "Build Settings");

            Window.minSize = new Vector2(500, 200);
            Window.maxSize = new Vector2(500, 200);

            Window.Show();
        }

        private void OnEnable()
        {
            // If the code adds another menu type while developing, we need to cache it
            SetupMenuWindows();
        }

        private void OnGUI()
        {
            var tabs = Enum.GetNames(typeof(BuildSettingMenus));
            SelectedMenu = (BuildSettingMenus) GUILayout.Toolbar(SelectedMenuIndex, tabs);

            var activeWindow = BuildSettingLookup[SelectedMenu];
            activeWindow.Draw();
        }

        private void SetupMenuWindows()
        {
            BuildSettingLookup.AddSafe(BuildSettingMenus.Build, new BuildWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Publishing, new PublishConfigurationWindow());
            BuildSettingLookup.AddSafe(BuildSettingMenus.Setup, new SetupWindow());
        }

        /// <summary>
        /// A way to recreate all the windows, in the case of a change in the constructor
        /// </summary>
        private void Clear()
        {
            BuildSettingLookup.Clear();
            SetupMenuWindows();
        }
    }
}