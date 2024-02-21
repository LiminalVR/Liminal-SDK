using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using UnityEditor.SearchService;
using UnityEditorInternal;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace Liminal.SDK.Build
{
    public class LimapPatchWindow : BaseWindowDrawer
    {
        public static string OutputDirectory;
        public static string InputDirectory;
        public static HashSet<int> ProcessedFile = new HashSet<int>();

        private static string SDKPath = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\Assets";

        public override void Draw(BuildWindowConfig config)
        {
            DrawDirectorySelection(ref OutputDirectory, "Output Directory");
            DrawDirectorySelection(ref InputDirectory, "Input Directory");

            var input = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\Assets\TestApp\DLLs\App000000000042.dll";
            var output = @"C:\Work\Liminal\Platform\Liminal-SDK - 2022\Liminal-SDK-Unity-Package\DLLFixes\App000000000042.dll";

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Copy Project DLL"))
            {
                var manifest = AppBuilder.ReadAppManifest();
                var dllPath = $"{Application.dataPath}/../Library/ScriptAssemblies/{manifest.Name}.dll";

                if (File.Exists(dllPath))
                {
                    Debug.Log($"Found dll, {dllPath}");
                    File.Copy(dllPath, $@"C:\Work\Liminal\2022\DLLs\{manifest.Name}.dll", true);
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
                var asmDef = AssemblyDefinition.ReadAssembly(input);
                foreach (var item in asmDef.MainModule.AssemblyReferences)
                    Debug.Log(item.FullName);
            }

            // Take a dll and rebuild it with Cecil.
            if (GUILayout.Button("Update DLL"))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(input);
                AddAssemblyReference(asmDef);

                Test(asmDef);
                // Change UnityEngine.GUIText.Text to UnityEngine.UI.Text
            }

            // Take a dll and rebuild it with Cecil.
            if (GUILayout.Button("Read from Output"))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(output);
                /*foreach (var item in asmDef.MainModule.AssemblyReferences)
                    Debug.Log(item.FullName);

                foreach (var item in asmDef.MainModule.Types)
                    Debug.Log(item.FullName);*/

                foreach (var module in asmDef.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        // Check fields
                        foreach (var field in type.Fields)
                        {
                            if (field.FieldType.FullName == "UnityEngine.GUIText")
                            {
                                Debug.Log($"Field '{field.Name}' in type '{type.FullName}' uses GUIText.");
                            }
                        }

                        // Check method parameters
                        foreach (var method in type.Methods)
                        {
                            foreach (var parameter in method.Parameters)
                            {
                                if (parameter.ParameterType.FullName == "UnityEngine.GUIText")
                                {
                                    Debug.Log($"Method '{method.Name}' in type '{type.FullName}' has a parameter '{parameter.Name}' that uses GUIText.");
                                }
                            }
                        }
                    }

                    foreach (var type in module.Types)
                    {
                        // Existing updates for fields, method parameters, and properties

                        // Update method return types
                        foreach (var method in type.Methods)
                        {
                            if (method.ReturnType.FullName == "UnityEngine.GUIText")
                            {
                                //method.ReturnType = newTextType;
                                Debug.Log($"Return type of method '{method.Name}' in type '{type.FullName}' updated from GUIText to Text.");
                            }

                            // Update local variables
                            if (method.HasBody)
                            {
                                foreach (var variable in method.Body.Variables)
                                {
                                    if (variable.VariableType.FullName == "UnityEngine.GUIText")
                                    {
                                        //variable.VariableType = newTextType;
                                        Debug.Log($"Local variable in method '{method.Name}' in type '{type.FullName}' updated from GUIText to Text.");
                                    }
                                }
                            }
                        }
                    }
                }

                /*var newInputType = asmDef.MainModule.ImportReference(typeof(UnityEngine.UI.Text));
                var methods = GetAllMethodDefinitions(asmDef);
                foreach (var methodDef in methods)
                {
                    Replace(methodDef, newInputType, "UnityEngine.GUIText");
                }*/
            }

            void Test(AssemblyDefinition asmDef)
            {
                var newTextType = asmDef.MainModule.ImportReference(typeof(UnityEngine.UI.Text));

                foreach (var module in asmDef.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        // Check fields
                        foreach (var field in type.Fields)
                        {
                            if (field.FieldType.FullName == "UnityEngine.GUIText")
                            {
                                field.FieldType = newTextType;
                                Debug.Log($"Field '{field.Name}' in type '{type.FullName}' uses GUIText.");
                            }
                        }

                        // Check method parameters
                        foreach (var method in type.Methods)
                        {
                            foreach (var parameter in method.Parameters)
                            {
                                if (parameter.ParameterType.FullName == "UnityEngine.GUIText")
                                {
                                    parameter.ParameterType = newTextType;
                                    Debug.Log($"Method '{method.Name}' in type '{type.FullName}' has a parameter '{parameter.Name}' that uses GUIText.");
                                }
                            }
                        }
                    }
                }

                asmDef.Write(output);
                Debug.Log("Assembly updated and saved successfully.");
            }

            void AddAssemblyReference(AssemblyDefinition asmDef)
            {
                //asmDef.Name.Name = asmDef.Name.Name.Replace(ipp, version);
                var reference = AssemblyNameReference.Parse("UnityEngine.InputLegacyModule, Version=0.0.0.0");
                asmDef.MainModule.AssemblyReferences.Add(reference);

                var newInputType = asmDef.MainModule.ImportReference(typeof(UnityEngine.Input));
                

                var methods = GetAllMethodDefinitions(asmDef);
                foreach (var methodDef in methods)
                {
                    Replace(methodDef, newInputType, nameof(UnityEngine.Input));
                }
                
                //Debug.Log(inputTypeRef.FullName);

                asmDef.Write(output);
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

                    Debug.Log($"{mRef.Name}, Declare Type {mRef.DeclaringType}");
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
