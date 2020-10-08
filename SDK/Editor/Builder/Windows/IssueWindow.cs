using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.VR.Avatars;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VR;
using UnityEngine.XR;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class IssueWindow : BaseWindowDrawer
    {
        private string _referenceInput;

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Issue Resolution");
                EditorGUILayout.LabelField("This window will help you resolve known issues and edge cases");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                CheckRendering();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                CheckVRAvatar();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                CheckTagsAndLayers();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                EditorGUILayout.EndScrollView();

                if (_showRenderingOptions == false && _showRenderingOptions == false)
                    EditorGUIHelper.DrawTitle("No Outstanding Issues");

                if (GUILayout.Button("Test"))
                {
                    DetectMethods();
                }

                GUILayout.FlexibleSpace();
               
                EditorGUILayout.EndVertical();
            }
        }

        private void CheckRendering()
        {
            if (_showRenderingOptions)
                EditorGUIHelper.DrawTitle("Rendering");

            if (!PlayerSettings.virtualRealitySupported)
            {
                _showRenderingOptions = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Virtual Reality Must Be Supported");

                if (GUILayout.Button("Enable VR Support"))
                    PlayerSettings.virtualRealitySupported = true;

                EditorGUILayout.EndHorizontal();
                return;
            }

            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.SinglePass)
            {
                _showRenderingOptions = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Stereo Rendering Mode Must be Set To Single Pass");

                if (GUILayout.Button("Set To Single Pass"))
                    PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;

                EditorGUILayout.EndHorizontal();
                return;
            }

            _showRenderingOptions = false;
        }

        private void CheckVRAvatar()
        {
            var sceneObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(sceneObjects);

            VRAvatar avatar = null;

            foreach (var item in sceneObjects)
            {
                if(item.GetComponentInChildren<VRAvatar>())
                {
                    avatar = item.GetComponentInChildren<VRAvatar>();
                    break;
                }
            }

            if (_showVRAvatarOptions)
                EditorGUIHelper.DrawTitle("VR Avatar");

            if (avatar == null)
            {
                _showVRAvatarOptions = true;
                EditorGUILayout.LabelField("Scene Must Contain A VR Avatar");
                return;
            }

            if (avatar.Head.Transform.localEulerAngles != Vector3.zero)
            {
                _showVRAvatarOptions = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VR Avatar Head Rotation Must be Zeroed");

                if (GUILayout.Button("Set Head Rotation To 0, 0, 0"))
                    avatar.Head.Transform.localEulerAngles = Vector3.zero;

                EditorGUILayout.EndHorizontal();
                return;
            }

            var eyes = new List<Camera>
            {
                avatar.Head.CenterEyeCamera,
                avatar.Head.LeftEyeCamera,
                avatar.Head.RightEyeCamera
            };

            var eyeRotWrong = false;
            var eyePosWrong = false;

            foreach (var item in eyes)
            {
                if (item.transform.localEulerAngles != Vector3.zero)
                    eyeRotWrong = true;

                if (item.transform.localPosition != Vector3.zero)
                    eyePosWrong = true;
            }

            if (eyeRotWrong || eyePosWrong)
            {
                _showVRAvatarOptions = true;

                if (eyeRotWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Rotation Must be Zeroed");

                    if (GUILayout.Button("Set Local Rotation To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localEulerAngles = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                if(eyePosWrong)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Eye Local Position Must be Zeroed");

                    if (GUILayout.Button("Set Local Position To 0, 0, 0"))
                        eyes.ForEach(x => x.transform.localPosition = Vector3.zero);

                    EditorGUILayout.EndHorizontal();
                }

                return;
            }

            _showVRAvatarOptions = false;
        }

        private void CheckTagsAndLayers()
        {

        }

        private void DetectMethods()
        {
            List<GameObject> test = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(test);

            List<MethodInfo> methods = new List<MethodInfo>();

            // this finds out if methods are included in scripts, but I need to change it to find if methods are being called in scripts

            foreach (var item in test)
            {
                var scripts = item.GetComponentsInChildren<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    if (script == null)
                        continue;

                    Type type = script.GetType();
 
                    if (type.GetMethod("test") != null)
                        methods.Add(type.GetMethod("test"));

                    if (type.GetMethod("temp") != null)
                        methods.Add(type.GetMethod("temp"));
                }  
            }

            foreach (var item in methods)
            {
                Debug.Log(item);
            }
        }

        bool _showRenderingOptions;
        bool _showVRAvatarOptions;
        bool _showTagsAndLayersOptions;
        Vector2 _scrollPos;
    }
}
