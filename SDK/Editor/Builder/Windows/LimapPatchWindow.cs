﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.Serialization;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Assembly = System.Reflection.Assembly;

namespace Liminal.SDK.Build
{
    public class LimapPatchWindow : BaseWindowDrawer
    {
        public static string OutputDirectory = @"C:\Work\Liminal\2019\DLL\2022";
        public static string InputDirectory = @"C:\Work\Liminal\2019\DLL\2019";
        public static HashSet<int> ProcessedFile = new HashSet<int>();

        private static string SDKPath = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\Assets";

        public override void Draw(BuildWindowConfig config)
        {
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref InputDirectory, "Input Directory");

            GUILayout.BeginHorizontal();

            var manifest = AppBuilder.ReadAppManifest();
            var folderRoot = $@"C:\Work\Liminal\2022\Bundles\{manifest.Id}";

            if (GUILayout.Button("Open Copy Folder"))
            {
                if (!Directory.Exists(folderRoot))
                    Directory.CreateDirectory(folderRoot);

                EditorUtility.RevealInFinder($"{folderRoot}/");
            }

            if (GUILayout.Button("Transfer Files"))
            {
                var dllPath = $"{Application.dataPath}/../Library/Bee/PlayerScriptAssemblies/{manifest.Name}.dll";
                var dllPath2 = $"{Application.dataPath}/../Library/Bee/artifacts/1300b0aP.dag/{manifest.Name}.dll";

                if (File.Exists(dllPath))
                {
                    Debug.Log($"Found dll, {dllPath}");
                    File.Copy(dllPath, $@"C:\Work\Liminal\2022\DLLs\{manifest.Name}.dll", true);
                }
                else if (File.Exists(dllPath2))
                {
                    Debug.Log($"Found dll, {dllPath2}");
                    File.Copy(dllPath2, $@"C:\Work\Liminal\2022\DLLs\{manifest.Name}.dll", true);
                }
                else
                {
                    Debug.LogError("Could not find dll, you need to perform a manual build or add an assembly definition.");
                }

                var bundleStandalonePath = $"{Application.dataPath}/../AssetBundles/StandaloneWindows";
                if (Directory.Exists(bundleStandalonePath))
                {
                    if (!Directory.Exists(folderRoot))
                        Directory.CreateDirectory(folderRoot);

                    var newFolder = $@"{folderRoot}\StandaloneWindows";
                    if (Directory.Exists(newFolder))
                        Directory.Delete(newFolder, true);

                    FileUtil.CopyFileOrDirectory(bundleStandalonePath, newFolder);
                }

                var bundleAndroidPath = $"{Application.dataPath}/../AssetBundles/Android";
                if (Directory.Exists(bundleAndroidPath))
                {
                    if (!Directory.Exists(folderRoot))
                        Directory.CreateDirectory(folderRoot);

                    var newFolder = $@"{folderRoot}\Android";
                    if (Directory.Exists(newFolder))
                        Directory.Delete(newFolder, true);

                    FileUtil.CopyFileOrDirectory(bundleAndroidPath, newFolder);
                }
            }

            if (GUILayout.Button("Add Root ASM Def"))
            {
                CreateAssemblyDefinition(Application.dataPath, true);
            }

            if (GUILayout.Button("Add ASM Def for Editors"))
            {
                AddAssemblyDefinitionToEditorFolders();
            }

            if (GUILayout.Button("Read Editor Folders from Selection"))
            {
                FindAllEditorFoldersFromSelection();
            }
            
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Explorer"))
            {
                EditorUtility.RevealInFinder($"{LimappExplorer.GetDefaultOutputPath}/");
            }

            if (GUILayout.Button("Open SDK Streaming Asset"))
            {
                EditorUtility.RevealInFinder($"{SDKPath}/StreamingAssets/Limapps/");
            }

            if (GUILayout.Button("Open SDK DLL"))
            {
                EditorUtility.RevealInFinder($"{SDKPath}/TestApp/DLLs/2022/");
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Read from Input"))
            {
                var dllFiles = FindAllDllsInPath(InputDirectory);

                foreach (var dllFile in dllFiles)
                {
                    var asmDef = AssemblyDefinition.ReadAssembly(dllFile);
                    foreach (var item in asmDef.MainModule.AssemblyReferences)
                        Debug.Log(item.FullName);
                }
            }

            // Take a dll and rebuild it with Cecil.
            if (GUILayout.Button("Update DLL"))
            {
                var dllFiles = FindAllDllsInPath(InputDirectory);

                foreach (var dllFile in dllFiles)
                {
                    var asmDef = AssemblyDefinition.ReadAssembly(dllFile);
                    var fileName = Path.GetFileName(dllFile);
                    var output = Path.Combine(OutputDirectory, fileName);
                    EmptyMethodBody(asmDef, "TMPro.Examples.CameraController", "GetPlayerInput");
                    EmptyMethodBody(asmDef, "mitaywalle.ThreeSliceLine.Demo.Scripts.MouseOrbitImproved", "LateUpdate");
                    EmptyMethodBody(asmDef, "SpaceGraphicsToolkit.SgtInputManager", "Poll");
                    //csShowAllEffect.Update
                    AddAssemblyReference(asmDef, output);
                    UpdateDLLWithReferences(asmDef, output);
                    
                    // We want to read and write from the output.
                    asmDef.Write(output);
                    Debug.Log("Assembly updated and saved successfully.");
                }

                // Change UnityEngine.GUIText.Text to UnityEngine.UI.Text
            }

            void EmptyMethodBody(AssemblyDefinition assembly, string classFullName, string methodName)
            {
                var classContainer = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == classFullName);

                if (classContainer == null)
                    return;

                foreach (var method in classContainer.Methods)
                {
                    Debug.Log($"{method.Name}");
                }

                var getPlayerInputMethod = classContainer.Methods.FirstOrDefault(m => m.Name == methodName);
                if (getPlayerInputMethod == null)
                    return;

                DeleteWholeBody();

                Debug.Log($"Deleted {classContainer.FullName}.{getPlayerInputMethod.FullName}()");

                void DeleteWholeBody()
                {
                    var ilProcessor = getPlayerInputMethod.Body.GetILProcessor();
                    getPlayerInputMethod.Body = new MethodBody(getPlayerInputMethod);
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ret)); // Add a return statement to avoid errors
                }
            }

            void UpdateDLLWithReferences(AssemblyDefinition asmDef, string output)
            {
                var newTextType = new TypeReference("UnityEngine.UI", "Text", asmDef.MainModule, asmDef.MainModule.TypeSystem.CoreLibrary);
                var newInputType = asmDef.MainModule.ImportReference(typeof(UnityEngine.Input));

                var overrideTextAssemblyPath = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\Library\ScriptAssemblies\LiminalSdk.dll";
                var overrideTextAssembly = AssemblyDefinition.ReadAssembly(overrideTextAssemblyPath);

                // Assuming asmDef is your target assembly definition that you're modifying
                var overrideTextType = asmDef.MainModule.ImportReference(overrideTextAssembly.MainModule.GetType("Liminal.OverrideTextClass"));
                newTextType = overrideTextType;

                foreach (var module in asmDef.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        ProcessType(type, newTextType, "UnityEngine.GUIText");
                        ProcessType(type, newInputType, "UnityEngine.Input");
                    }
                }

                for (int i = asmDef.MainModule.AssemblyReferences.Count - 1; i >= 0; i--)
                {
                    var reference = asmDef.MainModule.AssemblyReferences[i];
                    if (reference.Name == "UnityEngine.GUIText")
                    {
                        asmDef.MainModule.AssemblyReferences.RemoveAt(i);
                        Debug.Log("Reference moved - UnityEngine.GUIText");
                    }
                }

                foreach (var typeReference in asmDef.MainModule.GetTypeReferences())
                {
                    if (typeReference.FullName == "UnityEngine.GUIText")
                    {
                        Console.WriteLine($"TypeReference to 'GUIText' found: {typeReference}");
                    }
                }

                // Go through all types in the assembly
                foreach (var typeDefinition in asmDef.MainModule.GetTypes())
                {
                    // Inspect fields, methods, etc.
                    foreach (var field in typeDefinition.Fields)
                    {
                        if (field.FieldType.FullName == "UnityEngine.GUIText")
                        {
                            Console.WriteLine($"Field '{field.Name}' in type '{typeDefinition.Name}' is of type 'GUIText'.");
                        }
                    }
                }


            }

            void ProcessType(TypeDefinition type, TypeReference newType, string typeName)
            {
                if (type.BaseType != null && type.BaseType.FullName == typeName)
                {
                    type.BaseType = newType;
                    Debug.Log($"[Process Type] - {typeName} - Updated");
                }

                foreach (var method in type.Methods)
                {
                    //Debug.Log($"[Process Type Method] {method.Name}");
                    if (method.ReturnType.FullName == typeName)
                    {
                        method.ReturnType = newType;
                        Debug.Log($"[Process Type] - {typeName} - Updated");
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        if (parameter.ParameterType.FullName == typeName)
                        {
                            parameter.ParameterType = newType;
                            Debug.Log($"[Process Type] - {typeName} - Updated");
                        }
                    }

                    //Replace(method, newTextType, typeName);

                    var targetMethod = method;
                    if (!targetMethod.HasBody)
                        return;

                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.ToString().Contains("UnityEngine.GUIText") && typeName == "UnityEngine.GUIText")
                        {
                            Debug.Log($"Instructions {instruction.ToString()}");
                            var methodRef = instruction.Operand as MethodReference;
                            if (methodRef.Name == "set_text" && methodRef.DeclaringType.Name == "GUIText")
                            {
                                // Change the instruction to call the new method
                                //var newMethod = new MethodReference("set_text", methodRef.ReturnType, newType) {HasThis = true};
                                //instruction.Operand = newMethod;
                                
                                instruction.Operand = CloneMethodWithDeclaringType(methodRef, newType);

                                // Create a new MethodReference to the modified method name in the same type
                                /*var newMethodRef = new MethodReference("_set_text_override", methodRef.ReturnType, methodRef.DeclaringType)
                                {
                                    HasThis = methodRef.HasThis // Maintain the instance method flag
                                };

                                // Copy parameters from the original to new method reference
                                foreach (var param in methodRef.Parameters)
                                {
                                    newMethodRef.Parameters.Add(new ParameterDefinition(param.ParameterType));
                                }

                                // Replace the operand with the new method reference
                                instruction.Operand = newMethodRef;*/

                                Debug.Log($"Found instruction, changed to {instruction.ToString()}");
                            }
                        }

                        if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
                        {
                            var fieldReference = instruction.Operand as FieldReference;
                            if (fieldReference != null && fieldReference.FieldType.FullName == typeName)
                            {
                                Debug.Log($"[Process Type Method] - {fieldReference.FullName} - Replaced Field. Current Declare Type {fieldReference.DeclaringType.FullName}");

                                // Create a new FieldReference with the new type
                                var newFieldReference = new FieldReference(fieldReference.Name, newType, fieldReference.DeclaringType);
                                instruction.Operand = newFieldReference;
                                // Additional steps might be required to handle type conversions
                            }
                        }
                    }

                    //var ilProcessor = method.Body.GetILProcessor();

                    if (typeName == "UnityEngine.Input")
                    {
                        foreach (var instruction in method.Body.Instructions)
                        {
                            // Check if the instruction is a call to UnityEngine.Input.get_touchCount
                            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference operand)
                            {
                                if (operand.DeclaringType.FullName == "UnityEngine.Input" && operand.Name == "get_touchCount" ||
                                    operand.DeclaringType.FullName == "UnityEngine.Input" && operand.Name == "get_touches")
                                    instruction.Operand = CloneMethodWithDeclaringType(operand, newType);
                            }
                        }
                    }

                }

                foreach (var field in type.Fields)
                {
                    if (field.FieldType.FullName == typeName)
                    {
                        field.FieldType = newType;
                        Debug.Log($"[Process Type] - {typeName} - Updated");
                    }
                }

                foreach (var property in type.Properties)
                {
                    if (property.PropertyType.FullName == typeName)
                    {
                        property.PropertyType = newType;
                        Debug.Log($"[Process Type] - {typeName} - Updated");
                    }
                }

                // Recursively process nested types
                foreach (var nestedType in type.NestedTypes)
                    ProcessType(nestedType, newType, typeName);
            }

            void AddAssemblyReference(AssemblyDefinition asmDef, string output)
            {
                //asmDef.Name.Name = asmDef.Name.Name.Replace(ipp, version);
                var reference = AssemblyNameReference.Parse("UnityEngine.InputLegacyModule, Version=0.0.0.0");
                asmDef.MainModule.AssemblyReferences.Add(reference);

                var newInputType = asmDef.MainModule.ImportReference(typeof(UnityEngine.Input));
                var newTextClass = asmDef.MainModule.ImportReference(typeof(Liminal.OverrideTextClass));

                var methods = GetAllMethodDefinitions(asmDef);
                foreach (var methodDef in methods)
                {
                    Replace(methodDef, newInputType, nameof(UnityEngine.Input));
                    Replace(methodDef, newTextClass, "UnityEngine.GUIText");
                }
            }

            void Replace(MethodDefinition targetMethod, TypeReference replacementTypeRef, string type)
            {
                if (!targetMethod.HasBody)
                    return;

                var methodCalls = targetMethod.Body.Instructions
                    .Where(x => x.OpCode == OpCodes.Call)
                    .ToArray();

                foreach (var instruction in methodCalls)
                {
                    if (instruction.Operand is not MethodReference mRef)
                        continue;

                    if (!mRef.DeclaringType.Name.Equals(type))
                        continue;

                    Debug.Log($"[Replace] - {mRef.Name}, Declare Type {mRef.DeclaringType}");
                    instruction.Operand = CloneMethodWithDeclaringType(mRef, replacementTypeRef);
                }
            }

            MethodReference CloneMethodWithDeclaringType(MethodReference methodRef, TypeReference declaringTypeRef)
            {
                // If the input method reference is generic, it will be wrapped in a GenericInstanceMethod object
                var genericRef = methodRef as GenericInstanceMethod;
                if (genericRef != null)
                {
                    // The actual method data we need to replicate is the ElementMethod
                    // Replace the method reference with the element method from the generic wrapper
                    methodRef = methodRef.GetElementMethod();
                }

                // Build a new method reference that matches the original exactly, but with a different declaring type
                var newRef = new MethodReference(methodRef.Name, methodRef.ReturnType, declaringTypeRef)
                {
                    CallingConvention = methodRef.CallingConvention,
                    HasThis = methodRef.HasThis,
                    ExplicitThis = methodRef.ExplicitThis,
                    MethodReturnType = methodRef.MethodReturnType,
                };

                // Clone method input parameters
                foreach (var p in methodRef.Parameters)
                {
                    newRef.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                }

                // Clone method generic parameters
                foreach (var p in methodRef.GenericParameters)
                {
                    newRef.GenericParameters.Add(new GenericParameter(p.Name, newRef));
                }

                if (genericRef == null)
                {
                    // For non-generic methods, we can simply return the new method reference
                    return newRef;
                }
                else
                {
                    // For generic methods, copy the generic arguments into the new method refernce
                    var newGenericRef = new GenericInstanceMethod(newRef);
                    foreach (var typeDef in genericRef.GenericArguments)
                    {
                        newGenericRef.GenericArguments.Add(typeDef);
                    }

                    // Done
                    return newGenericRef;
                }
            }

            IEnumerable<MethodDefinition> GetAllMethodDefinitions(AssemblyDefinition asmDef)
            {
                return asmDef.Modules
                    .SelectMany(m => m.Types
                        .Concat(m.Types.SelectMany(x => x.NestedTypes))
                        .SelectMany(x => x.Methods)
                    );
            }

            if (GUILayout.Button("Extract"))
            {
                ProcessedFile.Clear();

                var limapps = Directory.GetFiles(InputDirectory);
                EditorCoroutineUtility.StartCoroutineOwnerless(ExtractAll(limapps));
            }

            IEnumerator ExtractAll(string[] paths)
            {
                var limappPaths = paths.Where(x => Path.GetExtension(x) == ".limapp").ToArray();
                for (var i = 0; i < limappPaths.Length; i++)
                {
                    var limappPath = limappPaths[i];

                    if (Path.GetExtension(limappPath) != ".limapp")
                        continue;

                    EditorUtility.DisplayProgressBar("Extracting...", limappPath, i / (float)limappPath.Length);

                    Debug.Log($"Processing: {limappPath}");
                    var bytes = File.ReadAllBytes(limappPath);
                    yield return EditorCoroutineUtility.StartCoroutineOwnerless(ExtractPack(bytes, limappPath));
                }

                EditorUtility.ClearProgressBar();
            }

            string[] FindAllDllsInPath(string path)
            {
                // Search for all DLL files in the specified path. 
                // The search pattern "*.dll" matches all files with a .dll extension.
                // SearchOption.TopDirectoryOnly searches the current directory only, not subdirectories.
                // To include all subdirectories, use SearchOption.AllDirectories.
                try
                {
                    string[] dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
                    return dllFiles;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("An error occurred: " + e.Message);
                    return new string[0]; // Return an empty array if an error occurs
                }
            }

            IEnumerator ExtractPack(byte[] appBytes, string limappPath)
            {
                Debug.Log("Unpacking...");
                var unpacker = new AppUnpacker();
                unpacker.UnpackAsync(appBytes);

                yield return new WaitUntil(() => unpacker.IsDone);

                var fileName = Path.GetFileNameWithoutExtension(limappPath);

                // write all assemblies on disk
                var assmeblies = unpacker.Data.Assemblies;
                //Application.persistentDataPath
                var appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}/{unpacker.Data.TargetPlatform}";

                if (ProcessedFile.Contains(unpacker.Data.ApplicationId))
                    appFolder = $"{OutputDirectory}/{unpacker.Data.ApplicationId}-{fileName}/{unpacker.Data.TargetPlatform}";

                var assemblyFolder = $"{appFolder}/assemblyFolder";
                
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                if (!Directory.Exists(assemblyFolder))
                    Directory.CreateDirectory(assemblyFolder);

                // Wait, in theory, I can rewrite the assembly to match ah, but that's not it.

                for (var i = 0; i < assmeblies.Count; i++)
                {
                    var asmBytes = assmeblies[i];
                    var asm = Assembly.Load(asmBytes);
                    File.WriteAllBytes($"{assemblyFolder}/{asm.GetName()}", asmBytes);
                }

                File.WriteAllBytes($"{appFolder}/appBundle", unpacker.Data.SceneBundle);
                File.WriteAllText($"{appFolder}/manifest.txt", $"Filename: {Path.GetFileName(limappPath)}");

                ProcessedFile.Add(unpacker.Data.ApplicationId);
                Debug.Log("Done!");

                yield break;
            }

        }

        private static void FindAllEditorFoldersFromSelection()
        {
            // Get GUIDs of the selected assets or folders
            string[] selectedGUIDs = Selection.assetGUIDs;

            if (selectedGUIDs.Length == 0)
            {
                Debug.Log("No assets or folders selected.");
                return;
            }

            bool foundAtLeastOneEditorFolder = false;

            foreach (string guid in selectedGUIDs)
            {
                // Convert the GUID to an asset path
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Check and process if the asset path is a valid folder
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    ProcessFolderRecursively(assetPath, ref foundAtLeastOneEditorFolder);
                }
            }

            if (!foundAtLeastOneEditorFolder)
            {
                Debug.Log("No Editor folders found in the selection.");
            }
        }

        private static void ProcessFolderRecursively(string folderPath, ref bool foundAtLeastOneEditorFolder)
        {
            // Check if the folder is an Editor folder
            if (folderPath.Contains("/Editor") || folderPath.EndsWith("/Editor"))
            {
                Debug.Log($"Editor Folder: {folderPath}");
                if (!foundAtLeastOneEditorFolder)
                {
                    foundAtLeastOneEditorFolder = true;
                }
            }

            // Get all subdirectories
            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            foreach (string subFolder in subFolders)
            {
                ProcessFolderRecursively(subFolder, ref foundAtLeastOneEditorFolder);
            }
        }

        private static void AddAssemblyDefinitionToEditorFolders()
        {
            // Get GUIDs of the selected assets or folders
            string[] selectedGUIDs = Selection.assetGUIDs;

            if (selectedGUIDs.Length == 0)
            {
                Debug.Log("No assets or folders selected.");
                return;
            }

            foreach (string guid in selectedGUIDs)
            {
                // Convert the GUID to an asset path
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Check and process if the asset path is a valid folder
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    ProcessFolderRecursively(assetPath);
                }
            }
        }

        private static void ProcessFolderRecursively(string folderPath)
        {
            // Check if the folder is an Editor folder
            if (folderPath.Contains("/Editor") || folderPath.EndsWith("/Editor"))
            {
                CreateAssemblyDefinition(folderPath, false);
            }

            // Get all subdirectories
            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            foreach (string subFolder in subFolders)
            {
                ProcessFolderRecursively(subFolder);
            }
        }

        private static void CreateAssemblyDefinition(string folderPath, bool isPrimary)
        {
            var appManifest = AppBuilder.ReadAppManifest();
            var primaryName = appManifest.Name;

            var relativeFolderPath = folderPath.Substring(folderPath.IndexOf("Assets"));
            var uniqueName = relativeFolderPath.Replace('/', '.').Replace('\\', '.');

            if (isPrimary)
                uniqueName = primaryName;

            var fileName = uniqueName + ".asmdef";
            var fullPath = System.IO.Path.Combine(folderPath, fileName);

            // TODO just make a class!
            var sdkReferences = $"    \"references\": [\"LiminalSdk\"]\n";
            var references = isPrimary ? sdkReferences : $"    \"references\": [\"{primaryName}\"]\n";
            var includePlatform = isPrimary ? "" : $"    \"includePlatforms\": [\"Editor\"],\n";
            var autoReferences = isPrimary ? $"    \"autoReferenced\": true,\n" : $"    \"autoReferenced\": false,\n";

            // if file exist, delete it?

            var overwrite = true;
            if (!System.IO.File.Exists(fullPath))
            {
                var newAsm = new AsmDef
                {
                    Name = uniqueName,
                    AutoReferenced = true,
                    References = new List<string>(),
                    IncludePlatforms = new List<string>()
                };

                if (isPrimary)
                {
                    newAsm.References.Add("LiminalSdk");
                }
                else
                {
                    newAsm.References.Add(primaryName);
                    newAsm.IncludePlatforms.Add("Editor");
                }

                var newAsmJson = JsonConvert.SerializeObject(newAsm, Formatting.Indented);

                // Manually create the JSON string
                /*string assemblyDefinitionContent = $"{{\n" +
                                                   $"    \"name\": \"{uniqueName}\",\n" +
                                                   autoReferences +
                                                   includePlatform +
                                                   references +
                                                   $"}}";*/

                System.IO.File.WriteAllText(fullPath, newAsmJson);
                AssetDatabase.Refresh(); // Ensure the AssetDatabase is refreshed
                AssetDatabase.ImportAsset(fullPath);

                Debug.Log($"Assembly Definition File created at: {fullPath}");
            }
            else
            {
                var asmJson = File.ReadAllText(fullPath);
                var asm = JsonConvert.DeserializeObject<AsmDef>(asmJson);
                Debug.Log($"Assembly Definition File {asm.Name} already exists at: {fullPath}");
            }
        }

        public class AsmDef
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("rootNamespace")]
            public string RootNamespace { get; set; }

            [JsonProperty("references")]
            public List<string> References { get; set; }

            [JsonProperty("includePlatforms")]
            public List<string> IncludePlatforms { get; set; }

            [JsonProperty("excludePlatforms")]
            public List<string> ExcludePlatforms { get; set; }

            [JsonProperty("allowUnsafeCode")]
            public bool AllowUnsafeCode { get; set; }

            [JsonProperty("overrideReferences")]
            public bool OverrideReferences { get; set; }

            [JsonProperty("precompiledReferences")]
            public List<string> PrecompiledReferences { get; set; }

            [JsonProperty("autoReferenced")]
            public bool AutoReferenced { get; set; }

            [JsonProperty("defineConstraints")]
            public List<string> DefineConstraints { get; set; }

            [JsonProperty("versionDefines")]
            public List<string> VersionDefines { get; set; }

            [JsonProperty("noEngineReferences")]
            public bool NoEngineReferences { get; set; }
        }

        private void BuildAssetBundle()
        {
            List<AssetBundleBuild> assetBundleDefinitionList = new();
            // Define two asset bundles, populated based on file system structure
            // The first bundle is all the scene files in the Scenes directory (non-recursive)
            {
                AssetBundleBuild ab = new();
                ab.assetBundleName = "Scenes";
                ab.assetNames = Directory.EnumerateFiles("Assets/_Project/" + ab.assetBundleName, "*.unity", SearchOption.TopDirectoryOnly).ToArray();
                assetBundleDefinitionList.Add(ab);
            }
            // The second bundle is all the asset files found recursively in the Meshes directory
            /*{
                AssetBundleBuild ab = new();
                ab.assetBundleName = "Meshes";
                ab.assetNames = RecursiveGetAllAssetsInDirectory("Assets/" + ab.assetBundleName).ToArray();
                assetBundleDefinitionList.Add(ab);
            }*/

            string outputPath = "MyBuild";  // Subfolder of the current project
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            // Assemble all the input needed to perform the build in this structure.
            // The project's current build settings will be used because target and subtarget fields are not filled in
            BuildAssetBundlesParameters buildInput = new()
            {
                outputPath = outputPath,
                options = BuildAssetBundleOptions.AssetBundleStripUnityVersion,
                bundleDefinitions = assetBundleDefinitionList.ToArray()
            };
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildInput);
            // Look at the results
            if (manifest != null)
            {
                foreach (var bundleName in manifest.GetAllAssetBundles())
                {
                    string projectRelativePath = buildInput.outputPath + "/" + bundleName;
                    Debug.Log($"Size of AssetBundle {projectRelativePath} is {new FileInfo(projectRelativePath).Length}");
                }
            }
            else
            {
                Debug.Log("Build failed, see Console and Editor log for details");
            }
        }

        private void BuildAssembly(bool wait = true)
        {
            var scripts = new[] { @$"{Application.dataPath}/_Project/Scripts/IonTestScript.cs" };
            var outputAssembly = @$"{Application.dataPath}/../Limapp-output/Ion.dll";

            Directory.CreateDirectory("Temp/MyAssembly");

            // Create scripts
            foreach (var scriptPath in scripts)
            {
                //var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                //File.WriteAllText(scriptPath, string.Format("using UnityEngine; public class {0} : MonoBehaviour {{ void Start() {{ Debug.Log(\"{0}\"); }} }}", scriptName));
            }

            var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);

            // Exclude a reference to the copy of the assembly in the Assets folder, if any.
            //assemblyBuilder.excludeReferences = new string[] { assemblyProjectPath };

            // Called on main thread
            assemblyBuilder.buildStarted += delegate (string assemblyPath)
            {
                Debug.LogFormat("Assembly build started for {0}", assemblyPath);
            };

            // Called on main thread
            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {
                var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                var warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                Debug.LogFormat("Assembly build finished for {0}", assemblyPath);
                Debug.LogFormat("Warnings: {0} - Errors: {0}", errorCount, warningCount);

                if (errorCount == 0)
                {
                    //File.Copy(outputAssembly, assemblyProjectPath, true);
                    //AssetDatabase.ImportAsset(assemblyProjectPath);
                }
            };

            // Start build of assembly
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat("Failed to start build of assembly {0}!", assemblyBuilder.assemblyPath);
                return;
            }

            if (wait)
            {
                while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
                    System.Threading.Thread.Sleep(10);
            }
        }

        public void DrawDirectorySelection(ref string directoryPath, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(Size.x * 0.15F));
                directoryPath = GUILayout.TextField(directoryPath, GUILayout.Width(Size.x * 0.7F));

                if (GUILayout.Button("...", GUILayout.Width(Size.x * 0.1F)))
                {
                    directoryPath = EditorUtility.OpenFolderPanel("Select Limapp Folder", "", "");
                    directoryPath = DirectoryUtils.ReplaceBackWithForwardSlashes(directoryPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }




    }
}

public static class TempInput
{
    public static void GetKey(KeyCode keycode)
    {
    }
}
